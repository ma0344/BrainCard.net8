# ISSUE-SKIA-008-01-01 SaveAsで`.bcf`を選べないようにする

## Goal
- SaveAs（名前を付けて保存）でユーザーが`.bcf`を選択できず、保存先は必ず`.bcf2`になる

## Non-goals
- 上書き保存（OverWrite）の挙動変更（別Issue）
- v2保存処理の中身の変更（`SaveState`の内容）

## Scope
- `SaveAs_Click` の `SaveFileDialog` を `.bcf2` のみ提示する
  - `Filter` を `.bcf2` のみにする
  - `DefaultExt` を `.bcf2` にする
  - `AddExtension=true` を維持する
- 必要なら `FileName` 初期値を `.bcf2` に正規化する（ただし全体の初期名変更は別Issue）

## Acceptance Criteria
- SaveAsで`.bcf2`以外が選べない
- SaveAsで`.bcf2`が保存される

## Files
- `MainWindow.xaml.cs` (modify)

## Validation
- build: `dotnet build`
- manual:
  1. SaveAsを開く
  2. フィルタに`.bcf2`のみが表示される
  3. 拡張子なしで保存しても`.bcf2`が付与される

## Risks
- 既存の`.bcf`保存導線が残る - OverWrite側での強制は別Issue（008-01-02）で対処する

## Status
- Closed

## Implementation Notes
- `MainWindow.xaml.cs`: `SaveAs_Click` の `SaveFileDialog.Filter` を `.bcf2` のみに変更
- `MainWindow.xaml.cs`: `.bcf` を開いた後のSaveAsで `xx.bcf.bcf2` にならないよう初期FileNameを正規化

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
