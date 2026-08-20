/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

using System.Linq;
using AasxServer;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.Data;

// Link builders shared by Pages/Glc.razor (one row per AAS) and Shared/MainLayout.razor
// (the company logo in the GLC header). Pure functions of Program.externalBlazor and the AAS
// data - no credentials, no per-request state, so statics are safe here. GLC needs neither
// cs.credentials nor the registry, unlike the PCF path in MainLayout.
public static class GlcLinks
{
    public static string GetSmLink(ISubmodel sm)
    {
        if (sm.Extensions != null && sm.Extensions.Count > 0)
            return sm.Extensions[0].Value;

        return Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(sm.Id);
    }

    public static string GetAasLink(AssetAdministrationShell aas)
    {
        if (aas.Extensions != null && aas.Extensions.Count > 0)
            return aas.Extensions[0].Value;

        return Program.externalBlazor + "/shells/" + Base64UrlEncoder.Encode(aas.Id);
    }

    // Builds the attachment URL of an image File element. The Parent chain is populated,
    // because CrudOperator.ReadSubmodel() calls submodel.SetAllParents().
    public static string GetImageLink(AasCore.Aas3_1.File f)
    {
        if (f == null || string.IsNullOrEmpty(f.Value))
            return null;

        var createPath = false;
        if (f.Value.ToLower().StartsWith("http"))
        {
            // External location: our own attachment endpoint rejects these - FileService.ReadFileInZip
            // throws NotImplementedException for http(s) values (src/AasxServerDB/FileService.cs:176).
            // Let the browser fetch the image straight from the source.
            return f.Value;
        }
        else
        {
            string[] split = f.Value.Split(new char[] { '/' });
            if (split.Length == 1 || split.Length == 2 || (split.Length > 1 && (split[1].ToLower() == "aasx" || split[1].ToLower() == "tmp")))
            {
                split = f.Value.Split(new char[] { '.' });
                switch (split.Last().ToLower())
                {
                    case "jpg":
                    case "bmp":
                    case "png":
                    case "svg":
                        createPath = true;
                        break;
                }
            }
        }

        if (!createPath)
            return null;

        try
        {
            string idShortPath = f.IdShort;
            var p = f.Parent;
            while (p != null && !(p is Submodel))
            {
                if (p is SubmodelElementList)
                {
                    //for now only take first image
                    idShortPath = (p as ISubmodelElement).IdShort + "[0]" + idShortPath;
                }
                else if (p is ISubmodelElement)
                {
                    idShortPath = (p as ISubmodelElement).IdShort + "." + idShortPath;
                }
                p = (p as ISubmodelElement).Parent;
            }

            if (p == null)
                return null;

            return GetSmLink(p as Submodel) + "/submodel-elements/" + idShortPath + "/attachment";
        }
        catch
        {
        }

        return null;
    }
}