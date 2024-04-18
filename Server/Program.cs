using Server.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
var _uploadPath = $"{Directory.GetCurrentDirectory()}/RemoteDirectory";

app.MapGet("/api/files/info", () =>
{
    string directoryPath = $"{Directory.GetCurrentDirectory()}/RemoteDirectory"; // Путь к папке, которую нужно просканировать
    string[] fileNamesArray = Directory.GetFiles(directoryPath);
    return Results.Json(fileNamesArray);
});
app.MapPost("/api/files/post", async (HttpContext context) =>
{
    // получем файл
    IFormFile file = context.Request.Form.Files[0];
    
    // формируем путь к файлу в папке uploads
    string fullPath = $"{_uploadPath}/{file.FileName}";

    // сохраняем файл в папку uploads
    using (var fileStream = new FileStream(fullPath, FileMode.Create))
    {
        await file.CopyToAsync(fileStream);
    }
    
    await context.Response.WriteAsync("Файл успешно загружен");
});


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