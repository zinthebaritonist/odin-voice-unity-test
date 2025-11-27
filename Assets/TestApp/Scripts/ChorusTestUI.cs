using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OdinVoiceChat.Core;
using OdinVoiceChat.XR;
using OdinVoiceChat;

namespace OdinVoiceChat.Test
{
    public class ChorusTestUI : MonoBehaviour
    {
        [Header("Main UI Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject chorusPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Login UI")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField accessTokenInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private TextMeshProUGUI connectionStatus;

        [Header("Lobby UI")]
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button joinRandomButton;
        [SerializeField] private Transform roomListContainer;
        [SerializeField] private GameObject roomItemPrefab;
        [SerializeField] private TextMeshProUGUI lobbyStatus;

        [Header("Chorus UI")]
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerItemPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button leaveRoomButton;
        [SerializeField] private TextMeshProUGUI latencyText;
        [SerializeField] private Slider micVolumeSlider;
        [SerializeField] private Toggle micMuteToggle;
        [SerializeField] private Image micActivityIndicator;

        [Header("Audio Controls")]
        [SerializeField] private Slider monitorVolumeSlider;
        [SerializeField] private Slider broadcastVolumeSlider;
        [SerializeField] private Toggle monitorMuteToggle;
        [SerializeField] private Toggle broadcastMuteToggle;
        [SerializeField] private TextMeshProUGUI monitorVolumeText;
        [SerializeField] private TextMeshProUGUI broadcastVolumeText;

        [Header("Platform UI")]
        [SerializeField] private TextMeshProUGUI platformText;
        [SerializeField] private Toggle xrModeToggle;
        [SerializeField] private Toggle pushToTalkToggle;
        [SerializeField] private Toggle spatialAudioToggle;
        [SerializeField] private Button recenterButton;

        [Header("Debug UI")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private Toggle debugToggle;

        private Dictionary<ulong, GameObject> _playerUIItems = new Dictionary<ulong, GameObject>();
        private List<GameObject> _roomUIItems = new List<GameObject>();
        private bool _isReady = false;
        private float _micActivityTimer = 0f;

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            UpdatePlatformInfo();

            ShowPanel(loginPanel);
        }

        private void InitializeUI()
        {
            connectButton.onClick.AddListener(OnConnectClicked);
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            joinRandomButton.onClick.AddListener(OnJoinRandomClicked);
            readyButton.onClick.AddListener(OnReadyClicked);
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

            micVolumeSlider.onValueChanged.AddListener(OnMicVolumeChanged);
            micMuteToggle.onValueChanged.AddListener(OnMicMuteChanged);

            monitorVolumeSlider.onValueChanged.AddListener(OnMonitorVolumeChanged);
            broadcastVolumeSlider.onValueChanged.AddListener(OnBroadcastVolumeChanged);
            monitorMuteToggle.onValueChanged.AddListener(OnMonitorMuteChanged);
            broadcastMuteToggle.onValueChanged.AddListener(OnBroadcastMuteChanged);

            if (xrModeToggle != null)
            {
                xrModeToggle.onValueChanged.AddListener(OnXRModeChanged);
                xrModeToggle.gameObject.SetActive(PlatformManager.Instance.IsXRPlatform());
            }

            if (pushToTalkToggle != null)
            {
                pushToTalkToggle.onValueChanged.AddListener(OnPushToTalkChanged);
            }

            if (spatialAudioToggle != null)
            {
                spatialAudioToggle.onValueChanged.AddListener(OnSpatialAudioChanged);
            }

            if (recenterButton != null)
            {
                recenterButton.onClick.AddListener(OnRecenterClicked);
                recenterButton.gameObject.SetActive(PlatformManager.Instance.IsXRPlatform());
            }

            if (debugToggle != null)
            {
                debugToggle.onValueChanged.AddListener(OnDebugToggleChanged);
            }

            micVolumeSlider.value = 1.0f;
            monitorVolumeSlider.value = 1.0f;
            broadcastVolumeSlider.value = 1.0f;
        }

        private void SetupEventListeners()
        {
            OdinVoiceManager.Instance.OnRoomJoined += OnRoomJoined;
            OdinVoiceManager.Instance.OnRoomLeft += OnRoomLeft;
            OdinVoiceManager.Instance.OnPeerJoined += OnPeerJoined;
            OdinVoiceManager.Instance.OnPeerLeft += OnPeerLeft;
            OdinVoiceManager.Instance.OnLatencyUpdated += OnLatencyUpdated;

            OdinRoomManager.Instance.OnRoomCreated += OnRoomCreated;
            OdinRoomManager.Instance.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            OdinRoomManager.Instance.OnPlayerLeftRoom += OnPlayerLeftRoom;
        }

        private void UpdatePlatformInfo()
        {
            if (platformText != null)
            {
                platformText.text = $"Platform: {PlatformManager.Instance.GetCurrentPlatform()}";
            }
        }

        private void ShowPanel(GameObject panel)
        {
            loginPanel.SetActive(panel == loginPanel);
            lobbyPanel.SetActive(panel == lobbyPanel);
            chorusPanel.SetActive(panel == chorusPanel);
            settingsPanel.SetActive(panel == settingsPanel);
        }

        private void OnConnectClicked()
        {
            string playerName = playerNameInput.text;
            string accessToken = accessTokenInput.text;

            if (string.IsNullOrEmpty(playerName))
            {
                connectionStatus.text = "Please enter a player name";
                return;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                connectionStatus.text = "Please enter an access token";
                return;
            }

            connectionStatus.text = "Connecting...";
            connectButton.interactable = false;

            OdinVoiceManager.Instance.SetAccessToken(accessToken);

            ShowPanel(lobbyPanel);
            UpdateRoomList();
        }

        private void OnCreateRoomClicked()
        {
            string roomName = roomNameInput.text;
            OdinRoomManager.Instance.CreateRoom(roomName);
        }

        private void OnJoinRoomClicked()
        {
            string roomName = roomNameInput.text;
            if (!string.IsNullOrEmpty(roomName))
            {
                OdinRoomManager.Instance.JoinRoom(roomName);
            }
        }

        private void OnJoinRandomClicked()
        {
            OdinRoomManager.Instance.JoinRandomRoom();
        }

        private void OnRoomJoined(OdinNative.Odin.Room.Room room)
        {
            ShowPanel(chorusPanel);
            roomNameText.text = $"Room: {room.Config.Name}";
            UpdatePlayerList();
        }

        private void OnRoomCreated(string roomName)
        {
            lobbyStatus.text = $"Created room: {roomName}";
        }

        private void OnRoomLeft()
        {
            ShowPanel(lobbyPanel);
            ClearPlayerList();
            UpdateRoomList();
        }

        private void OnPeerJoined(ulong peerId, string peerName)
        {
            UpdatePlayerList();
        }

        private void OnPeerLeft(ulong peerId)
        {
            if (_playerUIItems.TryGetValue(peerId, out GameObject item))
            {
                Destroy(item);
                _playerUIItems.Remove(peerId);
            }
            UpdatePlayerList();
        }

        private void OnPlayerJoinedRoom(OdinRoomManager.PlayerInfo player)
        {
            CreatePlayerUIItem(player);
        }

        private void OnPlayerLeftRoom(ulong peerId)
        {
            RemovePlayerUIItem(peerId);
        }

        private void UpdateRoomList()
        {
            foreach (var item in _roomUIItems)
            {
                Destroy(item);
            }
            _roomUIItems.Clear();

            var rooms = OdinRoomManager.Instance.GetAvailableRooms();
            foreach (var roomId in rooms)
            {
                CreateRoomUIItem(roomId);
            }
        }

        private void CreateRoomUIItem(string roomId)
        {
            if (roomItemPrefab == null || roomListContainer == null) return;

            GameObject item = Instantiate(roomItemPrefab, roomListContainer);
            _roomUIItems.Add(item);

            TextMeshProUGUI roomText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (roomText != null)
            {
                var roomData = OdinRoomManager.Instance.GetRoomData(roomId);
                roomText.text = $"{roomId} ({roomData?.PlayerCount ?? 0}/4)";
            }

            Button joinButton = item.GetComponentInChildren<Button>();
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(() => OdinRoomManager.Instance.JoinRoom(roomId));
            }
        }

