using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace OdinVoiceChat.Core
{
    public class OdinAudioRouter : MonoBehaviour
    {
        private static OdinAudioRouter _instance;
        public static OdinAudioRouter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<OdinAudioRouter>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("OdinAudioRouter");
                        _instance = go.AddComponent<OdinAudioRouter>();
                    }
                }
                return _instance;
            }
        }

        [Header("Audio Mixer Configuration")]
        [SerializeField] private AudioMixer mainAudioMixer;
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup monitorBusGroup;
        [SerializeField] private AudioMixerGroup broadcastBusGroup;

        [Header("Routing Settings")]
        [SerializeField] private bool enableDualRouting = true;
        [SerializeField] private float monitorVolume = 1.0f;
        [SerializeField] private float broadcastVolume = 1.0f;
        [SerializeField] private bool monitorMuted = false;
        [SerializeField] private bool broadcastMuted = false;

        [Header("Audio Processing")]
        [SerializeField] private bool enableCompression = true;
        [SerializeField] private bool enableEQ = false;
        [SerializeField] private bool enableReverb = false;
        [SerializeField] private float reverbAmount = 0.2f;

        [Header("Platform Specific")]
        [SerializeField] private bool useQuestOptimization = false;
        [SerializeField] private int questSampleRate = 48000;
        [SerializeField] private int pcSampleRate = 48000;

        private Dictionary<ulong, AudioSourcePair> _playerAudioSources = new Dictionary<ulong, AudioSourcePair>();
        private AudioListener _mainListener;
        private float[] _broadcastBuffer;
        private int _bufferSize = 1024;

        public event Action<float, float> OnVolumeChanged;
        public event Action<float[]> OnBroadcastBufferReady;

        public class AudioSourcePair
        {
            public AudioSource MonitorSource { get; set; }
            public AudioSource BroadcastSource { get; set; }
            public float Volume { get; set; } = 1.0f;
            public bool Muted { get; set; } = false;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            _broadcastBuffer = new float[_bufferSize * 2];

            SetupAudioListener();
            ConfigurePlatformSettings();
            ApplyMixerSettings();

            Debug.Log("[OdinAudioRouter] Audio router initialized");
        }

        private void SetupAudioListener()
        {
            _mainListener = FindFirstObjectByType<AudioListener>();
            if (_mainListener == null)
            {
                GameObject listenerObj = new GameObject("MainAudioListener");
                _mainListener = listenerObj.AddComponent<AudioListener>();
                listenerObj.transform.SetParent(transform);
            }
        }

        private void ConfigurePlatformSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            useQuestOptimization = true;
            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.sampleRate = questSampleRate;
            config.dspBufferSize = 512;
            config.numRealVoices = 32;
            config.numVirtualVoices = 64;
            AudioSettings.Reset(config);

            Debug.Log("[OdinAudioRouter] Quest audio optimization applied");
#else
            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.sampleRate = pcSampleRate;
            config.dspBufferSize = _bufferSize;
            AudioSettings.Reset(config);

            Debug.Log("[OdinAudioRouter] PC audio configuration applied");
