using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.utility
{
    public static class SystemStartup
    {
        /// <summary>
        /// Adds startup entry in registry under application name "livelywpf", current user ONLY. (Does not require admin rights).
        /// </summary>
        /// <param name="setStartup">false: delete entry, true: add/update entry.</param>
        public static void SetStartupRegistry(bool setStartup = false)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            if (setStartup)
            {
                try
                {
                    key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
                }
                catch 
                {
                    throw;
                }
            }
            else
            {
                try
                {
                    key.DeleteValue(curAssembly.GetName().Name, false);
                }
                catch 
                {
                    throw;
                }
            }
            key.Close();
        }

        /// <summary>
        /// Checks if startup registry entry is present.
        /// </summary>
        /// <returns></returns>
        public static bool CheckStartupRegistry()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            string result = null;
            try
            {
                result = (string)key.GetValue(curAssembly.GetName().Name);
            }
            catch 
            {
                throw;
            }
            finally
            {
                key.Close();
            }

            if (String.IsNullOrEmpty(result))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [Obsolete("Fails to work when folderpath contains non-english characters(WshShell is ancient afterall); use SetStartupRegistry() instead.")]
        /// <summary>
        /// Creates application shortcut & copy to startup folder of current user(does not require admin rights).
        /// </summary>
        /// <param name="setStartup"></param>
        public static
            void SetStartupFolder(bool setStartup = false)
        {
            string shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\LivelyWallpaper.lnk";
            if (setStartup)
            {
                try
                {
                    IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                    System.Reflection.Assembly curAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                    IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
                    shortcut.Description = "Lively Wallpaper System";
                    shortcut.WorkingDirectory = App.PathData;
                    shortcut.TargetPath = curAssembly.Location;
                    shortcut.Save();
                }
                catch 
                {
                    throw;
                }
            }
            else
            {
                if (File.Exists(shortcutAddress))
                {
                    try
                    {
                        File.Delete(shortcutAddress);
                    }
                    catch 
                    {
                        throw;
                    }
                }
            }
        }
    }
}
