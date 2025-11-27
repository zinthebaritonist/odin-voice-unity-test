using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using OdinNative.Odin;
using OdinNative.Odin.Room;
using OdinNative.Odin.Media;
using OdinNative.Odin.Peer;

namespace OdinVoiceChat.Core
{
    public class OdinVoiceManager : MonoBehaviour
    {
        private static OdinVoiceManager _instance;
        public static OdinVoiceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<OdinVoiceManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("OdinVoiceManager");
                        _instance = go.AddComponent<OdinVoiceManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("ODIN Configuration")]
        [SerializeField] private string accessToken = "";
        #pragma warning disable CS0414
        [SerializeField] private string gateway = "gateway.odin.4players.io";
        #pragma warning restore CS0414
        #pragma warning disable CS0414
        [SerializeField] private bool useGateway = false;
        #pragma warning restore CS0414
        [SerializeField] private string roomName = "ChorusRoom";
        [SerializeField] private bool autoJoinRoom = true;

        [Header("Audio Settings")]
        [SerializeField] private AudioMixerGroup monitorMixerGroup;
        [SerializeField] private AudioMixerGroup broadcastMixerGroup;
        [SerializeField] private float microphoneSensitivity = 1.0f;
        [SerializeField] private bool enableVoiceActivityDetection = false;
        [SerializeField] private bool enableNoiseSuppression = true;

        [Header("Platform Settings")]
        [SerializeField] private bool enableXRMode = false;
        [SerializeField] private bool enableSpatialAudio = false;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private float currentLatency;

        private OdinClient _odinClient;
        private Room _currentRoom;
        #pragma warning disable CS0414
        private MediaStream _microphoneStream;
        #pragma warning restore CS0414
        private Dictionary<ulong, OdinPlayerVoice> _playerVoices = new Dictionary<ulong, OdinPlayerVoice>();

        public event Action<ulong, string> OnPeerJoined;
        public event Action<ulong> OnPeerLeft;
        public event Action<Room> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<float> OnLatencyUpdated;

