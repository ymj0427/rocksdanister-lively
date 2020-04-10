using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;
using System.Reflection;
using Ionic.Zip;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using System.Security.Cryptography;
using Octokit;
using FileMode = System.IO.FileMode;
using Microsoft.WindowsAPICodePack.Shell;
using System.Threading;
using File = System.IO.File;
using NLog;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.ComponentModel;
using static livelywpf.SaveData;
using System.Windows.Interop;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Globalization;
using livelywpf.Lively.Helpers;
using Enterwell.Clients.Wpf.Notifications;
using MahApps.Metro;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool Multiscreen { get; private set;}
        public static bool HighContrastFix { get; private set; }
        private ProgressDialogController progressController = null;
        private ObservableCollection<TileData> tileDataList = new ObservableCollection<TileData>();
        private ObservableCollection<TileData> selectedTile = new ObservableCollection<TileData>();

        private ICollectionView tileDataFiltered;
        private bool _isRestoringWallpapers = false;

        //private Release gitRelease = null;
        //private string gitUrl = null;
        GitUpdater.GitData gitInfo = null;
        RawInputDX DesktopInputForward = null;
        readonly utility.SystemTray SysTray = new utility.SystemTray();

        public MainWindow()
        {
            #region lively_SubProcess
            //External process that runs, kills external pgm wp's( unity, app etc) and refresh desktop in the event lively crashed, could do this in UnhandledException event but this is guaranteed to work even if user kills livelywpf in taskmgr.
            //todo:- look for a better alternative?
            try
            {
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "livelySubProcess.exe"), Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
            }
            catch(Exception e)
            {
                Logger.Error(e,"Starting livelySubProcess failure: " + e.Message);
            }
            #endregion lively_SubProcess

            //settings applied only during app start.
            #region misc_fixes
            SetupDesktop.wallpaperWaitTime = SaveData.config.WallpaperWaitTime;

            //older ver of windows, with highcontrast mode cannot render behind icon!
            //using bottom most window(infront icons) instead of behind.
            if (SaveData.config.WallpaperRendering == WallpaperRenderingMode.bottom_most)
                HighContrastFix = true;
            else
                HighContrastFix = false;

            //force 120fps, in some systems gpu downclocking too much due to low power usage..this is a workaround for smoother ui.
            if (SaveData.config.Ui120FPS)
                Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline),
                             new FrameworkPropertyMetadata { DefaultValue = 120 });

            //disable UI HW-Acceleration, for very low end systems optional.
            if (SaveData.config.UiDisableHW)
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            //always show tooltip, even disabled ui elements.
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(System.Windows.Controls.Control),
                                                            new FrameworkPropertyMetadata(true));
            #endregion misc_fixes

            InitializeComponent();
            notify.Manager = new NotificationMessageManager();
            this.Closing += MainWindow_Closing;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged; //static event, unsubcribe!
            //todo:- Window.DpiChangedEvent, so far not required.
            //todo:- Suspend/hibernate events (SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged)

            //this.DataContext = SaveData.config;
            RestoreSaveSettings();

            //data binding
            wallpapersLV.ItemsSource = tileDataList;
            tileDataFiltered = CollectionViewSource.GetDefaultView(tileDataList);
            tileInfo.ItemsSource = selectedTile;
            //this.DataContext = selectedTile; //todo: figure out why is this not working?

            SubcribeUI();

            lblVersionNumber.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //attribution document.
            TextRange textRange = new TextRange(licenseDocument.ContentStart, licenseDocument.ContentEnd);
            try
            {
                using (FileStream fileStream = File.Open(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "license.rtf")), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
                }
                licenseFlowDocumentViewer.Document = licenseDocument;
            }
            catch(Exception e)
            {
                Logger.Error("Failed to load license file:" + e.Message);
            }

            //whats new (changelog) screen!
            if (!SaveData.config.AppVersion.Equals(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), StringComparison.OrdinalIgnoreCase)
                && SaveData.config.IsFirstRun != true)
            {
                //if previous savedata version is different from currently running app, show help/update info screen.
                SaveData.config.AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                SaveData.SaveConfig();

                Dialogues.Changelog cl = new Dialogues.Changelog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowActivated = true
                };
                cl.Show();
            }

            //restore previously running wp's.
            Multiscreen = false;
            SystemEvents_DisplaySettingsChanged(this, null); 

            //Incomplete, currently in development:- all process algorithm with multiscreen is buggy.
            if (Multiscreen && SaveData.config.ProcessMonitorAlgorithm == ProcessMonitorAlgorithm.all)
            {
                Logger.Info("Skipping all-process algorthm on multiscreen(in-development)");
                comboBoxPauseAlgorithm.SelectedIndex = (int)ProcessMonitorAlgorithm.foreground; //event will save settings.
            }

        }

        private async void RestoreSaveSettings()
        {
            //load savefiles. 
            SaveData.LoadApplicationRules();
            //SaveData.LoadConfig(); //App() loads config file.
            SaveData.LoadWallpaperLayout();
            RestoreMenuSettings();

            try
            {
                //update checking.
                gitInfo = await GitUpdater.CheckForUpdate("lively", "rocksdanister", "lively_setup_x86_full", 45000);
                if (gitInfo.Result > 0) //github ver greater, update available!
                {
                    utility.SystemTray.SetUpdateTrayBtnText(Properties.Resources.txtContextMenuUpdate2);
                    if (UpdateNotifyOrNot())
                    {
                        if (App.W != null)
                        {
                            //system tray notification, only displayed if lively is minimized to tray.
                            if (!App.W.IsVisible)
                            {
                                utility.SystemTray.TrayIcon.ShowBalloonTip(2000, "lively", Properties.Resources.toolTipUpdateMsg, ToolTipIcon.None);
                            }

                            notify.Manager.CreateMessage()
                               .Animates(true)
                               .AnimationInDuration(0.75)
                               .AnimationOutDuration(0.75)
                               .Accent("#808080")
                               .Background("#333")
                               .HasBadge("Info")
                               .HasMessage(Properties.Resources.txtUpdateBanner + " " + gitInfo.Release.TagName)
                               .WithButton(Properties.Resources.txtDownload, button => { ShowLivelyUpdateWindow(); })
                               .Dismiss().WithButton(Properties.Resources.txtLabel37, button => { })
                               .Queue();
                        }
                    }
                }
                else if (gitInfo.Result < 0) //this is early access software.
                {
                    utility.SystemTray.SetUpdateTrayBtnText(Properties.Resources.txtContextMenuUpdate3);
                }
                else //up-to-date
                {
                    utility.SystemTray.SetUpdateTrayBtnText(Properties.Resources.txtContextMenuUpdate4);
                }
            }
            catch(Exception e)
            {
                Logger.Error("Update check fail:" + e.Message);
            }
            utility.SystemTray.UpdateTrayBtnToggle(true);
        }

        #region wp_input_setup
        private void WallpaperInputForwardingToggle(bool isEnable)
        {
            if (isEnable)
            {
                if (DesktopInputForward == null)
                {
                    DesktopInputForward = new RawInputDX();
                    DesktopInputForward.Closing += DesktopInputForward_Closing;
                    DesktopInputForward.Show();
                }
            }
            else
            {
                if (DesktopInputForward != null)
                {
                    DesktopInputForward.Close();
                }
            }
        }

        private void DesktopInputForward_Closing(object sender, CancelEventArgs e)
        {
            DesktopInputForward = null;
        }
        #endregion

        #region git_update

        Dialogues.AppUpdate appUpdateWindow = null;
        public void ShowLivelyUpdateWindow()
        {
            if (appUpdateWindow == null)
            {            
                appUpdateWindow = new Dialogues.AppUpdate(gitInfo.Release, gitInfo.GitUrl)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                if (App.W.IsVisible)
                {
                    appUpdateWindow.Owner = App.W;
                    WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                
                appUpdateWindow.Show();
                appUpdateWindow.Closed += AppUpdateWindow_Closed;
            }
        }

        private void AppUpdateWindow_Closed(object sender, EventArgs e)
        {
            appUpdateWindow = null;
        }

        private bool UpdateNotifyOrNot()
        {
            if(gitInfo.Release == null || gitInfo.GitUrl == null)
            {
                return false;
            }
            else if(SaveData.config.IsFirstRun || String.IsNullOrWhiteSpace(gitInfo.Release.TagName) ||
                gitInfo.Release.TagName.Equals(SaveData.config.IgnoreUpdateTag, StringComparison.Ordinal))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion git_update

        #region system_events
 
        bool _startupRun = true;
        /// <summary>
        /// Display device settings changed event. 
        /// Closes and restarts wp's in the event system display layout changes.(based on wallpaperlayout SaveData file)
        /// Updates wp dimensions in the event ONLY resolution changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            //bool _startupRun = false;
            if (Screen.AllScreens.Length > 1)
            {
                foreach (var item in Screen.AllScreens)
                {
                    Debug.WriteLine("Detected Displays:- " + item);
                    Logger.Debug("Detected Displays:- " + item);
                }
                Multiscreen = true;
            }
            else
            {
                Multiscreen = false;
                Logger.Debug("Single Display Mode:- " + Screen.PrimaryScreen);
            }

            List<SaveData.WallpaperLayout> toBeRemoved = new List<SaveData.WallpaperLayout>();
            List<SaveData.WallpaperLayout> wallpapersToBeLoaded = new List<SaveData.WallpaperLayout>(SetupDesktop.wallpapers);

            if (_startupRun) //first run.
            {
                _startupRun = true;
            }
            else
            {
                Logger.Info("Display Settings Changed Event..");
            }

            bool found;
            foreach (var item in wallpapersToBeLoaded)
            {
                found = false;
                foreach (var scr in Screen.AllScreens)
                {
                    if (item.DeviceName == scr.DeviceName) //ordinal comparison
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    toBeRemoved.Add(item);
                }
            }
            if (toBeRemoved.Count > 0)
                wallpapersToBeLoaded = wallpapersToBeLoaded.Except(toBeRemoved).ToList(); //new list

            foreach (var item in wallpapersToBeLoaded)
            {
                Logger.Info("Display(s) wallpapers to load:-" + item.DeviceName);
            }

            if (!wallpapersToBeLoaded.SequenceEqual(SetupDesktop.wallpapers) || _startupRun)
            {
                SetupDesktop.CloseAllWallpapers(); //todo: only close wallpapers that which is running on disconnected display.
                Logger.Info("Restarting/Restoring All Wallpaper(s)");

                //remove wp's with file missing on disk, except for url type( filePath =  website url).
                if( wallpapersToBeLoaded.RemoveAll(x => !File.Exists(x.FilePath) && x.Type != SetupDesktop.WallpaperType.url && x.Type != SetupDesktop.WallpaperType.video_stream) > 0)
                {
                    utility.SystemTray.TrayIcon.ShowBalloonTip(10000,"lively",Properties.Resources.toolTipWallpaperSkip, ToolTipIcon.None);
                    notify.Manager.CreateMessage()
                   .Accent("#FF0000")
                   .HasBadge("Warn")
                   .Background("#333")
                   .HasHeader(Properties.Resources.txtLivelyErrorMsgTitle)
                   .HasMessage(Properties.Resources.toolTipWallpaperSkip)
                   .Dismiss().WithButton("Ok", button => { })
                   .Queue();
                }

                if(SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                {
                    //unlikely to happen unless user edits the json file manually or some file error? 
                    if(wallpapersToBeLoaded.Count > 1)
                    {
                        //span across all display(s), only 1 wp allowed!
                        wallpapersToBeLoaded.RemoveRange(1, (wallpapersToBeLoaded.Count-1) );
                    }
                }
                RestoreWallpaper(wallpapersToBeLoaded);
            }
            else
            {
                Logger.Info("Display(s) settings such as resolution etc changed, updating wp(s) dimensions");
                SetupDesktop.UpdateAllWallpaperRect();
            }
            //wallpapersToBeLoaded.Clear();
            _startupRun = false;
        }

        #endregion system_events

        #region wallpaper_library  

        /// <summary>
        /// Loads and populate lively wp library from "//wallpapers" path if any. 
        /// </summary>
        public void UpdateWallpaperLibrary()
        {
            tileDataList.Clear();
            selectedTile.Clear();
            //wallpapersLV.SelectedIndex = -1;
            List<SaveData.TileData> tmpLoadedWallpapers = new List<SaveData.TileData>();
            var wpDir = Directory.GetDirectories( Path.Combine( App.PathData, "wallpapers"));
            var tmpDir = Directory.GetDirectories( Path.Combine(App.PathData, "SaveData", "wptmp"));
            var dir = wpDir.Concat(tmpDir).ToArray();

            for (int i = 0; i < dir.Length; i++)
            {
                var item = dir[i];
                if (File.Exists(Path.Combine(item, "LivelyInfo.json")))
                {
                    if (SaveData.LoadWallpaperMetaData(item))
                    {       
                        if (i < wpDir.Length) //wallpaper folder; relative path.
                        {
                            if (info.Type == SetupDesktop.WallpaperType.video_stream || info.Type == SetupDesktop.WallpaperType.url)
                            {
                                //online content.
                            }
                            else
                            {
                                SaveData.info.FileName = Path.Combine(item, SaveData.info.FileName);
                            }

                            try
                            {
                                SaveData.info.Preview = Path.Combine(item, SaveData.info.Preview);
                            }
                            catch(ArgumentNullException)
                            {
                                SaveData.info.Preview = null;
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Preview = null;
                            }

                            try
                            {
                                SaveData.info.Thumbnail = Path.Combine(item, SaveData.info.Thumbnail);
                            }
                            catch(ArgumentNullException)
                            {
                                SaveData.info.Thumbnail = null;
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Thumbnail = null;
                            }
                        }
                        else //absolute path wp's ( //SaveData//wptmp//)
                        {                     
                            if (File.Exists(SaveData.info.Preview) != true)
                            {
                                //backward compatible with portable ver of lively, if file is moved and absolute path is wrong.
                                if (File.Exists(Path.Combine(item, Path.GetFileName(SaveData.info.Preview))))
                                {
                                    SaveData.info.Preview = Path.Combine(item, Path.GetFileName(SaveData.info.Preview));
                                }
                                else
                                    SaveData.info.Preview = null;
                            }

                            if (File.Exists(SaveData.info.Thumbnail) != true)
                            {
                                if (File.Exists(Path.Combine(item, Path.GetFileName(SaveData.info.Thumbnail))))
                                {
                                    SaveData.info.Thumbnail = Path.Combine(item, Path.GetFileName(SaveData.info.Thumbnail));
                                }
                                else
                                    SaveData.info.Thumbnail = null;
                            }
                            
                        }

                        //load anyway for absolutepath, setupwallpaper will check if file exists for this type and give warning.
                        //this also prevents disk powerup in the event the files are in different hdd thats sleeping and lively is launched from tray.
                        if (info.IsAbsolutePath) 
                        {
                            Logger.Info("Loading Wallpaper (absolute path):- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            tmpLoadedWallpapers.Add(new TileData(info, item));
                        }
                        else if(info.Type == SetupDesktop.WallpaperType.video_stream
                                || info.Type == SetupDesktop.WallpaperType.url) //no files for this type.)
                        {
                            Logger.Info("Loading Wallpaper (url/stream):- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            tmpLoadedWallpapers.Add(new TileData(info, item));
                        }
                        else if (File.Exists(SaveData.info.FileName))
                        {
                            Logger.Info("Loading Wallpaper (wp dir):- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            tmpLoadedWallpapers.Add(new TileData(info, item));
                        }
                        else
                        {
                            Logger.Info("Skipping wallpaper:- " + SaveData.info.FileName + " " + SaveData.info.Type);
                        }
                    }
                }
                else
                {
                    Logger.Info("Not a lively wallpaper folder, skipping:- " + item);
                }
            }

            //tmpItems.Sort((x, y) => string.Compare(x.LivelyInfo.Title, y.LivelyInfo.Title));
            //sorting based on alphabetical order of wp title text. 
            var sortedList = tmpLoadedWallpapers.OrderBy(x => x.LivelyInfo.Title).ToList();
            foreach (var item in sortedList)
            {               
                tileDataList.Add(new TileData(item.LivelyInfo, item.LivelyInfoDirectoryLocation));
            }

            sortedList.Clear();
            tmpLoadedWallpapers.Clear();
            sortedList = null;
            tmpLoadedWallpapers = null;

            InitializeTilePreviewGifs();

            if (prevSelectedLibIndex < tileDataList.Count)
                wallpapersLV.SelectedIndex = prevSelectedLibIndex;
            else
                wallpapersLV.SelectedIndex = -1;
        }

        /// <summary>
        /// Copy and load wallpaper file from tmpdata/wpdata folder into Library.
        /// </summary>
        public void LoadWallpaperFromWpDataFolder()
        {
            //library tab.
            if(tabControl1.SelectedIndex != 0)
                tabControl1.SelectedIndex = 0;

            var randomFolderName = Path.GetRandomFileName();
            var dir = Path.Combine(App.PathData, "SaveData", "wptmp", randomFolderName);
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch(Exception e)
            {
                Logger.Error(e.ToString());
                return;
            }

            if (SaveData.LoadWallpaperMetaData(Path.Combine(App.PathData, "tmpdata","wpdata\\") ))
            {
                //making the thumbnail and preview absolute paths.
                if(File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", SaveData.info.Preview) ))
                    SaveData.info.Preview = Path.Combine(dir,SaveData.info.Preview);
                if (File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", SaveData.info.Thumbnail) ))
                    SaveData.info.Thumbnail = Path.Combine(dir, SaveData.info.Thumbnail);

                SaveData.SaveWallpaperMetaData(SaveData.info, dir);
            }
            else
            {
                Logger.Error("LoadWallpaperFromWpDataFolder(): Failed to load livelyinfo for tmpwallpaper!..deleting tmpfiles.");
                Task.Run(() => (FileOperations.EmptyDirectory( Path.Combine(App.PathData, "tmpdata", "wpdata\\") )));
                try
                {
                    Directory.Delete(dir);
                }
                catch (Exception ie1)
                {
                    Logger.Error(ie1.ToString());
                }
                return;
            }

            try
            {
                if (File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Thumbnail)) ))
                    File.Copy( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Thumbnail)), SaveData.info.Thumbnail);

                if (File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Preview)) ))
                    File.Copy( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Preview)), SaveData.info.Preview);
                    
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            finally
            {
                UpdateWallpaperLibrary();
                Task.Run(() => (FileOperations.EmptyDirectory(Path.Combine(App.PathData, "tmpdata", "wpdata\\"))));
                //selecting newly added wp.
                foreach (var item in tileDataList)
                {
                    if (item.LivelyInfoDirectoryLocation.Contains(randomFolderName))
                    {
                        wallpapersLV.SelectedItem = item;
                        if (SaveData.config.LivelyZipGenerate)
                        {
                            MenuItem_CreateZip_Click(this, null);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// User selected tile in library.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WallpapersLV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedTile.Clear();
            if (wallpapersLV.SelectedIndex == -1)
            {
                return;
            }
            var selection = (TileData)wallpapersLV.SelectedItem;

            selection.CustomiseBtnToggle = false;
            if ((SetupDesktop.wallpapers.FindIndex(x => x.FilePath.Equals(selection.LivelyInfo.FileName, StringComparison.Ordinal))) != -1)
            {
                if (selection.IsCustomisable)
                {
                    selection.CustomiseBtnToggle = true;
                }
            }
            selectedTile.Add(selection);
            wallpapersLV.ScrollIntoView(selection);
        }

        #region scroll_gif_logic

        /// <summary>
        /// Initialize only few gif preview to reduce cpu usage, gif's are loaded and disposed(atleast marked) based on ScrollChanged event.
        /// </summary>
        private void InitializeTilePreviewGifs()
        {
            if (!SaveData.config.LiveTile)
                return;

            for (int i = 0; i < tileDataList.Count; i++)
            {
                if (i >= 20)
                    return;
                if(File.Exists(tileDataList[i].LivelyInfo.Preview))
                    tileDataList[i].TilePreview = tileDataList[i].LivelyInfo.Preview;
            }
        }

        private int prevIndexOffset = 0;
        /// <summary>
        /// only loading upto a certain no: of gif at a time in library to reduce load.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WallpapersLV_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!SaveData.config.LiveTile)
                return;

            ScrollViewer scrollViewer = GetDescendantByType(wallpapersLV, typeof(ScrollViewer)) as ScrollViewer;
            if (scrollViewer != null)
            {
                int indexOffset = 0;
                double percent = scrollViewer.ViewportHeight * .33f;
                double shiftY = scrollViewer.VerticalOffset / percent;

                int startIndex = 0;
                if (shiftY > 0 || sender == null )
                {
                    indexOffset = startIndex +  Convert.ToInt32(shiftY) * 5;
                }

                if (indexOffset != prevIndexOffset)
                {
                    int count = 0;
                    for (int i = 0; i < tileDataList.Count; i++)
                    {
                        if (i >= indexOffset && count <= 20)
                        {
                            if (File.Exists(tileDataList[i].LivelyInfo.Preview))
                            {
                                tileDataList[i].TilePreview = tileDataList[i].LivelyInfo.Preview;
                            }
                            count++;
                        }
                        else
                        {
                            tileDataList[i].TilePreview = null;
                        }
                    }
                }
                prevIndexOffset = indexOffset;
            }           
        }

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }
            if (element.GetType() == type)
            {
                return element;
            }
            Visual foundElement = null;
            if (element is FrameworkElement)
            {
                (element as FrameworkElement).ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }

        #endregion scroll_gif_logic

        class ZipCreateInfo
        {
            public MetroProgressBar ProgressBar { get; private set; }
            public INotificationMessage Notification { get; private set; }
            public ZipFile ZipFile { get; private set; }
            public bool AbortZipExtraction { get; set; }
            public ZipCreateInfo(MetroProgressBar progressBar, INotificationMessage notification, ZipFile zipFile)
            {
                this.Notification = notification;
                this.ProgressBar = progressBar;
                this.ZipFile = zipFile;
                AbortZipExtraction = false;
            }
        }
        
        List<ZipCreateInfo> zipCreator = new List<ZipCreateInfo>();
        /// <summary>
        /// Creates Lively zip file for already extracted wp's in Library.
        /// </summary>
        private async void MenuItem_CreateZip_Click(object sender, RoutedEventArgs e)
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;

            string savePath = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog()
            {
                Title = "Select location to save the file",
                Filter = "Lively/zip file|*.zip"
            };

            if (saveFileDialog1.ShowDialog() == true)
            {
                savePath = saveFileDialog1.FileName;
            }

            if (String.IsNullOrEmpty(savePath))
            {
                return;
            }

            ZipCreateInfo zipInstance = null;
            List<string> folderContents = new List<string>();
            var selection = (TileData)wallpapersLV.SelectedItem;

            string parentDirectory = null;
            if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream
                || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url)
            {
                parentDirectory = selection.LivelyInfoDirectoryLocation;
            }
            else
            {
                parentDirectory = Path.GetDirectoryName(selection.LivelyInfo.FileName);
            }

            //absolute path values in livelyinfo.json, the wallpaper files are outside of lively folder, requires some work..
            if (selection.LivelyInfo.IsAbsolutePath)
            {
                //only single file on disk.
                if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video
                    || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.gif)
                {
                    folderContents.Add(selection.LivelyInfo.FileName);
                    //preview gif/thumb, livelyinfo, liveyproperty.json & maybe wp files..
                    folderContents.AddRange(Directory.GetFiles(selection.LivelyInfoDirectoryLocation, "*.*", SearchOption.AllDirectories));
                }
                //no file, online wp.
                else if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream
                      || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url) 
                {
                    //folderContents.Clear();
                }
                // exe, html etc with more files.
                else
                {
                    folderContents.AddRange(Directory.GetFiles(Path.GetDirectoryName(selection.LivelyInfo.FileName), "*.*", SearchOption.AllDirectories));
                    folderContents.AddRange(Directory.GetFiles(selection.LivelyInfoDirectoryLocation, "*.*", SearchOption.AllDirectories));
                }

                if (folderContents.Count != 0)
                {
                    CreateWallpaperAddedFiles w = new CreateWallpaperAddedFiles(folderContents)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    w.ShowDialog();

                    if (w.DialogResult.HasValue && w.DialogResult.Value) //ok btn
                    {
                    }
                    else //back btn
                    {
                        folderContents.Clear();
                        return;
                    }
                }
            }
            else // already installed lively wp, just zip the folder.
            {
                folderContents.AddRange(Directory.GetFiles(parentDirectory, "*.*", SearchOption.AllDirectories));
            }

            try
            {
                using (ZipFile zip = new ZipFile(savePath))
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.ZipErrorAction = ZipErrorAction.Throw;

                    //lively metadata files..
                    if (selection.LivelyInfo.IsAbsolutePath)
                    {
                        //converting absolute path to relative and saving livelyinfo file.
                        if (SaveData.LoadWallpaperMetaData(Path.GetDirectoryName(selection.LivelyInfo.Thumbnail)))
                        {
                            SaveData.info.IsAbsolutePath = false;
                            try
                            {
                                SaveData.info.Thumbnail = Path.GetFileName(selection.LivelyInfo.Thumbnail);
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Thumbnail = null;
                            }
                            try
                            {
                                SaveData.info.Preview = Path.GetFileName(selection.LivelyInfo.Preview);
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Preview = null;
                            }

                            if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream
                                    || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url)
                            {
                                //SaveData.info.FileName = SaveData.info.FileName;
                            }
                            else
                            {
                                try
                                {
                                    SaveData.info.FileName = Path.GetFileName(selection.LivelyInfo.FileName);
                                }
                                catch(ArgumentException)
                                {
                                    SaveData.info.FileName = null;
                                }
                            }
                            SaveData.SaveWallpaperMetaData(SaveData.info, App.PathData + "\\tmpdata\\wpdata\\");
                        }
                        else
                        {
                            return;
                        }
                        zip.AddFile(App.PathData + "\\tmpdata\\wpdata\\LivelyInfo.json", "");
                        folderContents.Remove(folderContents.Single(x => Contains(x, "LivelyInfo.json", StringComparison.OrdinalIgnoreCase)));
                    }
                    else
                    {
                        var infoFile = folderContents.Single(x => Contains(x, "LivelyInfo.json", StringComparison.OrdinalIgnoreCase));
                        zip.AddFile(infoFile, "");
                        folderContents.Remove(infoFile);
                    }

                    if (!String.IsNullOrWhiteSpace(selection.LivelyInfo.Thumbnail))
                    {
                        zip.AddFile(selection.LivelyInfo.Thumbnail, "");
                        folderContents.Remove(selection.LivelyInfo.Thumbnail);
                    }

                    if (!String.IsNullOrWhiteSpace(selection.LivelyInfo.Preview))
                    {
                        zip.AddFile(selection.LivelyInfo.Preview, "");
                        folderContents.Remove(selection.LivelyInfo.Preview);
                    }

                    for (int i = 0; i < folderContents.Count; i++)
                    {
                        try
                        {
                            //adding files in root directory of zip, maintaining folder structure.
                            zip.AddFile(folderContents[i], Path.GetDirectoryName(folderContents[i]).Replace(parentDirectory, string.Empty));
                            
                        }
                        catch(Exception ie)
                        {
                            //MessageBox.Show(ie.Message + ": " + folderContents[i]);
                            Logger.Info(ie.Message + ": " + folderContents[i]);
                            WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, ie.Message);
                            break;
                        }
                    }

                    MetroProgressBar progressBar = null;
                    var notification = notify.Manager.CreateMessage()
                        //.Accent("#808080")
                        .Background("#333")
                        .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                        .HasMessage(Properties.Resources.txtCreatingZip + " " + selection.LivelyInfo.Title)
                        .Dismiss().WithButton("Stop", button => { ZipCreationCancel(zip); }) 
                        .WithOverlay(progressBar = new MetroProgressBar
                        {
                            Minimum = 0,
                            Maximum = 100,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                //Height = 0.5f,
                                //BorderThickness = new Thickness(0),
                                //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                                Background = Brushes.Transparent,
                            IsIndeterminate = false,
                            IsHitTestVisible = false,

                        })
                        .Queue();
                    zipCreator.Add(zipInstance = new ZipCreateInfo(progressBar, notification, zip));

                    zip.SaveProgress += Zip_SaveProgress;
                    await Task.Run(() => zip.Save());
                }
            }
            catch (Ionic.Zip.ZipException e1)
            {
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, e1.Message);
                Logger.Error(e1.ToString());
            }
            catch (Exception e2)
            {
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, e2.Message);
                Logger.Error(e2.ToString());
            }
            finally
            {
                if (zipInstance != null)
                {
                    if (zipInstance.Notification != null)
                        notify.Manager.Dismiss(zipInstance.Notification);

                    if (zipInstance.AbortZipExtraction)
                    {
                        //ionic zip deletes the file when aborted, nothing to do here.
                    }
                    else
                    {
                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = "\"" + Path.GetDirectoryName(savePath) + "\"",
                                FileName = "explorer.exe"
                            };
                            Process.Start(startInfo);
                        }
                        catch { }
                    }
                    zipCreator.Remove(zipInstance);
                }
            }

        }

        private void Zip_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            var zip = (ZipFile)sender;
            var obj = zipCreator.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            if(obj.AbortZipExtraction)
            {
                e.Cancel = true;
                return;
            }
            //if (zipWasCanceled) { e.Cancel = true; }

            if (e.EntriesTotal != 0)
            {

                if(obj.ProgressBar != null)
                {
                    this.Dispatcher.Invoke(() => {
                        obj.ProgressBar.Value = ((float)e.EntriesSaved / (float)e.EntriesTotal) * 100f;
                    });
                }

                if (e.EntriesSaved == e.EntriesTotal && e.EntriesTotal != 0) //completion
                {

                }
            }
        }

        private void ZipCreationCancel(ZipFile zip)
        {
            var obj = zipCreator.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            obj.AbortZipExtraction = true;
        }

        /// <summary>
        /// String Contains method with StringComparison property.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="substring"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static bool Contains(String str, String substring,
                                    StringComparison comp)
        {
            if (substring == null)
                throw new ArgumentNullException("substring",
                                             "substring cannot be null.");
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException("comp is not a member of StringComparison",
                                         "comp");

            return str.IndexOf(substring, comp) >= 0;
        }

        #endregion wallpaper_library

        #region systray
        public static bool _isExit;

        private int prevSelectedLibIndex = -1;
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                if (SaveData.config.IsFirstRun)
                {
                   utility.SystemTray.TrayIcon.ShowBalloonTip(3000, "Lively",Properties.Resources.toolTipMinimizeMsg,ToolTipIcon.None);

                    SaveData.config.IsFirstRun = false;
                    SaveData.SaveConfig();
                }

                prevSelectedLibIndex = wallpapersLV.SelectedIndex;
                tileDataList.Clear();
                selectedTile.Clear();

                this.Hide();
                //testing
                GC.Collect();
            }
            else
            {
                //static event, otherwise memory leak.
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

                SaveData.config.SafeShutdown = true;
                SaveData.SaveConfig();

                SetupDesktop.CloseAllWallpapers(true);
         
                SetupDesktop.RefreshDesktop();

                SysTray.Dispose();
            }
        }

        public void ExitApplication()
        {
            _isExit = true;
            System.Windows.Application.Current.Shutdown();
            //this.Close(); // MainWindow_Closing() handles the exit actions.
        }

        public async void ShowMainWindow()
        {
            //TODO:- add error if waitin to kill browser process, onclose runnin and user call this fn.
            if (App.W.IsVisible)//this.IsVisible)
            {
                if (App.W.WindowState == WindowState.Minimized)
                {
                    App.W.WindowState = WindowState.Normal;
                }
                App.W.Activate();
            }
            else
            {
                UpdateWallpaperLibrary();
                App.W.Show();
                //App.w.Activate();

            }
        }

        #endregion systray

        #region wp_setup
        /// <summary>
        /// Sets up wallpaper, shows dialog to select display if multiple displays are detected.
        /// </summary>
        /// <param name="path">wallpaper location.</param>
        /// <param name="type">wallpaper category.</param>
        private async void SetupWallpaper(string path, SetupDesktop.WallpaperType type, string args = null, bool showAddWallpaperWindow = false)
        {
            if(_isRestoringWallpapers)
            {
                //_ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgRestoringInProgress, Properties.Resources.txtLivelyWaitMsgTitle, MessageBoxButton.OK, MessageBoxImage.Information)));
                WpfNotification(NotificationType.info, Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.msgRestoringInProgress);
                return;
            }

            if ( !(File.Exists(path) || type == SetupDesktop.WallpaperType.video_stream || type == SetupDesktop.WallpaperType.url) )
            {
                //_ = Task.Run(() => (MessageBox.Show("File missing on disk!\n" + path, Properties.Resources.txtLivelyErrorMsgTitle, MessageBoxButton.OK , MessageBoxImage.Error)));
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.toolTipWallpaperSkip + "\n" + path);
                return;
            }

            SaveData.WallpaperLayout tmpData = new SaveData.WallpaperLayout();
            tmpData.Arguments = args;

            if (type == SetupDesktop.WallpaperType.app && args == null)
            {
                var arg = await this.ShowInputAsync(Properties.Resources.msgAppCommandLineArgsTitle, Properties.Resources.msgAppCommandLineArgs, new MetroDialogSettings()
                { DialogTitleFontSize = 16, DialogMessageFontSize = 14, AnimateShow = false, AnimateHide = false });
                if (arg == null) //cancel btn or ESC key
                    return;

                if (!string.IsNullOrWhiteSpace(arg))
                    tmpData.Arguments = arg;
            }
            else if (type == SetupDesktop.WallpaperType.web || type == SetupDesktop.WallpaperType.url || type == SetupDesktop.WallpaperType.web_audio)
            {
                if (HighContrastFix)
                {
                    Logger.Info("behind-icon mode, skipping cef.");
                    _ = Task.Run(() => (MessageBox.Show("Web wallpaper is not available in High Contrast mode workaround, coming soon.", Properties.Resources.txtLivelyErrorMsgTitle)));
                    return;
                }

                if (!File.Exists(App.PathData + "\\external\\cef\\LivelyCefSharp.exe"))
                {
                    Logger.Info("cefsharp is missing, skipping wallpaper.");
                    //_ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgWebBrowserMissing, Properties.Resources.txtLivelyErrorMsgTitle, 
                    //                                                                                            MessageBoxButton.OK, MessageBoxImage.Information)));
                    WpfNotification(NotificationType.info, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.msgWebBrowserMissing);
                    return;
                }
            }
            else if ((type == SetupDesktop.WallpaperType.video && SaveData.config.VidPlayer == VideoPlayer.mpv))
            {
                if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe"))
                {
                    //_ = Task.Run(() => (MessageBox.Show("mpv player missing!\nwww.github.com/rocksdanister/lively/wiki/Video-Guide", Properties.Resources.txtLivelyErrorMsgTitle, 
                    //                                                                                                MessageBoxButton.OK, MessageBoxImage.Information)));
                    WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player missing!", "https://www.github.com/rocksdanister/lively/wiki/Video-Guide");
                    return;
                }
            }
            else if (type == SetupDesktop.WallpaperType.video_stream)
            {
                if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe") || !File.Exists(App.PathData + "\\external\\mpv\\youtube-dl.exe"))
                {
                    WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player/youtube-dl missing!", "https://github.com/rocksdanister/lively/wiki/Youtube-Wallpaper");
                    return;
                }
            }

            if (type == SetupDesktop.WallpaperType.video_stream)
            {
                tmpData.Arguments = utility.YTDL.YoutubeDLArgGenerate(path, SaveData.config.StreamQuality);
            }

            //if previously running or cancelled, waiting to end.
            CancelWallpaperWaiting();
            while (SetupDesktop.IsProcessWaitDone() == 0)
            {
                await Task.Delay(1);
            }

            MetroProgressBar progressBar = null;
            isProcessWaitCancelled = false;
            INotificationMessage notification = null;
            if (Multiscreen && SaveData.config.WallpaperArrangement == WallpaperArrangement.duplicate)
            {
                List<WallpaperLayout> tmp = new List<WallpaperLayout>();
                foreach (var item in Screen.AllScreens)
                {
                    tmp.Add(new WallpaperLayout() { Arguments = tmpData.Arguments, DeviceName = item.DeviceName, FilePath = path, Type = type });
                }

                foreach (var item in tmp)
                {
                    Logger.Info("Duplicating wp(s):" + item.FilePath + " " + item.DeviceName);
                }
                RestoreWallpaper(tmp);
                return;
            }
            else if (Multiscreen && SaveData.config.WallpaperArrangement == WallpaperArrangement.per)
            {   
                //monitor select dialog
                DisplaySelectWindow displaySelectWindow = new DisplaySelectWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                displaySelectWindow.ShowDialog();

                if (DisplaySelectWindow.selectedDisplay == null) //none
                {
                    return;
                }
                else
                {
                    tmpData.FilePath = path;
                    tmpData.Type = type;

                    tmpData.DeviceName = DisplaySelectWindow.selectedDisplay;

                    //remove prev if new wallpaper on same screen
                    int i = 0;
                    if ((i = SetupDesktop.wallpapers.FindIndex(x => x.DeviceName == tmpData.DeviceName)) != -1)
                    {
                        SetupDesktop.CloseWallpaper(SetupDesktop.wallpapers[i].DeviceName);
                    }
                }
            }
            else //single screen
            {
                tmpData.FilePath = path;
                tmpData.Type = type;
                //tmpData.displayID = 0;
                tmpData.DeviceName = Screen.PrimaryScreen.DeviceName;

                SetupDesktop.CloseAllWallpapers(); //close previous wallpapers.
            }

            //progressbar
            if (type == SetupDesktop.WallpaperType.app 
                || type == SetupDesktop.WallpaperType.unity 
                || type == SetupDesktop.WallpaperType.bizhawk 
                || type == SetupDesktop.WallpaperType.godot 
                || type == SetupDesktop.WallpaperType.unity_audio
                || type == SetupDesktop.WallpaperType.video_stream
                //|| SaveData.config.VidPlayer == VideoPlayer.mpv   //should be fast enough, no need for progressdialog..youtube parsing is apptype       
                )
            {
                /*
                progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.msgLoadingAppWallpaper, true,
                     new MetroDialogSettings() { AnimateHide = true, AnimateShow = false });
                //progressController.Canceled += ProgressController_Canceled;
                */
                notification = notify.Manager.CreateMessage()
                    .Accent("#FF0000")
                    .Background("#333")
                    .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                    .HasMessage(Properties.Resources.msgLoadingAppWallpaper)
                    .Dismiss().WithButton("Stop", button => { CancelWallpaperWaiting(); })
                    .WithOverlay(progressBar = new MetroProgressBar
                    {
                        Minimum = 0,
                        Maximum = 100,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                //Height = 0.5f,
                                //BorderThickness = new Thickness(0),
                                //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                                Background = Brushes.Transparent,
                        IsIndeterminate = false,
                        IsHitTestVisible = false,

                    })
                    .Queue();
            }

            Logger.Info("Setting up wallpaper:-" + tmpData.FilePath);
            SetupDesktop.SetWallpaper(tmpData, !showAddWallpaperWindow); //set wallpaper

            float progress = 0;

            while (SetupDesktop.IsProcessWaitDone() == 0)
            {
                if(progressBar != null)
                {
                    if (progress > 100)
                        progressBar.Value = 100 ;

                    progressBar.Value = progress;
                    progress += (100f / SetupDesktop.wallpaperWaitTime)*100f; //~approximation
                    
                    if (isProcessWaitCancelled)
                    {
                        break;
                    }
                }
                await Task.Delay(50);
            }
            if (progressController != null)
            {
                progressController.SetProgress(1);
                //progressController.Canceled -= ProgressController_Canceled;
                await progressController.CloseAsync();
                progressController = null;
            }
            
            if(progressBar != null)
            {
                if (notification != null)
                {
                    notify.Manager.Dismiss(notification);
                }
            }
            
        }

        private bool isProcessWaitCancelled = false;
        private void CancelWallpaperWaiting()
        {
            SetupDesktop.TaskProcessWaitCancel();
            isProcessWaitCancelled = true;
        }

        private bool isProcessRestoreCancelled = false;
        private void CancelWallpaperRestoring()
        {
            SetupDesktop.TaskProcessWaitCancel();
            isProcessRestoreCancelled = true;
        }

        /// <summary>
        /// Restores saved list of wallpapers. (no dialog asking user for input etc compared to setupdesktop());
        /// </summary>
        /// <param name="layout"></param>
        private async void RestoreWallpaper(List<SaveData.WallpaperLayout> layoutList)
        {
            //bool cancelled = false;
            isProcessRestoreCancelled = false;
            float progress = 0;
            int loadedWallpaperCount = 0;
            _isRestoringWallpapers = true;

            MetroProgressBar progressBar = null;
            var notification = notify.Manager.CreateMessage()
                .Accent("#FF0000")
                .Background("#333")
                .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                .HasMessage(Properties.Resources.msgLoadingAppWallpaper)
                .Dismiss().WithButton("Stop", button => { CancelWallpaperWaiting(); })
                .WithOverlay(progressBar = new MetroProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    //Height = 0.5f,
                    //BorderThickness = new Thickness(0),
                    //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                    Background = Brushes.Transparent,
                    IsIndeterminate = false,
                    IsHitTestVisible = false,

                })
                .Queue();

            foreach (var layout in layoutList)
            {
                if (layout.Type == SetupDesktop.WallpaperType.web || layout.Type == SetupDesktop.WallpaperType.url || layout.Type == SetupDesktop.WallpaperType.web_audio)
                {
                    if (HighContrastFix)
                    {
                        Logger.Info("behind-icon mode, skipping cef.");
                        _ = Task.Run(() => (MessageBox.Show("Web wallpaper is not available in High Contrast mode workaround, coming soon.", "Lively: Error, High Contrast Mode")));
                        continue;
                    }

                    if (!File.Exists(Path.Combine(App.PathData + "\\external\\cef\\LivelyCefSharp.exe")))
                    {
                        Logger.Info("cefsharp is missing, skipping wallpaper.");
                        //_ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgWebBrowserMissing, Properties.Resources.txtLivelyErrorMsgTitle)));
                        WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.msgWebBrowserMissing);
                        continue;
                    }
                }
                else if (layout.Type == SetupDesktop.WallpaperType.video && SaveData.config.VidPlayer == VideoPlayer.mpv)
                {
                    if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe"))
                    {
                        //_ = Task.Run(() => (MessageBox.Show("mpv player missing!\nwww.github.com/rocksdanister/lively/wiki/Video-Guide", Properties.Resources.txtLivelyErrorMsgTitle)));
                        WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player missing!", "https://www.github.com/rocksdanister/lively/wiki/Video-Guide");
                        continue;
                    }
                }
                else if (layout.Type == SetupDesktop.WallpaperType.video_stream)
                {
                    if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe") || !File.Exists(App.PathData + "\\external\\mpv\\youtube-dl.exe"))
                    {
                        WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player/youtube-dl missing!", "https://github.com/rocksdanister/lively/wiki/Youtube-Wallpaper");
                        continue;
                    }
                }

                SaveData.WallpaperLayout tmpData = new SaveData.WallpaperLayout();
                if (Multiscreen && (SaveData.config.WallpaperArrangement == WallpaperArrangement.per || SaveData.config.WallpaperArrangement == WallpaperArrangement.duplicate))
                {
                    tmpData.FilePath = layout.FilePath;
                    tmpData.Type = layout.Type;

                    if (layout.Type == SetupDesktop.WallpaperType.video_stream)
                    {
                        tmpData.Arguments = utility.YTDL.YoutubeDLArgGenerate(layout.FilePath, SaveData.config.StreamQuality);
                    }
                    else
                        tmpData.Arguments = layout.Arguments;

                    tmpData.DeviceName = layout.DeviceName;

                    //remove prev if new wallpaper on same screen, in restore case this can happen if savefile is messed up or user just messed with it?
                    int i = 0;
                    if ((i = SetupDesktop.wallpapers.FindIndex(x => x.DeviceName == tmpData.DeviceName)) != -1)
                    {
                        SetupDesktop.CloseWallpaper(SetupDesktop.wallpapers[i].DeviceName);
                    }
                }
                else //single screen
                {
                    tmpData.FilePath = layout.FilePath;
                    tmpData.Type = layout.Type;
                    tmpData.DeviceName = Screen.PrimaryScreen.DeviceName;

                    if (layout.Type == SetupDesktop.WallpaperType.video_stream)
                    {
                        tmpData.Arguments = utility.YTDL.YoutubeDLArgGenerate(layout.FilePath, SaveData.config.StreamQuality);
                    }
                    else
                        tmpData.Arguments = layout.Arguments;

                    SetupDesktop.CloseAllWallpapers(); //close previous wallpapers.
                }

                Logger.Info("Setting up wallpaper:-" + tmpData.FilePath);
                SetupDesktop.SetWallpaper(tmpData, false); //set wallpaper
                loadedWallpaperCount++;


                progressBar.Value = progress * 100;
                progress += (float)loadedWallpaperCount / (float)layoutList.Count;
                //SetupDesktop.wallpapers.Add(tmpData);
                while (SetupDesktop.IsProcessWaitDone() == 0)
                {
                    await Task.Delay(50);
                    if (isProcessRestoreCancelled)
                    {
                        break;
                    }
                }
                
                if (isProcessRestoreCancelled)
                { 
                    break;
                }
            }

            _isRestoringWallpapers = false;
            layoutList.Clear();
            layoutList = null;

            notify.Manager.Dismiss(notification);
        }
        #endregion wp_setup

        #region wallpaper_installer

        class ZipInstallInfo
        {
            public MetroProgressBar ProgressBar { get; private set; }
            public INotificationMessage Notification { get; private set; }
            public ZipFile ZipFile { get; private set; }
            public bool AbortZipExtraction { get; set; }
            public ZipInstallInfo(MetroProgressBar progressBar, INotificationMessage notification, ZipFile zipFile)
            {
                this.Notification = notification;
                this.ProgressBar = progressBar;
                this.ZipFile = zipFile;
                AbortZipExtraction = false;
            }
        }

        List<ZipInstallInfo> zipInstaller = new List<ZipInstallInfo>();
        private async void WallpaperInstaller(string zipLocation)
        {
            ZipInstallInfo zipInstance = null;
            string randomFolderName = Path.GetRandomFileName();
            string extractPath = null;
            extractPath = Path.Combine(App.PathData, "wallpapers", randomFolderName);

            //Todo: implement CheckZip() {thread blocking}, Error will be thrown during extractiong, which is being handled so not a big deal.
            //Ionic.Zip.ZipFile.CheckZip(zipLocation)

            if (Directory.Exists(extractPath)) //likely impossible.
            {
                Debug.WriteLine("same foldername with files, should be impossible... retrying with new random foldername");
                extractPath = App.PathData + "\\wallpapers\\" + Path.GetRandomFileName();

                if (Directory.Exists(extractPath))
                {
                    Logger.Error("same folderpath name, stopping wallpaper installation");
                    return;
                }
            }
            Directory.CreateDirectory(extractPath);

            string zipPath = zipLocation;
            // Normalizes the path.
            extractPath = Path.GetFullPath(extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char. 
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            try
            {
                // Specifying Console.Out here causes diagnostic msgs to be sent to the Console
                // In a WinForms or WPF or Web app, you could specify nothing, or an alternate
                // TextWriter to capture diagnostic messages.

                //var options = new ReadOptions { StatusMessageWriter = System.Console.Out };
                using (ZipFile zip = ZipFile.Read(zipPath))//, options))
                {
                    zip.ZipErrorAction = ZipErrorAction.Throw; //todo:- test with a corrupted zip that starts extracting.
                    //zip.ZipError += Zip_ZipError;
                    // This call to ExtractAll() assumes:
                    //   - none of the entries are password-protected.
                    //   - want to extract all entries to current working directory
                    //   - none of the files in the zip already exist in the directory;
                    //     if they do, the method will throw.
                    if (zip.ContainsEntry("LivelyInfo.json")) //outer directory only.
                    {
                        MetroProgressBar progressBar = null;
                        var notification = notify.Manager.CreateMessage()
                            //.Accent("#808080")
                            .Background("#333")
                            .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                            .HasMessage(Properties.Resources.txtLabel39 +" " + Path.GetFileName(zipLocation))
                            .Dismiss().WithButton("Stop", button => { Zip_ExtractCancel(zip); }) //HOW TO CANCEL?
                            .WithOverlay(progressBar = new MetroProgressBar
                            {
                                Minimum = 0,
                                Maximum = 100,
                                VerticalAlignment = VerticalAlignment.Bottom,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                //Height = 0.5f,
                                //BorderThickness = new Thickness(0),
                                //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                                Background = Brushes.Transparent,
                                IsIndeterminate = false,
                                IsHitTestVisible = false,

                            })
                            .Queue();

                         zipInstaller.Add(zipInstance = new ZipInstallInfo(progressBar, notification, zip));

                        //progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.txtLabel39, true);
                        zip.ExtractProgress += Zip_ExtractProgress;
                        //zip.ExtractAll(extractPath);
                        await Task.Run(() => zip.ExtractAll(extractPath));

                    }
                    else
                    {
                        try
                        {
                            Directory.Delete(extractPath, true);
                        }
                        catch 
                        {
                            Logger.Error("Extractionpath delete error");
                        }

                        notify.Manager.CreateMessage()
                        .Accent("#FF0000")
                        .HasBadge("Warn")
                        .Background("#333")
                        .HasHeader(Properties.Resources.txtLivelyErrorMsgTitle)
                        .HasMessage("Not Lively wallpaper .zip file.")
                        .Dismiss().WithButton("Ok", button => { })
                        .Queue();

                        //await this.ShowMessageAsync("Error", "Not Lively wallpaper file.\nCheck out wiki page on how to create proper wallpaper file", MessageDialogStyle.Affirmative,
                        //    new MetroDialogSettings() { DialogTitleFontSize = 25, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });
                    }
                }
            }
            catch (Ionic.Zip.ZipException e)
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch 
                {
                    Logger.Error("Extractionpath delete error");
                }

                Logger.Error(e.ToString());
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.msgDamangedLivelyFile +"\n" + e.Message);
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch 
                {
                    Logger.Error("Extractionpath delete error");
                }
                Logger.Error(ex.ToString());
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, ex.Message);
            }
            finally
            {
                if (zipInstance != null)
                {
                    if(zipInstance.Notification != null)
                        notify.Manager.Dismiss(zipInstance.Notification);
         
                    if (zipInstance.AbortZipExtraction)
                    {
                        try
                        {
                            Directory.Delete(extractPath, true);
                        }
                        catch
                        {
                            Logger.Error("Extractionpath delete error (Aborted)");
                        }
                    }
                    zipInstaller.Remove(zipInstance);
                }
            }

            UpdateWallpaperLibrary();
            //selecting installed wp..
            foreach (var item in tileDataList)
            {
                if(item.LivelyInfoDirectoryLocation.Contains(randomFolderName))
                {
                    wallpapersLV.SelectedItem = item;
                    break;
                }
            }
        }

        private void Zip_ExtractCancel(ZipFile zip)
        {
            var obj = zipInstaller.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            obj.AbortZipExtraction = true;
        }

        private async void Zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            var zip = (ZipFile)sender;
            var obj = zipInstaller.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            if(obj.AbortZipExtraction)
            {
                e.Cancel = true;
                return;
            }

            if (e.EntriesTotal != 0)
            {
                if(obj.ProgressBar != null)
                {
                    this.Dispatcher.Invoke(() => {
                        obj.ProgressBar.Value = ((float)e.EntriesExtracted / (float)e.EntriesTotal) * 100f;
                    });
                }

            }

            if(e.EntriesExtracted == e.EntriesTotal && e.EntriesTotal != 0)
            {
                //completion.
            }
        }

        public void Button_Click_InstallWallpaper(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Title = "Open Lively Wallpaper File",
                Filter = "Lively Wallpaper (*.zip) |*.zip"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                if (tabControl1.SelectedIndex != 0) //switch to library tab.)
                    tabControl1.SelectedIndex = 0;
                WallpaperInstaller(openFileDialog1.FileName);
            }
        }

        #endregion wallpaper_installer
        
        #region notification_dialogues

        public enum NotificationType
        {
            info,
            error,
            alert,
            infoUrl,
            errorUrl
        }
        public void WpfNotification(NotificationType type, object title, object message, string url = null)
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var accentColor = this.TryFindResource("AccentColorBrush") as SolidColorBrush;

            if (type == NotificationType.info)
            {
                notify.Manager.CreateMessage()
                   .Accent(accentColor)
                   .HasBadge("INFO")
                   .Background("#333")
                   .HasHeader((string)title)
                   .HasMessage((string)message)
                   .Dismiss().WithButton("Ok", button => { })
                   .Queue();
            }
            else if (type == NotificationType.infoUrl)
            {

                Hyperlink hyper = new Hyperlink
                {                
                    Foreground = new SolidColorBrush(Colors.Gray),
                };

                try
                {
                    hyper.NavigateUri = new System.Uri(url);
                }
                catch
                {
                    url = "https://github.com/rocksdanister/lively/wiki";
                    hyper.NavigateUri = new System.Uri(url);
                }
                hyper.RequestNavigate += Hyperlink_RequestNavigate;

                TextBlock tb = new TextBlock()
                {
                    Margin = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };

                Run run = new Run
                {
                    Text = url,      
                };

                hyper.Inlines.Add(run);
                tb.Inlines.Add(hyper);

                notify.Manager.CreateMessage()
                    .Accent(this.TryFindResource("AccentColorBrush") as SolidColorBrush)
                    //.Accent("#808080")
                    .HasBadge("INFO")
                    .Background("#333")
                    .HasHeader((string)title)
                    .HasMessage((string)message)
                    .Dismiss().WithButton("Ok", button => { })
                    .WithAdditionalContent(ContentLocation.Bottom, new Border
                    {
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(128, 28, 28, 28)),
                        Child = tb
                    })
                    //.WithAdditionalContent(ContentLocation.Bottom, hyper)
                    .Queue();

            }
            else if( type == NotificationType.error)
            {
                notify.Manager.CreateMessage()
                 .Accent("#FF0000")
                 .HasBadge("ERROR")
                 .Background("#333")
                 .HasHeader((string)title)
                 .HasMessage((string)message)
                 .Dismiss().WithButton("Ok", button => { })
                 .Queue();
            }
            else if( type == NotificationType.errorUrl)
            {
                Hyperlink hyper = new Hyperlink
                {
                    Foreground = new SolidColorBrush(Colors.Gray),
                };

                try
                {
                    hyper.NavigateUri = new System.Uri(url);
                }
                catch
                {
                    url = "https://github.com/rocksdanister/lively/wiki";
                    hyper.NavigateUri = new System.Uri(url);
                }
                hyper.RequestNavigate += Hyperlink_RequestNavigate;

                TextBlock tb = new TextBlock()
                {
                    Margin = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };

                Run run = new Run
                {
                    Text = url,
                };

                hyper.Inlines.Add(run);
                tb.Inlines.Add(hyper);

                notify.Manager.CreateMessage()
                    .Accent("#FF0000")
                    //.Accent("#808080")
                    .HasBadge("ERROR")
                    .Background("#333")
                    .HasHeader((string)title)
                    .HasMessage((string)message)
                    .Dismiss().WithButton("Ok", button => { })
                    .WithAdditionalContent(ContentLocation.Bottom, new Border
                    {
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(128, 28, 28, 28)),
                        Child = tb
                    })
                    //.WithAdditionalContent(ContentLocation.Bottom, hyper)
                    .Queue();
            }
        }

        #endregion 
    }
}
