using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Thalmic.Myo;
using UnityEngine.UI;
using TMPro;  // Add this at the top with other using statements

public class EMGInferencer : MonoBehaviour
{
    // Model and scaling parameters
    public ModelAsset modelAsset;                   // Reference to the ONNX model asset
    public TextAsset scalerParamsJson;              // Reference to the StandardScaler parameters
    private IWorker worker;                         // Sentis worker for model execution
    private Model runtimeModel;                     // Runtime representation of the ONNX model

    [SerializeField] private TactorConnector tactorInterface;       // Reference to the TactorInterface for feedback

    [Header("Model Classification Thresholds")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float restingThreshold;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float pinchThreshold;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float graspThreshold;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float uncertainThreshold;

    // Classification labels
    private readonly string[] gestureLabels = new string[] { "Fist", "Pinch", "Resting" };

    // EMG processing constants
    private const int WINDOW_SIZE = 18;             // 250ms window at 70Hz sampling rate
    private const int NUM_FEATURES = 48;            // 6 features * 8 EMG channels

    // Data buffers and processing
    private Queue<int[]> emgBuffer;                 // Circular buffer for EMG samples
    private List<float> features;                   // Computed features for classification
    private float[] scalerMean;                     // StandardScaler mean values
    private float[] scalerScale;                    // StandardScaler scale values
    private int[] lastEMGSample = null;

    // Debug and visualization
    [Header("Debug")]
    public bool showDebugInfo = true;               // Toggle debug overlay
    [HideInInspector]
    public string currentGesture = "Waiting for data...";
    [HideInInspector]
    public float currentConfidence = 0f;

    // Modify the class fields at the top
    private string inputTensorName;  // Will store model's input name
    private string outputTensorName; // Will store model's output name
    private bool isPredicting = false;                  // Flag to prevent concurrent predictions

    [Header("UI Elements")]
    public Text predictionText;  // Add this field for the UI text component

    // Add these fields at the top of the class with other private fields
    private Queue<string> predictionHistory = new Queue<string>();
    private const int PREDICTION_POOL_SIZE = 5;
    private string lastStablePrediction = "Waiting for data...";

    /// <summary>
    /// Initializes the EMG processing pipeline, loads the model and scaler parameters.
    /// </summary>
    void Start()
    {
        Debug.Log("=== Starting EMG Inferencer Initialization ===");
        
        // Initialize EMG buffer
        emgBuffer = new Queue<int[]>();
        features = new List<float>();
        Debug.Log("Initialized EMG buffer and features list");

        // Load scaler parameters
        LoadScalerParams();
        
        // Load and initialize the ONNX model
        Debug.Log($"Loading EMG classifier model from asset: {modelAsset != null}");
        runtimeModel = ModelLoader.Load(modelAsset);

        // Set the exact input/output tensor names used by the model
        inputTensorName = "input";
        outputTensorName = "output";
        
        Debug.Log($"Using input tensor name: {inputTensorName}");
        Debug.Log($"Using output tensor name: {outputTensorName}");

        // Create worker with appropriate backend
        try
        {
            worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);
            Debug.Log("Successfully initialized GPU backend for inference");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GPU backend failed: {e.Message}. Falling back to CPU.");
            worker = WorkerFactory.CreateWorker(BackendType.CPU, runtimeModel);
            Debug.Log("Successfully initialized CPU backend for inference");
        }
        Debug.Log("=== EMG Inferencer Initialization Complete ===");
    }

