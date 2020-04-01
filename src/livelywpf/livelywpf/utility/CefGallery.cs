using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace livelywpf.utility
{
    class CefGallery
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //incomplete: work in progress, not sure weather to finish this.. deviantart downloader.
        Process webProcess;
        private void StartCefBrowserNewWindow(string url)
        {
            webProcess = new Process();
            ProcessStartInfo start1 = new ProcessStartInfo();
            //start1.Arguments = url + @" deviantart";
            start1.Arguments = url + @" online";

            start1.FileName = App.PathData + @"\external\cef\LivelyCefSharp.exe";
            start1.RedirectStandardInput = true;
            start1.RedirectStandardOutput = true;
            start1.UseShellExecute = false;

            webProcess = new Process();
            webProcess = Process.Start(start1);
            webProcess.EnableRaisingEvents = true;
            webProcess.OutputDataReceived += WebProcess_OutputDataReceived;
            webProcess.Exited += WebProcess_Exited;
            webProcess.BeginOutputReadLine();

        }

        private void WebProcess_Exited(object sender, EventArgs e)
        {
            webProcess.OutputDataReceived -= WebProcess_OutputDataReceived;
            webProcess.Close();
        }

        private static void WebProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Info("CEF:" + e.Data);
            try
            {
                if (e.Data.Contains("LOADWP"))
                {
                    var downloadedFilePath = e.Data.Replace("LOADWP", String.Empty);

                    System.Windows.Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        App.W.ShowMainWindow();
                        //App.W.WallpaperInstaller(downloadedFilePath);
                    }));
                }
            }
            catch (NullReferenceException)
            {

            }
            catch (Exception)
            {
                //todo
            }
        }

    }
}
