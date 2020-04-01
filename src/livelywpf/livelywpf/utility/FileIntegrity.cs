using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.utility
{
    public static class FileIntegrity
    {
        /// <summary>
        /// Calculates SHA256 hash of file.
        /// </summary>
        /// <param name="filepath">path to the file.</param>
        public static string CalculateFileCheckSum(string filepath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
