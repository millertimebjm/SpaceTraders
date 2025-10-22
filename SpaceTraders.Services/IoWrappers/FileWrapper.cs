using SpaceTraders.Services.IoWrappers.Interfaces;

namespace SpaceTraders.Services.IoWrappers;

public class FileWrapper : IFileWrapper
{
    public async Task<string[]> ReadAllLinesAsync(string path)
    {
        return await File.ReadAllLinesAsync(path);
    }

    public async Task WriteAllLinesAsync(string path, string[] lines)
    {
        await File.WriteAllLinesAsync(path, lines);
    }

    public async Task WriteAllLinesAsync(string path, IEnumerable<string> lines)
    {
        await File.WriteAllLinesAsync(path, lines);
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }
}