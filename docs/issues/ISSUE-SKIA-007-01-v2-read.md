# ISSUE-SKIA-007-01 v2ファイル読込経路を追加する

## Goal
- `.bcf2`（v2 JSON）を読み込み、`MainWindow` にカード一覧（PNG表示）として復元できる

## Non-goals
- 旧`.bcf`（legacy）読込（Step 9）
- 旧ISF→v2変換（Step 10）
- SubWindowで既存ストロークを再編集表示する

## Parent / Child Issues
本Issueは親Issueとして扱い、実装は子Issueに分割して順に進める。

- `ISSUE-SKIA-007-01-01-open-dialog`（ロード入口の拡張子分岐を追加）
- `ISSUE-SKIA-007-01-02-deserialize`（v2ドキュメントのデシリアライズを実装）
- `ISSUE-SKIA-007-01-03-restore-cards`（v2カードの復元処理を実装）
- `ISSUE-SKIA-007-01-04-load-ui`（ロードUIとエラーハンドリングを調整）

## Scope
- v2 JSON のデシリアライズ（Newtonsoft.Json継続）
- v2のカードメタデータ（id/x/y/z/recogText）読込
- Assets 配下のPNG（`(<file>.Assets/<cardId>.png)`）が存在する場合はそれを表示に使う
- PNGが無い場合の扱いは「未表示/スキップ」を暫定許容（後段のPNG生成責務整理に合わせる）

## Data Contract (expected)
- v2ファイルは「ファイル→カード→ストローク→点」の階層
- 本Issueではカード一覧復元が目的のため、ストロークは「保持して読める」までで可（描画/再編集は別Issue）

## Acceptance Criteria
- `.bcf2` を開くとカードが復元される（位置・Z順・テキストが復元）
- PNGキャッシュが存在するカードは画像が表示される
- 読み込み失敗時にアプリがクラッシュしない（エラー表示/ログのどちらか）

## Files (expected)
- `MainWindow.xaml.cs` (modify) - Open/Loadの入口追加
- `Models/FileFormatV2/*` (modify if needed) - v2ルートモデル整備

## Validation
- build: `dotnet build`
- manual:
  1. `.bcf2` を選択してロード
  2. 既存カードが復元される

## Risks
- v2モデル未整備 - 最小のルートモデルを追加/補完して対応
- PNGキャッシュ不在 - スキップ方針を明記し、後段で生成対応
