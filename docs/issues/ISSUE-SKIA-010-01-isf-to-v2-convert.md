# ISSUE-SKIA-010-01 旧ISF（InkStroke）をv2ストロークへ変換する

## Goal
- 旧`.bcf`内のISF（`InkStrokeContainer`）からストローク点列（x,y,pressure,t）と描画属性を抽出し、v2ストロークへ変換して内部表現を統一できる

## Non-goals
- v2保存/読込（Step7）
- 消しゴム（Step11）
- スムージング

## Scope
- `InkStrokeContainer.LoadAsync()` で復元した `InkStroke` を走査
- 変換する情報
  - 点列: `x,y`（DIP基準キャンバスへ整合）
  - `pressure`
  - `t`
    - ISFから取得できない場合は速度推定で補完（相対ms、開始=0）
  - 描画属性
    - color, size, opacity, tool（ペン/蛍光）等（取得可能な範囲）
- 変換後は Card の `V2Strokes` へ格納できる

## Acceptance Criteria
- 旧`.bcf`を読み込んだ際、カードごとにv2ストロークが生成されて保持される（少なくとも点列が入る）
- `t` が得られない場合でも、単調増加の相対msとして補完される
- 変換結果でSubWindowが再描画できる土台ができる（表示そのものは別Issueでも可）

## Files (expected)
- `MainWindow.xaml.cs` (modify) - legacy読み込み後に変換を呼ぶ
- `Models/FileFormatV2/*` (modify if needed)
- 変換ユーティリティ（新規ファイルの可能性）

## Validation
- build: `dotnet build`
- manual:
  1. ISFを含む旧`.bcf`をロード
  2. 1枚以上のカードで `V2Strokes.Count > 0` を確認

## Risks
- 座標系/スケール差 - 既存カードPNG生成時のスケール規則と合わせる
- t補完の品質 - 最初は等間隔16msでも許容し、後で速度推定へ置換する
