using System;
using UnityEngine;
using UnityEngine.InputSystem;
using OdinVoiceChat.Core;
using OdinVoiceChat.XR;

namespace OdinVoiceChat.Test
{
    public class TestPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private bool enableMovement = true;

        [Header("Player Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private AudioListener audioListener;

        [Header("Voice Visualization")]
        [SerializeField] private GameObject voiceIndicator;
        [SerializeField] private Renderer voiceIndicatorRenderer;
        [SerializeField] private Color speakingColor = Color.green;
        [SerializeField] private Color mutedColor = Color.red;
        [SerializeField] private Color idleColor = Color.gray;

        [Header("XR Support")]
        [SerializeField] private bool isXRPlayer = false;
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;
        [SerializeField] private TextMesh debugText;

        private Vector3 _velocity;
        private float _xRotation = 0f;
        private bool _isGrounded;
        private bool _isSpeaking = false;
        private bool _isMuted = false;
        private OdinPlayerVoice _playerVoice;
        private XRVoiceController _xrController;

        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _muteAction;

        private void Awake()
        {
            SetupComponents();
            SetupInput();
            CheckXRMode();
        }

        private void SetupComponents()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (characterController == null)
                characterController = gameObject.AddComponent<CharacterController>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                playerCamera = cameraObj.AddComponent<Camera>();
                cameraTransform = playerCamera.transform;
            }
            else
            {
                cameraTransform = playerCamera.transform;
            }

            if (audioListener == null)
                audioListener = playerCamera.GetComponent<AudioListener>();

            if (audioListener == null)
                audioListener = playerCamera.gameObject.AddComponent<AudioListener>();

            SetupVoiceIndicator();
        }

        private void SetupVoiceIndicator()
        {
            if (voiceIndicator == null)
            {
                voiceIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                voiceIndicator.transform.SetParent(transform);
                voiceIndicator.transform.localPosition = new Vector3(0, 2.2f, 0);
                voiceIndicator.transform.localScale = Vector3.one * 0.3f;

                Collider collider = voiceIndicator.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
            }

            voiceIndicatorRenderer = voiceIndicator.GetComponent<Renderer>();
            if (voiceIndicatorRenderer != null)
            {
                voiceIndicatorRenderer.material = new Material(Shader.Find("Standard"));
                voiceIndicatorRenderer.material.color = idleColor;
            }
        }

        private void SetupInput()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
                _playerInput = gameObject.AddComponent<PlayerInput>();

