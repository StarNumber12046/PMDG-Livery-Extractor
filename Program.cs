using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CabLib;

namespace PtpExtractor
{
    class Program
    {
        const string DecryptionKey = "PMDG_SecurityCode";

        public class LayoutEntry
        {
            [JsonPropertyName("path")]
            public string Path { get; set; } = string.Empty;

            [JsonPropertyName("size")]
            public long Size { get; set; }

            [JsonPropertyName("date")]
            public long Date { get; set; }
        }

        public class LayoutRoot
        {
            [JsonPropertyName("content")]
            public List<LayoutEntry> Content { get; set; } = new List<LayoutEntry>();
        }

        static int Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=========================================");
            Console.WriteLine("    PMDG .ptp Offline Livery Extractor   ");
            Console.WriteLine("=========================================\n");
            Console.ResetColor();

            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  PtpExtractor.exe <file.ptp> [output_dir]");
                Console.WriteLine("  PtpExtractor.exe --p3d \"C:\\Path\\To\\SimObjects\\PMDG Plane\" <file.ptp>");
                Console.WriteLine("\nAlternatively, drag and drop a .ptp file onto the batch script.");
                return 1;
            }

            string ptpPath = null;
            string outDir = null;
            bool forceP3d = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--p3d" && i + 1 < args.Length)
                {
                    forceP3d = true;
                    outDir = Path.GetFullPath(args[++i]);
                }
                else if (ptpPath == null)
                {
                    ptpPath = Path.GetFullPath(args[i]);
                }
                else if (outDir == null && !forceP3d)
                {
                    outDir = Path.GetFullPath(args[i]);
                }
            }

            if (ptpPath == null || !File.Exists(ptpPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[Error] File not found: {ptpPath}");
                Console.ResetColor();
                return 1;
            }

            outDir = outDir ?? Path.Combine(Path.GetDirectoryName(ptpPath)!, Path.GetFileNameWithoutExtension(ptpPath));

            Console.WriteLine($"[+] Target File : {ptpPath}");
            Console.WriteLine($"[+] Destination : {outDir}\n");

            try
            {
                bool isP3DInstall = forceP3d || File.Exists(Path.Combine(outDir, "aircraft.cfg"));

                if (isP3DInstall)
                {
                    Console.WriteLine("[+] Automated P3D/FSX Installation detected.");
                    InstallToP3D(ptpPath, outDir);
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n[+] Livery fully installed into P3D/FSX successfully!");
                    Console.ResetColor();
                    return 0;
                }

                // Standard extraction (and MSFS layout.json merging)
                List<string> extractedFiles = ExtractCabinet(ptpPath, outDir);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[+] Extraction completed successfully!");
                Console.ResetColor();

                UpdateLayoutJson(outDir, extractedFiles);
                
                Console.WriteLine($"\nYou can now copy the extracted folder into your MSFS 'Community' folder:\n{outDir}");
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"\n[Error] Extraction failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static void InstallToP3D(string ptpPath, string p3dAircraftDir)
        {
            if (!File.Exists(Path.Combine(p3dAircraftDir, "aircraft.cfg")))
            {
                throw new Exception($"aircraft.cfg not found in target directory: {p3dAircraftDir}\nPlease ensure you selected the root of the aircraft's SimObjects folder.");
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "PtpExtractor_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                ExtractCabinet(ptpPath, tempDir, silent: true);

                // Move Texture folders
                var dirs = Directory.GetDirectories(tempDir, "Texture.*");
                if (dirs.Length == 0)
                {
                    throw new Exception("No 'Texture.*' folder found in the .ptp package.");
                }

                foreach (var d in dirs)
                {
                    string dest = Path.Combine(p3dAircraftDir, Path.GetFileName(d));
                    if (Directory.Exists(dest)) Directory.Delete(dest, true);
                    Directory.Move(d, dest);
                    Console.WriteLine($"    -> Installed texture directory: {Path.GetFileName(d)}");
                }

                // Find livery configuration text
                var configFiles = Directory.GetFiles(tempDir, "*.ini").Concat(Directory.GetFiles(tempDir, "*.cfg"));
                string liveryConfigText = null;
                foreach (var f in configFiles)
                {
                    string text = File.ReadAllText(f);
                    if (text.Contains("[fltsim.", StringComparison.OrdinalIgnoreCase))
                    {
                        liveryConfigText = text;
                        break;
                    }
                }

                if (liveryConfigText == null)
                {
                    throw new Exception("Could not find Aircraft.ini or a [fltsim.x] block in the extracted files.");
                }

                // Extract just the fltsim block using Regex
                var match = Regex.Match(liveryConfigText, @"(?is)(\[fltsim\.[^\]]*\].*?)(?=\n\[|$)");
                if (!match.Success)
                {
                    throw new Exception("Failed to parse the [fltsim.x] block from the livery config.");
                }
                string fltSimBlock = match.Groups[1].Value.Trim();

                // Update aircraft.cfg
                string aircraftCfgPath = Path.Combine(p3dAircraftDir, "aircraft.cfg");
                string aircraftCfgText = File.ReadAllText(aircraftCfgPath);

                // Find max existing fltsim index
                int maxIndex = -1;
                var existingMatches = Regex.Matches(aircraftCfgText, @"(?i)\[fltsim\.(\d+)\]");
                foreach (Match m in existingMatches)
                {
                    if (int.TryParse(m.Groups[1].Value, out int idx) && idx > maxIndex)
                    {
                        maxIndex = idx;
                    }
                }

                int nextIndex = maxIndex + 1;

                // Replace the header with the correct consecutive number
                fltSimBlock = Regex.Replace(fltSimBlock, @"(?i)\[fltsim\.[^\]]*\]", $"[fltsim.{nextIndex}]");

                // Ensure it ends with a newline
                string newContent = aircraftCfgText.TrimEnd() + "\r\n\r\n" + fltSimBlock + "\r\n";
                File.WriteAllText(aircraftCfgPath, newContent);

                Console.WriteLine($"    -> Appended livery to aircraft.cfg securely as [fltsim.{nextIndex}]");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        static List<string> ExtractCabinet(string ptpPath, string outDir, bool silent = false)
        {
            Directory.CreateDirectory(outDir);

            var extract = new Extract();
            extract.SetDecryptionKey(Encoding.ASCII.GetBytes(DecryptionKey));

            var headerInfo = new Extract.kHeaderInfo();
            if (!extract.IsFileCabinet(ptpPath, out headerInfo))
            {
                throw new Exception("Not a valid .ptp cabinet file (wrong decryption key or corrupt file).");
            }

            if (!silent) Console.WriteLine($"[+] Cabinet verified. Contains {headerInfo.u16_Files} file(s). Extracting...\n");

            List<string> extractedFiles = new List<string>();

            extract.evBeforeCopyFile += (Extract.kCabinetFileInfo info) =>
            {
                if (!silent) Console.WriteLine($"    -> {info.s_RelPath}");
                extractedFiles.Add(info.s_RelPath);
                return true;
            };

            extract.ExtractFile(ptpPath, outDir);
            return extractedFiles;
        }

        static void UpdateLayoutJson(string baseDir, List<string> extractedFiles)
        {
            string layoutPath = Path.Combine(baseDir, "layout.json");
            LayoutRoot layout;

            if (File.Exists(layoutPath))
            {
                Console.WriteLine($"\n[+] Found existing layout.json. Merging new files...");
                string json = File.ReadAllText(layoutPath);
                try
                {
                    layout = JsonSerializer.Deserialize<LayoutRoot>(json) ?? new LayoutRoot();
                }
                catch
                {
                    Console.WriteLine("    [!] Failed to parse layout.json. Overwriting.");
                    layout = new LayoutRoot();
                }
            }
            else
            {
                Console.WriteLine($"\n[+] No layout.json found. Creating a new one...");
                layout = new LayoutRoot();
            }

            var newFileSet = new HashSet<string>(extractedFiles.Select(f => f.Replace('\\', '/').ToLowerInvariant()));
            layout.Content.RemoveAll(entry => newFileSet.Contains(entry.Path.ToLowerInvariant()));

            foreach (var relPath in extractedFiles)
            {
                string fullPath = Path.Combine(baseDir, relPath);
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    layout.Content.Add(new LayoutEntry
                    {
                        Path = relPath.Replace('\\', '/'),
                        Size = fileInfo.Length,
                        Date = fileInfo.LastWriteTimeUtc.ToFileTimeUtc()
                    });
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string newJson = JsonSerializer.Serialize(layout, options);
            File.WriteAllText(layoutPath, newJson);
            
            Console.WriteLine($"[+] layout.json updated with {extractedFiles.Count} entries.");
        }
    }
}
