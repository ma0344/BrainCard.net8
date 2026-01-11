# ISSUE-SKIA-008-01-03 新規作成時の既定ファイル名とタイトル表記を`.bcf2`へ寄せる

## Goal
- 新規ファイル状態の既定ファイル名/タイトル表示が`.bcf2`前提になり、保存導線（v2強制）と矛盾しない

## Non-goals
- 保存ダイアログのフィルタ変更（別Issue）
- 上書き保存の強制（別Issue）

## Scope
- `MainWindow` 初期化時の `vm.CurrentFileName` 既定値を `Untitled.bcf2` にする
- `MainWindowViewModel.WindowTitle` のフォールバック名も `Untitled.bcf2` にする

## Acceptance Criteria
- 起動直後のタイトルが `Untitled.bcf2` 系になる
- 新規→SaveAsで`.bcf2`が自然に保存できる

## Files
- `MainWindow.xaml.cs` (modify)
- `ViewModels/MainWindowViewModel.cs` (modify)

## Validation
- build: `dotnet build`
- manual:
  1. 起動直後のタイトルを確認する
  2. 新規のままSaveAsを開き、`.bcf2`で保存できる

## Risks
- 既存ドキュメント/スクショとの不一致 - UIの統一を優先し、変更はファイル名のみとする

## Status
- Closed

## Implementation Notes
- `MainWindow.xaml.cs`: 新規時の既定ファイル名を `Untitled.bcf2` に変更
- `ViewModels/MainWindowViewModel.cs`: `WindowTitle` のフォールバック名を `Untitled.bcf2` に変更

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
