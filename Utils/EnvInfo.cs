using Microsoft.Win32;
using System;
using System.Linq;

namespace ShotgunMetagenome.Utils
{
    public static class EnvInfo
    {
        private static string getRegistryValue(string keyname, string valuename)
                                        => Registry.GetValue(keyname, valuename, "").ToString();

        public static string osVersion { get; }
            = Environment.OSVersion.VersionString;

        public static string osProductName { get; }
            = getRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");

        public static string osRelease { get; }
            = getRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId");

        public static string osbuild { get; }
            = getRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild");

        public static string osbit { get; }
            = Environment.Is64BitOperatingSystem ? "64 bit" : "32 bit";

        public static string processbit { get; }
            = Environment.Is64BitProcess ? "64 bit" : "32 bit";

        public static string frameworkVersion { get; }
            = Environment.Version.ToString();

        public static string registryFrameworkVersion { get; }
            = getRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Version");

        public static string registryFrameworkRelease { get; }
            = getRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Release");

        public static string machineName { get; }
            = Environment.MachineName;

        public static string firstAddress { get; }
            = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().First().GetPhysicalAddress().ToString();

        public static bool IsDupliStart()
        {
            // var minePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            var mine = System.Diagnostics.Process.GetCurrentProcess();
            var mineName = mine.ProcessName;

            System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process p in ps)
            {
                try
                {
                    // if (p.MainModule.FileName.Equals(minePath) && mine.Id != p.Id)
                    if (p.ProcessName.Equals(mineName) && mine.Id != p.Id)
                    {
                        Console.WriteLine("ファイル名: {0}", p.MainModule.FileName);
                        return true;
                    }
                }
                catch (Exception ex)
                {   // エラー: アクセスが拒否されました。
                    // Utils.WriteError("app.err", ex.Message);
                    Console.WriteLine("エラー: {0}", ex.Message);
                }
            }
            return false;
        }

        public static bool IsLicenceInvalid()
        {
            // Check Lic
            if (Approbate.IsNonApprobate(string.Empty))
            {
                // Application.Current.Shutdown();
                return true;
            }
            return false;
        }

        public static string tempPath => System.IO.Path.Combine(
                                        Environment.GetEnvironmentVariable("temp"),
                                        Approbate.LicenceFileName);

        public static void CreateDammyLicense()
        {
            var mes = string.Empty;
            var err = string.Empty;
            var words = new string[] { machineName, firstAddress };
            WfComponent.Utils.FileUtils.WriteFile(tempPath, words, ref err);
        }

        public static int DefaultCpuCore()
        {
            var coreCnt = System.Environment.ProcessorCount;
            if (coreCnt > 5)
                return (coreCnt - 2);
            else
                return (coreCnt - 1);
        }

        public static string MaxCpuCore()
        {
            return Environment.ProcessorCount.ToString();
        }

        public static string CpuCore()
        {
            return DefaultCpuCore().ToString();
        }



        public static void MoveProperties(ref object fromObj, ref object toObj)
        {
            var f = fromObj.GetType().GetProperties().Select(s => s.Name).ToArray();
            var t = toObj.GetType().GetProperties().Select(s => s.Name).ToArray();

            foreach (var fp in f)
            {
                if (t.Contains(fp))
                    toObj.GetType().GetProperty(fp.ToString()).SetValue(toObj,
                        fromObj.GetType().GetProperty(fp.ToString()).GetValue(fromObj));
            }
        }

        public static string DirectoryBackup(string dirPath, string backupDate)
        {
            var bkDir = dirPath + "-" + backupDate;

            // 既存リファレンスのバックアップ
            if (System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.Move(dirPath, bkDir);
                System.IO.Directory.CreateDirectory(dirPath);
            }

            // バックアップ後のディレクトリ名を返す。
            return bkDir;
        }


        public static string DirectoryRollback(string dirPath, string backupDate)
        {
            var bkDir = dirPath + "-" + backupDate;
            var badDir = dirPath + "-" + backupDate + "-falt";

            if (System.IO.Directory.Exists(bkDir))
            {
                // 新規に作成したと思われるディレクトリを保存する。
                if (System.IO.Directory.Exists(dirPath)) System.IO.Directory.Move(dirPath, badDir);
                System.IO.Directory.Move(bkDir, dirPath);
            }
            else
            {
                return "not found backuped directory. " + bkDir;
            }

            return badDir;
        }


        /// <summary>
        /// 指定されたファイルに関連付けられたコマンドを取得する
        /// </summary>
        /// <param name="fileName">関連付けを調べるファイル</param>
        /// <param name="extra">アクション(open,print,editなど)</param>
        /// <returns>取得できた時は、コマンド(実行ファイルのパス+コマンドライン引数)。
        /// 取得できなかった時は、空の文字列。</returns>
        /// <example>
        /// "1.txt"ファイルの"open"に関連付けられたコマンドを取得する例
        /// <code>
        /// string command = FindAssociatedCommand("1.txt", "open");
        /// </code>
        /// </example>
        public static string FindAssociatedCommand(string fileName, string extra)
        {
            //拡張子を取得
            string extName = System.IO.Path.GetExtension(fileName);
            if (extName.Length == 0 || extName[0] != '.')
            {
                return string.Empty;
            }

            //HKEY_CLASSES_ROOT\(extName)\shell があれば、
            //HKEY_CLASSES_ROOT\(extName)\shell\(extra)\command の標準値を返す
            if (ExistClassesRootKey(extName + @"\shell"))
            {
                return GetShellCommandFromClassesRoot(extName, extra);
            }

            //HKEY_CLASSES_ROOT\(extName) の標準値を取得する
            string fileType = GetDefaultValueFromClassesRoot(extName);
            if (fileType.Length == 0)
            {
                return string.Empty;
            }

            //HKEY_CLASSES_ROOT\(fileType)\shell\(extra)\command の標準値を返す
            return GetShellCommandFromClassesRoot(fileType, extra);
        }

        public static string FindAssociatedCommand(string fileName)
        {
            return FindAssociatedCommand(fileName, "open");
        }

        private static bool ExistClassesRootKey(string keyName)
        {
            Microsoft.Win32.RegistryKey regKey =
                Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(keyName);
            if (regKey == null)
            {
                return false;
            }
            regKey.Close();
            return true;
        }

        private static string GetDefaultValueFromClassesRoot(string keyName)
        {
            Microsoft.Win32.RegistryKey regKey =
                Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(keyName);
            if (regKey == null)
            {
                return string.Empty;
            }
            string val = (string)regKey.GetValue(string.Empty, string.Empty);
            regKey.Close();

            return val;
        }

        private static string GetShellCommandFromClassesRoot(
            string fileType, string extra)
        {
            if (extra.Length == 0)
            {
                //アクションが指定されていない時は、既定のアクションを取得する
                extra = GetDefaultValueFromClassesRoot(fileType + @"shell")
                    .Split(',')[0];
                if (extra.Length == 0)
                {
                    extra = "open";
                }
            }
            return GetDefaultValueFromClassesRoot(
                string.Format(@"{0}\shell\{1}\command", fileType, extra));
        }
    }
}
