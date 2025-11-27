using UnityEngine;
using OdinNative.Unity;
using System.Collections.Generic;

public class AudioSettingsManager : MonoBehaviour
{
    private static AudioSettingsManager instance;
    public static AudioSettingsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioSettingsManager>();
            }
            return instance;
        }
    }

    // 設定保存キー
    private const string KEY_MICROPHONE_INDEX = "ODIN_MIC_INDEX";
    private const string KEY_SPEAKER_VOLUME = "ODIN_SPEAKER_VOLUME";
    private const string KEY_MIC_VOLUME = "ODIN_MIC_VOLUME";
    private const string KEY_NOISE_SUPPRESSION = "ODIN_NOISE_SUPPRESSION";
    private const string KEY_VAD_ENABLED = "ODIN_VAD_ENABLED";
    private const string KEY_VAD_SENSITIVITY = "ODIN_VAD_SENSITIVITY";

    // 現在の設定
    [Header("Audio Devices")]
    public int selectedMicrophoneIndex = 0;
    public float speakerVolume = 1.0f;
    public float microphoneVolume = 1.0f;

    [Header("Voice Processing")]
    public bool noiseSuppressionEnabled = true;
    public bool vadEnabled = true;
    public int vadSensitivity = 3;

    [Header("Runtime Info")]
    public string currentMicrophoneName = "";
    public List<string> availableMicrophones = new List<string>();

    void Awake()
    {
        instance = this;
        LoadSettings();
        RefreshDeviceList();
    }

    void Start()
    {
        ApplySettings();
    }

    public void RefreshDeviceList()
    {
        availableMicrophones.Clear();

        string[] devices = Microphone.devices;
        foreach (string device in devices)
        {
            availableMicrophones.Add(device);
        }

        if (availableMicrophones.Count > 0 && selectedMicrophoneIndex < availableMicrophones.Count)
        {
            currentMicrophoneName = availableMicrophones[selectedMicrophoneIndex];
        }

        Debug.Log($"[AudioSettings] Found {availableMicrophones.Count} microphone(s)");
    }

    public void LoadSettings()
    {
        // PlayerPrefsから設定を読み込み
        selectedMicrophoneIndex = PlayerPrefs.GetInt(KEY_MICROPHONE_INDEX, 0);
        speakerVolume = PlayerPrefs.GetFloat(KEY_SPEAKER_VOLUME, 1.0f);
        microphoneVolume = PlayerPrefs.GetFloat(KEY_MIC_VOLUME, 1.0f);
        noiseSuppressionEnabled = PlayerPrefs.GetInt(KEY_NOISE_SUPPRESSION, 1) == 1;
        vadEnabled = PlayerPrefs.GetInt(KEY_VAD_ENABLED, 1) == 1;
        vadSensitivity = PlayerPrefs.GetInt(KEY_VAD_SENSITIVITY, 3);

        Debug.Log("[AudioSettings] Settings loaded from PlayerPrefs");
    }

    public void SaveSettings()
    {
        // PlayerPrefsに設定を保存
        PlayerPrefs.SetInt(KEY_MICROPHONE_INDEX, selectedMicrophoneIndex);
        PlayerPrefs.SetFloat(KEY_SPEAKER_VOLUME, speakerVolume);
        PlayerPrefs.SetFloat(KEY_MIC_VOLUME, microphoneVolume);
        PlayerPrefs.SetInt(KEY_NOISE_SUPPRESSION, noiseSuppressionEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_VAD_ENABLED, vadEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_VAD_SENSITIVITY, vadSensitivity);
        PlayerPrefs.Save();

        Debug.Log("[AudioSettings] Settings saved to PlayerPrefs");
    }

    public void ApplySettings()
    {
        // マイクデバイスを設定
        if (OdinHandler.Instance != null && OdinHandler.Instance.Microphone != null)
        {
            if (selectedMicrophoneIndex < availableMicrophones.Count)
            {
                // マイクを再起動して新しいデバイスを適用
                OdinHandler.Instance.Microphone.StopListen();

                // Unity標準のマイク設定
                currentMicrophoneName = availableMicrophones[selectedMicrophoneIndex];

                OdinHandler.Instance.Microphone.StartListen();
                Debug.Log($"[AudioSettings] Microphone set to: {currentMicrophoneName}");
            }

            // ボリューム設定
            AudioListener.volume = speakerVolume;

            // VAD設定
            if (OdinHandler.Instance.Config != null)
            {
                OdinHandler.Instance.Config.VoiceActivityDetection = vadEnabled;
                OdinHandler.Instance.Config.VoiceActivityDetectionAttackProbability = vadSensitivity * 0.2f;
            }
        }

        Debug.Log("[AudioSettings] All settings applied");
    }

    public void SetMicrophone(int index)
    {
        if (index >= 0 && index < availableMicrophones.Count)
        {
            selectedMicrophoneIndex = index;
            ApplySettings();
            SaveSettings();
        }
    }

    public void SetSpeakerVolume(float volume)
    {
        speakerVolume = Mathf.Clamp01(volume);
        AudioListener.volume = speakerVolume;
        SaveSettings();
    }

    public void SetMicrophoneVolume(float volume)
    {
        microphoneVolume = Mathf.Clamp01(volume);
        SaveSettings();
    }

    public void SetNoiseSuppression(bool enabled)
    {
        noiseSuppressionEnabled = enabled;
        ApplySettings();
        SaveSettings();
    }

    public void SetVAD(bool enabled)
    {
        vadEnabled = enabled;
        ApplySettings();
        SaveSettings();
    }

    public void SetVADSensitivity(int sensitivity)
    {
        vadSensitivity = Mathf.Clamp(sensitivity, 1, 5);
        ApplySettings();
        SaveSettings();
    }

    public void ResetToDefaults()
    {
        selectedMicrophoneIndex = 0;
        speakerVolume = 1.0f;
        microphoneVolume = 1.0f;
        noiseSuppressionEnabled = true;
        vadEnabled = true;
        vadSensitivity = 3;

        SaveSettings();
        ApplySettings();

        Debug.Log("[AudioSettings] Reset to default settings");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            // アプリ復帰時にデバイスリストを更新
            RefreshDeviceList();
            ApplySettings();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            // フォーカス復帰時にデバイスリストを更新
            RefreshDeviceList();
        }
    }

    void OnDestroy()
    {
        SaveSettings();
    }
}