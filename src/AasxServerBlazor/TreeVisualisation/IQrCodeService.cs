namespace AasxServerBlazor.TreeVisualisation
{
    public interface IQrCodeService
    {
        /// <summary>
        /// Gets the QR code link for the specified <paramref name="treeItem"/>.
        /// </summary>
        /// <param name="treeItem">The tree item to generate the QR code link for.</param>
        /// <returns>The QR code link.</returns>
        string GetQrCodeLink(TreeItem treeItem);

        /// <summary>
        /// Gets the QR code image for the specified <paramref name="treeItem"/>.
        /// </summary>
        /// <param name="treeItem">The tree item to get the QR code image for.</param>
        /// <returns>The base64 encoded QR code image.</returns>
        string GetQrCodeImage(TreeItem treeItem);

        /// <summary>
        /// Creates a QR code image for the specified <paramref name="treeItem"/>.
        /// </summary>
        /// <param name="treeItem">The tree item to create the QR code image for.</param>
        void CreateQrCodeImage(TreeItem treeItem);
    }
}