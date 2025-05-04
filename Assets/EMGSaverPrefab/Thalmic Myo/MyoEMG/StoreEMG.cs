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

    public void Update() {
        storeData(ThalmicMyo.emg);
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
}