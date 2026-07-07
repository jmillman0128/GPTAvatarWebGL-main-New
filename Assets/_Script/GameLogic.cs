/*

Source code by Seth A. Robinson

 */

//#define RT_NOAUDIO

using UnityEngine;
using DG.Tweening;
//using UnityEngine.Networking;
using UnityEngine.UI;

public class GameLogic : MonoBehaviour
{
    static GameLogic _this = null;
    public GameObject m_notepadTemplatePrefab;
    public GameObject[] backgrounds;
    public int backgroundIndex = 0;

    public static string GetName()
    {
        return Get().name;
    }

    private void Awake()
    {

        //float targetFrameRate = Screen.currentResolution.refreshRateRatio * 60f;
        //Application.targetFrameRate = (int)targetFrameRate;
        QualitySettings.vSyncCount = 1;
        //QualitySettings.antiAliasing = 4;

    }

    // Use this for initialization
    void Start()
    {
        DOTween.Init(true, true, LogBehaviour.Verbose).SetCapacity(200, 20);
        // RTAudioManager.Get().SetDefaultMusicVol(0.4f);
        _this = this;

#if RT_NOAUDIO
		AudioListener.pause = true;
#endif


     RTConsole.Get().SetShowUnityDebugLogInConsole(true);
       
        //RTEventManager.Get().Schedule(RTAudioManager.GetName(), "PlayMusic", 1, "intro");
        string version = "Unity V "+ Application.unityVersion+" :";

#if NET_2_0
        version += " Net 2.0 API";
#endif
#if NET_2_0_SUBSET
        version += " Net 2.0 Subset API";
#endif

#if NET_4_6
            version += " .Net 4.6 API";
#endif

        #if RT_BETA
        print ("Beta build detected!");
#endif

       
        RTConsole.Get().SetMirrorToDebugLog(true);

        // initialize background array
        backgrounds = new GameObject[8];
        backgrounds[0] = GameObject.Find("Backgrounds/parc_background");
        backgrounds[1] = GameObject.Find("Backgrounds/office_background");
        backgrounds[2] = GameObject.Find("Backgrounds/marketsquare_background");
        backgrounds[3] = GameObject.Find("Backgrounds/classroomWestern_background");


        backgrounds[4] = GameObject.Find("Backgrounds/computer_background");
        backgrounds[5] = GameObject.Find("Backgrounds/coffee_background");
        backgrounds[6] = GameObject.Find("Backgrounds/doctorsoffice_background");
        backgrounds[7] = GameObject.Find("Backgrounds/classroom_background");


        // set all backgrounds to transparent
        // Loop through each GameObject in the backgrounds array
        for (int i = 0; i < backgrounds.Length; i++)
        {
            // Check if the current GameObject is not null
            if (backgrounds[i] != null)
            {
                // Get the SpriteRenderer component from the GameObject
                SpriteRenderer spriteRenderer = backgrounds[i].GetComponent<SpriteRenderer>();

                if (spriteRenderer != null)
                {
                    // Set the sprite's color to be fully transparent
                    Color color = spriteRenderer.color;
                    color.a = 0f; // Set alpha to 0 to make the sprite invisible
                    spriteRenderer.color = color;
                }
                else
                {
                    Debug.LogError("SpriteRenderer component missing on GameObject at index " + i);
                }
            }
            else
            {
                Debug.LogError("GameObject at index " + i + " is null.");
            }
        }

        
    }

    static public GameLogic Get()
	{
		return _this;
	}
 
	void OnApplicationQuit() 
	{
        // Make sure prefs are saved before quitting.
        //PlayerPrefs.Save();
        RTConsole.Log("Application quitting normally");

//        NetworkTransport.Shutdown();
        print("QUITTING!");
    }
    

    private void OnDestroy()
    {
        print("Game logic destroyed");
    }

