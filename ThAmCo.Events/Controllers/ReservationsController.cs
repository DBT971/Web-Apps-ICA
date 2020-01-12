using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ThAmCo.Events.Data;
using ThAmCo.Events.Services;

namespace ThAmCo.Events.Controllers
{


    public class ReservationsController : Controller
    {

        private readonly EventsDbContext _context;

        public ReservationsController(EventsDbContext context)
        {
            _context = context;
        }

        //GET: Reservations
        public async Task<ActionResult> Index(int? id)
        {
            if(id == null)
            {
                return BadRequest();
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri("http://localhost:23652");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            ReservationPostDto req = new ReservationPostDto
            {
                EventDate = new DateTime(2018,6,6),
                VenueCode = "",
                StaffId = "staff"
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("api/Reservations", req);

            client.Timeout = TimeSpan.FromSeconds(5);

            response.EnsureSuccessStatusCode();

            return View(req);
        }

        //GET: Reservations/Details/
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            return View();
        }
    }
}