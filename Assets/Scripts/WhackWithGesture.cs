using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhackWithGesture : MonoBehaviour
{
    // Holds the currently active (visible) mole:
    private Mole currentMole;

    /*void Start()
    {
        // Find all Mole instances in the scene (they may all be instantiated up front)
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
        }
    }*/

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
        // Example trigger: press the space bar to “hit” the current mole
        if (Input.GetKeyDown(KeyCode.J))
        {
            //if (currentMole != null)
            //{
                // Call Pop(...) in code, which under the hood calls PlayPop()
                Debug.Log("Attempted to Pop Mole Programmatically");
                currentMole.PopMoleProgrammatically();
                currentMole = null;
            //}
        }
    }
}
