# ISSUE-SKIA-009-01-01 legacy読込の責務分離

## Goal
- 旧`.bcf`読み込みロジックをv2読込と混在しない形に整理し、`MainWindow.xaml.cs`の巨大化を抑えつつ互換挙動を維持する

## Non-goals
- 旧ISF（`InkData`）の復元（別Issue）
- 旧ISF→v2変換（Step10）

## Scope
- `.bcf`読込経路（legacy）をメソッド/クラスに分離する
- 既存挙動は維持する
  - PNGキャッシュが無いカードはスキップ
  - 既存の`vm.IsLoading`/進捗更新/overlay表示の挙動を壊さない

## Acceptance Criteria
- `.bcf2`読込経路（v2）と`.bcf`読込経路（legacy）がコード上明確に分離される
- `.bcf`を読んだ際の表示結果が現状と同等である（PNGキャッシュ依存は維持）

## Files
- `MainWindow.xaml.cs` (modify)
- （必要なら）`BrainCard/Legacy`等に読込クラスを追加 (new)

## Validation
- build: `dotnet build`
- manual:
  1. `.bcf`を開いて表示される
  2. `.bcf2`を開いて表示される

## Risks
- リファクタで挙動差が出る - 既存メソッドの抽出に留め、外部I/Fは変えない

## Status
- Closed (done)

## Implementation Notes
- `BrainCard/Legacy/LegacyBcfModels.cs`: legacy `.bcf` のデータモデルを分離
- `BrainCard/Legacy/LegacyBcfLoader.cs`: legacy `.bcf` のJSON読込を分離
- `MainWindow.xaml.cs`: `LoadStateCoreAsync` で `LegacyBcfLoader` を使用するよう変更

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
  - `.bcf`を開いて従来通り表示される（PNGキャッシュ無しカードはスキップ）
