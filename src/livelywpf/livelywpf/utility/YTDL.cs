using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static livelywpf.SaveData;

namespace livelywpf.utility
{
    public static class YTDL
    {
        /// <summary>
        /// Returns commandline argument for youtube-dl + mpv, depending on the saved Quality setting.
        /// todo: add codec selection.
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public static string YoutubeDLArgGenerate(string link, SaveData.StreamQualitySuggestion suggestedQuality )
        {
            if (link == null)
                return null;

            string quality = null;
            if (!link.Contains("bilibili.com/video/")) //youtube-dl failing if quality flag is set.
            {
                switch (suggestedQuality)
                {
                    case StreamQualitySuggestion.best:
                        quality = String.Empty;
                        break;
                    case StreamQualitySuggestion.h2160p:
                        quality = " --ytdl-format bestvideo[height<=2160]+bestaudio/best[height<=2160]";
                        break;
                    case StreamQualitySuggestion.h1440p:
                        quality = " --ytdl-format bestvideo[height<=1440]+bestaudio/best[height<=1440]";
                        break;
                    case StreamQualitySuggestion.h1080p:
                        quality = " --ytdl-format bestvideo[height<=1080]+bestaudio/best[height<=1080]";
                        break;
                    case StreamQualitySuggestion.h720p:
                        quality = " --ytdl-format bestvideo[height<=720]+bestaudio/best[height<=720]";
                        break;
                    case StreamQualitySuggestion.h480p:
                        quality = " --ytdl-format bestvideo[height<=480]+bestaudio/best[height<=480]";
                        break;
                    default:
                        quality = " --ytdl-format bestvideo[height<=720]+bestaudio/best[height<=720]";
                        break;
                }
            }
            else
            {
                quality = String.Empty;
            }

            return "\"" + link + "\"" + " --force-window=yes --loop-file --keep-open --hwdec=yes" + quality;
        }
    }
}
