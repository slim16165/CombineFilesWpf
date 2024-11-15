```markdown
# TreeView File Explorer

![License](https://img.shields.io/badge/license-MIT-blue.svg)

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technologies Used](#technologies-used)
- [Installation](#installation)
- [Usage](#usage)
- [Architecture](#architecture)
  - [Functional Components](#functional-components)
  - [Technical Components](#technical-components)
- [Dependency Injection](#dependency-injection)
- [Error Handling](#error-handling)
- [Contributing](#contributing)
- [License](#license)

## Overview

**TreeView File Explorer** is a robust and user-friendly file exploration tool built with WPF (Windows Presentation Foundation). It leverages the MVVM (Model-View-ViewModel) design pattern to provide a responsive and maintainable application. The explorer offers functionalities such as browsing directories, managing files, applying regex filters, and handling drag-and-drop operations with ease.

## Features

- **Hierarchical File System Navigation**: Browse through drives, folders, and files using a TreeView interface.
- **Asynchronous Operations**: Load directories and files asynchronously to ensure a smooth user experience.
- **Icon Management**: Retrieve and cache system icons for files and directories, supporting both small and large icon sizes.
- **Filtering**: Apply regex-based filters to display specific files and folders.
- **Hidden Files Toggle**: Show or hide hidden files and directories.
- **Drag-and-Drop Support**: Move or copy files and directories within the explorer using drag-and-drop.
- **Context Menus**: Perform actions like Open, Delete, Rename, Copy, and Move via context menus.
- **Event Aggregation**: Utilize an event aggregator for decoupled communication between components.
- **Responsive UI**: Implements virtualization and deferred scrolling for handling large file systems efficiently.
- **Error Handling**: Robust exception handling to manage file system access issues and other runtime errors.

## Technologies Used

- **.NET Framework 4.7.2** or later
- **WPF (Windows Presentation Foundation)**
- **MVVM (Model-View-ViewModel) Pattern**
- **Telerik UI for WPF** (for enhanced UI components)
- **C#**
- **Dependency Injection**
- **Async/Await** for asynchronous programming

## Installation

### Prerequisites

- **.NET Framework 4.7.2** or later
- **Visual Studio 2019** or later
- **Telerik UI for WPF** (ensure you have the necessary licenses)

### Steps

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/TreeViewFileExplorer.git
   ```
2. **Open the Solution**
   - Navigate to the cloned directory and open `TreeViewFileExplorer.sln` with Visual Studio.

3. **Restore NuGet Packages**
   - In Visual Studio, go to `Tools` > `NuGet Package Manager` > `Manage NuGet Packages for Solution...` and restore all packages.

4. **Build the Solution**
   - Press `Ctrl + Shift + B` or navigate to `Build` > `Build Solution`.

5. **Run the Application**
   - Press `F5` or click the `Start` button to launch the application.

## Usage

Upon launching the **TreeView File Explorer**, you will be greeted with a toolbar and a TreeView interface displaying your system's drives.

### Navigating Directories

- **Expand/Collapse**: Click on the arrow next to a drive or folder to expand or collapse its contents.
- **Lazy Loading**: Directories and files are loaded asynchronously when you expand a node to ensure performance.

### Managing Files and Folders

- **Context Menus**: Right-click on any file or folder to access context menu options such as Open, Delete, Rename, Copy, and Move.
- **Drag-and-Drop**: Drag files or folders and drop them into another directory to move or copy them.

### Filtering and Viewing Options

- **Toggle Hidden Files**: Click the eye icon (`👁️`) in the toolbar to show or hide hidden files and folders.
- **Apply Regex Filter**: Click the search icon (`🔍`) to open a dialog where you can input a regex pattern to filter displayed items.
- **Navigate to Folder**: Click the folder icon (`📂`) to open a dialog and navigate directly to a specific folder.

### Loading Indicator

- A busy indicator (`RadBusyIndicator`) displays during long-running operations such as loading directories or applying filters.

## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern to ensure a clear separation of concerns, making the codebase maintainable and testable.

### Functional Components

