using Microsoft.IdentityModel.Tokens;
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

        public string GetRestURL(Item item)
        {
            var extensionItem = item as ExtensionItem;
            if (extensionItem == null || extensionItem.restBaseURL == null || extensionItem.extension == null)
            {
                return null;
            }
            var extension = extensionItem.extension;
            var fragment = extension.GetFragment(item);
            return extensionItem.restBaseURL + "/fragments/" + extension.GetFragmentType(item) + "/" + Base64UrlEncoder.Encode(fragment);
        }
        public virtual string GetDownloadLink(Item item)
        {
            return null;
        }
        public virtual bool RepresentsFileToBeBrowsed(Item item)
        {
            return false;
        }
    }
}
