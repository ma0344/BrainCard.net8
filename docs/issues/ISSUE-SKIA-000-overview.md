# ISSUE-SKIA-000 SkiaSharp移行計画: 全体方針と責務分担（SubWindow中心）

## Goal
- SkiaSharp（`SkiaSharp.Views.WPF` / `SKElement`）を用いて、`SubWindow` で入力→描画→固定サイズPNG生成→`MainWindow` へ反映できる状態を定義する。

## Non-goals
- 旧ISF→v2変換の実装
- 筆圧/スムージング/消しゴムの実装
- 高解像度書き出し（将来拡張の余地は残す）

## 責務（現状の実装反映）
- `MainWindow` はカード表示・配置・選択・編集モード遷移を担う。
- `SubWindow` は編集キャンバス（Skia）と、Keep時のPNG生成（537x380固定）を担う。

## 現状の実装ステータス（重要）
- ? SubWindowに`SKElement`をホスト
- ? マウス入力でストローク点列（基準キャンバスDIP座標）を収集
- ? 収集点列を`SKElement`へ即時描画
- ? Keepで537x380px固定PNGをオフスクリーン（`SKSurface(537,380)`）で生成し、`MainWindow`へ`ImageSource`として反映
- ? 空ストロークの扱い
  - 新規作成: 追加しない
  - 編集モード: Keepで削除確認を出せる（Keepは空でも有効）
- ? Keepボタン有効/無効
  - 新規: ストローク>0になるまで無効
  - 編集: 常に有効
- ? 既存カードのストローク復元（編集開始時の再表示）は未実装（ロードマップ後段で対応）

## 基準キャンバスとスケーリング（方針）
- 基準キャンバスサイズは `Values.cardWidth=537`, `Values.cardHeight=380`（DIP）
- ストローク点列は基準キャンバス座標（DIP）で保持
- 表示サイズは `ViewScale` として扱い、入力は逆変換（displayed→base）、描画は順変換（base→displayed）

## PNG固定サイズ仕様
- 出力PNGピクセルサイズ: **537x380 px 固定**
- 方式: 表示と同一の描画ロジックを、オフスクリーン `SKSurface(537,380)` に適用して生成する

## Validation
- manual:
  1. 起動直後、SubWindowのKeepが無効であることを確認
  2. SubWindowで描画するとKeepが有効になることを確認
  3. KeepでMainWindowにカードが追加され、表示が更新されることを確認
  4. SubWindowを任意サイズに変更しても生成PNGは537x380相当であることを確認

## Risks
- WPFのDPIとSkiaのpx換算差 - オフスクリーンは537x380px固定、表示はpxPerDipで換算して描画する
- 既存ストローク復元の未実装 - ロードマップStep9?10で旧ISF→v2変換を実装して解消する
