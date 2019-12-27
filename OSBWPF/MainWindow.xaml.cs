﻿using OSB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using vJoyInterfaceWrap;
using System.Linq;
using Microsoft.Win32;

namespace OSBWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IntPtr mPreviousForegroundWindow;
        static vJoy joystick;
        static Dictionary<string, BitmapImage> mImages = new Dictionary<string, BitmapImage>();
        static string OSB_BUTTON_PREFIX = "osb_btn_";
        OSBConfig Config;
        const double FOREGROUND_WINDOW_CHECK_INTERVAL = 100;
        DispatcherTimer timer = new DispatcherTimer();
        static bool WindowMoveEnabled = false;
        public MainWindow()
        {
            InitializeComponent();
            if (App.args.Length > 0)
            {
                Config = OSBConfig.Get(App.args[0]);
            }
            else
            {
                Config = OSBConfig.Get();
            }
            this.Topmost = true;
            InitJoystickDevice();
            mCanvas.MouseUp += OnCanvasMouseUp;
            RenderForm();
            timer.Interval = TimeSpan.FromMilliseconds(FOREGROUND_WINDOW_CHECK_INTERVAL);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        #region *** Event handlers ***

        private void WindowClosed(object sender, EventArgs e)
        {
            if (Config == null || joystick == null) { return; }
            try
            {
                //Release virtual joystick device
                if (joystick.vJoyEnabled()) {
                    joystick.RelinquishVJD(Config.VJDeviceId);
                }                
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Timer tick, periodically check foreground window so we can restore focus to that window after executing OSB button command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            IntPtr current = OSB.Core.win32.GetForegroundWindow();
            IntPtr thisHandle = new WindowInteropHelper(this).Handle;
            if (current != thisHandle)
            {
                mPreviousForegroundWindow = current;
            }
        }

        /// <summary>
        /// Handles OSB button press
        /// </summary>
        /// <param name="sender">Button being pressed</param>
        /// <param name="e"></param>
        private void OSBPress(object sender, MouseButtonEventArgs e)
        {
            OSBButtonTag tag = (OSBButtonTag)((Button)sender).Tag;
            int buttonIndex = tag.ButtonIndex;
            joystick.SetBtn(true, Config.VJDeviceId, (uint)tag.vJoyButtonId);
            SetButtonVisual(((Button)sender), Config.Buttons[buttonIndex], true);
        }

        /// <summary>
        /// Handles OSB button release
        /// </summary>
        /// <param name="sender">Button being released</param>
        /// <param name="e"></param>
        private void OSBRelease(object sender, MouseButtonEventArgs e)
        {
            OSBButtonTag tag = (OSBButtonTag)((Button)sender).Tag;
            int buttonIndex = tag.ButtonIndex;
            joystick.SetBtn(false, Config.VJDeviceId, (uint)tag.vJoyButtonId);
            RestorePrevious();
            SetButtonVisual(((Button)sender), Config.Buttons[buttonIndex], false);
        }

        /// <summary>
        /// Triggered when Exit button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitPress(object sender, MouseButtonEventArgs e) {
            SetButtonVisual(((Button)sender), Config.ExitButton, true);
        }

        /// <summary>
        /// Triggered when Exit button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitRelease(object sender, MouseButtonEventArgs e) {
            RestorePrevious();
            this.Close();
        }

        /// <summary>
        /// Triggered when reload button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReloadPress(object sender, MouseButtonEventArgs e) {
            OSBButtonTag tag = (OSBButtonTag)((Button)sender).Tag;
            SetButtonVisual(((Button)sender), Config.ReloadButton, true);
        }

        /// <summary>
        /// Triggered when reload button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReloadRelease(object sender, MouseButtonEventArgs e)
        {
            ReloadConfig();
        }

        /// <summary>
        /// Triggered when load button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadPress(object sender, MouseButtonEventArgs e)
        {
            SetButtonVisual(((Button)sender), Config.LoadButton, true);
        }

        /// <summary>
        /// Triggered when load button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadRelease(object sender, MouseButtonEventArgs e)
        {
            SetButtonVisual(((Button)sender), Config.LoadButton, false);
            LoadConfig();
        }


        /// <summary>
        /// Occurs when canvas was clicked (basically anywhere except buttons)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            RestorePrevious();
        }

        /// <summary>
        /// Context menu reload item click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnu_ReloadClick(object sender, RoutedEventArgs e)
        {
            ReloadConfig();
        }

        /// <summary>
        /// Menu Load config click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnu_LoadClick(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        /// <summary>
        /// Menu Exit item click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnu_ExitClick(object sender, RoutedEventArgs e)
        {
            RestorePrevious();
            this.Close();
        }
        #endregion

        /// <summary>
        /// Restores the window that was in the foreground before MATRIC OSB was clicked 
        /// </summary>
        private void RestorePrevious()
        {
            if (mPreviousForegroundWindow != IntPtr.Zero)
            {
                OSB.Core.win32.SetForegroundWindow(mPreviousForegroundWindow);
            }
        }

        /// <summary>
        /// Reloads the current config
        /// </summary>
        private void ReloadConfig() {
            Config = OSBConfig.Get(Config.ConfigFilePath);
            InitJoystickDevice();
            RenderForm();
            RestorePrevious();
        }

        /// <summary>
        /// Opens a dialog to browse for and load the configuration
        /// </summary>
        private void LoadConfig() {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), OSBConfig.DEFAULT_APP_SUBFOLDER);
            openFileDialog.Filter = "MATRIC OSB configuration (*.json)|*.json";
            if (openFileDialog.ShowDialog() == true)
            {
                Config = OSBConfig.Get(openFileDialog.FileName);
                InitJoystickDevice();
                RenderForm();
                RestorePrevious();
            }
        }

        /// <summary>
        /// Initializes the vjoy joystick device
        /// </summary>
        void InitJoystickDevice()
        {
            uint joystickId = Config.VJDeviceId;
            joystick = new vJoy();
            if (!joystick.vJoyEnabled())
            {
                MessageBox.Show("VJoy is not enabled, please fix VJoy configuration and try again.", "Could not initialize vjoy device", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            VjdStat status = joystick.GetVJDStatus(joystickId);
            string errorMessage = string.Empty;
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder", joystickId);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free", joystickId);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue", joystickId);
                    errorMessage = $@"vJoy Device {joystickId} is already owned by another feeder\nClose the application that is using vjoy device and try again.";
                    break;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue", joystickId);
                    errorMessage = $@"vJoy Device {joystickId} is not installed or disabled\nMake sure the vJoy device is enabled and try again.";
                    break;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue", joystickId);
                    errorMessage = $@"vJoy Device {joystickId} general error\nIs vjoy installed?";
                    break;
            };
            if (errorMessage != string.Empty)
            {
                MessageBoxResult dlgResult = MessageBox.Show(errorMessage, "Could not initialize vjoy device", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            // Acquire the target
            if (status == VjdStat.VJD_STAT_OWN || (status == VjdStat.VJD_STAT_FREE) && joystick.AcquireVJD(joystickId)){
                Console.WriteLine("Acquired: vJoy device number {0}.\n", joystickId);
            }
            else {
                Console.WriteLine($@"Failed to acquire vJoy device number {joystickId}.\nStatus = {status.ToString()}");
                return;
            }
        }

        /// <summary>
        /// Displays the OSB overlay
        /// </summary>
        protected void RenderForm() {
            Left = Config.X;
            Top = Config.Y;
            Width = Config.Width;
            Height = Config.Height;
            mWindowBackground.Width = Width;
            mWindowBackground.Height = Height;
            mImages.Clear();
            //Clear buttons
            mCanvas.Children.OfType<Button>().ToList().ForEach(b => mCanvas.Children.Remove(b));
            
            BitmapImage bitmap = GetImage(Config.MaskBitmap);
            if (bitmap != null)
            {
                mWindowBackground.Source = bitmap;
            }
            else {
                MessageBox.Show($@"Could not find mask {Config.MaskBitmap}");
                Environment.Exit(1);
            }

            #region Generate application control buttons
            if (Config.ExitButton != null) {
                Button btnExit = CreateButton(Config.ExitButton, null);
                btnExit.PreviewMouseDown += ExitPress;
                btnExit.PreviewMouseUp += ExitRelease;
                mCanvas.Children.Add(btnExit);
            }

            if (Config.ReloadButton != null) {
                Button btnReload = CreateButton(Config.ReloadButton, null);
                btnReload.PreviewMouseDown += ReloadPress;
                btnReload.PreviewMouseUp += ReloadRelease;
                mCanvas.Children.Add(btnReload);
            }

            if (Config.LoadButton != null) {
                Button btnLoad = CreateButton(Config.LoadButton, null);
                btnLoad.PreviewMouseDown += LoadPress;
                btnLoad.PreviewMouseUp += LoadRelease;
                mCanvas.Children.Add(btnLoad);
            }
            #endregion

            //Generate buttons OSB buttons
            int i = 0;
            foreach (OSBButton buttonConfig in Config.Buttons)
            {
                if (buttonConfig == null) { continue; }
                OSBButtonTag tag = null;
                if (buttonConfig.JoyBtnId != -1)
                {
                    tag = new OSBButtonTag(buttonConfig.JoyBtnId, i);
                }
                else
                {
                    tag = new OSBButtonTag(i + 1, i);
                }
                Button button = CreateButton(buttonConfig, tag);
                button.Name = $@"{OSB_BUTTON_PREFIX}{i}";
                button.PreviewMouseDown += OSBPress;
                button.PreviewMouseUp += OSBRelease;
                mCanvas.Children.Add(button);
                i++;
            }
        }

        /// <summary>
        /// Changes the button immage indicating the button state (pressed or not pressed)
        /// </summary>
        /// <param name="button">Buttton control</param>
        /// <param name="btnConfig">Button definition</param>
        /// <param name="pressed">true if button is pressed</param>
        private void SetButtonVisual(Button button, OSBButton btnConfig, bool pressed) {
            string imagePath = string.Empty;
            BitmapImage bitmap = null;
            if (pressed && !string.IsNullOrEmpty(btnConfig.ImageOn))
            {
                bitmap = GetImage(btnConfig.ImageOn);
            }
            else {
                if (!string.IsNullOrEmpty(btnConfig.ImageOff)) {
                    bitmap = GetImage(btnConfig.ImageOff);
                }
            }
            if (bitmap != null)
            {
                var brush = new ImageBrush();
                brush.ImageSource = bitmap;
                brush.Stretch = Stretch.Uniform;
                button.Background = brush;
            }
        }

        /// <summary>
        /// Creates a button control specified by OSB button definition
        /// </summary>
        /// <param name="btnConfig">Button definition</param>
        /// <param name="tag">Tag containing button index and vjoy device button number</param>
        /// <returns></returns>
        protected Button CreateButton(OSBButton btnConfig, OSBButtonTag tag)
        {
            Button button = new Button();
            button.Tag = tag;
            button.Margin = new Thickness(btnConfig.X, btnConfig.Y, 0, 0);
            button.Style = Resources["OSBButtonStyle"] as Style;
            button.BorderThickness = new Thickness(0, 0, 0, 0);
            button.Height = btnConfig.Height;
            button.Width = btnConfig.Width;
            BitmapImage bitmap = GetImage(btnConfig.ImageOff);
            if (bitmap != null) {
                var brush = new ImageBrush();
                brush.ImageSource = bitmap;
                brush.Stretch = Stretch.Uniform;
                button.Background = brush;
            }
            //Cache the pressed button image
            GetImage(btnConfig.ImageOn);
            return button;
        }

        /// <summary>
        /// Loads an image from the file system
        /// </summary>
        /// <param name="path">Path to image file</param>
        /// <returns>BitmapImage</returns>
        protected BitmapImage GetImage(string path)
        {
            if (path == null) {
                return null;
            }

            if (mImages.ContainsKey(path))
            {
                return mImages[path];
            }

            string absPath = $@"{path}";
            if (!File.Exists(path)) {
                //Try converting relative path to absolute
                absPath = System.IO.Path.Combine(Config.ConfigDir, path);
                if (mImages.ContainsKey(absPath))
                {
                    return mImages[absPath];
                }
            }

            if (File.Exists(absPath)) {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(absPath);
                bitmap.EndInit();
                mImages.Add(absPath, bitmap);
                return bitmap;
            }

            return null;
        }

        private void mnuWindowMoveChecked(object sender, RoutedEventArgs e)
        {
            WindowMoveEnabled = true;
        }

        private void mnuWindowMoveUnchecked(object sender, RoutedEventArgs e)
        {
            WindowMoveEnabled = false;
            Config.X = (int) this.Left;
            Config.Y = (int) this.Top;
            OSBConfig.Save(Config);
        }

        private void OnCanvasLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (WindowMoveEnabled)
            {
                base.OnMouseLeftButtonDown(e);
                // Begin dragging the window
                this.Cursor = Cursors.Hand;
                this.DragMove();
            }
        }

        private void OnCanvasLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }
    }
}