    // originalCode by seth
    public void OnConfigButton()
    {
        RTNotepad notepadScript = RTNotepad.OpenFile(Config.Get().GetConfigText(), m_notepadTemplatePrefab);
        notepadScript.m_onClickedSavedCallback += OnConfigSaved;
        notepadScript.m_onClickedCancelCallback += OnConfigCanceled;
    }
    void OnConfigSaved(string text)
    {
        //Config.Get().ProcessConfigString(text);
        //Config.Get().SaveConfigToFile(); //it might have changed.

        //Debug.Log("They clicked save.  Text entered: " + text);

        Config.Get().LoadConfigFile(text);
    }
    void OnConfigCanceled(string text)
    {
        Debug.Log("Clicked cancel.");
    }

    // ----- 
    // new code for the new function (added by m fink)
    public void OnConfigWithoutKeysButton()
    {
        RTNotepad notepadScript = RTNotepad.OpenFile(Config.Get().GetConfigTextWithoutKeys(), m_notepadTemplatePrefab);
        notepadScript.m_onClickedSavedCallback += OnConfigSavedWithoutKeys;
        notepadScript.m_onClickedCancelCallback += OnConfigCanceledWithoutKeys;

        // add copy and paste functionality
        // notepadScript.
        GameObject currentGO;
        currentGO = GameObject.Find("RTFriendNotepadPrefab(Clone)/Panel/InputField (TMP)");
        // currentGO.AddComponent<WebGLSupport.WebGLInput>(); // adding copy and paste functionality manually
        /*bool notSet = true;
        if (notSet) {
            currentGO.GetComponent<WebGLSupport.WebGLInput>().enabled = true;
            notSet = false;
        }*/


        // paste only shortened config in the textfield
        //notepadScript.m_textInput.text = Config.shortenedConfig;
    }

    void OnConfigSavedWithoutKeys(string text)
    {
        //Config.Get().ProcessConfigString(text);
        //Config.Get().SaveConfigToFile(); //it might have changed.

        //Debug.Log("They clicked save.  Text entered: " + text);
        Debug.Log("Clicked save - without keys");
        //Config.Get().LoadConfigFile(text); original code by seth
        Config.shortenedConfig = text; // write the text from the editor to the shortned config
        Config.Get().LoadConfigFile(Config.keyConfig + text);
        // Debug.Log("Saving on save button: " + Config.keyConfig + text); outcommented for security reasons
    }
    void OnConfigCanceledWithoutKeys(string text)
    {
        Debug.Log("Clicked cancel - without keys");
    }

    public void OnLogFileButton()
    {
        // search for the panel
        // call on the panel the setUpInputField Method, a dynamic method by the LogFile object
        //GameObject inputFieldObject = GameObject.Find("Canvas/Panel/userInputField");
        // LogFiles.MakeInputFielVisible(inputFieldObject);

        RTNotepad notepadScript2 = RTNotepad.OpenLog(m_notepadTemplatePrefab);
        notepadScript2.m_onClickedSavedCallback += OnClickedSaveLogfile;
        notepadScript2.m_onClickedCancelCallback += OnClickedCancelLogfile;

        // deactivate / hide cancel button
        Button saveButton = notepadScript2.transform.Find("Panel/SaveButton").GetComponent<Button>();
        if (saveButton != null)
        {
            saveButton.interactable = false;
        }
    }

    void OnLogFileShow(string logfile)
    {
        Debug.Log("Clicked on the Logfile button");
        Debug.Log("Printing the list of logfiles: --- ");
        foreach (string elem in LogFiles.logData)
        {
            Debug.Log(elem);
        }
        Debug.Log("End of logfile---");
    }

    void OnClickedSaveLogfile(string a)
    {
        // do nothing
        // this function serves just for having a reference to the button without changing 
        // the logic of the callback functions that take a string as argument
    }
    void OnClickedCancelLogfile(string b)
    {
        // do nothing
        // this function serves just for having a reference to the button without changing 
        // the logic of the callback functions that take a string as argument
    }

    // Update is called once per frame
    void Update () 
    {
       
    }

