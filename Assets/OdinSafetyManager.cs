using UnityEngine;
using OdinNative.Unity;

public class OdinSafetyManager : MonoBehaviour
{
    [Header("Safety Settings")]
    public bool disableOdinOnError = true;
    public float errorThreshold = 10f; // errors per second

    private float errorCount = 0f;
    private float timeWindow = 1f;
    private float lastErrorTime = 0f;
    private bool odinDisabled = false;

    void Start()
    {
        // Log all existing OdinHandler instances
        var handlers = FindObjectsOfType<OdinHandler>();
        Debug.Log($"[OdinSafetyManager] Found {handlers.Length} OdinHandler instances");

        if (handlers.Length == 0)
        {
            Debug.LogWarning("[OdinSafetyManager] No OdinHandler found - running in safe mode");
        }
    }

    void Update()
    {
        if (odinDisabled) return;

        CheckForErrors();
    }

    private void CheckForErrors()
    {
        // Check if there are continuous null reference exceptions
        if (Time.time - lastErrorTime < timeWindow)
        {
            errorCount++;
        }
        else
        {
            errorCount = 0;
        }

        if (errorCount > errorThreshold)
        {
            DisableOdin();
        }
    }

    private void DisableOdin()
    {
        if (odinDisabled) return;

        odinDisabled = true;
        Debug.LogError("[OdinSafetyManager] Too many ODIN errors detected - disabling ODIN components");

        // Find and disable all ODIN related components
        var handlers = FindObjectsOfType<OdinHandler>();
        foreach (var handler in handlers)
        {
            handler.enabled = false;
            Debug.Log($"[OdinSafetyManager] Disabled OdinHandler: {handler.name}");
        }

        var microphones = FindObjectsOfType<MicrophoneReader>();
        foreach (var mic in microphones)
        {
            mic.enabled = false;
            Debug.Log($"[OdinSafetyManager] Disabled MicrophoneReader: {mic.name}");
        }

        Debug.LogWarning("[OdinSafetyManager] ODIN disabled - application running in safe mode");
    }

    // Call this when an error is detected
    public void ReportError()
    {
        lastErrorTime = Time.time;
    }
}