# ODIN Voice Chat Unity Project - 低レイテンシ合唱アプリ

## 概要
4Players ODIN を使用した低レイテンシ音声基盤のUnityプロジェクトです。Quest 2/3、Quest Pro、Windows PCに対応し、4人同時の合唱が可能な音声チャットシステムを実装しています。

## 主な特徴
- **超低レイテンシ音声通信**: ODINの高速音声通信技術により、リアルタイム合唱を実現
- **デュアルバス構成**: モニター用と配信用の音声を独立して制御
- **マルチプラットフォーム対応**: Quest 2/3、Quest Pro、Windows PCで動作
- **3D空間音響**: XR環境での立体音響に対応
- **自動最適化**: プラットフォームごとに最適な設定を自動適用

## 必要要件

### Unity
- Unity 2019.4 LTS 以降
- Universal Render Pipeline（URP）推奨

### パッケージ
- ODIN Unity SDK
- XR Plugin Management（Quest対応の場合）
- OpenXR Plugin（Quest対応の場合）
- Oculus XR Plugin（Quest対応の場合）
- TextMeshPro

### ODIN アクセストークン
- 開発用: 25 PCU までの無料キーを使用
- 本番用: [4Players ODIN](https://odin.4players.io/)でアカウント作成

## インストール手順

### 1. プロジェクトのクローン
```bash
git clone [your-repository-url]
cd OdinVoiceProject
```

### 2. Unityでプロジェクトを開く
1. Unity Hub を起動
2. 「Add」からプロジェクトフォルダを選択
3. Unity 2019.4 以降で開く

### 3. ODIN SDK のセットアップ
Package Manager から ODIN Unity SDK をインポート（manifest.json に記載済み）

### 4. プラットフォーム設定

#### Windows向け
1. File > Build Settings > Platform: PC, Mac & Linux Standalone
2. Target Platform: Windows
3. Architecture: x86_64

#### Quest向け
1. File > Build Settings > Platform: Android
2. Texture Compression: ASTC
3. Project Settings > XR Plug-in Management > Android: Oculus を有効化
4. Project Settings > Player > Android Settings:
   - Minimum API Level: 29
   - Target API Level: 32

## プロジェクト構造

```
OdinVoiceProject/
├── Assets/
│   ├── OdinCore/          # コアモジュール
│   │   ├── Scripts/
│   │   │   ├── OdinVoiceManager.cs    # ODIN接続管理
│   │   │   ├── OdinAudioRouter.cs     # オーディオルーティング
│   │   │   ├── OdinRoomManager.cs     # ルーム管理
│   │   │   ├── OdinPlayerVoice.cs     # プレイヤー音声処理
│   │   │   └── PlatformManager.cs     # プラットフォーム最適化
│   │   ├── Prefabs/
│   │   └── Audio/
│   │       └── MainAudioMixer.mixer   # AudioMixer設定
│   │
│   ├── TestApp/           # テストアプリケーション
│   │   ├── Scenes/
│   │   ├── Scripts/
│   │   │   ├── ChorusTestUI.cs        # UI制御
│   │   │   └── TestPlayerController.cs # プレイヤー制御
│   │   └── UI/
│   │
│   └── XR/               # XR/Quest対応
│       ├── Scripts/
│       │   └── XRVoiceController.cs   # XR音声制御
│       └── Prefabs/
```

## 使用方法

### 基本的な実装

```csharp
// ODIN への接続
OdinVoiceManager.Instance.SetAccessToken("your-access-token");
OdinVoiceManager.Instance.JoinRoom("room-name");

// 音量調整
OdinAudioRouter.Instance.SetMonitorVolume(0.8f);
OdinAudioRouter.Instance.SetBroadcastVolume(1.0f);

// プラットフォーム判定
if (PlatformManager.Instance.IsXRPlatform())
{
    // Quest向けの処理
}
```

### テストアプリの起動
1. TestApp/Scenes/ChorusTestScene を開く
2. ODIN アクセストークンを入力
3. ルームを作成または参加
4. 最大4人で同時接続可能

## AudioMixer 構成

```
MainAudioMixer
├── Master
├── MonitorBus        # 演者用モニター音声
│   ├── Player1_Monitor
│   ├── Player2_Monitor
│   ├── Player3_Monitor
│   └── Player4_Monitor
└── BroadcastBus      # 配信・録音用ミックス
    ├── Player1_Broadcast
    ├── Player2_Broadcast
    ├── Player3_Broadcast
    └── Player4_Broadcast
```

## プラットフォーム別最適化

### Quest 2
- フレームレート: 90/120 Hz
- オーディオ: 48kHz, 512バッファ
- CPU/GPU レベル: 2

### Quest 3
- フレームレート: 120 Hz
- オーディオ: 48kHz, 256バッファ
- CPU/GPU レベル: 3

### Quest Pro
- フレームレート: 90 Hz
- オーディオ: 48kHz, 256バッファ
- アイトラッキング対応
- Foveated Rendering 有効

### Windows PC
- フレームレート: 無制限
- オーディオ: 48kHz, 512バッファ
- 最高品質設定

## 料金目安（ODIN クラウド版）

- 1〜19,999 PCU: 0.29 €/PCU/月
- 開発用: 25 PCU まで無料

### 使用例
- 600人が月20%同時接続: 約35€/月（約5,600円）
- 3,000人が月10%同時接続: 約87€/月（約14,000円）

## トラブルシューティング

### 接続できない場合
- アクセストークンが正しく設定されているか確認
- ネットワーク接続を確認
- ファイアウォール設定を確認

### 音声が聞こえない場合
- AudioMixer の設定を確認
- マイク権限が許可されているか確認
- プラットフォーム設定が正しいか確認

### Quest でのビルドエラー
- Android SDK/NDK が正しくインストールされているか確認
- Oculus Integration が最新版か確認
- テクスチャ圧縮が ASTC になっているか確認

## ライセンス
このプロジェクトは MIT ライセンスです。
ODIN SDK は 4Players のライセンスに従います。

## サポート
- ODIN ドキュメント: https://docs.4players.io/
- Unity フォーラム: https://forum.unity.com/
- バグ報告: [Issues]

## 今後の展開予定
- [ ] Web版対応（WebRTC実装）
- [ ] 録音機能の追加
- [ ] エフェクト処理の拡充
- [ ] AIノイズ除去の実装
- [ ] より多人数での合唱対応