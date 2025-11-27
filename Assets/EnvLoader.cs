using UnityEngine;
using System.IO;
using System;

public static class EnvLoader
{
    private static bool _loaded = false;
    private static string _odinAccessToken = "";

    public static string OdinAccessToken
    {
        get
        {
            if (!_loaded) LoadEnv();
            return _odinAccessToken;
        }
    }

    private static void LoadEnv()
    {
        _loaded = true;
        string envPath = Path.Combine(Application.dataPath, "../.env");

        Debug.Log($"[EnvLoader] Looking for .env file at: {envPath}");

        if (File.Exists(envPath))
        {
            Debug.Log("[EnvLoader] .env file found, reading...");
            string[] lines = File.ReadAllLines(envPath);
            foreach (string line in lines)
            {
                Debug.Log($"[EnvLoader] Processing line: {line}");
                if (line.StartsWith("ODIN_ACCESS_TOKEN="))
                {
                    _odinAccessToken = line.Substring("ODIN_ACCESS_TOKEN=".Length).Trim();
                    Debug.Log($"[EnvLoader] Token loaded: {_odinAccessToken.Substring(0, Math.Min(10, _odinAccessToken.Length))}...");
                    break;
                }
            }
        }
        else
        {
            Debug.LogError($"[EnvLoader] .env file not found at: {envPath}");
        }

        if (string.IsNullOrEmpty(_odinAccessToken))
        {
            Debug.LogError("[EnvLoader] ODIN_ACCESS_TOKEN not found in .env file");
        }
    }
}