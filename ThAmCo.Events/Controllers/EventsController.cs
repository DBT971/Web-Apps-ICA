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
            return View(await _context.Events.Include(s => s.Bookings).Include(s => s.StaffBookings).ThenInclude(s => s.Staff).ToListAsync());
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            /** This is what shows the guests as a table on the events details page.
             * The if statement returns not Found as in order to reach that an event would have to exist
             * with no customers or bookings.
             */
            var @event = await _context.Events.Include(b => b.Bookings).ThenInclude(c => c.Customer).Include(s => s.StaffBookings).ThenInclude(s => s.Staff).FirstOrDefaultAsync(m => m.Id == id);

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Date,Duration,TypeId")] Event @event)
        {
            /** This adds the new event to the the DB Context, and after creating the event returns the user to the index page.
             *If the new event is not valid, it is returned to the create view with all of the details filled in for usability.
             */
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
                /** This updates the event with the new information from the edit view.
                 */
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

        //GET: Events/ReserveVenue/5
        public async Task<IActionResult> ReserveVenue(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Checks to see if the event exists
            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }
               
            /**
             * This creates the path for the events controller to follow to get available
             * venues to reserve
             */
            HttpClient client = new HttpClient();
            var VenueBuilder = new UriBuilder("http://localhost");
            VenueBuilder.Port = 23652;
            VenueBuilder.Path = "api/Availability";

            /**
             * This creates a query for the venue with the event details to find which
             * venue is available on the day, for the event type.
             */
            var VenueQuery = HttpUtility.ParseQueryString(VenueBuilder.Query);
            VenueQuery["eventType"] = @event.TypeId;
            VenueQuery["beginDate"] = @event.Date.ToString("yyyy/MM/dd HH:mm:ss");
            VenueQuery["endDate"] = @event.Date.Add(@event.Duration.Value).ToString("yyyy/MM/dd HH:mm:ss");
            VenueBuilder.Query = VenueQuery.ToString();

            //This creates the url for the system to check in order to get the information needed.
            String url = VenueBuilder.ToString();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            HttpResponseMessage response = await client.GetAsync(url);

            /**
             * If the response is  valid, create the new Venue
             * Else, send a custom error message.
             */
            if (response.IsSuccessStatusCode)
            {
                var Venue = await response.Content.ReadAsAsync<IEnumerable<Venue>>();

                ViewData["Venues"] = new SelectList(Venue, "Code", "Name");

            }
            else
            {
                ModelState.AddModelError("", "Connection has failed, please try again.");
            }

            return View();
        }

        //POST: Events/ReserveVenue/5
        public async Task<IActionResult> ConfirmReservation(int id, string VenueCode, int StaffId)
        {

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
                StaffId = StaffId
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

        public async Task<IActionResult> Cancel(int id)
        {
            var @event = await _context.Events.Include(e => e.StaffBookings).FirstOrDefaultAsync(m => m.Id == id);

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

            if (@event.StaffBookings.Count() > 0)
            {
                foreach(StaffBooking s in @event.StaffBookings)
                {
                    _context.Remove(s);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
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
