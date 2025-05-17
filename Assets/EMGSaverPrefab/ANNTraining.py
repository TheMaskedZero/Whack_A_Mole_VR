import pandas as pd
import numpy as np
from scipy import stats
import matplotlib.pyplot as plt
from sklearn.preprocessing import StandardScaler
from sklearn.neural_network import MLPClassifier
from sklearn.model_selection import train_test_split
from sklearn.metrics import f1_score, classification_report
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType


# Read the data and convert EMG columns to numeric
df = pd.read_csv('emg_clean.csv')
emg_columns = ['Pod1', 'Pod2', 'Pod3', 'Pod4', 'Pod5', 'Pod6', 'Pod7', 'Pod8']
df[emg_columns] = df[emg_columns].apply(pd.to_numeric, errors='coerce')

# Add data validation
print("Data types:", df[emg_columns].dtypes)
print("\nMissing values:", df[emg_columns].isnull().sum())
print("\nSample of numeric data:\n", df[emg_columns].head())

# Function to compute features for a window of EMG data
def compute_features(window):
    features = []

    # Verify data type
    if not np.issubdtype(window.dtype, np.number):
        raise TypeError(f"Expected numeric data, got {window.dtype}")

    # For each channel
    for channel in range(8):
        signal = window[:, channel]

        # Mean Absolute Value (MAV)
        mav = np.mean(np.abs(signal))

        # Waveform Length (WL)
        wl = np.sum(np.abs(np.diff(signal)))

        # Zero Crossings (ZC)
        zc = np.sum(np.diff(np.signbit(signal)).astype(int))

        # Slope Sign Changes (SSC)
        diff = np.diff(signal)
        ssc = np.sum((diff[:-1] * diff[1:]) < 0)

        # Root Mean Square (RMS)
        rms = np.sqrt(np.mean(signal**2))

        # Standard Deviation (STD)
        std = np.std(signal)

        features.extend([mav, wl, zc, ssc, rms, std])

    return features

# Function to create windows and extract features
def prepare_dataset(df, window_size_ms=250):
    # Convert index to time assuming 70Hz sampling rate
    time_per_sample = 1000/70  # in milliseconds

    # Ensure EMG columns are numeric
    emg_columns = ['Pod1', 'Pod2', 'Pod3', 'Pod4', 'Pod5', 'Pod6', 'Pod7', 'Pod8']
    df[emg_columns] = df[emg_columns].astype(float)

    # Group by label
    groups = df.groupby('Label')

    X = []
    y = []

    for label, group in groups:
        # Convert group to numpy array for EMG channels
        emg_data = group[emg_columns].values.astype(float)  # Explicit conversion to float

        # Create windows
        n_samples = len(emg_data)
        samples_per_window = int(window_size_ms / time_per_sample)

        for i in range(0, n_samples - samples_per_window, samples_per_window):
            window = emg_data[i:i+samples_per_window]
            if len(window) == samples_per_window:  # ensure complete window
                features = compute_features(window)
                X.append(features)
                y.append(label)

    return np.array(X), np.array(y)

# Prepare the dataset
X, y = prepare_dataset(df)

# Split the data
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

# Scale the features
scaler = StandardScaler()
X_train_scaled = scaler.fit_transform(X_train)
X_test_scaled = scaler.transform(X_test)

# Create and train the MLP
mlp = MLPClassifier(hidden_layer_sizes=(100, 50),
                    max_iter=1000,
                    activation='relu',
                    solver='adam',
                    random_state=42)

mlp.fit(X_train_scaled, y_train)

# Make predictions
y_pred_train = mlp.predict(X_train_scaled)
y_pred_test = mlp.predict(X_test_scaled)

# Calculate F1 scores
f1_train_weighted = f1_score(y_train, y_pred_train, average='weighted')
f1_test_weighted = f1_score(y_test, y_pred_test, average='weighted')

# Print weighted F1 scores
print("Weighted F1 Scores:")
print(f"Training F1: {f1_train_weighted:.3f}")
print(f"Testing F1: {f1_test_weighted:.3f}")

# Print detailed classification report
print("\nDetailed Classification Report:")
print("\nTraining Set:")
print(classification_report(y_train, y_pred_train))
print("\nTest Set:")
print(classification_report(y_test, y_pred_test))

# Optional: Plot F1 scores per class
def plot_f1_scores(y_true, y_pred, title):
    report = classification_report(y_true, y_pred, output_dict=True)
    classes = [key for key in report.keys() if key not in ['accuracy', 'macro avg', 'weighted avg']]
    f1_scores = [report[cls]['f1-score'] for cls in classes]

    plt.figure(figsize=(8, 4))
    plt.bar(classes, f1_scores)
    plt.title(f'F1 Scores by Class - {title}')
    plt.ylabel('F1 Score')
    plt.ylim(0, 1)
    plt.show()

# Plot F1 scores
plot_f1_scores(y_test, y_pred_test, 'Test Set')

# Export model to ONNX
# Display label mapping information
unique_labels = np.unique(y)
label_mapping = {idx: label for idx, label in enumerate(unique_labels)}
print("\nLabel Mapping (for ONNX model):")
print("Index -> Label")
print("-" * 20)
for idx, label in label_mapping.items():
    print(f"{idx} -> {label}")

# Define the input type for the model
initial_type = [('float_input', FloatTensorType([None, X_train.shape[1]]))]

# Convert the model to ONNX
onx = convert_sklearn(mlp, initial_types=initial_type)

# Save the model and mapping
model_path = 'emg_classifier.onnx'
mapping_path = 'label_mapping.txt'

# Save the ONNX model
with open(model_path, "wb") as f:
    f.write(onx.SerializeToString())

# Save the label mapping to a separate file
with open(mapping_path, "w") as f:
    for idx, label in label_mapping.items():
        f.write(f"{idx},{label}\n")

print(f"\nModel saved as: {model_path}")
print(f"Label mapping saved as: {mapping_path}")
print("\nAvailable gesture labels:", np.unique(y))