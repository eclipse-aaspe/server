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

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AasOpcUaServer
{
    public class AasUaUtils
    {
        public static string ToOpcUaName(string input)
        {
            var clean = Regex.Replace(input, @"[^a-zA-Z0-9\-_]", "_");
            while (true)
            {
                var len0 = clean.Length;
                clean = clean.Replace("__", "_");
                if (len0 == clean.Length)
                    break;
            }
            return clean;
        }

        public static string ToOpcUaReference(Reference refid)
        {
            if (refid == null || refid.Keys == null || refid.Keys.Count == 0)
                return null;

            var semstr = "";
            foreach (var k in refid.Keys)
            {
                if (semstr != "")
                    semstr += ",";
                semstr += String.Format("({0}){1}",
                            k.Type, k.Value);
            }

            return semstr;
        }


        public static List<string> ToOpcUaReferenceList(Reference refid)
        {
            if (refid == null || refid.Keys == null || refid.Keys.Count == 0)
                return null;

            var res = new List<string>();
            foreach (var k in refid.Keys)
            {
                res.Add(String.Format("{0}", k.Value));
            }

            return res;
        }

        public static LocalizedText[] GetUaLocalizedTexts(List<ILangStringTextType> ls)
        {
            if (ls == null || ls.Count < 1)
                return new LocalizedText[] { new LocalizedText("", "") };
            var res = new LocalizedText[ls.Count];
            for (int i = 0; i < ls.Count; i++)
                res[i] = new LocalizedText(ls[i].Language, ls[i].Text);
            return res;
        }

        public static LocalizedText GetBestUaDescriptionFromAasDescription(List<ILangStringTextType> desc)
        {
            var res = new LocalizedText("", "");
            if (desc != null && desc != null)
            {
                var found = false;
                foreach (var ls in desc)
                    if (!found || ls.Language.Trim().ToLower().StartsWith("en"))
                    {
                        found = true;
                        res = new LocalizedText(ls.Language, ls.Text);
                    }
            }
            return res;
        }

        public static bool AasValueTypeToUaDataType(string valueType, out Type sharpType, out NodeId dataTypeId)
        {
            // defaults
            sharpType = "".GetType();
            dataTypeId = DataTypeIds.String;
            if (valueType == null)
                return false;

            // parse
            var vt = valueType.ToLower().Trim();
            if (vt == "boolean")
            {
                sharpType = typeof(bool);
                dataTypeId = DataTypeIds.Boolean;
                return true;
            }
            else if (vt == "datetime" || vt == "datetimestamp" || vt == "time")
            {
                sharpType = typeof(Int64);
                dataTypeId = DataTypeIds.DateTime;
                return true;
            }
            else if (vt == "decimal" || vt == "integer" || vt == "long" || vt == "nonpositiveinteger" || vt == "negativeinteger")
            {
                sharpType = typeof(Int64);
                dataTypeId = DataTypeIds.Int64;
                return true;
            }
            else if (vt == "int")
            {
                sharpType = typeof(Int32);
                dataTypeId = DataTypeIds.Int32;
                return true;
            }
            else if (vt == "short")
            {
                sharpType = typeof(Int16);
                dataTypeId = DataTypeIds.Int16;
                return true;
            }
            else if (vt == "byte")
            {
                sharpType = typeof(SByte);
                dataTypeId = DataTypeIds.Byte;
                return true;
            }
            else if (vt == "nonnegativeinteger" || vt == "positiveinteger" || vt == "unsignedlong")
            {
                sharpType = typeof(UInt64);
                dataTypeId = DataTypeIds.UInt64;
                return true;
            }
            else if (vt == "unsignedint")
            {
                sharpType = typeof(UInt32);
                dataTypeId = DataTypeIds.UInt32;
                return true;
            }
            else if (vt == "unsignedshort")
            {
                sharpType = typeof(UInt16);
                dataTypeId = DataTypeIds.UInt16;
                return true;
            }
            else if (vt == "unsignedbyte")
            {
                sharpType = typeof(Byte);
                dataTypeId = DataTypeIds.Byte;
                return true;
            }
            else if (vt == "double")
            {
                sharpType = typeof(double);
                dataTypeId = DataTypeIds.Double;
                return true;
            }
            else if (vt == "float")
            {
                sharpType = typeof(float);
                dataTypeId = DataTypeIds.Float;
                return true;
            }
            else if (vt == "string")
            {
                sharpType = typeof(string);
                dataTypeId = DataTypeIds.String;
                return true;
            }

            return false;
        }
    }
}
