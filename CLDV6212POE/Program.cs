using CLDV6212POE.Services;
using Microsoft.EntityFrameworkCore;
using CLDV6212POE.Data;
using CLDV6212POE.Models;
using Azure.Data.Tables;

var builder = WebApplication.CreateBuilder(args);

// Encryption Service
builder.Services.AddSingleton<EncryptionService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var hexKey = config["Encryption:Key"];
    var hexIV = config["Encryption:IV"];
    return new EncryptionService(hexKey, hexIV);
});

// Config (Azure SQL Database)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AzureDatabase")));

// Config (Azure Storage)
var azureConnStr = builder.Configuration.GetConnectionString("AzureStorage");

// MVC Controllers + Views
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<TableStorageInitializer>();

// Azure Storage Services
builder.Services.AddAzureStorageServices(azureConnStr);

builder.Services.AddHttpClient<FunctionConnector>();

// Register application services
builder.Services.AddScoped<CartService>();


// Enable Sessions
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

app.Run();