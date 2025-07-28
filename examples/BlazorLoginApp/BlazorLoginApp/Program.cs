/********************************************************************************
* Copyright (c) 2025 Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://mit-license.org/
*
* SPDX-License-Identifier: MIT
********************************************************************************/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

/*
// Verbose logging information
AzureEventSourceListener listener = new AzureEventSourceListener(
    (args, message) => Console.WriteLine(message),
    System.Diagnostics.Tracing.EventLevel.Verbose);
*/

// Konfiguration
var tenant = "common"; // Damit auch externe Konten wie @live.de funktionieren
var clientId = "865f6ac0-cdbc-44c6-98cc-3e35c39ecb6e"; // aus der App-Registrierung
// var scopes = new[] { "User.Read" }; // Microsoft Graph (oder dein eigener Scope)
// var scopes = new[] { "https://graph.microsoft.com/User.Read" }; // explizit für Microsoft Graph
var scopes = new[] { "openid", "profile", "email" }; // für ID Token im JWT-Format

Console.WriteLine("🔐 Starte Login...");

/* Variante 1: läuft nicht
var options = new InteractiveBrowserCredentialOptions
{
    TenantId = tenantId,
    ClientId = clientId,
    RedirectUri = new Uri("http://localhost"),
};

var credential = new InteractiveBrowserCredential(options);

var token = await credential.GetTokenAsync(new TokenRequestContext(scopes));

Console.WriteLine("✅ Access Token erhalten:\n");
Console.WriteLine(token.Token);
*/

// Variante 2
var app = PublicClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(AzureCloudInstance.AzurePublic, tenant)
    .WithDefaultRedirectUri()             // entspricht http://localhost
    .Build();

var result = await app
    .AcquireTokenInteractive(scopes)
    .WithPrompt(Prompt.SelectAccount)
    .ExecuteAsync();

Console.WriteLine("✅ ID Token:");
Console.WriteLine(result.IdToken);

var idToken = result.IdToken;
Console.WriteLine("\n✅ ID Token erhalten.\n");

var handler = new JsonWebTokenHandler();
var jwtUnvalidated = handler.ReadJsonWebToken(idToken);
var tenantIdClaim = jwtUnvalidated.Claims
    .First(c => c.Type == "tid")
    .Value;

// OpenID-Konfiguration für tenant laden (Signing Keys, Issuer)
string metadataUrl = $"https://login.microsoftonline.com/{tenantIdClaim}/v2.0/.well-known/openid-configuration";
var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    metadataUrl,
    new OpenIdConnectConfigurationRetriever());
var openIdConfig = await configManager.GetConfigurationAsync();

Console.WriteLine("Configured Issuer: " + openIdConfig.Issuer);

var validationParameters = new TokenValidationParameters
{
    // Signaturkontrolle
    IssuerSigningKeys = openIdConfig.SigningKeys,

    // Genau dieser Issuer
    ValidIssuer = openIdConfig.Issuer,

    // Nur für diese App (Audience = ClientId)
    ValidAudience = clientId,

    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,

    ClockSkew = TimeSpan.FromMinutes(2)
};

handler = new JsonWebTokenHandler();

TokenValidationResult validationResult = await handler.ValidateTokenAsync(idToken, validationParameters);

if (!validationResult.IsValid)
{
    Console.WriteLine("❌ Token-Validierung fehlgeschlagen:");
    Console.WriteLine(validationResult.Exception?.Message);
    return;
}

Console.WriteLine("🔒 Token ist SIGNIERT und GÜLTIG!\n");

Console.WriteLine("\n📋 JWT-Claims:\n");

handler = new JsonWebTokenHandler();
var jwt = handler.ReadJsonWebToken(result.IdToken);

foreach (var claim in jwt.Claims)
{
    Console.WriteLine($"{claim.Type}: {claim.Value}");
}

Console.WriteLine("\n📧 E-Mail (wenn vorhanden):");
Console.WriteLine(jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ??
                  jwt.Claims.FirstOrDefault(c => c.Type == "upn")?.Value ??
                  jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ??
                  "(nicht vorhanden)");

Console.WriteLine("Input any character to end program ...");
Console.ReadLine();
