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

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using AasSecurity.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace AasSecurity;

public class TrustListTextParserTests
{
    [Fact]
    public void IssuerEntry_WithAudiences_IsParsed()
    {
        var entries = SecuritySettingsForServerParser.ParseTrustListText(new[]
        {
            "# comment",
            "",
            "audience: aasx-server, other",
            "audience: third",
            "issuer: https://idp.example.com/realms/fx"
        });

        entries.Should().ContainSingle();
        entries[0].Issuer.Should().Be("https://idp.example.com/realms/fx");
        entries[0].JwksUrl.Should().BeEmpty();
        entries[0].Audiences.Should().Equal("aasx-server", "other", "third");
    }

    [Fact]
    public void KidEntry_WithJwks_IsParsed()
    {
        var entries = SecuritySettingsForServerParser.ParseTrustListText(new[]
        {
            "domain: example.com",
            "jwks: https://idp.example.com/jwks",
            "kid: k1"
        });

        entries.Should().ContainSingle();
        entries[0].Kid.Should().Be("k1");
        entries[0].JwksUrl.Should().Be("https://idp.example.com/jwks");
        entries[0].Domain.Should().Be("example.com");
        entries[0].Issuer.Should().BeEmpty();
    }

    [Fact]
    public void KidEntry_WithoutJwks_IsIgnored()
    {
        SecuritySettingsForServerParser.ParseTrustListText(new[] { "kid: k1" }).Should().BeEmpty();
    }

    [Fact]
    public void Values_DoNotLeakIntoNextEntry()
    {
        var entries = SecuritySettingsForServerParser.ParseTrustListText(new[]
        {
            "domain: example.com",
            "jwks: https://idp.example.com/jwks",
            "audience: a",
            "kid: k1",
            "issuer: https://other.example.com"
        });

        entries.Should().HaveCount(2);
        entries[1].Domain.Should().BeEmpty();
        entries[1].JwksUrl.Should().BeEmpty();
        entries[1].Audiences.Should().BeEmpty();
    }

    [Fact]
    public void RepositoryTrustList_ContainsCertificatesAndPxcioIssuer()
    {
        var path = FindRepoFile(Path.Combine("src", "AasxServerBlazor", "trustlist.txt"));

        var entries = SecuritySettingsForServerParser.ParseTrustListText(System.IO.File.ReadAllLines(path));

        entries.Where(e => e.Certificate != null).Should().HaveCount(4);
        entries.Should().ContainSingle(e => e.Issuer == "https://identity.dev.pxcio.net/realms/fx")
               .Which.Audiences.Should().Equal("aasx-server");
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !System.IO.File.Exists(Path.Combine(dir.FullName, relativePath)))
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull($"{relativePath} must exist in a parent directory of the test output");
        return Path.Combine(dir!.FullName, relativePath);
    }
}

public class TrustedJwksEntryMatcherTests
{
    private static readonly TrustedServer IssuerEntry = new() { Issuer = "https://idp.example.com/realms/fx/" };
    private static readonly TrustedServer KidEntry = new() { Kid = "k1", JwksUrl = "https://keys.example.com" };
    private static readonly TrustedServer CertEntry = new() { CertFileName = "server.cer" };
    private static readonly TrustedServer[] Entries = { CertEntry, KidEntry, IssuerEntry };

    [Theory]
    [InlineData("https://idp.example.com/realms/fx")]
    [InlineData("https://idp.example.com/realms/fx/")]
    public void MatchesIssuer_IgnoringTrailingSlash(string iss)
    {
        SecurityHelper.FindTrustedJwksEntry(Entries, null, iss).Should().BeSameAs(IssuerEntry);
    }

    [Fact]
    public void IssuerWinsOverKid()
    {
        SecurityHelper.FindTrustedJwksEntry(Entries, "k1", "https://idp.example.com/realms/fx").Should().BeSameAs(IssuerEntry);
    }

    [Fact]
    public void MatchesKid()
    {
        SecurityHelper.FindTrustedJwksEntry(Entries, "k1", "https://unknown.example.com").Should().BeSameAs(KidEntry);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("k2", "https://unknown.example.com")]
    public void NoMatch_ReturnsNull(string? kid, string? iss)
    {
        SecurityHelper.FindTrustedJwksEntry(Entries, kid, iss).Should().BeNull();
    }
}

