
namespace ApiNoUi.Services;

public interface IFileParser
{
    Task ParseAsync(string filePath, string user = "");
}
