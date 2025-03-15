using System;
using CombineFiles.Core;
using CombineFiles.Core.Configuration;
using CombineFiles.Core.Helpers;
using CombineFiles.Core.Infrastructure;

namespace CombineFiles.ConsoleApp.Helpers
{
    public static class ParameterHelper
    {
        /// <summary>
        /// Verifica se l’utente ha richiesto l’help (es. se hai un bool Help in CombineFilesOptions).
        /// </summary>
        public static bool CheckHelp(CombineFilesOptions options)
        {
            return options.Help;
        }

        /// <summary>
        /// Stampa il messaggio di help.
        /// </summary>
        public static void PrintHelp()
        {
            Console.WriteLine("Uso: CombineFiles [--preset <PresetName>] [--mode <list|extensions|regex|InteractiveSelection>] ...");
            Console.WriteLine("Parametri disponibili:");
            Console.WriteLine("  - ListPresets: elenca i preset disponibili");
            Console.WriteLine("  - Mode: list, extensions, regex, InteractiveSelection");
            Console.WriteLine("  - FileList: elenco file in modalità 'list'");
            Console.WriteLine("  - Extensions: estensioni in modalità 'extensions'");
            Console.WriteLine("  - RegexPatterns: pattern in modalità 'regex'");
            Console.WriteLine("  - OutputFile: specifica il file di destinazione (default: CombinedFile.txt)");
            Console.WriteLine("  - OutputToConsole: scrive l'output a schermo invece che su file");
            Console.WriteLine("  - Recurse: ricerca ricorsiva nelle sottocartelle");
            Console.WriteLine("  - ExcludePaths, ExcludeFiles, ExcludeFilePatterns: filtri di esclusione");
            Console.WriteLine("  - MinSize, MaxSize, MinDate, MaxDate: filtri aggiuntivi");
            Console.WriteLine("  - FileNamesOnly: se True, scrive solo i nomi dei file invece che i contenuti");
            // etc.
        }

        /// <summary>
        /// Stampa la lista dei preset disponibili e termina.
        /// </summary>
        public static void PrintPresetList()
        {
            ConsoleHelper.WriteColored("Preset disponibili:", ConsoleColor.Cyan);
            foreach (var pName in PresetManager.Presets.Keys)
                Console.WriteLine($"- {pName}");
        }

        /// <summary>
        /// Esegue validazioni base su parametri essenziali, in modo simile allo script PS1.
        /// </summary>
        public static bool ValidateParameters(CombineFilesOptions options, Logger logger)
        {
            // Esempio in ValidateParameters:
            if ((options.Mode?.Equals("list", StringComparison.OrdinalIgnoreCase) ?? false)
                && (options.FileList == null || options.FileList.Count == 0))
            {
                logger.WriteLog("La modalità 'list' richiede almeno un FileList.", "ERROR");
                ConsoleHelper.WriteColored("Errore: Modalità 'list' -> -FileList mancante o vuota.", ConsoleColor.Red);
                return false;
            }


            // Se Mode è 'extensions', devi avere almeno un'estensione
            if ((options.Mode?.Equals("extensions", StringComparison.OrdinalIgnoreCase) ?? false)
                && (options.Extensions == null || options.Extensions.Count == 0))
            {
                logger.WriteLog("La modalità 'extensions' richiede -Extensions.", "ERROR");
                ConsoleHelper.WriteColored("Errore: Modalità 'extensions' -> -Extensions mancante o vuota.", ConsoleColor.Red);
                return false;
            }

            // Se Mode è 'regex', devi avere almeno un pattern
            if ((options.Mode?.Equals("regex", StringComparison.OrdinalIgnoreCase) ?? false)
                && (options.RegexPatterns == null || options.RegexPatterns.Count == 0))
            {
                logger.WriteLog("La modalità 'regex' richiede -RegexPatterns.", "ERROR");
                ConsoleHelper.WriteColored("Errore: Modalità 'regex' -> -RegexPatterns mancante o vuota.", ConsoleColor.Red);
                return false;
            }

            return true;
        }
    }
}