        private void UpdatePlayerList()
        {
            ClearPlayerList();

            var players = OdinRoomManager.Instance.GetConnectedPlayers();
            playerCountText.text = $"Players: {players.Count + 1}/4";

            foreach (var player in players)
            {
                CreatePlayerUIItem(player);
            }
        }

        private void CreatePlayerUIItem(OdinRoomManager.PlayerInfo player)
        {
            if (playerItemPrefab == null || playerListContainer == null) return;

            GameObject item = Instantiate(playerItemPrefab, playerListContainer);
            _playerUIItems[player.PeerId] = item;

            TextMeshProUGUI nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = player.PlayerName;
            }

            TextMeshProUGUI latencyText = item.transform.Find("LatencyText")?.GetComponent<TextMeshProUGUI>();
            if (latencyText != null)
            {
                latencyText.text = $"{player.Latency:F1}ms";
            }

            Image readyIndicator = item.transform.Find("ReadyIndicator")?.GetComponent<Image>();
            if (readyIndicator != null)
            {
                readyIndicator.color = player.IsReady ? Color.green : Color.gray;
            }

            Image speakingIndicator = item.transform.Find("SpeakingIndicator")?.GetComponent<Image>();
            if (speakingIndicator != null)
            {
                var playerVoice = OdinVoiceManager.Instance.GetPlayerVoice(player.PeerId);
                if (playerVoice != null)
                {
                    playerVoice.OnSpeakingStateChanged += (isSpeaking) =>
                    {
                        speakingIndicator.color = isSpeaking ? Color.green : Color.gray;
                    };
                }
            }

