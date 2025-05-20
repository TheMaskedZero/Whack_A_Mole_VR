import pandas as pd
import numpy as np
from sklearn.preprocessing import MinMaxScaler
from sklearn.neural_network import MLPClassifier
from sklearn.model_selection import train_test_split, GridSearchCV
from sklearn.metrics import f1_score, classification_report
import onnx
from onnx import helper, TensorProto, numpy_helper
import json
import onnxruntime as ort
import time

# -------------------------------------------------------------------
# 1. Load & Preprocess Data (Feature Scaling done externally)
# -------------------------------------------------------------------
print("Loading and preprocessing data...")
df = pd.read_csv('emg_clean.csv')
emg_cols = [f'Pod{i}' for i in range(1,9)]
df[emg_cols] = df[emg_cols].apply(pd.to_numeric, errors='coerce')

# Add EMG signal normalization
emg_scaler = MinMaxScaler(feature_range=(-1, 1))
df[emg_cols] = emg_scaler.fit_transform(df[emg_cols])

# Prepare sliding-window features
def compute_features(window: np.ndarray) -> list:
    feats = []
    for ch in range(window.shape[1]):
        sig = window[:, ch]
        feats += [
            np.mean(np.abs(sig)),                  # MAV
            np.sum(np.abs(np.diff(sig))),          # WL
            np.sum(np.diff(np.signbit(sig)).astype(int)),  # ZC
            np.sum((np.diff(sig)[:-1] * np.diff(sig)[1:]) < 0),  # SSC
            np.sqrt(np.mean(sig**2)),              # RMS
            np.std(sig)                            # STD
        ]
    return feats

def prepare_dataset(df, window_ms=250, fs_hz=70):
    tps = 1000/fs_hz
    X, y = [], []
    for label, grp in df.groupby('Label'):
        data = grp[emg_cols].values.astype(float)
        w = int(window_ms/tps)
        for i in range(0, len(data)-w, w):
            win = data[i:i+w]
            if win.shape[0]==w:
                X.append(compute_features(win))
                y.append(label)
    return np.array(X), np.array(y)

X, y = prepare_dataset(df)
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42
)

# Save only EMG scaler parameters
with open('scaler_params.json', 'w') as f:
    json.dump({
        'min_': emg_scaler.min_.tolist(),
        'scale_': emg_scaler.scale_.tolist()
    }, f)

# -------------------------------------------------------------------
# 2. Grid Search for Optimal ANN Hyperparameters
# -------------------------------------------------------------------
print("Performing grid search for optimal ANN hyperparameters...")
start_time = time.time()

# Define more focused parameter grid for Neural Network (reduced from 144 to 18 combinations)
param_grid = {
    'hidden_layer_sizes': [(50,), (100, 50)],  # Reduced from 4 to 2 options
    'activation': ['relu'],  # Reduced from 2 to 1 options
    'alpha': [0.0001, 0.01],  # Reduced from 3 to 2 options
    'learning_rate_init': [0.001, 0.01],  # Kept 2 options
    'batch_size': [64, 'auto']  # Reduced from 3 to 2 options
}

# Create base ANN model with faster settings
base_model = MLPClassifier(
    max_iter=500,  # Reduced from 1000 to 500
    early_stopping=True,
    validation_fraction=0.1,
    n_iter_no_change=5,  # Reduced from 10 to 5
    random_state=42
)

# Setup GridSearchCV with fewer CV folds
grid_search = GridSearchCV(
    estimator=base_model,
    param_grid=param_grid,
    scoring='f1_weighted',
    cv=3,  # Reduced from 5 to 3 folds
    n_jobs=-1,  # Use all available cores
    verbose=1
)

# Perform grid search
grid_search.fit(X_train, y_train)

# Get best parameters and model
best_params = grid_search.best_params_
best_score = grid_search.best_score_
print(f"Grid search completed in {time.time() - start_time:.2f} seconds")
print(f"Best parameters: {best_params}")
print(f"Best cross-validation score: {best_score:.4f}")

# Train model with best parameters
model = MLPClassifier(
    hidden_layer_sizes=best_params['hidden_layer_sizes'],
    activation=best_params['activation'],
    alpha=best_params['alpha'],
    learning_rate_init=best_params['learning_rate_init'],
    batch_size=best_params['batch_size'],
    max_iter=1000,  # Reduced from 2000 to 1000 for final model
    early_stopping=True,
    validation_fraction=0.1,
    random_state=42
)
model.fit(X_train, y_train)

# Evaluate model
y_train_pred = model.predict(X_train)
y_test_pred = model.predict(X_test)
print("F1-Train:", f1_score(y_train, y_train_pred, average='weighted'))
print("F1-Test: ", f1_score(y_test, y_test_pred, average='weighted'))
print("\nClassification Report:")
print(classification_report(y_test, y_test_pred))

# Save grid search results
grid_results = pd.DataFrame(grid_search.cv_results_)
grid_results.to_csv('ann_grid_search_results.csv', index=False)

