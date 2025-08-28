using CLDV6212POE.Services;

var builder = WebApplication.CreateBuilder(args);

// Config
var azureConnStr = builder.Configuration.GetConnectionString("AzureStorage");

// MVC Controllers + Views
builder.Services.AddControllersWithViews();

// Azure Storage Services
builder.Services.AddAzureStorageServices(azureConnStr);

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
app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

app.Run();