using System;
using System.IO;
using System.Windows;

namespace WClocks
{
    internal class Logger
    {
        static readonly string logFile = System.IO.Path.Combine(MainWindow.ApplicationFolder, "log.txt");


        public const string TraceString = "TRACE";
        public const string DebugString = "DEBUG";
        public const string InfoString = "INFO";
        public const string WarnString = "WARN";
        public const string ErrorString = "ERROR";
        public const string FatalString = "FATAL";


        public static void Write(string label, string text)
        {
            try
            {
                File.AppendAllText(logFile, $"{label} [{DateTime.Now}] {text}\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error write log to\n{logFile}\n\nDetails: {ex.Message}", MainWindow.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void Trace(string text)
        {
            Write(TraceString, text);
        }

        public static void Debug(string text)
        {
            Write(DebugString, text);
        }

        public static void Info(string text)
        {
            Write(InfoString, text);
        }

        public static void Warn(string text)
        {
            Write(WarnString, text);
        }

        public static void Error(string text)
        {
            Write(ErrorString, text);
        }

        public static void Fatal(string text)
        {
            Write(FatalString, text);
        }
    }
}
