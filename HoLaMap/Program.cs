using HoLaMap.Cloudflare;
using HoLaMap.Data;
using HoLaMap.Hosts;
using HoLaMap.Hue;
using HoLaMap.Power;
using HoLaMap.Semaphore;
using HoLaMap.UniFi;
using HoLaMap.Zigbee;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddDbContext<HoLaMapContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("HoLaMap")));
builder.Services.Configure<UniFiOptions>(builder.Configuration.GetSection("UniFi"));
builder.Services.AddHttpClient<UniFiClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
builder.Services.AddScoped<UniFiSyncService>();
builder.Services.AddHostedService<UniFiSyncWorker>();
builder.Services.Configure<HomeAssistantOptions>(builder.Configuration.GetSection("HomeAssistant"));
builder.Services.AddScoped<HomeAssistantClient>();
builder.Services.AddScoped<ZigbeeSyncService>();
builder.Services.AddHostedService<ZigbeeSyncWorker>();
builder.Services.Configure<HueOptions>(builder.Configuration.GetSection("Hue"));
builder.Services.AddHttpClient<HueClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
builder.Services.AddScoped<HueSyncService>();
builder.Services.AddHostedService<HueSyncWorker>();
builder.Services.Configure<HostReportOptions>(builder.Configuration.GetSection("Hosts"));
builder.Services.AddScoped<HostSyncService>();
builder.Services.Configure<OpenApiOptions>(builder.Configuration.GetSection("OpenApi"));
builder.Services.AddHttpClient<OpenApiSyncService>();
builder.Services.AddHostedService<OpenApiSyncWorker>();
builder.Services.Configure<CloudflareOptions>(builder.Configuration.GetSection("Cloudflare"));
builder.Services.AddHttpClient<CloudflareSyncService>();
builder.Services.AddHostedService<CloudflareSyncWorker>();
builder.Services.Configure<SemaphoreOptions>(builder.Configuration.GetSection("Semaphore"));
builder.Services.AddHttpClient<SemaphoreSyncService>();
builder.Services.AddHostedService<SemaphoreSyncWorker>();
builder.Services.Configure<PowerOptions>(builder.Configuration.GetSection("Power"));
builder.Services.AddScoped<PowerStatusService>();
builder.Services.AddScoped<WakeOnLanService>();
builder.Services.AddHttpClient<PowerActionService>();
builder.Services.AddHealthChecks();

// Sign in with Pocket ID, required everywhere unless an endpoint allows anonymous access.
AuthenticationBuilder authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
});
authentication.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});
authentication.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});
builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

WebApplication app = builder.Build();

// Bring the database up to date on start. If Robert (and so Postgres) is off, log it and carry on, so the Power page can still be reached to wake it.
using (IServiceScope scope = app.Services.CreateScope())
{
    try
    {
        scope.ServiceProvider.GetRequiredService<HoLaMapContext>().Database.Migrate();
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Couldn't apply database migrations on start.");
    }
}

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages().WithStaticAssets();
app.MapHostEndpoints();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();