    /// <summary>
    /// Loads and validates the StandardScaler parameters from JSON.
    /// </summary>
    private void LoadScalerParams()
    {
        Debug.Log("=== Loading Scaler Parameters ===");
        if (scalerParamsJson != null)
        {
            try
            {
                var scalerParams = JsonUtility.FromJson<ScalerParams>(scalerParamsJson.text);
                scalerMean = scalerParams.mean;
                scalerScale = scalerParams.scale;
                Debug.Log($"Loaded scaler parameters successfully:");
                Debug.Log($"Mean array length: {scalerMean.Length}");
                Debug.Log($"Scale array length: {scalerScale.Length}");
                Debug.Log($"Sample values - Mean[0]: {scalerMean[0]}, Scale[0]: {scalerScale[0]}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scaler parameters: {e.Message}\nStack trace: {e.StackTrace}");
                // Initialize with zeros as fallback
                scalerMean = new float[NUM_FEATURES];
                scalerScale = Enumerable.Repeat(1f, NUM_FEATURES).ToArray();
                Debug.Log("Initialized fallback scaler parameters");
            }
        }
        else
        {
            Debug.LogWarning("No scaler parameters provided. Using default values.");
            scalerMean = new float[NUM_FEATURES];
            scalerScale = Enumerable.Repeat(1f, NUM_FEATURES).ToArray();
        }
    }

    /// <summary>
    /// Updates the EMG buffer with new samples and triggers inference when enough data is collected.
    /// </summary>
    void Update()
    {
        // Skip if we're currently processing a prediction
        if (isPredicting)
        {
            return;
        }

        // Get latest EMG data from ThalmicMyo
        var emgData = ThalmicMyo.emg;
        if (emgData != null && emgData.Length == 8)
        {
            //Check whether EMG data is different than the previous sample
            if (lastEMGSample == null || !emgData.SequenceEqual(lastEMGSample))
            {
                lastEMGSample = (int[])emgData.Clone(); // Update lastEmgSample

                // Add new EMG data to buffer
                emgBuffer.Enqueue((int[])emgData.Clone());

                // Keep buffer size limited to window size
                while (emgBuffer.Count > WINDOW_SIZE)
                {
                    emgBuffer.Dequeue();
                }
            }

            // When we have enough samples, compute features and run inference
            if (emgBuffer.Count == WINDOW_SIZE)
            {
                isPredicting = true;
                RunInference();
            

                // Instead of clearing the entire buffer, remove half of the samples
                int samplesToRemove = WINDOW_SIZE / 2;
                for (int i = 0; i < samplesToRemove; i++)
                {
                    emgBuffer.Dequeue();
                }
                isPredicting = false;

                
            }
           /* // Add new EMG data to buffer
            emgBuffer.Enqueue((int[])emgData.Clone());

            // Keep buffer size limited to window size
            while (emgBuffer.Count > WINDOW_SIZE)
            {
                emgBuffer.Dequeue();
            }

            // When we have enough samples, compute features and run inference
            if (emgBuffer.Count == WINDOW_SIZE)
            {
                isPredicting = true;  // Raise the flag
                RunInference();
                
            }*/
        }
    }

