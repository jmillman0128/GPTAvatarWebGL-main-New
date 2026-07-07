using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OnToggle : MonoBehaviour
{
    [SerializeField] private Button toggleButton;  // The button
    [SerializeField] private TMP_Text textToToggle;  // The TextMeshPro component to enable/disable
    [SerializeField] private string buttonText;  // The text to display on the button

    // Can later also be queried from the config file for both status and dialogue text
    private bool isOn = false;  // Track the toggle state

    public void Awake()
    {
        if(isOn == false)
        {
            textToToggle.enabled = false;
            TMP_Text buttonLabel = toggleButton.GetComponentInChildren<TMP_Text>();
            buttonLabel.text = isOn ? buttonText + " On" : buttonText + " Off";
        }
        else
        {
             textToToggle.enabled = true;
        }
    }

    // Method to be called when the button is clicked
    public void Toggle()
    {
        // Toggle the state
        isOn = !isOn;

        // Update the button text based on the new state
        TMP_Text buttonLabel = toggleButton.GetComponentInChildren<TMP_Text>();
        if (buttonLabel != null)
        {
            buttonLabel.text = isOn ? buttonText + " On" : buttonText + " Off";
        }

        // Enable/Disable the TextMeshPro component
        textToToggle.enabled = isOn;
    }
}