- **View (XAML)**: Defines the UI elements, data bindings, and styles.
- **ViewModel**: Handles the presentation logic, commands, and interacts with services.
- **Model**: Represents the data structures, such as `FileItem`.

### Technical Components

- **Services**
  - **IFileSystemService**: Interface for file system operations.
  - **FileSystemService**: Implementation of `IFileSystemService` handling asynchronous file and directory retrieval.
  - **IIconService**: Interface for retrieving system icons.
  - **IconService**: Implementation that interacts with the Windows Shell to fetch icons and manage caching.
  - **EventAggregator**: Facilitates decoupled communication between components.

- **ViewModels**
  - **TreeViewExplorerViewModel**: Main ViewModel managing root items, selected files, filters, and commands.
  - **DirectoryViewModel**: Represents a directory in the TreeView, handling expansion and loading of child items.
  - **FileViewModel**: Represents a file in the TreeView.
  - **BaseFileSystemObjectViewModel**: Abstract base class providing common functionalities for `DirectoryViewModel` and `FileViewModel`.

- **Converters**
  - **BooleanToVisibilityConverter**: Converts boolean values to `Visibility` enums for UI binding.

- **Commands**
  - **RelayCommand**: Implements `ICommand` to relay execution and can-execute logic to methods in ViewModels.

### Diagram

```
+---------------------+       +--------------------+
|       View          | <---> |    ViewModel       |
+---------------------+       +--------------------+
           |                              |
           |                              |
           v                              v
+---------------------+       +--------------------+
|      Services       | <---- |     Models         |
+---------------------+       +--------------------+
```

## Dependency Injection

The application utilizes **Dependency Injection (DI)** to manage dependencies between components, promoting loose coupling and easier testing.

### Current Setup

- **Manual Injection**: Dependencies are injected manually in the constructors. For instance, `TreeViewExplorerViewModel` receives instances of `IIconService`, `IFileSystemService`, and `IEventAggregator` through its constructor.

### Resolving Dependency Issues

A common issue encountered was a **circular dependency** between `FileSystemService` and `TreeViewExplorerViewModel`, leading to `NullReferenceException`. This was addressed by:

1. **Eliminating Circular Dependencies**: Removed the dependency of `FileSystemService` on `TreeViewExplorerViewModel`.
2. **Passing Required Parameters Directly**: Methods in `FileSystemService` now accept necessary parameters (e.g., `showHiddenFiles`, `filterRegex`) instead of accessing them from the ViewModel.
3. **Proper Initialization**: Ensured that services are initialized correctly without relying on ViewModel instances.

### Recommendations

For enhanced DI management, consider integrating a DI container such as **Microsoft.Extensions.DependencyInjection**, **Autofac**, or **Unity**. This would automate the injection process and further prevent circular dependencies.

## Error Handling

Robust error handling mechanisms are in place to manage runtime exceptions, especially those related to file system access:

- **Try-Catch Blocks**: Surround critical file system operations with try-catch blocks to handle exceptions gracefully.
- **User Feedback**: Display informative messages to the user in case of errors, such as access denied or invalid operations.
- **Logging (Planned)**: Integrate a logging framework (e.g., NLog, log4net) to log errors for debugging and maintenance purposes.

### Example

```csharp
try
{
    // Attempt to move a file
    System.IO.File.Move(sourcePath, destinationPath);
    MessageBox.Show($"Moved {Name} to {destinationPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
}
catch (Exception ex)
{
    MessageBox.Show($"Error moving {Name}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
```

## Contributing

Contributions are welcome! Please follow these steps to contribute:

1. **Fork the Repository**
2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/YourFeatureName
   ```
3. **Commit Your Changes**
   ```bash
   git commit -m "Add your message"
   ```
4. **Push to the Branch**
   ```bash
   git push origin feature/YourFeatureName
   ```
5. **Open a Pull Request**

Please ensure your code adheres to the project's coding standards and includes necessary documentation.

## License

This project is licensed under the [MIT License](LICENSE).

---

*Developed with ❤️ by [Your Name](https://github.com/yourusername)*

```