using System;
using System.Collections.Generic;
using CombineFiles.Core.Configuration;
using CombineFiles.ConsoleApp.Helpers;

namespace CombineFiles.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Se non vengono passati argomenti, usiamo valori di default per testare il comportamento
            CombineFilesOptions options = new CombineFilesOptions();
            if (args.Length == 0)
            {
                options.Preset = "CSharp";
                options.Mode = "extensions";
                options.Extensions = new List<string> { ".cs", ".xaml" };
                options.Recurse = true;
                options.EnableLog = true;
                // Altre proprietà (ExcludePaths, OutputFile, etc.) possono essere impostate dal preset
            }
            else
            {
                // Qui puoi implementare un semplice parser degli argomenti
                // Ad esempio, potresti interpretare il primo argomento come -Preset e così via.
                // Per semplicità, usiamo i default se non sviluppi un parser completo.
                options.Preset = args[0];
            }

            CombineFilesApp.Execute(options);

            Console.WriteLine("Fine elaborazione");
        }
    }
}