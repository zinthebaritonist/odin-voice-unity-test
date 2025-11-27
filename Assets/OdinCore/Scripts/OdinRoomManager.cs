using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OdinNative.Odin.Room;
using OdinNative.Odin.Peer;

namespace OdinVoiceChat.Core
{
    public class OdinRoomManager : MonoBehaviour
    {
        private static OdinRoomManager _instance;
        public static OdinRoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<OdinRoomManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("OdinRoomManager");
                        _instance = go.AddComponent<OdinRoomManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Room Configuration")]
        [SerializeField] private int maxPlayersPerRoom = 4;
        [SerializeField] private string roomPrefix = "Chorus_";
        [SerializeField] private bool autoCreateRoom = true;
        [SerializeField] private bool allowDynamicRoomCreation = true;

        [Header("Sync Settings")]
        [SerializeField] private float syncInterval = 0.05f;
        [SerializeField] private bool enableTimeSynchronization = true;
        [SerializeField] private float maxLatencyCompensation = 100f;

        [Header("Room Status")]
        [SerializeField] private string currentRoomId;
        [SerializeField] private int currentPlayerCount;
        [SerializeField] private List<PlayerInfo> connectedPlayers = new List<PlayerInfo>();
        [SerializeField] private float averageLatency;
        [SerializeField] private bool isHost = false;

        private Dictionary<string, RoomData> _availableRooms = new Dictionary<string, RoomData>();
        private float _lastSyncTime;
        private float _sessionStartTime;
        private Dictionary<ulong, float> _playerLatencies = new Dictionary<ulong, float>();

        public event Action<string> OnRoomCreated;
        public event Action<string> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<PlayerInfo> OnPlayerJoinedRoom;
        public event Action<ulong> OnPlayerLeftRoom;
        public event Action<float> OnSyncTick;

        [Serializable]
        public class PlayerInfo
        {
            public ulong PeerId;
            public string PlayerName;
            public bool IsReady;
            public float Latency;
            public Vector3 Position;
            public bool IsHost;
            public DateTime JoinTime;
        }

        public class RoomData
        {
            public string RoomId;
            public int PlayerCount;
            public bool IsFull;
            public DateTime CreatedTime;
            public List<PlayerInfo> Players = new List<PlayerInfo>();
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
            OdinVoiceManager.Instance.OnRoomJoined += HandleRoomJoined;
            OdinVoiceManager.Instance.OnRoomLeft += HandleRoomLeft;
            OdinVoiceManager.Instance.OnPeerJoined += HandlePeerJoined;
            OdinVoiceManager.Instance.OnPeerLeft += HandlePeerLeft;
            OdinVoiceManager.Instance.OnLatencyUpdated += HandleLatencyUpdate;

            _sessionStartTime = Time.time;

            Debug.Log("[OdinRoomManager] Room manager initialized");
        }

        public void CreateRoom(string roomName = null)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = GenerateRoomName();
            }

            currentRoomId = roomName;
            isHost = true;

            OdinVoiceManager.Instance.JoinRoom(roomName);

            RoomData newRoom = new RoomData
            {
                RoomId = roomName,
                PlayerCount = 0,
                IsFull = false,
                CreatedTime = DateTime.Now
            };

            _availableRooms[roomName] = newRoom;

            OnRoomCreated?.Invoke(roomName);

