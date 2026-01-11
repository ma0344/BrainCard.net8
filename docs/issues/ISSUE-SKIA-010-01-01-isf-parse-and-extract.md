# ISSUE-SKIA-010-01-01 ISF bytesからストローク情報を抽出する

## Goal
- `Card.LegacyInk.IsfBytes`（ISF）から、Skia描画とv2ストローク化に必要な最小データ（点列と基本属性）を抽出できる

## Non-goals
- 完全な同等品質（別Issueで段階導入）
- PNG生成（別Issue）
- t補完（別Issue）

## Scope
- 入力: ISF bytes
- 出力: 中間表現（例: strokes[] with points(x,y[,pressure]) + basic attributes）
- 異常系: 解析失敗はカード単位でスキップし、全体は落とさない

## Acceptance Criteria
- 解析可能なISFについて、1ストローク以上が抽出される
- 解析不能なISFが混在してもアプリが落ちない

## Files
- `BrainCard/Legacy/*` もしくは `Models/*` に解析ユーティリティを追加 (new|modify)

## Validation
- build: `dotnet build`
- manual:
  1. ISFを含む旧`.bcf`をロード
  2. 1枚以上のカードで「抽出ストローク数>0」をログ等で確認

## Risks
- ISF仕様差分 - まず最小サポートに絞り、非対応は明示して回収する
