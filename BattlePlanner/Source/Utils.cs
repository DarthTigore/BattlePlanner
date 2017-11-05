using System;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace BattlePlanner
{
    /// <summary>
    /// Collection of useful data and functions.
    /// </summary>
    class Utils
    {
        public static string LastError = string.Empty;

        /// <summary>
        /// Write text to a file
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">Text data to write to the file.</param>
        /// <param name="append">Should the data append the file?</param>
        /// <returns>True if the operation succeeded.</returns>
        public static bool WriteFile(string fileName, string data, bool append = false)
        {
            try
            {
                if (append)
                {
                    File.AppendAllText(fileName, data);
                }
                else
                {
                    File.WriteAllText(fileName, data);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Load a file fully into memory and pass it back as a string
        /// </summary>
        /// <param name="fileName">Name of the file to load.</param>
        /// <returns>String representing the file contents. Empty string on failure.</returns>
        public static string LoadFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    return File.ReadAllText(fileName);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// Clear out all files from a directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true if successful</returns>
        public static bool ClearDirectory(string path)
        {
            try
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    File.Delete(file);
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Get a substring
        /// </summary>
        /// <param name="text"></param>
        /// <param name="token1"></param>
        /// <param name="token2"></param>
        /// <returns></returns>
        public static string GetSubstring(string text, string token1, string token2)
        {
            var startIdx = text.IndexOf(token1) + token1.Length;
            var endIdx = (token2 == null) ? text.Length : text.IndexOf(token2);
            var value = text.Substring(startIdx, endIdx - startIdx).Trim();

            return value;
        }

        /// <summary>
        /// Fixup special characters in swgoh.gg web source
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string FixString(string text)
        {
            return text.Replace("&quot;", "\"").Replace("&#39;", "'");
        }

        /// <summary>
        /// Get the source text from a web url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetWebSource(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                var response = request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                var data = reader.ReadToEnd();

                return data;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                return string.Empty;
            }
        }

        /// <summary>
        /// Convert a Bitmap to a BitmapImage
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static BitmapImage BitmapToBitmapImage(Bitmap bmp)
        {
            
            using (var memory = new MemoryStream())
            {
                bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }


    }
}
