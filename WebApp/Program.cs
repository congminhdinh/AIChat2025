using Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebApp.Business;
using WebApp.Helpers;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
// Add services to the container.
builder.Services.AddScoped<IdentityHelper>();
builder.AddInfrastructure();

// Configure Cookie Authentication for WebApp (in addition to JWT from infrastructure)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = "AIChat2025.Auth";
});

// Add Cookie Authentication scheme
builder.Services.AddAuthentication()
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "AIChat2025.Auth";
    });

builder.Services.AddControllersWithViews();
builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings = builder.Configuration.Get<AppSettings>()?? new AppSettings();
builder.Services.AddScoped<AuthBusiness>();
builder.Services.AddHttpClient<AuthBusiness>(httpClient =>
{
    httpClient.BaseAddress = new Uri(appSettings.ApiGatewayUrl);
});
builder.Services.AddScoped<PermissionBusiness>();
builder.Services.AddHttpClient<PermissionBusiness>(httpClient =>
{
    httpClient.BaseAddress = new Uri(appSettings.ApiGatewayUrl);
});
builder.Services.AddScoped<PermissionBusiness>();
builder.Services.AddHttpClient<PermissionBusiness>(httpClient =>
{
    httpClient.BaseAddress = new Uri(appSettings.ApiGatewayUrl);
});
var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

app.UseHttpsRedirection();
app.UseRouting();

// Authentication and Authorization must come after UseRouting
app.UseAuthentication();
app.UseAuthorization();

// Add Antiforgery middleware
app.UseAntiforgery();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
