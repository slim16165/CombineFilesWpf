### Contenuto di OptionBuilder.cs ###
using System;
using System.Collections.Generic;
using System.CommandLine;

namespace CombineFiles.ConsoleApp.Extensions;

/// <summary>
/// Builder per la creazione di Option con un'interfaccia fluida che distingue alias lunghi e corti.
/// </summary>
public class OptionBuilder<T>
{
    private string? _longAlias;
    private string? _shortAlias;
    private string _description = "";
    private T _defaultValue = default!;
    private bool _hasDefaultValue = false;

    /// <summary>
    /// Imposta l'alias lungo. È obbligatorio, ed è formattato automaticamente per iniziare con "--".
    /// </summary>
    public OptionBuilder<T> WithLongAlias(string longAlias)
    {
        _longAlias = FormatLongAlias(longAlias);
        return this;
    }

    /// <summary>
    /// Imposta l'alias corto. Viene formattato automaticamente per iniziare con "-".
    /// </summary>
    public OptionBuilder<T> WithShortAlias(string shortAlias)
    {
        _shortAlias = FormatShortAlias(shortAlias);
        return this;
    }

    /// <summary>
    /// Imposta la descrizione dell'opzione.
    /// </summary>
    public OptionBuilder<T> WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Imposta il valore di default, abilitando il default.
    /// </summary>
    public OptionBuilder<T> WithDefaultValue(T defaultValue)
    {
        _defaultValue = defaultValue;
        _hasDefaultValue = true;
        return this;
    }

    /// <summary>
    /// Costruisce l'istanza di Option combinando gli alias e applicando le impostazioni.
    /// </summary>
    public Option<T> Build()
    {
        var aliases = BuildAliasList();
        Option<T> option = _hasDefaultValue
            ? new Option<T>(aliases.ToArray(), () => _defaultValue, _description)
            : new Option<T>(aliases.ToArray(), _description);
        return option;
    }

    /// <summary>
    /// Costruisce la lista degli alias, includendo sia quello lungo che quello corto se specificato.
    /// Se c'è una collisione (alias duplicato), lo short alias non viene aggiunto.
    /// </summary>
    private List<string> BuildAliasList()
    {
        var aliases = new List<string>();

        if (!string.IsNullOrEmpty(_longAlias))
        {
            aliases.Add(_longAlias);
            // Aggiunge anche la versione in minuscolo, se diversa
            var lowerAlias = _longAlias.ToLowerInvariant();
            if (!aliases.Contains(lowerAlias))
                aliases.Add(lowerAlias);
        }
        else
        {
            throw new InvalidOperationException("L'alias lungo è obbligatorio.");
        }

        if (!string.IsNullOrEmpty(_shortAlias))
        {
            if (!aliases.Contains(_shortAlias))
                aliases.Add(_shortAlias);
        }

        return aliases;
    }


    /// <summary>
    /// Formatta l'alias lungo per assicurarsi che inizi con "--".
    /// </summary>
    private static string FormatLongAlias(string alias)
    {
        // Se l'alias inizia già con "-" (sia uno che due), restituisce l'alias così com'è.
        if (alias.StartsWith("-"))
            return alias;
        return "--" + alias;
    }


    /// <summary>
    /// Formatta lo short alias per assicurarsi che inizi con "-".
    /// </summary>
    private static string FormatShortAlias(string alias)
    {
        return alias.StartsWith("-") ? alias : "-" + alias;
    }
}

/// <summary>
/// Metodo helper per iniziare la costruzione dell'opzione in modo fluente.
/// </summary>
public static class OptionBuilder
{
    public static OptionBuilder<T> For<T>(string longAlias)
    {
        return new OptionBuilder<T>().WithLongAlias(longAlias);
    }
}

### Contenuto di RootCommandBuilder.cs ###
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using CombineFiles.ConsoleApp.Helpers;
using CombineFiles.Core.Configuration;

namespace CombineFiles.ConsoleApp.Extensions;

/// <summary>
/// Classe dedicata a creare e configurare il RootCommand, con tutte le opzioni e il relativo Binder.
/// </summary>
public static class RootCommandBuilder
{
    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Strumento per combinare file seguendo diverse modalità di selezione");

        // Utilizzo del builder pattern per creare le opzioni in modo fluido
        var helpOption = OptionBuilder.For<bool>("Help")
            .WithDescription("Mostra l'aiuto")
            .WithShortAlias("h")
            .Build();

        var listPresetsOption = OptionBuilder.For<bool>("List-presets")
            .WithDescription("Mostra l'elenco dei preset disponibili")
            .WithShortAlias("l")
            .Build();

        var presetOption = OptionBuilder.For<string>("Preset")
            .WithDescription("Specifica il preset da utilizzare")
            .WithShortAlias("p")
            .Build();

        var modeOption = OptionBuilder.For<string>("Mode")
            .WithDescription("Modalità di selezione (list, extensions, regex, InteractiveSelection)")
            .WithShortAlias("m")
            .Build();

        // Per le opzioni che iniziano con 'e' il builder ignora il short alias
        var extensionsOption = OptionBuilder.For<List<string>>("Extensions")
            .WithDescription("Elenco di estensioni da includere")
            .Build();

        var excludePathsOption = OptionBuilder.For<List<string>>("exclude-paths")
            .WithDescription("Percorsi da escludere")
            .Build();

        var excludeFilePatternsOption = OptionBuilder.For<List<string>>("Exclude-file-patterns")
            .WithDescription("Pattern regex per escludere file")
            .Build();

        var outputFileOption = OptionBuilder.For<string>("Output-file")
            .WithDescription("File di output")
            .WithDefaultValue("CombinedFile.txt")
            .WithShortAlias("o")
            .Build();

        var recurseOption = OptionBuilder.For<bool>("Recurse")
            .WithDescription("Ricerca ricorsiva nelle sottocartelle")
            .Build();

        var enableLogOption = OptionBuilder.For<bool>("Enable-log")
            .WithDescription("Abilita la generazione del log")
            .Build();

        // Aggiunta delle opzioni al comando radice
        rootCommand.AddOption(helpOption);
        rootCommand.AddOption(listPresetsOption);
        rootCommand.AddOption(presetOption);
        rootCommand.AddOption(modeOption);
        rootCommand.AddOption(extensionsOption);
        rootCommand.AddOption(excludePathsOption);
        rootCommand.AddOption(excludeFilePatternsOption);
        rootCommand.AddOption(outputFileOption);
        rootCommand.AddOption(recurseOption);
        rootCommand.AddOption(enableLogOption);

        // Imposta il gestore con un binder personalizzato
        rootCommand.SetHandler(
            (CombineFilesOptions options) => ExecutionFlow.Execute(options),
            new CombineFilesOptionsBinder(
                helpOption,
                listPresetsOption,
                presetOption,
                modeOption,
                extensionsOption,
                excludePathsOption,
                excludeFilePatternsOption,
                outputFileOption,
                recurseOption,
                enableLogOption
            ));

        return rootCommand;
    }
}

### Contenuto di CombineFilesOptionsBinder.cs ###
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using CombineFiles.Core.Configuration;

namespace CombineFiles.ConsoleApp.Helpers;

public class CombineFilesOptionsBinder : BinderBase<CombineFilesOptions>
{
    private readonly Option<bool> _helpOption;
    private readonly Option<bool> _listPresetsOption;
    private readonly Option<string> _presetOption;
    private readonly Option<string> _modeOption;
    private readonly Option<List<string>> _extensionsOption;
    private readonly Option<List<string>> _excludePathsOption;
    private readonly Option<List<string>> _excludeFilePatternsOption;
    private readonly Option<string> _outputFileOption;
    private readonly Option<bool> _recurseOption;
    private readonly Option<bool> _enableLogOption;

    public CombineFilesOptionsBinder(
        Option<bool> helpOption,
        Option<bool> listPresetsOption,
        Option<string> presetOption,
        Option<string> modeOption,
        Option<List<string>> extensionsOption,
        Option<List<string>> excludePathsOption,
        Option<List<string>> excludeFilePatternsOption,
        Option<string> outputFileOption,
        Option<bool> recurseOption,
        Option<bool> enableLogOption)
    {
        _helpOption = helpOption;
        _listPresetsOption = listPresetsOption;
        _presetOption = presetOption;
        _modeOption = modeOption;
        _extensionsOption = extensionsOption;
        _excludePathsOption = excludePathsOption;
        _excludeFilePatternsOption = excludeFilePatternsOption;
        _outputFileOption = outputFileOption;
        _recurseOption = recurseOption;
        _enableLogOption = enableLogOption;
    }

    protected override CombineFilesOptions GetBoundValue(BindingContext bindingContext)
    {
        return new CombineFilesOptions
        {
            Help = bindingContext.ParseResult.GetValueForOption(_helpOption),
            ListPresets = bindingContext.ParseResult.GetValueForOption(_listPresetsOption),
            Preset = bindingContext.ParseResult.GetValueForOption(_presetOption),
            Mode = bindingContext.ParseResult.GetValueForOption(_modeOption),
            Extensions = bindingContext.ParseResult.GetValueForOption(_extensionsOption) ?? new List<string>(),
            ExcludePaths = bindingContext.ParseResult.GetValueForOption(_excludePathsOption) ?? new List<string>(),
            ExcludeFilePatterns = bindingContext.ParseResult.GetValueForOption(_excludeFilePatternsOption) ?? new List<string>(),
            OutputFile = bindingContext.ParseResult.GetValueForOption(_outputFileOption),
            Recurse = bindingContext.ParseResult.GetValueForOption(_recurseOption),
            EnableLog = bindingContext.ParseResult.GetValueForOption(_enableLogOption)
        };
    }
}

### Contenuto di ParameterHelper.cs ###
using System;
using System.CommandLine;
using System.Linq;
using System.Collections.Generic;
using CombineFiles.Core;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.ConsoleApp.Helpers;

public static class ParameterHelper
{
    /// <summary>
    /// Stampa l’help.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine("Uso: CombineFiles [opzioni]");
        Console.WriteLine("--help, --list-presets, --mode, --extensions, ecc.");
        // altri dettagli...
    }

    /// <summary>
    /// Stampa la lista dei preset disponibili.
    /// </summary>
    public static void PrintPresetList()
    {
        ConsoleHelper.WriteColored("Preset disponibili:", ConsoleColor.Cyan);
        foreach (var pName in PresetManager.Presets.Keys)
            Console.WriteLine($"- {pName}");
    }

    /// <summary>
    /// Valida i parametri (alcuni controlli di esempio).
    /// </summary>
    public static bool ValidateParameters(CombineFilesOptions options, Logger logger)
    {
        if ((options.Mode?.Equals("list", StringComparison.OrdinalIgnoreCase) ?? false)
            && (options.FileList == null || options.FileList.Count == 0))
        {
            logger.WriteLog("La modalità 'list' richiede almeno un FileList.", LogLevel.ERROR);
            ConsoleHelper.WriteColored("Errore: Modalità 'list' -> -FileList mancante o vuota.", ConsoleColor.Red);
            return false;
        }
        // altri controlli...
        return true;
    }

    /// <summary>
    /// Verifica l'unicità degli alias nei simboli (Command/Option) contenuti nell'albero.
    /// Se skipConflictingAliases è true, in caso di collisione rimuove l’alias duplicato (loggando un warning)
    /// anziché lanciare un’eccezione.
    /// </summary>
    public static void CheckAliasCollisions(Command command, bool skipConflictingAliases = false)
    {
        // Dizionario globale: alias -> simbolo (Command o Option)
        var aliasMap = new Dictionary<string, Symbol>(StringComparer.Ordinal);
        TraverseCommand(command, aliasMap, skipConflictingAliases);
    }

    private static void TraverseCommand(
        Command command,
        Dictionary<string, Symbol> aliasMap,
        bool skipConflictingAliases)
    {
        // Controlla gli alias del comando corrente
        CheckSymbolAliases(command, aliasMap, skipConflictingAliases);

        // Controlla gli alias delle opzioni associate al comando
        foreach (var opt in command.Options)
        {
            CheckSymbolAliases(opt, aliasMap, skipConflictingAliases);
        }

        // Ripeti ricorsivamente sui subcomandi
        foreach (var sub in command.Subcommands)
        {
            TraverseCommand(sub, aliasMap, skipConflictingAliases);
        }
    }

    /// <summary>
    /// Restituisce l'elenco degli alias per il simbolo: se è un Command o un Option,
    /// li estrae, altrimenti restituisce il solo Name.
    /// </summary>
    private static IEnumerable<string> GetAliases(Symbol symbol)
    {
        if (symbol is Command command)
        {
            return command.Aliases; // Command espone Aliases
        }
        if (symbol is Option option)
        {
            return option.Aliases; // Option espone Aliases
        }
        return new List<string> { symbol.Name };
    }

    /// <summary>
    /// Rimuove l'alias dal simbolo, se possibile.
    /// </summary>
    private static void RemoveAlias(Symbol symbol, string alias)
    {
        if (symbol is Command command)
        {
            // Use reflection to access the private RemoveAlias method
            var removeAliasMethod = typeof(Command).GetMethod("RemoveAlias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            removeAliasMethod?.Invoke(command, [alias]);
        }
        else if (symbol is Option option)
        {
            // Use reflection to access the private RemoveAlias method
            var removeAliasMethod = typeof(Option).GetMethod("RemoveAlias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            removeAliasMethod?.Invoke(option, [alias]);
        }
        // Altri tipi: non modificabili
    }

    /// <summary>
    /// Controlla gli alias di un singolo simbolo e aggiorna il dizionario globale.
    /// In caso di collisione:
    /// - Se skipConflictingAliases è false, lancia un’eccezione.
    /// - Se è true, rimuove l’alias duplicato dal simbolo corrente e logga un warning.
    /// </summary>
    private static void CheckSymbolAliases(
        Symbol symbol,
        Dictionary<string, Symbol> aliasMap,
        bool skipConflictingAliases)
    {
        // Ottiene una copia degli alias (per iterare in sicurezza)
        var aliases = GetAliases(symbol).ToList();

        foreach (var alias in aliases)
        {
            if (aliasMap.TryGetValue(alias, out var existingSymbol))
            {
                if (!skipConflictingAliases)
                {
                    throw new ArgumentException(
                        $"Alias duplicato '{alias}' tra '{existingSymbol.Name}' e '{symbol.Name}'");
                }
                else
                {
                    // Rimuove l'alias duplicato dal simbolo corrente
                    RemoveAlias(symbol, alias);
                    ConsoleHelper.WriteColored(
                        $"[WARNING] Alias '{alias}' rimosso da '{symbol.Name}' perché già usato da '{existingSymbol.Name}'",
                        ConsoleColor.Yellow);
                }
            }
            else
            {
                aliasMap[alias] = symbol;
            }
        }
    }
}

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di Program.cs ###
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using CombineFiles.ConsoleApp.Extensions;
using CombineFiles.ConsoleApp.Helpers;

namespace CombineFiles.ConsoleApp;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1) Creiamo il RootCommand con il builder dedicato
        var rootCommand = RootCommandBuilder.CreateRootCommand();

        // 2) Controlla collisioni e rimuove alias duplicati (invece di lanciare eccezioni)
        ParameterHelper.CheckAliasCollisions(
            rootCommand,
            skipConflictingAliases: true /* se false => lancia eccezione */
        );

        // 3) Costruiamo il parser
        var commandLineBuilder = new CommandLineBuilder(rootCommand)
            .UseDefaults();

        var parser = commandLineBuilder.Build();

        // 4) Eseguiamo il parser
        return await parser.InvokeAsync(args);
    }
}

