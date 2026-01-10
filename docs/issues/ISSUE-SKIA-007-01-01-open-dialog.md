# ISSUE-SKIA-007-01-01 ロード入口の拡張子分岐を追加する

## Goal
- ロード操作で`.bcf2`を選択した場合にv2読込経路へ入り、`.bcf`は既存のlegacy読込経路へ入る

## Non-goals
- v2のデシリアライズ実装（007-01-02）
- v2カードの復元（007-01-03）
- UI/エラーハンドリング改善（007-01-04）

## Scope
- `OpenFileDialog` のFilterを `.bcf2` と `.bcf` の両方に対応させる
- `LoadStateAsync` の入口で拡張子により処理を分岐する

## Acceptance Criteria
- `.bcf2` を選ぶとv2経路に分岐する
- `.bcf` を選ぶと既存経路に分岐する

## Files
- `MainWindow.xaml.cs` (modify) - `LoadButton_Click`/`LoadStateAsync` の分岐追加

## Validation
- build: `dotnet build`
- manual:
  1. ロードダイアログで`.bcf2`が選択できる
  2. `.bcf`も従来通り選択できる

## Status
- Closed

## Implementation Notes
- `MainWindow.xaml.cs`: `OpenFileDialog` のFilterを `.bcf2`/`.bcf` 両対応に更新
- `MainWindow.xaml.cs`: `LoadStateAsync` で拡張子分岐を追加（`.bcf2`→v2、`.bcf`→legacy）

## Validation Results
- build: `dotnet build` (pass)
- manual: Loadダイアログで `.bcf2`/`.bcf` が選択できることを確認
