# ISSUE-SKIA-009-01 旧 .bcf 読み込み経路を分離する

## Goal
- 旧`.bcf`の読み込みを「互換読み込み」として残しつつ、v2（`.bcf2`）の読み込み経路とコード上で分離できる
- 旧`.bcf`内の`InkData`（ISF）を、後段のSkia復元（Step10）へ渡せる形でデコード・保持できる状態にする（XAML Islandsは無効で進める）

## Non-goals
- 旧ISF→v2変換（Step 10）
- 旧`.bcf`の保存（Step 8で禁止）
- XAML Islands有効化のための構成変更

## Scope
- ファイル拡張子で処理経路を分岐
  - `.bcf2` → v2読込（Step7）
  - `.bcf` → legacy読込（本Issue）
- legacy読込は現状の実装（PNGキャッシュが無いカードはスキップ）を維持しつつ整理
- 旧`.bcf`内の`InkData`（ISF）をデコードしてカードに保持し、後段のStep10へ接続しやすい形にする

## Acceptance Criteria
- `.bcf2` と `.bcf` を開いたときに、それぞれ対応する経路が呼ばれる
- 既存の `.bcf` 読み込み挙動が大きく変わらない（少なくとも現状のPNGキャッシュ依存の読み込みが維持される）
- 旧`.bcf`の`InkData`についてBase64デコードが完走し、カード単位で保持できる（無効データ混在でも全体が落ちない）

## Files (expected)
- `MainWindow.xaml.cs` (modify) - Load入口の分岐/整理
- `SavedImage` など legacyモデル（必要なら別ファイルへ）

## Parent / Child Issues
- `ISSUE-SKIA-009-01-01-legacy-loader-refactor` - legacy読込の責務分離
- `ISSUE-SKIA-009-01-02-isf-restore` - legacyの`InkData`（ISF）デコード/保持
- `ISSUE-SKIA-009-01-03-normalize-after-legacy-load` - 読込後の内部表現統一（最小、Step10へ接続）

## Validation
- build: `dotnet build`
- manual:
  1. 既存`.bcf`をロードして表示される
  2. `.bcf2`をロードして表示される（Step7が完了している前提）
  3. 旧`.bcf`をロードした際に `[LegacyISF] decode ok/skipped` が出ることを確認する

## Risks
- ロード処理が巨大 - メソッド分割/クラス分離で局所化し、挙動は維持する
- ISFの完全再現は困難 - Step10で「可能な限り同等の品質でSkia復元」を実装し、差分は既知制約として扱う

## Status
- Closed (done)

## Child Issue Status
- Closed: ISSUE-SKIA-009-01-01-legacy-loader-refactor
- Closed: ISSUE-SKIA-009-01-02-isf-restore
- Closed: ISSUE-SKIA-009-01-03-normalize-after-legacy-load

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
  1. `.bcf`をロードして従来通り表示される（PNGキャッシュ無しカードはスキップ）
  2. `.bcf`読込時に`[LegacyISF] decode ok`が出力される
  3. XAML Islands無効のため`[LegacyISF] restore skipped (BRAIN_CARD_DISABLE_XAML_ISLANDS)`が出力される（想定通り）
