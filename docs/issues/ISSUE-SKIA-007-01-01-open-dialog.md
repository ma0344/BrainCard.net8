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
