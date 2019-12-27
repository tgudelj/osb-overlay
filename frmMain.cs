using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using vJoyInterfaceWrap;

namespace OSB {
    public partial class frmMain : Form {
        IntPtr mPreviousForegroundWindow;
        static vJoy joystick;
        static vJoy.JoystickState iReport;
        static Dictionary<string, Image> mImages;
        static string OSB_BUTTON_PREFIX = "osb_btn_";
        Button btnReload;
        Button btnExit;
        /// <summary>
        /// Application command button types
        /// </summary>
        public enum CommandButtonType {
            ReloadConfig,
            ExitApp
        }

        #region Win32 API

        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        //enum GetWindow_Cmd : uint {
        //    GW_HWNDFIRST = 0,
        //    GW_HWNDLAST = 1,
        //    GW_HWNDNEXT = 2,
        //    GW_HWNDPREV = 3,
        //    GW_OWNER = 4,
        //    GW_CHILD = 5,
        //    GW_ENABLEDPOPUP = 6
        //}
        //[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        //public static extern IntPtr GetParent(IntPtr hWnd);
        //[DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool SetForegroundWindow(IntPtr hWnd);
        //[DllImport("user32.dll")]
        //static extern IntPtr GetForegroundWindow();
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        //[DllImport("user32.dll")]
        //static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //const int GWL_EXSTYLE = -20;
        //const int WS_EX_LAYERED = 0x80000;
        //const int WS_EX_TRANSPARENT = 0x20;
        #endregion

        public frmMain()
        {
            InitializeComponent();
            InitJoystickDevice();
            mImages = new Dictionary<string, Image>();
            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
            this.TopMost = true;

            mTimer.Interval = 300;
            mTimer.Start();
            RenderForm();
        }

        /// <summary>
        /// Handles OSB button press
        /// </summary>
        /// <param name="sender">Button being pressed</param>
        /// <param name="e"></param>
        private void OSBPress(object sender, MouseEventArgs e)
        {
            OSBButtonTag tag = (OSBButtonTag)((Button)sender).Tag;
            int buttonIndex = tag.ButtonIndex;
            joystick.SetBtn(true, Program.Config.VJDeviceId, (uint)tag.vJoyButtonId); //VJoj buttons have 1-based indexes
            if (Program.Config.Buttons[buttonIndex].ImageOn != null)
            {
                string key = Program.Config.Buttons[buttonIndex].ImageOn;
                ((Button)sender).BackgroundImage = GetImage(key);
            }
        }

        /// <summary>
        /// Handles OSB button release
        /// </summary>
        /// <param name="sender">Button being released</param>
        /// <param name="e"></param>
        private void OSBRelease(object sender, MouseEventArgs e)
        {
            OSBButtonTag tag = (OSBButtonTag)((Button)sender).Tag;
            int buttonIndex = tag.ButtonIndex;
            joystick.SetBtn(false, Program.Config.VJDeviceId, (uint)tag.vJoyButtonId);
            RestorePrevious();
            if (Program.Config.Buttons[buttonIndex].ImageOff != null)
            {
                string key = Program.Config.Buttons[buttonIndex].ImageOff;
                ((Button)sender).BackgroundImage = GetImage(key);
            }

        }

        private void ExitClick(object sender, EventArgs e) {
            RestorePrevious();
            this.Close();
        }

        private void ReloadClick(object sender, EventArgs e) {
            Program.Config = OSBConfig.Get(Program.Config.ConfigFilePath);
            RenderForm();
            RestorePrevious();
        }


        /// <summary>
        /// Occurs on timer event, saves the current foreground window
        /// so we can restore it after OSB click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        private void mTimer_Tick(object sender, EventArgs e)
        {
            IntPtr current = GetForegroundWindow();
            IntPtr thisHandle = this.Handle;
            if (current != thisHandle)
            {
                mPreviousForegroundWindow = current;
            }
        }

        protected bool ClearButtons() {
            foreach (Control ctl in this.Controls)
            {
                if (ctl.Name.StartsWith(OSB_BUTTON_PREFIX))
                {
                    this.Controls.Remove(ctl);
                    return true;
                }
            }
            return false;
        }

