# ISSUE-SKIA-000 SkiaSharp移行検討: 全体方針と責務分担（SubWindow中心）

## Goal
- SkiaSharp（`SkiaSharp.Views.WPF` の `SKElement`）を用いて、`SubWindow` 上で入力→描画→PNG生成→MainWindow反映を成立させるための設計・責務分担・成果物の受け渡し契約を明文化する。

## Non-goals
- 直ちに実装を完了すること
- ペン筆圧・スムージング・消しゴムの実装
- 高解像度書き出しの実装（将来的な拡張余地は残す）

## 現状（要点）
- `MainWindow` はカード表示とファイル操作（ロード/セーブ）を担う。
- `SubWindow` はカード編集（入力/描画）と、Keep時のPNG生成を担う。
- 現行実装は Win2D/Vortice/HwndHost を経由しており、表示サイズ→PNGサイズが連動しやすい。

## 目標とする責務分担
### MainWindow の責務
- カードオブジェクトの管理
  - 表示、配置（`x,y,z`）、選択、削除
- 永続化（`*.bcf2` JSON）
  - `previewPng`（Assets配下）と `ink`（v2ストローク）を整合させる
- 読み込み
  - PNGキャッシュがある場合はそれを表示に使用
  - PNGキャッシュが欠損している場合は SubWindow に再生成を依頼する（契約化）

### SubWindow の責務
- 入力
  - 当面はマウス入力のみ（押下→移動→離す）
- 編集モデル管理
  - v2の点列（DIP）と描画属性（色/太さ/筆圧）を保持
- 描画
  - `SKElement` 上で即時に描画
- PNG生成
  - Keep時に 537x380px のPNGを生成する（表示サイズに依存しない）
  - キャッシュ欠損時の再生成（ink→PNG）もSubWindowが担う

## 基準キャンバスとスケーリング方針（B）
- 基準キャンバスサイズは `Values.cardWidth=537`, `Values.cardHeight=380`（DIP）とする。
- 点列の基準座標系は「基準キャンバスDIP座標」とし、SubWindowの表示サイズは倍率（ViewScale）として扱う。
- ストロークの太さも座標と同じ倍率で拡大縮小される（見た目の一貫性を優先）。

## PNG固定サイズ仕様
- 出力PNGピクセルサイズ: **537x380 px 固定**
- 背景: `CardCanvasGrid` 相当の背景（既定は白）
- 見た目一致: SubWindow表示と同じレンダリングロジックを、オフスクリーンの `SKSurface(537,380)` に適用して生成する。
- 将来の高解像度: `倍率`（例: 2x, 4x）で `SKSurface(537*k, 380*k)` に描画できる余地を残す。

## cardId 採番ルール（SubWindow主体）
- 原則: **cardIdはSubWindowが最終的に確定する**（成果物に含めてMainWindowへ渡す）。
- 新規カード作成（新規作成Keep）
  - SubWindowが `Guid.NewGuid().ToString("N")` で `cardId` を生成する。
  - 生成した `cardId` を `previewPng` のファイル名に利用する（例: `<cardId>.png`）。
- 既存カード編集（編集モードKeep）
  - **既存の `cardId` を必ず維持する**（新規採番しない）。
  - SubWindowは編集開始時にMainWindowから対象カードの `cardId` を受け取り、編集コンテキストとして保持する。
  - Keep時は同一 `cardId` でPNGを再生成し、既存の `Assets/<cardId>.png` を上書きする。

## 欠損PNG再生成の契約（MainWindow → SubWindow）
ロード時にPNGキャッシュが欠損しているカードがあった場合、MainWindowはSubWindowへ再生成を依頼し、ロードを継続する。

### 依頼（入力）
MainWindowがSubWindowへ渡す最低限の入力:
- `bcf2Path` または `assetsDir`
  - 保存先: `(<bcf2Path>).Assets/<cardId>.png`
- `cardId`
- `strokes`（v2ストローク、units=dip）
- `backgroundColor`（当面は白固定。将来のユーザー選択に備えて引数化してもよい）

### 応答（出力）
SubWindowは実ファイル保存までを担い、MainWindowへ結果を返す:
- `pngPath`（保存した絶対パス）
- `success`（bool）
- `error`（任意。ログ出力用）

### 前提
- 生成するPNGは **常に 537x380 px**（表示サイズに依存しない）。
- 既存の `Assets/<cardId>.png` が存在する場合は上書きする（キャッシュ更新）。

## 成果物受け渡し（契約案）
SubWindow → MainWindow へ渡す成果物は最低限以下を含む:
- `cardId`: 既存カード更新か新規作成の識別
- `previewPngBytes` または `previewPngBitmapSource`: 537x380px
- `strokes`: v2ストローク（`stroke-v1`相当、units=dip）
- `recognizedText`: 任意（現状維持）
- `isEmpty`: 空ストローク判定

※実装形は `EditingResult` のようなDTOとしてまとめ、引数の増殖を避ける。

## Validation
- manual:
  1. SubWindowを任意サイズにリサイズ
  2. 1本ストロークを入力
  3. Keepで生成されたPNGが常に537x380pxであること
  4. 表示サイズを変えても線の太さが見た目上スケールすること

## Risks
- WPFのDPIスケールとSkiaのピクセル座標変換の齟齬 - DIP基準のスケール計算と検証項目を明文化する
- 空ストローク判定/削除フローのUI整合 - 成果物契約に `isEmpty` を含める
