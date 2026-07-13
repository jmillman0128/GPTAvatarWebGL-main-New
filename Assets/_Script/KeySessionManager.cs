using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Manages API key resolution at app startup.
///
/// SCENE SETUP — wire these in the Inspector on a KeySessionManager GameObject
/// inside your Startup Menu hierarchy:
///
///   Panels (GameObjects, toggled via SetActive):
///     savedKeyPanel  — shown when a key is already stored on the device
///                      needs two child Buttons wired to useSavedKeyButton / enterNewKeyButton
///     waitingPanel   — shown while the user has not yet submitted a key
///                      needs a large TMP_Text for the code and a smaller one for status
///     loadingPanel   — shown briefly while the experience loads after key is received
///
///   Relay URL:
///     relayBaseUrl   — set to your deployed relay server URL, e.g. https://your-relay.onrender.com
///
/// PHASE 2 HOOK:
///   After this script resolves a key it calls OnKeyReady().
///   Phase 2 will extend that method to trigger AzureFileDownload.BeginDownload()
///   and inject the key into Config.OnConfigDownloadComplete() instead of the
///   hard-coded value.  Until then, ResolvedOpenAIKey is available as a static
///   property for Config.cs to read.
/// </summary>
public class KeySessionManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    //  Inspector fields — wire these up in the Unity Editor
    // -------------------------------------------------------------------------

    [Header("Panels")]
    [SerializeField] private GameObject savedKeyPanel;
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private GameObject loadingPanel;

    [Tooltip("Panel that contains the case-number (experience ID) entry UI. " +
             "Activated after the key is resolved in VR/Android builds. " +
             "Leave unassigned in WebGL mode — the download starts automatically.")]
    [SerializeField] private GameObject caseNumberPanel;

    [Tooltip("Parent GameObject that contains all key-pairing panels (savedKeyPanel, waitingPanel, " +
             "loadingPanel). This entire object is scaled to zero once the API key is resolved, " +
             "regardless of which panel the flow ends on.")]
    [SerializeField] private GameObject keyPairingContainer;

    [Header("Scene References")]
    [Tooltip("Drag the FileDownloader GameObject here (the one with AzureFileDownload attached).")]
    [SerializeField] private AzureFileDownload azureFileDownload;

    [Header("Text Elements")]
    [Tooltip("Large TMP_Text that displays the 6-digit pairing code")]
    [SerializeField] private TMP_Text sessionCodeText;
    [Tooltip("Smaller TMP_Text for status and instruction messages")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("TMP_Text on the saved-key panel — lists which key types are already stored on the device")]
    [SerializeField] private TMP_Text savedKeyTypeText;

    [Header("Saved Key Panel — Buttons")]
    [Tooltip("Button that continues the experience using the stored key")]
    [SerializeField] private Button useSavedKeyButton;
    [Tooltip("Button that clears the stored key and prompts for a new one")]
    [SerializeField] private Button enterNewKeyButton;

    [Header("Relay Server")]
    [Tooltip("Base URL of your deployed key-relay server, no trailing slash")]
    [SerializeField] private string relayBaseUrl = "https://your-relay.example.com";

    [Header("Start Menu")]
    [Tooltip("The root GameObject of the startup menu that should be hidden during API key pairing " +
             "and revealed once the key is ready. Its localScale will be tweened to zero on start " +
             "and back to (1,1,1) after the key is resolved.")]
    [SerializeField] private GameObject startMenu;
    [Tooltip("Duration (seconds) of the scale-in animation when the start menu appears.")]
    [SerializeField] private float startMenuScaleInDuration = 0.4f;

    // -------------------------------------------------------------------------
    //  API key type detection
    // -------------------------------------------------------------------------

    public enum ApiKeyType { Unknown, OpenAI, Google, ElevenLabs }

    /// <summary>
    /// Detects the service provider of an API key from its format.
    ///   OpenAI     — starts with "sk-"  (covers sk-proj-..., standard sk-... keys)
    ///   Google TTS — starts with "AIza" and is at least 35 characters
    ///   ElevenLabs — exactly 32 lowercase hexadecimal characters
    /// </summary>
    public static ApiKeyType DetectKeyType(string key)
    {
        if (string.IsNullOrWhiteSpace(key))              return ApiKeyType.Unknown;
        if (key.StartsWith("sk-"))                       return ApiKeyType.OpenAI;
        if (key.StartsWith("AIza") && key.Length >= 35)  return ApiKeyType.Google;
        if (Regex.IsMatch(key, @"^[a-f0-9]{32}$"))       return ApiKeyType.ElevenLabs;
        return ApiKeyType.Unknown;
    }

    // -------------------------------------------------------------------------
    //  Static API — read by Config.cs
    //  Properties read live from PlayerPrefs so they are always current.
    // -------------------------------------------------------------------------

    private const string PREFS_OPENAI     = "api_key_openai";
    private const string PREFS_GOOGLE     = "api_key_google";
    private const string PREFS_ELEVENLABS = "api_key_elevenlabs";

    /// <summary>OpenAI key stored on this device. Empty if not yet provided.</summary>
    public static string ResolvedOpenAIKey
        => PlayerPrefs.GetString(PREFS_OPENAI, string.Empty);

    /// <summary>Google TTS key stored on this device. Empty if not provided.</summary>
    public static string ResolvedGoogleKey
        => PlayerPrefs.GetString(PREFS_GOOGLE, string.Empty);

    /// <summary>ElevenLabs key stored on this device. Empty if not provided.</summary>
    public static string ResolvedElevenLabsKey
        => PlayerPrefs.GetString(PREFS_ELEVENLABS, string.Empty);

    /// <summary>
    /// True when an OpenAI key is present. The experience requires OpenAI;
    /// Google and ElevenLabs keys are supplemental.
    /// </summary>
    public static bool IsKeyReady => !string.IsNullOrEmpty(ResolvedOpenAIKey);

    // -------------------------------------------------------------------------
    //  Private state
    // -------------------------------------------------------------------------

    private const float POLL_INTERVAL_S = 3f;
    private const float TIMEOUT_S       = 600f;

    private string    _sessionCode;
    private Coroutine _pollCoroutine;
    private float     _elapsedWait;

    // -------------------------------------------------------------------------
    //  Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Migrate from the old single-key PlayerPrefs entry (pre-multi-key builds)
        if (PlayerPrefs.HasKey("openai_key") && !PlayerPrefs.HasKey(PREFS_OPENAI))
        {
            PlayerPrefs.SetString(PREFS_OPENAI, PlayerPrefs.GetString("openai_key"));
            PlayerPrefs.DeleteKey("openai_key");
            PlayerPrefs.Save();
            Debug.Log("[KeySessionManager] Migrated legacy openai_key PlayerPrefs entry.");
        }

        // Ensure the key-pairing container is fully visible at startup
        if (keyPairingContainer != null)
        {
            keyPairingContainer.SetActive(true);
            keyPairingContainer.transform.localScale = Vector3.one;
        }

        // Hide the start menu immediately so it doesn't overlap the key-pairing UI
        if (startMenu != null)
            startMenu.transform.localScale = Vector3.zero;

        SetAllPanelsInactive();

        if (IsKeyReady)
            ShowSavedKeyPanel();
        else
            BeginWaitingFlow();
    }

    // -------------------------------------------------------------------------
    //  Panel management
    // -------------------------------------------------------------------------

    private void SetAllPanelsInactive()
    {
        if (savedKeyPanel   != null) savedKeyPanel.SetActive(false);
        if (waitingPanel    != null) waitingPanel.SetActive(false);
        if (loadingPanel    != null) loadingPanel.SetActive(false);
        if (caseNumberPanel != null) caseNumberPanel.SetActive(false);
    }

    private void ShowSavedKeyPanel()
    {
        if (savedKeyPanel == null)
        {
            Debug.LogWarning("[KeySessionManager] savedKeyPanel not assigned — falling back to waiting flow.");
            BeginWaitingFlow();
            return;
        }

        // Show which key types are already saved
        if (savedKeyTypeText != null)
        {
            var found = new List<string>();
            if (!string.IsNullOrEmpty(ResolvedOpenAIKey))     found.Add("OpenAI");
            if (!string.IsNullOrEmpty(ResolvedGoogleKey))     found.Add("Google TTS");
            if (!string.IsNullOrEmpty(ResolvedElevenLabsKey)) found.Add("ElevenLabs");
            savedKeyTypeText.text = "Saved on device: " + string.Join(", ", found);
        }

        savedKeyPanel.SetActive(true);

        if (useSavedKeyButton != null) useSavedKeyButton.onClick.AddListener(OnUseSavedKeyPressed);
        if (enterNewKeyButton != null) enterNewKeyButton.onClick.AddListener(OnEnterNewKeyPressed);
    }

    // -------------------------------------------------------------------------
    //  Saved key panel callbacks
    // -------------------------------------------------------------------------

    private void OnUseSavedKeyPressed()
    {
        if (!IsKeyReady)
        {
            // Guard: flag set but OpenAI key is empty — fall through to waiting
            savedKeyPanel.SetActive(false);
            BeginWaitingFlow();
            return;
        }

        SetAllPanelsInactive();
        OnKeyReady();
    }

    private void OnEnterNewKeyPressed()
    {
        ClearStoredKey();
        savedKeyPanel.SetActive(false);
        BeginWaitingFlow();
    }

    // -------------------------------------------------------------------------
    //  Waiting / polling flow
    // -------------------------------------------------------------------------

    private void BeginWaitingFlow()
    {
        if (string.IsNullOrEmpty(relayBaseUrl) || relayBaseUrl.StartsWith("https://your-relay"))
            Debug.LogError("[KeySessionManager] relayBaseUrl is not configured — set it in the Inspector.");

        if (waitingPanel != null) waitingPanel.SetActive(true);
        GenerateNewCode();
        _pollCoroutine = StartCoroutine(PollForKey());
    }

    /// <summary>Generates a fresh 6-digit code and resets the wait timer.</summary>
    private void GenerateNewCode()
    {
        _sessionCode = UnityEngine.Random.Range(100000, 999999).ToString();
        _elapsedWait = 0f;
        if (sessionCodeText != null) sessionCodeText.text = _sessionCode;
        if (statusText      != null) statusText.text = "Visit the website and enter this code to begin.";
    }

    private IEnumerator PollForKey()
    {
        while (_elapsedWait < TIMEOUT_S)
        {
            yield return new WaitForSeconds(POLL_INTERVAL_S);
            _elapsedWait += POLL_INTERVAL_S;

            yield return StartCoroutine(FetchKey(_sessionCode));
        }

        // Session window elapsed without a key being submitted
        if (statusText != null)
            statusText.text = "Session expired. Please restart the app.";

        Debug.LogWarning("[KeySessionManager] Polling timed out after " + TIMEOUT_S + "s.");
    }

    private IEnumerator FetchKey(string code)
    {
        string url = relayBaseUrl.TrimEnd('/') + "/api/get-key?code=" + code;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                // HTTP 200 — parse the returned key
                try
                {
                    KeyResponse parsed = JsonUtility.FromJson<KeyResponse>(req.downloadHandler.text);
                    if (parsed != null && !string.IsNullOrEmpty(parsed.openaiKey))
                    {
                        ResolveKeys(parsed);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[KeySessionManager] Failed to parse key response: " + ex.Message);
                }
            }
            // HTTP 404 (key not yet submitted) or network error — continue polling silently
        }
    }

    // -------------------------------------------------------------------------
    //  Key resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when the relay returns all three keys in one payload.
    /// Stores each non-empty key in PlayerPrefs, then advances to the experience.
    /// </summary>
    private void ResolveKeys(KeyResponse keys)
    {
        if (_pollCoroutine != null) { StopCoroutine(_pollCoroutine); _pollCoroutine = null; }

        PlayerPrefs.SetString(PREFS_OPENAI, keys.openaiKey);
        if (!string.IsNullOrEmpty(keys.googleKey))
            PlayerPrefs.SetString(PREFS_GOOGLE, keys.googleKey);
        if (!string.IsNullOrEmpty(keys.elevenLabsKey))
            PlayerPrefs.SetString(PREFS_ELEVENLABS, keys.elevenLabsKey);
        PlayerPrefs.Save();

        Debug.Log("[KeySessionManager] Keys stored — OpenAI: yes"
            + (!string.IsNullOrEmpty(keys.googleKey)     ? ", Google TTS: yes" : "")
            + (!string.IsNullOrEmpty(keys.elevenLabsKey) ? ", ElevenLabs: yes" : ""));

        SetAllPanelsInactive();
        OnKeyReady();
    }

    /// <summary>
    /// Called once the API key is ready.
    /// Collapses all key-pairing panels by scaling keyPairingContainer to zero,
    /// triggers AzureFileDownload.BeginDownload(), then reveals the start menu.
    /// In VR/Android builds the case-number panel is shown after the container
    /// finishes scaling out; leave caseNumberPanel unassigned in WebGL builds.
    /// </summary>
    protected virtual void OnKeyReady()
    {
        // Hide every individual panel immediately so nothing overlaps during
        // the scale-out animation.
        SetAllPanelsInactive();

        // Trigger config download.
        // WebGL / Editor: fileUrl is already set, so this downloads immediately.
        // VR / Android:   fileUrl is empty, BeginDownload() logs and returns;
        //                 OnEnterPressed() in AzureFileDownload handles the
        //                 actual download once the user enters a case number.
        if (azureFileDownload != null)
            azureFileDownload.BeginDownload();
        else
            Debug.LogWarning("[KeySessionManager] azureFileDownload is not assigned in the Inspector.");

        // Collapse the key-pairing container, then reveal whatever comes next.
        if (keyPairingContainer != null)
            StartCoroutine(ScaleOutThenReveal(keyPairingContainer.transform, startMenuScaleInDuration));
        else
            RevealAfterKeyReady();
    }

    /// <summary>Scales a transform to zero, then calls RevealAfterKeyReady.</summary>
    /// <remarks>
    /// We intentionally do NOT call SetActive(false) on the container here.
    /// If KeySessionManager is a child of keyPairingContainer, deactivating the
    /// container would kill this coroutine before RevealAfterKeyReady() executes,
    /// preventing startMenu from ever scaling in.  Scale-zero is visually equivalent.
    /// </remarks>
    private IEnumerator ScaleOutThenReveal(Transform t, float duration)
    {
        Vector3 startScale = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            t.localScale = startScale * (1f - progress);
            yield return null;
        }
        t.localScale = Vector3.zero;
        RevealAfterKeyReady();
    }

    /// <summary>
    /// Shows whatever the user should see immediately after the key is accepted.
    /// Called either after ScaleOutThenReveal completes or directly when
    /// keyPairingContainer is unassigned.
    /// </summary>
    private void RevealAfterKeyReady()
    {
        // VR/Android only: show the case-number entry panel so the user can
        // select their experience. Leave caseNumberPanel unassigned in WebGL —
        // the download starts automatically via azureFileDownload.BeginDownload().
        if (caseNumberPanel != null) caseNumberPanel.SetActive(true);

        // Animate the start menu in now that key pairing is fully done.
        if (startMenu != null)
            StartCoroutine(ScaleIn(startMenu.transform, startMenuScaleInDuration));
    }

    private IEnumerator ScaleIn(Transform t, float duration)
    {
        t.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // Ease out back: overshoot then settle
            float overshoot = 1.70158f;
            progress -= 1f;
            float scale = progress * progress * ((overshoot + 1f) * progress + overshoot) + 1f;
            t.localScale = Vector3.one * scale;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Clears the stored key and resets state.
    /// Can be called from a UI "Sign Out / Change Key" button if desired.
    /// </summary>
    /// <summary>Clears all stored API keys from the device.</summary>
    public void ClearStoredKey()
    {
        PlayerPrefs.DeleteKey(PREFS_OPENAI);
        PlayerPrefs.DeleteKey(PREFS_GOOGLE);
        PlayerPrefs.DeleteKey(PREFS_ELEVENLABS);
        PlayerPrefs.Save();
        Debug.Log("[KeySessionManager] All stored API keys cleared from device.");
    }

    // -------------------------------------------------------------------------
    //  JSON helper — maps the relay server's { "apiKey": "..." } response
    // -------------------------------------------------------------------------

    [Serializable]
    private class KeyResponse
    {
        public string openaiKey;
        public string googleKey;
        public string elevenLabsKey;
    }
}
