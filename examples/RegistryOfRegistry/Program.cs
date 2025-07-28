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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.RegularExpressions;

public class Descriptor
{
    public string Url { get; set; }
    public string? Security { get; set; }
    public string Match { get; set; }
    public string Pattern { get; set; }
    public string? Domain { get; set; }
    public string Id { get; set; }
    public string Info { get; set; }

    public Descriptor()
    {
        Url = "";
        Security = "";
        Match = "";
        Pattern = "";
        Domain = "";
        Id = "";
        Info = "";
    }
}

[ApiController]
[Route("[controller]")]
public class DescriptorsController : ControllerBase
{
    private static List<Descriptor> Descriptors = new List<Descriptor>();

    public static void test()
    {
        Descriptors.Add(new Descriptor
        {
            Url = "http://example.com/12345",
            Security = "",
            Match = "LIKE",
            Pattern = "123%",
            Domain = "example.com",
            Id = "xxx",
            Info = "Demo"
        });

        Descriptors.Add(new Descriptor
        {
            Url = "http://example.com/67890",
            Security = "",
            Match = "REGEX",
            Pattern = "^678.*",
            Domain = "example.com",
            Id = "xxx",
            Info = "Demo"
        });

        Descriptors.Add(new Descriptor
        {
            Url = "http://example.com/abcde",
            Security = "",
            Match = "LIKE",
            Pattern = "%abc%",
            Domain = "example.com",
            Id = "xxx",
            Info = "Demo"
        });

        Descriptors.Add(new Descriptor
        {
            Url = "http://example.com/1-2-3",
            Security = "",
            Match = "REGEX",
            Pattern = ".*1.*2.*3.*",
            Domain = "example.com",
            Id = "xxx",
            Info = "Demo"
        });
    }

