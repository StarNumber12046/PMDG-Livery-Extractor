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

## Installation into MSFS
After the tool successfully extracts your package, a folder with the same name as the `.ptp` file will be created. Simply move or copy this folder directly into your MSFS `Community` folder.

## Build Instructions
To compile the project from source, ensure you have the .NET SDK installed.

```cmd
dotnet build -c Release
```
The compiled tool will be located in `bin\Release\net472\`.

## Technical Details
This tool was built by reverse-engineering the SmartAssembly-obfuscated PMDG Operations Center. The application encrypts its cabinet packages using a hardcoded AES key (`PMDG_SecurityCode`). By leveraging the bundled `CabLib.dll` (a mixed-mode C++/CLI assembly) and supplying the recovered key, the cabinet extraction routine works transparently.
