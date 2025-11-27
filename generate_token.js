const odinTokens = require("@4players/odin-tokens");

// アクセスキーからルームトークンを生成
const accessKey = "あなたの44文字のアクセスキーをここに入力";
const roomId = "TestRoom";
const userId = "user123";

async function generateRoomToken() {
    try {
        // アクセスキーを読み込み
        console.log("Access Key:", accessKey);

        // TokenGeneratorを使用してルームトークンを生成
        const generator = new odinTokens.TokenGenerator(accessKey);

        const roomToken = await generator.createToken(roomId, userId);

        console.log("\nGenerated Room Token for Unity:");
        console.log(roomToken);

        // .envファイル用の出力
        console.log("\nAdd this to your .env file:");
        console.log(`ODIN_ACCESS_TOKEN=${roomToken}`);

        return roomToken;
    } catch (error) {
        console.error("Error generating token:", error.message);
        console.error("Stack:", error.stack);
    }
}

generateRoomToken();