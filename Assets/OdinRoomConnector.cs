using UnityEngine;
using OdinNative.Odin;

public class OdinRoomConnector : MonoBehaviour
{
    [Header("Room Settings")]
    public string roomName = "TestRoom";
    public string accessToken = ""; // トークンは別途提供されます

    [Header("Auto Join")]
    public bool autoJoinOnStart = true;

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

        try
        {
            // Simply join the room with the access token
            OdinHandler.Instance.JoinRoom(roomName, accessToken);
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