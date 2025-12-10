# CombineFiles

A lightweight .NET tool and library for merging multiple text files into a single output.  
Use it as a **CLI application** for quick “merge files” tasks, or as a **.NET472 library** (`CombineFiles.Core`) in your own projects for more fine-grained control (deduplication by hash, per-file line limits, global token budgets, etc.).

---

## Table of Contents

1. [Features](#features)  
2. [Getting Started](#getting-started)  
   - [NuGet Package (CombineFiles.Core)](#nuget-package-combinefilescore)  
   - [Clone & Build from Source](#clone--build-from-source)  
   - [Install CLI as .NET Tool (optional)](#install-cli-as-net-tool-optional)  
3. [CLI Usage](#cli-usage)  
   - [Basic Merge](#basic-merge)  
   - [Deduplication and Line Limits](#deduplication-and-line-limits)  
   - [Interactive Mode](#interactive-mode)  
   - [Help & Options](#help--options)  
4. [Library Usage (`CombineFiles.Core`)](#library-usage-combinefilescore)  
   - [API Overview](#api-overview)  
   - [Example: Merge in Code](#example-merge-in-code)  
5. [Configuration & Extensions](#configuration--extensions)  
   - [Filtering Hidden Files](#filtering-hidden-files)  
   - [Custom Token Budget Logic](#custom-token-budget-logic)  
6. [Contributing](#contributing)  
7. [License](#license)  
8. [Contact](#contact)  

---

## Features

- **Concatenate multiple text files** into a single output file.  
- **Deduplication by hash**: omit duplicate files based on content.  
- **Per-file line limits**: stop reading a file once a certain number of lines is reached.  
- **Global token budget**: track "tokens" (e.g., line count or custom metric) across all input files, and stop merging when budget is exhausted.  
- **Text compaction for LLM optimization**: reduce token usage by converting 4 spaces to tabs and removing excessive empty lines.  
- **Interactive CLI wizard** (via Spectre.Console) to guide through merge options.  
- **Cross-platform**: runs on Windows, macOS, and Linux via .NET 6+/.NET 8+.  
- **Library-friendly**: reference `CombineFiles.Core` in your own .NET projects.  

---

## Getting Started

Below are various ways to start using CombineFiles—either as a library or as a command-line tool.

### NuGet Package (`CombineFiles.Core`)

To install the core library into your .NET project:

```bash
dotnet add package CombineFiles.Core --version 1.3.0
````

> This package contains all merging logic (classes like `FileMerger`, helpers, and models). It does **not** include the console/UI layer.

### Clone & Build from Source

1. **Clone the repository** (assuming the remote is `https://github.com/slim16165/CombineFiles.git`):

   ```bash
   git clone https://github.com/slim16165/CombineFiles.git
   cd CombineFiles
   ```

2. **Restore and build** the solution (using .NET 8.0 SDK or later):

   ```bash
   dotnet build CombineFiles.sln -c Release
   ```

3. **Run unit tests** (optional):

   ```bash
   dotnet test CombineFiles.Tests/CombineFiles.Tests.csproj -c Release
   ```

After a successful build, you will have:

* **`CombineFiles.Core.dll`** in `CombineFiles.Core/bin/Release/netstandard2.0/` (or `net6.0`, depending on your target).
* **CLI executable** (e.g., `CombineFiles.exe` on Windows, `CombineFiles` on Linux/macOS) under `CombineFiles.CLI/bin/Release/net6.0/` (or `net7.0/net8.0`, depending on the project’s TFM).

### Install CLI as .NET Tool (Optional)

If you want to use the CLI globally:

1. Navigate to the CLI project folder and pack it as a NuGet package (if you haven’t already created one):

   ```bash
   cd CombineFiles.CLI
   dotnet pack -c Release
   ```

   You’ll get a `.nupkg` file in `CombineFiles.CLI/bin/Release`.

2. Install the CLI as a global tool locally (using the generated `.nupkg`):

   ```bash
   dotnet tool install --global --add-source ./bin/Release CombineFiles.CLI
   ```

3. Confirm installation:

   ```bash
   CombineFiles --version
   ```

> After this, you can call `CombineFiles` from any directory.

---

## CLI Usage

Below are common scenarios and options for the CLI application (CombineFiles.CLI).

### Basic Merge

Merge all `.txt` files in a folder into a single output:

```bash
CombineFiles merge --input-folder "./logs" --output-file "./merged.txt"
```

* `--input-folder`: path to the directory containing the files you want to merge (search is recursive by default).
* `--output-file`: path (and filename) for the merged result.

### Deduplication and Line Limits

Omit duplicate files (by content hash), and limit each file to the first 1000 lines:

```bash
CombineFiles merge \
  --input-folder "./data" \
  --output-file "./result.txt" \
  --dedupe-by-hash \
  --max-lines-per-file 1000
```

* `--dedupe-by-hash` (or `-d`): skip any file whose computed hash matches an already-processed file.
* `--max-lines-per-file <int>`: stop reading a given file after `<int>` lines.

### Text Compaction for LLM Optimization

Optimize output for use with LLMs (ChatGPT, Claude, etc.) by reducing token usage:

```bash
CombineFiles merge \
  --input-folder "./source" \
  --output-file "./optimized.txt" \
  --compact-spaces \
  --compact-llm
```

* `--compact-spaces`: converts 4 consecutive spaces to 1 tab, reducing token count significantly for indented code.
* `--compact-llm`: removes excessive empty lines (keeps max 1 consecutive empty line) and compacts headers for better LLM processing.

### Interactive Mode

Launch an interactive wizard that will guide you through selecting folder, output path, and options:

```bash
CombineFiles merge --interactive
```

The wizard (powered by Spectre.Console) will prompt you step by step.

### Help & Options

To see all available commands and flags:

```bash
CombineFiles help
```

Or for a specific command:

```bash
CombineFiles merge --help
```

Below is a summary of the most common CLI options:

```
merge:
  --input-folder <path>        Path to directory containing files to merge
  --output-file <path>         Path (including filename) for merged result
  -d, --dedupe-by-hash         Skip duplicate files based on computed hash
  --max-lines-per-file <int>   Maximum lines to read from each input file
  --token-budget <int>         Global "token" limit across all files (e.g., total lines)
  --filter-hidden              Include hidden files in the merge (off by default)
  --compact-spaces             Convert 4 consecutive spaces to 1 tab (reduces token usage)
  --compact-llm                Optimize output for LLM: remove excessive empty lines and compact headers
  --interactive                Launch interactive wizard
  -v, --verbose                Enable verbose logging to console
  -h, --help                   Show help and exit
```

---

## Library Usage (`CombineFiles.Core`)

If you want to incorporate file-merging logic into your own .NET application, reference the `CombineFiles.Core` package (via NuGet or local project reference) and use classes under the `CombineFiles.Core.Services` namespace.

### API Overview

* **`FileMerger`** (main service)

  * `FileMerger(string outputFilePath, bool avoidDuplicatesByHash = true, int? maxLinesPerFile = null, Func<ProcessedTokenBudget, bool>? tokenBudgetCallback = null)`
    Construct a new instance.

    * `outputFilePath`: where to write the merged file.
    * `avoidDuplicatesByHash`: whether to skip duplicate files.
    * `maxLinesPerFile`: if specified, maximum lines per file.
    * `tokenBudgetCallback`: optional callback to decide when to stop merging (e.g., implement a custom “token” limit).

  * `bool MergeFile(string filePath)`
    Reads `filePath` and appends its contents to the output file (subject to dedupe, line and token checks). Returns `true` if the file was fully or partially merged; `false` if skipped (e.g., duplicate or budget exhausted).

  * `void Dispose()`
    Flushes and closes the underlying writer. Use in a `using` block or call explicitly.

* **`ProcessedTokenBudget`** (model)

  * Properties like `LinesProcessed` or other metrics to help implement a budget-based stop condition.

### Example: Merge in Code

```csharp
using System;
using CombineFiles.Core.Services;

namespace MyApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string outputPath = @"C:\temp\all-combined.txt";
            var maxLines = 500; // read up to 500 lines per file
            var totalLineBudget = 5000; // global limit

            // Custom callback: return true if still under budget; false to stop
            Func<ProcessedTokenBudget, bool> budgetCallback = budget =>
            {
                return budget.LinesProcessed < totalLineBudget;
            };

            // Ensure writer is properly closed/disposed
            using var merger = new FileMerger(outputPath, avoidDuplicatesByHash: true, maxLinesPerFile: maxLines, tokenBudgetCallback: budgetCallback)
            {
                // (Optional) Enable console logging
                merger.EnableConsoleLogging();
            };

            // Example: merge all .log files under a folder
            foreach (var file in Directory.GetFiles(@"C:\logs", "*.log", SearchOption.AllDirectories))
            {
                bool merged = merger.MergeFile(file);
                if (!merged)
                {
                    Console.WriteLine($"Skipped: {file}");
                }
            }

            // Dispose is called automatically at the end of the using block,
            // which flushes any remaining buffer to disk.
        }
    }
}
```

---

## Configuration & Extensions

Depending on your needs, you can tweak or extend default behavior:

### Filtering Hidden Files

By default, hidden files (e.g., files beginning with a “.” on Unix, or `Hidden` attribute on Windows) are **skipped**. To include them in the merge:

* **CLI**: pass `--filter-hidden`.
* **Library**: set `merger.IncludeHiddenFiles = true;` before calling `MergeFile(...)` (assuming public setter available).

### Custom Token Budget Logic

If you want more sophisticated “token” logic (for example, count words or custom markers), implement your own `Func<ProcessedTokenBudget, bool>`:

```csharp
Func<ProcessedTokenBudget, bool> wordBudgetCallback = budget =>
{
    // Suppose ProcessedTokenBudget.WordsProcessed exists
    return budget.WordsProcessed < 20000;
};

using var merger = new FileMerger(
    outputFilePath: "./combined.txt",
    avoidDuplicatesByHash: false,
    maxLinesPerFile: null,
    tokenBudgetCallback: wordBudgetCallback);
```

---

## Contributing

1. Fork the repository:

   ```bash
   git clone https://github.com/slim16165/CombineFiles.git
   cd CombineFiles
   ```

2. Create a new branch for your feature or bug fix:

   ```bash
   git checkout -b feature/my-new-feature
   ```

3. Implement changes, update tests if needed, and add new unit tests:

   ```bash
   cd CombineFiles.Core
   dotnet test
   ```

4. Commit your changes and push to your fork:

   ```bash
   git commit -am "Add my new feature"
   git push origin feature/my-new-feature
   ```

5. Open a Pull Request on the main repository. Ensure your PR description clearly explains the rationale, changes, and any backward-compatibility considerations.

Please follow the existing code style, naming conventions, and documentation format. All new features should include unit tests under `CombineFiles.Tests`.

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.

---

## Contact

* **Author**: Gianluigi Salvi
* **Repo**: [https://github.com/slim16165/CombineFiles](https://github.com/slim16165/CombineFiles)
* **NuGet**: [CombineFiles.Core](https://www.nuget.org/packages/CombineFiles.Core)

Feel free to open issues, submit PRs, or reach out for questions. Happy merging!