    [HttpGet("/")]
    public IActionResult GetRoot()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Content(
            $"<html>" +
            $"<body>" +
            $"Welcome to the Registry of Registries!" +
            $"<ul>" +
            $"<li>GET <a href='{baseUrl}/registry-descriptors' target='_blank'>/registry-descriptors</a></li>" +
            $"<li>GET <a href='{baseUrl}/registry-descriptors/assetid' target='_blank'>/registry-descriptors/assetid-BASE64URL-encoded[?domain=optional]</a></li>" +
            $"<li>POST /registry-descriptors?verification=verification</li>" +
            $"<li>PUT /registry-descriptors?verification=verification</li>" +
            $"<li>DELETE /registry-descriptors?verification=verification</li>" +
            "<br>" +
            "Example:<br>" +
            "{<br>" +
            "   \"Url\": \"http://example.com/6789\",<br>" +
            "   \"Security\": \"\",<br>" +
            "   \"Match\": \"LIKE\",<br>" +
            "   \"Pattern\": \"%6789%\",<br>" +
            "   \"Domain\": \"example.com\",<br>" +
            "   \"Id\": \"xxx\"<br>" +
            "}<br>" +
            "<br>" +
            "Match ::= LIKE | REGEX<br>" +
            "Optional ?domain can be used with specificAssedIds<br>" +
            "PUT and DELETE are only possible with correct ID.<br>" +
            "<br>" +
            "bearer token can be used by: ?verification=bearer:bearer-token<br>" +
            "bearer token must include claim userName with userName == id in descriptor<br>" +
            $"</ul>" +
            $"</body>" +
            $"</html>",
            "text/html"
        );
    }

    [HttpPost("/registry-descriptors")]
    public IActionResult Post([FromBody] Descriptor descriptor, [FromQuery] string verification)
    {
        if (!Verify(descriptor.Id, verification))
        {
            return Unauthorized("Invalid email or verification code.");
        }

        var index = Descriptors.FindIndex(d => d.Url == descriptor.Url);
        if (index != -1)
        {
            return Conflict("URL already exists!");
        }

        Descriptors.Add(descriptor);
        SaveDescriptors();
        return Ok();
    }

    [HttpPut("/registry-descriptors")]
    public IActionResult Put([FromBody] Descriptor descriptor, [FromQuery] string verification)
    {
        if (!Verify(descriptor.Id, verification))
        {
            return Unauthorized("Invalid email or verification code.");
        }

        var index = Descriptors.FindIndex(d => d.Url == descriptor.Url && d.Id == descriptor.Id);
        if (index != -1)
        {
            Descriptors[index] = descriptor;
            SaveDescriptors();
            return Ok();
        }
        return NotFound("URL with ID not found!");
    }

    [HttpDelete("/registry-descriptors")]
    public IActionResult Delete([FromBody] Descriptor descriptor, [FromQuery] string verification)
    {
        if (!Verify(descriptor.Id, verification))
        {
            return Unauthorized("Invalid email or verification code.");
        }

        var index = Descriptors.FindIndex(d => d.Url == descriptor.Url && d.Id == descriptor.Id);
        if (index != -1)
        {
            Descriptors.RemoveAt(index);
            SaveDescriptors();
            return Ok();
        }
        return NotFound("URL with ID not found!");
    }

    private bool Verify(string id, string verification)
    {
        if (verification.StartsWith("bearer:"))
        {
            var jwtToken = verification.Replace("bearer:", "");
            if (!string.IsNullOrEmpty(jwtToken))
            {
                var user = "";
                var serverName = "";
                Console.WriteLine("Validating jwtToken: " + jwtToken);
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(jwtToken);
                if (jwtSecurityToken != null)
                {
                    if (jwtSecurityToken.Claims != null)
                    {

                        var emailClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type.Equals("email"));
                        if (emailClaim != null && !string.IsNullOrEmpty(emailClaim.Value))
                        {
                            user = emailClaim.Value.ToLower();
                        }

                        var serverNameClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type.Equals("serverName"));
                        if (serverNameClaim != null && !string.IsNullOrEmpty(serverNameClaim.Value))
                        {
                            serverName = serverNameClaim.Value;
                        }

                        var userNameClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type.Equals("userName"));
                        if (userNameClaim != null && !string.IsNullOrEmpty(userNameClaim.Value))
                        {
                            var userName = userNameClaim.Value;
                            if (!string.IsNullOrEmpty(userName))
                            {
                                user = userName.ToLower();
                            }
                        }
                        Console.WriteLine($"user: {user}, serverName: {serverName}");
                        if (user != "")
                        {
                            if (id == user)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        string filePath = "verify.dat";

        if (System.IO.File.Exists(filePath))
        {
            foreach (var line in System.IO.File.ReadLines(filePath))
            {
                var parts = line.Split(' ', 3); // Split the line into three parts
                if (parts.Length >= 2)
                {
                    if (id == parts[0] && verification == parts[1])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        if (id == "id" && verification == "verification")
        {
            return true;
        }

        return false;
    }

    private static void copyWithoutId(Descriptor from, Descriptor to)
    {
        to.Url = from.Url;
        to.Security = from.Security;
        to.Match = from.Match;
        to.Pattern = from.Pattern;
        to.Domain = from.Domain;
        to.Info = from.Info;
    }
    public static List<Descriptor> ClearIds(IEnumerable<Descriptor> descriptors)
    {
        var result = new List<Descriptor>();
        foreach (var d in descriptors)
        {
            var r = new Descriptor();
            copyWithoutId(d, r);
            result.Add(r);
        }
        return result;
    }

    public static void SaveDescriptors()
    {
        var filePath = "descriptors.dat";
        var json = JsonSerializer.Serialize(Descriptors);
        System.IO.File.WriteAllText(filePath, json);
    }

    public static void LoadDescriptors()
    {
        var filePath = "descriptors.dat";
        if (System.IO.File.Exists(filePath))
        {
            var json = System.IO.File.ReadAllText(filePath);
            Descriptors = JsonSerializer.Deserialize<List<Descriptor>>(json);
        }
        if (Descriptors == null)
        {
            Descriptors = new List<Descriptor>();
        }
    }

    [HttpGet("/registry-descriptors")]
    public IActionResult GetAllDescriptors([FromQuery] string? domain)
    {
        var result = Descriptors.Where(d =>
            domain == null || (d.Domain != null && (d.Domain == domain || d.Domain.EndsWith("." + domain))));
        result = ClearIds(result);

        if (result != null)
        {
            return Ok(result);
        }

        return NotFound();
    }

    [HttpGet("/registry-descriptors/{assetId}")]
    public IActionResult GetDescriptors(string assetId, [FromQuery] string? domain)
    {
        assetId = Base64UrlEncoder.Decode(assetId);
        var result = Descriptors.Where(d =>
            (domain == null || (d.Domain != null && (d.Domain == domain || d.Domain.EndsWith("." + domain))))
            && Match(d.Match, d.Pattern, assetId)
        );
        result = ClearIds(result);

        if (result != null)
        {
            return Ok(result);
        }

        return NotFound();
    }

    private bool Match(string match, string format, string assetId)
    {
        var pattern = "";
        switch (match)
        {
            case "LIKE":
                pattern = "^" + Regex.Escape(format).Replace("%", ".*") + "$";
                break;
            case "REGEX":
                pattern = format;
                break;
            default:
                return false;
        }
        var result = Regex.IsMatch(assetId, pattern);
        return result;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        DescriptorsController.test();
        DescriptorsController.LoadDescriptors();

        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls("http://localhost:5001");
            });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
