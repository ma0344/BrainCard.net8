# ISSUE-005-05 新ストロークモデル（v2）へ反映できるようにする（まずはメモリ内）

## Goal
- `SubWindow` で収集した点列（DIP）と最小描画属性を、v2ストロークモデル（`Bcf2Stroke` / `Bcf2Point`）へメモリ内でマッピングできる

## Scope
- 1ストローク確定（離し）時に v2 ストロークを生成して保持する
- 属性は `ISSUE-005-04` の最小セット（単色・固定太さ・固定筆圧）を固定値で適用する

## Non-goals
- JSON保存/読込
- 旧ISF変換
- `t` の厳密な管理（最小到達点では簡易で可）

## Mapping
- 点列
  - `Point`（DIP） -> `Bcf2Point.X/Y`（DIP）
  - `Bcf2Point.Pressure` は固定値（例: 0.5）
  - `Bcf2Point.T` は最小到達点では仮の等間隔（例: 16ms刻み）
- 属性
  - `Bcf2Stroke.Tool`: `"pen"`
  - `Bcf2Stroke.Color`: `"#FF000000"`（黒）
  - `Bcf2Stroke.Size`: `2.0`（DIP）
  - `Bcf2Stroke.DeviceKind`: `"mouse"`

## 実装状況
- 現状: 達成（メモリ内）
  - `SubWindow` のストローク確定時に `Bcf2Stroke` を生成してリストへ保持している
  - Outputに `[V2] Stroke:` のログを出し、生成確認できる
  - `CanvasClear()` で保持している v2 ストロークもクリアする

## Files
- `SubWindow.xaml.cs` (modify)
- `Models/FileFormatV2/Bcf2Stroke.cs` (refer)
- `Models/FileFormatV2/Bcf2Point.cs` (refer)

## Validation
- manual:
  1. SubWindowのカード領域で1ストローク描く
  2. Outputに `[V2] Stroke:` ログが出て、`points=N` が `Input Up` の点数と一致する

## Risks
- `t` の定義が後続で変更になる - 後で `Stopwatch` 等へ差し替える前提で、まずは固定刻みで通す
