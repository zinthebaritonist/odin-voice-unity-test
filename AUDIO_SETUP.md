# 🎧 ODIN Voice Chat オーディオ設定ガイド

## クイック設定（3ステップ）

### ステップ1: デバッグUIを追加
1. Unity Hierarchyで右クリック → **Create Empty**
2. 作成したGameObjectに **OdinDebugUI** コンポーネントを追加
3. Playボタンで実行

### ステップ2: TABキーでパネル表示
- **TABキー**を押してデバッグパネルを開く
- リアルタイムで接続状態とデバイス設定を確認

### ステップ3: マイク/スピーカー調整
デバッグパネルで以下を調整：
- マイクデバイス選択
- 音声レベル確認
- ミュート/アンミュート

---

## 詳細設定

### 🎤 マイク設定

#### Unity Project Settings
```
Edit → Project Settings → Audio
```
- **System Sample Rate**: 48000
- **DSP Buffer Size**: Best latency
- **Max Real Voices**: 32

#### OdinHandler Inspector設定
`OdinHandler` GameObjectを選択：
- **Voice Activity Detection**: ✅ 有効
- **VAD Sensitivity**: 3 (1-5で調整)
- **Noise Suppression**: ✅ 有効

### 🔊 スピーカー設定

#### AudioMixer設定
`Assets/OdinCore/Audio/MainAudioMixer.mixer` で調整：
- **Master Volume**: 0 dB
- **Voice Volume**: 0 dB
- **Effects Volume**: -10 dB

### 📊 推奨設定値

| 項目 | 推奨値 | 説明 |
|------|--------|------|
| Sample Rate | 48000 Hz | 高音質 |
| Buffer Size | Best latency | 最低遅延 |
| VAD Sensitivity | 3 | バランス型 |
| Volume Scale | 1.0 | 標準音量 |

### 🐛 トラブルシューティング

#### 音が聞こえない
1. マイクの権限を確認（macOS: システム設定 → プライバシー）
2. デバッグUIで接続状態を確認
3. Input Levelバーが動いているか確認

#### エコーが発生する
1. ヘッドフォン使用を推奨
2. Noise Suppressionを有効化
3. マイク感度を下げる（VAD Sensitivity: 2）

#### 遅延が大きい
1. DSP Buffer Size → Best latency
2. ネットワーク接続を確認
3. 他のアプリを終了

### 📱 デバイス別設定

#### Windows
- デフォルト設定で動作
- Realtek Audio Consoleで詳細調整可能

#### macOS
- システム設定 → サウンド で入出力デバイス選択
- Audio MIDI設定で詳細調整

#### Quest 2/3
- Quest本体の音量ボタンで調整
- Unityで`XR Audio Settings`を確認

---

## デバッグUIの使い方

### 基本操作
- **TABキー**: パネル表示/非表示
- **Connect/Disconnect**: ルーム接続/切断
- **Refresh Devices**: デバイスリスト更新

### 表示情報
- 📡 **Connection Status**: 接続状態
- 🎤 **Microphone Settings**: マイク設定
- 📊 **Input Level**: 入力レベル
- 📋 **Debug Log**: イベントログ

### よくある質問

**Q: マイクが認識されない**
A: Unity再起動 → マイク権限確認 → Refresh Devices

**Q: 特定のマイクを使いたい**
A: デバッグUIでマイク名をクリックして切り替え

**Q: 音量調整はどこ？**
A: AudioMixer または OdinHandler の Volume Scale

---

更新日: 2024/11/27