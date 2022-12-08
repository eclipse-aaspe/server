using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;

namespace AasxServerBlazor.Data
{
    /**
     * An interface that needs to be implemented by extensions that allow to browse into files
     * in order to show the file contents within the Tree/TreePage.
     */
    public interface IAASServiceExtension
    {
        /**
         * Whether this extension supports browsing into the given file.
         */
        public bool IsSuitableFor(File file);
        /**
         * Recursively generates the child items for the given file and adds them to the given fileItem.
         */
        public abstract void CreateItems(Item fileItem, System.IO.Stream fileStream, string fileRestURL);

        /**
         * Method 'TreePage.ViewNodeID(...)' will delegate to this implementation for items created by the specific extension.
         */
        public string ViewNodeID(Item item);

        /**
         * Method 'TreePage.ViewNodeType(...)' will delegate to this implementation for items created by the specific extension.
         */
        public string ViewNodeType(Item item);

        /**
         * Method 'TreePage.ViewNodeDetails(...)' will delegate to this implementation for items created by the specific extension.
         */
        public string ViewNodeDetails(Item item, int line, int col);

        /**
         * Method 'TreePage.ViewNodeInfo(...)' will delegate to this implementation for items created by the specific extension.
         */
        public string ViewNodeInfo(Item item);

        /**
         * Return the <fragment-type> to use in REST calls.
         */
        public string GetFragmentType(Item item);

        /**
         * Get the <fragment> to use in REST calls.
         */
        public string GetFragment(Item item);

        /**
         * Get the REST URL that can be used to retrieve the element that this item represents.
         */
        public string GetRestURL(Item item);

        /**
         * If the item represents a file (and if the extension supports file retrieval), returns a URL that can be used to retrieve the file; otherwise returns null.
         * This will probably be something like <GetRestURL(item)>?content=raw.
         */
        public string GetDownloadLink(Item item);

        /**
         * Whether the given item created by an extension represents a File that should be browsed deeper (possibly by another extension).
         */
        public bool RepresentsFileToBeBrowsed(Item item);
    }
}
