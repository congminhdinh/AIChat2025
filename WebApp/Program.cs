using Infrastructure;
using WebApp.Business;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddInfrastructure();
builder.Services.AddControllersWithViews();
builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings = builder.Configuration.Get<AppSettings>()?? new AppSettings();
builder.Services.AddScoped<PermissionBusiness>();
builder.Services.AddHttpClient<PermissionBusiness>(httpClient =>
{
    httpClient.BaseAddress = new Uri(appSettings.ApiGatewayUrl);
});
var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseInfrastructure();
// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
