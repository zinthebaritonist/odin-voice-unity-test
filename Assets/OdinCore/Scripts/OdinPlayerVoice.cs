using System;
using UnityEngine;
using UnityEngine.Audio;
using OdinNative.Odin.Media;
using OdinNative.Odin.Room;

namespace OdinVoiceChat.Core
{
    public class OdinPlayerVoice : MonoBehaviour
    {
        [Header("Player Information")]
        [SerializeField] private ulong peerId;
        [SerializeField] private string playerName;
        [SerializeField] private bool isLocalPlayer = false;

        [Header("Audio Components")]
        [SerializeField] private AudioSource monitorAudioSource;
        [SerializeField] private AudioSource broadcastAudioSource;
        [SerializeField] private AudioMixerGroup monitorMixerGroup;
        [SerializeField] private AudioMixerGroup broadcastMixerGroup;

        [Header("Voice Settings")]
        [SerializeField] private float volume = 1.0f;
        [SerializeField] private bool isMuted = false;
        [SerializeField] private bool spatialAudioEnabled = false;
        [SerializeField] private float spatialBlend = 1.0f;
        [SerializeField] private float minDistance = 1.0f;
        [SerializeField] private float maxDistance = 10.0f;

        [Header("Voice Activity")]
        [SerializeField] private bool isSpeaking = false;
        [SerializeField] private float voiceActivityLevel = 0f;
        [SerializeField] private float smoothedActivityLevel = 0f;
        [SerializeField] private float activitySmoothTime = 0.1f;

        [Header("Audio Processing")]
        [SerializeField] private bool enableLowPassFilter = false;
        [SerializeField] private float lowPassCutoff = 5000f;
        [SerializeField] private bool enableEcho = false;
        [SerializeField] private float echoDelay = 0.5f;
        [SerializeField] private float echoDecay = 0.5f;

        private MediaStream _mediaStream;
        private PlaybackStream _playbackStream;
        private float[] _audioBuffer;
        private int _bufferSize = 960;
        private float _lastActivityTime;
        private AudioLowPassFilter _lowPassFilter;
        private AudioEchoFilter _echoFilter;
        private OdinAudioRouter.AudioSourcePair _audioSourcePair;

        public event Action<bool> OnSpeakingStateChanged;
        public event Action<float> OnVoiceActivityLevelChanged;

        public ulong PeerId => peerId;
        public string PlayerName => playerName;
        public bool IsSpeaking => isSpeaking;
        public float VoiceActivityLevel => smoothedActivityLevel;
        public bool IsMuted => isMuted;

        public void Initialize(ulong id, string name, AudioMixerGroup monitorGroup, AudioMixerGroup broadcastGroup)
        {
            peerId = id;
            playerName = name;
            monitorMixerGroup = monitorGroup;
            broadcastMixerGroup = broadcastGroup;

            SetupAudioSources();
            SetupAudioFilters();

            _audioBuffer = new float[_bufferSize * 2];

            Debug.Log($"[OdinPlayerVoice] Initialized for player {playerName} (ID: {peerId})");
        }

        private void SetupAudioSources()
        {
            _audioSourcePair = OdinAudioRouter.Instance.CreatePlayerAudioSources(peerId, playerName);

            if (_audioSourcePair != null)
            {
                monitorAudioSource = _audioSourcePair.MonitorSource;
                broadcastAudioSource = _audioSourcePair.BroadcastSource;

                if (monitorAudioSource != null)
                {
                    monitorAudioSource.outputAudioMixerGroup = monitorMixerGroup;
                    monitorAudioSource.volume = volume;
                    monitorAudioSource.spatialBlend = spatialAudioEnabled ? spatialBlend : 0f;
                    monitorAudioSource.minDistance = minDistance;
                    monitorAudioSource.maxDistance = maxDistance;
                    monitorAudioSource.rolloffMode = AudioRolloffMode.Linear;
                    monitorAudioSource.dopplerLevel = 0f;
                }

                if (broadcastAudioSource != null)
                {
                    broadcastAudioSource.outputAudioMixerGroup = broadcastMixerGroup;
                    broadcastAudioSource.volume = volume;
                    broadcastAudioSource.spatialBlend = 0f;
                }
            }
        }

        private void SetupAudioFilters()
        {
            if (monitorAudioSource != null)
            {
                _lowPassFilter = monitorAudioSource.gameObject.AddComponent<AudioLowPassFilter>();
                _lowPassFilter.cutoffFrequency = lowPassCutoff;
                _lowPassFilter.enabled = enableLowPassFilter;

                _echoFilter = monitorAudioSource.gameObject.AddComponent<AudioEchoFilter>();
                _echoFilter.delay = echoDelay * 1000f;
                _echoFilter.decayRatio = echoDecay;
                _echoFilter.enabled = enableEcho;
            }
        }

        public void SetMediaStream(MediaStream mediaStream)
        {
            _mediaStream = mediaStream;

            if (_mediaStream is PlaybackStream playback)
            {
                _playbackStream = playback;
                StartAudioPlayback();
            }

            Debug.Log($"[OdinPlayerVoice] Media stream set for {playerName}");
        }

        public void RemoveMediaStream()
        {
            StopAudioPlayback();
            _mediaStream = null;
            _playbackStream = null;

            Debug.Log($"[OdinPlayerVoice] Media stream removed for {playerName}");
        }

        private void StartAudioPlayback()
        {
            if (_playbackStream == null) return;

            if (monitorAudioSource != null && !monitorAudioSource.isPlaying)
            {
                monitorAudioSource.Play();
            }

            if (broadcastAudioSource != null && !broadcastAudioSource.isPlaying)
            {
                broadcastAudioSource.Play();
            }
        }

