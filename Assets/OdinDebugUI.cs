using UnityEngine;
using OdinNative.Unity;
using OdinNative.Unity.Events;
using System.Linq;
using System.Collections.Generic;

public class OdinDebugUI : MonoBehaviour
{
    // UI設定
    private bool showDebugUI = true;
    private Vector2 scrollPosition;
    private List<string> logMessages = new List<string>();
    private int maxLogMessages = 100;

    // 接続状態
    private string connectionStatus = "Disconnected";
    private int connectedPeers = 0;
    private string currentRoom = "";

    // オーディオデバイス
    private string[] availableMicrophones;
    private int selectedMicrophoneIndex = 0;
    private string currentMicrophone = "";

    // 音声レベル
    private float inputLevel = 0f;
    private Dictionary<ulong, float> peerAudioLevels = new Dictionary<ulong, float>();

    // UIスタイル
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;

    void Start()
    {
        // マイクデバイス一覧を取得
        RefreshMicrophoneList();

        // ODINイベントをサブスクライブ
        SubscribeToOdinEvents();

        AddLog("OdinDebugUI initialized");
        AddLog($"Found {availableMicrophones.Length} microphone(s)");

        // 初期マイクを設定
        if (availableMicrophones.Length > 0)
        {
            SelectMicrophone(0);
        }
    }

    void RefreshMicrophoneList()
    {
        availableMicrophones = Microphone.devices;
        if (availableMicrophones.Length == 0)
        {
            availableMicrophones = new string[] { "No microphone detected" };
        }
    }

    void SubscribeToOdinEvents()
    {
        if (OdinHandler.Instance != null)
        {
            OdinHandler.Instance.OnRoomJoined.AddListener((args) =>
            {
                connectionStatus = "Connected";
                currentRoom = args.Room.Config.Name;
                AddLog($"✅ Joined room: {currentRoom}");
            });

            OdinHandler.Instance.OnRoomLeft.AddListener((args) =>
            {
                connectionStatus = "Disconnected";
                currentRoom = "";
                connectedPeers = 0;
                peerAudioLevels.Clear();
                AddLog($"❌ Left room: {args.RoomName}");
            });

            OdinHandler.Instance.OnPeerJoined.AddListener((sender, args) =>
            {
                connectedPeers++;
                AddLog($"👤 Peer joined: {args.Peer.Id} (Total: {connectedPeers})");
            });

            OdinHandler.Instance.OnPeerLeft.AddListener((sender, args) =>
            {
                connectedPeers--;
                // PeerLeftEventArgsのPeerIdプロパティを使用
                var peerId = args.PeerId;
                if (peerAudioLevels.ContainsKey(peerId))
                {
                    peerAudioLevels.Remove(peerId);
                }
                AddLog($"👤 Peer left: {peerId} (Total: {connectedPeers})");
            });

            OdinHandler.Instance.OnMediaAdded.AddListener((sender, args) =>
            {
                AddLog($"🎤 Media added from peer: {args.PeerId}");
            });

            OdinHandler.Instance.OnMediaRemoved.AddListener((sender, args) =>
            {
                // MediaStreamIdを使用
                AddLog($"🔇 Media removed - Stream ID: {args.MediaStreamId}");
            });
        }
    }

    void SelectMicrophone(int index)
    {
        if (index >= 0 && index < availableMicrophones.Length)
        {
            selectedMicrophoneIndex = index;
            currentMicrophone = availableMicrophones[index];

            // AudioSettingsManagerを使用して設定を適用・保存
            if (AudioSettingsManager.Instance != null)
            {
                AudioSettingsManager.Instance.SetMicrophone(index);
                AddLog($"🎤 Switched to microphone: {currentMicrophone} (saved)");
            }
            else
            {
                // フォールバック処理
                if (OdinHandler.Instance != null && OdinHandler.Instance.Microphone != null)
                {
                    OdinHandler.Instance.Microphone.StopListen();
                    OdinHandler.Instance.Microphone.StartListen();
                    AddLog($"🎤 Switched to microphone: {currentMicrophone}");
                }
            }
        }
    }

    void AddLog(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        logMessages.Insert(0, $"[{timestamp}] {message}");

        if (logMessages.Count > maxLogMessages)
        {
            logMessages.RemoveAt(logMessages.Count - 1);
        }

        Debug.Log($"[OdinDebugUI] {message}");
    }

    void Update()
    {
        // Toggle UI with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showDebugUI = !showDebugUI;
        }