            var actionMap = _playerInput.currentActionMap;
            if (actionMap != null)
            {
                _moveAction = actionMap.FindAction("Move");
                _lookAction = actionMap.FindAction("Look");
                _jumpAction = actionMap.FindAction("Jump");
                _muteAction = actionMap.FindAction("Mute");
            }
        }

        private void CheckXRMode()
        {
            isXRPlayer = PlatformManager.Instance != null && PlatformManager.Instance.IsXRPlatform();

            if (isXRPlayer)
            {
                _xrController = GameObject.FindObjectOfType<XRVoiceController>();
                if (_xrController == null)
                {
                    GameObject xrControllerObj = new GameObject("XRVoiceController");
                    _xrController = xrControllerObj.AddComponent<XRVoiceController>();
                }

                enableMovement = false;

                if (xrOrigin == null)
                {
                    GameObject xrOriginObj = new GameObject("XROrigin");
                    xrOriginObj.transform.SetParent(transform);
                    xrOrigin = xrOriginObj.transform;
                }
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ConnectToVoiceChat();
        }

        private void ConnectToVoiceChat()
        {
            _playerVoice = GetComponent<OdinPlayerVoice>();
            if (_playerVoice != null)
            {
                _playerVoice.OnSpeakingStateChanged += OnSpeakingStateChanged;
                _playerVoice.OnVoiceActivityLevelChanged += OnVoiceActivityLevelChanged;
            }
        }

        private void Update()
        {
            if (!isXRPlayer && enableMovement)
            {
                HandleMovement();
                HandleLook();
            }

            HandleVoiceControls();
            UpdateVoiceIndicator();
            UpdateDebugInfo();
        }

        private void HandleMovement()
        {
            _isGrounded = characterController.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            Vector2 moveInput = Vector2.zero;
            if (_moveAction != null)
                moveInput = _moveAction.ReadValue<Vector2>();

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            characterController.Move(move * moveSpeed * Time.deltaTime);

            if (_jumpAction != null && _jumpAction.triggered && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            }

            _velocity.y += Physics.gravity.y * Time.deltaTime;
            characterController.Move(_velocity * Time.deltaTime);

            if (_playerVoice != null && PlatformManager.Instance != null)
            {
                _playerVoice.SetSpatialPosition(transform.position);
            }
        }

        private void HandleLook()
        {
            if (!Input.GetKey(KeyCode.LeftAlt))
            {
                Vector2 lookInput = Vector2.zero;
                if (_lookAction != null)
                    lookInput = _lookAction.ReadValue<Vector2>();

                float mouseX = lookInput.x * lookSpeed;
                float mouseY = lookInput.y * lookSpeed;

                _xRotation -= mouseY;
                _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

                cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
                transform.Rotate(Vector3.up * mouseX);
            }
        }

        private void HandleVoiceControls()
        {
            if (_muteAction != null && _muteAction.triggered)
            {
                ToggleMute();
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleMute();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetMicrophoneVolume(0.5f);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetMicrophoneVolume(1.0f);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetMicrophoneVolume(1.5f);
            }
        }

        private void ToggleMute()
        {
            _isMuted = !_isMuted;
            OdinVoiceManager.Instance.MuteMicrophone(_isMuted);

            if (_playerVoice != null)
            {
                _playerVoice.SetMuted(_isMuted);
            }

            Debug.Log($"[TestPlayerController] Microphone {(_isMuted ? "muted" : "unmuted")}");
        }

        private void SetMicrophoneVolume(float volume)
        {
            OdinVoiceManager.Instance.SetMicrophoneVolume(volume);

            if (_playerVoice != null)
            {
                _playerVoice.SetVolume(volume);
            }

            Debug.Log($"[TestPlayerController] Microphone volume set to {volume}");
        }

        private void OnSpeakingStateChanged(bool speaking)
        {
            _isSpeaking = speaking;
        }

        private void OnVoiceActivityLevelChanged(float level)
        {
            // Could be used for voice activity visualization
        }

        private void UpdateVoiceIndicator()
        {
            if (voiceIndicatorRenderer == null) return;

            if (_isMuted)
            {
                voiceIndicatorRenderer.material.color = mutedColor;
                voiceIndicator.SetActive(true);
            }
            else if (_isSpeaking)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                voiceIndicatorRenderer.material.color = Color.Lerp(idleColor, speakingColor, pulse);
                voiceIndicator.SetActive(true);
            }
            else
            {
                voiceIndicatorRenderer.material.color = idleColor;
                voiceIndicator.SetActive(false);
            }
        }

        private void UpdateDebugInfo()
        {
            if (debugText == null) return;

            string info = $"Player: {gameObject.name}\n";
            info += $"Position: {transform.position}\n";
            info += $"Speaking: {_isSpeaking}\n";
            info += $"Muted: {_isMuted}\n";

            if (_playerVoice != null)
            {
                info += $"Latency: {_playerVoice.GetLatency():F1}ms\n";
            }

            debugText.text = info;
        }

        public void TeleportTo(Vector3 position)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = position;
                characterController.enabled = true;
            }
            else
            {
                transform.position = position;
            }
        }

        public void SetAsLocalPlayer()
        {
            if (_playerVoice != null)
            {
                _playerVoice.SetLocalPlayer(true);
            }

            if (playerCamera != null)
            {
                playerCamera.enabled = true;
            }

            if (audioListener != null)
            {
                audioListener.enabled = true;
            }

            enableMovement = true;
        }

        public void SetAsRemotePlayer()
        {
            if (_playerVoice != null)
            {
                _playerVoice.SetLocalPlayer(false);
            }

            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }

            if (audioListener != null)
            {
                audioListener.enabled = false;
            }

            enableMovement = false;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = _isSpeaking ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.2f, 0.15f);

            if (_playerVoice != null && _playerVoice.IsMuted)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up * 2f, transform.position + Vector3.up * 2.4f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }

        private void OnDestroy()
        {
            if (_playerVoice != null)
            {
                _playerVoice.OnSpeakingStateChanged -= OnSpeakingStateChanged;
                _playerVoice.OnVoiceActivityLevelChanged -= OnVoiceActivityLevelChanged;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}