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