        // マイクの音声レベルを更新
        if (OdinHandler.Instance != null && OdinHandler.Instance.Microphone != null)
        {
            // 簡易的な音声レベル計算
            inputLevel = Mathf.Lerp(inputLevel, 0f, Time.deltaTime * 5f);

            // Unityのマイクが録音中かチェック
            if (Microphone.IsRecording(currentMicrophone))
            {
                // マイクが録音中の場合の視覚的フィードバック
                // 実際の音声データは OdinHandler.Instance.Microphone から取得可能
                inputLevel = 0.2f; // 録音中を示す最小レベル
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugUI) return;

        InitializeStyles();

        // メインウィンドウ
        float windowWidth = 450;
        float windowHeight = 600;
        float padding = 10;

        GUILayout.BeginArea(new Rect(padding, padding, windowWidth, windowHeight));

        // タイトル
        GUILayout.Box("🎤 ODIN Voice Chat Debug Panel", boxStyle, GUILayout.Width(windowWidth - 20));

        // 接続状態セクション
        GUILayout.BeginVertical(boxStyle);
        GUILayout.Label("📡 Connection Status", labelStyle);
        GUILayout.Label($"Status: {GetStatusColor(connectionStatus)}", labelStyle);
        GUILayout.Label($"Room: {(string.IsNullOrEmpty(currentRoom) ? "Not connected" : currentRoom)}", labelStyle);
        GUILayout.Label($"Connected Peers: {connectedPeers}", labelStyle);

        // 接続/切断ボタン
        GUILayout.BeginHorizontal();
        if (connectionStatus == "Disconnected")
        {
            if (GUILayout.Button("🔗 Connect to Room", buttonStyle))
            {
                ConnectToRoom();
            }
        }
        else
        {
            if (GUILayout.Button("🔌 Disconnect", buttonStyle))
            {
                DisconnectFromRoom();
            }
        }

        if (GUILayout.Button("🔄 Refresh Devices", buttonStyle))
        {
            RefreshMicrophoneList();
            AddLog("Refreshed device list");
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        // マイクロフォンセクション
        GUILayout.BeginVertical(boxStyle);
        GUILayout.Label("🎤 Microphone Settings", labelStyle);

        // マイク選択ドロップダウン
        GUILayout.BeginHorizontal();
        GUILayout.Label("Device:", GUILayout.Width(60));

        // Simple dropdown using buttons
        if (GUILayout.Button(currentMicrophone, buttonStyle))
        {
            selectedMicrophoneIndex = (selectedMicrophoneIndex + 1) % availableMicrophones.Length;
            SelectMicrophone(selectedMicrophoneIndex);
        }
        GUILayout.EndHorizontal();

        // 音声レベルインジケーター
        GUILayout.Label($"Input Level:", labelStyle);
        DrawAudioLevelBar(inputLevel, Color.green);

        // ボリューム調整スライダー（ビルド版用）
        GUILayout.Label($"Speaker Volume: {(int)(AudioListener.volume * 100)}%", labelStyle);
        float newVolume = GUILayout.HorizontalSlider(AudioListener.volume, 0f, 1f);
        if (Mathf.Abs(newVolume - AudioListener.volume) > 0.01f)
        {
            AudioListener.volume = newVolume;
            if (AudioSettingsManager.Instance != null)
            {
                AudioSettingsManager.Instance.SetSpeakerVolume(newVolume);
            }
        }

        // ミュートボタン（停止/開始で制御）
        bool isListening = OdinHandler.Instance?.Microphone != null;
        if (GUILayout.Button(isListening ? "🔊 Stop Mic" : "🔇 Start Mic", buttonStyle))
        {
            if (OdinHandler.Instance?.Microphone != null)
            {
                if (isListening)
                {
                    OdinHandler.Instance.Microphone.StopListen();
                    AddLog("Microphone stopped");
                }
                else
                {
                    OdinHandler.Instance.Microphone.StartListen();
                    AddLog("Microphone started");
                }
            }
        }

        GUILayout.EndVertical();

        // ピア音声レベル
        if (peerAudioLevels.Count > 0)
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("👥 Peer Audio Levels", labelStyle);

            foreach (var peer in peerAudioLevels)
            {
                GUILayout.Label($"Peer {peer.Key}:", labelStyle);
                DrawAudioLevelBar(peer.Value, Color.cyan);
            }

            GUILayout.EndVertical();
        }

        // ログセクション
        GUILayout.BeginVertical(boxStyle);
        GUILayout.Label("📋 Debug Log", labelStyle);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        foreach (string log in logMessages)
        {
            GUILayout.Label(log, GUILayout.Width(400));
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // ヘルプテキスト
        GUILayout.Label("Press TAB to toggle this panel", labelStyle);

        GUILayout.EndArea();
    }

    void InitializeStyles()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.margin = new RectOffset(0, 0, 5, 5);
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;
            labelStyle.normal.textColor = Color.white;
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(10, 10, 5, 5);
        }
    }

    void DrawAudioLevelBar(float level, Color color)
    {
        Rect rect = GUILayoutUtility.GetRect(400, 20);

        // 背景
        GUI.Box(rect, "");

        // レベルバー
        rect.x += 2;
        rect.y += 2;
        rect.width = (rect.width - 4) * Mathf.Clamp01(level);
        rect.height -= 4;

        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    string GetStatusColor(string status)
    {
        switch (status)
        {
            case "Connected":
                return $"<color=green>{status}</color>";
            case "Connecting":
                return $"<color=yellow>{status}</color>";
            case "Disconnected":
                return $"<color=red>{status}</color>";
            default:
                return status;
        }
    }

    void ConnectToRoom()
    {
        connectionStatus = "Connecting";
        AddLog("Attempting to connect...");

        var connector = FindFirstObjectByType<OdinRoomConnector>();
        if (connector != null)
        {
            connector.JoinRoom();
        }
    }

    void DisconnectFromRoom()
    {
        AddLog("Disconnecting from room...");

        if (OdinHandler.Instance != null && !string.IsNullOrEmpty(currentRoom))
        {
            OdinHandler.Instance.LeaveRoom(currentRoom);
        }
    }
}