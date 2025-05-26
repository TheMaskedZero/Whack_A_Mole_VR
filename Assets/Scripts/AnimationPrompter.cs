using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPrompter : MonoBehaviour
{
    [SerializeField] private bool handDefaultState;
    [SerializeField] private bool handGraspState;
    [SerializeField] private bool handPinchState;
    [SerializeField] private bool triggerTactor = false;

    private Animator animator;

    [SerializeField] private TactorConnector tactorConnector;

    // Start is called before the first frame update
    void Start()
    {
        handDefaultState = false;
        handGraspState = false;
        handPinchState = false;

        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // These were purely used for manual triggering during debugging.
        if (handDefaultState == true)
        {
            TriggerDefaultAnimationState();
            handDefaultState = false;
        }
        if (handGraspState == true)
        {
            TriggerGraspAnimationState();
            handGraspState = false;
        }
        if (handPinchState == true)
        {
            TriggerPinchAnimationState();
            handPinchState = false;
        }
    }

    // Trigger the resting state animation on the prosthetic hand, and the resting state feedback (bugged).
    public void TriggerDefaultAnimationState()
    {
        animator.SetTrigger("ReturnToRest");
        if (triggerTactor == true)
        {
            tactorConnector.TriggerRestingStateFeedback();
        }
    }

    // Same as above, but Fist state.
    public void TriggerGraspAnimationState()
    {
        animator.ResetTrigger("ReturnToRest");
        animator.Play("Armature|power");
        if (triggerTactor == true)
        {
            tactorConnector.TriggerGraspingStateFeedback();
        }
    }

    // Same as above, but Pinch state.
    public void TriggerPinchAnimationState()
    {
        animator.ResetTrigger("ReturnToRest");
        animator.Play("Armature|pinch3_003");
        if (triggerTactor == true)
        {
            tactorConnector.TriggerPinchingStateFeedback();
        }
    }

    // Construct a dictionary to be retrieved by the LoggingManager, in order to log whether tactors are enabled or disabled (test condition 1 or 2).
    public Dictionary<string, object> GetTactorStatus()
        {
        var tactorData = new Dictionary<string, object>();

        // Add the gesture type based on which is active
        if (triggerTactor == true)
        {
            tactorData["TactorsEnabled"] = "True";
        }
        else
        {
            tactorData["TactorsEnabled"] = "False";
        }

        return tactorData;
    }
}