using UnityEngine;
using OdinNative.Odin;
using System;

public class OdinRoomConnector : MonoBehaviour
{
    [Header("Room Settings")]
    public string roomName = "TestRoom";

    [Header("Auto Join")]
    public bool autoJoinOnStart = true;

    [Header("Debug")]
    public bool useTestMode = false;

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
        Debug.Log($"[OdinRoomConnector] Test mode: {useTestMode}");

        if (useTestMode)
        {
            Debug.LogWarning("[OdinRoomConnector] Test mode enabled - simulating connection without ODIN");
            SimulateConnection();
            return;
        }

        if (string.IsNullOrEmpty(AccessToken))
        {
            Debug.LogError("[OdinRoomConnector] Access token is empty! Please set ODIN_ACCESS_TOKEN in .env file or enable Test Mode.");
            return;
        }

        // Check if OdinHandler is available and working
        if (OdinHandler.Instance == null)
        {
            Debug.LogError("[OdinRoomConnector] OdinHandler.Instance is null! Falling back to test mode.");
            useTestMode = true;
            SimulateConnection();
            return;
        }

        try
        {
            // Simply join the room with the access token
            OdinHandler.Instance.JoinRoom(roomName, AccessToken);
            Debug.Log("[OdinRoomConnector] Join room command sent");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OdinRoomConnector] Error: {e.Message}");
            Debug.LogError("[OdinRoomConnector] Switching to test mode due to ODIN error");
            useTestMode = true;
            SimulateConnection();
        }
    }

    private void SimulateConnection()
    {
        Debug.Log($"[OdinRoomConnector] [SIMULATION] Successfully joined room: {roomName}");
        Debug.Log("[OdinRoomConnector] [SIMULATION] Voice chat interface ready for testing");
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