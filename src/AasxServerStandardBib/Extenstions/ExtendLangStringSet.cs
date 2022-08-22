using AasCore.Aas3_0_RC02;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendLangStringSet
    {
        public static string GetDefaultString(this LangStringSet langStringSet, string defaultLang = null)
        {
            // start
            if (defaultLang == null)
                defaultLang = "en"; //Default Lang in old implementation is en

            string res = null;

            // search
            foreach (var langString in langStringSet.LangStrings)
                if (langString.Language.Equals(defaultLang, StringComparison.OrdinalIgnoreCase))
                    res = langString.Text;

            if (res == null && langStringSet.LangStrings.Count > 0)
                res = langStringSet.LangStrings[0].Text;

            // found?
            return res;
        }

        public static LangStringSet ConvertFromV20(this LangStringSet langStringSet, AasxCompatibilityModels.AdminShellV20.LangStringSet sourceLangStrings)
        {

            if (!sourceLangStrings.langString.IsNullOrEmpty())
            {
                langStringSet.LangStrings = new List<LangString>();
                foreach (var sourceLangString in sourceLangStrings.langString)
                {
                    var langString = new LangString(sourceLangString.lang, sourceLangString.str);
                    langStringSet.LangStrings.Add(langString);
                }
            }
            return langStringSet;
        }
    }
}
