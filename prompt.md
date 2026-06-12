# AI Context Prompt For OpenAs

You are working on OpenAs, a C#/.NET 8 WPF Windows desktop utility.

## Product Goal

OpenAs lets a Windows user map a custom file extension to a real file type.

Example:

```text
.gablec -> PDF document
.finger -> JPEG image
```

The original file content is not converted. The extension is mapped through per-user Windows file associations, and OpenAs opens the file as the selected real type.

## Safety Rules

- Do not write to `HKEY_LOCAL_MACHINE`.
- Use only `HKEY_CURRENT_USER\Software\Classes` and `HKEY_CURRENT_USER\Software\OpenAs`.
- Do not require administrator rights.
- Keep blocking executable/system-sensitive extensions in `ExtensionRules`.
- Do not remove or overwrite registry keys unless they were created by OpenAs.
- Keep the "Reset all mappings" behavior scoped to OpenAs-managed mappings.

## Architecture

Important files:

```text
OpenAs/App.xaml.cs
OpenAs/MainWindow.xaml
OpenAs/MainWindow.xaml.cs
OpenAs/Models/
OpenAs/Services/
```

Main services:

```text
AssociationRegistryService
  Saves/removes per-user file associations and OpenAs metadata.

InstalledFileFormatService
  Reads installed Windows file types and adds them to the "Open as" dropdown.

FileSignatureService
  Checks magic bytes for known formats.

FileTypeDetectionService
  Detects a supported real type from a selected test file.

FileOpenService
  Handles command-line file opens, creates a temporary typed copy, and launches it.

FileTypeIconService
  Reads effective Windows icons for real file types.

IconStorageService
  Copies user-selected .ico files into AppData.

ShellNotificationService
  Notifies Windows Explorer when associations change.
```

## Runtime Flow

Normal app launch:

```text
App.xaml.cs -> MainWindow
```

Windows file open launch:

```text
OpenAs.exe --open "%1" --format <format-id>
App.xaml.cs -> FileOpenRequest -> FileOpenService
```

## UI Guidelines

- Keep the UI in English.
- Keep it as a practical Windows utility, not a landing page.
- Keep the content centered with a max width so fullscreen does not stretch the app across ultrawide screens.
- Preserve the current WPF-only approach; do not add web frameworks.
- Keep the mappings table user-facing. Do not expose raw registry details in the main table.

## Build

Use:

```bat
dotnet build OpenAs\OpenAs.csproj
```

If the app is currently running, build may fail because `OpenAs.exe` or `OpenAs.dll` is locked. Stop debugging or close the app before rebuilding.

For verification while the app is running, use a separate output path:

```bat
dotnet build OpenAs\OpenAs.csproj -p:UseAppHost=false -p:OutputPath=..\_verify_bin\
```

## Known Design Decisions

- Built-in formats have signature checks.
- Additional installed Windows file types may not have signature checks, so they are opened by temporary typed copy only.
- Custom icons are copied to `%AppData%\OpenAs\Icons`.
- Default icons are resolved from Windows' effective file association data.
