# ISSUE-SKIA-007-02 v2ファイル保存経路を追加する（親Issue）

## Goal
- 現在のカード一覧（PNGキャッシュ参照＋v2ストローク保持）を `.bcf2`（v2 JSON）として保存できる

## Non-goals
- v2読込（`ISSUE-SKIA-007-01`）
- 旧`.bcf`形式での保存（Step 8で禁止へ / `ISSUE-SKIA-008-01`）
- 旧ISF→v2変換（Step 10 / `ISSUE-SKIA-010-01`）
- PNGをMainWindow側で生成する（責務はSubWindow）

## Approach
- 実装範囲が広く破壊しやすいため、本Issueは「親Issue」とし、以下の子Issueに分割して段階的に実装する。

## Child Issues
- `docs/issues/ISSUE-SKIA-007-02-01-saveas-ui.md` (SaveAsで`.bcf2`を選べるようにする)
- `docs/issues/ISSUE-SKIA-007-02-02-save-json.md` (v2 JSONの保存を実装する)
- `docs/issues/ISSUE-SKIA-007-02-03-save-assets-png.md` (Assets PNGの保存を実装する)

## Acceptance Criteria
- 上記子Issueが完了し、SaveAsで`.bcf2`が生成され、JSONとPNGキャッシュが揃う

## Files (expected)
- `MainWindow.xaml.cs` (modify) - SaveState/SaveAsのv2対応
- `Models/FileFormatV2/*` (modify if needed) - v2ルートモデル整備

## Validation
- build: `dotnet build`
- manual:
  1. カードを複数作成
  2. SaveAsで `.bcf2` を保存
  3. `<file>.Assets/<cardId>.png` が作成される
  4. JSON内に `cards` / `ink.strokes` が出力される

## Risks
- MainWindow.xaml.csが巨大 - 変更をSaveAs/SaveState周辺へ局所化する
- Assetsパスが未整理 - `.Assets`規約で統一する
