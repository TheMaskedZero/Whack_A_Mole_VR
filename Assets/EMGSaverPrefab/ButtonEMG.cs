using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonEMG : MonoBehaviour
{
    public static bool buttonEmgOnState = false;    // Start with button OFF
    private Coroutine timerCoroutine;              // Reference to the timer coroutine

    void Start()
    {
        // Start button click listener
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(TaskOnClick);
    }

    void Update()
    {
        // If state is false, set colour to grey
        if (buttonEmgOnState == false)
        {
            Color greyOff = new Color(200 / 255f, 200 / 255f, 200 / 255f, 1f);
            ChangeButtonColour(greyOff);
        }
        // If state is true, set colour to green
        else
        {
            Color greenOn = new Color(136 / 255f, 221 / 255f, 78 / 255f, 1f);
            ChangeButtonColour(greenOn);
        }
    }

    // ====================== This function is only called when the button is clicked ======================
    void TaskOnClick()
    {
        UnityEngine.Debug.Log("You have clicked the button!");

        // Only start a new recording if not already recording
        if (!buttonEmgOnState)
        {
            buttonEmgOnState = true;
            var storeEMG = FindObjectOfType<StoreEMG>();
            storeEMG.StartEMGRecording();
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(ButtonTimer(storeEMG));
        }
        // Optionally, you can ignore clicks while already recording
    }

    // ============================== Coroutine for 10-second timer ==============================
    private IEnumerator ButtonTimer(StoreEMG storeEMG)
    {
        yield return new WaitForSeconds(10f); // Wait for 10 seconds
        buttonEmgOnState = false;            // Untoggle the button
        storeEMG.StopAndSaveEMG();           // Stop and save after 10 seconds
        UnityEngine.Debug.Log("10 seconds passed. Button untoggled and recording stopped.");
    }

    // ============================== Function to change button selected colour ============================
    void ChangeButtonColour(Color newColor)
    {
        // Change the color of the button
        Button btn = GetComponent<Button>();
        ColorBlock btnColor = btn.colors;
        btnColor.selectedColor = newColor;
        btn.colors = btnColor;
    }
}