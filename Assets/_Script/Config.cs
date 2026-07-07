using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class Friend : ScriptableObject
{
    public string _name = "Unset";
    public string _language = "english";
    public string _basePrompt = "unset";
    public string _directionPrompt;
    public string _advicePrompt = "Tell me there has been an error";
    public int _index;
    public int _friendTokenMemory = 200;
    public string _googleVoice = "";
    public string _elevelLabsVoice = "";
    public float _pitch = 0.0f;
    public float _speed = 1.0f;
    public string _visual = "";
    public int _maxTokensToGenerate = 50;
    public float _temperature = 1.3f;
    public float _elevenlabsStability = 0.7f;
    public string _background = "unset";
    public bool _ownbackground = false;
}

public class Config : MonoBehaviour
{  
    public static bool _isTestMode = false; //could do anything, _testMode is checked by random functions
    public const float _clientVersion = 0.1f;
  
    static Config _this;
    public List<AudioClip> m_audioClips;
    AIManager _aiManagerScript;
    public List<Friend> _friendList = new List<Friend>();
    string _loadedConfigFile = "";
    float m_version = 0.01f;


    // new variables by m fink
    public static String shortenedConfig = "";
    public static String keyConfig = "";

    private void Start()
    {
        RTAudioManager.Get().AddClipsToLibrary(m_audioClips);

        // Subscribe to the download completion event
        AzureFileDownload.OnDownloadComplete += OnConfigDownloadComplete;

        // If already downloaded (just in case), proceed directly
        // we need to make sure the cases are downloaded, before proceeding
        if (AzureFileDownload.isDownloadComplete)
        {
            OnConfigDownloadComplete();
        }
    }

    private void OnConfigDownloadComplete()
    {

        // new code for downloading files
        TextAsset txtAsset = AzureFileDownload.downloadedTextAsset;
        //Debug.Log("Downloaded Text Content: " + AzureFileDownload.downloadedText);


        // API keys are resolved at startup by KeySessionManager and stored in PlayerPrefs.
        // OpenAI is required; Google TTS falls back to the internal developer key if not
        // user-supplied; ElevenLabs is used only when the user has provided their own key.
        if (!KeySessionManager.IsKeyReady)
            Debug.LogWarning("[Config] OnConfigDownloadComplete fired before an OpenAI key was resolved — dialogue will not work.");

        var keySb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(KeySessionManager.ResolvedOpenAIKey))
            keySb.AppendLine("set_openai_api_key|" + KeySessionManager.ResolvedOpenAIKey);

        // Google TTS: prefer user-supplied key, otherwise fall back to internal developer key
        keySb.AppendLine("set_google_api_key|" + (
            !string.IsNullOrEmpty(KeySessionManager.ResolvedGoogleKey)
                ? KeySessionManager.ResolvedGoogleKey
                : ""));

        // ElevenLabs: use user-supplied key only if present, otherwise leave empty
        keySb.AppendLine("set_elevenlabs_api_key|" + KeySessionManager.ResolvedElevenLabsKey);

        String hardCodedKeys = keySb.ToString();

        string tileFile = hardCodedKeys + AzureFileDownload.FilterConfig(AzureFileDownload.downloadedText); 
        // Debug.Log("Final Config Content in tileFile: " + tileFile);


        LoadConfigFile(tileFile); // load config file

        // my code for saving in the editor and removing keys
        // call this method to instantiate the variables 
        // the variables are  Config.shortenedConfig  and  Config.keyConfig
        GetConfigTextWithoutKeys();

