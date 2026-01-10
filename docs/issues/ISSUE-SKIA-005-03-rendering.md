# ISSUE-SKIA-005-03 点列を即時描画できるようにする（Skia）

## Goal
- `SubWindow` 上で収集した点列を `SKElement` に即時描画できる。

## Scope
- 収集中ストロークと確定済みストロークを描画
- 最小属性（単色/固定太さ/固定筆圧）は固定値で適用
- 表示サイズ変更時に見た目が追従する（倍率スケール）

## Non-goals
- 高品質レンダリング（スムージング、筆圧カーブ、ブラシ表現）

## Rendering Policy
- 座標系: 基準キャンバス（537x380 DIP）
- 表示倍率（安全策）: `ViewScale = min(displayedWidthDip / 537, displayedHeightDip / 380)`
- 描画領域: `(537 * ViewScale) x (380 * ViewScale)`（DIP）
  - 表示領域が基準比率と一致しない場合は余白（letterbox/pillarbox）が発生する
- 太さ: `widthDip * ViewScale` と同率でスケール
- クリップ: 描画もPNGも 537x380 の範囲でクリップ（範囲外は切り捨て）

## Acceptance Criteria
- マウスドラッグに追従して線が見える
- 離した後も線が残る
- Clearで線が消える

## Files (expected)
- `SubWindow.xaml.cs` (modify)
