# ISSUE-SKIA-009-01-03 legacy読込後の内部表現を最小で統一する

## Goal
- 旧`.bcf`読込後の内部状態を、後段のStep10（Skiaで「可能な限り同等の品質で復元」）に接続しやすい形に最小限で統一する

## Non-goals
- 旧ISF→v2点列変換の実装（Step10で行う）
- 編集UIでのストローク再表示
- XAML Islandsを有効化するための構成変更

## Scope
- legacy読込で得られた情報（カードID、位置、テキスト、ISFバイト列）を、変換/復元パイプラインに渡しやすい構造へ整理
- 最小限の保持契約として「カードごとのISFバイト列」を保持できるようにする
  - 例: `Card.LegacyInk.IsfBytes`（無い場合はnull/空）

## Acceptance Criteria
- Step10で「ISF→Skia復元（および必要ならv2点列化）」処理を追加する際に、legacy読込側の改修が最小で済む状態になる

## Status
- Closed (done)

## Implementation Notes
- `BrainCard/Legacy/LegacyInkAttachment.cs`: legacy ISFバイト列を保持する最小の中間モデルを追加
- `Card.xaml.cs`: `LegacyInk` プロパティを追加（Step10が参照できる保持場所）
- `MainWindow.xaml.cs`: legacy読込時に`InkData`をデコードして`card.LegacyInk`へ保持

## Validation Results
- build: `dotnet build` (pass)
- manual: pass
  - `.bcf`読込後、カードにISFバイト列が保持される（`[LegacyISF] decode ok`で間接確認）
