using System;
using UnityEngine;
using UnityEngine.Rendering;
using OdinVoiceChat.Core;
using OdinVoiceChat.XR;

namespace OdinVoiceChat
{
    public class PlatformManager : MonoBehaviour
    {
        public enum Platform
        {
            Windows,
            Mac,
            Quest2,
            Quest3,
            QuestPro,
            Android,
            iOS
        }

        private static PlatformManager _instance;
        public static PlatformManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlatformManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlatformManager");
                        _instance = go.AddComponent<PlatformManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Platform Detection")]
        [SerializeField] private Platform currentPlatform;
        [SerializeField] private bool autoDetectPlatform = true;
        [SerializeField] private string deviceModel;
        [SerializeField] private string deviceName;

        [Header("Platform Settings")]
        [SerializeField] private bool isXRPlatform = false;
        [SerializeField] private bool isMobilePlatform = false;
        [SerializeField] private bool supportsHandTracking = false;
        [SerializeField] private bool supports120Hz = false;

        [Header("Performance Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private int audioSampleRate = 48000;
        [SerializeField] private int audioBufferSize = 512;
        #pragma warning disable CS0414
        [SerializeField] private bool useMultithreading = true;
        #pragma warning restore CS0414

        [Header("Graphics Settings")]
        [SerializeField] private RenderPipelineAsset pcRenderPipeline;
        [SerializeField] private RenderPipelineAsset mobileRenderPipeline;
        [SerializeField] private RenderPipelineAsset questRenderPipeline;

        public event Action<Platform> OnPlatformChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoDetectPlatform)
            {
                DetectPlatform();
            }

            ApplyPlatformSettings();
        }

        private void DetectPlatform()
        {
            deviceModel = SystemInfo.deviceModel;
            deviceName = SystemInfo.deviceName;

#if UNITY_EDITOR
            #if UNITY_EDITOR_WIN
                currentPlatform = Platform.Windows;
            #elif UNITY_EDITOR_OSX
                currentPlatform = Platform.Mac;
            #endif
#elif UNITY_STANDALONE_WIN
            currentPlatform = Platform.Windows;
#elif UNITY_STANDALONE_OSX
            currentPlatform = Platform.Mac;
#elif UNITY_ANDROID
            DetectQuestDevice();
#elif UNITY_IOS
            currentPlatform = Platform.iOS;
#endif

            UpdatePlatformFlags();

            Debug.Log($"[PlatformManager] Detected Platform: {currentPlatform}, Device: {deviceModel}");
        }

        private void DetectQuestDevice()
        {
            string model = SystemInfo.deviceModel.ToLower();

            if (model.Contains("quest 3"))
            {
                currentPlatform = Platform.Quest3;
                supports120Hz = true;
                supportsHandTracking = true;
            }
            else if (model.Contains("quest pro"))
            {
                currentPlatform = Platform.QuestPro;
                supports120Hz = true;
                supportsHandTracking = true;
            }
            else if (model.Contains("quest 2"))
            {
                currentPlatform = Platform.Quest2;
                supports120Hz = true;
                supportsHandTracking = true;
            }
            else
            {
                currentPlatform = Platform.Android;
            }

            isXRPlatform = currentPlatform != Platform.Android;
        }

        private void UpdatePlatformFlags()
        {
            switch (currentPlatform)
            {
                case Platform.Quest2:
                case Platform.Quest3:
                case Platform.QuestPro:
                    isXRPlatform = true;
                    isMobilePlatform = true;
                    break;

                case Platform.Android:
                case Platform.iOS:
                    isXRPlatform = false;
                    isMobilePlatform = true;
                    break;

                case Platform.Windows:
                case Platform.Mac:
                    isXRPlatform = false;
                    isMobilePlatform = false;
                    break;
            }
        }

        private void ApplyPlatformSettings()
        {
            switch (currentPlatform)
            {
                case Platform.Windows:
                case Platform.Mac:
                    ApplyPCSettings();
                    break;

                case Platform.Quest2:
                    ApplyQuest2Settings();
                    break;

                case Platform.Quest3:
                    ApplyQuest3Settings();
                    break;

                case Platform.QuestPro:
                    ApplyQuestProSettings();
                    break;

                case Platform.Android:
                case Platform.iOS:
                    ApplyMobileSettings();
                    break;
            }

            ConfigureAudioForPlatform();
            ConfigureGraphicsForPlatform();

            OnPlatformChanged?.Invoke(currentPlatform);
        }

        private void ApplyPCSettings()
        {
            targetFrameRate = -1; // Uncapped
            audioSampleRate = 48000;
            audioBufferSize = 512;
            useMultithreading = true;

            QualitySettings.SetQualityLevel(5); // Ultra
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.antiAliasing = 4;

            Debug.Log("[PlatformManager] Applied PC settings");
        }

        private void ApplyQuest2Settings()
        {
            targetFrameRate = supports120Hz ? 120 : 90;
            audioSampleRate = 48000;
            audioBufferSize = 512;
            useMultithreading = true;

            Application.targetFrameRate = targetFrameRate;

            QualitySettings.SetQualityLevel(2); // Medium
            QualitySettings.pixelLightCount = 2;
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.antiAliasing = 2;
            QualitySettings.lodBias = 0.7f;

#if UNITY_ANDROID && !UNITY_EDITOR
            OVRManager.display.displayFrequency = targetFrameRate;
            OVRManager.cpuLevel = 2;
            OVRManager.gpuLevel = 2;
#endif

            Debug.Log("[PlatformManager] Applied Quest 2 settings");
        }

