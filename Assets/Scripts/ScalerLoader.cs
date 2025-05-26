// Generative AI (ChatGPT, 4.o mini) was used while writing this script.
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Helper script to load scaler parameters from a JSON file at runtime
/// </summary>
public class ScalerLoader : MonoBehaviour
{
    [SerializeField] private EMGInferencer emgInferencer;
    [SerializeField] private string scalerJsonPath = "scaler_params.json";
    
    // Optional: for Unity editor, allow direct setting of a TextAsset
    [SerializeField] private TextAsset scalerParamsAsset;

    [System.Serializable]
    public class ScalerParams
    {
        public float[] mean;
        public float[] scale;
    }

    private void Awake()
    {
        if (emgInferencer == null)
        {
            emgInferencer = GetComponent<EMGInferencer>();
            if (emgInferencer == null)
            {
                Debug.LogError("EMGInferencer component not found!");
                return;
            }
        }

        // Try to load scaler parameters from TextAsset first
        if (scalerParamsAsset != null)
        {
            // The EMGInferencer already handles TextAsset loading
            return;
        }

        // Otherwise, try to load from file
        LoadScalerFromFile();
    }

    public void LoadScalerFromFile()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, scalerJsonPath);
        
        if (File.Exists(filePath))
        {
            try
            {
                string jsonText = File.ReadAllText(filePath);
                var scalerParams = JsonUtility.FromJson<ScalerParams>(jsonText);
                
                // Now you could apply these parameters to EMGInferencer
                Debug.Log($"Loaded scaler parameters from file: {filePath}");
                Debug.Log($"Mean values: {scalerParams.mean.Length}, Scale values: {scalerParams.scale.Length}");
                
                // You would need to add a public method to EMGInferencer to set these parameters
                // Or modify EMGInferencer to load them itself
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load or parse scaler parameters: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Scaler parameters file not found at: {filePath}");
        }
    }
}