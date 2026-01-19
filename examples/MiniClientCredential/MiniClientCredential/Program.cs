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

using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

var config = File.ReadAllLines("config.txt")
    .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains(":"))
    .Select(line => line.Split(new[] { ':' }, 2))
    .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

string certName = config["certName"]; // "client-credential.rsa"
string certPW = config["certPW"]; // "changeit"
string Issuer = config["Issuer"]; //  "https://localhost:5001";
string Audience = config["Audience"]; // "https://localhost:5001/resources";
string LocalURL = config["LocalURL"]; //  "http://localhost:5001";

var builder = WebApplication.CreateBuilder(args);

var cert = new X509Certificate2(certName + ".p12", certPW,
    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

var signingKey = new X509SecurityKey(cert);
var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

var app = builder.Build();

app.MapPost("/token", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var grantType = form["grant_type"].ToString().Trim();
    var clientId = form["client_id"].ToString().Trim();
    var clientSecret = form["client_secret"].ToString().Trim();

    if (grantType != "client_credentials" ||
        clientSecret != clientId + "-secret")
    {
        return Results.BadRequest(new { error = "invalid_client" });
    }

    var serverName = certName;

    var userName = "";
    if (clientId.Contains("@"))
    {
        userName = clientId;
    }
    else
    {
        userName = clientId + "@" + "___credential___" + "/" + serverName;
    }

    var now = DateTime.UtcNow;
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, clientId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat,
                  new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                  ClaimValueTypes.Integer64),
        new Claim("client_id", clientId),
        new Claim("serverName", serverName),
        new Claim("userName", userName),
        new Claim("email", userName)
    };

    var jwt = new JwtSecurityToken(
        issuer: Issuer,
        audience: Audience,
        claims: claims,
        notBefore: now,
        expires: now.AddHours(1),
        signingCredentials: signingCredentials
    );
    var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
    Console.WriteLine();
    Console.WriteLine( tokenString );

    return Results.Json(new
    {
        access_token = tokenString,
        token_type = "Bearer",
        expires_in = 3600
    });
});

app.Run(LocalURL);