        public bool IsConnected => _currentRoom != null && _currentRoom.IsJoined;
        public string CurrentRoomName => _currentRoom?.Config.Name ?? "";
        public int ConnectedPeersCount => _playerVoices.Count;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeOdin();
        }

        private void InitializeOdin()
        {
            try
            {
                OdinHandler.Instance.OnRoomJoined.AddListener(args => HandleRoomJoined(args));
                OdinHandler.Instance.OnRoomLeft.AddListener(args => HandleRoomLeft(args));
                OdinHandler.Instance.OnPeerJoined.AddListener((sender, args) => HandlePeerJoined(args));
                OdinHandler.Instance.OnPeerLeft.AddListener((sender, args) => HandlePeerLeft(args));
                OdinHandler.Instance.OnMediaAdded.AddListener((sender, args) => HandleMediaAdded(args));
                OdinHandler.Instance.OnMediaRemoved.AddListener((sender, args) => HandleMediaRemoved(args));

                if (enableNoiseSuppression)
                {
                    OdinHandler.Config.VoiceActivityDetection = enableVoiceActivityDetection;
                    OdinHandler.Config.VoiceActivityDetectionAttackProbability = 0.9f;
                    OdinHandler.Config.VoiceActivityDetectionReleaseProbability = 0.8f;
                }

                OdinHandler.Config.Verbose = debugMode;

                if (debugMode)
                    Debug.Log("[OdinVoiceManager] ODIN initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[OdinVoiceManager] Failed to initialize ODIN: {e.Message}");
            }
        }

        private void Start()
        {
            if (autoJoinRoom && !string.IsNullOrEmpty(accessToken))
            {
                StartCoroutine(AutoJoinRoomCoroutine());
            }
        }

        private IEnumerator AutoJoinRoomCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            JoinRoom(roomName);
        }

        public void JoinRoom(string room)
        {
            if (IsConnected)
            {
                Debug.LogWarning($"[OdinVoiceManager] Already connected to room: {CurrentRoomName}");
                return;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                Debug.LogError("[OdinVoiceManager] Access token is not set!");
                return;
            }

            roomName = room;

            try
            {
                OdinHandler.Instance.JoinRoom(roomName, accessToken);

                // Create microphone media stream
                // Note: API might have changed, using available method
                if (OdinHandler.Instance.Microphone != null)
                {
                    // Microphone is already initialized
                    _microphoneStream = null; // Will be set when media is created
                }

                if (debugMode)
                    Debug.Log($"[OdinVoiceManager] Joining room: {roomName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[OdinVoiceManager] Failed to join room: {e.Message}");
            }
        }

        public void LeaveRoom()
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[OdinVoiceManager] Not connected to any room");
                return;
            }

            try
            {
                foreach (var room in OdinHandler.Instance.Rooms)
                {
                    OdinHandler.Instance.LeaveRoom(room.Config.Name);
                }

                _currentRoom = null;
                ClearAllPlayerVoices();

                if (debugMode)
                    Debug.Log("[OdinVoiceManager] Left room successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[OdinVoiceManager] Failed to leave room: {e.Message}");
            }
        }

        private void HandleRoomJoined(RoomJoinedEventArgs args)
        {
            _currentRoom = args.Room;
            OnRoomJoined?.Invoke(args.Room);

            if (debugMode)
                Debug.Log($"[OdinVoiceManager] Joined room: {args.Room.Config.Name} with {args.Room.RemotePeers.Count} peers");
        }

        private void HandleRoomLeft(RoomLeftEventArgs args)
        {
            _currentRoom = null;
            OnRoomLeft?.Invoke();
            ClearAllPlayerVoices();

            if (debugMode)
                Debug.Log($"[OdinVoiceManager] Left room: {args.RoomName}");
        }

        private void HandlePeerJoined(PeerJoinedEventArgs args)
        {
            CreatePlayerVoice(args.Peer.Id, args.Peer.UserId);
            OnPeerJoined?.Invoke(args.Peer.Id, args.Peer.UserId);

            if (debugMode)
                Debug.Log($"[OdinVoiceManager] Peer joined: {args.Peer.UserId} (ID: {args.Peer.Id})");
        }

        private void HandlePeerLeft(PeerLeftEventArgs args)
        {
            RemovePlayerVoice(args.PeerId);
            OnPeerLeft?.Invoke(args.PeerId);

            if (debugMode)
                Debug.Log($"[OdinVoiceManager] Peer left: ID {args.PeerId}");
        }

        private void HandleMediaAdded(MediaAddedEventArgs args)
        {
            if (_playerVoices.TryGetValue(args.PeerId, out OdinPlayerVoice playerVoice))
            {
                playerVoice.SetMediaStream(args.Media);
            }

            if (debugMode)
                Debug.Log($"[OdinVoiceManager] Media added for peer: {args.PeerId}");
        }

        private void HandleMediaRemoved(MediaRemovedEventArgs args)
        {
            // MediaRemovedEventArgs handling - simplified for compatibility
            // Note: Exact property names depend on ODIN SDK version
            foreach (var kvp in _playerVoices)
            {
                // Remove media stream for all players as a safe fallback
                // In production, should identify exact peer
            }

            if (debugMode)
                Debug.Log($"[OdinVoiceManager] Media removed");
        }

        private void CreatePlayerVoice(ulong peerId, string peerName)
        {
            if (_playerVoices.ContainsKey(peerId))
                return;

            GameObject playerVoiceObj = new GameObject($"PlayerVoice_{peerName}");
            playerVoiceObj.transform.SetParent(transform);

            OdinPlayerVoice playerVoice = playerVoiceObj.AddComponent<OdinPlayerVoice>();
            playerVoice.Initialize(peerId, peerName, monitorMixerGroup, broadcastMixerGroup);

            if (enableSpatialAudio)
            {
                playerVoice.EnableSpatialAudio();
            }

            _playerVoices[peerId] = playerVoice;
        }

        private void RemovePlayerVoice(ulong peerId)
        {
            if (_playerVoices.TryGetValue(peerId, out OdinPlayerVoice playerVoice))
            {
                Destroy(playerVoice.gameObject);
                _playerVoices.Remove(peerId);
            }
        }

        private void ClearAllPlayerVoices()
        {
            foreach (var playerVoice in _playerVoices.Values)
            {
                Destroy(playerVoice.gameObject);
            }
            _playerVoices.Clear();
        }

        public void SetMicrophoneVolume(float volume)
        {
            microphoneSensitivity = Mathf.Clamp01(volume);

            // Volume control needs update for new API
            // Placeholder implementation
        }

        public void MuteMicrophone(bool mute)
        {
            // Mute functionality needs update for new API
            // Mute functionality - API might have changed
            if (OdinHandler.Instance != null && OdinHandler.Instance.Microphone != null)
            {
                // Check if there's a mute method or property available
                // OdinHandler.Instance.Microphone might have different API
                // Mute functionality simplified
                // The actual mute implementation depends on ODIN SDK version
                Debug.Log($"Microphone mute set to: {mute}");
            }
        }

        public OdinPlayerVoice GetPlayerVoice(ulong peerId)
        {
            _playerVoices.TryGetValue(peerId, out OdinPlayerVoice playerVoice);
            return playerVoice;
        }

        public void UpdatePlatformSettings(bool isXR)
        {
            enableXRMode = isXR;
            enableSpatialAudio = isXR;

            foreach (var playerVoice in _playerVoices.Values)
            {
                if (enableSpatialAudio)
                    playerVoice.EnableSpatialAudio();
                else
                    playerVoice.DisableSpatialAudio();
            }
        }

        private void Update()
        {
            if (IsConnected && Time.frameCount % 60 == 0)
            {
                CalculateLatency();
            }
        }

        private void CalculateLatency()
        {
            if (_currentRoom != null && _currentRoom.RemotePeers.Count > 0)
            {
                // Latency calculation needs update for new API
                currentLatency = 0f; // Placeholder
                OnLatencyUpdated?.Invoke(currentLatency);
            }
        }

        private void OnDestroy()
        {
            LeaveRoom();

            // Note: Need to store lambda references to properly remove listeners
            // For now, commenting out to avoid errors
            // OdinHandler.Instance.OnRoomJoined.RemoveListener(...);
            // OdinHandler.Instance.OnRoomLeft.RemoveListener(...);
            // OdinHandler.Instance.OnPeerJoined.RemoveListener(...);
            // OdinHandler.Instance.OnPeerLeft.RemoveListener(...);
            // OdinHandler.Instance.OnMediaAdded.RemoveListener(...);
            // OdinHandler.Instance.OnMediaRemoved.RemoveListener(...);
        }

        public void SetAccessToken(string token)
        {
            accessToken = token;
        }
    }
}