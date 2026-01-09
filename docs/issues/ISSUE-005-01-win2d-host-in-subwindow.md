# ISSUE-005-01 SubWindow上でWin2D描画ビューをホストできるようにする

## ゴール
- WPFの`SubWindow`にWin2Dの描画サーフェスを表示できる（マウス入力・描画は別Issue）

## ステータス
- 現状: `BrainCard` 側は `BRAIN_CARD_DISABLE_XAML_ISLANDS` が常時有効（`BrainCard.csproj`）

## スコープ（作業内容）
- WPF上でWin2D描画を行うホスト方式を選定し、最小構成で表示まで到達する

## 非対象（Non-goals）
- 入力（ポインタ収集）
- ストロークモデル生成
- PNG生成

## 制約 / 仮定
- ターゲットOSは **Windows 10 (19041) 以上**で固定する
- `BrainCard` は WPF（`net8.0-windows10.0.19041.0`）で、現時点では XAML Islands を無効化している
- 依存追加は最小（ただし Win2D を使う以上、必要なパッケージ追加は許容）
- まずは「表示できる」ことを優先し、描画内容はクリアカラー程度でよい

## 選択肢
### 選択肢A: Win2D + DirectX (SwapChain) を `HwndHost` でホスト
- 方式: WPFの`HwndHost`で子HWNDを作り、DirectX/SwapChainへWin2Dで描画
- Pros
  - XAML Islands/WinUIに依存しない
  - WPFとの統合が比較的明確（HWND境界）
- Cons
  - 初期実装コストが高い（デバイス/SwapChain/Resize/DPI/Present）
  - 入力座標変換やDPIの罠が多い

### 選択肢B: WinUI 3 / XAML Islands (WindowsXamlHost) 上に Win2Dコントロール
- 方式: `WindowsXamlHost`等を使ってWinUI/UWP側の`CanvasControl`/`CanvasSwapChainPanel`相当をホスト
- Pros
  - Win2Dの標準的なコントロール（`CanvasControl`等）を使いやすい
  - 入力イベントもWinUI側で扱いやすい
- Cons
  - 現状方針（XAML Islands無効）と衝突
  - `BrainCard.csproj` では `MyUWPApp` が除外されるため、戻り作業が大きい

### 選択肢C: 最小到達点のための暫定ホスト（WPFで描画）
- 方式: まずWPF（例: `DrawingVisual`/`WriteableBitmap`）で“入力→表示→PNG化”を成立させ、Win2Dは後続で置換
- Pros
  - 早くE2Eを通せる
  - DPI/入力の統合が簡単
- Cons
  - 「Win2Dでカードが作れる」という最小到達点定義と一致しない

## 決定
- 採用: **選択肢A**（`HwndHost` + SwapChain を採用し、XAML Islandsに戻らない）
  - 理由: 現状のプロジェクトがXAML Islands無効なため、同方針のままWin2D導入するにはこのルートが最短

## 技術方針（SwapChainの種類）
- 採用するSwapChain
  - **DXGI SwapChain for HWND**（`CreateSwapChainForHwnd` 相当）
- デバイス
  - **D3D11**（Win2D/Direct2D系の基盤として採用）
- Present
  - 最小到達点では **通常の`Present`** を行い、まずは「黒画面にならず安定表示」へ到達する

## パッケージ方針（導入OK: DXGI/D3D11のC#ラッパ）
- 方針
  - DXGI/D3D11をC#から扱うためのラッパ導入は許容する（最小到達点優先）
- 第一候補
  - **Vortice系**（`Vortice.Windows` など）
    - 理由: 現行.NET環境での継続保守とAPIカバレッジが比較的安定しているため
- Win2Dの位置づけ
  - 本Issueでは「ホストして表示できる」ことを優先し、Win2Dによる実描画・入力は後続Issueで段階導入する

## 描画品質方針（将来: UWPのInkCanvas相当へ近づける）
- 判断基準
  - 導入時点では生のストローク（加工なし）でよいが、**将来的に描画クオリティを上げられる経路であること**を必須とする
- このルート（D3D11 + DXGI SwapChain + Win2D/2D描画）の強み
  - 将来、ストロークのスムージング、筆圧による太さ変化、アンチエイリアス、ブラシ表現などを段階的に追加できる
  - 描画品質の改善は「入力点列の改善」と「描画アルゴリズムの改善」に分離して前進できる
- フォールバック（ブロック時のみ）
  - Win2Dの導入がホスト方式に対してブロック要因になる場合は、まず **D3D11/D2D（Vortice）で描画→PNG化** を成立させ、後続でWin2Dへ寄せる
  - ただし、最終的な品質目標（UWP InkCanvas相当）に近づけるため、フォールバック採用時は「Win2Dへ戻す条件」を別Issueで明記する

## DPI/座標系（ISSUE-005-03への接続点）
- 基本方針
  - 保存/モデル上の座標は **DIP** を正とする
  - SwapChainのバッファサイズは **ピクセル** で持つ
- 変換の責務
  - `HwndHost`側: WPFの表示サイズ（DIP）とDPIスケールから、SwapChainのピクセルサイズを決める
  - 入力側（後続Issue）: ポインタ座標（DIP想定）をv2点列（DIP）として保持し、描画時にピクセルへ変換する

## 受け入れ基準
- `SubWindow`にWin2Dビュー領域が表示される（背景クリアでも可）
- ウィンドウ表示/非表示、リサイズ、DPI変更でクラッシュしない（厳密なレイアウト追従は後回し可）
- `SubWindow`を閉じたときにGPU/COMリソースがリークせず例外が出ない（少なくとも継続起動で問題が顕在化しない）

## 実装スケッチ（non-code）
- `SubWindow.xaml`
  - Win2Dビュー用のプレースホルダ（`Border` or `Grid`）を用意し、そこに`HwndHost`派生コントロールを配置する
- `SubWindow.xaml.cs`
  - `Loaded`/`Unloaded`/`Closed`でホストのライフサイクルを開始/停止する
  - `SizeChanged`でSwapChainサイズ更新を呼ぶ（DPIスケール込み）
- 新規: `Win2DHost`（仮）
  - `HwndHost`派生で子HWND生成
  - デバイス作成、SwapChain作成、`Clear()`して`Present()`する最低限の描画ループ

## 対象ファイル（想定）
- `SubWindow.xaml` (modify)
- `SubWindow.xaml.cs` (modify)
- `BrainCard.csproj` (modify) - Win2D関連パッケージ追加が必要な場合のみ
- `Win2DHost/*` or `BrainCard/Win2D/*` (new) - `HwndHost`派生のホスト実装

## 実装メモ（PoC結果）
- 現状は Win2D をまだ差し込まず、**Vortice（D3D11 + DXGI SwapChain for HWND）** の最小描画（クリア色）で表示を成立させている
- `SubWindow` の描画領域は **WPF `InkCanvas` を置換**し、`HwndHost` 派生のホスト（`DxSwapChainHost`）を配置している
- レイアウト注意点
  - `d:Width`/`d:Height` はデザイナ専用のため、実行時サイズの基準は `ContentRootGrid.Width/Height` など **実プロパティ**で指定する
  - `Window.Template` 内で `ContentPresenter` が載る行は `Auto` だと潰れることがあるため、`RootGrid` の2行目は `*` を推奨

## 検証結果（手動）
- 起動直後からカード領域が全面クリア色で表示される
- リサイズでも描画領域が追従し、黒画面や例外が発生しない
- 注記: ウィンドウ全体のリサイズUX（比率維持や境界動作）は既存実装の改善余地があるため、別途改善する
