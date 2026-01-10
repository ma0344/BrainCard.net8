# ISSUE-SKIA-007-01-04 ロードUIとエラーハンドリングを調整する

## Goal
- v2読込中にロードオーバーレイが表示され、失敗時にクラッシュせずユーザーに失敗が分かる

## Non-goals
- 詳細な例外ダイアログ/ログ基盤の刷新

## Scope
- v2読込でも既存の`SetLoadingUi`を使用する
- 例外時にステータスバー等へ失敗表示を行う

## Acceptance Criteria
- `.bcf2` 読込中にオーバーレイが表示される
- 不正ファイルでもアプリが落ちず、失敗が分かる

## Files
- `MainWindow.xaml.cs` (modify)

## Validation
- build: `dotnet build`
- manual:
  1. 大きめの`.bcf2`読込でオーバーレイが出る
  2. 壊れたJSONの`.bcf2`で失敗表示が出る
