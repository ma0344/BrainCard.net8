# 引継ぎメモ（次スレッド用）

## 📋 分析結果 Summary:
- Hot Reload後も見た目が変わらなかった原因は「表示が `*.Assets\\<cardId>.png` のプレビューPNGキャッシュ参照」+「Pencilノイズが `PencilBrushTexture` でプロセス内キャッシュ」になっていたため。
- 対策として、PNGキャッシュ削除とノイズキャッシュ破棄をUIから実行できるようにし、Hot Reload後の変更が反映されることをユーザー確認済み。
- キャッシュクリアUIは ModernWPF の `ui:SplitButton` で、デフォルトクリック=PNGのみクリア、FlyoutでPNGのみ/PNG+ノイズを選択できるようにした（ユーザー確認済み）。
- 追加要望（未対応）:
  - 添付画像の「ストローク周囲に、描画方向に平行な線（バンディング/ストライプ）」を消したい
  - Pressure値に合わせて描画を薄くしていきたい（v2 strokeの `Pressure` を反映）

## ✅ 実装済み変更点（ファイル/概要）:
- `BrainCard/Legacy/PencilBrushTexture.cs`
  - `PencilBrushTexture.ClearCache()` を追加（`_alphaNoise` をDisposeしてnull）
- `MainWindow.xaml`
  - キャッシュクリアを `ui:SplitButton`（`ClearCacheSplitButton`）へ置換
  - Flyout メニュー:
    - `PNGのみクリア`
    - `PNG + ノイズもクリア`
- `MainWindow.xaml.cs`
  - キャッシュクリア処理を分割実装
    - `ClearCacheSplitButton_Click(ui.SplitButton, ui.SplitButtonClickEventArgs)`：デフォルト（PNGのみ）
    - `ClearCachePngOnlyMenuItem_Click(...)`：PNGのみ
    - `ClearCachePngAndNoiseMenuItem_Click(...)`：PNG+ノイズ（`PencilBrushTexture.ClearCache()` を呼ぶ）
    - 共通 `ClearPngCacheAndRefresh(bool clearNoiseCache)`：Assets配下PNG削除 + 可能なら再ロード
  - 旧 `ClearCacheButton_Click` は後方互換として残し、新実装へ委譲

## ✅ ユーザー確認済み:
- Hot Reload後に変更した値が反映される
- SplitButtonの両機能（PNGのみ / PNG+ノイズ）が適切に動作する

## 🔄 これから調査/修正すべき箇所の当たり（次スレッドでの開始点）:
- ストロークの描画（Pencilのストライプ問題/Pressure反映）
  - `BrainCard/Legacy/LegacyPngRenderer.cs`（レンダリング本体の可能性が高い）
  - `BrainCard/Legacy/PencilBrushTexture.cs`（ノイズ生成/適用が原因の可能性）
  - `BrainCard/Models/FileFormatV2/*`（`Bcf2Stroke.Points[].Pressure` の利用箇所）
  - `SubWindow.xaml.cs`（Skiaでの描画は現状一定線幅/一定色のため、Pressure適用にはここも関与する可能性）
- ドキュメント反映（当該スレッドで対応済み）
  - `Refactoring_Roadmap.md`：キャッシュクリアSplitButtonと次課題を追記済み
  - `docs/issues/ISSUE-SKIA-010-01-04-skiasharp-render-parity.md`：末尾に現状/次課題を追記済み（既存本文は文字化けのため温存）

## ❓ 不明点・要確認（次スレッドで確認すると早い）:
- 「線が入っている」の発生箇所が
  - `LegacyPngRenderer` のPencil描画（プレビューPNG側）なのか
  - `SubWindow` のSkia描画（編集ウィンドウ側）なのか
  - 両方なのか
- Pressureの期待仕様（例: 0→完全透明、1→通常濃度、非線形カーブ有無、筆圧で線幅も変えるか等）

## 最終ビルド状態:
- 最新の変更はビルド成功済み（SplitButton化後も成功）。
