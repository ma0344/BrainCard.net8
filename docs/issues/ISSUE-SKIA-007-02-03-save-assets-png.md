# ISSUE-SKIA-007-02-03 Assets PNGを保存する

## Goal
- SaveAs時に `<file>.Assets/<cardId>.png` を作成し、カードのプレビューPNGキャッシュを保存できる

## Non-goals
- PNG生成の責務をMainWindowへ移す（画像生成はSubWindow責務）
- v2読込（`ISSUE-SKIA-007-01`）

## Scope
- 既にカードが保持している `CardImage.Source`（`BitmapSource`）をPNGとして保存する
- 出力先規約は既存の `.Assets` を踏襲する

## Acceptance Criteria
- SaveAs後に `<file>.Assets` フォルダが作成され、カード数分のPNGが出力される
- ビルドが通る

## Files
- `MainWindow.xaml.cs` (modify) - `SaveState` にPNG保存を追加

## Validation
- build: `dotnet build`
- manual:
  1. 複数カードを作成
  2. SaveAsで`.bcf2`保存
  3. `<file>.Assets` に `<cardId>.png` が生成される

## Risks
- CardImage.SourceがBitmapSourceでない - 保存をスキップしようにする
