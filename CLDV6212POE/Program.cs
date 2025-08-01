using Azure.Storage.Blobs;
using CLDV6212POE.Models.Entities;
using CLDV6212POE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Table storage + Customer Table
builder.Services.AddScoped<TableStorageService<CustomerProfile>>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    string connStr = config.GetConnectionString("AzureStorage");
    return new TableStorageService<CustomerProfile>(connStr, "Customer");
});

// Product Table
builder.Services.AddScoped<TableStorageService<ProductInfo>>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    string connStr = config.GetConnectionString("AzureStorage");
    return new TableStorageService<ProductInfo>(connStr, "Product");
});

// Queue storage
builder.Services.AddScoped<QueueStorageService<ProductInfo>>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    string connStr = config.GetConnectionString("AzureStorage");
    return new QueueStorageService<ProductInfo>(connStr, "product-queue");
});

// Blob storage
var blobConnectionString = builder.Configuration.GetConnectionString("AzureStorage");

builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

builder.Services.AddSingleton<BlobStorageService>();

// File Sharing 
builder.Services.AddSingleton<FileStorageService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    return new FileStorageService(config);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
