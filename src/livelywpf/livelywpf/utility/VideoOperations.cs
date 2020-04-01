using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.utility
{
    public static class VideoOperations
    {
        /// <summary>
        /// Determine video resolution
        /// </summary>
        /// <param name="videoFullPath"></param>
        /// <returns>x = width, y = heigh</returns>
        public static Size GetVideoSize(string videoFullPath)
        {
            try
            {
                if (File.Exists(videoFullPath))
                {
                    ShellFile shellFile = ShellFile.FromFilePath(videoFullPath);

                    int videoWidth = (int)shellFile.Properties.System.Video.FrameWidth.Value;
                    int videoHeight = (int)shellFile.Properties.System.Video.FrameHeight.Value;

                    return new Size(videoWidth, videoHeight);
                }
            }
            catch (Exception)
            {
                return Size.Empty;
            }
            return Size.Empty;
        }

        public static bool IsVideoFile(string path)
        {
            string[] formatsVideo = { ".dat", ".wmv", ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf",  ".avi", ".bin", ".cue", ".divx", ".dv", ".flv", ".gxf", ".iso", ".m1v", ".m2v", ".m2t", ".m2ts", ".m4v",
                                        ".mkv", ".mov", ".mp2", ".mp2v", ".mp4", ".mp4v", ".mpa", ".mpe", ".mpeg", ".mpeg1", ".mpeg2", ".mpeg4", ".mpg", ".mpv2", ".mts", ".nsv", ".nuv", ".ogg", ".ogm", ".ogv", ".ogx", ".ps", ".rec", ".rm",
                                        ".rmvb", ".tod", ".ts", ".tts", ".vob", ".vro", ".webm" };
            if (formatsVideo.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}
