# ISSUE-SKIA-007-02-02 v2 JSONを保存する

## Goal
- 現在のカード一覧から `Bcf2Document` を組み立て、`.bcf2`（JSON）として保存できる

## Non-goals
- Assets PNGの保存（`ISSUE-SKIA-007-02-03`）
- v2読込（`ISSUE-SKIA-007-01`）
- 旧`.bcf`保存禁止（`ISSUE-SKIA-008-01`）

## Scope
- `Models/FileFormatV2` の `Bcf2Document/Bcf2Card/Bcf2InkData` を使用
- 保存対象
  - `id, x, y, z, recognizedText`
  - `ink.strokes`（Cardが保持するv2ストローク）
  - `canvas`（`cardWidth/cardHeight`, dpi=96）

## Acceptance Criteria
- SaveAsで`.bcf2`を保存するとJSONが出力され、`cards` が空でない
- 各カードに `ink.strokes` が出力される（空でも良いが、存在はする）
- ビルドが通る

## Files
- `MainWindow.xaml.cs` (modify) - `SaveState` をv2 JSON保存へ

## Validation
- build: `dotnet build`
- manual:
  1. カードを作成（Keep）
  2. SaveAsで`.bcf2`を保存
  3. 生成されたJSONを開き `cards` を確認

## Risks
- Cardのストローク未保持 - 空配列で保存し後段で埋める
