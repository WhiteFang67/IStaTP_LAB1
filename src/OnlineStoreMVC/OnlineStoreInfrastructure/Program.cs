using OnlineStoreInfrastructure;
using Microsoft.EntityFrameworkCore;
using OnlineStoreInfrastructure.Services;
using OnlineStoreDomain.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DbContext
builder.Services.AddDbContext<OnlineStoreContext>(option => option.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
));

// Register services for export and import
builder.Services.AddScoped<IExportService<Product>, ProductExportService>();
builder.Services.AddScoped<IImportService<Product>, ProductImportService>();
builder.Services.AddScoped<IDataPortServiceFactory<Product>, DataPortServiceFactory>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Categories}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();