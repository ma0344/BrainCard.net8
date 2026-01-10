using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using BrainCard.Models.FileFormatV2;
using ModernWpf.Controls;
using static BrainCard.Values;
using forms = System.Windows.Forms;
using BrainCard.Overlay;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace BrainCard
{
    /// <summary>
    /// SubWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SubWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public double heightGap;
        public double widthGap;
        private int currentSizingEdge = 0;
        private double windowTop = 100;
        private double windowLeft = 100;

        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        private const int WM_SIZING = 0x0214;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_SHOWWINDOW = 0x0018;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;

        public forms.Screen currentScreen;
        public EventHandler WindowLoaded;

        private readonly MainWindow mainWindow;

        // 旧XAML Islands公開フィールド互換（当面MainWindow側の整理が終わるまで保持）
        public object CardInkCanvasHost;
        public object cardInkCanvas;
        public object cardBaseGrid;
        public object customInkCanvas;
        public object customInkToolbar;
        public object resizeInkCanvas;
        public object ResizeInkCanvasHost;
        public object testPict;

        private RECT prevPoint = new RECT();
        private WindowChrome windowChrome;
        private SplitView splitView;
        private ToggleButton inkToolbarToggleButton;

        private bool _isCollecting;
        private readonly List<Point> _currentStroke = new();
        private readonly List<List<Point>> _strokes = new();

        private const string DefaultStrokeColor = "#FF000000";
        private const double DefaultStrokeSizeDip = 2.0;
        private const double DefaultStrokePressure = 0.5;

        private readonly List<Bcf2Stroke> _v2Strokes = new();

        private StrokeOverlayWindow _strokeOverlayWindow;

        private bool _overlaySyncQueued;

        public SubWindow(MainWindow main)
        {
            InitializeComponent();
            mainWindow = main;
            windowChrome = chrome;

            Loaded += SubWindow_Loaded;
            Unloaded += SubWindow_Unloaded;
            Closed += SubWindow_Closed;

            LocationChanged += (_, __) => SyncOverlayWindowBounds();
            SizeChanged += (_, __) =>
            {
                SyncOverlayWindowBounds();
                QueueOverlaySync();

                // Skia側も再描画
                SkiaElement?.InvalidateVisual();
            };
            IsVisibleChanged += (_, __) =>
            {
                if (_strokeOverlayWindow != null)
                {
                    _strokeOverlayWindow.Visibility = IsVisible ? Visibility.Visible : Visibility.Hidden;
                }
            };

            Canvas.SetLeft(ContentRootGrid, 0);
            Canvas.SetTop(ContentRootGrid, 0);

            SourceInitialized += SubWindow_SourceInitialized;

            // Win2Dホストは当面不使用（Skia置換）
            // DxHost.PointerDown += DxHost_PointerDown;
            // DxHost.PointerMove += DxHost_PointerMove;
            // DxHost.PointerUp += DxHost_PointerUp;
        }

        private void SubWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[SubWindow] Unloaded: hash={GetHashCode()}, IsVisible={IsVisible}, Visibility={Visibility}");
            CloseStrokeOverlayWindow();
        }

        private void SubWindow_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine($"[SubWindow] Closed: hash={GetHashCode()}, IsVisible={IsVisible}, Visibility={Visibility}");
            CloseStrokeOverlayWindow();
        }

        private void SubWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[SubWindow] Loaded: hash={GetHashCode()}, IsVisible={IsVisible}, Visibility={Visibility}");

            splitView = subwindow.Template.FindName("InkToolbarSplitView", subwindow) as SplitView;
            inkToolbarToggleButton = subwindow.Template.FindName("inkToolbarToggleButton", subwindow) as ToggleButton;

            // 初回表示時にSkia描画を促す
            try
            {
                SkiaElement?.InvalidateVisual();
            }
            catch
            {
            }

            EnsureStrokeOverlayWindow();

            OnWindowLoaded();
        }

        private void CloseToolbarPaneIfOpen()
        {
            if (splitView?.IsPaneOpen == true)
            {
                splitView.IsPaneOpen = false;
                if (inkToolbarToggleButton != null)
                {
                    inkToolbarToggleButton.IsChecked = false;
                }
            }
        }

        private void SubWindow_SourceInitialized(object sender, EventArgs e)
        {
            var hwndSource = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(this);
            hwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var rootGrid = (Grid)Template.FindName("RootGrid", this);
            var titleBar = (Grid)rootGrid.FindName("CustomTitleBar");
            double actualWidth = titleBar.ActualWidth;
            double canvasHeight = actualWidth * AspectRatio;
            double actualHeight = canvasHeight + titleBar.ActualHeight;

            switch (msg)
            {
                case WM_SHOWWINDOW:
                    widthGap = 20;
                    heightGap = titleBar.ActualHeight + 20;
                    windowTop = mainWindow.Top + 30;
                    windowLeft = mainWindow.Left + 30;

                    var rect = new RECT();
                    if (mainWindow.vm.SubWindowPosition == new Point() && mainWindow.vm.SubwindowVisible)
                    {
                        rect.Left = (int)(windowLeft * scale);
                        rect.Top = (int)(windowTop * scale);
                        rect.Right = (int)((windowLeft + cardWidth + widthGap) * scale);
                        rect.Bottom = (int)((windowTop + cardHeight + heightGap) * scale);
                    }
                    else
                    {
                        break;
                    }

                    var width = rect.Right - rect.Left;
                    var height = rect.Bottom - rect.Top;

                    if (!mainWindow.vm.IsInEditMode)
                    {
                        SetWindowPos(hwnd, IntPtr.Zero, rect.Left, rect.Top, width, height, 0);
                    }

                    ContentRootGrid.Width = cardWidth;
                    ContentRootGrid.Height = cardHeight;
                    handled = true;
                    break;

                case WM_SYSCOMMAND:
                    if (wParam.ToInt32() == SC_MINIMIZE)
                    {
                        prevPoint = new RECT
                        {
                            Left = (int)(Left * scale),
                            Top = (int)(Top * scale),
                            Right = (int)((Left + actualWidth) * scale),
                            Bottom = (int)((Top + actualHeight) * scale)
                        };
                    }
                    break;

                case WM_SIZING:
                    currentSizingEdge = wParam.ToInt32();
                    break;

                case WM_WINDOWPOSCHANGING:
                    if (lParam != IntPtr.Zero)
                    {
                        var position = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                        if (currentSizingEdge != 0 && position.cx != 0 && position.cy != 0)
                        {
                            AdjustWindowSize(ref position, currentSizingEdge, scale);
                            Marshal.StructureToPtr(position, lParam, true);
                        }
                        currentSizingEdge = 0;
                    }
                    break;

                case WM_EXITSIZEMOVE:
                    break;
            }

            return IntPtr.Zero;
        }

        private void AdjustWindowSize(ref WINDOWPOS pos, int sizingEdge, double dpiScale)
        {
            if (sizingEdge == 0 || pos.cy == 0 || pos.cx == 0) return;

            var resizeBorder = windowChrome.ResizeBorderThickness;

            // SubWindowはカード比率固定
            switch (sizingEdge)
            {
                case WMSZ_TOP:
                case WMSZ_BOTTOM:
                case WMSZ_TOPRIGHT:
                    UpdateChildSizes(windowHeight: pos.cy);
                    pos.cx = (int)(((ContentRootGrid.ActualWidth + resizeBorder.Right) * dpiScale));
                    break;

                case WMSZ_LEFT:
                case WMSZ_RIGHT:
                case WMSZ_BOTTOMRIGHT:
                case WMSZ_BOTTOMLEFT:
                    UpdateChildSizes(windowWidth: pos.cx);
                    pos.cy = (int)(((ContentRootGrid.ActualHeight + resizeBorder.Bottom) * dpiScale) + heightGap - 20);
                    break;

                case WMSZ_TOPLEFT:
                    UpdateChildSizes(windowHeight: pos.cy);
                    pos.cx = (int)(((ContentRootGrid.ActualWidth + resizeBorder.Right) * dpiScale));
                    pos.x = (int)(pos.x - (pos.cx - Width * dpiScale));
                    break;
            }
        }

        private void UpdateChildSizes(double windowWidth = double.NaN, double windowHeight = double.NaN)
        {
            var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            double clientWidth = ContentRootGrid.ActualWidth;
            double clientHeight = ContentRootGrid.ActualHeight;

            if (!double.IsNaN(windowHeight))
            {
                clientHeight = (windowHeight / scale) - heightGap;
                clientWidth = clientHeight * AspectRatio;
            }
            else if (!double.IsNaN(windowWidth))
            {
                clientWidth = (windowWidth / scale) - widthGap;
                clientHeight = clientWidth / AspectRatio;
            }

            if (clientWidth < 0 || clientHeight < 0) return;

            ContentRootGrid.Width = clientWidth;
            ContentRootGrid.Height = clientHeight;

            try
            {
                SkiaElement?.InvalidateVisual();
            }
            catch
            {
            }

            SyncOverlayWindowBounds();
            QueueOverlaySync();
        }

        public bool HasInkStrokes => false;

        // SubWindow : Keepボタンがクリックされたときの処理
        // 保存処理は後でまとめて行うため、ここでは編集結果のキャプチャのみ行う（ストローク/認識はダミー）
        public async void ButtonKeep_Click(object sender, RoutedEventArgs e)
        {
            var hasStrokes = HasInkStrokes;
            var imageSource = await CaputureCanvasAsync();

            if (mainWindow != null)
            {
                // 編集確定（新規追加/更新/削除）をMainWindow側に委譲
                mainWindow.ApplyEditingResultFromSubWindow(imageSource, hasStrokes);
            }

            CanvasClear();
        }

        public ImageSource CaputureCanvas()
        {
            // Skia表示はWPF要素上に描画されるためCardCanvasGridをレンダリングすればよい
            var rtb = new RenderTargetBitmap(
                (int)Math.Max(1, CardCanvasGrid.ActualWidth),
                (int)Math.Max(1, CardCanvasGrid.ActualHeight),
                96,
                96,
                PixelFormats.Pbgra32);
            rtb.Render(CardCanvasGrid);
            rtb.Freeze();
            return rtb;
        }

        public Task<ImageSource> CaputureCanvasAsync()
        {
            // WPF RenderTargetBitmapは同期レンダリング
            return Task.FromResult(CaputureCanvas());
        }

        private void EnsureStrokeOverlayWindow()
        {
            if (_strokeOverlayWindow != null)
            {
                return;
            }

            _strokeOverlayWindow = new StrokeOverlayWindow
            {
                Owner = this,
                ShowActivated = false
            };

            _strokeOverlayWindow.Loaded += (_, __) => SyncOverlayWindowBounds();
            _strokeOverlayWindow.Show();

            SyncOverlayWindowBounds();
            UpdateStrokeOverlayWindow();
        }

        private void CloseStrokeOverlayWindow()
        {
            try
            {
                _strokeOverlayWindow?.Close();
            }
            catch
            {
            }
            finally
            {
                _strokeOverlayWindow = null;
            }
        }

        private void SyncOverlayWindowBounds()
        {
            if (_strokeOverlayWindow == null)
            {
                return;
            }

            try
            {
                // CardCanvasGridのスクリーン座標へ合わせる
                var topLeft = CardCanvasGrid.PointToScreen(new Point(0, 0));
                var dpi = VisualTreeHelper.GetDpi(this);
                var leftDip = topLeft.X / dpi.DpiScaleX;
                var topDip = topLeft.Y / dpi.DpiScaleY;

                _strokeOverlayWindow.Left = leftDip;
                _strokeOverlayWindow.Top = topDip;
                _strokeOverlayWindow.Width = Math.Max(1, CardCanvasGrid.ActualWidth);
                _strokeOverlayWindow.Height = Math.Max(1, CardCanvasGrid.ActualHeight);
            }
            catch
            {
            }
        }

        private void UpdateStrokeOverlayWindow()
        {
            if (_strokeOverlayWindow == null)
            {
                return;
            }

            // 既存の点列はDIP座標なので、そのままCanvasへ投入できる
            _strokeOverlayWindow.SetStrokes(_strokes, _currentStroke, includeCurrent: _isCollecting);
        }

        private void EnsureOverlayCanvas()
        {
            // no-op: HwndHostのAirspace問題で同一ツリーのCanvasは見えないため使用しない
        }

        private readonly List<Polyline> _overlayPolylines = new();

        private void AddOverlayStroke(IReadOnlyList<Point> dipPoints)
        {
            // no-op: 透明オーバーレイWindow側で描画する
        }

        private static Bcf2Stroke MapToV2Stroke(IReadOnlyList<Point> dipPoints)
        {
            var stroke = new Bcf2Stroke
            {
                Id = Guid.NewGuid().ToString("N"),
                Tool = "pen",
                Color = DefaultStrokeColor,
                Size = DefaultStrokeSizeDip,
                DeviceKind = "mouse"
            };

            var t = 0;
            foreach (var p in dipPoints)
            {
                stroke.Points.Add(new Bcf2Point
                {
                    X = p.X,
                    Y = p.Y,
                    Pressure = DefaultStrokePressure,
                    T = t
                });

                // 最小到達点: 時刻は仮（等間隔）
                t += 16;
            }

            return stroke;
        }

        private const double MinDistanceDip = 0.5;

        private static bool ShouldAppendPoint(IReadOnlyList<Point> stroke, Point p, double minDistanceDip)
        {
            if (stroke.Count == 0) return true;
            var last = stroke[stroke.Count - 1];
            var dx = p.X - last.X;
            var dy = p.Y - last.Y;
            return (dx * dx + dy * dy) >= (minDistanceDip * minDistanceDip);
        }

        private Point ToCardCanvasGridPoint(Point dxHostPoint)
        {
            // Skia置換でDxHostは存在しないため、座標変換は不要（入力はCardCanvasGrid基準で取得する）
            return dxHostPoint;
        }

        private bool _isInputCaptured;

        private void InputCaptureLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseToolbarPaneIfOpen();

            _isCollecting = true;
            _currentStroke.Clear();

            var p = e.GetPosition(CardCanvasGrid);
            _currentStroke.Add(p);

            _isInputCaptured = true;
            InputCaptureLayer.CaptureMouse();

            UpdateOverlay();
            e.Handled = true;
        }

        private void InputCaptureLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isCollecting || !_isInputCaptured)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var p = e.GetPosition(CardCanvasGrid);
            if (ShouldAppendPoint(_currentStroke, p, MinDistanceDip))
            {
                _currentStroke.Add(p);
                UpdateOverlay();
            }

            e.Handled = true;
        }

        private void InputCaptureLayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isCollecting)
            {
                return;
            }

            _isCollecting = false;

            var p = e.GetPosition(CardCanvasGrid);
            if (_currentStroke.Count == 0)
            {
                _currentStroke.Add(p);
            }

            _strokes.Add(new List<Point>(_currentStroke));
            var v2 = MapToV2Stroke(_currentStroke);
            _v2Strokes.Add(v2);

            UpdateOverlay();

            if (_isInputCaptured)
            {
                _isInputCaptured = false;
                InputCaptureLayer.ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        // DxHost_* はWin2Dホスト用。Skia移行中は未使用のため除外。
        // private void DxHost_PointerDown(object sender, Win2DHost.HostPointerEventArgs e) { }
        // private void DxHost_PointerMove(object sender, Win2DHost.HostPointerEventArgs e) { }
        // private void DxHost_PointerUp(object sender, Win2DHost.HostPointerEventArgs e) { }

        public void CanvasClear()
        {
            _isCollecting = false;
            _currentStroke.Clear();
            _strokes.Clear();
            _v2Strokes.Clear();

            _strokeOverlayWindow?.Clear();

            UpdateOverlay();

            try
            {
                SkiaElement?.InvalidateVisual();
            }
            catch
            {
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            CanvasClear();
            if (mainWindow.vm.IsInEditMode) mainWindow.CancelEditing();
        }

        private void SplitView_PaneOpening(ModernWpf.Controls.SplitView sender, object args) { }

        private void SplitView_PaneClosed(ModernWpf.Controls.SplitView sender, object args) { }

        private void CustomTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseToolbarPaneIfOpen();

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                if (!mainWindow.vm.IsInEditMode)
                {
                    DragMove();
                    SyncOverlayWindowBounds();
                }
                currentScreen = forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (splitView != null) splitView.IsPaneOpen = true;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (splitView != null) splitView.IsPaneOpen = false;
        }

        public void OnWindowLoaded()
        {
            WindowLoaded?.Invoke(null, null);
        }

        public void setEditCanvas(Card card)
        {
            // 暫定: PNGキャッシュ表示が前提になるまで、ストローク復元は行わない
            // （Win2D移行後に v2 stroke へ統一して復元する）
        }

        private void QueueOverlaySync()
        {
            if (_overlaySyncQueued) return;
            _overlaySyncQueued = true;

            Dispatcher.InvokeAsync(() =>
            {
                _overlaySyncQueued = false;
                SyncOverlayWindowBounds();
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void UpdateOverlay()
        {
            // Skiaへ移行するため、オーバーレイWindowでの描画は当面停止
            // （必要になったら削除か、Skia描画へ統合する）
            try
            {
                SkiaElement?.InvalidateVisual();
            }
            catch
            {
            }
        }

        private void SkiaElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.LightGray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            // テスト用フレーム（描画が動作していることを目視確認するため）
            var rect = new SKRect(1, 1, e.Info.Width - 2, e.Info.Height - 2);
            canvas.DrawRect(rect, paint);

            // TODO: 次ステップで_strokes/_currentStrokeを描画する
        }
    }
}
