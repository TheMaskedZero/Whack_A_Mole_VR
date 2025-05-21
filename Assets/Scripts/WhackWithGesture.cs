using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhackWithGesture : MonoBehaviour
{
    // Holds the currently active (visible) mole:
    private Mole currentMole;
    [SerializeField] private LoggingManager loggingManager;
    [SerializeField] private AnimationPrompter animationPrompter;

    [Range(0f, 2.5f)]
    [SerializeField] float holdKeyBeforeTrigger = 2f;
    private LoggerNotifier loggerNotifier;

    private float fistHoldStart, pinchHoldStart, restHoldStart;
    
    private bool fistGestureTriggered, pinchGestureTriggered, restGestureTriggered;

    void Start()
    {
        //loggingManager = FindObjectOfType<LoggingManager>();

        fistHoldStart = -1f;
        pinchHoldStart = -1f;
        restHoldStart = -1f;

        fistGestureTriggered = false;
        pinchGestureTriggered = false;
        restGestureTriggered = false;

        loggerNotifier = new LoggerNotifier(
        updateGeneralValues: () => new LogEventContainer(), // No need to include session/email again
        eventsHeadersDefaults: new Dictionary<string, string>
        {
            { "GestureType", "Unknown" },
            { "Result", "None" },
            { "Framecount", "-1" },
            { "Time", "-1" }
        });

        /*// Find all Mole instances in the scene (they may all be instantiated up front)
        foreach (var mole in FindObjectsOfType<Mole>())
        {
            // Subscribe to the visibility event: isEnabled==true means this mole just appeared
            mole.GetUpdateEvent().AddListener(OnMoleStateChanged);
        }
    }

    // This listener runs whenever any mole enters or exits the Enabled state.
    private void OnMoleStateChanged(bool isEnabled, Mole mole)
    {
        if (isEnabled)
        {
            // Mole just appeared → make it our “current”
            currentMole = mole;
        }
        else if (currentMole == mole)
        {
            // Current mole just disappeared → clear it
            currentMole = null;
        }*/
    }

    void Update()
    {
        foreach (Mole mole in FindObjectsOfType<Mole>())
            {
                string moleState = mole.CheckMoleActive();
                if (moleState == "Enabled")
                {
                    currentMole = mole;
                }
            }

        if (Input.GetKeyDown(KeyCode.J))
        {
            fistHoldStart = Time.time;
            fistGestureTriggered = false;
            animationPrompter.TriggerGraspAnimationState();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            pinchHoldStart = Time.time;
            pinchGestureTriggered = false;
            animationPrompter.TriggerPinchAnimationState();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            restHoldStart = Time.time;
            restGestureTriggered = false;
            animationPrompter.TriggerDefaultAnimationState();
        }


        // If key is held for more than holdKeyBeforeTrigger (default 0.8 sec), mole is whacked and tactors are triggered.
        if (Input.GetKey(KeyCode.J))
        {
           /*loggerNotifier.NotifyLogger(
            eventName: "Observed_Gesture",
            eventType: EventLogger.EventType.DefaultEvent,
            overrideEventParameters: new Dictionary<string, object>
            {
                { "GestureType", "Fist" },
                { "Time", Time.time }
            });*/


           if (!fistGestureTriggered && Time.time - fistHoldStart >= holdKeyBeforeTrigger)
           {
                Debug.Log("Attempted to Pop Mole Programmatically After Holding Manual Trigger");
                WhackDatMoleOnDaNoggin("Fist"); // GREEN MOLE
                fistGestureTriggered = true;
           }
        }

        if (Input.GetKey(KeyCode.K))
        {
            /*loggerNotifier.NotifyLogger(
            eventName: "Observed_Gesture",
            eventType: EventLogger.EventType.DefaultEvent,
            overrideEventParameters: new Dictionary<string, object>
            {
                { "GestureType", "Pinch" },
                { "Time", Time.time }
            });*/


            if (!pinchGestureTriggered && Time.time - pinchHoldStart >= holdKeyBeforeTrigger)
            {
                Debug.Log("Attempted to Pop Mole Programmatically After Holding Manual Trigger");
                WhackDatMoleOnDaNoggin("Pinch"); // BLUE MOLE
                pinchGestureTriggered = true;
            }
        }

        if (Input.GetKey(KeyCode.L))
        {
            /*loggerNotifier.NotifyLogger(
            eventName: "Observed_Gesture",
            eventType: EventLogger.EventType.DefaultEvent,
            overrideEventParameters: new Dictionary<string, object>
            {
                { "GestureType", "Resting" },
                { "Time", Time.time }
            });*/


            if (!restGestureTriggered && Time.time - restHoldStart >= 0.7f)
            {
                Debug.Log("Attempted to Pop Mole Programmatically After Holding Manual Trigger");
                WhackDatMoleOnDaNoggin("Resting"); // GRAY MOLE
                restGestureTriggered = true;
            }
        }

        if (Input.GetKeyUp(KeyCode.J))
        {
            fistHoldStart = -1f;
            fistGestureTriggered = false;
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            pinchHoldStart = -1f;
            pinchGestureTriggered = false;
        }

        if (Input.GetKeyUp(KeyCode.L))
        {
            restHoldStart = -1f;
            restGestureTriggered = false;
        }


        /*// Example trigger: press the space bar to “hit” the current mole
        if (Input.GetKey(KeyCode.J))
        {
            //if (currentMole != null)
            //{
                // Call Pop(...) in code, which under the hood calls PlayPop()
                Debug.Log("Attempted to Pop Mole Programmatically");
                currentMole?.PopMoleProgrammatically("Fist"); // GREEN MOLE
                //currentMole = null;
            //}
        }
        if (Input.GetKey(KeyCode.K))
        {
            //if (currentMole != null)
            //{
                // Call Pop(...) in code, which under the hood calls PlayPop()
                Debug.Log("Attempted to Pop Mole Programmatically");
                currentMole?.PopMoleProgrammatically("Pinch"); // BLUE MOLE
                //currentMole = null;
            //}
        }
        if (Input.GetKey(KeyCode.L))
        {
            //if (currentMole != null)
            //{
                // Call Pop(...) in code, which under the hood calls PlayPop()
                Debug.Log("Attempted to Pop Mole Programmatically");
                currentMole?.PopMoleProgrammatically("Resting"); // GRAY MOLE
                //currentMole = null;
            //}
        }*/
    }

    public void WhackDatMoleOnDaNoggin(string Gesture)
    {
        currentMole?.PopMoleProgrammatically(Gesture);
    }

    public Dictionary<string, object> GetObservedGestures()
        {
        var gestureData = new Dictionary<string, object>();

        // Add the gesture type based on which is active
        if (fistGestureTriggered)
        {
            gestureData["GestureType"] = "Fist";
            gestureData["GestureDuration"] = Time.time - fistHoldStart;
        }
        else if (pinchGestureTriggered)
        {
            gestureData["GestureType"] = "Pinch";
            gestureData["GestureDuration"] = Time.time - pinchHoldStart;
        }
        else if (restGestureTriggered)
        {
            gestureData["GestureType"] = "Resting";
            gestureData["GestureDuration"] = Time.time - restHoldStart;
        }
        else
        {
            gestureData["GestureType"] = "None";
            gestureData["GestureDuration"] = 0f;
        }

        // You can add more info as needed:
        //gestureData["Time"] = Time.time;
        //gestureData["FrameCount"] = Time.frameCount;

        return gestureData;
    }

}
