using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class GuestsController : Controller
{
    private readonly ApplicationDbContext _context;

    public GuestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Guests
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(string sortOrder, string searchName, string searchEmail, int page = 1)
    {
        int pageSize = 10;

        ViewData["NameSort"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["EmailSort"] = sortOrder == "email" ? "email_desc" : "email";

        var guests = _context.Guests.AsQueryable();

        // Search by name & email
        if (!string.IsNullOrEmpty(searchName))
            guests = guests.Where(g => g.FullName.Contains(searchName));

        if (!string.IsNullOrEmpty(searchEmail))
            guests = guests.Where(g => g.Email.Contains(searchEmail));

        // Sorting
        guests = sortOrder switch
        {
            "name_desc" => guests.OrderByDescending(g => g.FullName),
            "email" => guests.OrderBy(g => g.Email),
            "email_desc" => guests.OrderByDescending(g => g.Email),
            _ => guests.OrderBy(g => g.FullName)
        };

        // Paging
        int totalItems = await guests.CountAsync();
        var items = await guests.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var model = new PaginatedList<Guest>(items, totalItems, page, pageSize);

        return View(model);
    }

    // GET: Guests/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var guest = await _context.Guests.FirstOrDefaultAsync(m => m.Id == id);
        if (guest == null)
            return NotFound();

        return View(guest);
    }

    // GET: Guests/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Guests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("Id,FullName,Email,Phone,Notes")] Guest guest)
    {
        if (ModelState.IsValid)
        {
            guest.CreatedAt = DateTime.Now;
            _context.Add(guest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(guest);
    }

    // GET: Guests/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var guest = await _context.Guests.FindAsync(id);
        if (guest == null)
            return NotFound();

        return View(guest);
    }

    // POST: Guests/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Email,Phone,Notes,CreatedAt")] Guest guest)
    {
        if (id != guest.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(guest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(guest);
    }

    // GET: Guests/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var guest = await _context.Guests.FirstOrDefaultAsync(m => m.Id == id);
        if (guest == null)
            return NotFound();

        return View(guest);
    }

    // POST: Guests/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var guest = await _context.Guests.FindAsync(id);
        if (guest != null)
        {
            _context.Guests.Remove(guest);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