    public static void ViewCharacterBackground(Friend myFriend)
    {
        // blendet immer aktuellen background aus vom aktuellen Avatar
        GameObject go = GameObject.Find("GameLogic"); // sucht erstmal die Logik auf dem GameLogic Skript
        if (go != null)
        {
            // Get the Config component attached to the GameObject
            Config configComponent = go.GetComponent<Config>();

            if (configComponent != null)
            {
                // Call the SetCurrentBackgroundBlank method on the Config component
                configComponent.SetCurrentBackgroundBlank(); // erst dort wird der jeweilige Character blank gesetzt
            }
            else
            {
                Debug.LogError("Config component not found on GameObject 'GameLogic'.");
            }
        }
        else
        {
            Debug.LogError("GameObject 'GameLogic' not found.");
        }

        
        // section that loads the background from the editor
        GameObject backgroundsParent = GameObject.Find("Backgrounds");
        backgroundsParent.SetActive(true);

        if (backgroundsParent == null)
        {
            Debug.LogError($"Could not find child GameObject '{myFriend._background}' in 'Backgrounds'.");
            return;
        }

        if (string.IsNullOrEmpty(myFriend._background))
        {
            Debug.LogError("Friend's background name is not set.");
            return;
        }

        // Find all GameObjects in the scene
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        GameObject targetBackground = null;

        // Search for the GameObject with the unique name
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == myFriend._background)
            {
                targetBackground = obj;
                break;
            }
        }

        if (targetBackground == null)
        {
            Debug.LogError($"Could not find GameObject with name '{myFriend._background}'.");
            return;
        }

        // Get the SpriteRenderer component
        SpriteRenderer spriteRenderer = targetBackground.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Set the sprite's alpha to fully visible
            Color color = spriteRenderer.color;
            color.a = 1f; // Fully visible
            spriteRenderer.color = color;

            Debug.Log($"Set '{myFriend._background}' to fully visible.");
        }
        else
        {
            Debug.LogError($"No SpriteRenderer found on GameObject '{myFriend._background}'.");
        }


    }

    public void OnNextBackground()
    {
        // blendet immer aktuellen background aus vom aktuellen Avatar
        GameObject go = GameObject.Find("GameLogic"); // sucht erstmal die Logik auf dem GameLogic Skript
        if (go != null)
        {
            // Get the Config component attached to the GameObject
            Config configComponent = go.GetComponent<Config>(); 

            if (configComponent != null)
            {
                // Call the SetCurrentBackgroundBlank method on the Config component
                configComponent.SetCurrentBackgroundBlank(); // erst dort wird der jeweilige Character blank gesetzt
            }
            else
            {
                Debug.LogError("Config component not found on GameObject 'GameLogic'.");
            }
        }
        else
        {
            Debug.LogError("GameObject 'GameLogic' not found.");
        }

        // operiert auf dem BackgroundArray, dieses
        // ist direkt bei der GameLogic im GameObject angesiedelt
        // Ensure backgroundIndex is within the bounds of the array
        Debug.Log($"backgroundIndex: {backgroundIndex}, backgrounds.Length: {backgrounds.Length}");

        if (backgroundIndex < backgrounds.Length)
        {
            // Set the current background's sprite to be visible
            SpriteRenderer currentSpriteRenderer = backgrounds[backgroundIndex].GetComponent<SpriteRenderer>();
            if (currentSpriteRenderer != null)
            {
                Color currentColor = currentSpriteRenderer.color;
                currentColor.a = 1f; // Set alpha to 1 to make the sprite fully visible
                currentSpriteRenderer.color = currentColor;
            }

            // Handle wrap-around and set the previous background invisible
            int previousIndex = (backgroundIndex - 1 + backgrounds.Length) % backgrounds.Length;
            SpriteRenderer previousSpriteRenderer = backgrounds[previousIndex].GetComponent<SpriteRenderer>();
            if (previousSpriteRenderer != null)
            {
                Color previousColor = previousSpriteRenderer.color;
                previousColor.a = 0f; // Set alpha to 0 to make the sprite invisible
                previousSpriteRenderer.color = previousColor;
            }

            // Increment the backgroundIndex
            backgroundIndex++;
        }

        // Reset backgroundIndex if it exceeds the array length
        if (backgroundIndex >= backgrounds.Length)
        {
            backgroundIndex = 0;
        }
    }

    }
