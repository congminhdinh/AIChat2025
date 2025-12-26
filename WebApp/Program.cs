using Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using WebApp.Business;
using WebApp.Helpers;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
// Add services to the container.
builder.Services.AddScoped<IdentityHelper>();
builder.AddInfrastructure();

// Configure Cookie Authentication for WebApp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "AIChat2025.Auth";
    });

builder.Services.AddControllersWithViews(options =>
{
    // Require authentication globally - controllers must opt-out with [AllowAnonymous]
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
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
builder.Services.AddScoped<ChatBusiness>();
builder.Services.AddHttpClient<ChatBusiness>(httpClient =>
{
    httpClient.BaseAddress = new Uri(appSettings.ApiGatewayUrl);
});
var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Add Antiforgery middleware
app.UseAntiforgery();

app.MapStaticAssets();

// Root route - handle root URL with smart redirect based on auth status
app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Home", action = "Index" })
    .WithStaticAssets();

// Default route - all other controllers default to Index action
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
