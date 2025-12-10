using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;

namespace SkyLineSQL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private HwndSource _source;
        private const int HOTKEY_ID = 1;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
            base.OnClosed(e);
        }

        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            const uint VK_PLUS = 0x6B;
            const uint VK_BACK_SLASH = 0xDC;
            const uint MOD_CTRL_SHIFT = 6;
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL_SHIFT, VK_PLUS))
            {
                // handle error
            }
            else if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL_SHIFT, VK_BACK_SLASH))
            {
                // handle error
            }
        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            OnHotKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private void OnHotKeyPressed()
        {
            if (this.IsVisible)
            {
                HideWindowLogic();
            }
            else
            {
                ShowWindowLogic();
            }
        }

        void HideWindowLogic()
        {
            this.Hide();
        }

        private double GetDpiFactor()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = g.DpiX;
                return dpiX / 96.0; // 96 is the standard DPI for WPF units
            }
        }

        void ShowWindowLogic()
        {
            //var activeScreen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            var activeScreen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            var workingArea = activeScreen.WorkingArea;

            var dpiFactor = GetDpiFactor();
            var wpfWorkingArea = new Rect(
                workingArea.X / dpiFactor,
                workingArea.Y / dpiFactor,
                workingArea.Width / dpiFactor,
                workingArea.Height / dpiFactor
            );

            // Position the window in the center of the active screen's working area
            this.Left = wpfWorkingArea.Left + (wpfWorkingArea.Width - this.Width) / 2;
            this.Top = wpfWorkingArea.Top + (wpfWorkingArea.Height - this.Height) / 2;

            //this.Show();
            //this.Activate();

            // --- critical repairs ---
            this.Show();
            this.Activate();
            // -------------------------

            //SearchBox_txt.Clear();
            SearchBox_txt.Focus();
            SearchBox_txt.SelectAll();
            SearchBox_txt.ForceCursor = true;
        }

        MainWindowViewModel vm;
        public MainWindow()
        {
            InitializeComponent();

            vm = new MainWindowViewModel();
            this.DataContext = vm;

            ShowWindowLogic();
        }

        private void dg_source_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (vm.SelectedIndex > -1 && vm.DatabaseObjects.Count > 0)
                dg_source.ScrollIntoView(vm.DatabaseObjects[vm.SelectedIndex]);
        }
    }
}