        private void ApplyQuest3Settings()
        {
            targetFrameRate = 120;
            audioSampleRate = 48000;
            audioBufferSize = 256;
            useMultithreading = true;

            Application.targetFrameRate = targetFrameRate;

            QualitySettings.SetQualityLevel(3); // High
            QualitySettings.pixelLightCount = 3;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 30f;
            QualitySettings.antiAliasing = 4;
            QualitySettings.lodBias = 1.0f;

#if UNITY_ANDROID && !UNITY_EDITOR
            OVRManager.display.displayFrequency = targetFrameRate;
            OVRManager.cpuLevel = 3;
            OVRManager.gpuLevel = 3;
            OVRManager.suggestedCpuPerfLevel = OVRManager.ProcessorPerformanceLevel.Boost;
            OVRManager.suggestedGpuPerfLevel = OVRManager.ProcessorPerformanceLevel.Boost;
#endif

            Debug.Log("[PlatformManager] Applied Quest 3 settings");
        }

        private void ApplyQuestProSettings()
        {
            targetFrameRate = 90;
            audioSampleRate = 48000;
            audioBufferSize = 256;
            useMultithreading = true;

            Application.targetFrameRate = targetFrameRate;

            QualitySettings.SetQualityLevel(4); // Very High
            QualitySettings.pixelLightCount = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 40f;
            QualitySettings.antiAliasing = 4;
            QualitySettings.lodBias = 1.2f;

#if UNITY_ANDROID && !UNITY_EDITOR
            OVRManager.display.displayFrequency = targetFrameRate;
            OVRManager.cpuLevel = 4;
            OVRManager.gpuLevel = 4;
            OVRManager.eyeTrackedFoveatedRenderingEnabled = true;
            OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.HighTop;
#endif

            Debug.Log("[PlatformManager] Applied Quest Pro settings");
        }

        private void ApplyMobileSettings()
        {
            targetFrameRate = 60;
            audioSampleRate = 44100;
            audioBufferSize = 1024;
            useMultithreading = true;

            Application.targetFrameRate = targetFrameRate;

            QualitySettings.SetQualityLevel(1); // Low
            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.lodBias = 0.5f;

            Debug.Log("[PlatformManager] Applied mobile settings");
        }

        private void ConfigureAudioForPlatform()
        {
            AudioConfiguration audioConfig = AudioSettings.GetConfiguration();
            audioConfig.sampleRate = audioSampleRate;
            audioConfig.dspBufferSize = audioBufferSize;

            if (isMobilePlatform)
            {
                audioConfig.numRealVoices = 24;
                audioConfig.numVirtualVoices = 48;
                audioConfig.speakerMode = AudioSpeakerMode.Stereo;
            }
            else
            {
                audioConfig.numRealVoices = 32;
                audioConfig.numVirtualVoices = 512;
                audioConfig.speakerMode = AudioSpeakerMode.Stereo;
            }

            AudioSettings.Reset(audioConfig);

            OdinAudioRouter audioRouter = OdinAudioRouter.Instance;
            if (audioRouter != null)
            {
                // Platform-specific audio router configuration
            }
        }

        private void ConfigureGraphicsForPlatform()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                switch (currentPlatform)
                {
                    case Platform.Windows:
                    case Platform.Mac:
                        if (pcRenderPipeline != null)
                            GraphicsSettings.defaultRenderPipeline = pcRenderPipeline;
                        break;

                    case Platform.Quest2:
                    case Platform.Quest3:
                    case Platform.QuestPro:
                        if (questRenderPipeline != null)
                            GraphicsSettings.defaultRenderPipeline = questRenderPipeline;
                        break;

                    case Platform.Android:
                    case Platform.iOS:
                        if (mobileRenderPipeline != null)
                            GraphicsSettings.defaultRenderPipeline = mobileRenderPipeline;
                        break;
                }
            }
        }

        public void SetPlatform(Platform platform)
        {
            currentPlatform = platform;
            UpdatePlatformFlags();
            ApplyPlatformSettings();
        }

        public Platform GetCurrentPlatform()
        {
            return currentPlatform;
        }

        public bool IsXRPlatform()
        {
            return isXRPlatform;
        }

        public bool IsMobilePlatform()
        {
            return isMobilePlatform;
        }

        public bool SupportsHandTracking()
        {
            return supportsHandTracking;
        }

        public bool Supports120Hz()
        {
            return supports120Hz;
        }

        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = fps;
            Application.targetFrameRate = targetFrameRate;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (isXRPlatform)
            {
                OVRManager.display.displayFrequency = targetFrameRate;
            }
#endif
        }

        public int GetRecommendedFrameRate()
        {
            switch (currentPlatform)
            {
                case Platform.Quest3:
                    return 120;
                case Platform.Quest2:
                    return supports120Hz ? 120 : 90;
                case Platform.QuestPro:
                    return 90;
                case Platform.Android:
                case Platform.iOS:
                    return 60;
                default:
                    return -1; // Uncapped for PC
            }
        }

        public void OptimizeForBattery()
        {
            if (isMobilePlatform)
            {
                targetFrameRate = 72;
                Application.targetFrameRate = targetFrameRate;

#if UNITY_ANDROID && !UNITY_EDITOR
                if (isXRPlatform)
                {
                    OVRManager.cpuLevel = 1;
                    OVRManager.gpuLevel = 1;
                }
#endif

                QualitySettings.SetQualityLevel(0); // Very Low
            }
        }

        public void OptimizeForPerformance()
        {
            if (isMobilePlatform)
            {
                targetFrameRate = GetRecommendedFrameRate();
                Application.targetFrameRate = targetFrameRate;

#if UNITY_ANDROID && !UNITY_EDITOR
                if (isXRPlatform)
                {
                    OVRManager.cpuLevel = 4;
                    OVRManager.gpuLevel = 4;
                }
#endif

                QualitySettings.SetQualityLevel(3); // High
            }
        }
    }
}