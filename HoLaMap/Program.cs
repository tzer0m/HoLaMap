namespace HoLaMap
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Configures and runs the web application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRouting(options => options.LowercaseUrls = true);
            builder.Services.AddRazorPages();

            WebApplication app = builder.Build();
            app.UseRouting();
            app.MapStaticAssets();
            app.MapRazorPages().WithStaticAssets();
            app.Run();
        }
    }
}