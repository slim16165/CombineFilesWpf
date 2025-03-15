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