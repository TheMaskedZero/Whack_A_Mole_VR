import pandas as pd
import numpy as np
import glob
import os

# Configuration parameters
samples_per_session = 1000  # Approximate 10 seconds of data at 100Hz
min_activity = 5  # Minimum activity threshold
min_samples = samples_per_session // 2  # Minimum samples for valid session

def validate_emg_data(df):
    """Validate EMG data format and content"""
    expected_cols = ['Timestamp'] + [f'Pod{i}' for i in range(1,9)] + ['Label']
    missing_cols = [col for col in expected_cols if col not in df.columns]
    if missing_cols:
        raise ValueError(f"Missing columns: {missing_cols}")
    return True

# 1) Load data files
file_list = glob.glob('*_RAW.csv')
dfs = []
for file in file_list:
    label = os.path.splitext(os.path.basename(file))[0].replace('_RAW', '')
    df = pd.read_csv(file, parse_dates=['Timestamp'])
    df['Label'] = label
    dfs.append(df)

# Combine all into one DataFrame
df = pd.concat(dfs, ignore_index=True)

# 2) Segment into fixed-size sessions
all_sessions = []
session_counts = {}

for label, group in df.groupby('Label'):
    validate_emg_data(group)
    session_counts[label] = 0
    
    # Calculate activity level
    signal_sum = np.abs(group.filter(regex='Pod')).sum(axis=1)
    
    # Split into chunks
    n_samples = len(group)
    for i in range(0, n_samples, samples_per_session):
        chunk = group.iloc[i:i + samples_per_session]
        chunk_activity = signal_sum.iloc[i:i + samples_per_session]
        
        if len(chunk) >= min_samples and chunk_activity.mean() > min_activity:
            # Add session number column
            chunk['Session'] = session_counts[label]
            session_counts[label] += 1
            all_sessions.append(chunk)

# Combine all sessions and save to single file
if all_sessions:
    clean_df = pd.concat(all_sessions, ignore_index=True)
    clean_df.to_csv('emg_clean.csv', index=False)
    print(f"\nSaved all sessions to emg_clean.csv")
    print(f"Total samples: {len(clean_df)}")

# Print summary
print(f"\nProcessed {len(session_counts)} labels:")
for label, count in session_counts.items():
    print(f"- {label}: {count} sessions")