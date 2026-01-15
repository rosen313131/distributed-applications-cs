using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // ADMIN LIST
        // ============================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string sortOrder, string? guestName, int? roomNumber, int pageNumber = 1)
        {
            int pageSize = 10;

            // за да помним текущата сортировка при кликване на следваща
            ViewData["CurrentSort"] = sortOrder;

            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["RoomSortParm"] = sortOrder == "room" ? "room_desc" : "room";
            ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";

            var reservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.Guest)
                .AsQueryable();

            // --- Филтри ---
            if (!string.IsNullOrWhiteSpace(guestName))
                reservations = reservations.Where(r => r.Guest.FullName.Contains(guestName));

            if (roomNumber.HasValue)
                reservations = reservations.Where(r => r.Room.RoomNumber == roomNumber);

            // --- СОРТИРОВКА ---
            reservations = sortOrder switch
            {
                "date" => reservations.OrderBy(r => r.CheckInDate),
                "date_desc" => reservations.OrderByDescending(r => r.CheckInDate),

                "room" => reservations.OrderBy(r => r.Room.RoomNumber),
                "room_desc" => reservations.OrderByDescending(r => r.Room.RoomNumber),

                "status" => reservations.OrderBy(r => r.Status),
                "status_desc" => reservations.OrderByDescending(r => r.Status),

                _ => reservations.OrderBy(r => r.Id)
            };

            var paginated = await PaginatedList<Reservation>.CreateAsync(
                reservations.AsNoTracking(), pageNumber, pageSize);

            return View(paginated);
        }


        // ============================================================
        // DETAILS
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // ============================================================
        // USER: RESERVE FROM ROOM PAGE
        // ============================================================
        [Authorize]
        [Authorize]
        public IActionResult CreateFromRoom(int roomId)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null)
                return NotFound();

            var userEmail = User.Identity!.Name;
            var guest = _context.Guests.FirstOrDefault(g => g.Email == userEmail);

            // казваме на view-то дали user има Guest запис
            ViewBag.IsNewGuest = guest == null;

            // подаваме истинския номер на стаята през ViewBag
            ViewBag.RoomNumber = room.RoomNumber;

            var model = new Reservation
            {
                RoomId = room.Id,
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1)
            };

            return View("CreateForUser", model);
        }

        // ============================================================
        // ADMIN – RESERVE FROM ROOM (select guest)
        // ============================================================
        [Authorize(Roles = "Admin")]
        public IActionResult CreateFromRoomForAdmin(int roomId)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null)
                return NotFound();

            // Dropdown with all guests
            ViewData["GuestId"] = new SelectList(
                _context.Guests.OrderBy(g => g.FullName),
                "Id",
                "FullName"
            );

            ViewBag.RoomNumber = room.RoomNumber;

            var model = new Reservation
            {
                RoomId = room.Id,
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1)
            };

            return View("CreateForAdmin", model);
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFromRoomPost(Reservation reservation, string? FullName)
        {
            var room = _context.Rooms.Find(reservation.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("", "Room not found.");
                return View("CreateForUser", reservation);
            }

            var userEmail = User.Identity!.Name;
            var guest = _context.Guests.FirstOrDefault(g => g.Email == userEmail);

            // USER IS NOT GUEST → MUST ENTER FULL NAME
            if (guest == null)
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    ViewBag.IsNewGuest = true;
                    ModelState.AddModelError("", "Full Name is required.");
                    reservation.Room = room;
                    return View("CreateForUser", reservation);
                }

                guest = new Guest
                {
                    FullName = FullName,
                    Email = userEmail,
                    CreatedAt = DateTime.Now
                };

                _context.Guests.Add(guest);
                _context.SaveChanges();
            }

            // DATE VALIDATION
            if (reservation.CheckOutDate <= reservation.CheckInDate)
            {
                reservation.Room = room;
                ModelState.AddModelError("", "Check-out must be after check-in.");
                return View("CreateForUser", reservation);
            }

            // ROOM AVAILABILITY CHECK
            if (IsRoomOverlapping(reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate))
            {
                reservation.Room = room;
                ModelState.AddModelError("", "This room is already booked for these dates.");
                return View("CreateForUser", reservation);
            }

            // CALCULATE PRICE
            int days = (reservation.CheckOutDate - reservation.CheckInDate).Days;
            reservation.TotalPrice = days * room.PricePerNight;
            reservation.Status = "Active";
            reservation.CreatedAt = DateTime.Now;

            // ASSIGN GUEST
            reservation.GuestId = guest.Id;

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            return RedirectToAction("MyReservations");
        }

        // ============================================================
        // ADMIN POST – Create reservation for ANY guest
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFromRoomForAdminPost(Reservation reservation)
        {
            var room = _context.Rooms.Find(reservation.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("", "Room not found.");
                return View("CreateForAdmin", reservation);
            }

            if (reservation.CheckOutDate <= reservation.CheckInDate)
            {
                ModelState.AddModelError("", "Check-out must be after check-in.");
                reservation.Room = room;
                return View("CreateForAdmin", reservation);
            }

            if (IsRoomOverlapping(reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate))
            {
                ModelState.AddModelError("", "Room is already booked for these dates.");
                reservation.Room = room;
                return View("CreateForAdmin", reservation);
            }

            reservation.TotalPrice =
                (reservation.CheckOutDate - reservation.CheckInDate).Days * room.PricePerNight;

            reservation.Status = "Active";
            reservation.CreatedAt = DateTime.Now;

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        // ============================================================
        // USER: MY RESERVATIONS
        // ============================================================
        [Authorize]
        public async Task<IActionResult> MyReservations()
        {
            var userEmail = User.Identity!.Name;
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == userEmail);

            if (guest == null)
                return View(new List<Reservation>());

            var list = await _context.Reservations
                .Include(r => r.Room)
                .Where(r => r.GuestId == guest.Id)
                .OrderByDescending(r => r.CheckInDate)
                .ToListAsync();

            return View(list);
        }

        // ============================================================
        // ADMIN – CREATE RESERVATION FOR ANY GUEST
        // ============================================================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            PopulateDropDowns();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Reservation reservation)
        {
            if (reservation.CheckOutDate <= reservation.CheckInDate)
            {
                ModelState.AddModelError("", "Invalid dates.");
                PopulateDropDowns(reservation.RoomId, reservation.GuestId);
                return View(reservation);
            }

            if (IsRoomOverlapping(reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate))
            {
                ModelState.AddModelError("", "Room is already booked.");
                PopulateDropDowns(reservation.RoomId, reservation.GuestId);
                return View(reservation);
            }

            var room = _context.Rooms.Find(reservation.RoomId);
            reservation.TotalPrice = (reservation.CheckOutDate - reservation.CheckInDate).Days * room.PricePerNight;
            reservation.Status = "Active";
            reservation.CreatedAt = DateTime.Now;

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // ADMIN – EDIT
        // ============================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            PopulateDropDowns(reservation.RoomId, reservation.GuestId);
            return View(reservation);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reservation reservation)
        {
            if (id != reservation.Id)
                return NotFound();

            if (reservation.CheckOutDate <= reservation.CheckInDate)
            {
                ModelState.AddModelError("", "Check-out must be after check-in.");
                PopulateDropDowns(reservation.RoomId, reservation.GuestId);
                return View(reservation);
            }

            if (IsRoomOverlapping(reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate, reservation.Id))
            {
                ModelState.AddModelError("", "Room is already booked for these dates.");
                PopulateDropDowns(reservation.RoomId, reservation.GuestId);
                return View(reservation);
            }

            try
            {
                _context.Update(reservation);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservations.Any(e => e.Id == reservation.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // ADMIN – DELETE
        // ============================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private void PopulateDropDowns(int? roomId = null, int? guestId = null)
        {
            ViewData["RoomId"] = new SelectList(_context.Rooms.OrderBy(r => r.RoomNumber), "Id", "RoomNumber", roomId);
            ViewData["GuestId"] = new SelectList(_context.Guests.OrderBy(g => g.FullName), "Id", "FullName", guestId);
        }

        private bool IsRoomOverlapping(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId = null)
        {
            var query = _context.Reservations
                .Where(r => r.RoomId == roomId && r.Status == "Active");

            if (excludeId.HasValue)
                query = query.Where(r => r.Id != excludeId.Value);

            return query.Any(r =>
                checkIn < r.CheckOutDate &&
                checkOut > r.CheckInDate);
        }
    }
}
