using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class StoreEMG : MonoBehaviour
{
    public static List<DateTime> storeTimestamp;
    public static List<int> storeEMG01 = new List<int>();
    public static List<int> storeEMG02 = new List<int>();
    public static List<int> storeEMG03 = new List<int>();
    public static List<int> storeEMG04 = new List<int>();
    public static List<int> storeEMG05 = new List<int>();
    public static List<int> storeEMG06 = new List<int>();
    public static List<int> storeEMG07 = new List<int>();
    public static List<int> storeEMG08 = new List<int>();
    public static List<DateTime> timestamp = new List<DateTime>();
    public int counter = 0;

    public bool RecordingEMGData;

    void Start()
    {
        StartCoroutine(StoreTheEMGData()); // Start the coroutine to store EMG data
    }

    private IEnumerator StoreTheEMGData()
    {
        while(RecordingEMGData == true)
        {

            if (ThalmicMyo.emg != null && ThalmicMyo.emg.Length >= 8) // Check if EMG data is available
            {
                
                storeData(ThalmicMyo.emg); // Store EMG data
                

                Debug.Log("EMG data stored: " + storeEMG01[storeEMG01.Count - 1] + ", " + storeEMG02[storeEMG02.Count - 1] + ", " + storeEMG03[storeEMG03.Count - 1] + ", " + storeEMG04[storeEMG04.Count - 1] + ", " + storeEMG05[storeEMG05.Count - 1] + ", " + storeEMG06[storeEMG06.Count - 1] + ", " + storeEMG07[storeEMG07.Count - 1] + ", " + storeEMG08[storeEMG08.Count - 1]);
            }
            else
            {
                Debug.LogWarning("EMG data is incomplete or null. Skipping this iteration.");
            }
            
            yield return new WaitForSecondsRealtime(0.01f); // Wait for 10ms before storing the next data point
            

        }
    }

    public void Update() {
        //storeData(ThalmicMyo.emg);
    }

    public void storeData(int[] emg)
    {
        // Ensure the EMG array has at least 8 elements before accessing it
        if (emg != null && emg.Length >= 8)
        {
            if (counter > 2)
            {
                // Store data in lists
                storeEMG01.Add(emg[0]);   // Get current EMG
                storeEMG02.Add(emg[1]);
                storeEMG03.Add(emg[2]);
                storeEMG04.Add(emg[3]);
                storeEMG05.Add(emg[4]);
                storeEMG06.Add(emg[5]);
                storeEMG07.Add(emg[6]);
                storeEMG08.Add(emg[7]);

                timestamp.Add(DateTime.Now);   // Get current local time and date

                counter++;
            }
            else
            {
                counter++;
            }
        }
        else
        {
            Debug.LogWarning("EMG data is incomplete or null. Skipping this iteration.");
        }
    }

    void OnDestroy()
    {
        // Stop the coroutine when the object is destroyed
        StopCoroutine(StoreTheEMGData());
    }
}