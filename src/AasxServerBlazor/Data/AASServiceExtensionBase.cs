using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AasxServerBlazor.Pages.TreePage;

namespace AasxServerBlazor.Data
{
    public abstract class AASServiceExtensionBase
    {

        protected ExtensionItem CreateItem(Item parent, string text, object tag, string type = null, string fileRestUrl = null)
        {
            var item = new ExtensionItem();
            item.envIndex = parent.envIndex;
            item.parent = parent;
            item.Text = text;
            item.Tag = tag;
            item.Type = type;
            item.extension = this as IAASServiceExtension;
            if (fileRestUrl != null)
            {
                item.restBaseURL = fileRestUrl;
            }
            else if (parent is ExtensionItem)
            {
                item.restBaseURL = (parent as ExtensionItem).restBaseURL;
            }

            item.Childs = CreateChildren(item, tag);

            return item;
        }

        protected abstract List<Item> CreateChildren(Item item, object tag);

    }
}
