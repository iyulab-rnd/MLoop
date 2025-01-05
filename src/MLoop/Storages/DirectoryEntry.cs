namespace MLoop.Storages;

public class DirectoryEntry
{
    public string Name { get; set; }
    public string Path { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDirectory { get; set; }

    public DirectoryEntry(string name, string path, long size, DateTime lastModified, bool isDirectory)
    {
        Name = name;
        Path = path;
        Size = size;
        LastModified = lastModified;
        IsDirectory = isDirectory;
    }
}