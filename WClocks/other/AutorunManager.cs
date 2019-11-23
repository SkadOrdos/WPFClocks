using System;
using System.IO;
using System.Reflection;

namespace WClocks
{
    class AutorunManager
    {
        readonly string applicationPath;
        public AutorunManager(string appFolder)
        {
            applicationPath = Path.Combine(appFolder, AppDomain.CurrentDomain.FriendlyName);
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            if (File.GetLastWriteTime(applicationPath) < File.GetLastWriteTime(assemblyPath))
                File.Copy(assemblyPath, applicationPath, true);
        }


        private string GetAutorunPath(string fileExt = ".lnk")
        {
            string appName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupFolder, appName + fileExt);
        }

        public bool IsAutorunEnabled
        {
            get
            {
                string autorunFileLink = GetAutorunPath();
                string autorunFileExe = GetAutorunPath(".exe");
                return (File.Exists(autorunFileLink) || File.Exists(autorunFileExe));
            }
        }


        private void CreateShortcut()
        {
            string appName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);

            object shDesktop = (object)"Startup"; // (object)"Desktop"
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\" + appName + ".lnk";
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.WorkingDirectory = Path.GetDirectoryName(applicationPath);
            shortcut.Description = "WClocks autorun link. You can disable autorun in WClocks menu.";
            shortcut.TargetPath = applicationPath;
            shortcut.Save();
        }

        public void EnableAutorun()
        {
            string autorunFileLink = GetAutorunPath();
            if (!File.Exists(autorunFileLink)) CreateShortcut();
        }

        public void DisableAutorun()
        {
            try
            {
                string autorunFileLink = GetAutorunPath();
                if (File.Exists(autorunFileLink)) File.Delete(autorunFileLink);

                string autorunFileExe = GetAutorunPath(".exe");
                if (File.Exists(autorunFileExe)) File.Delete(autorunFileExe);
            }
            catch (Exception) { }
        }
    }
}
