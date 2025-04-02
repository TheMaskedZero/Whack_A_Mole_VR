using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Thalmic.Myo;

public class SampleLogger3 : MonoBehaviour
{
    bool log = true;
    int samplingFrequency = 4; // 5ms, = 125Hz, 4ms = 200Hz, 2ms = 500Hz
    List<int> numbers = new List<int>();
    List<int> emgData = new List<int>();
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    Task sampleTask;
    Stopwatch writeStopwatch = new Stopwatch();
    private LoggingManager loggingManager;
    private bool manualFramecount = true;

    //  public GameObject thalmicMyo;
    //   private ThalmicMyo myo;

    public ThalmicMyo thalmicMyo;

    void Start ()
    {

        // Access the ThalmicMyo component attached to this GameObject
        //thalmicMyo = GameObject.Find("Myo");
        //myo = thalmicMyo.GetComponent<ThalmicMyo>();

        // Subscribe to the EmgData event from ThalmicMyo
        thalmicMyo._myo.EmgData += onReceiveData;
        Debug.Log(thalmicMyo);
        StartLog();
    }

    // Start is called before the first frame update
    void OnApplicationQuit ()
    {
        StopLog();
    }

    public void StartLog() {
        loggingManager = GetComponent<LoggingManager>();
        loggingManager.CreateLog("Sample", headers: new List<string>() {"Event","TestVar"});
        writeStopwatch.Start();
        //sampleTask = SampleLog(cancellationTokenSource.Token);
    }

    private void onReceiveData(object sender, EmgDataEventArgs data)
    {
        // Print a simple message when EMG data is received
        Debug.Log("Received EMG Data!");

        // Optionally, print the EMG data values
        Dictionary<string, object> sampleLog = new Dictionary<string, object>() {
                            {"Event", "Sample"},
                            {"TestVar", data.Emg[0]},
                            {"TestVar1", data.Emg[1]},
                            {"TestVar2", data.Emg[2]},
                            {"TestVar3", data.Emg[3]},
                            {"TestVar4", data.Emg[4]},
                            {"TestVar5", data.Emg[5]},
                            {"TestVar6", data.Emg[6]},
                            {"TestVar7", data.Emg[7]},
                        };

        loggingManager.Log("Sample", sampleLog);

    }

    public void StopLog() {
        cancellationTokenSource.Cancel();
        //sampleTask.Wait();
        writeStopwatch.Stop();
        loggingManager.SaveLog("Sample", false);
        TimeSpan writeTs = writeStopwatch.Elapsed;
        string writeElapsedTime = String.Format("{0:00}:{1:0000}",
            writeTs.Seconds, writeTs.Milliseconds);
        Debug.Log(" numbers appended in " + writeElapsedTime);
        Debug.Log(numbers.Count);
        Debug.Log(emgData.Count);
    }

    void Update()
    {
        Debug.Log(thalmicMyo._myo.emgData[0]);
    }

    // Generates a "logs" row (see class description) from the given datas. Adds mandatory parameters and 
    // the PersistentEvents parameters to the row when generating it.
    public Task SampleLog(CancellationToken token)
    {
        return Task.Run(() =>
                {

                    int testVar = 0;
                    while (true) {
                        Dictionary<string, object> sampleLog = new Dictionary<string, object>() {
                            {"Event", "Sample"},
                            {"TestVar", thalmicMyo._myo.emgData[0]},
                            {"TestVar1", thalmicMyo._myo.emgData[1]},
                            {"TestVar2", thalmicMyo._myo.emgData[2]},
                            {"TestVar3", thalmicMyo._myo.emgData[3]},
                            {"TestVar4", thalmicMyo._myo.emgData[4]},
                            {"TestVar5", thalmicMyo._myo.emgData[5]},
                        };

                        loggingManager.Log("Sample", sampleLog);
                        testVar++;

                        // Check if cancellation is requested
                        if (token.IsCancellationRequested)
                        {
                            Console.WriteLine("Task has been canceled.");
                            break;  // Exit the task if cancellation is requested
                        }

                        Thread.Sleep(samplingFrequency); 
                    }
                });
    }

}