    /// <summary>
    /// Performs gesture classification on the current EMG window.
    /// Extracts features, applies scaling, and runs the neural network model.
    /// </summary>
    private void RunInference()
    {
        Debug.Log("=== Starting Inference ===");
        
        // Convert buffer to 2D array for feature extraction
        var window = new float[WINDOW_SIZE, 8];
        var emgArray = emgBuffer.ToArray();
        
        // Populate the window with EMG data
        for (int i = 0; i < emgArray.Length; i++)
        {
            for (int channel = 0; channel < 8; channel++)
            {
                window[i, channel] = emgArray[i][channel];
            }
        }

        // Log first sample of EMG data
       

        // Compute features
        features.Clear();
        for (int channel = 0; channel < 8; channel++)
        {
            var signal = GetChannelData(window, channel);
            
            // Mean Absolute Value (MAV)
            features.Add(signal.Select(x => Mathf.Abs(x)).Average());
            
            // Waveform Length (WL)
            features.Add(GetWaveformLength(signal));
            
            // Zero Crossings (ZC)
            features.Add(GetZeroCrossings(signal));
            
            // Slope Sign Changes (SSC)
            features.Add(GetSlopeSignChanges(signal));
            
            // Root Mean Square (RMS)
            features.Add(Mathf.Sqrt(signal.Select(x => x * x).Average()));
            
            // Standard Deviation (STD)
            features.Add(GetStandardDeviation(signal));
        }

        Debug.Log($"Computed {features.Count} features");
        Debug.Log($"Feature sample [0-2]: [{string.Join(", ", features.Take(3))}]");

        // After scaling
        var scaledFeatures = new float[NUM_FEATURES];
        for (int i = 0; i < NUM_FEATURES; i++)
        {
            scaledFeatures[i] = (features[i] - scalerMean[i]) / scalerScale[i];
        }
        Debug.Log($"Scaled features sample [0-2]: [{string.Join(", ", scaledFeatures.Take(3))}]");

        // Create and execute tensor
        using TensorFloat inputTensor = new TensorFloat(new TensorShape(1, NUM_FEATURES), scaledFeatures);
        Debug.Log($"Created input tensor with shape: (1, {NUM_FEATURES})");

        // Feed the tensor to the model
        worker.SetInput(inputTensorName, inputTensor);
        
        try
        {
            worker.Execute();
            Debug.Log("Model execution completed successfully");

            // Retrieve and read the "prob" output
            TensorFloat outputTensor = worker.PeekOutput(outputTensorName) as TensorFloat;
            outputTensor.MakeReadable();
            float[] probabilities = outputTensor.ToReadOnlyArray();
            
            Debug.Log($"Output probabilities: [{string.Join(", ", probabilities)}]");
            
            // Interpret results
            int predictedClass = System.Array.IndexOf(probabilities, probabilities.Max());
            float maxProb = probabilities.Max();

            if (predictedClass == 2 && maxProb < restingThreshold)
            {
                if (showDebugInfo == true)
                {
                    Debug.Log("Resting predicted, but below threshold.");
                }
                return;
            }

            if ((predictedClass == 0 || predictedClass == 1) && maxProb < uncertainThreshold)
            {
                if (showDebugInfo == true)
                {
                    Debug.Log($"Low confidence in class: {maxProb} detection. Defaulting to resting.");
                }
                predictedClass = 2;
                maxProb = probabilities[2];
            }

            if (predictedClass == 0 && maxProb < graspThreshold)
            {
                if (showDebugInfo == true)
                {
                    Debug.Log("Grasp detected, but below threshold.");
                }
                return;
            }
            else if (predictedClass == 1 && maxProb < pinchThreshold)
            {
                if (showDebugInfo == true)
                {
                    Debug.Log("Pinch detected, but below threshold.");
                }
                return;
            }
            
            // After getting the prediction results:
            if (predictedClass >= 0 && predictedClass < gestureLabels.Length)
            {
                currentGesture = gestureLabels[predictedClass];
                currentConfidence = probabilities[predictedClass];

                // Add to prediction history
                predictionHistory.Enqueue(currentGesture);
                if (predictionHistory.Count > PREDICTION_POOL_SIZE)
                {
                    predictionHistory.Dequeue();
                }

                // Get most common prediction from the pool
                if (predictionHistory.Count == PREDICTION_POOL_SIZE)
                {
                    var mostCommon = predictionHistory
                        .GroupBy(x => x)
                        .OrderByDescending(g => g.Count())
                        .First();

                    // Only update if the most common prediction has occurred at least twice
                    if (mostCommon.Count() >= 2)
                    {
                        lastStablePrediction = mostCommon.Key;
                        switch (lastStablePrediction)
                        {
                            case "Fist":
                                tactorInterface.TriggerGraspingStateFeedback();
                                break;
                            case "Pinch":
                                tactorInterface.TriggerPinchingStateFeedback();
                                break;
                            case "Resting":
                                tactorInterface.TriggerRestingStateFeedback();
                                break;
                            default:
                                break;
                        }
                    }
                }

                // Update UI with the stable prediction
                if (predictionText != null)
                {
                    predictionText.text = $"Gesture: {lastStablePrediction}\nConfidence: {currentConfidence:P2}";
                }
                
                Debug.Log($"Raw Prediction: {currentGesture} (confidence: {currentConfidence:F4})");
                Debug.Log($"Stable Prediction: {lastStablePrediction}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Model execution failed: {e.Message}");
            return;
        }
        finally
        {
            isPredicting = false;
        }
        
        Debug.Log("=== Inference Complete ===\n");
    }

    /// <summary>
    /// Extracts data for a specific EMG channel from the processing window.
    /// </summary>
    /// <param name="window">2D array containing EMG data for all channels</param>
    /// <param name="channel">Channel index to extract (0-7)</param>
    /// <returns>Array of EMG values for the specified channel</returns>
    private float[] GetChannelData(float[,] window, int channel)
    {
        float[] channelData = new float[WINDOW_SIZE];
        for (int i = 0; i < WINDOW_SIZE; i++)
        {
            channelData[i] = window[i, channel];
        }
        return channelData;
    }

    /// <summary>
    /// Calculates the Waveform Length feature for an EMG signal.
    /// Measures the cumulative length of the waveform over the time segment.
    /// </summary>
    /// <param name="signal">EMG signal array</param>
    /// <returns>Waveform length value</returns>
    private float GetWaveformLength(float[] signal)
    {
        float wl = 0;
        for (int i = 1; i < signal.Length; i++)
        {
            wl += Mathf.Abs(signal[i] - signal[i-1]);
        }
        return wl;
    }

    /// <summary>
    /// Counts the number of zero crossings in the EMG signal.
    /// Indicates frequency content of the signal.
    /// </summary>
    /// <param name="signal">EMG signal array</param>
    /// <returns>Number of zero crossings</returns>
    private float GetZeroCrossings(float[] signal)
    {
        int zc = 0;
        for (int i = 1; i < signal.Length; i++)
        {
            if (signal[i] * signal[i-1] < 0)
            {
                zc++;
            }
        }
        return zc;
    }

    /// <summary>
    /// Counts the number of slope sign changes in the EMG signal.
    /// Indicates frequency content and complexity of the signal.
    /// </summary>
    /// <param name="signal">EMG signal array</param>
    /// <returns>Number of slope sign changes</returns>
    private float GetSlopeSignChanges(float[] signal)
    {
        int ssc = 0;
        for (int i = 1; i < signal.Length - 1; i++)
        {
            float diff1 = signal[i] - signal[i-1];
            float diff2 = signal[i+1] - signal[i];
            if (diff1 * diff2 < 0)
            {
                ssc++;
            }
        }
        return ssc;
    }

    /// <summary>
    /// Calculates the standard deviation of the EMG signal.
    /// Measures the spread of the signal amplitudes.
    /// </summary>
    /// <param name="signal">EMG signal array</param>
    /// <returns>Standard deviation value</returns>
    private float GetStandardDeviation(float[] signal)
    {
        float mean = signal.Average();
        float sumSquares = signal.Select(x => (x - mean) * (x - mean)).Sum();
        return Mathf.Sqrt(sumSquares / (signal.Length - 1));
    }

    /// <summary>
    /// Cleans up resources when the component is disabled.
    /// </summary>
    void OnDisable()
    {
        if (worker != null)
        {
            worker.Dispose();
        }
    }

    /// <summary>
    /// Draws debug information overlay when enabled.
    /// </summary>
    void OnGUI()
    {
        if (showDebugInfo)
        {
            GUI.Label(new Rect(10, 10, 300, 30), $"Gesture: {currentGesture}");
            GUI.Label(new Rect(10, 40, 300, 30), $"Confidence: {currentConfidence:F2}");
        }
    }

    /// <summary>
    /// Structure for deserializing StandardScaler parameters from JSON.
    /// </summary>
    [System.Serializable]
    private class ScalerParams
    {
        public float[] mean;    // Mean values for each feature
        public float[] scale;   // Scale values for each feature
    }
}