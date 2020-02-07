using FAT32Lib.Util;
using System;
using System.IO;

namespace FAT32Lib.Example {
    public class TestProgram {
        public static void Exec(string nativeCommand, string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("Usage:\n\t{0} <fatfile> <outdir>", nativeCommand);
                Environment.Exit(33);
            }
            FileDisk fd = null;
            try {
                fd = new FileDisk(args[0], true);
            }
            catch (FileNotFoundException) {
                Console.WriteLine("File not found.");
                Environment.Exit(1);
            }
            IFileSystem fs = null;
            try {
                fs = FileSystemFactory.Create(fd, true);
            }
            catch (IOException e) {
                Console.WriteLine("IO Exception occurred. Details:\n" + e.Message);
                Environment.Exit(2);
            }
            IFsDirectory root = null;
            try {
                root = fs.GetRoot();
            }
            catch (IOException e) {
                Console.WriteLine("IO Exception occurred. Details:\n" + e.Message);
                Environment.Exit(3);
            }
            string t;
            if (args.Length == 2)
                t = args[1];
            else
                t = Path.Combine(args[0], $"{Path.GetFileName(args[0])}_converted");
            if (!Directory.Exists(t) && !Directory.CreateDirectory(t).Exists) {
                Console.WriteLine($"Failed to make target directory at {t}\n");
                Environment.Exit(3);
            }
            try {
                var success = RecurseExtract(t, "", root);
                Console.WriteLine("Extract complete.");
                if (!success)
                    Environment.Exit(8);
            }
            catch (IOException e) {
                Console.WriteLine("IO Exception occurred. Details:\n" + e.Message);
                Environment.Exit(7);
            }
        }
        private static bool RecurseExtract(string targetRoot, string path, IFsDirectory dir) {
            foreach (IFsDirectoryEntry e in dir) {
                if (".".Equals(e.GetName())) continue;
                if (e.IsFile()) {
                    var output = Path.Combine(targetRoot, e.GetName());
                    Console.WriteLine($"Writing {Path.Combine(path, e.GetName())}...");
                    var fsf = e.GetFile();
                    var ms = new MemoryStream((int)fsf.GetLength());
                    ms.SetLength(fsf.GetLength());
                    fsf.Read(0L, ms);
                    var fs = new FileStream(output, FileMode.Create, FileAccess.Write);
                    ms.Position = 0;
                    ms.CopyTo(fs);
                    fs.Close();
                    ms.Close();
                }
                else if (e.IsDirectory()) {
                    if ("..".Equals(e.GetName())) continue;
                    var output = Path.Combine(targetRoot, e.GetName());
                    if (!Directory.Exists(output) && !Directory.CreateDirectory(output).Exists) {
                        Console.WriteLine($"Failed to make directory at {output}\n");
                        return false;
                    }
                    if (!RecurseExtract(output, Path.Combine(path, e.GetName()), e.GetDirectory()))
                        return false;
                }
            }
            return true;
        }
    }
}
