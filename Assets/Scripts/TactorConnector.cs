using UnityEngine;
using System;
using Tdk;

public class TactorConnector : MonoBehaviour
{
    private int connectedBoardId = -1;
    [SerializeField] private int delay = 0;


    [SerializeField] private string comPort = "COM4"; // Change this to your actual port


    // ---------------- Tactor 1 ----------------
    [Header("Tactor 1")]
    [SerializeField] private int gain1 = 100;
    [SerializeField] private int frequency1 = 150;
    [SerializeField] private int gain1Start = 0;
    [SerializeField] private int gain1End = 255;
    [SerializeField] private int freq1Start = 100;
    [SerializeField] private int freq1End = 200;
    [SerializeField] private int ramp1Duration = 1000;
    [SerializeField] private int ramp1Func = 1;

    // ---------------- Tactor 2 ----------------
    [Header("Tactor 2")]
    [SerializeField] private int gain2 = 100;
    [SerializeField] private int frequency2 = 150;
    [SerializeField] private int gain2Start = 0;
    [SerializeField] private int gain2End = 255;
    [SerializeField] private int freq2Start = 100;
    [SerializeField] private int freq2End = 200;
    [SerializeField] private int ramp2Duration = 1000;
    [SerializeField] private int ramp2Func = 1;

    // ---------------- Tactor 3 ----------------
    [Header("Tactor 3")]
    [SerializeField] private int gain3 = 100;
    [SerializeField] private int frequency3 = 150;
    [SerializeField] private int gain3Start = 0;
    [SerializeField] private int gain3End = 255;
    [SerializeField] private int freq3Start = 100;
    [SerializeField] private int freq3End = 200;
    [SerializeField] private int ramp3Duration = 1000;
    [SerializeField] private int ramp3Func = 1;

    // ---------------- Tactor 4 ----------------
    [Header("Tactor 4")]
    [SerializeField] private int gain4 = 100;
    [SerializeField] private int frequency4 = 150;
    [SerializeField] private int gain4Start = 0;
    [SerializeField] private int gain4End = 255;
    [SerializeField] private int freq4Start = 100;
    [SerializeField] private int freq4End = 200;
    [SerializeField] private int ramp4Duration = 1000;
    [SerializeField] private int ramp4Func = 1;

    // ---------------- Tactor 5 ----------------
    [Header("Tactor 5")]
    [SerializeField] private int gain5 = 100;
    [SerializeField] private int frequency5 = 150;
    [SerializeField] private int gain5Start = 0;
    [SerializeField] private int gain5End = 255;
    [SerializeField] private int freq5Start = 100;
    [SerializeField] private int freq5End = 200;
    [SerializeField] private int ramp5Duration = 1000;
    [SerializeField] private int ramp5Func = 1;

    void Start()
    {
        Debug.Log("Initializing TDK...");
        CheckError(TdkInterface.InitializeTI());

        Debug.Log($"Connecting to {comPort}...");
        int boardId = TdkInterface.Connect(comPort, (int)TdkDefines.DeviceTypes.Serial, IntPtr.Zero);
        
        if (boardId >= 0)
        {
            connectedBoardId = boardId;
            Debug.Log($"Connected! Board ID: {connectedBoardId}");
        }
        else
        {
            Debug.LogError("Failed to connect: " + TdkDefines.GetLastEAIErrorString());
        }
    }

    void Update()
    {
        // Press spacebar to pulse tactor 1
        //if (connectedBoardId >= 0 && Input.GetKeyDown(KeyCode.Space))
        //{
            //Debug.Log("Pulsing tactor 1 for 250 ms...");
            //CheckError(TdkInterface.Pulse(connectedBoardId, 1, 250, 0));
        //}
         if (connectedBoardId < 0) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplySettingsToTactor(1, gain1, frequency1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplySettingsToTactor(2, gain2, frequency2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplySettingsToTactor(3, gain3, frequency3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplySettingsToTactor(4, gain4, frequency4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ApplySettingsToTactor(5, gain5, frequency5);

        if (Input.GetKeyDown(KeyCode.Q)) ApplyAllStaticSettings();
        if (Input.GetKeyDown(KeyCode.W)) RampAllGains();
        if (Input.GetKeyDown(KeyCode.E)) RampAllFrequencies();
    }

    void OnApplicationQuit()
    {
        if (connectedBoardId >= 0)
        {
            Debug.Log("Closing connection...");
            CheckError(TdkInterface.Close(connectedBoardId));
        }

        Debug.Log("Shutting down TDK...");
        CheckError(TdkInterface.ShutdownTI());
    }

    private void CheckError(int ret)
    {
        if (ret < 0)
        {
            Debug.LogError("TDK Error: " + TdkDefines.GetLastEAIErrorString());
        }
    }

    void ApplySettingsToTactor(int tactorID, int gain, int frequency)
    {
        Debug.Log($"[Tactor {tactorID}] Setting Gain: {gain}, Freq: {frequency}");
        CheckError(TdkInterface.ChangeGain(connectedBoardId, tactorID, gain, delay));
        CheckError(TdkInterface.ChangeFreq(connectedBoardId, tactorID, frequency, delay));
    }

    void RampGain(int tactorID, int start, int end, int duration, int func)
    {
        Debug.Log($"[Tactor {tactorID}] Ramping Gain: {start} → {end}");
        CheckError(TdkInterface.RampGain(connectedBoardId, tactorID, start, end, duration, func, delay));
    }

    void RampFrequency(int tactorID, int start, int end, int duration, int func)
    {
        Debug.Log($"[Tactor {tactorID}] Ramping Frequency: {start}Hz → {end}Hz");
        CheckError(TdkInterface.RampFreq(connectedBoardId, tactorID, start, end, duration, func, delay));
    }

    public void ApplyAllStaticSettings()
    {
        ApplySettingsToTactor(1, gain1, frequency1);
        ApplySettingsToTactor(2, gain2, frequency2);
        ApplySettingsToTactor(3, gain3, frequency3);
        ApplySettingsToTactor(4, gain4, frequency4);
        ApplySettingsToTactor(5, gain5, frequency5);
    }

    public void RampAllGains()
    {
        RampGain(1, gain1Start, gain1End, ramp1Duration, ramp1Func);
        RampGain(2, gain2Start, gain2End, ramp2Duration, ramp2Func);
        RampGain(3, gain3Start, gain3End, ramp3Duration, ramp3Func);
        RampGain(4, gain4Start, gain4End, ramp4Duration, ramp4Func);
        RampGain(5, gain5Start, gain5End, ramp5Duration, ramp5Func);
    }

    public void RampAllFrequencies()
    {
        RampFrequency(1, freq1Start, freq1End, ramp1Duration, ramp1Func);
        RampFrequency(2, freq2Start, freq2End, ramp2Duration, ramp2Func);
        RampFrequency(3, freq3Start, freq3End, ramp3Duration, ramp3Func);
        RampFrequency(4, freq4Start, freq4End, ramp4Duration, ramp4Func);
        RampFrequency(5, freq5Start, freq5End, ramp5Duration, ramp5Func);
    }
}
