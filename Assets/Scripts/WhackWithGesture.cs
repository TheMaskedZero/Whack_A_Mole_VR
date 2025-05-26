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

        // Manually trigger animations and feedback.
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


        // If key is held for more than holdKeyBeforeTrigger (default 0.8 sec), mole is whacked.
        if (Input.GetKey(KeyCode.J))
        {
           if (!fistGestureTriggered && Time.time - fistHoldStart >= holdKeyBeforeTrigger)
           {
                Debug.Log("Attempted to Pop Mole Programmatically After Holding Manual Trigger");
                WhackDatMoleOnDaNoggin("Fist"); // GREEN MOLE
                fistGestureTriggered = true;
           }
        }

        if (Input.GetKey(KeyCode.K))
        {
            if (!pinchGestureTriggered && Time.time - pinchHoldStart >= holdKeyBeforeTrigger)
            {
                Debug.Log("Attempted to Pop Mole Programmatically After Holding Manual Trigger");
                WhackDatMoleOnDaNoggin("Pinch"); // BLUE MOLE
                pinchGestureTriggered = true;
            }
        }

        if (Input.GetKey(KeyCode.L))
        {
            if (!restGestureTriggered && Time.time - restHoldStart >= 0.7f)
            {
                Debug.Log("Attempted to Pop Mole Programmatically After Holding Manual Trigger");
                WhackDatMoleOnDaNoggin("Resting"); // GRAY MOLE
                restGestureTriggered = true;
            }
        }

        // Reset to default state when key is released.
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
    }

    // Pop the current target mole.
    public void WhackDatMoleOnDaNoggin(string Gesture)
    {
        currentMole?.PopMoleProgrammatically(Gesture);
    }

    // Constructing a dictionary to be collected by the logger.
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

        return gestureData;
    }

}