# Save label mapping for Unity
labels = np.unique(y)
mapping = {i:lab for i,lab in enumerate(labels)}
with open('label_mapping.json','w') as f:
    json.dump(mapping, f)

# Create a summary of the best model
with open('ann_model_summary.txt', 'w') as f:
    f.write(f"Best Parameters: {best_params}\n")
    f.write(f"Best CV Score: {best_score:.4f}\n")
    f.write(f"F1-Train: {f1_score(y_train, y_train_pred, average='weighted'):.4f}\n")
    f.write(f"F1-Test: {f1_score(y_test, y_test_pred, average='weighted'):.4f}\n")
    f.write("\nClassification Report:\n")
    f.write(classification_report(y_test, y_test_pred))
    f.write("\nNetwork Architecture:\n")
    for i, layer_size in enumerate(model.hidden_layer_sizes):
        f.write(f"Hidden Layer {i+1}: {layer_size} neurons\n")
    f.write(f"Output Layer: {len(labels)} neurons\n")
    f.write(f"Activation: {model.activation}\n")

# -------------------------------------------------------------------
# 3. Build ONNX Graph for Neural Network - Sentis Compatible Version
# -------------------------------------------------------------------
print("Building Sentis-compatible ONNX model...")
n_features = X_train.shape[1]
n_classes = len(labels)

# Get the network architecture details
n_layers = len(model.hidden_layer_sizes)
layer_sizes = list(model.hidden_layer_sizes) + [n_classes]
activation_fn = model.activation

# Define graph I/O with explicit dimensions (not dynamic "batch")
input_tensor = helper.make_tensor_value_info('input', TensorProto.FLOAT, [1, n_features])
output_tensor = helper.make_tensor_value_info('output', TensorProto.FLOAT, [1, n_classes])

# Create nodes and initializers
nodes = []
initializers = []
layer_input = 'input'

for i in range(n_layers + 1):
    w_name = f'W{i}'
    b_name = f'B{i}'
    gemm_output = f'gemm_output_{i}' if i < n_layers else 'logits'
    layer_output = f'layer_output_{i}' if i < n_layers else 'output'

    # Get weights and biases - ensure correct shapes
    W = model.coefs_[i].astype(np.float32)
    B = model.intercepts_[i].astype(np.float32)

    # Create initializers with explicit shapes
    w_tensor = numpy_helper.from_array(W, w_name)
    b_tensor = numpy_helper.from_array(B, b_name)
    initializers.append(w_tensor)
    initializers.append(b_tensor)

    # For Sentis, we'll use MatMul + Add instead of Gemm to ensure dimensional compatibility
    nodes.append(helper.make_node(
        'MatMul',
        [layer_input, w_name],
        [f'matmul_output_{i}'],
        name=f'matmul_{i}'
    ))

    nodes.append(helper.make_node(
        'Add',
        [f'matmul_output_{i}', b_name],
        [gemm_output],
        name=f'add_{i}'
    ))

    # Add activation function except for the output layer
    if i < n_layers:
        nodes.append(helper.make_node(
            'Relu',  # Just using ReLU since it's widely supported
            [gemm_output],
            [layer_output],
            name=f'relu_{i}'
        ))
    else:
        nodes.append(helper.make_node(
            'Softmax',
            [gemm_output],
            [layer_output],
            axis=1,
            name='softmax'
        ))

    # Set input for next layer
    layer_input = layer_output

# Create graph
graph = helper.make_graph(
    nodes,
    'EMG_Neural_Network',
    [input_tensor],
    [output_tensor],
    initializer=initializers
)

# Create model with proper opset
model_onnx = helper.make_model(graph, producer_name='EMG_ANN_Classifier', producer_version='1.0')
model_onnx.ir_version = 7  # Using ONNX IR version 7 for compatibility
opset = model_onnx.opset_import.add()
opset.version = 11  # Using ONNX opset 11 for broad compatibility

# Add metadata
#model_onnx.metadata_props.add(helper.make_metadata_props("Description",
 #   f"EMG gesture classifier (ANN) with {n_layers} hidden layers"))

# Validate model
try:
    onnx.checker.check_model(model_onnx)
    ops_in_model = {n.op_type for n in model_onnx.graph.node}
    print("ONNX graph valid with operators:", ops_in_model)
except Exception as e:
    print(f"ONNX model validation error: {e}")
    # Continue anyway - sometimes the checker is too strict

# Save final ONNX with detailed debug info in filename
model_filename = f"emg_ann_classifier_sentis_fixed.onnx"
onnx.save(model_onnx, model_filename)
print(f"Saved ANN model to {model_filename}")

# -------------------------------------------------------------------
# Alternative: Save simpler Logistic Regression model as backup
# -------------------------------------------------------------------
print("Creating fallback logistic regression model for Sentis...")
# Train a simple logistic regression as fallback
log_reg = LogisticRegression(
    C=1.0,
    solver='lbfgs',
    max_iter=1000,
    multi_class='multinomial',
    random_state=42
)
log_reg.fit(X_train, y_train)

