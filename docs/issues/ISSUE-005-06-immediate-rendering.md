# ISSUE-005-06 点列を即時描画できるようにする（スムージング不要）

## Goal
- `SubWindow` 上で収集した点列を、編集ビュー領域に即時に可視化できる

## Scope
- 収集中ストローク（ドラッグ中）と確定済みストローク（離した後）を描画する
- 最小属性（単色/固定太さ）は固定値で適用する

## Non-goals
- Win2D導入
- スムージング/補間/ベジェ
- 高品質レンダリング（アンチエイリアス、筆圧表現など）

## Design / Implementation Notes
- 最小到達点として、D3D11で2D線描画を作り込まず、子HWND上へ **GDI（WM_PAINT）** でオーバーレイ描画する
- `DxSwapChainHost.SetOverlayStrokes(IEnumerable<IEnumerable<Point>>)` にDIP点列を渡し、`InvalidateRect` で再描画する
- 座標はDIPで保持（ISSUE-005-03）。描画時にDPIスケールでPixelへ変換してGDIの座標へ渡す

## Acceptance Criteria
- マウスドラッグに追従して線が見える
- 離した後も線が残る（確定済みとして描画される）
- `Clear` で線が消える

## Files
- `Win2DHost/DxSwapChainHost.cs` (modify)
- `SubWindow.xaml.cs` (modify)

## Validation
- manual:
  1. SubWindowを開く
  2. カード領域をドラッグして線が見えることを確認
  3. 離しても線が残ることを確認
  4. Clearボタンで線が消えることを確認

## Risks
- GDIオーバーレイは最終形ではない - Win2D差し替えのための暫定経路として扱う
- 再描画タイミングによってちらつきが出る - 必要に応じてBeginPaint/EndPaintやダブルバッファ化を検討する
