using System;
using System.IO;
using System.Threading.Tasks;
using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using Microsoft.JSInterop;

namespace AasxServerBlazor.WebActions
{
    public class FileDownloader : IFileDownloader
    {
        private readonly IJSRuntime _jsRuntime;

        public FileDownloader(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        public async Task DownloadFile(TreeItem selectedNode)
        {
            if (selectedNode == null)
            {
                throw new ArgumentNullException(nameof(selectedNode));
            }

            if (selectedNode.Tag is not AasCore.Aas3_0.File file)
            {
                throw new ArgumentException("Invalid tree item tag", nameof(selectedNode));
            }

            var fileName = Path.GetFileName(file.Value);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            try
            {
                var data = GetDataFromStream(selectedNode, file);
                await _jsRuntime.InvokeAsync<object>("saveAsFile", fileName, data);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error while trying to save file: {exception.Message}");
                Console.WriteLine($"While trying to save Tree Item: {selectedNode}");
            }
        }

        private static byte[] GetDataFromStream(TreeItem selectedNode, IFile file)
        {
            using var memoryStream = new MemoryStream();
            Program.env[selectedNode.EnvironmentIndex].GetLocalStreamFromPackage(file.Value).CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}