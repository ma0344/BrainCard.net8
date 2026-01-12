# ISSUE-SKIA-010-01-04 Skia描画で可能な限り同等品質を目指す

## Goal
- v2ストローク（または抽出済みの中間表現）をSkiaSharpで描画し、可能な限り従来と同等の見た目に近づける

## Non-goals
- UI編集機能の完全復旧
- 消しゴム（Step11）

## Scope
- strokeの描画（太さ/色/透明度/合成）
- 可能なら蛍光ペン相当の合成（blend）
- 画像化（bitmap）に必要な座標系/スケール整合

## Design Notes
### Highlighter（蛍光ペン）の方針
- 旧（UWP/XAML Islands）では `InkStroke.DrawingAttributes.DrawAsHighlighter` を含む場合、`RenderTargetBitmap` で親要素（白背景）をレンダリングし、
  その後「白（閾値つき）→透明化」の後処理を行ってPNGキャッシュとして保存していた。
- ただし将来的にカード背景色を選択可能にする想定があるため、PNG生成で背景色を白で固定する方式は採用しない。
- Skia側では背景を固定せず、透過PNGのまま合成（BlendMode）で蛍光ペンの見た目を近似する。
  - 実装: `canvas.SaveLayer(...)` + 蛍光ペン専用 `BlendMode`（初期値は `Multiply`）
  - alpha: 既存互換のため蛍光ペンは最大alphaを160に制限
  - `Bcf2Stroke.Opacity` をSkia描画で反映する（高lighter/通常ペン共通）

## Acceptance Criteria
- 旧`.bcf`でPNGキャッシュを欠損させても、Skia描画したプレビューが生成され表示できる
- 主要属性（色・太さ）が反映される
- 蛍光ペンストロークが空PNGにならない

## Files
- `BrainCard/Legacy/LegacyPngRenderer.cs` (modify) - v2ストロークのPNG生成（highlighter合成、opacity反映）
- Skia描画ユーティリティ (new|modify)
- `MainWindow.xaml.cs` もしくは `Card` 周辺（表示経路）(modify)

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`をロード
  2. PNGキャッシュを削除したカードが（Skia生成で）表示される
  3. highlighterを含むカードでもプレビューが生成される

## Risks
- 見た目差分 - サンプルファイルを固定して比較し、BlendMode/alpha/opacityを段階導入する
