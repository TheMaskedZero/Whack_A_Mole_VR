using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPrompter : MonoBehaviour
{
    [SerializeField] private bool handDefaultState;
    [SerializeField] private bool handGraspState;
    [SerializeField] private bool handPinchState;
    [SerializeField] private bool triggerTactor;

    private Animator animator;

    [SerializeField] private TactorConnector tactorConnector;

    //private float timeSinceAnimationTriggered;

    // Start is called before the first frame update
    void Start()
    {
        handDefaultState = false;
        handGraspState = false;
        handPinchState = false;
        triggerTactor = false;
        //timeSinceAnimationTriggered = 0f;

        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
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

    public void TriggerDefaultAnimationState()
    {
        //if (Time.time - timeSinceAnimationTriggered > 2f)
        animator.SetTrigger("ReturnToRest");
        //animator.Play("Armature|Rest Position");
        if (triggerTactor = true)
        {
            tactorConnector.TriggerRestingStateFeedback();
        }
        //timeSinceAnimationTriggered = Time.time;
    }

    public void TriggerGraspAnimationState()
    {
        //if (Time.time - timeSinceAnimationTriggered > 2f)
        animator.ResetTrigger("ReturnToRest");
        animator.Play("Armature|power");
        if (triggerTactor = true)
        {
            tactorConnector.TriggerGraspingStateFeedback();
        }
        //timeSinceAnimationTriggered = Time.time;
    }

    public void TriggerPinchAnimationState()
    {
        //if (Time.time - timeSinceAnimationTriggered > 2f)
        animator.ResetTrigger("ReturnToRest");
        animator.Play("Armature|pinch3_003");
        if (triggerTactor = true)
        {
            tactorConnector.TriggerPinchingStateFeedback();
        }
        //timeSinceAnimationTriggered = Time.time;
    }
}