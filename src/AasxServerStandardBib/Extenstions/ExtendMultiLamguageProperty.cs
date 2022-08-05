using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendMultiLamguageProperty
    {
        public static string ValueAsText(this MultiLanguageProperty multiLanguageProperty)
        {
            //TODO: need to check/test again
            return "" + multiLanguageProperty.Value?.LangStrings.FirstOrDefault().Text;
        }
    }
}