        // Optional cleanup
        AzureFileDownload.OnDownloadComplete -= OnConfigDownloadComplete;
    }

    static public Config Get() { return _this; }

    void Awake()
    {
#if RT_BETA

#endif
        _this = this;
        _aiManagerScript = gameObject.GetComponent<AIManager>();
    }

    public string GetVersionString() { return m_version.ToString("0.00"); }
    public float GetVersion() { return m_version; }

    string LoadConfigFromFile(string fileName)
    {

        string config = "";

        try
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                config = reader.ReadToEnd();
            }

        }
        catch (FileNotFoundException e)
        {
            Debug.Log("Rename config_template.txt to config.txt! (" + e.Message + ")");
        }

        return config;
    }

    public string GetConfigText()
    {

        if (_loadedConfigFile.Length > 3)
        {
            //already have one loaded
            return _loadedConfigFile;
        }

        return "";
    }

    // outcommented code, tried to develop a config file for each 
    public string GetConfigTextWithoutKeysPerCase()
    {
        /*
        // get Index of Current Friend -> we need it to read the name
        int curFriendIndex = _aiManagerScript.GetFriendIndex();
        String friendName = _friendList[curFriendIndex]._name;
        int startIndex = _loadedConfigFile.IndexOf("add_friend|" + friendName); // start index of friend in config
        Debug.Log("Index of current friend beginning in config: " + startIndex);

        // now we need to know until what line this friend goes
        // we need to loop over the config end encounter the third occurance
        string searchWord = "<END_TEXT>";
        int occurrenceCount = 3; // We want the third occurrence
        int endIndex = -1; // end of the current friend

        for (int i = 0; i < occurrenceCount; i++)
        {
            endIndex = _loadedConfigFile.IndexOf(searchWord, endIndex + 1);
            Debug.Log("Looping in the search function: Oucurrance of <End_Text>: " + i );
            // If the word is not found, break out of the loop
            if (endIndex == -1)
            {
                Debug.Log("The word was not found " + occurrenceCount + " times.");
                return "";
            }
        }

        // Calculate the length of the friend description in the config for the substring method 
        //  the length of "<END_TEXT>" is 11 characters and is added
        int friendConfigLength = endIndex - startIndex;

        Debug.Log("Length: " + friendConfigLength);*/

        //Debug.Log("This is the config text: " + _loadedConfigFile);
        //String shortenedConfig = "";
        return "";
    }

    // function added by max fink to get config without keys
    public string GetConfigTextWithoutKeys()
    {

        // #PlaceHolderForFilterConfig
        // only run if the length is long enough and if the model actually still contains keys
        string pattern = "#PlaceHolderForFilterConfig";
        if (_loadedConfigFile.Length > 3 && _loadedConfigFile.Contains(pattern))
        {
            int startIndex = _loadedConfigFile.IndexOf(pattern) + pattern.Length;
            int keysEndIndex = startIndex - 1;

            //Debug.Log("End Index (end of Keys): " + keysEndIndex + "; StartIndex: " + startIndex);

            // write the keyConfig in its variable,
            // it starts at 0 and goes to the keysEndIndex from the string match above
            if (_loadedConfigFile.Length > (keysEndIndex))
            { 
               keyConfig = _loadedConfigFile.Substring(0, keysEndIndex);
               //Debug.Log("keyConfig:" + keyConfig);
            }

            // write the keyConfig in its variable,
            // it starts at the startIndex for the string match above, that starts describing the cases until the file end
            shortenedConfig = _loadedConfigFile.Substring(startIndex);
            //Debug.Log("shortenedConfig: " + shortenedConfig);

            //already have one loaded
            // always return the full version
            return _loadedConfigFile;
        }
        // return the last pure file, if it doesnt contain any keys
        else if (_loadedConfigFile.Length > 3 && !_loadedConfigFile.Contains(pattern))
        {
            return _loadedConfigFile;
        }

        return "";
    }

    public string ResetConfig()
    {
        _loadedConfigFile = "";
        return _loadedConfigFile;
    }
    public void LoadConfigFile(string fileContents)
    {
        int curFriendIndex = _aiManagerScript.GetFriendIndex();

        _loadedConfigFile = fileContents;

        //process it line by line
        _friendList.Clear();

        Friend friend = null;

        using (var reader = new StringReader(fileContents))
        {
            for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                // Do something with the line
                string[] words = line.Trim().Split('|');
                if (words[0] == "set_google_api_key")
                {
                    _aiManagerScript.SetGoogleAPIKey(words[1]);
                }
                else
                if (words[0] == "set_openai_api_key")
                {
                    _aiManagerScript.SetOpenAI_APIKey(words[1]);
                }
                else if(words[0] == "set_openai_model")
                {
                    _aiManagerScript.SetOpenAI_Model(words[1]);
                }
                else
                if (words[0] == "set_elevenlabs_api_key")
                {
                    _aiManagerScript.SetElevenLabsAPIKey(words[1]);
                }
                else
                if (words[0] == "add_friend")
                {
                    //need to use scriptable object
                    friend = ScriptableObject.CreateInstance<Friend>();
                    _friendList.Add(friend);
                    friend._index = _friendList.Count - 1;

                    friend._name = words[1];
                    Debug.Log("adding friend " + friend._name);
                } 
                else if (words[0] == "set_friend_base_prompt")
                {
                    //multiline input
                    //collect all the proceeding lines until a line has only "<END_TEXT>" on it
                    string basePrompt = "";
                    for (string line2 = reader.ReadLine(); line2 != null; line2 = reader.ReadLine())
                    {
                        if (line2.Trim() == "<END_TEXT>")
                        { break; }
                        else
                        {
                            basePrompt += line2 + "\n";   
                        }
                    }

                    friend._basePrompt = basePrompt.Trim();
                }
                else if (words[0] == "set_friend_direction_prompt")
                {
                    //multiline input
                    //collect all the proceeding lines until a line has only "<END_TEXT>" on it
                    string basePrompt = "";
                    for (string line2 = reader.ReadLine(); line2 != null; line2 = reader.ReadLine())
                    {
                        if (line2.Trim().ToUpper() == "<END_TEXT>")
                        { break; }
                        else
                        {
                            basePrompt += line2 + "\n";
                        }
                    }

                    friend._directionPrompt = basePrompt.Trim();
                }
                else if (words[0] == "set_friend_advice_prompt")
                {
                    //multiline input
                    //collect all the proceeding lines until a line has only "<END_TEXT>" on it
                    string basePrompt = "";
                    for (string line2 = reader.ReadLine(); line2 != null; line2 = reader.ReadLine())
                    {
                        if (line2.Trim() == "<END_TEXT>")
                        { break; }
                        else
                        {
                            basePrompt += line2 + "\n";
                        }
                    }

                    friend._advicePrompt = basePrompt.Trim();
                } else
                if (words[0] == "set_friend_language")
                {
                    friend._language = words[1].ToLower();
                }
                else
                if (words[0] == "set_friend_token_memory")
                {
                    //convert to int with TryParse
                    int.TryParse(words[1], out friend._friendTokenMemory);
                } else
                if (words[0] == "set_friend_max_tokens_to_generate")
                {
                    //convert to int with TryParse
                    int.TryParse(words[1], out friend._maxTokensToGenerate);
                }

                else
                if (words[0] == "set_friend_voice_pitch")
                {
                    //convert to int with TryParse
                    float.TryParse(words[1], out friend._pitch);
                }
                else
                if (words[0] == "set_friend_voice_speed")
                {
                    //convert to int with TryParse
                    float.TryParse(words[1], out friend._speed);
                }
                else
                if (words[0] == "set_friend_google_voice")
                {
                    //convert to int with TryParse
                    friend._googleVoice = words[1];
                }
                else
                if (words[0] == "set_friend_elevenlabs_voice")
                {
                    //convert to int with TryParse
                    friend._elevelLabsVoice = words[1];
                }
                else
                if (words[0] == "set_friend_visual")
                {
                    //convert to int with TryParse
                    friend._visual = words[1];
                }
                else
                if (words[0] == "set_friend_temperature")
                {
                    //convert to int with TryParse
                    float.TryParse(words[1], out friend._temperature);
                }
                else
                if (words[0] == "set_friend_elevenlabs_stability")
                {
                    //convert to int with TryParse
                    float.TryParse(words[1], out friend._elevenlabsStability);
                }

                // activating and deactivating backgrounds (Code by Max)
                else if (words[0] == "#set_background")
                {
                    friend._background = words[1];
                    friend._ownbackground = true;  // flag set for loading background -> this takes place somewhere else
                    // Debug.Log("Found background: " + words[1]);
                }



            }
        }

        //Debug.Log("Loaded config.  A total of " + _friendList.Count + " friends laoded.");
        
        
        if (_friendList.Count == 0)
        {
            //error
            _friendList.Add(ScriptableObject.CreateInstance<Friend>()); //at least add one

        }

        if (curFriendIndex >= _friendList.Count)
        {
            //error
            curFriendIndex = 0;
        }

        _aiManagerScript.SetActiveFriend(_friendList[curFriendIndex]);
        
    }

    public Friend GetFriendByIndex(int index)
    {
        return _friendList[index];
    }

    public int GetFriendCount() 
    {
        return _friendList.Count;
    }

    public void SetCurrentBackgroundBlank()
    {
        // get the info about the current friend -> we need the name in string
        int curFriendIndex = _aiManagerScript.GetFriendIndex();
        String currFriendName = _friendList[curFriendIndex]._visual;
        // Debug.Log("Aktueller FreundName: " + currFriendName);

        // search for the current friend as Game object
        GameObject targetObject = GameObject.Find("char_visual_" + currFriendName + "/background");

        // deactivate the background of the current friend
        if (targetObject != null)
        {
            // Get the SpriteRenderer component from the GameObject
            SpriteRenderer spriteRenderer = targetObject.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                // Set the sprite's color to be fully transparent
                Color color = spriteRenderer.color;
                color.a = 0f; // Set alpha to 0 to make the sprite invisible
                spriteRenderer.color = color;
            }
            else
            {
                Debug.LogError("SpriteRenderer component not found on the GameObject: " + currFriendName);
            }
        }
        else
        {
            Debug.LogError("GameObject with name " + currFriendName + " not found.");
        }

    }

}