public class JwksTokenValidatorTests
{
    private const string Issuer = "https://idp.example.com/realms/fx";
    private const string JwksUri = Issuer + "/protocol/openid-connect/certs";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "k1" };
    private readonly RsaSecurityKey _otherKey = new(RSA.Create(2048)) { KeyId = "k1" };
    private readonly Dictionary<string, string> _web = new();
    private readonly List<string> _requested = new();

    public JwksTokenValidatorTests()
    {
        _web[Issuer + "/.well-known/openid-configuration"] = $"{{\"issuer\":\"{Issuer}\",\"jwks_uri\":\"{JwksUri}\"}}";
        _web[JwksUri] = Jwks(_key);
    }

    private string HttpGet(string url)
    {
        _requested.Add(url);
        return _web.TryGetValue(url, out var content) ? content : throw new HttpRequestException($"404 {url}");
    }

    private static string Jwks(RsaSecurityKey key)
    {
        var p = key.Rsa.ExportParameters(false);
        return $"{{\"keys\":[{{\"kty\":\"RSA\",\"use\":\"sig\",\"alg\":\"RS256\",\"kid\":\"{key.KeyId}\"," +
               $"\"n\":\"{Base64UrlEncoder.Encode(p.Modulus)}\",\"e\":\"{Base64UrlEncoder.Encode(p.Exponent)}\"}}]}}";
    }

    private string Token(string iss = Issuer, object? aud = null, RsaSecurityKey? key = null, int expiresInMinutes = 5)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new JwtPayload
        {
            { "iss", iss },
            { "sub", "d3eceb07-ba47-4f8a-8aa1-337bf6eb17b6" },
            { "iat", now.AddMinutes(-10).ToUnixTimeSeconds() },
            { "nbf", now.AddMinutes(-10).ToUnixTimeSeconds() },
            { "exp", now.AddMinutes(expiresInMinutes).ToUnixTimeSeconds() }
        };
        if (aud != null)
        {
            payload.Add("aud", aud);
        }

        var header = new JwtHeader(new SigningCredentials(key ?? _key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
    }

    private bool Validate(string token, TrustedServer[] entries, string? globalAudience = null, string? iss = Issuer)
        => JwksTokenValidator.TryValidate(token, "k1", iss, entries, HttpGet, globalAudience, NullLogger.Instance, out _);

    private static TrustedServer IssuerEntry(params string[] audiences) => new() { Issuer = Issuer, Audiences = audiences, Domain = "example.com" };

    [Fact]
    public void TrustedIssuer_IsAccepted_UsingOnlyConfiguredUrls()
    {
        var valid = JwksTokenValidator.TryValidate(Token(aud: "aasx-server"), "k1", Issuer, new[] { IssuerEntry("aasx-server") },
            HttpGet, null, NullLogger.Instance, out var domain);

        valid.Should().BeTrue();
        domain.Should().Be("example.com");
        _requested.Should().Equal(Issuer + "/.well-known/openid-configuration", JwksUri);
    }

    [Fact]
    public void UntrustedIssuer_IsRejected_WithoutHttpCall()
    {
        const string attacker = "https://attacker.example.com";
        _web[attacker + "/.well-known/openid-configuration"] = $"{{\"jwks_uri\":\"{JwksUri}\"}}";

        Validate(Token(iss: attacker, aud: "aasx-server"), new[] { IssuerEntry("aasx-server") }, iss: attacker).Should().BeFalse();
        _requested.Should().BeEmpty();
    }

    [Fact]
    public void WrongSigningKey_IsRejected()
    {
        Validate(Token(aud: "aasx-server", key: _otherKey), new[] { IssuerEntry("aasx-server") }).Should().BeFalse();
    }

    [Fact]
    public void KidEntryWithIssuer_TokenWithOtherIssuer_IsRejected()
    {
        var entry = new TrustedServer { Kid = "k1", JwksUrl = JwksUri, Issuer = "https://configured.example.com" };

        Validate(Token(iss: "https://other.example.com"), new[] { entry }, iss: "https://other.example.com").Should().BeFalse();
    }

    [Fact]
    public void KidOnlyEntry_DoesNotValidateIssuer()
    {
        var entry = new TrustedServer { Kid = "k1", JwksUrl = JwksUri };

        Validate(Token(iss: "https://any.example.com"), new[] { entry }, iss: "https://any.example.com").Should().BeTrue();
    }

    [Fact]
    public void WrongAudience_IsRejected()
    {
        Validate(Token(aud: "account"), new[] { IssuerEntry("aasx-server") }).Should().BeFalse();
    }

    [Fact]
    public void AudienceArray_ContainingExpectedAudience_IsAccepted()
    {
        Validate(Token(aud: new[] { "aasx-server", "account" }), new[] { IssuerEntry("aasx-server") }).Should().BeTrue();
    }

    [Fact]
    public void NoAudienceConfigured_AudienceIsNotValidated()
    {
        Validate(Token(aud: "account"), new[] { IssuerEntry() }).Should().BeTrue();
    }

    [Fact]
    public void GlobalAudience_IsUsedWhenEntryHasNone()
    {
        Validate(Token(aud: "account"), new[] { IssuerEntry() }, globalAudience: "aasx-server").Should().BeFalse();
        Validate(Token(aud: "aasx-server"), new[] { IssuerEntry() }, globalAudience: "x, aasx-server").Should().BeTrue();
    }

    [Fact]
    public void EntryAudience_TakesPrecedenceOverGlobalAudience()
    {
        Validate(Token(aud: "aasx-server"), new[] { IssuerEntry("aasx-server") }, globalAudience: "other").Should().BeTrue();
    }

    [Fact]
    public void ExpiredToken_IsRejected()
    {
        Validate(Token(aud: "aasx-server", expiresInMinutes: -10), new[] { IssuerEntry("aasx-server") }).Should().BeFalse();
    }

    [Fact]
    public void MissingDiscovery_FallsBackToConfiguredIssuerJwks()
    {
        _web.Remove(Issuer + "/.well-known/openid-configuration");
        _web[Issuer + "/jwks"] = Jwks(_key);

        Validate(Token(aud: "aasx-server"), new[] { IssuerEntry("aasx-server") }).Should().BeTrue();
        _requested.Should().EndWith(Issuer + "/jwks");
    }
}
