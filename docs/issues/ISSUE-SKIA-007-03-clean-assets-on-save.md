# ISSUE-SKIA-007-03 v2保存時にAssetsキャッシュをクリーンアップする

## Goal
- 新規保存（SaveAs）で既存の`.bcf2`ファイルへ保存した場合でも、`<file>.Assets` が現状のカード一覧と一致する状態に更新される

## Background
- 現状の `SaveState` は、カードごとにPNGを `<file>.Assets/<cardId>.png` へ上書き保存する
- ただし「以前存在したカードのPNG」が削除されないため、SaveAsで既存`.bcf2`を選んで保存すると、`<file>.Assets` 内に不要なPNGキャッシュが残留する

## Non-goals
- `.bcf`（legacy）保存の変更
- PNG未生成カードのプレビュー生成
- v2 JSONスキーマ変更

## Scope
- 保存時、`CardList` の cardId 集合を正とし、`<file>.Assets` 直下の `*.png` のうち不要なものを削除する
- 安全のため、サブディレクトリは対象外とする（直下のみ）
- `previewPng` が `"<cardId>.png"` である前提の範囲で実施（現状実装に合わせる）

## Acceptance Criteria
- SaveAsで既存`.bcf2`ファイルを指定して保存した場合、`<file>.Assets` に削除済みカードのPNGが残らない
- 既存カードのPNGは保存後も表示できる
- アプリがクラッシュしない（失敗時はログ/ステータス）

## Files
- `MainWindow.xaml.cs` (modify) - `SaveState` のAssetsクリーンアップ

## Validation
- build: `dotnet build`
- manual:
  1. 既存の`.bcf2`を開く
  2. カードを削除して SaveAs で同じ`.bcf2` を選択して保存する
  3. `<file>.Assets` に削除済み cardId のPNGが残っていないことを確認する

## Risks
- 誤削除 - 直下のpngのみを対象にし、ファイル名が `"<cardId>.png"` 形式で一致するもの以外は削除しない

## Status
- Closed

## Validation Results
- build: `dotnet build` (pass)
- manual: SaveAsで既存`.bcf2`を指定して保存した際に、`<file>.Assets`に不要PNGが残らないことを確認
