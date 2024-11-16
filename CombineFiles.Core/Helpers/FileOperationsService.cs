// FileOperationsService.cs (nel progetto Core)

using System;
using System.IO;
using System.Threading.Tasks;

namespace CombineFiles.Core.Helpers;

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