### Contenuto di CombineFilesOptions.cs ###
using System;
using System.Collections.Generic;

namespace CombineFiles.Core.Configuration;

public class CombineFilesOptions
{
    public bool Help { get; set; }
    public bool EnableLog { get; set; }
    public bool ListPresets { get; set; }
    public string? Preset { get; set; }
    public string? Mode { get; set; }
    public List<string> Extensions { get; set; } = new();
    public List<string> ExcludePaths { get; set; } = new();
    public List<string> ExcludeFiles { get; set; } = new();
    public List<string> ExcludeFilePatterns { get; set; } = new();
    public List<string> FileList { get; set; } = new();
    public List<string> RegexPatterns { get; set; } = new();
    public string? OutputFile { get; set; } = DefaultOutputFile;
    public bool OutputToConsole { get; set; }
    public string OutputFormat { get; set; } = "txt";
    public bool FileNamesOnly { get; set; }
    public string MinSize { get; set; }
    public string MaxSize { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool Recurse { get; set; }
    public static string DefaultOutputFile { get; set; } = "CombinedFile.txt";
}

### Contenuto di FileProcessingConfig.cs ###
namespace CombineFiles.Core.Configuration;

public class FileSearchConfig
{
    public bool IncludeSubfolders { get; set; }      // Se includere o meno le sottocartelle
    public bool ExcludeHidden { get; set; }         // Se escludere i file nascosti
    public string IncludeExtensions { get; set; }   // Estensioni da includere (es. ".txt,.cs")
    public string ExcludeExtensions { get; set; }   // Estensioni da escludere
    public string ExcludePaths { get; set; }        // Percorsi da escludere
}

public class FileMergeConfig
{
    public string OutputFolder { get; set; }
    public string OutputFileName { get; set; }      // Nome del file di output
    public bool OneFilePerExtension { get; set; }   // Se creare un file per ogni estensione
    public bool OverwriteFiles { get; set; }        // Se sovrascrivere i file esistenti
}

### Contenuto di FileProcessor.cs ###
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Handlers;
using CombineFiles.Core.Helpers;

namespace CombineFiles.Core;

public static class FileProcessor
{
    /// <summary>
    /// Metodo per ottenere la lista di file validi, su cui operare.
    /// Puoi estenderlo con filtri, regex, estensioni, etc.
    /// </summary>
    public static IEnumerable<string> GetFilesToProcess(string[] paths, FileSearchConfig? config = null)
    {
        config ??= new FileSearchConfig();

        var validFiles = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path, "*.*",
                    config.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                validFiles.AddRange(files.Where(f => FileFilterHelper.ShouldIncludeFile(
                    f, config.ExcludeHidden, config.ExcludePaths, config.ExcludeExtensions, config.IncludeExtensions)));
            }
            else if (File.Exists(path))
            {
                if (FileFilterHelper.ShouldIncludeFile(path, config.ExcludeHidden, config.ExcludePaths,
                        config.ExcludeExtensions, config.IncludeExtensions))
                {
                    validFiles.Add(path);
                }
            }
        }
        return validFiles;
    }

    public static string CombineContents(IEnumerable<string> files)
    {
        // Utilizzo di un comparatore case-insensitive per gestire correttamente le estensioni
        var handlers = new Dictionary<string, IFileContentHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { ".csv", new CsvContentHandler() },
            { ".json", new JsonContentHandler() }
        };

        var sb = new StringBuilder();
        foreach (var file in files)
        {
            sb.AppendLine($"### {Path.GetFileName(file)} ###");
            try
            {
                string content = File.ReadAllText(file);
                string extension = Path.GetExtension(file);

                if (!handlers.TryGetValue(extension, out var handler))
                {
                    handler = new DefaultContentHandler();
                }
                sb.AppendLine(handler.Handle(content));
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ERROR: unable to read {file} - {ex.Message}]");
            }
        }
        return sb.ToString();
    }
}

### Contenuto di CsvContentHandler.cs ###
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace CombineFiles.Core.Handlers;

public class CsvContentHandler : IFileContentHandler
{
    private readonly bool _hasHeaders;

    public CsvContentHandler(bool hasHeaders = true) => _hasHeaders = hasHeaders;

    public string Handle(string content)
    {
        // Configurazione del CsvReader impostando la presenza o meno dell'intestazione
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = _hasHeaders,
        };

        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, config);

        // Legge tutti i record dal CSV
        var records = csv.GetRecords<dynamic>().ToList();
        if (!records.Any())
            return string.Empty;

        var table = new StringBuilder();
        List<string> headers = new List<string>();

        if (_hasHeaders)
        {
            // Estrae le intestazioni dalle chiavi del primo record
            var firstRecord = (IDictionary<string, object>)records.First();
            headers = firstRecord.Keys.ToList();
            table.AppendLine(string.Join(" | ", headers));
            table.AppendLine(new string('-', headers.Count * 4));
        }

        // Itera sui record e costruisce le righe della tabella
        foreach (var record in records)
        {
            var dict = (IDictionary<string, object>)record;
            if (_hasHeaders)
            {
                // Garantisce l'ordine delle colonne secondo le intestazioni
                var values = headers.Select(header => dict[header]);
                table.AppendLine(string.Join(" | ", values));
            }
            else
            {
                table.AppendLine(string.Join(" | ", dict.Values));
            }
        }

        return table.ToString();
    }
}

### Contenuto di DefaultContentHandler.cs ###
namespace CombineFiles.Core.Handlers;

public class DefaultContentHandler : IFileContentHandler
{
    public string Handle(string content) => content;
}

### Contenuto di IFileContentHandler.cs ###
namespace CombineFiles.Core.Handlers;

public interface IFileContentHandler
{
    string Handle(string content);
}

### Contenuto di JsonContentHandler.cs ###
using System.Text.Json;

namespace CombineFiles.Core.Handlers;

public class JsonContentHandler : IFileContentHandler
{
    public string Handle(string content)
    {
        var doc = JsonDocument.Parse(content);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}

### Contenuto di ConsoleHelper.cs ###
using System;

namespace CombineFiles.Core.Helpers;

public static class ConsoleHelper
{
    /// <summary>
    /// Scrive un messaggio in console con il colore specificato e poi resetta il colore.
    /// </summary>
    /// <param name="message">Il messaggio da scrivere.</param>
    /// <param name="color">Il colore da usare.</param>
    public static void WriteColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}

### Contenuto di FileCollectionHelper.cs ###
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Infrastructure;
using CombineFiles.Core.Services;

namespace CombineFiles.Core.Helpers;

public static class FileCollectionHelper
{
    /// <summary>
    /// Metodo “unico” che, in base a options.Mode, decide come ottenere la lista di file.
    /// </summary>
    public static List<string> CollectFiles(CombineFilesOptions options, Logger logger, FileCollector collector, string sourcePath)
    {
        List<string> filesToProcess;

        switch (options.Mode?.ToLowerInvariant())
        {
            case "list":
                filesToProcess = HandleListMode(options, logger);
                break;

            case "extensions":
                filesToProcess = HandleExtensionsMode(options, logger, collector);
                break;

            case "regex":
                filesToProcess = HandleRegexMode(options, logger, collector);
                break;

            case "interactiveselection":
                // Ottieni prima i file per estensione
                filesToProcess = HandleExtensionsMode(options, logger, collector);

                // Poi avvia la selezione interattiva
                filesToProcess = collector.StartInteractiveSelection(filesToProcess, sourcePath);
                break;

            default:
                // Se non specificato, raccogli tutti i file
                filesToProcess = collector.GetAllFiles(sourcePath, options.Recurse);
                break;
        }

        return filesToProcess;
    }

    private static List<string> HandleListMode(CombineFilesOptions options, Logger logger)
    {
        var filesToProcess = new List<string>();
        string basePath = Directory.GetCurrentDirectory();

        foreach (var relativeFile in options.FileList)
        {
            string absPath = Path.IsPathRooted(relativeFile)
                ? relativeFile
                : Path.Combine(basePath, relativeFile);

            if (File.Exists(absPath))
            {
                logger.WriteLog($"File incluso dalla lista: {absPath}", LogLevel.INFO);
                filesToProcess.Add(absPath);
            }
            else
            {
                logger.WriteLog($"File non trovato: {absPath}", LogLevel.WARNING);
                Console.WriteLine($"Avviso: File non trovato: {absPath}");
            }
        }

        return filesToProcess;
    }

    private static List<string> HandleExtensionsMode(CombineFilesOptions options, Logger logger, FileCollector collector)
    {
        var basePath = Directory.GetCurrentDirectory();
        var allFiles = collector.GetAllFiles(basePath, options.Recurse);
        var matched = new List<string>();

        foreach (var file in allFiles)
        {
            foreach (var ext in options.Extensions)
            {
                if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    matched.Add(file);
                    break;
                }
            }
        }

        matched = matched.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        logger.WriteLog($"File da processare dopo filtraggio per estensioni: {matched.Count}", LogLevel.INFO);
        return matched;
    }

    private static List<string> HandleRegexMode(CombineFilesOptions options, Logger logger, FileCollector collector)
    {
        var basePath = Directory.GetCurrentDirectory();
        var allFiles = collector.GetAllFiles(basePath, options.Recurse);
        var matched = new List<string>();

        foreach (var file in allFiles)
        {
            foreach (var pattern in options.RegexPatterns)
            {
                if (Regex.IsMatch(file, pattern))
                {
                    matched.Add(file);
                    break; // Evita duplicati
                }
            }
        }

        matched = matched.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        logger.WriteLog($"File da processare (regex): {matched.Count}", LogLevel.INFO);
        return matched;
    }
}

### Contenuto di FileHelper.cs ###
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CombineFiles.Core.Helpers;

public static class FileHelper
{
    /// <summary>
    /// Converte una stringa (es. "10MB", "1024") in byte.
    /// </summary>
    public static long ConvertSizeToBytes(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
            return 0;

        size = size.Trim().ToUpperInvariant();
        var match = Regex.Match(size, @"^(\d+)\s*(KB|MB|GB)$", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            long value = long.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value.ToUpperInvariant();
            return unit switch
            {
                "KB" => value * 1024,
                "MB" => value * 1024 * 1024,
                "GB" => value * 1024 * 1024 * 1024,
                _ => throw new ArgumentException($"Unità sconosciuta: {unit}")
            };
        }

        if (Regex.IsMatch(size, @"^\d+$"))
        {
            return long.Parse(size);
        }

        throw new ArgumentException($"Formato di dimensione non riconosciuto: {size}");
    }

    /// <summary>
    /// Restituisce il percorso relativo.
    /// </summary>
    public static string GetRelativePath(string basePath, string targetPath)
    {
        Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
        Uri targetUri = new Uri(targetPath);

        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri)
            .ToString().Replace('/', Path.DirectorySeparatorChar));
    }
}

### Contenuto di FileSystemHelper.cs ###
using System;
using System.IO;
using System.Linq;

namespace CombineFiles.Core.Helpers;

public static class FileSystemHelper
{
    /// <summary>
    /// Copia una directory sorgente nella destinazione.
    /// </summary>
    /// <param name="sourceDir">Percorso della directory sorgente.</param>
    /// <param name="destinationDir">Percorso della directory di destinazione.</param>
    /// <param name="recursive">Se copiare anche le sottocartelle.</param>
    /// <param name="overwrite">Se sovrascrivere i file esistenti (default: false).</param>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, bool overwrite = false)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            string targetPath = Path.Combine(destinationDir, file.Name);
            try
            {
                file.CopyTo(targetPath, overwrite);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to copy {file.FullName} to {targetPath}: {ex.Message}", ex);
            }
        }

        if (recursive)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                string newDestDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir, true, overwrite);
            }
        }
    }
    /// <summary>
    /// Sposta la directory o il file dalla posizione source a destination.
    /// </summary>
    public static void Move(string source, string destination, bool isDirectory)
    {
        if (isDirectory && Directory.Exists(destination) || !isDirectory && File.Exists(destination))
            throw new IOException($"Destination already exists: {destination}");
        if (isDirectory) Directory.Move(source, destination);
        else File.Move(source, destination);
    }

    /// <summary>
    /// Rinomina un file o una directory.
    /// </summary>
    public static void Rename(string source, string newName, bool isDirectory)
    {
        string parent = System.IO.Path.GetDirectoryName(source);
        string newPath = System.IO.Path.Combine(parent, newName);

        if (isDirectory)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Source directory not found: {source}");
            Directory.Move(source, newPath);
        }
        else
        {
            if (!File.Exists(source))
                throw new FileNotFoundException($"Source file not found: {source}");
            File.Move(source, newPath);
        }
    }

    /// <summary>
    /// Controlla se il nome è valido per un file o una directory.
    /// </summary>
    public static bool IsValidName(string name)
    {
        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return !name.Any(c => invalidChars.Contains(c));
    }
}

