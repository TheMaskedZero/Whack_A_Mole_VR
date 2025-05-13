using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Thalmic.Myo;

public class DebugMyo : MonoBehaviour
{
    ThalmicMyo thalmicMyo;

    Myo myo;

    [SerializeField] float[] currentValues;

    bool thalmicMyoEnabled;
    // Start is called before the first frame update
    void Start()
    {
        // Find the ThalmicMyo component (seems to be the wrapper for the Myo device)
        thalmicMyo = FindObjectOfType<ThalmicMyo>();
        if (thalmicMyo == null)
        {
            thalmicMyoEnabled = false;
            Debug.Log("DebugMyo.cs: No ThalmicMyo object found in the scene.");
            return;
        }

        // Grab the Myo API object
        myo = thalmicMyo.internalMyo;
        if (myo == null)
        {
            Debug.LogError("DebugMyo.cs: Myo Not Paired.");
            return;
        }

        // Enable streaming of raw EMG data
        myo.SetStreamEmg(StreamEmg.Enabled);

        // Subscribe to EMG callback
        myo.EmgData += OnEmgData;
    }

    // Update is called once per frame
    void Update()
    {
        // If there was an issue connecting to the Myo band on Start(), retry connection each refresh.
        if (myo == null)
        {
            thalmicMyo = FindObjectOfType<ThalmicMyo>();
            if (thalmicMyo != null && thalmicMyo.internalMyo != null)
            {
                myo = thalmicMyo.internalMyo;
                //myo.SetStreamEmg(StreamEmg.Enabeld);
                myo.EmgData += OnEmgData;
                Debug.Log("DebugMyo.cs: Myo paired. Enabled EMG data streaming.");
            }
        }
    }

    void OnEmgData(object callbackresponder, EmgDataEventArgs args)
    {
        //Debug.Log($"Raw EMG Data: {args.EmgData}.");
    }

    void OnDestroy()
    {
        if (myo != null)
        {
            myo.EmgData -= OnEmgData;
        }
    }
}
