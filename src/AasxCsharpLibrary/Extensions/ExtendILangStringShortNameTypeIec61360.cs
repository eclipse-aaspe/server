/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class ExtendILangStringShortNameTypeIec61360
    {
        public static string? GetDefaultString(this List<ILangStringShortNameTypeIec61360> langStringSet, string defaultLang = null)
        {
            // start
            if (defaultLang == null)
                defaultLang = "en"; //Default Lang in old implementation is en

            string? res = null;

            // search
            foreach (var langString in langStringSet)
                if (langString.Language.Equals(defaultLang, StringComparison.OrdinalIgnoreCase))
                    res = langString.Text;

            if (res == null && langStringSet.Count > 0)
                res = langStringSet[0].Text;

            // found?
            return res;
        }
        public static List<ILangStringShortNameTypeIec61360> ConvertFromV20(
            this List<ILangStringShortNameTypeIec61360> lss,
            AasxCompatibilityModels.AdminShellV20.LangStringSetIEC61360 src)
        {
            lss = new List<ILangStringShortNameTypeIec61360>();
            if (src != null && src.Count != 0)
            {
                foreach (var sourceLangString in src)
                {
                    //Remove ? in the end added by AdminShellV20, to avoid verification error
                    string? lang = sourceLangString.lang;
                    if (!string.IsNullOrEmpty(sourceLangString.lang) && sourceLangString.lang.EndsWith("?"))
                    {
                        lang = sourceLangString.lang.Remove(sourceLangString.lang.Length - 1);
                    }
                    var langString = new LangStringShortNameTypeIec61360(lang, sourceLangString.str);
                    lss.Add(langString);
                }
            }
            else
            {
                //set default preferred name
                lss.Add(new LangStringShortNameTypeIec61360("en", ""));
            }
            return lss;
        }
    }
}