### Contenuto di OutputFileHelper.cs ###
using System.IO;
using System.Text;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Helpers;

public static class OutputFileHelper
{
    /// <summary>
    /// Svuota o crea il file di output (equivalente a "Out-File -Force" in PowerShell).
    /// </summary>
    public static void PrepareOutputFile(string? outputFile, string outputFormat, Encoding encoding, Logger logger)
    {
        File.WriteAllText(outputFile, string.Empty, encoding);
        logger.WriteLog($"File di output creato/svuotato: {outputFile}", LogLevel.INFO);
    }
}

### Contenuto di PathHelper.cs ###
using System.Collections.Generic;
using System.IO;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Helpers;

public static class PathHelper
{
    /// <summary>
    /// Normalizza i percorsi da escludere in percorsi assoluti, loggando eventuali avvisi se non trovati.
    /// </summary>
    public static List<string> NormalizeExcludePaths(List<string> excludePaths, string basePath, Logger logger)
    {
        var fullExcludePaths = new List<string>();
        if (excludePaths == null) return fullExcludePaths;

        foreach (var p in excludePaths)
        {
            string candidate = p;
            if (!Path.IsPathRooted(candidate))
                candidate = Path.Combine(basePath, p);

            if (Directory.Exists(candidate))
            {
                fullExcludePaths.Add(candidate);
                logger.WriteLog($"Percorso escluso aggiunto: {candidate}", LogLevel.DEBUG);
            }
            else
            {
                // Se non esiste come directory, avvisa
                logger.WriteLog($"Directory di esclusione non trovata o non valida: {candidate}", LogLevel.WARNING);
            }
        }
        return fullExcludePaths;
    }
}

### Contenuto di Logger.cs ###
using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace CombineFiles.Core.Infrastructure
{
    public class Logger
    {
        private readonly string _logFile;
        private readonly bool _enabled;

        // Aggiunta: livello minimo di log
        public LogLevel MinimumLogLevel { get; }


        public Logger(string logFile, bool enabled, LogLevel minimumLogLevel = LogLevel.INFO)
        {
            _logFile = logFile;
            _enabled = enabled;
            MinimumLogLevel = minimumLogLevel;
        }

        /// <summary>
        /// Logga un messaggio su file (se abilitato e livello >= MinimumLogLevel) e lo mostra a console con colori diversi
        /// in base al livello. Il livello è passato come stringa e convertito in enum.
        /// </summary>
        public void WriteLog(string message, string level)
        {
            if (!Enum.TryParse(level, true, out LogLevel logLevel))
                logLevel = LogLevel.INFO;

            // Se il livello del messaggio è minore di quello impostato, non loggare
            if (logLevel > MinimumLogLevel)
                return;

            LogInternal(message, logLevel);
        }

        /// <summary>
        /// Overload che accetta direttamente il LogLevel e sceglie il colore tramite Lev2Color.
        /// </summary>
        public void WriteLog(string message, LogLevel logLevel)
        {
            if (logLevel > MinimumLogLevel)
                return;

            LogInternal(message, logLevel);
        }

        /// <summary>
        /// Metodo interno per gestire il log su file e console.
        /// </summary>
        private void LogInternal(string message, LogLevel logLevel)
        {
            // Log su file
            if (_enabled)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logMessage = $"{timestamp} [{logLevel}] {message}";
                    File.AppendAllText(_logFile, logMessage + Environment.NewLine);
                }
                catch
                {
                    // Se fallisce la scrittura su file, gestire l’errore (qui viene ignorato)
                }
            }

            // Visualizzazione a console con colore scelto tramite Lev2Color
            string color = Lev2Color(logLevel);
            AnsiConsole.MarkupLine($"[{color}]{message}[/]");
        }

        /// <summary>
        /// Restituisce il colore associato al LogLevel.
        /// </summary>
        private string Lev2Color(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.INFO => "blue",
                LogLevel.WARNING => "yellow",
                LogLevel.ERROR => "red",
                LogLevel.DEBUG => "grey",
                _ => "white",
            };
        }
    }
}

### Contenuto di LogLevel.cs ###
namespace CombineFiles.Core.Infrastructure;

public enum LogLevel
{
    ERROR = 0,
    WARNING = 1,
    INFO = 2,
    DEBUG = 3
}

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di PresetManager.cs ###
using System;
using System.Collections.Generic;
using System.Text;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;

#pragma warning disable CS8604 // Possible null reference argument.

namespace CombineFiles.Core;

// Preset immutabile con proprietà di sola lettura.
public class Preset
{
    public string Mode { get; }
    public IReadOnlyList<string> Extensions { get; }
    public string OutputFile { get; }
    public bool Recurse { get; }
    public IReadOnlyList<string> ExcludePaths { get; }
    public IReadOnlyList<string> ExcludeFilePatterns { get; }

    public Preset(
        string mode,
        List<string> extensions,
        string outputFile,
        bool recurse,
        List<string> excludePaths,
        List<string> excludeFilePatterns)
    {
        Mode = mode;
        Extensions = extensions.AsReadOnly();
        OutputFile = outputFile;
        Recurse = recurse;
        ExcludePaths = excludePaths.AsReadOnly();
        ExcludeFilePatterns = excludeFilePatterns.AsReadOnly();
    }
}

public static class PresetManager
{
    // I preset sono definiti in modo immutabile e possono essere eventualmente spostati in un file di configurazione.
    public static readonly Dictionary<string, Preset> Presets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CSharp"] = new Preset(
                mode: "extensions",
                extensions: [".cs", ".xaml"],
                outputFile: "CombinedFile.cs",
                recurse: true,
                excludePaths: ["Properties", "obj", "bin"],
                excludeFilePatterns:
                [
                    ".*\\.g\\.i\\.cs$", ".*\\.g\\.cs$", ".*\\.designer\\.cs$", ".*AssemblyInfo\\.cs$",
                    "^auto-generated"
                ]
            ),
            ["VB"] = new Preset(
                mode: "extensions",
                extensions: [".vb", ".resx"],
                outputFile: "CombinedFile.vb",
                recurse: true,
                excludePaths: ["My Project", "bin", "obj"],
                excludeFilePatterns: [".*\\.designer\\.vb$", ".*AssemblyInfo\\.vb$"]
            ),
            ["JavaScript"] = new Preset(
                mode: "extensions",
                extensions: [".js", ".jsx"],
                outputFile: "CombinedFile.js",
                recurse: true,
                excludePaths: ["node_modules", "dist"],
                excludeFilePatterns: [".*\\.min\\.js$"]
            )
        };

    public static void ApplyPreset(CombineFilesOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Preset))
            return;

        if (!Presets.TryGetValue(options.Preset, out Preset? preset))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Errore: Preset '{options.Preset}' non trovato.");
            Console.ResetColor();
            throw new ArgumentException($"Preset '{options.Preset}' non trovato");
        }

        // Usa il mapper per applicare il preset alle opzioni
        var logMessages = OptionsMapper.Map(preset, options);

        if (logMessages.Length > 0)
        {
            ConsoleHelper.WriteColored($"Applicato preset '{options.Preset}':\n{logMessages.ToString()}", ConsoleColor.Green);
        }
    }
}

// Classe dedicata alla mappatura dei preset sulle opzioni,
// centralizzando così la logica e rendendola più facilmente testabile.
public static class OptionsMapper
{
    public static string Map(Preset preset, CombineFilesOptions options)
    {
        var logMessages = new StringBuilder();

        if (string.IsNullOrEmpty(options.Mode))
        {
            options.Mode = preset.Mode;
            logMessages.AppendLine($"Mode = {preset.Mode}");
        }

        if (options.Extensions.Count == 0)
        {
            options.Extensions = new List<string>(preset.Extensions);
            logMessages.AppendLine($"Extensions = {string.Join(" ", preset.Extensions)}");
        }

        // Supponiamo che CombineFilesOptions.DefaultOutputFile sia "CombinedFile.txt"
        if (options.OutputFile == CombineFilesOptions.DefaultOutputFile)
        {
            options.OutputFile = preset.OutputFile;
            logMessages.AppendLine($"OutputFile = {preset.OutputFile}");
        }

        if (!options.Recurse)
        {
            options.Recurse = preset.Recurse;
            logMessages.AppendLine($"Recurse = {preset.Recurse}");
        }

        if (options.ExcludePaths.Count == 0)
        {
            options.ExcludePaths = new List<string>(preset.ExcludePaths);
            logMessages.AppendLine($"ExcludePaths = {string.Join(" ", preset.ExcludePaths)}");
        }

        if (options.ExcludeFilePatterns.Count == 0)
        {
            options.ExcludeFilePatterns = new List<string>(preset.ExcludeFilePatterns);
            logMessages.AppendLine($"ExcludeFilePatterns = {string.Join(" ", preset.ExcludeFilePatterns)}");
        }

        return logMessages.ToString();
    }
}

### Contenuto di FileCollector.cs ###
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

public class FileCollector
{
    private readonly Logger _logger;
    private readonly List<string> _excludePaths;
    private readonly List<string> _excludeFiles;
    private readonly List<string> _excludeFilePatterns;

    // Base path da usare per i percorsi relativi (puoi usare anche un costruttore
    // che riceve la cartella di partenza esplicitamente).
    private readonly string _basePath;

