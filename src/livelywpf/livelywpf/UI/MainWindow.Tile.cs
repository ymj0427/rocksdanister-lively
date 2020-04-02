using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf
{
    /// <summary>
    /// "Type" page metro tile events.
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        private void Tile_Video_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "All Videos Files |*.dat; *.wmv; *.3g2; *.3gp; *.3gp2; *.3gpp; *.amv; *.asf;  *.avi; *.bin; *.cue; *.divx; *.dv; *.flv; *.gxf; *.iso; *.m1v; *.m2v; *.m2t; *.m2ts; *.m4v; " +
                  " *.mkv; *.mov; *.mp2; *.mp2v; *.mp4; *.mp4v; *.mpa; *.mpe; *.mpeg; *.mpeg1; *.mpeg2; *.mpeg4; *.mpg; *.mpv2; *.mts; *.nsv; *.nuv; *.ogg; *.ogm; *.ogv; *.ogx; *.ps; *.rec; *.rm; *.rmvb; *.tod; *.ts; *.tts; *.vob; *.vro; *.webm"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.video);
            }

        }

        private void Tile_GIF_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Filter = "Animated GIF (*.gif) |*.gif"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.gif);
            }

        }

        private async void Tile_Unity_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningUnity == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle, Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                           new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningUnity++;
                    SaveData.SaveConfig();
                }
            }
            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Title = "Select Unity game",
                Filter = "Executable |*.exe"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.unity);
            }
        }

        private async void Tile_UNITY_AUDIO_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningUnity == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle, Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                           new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningUnity++;
                    SaveData.SaveConfig();
                }
            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Title = "Select Unity audio visualiser",
                Filter = "Executable |*.exe"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.unity_audio);
            }

        }

        private void Tile_LIVELY_ZIP_Click(object sender, RoutedEventArgs e)
        {
            //tabControl1.SelectedIndex = 0; //switch to library tab.
            Button_Click_InstallWallpaper(this, null);
        }

        private async void Tile_Godot_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningGodot == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle, Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                           new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningGodot++;
                    SaveData.SaveConfig();
                }
            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Title = "Select Godot game",
                Filter = "Executable |*.exe"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.godot);
            }
        }

        private async void Tile_BizHawk_Click(object sender, RoutedEventArgs e)
        {
            WpfNotification(NotificationType.info, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.txtComingSoon);
            return;

            var dir = Directory.GetFiles(App.PathData + @"\external\bizhawk", "EmuHawk.exe", SearchOption.AllDirectories); //might be slow, only check top?
            if (dir.Length != 0)
            {
                SetupWallpaper(dir[0], SetupDesktop.WallpaperType.bizhawk);
            }
            else if (File.Exists(SaveData.config.BizHawkPath))
            {
                SetupWallpaper(SaveData.config.BizHawkPath, SetupDesktop.WallpaperType.bizhawk);
            }
            else
            {
                var ch = await this.ShowMessageAsync("Bizhawk Not Found", "Download BizHawk Emulator:\nhttps://github.com/TASVideos/BizHawk\nExtract & copy contents to:\nexternal\\bizhawk folder" +
                        "\n\n\t\tOR\n\nClick Browse & select EmuHawk.exe", MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings() { AffirmativeButtonText = "Ok", NegativeButtonText = "Browse", DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Theme, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Affirmative) //Ok
                {
                    return;
                }
                else if (ch == MessageDialogResult.Negative) //Browse
                {

                    OpenFileDialog openFileDialog1 = new OpenFileDialog
                    {
                        Title = "Select EmuHawk.exe",
                        FileName = "EmuHawk.exe"
                    };
                    // openFileDialog1.Filter = formatsVideo;
                    if (openFileDialog1.ShowDialog() == true)
                    {
                        SaveData.config.BizHawkPath = openFileDialog1.FileName;
                        SaveData.SaveConfig();

                        SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.bizhawk);
                    }
                }
            }

        }

        private async void Tile_Other_Click(object sender, RoutedEventArgs e)
        {
            var ch = await this.ShowMessageAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.txtLivelyAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                       new MetroDialogSettings()
                       {
                           DialogTitleFontSize = 18,
                           ColorScheme = MetroDialogColorScheme.Inverted,
                           DialogMessageFontSize = 16,
                           AnimateHide = false,
                           AnimateShow = false
                       });

            if (ch == MessageDialogResult.Negative)
                return;
            else if (ch == MessageDialogResult.Affirmative)
            {

            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Application (*.exe) |*.exe"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.app);
            }
        }

        private void Tile_HTML_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Web Page (*.html) |*.html"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.web);
            }
        }


        private void Tile_HTML_AUDIO_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Web Page with visualiser (*.html) |*.html"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.web_audio);
            }
        }

        private async void Tile_Video_stream_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningURL == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgUrlWarningTitle, Properties.Resources.msgUrlWarning, MessageDialogStyle.AffirmativeAndNegative,
                                new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningURL++;
                    SaveData.SaveConfig();
                }
            }

            var url = await this.ShowInputAsync("Stream", "Load online video..", new MetroDialogSettings()
            {
                DialogTitleFontSize = 16,
                DialogMessageFontSize = 14,
                DefaultText = String.Empty,
                AnimateHide = false,
                AnimateShow = false
            });
            if (string.IsNullOrEmpty(url))
                return;

            SetupWallpaper(url, SetupDesktop.WallpaperType.video_stream);
        }

        private async void Tile_URL_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningURL == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgUrlWarningTitle, Properties.Resources.msgUrlWarning, MessageDialogStyle.AffirmativeAndNegative,
                                new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningURL++;
                    SaveData.SaveConfig();
                }
            }

            var url = await this.ShowInputAsync(Properties.Resources.msgUrlLoadTitle, Properties.Resources.msgUrlLoad, new MetroDialogSettings() { DialogTitleFontSize = 16, DialogMessageFontSize = 14, DefaultText = SaveData.config.DefaultURL });
            if (string.IsNullOrEmpty(url))
                return;

            SaveData.config.DefaultURL = url;
            SaveData.SaveConfig();

            WebLoadDragDrop(url);
            //SetupWallpaper(url, SetupDesktop.WallpaperType.url);
        }

    }
}
