# ISSUE-SKIA-009-01-02 legacyの`InkData`（ISF）を復元する

## Goal
- 旧`.bcf`内の`InkData`（ISF）を、後段のSkia復元（Step10）に渡せる形でデコード・保持できる状態にする

## Non-goals
- `InkStrokeContainer.LoadAsync()` による復元（XAML Islands無効で進めるため）
- 復元したストロークのSkia描画や編集UI反映
- 旧ISF→v2変換（Step10）

## Scope
- legacyモデル（`SavedImage.InkData`）からISFバイト列を抽出して保持する
  - `InkData`がJSON文字列として二重にエスケープされているケースを吸収する
  - Base64のデコードに失敗した場合は、カード単位でスキップ/エラーを記録する（クラッシュしない）
- XAML Islands無効（`BRAIN_CARD_DISABLE_XAML_ISLANDS`）を前提に、WinRT依存を呼ばない形で実装する

## Acceptance Criteria
- `.bcf`の各カードについて、`InkData`からISFバイト列を取得して保持できる（少なくとも有効データで）
- 無効な`InkData`が混在しても読み込み全体が落ちない

## Files
- `BrainCard/Legacy/LegacyIsfRestore.cs` (modify) もしくは同等の復元ヘルパー
- `MainWindow.xaml.cs` (modify) もしくは読込後保持の実装

## Validation
- build: `dotnet build`
- manual:
  1. `InkData`を含む旧`.bcf`を開く
  2. `Debug`出力に `[LegacyISF] decode ok/skipped` が出る
  3. デコード失敗が混在してもアプリが落ちない

## Risks
- ISFバイト列のみでは完全再現が難しい - Step10で「可能な限り同等の品質でSkia復元」を実装し、差分は既知制約として扱う

## Status
- Closed (done)

## Implementation Notes
- `BrainCard/Legacy/LegacyIsfRestore.cs`: legacy `InkData` のデコード（JSON文字列/生base64両対応）を実装
- `MainWindow.xaml.cs`: legacy読込ループ内でデコード結果を保持し、デバッグ出力

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
  - `.bcf`読込時に`[LegacyISF] decode ok/skipped`が出力されることを確認
