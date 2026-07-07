using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnterHandler : MonoBehaviour
{
    private Button enter;
    private GameObject textMeshPro;
    private AzureFileDownload azureFileDownload;
    private LogFiles logFiles;
    private GameObject idCode;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject scriptHolder = GameObject.Find("InputManager");
        GameObject azureDownloader = GameObject.Find("FileDownloader");
        textMeshPro = GameObject.Find("Welcome");
        idCode = GameObject.Find("userInputField");
        enter = GameObject.Find("Enter").GetComponent<Button>();
        azureFileDownload = azureDownloader.GetComponent<AzureFileDownload>();
        logFiles = scriptHolder.GetComponent<LogFiles>();
        enter.onClick.AddListener(OnEnterPressed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnterPressed()
    {
        if (textMeshPro.GetComponent<TMP_Text>().text == "Experience ID")
        {
            azureFileDownload.OnEnterPressed();
        }
        if (textMeshPro.GetComponent<TMP_Text>().text == "User ID")
        {
            logFiles.writeUserNumberOnEnter();
            textMeshPro.GetComponent<TMP_Text>().text = "Experience ID";
        }
        idCode.GetComponent<TMP_InputField>().text = "";
    }
}
