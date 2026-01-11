# ISSUE-SKIA-010-01-03 t（時間）を補完する

## Goal
- v2ストロークの点列に `t`（相対ms、開始=0）を付与できる

## Non-goals
- 速度推定の高精度化（初期は単純補完で可）

## Scope
- ISFに時刻が含まれない/取得できない場合の補完ルールを実装
  - 例: 点ごとに16ms刻み、またはストローク長に応じた配分
- `t`は単調増加であること

## Acceptance Criteria
- `t`が無いケースでも、単調増加の相対msが埋まる

## Files
- 変換ユーティリティ (modify)

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`をロード
  2. 生成されたv2ストロークで `t` が単調増加になっていることを確認

## Risks
- 補完品質 - 初期は等間隔で許容し、必要なら後続Issueで改善する
