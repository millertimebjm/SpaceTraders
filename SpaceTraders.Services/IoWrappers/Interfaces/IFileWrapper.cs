namespace SpaceTraders.Services.IoWrappers.Interfaces;

public interface IFileWrapper
{
    Task WriteAllLinesAsync(string path, string[] lines);
    Task WriteAllLinesAsync(string path, IEnumerable<string> lines);
    Task<string[]> ReadAllLinesAsync(string path);
}