    public FileCollector(
        Logger logger,
        List<string> excludePaths,
        List<string> excludeFiles,
        List<string> excludeFilePatterns)
    {
        _logger = logger;
        _excludePaths = excludePaths;
        _excludeFiles = excludeFiles;
        _excludeFilePatterns = excludeFilePatterns;

        // Ad esempio, la cartella di partenza potrebbe essere l'ultima
        // dove hai lanciato l’app o un parametro in CombineFilesOptions.
        _basePath = Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Converte un percorso assoluto in relativo rispetto a _basePath.
    /// Se la conversione fallisce, restituisce comunque il path originale.
    /// </summary>
    private string ToRelativePath(string fullPath)
    {
        try
        {
            return FileHelper.GetRelativePath(_basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }

    private bool IsPathExcluded(string filePath)
    {
        // Esclusione per directory
        foreach (var path in _excludePaths)
        {
            if (filePath.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                // Log di debug (non comparirà se MinimumLogLevel > DEBUG)
                _logger.WriteLog(
                    $"Escluso per percorso: {ToRelativePath(filePath)} corrisponde a {ToRelativePath(path)}",
                    LogLevel.DEBUG
                );
                return true;
            }
        }

        // Esclusione per nome file
        string fileName = Path.GetFileName(filePath);
        foreach (var excludedFile in _excludeFiles)
        {
            if (fileName.Equals(excludedFile, StringComparison.OrdinalIgnoreCase))
            {
                _logger.WriteLog(
                    $"Escluso per nome file: {ToRelativePath(filePath)} corrisponde a {excludedFile}",
                    LogLevel.DEBUG
                );
                return true;
            }
        }

        // Esclusione per pattern regex
        foreach (var pattern in _excludeFilePatterns)
        {
            if (Regex.IsMatch(filePath, pattern))
            {
                _logger.WriteLog(
                    $"Escluso per pattern regex: {ToRelativePath(filePath)} corrisponde a {pattern}",
                    LogLevel.DEBUG
                );
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Ricorsivamente raccoglie tutti i file a partire da un percorso.
    /// </summary>
    public List<string> GetAllFiles(string startPath, bool recurse)
    {
        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void RecursiveGetFiles(string currentPath)
        {
            if (!visited.Add(currentPath))
                return;

            string[] items;
            try
            {
                items = Directory.GetFileSystemEntries(currentPath);
            }
            catch (Exception ex)
            {
                // In output normale, la segnali come WARNING
                _logger.WriteLog(
                    $"Errore durante l'accesso al percorso: {ToRelativePath(currentPath)} - {ex.Message}",
                    LogLevel.WARNING);
                return;
            }

            foreach (var item in items)
            {
                if (IsPathExcluded(item))
                {
                    // Log di debug. I dettagli li scrivo a livello debug.
                    _logger.WriteLog($"Percorso o file escluso: {ToRelativePath(item)}", LogLevel.DEBUG);
                    continue;
                }

                if (Directory.Exists(item))
                {
                    if (recurse)
                    {
                        var attr = File.GetAttributes(item);
                        bool isReparse = (attr & FileAttributes.ReparsePoint) != 0;
                        if (isReparse)
                        {
                            _logger.WriteLog(
                                $"Trovato reparse point: {ToRelativePath(item)} – non risolto",
                                LogLevel.DEBUG
                            );
                        }

                        RecursiveGetFiles(item);
                    }
                }
                else
                {
                    // Se vuoi, puoi mostrare a schermo i file inclusi,
                    // ma anche questo può essere spostato su DEBUG.
                    // _logger.WriteLog($"File incluso: {ToRelativePath(item)}", LogLevel.DEBUG);
                    result.Add(item);
                }
            }
        }

        RecursiveGetFiles(startPath);
        return result;
    }

    /// <summary>
    /// Avvia la selezione interattiva tramite Notepad.
    /// </summary>
    public List<string> StartInteractiveSelection(List<string> initialFiles, string sourcePath)
    {
        var relativePaths = initialFiles.Select(file =>
        {
            try
            {
                return FileHelper.GetRelativePath(sourcePath, file);
            }
            catch
            {
                return file;
            }
        }).ToList();

        string tempFilePath = Path.Combine(Path.GetTempPath(), "CombineFiles_InteractiveSelection.txt");
        try
        {
            File.WriteAllLines(tempFilePath, (IEnumerable<string>)relativePaths, Encoding.UTF8);
            _logger.WriteLog($"File di configurazione temporaneo creato: {tempFilePath}", LogLevel.DEBUG);
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nella scrittura del file temporaneo: {ex.Message}", LogLevel.ERROR);
            return new List<string>();
        }

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "notepad.exe";
            process.StartInfo.Arguments = tempFilePath;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            process.WaitForExit();

            _logger.WriteLog("Editor chiuso. Lettura del file di configurazione aggiornato.", LogLevel.DEBUG);
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nell'apertura di notepad: {ex.Message}", LogLevel.ERROR);
            return new List<string>();
        }

        List<string> updatedRelativePaths;
        try
        {
            updatedRelativePaths = File.ReadAllLines(tempFilePath, Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Errore nella lettura del file temporaneo: {ex.Message}", LogLevel.ERROR);
            return new List<string>();
        }

        var updatedFiles = new List<string>();
        foreach (var rel in updatedRelativePaths)
        {
            string absPath = Path.Combine(sourcePath, rel);
            if (File.Exists(absPath))
            {
                updatedFiles.Add(absPath);
            }
            else
            {
                _logger.WriteLog($"File non trovato durante la lettura del config: {absPath}", LogLevel.WARNING);
            }
        }

        _logger.WriteLog($"File aggiornati dopo InteractiveSelection: {updatedFiles.Count}", LogLevel.DEBUG);

        try
        {
            File.Delete(tempFilePath);
        }
        catch
        {
            /* Ignora errori di cancellazione */
        }

        return updatedFiles;
    }
}

### Contenuto di FileMerger.cs ###
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.Core.Services;

public class FileMerger
{
    private readonly Logger _logger;
    private readonly bool _outputToConsole;
    private readonly string? _outputFile;
    private readonly string _outputFormat;
    private readonly bool _fileNamesOnly;
    private readonly HashSet<string> _processedHashes = new(StringComparer.OrdinalIgnoreCase);

    public FileMerger(Logger logger, bool outputToConsole, string? outputFile, string outputFormat, bool fileNamesOnly)
    {
        _logger = logger;
        _outputToConsole = outputToConsole;
        _outputFile = outputFile;
        _outputFormat = outputFormat;
        _fileNamesOnly = fileNamesOnly;
    }

    /// <summary>
    /// Stampa in console oppure scrive su file.
    /// </summary>
    public void WriteOutputOrFile(string content)
    {
        if (_outputToConsole)
        {
            Console.WriteLine(content);
        }
        else
        {
            File.AppendAllText(_outputFile, content + Environment.NewLine);
        }
    }

    /// <summary>
    /// Aggiunge il contenuto dei file alla destinazione.
    /// </summary>
    public void MergeFiles(List<string> files)
    {
        foreach (var filePath in files)
        {
            // Calcola l’hash SHA256 per evitare duplicati (hard link)
            string hashString;
            try
            {
                using var sha256 = SHA256.Create();
                using var fs = File.OpenRead(filePath);
                var hash = sha256.ComputeHash(fs);
                hashString = BitConverter.ToString(hash).Replace("-", "");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Attenzione: Impossibile calcolare l'hash del file: {filePath}");
                Console.ResetColor();

                _logger.WriteLog($"Impossibile calcolare l'hash del file: {filePath} - {ex.Message}", LogLevel.WARNING);
                continue;
            }

            if (_processedHashes.Contains(hashString))
            {
                _logger.WriteLog($"File già processato (hard link): {filePath}", LogLevel.DEBUG);
                continue;
            }
            _processedHashes.Add(hashString);

            // Aggiunge intestazione
            string fileName = Path.GetFileName(filePath);
            string header = _fileNamesOnly
                ? $"### {fileName} ###"
                : $"### Contenuto di {fileName} ###";
            WriteOutputOrFile(header);

            if (!_fileNamesOnly)
            {
                _logger.WriteLog($"Aggiungendo contenuto di: {fileName}", LogLevel.INFO);

                try
                {
                    var lines = File.ReadAllLines(filePath);
                    if (_outputToConsole)
                    {
                        foreach (var line in lines)
                            Console.WriteLine(line);
                        Console.WriteLine();
                    }
                    else
                    {
                        File.AppendAllLines(_outputFile, lines);
                        File.AppendAllText(_outputFile, Environment.NewLine);
                    }
                    _logger.WriteLog($"File aggiunto correttamente: {fileName}", LogLevel.INFO);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"Attenzione: Impossibile leggere il file: {filePath}");
                    Console.ResetColor();

                    _logger.WriteLog($"Impossibile leggere il file: {filePath} - {ex.Message}", LogLevel.WARNING);
                }
            }
        }
    }

    public void MergeFile(string filePath)
    {
        // Logica di elaborazione per un singolo file, ad esempio:
        // - Calcolo hash per evitare duplicati
        // - Aggiunta di intestazione e contenuto
        // - Gestione delle eccezioni
        try
        {
            // Esempio: aggiunge l'intestazione
            string fileName = Path.GetFileName(filePath);
            string header = _fileNamesOnly ? $"### {fileName} ###" : $"### Contenuto di {fileName} ###";
            WriteOutputOrFile(header);

            if (!_fileNamesOnly)
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                    WriteOutputOrFile(line);
                // Aggiunge una riga vuota
                WriteOutputOrFile("");
            }
        }
        catch (Exception ex)
        {
            // Gestione dell'errore
            WriteOutputOrFile($"[ERROR: impossibile leggere {filePath} - {ex.Message}]");
            _logger.WriteLog($"Impossibile leggere il file: {filePath} - {ex.Message}", LogLevel.WARNING);
        }
    }
}

### Contenuto di FileOperationsService.cs ###
// FileOperationsService.cs (nel progetto Core)

using System;
using System.IO;
using System.Threading.Tasks;
using CombineFiles.Core.Helpers;

namespace CombineFiles.Core.Services;

public interface IFileOperationsService
{
    void Copy(string sourcePath, string destinationPath, bool isDirectory);
    void Move(string sourcePath, string destinationPath, bool isDirectory);
    void Rename(string currentPath, string newName, bool isDirectory);
    void Delete(string path, bool isDirectory);
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class FileOperationsService : IFileOperationsService
{
    public void Copy(string sourcePath, string destinationPath, bool isDirectory)
    {
        if (isDirectory)
        {
            FileSystemHelper.CopyDirectory(sourcePath, destinationPath, true);
        }
        else
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    public async Task<OperationResult> CopyAsync(string sourcePath, string destinationPath, bool isDirectory)
    {
        try
        {
            if (isDirectory) FileSystemHelper.CopyDirectory(sourcePath, destinationPath, true, true);
            else await Task.Run(() => File.Copy(sourcePath, destinationPath, true));
            return new OperationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, Message = ex.Message };
        }
    }

    public void Move(string sourcePath, string destinationPath, bool isDirectory)
    {
        if (isDirectory)
            Directory.Move(sourcePath, destinationPath);
        else
            File.Move(sourcePath, destinationPath);
    }

    public void Rename(string currentPath, string newName, bool isDirectory)
    {
        string parent = Path.GetDirectoryName(currentPath) ?? string.Empty;
        string newPath = Path.Combine(parent, newName);

        if (isDirectory)
            Directory.Move(currentPath, newPath);
        else
            File.Move(currentPath, newPath);
    }

    public void Delete(string path, bool isDirectory)
    {
        if (isDirectory)
            Directory.Delete(path, true);
        else
            File.Delete(path);
    }
}

### Contenuto di App.xaml ###
<Application x:Class="CombineFiles.ShellUi.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:CombineFiles.ShellUi"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
         
    </Application.Resources>
</Application>

### Contenuto di App.xaml.cs ###
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CombineFiles.ShellUi;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
}

### Contenuto di ContextMenuRegistration.cs ###
using System;
using Microsoft.Win32;

namespace CombineFiles.ShellUi;

public static class ContextMenuRegistration
{
    private const string MenuName = "*\\shell\\Copia con CombineFiles";
    private const string CommandName = "*\\shell\\Copia con CombineFiles\\command";

    public static void Register(string exePath)
    {
        try
        {
            using (var key = Registry.ClassesRoot.CreateSubKey(MenuName))
            {
                key.SetValue("", "Copia con CombineFiles");
            }

            using (var keyCmd = Registry.ClassesRoot.CreateSubKey(CommandName))
            {
                // %* permette di passare più file come argomenti in certe circostanze
                // Se riscontri problemi con la multi-selezione, valuta comandi COM
                string command = $"\"{exePath}\" \"%1\"";
                keyCmd.SetValue("", command);
            }
        }
        catch (Exception ex)
        {
            // Gestire l'eccezione
            throw;
        }
    }

    public static void Unregister()
    {
        try
        {
            Registry.ClassesRoot.DeleteSubKeyTree(MenuName, false);
        }
        catch
        {
            // Ignora errori se la chiave non esiste
        }
    }
}

### Contenuto di MainWindow.xaml ###
<Window x:Class="CombineFiles.ShellUi.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="CombineFilesApp" Height="400" Width="600">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="2*"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Lista dei file selezionati -->
        <ListBox x:Name="FilesListBox" 
                 SelectionMode="Single"
                 DisplayMemberPath="FileName"
                 Grid.Row="0"
                 Height="100"
                 SelectionChanged="FilesListBox_SelectionChanged" />

        <!-- Anteprima contenuto -->
        <TextBox x:Name="PreviewTextBox" 
                 Grid.Row="1" 
                 IsReadOnly="True" 
                 TextWrapping="Wrap" 
                 VerticalScrollBarVisibility="Auto" 
                 HorizontalScrollBarVisibility="Auto"/>

        <!-- Pulsante per copiare negli appunti -->
        <Button Content="Copia negli Appunti" 
                Grid.Row="2" 
                Margin="0,10,0,0" 
                Height="30" 
                Click="CopyToClipboard_Click"/>
    </Grid>
</Window>

### Contenuto di MainWindow.xaml.cs ###
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CombineFiles.Core;
using MessageBox = System.Windows.MessageBox;

namespace CombineFiles.ShellUi;

public partial class MainWindow : Window
{
    private List<FileItem> _fileItems = new List<FileItem>();

    public MainWindow()
    {
        InitializeComponent();

        // Leggi i parametri di riga di comando
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Any())
        {
            // Ottieni i file validi (riprendendo la logica da FileProcessor)
            var validFiles = FileProcessor.GetFilesToProcess(args);
            foreach (var vf in validFiles)
            {
                _fileItems.Add(new FileItem { FilePath = vf, FileName = Path.GetFileName(vf) });
            }

            FilesListBox.ItemsSource = _fileItems;
        }
        else
        {
            MessageBox.Show("Nessun file selezionato in input.", "CombineFilesApp", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesListBox.SelectedItem is not FileItem selectedItem) return;

        try
        {
            string content = File.ReadAllText(selectedItem.FilePath);
            // Esempio: se il file è JSON, prova a formattarlo
            

            PreviewTextBox.Text = content;
        }
        catch (Exception ex)
        {
            PreviewTextBox.Text = $"Impossibile leggere il file: {ex.Message}";
        }
    }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        // Esempio: unisce il contenuto di tutti i file e copia
        var files = _fileItems.Select(x => x.FilePath).ToList();
        string combined = FileProcessor.CombineContents(files);

        // Copia negli appunti (usa System.Windows.Clipboard o Windows.Forms.Clipboard)
        System.Windows.Clipboard.SetText(combined);

        MessageBox.Show("Contenuto copiato negli appunti!", "CombineFilesApp", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

/// <summary>
/// Classe di supporto per bindare file name e path in ListBox
/// </summary>
public class FileItem
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
}

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di App.xaml ###
<Application x:Class="CombineFilesWpf.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:CombineFilesWpf"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
         
    </Application.Resources>
</Application>

### Contenuto di App.xaml.cs ###
using System.Windows;

namespace CombineFilesWpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
}

### Contenuto di FileListControl.xaml ###
<UserControl x:Class="CombineFilesWpf.Controls.FileListControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:treeViewFileExplorer="clr-namespace:TreeViewFileExplorer;assembly=TreeViewFileExplorer"
             mc:Ignorable="d"
             Height="450" Width="800">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="200" />
        </Grid.RowDefinitions>

        <!-- Includi il controllo personalizzato -->
        <treeViewFileExplorer:TreeViewFileExplorerCustom x:Name="treeViewFileExplorer" Grid.Row="0" />

        <!-- ListBox per mostrare i file selezionati -->
        <ListBox ItemsSource="{Binding SelectedFiles, Source={x:Reference treeViewFileExplorer}}" 
                 Grid.Row="1" Margin="5">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Image Source="{Binding ImageSource}" Width="16" Height="16" Margin="0,0,5,0"/>
                        <TextBlock Text="{Binding Name}" />
                        <TextBlock Text=" - " />
                        <TextBlock Text="{Binding Path}" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </Grid>
</UserControl>

### Contenuto di FileListControl.xaml.cs ###
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.ViewModels;

namespace CombineFilesWpf.Controls;

public partial class FileListControl : UserControl
{
    // FileItems potrebbe non essere più necessario se gestito da TreeViewFileExplorerCustom
    // public ObservableCollection<FileItem> FileItems { get; set; }

    public ObservableCollection<FileItem> SelectedFiles { get; private set; } = new ObservableCollection<FileItem>();

    public FileListControl()
    {
        InitializeComponent();
        // Inizializzare i dati per il controllo TreeView personalizzato
        // radTreeListViewFiles.DataContext = FileItems; // Non necessario se gestito internamente
        // radTreeListViewFiles.radTreeView.SelectionChanged += RadTreeView_SelectionChanged; // Rimosso
    }

    // Metodo per aggiungere file
    public void AddFile(FileItem file)
    {
        // Potrebbe non essere necessario se TreeViewFileExplorerCustom gestisce l'aggiunta dei file
        // Se necessario, implementa un metodo nel TreeViewFileExplorerCustom per aggiungere file
    }

    // Metodo per resettare la lista
    public void ClearFiles()
    {
        // Potrebbe non essere necessario se TreeViewFileExplorerCustom gestisce la pulizia
        // Se necessario, implementa un metodo nel TreeViewFileExplorerCustom per pulire i file
    }

    // Evento sollevato quando SelectedFiles cambia nel TreeViewFileExplorerCustom
    private void TreeViewFileExplorer_SelectedFilesChanged(object sender, EventArgs e)
    {
        // Aggiorna la collezione SelectedFiles nel FileListControl
        SelectedFiles.Clear();
        foreach (var file in (treeViewFileExplorer.DataContext as TreeViewExplorerViewModel)?.SelectedFiles)
        {
            SelectedFiles.Add(file);
        }

        // Puoi aggiungere ulteriori logiche qui, ad esempio aggiornare una ListBox o altre UI
    }

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        treeViewFileExplorer.IsEnabled = isEnabled;
        // Altri controlli possono essere abilitati/disabilitati qui
    }
}

### Contenuto di FilterOptionsControl.xaml ###
<UserControl x:Class="CombineFilesWpf.Controls.FilterOptionsControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:telerik="http://schemas.telerik.com/2008/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <GroupBox Header="Opzioni di Filtro">
        <StackPanel Margin="5">
            <CheckBox x:Name="chkIncludeSubfolders" Content="Includi Sottocartelle" IsChecked="True" Margin="0,0,0,5"/>
            <CheckBox x:Name="chkExcludeHidden" Content="Escludi File Nascosti" IsChecked="True" Margin="0,0,0,5"/>
            <StackPanel Orientation="Horizontal" Margin="0,5">
                <Label Content="Estensioni da Includere (es: .txt;.cs):" Width="200"/>
                <telerik:RadWatermarkTextBox x:Name="txtIncludeExtensions" Width="400" WatermarkContent=".txt;.cs"/>
            </StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,5">
                <Label Content="Estensioni da Escludere:" Width="200"/>
                <telerik:RadWatermarkTextBox x:Name="txtExcludeExtensions" Width="400" WatermarkContent=".exe;.dll"/>
            </StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,5">
                <Label Content="Percorsi da Escludere:" Width="200"/>
                <telerik:RadWatermarkTextBox x:Name="txtExcludePaths" Width="400" WatermarkContent="C:\Temp;D:\Backup"/>
            </StackPanel>
        </StackPanel>
    </GroupBox>
</UserControl>

### Contenuto di FilterOptionsControl.xaml.cs ###
using System.Windows.Controls;

namespace CombineFilesWpf.Controls;

public partial class FilterOptionsControl : UserControl
{
    public FilterOptionsControl()
    {
        InitializeComponent();
    }

    // Proprietà per accedere ai controlli dal MainWindow
    public bool IncludeSubfolders => chkIncludeSubfolders.IsChecked == true;
    public bool ExcludeHidden => chkExcludeHidden.IsChecked == true;
    public string IncludeExtensions => txtIncludeExtensions.Text;
    public string ExcludeExtensions => txtExcludeExtensions.Text;
    public string ExcludePaths => txtExcludePaths.Text;

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        chkIncludeSubfolders.IsEnabled = isEnabled;
        chkExcludeHidden.IsEnabled = isEnabled;
        txtIncludeExtensions.IsEnabled = isEnabled;
        txtExcludeExtensions.IsEnabled = isEnabled;
        txtExcludePaths.IsEnabled = isEnabled;
    }
}

### Contenuto di OutputOptionsControl.xaml ###
<UserControl x:Class="CombineFilesWpf.Controls.OutputOptionsControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:telerik="http://schemas.telerik.com/2008/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <GroupBox Header="Opzioni di Output">
        <StackPanel Margin="5">
            <StackPanel Orientation="Horizontal" Margin="0,5">
                <Label Content="Cartella di Output:" Width="150"/>
                <telerik:RadWatermarkTextBox x:Name="txtOutputFolder" Width="400" WatermarkContent="Seleziona una cartella"/>
                <telerik:RadButton Content="Sfoglia..." Click="BtnBrowseOutputFolder_Click" Margin="5,0,0,0"/>
            </StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,5">
                <Label Content="Nome File di Output:" Width="150"/>
                <telerik:RadWatermarkTextBox x:Name="txtOutputFileName" Width="200" Text="merged_output.txt"/>
                <CheckBox x:Name="chkOneFilePerExtension" Content="Un File per Estensione" IsChecked="True" Margin="10,0,0,0"/>
            </StackPanel>
            <CheckBox x:Name="chkOverwriteFiles" Content="Sovrascrivi File Esistenti" Margin="0,5"/>
        </StackPanel>
    </GroupBox>
</UserControl>

### Contenuto di OutputOptionsControl.xaml.cs ###
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CombineFilesWpf.Controls;

public partial class OutputOptionsControl : UserControl
{
    public OutputOptionsControl()
    {
        InitializeComponent();
        txtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    private void BtnBrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            Title = "Seleziona la cartella di output",
            IsFolderPicker = true
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            txtOutputFolder.Text = dialog.FileName;
        }
    }

    // Proprietà per accedere ai controlli dal MainWindow
    public string OutputFolder => txtOutputFolder.Text;
    public string OutputFileName => txtOutputFileName.Text;
    public bool OneFilePerExtension => chkOneFilePerExtension.IsChecked == true;
    public bool OverwriteFiles => chkOverwriteFiles.IsChecked == true;

    // Metodo per disabilitare/abilitare i controlli
    public void ToggleControls(bool isEnabled)
    {
        txtOutputFolder.IsEnabled = isEnabled;
        txtOutputFileName.IsEnabled = isEnabled;
        chkOneFilePerExtension.IsEnabled = isEnabled;
        chkOverwriteFiles.IsEnabled = isEnabled;
    }
}

### Contenuto di MainWindow.xaml ###
<Window x:Class="CombineFilesWpf.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:telerik="http://schemas.telerik.com/2008/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:CombineFilesWpf.Controls"
        Title="File Merger" Height="700" Width="800">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <!-- Selezione Cartelle e File -->
            <RowDefinition Height="Auto"/>
            <!-- Opzioni di Filtro -->
            <RowDefinition Height="Auto"/>
            <!-- Opzioni di Output -->
            <RowDefinition Height="*"/>
            <!-- RadTreeView dei File -->
            <RowDefinition Height="Auto"/>
            <!-- Barra di Progresso -->
            <RowDefinition Height="Auto"/>
            <!-- Pulsanti di Controllo -->
        </Grid.RowDefinitions>

