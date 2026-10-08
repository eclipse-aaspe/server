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

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using AasSecurity.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AasSecurity
{
    /// <summary>
    /// Validates bearer tokens with a "kid" header against the JWKS of a trusted issuer.
    /// Only issuers/kids from the trust list are accepted; JWKS urls are derived from the trust list
    /// entry only, never from the token itself.
    /// </summary>
    internal static class JwksTokenValidator
    {
        private static readonly ConcurrentDictionary<TrustedServer, bool> _warnedNoAudience = new();

        /// <param name="httpGet">Fetches the content of a url (injected for testability).</param>
        /// <param name="globalAudienceCsv">Fallback audiences (TOKEN_AUDIENCE) for entries without own audience.</param>
        internal static bool TryValidate(string bearerToken, string? kid, string? iss, IReadOnlyList<TrustedServer> entries,
            Func<string, string> httpGet, string? globalAudienceCsv, ILogger logger, out string domain)
        {
            domain = "";

            var entry = SecurityHelper.FindTrustedJwksEntry(entries, kid, iss);
            if (entry == null)
            {
                logger.LogWarning($"Token rejected: issuer {iss} / kid {kid} is not in the trust list.");
                return false;
            }

            var jwksUrl = ResolveJwksUrl(entry, httpGet, logger);
            if (string.IsNullOrEmpty(jwksUrl))
            {
                logger.LogWarning($"Token rejected: no jwks url for trust list entry {entry}.");
                return false;
            }

            try
            {
                var signingKeys = new JsonWebKeySet(httpGet(jwksUrl)).GetSigningKeys();
                var audiences = ResolveAudiences(entry, globalAudienceCsv);
                if (audiences.Count == 0 && _warnedNoAudience.TryAdd(entry, true))
                {
                    logger.LogWarning($"No audience configured for trust list entry {entry} and TOKEN_AUDIENCE is not set: audience is not validated.");
                }

                var validationParameters = BuildParameters(entry, signingKeys, audiences);
                new JwtSecurityTokenHandler().ValidateToken(bearerToken, validationParameters, out _);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Token rejected for trust list entry {entry}: {ex.Message}");
                return false;
            }

            domain = entry.Domain;
            return true;
        }

        internal static string? ResolveJwksUrl(TrustedServer entry, Func<string, string> httpGet, ILogger logger)
        {
            if (!string.IsNullOrEmpty(entry.JwksUrl))
            {
                return entry.JwksUrl;
            }

            var issuer = SecurityHelper.NormalizeIssuer(entry.Issuer);
            if (issuer == "")
            {
                return null;
            }

            try
            {
                using var openIdConfig = JsonDocument.Parse(httpGet($"{issuer}/.well-known/openid-configuration"));
                if (openIdConfig.RootElement.TryGetProperty("jwks_uri", out var jwksUri))
                {
                    return jwksUri.GetString();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Could not access well-known from {issuer}: {ex.Message}");
            }

            return $"{issuer}/jwks";
        }

        internal static IReadOnlyList<string> ResolveAudiences(TrustedServer entry, string? globalAudienceCsv)
        {
            if (entry.Audiences.Count > 0)
            {
                return entry.Audiences;
            }

            return (globalAudienceCsv ?? "").Split(',').Select(a => a.Trim()).Where(a => a != "").ToList();
        }

        internal static TokenValidationParameters BuildParameters(TrustedServer entry, IEnumerable<SecurityKey> signingKeys,
            IReadOnlyList<string> audiences)
        {
            var parameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                IssuerSigningKeys = signingKeys,
                // Entries matched by kid only have no configured issuer; their keys are pinned by the configured jwks url.
                ValidateIssuer = entry.Issuer != "",
                ValidateAudience = audiences.Count > 0,
                ValidAudiences = audiences
            };

            if (entry.Issuer != "")
            {
                var normalized = SecurityHelper.NormalizeIssuer(entry.Issuer);
                parameters.ValidIssuers = new[] { entry.Issuer, normalized, normalized + "/" }.Distinct().ToList();
            }

            return parameters;
        }
    }
}