        private void StopAudioPlayback()
        {
            if (monitorAudioSource != null && monitorAudioSource.isPlaying)
            {
                monitorAudioSource.Stop();
            }

            if (broadcastAudioSource != null && broadcastAudioSource.isPlaying)
            {
                broadcastAudioSource.Stop();
            }

            isSpeaking = false;
            voiceActivityLevel = 0f;
            smoothedActivityLevel = 0f;
            OnSpeakingStateChanged?.Invoke(false);
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);

            if (monitorAudioSource != null)
                monitorAudioSource.volume = isMuted ? 0f : volume;

            if (broadcastAudioSource != null)
                broadcastAudioSource.volume = isMuted ? 0f : volume;

            OdinAudioRouter.Instance.SetPlayerVolume(peerId, volume);
        }

        public void SetMuted(bool mute)
        {
            isMuted = mute;

            if (monitorAudioSource != null)
                monitorAudioSource.mute = mute;

            if (broadcastAudioSource != null)
                broadcastAudioSource.mute = mute;

            OdinAudioRouter.Instance.MutePlayer(peerId, mute);
        }

        public void EnableSpatialAudio()
        {
            spatialAudioEnabled = true;

            if (monitorAudioSource != null)
            {
                monitorAudioSource.spatialBlend = spatialBlend;
            }

            OdinAudioRouter.Instance.EnableSpatialAudioForPlayer(peerId, true);
        }

        public void DisableSpatialAudio()
        {
            spatialAudioEnabled = false;

            if (monitorAudioSource != null)
            {
                monitorAudioSource.spatialBlend = 0f;
            }

            OdinAudioRouter.Instance.EnableSpatialAudioForPlayer(peerId, false);
        }

        public void SetSpatialPosition(Vector3 position)
        {
            if (spatialAudioEnabled)
            {
                transform.position = position;
                OdinRoomManager.Instance.UpdatePlayerPosition(peerId, position);
            }
        }

        public void SetLowPassFilter(bool enable, float cutoff = 5000f)
        {
            enableLowPassFilter = enable;
            lowPassCutoff = cutoff;

            if (_lowPassFilter != null)
            {
                _lowPassFilter.enabled = enable;
                _lowPassFilter.cutoffFrequency = cutoff;
            }
        }

        public void SetEchoFilter(bool enable, float delay = 0.5f, float decay = 0.5f)
        {
            enableEcho = enable;
            echoDelay = delay;
            echoDecay = decay;

            if (_echoFilter != null)
            {
                _echoFilter.enabled = enable;
                _echoFilter.delay = delay * 1000f;
                _echoFilter.decayRatio = decay;
            }
        }

        private void Update()
        {
            UpdateVoiceActivity();
            ProcessAudioStream();
        }

        private void UpdateVoiceActivity()
        {
            if (_playbackStream == null) return;

            float currentActivity = CalculateVoiceActivity();
            voiceActivityLevel = currentActivity;

            smoothedActivityLevel = Mathf.Lerp(smoothedActivityLevel, voiceActivityLevel, Time.deltaTime / activitySmoothTime);

            bool wasSpeaking = isSpeaking;
            isSpeaking = smoothedActivityLevel > 0.01f;

            if (wasSpeaking != isSpeaking)
            {
                OnSpeakingStateChanged?.Invoke(isSpeaking);

                if (isSpeaking)
                {
                    _lastActivityTime = Time.time;
                }
            }

            if (isSpeaking)
            {
                OnVoiceActivityLevelChanged?.Invoke(smoothedActivityLevel);
            }
        }

        private float CalculateVoiceActivity()
        {
            if (_audioBuffer == null || _audioBuffer.Length == 0)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < _audioBuffer.Length; i++)
            {
                sum += Mathf.Abs(_audioBuffer[i]);
            }

            return sum / _audioBuffer.Length;
        }

        private void ProcessAudioStream()
        {
            if (_playbackStream == null) return;

            try
            {
                // ReadData API changed in new ODIN SDK
                // This needs to be implemented based on new API
                uint readSamples = 0; // Placeholder

                if (readSamples > 0)
                {
                    ApplyAudioProcessing(_audioBuffer);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OdinPlayerVoice] Error processing audio stream: {e.Message}");
            }
        }

        private void ApplyAudioProcessing(float[] buffer)
        {
            // Apply any additional audio processing here
            // This could include noise suppression, gain control, etc.
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_playbackStream == null || isMuted)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0f;
                }
                return;
            }

            try
            {
                int samplesToRead = data.Length / channels;
                // ReadData API changed in new ODIN SDK
                uint readSamples = 0; // Placeholder

                if (readSamples < samplesToRead)
                {
                    for (int i = (int)readSamples * channels; i < data.Length; i++)
                    {
                        data[i] = 0f;
                    }
                }

                for (int i = 0; i < data.Length; i++)
                {
                    data[i] *= volume;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OdinPlayerVoice] Error in audio filter: {e.Message}");
            }
        }

        public float GetLatency()
        {
            if (_playbackStream != null)
            {
                // Latency API changed in new ODIN SDK
                return 0f; // Placeholder
            }
            return 0f;
        }

        private void OnDestroy()
        {
            StopAudioPlayback();

            if (OdinAudioRouter.Instance != null)
            {
                OdinAudioRouter.Instance.RemovePlayerAudioSources(peerId);
            }
        }

        public void SetLocalPlayer(bool isLocal)
        {
            isLocalPlayer = isLocal;

            if (isLocalPlayer && monitorAudioSource != null)
            {
                monitorAudioSource.volume = 0f;
            }
        }
    }
}