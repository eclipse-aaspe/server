using System.Collections.Generic;

namespace Extensions
{
    public static class ExtendILangStringDefinitionTypeIec61360
    {
        public static List<ILangStringDefinitionTypeIec61360> ConvertFromV20(
            this List<ILangStringDefinitionTypeIec61360> lss,
            AasxCompatibilityModels.AdminShellV20.LangStringSetIEC61360 src)
        {
            lss = new List<ILangStringDefinitionTypeIec61360>();
            //if (!sourceLangStrings.langString.IsNullOrEmpty())
            if (src != null && src.Count != 0)
            {
                foreach (var sourceLangString in src)
                {
                    //Remove ? in the end added by AdminShellV20, to avoid verification error
                    string lang = sourceLangString.lang;
                    if (!string.IsNullOrEmpty(sourceLangString.lang) && sourceLangString.lang.EndsWith("?"))
                    {
                        lang = sourceLangString.lang.Remove(sourceLangString.lang.Length - 1);
                    }
                    var langString = new LangStringDefinitionTypeIec61360(lang, sourceLangString.str);
                    lss.Add(langString);
                }
            }
            else
            {
                //set default preferred name
                lss.Add(new LangStringDefinitionTypeIec61360("en", ""));
            }
            return lss;
        }
    }
}
