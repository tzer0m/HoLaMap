using HoLaMap.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddRazorPages();
builder.Services.AddDbContext<HoLaMapContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("HoLaMap")));

WebApplication app = builder.Build();
app.UseRouting();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();