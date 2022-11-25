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
        public abstract void CreateItems(Item fileItem, File file);
    }
}
