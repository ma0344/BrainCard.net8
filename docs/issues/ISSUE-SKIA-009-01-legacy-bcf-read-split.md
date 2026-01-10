# ISSUE-SKIA-009-01 旧 .bcf 読み込み経路を分離する

## Goal
- 旧`.bcf`の読み込みを「互換読み込み」として残しつつ、v2（`.bcf2`）の読み込み経路とコード上で分離できる

## Non-goals
- 旧ISF→v2変換（Step 10）
- 旧`.bcf`の保存（Step 8で禁止）

## Scope
- ファイル拡張子で処理経路を分岐
  - `.bcf2` → v2読込（Step7）
  - `.bcf` → legacy読込（本Issue）
- legacy読込は現状の実装（PNGキャッシュが無いカードはスキップ）を維持しつつ整理
- 後段のStep10で「旧ISFを読み込んでv2へ統一」に繋げやすい形にする

## Acceptance Criteria
- `.bcf2` と `.bcf` を開いたときに、それぞれ対応する経路が呼ばれる
- 既存の `.bcf` 読み込み挙動が変わらない（少なくとも現状のPNGキャッシュ依存の読み込みが維持される）

## Files (expected)
- `MainWindow.xaml.cs` (modify) - Load入口の分岐/整理
- `SavedImage` など legacyモデル（必要なら別ファイルへ）

## Validation
- build: `dotnet build`
- manual:
  1. 既存`.bcf`をロードして表示される
  2. `.bcf2`をロードして表示される（Step7が完了している前提）

## Risks
- ロード処理が巨大 - メソッド分割/クラス分離で局所化し、挙動は維持する
