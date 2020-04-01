using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.utility
{
    public class ZipExtract
    {
        //unfinished

        public ZipExtract(string zipLocation)
        {
            //ZipInstallInfo zipInstance = null;
            string randomFolderName = Path.GetRandomFileName();
            string extractPath = null;
            extractPath = Path.Combine(App.PathData, "wallpapers", randomFolderName);

            //Todo: implement CheckZip() {thread blocking}, Error will be thrown during extractiong, which is being handled so not a big deal.
            //Ionic.Zip.ZipFile.CheckZip(zipLocation)

            if (Directory.Exists(extractPath)) //likely impossible.
            {
                //same foldername with files, should be impossible... retrying with new random foldername
                randomFolderName = Path.GetRandomFileName();
                extractPath = Path.Combine(App.PathData, "wallpapers", randomFolderName);
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
                        //zipInstaller.Add(zipInstance = new ZipInstallInfo(progressBar, notification, zip));

                        //progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.txtLabel39, true);
                        //zip.ExtractProgress += Zip_ExtractProgress;
                        //zip.ExtractAll(extractPath);             
                        Task.Run(() => zip.ExtractAll(extractPath));
                    }
                    else
                    {
                        throw new Exception("NotLivelyZipException");
                    }
                }
            }
            catch (Ionic.Zip.ZipException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
