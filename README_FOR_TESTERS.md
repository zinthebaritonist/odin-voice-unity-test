# ODIN Voice Chat テストプロジェクト - テスター向けガイド

## 🚀 5分で動作確認できます！

### 必要なもの
- Unity 2019.4以降（Unity Hub からインストール）
- マイク（内蔵でOK）
- Git

### セットアップ手順

#### 1. プロジェクトをクローン
```bash
git clone [このリポジトリのURL]
cd OdinVoiceProject
```

#### 2. Unity で開く
1. Unity Hub → 「追加」
2. OdinVoiceProject フォルダを選択
3. Unity バージョンは 2019.4 以降を選択

#### 3. 実行してテスト
1. プロジェクトが開いたら **Play ボタン** ▶️ を押すだけ！
2. 自動的に「TestRoom」に接続されます
3. Console に `Join room command sent` が出れば成功

### 複数人でのテスト方法

#### 簡単な方法：同じPCで2つ起動
1. Unity エディタで Play
2. File → Build & Run でもう1つ起動
3. 両方が同じ TestRoom に接続される
4. マイクに話しかけてテスト

#### 別のPCでテスト
- 同じ手順でプロジェクトを開いて実行
- 自動的に同じルームに接続されます

### トークンについて
テスト用トークン（25人まで同時接続可）がすでに設定済み：
```
WzXiMkqgXJf0wsjLmBaITsB3kMXQZnoL9HwG2XQC79e98193
```

### よくある質問

**Q: エラーが出る**
A: Safe Mode のダイアログが出たら「Ignore」を選択

**Q: 音が聞こえない**
A:
- マイクの権限を確認（Mac: システム設定 → プライバシー）
- 2つ目のウィンドウで音を確認

**Q: ルーム名を変えたい**
A: Hierarchy → OdinManager → Inspector → Room Name を変更

### フィードバック
- 接続できたか
- 音声の遅延はどうか
- エラーが出た場合の内容

を教えてください！