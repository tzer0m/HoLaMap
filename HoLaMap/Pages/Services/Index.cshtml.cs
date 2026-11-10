using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Services
{
    /// <summary>
    /// Lists every service by host, so services added by hand can be edited or deleted.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The hosts that have services, with their services loaded.
        /// </summary>
        public List<Device> Hosts { get; private set; } = [];

        /// <summary>
        /// The services in the cloud rather than on a device.
        /// </summary>
        public List<Service> Cloud { get; private set; } = [];

        /// <summary>
        /// Loads the hosts and their services.
        /// </summary>
        /// <returns>A task that completes when the services have loaded.</returns>
        public async Task OnGetAsync()
        {
            Hosts = await context.Devices.Where(device => device.Services.Any()).Include(device => device.Services).OrderBy(device => device.Name).ToListAsync();
            Cloud = await context.Services.Where(service => service.DeviceId == null).OrderBy(service => service.Name).ToListAsync();
        }

        /// <summary>
        /// Deletes a service added by hand.
        /// </summary>
        /// <param name="id">The ID of the service to delete.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Service? service = await context.Services.FirstOrDefaultAsync(service => service.Id == id && service.Source == ServiceSource.Manual);
            if (service != null)
            {
                context.Services.Remove(service);
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}