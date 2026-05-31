using System;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory()),
});

app.MapPost("/convert", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var files = form.Files;

    string tmpInputDir = "input_files";
    string tmpOutDir = "output_files";

    Directory.CreateDirectory(tmpInputDir);
    Directory.CreateDirectory(tmpOutDir);

    foreach (var file in files)
    {
        string inPath = Path.Combine(tmpInputDir, file.FileName);

        using (var stream = File.Create(inPath))
            await file.CopyToAsync(stream);

        string outPath = Path.Combine(tmpOutDir, file.FileName);

        LandXmlShift.Convert(inPath, outPath);
    }

    string zipPath = "Converted_LandXML.zip";
    if (File.Exists(zipPath)) File.Delete(zipPath);
    ZipFile.CreateFromDirectory(tmpOutDir, zipPath);

    byte[] zipBytes = await File.ReadAllBytesAsync(zipPath);

    Directory.Delete(tmpInputDir, true);
    Directory.Delete(tmpOutDir, true);
    File.Delete(zipPath);

    return Results.File(zipBytes, "application/zip", "Converted_LandXML.zip");
});

app.Run("http://0.0.0.0:8080");
