using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Links
{
    /// <summary>
    /// Lists manually added links.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The manual links.
        /// </summary>
        public List<Link> Links { get; private set; } = [];

        /// <summary>
        /// Loads the manual links.
        /// </summary>
        /// <returns>A task that completes when the links have loaded.</returns>
        public async Task OnGetAsync()
        {
            Links = await context.Links.Where(link => link.Source == LinkSource.Manual).Include(link => link.FromPort).ThenInclude(port => port.Device).Include(link => link.ToDevice).Include(link => link.ToPort).OrderBy(link => link.FromPort.Device.Name).ThenBy(link => link.FromPort.Index).ToListAsync();
        }

        /// <summary>
        /// Deletes a manual link.
        /// </summary>
        /// <param name="id">The ID of the link to delete.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Link? link = await context.Links.FirstOrDefaultAsync(link => link.Id == id && link.Source == LinkSource.Manual);
            if (link != null)
            {
                context.Links.Remove(link);
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}