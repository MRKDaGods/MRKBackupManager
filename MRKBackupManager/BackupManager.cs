using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MRKBackupManager {
    public class BackupManager {
        class Reference<T> {
            public T Value { get; set; }
        }

        const char DRV = 'E';
        const string DRVEXT = "mrkbkp";
        static string DrvPath => $"{DRV}:\\mrkbackupmanager";

        public static List<string> GetBackupFiles(char drv) {
            if (!Directory.Exists(DrvPath))
                Directory.CreateDirectory(DrvPath);

            return Directory.EnumerateFiles(DrvPath, $"*.{DRVEXT}", SearchOption.AllDirectories).ToList();
        }

        static Backup ReadBackup(string backupFile, Reference<string> err = null) {
            if (!File.Exists(backupFile))
                return null;

            try {
                using (FileStream fstream = new FileStream(backupFile, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(fstream)) {
                    Backup backup = new Backup {
                        Name = reader.ReadString(),
                        Source = reader.ReadString(),
                        Location = reader.ReadString(),
                        CreationDate = new DateTime(reader.ReadInt64()),
                        LastModificationDate = new DateTime(reader.ReadInt64())
                    };

                    reader.Close();

                    return backup;
                }
            }
            catch (Exception ex) {
                if (err != null)
                    err.Value = ex.Message;

                return null;
            }
        }

        static void WriteBackup(Backup backup, Reference<string> err = null) {
            try {
                using (FileStream fstream = new FileStream($"{backup.Location}\\__mrk_{backup.Name}.{DRVEXT}", FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(fstream)) {
                    writer.Write(backup.Name);
                    writer.Write(backup.Source);
                    writer.Write(backup.Location);
                    writer.Write(backup.CreationDate.Ticks);
                    writer.Write(backup.LastModificationDate.Ticks);

                    writer.Close();
                }
            }
            catch (Exception ex) {
                if (err != null)
                    err.Value = ex.Message;
            }
        }

        public static List<Backup> GetBackups() {
            List<Backup> buf = new List<Backup>();

            foreach (string backupFile in GetBackupFiles('E')) {
                Reference<string> error = new Reference<string>();
                Backup backup = ReadBackup(backupFile, error);
                if (backup == null) {
                    Console.WriteLine($"Corrupted backup file {backupFile} | {error.Value}");
                    continue;
                }

                buf.Add(backup);
            }

            return buf;
        }

        public static Backup FindBackup(string name) {
            return GetBackups().Find(x => x.Name == name);
        }

        static void WriteBackupInfo(Backup backup, bool raw) {
            //requires name&src

            string location = backup.Location;
            if (raw) {
                Random rand = new Random();
                int count = rand.Next(20, 25);
                for (int i = 0; i < count; i++)
                    location += rand.Next(0, 2) == 0 ? (char)rand.Next(97, 123) : (char)rand.Next(65, 91);
            }

            string loc = $"{DrvPath}\\{location.Replace(DrvPath, "")}";
            if (!raw)
                loc = loc.Remove(DrvPath.Length, 1); // remove \

            Directory.CreateDirectory(loc);

            backup.Location = loc;
            WriteBackup(backup);
        }

        public static void CreateBackup(string name, string src) {
            Backup backup = new Backup {
                Name = name,
                Source = src,
                CreationDate = DateTime.Now
            };

            WriteBackupInfo(backup, true);
        }

        static void CreateRecursiveDir(string dir) {
            int start = 0;
            while (start < dir.Length) {
                int sepIdx = dir.IndexOf('\\', start);
                if (sepIdx == -1)
                    sepIdx = dir.Length - 1;

                string _dir = dir.Substring(0, sepIdx + 1);
                if (!Directory.Exists(_dir))
                    Directory.CreateDirectory(_dir);

                start = sepIdx + 1;
            }

            Console.WriteLine($"Creating dir {dir}");
        }

        public static void UpdateBackup(Backup backup) {
            //write em all boiis
            foreach (string filename in Directory.EnumerateFiles(backup.Source, "*", SearchOption.AllDirectories)) {
                string subdir = filename.Substring(backup.Source.Length);
                int lastSepIdx = subdir.LastIndexOf('\\');
                string file = subdir.Substring(lastSepIdx + 1);
                subdir = subdir.Substring(0, lastSepIdx);

                string newDir = $"{backup.Location}{subdir}";
                if (!Directory.Exists(newDir))
                    CreateRecursiveDir(newDir);

                Console.WriteLine($"Copying {filename}");
                File.Copy(filename, $"{newDir}\\{file}", true);
            }

            backup.LastModificationDate = DateTime.Now;
            WriteBackupInfo(backup, false);
        }

        public static void DeleteBackup(Backup backup) {
            Directory.Delete(backup.Location, true);
        }

        public static void RestoreBackup(Backup backup, string target) {
            if (Directory.Exists(target))
                Directory.Delete(target, true);

            foreach (string filename in Directory.EnumerateFiles(backup.Location, "*", SearchOption.AllDirectories)) {
                if (filename.EndsWith($".{DRVEXT}"))
                    continue;

                string subdir = filename.Substring(backup.Location.Length);
                int lastSepIdx = subdir.LastIndexOf('\\');
                string file = subdir.Substring(lastSepIdx + 1);
                subdir = subdir.Substring(0, lastSepIdx);

                string newDir = $"{target}{subdir}";
                if (!Directory.Exists(newDir))
                    CreateRecursiveDir(newDir);

                Console.WriteLine($"Copying {filename}");
                File.Copy(filename, $"{newDir}\\{file}", true);
            }
        }
    }
}