#endif
        }

        public AudioSourcePair CreatePlayerAudioSources(ulong playerId, string playerName)
        {
            if (_playerAudioSources.ContainsKey(playerId))
            {
                Debug.LogWarning($"[OdinAudioRouter] Audio sources already exist for player {playerId}");
                return _playerAudioSources[playerId];
            }

            GameObject playerAudioObj = new GameObject($"PlayerAudio_{playerName}");
            playerAudioObj.transform.SetParent(transform);

            AudioSourcePair audioPair = new AudioSourcePair();

            GameObject monitorObj = new GameObject("MonitorSource");
            monitorObj.transform.SetParent(playerAudioObj.transform);
            audioPair.MonitorSource = monitorObj.AddComponent<AudioSource>();
            audioPair.MonitorSource.outputAudioMixerGroup = monitorBusGroup;
            audioPair.MonitorSource.spatialBlend = 0f;
            audioPair.MonitorSource.loop = true;
            audioPair.MonitorSource.playOnAwake = false;

            if (enableDualRouting)
            {
                GameObject broadcastObj = new GameObject("BroadcastSource");
                broadcastObj.transform.SetParent(playerAudioObj.transform);
                audioPair.BroadcastSource = broadcastObj.AddComponent<AudioSource>();
                audioPair.BroadcastSource.outputAudioMixerGroup = broadcastBusGroup;
                audioPair.BroadcastSource.spatialBlend = 0f;
                audioPair.BroadcastSource.loop = true;
                audioPair.BroadcastSource.playOnAwake = false;
            }

            _playerAudioSources[playerId] = audioPair;

            Debug.Log($"[OdinAudioRouter] Created audio sources for player {playerName} (ID: {playerId})");

            return audioPair;
        }

        public void RemovePlayerAudioSources(ulong playerId)
        {
            if (_playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair))
            {
                if (audioPair.MonitorSource != null)
                    Destroy(audioPair.MonitorSource.transform.parent.gameObject);

                _playerAudioSources.Remove(playerId);

                Debug.Log($"[OdinAudioRouter] Removed audio sources for player {playerId}");
            }
        }

        public void RoutePlayerAudio(ulong playerId, AudioClip audioClip)
        {
            if (!_playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair))
            {
                Debug.LogWarning($"[OdinAudioRouter] No audio sources found for player {playerId}");
                return;
            }

            if (audioPair.MonitorSource != null && !monitorMuted)
            {
                audioPair.MonitorSource.clip = audioClip;
                audioPair.MonitorSource.volume = audioPair.Volume * monitorVolume;
                if (!audioPair.MonitorSource.isPlaying)
                    audioPair.MonitorSource.Play();
            }

            if (enableDualRouting && audioPair.BroadcastSource != null && !broadcastMuted)
            {
                audioPair.BroadcastSource.clip = audioClip;
                audioPair.BroadcastSource.volume = audioPair.Volume * broadcastVolume;
                if (!audioPair.BroadcastSource.isPlaying)
                    audioPair.BroadcastSource.Play();
            }
        }

        public void SetPlayerVolume(ulong playerId, float volume)
        {
            if (_playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair))
            {
                audioPair.Volume = Mathf.Clamp01(volume);
                UpdatePlayerAudioVolumes(playerId);
            }
        }

        public void MutePlayer(ulong playerId, bool mute)
        {
            if (_playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair))
            {
                audioPair.Muted = mute;

                if (audioPair.MonitorSource != null)
                    audioPair.MonitorSource.mute = mute;

                if (audioPair.BroadcastSource != null)
                    audioPair.BroadcastSource.mute = mute;
            }
        }

        public void SetMonitorVolume(float volume)
        {
            monitorVolume = Mathf.Clamp01(volume);
            mainAudioMixer.SetFloat("MonitorVolume", Mathf.Log10(monitorVolume) * 20);

            foreach (var playerId in _playerAudioSources.Keys)
            {
                UpdatePlayerAudioVolumes(playerId);
            }

            OnVolumeChanged?.Invoke(monitorVolume, broadcastVolume);
        }

        public void SetBroadcastVolume(float volume)
        {
            broadcastVolume = Mathf.Clamp01(volume);
            mainAudioMixer.SetFloat("BroadcastVolume", Mathf.Log10(broadcastVolume) * 20);

            foreach (var playerId in _playerAudioSources.Keys)
            {
                UpdatePlayerAudioVolumes(playerId);
            }

            OnVolumeChanged?.Invoke(monitorVolume, broadcastVolume);
        }

        private void UpdatePlayerAudioVolumes(ulong playerId)
        {
            if (_playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair))
            {
                if (audioPair.MonitorSource != null)
                    audioPair.MonitorSource.volume = audioPair.Volume * monitorVolume;

                if (audioPair.BroadcastSource != null)
                    audioPair.BroadcastSource.volume = audioPair.Volume * broadcastVolume;
            }
        }

        public void MuteMonitorBus(bool mute)
        {
            monitorMuted = mute;
            mainAudioMixer.SetFloat("MonitorVolume", mute ? -80f : Mathf.Log10(monitorVolume) * 20);
        }

        public void MuteBroadcastBus(bool mute)
        {
            broadcastMuted = mute;
            mainAudioMixer.SetFloat("BroadcastVolume", mute ? -80f : Mathf.Log10(broadcastVolume) * 20);
        }

        private void ApplyMixerSettings()
        {
            if (mainAudioMixer == null) return;

            if (enableCompression)
            {
                mainAudioMixer.SetFloat("MasterCompressorThreshold", -10f);
                mainAudioMixer.SetFloat("MasterCompressorRatio", 4f);
            }

            if (enableReverb)
            {
                mainAudioMixer.SetFloat("ReverbRoom", -1000f + (reverbAmount * 2000f));
                mainAudioMixer.SetFloat("ReverbDryLevel", 0f);
            }
        }

        public float[] GetBroadcastBuffer()
        {
            return _broadcastBuffer;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!broadcastMuted && enableDualRouting)
            {
                int dataLength = data.Length;
                if (_broadcastBuffer.Length != dataLength)
                {
                    _broadcastBuffer = new float[dataLength];
                }

                Array.Copy(data, _broadcastBuffer, dataLength);
                OnBroadcastBufferReady?.Invoke(_broadcastBuffer);
            }
        }

        public void EnableSpatialAudioForPlayer(ulong playerId, bool enable)
        {
            if (_playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair))
            {
                float spatialBlend = enable ? 1f : 0f;

                if (audioPair.MonitorSource != null)
                    audioPair.MonitorSource.spatialBlend = spatialBlend;

                if (audioPair.BroadcastSource != null)
                    audioPair.BroadcastSource.spatialBlend = spatialBlend;
            }
        }

        public void SetPlayerPosition(ulong playerId, Vector3 position)
        {
            if (_playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair))
            {
                Transform parent = audioPair.MonitorSource?.transform.parent;
                if (parent != null)
                {
                    parent.position = position;
                }
            }
        }

        public AudioSourcePair GetPlayerAudioSources(ulong playerId)
        {
            _playerAudioSources.TryGetValue(playerId, out AudioSourcePair audioPair);
            return audioPair;
        }

        private void OnDestroy()
        {
            foreach (var audioPair in _playerAudioSources.Values)
            {
                if (audioPair.MonitorSource != null)
                    Destroy(audioPair.MonitorSource.transform.parent.gameObject);
            }
            _playerAudioSources.Clear();
        }
    }
}