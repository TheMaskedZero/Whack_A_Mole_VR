using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Globalization;
using TMPro; // Add this at the top

public class StoreEMG : MonoBehaviour
{
    // --- Public static storage lists (unchanged) ---
    public static List<int> storeEMG01 = new List<int>();
    public static List<int> storeEMG02 = new List<int>();
    public static List<int> storeEMG03 = new List<int>();
    public static List<int> storeEMG04 = new List<int>();
    public static List<int> storeEMG05 = new List<int>();
    public static List<int> storeEMG06 = new List<int>();
    public static List<int> storeEMG07 = new List<int>();
    public static List<int> storeEMG08 = new List<int>();
    public static List<DateTime> timestamp = new List<DateTime>();

    [SerializeField] private int initialBufferSize = 1000;
    [SerializeField] private int maxBufferSize = 2000; // Optional: warn if exceeded
    

    private Queue<int[]> emgBuffer;
    private Queue<DateTime> timeBuffer;
    private Coroutine emgCoroutine;
    private bool isRecording = false;

    private string lastSavedPath = "";

    [SerializeField] private TMP_Dropdown graspTypeDropdown; // Assign in Inspector

    void Awake()
    {
        emgBuffer = new Queue<int[]>(initialBufferSize);
        timeBuffer = new Queue<DateTime>(initialBufferSize);
    }

    // Always start listening in Start()
    void Start()
    {
        if (emgCoroutine == null)
            emgCoroutine = StartCoroutine(RecordEMGData());
    }

    // Call this to start recording
    public void StartEMGRecording()
    {
        // Clear any leftover samples from previous session
        emgBuffer.Clear();
        timeBuffer.Clear();

        // Clear static lists as well
        storeEMG01.Clear();
        storeEMG02.Clear();
        storeEMG03.Clear();
        storeEMG04.Clear();
        storeEMG05.Clear();
        storeEMG06.Clear();
        storeEMG07.Clear();
        storeEMG08.Clear();
        timestamp.Clear();

        isRecording = true;
        Debug.Log("[StoreEMG] Recording started.");
    }

    // Call this to stop and save (filename can be set by UI or SaveRoutine)
    public void StopAndSaveEMG(string filename = null)
    {
        if (emgCoroutine != null)
        {
            StopCoroutine(emgCoroutine);
            emgCoroutine = null;
            isRecording = false;
            Debug.Log("[StoreEMG] Recording stopped. Flushing buffer and saving...");

            // Use dropdown for filename if available
            string fileToUse = filename;
            if (graspTypeDropdown != null)
            {
                string graspType = graspTypeDropdown.options[graspTypeDropdown.value].text;
                fileToUse = $"{graspType}_RAW.csv";
            }
            else if (string.IsNullOrEmpty(filename))
            {
                //fileToUse = defaultFilename;
            }

            FlushBufferAndSave(fileToUse);
        }
    }

    public void StopAndSaveEMG()
    {
        if (isRecording)
        {
            isRecording = false;
            Debug.Log("[StoreEMG] Recording stopped. Flushing buffer and saving...");

            string graspType = graspTypeDropdown.options[graspTypeDropdown.value].text;
            string fileToUse = $"{graspType}_RAW.csv";

            FlushBufferAndSave(fileToUse);
        }
    }

    // Only buffer when recording is active
    private IEnumerator RecordEMGData()
    {
        while (true)
        {
            var emg = ThalmicMyo.emg;
            if (emg != null && emg.Length >= 8)
            {
                if (isRecording)
                    BufferSample(emg);
            }
            else
            {
                Debug.LogWarning("EMG data is incomplete or null. Skipping this iteration.");
            }

            yield return new WaitForSecondsRealtime(0.01f); // ~100 Hz
        }
    }

    private void BufferSample(int[] emg)
    {
        // Just buffer every sample, no duplicate check
        if (emgBuffer.Count >= maxBufferSize)
        {
            Debug.LogWarning("[StoreEMG] Buffer full! Flushing to avoid memory issues.");
            FlushBufferAndSave(graspTypeDropdown.options[graspTypeDropdown.value].text + "_RAW.csv");
        }
        emgBuffer.Enqueue((int[])emg.Clone());
        timeBuffer.Enqueue(DateTime.Now);
    }

    // Flush buffer and write to CSV
    private void FlushBufferAndSave(string filename)
    {
        int count = emgBuffer.Count;
        if (count == 0)
        {
            Debug.LogWarning("[StoreEMG] No samples to save.");
            return;
        }

        string path = GetPath(filename);
        bool fileExists = File.Exists(path);

        using (var sw = new StreamWriter(path, append: true))
        {
            if (!fileExists)
                sw.WriteLine("Timestamp,Pod1,Pod2,Pod3,Pod4,Pod5,Pod6,Pod7,Pod8");

            for (int i = 0; i < count; i++)
            {
                var sample = emgBuffer.Dequeue();
                var time = timeBuffer.Dequeue();
                string ts = time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                sw.WriteLine($"{ts},{sample[0]},{sample[1]},{sample[2]},{sample[3]},{sample[4]},{sample[5]},{sample[6]},{sample[7]}");
            }
        }

        lastSavedPath = path;
        Debug.Log($"[StoreEMG] Saved {count} samples to {path}");
    }

    private string GetPath(string filename)
    {
#if UNITY_EDITOR
        string dir = Application.dataPath + "/Thalmic Myo/MyoEMG/CSV/";
#elif UNITY_ANDROID
        string dir = Application.persistentDataPath + "/";
#elif UNITY_IPHONE
        string dir = Application.persistentDataPath + "/";
#else
        string dir = Application.dataPath + "/";
#endif
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, filename);
    }

    // Optional: for debugging or UI
    public string GetLastSavedPath() => lastSavedPath;

    void OnDestroy()
    {
        if (isRecording)
            StopAndSaveEMG();
    }
}