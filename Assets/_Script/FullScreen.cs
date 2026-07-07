using UnityEngine;

public class FullScreen : MonoBehaviour
{
    void Start()
    {
        Screen.fullScreen = true; // Ensure full screen
        UnityEngine.XR.XRSettings.showDeviceView = false; // Hide the device view, if present
    }
}

