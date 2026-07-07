using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

/*
 Example of use:

Somewhere, do this:
 public GameObject m_notepadTemplatePrefab;  (attach to RTNotepad prefab)


   Then do this:

    public void OnConfigButton()
    {
        RTNotepad notepadScript = RTNotepad.OpenFile("poop and crap\nyeah\\cool", m_notepadTemplatePrefab);
        notepadScript.m_onClickedSavedCallback += OnConfigSaved;
        notepadScript.m_onClickedCancelCallback += OnConfigCanceled;
    }

    void OnConfigSaved(string text)
    {
        Debug.Log("They clicked save.  Text entered: " + text);
    }
    void OnConfigCanceled(string text)
    {
        Debug.Log("They clicked cancel.  Text entered: " + text);
    }

*/


public class RTNotepad : MonoBehaviour
{
    public TMPro.TMP_InputField m_textInput;
    public Action<String> m_onClickedSavedCallback;
    public Action<String> m_onClickedCancelCallback;

    //This is a little helper object designed to be called statically to create the real thing
    public static RTNotepad OpenFile(string defaultText, GameObject prefab)
    {
        GameObject go = Instantiate(prefab);
        RTNotepad goScript = go.GetComponent<RTNotepad>();
        // original goScript.m_textInput.text = defaultText;
        goScript.m_textInput.text = Config.shortenedConfig; // paste the  shortened config instead of the full text in the field
        return goScript;
    }

    public void Start()
    {
        Transform childTransform = this.transform.Find("Panel/InputField (TMP)");
        if(childTransform != null)
        {
            // removed 
            // for copy and paste I went with this solution: https://github.com/greggman/unity-webgl-copy-and-paste?tab=readme-ov-file

            // not with webgLInput extension  -> uncommented this old code that was necessay but had a bug (always selecting everything, caret didnt work)
            //childTransform.gameObject.AddComponent<WebGLSupport.WebGLInput>(); // add webGLInput
            // childTransform.gameObject.AddComponent<WebGLSupport.WebGLUIToolkitTextField>(); // add webGLInput
            // childTransform.gameObject.AddComponent<WebGLSupport.WebGLInputMobile>(); // add webGLInput


        }
        else
        {
            Debug.Log("Input Field not found for adding WebGL Input");
        }

    }

    public void OnClickedSave()
    {
        m_onClickedSavedCallback.Invoke(m_textInput.text);
        GameObject.Destroy(gameObject);
    }

    public void OnClickedCancel()
    {
        m_onClickedCancelCallback.Invoke(m_textInput.text);
        GameObject.Destroy(gameObject);
    }

    public static RTNotepad OpenLog(GameObject prefab)
    {
        GameObject go2 = Instantiate(prefab);
        RTNotepad goScript2 = go2.GetComponent<RTNotepad>();
        // original goScript.m_textInput.text = defaultText;
        string logs = "";
        foreach(string element in LogFiles.logData)
        {
            logs += element;
        }

        goScript2.m_textInput.text = logs; // paste the  shortened config instead of the full text in the field
                                           // Find the child GameObject named "CancelButton"
       

        return goScript2;
    }



}
