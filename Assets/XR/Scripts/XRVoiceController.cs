using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using OdinVoiceChat.Core;

namespace OdinVoiceChat.XR
{
    public class XRVoiceController : MonoBehaviour
    {
        [Header("XR Configuration")]
        #pragma warning disable CS0414
        [SerializeField] private bool autoDetectXRDevice = true;
        #pragma warning restore CS0414
        #pragma warning disable CS0414
        [SerializeField] private XRNode primaryControllerNode = XRNode.RightHand;
        #pragma warning restore CS0414
        #pragma warning disable CS0414
        [SerializeField] private XRNode secondaryControllerNode = XRNode.LeftHand;
        #pragma warning restore CS0414

        [Header("Voice Controls")]
        [SerializeField] private bool pushToTalk = false;
        [SerializeField] private InputActionProperty pushToTalkAction;
        [SerializeField] private float triggerThreshold = 0.5f;
        [SerializeField] private bool spatialVoiceInXR = true;

        [Header("Quest Optimization")]
        [SerializeField] private bool enableQuestOptimization = true;
        #pragma warning disable CS0414
        [SerializeField] private bool useFoveatedRendering = true;
        #pragma warning restore CS0414
        [SerializeField] private int targetFrameRate = 72;

        [Header("Hand Tracking")]
        [SerializeField] private bool enableHandTracking = false;
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        [Header("Voice Indicators")]
        [SerializeField] private GameObject voiceIndicatorPrefab;
        [SerializeField] private float indicatorHeight = 2.0f;
        [SerializeField] private Color speakingColor = Color.green;
        [SerializeField] private Color mutedColor = Color.red;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private Transform debugCanvas;

        private XROrigin _xrOrigin;
        private UnityEngine.XR.InputDevice _primaryController;
        private UnityEngine.XR.InputDevice _secondaryController;
        private bool _isInXR = false;
        private bool _isMicrophoneMuted = false;
        private Dictionary<ulong, GameObject> _voiceIndicators = new Dictionary<ulong, GameObject>();
        private Camera _xrCamera;
        private AudioListener _xrAudioListener;

        public event Action<bool> OnXRModeChanged;
        public event Action<bool> OnPushToTalkStateChanged;

        private void Awake()
        {
            DetectXREnvironment();
            SetupXRComponents();

            if (_isInXR)
            {
                ConfigureForXR();
            }
        }

        private void DetectXREnvironment()
        {
            List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displaySubsystems);

            _isInXR = displaySubsystems.Count > 0 && displaySubsystems[0].running;

#if UNITY_ANDROID && !UNITY_EDITOR
            _isInXR = true;
            enableQuestOptimization = true;
#endif

            Debug.Log($"[XRVoiceController] XR Mode: {_isInXR}, Quest Optimization: {enableQuestOptimization}");
        }

        private void SetupXRComponents()
        {
            _xrOrigin = GameObject.FindFirstObjectByType<XROrigin>();
            if (_xrOrigin == null && _isInXR)
            {
                GameObject rigObject = new GameObject("XR Origin");
                _xrOrigin = rigObject.AddComponent<XROrigin>();
            }

            if (_xrOrigin != null)
            {
                _xrCamera = _xrOrigin.GetComponentInChildren<Camera>();
                if (_xrCamera == null)
                {
                    GameObject cameraObject = new GameObject("XR Camera");
                    cameraObject.transform.SetParent(_xrOrigin.transform);
                    _xrCamera = cameraObject.AddComponent<Camera>();
                    _xrCamera.nearClipPlane = 0.01f;
                }

                _xrAudioListener = _xrCamera.GetComponent<AudioListener>();
                if (_xrAudioListener == null)
                {
                    _xrAudioListener = _xrCamera.gameObject.AddComponent<AudioListener>();
                }
            }
        }

        private void ConfigureForXR()
        {
            if (enableQuestOptimization)
            {
                ApplyQuestOptimizations();
            }

            OdinVoiceManager.Instance.UpdatePlatformSettings(true);

            if (spatialVoiceInXR)
            {
                EnableSpatialVoice();
            }

            SetupControllers();
            OnXRModeChanged?.Invoke(true);
        }

        private void ApplyQuestOptimizations()
        {
            Application.targetFrameRate = targetFrameRate;

            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowCascades = 1;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.antiAliasing = 2;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (useFoveatedRendering)
            {
                OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.Medium;
                OVRManager.useDynamicFixedFoveatedRendering = true;
            }

            OVRManager.cpuLevel = 3;
            OVRManager.gpuLevel = 3;
#endif

            Debug.Log("[XRVoiceController] Quest optimizations applied");
        }

        private void SetupControllers()
        {
            List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevices(devices);

            foreach (var device in devices)
            {
                if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
                {
                    _primaryController = device;
                }
                else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                {
                    _secondaryController = device;
                }
            }

            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }

