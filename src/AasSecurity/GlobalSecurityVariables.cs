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

using AasSecurity.Models;
using System.Security.Cryptography.X509Certificates;

namespace AasSecurity
{
    public static class GlobalSecurityVariables
    {
        public static bool WithAuthentication { get; set; }
        public static List<SecurityRole> SecurityRoles = new();
        internal static List<X509Certificate2> ServerCertificates = new();
        internal static List<string> ServerCertFileNames = new();
        internal static List<SecurityRight> SecurityRights = new();
        internal static Dictionary<string, string> SecurityUsernamePassword = new();
    }
}
