using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RadShadowMan
{
    class Program
    {
        static readonly string kvMdlpattern = "[\"']model[\"']?(?<path>[\"'].+[\"'])";
        static readonly string kvVMFpattern = "[\"']file[\"']\\s+[\"'](?<path>.+)[\"']";

        static void ProcessReferncedVMFs(ref List<string> Files, string relativePath, string fileContents)
        {
            Regex rx = new Regex(kvVMFpattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            MatchCollection matches = rx.Matches(fileContents);
            if (matches.Count == 0)
                return;

            foreach (Match match in matches)
            {
                GroupCollection groups = match.Groups;
                string path = groups["path"]?.Value;
                if (string.IsNullOrWhiteSpace(path) || Files.Contains(relativePath + "/" + path + ".vmf"))
                    continue;

                Files.Add(relativePath + "/" + path + ".vmf");
            }
        }

        static void GetModels(string fileContents, ref List<string> Models)
        {
            Regex rx = new Regex(kvMdlpattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            MatchCollection matches = rx.Matches(fileContents);

            foreach (Match match in matches)
            {
                GroupCollection groups = match.Groups;
                string path = groups["path"].Value?.Replace("\"", "")?.Replace("'", "")?.Replace("models/", "")?.Trim();
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (!Models.Contains(path))
                    Models.Add(path);               
            }
        }

        static string GetFileContents(string file)
        {
            try
            {
                return File.ReadAllText(file);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ResetColor();
                return null;
            }
        }

        static void ProcessFile(string file)
        {
            string radFolder = Path.GetDirectoryName(file);
            string radFile = radFolder + "/" + Path.GetFileNameWithoutExtension(file) + ".rad";
            string fileContents = GetFileContents(file);
            if (fileContents == null)
                return;

            List<string> Files = new List<string>();
            Files.Add(file);

            ProcessReferncedVMFs(ref Files, radFolder, fileContents);
  
            List<string> Models = new List<string>();
            foreach(string fn in Files)
            {
                string contents = GetFileContents(fn);
                if (string.IsNullOrEmpty(contents))
                    continue;

                GetModels(contents, ref Models);
            }

            if(Models.Count > 0)
            {
                string kv = "forcetextureshadow " + Models.Aggregate((i, j) => i + "\nforcetextureshadow " + j);
                File.WriteAllText(radFile, kv);

                Console.WriteLine($"{file} contained {Models.Count} models.");
                Console.WriteLine($"{Files.Count} files were refenced and loaded.");
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"No models were found.");
                Console.ResetColor();
            }
        }

        static void RecurseCheck(string path)
        {
            foreach (string fn in Directory.GetFiles(path))
                if (fn.Substring(fn.Length - 3) == "vmf")
                    ProcessFile(fn);

            foreach (string dir in Directory.GetDirectories(path))
                RecurseCheck(dir);
        }

        static void Main(string[] args)
        {

#if DEBUG
            ProcessFile("C:/Users/Scott/Desktop/mapsource/rp_liberator_sup_b7/rp_liberator_sup_b7c.vmf");
#else
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("No argument given. Drag files or folders on this exe. or pass file/folder paths as arguments");
                return;
            }

            foreach (string path in args)
            {
                string p = path.Replace("\"", "").Trim();
                if (p.Substring(p.Length - 3) == "vmf")
                    ProcessFile(p);
                else if (File.Exists(p + ".vmf"))
                    ProcessFile(p + ".vmf");
                else
                    RecurseCheck(p);
            }
#endif

            Console.WriteLine("Rad files completed");

#if DEBUG
            Console.ReadLine();
#endif

        }
    }
}