# CombineFilesWpf

## Introduction

CombineFilesWpf is a WPF application designed to combine multiple files into a single file. It provides a user-friendly interface to select files, apply filters, and merge them efficiently. The application supports various file formats and offers customization options for the output file.

## Key Features

- Combine multiple files into a single file
- Support for various file formats
- Apply filters to include or exclude specific files
- Customize output file name and location
- Option to overwrite existing files
- Save and load configuration settings
- Progress indicator during the merging process

## Installation and Usage

### Prerequisites

- .NET Framework 4.7.2 or later
- Visual Studio 2019 or later

### Steps

1. **Clone the Repository**
   ```bash
   git clone https://github.com/slim16165/CombineFilesWpf.git
   ```
2. **Open the Solution**
   - Navigate to the cloned directory and open `CombineFilesWpf.sln` with Visual Studio.

3. **Restore NuGet Packages**
   - In Visual Studio, go to `Tools` > `NuGet Package Manager` > `Manage NuGet Packages for Solution...` and restore all packages.

4. **Build the Solution**
   - Press `Ctrl + Shift + B` or navigate to `Build` > `Build Solution`.

5. **Run the Application**
   - Press `F5` or click the `Start` button to launch the application.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Combine-Files.ps1 Script

The `Combine-Files.ps1` script is a PowerShell script designed to combine the contents of multiple files or print their names based on specified options. It supports predefined presets for common configurations and allows for customization through various parameters.

### Parameters

- **Preset**: Name of the preset to use (e.g., 'CSharp').
- **ListPresets**: List available presets.
- **Mode**: Mode of file selection ('list', 'extensions', 'regex').
- **FileList**: Array of file names to combine (used when mode is 'list').
- **Extensions**: Array of file extensions to include (used when mode is 'extensions').
- **RegexPatterns**: Array of regex patterns to select files (used when mode is 'regex').
- **OutputFile**: Path of the output file (default: 'CombinedFile.txt').
- **Recurse**: Indicates whether to search subdirectories.
- **FileNamesOnly**: Indicates whether to print only file names instead of content.
- **OutputToConsole**: Indicates whether to print to console instead of creating a file.
- **ExcludePaths**: Array of file or directory paths to exclude.
- **OutputEncoding**: Encoding of the output file (default: UTF8).
- **OutputFormat**: Format of the output file ('txt', 'csv', 'json').
- **MinDate**: Minimum date for files to include.
- **MaxDate**: Maximum date for files to include.
- **MinSize**: Minimum size of files (e.g., '1MB').
- **MaxSize**: Maximum size of files (e.g., '10MB').

### Example Usage

```powershell
.\Combine-Files.ps1 -Preset 'CSharp'
```

Combines all files with the .cs and .xaml extensions in the current and subdirectories, excluding 'Properties', 'obj', and 'bin' directories, and saves the result in 'CombinedFile.cs'.

## TreeViewFileExplorer

The `TreeViewFileExplorer` component is part of this project and provides a robust and user-friendly file exploration tool built with WPF. It leverages the MVVM design pattern to provide a responsive and maintainable application. For more details, see the [TreeViewFileExplorer README](ExternalLibraries/TreeViewFileExplorer/README.md).
