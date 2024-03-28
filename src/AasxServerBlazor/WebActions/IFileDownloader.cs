using System.Threading.Tasks;
using AasxServerBlazor.TreeVisualisation;

namespace AasxServerBlazor.WebActions;

/// <summary>
/// Represents a service responsible for downloading files.
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    /// Downloads a file specified by the provided <paramref name="selectedNode"/>.
    /// </summary>
    /// <param name="selectedNode">The tree item representing the file to download.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DownloadFile(TreeItem selectedNode);
}