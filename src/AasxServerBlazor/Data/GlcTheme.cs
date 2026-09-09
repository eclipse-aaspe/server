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

using System;

namespace AasxServerBlazor.Data;

// Brand colors for the GLC showcase, selected once by the GLCTHEME environment variable.
//
// Only brand colors belong here. The green/red submodel chips (GlcSubmodelNode.color,
// AasxTaskService.cs:3145) and the hue-rotate SVG filter in Glc.razor encode NO-ACCESS vs
// OK, not branding, and stay hard-coded.
//
// static readonly + static ctor rather than the lazily-flipped bool of Glc.razor's
// getIframePath(): the environment cannot change while the process runs, and the CLR runs a
// static initializer exactly once and thread-safely - which pathIntit does NOT guarantee
// across concurrent Blazor circuits. Nothing below may throw: an exception here would become
// a TypeInitializationException and poison every later access, blanking /glc for good.
public static class GlcTheme
{
    public static readonly string Name;

    // Surfaces: the ">" button, the disclaimer toggle dot
    public static readonly string Accent;

    public static readonly string AccentText;

    // Text on the white card: the material number.
    public static readonly string TextAccent;

    static GlcTheme()
    {
        // System.Environment fully qualified: the project-level <Using Include="AasCore.Aas3_1" />
        // brings AasCore.Aas3_1.Environment into scope, so a bare "Environment" is ambiguous.
        // \r\n stripping: Docker env files keep the CR, same as Program.cs:457-458
        var v = System.Environment.GetEnvironmentVariable("GLCTHEME");
        v = v?.Replace("\r", "").Replace("\n", "").Trim().ToLowerInvariant();

        if (v == "pxc")
        {
            Name = "pxc";
            Accent = "#0098A1";
            AccentText = "#FFFFFF";
            TextAccent = "#000000";
        }
        else
        {
            Name = "zvei";
            Accent = "#174a87";
            AccentText = "limegreen";
            TextAccent = "#174a87";
        }

        // first /glc render, not startup - this deliberately lives outside Program.cs
        Console.WriteLine("GLCTHEME: " + Name + " (accent " + Accent + ")");
    }
}