# ISSUE-SKIA-010-01-02 属性と筆圧をv2へマッピングする

## Goal
- WinRT Ink APIで復元した`InkStroke`から得られる（または推定する）描画属性/筆圧をv2ストローク表現へマッピングできる

## Non-goals
- ISFの独自バイナリ解析
- Skia描画の同等品質担保（別Issue）
- t補完（別Issue）

## Scope
- tool（ペン/蛍光）
- color/opacity
- size
- pressure（取得できない場合の既定値/補完ルール）

## Acceptance Criteria
- v2ストロークに、少なくとも色と太さが入る
- 筆圧が無い場合でも破綻しない（既定値で描画可能）

## Files
- `Models/FileFormatV2/*` (modify if needed)
- 変換ユーティリティ (modify)

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`をロード
  2. 1枚以上のカードで色/太さの差が反映される（ログ/目視のどちらか）

## Risks
- 属性の完全一致は困難 - まず主要属性のみ対応し段階拡張する
