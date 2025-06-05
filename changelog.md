# Changelog

All notable changes to this project will be documented in this file.

The format adheres to [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and follows [Semantic Versioning](https://semver.org/).

## [1.2.0] – 2025-06-05
### Fixed
- Refactored and fixed `MergeFile` method to prevent truncated output (missing `Flush`) and improve modularity.  
- Added explicit null/empty checks for the input file path.  
- Separated I/O logic into smaller private helpers (e.g., `WriteHeader`, `ProcessFileLines`, `ExceededMaxLines`, `TokenBudgetExceeded`, `Flush`, `LogSkipDuplicate`).  
- Preserved support for deduplication by hash, per-file line limits, and global token-budget constraints.  
- Default behavior now concatenates all files without limits as originally intended.  

> **Tag:** `v1.2.0`  
> **Commit:** “Refactor and fix MergeFile method to prevent truncated output and improve modularity” (Jun 05 2025).

---

## [1.1.0] – 2025-06-01
### Added
- Introduced `--interactive` option to trigger a Spectre.Console wizard in the CLI.  
- Support for per-file line limits and a global token budget in `FileMerger`.  
- Inclusion of the folder path in output filenames (minor cleanup to make output naming more descriptive).  

### Changed
- Refactored `FileMerger` to optimize I/O, deduplicate by hash, and use a single `StreamWriter` instance.  
- Bumped package versions (dependency updates).  

> **Tag:** `v1.1.0`  
> **Commits:**  
>  - “Add --interactive option to trigger Spectre.Console wizard” (Jun 01 2025)  
>  - “Add support for per-file line limits and global token budget in FileMerger” (Jun 01 2025)  
>  - “Include folder path in output filenames; minor cleanup” (Jun 01 2025)  
>  - “Refactor FileMerger: optimize I/O, deduplicate by hash, use single StreamWriter” (Jun 01 2025)  
>  - “package update” (Jun 01 2025)

---

## [1.0.0] – 2025-03-16
### Changed
- Updated `README.md` to include console version details and added a link to the new PowerShell repository.  
- Added Spectre.Console integration and improved CLI logic to better match PowerShell behavior.  

> **Tag:** `v1.0.0`  
> **Commits:**  
>  - “Updated README to include console version and link to the new PowerShell repo” (Mar 16 2025)  
>  - “Added Spectre.Console and improved logic to better match PowerShell functionality” (Mar 16 2025)

---

## [0.9.0] – 2025-03-15
### Added
- Introduced `ConsoleHelper` class to centralize console I/O patterns.  
- Refactored logging and alias handling into a dedicated helper.  

### Fixed
- Resolved parameter collision issues in several methods.  
- Removed `packages.config` and migrated all package references to `PackageReference`. Removed obsolete `HintPath` entries and cleaned up dependency management.  

> **Tag:** `v0.9.0`  
> **Commits:**  
>  - “Added ConsoleHelper and refactored Logging & Alias” (Mar 15 2025)  
>  - “Fixed parameter collisions” (Mar 15 2025)  
>  - “Removed packages.config and migrated to PackageReference; removed obsolete HintPath entries and cleaned up dependencies” (Mar 15 2025)

---

## [0.8.0] – 2025-03-04
### Changed
- Miscellaneous bug fixes and code refactoring across the core and CLI layers.  

> **Tag:** `v0.8.0`  
> **Commit:** “fix e refactor vari” (Mar 04 2025)

---

## [0.7.0] – 2025-03-03
### Fixed
- Corrected numerous runtime and compile-time errors discovered during manual code review.  

> **Tag:** `v0.7.0`  
> **Commit:** “Corrected numerous errors after manual review” (Mar 03 2025)

---

## [0.6.0] – 2025-03-02
### Changed
- Split the solution into multiple projects and moved functionality into `CombineFiles.Core`; added `README.md`.  
- Refactored core project in preparation for migrating away from PowerShell.  
- Created a porting version (with 4–5 “o1” example prompts).  
- Refactored the porting logic while still contained in ConsoleApp.  
- Moved relevant files into the `CombineFiles.Core` project.  

> **Tag:** `v0.6.0`  
> **Commits:**  
>  - “Split solution into multiple projects; moved functionality into CombineFiles.Core; added README.md” (Mar 02 2025)  
>  - “Refactored core project prior to PowerShell migration” (Mar 02 2025)  
>  - “Created porting version (with 4–5 o1 example prompts)” (Mar 02 2025)  
>  - “Refactored porting logic while still in ConsoleApp” (Mar 02 2025)  
>  - “Refactor: moved files to CombineFiles.Core” (Mar 02 2025)

---

## [0.5.0] – 2025-03-01
### Added
- Added the ability to filter and show hidden files during merge.  
- Refactored `TreeViewFileExplorer` for improved maintainability.  
- Migrated UI layer to Telerik controls.  

> **Tag:** `v0.5.0`  
> **Commits:**  
>  - “Added ability to filter and show hidden files” (Mar 01 2025)  
>  - “Refactored TreeViewFileExplorer” (Mar 01 2025)  
>  - “Migrated UI to Telerik” (Mar 01 2025)

---

## [0.4.0] – 2024-11-13
### Added
- Added new custom `TreeViewFileExplorer` control.  
- Refactored UI with Telerik: split into UserControls and improved merge options.  
- Added TreeView and completed migration to Telerik.  

> **Tag:** `v0.4.0`  
> **Commits:**  
>  - “Added new custom TreeViewFileExplorer” (Nov 13 2024)  
>  - “Refactored UI with Telerik: split into UserControls and improved merge options” (Nov 13 2024)  
>  - “Added TreeView and migrated to Telerik” (Nov 13 2024)

---

## [0.1.0] – 2024-11-13
### Initial Release
- Initial commit: project scaffolding, basic CLI & core setup.  

> **Tag:** `v0.1.0`  
> **Commit:** “Initial commit” (Nov 13 2024)
