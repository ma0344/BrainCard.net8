# ISSUE-SKIA-007-01-02 v2ドキュメントのデシリアライズを実装する

## Goal
- `.bcf2` を `Bcf2Document` として読み込み、最小限の妥当性チェック（format/version/cards null等）を行える

## Non-goals
- v2カードのUI復元（007-01-03）
- PNG/Assetsの読み込み（007-01-03）

## Scope
- `Newtonsoft.Json` で `Bcf2Document` をデシリアライズする
- 例外時は呼び出し側へエラー情報を返し、UIで握りつぶさない

## Acceptance Criteria
- 正常な`.bcf2`で`Bcf2Document`が取得できる
- 不正なJSON/フォーマットの場合にクラッシュしない（例外ハンドリングされる）

## Files
- `MainWindow.xaml.cs` (modify) - v2読み込み用の内部メソッド追加
- `Models/FileFormatV2/*` (modify|n/a) - 既存モデルで不足があれば補完

## Validation
- build: `dotnet build`
- manual:
  1. `.bcf2` を指定してロード
  2. 例外が未処理で落ちない（ログ/メッセージで確認できる）

## Status
- Closed

## Implementation Notes
- `MainWindow.xaml.cs`: `LoadBcf2DocumentAsync` を追加（`.bcf2`→`Bcf2Document`）
- `MainWindow.xaml.cs`: `ValidateBcf2Document` を追加（format/version/cards null、card.id必須の最小チェック）

## Validation Results
- build: `dotnet build` (pass)
- manual: 壊れたJSONの `.bcf2` で未処理例外により落ちないことを確認（007-01-04でユーザー通知も追加）
