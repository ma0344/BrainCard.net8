# ISSUE-SKIA-007-02 v2ファイル保存経路を追加する

## Goal
- 現在のカード一覧（PNGキャッシュ参照＋v2ストローク保持）を `.bcf2`（v2 JSON）として保存できる

## Non-goals
- 旧`.bcf`形式での保存（Step 8で禁止へ）
- 旧ISF→v2変換（Step 10）
- PNGをMainWindow側で生成する（責務はSubWindow）

## Scope
- v2 JSON のシリアライズ（Newtonsoft.Json継続）
- 保存対象:
  - `cardId`
  - `x,y,z`
  - `recogText`
  - `strokes`（Cardが保持するv2ストローク）
- Assets運用:
  - `(<bcf2Path>.Assets/<cardId>.png)` を参照/保持する前提
  - 保存時にPNGの再生成は行わない（存在するPNGを使う）

## Acceptance Criteria
- Save/SaveAsで `.bcf2` が作成される
- 再起動して `.bcf2` を読み込むと、カードの位置・順序・テキスト・（保持していた）ストロークデータが復元できる

## Files (expected)
- `MainWindow.xaml.cs` (modify) - SaveState/SaveAsのv2対応
- `Models/FileFormatV2/*` (modify if needed) - v2ルートモデル整備

## Validation
- build: `dotnet build`
- manual:
  1. カードを複数作成
  2. SaveAsで `.bcf2` を保存
  3. アプリ再起動
  4. `.bcf2` をロードして復元

## Risks
- v2ストロークがCardに無いカードが混在 - 空配列として保存し、後段の旧ISF変換で埋める
- Assetsパスが未整理 - 保存側は相対運用（`.Assets`規約）を徹底
