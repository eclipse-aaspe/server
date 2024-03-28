using System.Threading.Tasks;
using AasxServerBlazor.TreeVisualisation;

namespace AasxServerBlazor.WebActions;

public interface IFileDownloader
{
    Task DownloadFile(TreeItem selectedNode);
}