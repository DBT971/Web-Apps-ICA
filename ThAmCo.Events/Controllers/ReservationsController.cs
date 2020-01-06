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
using ThAmCo.Events.Services;

namespace ThAmCo.Events.Controllers
{
    public class ReservationsController : Controller
    {
        //GET: Reservations
        public async Task<ActionResult> Index()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri("http://localhost:23652");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            ReservationPostDto req = new ReservationPostDto
            {
                EventDate = @event.Date,
                VenueCode = VenueCode,
                StaffId = "staff"
            };
            //var client = new HttpClient
            //{
            //    BaseAddress = new Uri("https://localhost:23652")
            //};
            //client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            //client.Timeout = TimeSpan.FromSeconds(5);

            //var response = await client.GetAsync("api/reservations/getreservation");
            //response.EnsureSuccessStatusCode();
            //IEnumerable<ThAmCo.Events.Services.ReservationGetDto> reservations = await response.Content.ReadAsAsync<IEnumerable<ThAmCo.Events.Services.ReservationGetDto>>();

            //return View(reservations);
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