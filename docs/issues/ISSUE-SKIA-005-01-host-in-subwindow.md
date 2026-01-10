# ISSUE-SKIA-005-01 SubWindow上でSkia描画ビュー（SKElement）をホストする

## Goal
- WPF `SubWindow` 内に `SkiaSharp.Views.WPF.SKElement` を配置し、描画コールバックが動作する状態にする。

## Scope
- `SKElement` の配置
- `PaintSurface` が呼ばれ、背景クリア程度の描画が行えること

## Non-goals
- 入力（マウス/ペン）
- ストロークモデル
- PNG生成

## Notes
- XAML Islands / `MyUWPApp` を前提にしない（SkiaはWPFコントロールとして動作する）。
- x64を正式サポートとし、まずは `SKElement` を優先する。

## Acceptance Criteria
- `SubWindow` を表示すると描画領域が表示される（単色クリアでも可）
- リサイズでクラッシュしない

## Files (expected)
- `BrainCard.csproj` (modify) - NuGet追加が必要な場合
- `SubWindow.xaml` (modify) - `SKElement` 配置
- `SubWindow.xaml.cs` (modify) - 初期化/イベント
