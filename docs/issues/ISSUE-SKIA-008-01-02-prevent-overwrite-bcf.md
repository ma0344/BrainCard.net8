# ISSUE-SKIA-008-01-02 `.bcf`を開いているときの上書き保存をSaveAsへ誘導する

## Goal
- 現在開いているファイルが`.bcf`の場合に「上書き保存」を実行しても、`.bcf`には保存せずSaveAsへ誘導される

## Non-goals
- `.bcf`読込互換の削除
- v2保存処理の中身の変更（`SaveState`の内容）

## Scope
- `OverWrite_Click` で `vm.CurrentFileName` の拡張子を判定する
  - `.bcf` の場合は `SaveAs_Click` を呼ぶ（または同等の導線）
  - `.bcf2` の場合は従来どおり `SaveState(vm.CurrentFileName)` を呼ぶ
- 保存後ステータスメッセージを整合させる（必要なら）

## Acceptance Criteria
- `.bcf`を開いている状態で「上書き保存」を押すとSaveAsへ誘導される
- `.bcf`へ上書き保存されない（コード上も）

## Files
- `MainWindow.xaml.cs` (modify)

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`を開く
  2. 上書き保存を押す
  3. SaveAsが開き、`.bcf2`で保存できる

## Risks
- `vm.CurrentFileName` が空/想定外 - null/空は既存のIsNewFile導線へフォールバックする

## Status
- Closed

## Implementation Notes
- `MainWindow.xaml.cs`: `OverWrite_Click` で拡張子が `.bcf` の場合に `SaveAs_Click` へ誘導

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
