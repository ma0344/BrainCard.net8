# ISSUE-SKIA-010-01-01 ISF bytesをWinRTで復元してストローク情報を抽出する

## Goal
- `Card.LegacyInk.IsfBytes`（ISF）をWinRT Ink APIで`InkStrokeContainer`へ復元し、Skia描画とv2ストローク化に必要な最小データ（点列と基本属性）を抽出できる

## Non-goals
- ISFの独自バイナリ解析
- 完全な同等品質（別Issueで段階導入）
- PNG生成（別Issue）
- t補完（別Issue）

## Scope
- 入力: ISF bytes（`Card.LegacyInk.IsfBytes`）
- 復元: `InkStrokeContainer.LoadAsync()`
- 出力: 中間表現（例: strokes[] with points(x,y[,pressure]) + basic attributes）
- 異常系: 復元/抽出失敗はカード単位でスキップし、全体は落とさない

## Acceptance Criteria
- 復元可能なISFについて、1ストローク以上が抽出される
- 復元不能なISFが混在してもアプリが落ちない

## Files
- 変換ユーティリティ (new|modify)

## Validation
- build: `dotnet build`
- manual:
  1. ISFを含む旧`.bcf`をロード
  2. 1枚以上のカードで「抽出ストローク数>0」をログ等で確認

## Risks
- WinRT復元に依存 - 失敗カードはスキップしてログを残す
