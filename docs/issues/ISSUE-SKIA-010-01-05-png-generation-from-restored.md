# ISSUE-SKIA-010-01-05 復元ストロークからPNGキャッシュを生成する

## Goal
- 旧`.bcf`読込時にPNGキャッシュが無いカードでも、復元ストロークからPNGを生成して表示/キャッシュできる

## Non-goals
- v2保存（`.bcf2`）への直接移行

## Scope
- PNGキャッシュ欠損時のフォールバック生成
  - 生成先: `<bcfPath>.Assets/<cardId>.png`
  - 生成後: 既存の読み込み表示経路に乗せる
- 生成失敗時はスキップし、アプリは落とさない

## Acceptance Criteria
- PNGキャッシュを削除したカードが、再ロードで表示される
- 生成されたPNGがAssetsディレクトリに作成される

## Files
- `MainWindow.xaml.cs` (modify)
- PNG生成ユーティリティ（Skia描画結果の保存）(new|modify)

## Validation
- build: `dotnet build`
- manual:
  1. 旧`.bcf`のAssetsから任意の`<id>.png`を削除
  2. アプリで`.bcf`をロード
  3. 欠損カードが表示され、PNGが再生成される

## Risks
- 生成時間 - 最初は逐次生成でよく、必要ならジョブ化/キャッシュ戦略を追加する
