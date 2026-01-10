# ISSUE-SKIA-005-02 1ストローク分の入力収集（マウス）

## Goal
- `SubWindow` の描画領域でマウス入力（押下→移動→離す）から1ストローク分の点列を収集できる。

## Scope
- WPFのマウスイベントで点列を蓄積
- クリックのみ（移動なし）でも1点ストロークとして扱う

## Non-goals
- ペン/タッチ/筆圧の実取得
- スムージング/補間
- 消しゴム

## Coordinate Policy (B)
- 点列は「基準キャンバス（537x380 DIP）」座標として保持する。
- 表示サイズとの差は `ViewScale` として扱い、
  - 入力: 画面座標（DIP）→基準キャンバス座標（DIP）へ逆変換
  - 描画: 基準キャンバス座標（DIP）→画面へスケール

### ViewScale（安全策）
- `ViewScale = min(displayedWidthDip / 537, displayedHeightDip / 380)`
- 入力の逆変換は `basePointDip = displayedPointDip / ViewScale` を原則とする。

### 余白領域（letterbox/pillarbox）の扱い
- `ViewScale=min` のため、表示領域の縦横比が基準（537:380）と一致しない場合、描画領域の周囲に余白が発生する。
- 入力点は逆変換後に **基準キャンバス範囲へクランプ**する:
  - `x = clamp(x, 0, 537)`
  - `y = clamp(y, 0, 380)`
- 目的: 余白領域で入力しても保存点列が破綻しないようにする（描画/PNGは別Issueの通りクリップ）。

## Sampling
- 移動イベント毎に点を追加
- 点列肥大化防止のため最小距離閾値（例: 0.5 DIP）を適用する

## Acceptance Criteria
- ? マウス押下→移動→離しでストローク点列が収集される
- ? クリックのみ（移動なし）でも1点ストロークとして扱える
- ? `ViewScale` に基づく逆変換（displayed DIP → base DIP）で座標が基準キャンバスに揃う
- ? 取得点は基準キャンバス範囲へクランプされる
- ? 最小距離で間引きされる

## 実装メモ（現状）
- 入力は `InputCaptureLayer` で受け取り、`CardCanvasGrid` 基準の座標を `ToBaseCanvasDip(...)` で基準キャンバスDIPへ変換して保存する
- 収集データは `_currentStroke`（進行中）と `_strokes`（確定）に保持する