        private void OnDeviceConnected(UnityEngine.XR.InputDevice device)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                _primaryController = device;
            }
            else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            {
                _secondaryController = device;
            }
        }

        private void OnDeviceDisconnected(UnityEngine.XR.InputDevice device)
        {
            if (_primaryController.serialNumber == device.serialNumber)
            {
                _primaryController = default;
            }
            else if (_secondaryController.serialNumber == device.serialNumber)
            {
                _secondaryController = default;
            }
        }

        private void EnableSpatialVoice()
        {
            var players = OdinRoomManager.Instance.GetConnectedPlayers();
            foreach (var player in players)
            {
                var playerVoice = OdinVoiceManager.Instance.GetPlayerVoice(player.PeerId);
                if (playerVoice != null)
                {
                    playerVoice.EnableSpatialAudio();
                }
            }

            Debug.Log("[XRVoiceController] Spatial voice enabled for XR");
        }

        private void Update()
        {
            if (!_isInXR) return;

            HandlePushToTalk();
            UpdatePlayerPosition();
            UpdateVoiceIndicators();

            if (showDebugInfo)
            {
                UpdateDebugInfo();
            }
        }

        private void HandlePushToTalk()
        {
            if (!pushToTalk) return;

            bool wasPressed = !_isMicrophoneMuted;
            bool isPressed = false;

            if (pushToTalkAction.action != null)
            {
                isPressed = pushToTalkAction.action.ReadValue<float>() > triggerThreshold;
            }

            if (isPressed != wasPressed)
            {
                _isMicrophoneMuted = !isPressed;
                OdinVoiceManager.Instance.MuteMicrophone(_isMicrophoneMuted);
                OnPushToTalkStateChanged?.Invoke(!_isMicrophoneMuted);

                if (!_isMicrophoneMuted)
                {
                    ProvideHapticFeedback(0.1f, 0.1f);
                }
            }
        }

        private void UpdatePlayerPosition()
        {
            if (_xrCamera != null)
            {
                Vector3 position = _xrCamera.transform.position;
                Quaternion rotation = _xrCamera.transform.rotation;

                // Update local player position for spatial audio
                // This would be sent to other players through ODIN
            }
        }

        private void UpdateVoiceIndicators()
        {
            var players = OdinRoomManager.Instance.GetConnectedPlayers();

            foreach (var player in players)
            {
                UpdatePlayerIndicator(player.PeerId);
            }
        }

        private void UpdatePlayerIndicator(ulong peerId)
        {
            var playerVoice = OdinVoiceManager.Instance.GetPlayerVoice(peerId);
            if (playerVoice == null) return;

            if (!_voiceIndicators.TryGetValue(peerId, out GameObject indicator))
            {
                if (voiceIndicatorPrefab != null)
                {
                    indicator = Instantiate(voiceIndicatorPrefab);
                    _voiceIndicators[peerId] = indicator;
                }
            }

            if (indicator != null)
            {
                Vector3 position = playerVoice.transform.position;
                position.y += indicatorHeight;
                indicator.transform.position = position;

                Renderer renderer = indicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (playerVoice.IsMuted)
                    {
                        renderer.material.color = mutedColor;
                    }
                    else if (playerVoice.IsSpeaking)
                    {
                        float intensity = playerVoice.VoiceActivityLevel;
                        Color color = Color.Lerp(Color.white, speakingColor, intensity);
                        renderer.material.color = color;
                    }
                    else
                    {
                        renderer.material.color = Color.white;
                    }
                }

                indicator.SetActive(playerVoice.IsSpeaking || playerVoice.IsMuted);
            }
        }

        public void TogglePushToTalk()
        {
            pushToTalk = !pushToTalk;

            if (!pushToTalk)
            {
                _isMicrophoneMuted = false;
                OdinVoiceManager.Instance.MuteMicrophone(false);
            }
        }

        public void SetHandTrackingMode(bool enabled)
        {
            enableHandTracking = enabled;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (enabled)
            {
                OVRManager.handTrackingSupport = OVRManager.HandTrackingSupport.ControllersAndHands;
                OVRManager.handTrackingFrequency = OVRManager.HandTrackingFrequency.MAX;
            }
            else
            {
                OVRManager.handTrackingSupport = OVRManager.HandTrackingSupport.ControllersOnly;
            }
#endif
        }

        private void ProvideHapticFeedback(float amplitude, float duration)
        {
            if (_primaryController.isValid)
            {
                HapticCapabilities capabilities;
                if (_primaryController.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        _primaryController.SendHapticImpulse(0, amplitude, duration);
                    }
                }
            }
        }

        public void RecenterView()
        {
            if (_isInXR)
            {
                List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
                SubsystemManager.GetSubsystems(inputSubsystems);

                foreach (var subsystem in inputSubsystems)
                {
                    subsystem.TryRecenter();
                }
            }
        }

        private void UpdateDebugInfo()
        {
            if (debugCanvas == null) return;

            // Update debug canvas with XR info
            // Controller states, tracking status, etc.
        }

        public bool IsInXRMode()
        {
            return _isInXR;
        }

        public void SetXRMode(bool enabled)
        {
            if (enabled != _isInXR)
            {
                _isInXR = enabled;

                if (_isInXR)
                {
                    ConfigureForXR();
                }
                else
                {
                    DisableXRMode();
                }
            }
        }

        private void DisableXRMode()
        {
            OdinVoiceManager.Instance.UpdatePlatformSettings(false);

            foreach (var indicator in _voiceIndicators.Values)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
            _voiceIndicators.Clear();

            OnXRModeChanged?.Invoke(false);
        }

        private void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;

            foreach (var indicator in _voiceIndicators.Values)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
        }
    }
}