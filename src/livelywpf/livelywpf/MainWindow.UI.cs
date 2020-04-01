using Enterwell.Clients.Wpf.Notifications;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static livelywpf.SaveData;

namespace livelywpf
{
    public partial class MainWindow : MetroWindow
    {
        #region ui_events
        private void SubcribeUI()
        {
            //Subscribe to events here to prevent triggering calls during RestoreSaveSettings() todo: rewrite with data binding instead(Enum type need some extra code for convertion, skipping for now).
            comboBoxVideoPlayer.SelectionChanged += ComboBoxVideoPlayer_SelectionChanged;
            comboBoxGIFPlayer.SelectionChanged += ComboBoxGIFPlayer_SelectionChanged;
            comboBoxFocusedPerf.SelectionChanged += ComboBoxFocusedPerf_SelectionChanged;
            comboBoxFullscreenPerf.SelectionChanged += ComboBoxFullscreenPerf_SelectionChanged;
            comboBoxMonitorPauseRule.SelectionChanged += ComboBoxMonitorPauseRule_SelectionChanged;
            transparencyToggle.IsCheckedChanged += TransparencyToggle_IsCheckedChanged;
            StartupToggle.IsCheckedChanged += StartupToggle_IsCheckedChanged;
            videoMuteToggle.IsCheckedChanged += VideoMuteToggle_IsCheckedChanged;
            comboBoxFullscreenPerf.SelectionChanged += ComboBoxFullscreenPerf_SelectionChanged1;
            cefAudioInMuteToggle.IsCheckedChanged += CefAudioInMuteToggle_IsCheckedChanged;
            comboBoxPauseAlgorithm.SelectionChanged += ComboBoxPauseAlgorithm_SelectionChanged;
            TileAnimateToggle.IsCheckedChanged += TileAnimateToggle_IsCheckedChanged;
            appMuteToggle.IsCheckedChanged += AppMuteToggle_IsCheckedChanged;
            //fpsUIToggle.IsCheckedChanged += FpsUIToggle_IsCheckedChanged;
            //disableUIHWToggle.IsCheckedChanged += DisableUIHWToggle_IsCheckedChanged;
            comboBoxLanguage.SelectionChanged += ComboBoxLanguage_SelectionChanged;
            comboBoxTheme.SelectionChanged += ComboBoxTheme_SelectionChanged;
            audioFocusedToggle.IsCheckedChanged += AudioFocusedToggle_IsCheckedChanged;
            cmbBoxStreamQuality.SelectionChanged += CmbBoxStreamQuality_SelectionChanged;
            transparencySlider.ValueChanged += TransparencySlider_ValueChanged;
            TileGenerateToggle.IsCheckedChanged += TileGenerateToggle_IsCheckedChanged;
            comboBoxVideoPlayerScaling.SelectionChanged += ComboBoxVideoPlayerScaling_SelectionChanged;
            comboBoxGIFPlayerScaling.SelectionChanged += ComboBoxGIFPlayerScaling_SelectionChanged;
            comboBoxBatteryPerf.SelectionChanged += ComboBoxBatteryPerf_SelectionChanged;
            comboBoxWpInputSettings.SelectionChanged += ComboBoxWpInputSettings_SelectionChanged;
            chkboxMouseOtherAppsFocus.Checked += ChkboxMouseOtherAppsFocus_Checked;
            chkboxMouseOtherAppsFocus.Unchecked += ChkboxMouseOtherAppsFocus_Checked;
        }

        private void ChkboxMouseOtherAppsFocus_Checked(object sender, RoutedEventArgs e)
        {
            SaveData.config.MouseInputMovAlways = chkboxMouseOtherAppsFocus.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void ComboBoxWpInputSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxWpInputSettings.SelectedIndex == 0)
            {
                WallpaperInputForwardingToggle(false);
            }
            else if (comboBoxWpInputSettings.SelectedIndex == 1)
            {
                WallpaperInputForwardingToggle(true);
            }
            SaveData.config.InputForwardMode = comboBoxWpInputSettings.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void ComboBoxBatteryPerf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.BatteryPause = (SaveData.AppRulesEnum)comboBoxBatteryPerf.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void ComboBoxGIFPlayerScaling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.GifScaler = (Stretch)comboBoxGIFPlayerScaling.SelectedIndex;
            SaveData.SaveConfig();

