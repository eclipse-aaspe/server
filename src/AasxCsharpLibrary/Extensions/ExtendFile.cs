/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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

namespace Extensions
{
    public static class ExtendFile
    {
        public static string? ValueAsText(this File file)
        {
            return "" + file.Value;
        }

        public static void Set(this File file,
            string? contentType = "", string value = "")
        {
            file.ContentType = contentType;
            file.Value = value;
        }

        public static File ConvertFromV10(this File file, AasxCompatibilityModels.AdminShellV10.File sourceFile)
        {
            file.ContentType = sourceFile.mimeType;
            file.Value = sourceFile.value;
            return file;
        }
        public static File ConvertFromV20(this File file, AasxCompatibilityModels.AdminShellV20.File sourceFile)
        {
            file.ContentType = sourceFile.mimeType;
            file.Value = sourceFile.value;
            return file;
        }

        public static File UpdateFrom(this File elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is Property srcProp)
            {
                elem.Value = srcProp.Value;
            }

            if (source is AasCore.Aas3_0.Range srcRng)
            {
                elem.Value = srcRng.Min;
            }

            if (source is MultiLanguageProperty srcMlp)
            {
                elem.Value = "" + srcMlp.Value?.GetDefaultString();
            }

            if (source is File srcFile)
            {
                elem.Value = "" + srcFile.Value;
            }

            return elem;
        }

    }
}
