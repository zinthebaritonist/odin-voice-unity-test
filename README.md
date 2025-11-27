# 🎤 ODIN Voice Chat Unity Test Project

[![Unity](https://img.shields.io/badge/Unity-2019.4%2B-blue.svg)](https://unity.com)
[![ODIN](https://img.shields.io/badge/ODIN-Voice%20SDK-green.svg)](https://www.4players.io/odin/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac%20%7C%20Quest-lightgrey.svg)](https://github.com/zinthebaritonist/odin-voice-unity-test)

4Players ODIN を使用した**超低レイテンシ音声チャット**のUnityテストプロジェクトです。
リアルタイムでの合唱・セッションが可能なレベルの低遅延を実現しています。

## ✨ 特徴

- 🎵 **超低レイテンシ** - 4人同時の合唱が可能なレベル
- 🎧 **デュアルバス音声** - モニター用と配信用を独立制御
- 🥽 **XR対応** - Quest 2/3、Quest Pro に最適化
- 🖥️ **マルチプラットフォーム** - Windows/Mac/Quest で動作
- 🔊 **3D空間音響** - XR環境での立体音響サポート

## 🚀 クイックスタート（5分で動作確認！）

### 前提条件
- Unity 2019.4 LTS 以降（推奨: Unity 2022.3 LTS / Unity 6）
- マイク（内蔵でOK）
- Git

### セットアップ

#### 1. クローン
```bash
git clone https://github.com/zinthebaritonist/odin-voice-unity-test.git
cd odin-voice-unity-test
```

#### 2. Unity で開く
1. Unity Hub → 「追加」ボタン
2. クローンしたフォルダを選択
3. Unity バージョンを選択して開く

#### 3. アクセストークンを設定
> ⚠️ **重要**: アクセストークンは別途お伝えします

1. Hierarchy で `OdinManager` を選択
2. Inspector の `OdinRoomConnector` コンポーネント
3. `Access Token` フィールドに提供されたトークンを入力

#### 4. 実行
**Play ボタン** ▶️ を押すだけ！自動的に `TestRoom` に接続されます。

## 🎮 動作テスト

### 単体テスト（自分の声を確認）
1. Play ボタンで実行
2. Inspector → `Mic-AudioClip Settings`
3. `Loopback Test` にチェック ✅
4. マイクに話しかけて自分の声が聞こえればOK

### 複数人テスト

#### 同じPC で2つ起動
```
1. Unity エディタで Play
2. File → Build & Run で実行ファイルを起動
3. 両方が同じルームに自動接続
4. それぞれで話して相互に聞こえるか確認
```

#### 別の PC/デバイス
- 同じトークンを使用
- 同じ Room Name（デフォルト: TestRoom）
- 最大25人まで同時接続可能

## 📁 プロジェクト構造

```
OdinVoiceProject/
├── Assets/
│   ├── OdinCore/              # 🎯 コアモジュール
│   │   ├── Scripts/
│   │   │   ├── OdinRoomConnector.cs    # メイン接続スクリプト
│   │   │   ├── SimpleOdinManager.cs    # シンプル実装例
│   │   │   ├── OdinVoiceManager.cs     # 音声管理
│   │   │   └── OdinAudioRouter.cs      # オーディオルーティング
│   │   └── Audio/
│   │       └── MainAudioMixer.mixer    # 2バス構成設定
│   │
│   ├── XR/                    # 🥽 Quest/XR対応
│   │   └── Scripts/
│   │       └── XRVoiceController.cs    # XR用コントローラー
│   │
│   └── TestApp/               # 🧪 テスト用UI
│       └── Scripts/
│           ├── ChorusTestUI.cs         # テストUI
│           └── TestPlayerController.cs  # プレイヤー制御
│
├── Packages/
│   └── manifest.json          # 必要パッケージ定義
│
└── ProjectSettings/
    └── XRSettings.asset       # XR/Quest設定
```

## ⚙️ カスタマイズ

### ルーム設定の変更
Inspector で `OdinRoomConnector` を選択：
- **Room Name**: 接続するルーム名
- **Auto Join On Start**: 自動接続の ON/OFF

### 音声設定
`OdinManager` の Inspector で：
- **Microphone Test**: マイクテスト
- **Loopback Test**: 自分の声を聞く
- **Voice Activity Detection**: 音声検出
- **Noise Suppression**: ノイズ抑制

### プラットフォーム別ビルド

#### Windows/Mac
```
File → Build Settings
→ PC, Mac & Linux Standalone
→ Build and Run
```

#### Quest 2/3
```
File → Build Settings
→ Android
→ Switch Platform
→ Player Settings で以下を確認:
  - XR Settings → Oculus
  - Minimum API Level: 29
→ Build and Run
```

## 🐛 トラブルシューティング

### よくある問題と解決策

| 問題 | 解決方法 |
|------|----------|
| Safe Mode ダイアログ | 「Ignore」を選択 |
| 音が聞こえない | マイク権限を確認（システム設定） |
| 接続エラー | アクセストークンが正しいか確認 |
| エラー: `Error leaving room` | 初回起動時のみ、無視してOK |
| Package Manager エラー | Unity を再起動 |

### デバッグ方法
1. **Console ウィンドウ** でログを確認
2. 正常な場合のログ：
   ```
   [OdinRoomConnector] Starting...
   [OdinRoomConnector] Attempting to join room: TestRoom
   [OdinRoomConnector] Join room command sent
   ```

## 🎯 テスト項目チェックリスト

- [ ] Unity で開ける
- [ ] Play ボタンで起動する
- [ ] Console にエラーが出ない
- [ ] 自分の声が聞こえる（Loopback Test）
- [ ] 複数起動で相互に音声が聞こえる
- [ ] 遅延が許容範囲内（体感100ms以下）

## 📊 パフォーマンス目標

| 項目 | 目標値 | 備考 |
|------|--------|------|
| レイテンシ | < 50ms | ローカルネットワーク |
| 同時接続 | 4人 | 合唱用途 |
| 音質 | 48kHz | 高音質 |
| CPU使用率 | < 10% | デスクトップ |

## 🛠️ 技術仕様

### ODIN SDK
- バージョン: 最新版（GitHub経由）
- プロトコル: WebRTC ベース
- コーデック: Opus
- サンプルレート: 48kHz

### Unity 設定
- Render Pipeline: Built-in（互換性重視）
- Audio: 48kHz, Best latency
- XR: OpenXR + Oculus

## 🤝 コントリビューション

### バグ報告
Issue を作成する際は以下を含めてください：
1. Unity バージョン
2. プラットフォーム（Windows/Mac/Quest）
3. エラーログ（Console の内容）
4. 再現手順

### 改善提案
Pull Request 歓迎です！
- コード規約: C# standard
- コミットメッセージ: 日本語OK

## 📚 参考資料

- [ODIN 公式ドキュメント](https://docs.4players.io/voice/unity/)
- [Unity XR ドキュメント](https://docs.unity3d.com/Manual/XR.html)
- [4Players ODIN](https://www.4players.io/odin/)

## 📝 ライセンス

このテストプロジェクトは評価目的です。
ODIN SDK は 4Players のライセンスに従います。

## 🙏 謝辞

- 4Players ODIN チーム
- Unity コミュニティ
- テスターの皆様

---

## 📮 フィードバック

以下の点についてフィードバックをお待ちしています：

- ✅ 接続の成功/失敗
- 🎯 音声の遅延体感
- 🐛 発生したエラー
- 💡 改善案

Issue または Discussion でお知らせください。

---

**Happy Voice Chatting! 🎤**