            Slider volumeSlider = item.transform.Find("VolumeSlider")?.GetComponent<Slider>();
            if (volumeSlider != null)
            {
                volumeSlider.value = 1.0f;
                volumeSlider.onValueChanged.AddListener((value) =>
                {
                    var voice = OdinVoiceManager.Instance.GetPlayerVoice(player.PeerId);
                    voice?.SetVolume(value);
                });
            }

            Toggle muteToggle = item.transform.Find("MuteToggle")?.GetComponent<Toggle>();
            if (muteToggle != null)
            {
                muteToggle.onValueChanged.AddListener((muted) =>
                {
                    var voice = OdinVoiceManager.Instance.GetPlayerVoice(player.PeerId);
                    voice?.SetMuted(muted);
                });
            }
        }

        private void RemovePlayerUIItem(ulong peerId)
        {
            if (_playerUIItems.TryGetValue(peerId, out GameObject item))
            {
                Destroy(item);
                _playerUIItems.Remove(peerId);
            }
        }

        private void ClearPlayerList()
        {
            foreach (var item in _playerUIItems.Values)
            {
                Destroy(item);
            }
            _playerUIItems.Clear();
        }

        private void OnReadyClicked()
        {
            _isReady = !_isReady;
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = _isReady ? "Not Ready" : "Ready";

            // This would normally communicate ready state to server
        }

        private void OnLeaveRoomClicked()
        {
            OdinRoomManager.Instance.LeaveCurrentRoom();
        }

        private void OnMicVolumeChanged(float value)
        {
            OdinVoiceManager.Instance.SetMicrophoneVolume(value);
        }

        private void OnMicMuteChanged(bool muted)
        {
            OdinVoiceManager.Instance.MuteMicrophone(muted);
        }

        private void OnMonitorVolumeChanged(float value)
        {
            OdinAudioRouter.Instance.SetMonitorVolume(value);
            monitorVolumeText.text = $"{(value * 100):F0}%";
        }

