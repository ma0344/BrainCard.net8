# ISSUE-SKIA-008-01 保存時に新拡張子（.bcf2）へ強制する

## Goal
- 旧拡張子（`.bcf`）での保存・上書きを禁止し、保存時は常に `.bcf2` を提示/強制できる

## Non-goals
- v2保存/読込の実装そのもの（Step 7）
- 旧`.bcf`読込互換を削除する

## Scope
- UI/フィルタ/メッセージの更新
  - SaveAs: 既定フィルタを `.bcf2` にする
  - 上書き保存: 現在ファイルが `.bcf` の場合は SaveAs を強制する
- `vm.CurrentFileName` と WindowTitle 表示の整合

## Acceptance Criteria
- `.bcf` を開いている状態で「上書き保存」を押すと SaveAs に誘導される
- SaveAs の既定拡張子が `.bcf2` になる
- `.bcf` へ保存できない（UI上/コード上ともに）

## Files (expected)
- `MainWindow.xaml.cs` (modify)
- `ViewModels/MainWindowViewModel.cs` (modify if needed) - タイトル表記など
- ローカライズリソース（必要なら）

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`を開く
  2. 上書き保存→SaveAsへ誘導される
  3. SaveAsで`.bcf2`が保存される

## Risks
- 既存の未実装SaveStateと衝突 - 現状仕様（保存は未実装）を保ちつつ、拡張子の分岐だけ先行して入れる
