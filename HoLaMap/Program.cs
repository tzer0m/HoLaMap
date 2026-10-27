using HoLaMap.Data;
using HoLaMap.UniFi;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddRazorPages();
builder.Services.AddDbContext<HoLaMapContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("HoLaMap")));
builder.Services.Configure<UniFiOptions>(builder.Configuration.GetSection("UniFi"));
builder.Services.AddHttpClient<UniFiClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
builder.Services.AddScoped<UniFiSyncService>();
builder.Services.AddHostedService<UniFiSyncWorker>();

WebApplication app = builder.Build();
app.UseRouting();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();