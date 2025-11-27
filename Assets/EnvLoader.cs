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

        if (File.Exists(envPath))
        {
            string[] lines = File.ReadAllLines(envPath);
            foreach (string line in lines)
            {
                if (line.StartsWith("ODIN_ACCESS_TOKEN="))
                {
                    _odinAccessToken = line.Substring("ODIN_ACCESS_TOKEN=".Length).Trim();
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(_odinAccessToken))
        {
            Debug.LogWarning("[EnvLoader] .envファイルまたはODIN_ACCESS_TOKENが見つかりません");
        }
    }
}