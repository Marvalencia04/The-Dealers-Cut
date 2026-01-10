using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    public InputActionProperty pinchAnimationAction;
    public InputActionProperty gripAnimationAction;
    public Animator HandAnimator;
    public string triggerName = "Trigger";
    public string gripName = "Grip";


    // Update is called once per frame
    void Update()
    {
        float triggerValue = pinchAnimationAction.action.ReadValue<float>();
        HandAnimator.SetFloat(triggerName,triggerValue);
        float gripValue = gripAnimationAction.action.ReadValue<float>();
        HandAnimator.SetFloat(gripName, gripValue);
    }
}
