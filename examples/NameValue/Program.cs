using Microsoft.OpenApi.Models;

using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using ApiNoUi.Services;
using static System.Net.WebRequestMethods;

var files = Directory.EnumerateFiles("./files");
foreach (var file in files)
{
    DemoFileParser.Parse(file);
}

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NameValue API", Version = "v1", Description = "Minimal API mit FileUpload (BasicAuth) und NameValue (GET)" });
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Basic Auth (user==password) nur für /FileUpload"
    });
    options.OperationFilter<NameValue.Swagger.BasicAuthOperationFilter>();
});

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 1024L * 1024 * 1024;
});

builder.Services.AddScoped<IFileParser, DemoFileParser>();
builder.Services.AddScoped<IBusinessLogic, DemoBusinessLogic>();
builder.Services.AddScoped<BasicAuthFilter>();

var app = builder.Build();

// Swagger Middlewares
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NameValue API v1");
});

//var uploadRoot = app.Configuration["Storage:UploadPath"] 
//                 ?? Path.Combine(AppContext.BaseDirectory, "uploads");
var uploadRoot = "./files";

Directory.CreateDirectory(uploadRoot);

app.MapPost("/FileUpload", async (
    [FromForm] IFormFile file,
    IFileParser parser,
    ILogger<Program> logger,
    HttpContext context) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file or file empty.");

    var username = context.Items["Username"]?.ToString();

    if (username == null)
    {
        return Results.BadRequest($"No username.");
    }
    if (!file.FileName.StartsWith(username))
    {
        return Results.BadRequest($"Filename {file.FileName} does not start with {username}.");
    }

    var safeName = Path.GetFileName(file.FileName);
    var targetName = $"{safeName}";
    var targetPath = Path.Combine(uploadRoot, targetName);

    Directory.CreateDirectory(uploadRoot);

    await using (var fs = System.IO.File.Create(targetPath))
    {
        await file.CopyToAsync(fs);
    }

    logger.LogInformation("Datei gespeichert: {Path} ({Size} Bytes)", targetPath, file.Length);

    await parser.ParseAsync(targetPath, username);

    return Results.Created($"/files/{targetName}", new
    {
        message = "Upload OK",
        savedAs = targetName,
        size = file.Length
    });
})
.AddEndpointFilter<BasicAuthFilter>()
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.DisableAntiforgery();

app.MapGet("/NameValue", async (
    [FromQuery] string? domain,
    [FromQuery] string? name,
    [FromQuery] string? value,
    IBusinessLogic business) =>
{
    var result = await business.QueryAsync(domain, name, value);
    return Results.Ok(result);
})
.Produces<object>(StatusCodes.Status200OK);

app.MapGet("/", async () =>
{
    var filePath = "./home.txt";

    if (!System.IO.File.Exists(filePath))
    {
        return Results.NotFound("home.txt not found.");
    }

    var content = await System.IO.File.ReadAllTextAsync(filePath);
    return Results.Text(content, "text/plain");
})
.Produces(StatusCodes.Status200OK, typeof(string))
.Produces(StatusCodes.Status404NotFound);

app.Run();

public record NameValueRecord(string Domain, string Name, string Value);
