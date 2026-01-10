# ISSUE-SKIA-011-01 消しゴムをストローク削除として実装する

## Goal
- SubWindow上で消しゴム入力を検出し、ヒットしたストロークを「ストローク単位で削除」できる

## Non-goals
- 部分消去
- Undo/Redo
- 操作ログ

## Scope
- 入力判定
  - 右クリック押下/ペン裏（判定可能な範囲）
- ヒット判定
  - 点列への距離判定、もしくは簡易的にバウンディングボックス判定
- 削除
  - `_strokes` / `_v2Strokes` の両方から対象ストロークを削除

## Acceptance Criteria
- 消しゴム操作で既存ストロークが削除される
- Keepで削除結果がCardへ反映される（v2ストロークが更新される）

## Files (expected)
- `SubWindow.xaml.cs` (modify)

## Validation
- build: `dotnet build`
- manual:
  1. 複数ストロークを描画
  2. 消しゴム操作で任意のストロークが消える
  3. Keepしてカードのv2ストロークが減っている

## Risks
- ヒット判定の誤爆 - まずは保守的判定（閾値小さめ）で導入し、調整可能にする