        <!-- Selezione Cartelle e File -->
        <StackPanel Orientation="Horizontal" Grid.Row="0" Margin="0,0,0,10">
            <telerik:RadButton x:Name="BtnAddFolder" Content="Aggiungi Cartella" Click="BtnAddFolder_Click" Margin="0,0,5,0"/>
            <telerik:RadButton x:Name="BtnAddFiles" Content="Aggiungi File" Click="BtnAddFiles_Click" Margin="0,0,5,0"/>
            <telerik:RadButton x:Name="BtnRemoveSelected" Content="Rimuovi Selezionati" Click="BtnRemoveSelected_Click" Margin="0,0,5,0"/>
            <telerik:RadButton x:Name="BtnClearList" Content="Svuota Lista" Click="BtnClearList_Click"/>
        </StackPanel>

        <!-- Opzioni di Filtro -->
        <controls:FilterOptionsControl x:Name="FilterOptions" Grid.Row="1" Margin="0,0,0,10"/>

        <!-- Opzioni di Output -->
        <controls:OutputOptionsControl x:Name="OutputOptions" Grid.Row="2" Margin="0,0,0,10"/>

        <!-- RadTreeView dei File -->
        <controls:FileListControl x:Name="FileList" Grid.Row="3" Margin="0,0,0,10"/>

        <!-- Barra di Progresso -->
        <StackPanel Orientation="Horizontal" Grid.Row="4" Margin="0,0,0,10">
            <telerik:RadProgressBar x:Name="progressBar" Width="600" Height="20"/>
            <Label x:Name="lblProgress" Content="0 / 0" Margin="10,0,0,0" VerticalAlignment="Center"/>
        </StackPanel>

        <!-- Pulsanti di Controllo -->
        <StackPanel Orientation="Horizontal" Grid.Row="5" HorizontalAlignment="Right">
            <telerik:RadButton Name="BtnStartMerging" Content="Avvia Merging" Click="BtnStartMerging_Click" Margin="0,0,5,0"/>
            <telerik:RadButton Name="BtnStopMerging" Content="Interrompi" Click="BtnStopMerging_Click" IsEnabled="False" Margin="0,0,5,0"/>
            <telerik:RadButton Name="BtnSaveConfig" Content="Salva Configurazione" Click="BtnSaveConfig_Click" Margin="0,0,5,0"/>
            <telerik:RadButton Name="BtnLoadConfig" Content="Carica Configurazione" Click="BtnLoadConfig_Click"/>
        </StackPanel>
    </Grid>
</Window>

### Contenuto di MainWindow.xaml.cs ###
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CombineFiles.Core;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;
using TreeViewFileExplorer.Model;

namespace CombineFilesWpf;

public partial class MainWindow : Window
{
    private CancellationTokenSource cts;

    public MainWindow()
    {
        InitializeComponent();
    }

    // Metodo per aggiungere cartelle (UI)
    private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
    {
        var folderDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true
        };

