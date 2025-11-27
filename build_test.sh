#!/bin/bash

echo "Building Unity test app..."

# Unity 6のパス
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.62f1/Unity.app/Contents/MacOS/Unity"

# プロジェクトパス
PROJECT_PATH="/Users/zin/OdinVoiceProject"

# ビルド出力パス
BUILD_PATH="$PROJECT_PATH/Builds/Mac/OdinVoiceTest.app"

# ビルドコマンド
"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -buildOSXUniversalPlayer "$BUILD_PATH" \
  -logFile -

echo "Build complete. Starting app..."
open "$BUILD_PATH"