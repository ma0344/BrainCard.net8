# ISSUE-SKIA-007-01-03 v2カードの復元処理を実装する

## Goal
- `Bcf2Document.cards` から `MainWindow` 上へ `Card` を復元し、位置・Z・テキスト・PNG表示が復元される

## Non-goals
- SubWindowでのストローク再編集表示
- PNGが存在しないカードのプレビュー生成（後段）

## Scope
- `Bcf2Card.id/x/y/z/recognizedText` の反映
- `Bcf2Card.previewPng` が指す Assets PNG を `TryLoadPng` で読み込み、`CardImage.Source` に設定
- `Bcf2Card.ink.strokes` は `Card.SetV2Strokes(...)` または `Card.V2Strokes` に保持（描画しない）
- PNGが無い場合は暫定でスキップ（カードを作らない）

## Acceptance Criteria
- `.bcf2` をロードするとPNGがあるカードがキャンバスに復元される
- カードの順序（Z）が復元される

## Files
- `MainWindow.xaml.cs` (modify) - v2の復元実装

## Validation
- build: `dotnet build`
- manual:
  1. 既存の`.bcf2`をロード
  2. `<file>.Assets`のPNGが表示され、位置が復元される

## Status
- Closed

## Implementation Notes
- `MainWindow.xaml.cs`: `.bcf2` ロード時に `Bcf2Document.cards` から `Card` を復元
- PNGが無いカードは暫定でスキップ（後段で生成）
- `Bcf2Card.ink.strokes` は `Card.SetV2Strokes(...)` で保持（描画はしない）

## Validation Results
- build: `dotnet build` (pass)
- manual: 全てのカードが元の配置で復元されることを確認
