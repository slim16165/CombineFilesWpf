using System;
using System.Collections.Generic;
using CombineFiles.Core.Services;

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
    public bool ListOnlyFileNames { get; set; }
    public string MinSize { get; set; }
    public string MaxSize { get; set; }
    public int MaxLinesPerFile { get; set; }      // 0 = senza limite di righe
    public int MaxTotalTokens { get; set; }       // 0 = senza limite di token
    public TokenLimitStrategy PartialFileMode { get; set; } = TokenLimitStrategy.IncludePartial; 

    public bool Debug { get; set; } // Abilita la modalità di debug dettagliata

    //starts full‑screen interactive wizard
    public bool Interactive { get; set; }

    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool Recurse { get; set; }
    public static string DefaultOutputFile { get; set; } = "CombinedFile.txt";
}