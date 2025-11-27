using System;
using UnityEngine;
using OdinNative.Odin;
using OdinNative.Odin.Room;
using OdinNative.Odin.Peer;

namespace OdinVoiceChat.Core
{
    public class SimpleOdinManager : MonoBehaviour
    {
        [Header("ODIN Settings")]
        [SerializeField] private string accessToken = ""; // トークンは別途提供されます
        [SerializeField] private string roomName = "TestRoom";
        [SerializeField] private bool autoJoin = true;

        private void Start()
        {
            Debug.Log("[SimpleOdinManager] Starting...");

            if (autoJoin && !string.IsNullOrEmpty(accessToken))
            {
                JoinRoom();
            }
        }

        public void JoinRoom()
        {
            try
            {
                Debug.Log($"[SimpleOdinManager] Attempting to join room: {roomName}");

                // Set up event listeners before joining
                OdinHandler.Instance.OnRoomJoined.AddListener(args => OnRoomJoinedHandler(this, args));
                OdinHandler.Instance.OnPeerJoined.AddListener((sender, args) => OnPeerJoinedHandler(sender, args));

                // Join the room with the access token
                OdinHandler.Instance.JoinRoom(roomName, accessToken);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleOdinManager] Failed to join room: {e.Message}");
            }
        }

        public void LeaveRoom()
        {
            try
            {
                Debug.Log($"[SimpleOdinManager] Leaving room: {roomName}");
                OdinHandler.Instance.LeaveRoom(roomName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleOdinManager] Failed to leave room: {e.Message}");
            }
        }

        private void OnRoomJoinedHandler(object sender, RoomJoinedEventArgs args)
        {
            Debug.Log($"[SimpleOdinManager] Successfully joined room: {args.Room.Config.Name}");
            Debug.Log($"[SimpleOdinManager] Peers in room: {args.Room.RemotePeers.Count}");
        }

        private void OnPeerJoinedHandler(object sender, PeerJoinedEventArgs args)
        {
            Debug.Log($"[SimpleOdinManager] Peer joined: {args.Peer.UserId} (ID: {args.Peer.Id})");
        }

        private void OnDestroy()
        {
            LeaveRoom();
        }
    }
}