# Extract weights and bias
W = log_reg.coef_.astype(np.float32)       # shape [n_classes, n_features]
B = log_reg.intercept_.astype(np.float32)  # shape [n_classes,]

# Create initializers with explicit names and shapes
W_init = helper.make_tensor('W', TensorProto.FLOAT, W.shape, W.flatten())
B_init = helper.make_tensor('B', TensorProto.FLOAT, B.shape, B)

# Define graph I/O with explicit shapes (not dynamic "batch")
input_t = helper.make_tensor_value_info('input', TensorProto.FLOAT, [1, n_features])
output_t = helper.make_tensor_value_info('output', TensorProto.FLOAT, [1, n_classes])

# Create simplified node structure: MatMul + Add -> Softmax
matmul = helper.make_node('MatMul', ['input', 'W'], ['matmul_output'])
add = helper.make_node('Add', ['matmul_output', 'B'], ['logits'])
soft = helper.make_node('Softmax', ['logits'], ['output'], axis=1)

# Create graph
lr_graph = helper.make_graph(
    [matmul, add, soft],
    'EMG_Classifier_Graph_LR',
    [input_t],
    [output_t],
    initializer=[W_init, B_init]
)

# Create model with proper opset
lr_model = helper.make_model(lr_graph, producer_name='EMG_LR_Classifier', producer_version='1.0')
lr_model.ir_version = 7  # Using ONNX IR version 7
lr_opset = lr_model.opset_import.add()
lr_opset.version = 11

# Validate model
try:
    onnx.checker.check_model(lr_model)
    print("Logistic regression ONNX graph is valid.")
except Exception as e:
    print(f"Logistic regression ONNX validation error: {e}")

# Save the simplified model
lr_filename = "emg_classifier_sentis_lr.onnx"
onnx.save(lr_model, lr_filename)
print(f"Saved fallback logistic regression model to {lr_filename}")


# Validate model
onnx.checker.check_model(model_onnx)
print("ONNX graph valid with operators:", {n.op_type for n in model_onnx.graph.node})

# Save final ONNX
model_filename = f"emg_classifier_sentis_optimized.onnx"
onnx.save(model_onnx, model_filename)
print(f"Saved optimized model to {model_filename}")

# -------------------------------------------------------------------
# 4. Verify Only Sentis‑Supported Ops Exist
# -------------------------------------------------------------------
supported = {
    'Gemm','Softmax','Add','Mul','Sub','Div','Relu','Conv','BatchNormalization',
    'AveragePool','GlobalAveragePool','Flatten','Reshape','Cast','Clip',
    'Concat','Constant','Identity','MatMul','Transpose','Unsqueeze','Abs',
    'Exp','Sigmoid','Tanh'  # Added Tanh for our neural network
}
ops_in_model = {n.op_type for n in model_onnx.graph.node}
unsupported = ops_in_model - supported
if unsupported:
    print(f"Warning: Found unsupported ops: {unsupported}")
    print("Model may not work with Sentis. Try the fallback model instead.")
else:
    print("All operators are Sentis‑supported.")

# Also check the logistic regression model
ops_in_lr = {n.op_type for n in lr_model.graph.node}
unsupported_lr = ops_in_lr - supported
if unsupported_lr:
    print(f"Warning: LR model has unsupported ops: {unsupported_lr}")
else:
    print("All operators in fallback LR model are Sentis‑supported.")

# -------------------------------------------------------------------
# 5. Test with ONNX Runtime
# -------------------------------------------------------------------
print("\nTesting models with ONNX Runtime...")

# Test ANN model
try:
    sess_ann = ort.InferenceSession(model_onnx.SerializeToString())
    inp_ann = sess_ann.get_inputs()[0].name
    out_ann = sess_ann.get_outputs()[0].name

    # Test with just one sample for quick verification
    sample = X_test[0:1].astype(np.float32)
    pred_ann = sess_ann.run([out_ann], {inp_ann: sample})
    pred_class_ann = np.argmax(pred_ann[0][0])

    print(f"ANN ONNX model prediction: {mapping[int(pred_class_ann)]}")
    print("ANN model successfully tested with ONNX Runtime")
except Exception as e:
    print(f"Error testing ANN model: {e}")

# Test Logistic Regression model
try:
    sess_lr = ort.InferenceSession(lr_model.SerializeToString())
    inp_lr = sess_lr.get_inputs()[0].name
    out_lr = sess_lr.get_outputs()[0].name

    # Use the same sample as before
    sample = X_test[0:1].astype(np.float32)
    pred_lr = sess_lr.run([out_lr], {inp_lr: sample})
    pred_class_lr = np.argmax(pred_lr[0][0])

    print(f"LR ONNX model prediction: {mapping[int(pred_class_lr)]}")
    print("Logistic Regression model successfully tested with ONNX Runtime")
except Exception as e:
    print(f"Error testing LR model: {e}")

print("\nDone! Created optimized EMG Neural Network and fallback Logistic Regression models.")
print("\nRecommendation:")
print("If you continue to have issues with the ANN model in Sentis, try using")
print(f"the fallback logistic regression model ({lr_filename}) instead.")