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

using System.Security.Cryptography.X509Certificates;

namespace AasSecurity.Models
{
    /// <summary>
    /// One trusted token issuer, read from trustlist.txt, trustlist.xml or the publicCertificate
    /// of the SecuritySettingsForServer submodel.
    /// A certificate entry (Certificate + CertFileName) validates tokens carrying a "serverName" claim.
    /// A JWKS entry (Issuer and/or Kid, optional JwksUrl) validates tokens carrying a "kid" header.
    /// </summary>
    internal sealed class TrustedServer
    {
        public X509Certificate2? Certificate { get; init; }
        public string CertFileName { get; init; } = "";
        public string Domain { get; init; } = "";
        public string JwksUrl { get; init; } = "";
        public string Issuer { get; init; } = "";
        public string Kid { get; init; } = "";
        public IReadOnlyList<string> Audiences { get; init; } = [];
        public string Source { get; init; } = "";

        public bool IsJwksEntry => !string.IsNullOrEmpty(Issuer) || !string.IsNullOrEmpty(Kid);

        public override string ToString() =>
            !string.IsNullOrEmpty(Issuer) ? $"issuer {Issuer} ({Source})" :
            !string.IsNullOrEmpty(Kid) ? $"kid {Kid} ({Source})" :
            $"certificate {CertFileName} ({Source})";
    }
}
