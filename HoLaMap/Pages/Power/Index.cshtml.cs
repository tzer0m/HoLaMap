using HoLaMap.Data;
using HoLaMap.Models;
using HoLaMap.Power;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Pages.Power
{
    /// <summary>
    /// Shows whether each machine is up, with buttons to wake it, put it to sleep or power it off.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="options">The power settings.</param>
    /// <param name="statusService">The status service.</param>
    /// <param name="wakeService">The wake on LAN service.</param>
    /// <param name="actionService">The Semaphore power service.</param>
    /// <param name="logger">The logger.</param>
    public class IndexModel(HoLaMapContext context, IOptions<PowerOptions> options, PowerStatusService statusService, WakeOnLanService wakeService, PowerActionService actionService, ILogger<IndexModel> logger) : PageModel
    {
        /// <summary>
        /// The machines on the page, by name.
        /// </summary>
        public List<Device> Machines { get; private set; } = [];

        /// <summary>
        /// How often the page refreshes each machine's status, in milliseconds.
        /// </summary>
        public int RefreshIntervalMs { get; private set; }

        /// <summary>
        /// The message from the last action, when the page was posted without script.
        /// </summary>
        [TempData]
        public string? Message { get; set; }

        /// <summary>
        /// Loads the machines.
        /// </summary>
        /// <returns>A task that completes when the machines have loaded.</returns>
        public async Task OnGetAsync()
        {
            Machines = await context.Devices.Where(device => device.PowerControl).OrderBy(device => device.Name).ToListAsync();
            RefreshIntervalMs = options.Value.RefreshIntervalMs;
        }

        /// <summary>
        /// Checks every machine and returns whether each is up.
        /// </summary>
        /// <returns>The statuses as JSON.</returns>
        public async Task<IActionResult> OnGetStatusAsync()
        {
            List<Device> devices = await context.Devices.Where(device => device.PowerControl).ToListAsync();
            return new JsonResult(await statusService.CheckAllAsync(devices));
        }

        /// <summary>
        /// Sends a wake packet to a machine.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <returns>The message as JSON, or a redirect back to the page.</returns>
        public async Task<IActionResult> OnPostWakeAsync(int id)
        {
            Device? device = await context.Devices.FirstOrDefaultAsync(device => device.Id == id && device.PowerControl);
            if (device == null)
            {
                return NotFound();
            }
            PowerResult result = await wakeService.WakeAsync(device);
            Log(result);
            return Respond(result);
        }

        /// <summary>
        /// Puts a machine to sleep.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <returns>The message as JSON, or a redirect back to the page.</returns>
        public Task<IActionResult> OnPostSleepAsync(int id)
        {
            return RunAsync(id, PowerAction.Sleep);
        }

        /// <summary>
        /// Powers a machine off.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <returns>The message as JSON, or a redirect back to the page.</returns>
        public Task<IActionResult> OnPostPowerOffAsync(int id)
        {
            return RunAsync(id, PowerAction.Shutdown);
        }

        /// <summary>
        /// Starts Semaphore's power template for a machine.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <param name="action">Whether to power it off or put it to sleep.</param>
        /// <returns>The message as JSON, or a redirect back to the page.</returns>
        private async Task<IActionResult> RunAsync(int id, PowerAction action)
        {
            Device? device = await context.Devices.FirstOrDefaultAsync(device => device.Id == id && device.PowerControl);
            if (device == null)
            {
                return NotFound();
            }
            PowerResult result = await actionService.RunAsync(device, action);
            Log(result);
            return Respond(result);
        }

        /// <summary>
        /// Logs how a wake, sleep or power off went.
        /// </summary>
        /// <param name="result">The result.</param>
        private void Log(PowerResult result)
        {
            if (result.Success)
            {
                logger.LogInformation("Power: {Message}", result.Message);
            }
            else
            {
                logger.LogWarning("Power: {Message}", result.Message);
            }
        }

        /// <summary>
        /// Returns the result as JSON to the page's script, or shows its message after a redirect without script.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The response.</returns>
        private IActionResult Respond(PowerResult result)
        {
            if (Request.Headers.XRequestedWith == "fetch")
            {
                return new JsonResult(new { result.Success, result.Message });
            }
            Message = result.Message;
            return RedirectToPage();
        }
    }
}