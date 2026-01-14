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
  - 実装: `canvas.SaveLayer(...)` + 蛍光ペン専用 `BlendMode`
  - alpha: 既存互換のため蛍光ペンは最大alphaを160に制限
  - `Bcf2Stroke.Opacity` をSkia描画で反映する（highlighter/通常ペン共通）
- 調整方針:
  - 重なりが濃くなりすぎる場合、`Multiply` ではなく `SrcOver` などへ寄せる（比較対象の6枚で確認しながら選定）。

### StrokeCap（ストローク端の形状）の方針
- `SKStrokeCap.Round` は端点が丸くなるため、点列をそのまま線分で結ぶ実装だと「端点が丸い塊」に見えることがある。
- 端点の塊感を抑えるため、キャップ形状自体を変えるのではなく、端点近傍のみ線幅を細くする（taper）を導入する。
  - 実装例: 先頭/末尾の数セグメント（例: 3）だけ線幅倍率を `0.35..1.0` で補間
  - これにより、capがRoundでも端点が自然に細って消える見え方に寄せられる
- 段階導入: 比較対象の6枚で確認し、taper区間や最小倍率を調整する。

## Acceptance Criteria
- 旧`.bcf`でPNGキャッシュを欠損させても、Skia描画したプレビューが生成され表示できる
- 主要属性（色・太さ）が反映される
- 蛍光ペンストロークが空PNGにならない

## Files
- `BrainCard/Legacy/LegacyPngRenderer.cs` (modify) - v2ストロークのPNG生成（highlighter合成、opacity反映、taper）
- Skia描画ユーティリティ (new|modify)
- `MainWindow.xaml.cs` もしくは `Card` 周辺（表示経路）(modify)

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`をロード
  2. PNGキャッシュを削除したカードが（Skia生成で）表示される
  3. highlighterを含むカードでもプレビューが生成される
  4. ストローク端の塊感が軽減されている

## Risks
- 見た目差分 - サンプルファイルを固定して比較し、BlendMode/alpha/opacity/stroke端点(taper)を段階導入する

---

## 現状メモ（2026-01）

### Hot Reload時の見た目差分について
- 既存カード表示は `*.bcf2.Assets\\<cardId>.png` のPNGキャッシュ参照のため、レンダラー（`LegacyPngRenderer` 等）をHot Reloadで変更しても、表示は即時反映されない。
- 開発時の差分確認は「PNGキャッシュ削除 → 再生成トリガ → 表示更新」が必要。

### キャッシュクリアUI（実装済み）
- `MainWindow` にキャッシュクリア用の `ui:SplitButton` を追加。
  - デフォルトクリック: PNGのみクリア
  - Flyout: PNGのみ / PNG+ノイズ（`PencilBrushTexture.ClearCache()`）
- 目的: Hot Reload後の描画調整を検証しやすくする（PNGキャッシュとPencilノイズの両レイヤーを明示的にクリア可能）。

### 次課題（未対応）
- Pencilストローク周囲に「描画方向に平行な線（ストライプ/バンディング）」が入るケースの解消。
  - 原因候補: ノイズテクスチャの生成/適用方法、アルファ8の量子化、ブレンド/フィルタ、ブラシスタンプ間隔。
  - 主要調査対象: `BrainCard/Legacy/LegacyPngRenderer.cs` と `BrainCard/Legacy/PencilBrushTexture.cs`
- `Bcf2Point.Pressure` に応じて描画の濃さ（不透明度）を減衰させる。
  - まずはペン/鉛筆のalphaを `pressure` で乗算する方針。
  - 併せて線幅の圧力対応を行うかは別途判断。
