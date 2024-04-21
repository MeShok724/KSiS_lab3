using System.Text;
using Microsoft.AspNetCore.Mvc;
using Server.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
var _uploadPath = $"{Directory.GetCurrentDirectory()}/RemoteDirectory";

app.MapGet("/api/files/info", () =>
{
    string directoryPath = $"{Directory.GetCurrentDirectory()}/RemoteDirectory"; 
    string[] fileNamesArray = Directory.GetFiles(directoryPath);
    return Results.Json(fileNamesArray);
});

app.MapPost("/api/files/post", async (HttpContext context) =>
{
    try
    {
        string fullPath = _uploadPath +"/"+ context.Request.Form.Files[0].FileName;
        if (!File.Exists(fullPath))
        {
            return Results.NotFound(); 
        }
        var file = context.Request.Form.Files[0];
        using (var fileStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
        {
            await file.CopyToAsync(fileStream);
        }

        return Results.Ok();
    }
    catch
    {
        return Results.Problem();
    }
});

app.MapGet("/api/files/get", async (HttpContext context) =>
{
    try
    {
        string filePath = context.Request.Query["filePath"];
        string fullFilePath = _uploadPath + "/" + filePath;
        
        if (!File.Exists(fullFilePath))
        {
            return Results.NotFound(); 
        }
        
        byte[] fileBytes = File.ReadAllBytes(fullFilePath);
        
        return Results.File(fileBytes, "application/octet-stream");
    }
    catch
    {
        return Results.Problem();
    }
});

app.MapPut("/api/files/put/{fileName}", async (HttpContext context) =>
{
    string fullPath = _uploadPath + "/" + context.Request.RouteValues["fileName"];
    if (!File.Exists(fullPath))
    {
        return Results.NotFound(); 
    }
    using (MemoryStream ms = new MemoryStream())
    {
        await context.Request.Body.CopyToAsync(ms);
        byte[] fileBytes = ms.ToArray();
        using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            fileStream.Write(fileBytes);
        }
    }

    return Results.Ok();
});
app.MapDelete("/api/files/delete/{fileName}", async (HttpContext context) =>
{
    string fullPath = _uploadPath + "/" + context.Request.RouteValues["fileName"];
    if (!File.Exists(fullPath))
    {
        return Results.NotFound(); 
    }
    try
    {
        File.Delete(fullPath);
        return Results.Ok();
    }
    catch
    {
        return Results.Problem();
    }
});

app.MapPost("/api/files/copy", async context =>
{
    try
    {
        // Получаем пути исходного и целевого файлов из параметров запроса
        string sourcePath = _uploadPath + "/" + context.Request.Query["sourcePath"];
        string destinationPath = _uploadPath + "/" + context.Request.Query["destinationPath"];

        // Проверяем существование исходного файла
        if (!File.Exists(sourcePath))
        {
            context.Response.StatusCode = 404; // Not Found
            await context.Response.WriteAsync($"Исходный файл '{sourcePath}' не найден.");
            return;
        }

        // Создаем директорию для места назначения, если она не существует
        string destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // Копируем файл из исходного места в место назначения
        File.Copy(sourcePath, destinationPath, true);

        await context.Response.WriteAsync($"Файл успешно скопирован из '{sourcePath}' в '{destinationPath}'.");
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500; // Internal Server Error
        await context.Response.WriteAsync($"Ошибка при копировании файла: {ex.Message}");
    }
});
app.MapPost("/api/files/move", async context =>
{
    try
    {
        string sourcePath = _uploadPath + "/" + context.Request.Query["sourcePath"];
        string destinationPath = _uploadPath + "/" + context.Request.Query["destinationPath"];

        if (!File.Exists(sourcePath))
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"Исходный файл '{sourcePath}' не найден.");
            return;
        }

        // Создаем директорию для места назначения, если она не существует
        string destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        
        File.Move(sourcePath, destinationPath, true);

        await context.Response.WriteAsync($"Файл успешно перемещен из '{sourcePath}' в '{destinationPath}'.");
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500; // Internal Server Error
        await context.Response.WriteAsync($"Ошибка при перемещении файла: {ex.Message}");
    }
});

// app.MapGet("/api/files/copy/{sourcePath}/{destPath}", async (HttpContext context) =>
// {
//     string fullPath = _uploadPath + "/" + context.Request.RouteValues["sourcePath"];
//     string fullDestPath = _uploadPath + "/" + context.Request.RouteValues["destPath"];
//     if (!File.Exists(fullPath))
//     {
//         return Results.NotFound(); 
//     }
//     if (!Directory.Exists(fullDestPath))
//     {
//         try
//         {
//             Directory.CreateDirectory(fullDestPath);
//         }
//         catch 
//         {
//             return Results.Conflict();
//         }
//     }
//     try
//     {
//         File.Copy(fullPath, fullDestPath);
//         return Results.Ok();
//     }
//     catch
//     {
//         return Results.BadRequest();
//     }
// });
// app.MapGet("/api/files/move/{sourcePath}/{destPath}", async (HttpContext context) =>
// {
//     string fullPath = _uploadPath + "/" + context.Request.RouteValues["sourcePath"];
//     string fullDestPath = _uploadPath + "/" + context.Request.RouteValues["destPath"];
//     if (!File.Exists(fullPath))
//     {
//         return Results.NotFound(); 
//     }
//     if (!Directory.Exists(fullDestPath))
//     {
//         try
//         {
//             Directory.CreateDirectory(fullDestPath);
//         }
//         catch 
//         {
//             return Results.Conflict();
//         }
//     }
//     try
//     {
//         File.Move(fullPath, fullDestPath);
//         return Results.Ok();
//     }
//     catch
//     {
//         return Results.BadRequest();
//     }
// });

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();