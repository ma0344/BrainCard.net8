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

## Parent / Child Issues
- `ISSUE-SKIA-008-01-01-saveas-bcf2-only` - SaveAsで`.bcf`を選べないようにする
- `ISSUE-SKIA-008-01-02-prevent-overwrite-bcf` - `.bcf`を開いているときの上書き保存をSaveAsへ誘導する
- `ISSUE-SKIA-008-01-03-title-default-bcf2` - 新規作成時の既定ファイル名とタイトル表記を`.bcf2`へ寄せる

## Notes (current)
- `MainWindow.xaml.cs` の `SaveAs_Click` は `DefaultExt = ".bcf2"` になっているが、Filterに`.bcf`が残っているためUI上で`.bcf`保存が可能
- `OverWrite_Click` は拡張子判定が無く、`.bcf`を開いている場合でもそのまま保存できてしまう
- 新規時の既定ファイル名は `Untitled.bcf` のため、v2強制方針とタイトル表示が不整合になりうる

## Status
- Closed

## Child Issue Status
- Closed: ISSUE-SKIA-008-01-01-saveas-bcf2-only
- Closed: ISSUE-SKIA-008-01-02-prevent-overwrite-bcf
- Closed: ISSUE-SKIA-008-01-03-title-default-bcf2

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
