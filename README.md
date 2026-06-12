# OpenAs

OpenAs is a Windows desktop utility for mapping custom file extensions to real file types.

Example:

```text
report.gablec -> opens like report.pdf
photo.finger  -> opens like photo.jpg
```

The file content stays the same. OpenAs only teaches Windows how to treat a custom extension.

## What It Does

- Maps a custom extension to a real file type for the current Windows user.
- Uses `HKEY_CURRENT_USER`, so it does not require administrator rights.
- Blocks dangerous executable/system extensions such as `.exe`, `.bat`, `.cmd`, `.ps1`, `.lnk`, `.msi`, and similar.
- Uses the real file type icon by default.
- Allows a custom `.ico` icon per custom extension.
- Detects common file signatures for built-in types such as JPEG, PNG, GIF, WebP, PDF, ZIP, and text.
- Adds extra "Open as" options from file types installed on the user's Windows machine.
- Includes reset/remove controls for mappings created by OpenAs.

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK, if running from source
- Visual Studio 2022 with WPF/.NET desktop workload, if developing in Visual Studio

## Quick Start

Run:

```bat
startup.bat
```

The script checks for .NET 8 SDK. If it is missing and `winget` is available, it tries to install:

```text
Microsoft.DotNet.SDK.8
```

Then it restores, builds, and runs the WPF app.

## Manual Run

```bat
dotnet restore OpenAs\OpenAs.csproj
dotnet build OpenAs\OpenAs.csproj
dotnet run --project OpenAs\OpenAs.csproj
```

## How To Test

1. Open the app.
2. Add a mapping:

   ```text
   .gablec -> PDF document
   ```

3. Copy a real PDF file.
4. Rename the copy:

   ```text
   file.pdf -> file.gablec
   ```

5. Double-click `file.gablec`.

OpenAs creates a temporary copy with the real extension and asks Windows to open it with the normal default app.

## Safety Notes

OpenAs only writes per-user registry keys:

```text
HKEY_CURRENT_USER\Software\Classes
HKEY_CURRENT_USER\Software\OpenAs
```

It does not modify machine-wide `HKEY_LOCAL_MACHINE` associations.

Use **Reset all mappings** in the app to remove every mapping created by OpenAs for the current Windows user.

## Project Structure

```text
OpenAs/
  Models/      Data records used by the UI and services
  Services/    Registry, icon, file detection, and open handling logic
  Assets/      App icon
  App.xaml     Startup and command-line open handling
  MainWindow   WPF UI
```

## Important Implementation Detail

When Windows opens a custom extension, it launches OpenAs like this:

```text
OpenAs.exe --open "%1" --format <format-id>
```

OpenAs validates known file types where signatures are available, creates a temporary typed copy, and opens that copy with `UseShellExecute = true`.
