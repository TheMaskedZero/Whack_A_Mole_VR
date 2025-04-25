using UnityEngine;

public class HubResetter : MonoBehaviour
{
    // Reference to your Hub GameObject (set in Inspector)
    public ThalmicHub thalmicHub;

    // Wrapper with the required signature
    public void ResetHubVoid()
    {
        if (thalmicHub != null)
        {
            thalmicHub.ResetHub();
        }
        else
        {
            Debug.LogWarning("ThalmicHub reference not set on HubResetter.");
        }
    }
}
