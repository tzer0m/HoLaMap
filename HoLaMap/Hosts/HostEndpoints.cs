using System.Security.Cryptography;
using System.Text;
using HoLaMap.Hosts.Models;
using HoLaMap.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// The API hosts use to report what's running on them.
    /// </summary>
    public static class HostEndpoints
    {
        /// <summary>
        /// Maps the host report endpoints.
        /// </summary>
        /// <param name="app">The web application.</param>
        public static void MapHostEndpoints(this WebApplication app)
        {
            app.MapPost("/api/services/{host}/systemd", ReportSystemdAsync);
            app.MapPost("/api/services/{host}/docker", ReportDockerAsync);
            app.MapPost("/api/services/{host}/ports", ReportPortsAsync);
        }

        /// <summary>
        /// Syncs a host's systemd services from the output of systemctl show, sent as the request body.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="request">The request.</param>
        /// <param name="sync">The host sync service.</param>
        /// <param name="options">The host report settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of services, unauthorised, or not found if the host isn't on the map.</returns>
        private static Task<IResult> ReportSystemdAsync(string host, HttpRequest request, HostSyncService sync, IOptionsSnapshot<HostReportOptions> options, CancellationToken cancellationToken)
        {
            return ReportAsync(host, request, sync, options.Value.Token, ServiceKind.Systemd, output => SystemdParser.Services(output, options.Value.SystemdInclude, options.Value.SystemdIgnore), cancellationToken);
        }

        /// <summary>
        /// Syncs a host's Docker containers from the output of docker ps --format '{{json .}}', sent as the request body.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="request">The request.</param>
        /// <param name="sync">The host sync service.</param>
        /// <param name="options">The host report settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of services, unauthorised, or not found if the host isn't on the map.</returns>
        private static Task<IResult> ReportDockerAsync(string host, HttpRequest request, HostSyncService sync, IOptionsSnapshot<HostReportOptions> options, CancellationToken cancellationToken)
        {
            return ReportAsync(host, request, sync, options.Value.Token, ServiceKind.Docker, output => DockerParser.Services(output, options.Value.DockerIgnore), cancellationToken);
        }

        /// <summary>
        /// Saves which ports a host's systemd services listen on, from the output of ss -tlnpH, sent as the request body.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="request">The request.</param>
        /// <param name="sync">The host sync service.</param>
        /// <param name="options">The host report settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of services with a port, unauthorised, or not found if the host isn't on the map.</returns>
        private static async Task<IResult> ReportPortsAsync(string host, HttpRequest request, HostSyncService sync, IOptionsSnapshot<HostReportOptions> options, CancellationToken cancellationToken)
        {
            if (!IsAuthorised(request, options.Value.Token))
            {
                return Results.Unauthorized();
            }
            using StreamReader reader = new(request.Body);
            int? count = await sync.SyncPortsAsync(host, PortsParser.Parse(await reader.ReadToEndAsync(cancellationToken)), cancellationToken);
            return count == null ? Results.NotFound($"No device called {host}.") : Results.Ok(new { Services = count });
        }

        /// <summary>
        /// Checks the token, reads the report from the request body, and syncs one kind of service on the host.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="request">The request.</param>
        /// <param name="sync">The host sync service.</param>
        /// <param name="token">The expected token.</param>
        /// <param name="kind">The kind of service reported.</param>
        /// <param name="parse">Turns the report into services.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of services, unauthorised, or not found if the host isn't on the map.</returns>
        private static async Task<IResult> ReportAsync(string host, HttpRequest request, HostSyncService sync, string token, ServiceKind kind, Func<string, List<ReportedService>> parse, CancellationToken cancellationToken)
        {
            if (!IsAuthorised(request, token))
            {
                return Results.Unauthorized();
            }
            using StreamReader reader = new(request.Body);
            string output = await reader.ReadToEndAsync(cancellationToken);
            int? count = await sync.SyncAsync(host, kind, parse(output), cancellationToken);
            return count == null ? Results.NotFound($"No device called {host}.") : Results.Ok(new { Services = count });
        }

        /// <summary>
        /// Checks the request carries the host report token as a bearer token. No token set means nothing is let in.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="token">The expected token.</param>
        /// <returns>Whether the request is allowed.</returns>
        private static bool IsAuthorised(HttpRequest request, string token)
        {
            string header = request.Headers.Authorization.ToString();
            return token.Length > 0 && header.StartsWith("Bearer ", StringComparison.Ordinal) && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(header["Bearer ".Length..]), Encoding.UTF8.GetBytes(token));
        }
    }
}