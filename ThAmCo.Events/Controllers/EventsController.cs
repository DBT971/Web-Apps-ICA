using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThAmCo.Events.Data;
using ThAmCo.Events.Services;

namespace ThAmCo.Events.Controllers
{
    public class EventsController : Controller
    {
        private readonly EventsDbContext _context;

        public EventsController(EventsDbContext context)
        {
            _context = context;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            return View(await _context.Events.ToListAsync());
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            /** This is what shows the guests as a table on the events details page
             * 
             */
            var @event = await _context.Events
                .Include(b => b.Bookings)
                .ThenInclude(c => c.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Date,Duration,TypeId")] Event @event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string Title, TimeSpan Duration, Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(Title) || Duration == null)
            {

                return NotFound();
            }
            else
            {
                try
                {
                    Event e = await _context.Events.FindAsync(id);
                    e.Title = Title;
                    e.Duration = Duration;
                    await _context.SaveChangesAsync();
                    _context.Update(e);


                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> ReserveVenue(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            HttpClient client = new HttpClient();

            var VenueBuilder = new UriBuilder("http://localhost");
            VenueBuilder.Port = 23652;
            VenueBuilder.Path = "api/Availability";

            var VenueQuery = HttpUtility.ParseQueryString(VenueBuilder.Query);
            VenueQuery["eventType"] = @event.TypeId;
            VenueQuery["beginDate"] = @event.Date.ToString("yyyy/MM/dd HH:mm:ss");
            VenueQuery["endDate"] = @event.Date.Add(@event.Duration.Value).ToString("yyyy/MM/dd HH:mm:ss");

            VenueBuilder.Query = VenueQuery.ToString();

            String url = VenueBuilder.ToString();

            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            
            HttpResponseMessage response = await client.GetAsync(url);


            if (response.IsSuccessStatusCode)
            {
                var Venue = await response.Content.ReadAsAsync<IEnumerable<Venue>>();

                ViewData["Venues"] = new SelectList(Venue, "Code", "Name");

            }
            else
            {
                ModelState.AddModelError("", "You've messed up Jim");
            }

            return View();
        }

        public async Task<IActionResult> ConfirmReservation(int id, string VenueCode)
        {

            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            if (@event.VenueCode != null)
            {
                HttpClient client1 = new HttpClient();

                var VenueBuilder = new UriBuilder("http://localhost");
                VenueBuilder.Port = 23652;
                VenueBuilder.Path = "api/Reservations/" + @event.VenueCode;
                client1.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                string url = VenueBuilder.ToString();
                

                HttpResponseMessage response1 = await client1.DeleteAsync(url);

                if (response1.IsSuccessStatusCode)
                {
                    
                    @event.VenueCode = null;
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
            }

            HttpClient client = new HttpClient();

            client.BaseAddress = new System.Uri("http://localhost:23652");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            ReservationPostDto req = new ReservationPostDto
            {
                EventDate = @event.Date,
                VenueCode = VenueCode,
                StaffId = "staff"
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("api/Reservations", req);

            if (response.IsSuccessStatusCode)
            {
                var Reservation = await response.Content.ReadAsAsync<ReservationGetDto>();

                @event.VenueCode = Reservation.Reference;
                _context.Update(@event);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);

            if (@event.VenueCode != null)
            {
                HttpClient client1 = new HttpClient();

                var VenueBuilder = new UriBuilder("http://localhost");
                VenueBuilder.Port = 23652;
                VenueBuilder.Path = "api/Reservations/" + @event.VenueCode;
                client1.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                string url = VenueBuilder.ToString();


                HttpResponseMessage response1 = await client1.DeleteAsync(url);

                if (response1.IsSuccessStatusCode)
                {

                    @event.VenueCode = null;
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
            }

            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
