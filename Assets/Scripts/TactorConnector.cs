using UnityEngine;
using System;
using Tdk;

public class TactorConnector : MonoBehaviour
{
    private int connectedBoardId = -1;

    [SerializeField] private string comPort = "COM4"; // Change this to your actual port

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
        if (connectedBoardId >= 0 && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Pulsing tactor 1 for 250 ms...");
            CheckError(TdkInterface.Pulse(connectedBoardId, 1, 250, 0));
        }
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
}
