using Microsoft.Win32;

namespace AutocleanRegistry
{
    internal class RegistryController
    {
        private const string UninstallValueName = "UninstallString"; 
        private const string InstallSource = "InstallSource"; 
        private const string DisplayName = "DisplayName";
        private const string UninstallPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
       
        public void AutocleanUninstall()
        {
            CreateBackup();
            RegistryKey rk = Registry.LocalMachine;
            var uninstallRk = rk.OpenSubKey(UninstallPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (uninstallRk == null) { return; }
            FlatLook(uninstallRk, uk =>
            {
                var valueNames = uk.GetValueNames();
                if (valueNames.Contains(DisplayName))
                {
                    var displayName = uk.GetValue(DisplayName);
                    Console.WriteLine(displayName);
                }
                if (valueNames.Contains(UninstallValueName))
                {
                    var uninstallPath = uk.GetValue(UninstallValueName);
                    if (uninstallPath != null)
                    {
                        CheckAndRemoveKey(uk, uninstallRk, uninstallPath);
                    }
                }
                if (valueNames.Contains(InstallSource))
                {
                    var installFilePath = uk.GetValue(InstallSource);
                    if (installFilePath != null)
                    {
                        Console.WriteLine(installFilePath);
                        CheckAndRemoveKey(uk, uninstallRk, installFilePath);
                    }
                }
            });
        }

        private static void CheckAndRemoveKey(RegistryKey uk, RegistryKey uninstallRk, object? uninstallPath)
        {
            var rawPath = "" + uninstallPath.ToString();
            var indexOfFirstQuotes = rawPath.IndexOf("\"");
            Console.WriteLine(rawPath);
            if (indexOfFirstQuotes == -1)
            {
                return;
            }

            rawPath = rawPath.Substring(indexOfFirstQuotes + 1);
            var indexOfSecondQuotes = rawPath.IndexOf("\"");
            if (indexOfSecondQuotes == -1)
            {
                return;
            }
            rawPath = rawPath.Substring(0, indexOfSecondQuotes);
            var path = rawPath;

            //---
            var temp = Console.ForegroundColor;
            var isPathCorrect = Directory.Exists(path) || File.Exists(path);
            Console.ForegroundColor = isPathCorrect ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(path);
            Console.ForegroundColor = temp;
            if (!isPathCorrect)
            {
                var name = uk.Name.Split("\\").Last();
                uninstallRk.DeleteSubKey(name);
            }
        }

        public static void BackupRegistryKey(string keyPath, string backupFilePath)
        {
            var regKey = Registry.LocalMachine.OpenSubKey(keyPath);
            if (regKey == null)
            {
                Console.WriteLine("Registry key does not exist: " + keyPath);
                return;
            }
            using var writer = new StreamWriter(backupFilePath);
            WriteDeep(writer, regKey);
        }

        private static void WriteDeep(StreamWriter writer, RegistryKey regKey)
        {
            writer.WriteLine($"[{regKey}]");
            string[] valueNames = regKey.GetValueNames();
            foreach (string valueName in valueNames)
            {
                string? value = regKey.GetValue(valueName)?.ToString();
                writer.WriteLine($"{valueName}={value ?? ""}");
            }
            FlatLook(regKey, uk =>
            {
                WriteDeep(writer, uk);
            });
        }

        public static void RestoreRegistryKey(string keyPath, string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                Console.WriteLine("Backup file does not exist: " + backupFilePath);
                return;
            }
            using (StreamReader reader = new StreamReader(backupFilePath))
            {
                string? line;
                var regKey = Registry.CurrentUser.OpenSubKey(keyPath, true);
                if (regKey == null)
                {
                    Console.WriteLine("Registry key does not exist: " + keyPath);
                    return;
                }

                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('=');
                    string valueName = parts[0];
                    string valueData = parts[1];
                    regKey.SetValue(valueName, valueData);
                    Console.WriteLine($"Restored value: {valueName}={valueData}");
                }
            }
        }

        private static void Export(string exportPath, string registryPath)
        {
            BackupRegistryKey(registryPath, exportPath);
        }

        private static void FlatLook(RegistryKey rk, Action<RegistryKey> onEveryKey)
        {
            string[] names = rk.GetSubKeyNames();
            foreach (string subKeyName in rk.GetSubKeyNames())
            {
                try
                {
                    using (var tempKey = rk.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        if (tempKey != null)
                        {
                            onEveryKey(tempKey);
                        }
                        else
                        {
                            Console.WriteLine(subKeyName + "; " + tempKey + " is null");
                        }
                    }
                }
                catch (Exception ex)
                {
                    var tmp = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.ToString());
                    Console.ForegroundColor = tmp;
                }
            }
        }

        private static void CreateBackup()
        {
            var fileName = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fffffff") + ".reg";
            Export(fileName, UninstallPath);
        }
    }
}