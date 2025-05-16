# Colab setup: install dependencies (if missing)
# !pip install pandas numpy scipy scikit-learn

import pandas as pd
import numpy as np
from scipy import signal
from scipy.stats import skew, kurtosis
from sklearn.preprocessing import StandardScaler, MinMaxScaler
import glob
import os

# Configuration parameters
samples_per_session = 1000  # Approximate 10 seconds of data at 100Hz
min_activity = 5  # Minimum activity threshold
min_samples = samples_per_session // 2  # Minimum samples for valid session

# Feature configuration
feature_configs = {
    'mean': {'fn': np.mean, 'desc': 'Average EMG amplitude'},
    'std': {'fn': np.std, 'desc': 'Signal variation'},
    'rms': {'fn': lambda x: np.sqrt(np.mean(x**2)), 'desc': 'Root mean square'},
    'zl': {'fn': lambda x: ((x[:-1]*x[1:]<0).sum()), 'desc': 'Zero crossings'},
    'wl': {'fn': lambda x: np.sum(np.abs(np.diff(x))), 'desc': 'Waveform length'},
    'dmean': {'fn': lambda x: np.mean(np.diff(x)), 'desc': 'Mean of first derivative'},
    'dstd': {'fn': lambda x: np.std(np.diff(x)), 'desc': 'Std of first derivative'}
}

# Data validation function
def validate_emg_data(df):
    """Validate EMG data format and content"""
    expected_cols = ['Timestamp'] + [f'Pod{i}' for i in range(1,9)] + ['Label']
    missing_cols = [col for col in expected_cols if col not in df.columns]
    if missing_cols:
        raise ValueError(f"Missing columns: {missing_cols}")
    return True

# 1) Load your data
# Replace with your file in Google Drive or upload via Colab UI
file_list = glob.glob('*_RAW.csv')  # or provide the full path

dfs = []
for file in file_list:
    label = os.path.splitext(os.path.basename(file))[0].replace('_RAW', '')  # e.g., 'Fist'
    df = pd.read_csv(file, parse_dates=['Timestamp'])
    df['Label'] = label
    dfs.append(df)

# Combine all into one DataFrame
df = pd.concat(dfs, ignore_index=True)

# Expect columns: ['Timestamp', 'EMG01',..., 'EMG08', 'Label']

# 2) Segment into fixed-size sessions with validation
sessions = []
for label, group in df.groupby('Label'):
    # Validate data format
    validate_emg_data(group)
    
    # Calculate activity level using all EMG channels
    signal_sum = np.abs(group.filter(regex='Pod')).sum(axis=1)
    
    # Split into chunks of samples_per_session
    n_samples = len(group)
    for i in range(0, n_samples, samples_per_session):
        chunk = group.iloc[i:i + samples_per_session]
        
        # Only keep chunks with sufficient samples and activity
        chunk_activity = signal_sum.iloc[i:i + samples_per_session]
        if len(chunk) >= min_samples and chunk_activity.mean() > min_activity:
            sessions.append((label, chunk))

print(f"Found {len(sessions)} sessions:")
for label in df['Label'].unique():
    count = sum(1 for l, _ in sessions if l == label)
    print(f"- {label}: {count} sessions")

# 3) Keep only the active parts of each session
def trim_inner(group):
    if len(group) < samples_per_session // 2:
        print(f"Session too short: {len(group)} samples")
        return None
    
    # Keep the most active 80% of the session
    activity = np.abs(group.filter(regex='Pod')).sum(axis=1)
    threshold = np.percentile(activity, 20)  # Cut bottom 20%
    return group[activity >= threshold]

# Filter out None results from trimming
trimmed = []
for lab, g in sessions:
    trimmed_group = trim_inner(g)
    if trimmed_group is not None and len(trimmed_group) > 0:
        trimmed.append((lab, trimmed_group))

print(f"\nTotal valid sessions after trimming: {len(trimmed)}")
print("Sessions per label after trimming:")
for label in df['Label'].unique():
    count = sum(1 for l, _ in trimmed if l == label)
    print(f"- {label}: {count} sessions")

# 4) Feature engineering per session
feature_list = []
for lab, grp in trimmed:
    # Use Pod1-Pod8 instead of EMG
    vals = grp.filter(regex='Pod').values  # shape (n_samples, 8)
    features = {'Label': lab}
    
    # Per-channel features
    for ch in range(vals.shape[1]):
        x = vals[:,ch]
        features[f'CH{ch+1}_mean'] = np.mean(x)
        features[f'CH{ch+1}_std']  = np.std(x)
        features[f'CH{ch+1}_rms']  = np.sqrt(np.mean(x**2))
        features[f'CH{ch+1}_zl']   = ((x[:-1]*x[1:]<0).sum())  # zero crossings
        features[f'CH{ch+1}_wl']   = np.sum(np.abs(np.diff(x)))  # waveform length
        dx = np.diff(x)
        features[f'CH{ch+1}_dmean'] = np.mean(dx)
        features[f'CH{ch+1}_dstd']  = np.std(dx)
    feature_list.append(features)

# Add debug print to check feature extraction
features_df = pd.DataFrame(feature_list)
print(f"\nExtracted features shape: {features_df.shape}")
print("Columns:", features_df.columns.tolist())

# 5) Feature scaling
if features_df.empty:
    print("No features extracted. Check your session segmentation and input files.")
else:
    scaler = StandardScaler()
    scaled = pd.DataFrame(
        scaler.fit_transform(features_df.drop('Label', axis=1)),
        columns=features_df.columns.drop('Label')
    )
    scaled['Label'] = features_df['Label'].values

    # 6) Save to CSV for ANN training
    features_df.to_csv('emg_features_raw.csv', index=False)
    scaled.to_csv('emg_features_scaled.csv', index=False)

    # Preview
    print(features_df.head(), scaled.head())
