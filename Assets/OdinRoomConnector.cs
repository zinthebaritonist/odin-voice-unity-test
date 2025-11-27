using UnityEngine;
using OdinNative.Odin;
using System;

public class OdinRoomConnector : MonoBehaviour
{
    [Header("Room Settings")]
    public string roomName = "TestRoom";

    [Header("Auto Join")]
    public bool autoJoinOnStart = true;

    private string AccessToken
    {
        get
        {
            return EnvLoader.OdinAccessToken;
        }
    }

    void Start()
    {
        Debug.Log("[OdinRoomConnector] Starting...");

        if (autoJoinOnStart)
        {
            JoinRoom();
        }
    }

    public void JoinRoom()
    {
        Debug.Log($"[OdinRoomConnector] Attempting to join room: {roomName}");
        Debug.Log($"[OdinRoomConnector] Using token: {(string.IsNullOrEmpty(AccessToken) ? "EMPTY" : AccessToken.Substring(0, Math.Min(10, AccessToken.Length)) + "...")}");

        if (string.IsNullOrEmpty(AccessToken))
        {
            Debug.LogError("[OdinRoomConnector] Access token is empty! Please set ODIN_ACCESS_TOKEN in .env file.");
            return;
        }

        try
        {
            // Join the room with the access token
            OdinHandler.Instance.JoinRoom(roomName, AccessToken);
            Debug.Log("[OdinRoomConnector] Join room command sent");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OdinRoomConnector] Error: {e.Message}");
        }
    }

    public void LeaveRoom()
    {
        try
        {
            OdinHandler.Instance.LeaveRoom(roomName);
            Debug.Log($"[OdinRoomConnector] Left room: {roomName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OdinRoomConnector] Error leaving room: {e.Message}");
        }
    }
}