# ISSUE-SKIA-010-01 旧ISF（InkData/ISF bytes）をv2ストロークへ変換する

## Goal
- 旧`.bcf`内のISF（`InkData`から得られるISFバイト列）をWinRT Ink APIで復元し、ストローク点列（x,y,pressure,t）と描画属性を抽出してv2ストロークへ変換し、内部表現を統一できる

## Decision
- XAML Islands（UWP UI）は無効（`BRAIN_CARD_DISABLE_XAML_ISLANDS`）のまま進める
- ISFの「独自バイナリ解析」は行わず、`Windows.UI.Input.Inking.InkStrokeContainer.LoadAsync()` を用いてISFを復元する
- 入力は Step9 で保持した `Card.LegacyInk.IsfBytes` を起点とする
- 復元後は `InkStroke` から必要情報を抽出し、すぐv2へ変換して保持する（WinRT型は保持しない）
- 復元品質は「Skiaで可能な限り同等の品質」を目標とし、差分が出る場合は既知制約として管理する

## Non-goals
- v2保存/読込（Step7）
- 消しゴム（Step11）
- スムージング（最初は無し。必要なら別Issue）

## Scope
- ISF bytes → `InkStrokeContainer` へ復元する
- `InkStroke` から抽出する情報
  - 点列: `x,y`（DIP基準キャンバスへ整合）
  - `pressure`（取得できない場合は既定値）
  - `t`
    - ISFから取得できない場合は補完（相対ms、開始=0）
  - 描画属性
    - color, size, opacity, tool（ペン/蛍光）等（取得可能な範囲）
- 抽出結果をv2ストロークへ変換し、Card の `V2Strokes` へ格納できる
- PNGキャッシュが無いカードは、復元ストロークからPNG生成して既存表示経路に乗せる（詳細は子Issueへ）

## Child Issues (planned)
- `ISSUE-SKIA-010-01-01-isf-parse-and-extract` - ISF bytesをWinRTで復元して点列/属性を抽出
- `ISSUE-SKIA-010-01-02-attribute-and-pressure-mapping` - ツール/色/太さ/筆圧のマッピング
- `ISSUE-SKIA-010-01-03-time-reconstruction` - `t` の補完（単調増加の相対ms）
- `ISSUE-SKIA-010-01-04-skiasharp-render-parity` - Skia描画での同等品質目標の実装
- `ISSUE-SKIA-010-01-05-png-generation-from-restored` - 復元ストロークからPNG生成

## Acceptance Criteria
- 旧`.bcf`を読み込んだ際、カードごとにv2ストロークが生成されて保持される（少なくとも点列が入る）
- `t` が得られない場合でも、単調増加の相対msとして補完される
- PNGキャッシュが無いカードでも、Skia復元によりpreview表示まで到達できる（品質は「可能な限り同等」）

## Files (expected)
- `MainWindow.xaml.cs` (modify) - legacy読み込み後に変換を呼ぶ/PNGキャッシュ無い場合の生成経路を追加
- `Models/FileFormatV2/*` (modify if needed)
- 変換ユーティリティ（新規ファイルの可能性）

## Validation
- build: `dotnet build`
- manual:
  1. ISFを含む旧`.bcf`をロード
  2. 1枚以上のカードで `V2Strokes.Count > 0` を確認
  3. PNGキャッシュを削除したカードも表示されることを確認

## Risks
- WinRT復元に依存 - 失敗カードはスキップしてログを残し、全体は落とさない
- 座標系/スケール差 - 既存カードPNG生成時のスケール規則と合わせる
- t補完の品質 - 最初は等間隔16msでも許容し、後で速度推定へ置換する
