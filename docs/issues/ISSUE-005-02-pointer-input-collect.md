# ISSUE-005-02 1ストローク分の入力収集（マウス）

## Goal
- マウス入力（押下→移動→離す）から1ストローク分の点列を収集できる

## Scope
- `SubWindow`上の編集ビュー領域でポインタ入力を受け取る
- 1ストロークを「押下→離す」の区間として点列を蓄積する

## Non-goals
- ペン/タッチ/筆圧の取得（一定値で代替する）
- スムージング/補間
- 消しゴム

## Design Notes
- 座標系（DIP/Pixelの関係、DPI変換責務）は `ISSUE-005-03` を参照する
- 収集した点列は当面メモリ内に保持し、直後に描画へ渡せる形にする

## Acceptance Criteria
- 押下でストローク開始、移動で点追加、離すでストローク確定ができる
- クリックのみ（移動なし）でも最小1点として扱える（本実装では1点扱い）

## 実装状況
- 現状: 達成
  - `DxSwapChainHost` が子HWNDの `WM_LBUTTONDOWN/WM_MOUSEMOVE/WM_LBUTTONUP` を受け取り、`PointerDown/Move/Up` として `SubWindow` へ通知
  - `SubWindow` が点列をメモリ内に蓄積し、ログで確認できる

## Files
- `SubWindow.xaml.cs` (modify)

## Validation
- manual: マウスで描画領域をドラッグしてイベントが発火し、点列が蓄積されることをログ等で確認できる

## Risks
- ホスト方式によって入力イベントの取り方が変わる - ISSUE-005-01の方式に追従する