        if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            AddFilesFromFolder(folderDialog.FileName);
        }
    }

    // Metodo per aggiungere file (UI)
    private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona i file da includere",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                // Usa il helper per verificare se includere il file
                if (FileFilterHelper.ShouldIncludeFile(
                        file,
                        FilterOptions.ExcludeHidden,
                        FilterOptions.ExcludePaths,
                        FilterOptions.ExcludeExtensions,
                        FilterOptions.IncludeExtensions))
                {
                    var fileItem = new FileItem
                    {
                        IsSelected = true,
                        Path = file,
                        Name = Path.GetFileName(file),
                        IsFolder = false
                    };
                    FileList.AddFile(fileItem);
                }
            }
        }
    }

    // Metodo per aggiungere file dalla cartella selezionata (UI)
    private void AddFilesFromFolder(string folderPath)
    {
        var searchOption = FilterOptions.IncludeSubfolders
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        try
        {
            var files = Directory.GetFiles(folderPath, "*.*", searchOption);
            foreach (var file in files)
            {
                if (FileFilterHelper.ShouldIncludeFile(
                        file,
                        FilterOptions.ExcludeHidden,
                        FilterOptions.ExcludePaths,
                        FilterOptions.ExcludeExtensions,
                        FilterOptions.IncludeExtensions))
                {
                    var fileItem = new FileItem
                    {
                        IsSelected = true,
                        Path = file,
                        Name = Path.GetFileName(file),
                        IsFolder = false
                    };
                    FileList.AddFile(fileItem);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore nell'accesso alla cartella {folderPath}: {ex.Message}", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Pulsante per rimuovere i file selezionati (UI)
    private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (var file in FileList.SelectedFiles)
        {
            FileList.SelectedFiles.Remove(file);
        }
    }

    // Pulsante per svuotare la lista (UI)
    private void BtnClearList_Click(object sender, RoutedEventArgs e)
    {
        FileList.ClearFiles();
    }

    // Pulsante per avviare il merging (UI)
    private async void BtnStartMerging_Click(object sender, RoutedEventArgs e)
    {
        var filesToMerge = new List<FileItem>(FileList.SelectedFiles);
        if (filesToMerge.Count == 0)
        {
            MessageBox.Show("Nessun file selezionato per il merging.", "Attenzione", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string outputFolder = OutputOptions.OutputFolder;
        if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
        {
            MessageBox.Show("Percorso di output non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string outputFileName = OutputOptions.OutputFileName;
        if (string.IsNullOrWhiteSpace(outputFileName))
        {
            MessageBox.Show("Nome del file di output non valido.", "Errore", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        // Disabilita i controlli durante il merging
        ToggleControls(false);
        cts = new CancellationTokenSource();
        progressBar.Value = 0;
        progressBar.Maximum = filesToMerge.Count;
        lblProgress.Content = $"0 / {filesToMerge.Count}";

        try
        {
            await Task.Run(() => StartMerging(filesToMerge, outputFolder, outputFileName, cts.Token));
            MessageBox.Show("Merging completato con successo.", "Successo", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Merging interrotto dall'utente.", "Interrotto", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante il merging: {ex.Message}", "Errore", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Riabilita i controlli
            ToggleControls(true);
        }
    }

    // Metodo per eseguire il merging (da implementare secondo la logica specifica)
    private void StartMerging(List<FileItem> filesToMerge, string outputFolder, string outputFileName,
        CancellationToken token)
    {
        // Implementazione del merging...
    }

    // Metodo per disabilitare/abilitare i controlli (UI)
    private void ToggleControls(bool isEnabled)
    {
        Dispatcher.Invoke(() =>
        {
            BtnStartMerging.IsEnabled = isEnabled;
            BtnStopMerging.IsEnabled = !isEnabled;
            BtnAddFolder.IsEnabled = isEnabled;
            BtnAddFiles.IsEnabled = isEnabled;
            BtnRemoveSelected.IsEnabled = isEnabled;
            BtnClearList.IsEnabled = isEnabled;

            FilterOptions.ToggleControls(isEnabled);
            OutputOptions.ToggleControls(isEnabled);
            FileList.ToggleControls(isEnabled);
        });
    }

    // Pulsante per interrompere il merging (UI)
    private void BtnStopMerging_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    // Pulsante per salvare la configurazione (UI)
    private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
    {
        // Crea l'oggetto per la configurazione di ricerca
        var searchConfig = new FileSearchConfig
        {
            IncludeSubfolders = FilterOptions.IncludeSubfolders,
            ExcludeHidden = FilterOptions.ExcludeHidden,
            IncludeExtensions = FilterOptions.IncludeExtensions,
            ExcludeExtensions = FilterOptions.ExcludeExtensions,
            ExcludePaths = FilterOptions.ExcludePaths
        };

        // Crea l'oggetto per la configurazione di unione
        var mergeConfig = new FileMergeConfig
        {
            OutputFolder = OutputOptions.OutputFolder,
            OutputFileName = OutputOptions.OutputFileName,
            OneFilePerExtension = OutputOptions.OneFilePerExtension,
            OverwriteFiles = OutputOptions.OverwriteFiles
        };

        // Contenitore temporaneo per entrambe le configurazioni
        var configContainer = new
        {
            SearchConfig = searchConfig,
            MergeConfig = mergeConfig
        };

        var saveFileDialog = new SaveFileDialog
        {
            Title = "Salva Configurazione",
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                var json = JsonConvert.SerializeObject(configContainer, Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, json);
                MessageBox.Show("Configurazione salvata con successo.", "Successo", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il salvataggio della configurazione: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnLoadConfig_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Carica Configurazione",
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(openFileDialog.FileName);
                var configContainer = JsonConvert.DeserializeAnonymousType(json, new
                {
                    SearchConfig = new FileSearchConfig(),
                    MergeConfig = new FileMergeConfig()
                });

                // Aggiorna i campi di FilterOptions con i valori di SearchConfig
                FilterOptions.chkIncludeSubfolders.IsChecked = configContainer.SearchConfig.IncludeSubfolders;
                FilterOptions.chkExcludeHidden.IsChecked = configContainer.SearchConfig.ExcludeHidden;
                FilterOptions.txtIncludeExtensions.Text = configContainer.SearchConfig.IncludeExtensions;
                FilterOptions.txtExcludeExtensions.Text = configContainer.SearchConfig.ExcludeExtensions;
                FilterOptions.txtExcludePaths.Text = configContainer.SearchConfig.ExcludePaths;

                // Aggiorna i campi di OutputOptions con i valori di MergeConfig
                OutputOptions.txtOutputFolder.Text = configContainer.MergeConfig.OutputFolder;
                OutputOptions.txtOutputFileName.Text = configContainer.MergeConfig.OutputFileName;
                OutputOptions.chkOneFilePerExtension.IsChecked = configContainer.MergeConfig.OneFilePerExtension;
                OutputOptions.chkOverwriteFiles.IsChecked = configContainer.MergeConfig.OverwriteFiles;

                MessageBox.Show("Configurazione caricata con successo.", "Successo", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il caricamento della configurazione: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di BooleanToVisibilityConverter.cs ###
// BooleanToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TreeViewFileExplorer.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

### Contenuto di FileAttribute.cs ###
namespace TreeViewFileExplorer.Enums;

public enum FileAttribute : uint
{
    Directory = 16,
    File = 256
}

### Contenuto di IconSize.cs ###
namespace TreeViewFileExplorer.Enums;

public enum IconSize : short
{
    Small,
    Large
}

### Contenuto di ItemState.cs ###
namespace TreeViewFileExplorer.Enums;

public enum ItemState : short
{
    Undefined,
    Open,
    Close
}

### Contenuto di ItemType.cs ###
namespace TreeViewFileExplorer.Enums;

public enum ItemType
{
    Drive,
    Folder,
    File
}

### Contenuto di ShellAttribute.cs ###
using System;

namespace TreeViewFileExplorer.Enums;

[Flags]
public enum ShellAttribute : uint
{
    LargeIcon = 0,              // 0x000000000
    SmallIcon = 1,              // 0x000000001
    OpenIcon = 2,               // 0x000000002
    ShellIconSize = 4,          // 0x000000004
    Pidl = 8,                   // 0x000000008
    UseFileAttributes = 16,     // 0x000000010
    AddOverlays = 32,           // 0x000000020
    OverlayIndex = 64,          // 0x000000040
    Others = 128,               // Not defined, really?
    Icon = 256,                 // 0x000000100  
    DisplayName = 512,          // 0x000000200
    TypeName = 1024,            // 0x000000400
    Attributes = 2048,          // 0x000000800
    IconLocation = 4096,        // 0x000001000
    ExeType = 8192,             // 0x000002000
    SystemIconIndex = 16384,    // 0x000004000
    LinkOverlay = 32768,        // 0x000008000 
    Selected = 65536,           // 0x000010000
    AttributeSpecified = 131072 // 0x000020000
}

### Contenuto di EventAggregator.cs ###
using System;
using System.Collections.Generic;

namespace TreeViewFileExplorer.Events;

/// <summary>
/// Defines methods for subscribing and publishing events.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Subscribes to a specific event type.
    /// </summary>
    void Subscribe<TEvent>(Action<TEvent> action);

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    void Publish<TEvent>(TEvent eventToPublish);
}

public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<Action<object>>> _subscribers = new Dictionary<Type, List<Action<object>>>();

    public void Subscribe<TEvent>(Action<TEvent> action)
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<Action<object>>();
        }
        _subscribers[eventType].Add(e => action((TEvent)e));
    }

    public void Publish<TEvent>(TEvent eventToPublish)
    {
        var eventType = typeof(TEvent);
        if (_subscribers.ContainsKey(eventType))
        {
            foreach (var action in _subscribers[eventType])
            {
                action(eventToPublish);
            }
        }
    }
}

### Contenuto di Events.cs ###
namespace TreeViewFileExplorer.Events;

public class BeforeExploreEvent
{
    public string Path { get; }

    public BeforeExploreEvent(string path)
    {
        Path = path;
    }
}

public class AfterExploreEvent
{
    public string Path { get; }

    public AfterExploreEvent(string path)
    {
        Path = path;
    }
}

### Contenuto di Interop.cs ###
using System;
using System.Runtime.InteropServices;
using TreeViewFileExplorer.Structs;

namespace TreeViewFileExplorer;

public static class Interop
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(string path,
        uint attributes,
        out ShellFileInfo fileInfo,
        uint size,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr pointer);
}

### Contenuto di ShellManager.cs ###
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Structs;

namespace TreeViewFileExplorer.Manager;

/// <summary>
/// Provides methods to interact with the Windows Shell to retrieve icons.
/// </summary>
public class ShellManager
{
    /// <summary>
    /// Retrieves the icon associated with a file or directory.
    /// </summary>
    /// <param name="path">The path to the file or directory.</param>
    /// <param name="type">The type of the item (File or Folder).</param>
    /// <param name="iconSize">The desired icon size.</param>
    /// <param name="state">The state of the item (Open or Closed for folders).</param>
    /// <returns>An <see cref="Icon"/> representing the item's icon.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the icon cannot be retrieved.</exception>
    public Icon GetIcon(string path, ItemType type, IconSize iconSize, ItemState state)
    {
        ShellFileInfo fileInfo = default;
        try
        {
            uint attributes = (uint)(type == ItemType.Folder ? FileAttribute.Directory : FileAttribute.File);
            ShellAttribute flags = ShellAttribute.Icon | ShellAttribute.UseFileAttributes;

            if (type == ItemType.Folder && state == ItemState.Open)
            {
                flags |= ShellAttribute.OpenIcon;
            }

            flags |= iconSize == IconSize.Small ? ShellAttribute.SmallIcon : ShellAttribute.LargeIcon;

            fileInfo = new ShellFileInfo();
            uint size = (uint)Marshal.SizeOf(fileInfo);
            IntPtr result = NativeMethods.SHGetFileInfo(path, attributes, out fileInfo, size, flags);

            if (result == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to retrieve icon for path: {path}");
            }

            Icon icon = Icon.FromHandle(fileInfo.hIcon).Clone() as Icon;
            return icon;
        }
        catch (Exception ex)
        {
            //Logger.Error(ex, $"Exception occurred while retrieving icon for path: {path}");
            throw;
        }
        finally
        {

            NativeMethods.DestroyIcon(fileInfo.hIcon);
        }
    }

    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out ShellFileInfo psfi,
            uint cbFileInfo,
            ShellAttribute uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }
}

### Contenuto di FileItem.cs ###
using System.Collections.Generic;

namespace TreeViewFileExplorer.Model;

public class FileItem
{
    public bool IsSelected { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }
    public bool IsFolder { get; set; }
    public List<FileItem> Children { get; set; } = new List<FileItem>();
}

### Contenuto di RelayCommand.cs ###
using System;
using System.Windows.Input;

namespace TreeViewFileExplorer.Model;

/// <summary>
/// A command whose sole purpose is to relay its functionality to other objects.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    public RelayCommand(Action<object> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        if (execute == null)
            throw new ArgumentNullException(nameof(execute));
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc/>
    public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc/>
    public void Execute(object parameter) => _execute(parameter);

    /// <inheritdoc/>
    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}

### Contenuto di .NETFramework,Version=v4.7.2.AssemblyAttributes.cs ###
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

### Contenuto di FileSystemService.cs ###
// FileSystemService.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Service for interacting with the file system asynchronously.
/// </summary>
public class FileSystemService : IFileSystemService
{
    public FileSystemService()
    {
    }

    public async Task<IEnumerable<DirectoryInfo>> GetDirectoriesAsync(string path, bool showHiddenFiles, Regex filterRegex)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dir = new DirectoryInfo(path);
                var directories = dir.GetDirectories();

                if (!showHiddenFiles)
                {
                    directories = directories.Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
                }

                if (filterRegex != null)
                {
                    directories = directories.Where(d => filterRegex.IsMatch(d.Name)).ToArray();
                }

                return directories.OrderBy(d => d.Name);
            }
            catch
            {
                return Enumerable.Empty<DirectoryInfo>();
            }
        });
    }

    public async Task<IEnumerable<FileInfo>> GetFilesAsync(string path, bool showHiddenFiles, Regex filterRegex)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dir = new DirectoryInfo(path);
                var files = dir.GetFiles();

                if (!showHiddenFiles)
                {
                    files = files.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
                }

                if (filterRegex != null)
                {
                    files = files.Where(f => filterRegex.IsMatch(f.Name)).ToArray();
                }

                return files.OrderBy(f => f.Name);
            }
            catch
            {
                return Enumerable.Empty<FileInfo>();
            }
        });
    }

    public async Task<bool> IsAccessibleAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dir = new DirectoryInfo(path);
                return dir.Exists && !dir.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
            catch
            {
                return false;
            }
        });
    }
}

### Contenuto di IconCacheService.cs ###
using System;
using System.Collections.Concurrent;
using System.Windows.Media;
using TreeViewFileExplorer.Enums;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Provides caching functionality for file system icons.
/// </summary>
public class IconCacheService : IDisposable
{
    private readonly ConcurrentDictionary<string, WeakReference<ImageSource>> _iconCache;
    private bool _disposed;

    public IconCacheService()
    {
        _iconCache = new ConcurrentDictionary<string, WeakReference<ImageSource>>();
    }

    /// <summary>
    /// Gets an icon from the cache or returns null if not found.
    /// </summary>
    public ImageSource GetIcon(string path, ItemType type, IconSize size, ItemState state)
    {
        var cacheKey = GenerateCacheKey(path, type, size, state);

        if (_iconCache.TryGetValue(cacheKey, out var weakRef))
        {
            if (weakRef.TryGetTarget(out var icon))
            {
                //Logger.Trace($"Icon cache hit for {cacheKey}");
                return icon;
            }
            else
            {
                // Remove dead reference
                _iconCache.TryRemove(cacheKey, out _);
            }
        }

        return null;
    }

    /// <summary>
    /// Adds an icon to the cache.
    /// </summary>
    public void AddIcon(string path, ItemType type, IconSize size, ItemState state, ImageSource icon)
    {
        var cacheKey = GenerateCacheKey(path, type, size, state);
        _iconCache.TryAdd(cacheKey, new WeakReference<ImageSource>(icon));
    }

    /// <summary>
    /// Clears the icon cache.
    /// </summary>
    public void ClearCache()
    {
        _iconCache.Clear();
    }

    private static string GenerateCacheKey(string path, ItemType type, IconSize size, ItemState state)
    {
        return $"{type}_{size}_{state}_{path.ToLowerInvariant()}";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ClearCache();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

### Contenuto di IconService.cs ###
using System;
using System.Collections.Concurrent;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Manager;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Service for retrieving icons as ImageSource objects with caching.
/// </summary>
public class IconService : IIconService
{
    private readonly ShellManager _shellManager;
    private readonly ConcurrentDictionary<string, ImageSource> _iconCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="IconService"/> class.
    /// </summary>
    /// <param name="shellManager">An instance of <see cref="ShellManager"/>.</param>
    public IconService(ShellManager shellManager)
    {
        _shellManager = shellManager;
        _iconCache = new ConcurrentDictionary<string, ImageSource>();
    }

    /// <inheritdoc/>
    public ImageSource GetIcon(string path, ItemType type, IconSize size, ItemState state)
    {
        string cacheKey = $"{type}_{System.IO.Path.GetExtension(path).ToLower()}_{size}_{state}";

        if (_iconCache.TryGetValue(cacheKey, out ImageSource cachedIcon))
        {
            return cachedIcon;
        }

        try
        {
            using (var icon = _shellManager.GetIcon(path, type, size, state))
            {
                int width = size == IconSize.Small ? 16 : 32;
                int height = size == IconSize.Small ? 16 : 32;

                ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(width, height));

                _iconCache.TryAdd(cacheKey, imageSource);
                return imageSource;
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}

### Contenuto di IFileSystemService.cs ###
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Defines methods for accessing the file system asynchronously.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Asynchronously retrieves the directories at the specified path.
    /// </summary>
    Task<IEnumerable<DirectoryInfo>> GetDirectoriesAsync(string path, bool showHiddenFiles, Regex filterRegex);

    /// <summary>
    /// Asynchronously retrieves the files at the specified path.
    /// </summary>
    Task<IEnumerable<FileInfo>> GetFilesAsync(string path, bool showHiddenFiles, Regex filterRegex);

    /// <summary>
    /// Asynchronously checks if the specified path is accessible.
    /// </summary>
    Task<bool> IsAccessibleAsync(string path);
}

### Contenuto di IIconService.cs ###
using System.Windows.Media;
using TreeViewFileExplorer.Enums;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Defines methods for retrieving system icons.
/// </summary>
public interface IIconService
{
    /// <summary>
    /// Gets the icon for a file system item.
    /// </summary>
    ImageSource GetIcon(string path, ItemType type, IconSize size, ItemState state);
}

### Contenuto di FileSystemEventHandler.cs ###
using System;

namespace TreeViewFileExplorer.ShellClasses;

public class FileSystemEventHandler
{
    public void AttachEvents(IFileSystemObjectInfo fileSystemObject)
    {
        fileSystemObject.BeforeExplore += OnBeforeExplore;
        fileSystemObject.AfterExplore += OnAfterExplore;
    }

    private void OnBeforeExplore(object sender, EventArgs e)
    {
        // Handle before explore
    }

    private void OnAfterExplore(object sender, EventArgs e)
    {
        // Handle after explore
    }
}

### Contenuto di IFileSystemObjectInfo.cs ###
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;

namespace TreeViewFileExplorer.ShellClasses;

public interface IFileSystemObjectInfo
{
    ObservableCollection<IFileSystemObjectInfo> Children { get; }
    ImageSource ImageSource { get; }
    bool IsSelected { get; set; }
    bool IsExpanded { get; set; }
    FileSystemInfo FileSystemInfo { get; }
    EventHandler<EventArgs> BeforeExplore { get; set; }
    void Explore();
    event EventHandler<EventArgs> AfterExplore;
}

### Contenuto di ShellFileInfo.cs ###
using System;
using System.Runtime.InteropServices;

namespace TreeViewFileExplorer.Structs;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct ShellFileInfo
{
    public IntPtr hIcon;

    public int iIcon;

    public uint dwAttributes;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szDisplayName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string szTypeName;
}

### Contenuto di TreeViewFileExplorerCustom.xaml ###
<UserControl x:Class="TreeViewFileExplorer.TreeViewFileExplorerCustom"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:telerik="http://schemas.telerik.com/2008/xaml/presentation"
             xmlns:vm="clr-namespace:TreeViewFileExplorer.ViewModels"
             Height="450" Width="800">

    <!-- Resources -->
    <UserControl.Resources>
        <DataTemplate DataType="{x:Type vm:FileViewModel}">
            <StackPanel Orientation="Horizontal">
                <CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}" Margin="0,0,5,0"/>
                <Image Source="{Binding ImageSource}" Width="16" Height="16" Margin="0,0,5,0"/>
                <TextBlock Text="{Binding Name}" VerticalAlignment="Center"/>
            </StackPanel>
        </DataTemplate>

        <HierarchicalDataTemplate DataType="{x:Type vm:DirectoryViewModel}" ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal">
                <CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}" Margin="0,0,5,0"/>
                <Image Source="{Binding ImageSource}" Width="16" Height="16" Margin="0,0,5,0"/>
                <TextBlock Text="{Binding Name}" VerticalAlignment="Center"/>
            </StackPanel>
        </HierarchicalDataTemplate>

