using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WClocks
{
    class AutorunManager
    {
        readonly string applicationName;
        readonly string appFileName;
        readonly string appPath;
        public AutorunManager(string aName, string aFolder)
        {
            applicationName = aName;
            appFileName = aName + ".exe";
            appPath = Path.Combine(aFolder, appFileName);
            InstallApp(appPath);
        }

        private void InstallApp(string installPath)
        {
            try
            {
                string assemblyPath = Process.GetCurrentProcess().MainModule.FileName;

                if (!String.Equals(installPath, assemblyPath, StringComparison.OrdinalIgnoreCase) &&
                    File.GetLastWriteTime(installPath) < File.GetLastWriteTime(assemblyPath))
                    File.Copy(assemblyPath, installPath, true);
            }
            catch (Exception ex) { }
        }

        private string GetAutorunPath(string fileName, string fileExt = ".lnk")
        {
            string appName = Path.GetFileNameWithoutExtension(fileName);
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupFolder, appName + fileExt);
        }

        public bool IsAutorunEnabled
        {
            get
            {
                string autorunFileLink = GetAutorunPath(applicationName);
                string autorunFileExe = GetAutorunPath(applicationName, ".exe");
                return (File.Exists(autorunFileLink) || File.Exists(autorunFileExe));
            }
        }


        private void CreateShortcut(string fileName)
        {
            string appName = Path.GetFileNameWithoutExtension(fileName);
            string shortcutAddress = GetAutorunPath(applicationName);

            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = $"{appName} autorun link. You can disable autorun in {appName} menu.";
            shortcut.WorkingDirectory = Path.GetDirectoryName(appPath);
            shortcut.TargetPath = appPath;
            shortcut.Save();
        }

        public void EnableAutorun()
        {
            string autorunFileLink = GetAutorunPath(applicationName);
            if (!File.Exists(autorunFileLink)) CreateShortcut(applicationName);
        }

        public void DisableAutorun()
        {
            try
            {
                string autorunFileLink = GetAutorunPath(applicationName);
                if (File.Exists(autorunFileLink)) File.Delete(autorunFileLink);
            }
            catch (Exception ex) { }

            try
            {
                string autorunFileExe = GetAutorunPath(applicationName, ".exe");
                if (File.Exists(autorunFileExe)) File.Delete(autorunFileExe);
            }
            catch (Exception ex) { }
        }

        public void SetAutorun(bool autorun)
        {
            if (autorun) EnableAutorun();
            else DisableAutorun();
        }
    }
}
