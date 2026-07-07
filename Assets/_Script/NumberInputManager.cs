using System;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NumberInputManager : MonoBehaviour
{
    public TMP_InputField inputField;  // Reference to the Input Field
    public Button[] numberButtons; // Array of Number Buttons
    public Button enterButton;     // Reference to the Enter Button
    public Button backspaceButton; // Reference to the Backspace Button

    void Start()
    {
        // Add listeners to number buttons
        for (int i = 0; i < numberButtons.Length; i++)
        {
            int index = i;  // Store the current index
            numberButtons[index].onClick.AddListener(() => AppendNumber(index));
        }

        // Add listener to enter button
        enterButton.onClick.AddListener(OnEnter);

        // Add listener to backspace button
        backspaceButton.onClick.AddListener(OnBackspace);
    }

    void AppendNumber(int number)
    {
        if(!Regex.IsMatch(inputField.text, @"\d"))
        {
            inputField.text = string.Empty;
        }
        inputField.text += number.ToString();
    }

    void OnEnter()
    {
        // Handle the entered number (e.g., print to console or process it)
        Debug.Log("Entered Number: " + inputField.text);
    }

    void OnBackspace()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
    }
}
