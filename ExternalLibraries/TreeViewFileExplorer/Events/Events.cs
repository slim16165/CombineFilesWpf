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