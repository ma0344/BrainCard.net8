using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace BrainCard.Win2DHost
{
    public sealed class DxSwapChainHost : HwndHost
    {
        private IntPtr _hwnd;
        private IntPtr _overlayHwnd;

        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private IDXGISwapChain1 _swapChain;
        private ID3D11RenderTargetView _rtv;

        public Color4 ClearColor { get; set; } = new Color4(0.10f, 0.40f, 0.70f, 1.0f);

        public event EventHandler<HostPointerEventArgs> PointerDown;
        public event EventHandler<HostPointerEventArgs> PointerMove;
        public event EventHandler<HostPointerEventArgs> PointerUp;

        private GCHandle _thisHandle;

        private readonly object _overlayLock = new();
        private List<List<Point>> _overlayStrokes = new();

        private static readonly WndProcDelegate HostWndProcThunk = HostStaticWndProc;
        private static readonly WndProcDelegate OverlayWndProcThunk = OverlayStaticWndProc;

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            _thisHandle = GCHandle.Alloc(this);
            var self = GCHandle.ToIntPtr(_thisHandle);

            _hwnd = CreateHostWindow(hwndParent.Handle, self);
            _overlayHwnd = CreateOverlayWindow(_hwnd, self);

            InitializeDeviceAndSwapChain();
            Render();

            if (_overlayHwnd != IntPtr.Zero)
            {
                // èÌÇ…ëOñ 
                SetWindowPos(_overlayHwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }

            return new HandleRef(this, _hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DisposeD3D();

            if (_overlayHwnd != IntPtr.Zero)
            {
                DestroyWindow(_overlayHwnd);
                _overlayHwnd = IntPtr.Zero;
            }

            if (hwnd.Handle != IntPtr.Zero)
            {
                DestroyWindow(hwnd.Handle);
            }

            _hwnd = IntPtr.Zero;

            if (_thisHandle.IsAllocated)
            {
                _thisHandle.Free();
            }
        }

        protected override void OnWindowPositionChanged(System.Windows.Rect rcBoundingBox)
        {
            base.OnWindowPositionChanged(rcBoundingBox);

            if (_hwnd == IntPtr.Zero)
            {
                return;
            }

            var width = Math.Max(1, (int)Math.Round(rcBoundingBox.Width));
            var height = Math.Max(1, (int)Math.Round(rcBoundingBox.Height));

            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, width, height, SWP_NOZORDER | SWP_NOACTIVATE);

            if (_overlayHwnd != IntPtr.Zero)
            {
                SetWindowPos(_overlayHwnd, HWND_TOPMOST, 0, 0, width, height, SWP_NOACTIVATE);
            }

            ResizeSwapChain((uint)width, (uint)height);
            Render();

            if (_overlayHwnd != IntPtr.Zero)
            {
                InvalidateRect(_overlayHwnd, IntPtr.Zero, false);
            }
        }

        private static System.Windows.Size GetClientSizePixels(IntPtr hwnd)
        {
            GetClientRect(hwnd, out var rc);
            return new System.Windows.Size(Math.Max(1, rc.Right - rc.Left), Math.Max(1, rc.Bottom - rc.Top));
        }

        public void Render()
        {
            if (_context == null || _rtv == null || _swapChain == null)
            {
                return;
            }

            _context.OMSetRenderTargets(_rtv);
            _context.ClearRenderTargetView(_rtv, ClearColor);
            _swapChain.Present(1, PresentFlags.None);
        }

        public void SetOverlayStrokes(IEnumerable<IEnumerable<Point>> strokesDip)
        {
            if (strokesDip == null) throw new ArgumentNullException(nameof(strokesDip));

            lock (_overlayLock)
            {
                _overlayStrokes = strokesDip.Select(s => s.ToList()).ToList();
            }

            if (_overlayHwnd != IntPtr.Zero)
            {
                InvalidateRect(_overlayHwnd, IntPtr.Zero, false);
            }
        }

        private static IntPtr HostStaticWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_NCCREATE)
            {
                var cs = Marshal.PtrToStructure<CREATESTRUCT>(lParam);
                SetWindowLongPtr(hwnd, GWLP_USERDATA, cs.lpCreateParams);
            }

            var userData = GetWindowLongPtr(hwnd, GWLP_USERDATA);
            if (userData != IntPtr.Zero)
            {
                try
                {
                    var handle = GCHandle.FromIntPtr(userData);
                    if (handle.Target is DxSwapChainHost host)
                    {
                        return host.HostInstanceWndProc(hwnd, msg, wParam, lParam);
                    }
                }
                catch
                {
                }
            }

            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private IntPtr HostInstanceWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_SETCURSOR:
                    SetCursor(LoadCursor(IntPtr.Zero, IDC_ARROW));
                    return new IntPtr(1);

                case WM_LBUTTONDOWN:
                    SetCapture(hwnd);
                    RaisePointer(PointerDown, lParam);
                    return IntPtr.Zero;

                case WM_MOUSEMOVE:
                    if ((wParam.ToInt64() & MK_LBUTTON) != 0)
                    {
                        RaisePointer(PointerMove, lParam);
                        return IntPtr.Zero;
                    }
                    break;

                case WM_LBUTTONUP:
                    ReleaseCapture();
                    RaisePointer(PointerUp, lParam);
                    return IntPtr.Zero;
            }

            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private static IntPtr OverlayStaticWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_NCCREATE)
            {
                var cs = Marshal.PtrToStructure<CREATESTRUCT>(lParam);
                SetWindowLongPtr(hwnd, GWLP_USERDATA, cs.lpCreateParams);
            }

            var userData = GetWindowLongPtr(hwnd, GWLP_USERDATA);
            if (userData != IntPtr.Zero)
            {
                try
                {
                    var handle = GCHandle.FromIntPtr(userData);
                    if (handle.Target is DxSwapChainHost host)
                    {
                        return host.OverlayInstanceWndProc(hwnd, msg, wParam, lParam);
                    }
                }
                catch
                {
                }
            }

            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private IntPtr OverlayInstanceWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    // ì¸óÕÇÕâ∫Ç÷ìßâﬂ
                    return new IntPtr(HTTRANSPARENT);

                case WM_ERASEBKGND:
                    return new IntPtr(1);

                case WM_PAINT:
                    PAINTSTRUCT ps;
                    var hdc = BeginPaint(hwnd, out ps);
                    try
                    {
                        if (hdc != IntPtr.Zero)
                        {
                            DrawOverlayGdi(hdc);
                        }
                    }
                    finally
                    {
                        EndPaint(hwnd, ref ps);
                    }
                    return IntPtr.Zero;
            }

            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private void DrawOverlayGdi(IntPtr hdc)
        {
            List<List<Point>> strokes;
            lock (_overlayLock)
            {
                strokes = _overlayStrokes.Select(s => new List<Point>(s)).ToList();
            }

            if (strokes.Count == 0)
            {
                return;
            }

            var source = PresentationSource.FromVisual(this);
            var scale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

            var pen = CreatePen(PS_SOLID, 2, unchecked((int)0x000000));
            var oldPen = SelectObject(hdc, pen);

            try
            {
                foreach (var stroke in strokes)
                {
                    if (stroke.Count < 2) continue;

                    var p0 = stroke[0];
                    MoveToEx(hdc, (int)(p0.X * scale), (int)(p0.Y * scale), IntPtr.Zero);

                    for (var i = 1; i < stroke.Count; i++)
                    {
                        var p = stroke[i];
                        LineTo(hdc, (int)(p.X * scale), (int)(p.Y * scale));
                    }
                }
            }
            finally
            {
                SelectObject(hdc, oldPen);
                DeleteObject(pen);
            }
        }

        private void RaisePointer(EventHandler<HostPointerEventArgs> handler, IntPtr lParam)
        {
            if (handler == null)
            {
                return;
            }

            var x = GET_X_LPARAM(lParam);
            var y = GET_Y_LPARAM(lParam);

            var source = PresentationSource.FromVisual(this);
            var dpi = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            var dip = new Point(x / dpi, y / dpi);

            handler(this, new HostPointerEventArgs(dip));
        }

        private void InitializeDeviceAndSwapChain()
        {
            DisposeD3D();

            var creationFlags = DeviceCreationFlags.BgraSupport;

            D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                creationFlags,
                null,
                out _device,
                out _context);

            using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
            using var adapter = dxgiDevice.GetAdapter();
            using var factory = adapter.GetParent<IDXGIFactory2>();

            var size = GetClientSizePixels(_hwnd);
            var w = (uint)Math.Max(1, size.Width);
            var h = (uint)Math.Max(1, size.Height);

            var desc = new SwapChainDescription1
            {
                Width = w,
                Height = h,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription(1, 0),
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                AlphaMode = AlphaMode.Ignore,
                Flags = SwapChainFlags.None
            };

            _swapChain = factory.CreateSwapChainForHwnd(_device, _hwnd, desc);
            CreateRenderTarget();
        }

        private void ResizeSwapChain(uint width, uint height)
        {
            if (_swapChain == null || width == 0 || height == 0)
            {
                return;
            }

            try
            {
                _rtv?.Dispose();
                _rtv = null;

                _swapChain.ResizeBuffers(0, width, height, Format.Unknown, SwapChainFlags.None);
                CreateRenderTarget();
            }
            catch
            {
            }
        }

        private void CreateRenderTarget()
        {
            if (_device == null || _swapChain == null)
            {
                return;
            }

            using var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            _rtv = _device.CreateRenderTargetView(backBuffer);
        }

        private void DisposeD3D()
        {
            _rtv?.Dispose();
            _rtv = null;

            _swapChain?.Dispose();
            _swapChain = null;

            _context?.Dispose();
            _context = null;

            _device?.Dispose();
            _device = null;
        }

        private static IntPtr CreateHostWindow(IntPtr parent, IntPtr lpCreateParams)
        {
            const int WS_CHILD = 0x40000000;
            const int WS_VISIBLE = 0x10000000;

            var hInstance = GetModuleHandle(null);

            var wc = new WNDCLASS
            {
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(HostWndProcThunk),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = HostWindowClass
            };

            RegisterClass(ref wc);

            return CreateWindowEx(
                0,
                HostWindowClass,
                "",
                WS_CHILD | WS_VISIBLE,
                0,
                0,
                1,
                1,
                parent,
                IntPtr.Zero,
                hInstance,
                lpCreateParams);
        }

        private static IntPtr CreateOverlayWindow(IntPtr parent, IntPtr lpCreateParams)
        {
            const int WS_CHILD = 0x40000000;
            const int WS_VISIBLE = 0x10000000;
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int WS_EX_LAYERED = 0x00080000;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var hInstance = GetModuleHandle(null);

            var wc = new WNDCLASS
            {
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(OverlayWndProcThunk),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = OverlayWindowClass
            };

            RegisterClass(ref wc);

            return CreateWindowEx(
                WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE,
                OverlayWindowClass,
                "",
                WS_CHILD | WS_VISIBLE,
                0,
                0,
                1,
                1,
                parent,
                IntPtr.Zero,
                hInstance,
                lpCreateParams);
        }

        private const string HostWindowClass = "BrainCard.DxSwapChainHost";
        private const string OverlayWindowClass = "BrainCard.DxSwapChainHost.Overlay";

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_NCCREATE = 0x0081;
        private const int WM_SETCURSOR = 0x0020;
        private const int WM_PAINT = 0x000F;
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_NCHITTEST = 0x0084;

        private const long MK_LBUTTON = 0x0001;
        private const int IDC_ARROW = 32512;
        private const int HTTRANSPARENT = -1;

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        private const int GWLP_USERDATA = -21;
        private const int PS_SOLID = 0;

        private static int GET_X_LPARAM(IntPtr lp) => (short)((long)lp & 0xFFFF);
        private static int GET_Y_LPARAM(IntPtr lp) => (short)(((long)lp >> 16) & 0xFFFF);

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct CREATESTRUCT
        {
#pragma warning disable CS0649
            public IntPtr lpCreateParams;
            public IntPtr hInstance;
            public IntPtr hMenu;
            public IntPtr hwndParent;
            public int cy;
            public int cx;
            public int y;
            public int x;
            public int style;
            public IntPtr lpszName;
            public IntPtr lpszClass;
            public uint dwExStyle;
#pragma warning restore CS0649
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASS
        {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
#pragma warning disable CS0649
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
#pragma warning restore CS0649
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClass([In] ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int crColor);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lpPoint);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool LineTo(IntPtr hdc, int x, int y);
    }

    public sealed class HostPointerEventArgs : EventArgs
    {
        public HostPointerEventArgs(Point position)
        {
            Position = position;
        }

        public Point Position { get; }
    }
}
