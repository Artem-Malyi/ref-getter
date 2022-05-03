using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace rsAMSI
{
    public static class amLogger
    {
        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        public static extern Int32 GetCurrentWin32ThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        [DllImport("shell32.dll")]
        public static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        // FOLDERID_LocalAppDataLow - {A520A1A4-1780-4FF6-BD18-167343C5AF16}
        private static readonly Guid LocalLowGuid = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
        private static readonly string LogFileName = "log.txt";
        private static readonly string LogMutexName = "Global\\235CC6D9-A4FF-49EE-826C-31CC44846B05";
        private static string logPath = "";

        [Flags]
        public enum KnownFolderFlag : uint
        {
            None = 0x0,
            CREATE = 0x8000,
            DONT_VERFIY = 0x4000,
            DONT_UNEXPAND = 0x2000,
            NO_ALIAS = 0x1000,
            INIT = 0x800,
            DEFAULT_PATH = 0x400,
            NOT_PARENT_RELATIVE = 0x200,
            SIMPLE_IDLIST = 0x100,
            ALIAS_ONLY = 0x80000000
        }

        private static void WriteToFileInAppDataLocalLow(string message)
        {
            if (0 == logPath.Length)
            {
                IntPtr pPath;
                int hr = SHGetKnownFolderPath(LocalLowGuid, (uint)KnownFolderFlag.None, IntPtr.Zero, out pPath);
                if (hr >= 0)
                {
                    logPath = System.Runtime.InteropServices.Marshal.PtrToStringUni(pPath) + "\\" + LogFileName;
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pPath);
                }
            }
            using (StreamWriter w = File.AppendText(logPath))
            {
                w.WriteLine(message);
            }
        }

        private static void LogToFile(string message)
        {
            System.Threading.Mutex m = null;

            bool doesNotExist = false;
            try { m = System.Threading.Mutex.OpenExisting(LogMutexName); }
            catch (WaitHandleCannotBeOpenedException) { doesNotExist = true; }
            if (doesNotExist)
            {
                m = new Mutex(false, LogMutexName);
            }

            bool signaled = m.WaitOne(5000);
            if (signaled)
            {
                WriteToFileInAppDataLocalLow(message);

                m.ReleaseMutex();
            }

            m.Dispose();
        }

        [Conditional("AMLOG_ENABLED")]
        public static void Log(string tag, string logMessage)
        {
            string fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            int threadId = GetCurrentWin32ThreadId();
            string functionName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name;
            string message = $"{ DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss.fff")} {fileName} [{processId}:{threadId}] {tag}: {functionName} - {logMessage}";

            // write to DBWIN_BUFFER
            OutputDebugString(message);

            // write to file
            LogToFile(message);
        }

    } // public static class amLogger

} // namespace rsAMSI