        protected void RenderForm() {
            //Clear buttons
            while (ClearButtons()) {
                ClearButtons();
            }

            if (btnReload != null) {
                this.Controls.Remove(btnReload);
            }

            if (btnExit != null)
            {
                this.Controls.Remove(btnExit);
            }

            //Clear image cache
            mImages.Clear();

            //Create application command buttons
            btnReload = CreateCommandButton(Program.Config.ReloadButton, CommandButtonType.ReloadConfig);
            this.Controls.Add(btnReload);
            if (Program.Config.ReloadButton.ImageOn != null)
            {
                btnReload.BackgroundImage = GetImage(Program.Config.ReloadButton.ImageOn);
            }
            btnExit = CreateCommandButton(Program.Config.ExitButton, CommandButtonType.ExitApp);
            this.Controls.Add(btnExit);


            string imagePath = Program.Config.MaskBitmap;
            //Set form dimensions and position
            this.Location = new Point(Program.Config.X, Program.Config.Y);
            this.Width = Program.Config.Width;
            this.Height = Program.Config.Height;
            //Set form background
            if (!File.Exists(imagePath)) {
                imagePath = Path.Combine(Program.Config.ConfigDir, imagePath);
            }
            if (File.Exists(imagePath))
            {
                //Load bitmap and set form background to bitmap
                this.BackgroundImage = GetImage(imagePath);
                this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            }
            //Generate buttons
            int i = 0;
            foreach (OSBButton button in Program.Config.Buttons) {
                if (button == null) { continue; }
                OSBButtonTag tag = null;
                if (button.JoyBtnId != -1)
                {
                    tag = new OSBButtonTag(button.JoyBtnId, i);
                }
                else {
                    tag = new OSBButtonTag(i + 1, i);
                }
                Button btn = CreateButton(button, tag);
                this.Controls.Add(btn);
                i++;
            }

            if (Program.Config.ReloadButton.ImageOff != null) {
                btnReload.BackgroundImage = GetImage(Program.Config.ReloadButton.ImageOff);
            }
        }

