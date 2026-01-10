# ISSUE-SKIA-007-02-01 SaveAsで.bcf2を選べるようにする

## Goal
- SaveAsダイアログで `.bcf2` を既定にし、ユーザーがv2保存を選択できる状態にする

## Non-goals
- v2 JSONの保存実装（`ISSUE-SKIA-007-02-02`）
- Assets PNGの保存実装（`ISSUE-SKIA-007-02-03`）
- 旧`.bcf`の保存禁止（`ISSUE-SKIA-008-01`）

## Scope
- `SaveAs_Click` のダイアログ設定のみ
  - Filterに `.bcf2` を追加
  - DefaultExt を `.bcf2` にする
  - AddExtension を有効化する

## Acceptance Criteria
- SaveAsダイアログで `.bcf2` が先頭に表示され、既定拡張子が `.bcf2` になる
- ビルドが通る

## Files
- `MainWindow.xaml.cs` (modify) - `SaveAs_Click` のフィルタ/拡張子

## Validation
- build: `dotnet build`
- manual:
  1. SaveAsを開く
  2. 既定が`.bcf2`になっている
  3. `.bcf2` を指定してファイルが作成される（中身は未実装でも可）

## Risks
- MainWindow.xaml.csが巨大 - SaveAsブロックのみ最小差分で編集する
