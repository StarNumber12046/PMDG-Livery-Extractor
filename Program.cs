using System;
using System.IO;
using System.Text;
using CabLib;

namespace PtpExtractor
{
    class Program
    {
        // The hardcoded AES encryption key found inside PMDG Operations Center
        const string DecryptionKey = "PMDG_SecurityCode";

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

            // Default to extracting to a folder with the same name as the .ptp file in the same directory
            string outDir = args.Length >= 2
                ? Path.GetFullPath(args[1])
                : Path.Combine(Path.GetDirectoryName(ptpPath)!, Path.GetFileNameWithoutExtension(ptpPath));

            Console.WriteLine($"[+] Target File : {ptpPath}");
            Console.WriteLine($"[+] Destination : {outDir}\n");

            try
            {
                ExtractCabinet(ptpPath, outDir);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[+] Extraction completely successfully!");
                Console.ResetColor();
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

        static void ExtractCabinet(string ptpPath, string outDir)
        {
            Directory.CreateDirectory(outDir);

            var extract = new Extract();
            
            // Provide the secret key to decrypt the CRYP CabLib header
            extract.SetDecryptionKey(Encoding.ASCII.GetBytes(DecryptionKey));

            // Verify it's a valid (encrypted) cabinet
            var headerInfo = new Extract.kHeaderInfo();
            if (!extract.IsFileCabinet(ptpPath, out headerInfo))
            {
                throw new Exception("Not a valid .ptp cabinet file (wrong decryption key or corrupt file).");
            }

            Console.WriteLine($"[+] Cabinet verified. Contains {headerInfo.u16_Files} file(s). Extracting...\n");

            // Hook into the extraction event to log progress
            extract.evBeforeCopyFile += (Extract.kCabinetFileInfo info) =>
            {
                Console.WriteLine($"    -> {info.s_RelPath}");
                return true; // Return false to skip file, true to extract
            };

            // Execute extraction
            extract.ExtractFile(ptpPath, outDir);
        }
    }
}