        protected Button CreateButton(OSBButton btnConfig, OSBButtonTag tag) {
            Button button = new Button();
            string imagePath = "";
            if (btnConfig.ImageOff != null) {
                if (!File.Exists(btnConfig.ImageOff))
                {
                    imagePath = Path.Combine(Program.Config.ConfigDir, btnConfig.ImageOff);
                }
                if (File.Exists(imagePath))
                {
                    button.BackgroundImage = GetImage(imagePath);
                }
            }

            imagePath = "";
            if (btnConfig.ImageOn != null)
            {
                if (!File.Exists(btnConfig.ImageOn))
                {
                    imagePath = Path.Combine(Program.Config.ConfigDir, btnConfig.ImageOn);
                }
                if (File.Exists(imagePath))
                {
                    //Cache image
                    GetImage(imagePath);
                }
            }
            button.Name = OSB_BUTTON_PREFIX + tag.ButtonIndex.ToString();
            button.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            button.BackColor = System.Drawing.Color.Transparent;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button.Location = new System.Drawing.Point(btnConfig.X, btnConfig.Y);
            button.Size = new System.Drawing.Size(btnConfig.Width, btnConfig.Height);
            button.TabStop = false;
            button.Tag = tag;
            button.UseVisualStyleBackColor = false;
            button.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OSBPress);
            button.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OSBRelease);
            //debug
            //button.ForeColor = Color.White;
            //button.Text = tag.ButtonIndex.ToString();
            //end debug
            Console.WriteLine($@"Created button [{tag.ButtonIndex}] {button.Name} vjoy #{tag.vJoyButtonId}");
            return button;
        }

        protected Button CreateCommandButton(OSBButton btnConfig, CommandButtonType buttonType)
        {
            Button button = new Button();
            string imagePath = "";
            if (btnConfig.ImageOff != null)
            {
                if (!File.Exists(btnConfig.ImageOff))
                {
                    imagePath = Path.Combine(Program.Config.ConfigDir, btnConfig.ImageOff);
                }
                if (File.Exists(imagePath))
                {
                    button.BackgroundImage = GetImage(imagePath);
                }
                if (mImages.ContainsKey(imagePath))
                {
                    button.MouseUp += (sender, args) => {
                        ((Button)sender).BackgroundImage = GetImage(btnConfig.ImageOff);
                    };
                }
            }

            imagePath = "";
            if (btnConfig.ImageOn != null)
            {
                if (!File.Exists(btnConfig.ImageOn))
                {
                    imagePath = Path.Combine(Program.Config.ConfigDir, btnConfig.ImageOn);
                }
                if (File.Exists(imagePath))
                {
                    //Cache image
                    GetImage(imagePath);
                }
                if (mImages.ContainsKey(imagePath)) {
                    button.MouseDown += (sender, args) => {
                        ((Button)sender).BackgroundImage = GetImage(btnConfig.ImageOn);
                    };
                }
            }

            button.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            button.FlatAppearance.BorderSize = 0;

            if (button.BackgroundImage != null) {
                button.BackColor = System.Drawing.Color.Transparent;
                button.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
                button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            }
            button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button.Location = new System.Drawing.Point(btnConfig.X, btnConfig.Y);
            button.Size = new System.Drawing.Size(btnConfig.Width, btnConfig.Height);
            button.TabStop = false;
            button.UseVisualStyleBackColor = false;
           
            switch (buttonType)
            {
                case CommandButtonType.ReloadConfig:
                    button.Click += new EventHandler(this.ReloadClick);
                    break;
                case CommandButtonType.ExitApp:
                    button.Click += new EventHandler(this.ExitClick);
                    break;
            }
            return button;
        }

        /// <summary>
        /// Loads an image from the file system
        /// </summary>
        /// <param name="path">Path to image file</param>
        /// <returns>Image</returns>
        protected Image GetImage(string path)
        {
            if (mImages.ContainsKey(path))
            {
                return mImages[path];
            }
            else
            {
                if (File.Exists(path))
                {
                    using (var temp = new Bitmap(path))
                    {
                        Image image = new Bitmap(temp);
                        mImages[path] = image;
                        return image;
                    }
                }
                else
                {
                    path = Path.Combine(Program.Config.ConfigDir, path);
                    if (File.Exists(path))
                    {
                        using (var temp = new Bitmap(path))
                        {
                            Image image = new Bitmap(temp);
                            mImages[path] = image;
                            return image;
                        }
                    }
                }

            }
            return null;
        }

        private void RestorePrevious() {
            //IntPtr lastWindowHandle = GetWindow(Process.GetCurrentProcess().MainWindowHandle, (uint)GetWindow_Cmd.GW_HWNDNEXT);
            //while (true)
            //{
            //    IntPtr temp = GetParent(lastWindowHandle);
            //    if (temp.Equals(IntPtr.Zero)) break;
            //    lastWindowHandle = temp;
            //}
            if (mPreviousForegroundWindow != IntPtr.Zero) {
                SetForegroundWindow(mPreviousForegroundWindow);
            }            
        }

        void InitJoystickDevice() {
            uint joystickId = Program.Config.VJDeviceId;
            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();
            if (!joystick.vJoyEnabled()) {
                return;
            }

            VjdStat status = joystick.GetVJDStatus(joystickId);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", joystickId);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", joystickId);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", joystickId);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", joystickId);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", joystickId);
                    return;
            };
            int nButtons = joystick.GetVJDButtonNumber(joystickId);
            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(joystickId))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", joystickId);
                return;
            }
            else
            {
                Console.WriteLine("Acquired: vJoy device number {0}.\n", joystickId);
            }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            joystick.RelinquishVJD(Program.Config.VJDeviceId);
            Environment.Exit(0);
        }

        private void FrmMain_MouseUp(object sender, MouseEventArgs e)
        {
            RestorePrevious();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            //var style = GetWindowLong(this.Handle, GWL_EXSTYLE);
            //SetWindowLong(this.Handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
    }
}
