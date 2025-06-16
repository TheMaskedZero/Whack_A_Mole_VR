# Import required libraries
import pandas as pd
import io
from google.colab import files
import zipfile
from datetime import datetime

"""# Whack-A-Mole VR Log Processor
This notebook processes log files from the Whack-A-Mole VR game, combining them into a readable format.
"""

def process_log_files(uploaded_files):
    """Process uploaded log files and return organized dataframes"""
    
    # Initialize dataframes
    samples_df = None
    events_df = None
    meta_df = None
    
    # Process each uploaded file
    for filename, content in uploaded_files.items():
        # Read the CSV content
        if 'Sample' in filename:
            samples_df = pd.read_csv(io.BytesIO(content), sep=';')
        elif 'Event' in filename:
            events_df = pd.read_csv(io.BytesIO(content), sep=';')
        elif 'Meta' in filename:
            meta_df = pd.read_csv(io.BytesIO(content), sep=';')
    
    # Convert timestamp columns to datetime
    if samples_df is not None:
        samples_df['Timestamp'] = pd.to_datetime(samples_df['Timestamp'])
    if events_df is not None:
        events_df['Timestamp'] = pd.to_datetime(events_df['Timestamp'])
    
    # Sort by timestamp
    if samples_df is not None:
        samples_df = samples_df.sort_values('Timestamp')
    if events_df is not None:
        events_df = events_df.sort_values('Timestamp')
    
    # Calculate time since start for each dataframe
    if samples_df is not None and events_df is not None:
        start_time = min(samples_df['Timestamp'].min(), events_df['Timestamp'].min())
        samples_df['Time'] = (samples_df['Timestamp'] - start_time).dt.total_seconds()
        events_df['Time'] = (events_df['Timestamp'] - start_time).dt.total_seconds()
    
    # Get metadata
    session_info = {}
    if meta_df is not None:
        session_info = {
            'session_id': meta_df['SessionID'].iloc[0],
            'session_duration': meta_df['SessionDuration'].iloc[0],
            'participant_id': meta_df['ParticipantID'].iloc[0]
        }
    
    return {
        'Samples': samples_df,
        'Events': events_df,
        'Meta': meta_df,
        **session_info
    }

def calculate_fps(events_df):
    # Convert Timestamp to datetime if not already
    events_df['Timestamp'] = pd.to_datetime(events_df['Timestamp'])
    
    # Sort by timestamp to ensure chronological order
    events_df = events_df.sort_values('Timestamp')
    
    # Calculate time differences in seconds
    time_diff = events_df['Timestamp'].diff().dt.total_seconds()
    
    # Calculate frame differences
    frame_diff = events_df['Framecount'].diff()
    
    # Calculate instantaneous FPS (frames/time)
    fps = frame_diff / time_diff
    
    # Calculate average FPS
    total_frames = events_df['Framecount'].max() - events_df['Framecount'].min()
    total_time = (events_df['Timestamp'].max() - events_df['Timestamp'].min()).total_seconds()
    average_fps = total_frames / total_time
    
    return {
        'Average FPS': average_fps,
        'Min FPS': fps.min(),
        'Max FPS': fps.max(),
        'Median FPS': fps.median()
    }

# File Upload Cell
print("Upload your log files (you can select multiple files):")
uploaded = files.upload()

# Process Files Cell
processed_data = process_log_files(uploaded)

# Create Summary Cell
summary = {
    'Total Events': len(processed_data['Events']) if 'Events' in processed_data else 0,
    'Total Samples': len(processed_data['Samples']) if 'Samples' in processed_data else 0,
    'Session Start': processed_data['Events']['Time'].min() if 'Events' in processed_data else 'N/A',
    'Session End': processed_data['Events']['Time'].max() if 'Events' in processed_data else 'N/A',
}
summary_df = pd.DataFrame([summary])
print("\nSession Summary:")
display(summary_df)

# FPS Statistics
if 'Events' in processed_data:
    events_df = processed_data['Events']
    fps_stats = calculate_fps(events_df)
    print("\nFPS Statistics:")
    for key, value in fps_stats.items():
        print(f"{key}: {value:.2f}")

# Export Results Cell
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
output_filename = f'processed_logs_{timestamp}.xlsx'

with pd.ExcelWriter(output_filename) as writer:
    # Write each dataframe to a separate sheet
    if 'Events' in processed_data:
        processed_data['Events'].to_excel(writer, sheet_name='Events', index=False)
    if 'Samples' in processed_data:
        processed_data['Samples'].to_excel(writer, sheet_name='Samples', index=False)
    if 'Meta' in processed_data:
        processed_data['Meta'].to_excel(writer, sheet_name='Meta', index=False)
    summary_df.to_excel(writer, sheet_name='Summary', index=False)

files.download(output_filename)