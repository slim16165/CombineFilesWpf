using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CombineFiles.ConsoleApp.Extensions;
using CombineFiles.ConsoleApp.Helpers;

namespace CombineFiles.ConsoleApp;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Attiva il debugger solo se presente -d o -debug tra gli argomenti
        if (args.Any(a => a.Equals("-d", System.StringComparison.OrdinalIgnoreCase) || a.Equals("--debug", System.StringComparison.OrdinalIgnoreCase)))
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();
        }
        
        // 1) Creiamo il RootCommand con il builder dedicato
        var rootCommand = RootCommandBuilder.CreateRootCommand();

        // 2) Controlla collisioni e rimuove alias duplicati (invece di lanciare eccezioni)
        ParameterHelper.CheckAliasCollisions(
            rootCommand,
            skipConflictingAliases: true /* se false => lancia eccezione */
        );

        // 3) Eseguiamo direttamente il comando (System.CommandLine 2.0+)
        return rootCommand.Parse(args).Invoke();
    }
}