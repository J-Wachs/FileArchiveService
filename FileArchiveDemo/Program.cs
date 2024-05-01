using FileArchive;
using FileArchive.DataAccess;
using FileArchive.Utils;
using FileArchiveDemo.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


// Added for FileArchive:
builder.Services.AddControllers();
builder.Services.AddScoped<IFileArchiveStorage, FileArchiveStorageFolder>();
//builder.Services.AddScoped<IFileArchiveStorage, FileArchiveFolderAzureBlob>();
builder.Services.AddScoped<IFileArchiveFileInfoCRUD, FileArchiveFileInfoCRUDJSON>();
//builder.Services.AddScoped<IFileArchiveFileInfoCRUD, FileArchiveFileInfoCRUDDB>();
builder.Services.AddScoped<IJWTokenHelper, JWTokenHelper>();
builder.Services.AddScoped<IFileArchiveJWTokenHelperBuild, FileArchiveJWTokenHelperBuild>();
builder.Services.AddScoped<IFileArchiveJWTokenHelperRead, FileArchiveJWTokenHelperRead>();

builder.Services.AddDbContext<FileArchiveContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["storageConnectionString:blob"]!, preferMsi: true);
    clientBuilder.AddQueueServiceClient(builder.Configuration["storageConnectionString:queue"]!, preferMsi: true);
});
// End


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
// Set in comment for JWFileArchive: app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Added for JWFileArchive:
app.UseRouting();
app.UseAntiforgery();
app.UseEndpoints(endpoints =>
{
	_ = endpoints.MapControllerRoute(
	"default",
	"api/{controller}/{action=Index}");
});
// End

app.Run();