            var result = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.gif);
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.gif);

            if (result.Count != 0)
            {
                RestoreWallpaper(result);
            }
        }

        private void ComboBoxVideoPlayerScaling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.VideoScaler = (Stretch)comboBoxVideoPlayerScaling.SelectedIndex;
            SaveData.SaveConfig();

            var videoWp = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.video); //youtube is started as apptype, not included!
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.video);

            if (videoWp.Count != 0)
            {
                RestoreWallpaper(videoWp);
            }
        }

        private void TileGenerateToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.GenerateTile = TileGenerateToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.Opacity = transparencySlider.Value;
            SaveData.config.AppTransparencyPercent = transparencySlider.Value;
            SaveData.SaveConfig();
        }

        private void CmbBoxStreamQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBoxStreamQuality.SelectedIndex == -1)
                return;

            SaveData.config.StreamQuality = (SaveData.StreamQualitySuggestion)cmbBoxStreamQuality.SelectedIndex;
            SaveData.SaveConfig();

            var streamWP = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.video_stream);
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.video_stream);

            if (streamWP.Count != 0)
            {
                RestoreWallpaper(streamWP);
            }

        }

        private void AudioFocusedToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.AlwaysAudio = audioFocusedToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void ComboBoxTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxTheme.SelectedIndex == -1)
                return;
            //todo:- do it more elegantly.
            SaveData.config.Theme = comboBoxTheme.SelectedIndex;
            //SaveData.SaveConfig();

            RestartLively();
        }

        private void AppMuteToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.MuteAppWP = !appMuteToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void RestoreMenuSettings()
        {
            chkboxMouseOtherAppsFocus.IsChecked = SaveData.config.MouseInputMovAlways;

            if (SaveData.config.InputForwardMode == 1)
            {
                WallpaperInputForwardingToggle(true);
            }

            try
            {
                comboBoxWpInputSettings.SelectedIndex = SaveData.config.InputForwardMode;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.InputForwardMode = 1;
                SaveData.SaveConfig();
                comboBoxWpInputSettings.SelectedIndex = 1;
            }

            if (!App.isPortableBuild)
            {
                lblPortableTxt.Visibility = Visibility.Collapsed;
            }

            if (SaveData.config.AppTransparency)
            {
                if (SaveData.config.AppTransparencyPercent >= 0.5 && SaveData.config.AppTransparencyPercent <= 0.9)
                {
                    this.Opacity = SaveData.config.AppTransparencyPercent;
                }
                else
                {
                    this.Opacity = 0.9f;
                }
                transparencyToggle.IsChecked = true;
                transparencySlider.IsEnabled = true;
            }
            else
            {
                this.Opacity = 1.0f;
                transparencyToggle.IsChecked = false;
                transparencySlider.IsEnabled = false;
            }

            if (SaveData.config.AppTransparencyPercent >= 0.5 && SaveData.config.AppTransparencyPercent <= 0.9)
                transparencySlider.Value = SaveData.config.AppTransparencyPercent;
            else
                transparencySlider.Value = 0.9f;

            audioFocusedToggle.IsChecked = SaveData.config.AlwaysAudio;
            TileGenerateToggle.IsChecked = SaveData.config.GenerateTile;
            appMuteToggle.IsChecked = !SaveData.config.MuteAppWP;
            TileAnimateToggle.IsChecked = SaveData.config.LiveTile;
            //fpsUIToggle.IsChecked = SaveData.config.Ui120FPS;
            //disableUIHWToggle.IsChecked = SaveData.config.UiDisableHW;

            try
            {
                comboBoxGIFPlayerScaling.SelectedIndex = (int)SaveData.config.GifScaler;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.GifScaler = Stretch.UniformToFill;
                SaveData.SaveConfig();
                comboBoxGIFPlayerScaling.SelectedIndex = (int)SaveData.config.GifScaler;
            }

            try
            {
                comboBoxBatteryPerf.SelectedIndex = (int)SaveData.config.BatteryPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.BatteryPause = AppRulesEnum.ignore;
                SaveData.SaveConfig();
                comboBoxBatteryPerf.SelectedIndex = (int)SaveData.config.BatteryPause;
            }

            try
            {
                comboBoxVideoPlayerScaling.SelectedIndex = (int)SaveData.config.VideoScaler;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.VideoScaler = Stretch.UniformToFill;
                SaveData.SaveConfig();
                comboBoxVideoPlayerScaling.SelectedIndex = (int)SaveData.config.VideoScaler;
            }

            try
            {
                comboBoxPauseAlgorithm.SelectedIndex = (int)SaveData.config.ProcessMonitorAlgorithm;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.ProcessMonitorAlgorithm = SaveData.ProcessMonitorAlgorithm.foreground;
                SaveData.SaveConfig();
                comboBoxPauseAlgorithm.SelectedIndex = (int)SaveData.config.ProcessMonitorAlgorithm;
            }

            //stream
            try
            {
                cmbBoxStreamQuality.SelectedIndex = (int)SaveData.config.StreamQuality;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.StreamQuality = SaveData.StreamQualitySuggestion.h720p;
                SaveData.SaveConfig();
                cmbBoxStreamQuality.SelectedIndex = (int)SaveData.config.StreamQuality;
            }


            cefAudioInMuteToggle.IsChecked = !SaveData.config.MuteCefAudioIn;
            if (!SaveData.config.MuteCefAudioIn)
                web_audio_WarningText.Visibility = Visibility.Hidden;
            else
                web_audio_WarningText.Visibility = Visibility.Visible;

            videoMuteToggle.IsChecked = !SaveData.config.MuteVideo;

            // performance ui

            try
            {
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFullscreenPause = SaveData.AppRulesEnum.pause;
                SaveData.SaveConfig();
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }

            try
            {
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFocusPause = SaveData.AppRulesEnum.ignore;
                SaveData.SaveConfig();
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }

            try
            {
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFullscreenPause = SaveData.AppRulesEnum.pause;
                SaveData.SaveConfig();
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }

            try
            {
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.DisplayPauseSettings = SaveData.DisplayPauseEnum.perdisplay;
                SaveData.SaveConfig();
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }

            try
            {
                comboBoxVideoPlayer.SelectedIndex = (int)SaveData.config.VidPlayer;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.VidPlayer = SaveData.VideoPlayer.windowsmp;
                SaveData.SaveConfig();
                comboBoxVideoPlayer.SelectedIndex = (int)SaveData.config.VidPlayer;
            }

            try
            {
                comboBoxGIFPlayer.SelectedIndex = (int)SaveData.config.GifPlayer;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.VidPlayer = (int)SaveData.GIFPlayer.xaml;
                SaveData.SaveConfig();
                comboBoxGIFPlayer.SelectedIndex = (int)SaveData.config.GifPlayer;
            }

            try
            {
                StartupToggle.IsChecked = utility.SystemStartup.CheckStartupRegistry();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }

            #region ugly
            //todo:- do it more elegantly.
            //language
            foreach (var item in SaveData.supportedLanguages)
            {
                comboBoxLanguage.Items.Add(item.Language);
            }

            bool found = false;
            for (int i = 0; i < SaveData.supportedLanguages.Length; i++)
            {
                if (Array.Exists(SaveData.supportedLanguages[i].Codes, x => x.Equals(SaveData.config.Language, StringComparison.OrdinalIgnoreCase)))
                {
                    comboBoxLanguage.SelectedIndex = i;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                comboBoxLanguage.SelectedIndex = 0; //en-US
            }

            //theme
            foreach (var item in SaveData.livelyThemes)
            {
                comboBoxTheme.Items.Add(item.Name);
            }

            try
            {
                comboBoxTheme.SelectedIndex = SaveData.config.Theme;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.Theme = 0; //DarkLime
                SaveData.SaveConfig();
                comboBoxTheme.SelectedIndex = SaveData.config.Theme;
            }

            #endregion ugly

        }

        private void ComboBoxPauseAlgorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxPauseAlgorithm.SelectedIndex == 1)
            {
                if (Multiscreen)
                {
                    comboBoxPauseAlgorithm.SelectedIndex = 0;
                    WpfNotification(NotificationType.info, Properties.Resources.txtLivelyErrorMsgTitle, "Currently this algorithm is incomplete in multiple display systems, disabling.");
                    return;
                }
            }

            SaveData.config.ProcessMonitorAlgorithm = (SaveData.ProcessMonitorAlgorithm)comboBoxPauseAlgorithm.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void CefAudioInMuteToggle_IsCheckedChanged(object sender, EventArgs e)
        {

            SaveData.config.MuteCefAudioIn = !cefAudioInMuteToggle.IsChecked.Value;
            SaveData.SaveConfig();

            if (!SaveData.config.MuteCefAudioIn)
                web_audio_WarningText.Visibility = Visibility.Hidden;
            else
                web_audio_WarningText.Visibility = Visibility.Visible;
        }

        private void ComboBoxFullscreenPerf_SelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.AppFullscreenPause = (SaveData.AppRulesEnum)comboBoxFullscreenPerf.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void VideoMuteToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.MuteVideo = !videoMuteToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void ComboBoxMonitorPauseRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.DisplayPauseSettings = (SaveData.DisplayPauseEnum)comboBoxMonitorPauseRule.SelectedIndex;
            try
            {
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.DisplayPauseSettings = SaveData.DisplayPauseEnum.perdisplay;
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }
            SaveData.SaveConfig();
        }

        private void ComboBoxFullscreenPerf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.AppFullscreenPause = (SaveData.AppRulesEnum)comboBoxFullscreenPerf.SelectedIndex;
            try
            {
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFullscreenPause = SaveData.AppRulesEnum.pause;
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            SaveData.SaveConfig();
        }

        private void ComboBoxFocusedPerf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.AppFocusPause = (SaveData.AppRulesEnum)comboBoxFocusedPerf.SelectedIndex;
            try
            {
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFocusPause = SaveData.AppRulesEnum.ignore;
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }
            SaveData.SaveConfig();
        }

        private void TransparencyToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (transparencyToggle.IsChecked == true)
            {
                if (SaveData.config.AppTransparencyPercent >= 0.5 && SaveData.config.AppTransparencyPercent <= 0.9)
                    this.Opacity = SaveData.config.AppTransparencyPercent;
                else
                    this.Opacity = 0.9f;
                SaveData.config.AppTransparency = true;
                transparencySlider.IsEnabled = true;
            }
            else
            {
                this.Opacity = 1.0f;
                SaveData.config.AppTransparency = false;
                transparencySlider.IsEnabled = false;
            }
            SaveData.SaveConfig();
        }


        private void TileAnimateToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (TileAnimateToggle.IsChecked == true)
            {
                SaveData.config.LiveTile = true;
                foreach (var item in tileDataList)
                {
                    if (File.Exists(item.LivelyInfo.Preview)) //only if preview gif exist, clear existing image.
                    {
                        item.Img = null;
                    }
                }
                InitializeTilePreviewGifs(); //loads first 15gifs. into TilePreview
            }
            else
            {
                SaveData.config.LiveTile = false;
                foreach (var item in tileDataList)
                {
                    item.Img = item.LoadConvertImage(item.LivelyInfo.Thumbnail);
                    //item.Img = item.LoadImage(item.LivelyInfo.Thumbnail);
                    item.TilePreview = null;
                }
            }

            textBoxLibrarySearch.Text = null;
            ScrollViewer scrollViewer = GetDescendantByType(wallpapersLV, typeof(ScrollViewer)) as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(0);
            }
            wallpapersLV.Items.Refresh(); //force redraw: not refreshing everything, even with INotifyPropertyChanged.
            SaveData.SaveConfig();
        }

        public void Button_Click_HowTo(object sender, RoutedEventArgs e)
        {
            Dialogues.HelpWindow w = new Dialogues.HelpWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            w.ShowDialog();
        }

        /// <summary>
        /// Display layout panel show.
        /// </summary>
        private void Image_Display_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DisplayLayoutWindow displayWindow = new DisplayLayoutWindow(this)
            {
                Owner = Window.GetWindow(this)
            };
            displayWindow.ShowDialog();
            displayWindow.Close();
            this.Activate();
            //Debug.WriteLine("retured val:- " + DisplayLayoutWindow.index);
        }

        private void Display_layout_Btn(object sender, EventArgs e)
        {
            DisplayLayoutWindow displayWindow = new DisplayLayoutWindow(this)
            {
                Owner = Window.GetWindow(this)
            };
            displayWindow.ShowDialog();
            displayWindow.Close();
            this.Activate();
            //Debug.WriteLine("retured val:- " + DisplayLayoutWindow.index);
        }

        /// <summary>
        /// Videoplayer change, restarts currently playing wp's to newly selected system.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxVideoPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.VidPlayer = (SaveData.VideoPlayer)comboBoxVideoPlayer.SelectedIndex;
            SaveData.SaveConfig();

            var videoWp = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.video); //youtube is started as apptype, not included!
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.video);

            if (videoWp.Count != 0)
            {
                RestoreWallpaper(videoWp);
            }
        }

        /// <summary>
        ///  Gif player change, restarts currently playing wp's to newly selected system.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxGIFPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.GifPlayer = (SaveData.GIFPlayer)comboBoxGIFPlayer.SelectedIndex;
            SaveData.SaveConfig();

            var result = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.gif);
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.gif);

            if (result.Count != 0)
            {
                RestoreWallpaper(result);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch { } //if no default mail client, win7 error.
        }

        private void Hyperlink_SupportPage(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://ko-fi.com/rocksdanister");
        }

        private void Button_Click_CreateWallpaper(object sender, RoutedEventArgs e)
        {
            CreateWallpaper obj = new CreateWallpaper
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            obj.ShowDialog();
        }

        /// <summary>
        /// Shows warning msg with link before proceeding to load hyperlink.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Hyperlink_RequestNavigate_Warning(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var ch = await this.ShowMessageAsync(Properties.Resources.msgLoadExternalLinkTitle, Properties.Resources.msgLoadExternalLink + "\n" + e.Uri.ToString(), MessageDialogStyle.AffirmativeAndNegative,
                         new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

            if (ch == MessageDialogResult.Negative)
                return;
            else if (ch == MessageDialogResult.Affirmative)
            {

            }

            Process.Start(e.Uri.AbsoluteUri);
        }

        /// <summary>
        /// Drag and Drop wallpaper.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] droppedFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop, true) as string[];

                if ((null == droppedFiles) || (!droppedFiles.Any())) { return; }

                Logger.Info("Dropped File, Selecting first file:- " + droppedFiles[0]);

                if (String.IsNullOrWhiteSpace(Path.GetExtension(droppedFiles[0])))
                    return;

                if (Path.GetExtension(droppedFiles[0]).Equals(".gif", StringComparison.OrdinalIgnoreCase))
                    SetupWallpaper(droppedFiles[0], SetupDesktop.WallpaperType.gif);
                else if (Path.GetExtension(droppedFiles[0]).Equals(".html", StringComparison.OrdinalIgnoreCase))
                    SetupWallpaper(droppedFiles[0], SetupDesktop.WallpaperType.web);
                else if (Path.GetExtension(droppedFiles[0]).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    tabControl1.SelectedIndex = 0; //switch to library tab.
                    WallpaperInstaller(droppedFiles[0]);
                }
                else if (utility.VideoOperations.IsVideoFile(droppedFiles[0]))
                    SetupWallpaper(droppedFiles[0], SetupDesktop.WallpaperType.video);
                else
                {
                    //exe format is skipped for drag and drop, mainly due to security reasons.
                    if (this.IsVisible)
                        this.Activate(); //bugfix.

                    System.Windows.Controls.Button btn = new System.Windows.Controls.Button
                    {
                        Margin = new Thickness(12, 8, 12, 8),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        Content = "Goto Type"
                    };
                    btn.Click += Btn_Gototab;

                    notify.Manager.CreateMessage()
                     .Accent("#808080")
                     .HasBadge("Info")
                     .Background("#333")
                     .HasHeader(Properties.Resources.msgDragDropOtherFormatsTitle)
                     .HasMessage(Properties.Resources.msgDragDropOtherFormats + "\n" + droppedFiles[0])
                     .Dismiss().WithButton("Ok", button => { })
                     .WithAdditionalContent(ContentLocation.Bottom,
                       new Border
                       {
                           BorderThickness = new Thickness(0, 1, 0, 0),
                           BorderBrush = new SolidColorBrush(Color.FromArgb(128, 28, 28, 28)),
                           Child = btn
                       })
                     .Queue();

                }

            }
            else if (e.Data.GetDataPresent(System.Windows.DataFormats.Text))
            {
                string droppedText = (string)e.Data.GetData(System.Windows.DataFormats.Text, true);
                Logger.Info("Dropped Text:- " + droppedText);
                if ((String.IsNullOrWhiteSpace(droppedText)))
                {
                    return;
                }
                WebLoadDragDrop(droppedText);
            }
        }

        private void Btn_Gototab(object sender, RoutedEventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }

        private void WebLoadDragDrop(string link)
        {
            if (link.Contains("youtube.com/watch?v=") || link.Contains("bilibili.com/video/")) //drag drop only for youtube.com streams
            {
                SetupWallpaper(link, SetupDesktop.WallpaperType.video_stream);
            }
            else
            {
                SetupWallpaper(link, SetupDesktop.WallpaperType.url);
            }
        }

        private void Button_AppRule_Click(object sender, RoutedEventArgs e)
        {
            Dialogues.ApplicationRuleDialogWindow w = new Dialogues.ApplicationRuleDialogWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };
            w.ShowDialog();
        }

        /*
        private void DisableUIHWToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (disableUIHWToggle.IsChecked == true)
                SaveData.config.UiDisableHW = true;
            else
                SaveData.config.UiDisableHW = false;

            SaveData.SaveConfig();
        }

        private void FpsUIToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (fpsUIToggle.IsChecked == true)
                SaveData.config.Ui120FPS = true;
            else
                SaveData.config.Ui120FPS = false;

            SaveData.SaveConfig();
        }
        */
        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxLanguage.SelectedIndex == -1)
                return;
            //todo:- do it more elegantly.
            SaveData.config.Language = SaveData.supportedLanguages[comboBoxLanguage.SelectedIndex].Codes[0];
            //SaveData.SaveConfig();
            RestartLively();
        }

        /// <summary>
        /// save config and restart lively.
        /// </summary>
        public static void RestartLively()
        {
            //Need more testing, mutex(for single instance of lively) release might not be quick enough.
            _isExit = true;
            SaveData.config.IsRestart = true;
            SaveData.SaveConfig();
            System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);
            System.Windows.Application.Current.Shutdown();
        }

        Dialogues.Changelog changelogWindow = null;
        private void lblVersionNumber_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (changelogWindow == null)
            {
                changelogWindow = new Dialogues.Changelog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowActivated = true
                };
                changelogWindow.Closed += ChangelogWindow_Closed;
                changelogWindow.Show();
            }
            else
            {
                if (changelogWindow.IsVisible)
                {
                    changelogWindow.Activate();
                }
            }
        }

        private void ChangelogWindow_Closed(object sender, EventArgs e)
        {
            changelogWindow = null;
        }

        private void hyperlinkUpdateBanner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowLivelyUpdateWindow();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StartupToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                utility.SystemStartup.SetStartupRegistry(StartupToggle.IsChecked.Value);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                StartupToggle.IsChecked = false;
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, "Failed to setup startup: " + ex.Message);
            }

            SaveData.config.Startup = StartupToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        #endregion ui_events
    }
}