        private void OnBroadcastVolumeChanged(float value)
        {
            OdinAudioRouter.Instance.SetBroadcastVolume(value);
            broadcastVolumeText.text = $"{(value * 100):F0}%";
        }

        private void OnMonitorMuteChanged(bool muted)
        {
            OdinAudioRouter.Instance.MuteMonitorBus(muted);
        }

        private void OnBroadcastMuteChanged(bool muted)
        {
            OdinAudioRouter.Instance.MuteBroadcastBus(muted);
        }

        private void OnXRModeChanged(bool enabled)
        {
            var xrController = GameObject.FindFirstObjectByType<XRVoiceController>();
            if (xrController != null)
            {
                xrController.SetXRMode(enabled);
            }
        }

        private void OnPushToTalkChanged(bool enabled)
        {
            var xrController = GameObject.FindFirstObjectByType<XRVoiceController>();
            if (xrController != null)
            {
                xrController.TogglePushToTalk();
            }
        }

        private void OnSpatialAudioChanged(bool enabled)
        {
            // Toggle spatial audio for all players
            var players = OdinRoomManager.Instance.GetConnectedPlayers();
            foreach (var player in players)
            {
                var voice = OdinVoiceManager.Instance.GetPlayerVoice(player.PeerId);
                if (voice != null)
                {
                    if (enabled)
                        voice.EnableSpatialAudio();
                    else
                        voice.DisableSpatialAudio();
                }
            }
        }

        private void OnRecenterClicked()
        {
            var xrController = GameObject.FindFirstObjectByType<XRVoiceController>();
            xrController?.RecenterView();
        }

        private void OnLatencyUpdated(float latency)
        {
            if (latencyText != null)
            {
                latencyText.text = $"Latency: {latency:F1}ms";
            }
        }

        private void OnDebugToggleChanged(bool enabled)
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(enabled);
            }
        }

        private void Update()
        {
            UpdateMicActivityIndicator();
            UpdateDebugInfo();
        }

        private void UpdateMicActivityIndicator()
        {
            if (micActivityIndicator == null) return;

            _micActivityTimer -= Time.deltaTime;
            if (_micActivityTimer <= 0f)
            {
                _micActivityTimer = 0.1f;

                float activity = UnityEngine.Random.Range(0f, 1f);
                Color color = Color.Lerp(Color.gray, Color.green, activity);
                micActivityIndicator.color = color;
            }
        }

        private void UpdateDebugInfo()
        {
            if (debugText == null || !debugPanel.activeSelf) return;

            string info = $"Platform: {PlatformManager.Instance.GetCurrentPlatform()}\n";
            info += $"XR Mode: {PlatformManager.Instance.IsXRPlatform()}\n";
            info += $"FPS: {1f / Time.smoothDeltaTime:F0}\n";
            info += $"Connected: {OdinVoiceManager.Instance.IsConnected}\n";
            info += $"Room: {OdinVoiceManager.Instance.CurrentRoomName}\n";
            info += $"Players: {OdinVoiceManager.Instance.ConnectedPeersCount}\n";

            debugText.text = info;
        }

        private void OnDestroy()
        {
            if (OdinVoiceManager.Instance != null)
            {
                OdinVoiceManager.Instance.OnRoomJoined -= OnRoomJoined;
                OdinVoiceManager.Instance.OnRoomLeft -= OnRoomLeft;
                OdinVoiceManager.Instance.OnPeerJoined -= OnPeerJoined;
                OdinVoiceManager.Instance.OnPeerLeft -= OnPeerLeft;
                OdinVoiceManager.Instance.OnLatencyUpdated -= OnLatencyUpdated;
            }

            if (OdinRoomManager.Instance != null)
            {
                OdinRoomManager.Instance.OnRoomCreated -= OnRoomCreated;
                OdinRoomManager.Instance.OnPlayerJoinedRoom -= OnPlayerJoinedRoom;
                OdinRoomManager.Instance.OnPlayerLeftRoom -= OnPlayerLeftRoom;
            }
        }
    }
}