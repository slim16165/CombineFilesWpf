namespace CombineFilesWpf;

public class MergingConfig
{
    public bool IncludeSubfolders { get; set; }
    public bool ExcludeHidden { get; set; }
    public string IncludeExtensions { get; set; }
    public string ExcludeExtensions { get; set; }
    public string ExcludePaths { get; set; }
    public string OutputFolder { get; set; }
    public string OutputFileName { get; set; }
    public bool OneFilePerExtension { get; set; }
    public bool OverwriteFiles { get; set; }
}