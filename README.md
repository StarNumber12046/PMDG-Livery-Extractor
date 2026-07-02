# PMDG .ptp Livery Extractor

A standalone, offline CLI tool to extract PMDG Operations Center `.ptp` livery packages. 

PMDG liveries are packaged as encrypted Microsoft Cabinet files utilizing a custom `CRYP` header and AES encryption via `CabLib`. Since PMDG servers are permanently offline, the official Operations Center cannot be used to install new liveries. This tool directly decrypts and extracts the packages to your disk so you can manually place them in your Microsoft Flight Simulator (MSFS) `Community` folder.

## Features
- Bypasses the need for the PMDG Operations Center or an active internet connection.
- Extremely fast native extraction using `CabLib.dll` (FDI API).
- Automatically preserves the exact `Community` folder structure required by MSFS.

## Requirements
- Windows (x64)
- .NET Framework 4.7.2 or later

## Usage

### Quick Extract (Drag and Drop)
Simply drag and drop any `.ptp` file onto `Extract-Livery.bat`. It will automatically decrypt the file and create a new folder right next to your `.ptp` file containing the ready-to-use livery.

### Command Line
You can also use the extractor directly via the command line:
```cmd
PtpExtractor.exe "C:\path\to\your_livery.ptp" [optional_output_directory]
```

## Manual Installation Instructions

Because you are no longer using the PMDG Operations Center, you must manually place the extracted files into your simulator. The `.ptp` format is universal across all PMDG products, but the installation method depends on your simulator.

### Microsoft Flight Simulator (MSFS)

PMDG liveries for MSFS are typically installed into a master "liveries" package inside your Community folder. 

1. **Locate your liveries package**: Find the PMDG liveries folder in your MSFS `Community` directory (e.g., `Community\pmdg-aircraft-738-liveries`).
2. **Extract directly to it**: Run the extractor via the command line and set the output directory to that livery package folder:
   ```cmd
   PtpExtractor.exe "C:\path\to\livery.ptp" "C:\path\to\Community\pmdg-aircraft-738-liveries"
   ```
   *Note: If you use the Drag & Drop script, you can simply move the contents of the extracted folder into your PMDG liveries package folder.*
3. **Automatic layout.json update**: The `PtpExtractor` will automatically detect the `layout.json` file in the destination directory and seamlessly merge the new texture files into it. No manual JSON editing or third-party layout generators are required!

### Prepar3D (P3D) and Flight Simulator X (FSX)

The tool features **fully automated** legacy installation for P3D and FSX. It will automatically extract the textures, place them in the correct folder, and seamlessly append the new livery to your `aircraft.cfg` with the correct `[fltsim.X]` numbering!

To trigger the automated installer, either:
1. Provide the root of your PMDG aircraft's `SimObjects` folder as the output directory:
   ```cmd
   PtpExtractor.exe "C:\path\to\livery.ptp" "C:\Program Files\Lockheed Martin\Prepar3D v4\SimObjects\Airplanes\PMDG 737-800NGXu"
   ```
2. Or use the explicit `--p3d` flag:
   ```cmd
   PtpExtractor.exe --p3d "C:\Program Files\Lockheed Martin\Prepar3D v4\SimObjects\Airplanes\PMDG 737-800NGXu" "C:\path\to\livery.ptp" 
   ```

If you prefer to extract the files locally and install them manually:
1. **Extract**: Use the drag-and-drop script to extract the `.ptp` file locally.
2. **Move Textures**: Copy the extracted `Texture.XYZ` folder into your specific PMDG aircraft directory.
3. **Update `aircraft.cfg`**: 
   - Open the extracted `Aircraft.ini` (or `Config.cfg`). Copy the block of text starting with `[fltsim.x]`.
   - Open your aircraft's `aircraft.cfg` file.
   - Paste the block at the bottom of the other liveries.
   - **Crucial**: Change the `[fltsim.x]` header to the next sequential number (e.g., change it to `[fltsim.5]` if the last one was 4).
   
## Build Instructions
To compile the project from source, ensure you have the .NET SDK installed.

```cmd
dotnet build -c Release
```
The compiled tool will be located in `bin\Release\net472\`.

## Technical Details
This tool was built by reverse-engineering the SmartAssembly-obfuscated PMDG Operations Center. The application encrypts its cabinet packages using a hardcoded AES key (`PMDG_SecurityCode`). By leveraging the bundled `CabLib.dll` (a mixed-mode C++/CLI assembly) and supplying the recovered key, the cabinet extraction routine works transparently.
