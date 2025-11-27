using UnityEngine;

public class SimpleOdinTest : MonoBehaviour
{
    [Header("ODIN Settings")]
    public string roomName = "TestRoom";
    public string accessToken = "WzXiMkqgXJf0wsjLmBaITsB3kMXQZnoL9HwG2XQC79e98193";

    void Start()
    {
        Debug.Log("=== ODIN Simple Test Started ===");
        Debug.Log($"Room Name: {roomName}");
        Debug.Log($"Token: {accessToken.Substring(0, 10)}...");

        // Try to find ODIN components
        var odinComponents = FindObjectsOfType<MonoBehaviour>();
        foreach (var comp in odinComponents)
        {
            if (comp.GetType().Name.Contains("Odin"))
            {
                Debug.Log($"Found ODIN Component: {comp.GetType().Name}");
            }
        }

        // Basic connection attempt
        try
        {
            Debug.Log("Attempting to connect to ODIN...");
            // The actual connection will be handled by OdinManager prefab
            // This script just logs the attempt
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), "ODIN Test - Check Console for logs");

        if (GUI.Button(new Rect(10, 50, 150, 30), "Manual Connect"))
        {
            Debug.Log("Manual connect button pressed");
            // Add connection logic here if needed
        }
    }
}