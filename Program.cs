using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using CabLib;

namespace PtpExtractor
{
    class Program
    {
        const string DecryptionKey = "PMDG_SecurityCode";

        // Layout.json data structures
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
                Console.WriteLine("\nAlternatively, drag and drop a .ptp file onto the extractor.");
                return 1;
            }

            string ptpPath = Path.GetFullPath(args[0]);
            if (!File.Exists(ptpPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[Error] File not found: {ptpPath}");
                Console.ResetColor();
                return 1;
            }

            string outDir = args.Length >= 2
                ? Path.GetFullPath(args[1])
                : Path.Combine(Path.GetDirectoryName(ptpPath)!, Path.GetFileNameWithoutExtension(ptpPath));

            Console.WriteLine($"[+] Target File : {ptpPath}");
            Console.WriteLine($"[+] Destination : {outDir}\n");

            try
            {
                List<string> extractedFiles = ExtractCabinet(ptpPath, outDir);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[+] Extraction completed successfully!");
                Console.ResetColor();

                // Merge with layout.json
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

        static List<string> ExtractCabinet(string ptpPath, string outDir)
        {
            Directory.CreateDirectory(outDir);

            var extract = new Extract();
            extract.SetDecryptionKey(Encoding.ASCII.GetBytes(DecryptionKey));

            var headerInfo = new Extract.kHeaderInfo();
            if (!extract.IsFileCabinet(ptpPath, out headerInfo))
            {
                throw new Exception("Not a valid .ptp cabinet file (wrong decryption key or corrupt file).");
            }

            Console.WriteLine($"[+] Cabinet verified. Contains {headerInfo.u16_Files} file(s). Extracting...\n");

            List<string> extractedFiles = new List<string>();

            extract.evBeforeCopyFile += (Extract.kCabinetFileInfo info) =>
            {
                Console.WriteLine($"    -> {info.s_RelPath}");
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

            // Initialize LayoutRoot
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

            // Remove previous entries for the files we just extracted, to avoid duplicates
            var newFileSet = new HashSet<string>(extractedFiles.Select(f => f.Replace('\\', '/').ToLowerInvariant()));
            layout.Content.RemoveAll(entry => newFileSet.Contains(entry.Path.ToLowerInvariant()));

            // Add the new entries
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

            // Write back to disk
            var options = new JsonSerializerOptions { WriteIndented = true };
            string newJson = JsonSerializer.Serialize(layout, options);
            File.WriteAllText(layoutPath, newJson);
            
            Console.WriteLine($"[+] layout.json updated with {extractedFiles.Count} entries.");
        }
    }
}
