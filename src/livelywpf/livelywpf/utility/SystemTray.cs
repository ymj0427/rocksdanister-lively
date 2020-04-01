using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace livelywpf.utility
{
    public class SystemTray : IDisposable
    {
        readonly public static NotifyIcon TrayIcon = new System.Windows.Forms.NotifyIcon();
        private static System.Windows.Forms.ToolStripMenuItem update_traybtn, pause_traybtn, configure_traybtn;
        public static void SetUpdateTrayBtnText(string txt)
        {
            update_traybtn.Text = txt;
        }

        public static void UpdateTrayBtnToggle(bool isEnable)
        {
            update_traybtn.Enabled = isEnable;
        }

        public SystemTray()
        {
            CreateSysTray();
        }

        private static void CreateSysTray()
        {
            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Rarely I get this error "The root Visual of a VisualTarget cannot have a parent..", hard to pinpoint not knowing how to recreate the error.
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            TrayIcon.DoubleClick += (s, args) => App.W.ShowMainWindow();
            TrayIcon.Icon = Properties.Icons.icons8_seed_of_life_96_normal;

            CreateContextMenu();
            TrayIcon.Visible = true;
        }

        private static void CreateContextMenu()
        {
            TrayIcon.ContextMenuStrip =
              new System.Windows.Forms.ContextMenuStrip();
            TrayIcon.Text = Properties.Resources.txtTitlebar;

            TrayIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuOpenLively, Properties.Icons.icon_monitor).Click += (s, e) => App.W.ShowMainWindow();
            TrayIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuCloseAll, Properties.Icons.icon_erase).Click += (s, e) => SetupDesktop.CloseAllWallpapers();
            update_traybtn = new System.Windows.Forms.ToolStripMenuItem(Properties.Resources.txtContextMenuUpdate1, Properties.Icons.icon_update);
            //update_traybtn.Click += (s, e) => Process.Start("https://github.com/rocksdanister/lively");
            update_traybtn.Click += (s, e) => App.W.ShowLivelyUpdateWindow();
            update_traybtn.Enabled = false;

            //todo:- store a "state" in setupdesktop, maintain that state even after wp change. (also checkmark this menu if paused)
            pause_traybtn = new System.Windows.Forms.ToolStripMenuItem("Pause All Wallpapers", Properties.Icons.icons8_pause_30);
            pause_traybtn.Click += (s, e) => ToggleWallpaperPlaybackState();
            TrayIcon.ContextMenuStrip.Items.Add(pause_traybtn);

            configure_traybtn = new System.Windows.Forms.ToolStripMenuItem("Customize Wallpaper", Properties.Icons.gear_color_48);
            configure_traybtn.Click += (s, e) => MainWindow.ShowCustomiseWidget();
            TrayIcon.ContextMenuStrip.Items.Add(configure_traybtn);

            TrayIcon.ContextMenuStrip.Items.Add("-");
            TrayIcon.ContextMenuStrip.Items.Add(update_traybtn);

            TrayIcon.ContextMenuStrip.Items.Add("-");

            TrayIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtSupport, Properties.Icons.icons8_heart_outline_16).Click += (s, e) => OpenSupportPage();
            TrayIcon.ContextMenuStrip.Items.Add("-");
            TrayIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuExit, Properties.Icons.icon_close).Click += (s, e) => App.W.ExitApplication();
        }

        private static void ToggleWallpaperPlaybackState()
        {
            if (App.W != null)
            {
                if (SetupDesktop.GetEngineState() == SetupDesktop.EngineState.normal)
                {
                    SetupDesktop.SetEngineState(SetupDesktop.EngineState.paused);
                    pause_traybtn.Checked = true;
                }
                else
                {
                    SetupDesktop.SetEngineState(SetupDesktop.EngineState.normal);
                    pause_traybtn.Checked = false;
                }
            }
        }

        public static void SwitchTrayIcon(bool isPaused)
        {
            try
            {
                //don't make much sense with per-display rule in multiple display systems, so turning off.
                if ((!MainWindow.Multiscreen || SaveData.config.WallpaperArrangement == SaveData.WallpaperArrangement.span) && !MainWindow._isExit)
                {
                    if (isPaused)
                    {
                        utility.SystemTray.TrayIcon.Icon = Properties.Icons.icons8_seed_of_life_96_pause;
                    }
                    else
                    {
                        utility.SystemTray.TrayIcon.Icon = Properties.Icons.icons8_seed_of_life_96_normal;
                    }
                }
            }
            catch (NullReferenceException)
            {
                //app closing.
            }
        }

        private static void OpenSupportPage()
        {
            try
            {
                Process.Start(@"https://ko-fi.com/rocksdanister");
            }
            catch { }
        }

        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass 
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //systraymenu dispose
                    TrayIcon.Visible = false;
                    TrayIcon.Icon.Dispose();
                    //_notifyIcon.Icon = null;
                    TrayIcon.Dispose();

                }

                // Indicate that the instance has been disposed.
                _disposed = true;
            }
        }
    }
}