        <!-- Context Menu for Files -->
        <ContextMenu x:Key="FileContextMenu">
            <MenuItem Header="Apri" Command="{Binding OpenCommand}" />
            <MenuItem Header="Elimina" Command="{Binding DeleteCommand}" />
            <MenuItem Header="Rinomina" Command="{Binding RenameCommand}" />
            <MenuItem Header="Copia" Command="{Binding CopyCommand}" />
            <MenuItem Header="Sposta" Command="{Binding MoveCommand}" />
        </ContextMenu>

        <!-- Context Menu for Directories -->
        <ContextMenu x:Key="DirectoryContextMenu">
            <MenuItem Header="Apri" Command="{Binding OpenCommand}" />
            <MenuItem Header="Elimina" Command="{Binding DeleteCommand}" />
            <MenuItem Header="Rinomina" Command="{Binding RenameCommand}" />
            <MenuItem Header="Copia" Command="{Binding CopyCommand}" />
            <MenuItem Header="Sposta" Command="{Binding MoveCommand}" />
        </ContextMenu>
        <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
    </UserControl.Resources>

    <DockPanel>
        <!-- Toolbar -->
        <telerik:RadToolBar DockPanel.Dock="Top" Height="40">
            <telerik:RadButton Command="{Binding ToggleHiddenFilesCommand}" 
                                      ToolTip="Mostra/Nascondi File Nascosti" 
                                      Content="👁️" />
            <telerik:RadButton Command="{Binding ApplyFilterCommand}" 
                               ToolTip="Applica Filtro Regex" 
                               Content="🔍" />
            <telerik:RadButton Command="{Binding NavigateToFolderCommand}" 
                               ToolTip="Vai a Cartella" 
                               Content="📂" />
            <!-- Spinner di Caricamento -->
            <telerik:RadBusyIndicator IsBusy="{Binding IsLoading}" 
                                     Width="24" Height="24" 
                                     Margin="10,0,0,0"
                                     Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
                <telerik:RadBusyIndicator.BusyContent>
                    <TextBlock Text="Caricamento..." />
                </telerik:RadBusyIndicator.BusyContent>
            </telerik:RadBusyIndicator>
        </telerik:RadToolBar>

        <!-- TreeView -->
        <Grid>
            <!--<telerik:RadTreeView ItemsSource="{Binding RootItems}"
                                 Margin="5"
                                 IsLoadOnDemandEnabled="True"
                                 VirtualizingPanel.IsVirtualizing="True"
                                 VirtualizingPanel.VirtualizationMode="Recycling"
                                 ScrollViewer.IsDeferredScrollingEnabled="True"
                                 AllowDrop="True"
                                 DragOver="RadTreeView_DragOver"
                                 Drop="RadTreeView_Drop">
            <telerik:RadTreeView.ItemContainerStyle>
                    <Style TargetType="telerik:RadTreeViewItem">
                        <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}" />
                        <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}" />
                        <Setter Property="ContextMenu" Value="{StaticResource FileContextMenu}" />
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding}" Value="{x:Type vm:DirectoryViewModel}">
                                <Setter Property="ContextMenu" Value="{StaticResource DirectoryContextMenu}" />
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </telerik:RadTreeView.ItemContainerStyle>
            </telerik:RadTreeView>-->
        </Grid>
    </DockPanel>
</UserControl>

### Contenuto di TreeViewFileExplorerCustom.xaml.cs ###
// TreeViewFileExplorerCustom.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Manager;
using TreeViewFileExplorer.Services;
using TreeViewFileExplorer.ViewModels;

namespace TreeViewFileExplorer;

/// <summary>
/// Interaction logic for TreeViewFileExplorerCustom.xaml
/// </summary>
public partial class TreeViewFileExplorerCustom : UserControl
{
    private readonly IIconService _iconService;
    private readonly IFileSystemService _fileSystemService;
    private readonly TreeViewExplorerViewModel _viewModel;

    public TreeViewFileExplorerCustom() : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeViewFileExplorerCustom"/> class.
    /// </summary>
    /// <param name="iconService">Optional custom icon service.</param>
    /// <param name="fileSystemService">Optional custom file system service.</param>
    public TreeViewFileExplorerCustom(IIconService iconService = null, IFileSystemService fileSystemService = null)
    {
        InitializeComponent();

        // Iniettiamo le dipendenze o usiamo quelle di default
        var eventAggregator = new EventAggregator();
        var shellManager = new ShellManager();
        _iconService = iconService ?? new IconService(shellManager);
        _fileSystemService = fileSystemService ?? new FileSystemService(); // Inizializzazione corretta
        _viewModel = new TreeViewExplorerViewModel(_iconService, _fileSystemService, eventAggregator);
        DataContext = _viewModel;
    }

    private void RadTreeView_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (var path in droppedPaths)
        {
            if (!_fileSystemService.IsAccessibleAsync(path).Result)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
        }

