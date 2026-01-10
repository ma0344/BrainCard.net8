# ISSUE-SKIA-005-04 PNG生成（537x380px固定）

## Goal
- `SubWindow` の編集内容から、表示サイズに依存しない **537x380px固定** のPNGを生成できる。

## Scope
- オフスクリーンの `SKSurface(537,380)` に対して、画面表示と同一の描画ロジックでレンダリングする
- 生成したPNGを `(<file>).Assets/<cardId>.png` に保存できる形にする

## Non-goals
- 高解像度書き出し（ただし拡張余地は残す）

## Notes
- 「ストロークデータ自体を拡大縮小してからPNG作成」と等価になるよう、描画時に変換（スケール）を統一する。
- 将来の高解像度対応は `scaleFactor` を導入し、`SKSurface(537*k,380*k)` とすることで対応可能。

## Acceptance Criteria
- SubWindowを任意サイズにリサイズしても、生成PNGは常に537x380px
- 線幅も座標と同率でスケールされ、見た目が一貫する

## Files (expected)
- `SubWindow.xaml.cs` (modify)
- `MainWindow.xaml.cs` (modify) - 保存/Assets更新フローが必要な場合
