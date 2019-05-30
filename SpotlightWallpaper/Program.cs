using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace SpotlightWallpaper
{
    internal class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int SystemParametersInfo(
            int uAction,
            int uParam,
            string lpvParam,
            int fuWinIni);

        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Spotlight lockscreen wallpaper sync.");

                var currentLockscreenImage = GetCurrentLockscreenImage(GetCurrentUserSid());
                if (string.IsNullOrEmpty(currentLockscreenImage))
                    currentLockscreenImage = GetCurrentLockscreenImage();

                if (string.IsNullOrEmpty(currentLockscreenImage))
                    return;

                SetDesktopBackgroup(currentLockscreenImage);

                Console.WriteLine("Spotlight lockscreen sync successfull.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string GetCurrentLockscreenImage(object userSid)
        {
            Console.WriteLine("Finding current lockscreen image for SID {0}...", userSid);

            using (var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var sidKey =
                    hklmKey.OpenSubKey(
                        $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\Creative\\{userSid}"))
                {
                    if (sidKey == null)
                        return null;

                    var name = sidKey.GetSubKeyNames().LastOrDefault();
                    if (string.IsNullOrEmpty(name))
                        return null;

                    using (var imgKey = sidKey.OpenSubKey(name))
                    {
                        if (imgKey == null)
                            return null;

                        var path = imgKey.GetValue("landscapeImage", string.Empty).ToString();
                        if (!File.Exists(path))
                            return null;

                        return path;
                    }
                }
            }
        }

        private static object GetCurrentUserSid()
        {
            Console.WriteLine("Gettting user SID.");

            return WindowsIdentity.GetCurrent().User.Value;
        }

        private static void SetDesktopBackgroup(string wallpaperPath)
        {
            Console.WriteLine("Setting desktop background to '{0}'...", wallpaperPath);

            SystemParametersInfo(20, 0, wallpaperPath, 3);
        }

        private static string GetCurrentLockscreenImage()
        {
            Console.WriteLine("Finding current lockscreen image...");

            using (var hkcuKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            {
                using (var creativeKey =
                    hkcuKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Lock Screen\\Creative"))
                {
                    if (creativeKey == null)
                        return null;

                    var path = creativeKey.GetValue("LandscapeAssetPath", string.Empty).ToString();
                    if (!File.Exists(path))
                        throw new FileNotFoundException("Could not find current lockscreen image.");

                    return path;
                }
            }
        }
    }
}