            Debug.Log($"[OdinRoomManager] Created room: {roomName}");
        }

        public void JoinRoom(string roomId)
        {
            if (OdinVoiceManager.Instance.IsConnected)
            {
                Debug.LogWarning("[OdinRoomManager] Already connected to a room");
                return;
            }

            currentRoomId = roomId;
            isHost = false;

            OdinVoiceManager.Instance.JoinRoom(roomId);

            Debug.Log($"[OdinRoomManager] Joining room: {roomId}");
        }

        public void JoinRandomRoom()
        {
            var availableRoom = _availableRooms.Values
                .Where(r => !r.IsFull && r.PlayerCount < maxPlayersPerRoom)
                .OrderBy(r => r.PlayerCount)
                .FirstOrDefault();

            if (availableRoom != null)
            {
                JoinRoom(availableRoom.RoomId);
            }
            else if (allowDynamicRoomCreation)
            {
                CreateRoom();
            }
            else
            {
                Debug.LogWarning("[OdinRoomManager] No available rooms to join");
            }
        }

        public void LeaveCurrentRoom()
        {
            if (!OdinVoiceManager.Instance.IsConnected)
            {
                Debug.LogWarning("[OdinRoomManager] Not connected to any room");
                return;
            }

            OdinVoiceManager.Instance.LeaveRoom();

            connectedPlayers.Clear();
            currentPlayerCount = 0;
            currentRoomId = "";
            isHost = false;

            OnRoomLeft?.Invoke();

            Debug.Log("[OdinRoomManager] Left current room");
        }

        private void HandleRoomJoined(Room room)
        {
            currentRoomId = room.Config.Name;
            currentPlayerCount = room.RemotePeers.Count + 1;

            UpdateRoomData(currentRoomId);

            OnRoomJoined?.Invoke(currentRoomId);

            Debug.Log($"[OdinRoomManager] Successfully joined room: {currentRoomId} with {currentPlayerCount} players");
        }

        private void HandleRoomLeft()
        {
            if (_availableRooms.ContainsKey(currentRoomId))
            {
                _availableRooms[currentRoomId].PlayerCount--;
                if (_availableRooms[currentRoomId].PlayerCount <= 0)
                {
                    _availableRooms.Remove(currentRoomId);
                }
            }

            connectedPlayers.Clear();
            _playerLatencies.Clear();
        }

        private void HandlePeerJoined(ulong peerId, string peerName)
        {
            PlayerInfo newPlayer = new PlayerInfo
            {
                PeerId = peerId,
                PlayerName = peerName,
                IsReady = false,
                Latency = 0f,
                Position = Vector3.zero,
                IsHost = false,
                JoinTime = DateTime.Now
            };

            connectedPlayers.Add(newPlayer);
            currentPlayerCount = connectedPlayers.Count + 1;

            UpdateRoomData(currentRoomId);

            OnPlayerJoinedRoom?.Invoke(newPlayer);

            CheckRoomCapacity();

            Debug.Log($"[OdinRoomManager] Player {peerName} joined room (Total: {currentPlayerCount}/{maxPlayersPerRoom})");
        }

        private void HandlePeerLeft(ulong peerId)
        {
            PlayerInfo leavingPlayer = connectedPlayers.FirstOrDefault(p => p.PeerId == peerId);
            if (leavingPlayer != null)
            {
                connectedPlayers.Remove(leavingPlayer);
                currentPlayerCount = connectedPlayers.Count + 1;

                if (_playerLatencies.ContainsKey(peerId))
                {
                    _playerLatencies.Remove(peerId);
                }

                UpdateRoomData(currentRoomId);

                OnPlayerLeftRoom?.Invoke(peerId);

                CheckRoomCapacity();

                if (leavingPlayer.IsHost && connectedPlayers.Count > 0)
                {
                    MigrateHost();
                }

                Debug.Log($"[OdinRoomManager] Player left room (Total: {currentPlayerCount}/{maxPlayersPerRoom})");
            }
        }

        private void HandleLatencyUpdate(float latency)
        {
            averageLatency = latency;

            if (enableTimeSynchronization)
            {
                CompensateForLatency();
            }
        }

        private void UpdateRoomData(string roomId)
        {
            if (_availableRooms.TryGetValue(roomId, out RoomData roomData))
            {
                roomData.PlayerCount = currentPlayerCount;
                roomData.IsFull = currentPlayerCount >= maxPlayersPerRoom;
                roomData.Players = new List<PlayerInfo>(connectedPlayers);
            }
        }

        private void CheckRoomCapacity()
        {
            if (currentPlayerCount >= maxPlayersPerRoom && _availableRooms.ContainsKey(currentRoomId))
            {
                _availableRooms[currentRoomId].IsFull = true;
                Debug.Log($"[OdinRoomManager] Room {currentRoomId} is now full");
            }
            else if (currentPlayerCount < maxPlayersPerRoom && _availableRooms.ContainsKey(currentRoomId))
            {
                _availableRooms[currentRoomId].IsFull = false;
            }
        }

        private void MigrateHost()
        {
            var oldestPlayer = connectedPlayers.OrderBy(p => p.JoinTime).FirstOrDefault();
            if (oldestPlayer != null)
            {
                oldestPlayer.IsHost = true;
                Debug.Log($"[OdinRoomManager] Host migrated to {oldestPlayer.PlayerName}");
            }
        }

        private void CompensateForLatency()
        {
            float compensation = Mathf.Min(averageLatency, maxLatencyCompensation);

            foreach (var player in connectedPlayers)
            {
                if (_playerLatencies.TryGetValue(player.PeerId, out float playerLatency))
                {
                    player.Latency = playerLatency;
                }
            }
        }

        private string GenerateRoomName()
        {
            return $"{roomPrefix}{DateTime.Now.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        private void Update()
        {
            if (!OdinVoiceManager.Instance.IsConnected)
                return;

            if (enableTimeSynchronization && Time.time - _lastSyncTime >= syncInterval)
            {
                _lastSyncTime = Time.time;
                PerformSync();
            }
        }

        private void PerformSync()
        {
            float syncTime = Time.time - _sessionStartTime;
            OnSyncTick?.Invoke(syncTime);

            if (isHost)
            {
                BroadcastSyncData(syncTime);
            }
        }

        private void BroadcastSyncData(float syncTime)
        {
            // Broadcast sync data through ODIN's custom data channel
            // This would require integration with ODIN's data messaging system
        }

        public void SetPlayerReady(ulong peerId, bool ready)
        {
            PlayerInfo player = connectedPlayers.FirstOrDefault(p => p.PeerId == peerId);
            if (player != null)
            {
                player.IsReady = ready;

                if (AreAllPlayersReady())
                {
                    StartChorus();
                }
            }
        }

        public bool AreAllPlayersReady()
        {
            if (connectedPlayers.Count == 0)
                return false;

            return connectedPlayers.All(p => p.IsReady);
        }

        private void StartChorus()
        {
            Debug.Log("[OdinRoomManager] All players ready! Starting chorus...");
            _sessionStartTime = Time.time;
        }

        public void UpdatePlayerPosition(ulong peerId, Vector3 position)
        {
            PlayerInfo player = connectedPlayers.FirstOrDefault(p => p.PeerId == peerId);
            if (player != null)
            {
                player.Position = position;
                OdinAudioRouter.Instance.SetPlayerPosition(peerId, position);
            }
        }

        public List<string> GetAvailableRooms()
        {
            return _availableRooms.Values
                .Where(r => !r.IsFull)
                .OrderBy(r => r.PlayerCount)
                .Select(r => r.RoomId)
                .ToList();
        }

        public RoomData GetRoomData(string roomId)
        {
            _availableRooms.TryGetValue(roomId, out RoomData roomData);
            return roomData;
        }

        public List<PlayerInfo> GetConnectedPlayers()
        {
            return new List<PlayerInfo>(connectedPlayers);
        }

        public PlayerInfo GetPlayerInfo(ulong peerId)
        {
            return connectedPlayers.FirstOrDefault(p => p.PeerId == peerId);
        }

        private void OnDestroy()
        {
            if (OdinVoiceManager.Instance != null)
            {
                OdinVoiceManager.Instance.OnRoomJoined -= HandleRoomJoined;
                OdinVoiceManager.Instance.OnRoomLeft -= HandleRoomLeft;
                OdinVoiceManager.Instance.OnPeerJoined -= HandlePeerJoined;
                OdinVoiceManager.Instance.OnPeerLeft -= HandlePeerLeft;
                OdinVoiceManager.Instance.OnLatencyUpdated -= HandleLatencyUpdate;
            }
        }
    }
}