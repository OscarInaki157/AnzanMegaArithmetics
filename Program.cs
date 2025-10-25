using AnzanMegaArithmetics.Services;
using DataBase;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();

builder.Services.AddScoped<IUsersDBService, UsersDBService>();
builder.Services.AddScoped<IClasesDBService, ClasesDBService>();

builder.Services.AddDbContext<AnzanMegaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AnzanConnection")).EnableSensitiveDataLogging()
);

builder.Services.AddSession(options =>
{
    //Tiempo de inactividad, acumulable
    //options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(
    option => {
        option.LoginPath = "/Inicio/Login";
        //Tiempo de vida de la cookie de authenticación
        //option.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        option.ExpireTimeSpan = TimeSpan.FromHours(1);
        option.AccessDeniedPath = "/Inicio/Inicio";
});

// Configuración de la localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
//Configuración de la localización
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("es-MX"),
        new CultureInfo("en-US"),
    };

    options.DefaultRequestCulture = new RequestCulture("es-MX");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Content")),
    RequestPath = new PathString("/Content")
});

app.UseSession();

app.UseRouting();

//Localizacion
app.UseRequestLocalization();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Inicio}/{id?}");

app.Run();
