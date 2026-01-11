# ISSUE-SKIA-010-01-04 Skia描画で可能な限り同等品質を目指す

## Goal
- 復元したストロークをSkiaSharpで描画し、可能な限り従来と同等の見た目に近づける

## Non-goals
- UI編集機能の完全復旧
- 消しゴム（Step11）

## Scope
- strokeの描画（太さ/色/透明度/合成）
- 可能なら蛍光ペン相当の合成（blend）
- 画像化（bitmap）に必要な座標系/スケール整合

## Acceptance Criteria
- 旧`.bcf`でPNGキャッシュを欠損させても、Skia描画したプレビューが生成され表示できる
- 主要属性（色・太さ）が反映される

## Files
- Skia描画ユーティリティ (new|modify)
- `MainWindow.xaml.cs` もしくは `Card` 周辺（表示経路）(modify)

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`をロード
  2. PNGキャッシュを削除したカードが（Skia生成で）表示される

## Risks
- 見た目差分 - サンプルファイルを固定して比較し、改善を段階導入する