        e.Effects = DragDropEffects.Move | DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void RadTreeView_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);

            var treeView = sender as Telerik.Windows.Controls.RadTreeView;
            var targetItem = treeView.InputHitTest(e.GetPosition(treeView)) as Telerik.Windows.Controls.RadTreeViewItem;

            if (targetItem != null)
            {
                var targetViewModel = targetItem.DataContext as IFileSystemObjectViewModel;
                if (targetViewModel != null && targetViewModel is DirectoryViewModel targetDirectory)
                {
                    foreach (var path in droppedPaths)
                    {
                        string fileName = System.IO.Path.GetFileName(path);
                        string destPath = System.IO.Path.Combine(targetDirectory.Path, fileName);

                        try
                        {
                            if (System.IO.Directory.Exists(path))
                            {
                                System.IO.Directory.Move(path, destPath);
                            }
                            else if (System.IO.File.Exists(path))
                            {
                                System.IO.File.Move(path, destPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Errore nello spostare {fileName}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    // Refresh la directory target
                    await targetDirectory.ExploreAsync();
                }
            }
        }
    }
}

### Contenuto di BaseFileSystemObjectViewModel.cs ###
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CombineFiles.Core;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Services;
// Per FileSystemHelper.IsValidName, se lo vuoi mantenere
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.Services;
using TreeViewFileExplorer.Views;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// Base class for file system object ViewModels.
/// </summary>
public abstract class BaseFileSystemObjectViewModel : IFileSystemObjectViewModel, INotifyPropertyChanged
{
    protected readonly IIconService IconService;
    protected readonly IFileSystemService FileSystemService;
    protected readonly IFileOperationsService FileOperationsService;

    /// <summary>
    /// Costruttore base per i ViewModel di file system (file e cartelle).
    /// </summary>
    protected BaseFileSystemObjectViewModel(
        IIconService iconService,
        IFileSystemService fileSystemService,
        IFileOperationsService fileOperationsService,
        bool showHiddenFiles,
        Regex filterRegex)
    {
        IconService = iconService;
        FileSystemService = fileSystemService;
        FileOperationsService = fileOperationsService;

        // Collezione di figli (file o directory)
        Children = new ObservableCollection<IFileSystemObjectViewModel>();

        // Comandi base
        OpenCommand = new RelayCommand(Open);
        DeleteCommand = new RelayCommand(Delete);
        RenameCommand = new RelayCommand(Rename);
        CopyCommand = new RelayCommand(Copy);
        MoveCommand = new RelayCommand(Move);
    }

    // Proprietà astratte da implementare in DirectoryViewModel e FileViewModel
    public abstract string Name { get; protected set; }
    public abstract string Path { get; protected set; }
    public abstract ImageSource ImageSource { get; protected set; }

    // Lista di figli (per le cartelle sarà la lista di file e sotto-cartelle)
    public ObservableCollection<IFileSystemObjectViewModel> Children { get; }

    // Gestione selezione/espansione (tipico di treeview in WPF)
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isExpanded;

        
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();

                // All'espansione, diamo il via all'esplorazione (lazy load)
                if (_isExpanded)
                {
                    // Chiamata asincrona (ignorata) per caricare i figli
                    ExploreAsync();
                }
            }
        }
    }

    // Comandi
    public ICommand OpenCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand MoveCommand { get; }

    // Metodi di business logici (copia, move, rename, delete) che usano IFileOperationsService
    protected void Copy(object parameter)
    {
        string destinationPath = PromptForDestinationPath();
        if (!string.IsNullOrWhiteSpace(destinationPath))
        {
            try
            {
                bool isDir = this is DirectoryViewModel;
                FileOperationsService.Copy(Path, System.IO.Path.Combine(destinationPath, Name), isDir);

                MessageBox.Show($"Copiato {Name} in {destinationPath}", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel copiare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    protected virtual void Move(object parameter)
    {
        string destinationPath = PromptForDestinationPath();
        if (!string.IsNullOrWhiteSpace(destinationPath))
        {
            try
            {
                bool isDir = this is DirectoryViewModel;
                FileOperationsService.Move(Path, System.IO.Path.Combine(destinationPath, Name), isDir);

                MessageBox.Show($"Spostato {Name} in {destinationPath}", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nello spostare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    protected void Rename(object parameter)
    {
        // Chiediamo all'utente il nuovo nome
        string newName = PromptForNewName();
        if (!string.IsNullOrWhiteSpace(newName) && FileSystemHelper.IsValidName(newName))
        {
            try
            {
                bool isDir = this is DirectoryViewModel;
                FileOperationsService.Rename(Path, newName, isDir);

                // Aggiorna le proprietà del ViewModel
                Name = newName;
                string parentDir = System.IO.Path.GetDirectoryName(Path);
                Path = System.IO.Path.Combine(parentDir, newName);

                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Path));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel rinominare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("Nome non valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected virtual void Delete(object parameter)
    {
        try
        {
            bool isDir = this is DirectoryViewModel;
            FileOperationsService.Delete(Path, isDir);

            // Se siamo riusciti a cancellare, rimuoviamo il riferimento dal genitore
            var parent = FindParentViewModel();
            if (parent != null)
            {
                parent.Children.Remove(this);
            }

            MessageBox.Show($"{Name} è stato eliminato con successo.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore nell'eliminare {Name}: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Invocato quando l'utente vuole "aprire" un file o una cartella (e.g. con doppio click)
    protected virtual void Open(object parameter)
    {
        try
        {
            // Per file apriamo con l'app predefinita, per cartelle apriamo Explorer, etc.
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // Qui se vuoi puoi avvisare l'utente o loggare
        }
    }

    /// <summary>
    /// Prompt che mostra all'utente un dialog per scegliere la cartella di destinazione (ad es. per Copy/Move).
    /// </summary>
    protected string PromptForDestinationPath()
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Seleziona la cartella di destinazione"
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            return dialog.FileName;
        }

        return string.Empty;
    }

    /// <summary>
    /// Chiede all'utente il nuovo nome (usato da Rename).
    /// </summary>
    protected string PromptForNewName()
    {
        var inputDialog = new InputDialog("Inserisci il nuovo nome:", "Rinomina");
        inputDialog.Owner = Application.Current.MainWindow; // Facoltativo, se vuoi modal su MainWindow

        if (inputDialog.ShowDialog() == true)
        {
            return inputDialog.ResponseText;
        }

        return string.Empty;
    }

    /// <summary>
    /// Se devi trovare il ViewModel genitore per rimuovere la tua istanza da Children, implementalo qui.
    /// O passa un riferimento di Parent nel costruttore, a seconda della tua struttura.
    /// </summary>
    private IFileSystemObjectViewModel FindParentViewModel()
    {
        // Logica a piacere: potresti salvare un reference a "Parent" nel costruttore,
        // oppure percorrere l'albero in qualche modo. Lascio un placeholder:
        return null;
    }

    /// <summary>
    /// Metodo astratto che (DirectoryViewModel) implementa per caricare file/ cartelle.
    /// </summary>
    public abstract Task ExploreAsync();

    // INotifyPropertyChanged per il binding
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Esempio di metodo helper per "filtrare" la visibilità in base a regole di UI
    /// (mostrare/nascondere file nascosti, applicare regex, ecc.).
    /// </summary>
    protected bool IsVisible(string name, bool isDirectory)
    {
        var mainViewModel = Application.Current.MainWindow.DataContext as TreeViewExplorerViewModel;
        if (mainViewModel == null)
            return true;

        // Se non vogliamo mostrare i file/cartelle nascoste
        if (!mainViewModel.ShowHiddenFiles && name.StartsWith("."))
            return false;

        // Se esiste una regex di filtro
        if (mainViewModel.FilterRegex != null && mainViewModel._regexFilter != null)
        {
            return mainViewModel._regexFilter.IsMatch(name);
        }

        return true;
    }
}

### Contenuto di BaseViewModel.cs ###
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TreeViewFileExplorer.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

### Contenuto di DirectoryViewModel.cs ###
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Services;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// ViewModel for directories.
/// </summary>
public class DirectoryViewModel : BaseFileSystemObjectViewModel
{
    private ImageSource _imageSource;
    private readonly IEventAggregator _eventAggregator;
    private readonly bool _showHiddenFiles;
    private readonly Regex _filterRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryViewModel"/> class.
    /// </summary>
    /// <param name="directoryInfo">Informazioni sulla directory (System.IO.DirectoryInfo).</param>
    /// <param name="iconService">Servizio per ottenere le icone.</param>
    /// <param name="fileSystemService">Servizio per l'accesso asincrono al file system.</param>
    /// <param name="fileOperationsService">Servizio che implementa le operazioni di copia/move/rename/delete.</param>
    /// <param name="eventAggregator">Event aggregator per comunicare Before/AfterExplore.</param>
    /// <param name="showHiddenFiles">Indica se mostrare i file/cartelle nascosti.</param>
    /// <param name="filterRegex">Regex di filtro (può essere null se non usato).</param>
    public DirectoryViewModel(
        DirectoryInfo directoryInfo,
        IIconService iconService,
        IFileSystemService fileSystemService,
        IFileOperationsService fileOperationsService,
        IEventAggregator eventAggregator,
        bool showHiddenFiles,
        Regex filterRegex)
        : base(iconService, fileSystemService, fileOperationsService, showHiddenFiles, filterRegex)
    {
        _eventAggregator = eventAggregator;
        _showHiddenFiles = showHiddenFiles;
        _filterRegex = filterRegex;

        // Impostiamo Nome e Path dal DirectoryInfo
        Name = directoryInfo.Name;
        Path = directoryInfo.FullName;

        // Icona "chiusa" di default
        _imageSource = iconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Close);

        // Aggiunge un "dummy" per permettere il caricamento lazy (espansione).
        Children.Add(new DummyViewModel());
    }

    public override string Name { get; protected set; }
    public override string Path { get; protected set; }

    public override ImageSource ImageSource
    {
        get => _imageSource;
        protected set
        {
            _imageSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Esegue l'esplorazione della directory, caricando la lista di sottocartelle e file.
    /// </summary>
    public override async Task ExploreAsync()
    {
        // Se c'è solo il DummyViewModel, significa che dobbiamo effettivamente caricare
        if (Children.Count == 1 && Children[0] is DummyViewModel)
        {
            // Pubblica l'evento "prima di esplorare"
            _eventAggregator.Publish(new BeforeExploreEvent(Path));

            Children.Clear();

            try
            {
                // Carichiamo le sub-directory
                var directories = await FileSystemService.GetDirectoriesAsync(Path, _showHiddenFiles, _filterRegex);
                foreach (var dir in directories)
                {
                    var dirViewModel = new DirectoryViewModel(
                        dir,
                        IconService,
                        FileSystemService,
                        FileOperationsService,
                        _eventAggregator,
                        _showHiddenFiles,
                        _filterRegex);

                    dirViewModel.PropertyChanged += OnChildPropertyChanged;
                    Children.Add(dirViewModel);
                }

                // Carichiamo i file
                var files = await FileSystemService.GetFilesAsync(Path, _showHiddenFiles, _filterRegex);
                foreach (var file in files)
                {
                    var fileViewModel = new FileViewModel(
                        file,
                        IconService,
                        FileSystemService,
                        _showHiddenFiles,
                        _filterRegex);

                    fileViewModel.PropertyChanged += OnChildPropertyChanged;
                    Children.Add(fileViewModel);
                }

                // Impostiamo l'icona "cartella aperta"
                ImageSource = IconService.GetIcon(Path, ItemType.Folder, IconSize.Small, ItemState.Open);
            }
            catch (Exception ex)
            {
                // Gestione eccezioni di caricamento (log, msg all'utente, ecc.)
                // Qui puoi usare un logger, o sollevare un evento. 
                // Per semplicità lasciamo vuoto.
            }
            finally
            {
                // Pubblica l'evento "dopo l'esplorazione"
                _eventAggregator.Publish(new AfterExploreEvent(Path));
            }
        }
    }

    /// <summary>
    /// Reagisce ai cambiamenti di proprietà dei figli (ad es. IsSelected).
    /// </summary>
    private void OnChildPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Se cambia IsSelected su un figlio, potremmo voler riflettere sul genitore
        // (dipende dalla tua logica).
        if (e.PropertyName == nameof(IsSelected))
        {
            // Esempio: aggiorniamo la notifica
            OnPropertyChanged(nameof(IsSelected));
        }
    }
}

### Contenuto di DummyViewModel.cs ###
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TreeViewFileExplorer.ViewModels;

public class DummyViewModel : IFileSystemObjectViewModel
{
    public ObservableCollection<IFileSystemObjectViewModel> Children => new ObservableCollection<IFileSystemObjectViewModel>();
    public ImageSource ImageSource => null;
    public string Name => string.Empty;
    public string Path => string.Empty;
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; }
    public Task ExploreAsync()
    {
        return default!;
    }
}

### Contenuto di FileSystemItemTemplateSelector.cs ###
using System.Windows;
using System.Windows.Controls;

namespace TreeViewFileExplorer.ViewModels;

public class FileSystemItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate FileTemplate { get; set; }
    public DataTemplate DirectoryTemplate { get; set; }
    public DataTemplate DummyTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is FileViewModel)
            return FileTemplate;
        if (item is DirectoryViewModel)
            return DirectoryTemplate;
        if (item is DummyViewModel)
            return DummyTemplate;
        return base.SelectTemplate(item, container);
    }
}

### Contenuto di FileViewModel.cs ###
// FileViewModel.cs
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Services;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// ViewModel for files.
/// </summary>
public class FileViewModel : BaseFileSystemObjectViewModel
{
    private readonly FileInfo _fileInfo;
    private ImageSource _imageSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileViewModel"/> class.
    /// </summary>
    public FileViewModel(FileInfo fileInfo, IIconService iconService, IFileSystemService fileSystemService, bool showHiddenFiles, Regex filterRegex)
        : base(iconService, fileSystemService, new FileOperationsService(), showHiddenFiles, filterRegex)
    {
        _fileInfo = fileInfo;
        Name = _fileInfo.Name;
        Path = _fileInfo.FullName;
        _imageSource = IconService.GetIcon(Path, ItemType.File, IconSize.Small, ItemState.Undefined);
    }

    public override string Name { get; protected set; }
    public override string Path { get; protected set; }


    public override ImageSource ImageSource
    {
        get => _imageSource;
        protected set
        {
            _imageSource = value;
            OnPropertyChanged();
        }
    }

    public override Task ExploreAsync()
    {
        return Task.CompletedTask;
    }

}

### Contenuto di IFileSystemObjectViewModel.cs ###
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TreeViewFileExplorer.ViewModels;

public interface IFileSystemObjectViewModel
{
    ObservableCollection<IFileSystemObjectViewModel> Children { get; }
    ImageSource ImageSource { get; }
    string Name { get; }
    string Path { get; }
    bool IsSelected { get; set; }
    bool IsExpanded { get; set; }
    Task ExploreAsync();
}

### Contenuto di TreeViewExplorerViewModel.cs ###
// TreeViewExplorerViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TreeViewFileExplorer.Events;
using TreeViewFileExplorer.Model;
using TreeViewFileExplorer.Services;

namespace TreeViewFileExplorer.ViewModels;

/// <summary>
/// ViewModel for the TreeView explorer.
/// </summary>
public class TreeViewExplorerViewModel : INotifyPropertyChanged
{
    private readonly IIconService _iconService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IEventAggregator _eventAggregator;

    public ObservableCollection<IFileSystemObjectViewModel> RootItems { get; set; }
    public ObservableCollection<FileItem> SelectedFiles { get; }

    // Proprietà per mostrare/nascondere file nascosti
    private bool _showHiddenFiles;
    public bool ShowHiddenFiles
    {
        get => _showHiddenFiles;
        set
        {
            if (_showHiddenFiles != value)
            {
                _showHiddenFiles = value;
                OnPropertyChanged(nameof(ShowHiddenFiles));
                RefreshRootItems();
            }
        }
    }

    // Proprietà per il filtro regex
    private string _filterRegex;
    public string FilterRegex
    {
        get => _filterRegex;
        set
        {
            if (_filterRegex != value)
            {
                _filterRegex = value;
                OnPropertyChanged(nameof(FilterRegex));
                ApplyFilter();
            }
        }
    }

    // Proprietà per la navigazione a una cartella specifica
    private string _navigatePath;
    public string NavigatePath
    {
        get => _navigatePath;
        set
        {
            if (_navigatePath != value)
            {
                _navigatePath = value;
                OnPropertyChanged(nameof(NavigatePath));
            }
        }
    }

    // Proprietà per lo stato di caricamento
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }
    }

    // Comandi
    public ICommand ToggleHiddenFilesCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand NavigateToFolderCommand { get; }

    // Regex Pattern
    internal Regex _regexFilter;

    public TreeViewExplorerViewModel(IIconService iconService, IFileSystemService fileSystemService, IEventAggregator eventAggregator)
    {
        _iconService = iconService;
        _fileSystemService = fileSystemService;
        _eventAggregator = eventAggregator;

        RootItems = new ObservableCollection<IFileSystemObjectViewModel>();
        SelectedFiles = new ObservableCollection<FileItem>();

        InitializeRootItems();

        ToggleHiddenFilesCommand = new RelayCommand(ToggleHiddenFiles);
        ApplyFilterCommand = new RelayCommand(ApplyFilterCommandExecute);
        NavigateToFolderCommand = new RelayCommand(NavigateToFolder);

        _regexFilter = null;
    }

    private void ToggleHiddenFiles(object parameter)
    {
        ShowHiddenFiles = !ShowHiddenFiles;
    }

    private void ApplyFilterCommandExecute(object parameter)
    {
        // Apri una finestra di dialogo per inserire la regex
        var inputDialog = new Views.InputDialog("Inserisci la regex per filtrare file e cartelle:", "Filtro Regex");
        if (inputDialog.ShowDialog() == true)
        {
            FilterRegex = inputDialog.ResponseText;
        }
    }

    private void NavigateToFolder(object parameter)
    {
        // Apri una finestra di dialogo per selezionare la cartella
        var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Seleziona una cartella"
        };

        if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
        {
            NavigatePath = dialog.FileName;
            NavigateToPath(NavigatePath);
        }
    }

    private async void NavigateToPath(string path)
    {
        IsLoading = true;
        RootItems?.Clear();
        RootItems ??= new ObservableCollection<IFileSystemObjectViewModel>();

        try
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists)
            {
                var dirViewModel = new DirectoryViewModel(directoryInfo, _iconService, _fileSystemService, null, _eventAggregator, ShowHiddenFiles, _regexFilter);
                dirViewModel.PropertyChanged += OnFileSystemObjectPropertyChanged;
                RootItems.Add(dirViewModel);
            }
            else
            {
                MessageBox.Show($"La cartella {path} non esiste.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore nel navigare alla cartella: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterRegex))
        {
            _regexFilter = null;
        }
        else
        {
            try
            {
                _regexFilter = new Regex(FilterRegex, RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Regex non valida: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                _regexFilter = null;
            }
        }
        RefreshRootItems();
    }

    private void RefreshRootItems()
    {
        IsLoading = true;
        RootItems.Clear();
        InitializeRootItems();
        IsLoading = false;
    }

    private void InitializeRootItems()
    {
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var dirViewModel = new DirectoryViewModel(drive.RootDirectory, _iconService, _fileSystemService, null, _eventAggregator, ShowHiddenFiles, _regexFilter);
            dirViewModel.PropertyChanged += OnFileSystemObjectPropertyChanged;
            RootItems.Add(dirViewModel);
        }
    }

    private void OnFileSystemObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IFileSystemObjectViewModel.IsSelected))
        {
            var item = sender as IFileSystemObjectViewModel;
            if (item != null)
            {
                if (item.IsSelected)
                {
                    AddToSelectedFiles(item);
                    _eventAggregator.Publish(new BeforeExploreEvent(item.Path));
                }
                else
                {
                    RemoveFromSelectedFiles(item);
                }
            }
        }
    }

    private void AddToSelectedFiles(IFileSystemObjectViewModel item)
    {
        if (item is FileViewModel file)
        {
            SelectedFiles.Add(new FileItem
            {
                Name = file.Name,
                Path = file.Path,
                IsFolder = false
            });
        }
        else if (item is DirectoryViewModel dir)
        {
            SelectedFiles.Add(new FileItem
            {
                Name = dir.Name,
                Path = dir.Path,
                IsFolder = true
            });
        }
        OnPropertyChanged(nameof(SelectedFiles));
    }

    private void RemoveFromSelectedFiles(IFileSystemObjectViewModel item)
    {
        var existingItem = SelectedFiles.FirstOrDefault(f => f.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase));
        if (existingItem != null)
        {
            SelectedFiles.Remove(existingItem);
            OnPropertyChanged(nameof(SelectedFiles));
        }
    }

    private void OnBeforeExplore(BeforeExploreEvent e)
    {
        // Gestisci l'evento prima dell'esplorazione
        IsLoading = true;
    }

    private void OnAfterExplore(AfterExploreEvent e)
    {
        // Gestisci l'evento dopo l'esplorazione
        IsLoading = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

### Contenuto di InputDialog.xaml ###
<Window x:Class="TreeViewFileExplorer.Views.InputDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Inserisci Nuovo Nome" Height="150" Width="400" WindowStartupLocation="CenterOwner" ResizeMode="NoResize">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <TextBlock Text="Inserisci il nuovo nome:" Grid.Row="0" Margin="0,0,0,5"/>
        <TextBox x:Name="InputTextBox" Grid.Row="1" Height="25"/>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
            <Button Content="OK" Width="75" Margin="0,0,5,0" IsDefault="True" Click="OkButton_Click"/>
            <Button Content="Annulla" Width="75" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>

### Contenuto di InputDialog.xaml.cs ###
using System.Windows;

namespace TreeViewFileExplorer.Views;

/// <summary>
/// Interaction logic for InputDialog.xaml
/// </summary>
public partial class InputDialog : Window
{
    public string ResponseText { get; private set; }

    public InputDialog(string question, string title = "Input")
    {
        InitializeComponent();
        this.Title = title;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        ResponseText = InputTextBox.Text;
        this.DialogResult = true;
    }
}

