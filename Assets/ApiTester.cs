using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ApiTester : MonoBehaviour
{
    //private string apiUrl = "https://localhost:7235/api/LogFiles";
    private string apiUrl = "https://avatar-research.com/api/LogFiles";

    // Automatically send a log when the scene starts
    private void Start()
    {
        SendLog();
    }

    public void SendLog()
    {
        StartCoroutine(PostLog());
    }

    private IEnumerator PostLog()
    {
       

        LogData logData = new LogData
        {
            Actor = "UnityApp",
            Verb = "StrongAction",
            Object = "File",
            Result = "Success",
            Context = "Sent from Unity",
            Authority = "UnityUser",
            Attachments = "https://example.com/unityfile",
            Timestamp = System.DateTime.UtcNow.ToString("o") // ISO 8601 format
        };

        string json = JsonUtility.ToJson(logData);
        Debug.Log($"JSON Payload: {json}"); // Print to check JSON payload


        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Log sent successfully!");
                Debug.Log($"Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"Error sending log: {request.error}");
            }
        }
    }
}
