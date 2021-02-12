/*
 * Copyright (c) 2021, Mohamed Ammar <mamar452@gmail.com>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Reflection;
using System.Windows.Forms;

using static System.Console;

namespace MRKBackupManager {
    class Program {
        static bool ms_Running = true;

        [STAThread]
        static void Main(string[] arg0) {
            WriteLine("MRKBackupManager");

            while (ms_Running) {
                Write("\n>");
                string[] cmdline = ReadLine().Trim(' ', '\t').Split(' ');
                if (cmdline.Length == 0 || cmdline[0].Length == 0)
                    continue;

                MethodInfo mInfo = typeof(Program).GetMethod($"__cmd_{cmdline[0]}", BindingFlags.NonPublic | BindingFlags.Static);
                if (mInfo == null) {
                    WriteLine("Command not found");
                    continue;
                }

                string[] args = new string[cmdline.Length - 1];
                Array.Copy(cmdline, 1, args, 0, args.Length);
                mInfo.Invoke(null, new object[1] { args });
            }

            WriteLine("Exiting...");
            ReadLine();
        }

        static T TryGetElement<T>(T[] array, int idx) {
            return array.Length > idx ? array[idx] : default;
        }

        static bool IsStringInvalid(string s) {
            return string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s);
        }

        static void WriteSpaced(string str, int len) {
            Write(str);

            int ln = len - str.Length;
            Write(new string(' ', ln > 0 ? ln : 0));
        }

        static void __cmd_exit(string[] args) {
            ms_Running = false;
        }

        static void __cmd_create(string[] args) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("name cannot be empty");
                return;
            }

            if (BackupManager.FindBackup(name) != null) {
                WriteLine($"backup {name} already exists");
                return;
            }

            string src = TryGetElement(args, 1);
            if (IsStringInvalid(src)) {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK) {
                    src = dialog.SelectedPath;
                }
                else {
                    WriteLine("invalid src path");
                    return;
                }
            }

            BackupManager.CreateBackup(name, src);
            WriteLine($"backup {name} was created");
        }

        static void __cmd_update(string[] args) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("name cannot be empty");
                return;
            }

            Backup backup = BackupManager.FindBackup(name);
            if (backup == null) {
                WriteLine($"backup {name} doesn't exist");
                return;
            }

            BackupManager.UpdateBackup(backup);
            WriteLine($"backup {name} was updated");
        }

        static void __cmd_list(string[] args) {
            WriteSpaced("Name", 15);
            WriteSpaced("Source", 40);
            WriteSpaced("Creation date", 25);
            WriteSpaced("Last modification date", 25);

            Write('\n');

            WriteLine(new string('-', BufferWidth));

            foreach (Backup backup in BackupManager.GetBackups()) {
                WriteSpaced(backup.Name, 15);
                WriteSpaced(backup.Source, 40);
                WriteSpaced(backup.CreationDate.ToString("dd/MM/yyyy (HH:mm)"), 25);
                WriteSpaced(backup.LastModificationDate.ToString("dd/MM/yyyy (HH:mm)"), 25);

                Write('\n');
            }
        }

        static void __cmd_delete(string[] args) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("name cannot be empty");
                return;
            }

            Backup backup = BackupManager.FindBackup(name);
            if (backup == null) {
                WriteLine($"backup {name} doesn't exist");
                return;
            }

            BackupManager.DeleteBackup(backup);
            WriteLine($"backup {name} was deleted");
        }

        static void __cmd_restore(string[] args) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("name cannot be empty");
                return;
            }

            Backup backup = BackupManager.FindBackup(name);
            if (backup == null) {
                WriteLine($"backup {name} doesn't exist");
                return;
            }

            string targetDir = TryGetElement(args, 1);
            if (IsStringInvalid(targetDir)) {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK) {
                    targetDir = dialog.SelectedPath;
                }
                else {
                    WriteLine("invalid target path");
                    return;
                }
            }

            if (targetDir == "$src")
                targetDir = backup.Source;

            BackupManager.RestoreBackup(backup, targetDir);
            WriteLine($"backup {name} was restored to {targetDir}");
        }

        static void __cmd_help(string[] args) {
            WriteSpaced("Command", 10);
            WriteSpaced("Arguments", 20);
            WriteLine("Description");

            WriteLine(new string('-', BufferWidth));

            WriteSpaced("create", 10);
            WriteSpaced("<name> [<path>]", 20);
            WriteLine("Creates a new backup with name <name> and source path <path>");

            WriteSpaced("update", 10);
            WriteSpaced("<name>", 20);
            WriteLine("Updates all files of backup with name <name>");

            WriteSpaced("restore", 10);
            WriteSpaced("<name> <target>", 20);
            WriteLine("Restores a backup with name <name> to dir <target>");

            WriteSpaced("delete", 10);
            WriteSpaced("<name>", 20);
            WriteLine("Deletes a backup with name <name>");

            WriteSpaced("list", 10);
            WriteSpaced("none", 20);
            WriteLine("Lists all available backups");

            WriteSpaced("help", 10);
            WriteSpaced("none", 20);
            WriteLine("Display this help message");

            WriteSpaced("exit", 10);
            WriteSpaced("none", 20);
            WriteLine("Exits the program");
        }
    }
}
