using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        // Read + търсене + сортиране + странициране
        public async Task<IActionResult> Index(
            string sortOrder,
            string typeFilter,
            int? capacityFilter,
            int? pageNumber)
        {
            // sort params за линковете
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NumberSortParm"] = String.IsNullOrEmpty(sortOrder) ? "number_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
            ViewData["CapacitySortParm"] = sortOrder == "Capacity" ? "capacity_desc" : "Capacity";

            // текущи филтри за формата
            ViewData["CurrentTypeFilter"] = typeFilter;
            ViewData["CurrentCapacityFilter"] = capacityFilter;

            var rooms = _context.Rooms.AsQueryable();

            // 🔎 Търсене по 2 критерия: тип и минимум капацитет
            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                rooms = rooms.Where(r => r.Type.Contains(typeFilter));
            }

            if (capacityFilter.HasValue)
            {
                rooms = rooms.Where(r => r.Capacity >= capacityFilter.Value);
            }

            // ↕️ Сортиране
            switch (sortOrder)
            {
                case "number_desc":
                    rooms = rooms.OrderByDescending(r => r.RoomNumber);
                    break;
                case "Price":
                    rooms = rooms.OrderBy(r => r.PricePerNight);
                    break;
                case "price_desc":
                    rooms = rooms.OrderByDescending(r => r.PricePerNight);
                    break;
                case "Capacity":
                    rooms = rooms.OrderBy(r => r.Capacity);
                    break;
                case "capacity_desc":
                    rooms = rooms.OrderByDescending(r => r.Capacity);
                    break;
                default:
                    rooms = rooms.OrderBy(r => r.RoomNumber);
                    break;
            }

            int pageSize = 10;
            var pagedList = await PaginatedList<Room>.CreateAsync(
                rooms.AsNoTracking(),
                pageNumber ?? 1,
                pageSize);

            return View(pagedList);
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // GET: Rooms/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("RoomNumber,Type,PricePerNight,Capacity,HasWifi,Description,LastRenovated")] Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RoomNumber,Type,PricePerNight,Capacity,HasWifi,Description,LastRenovated")] Room room)
        {
            if (id != room.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Rooms.Any(e => e.Id == room.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
