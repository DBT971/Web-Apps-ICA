using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ThAmCo.Events.Services
{
    public class ReservationPostDto
    {
        public string Reference { get; set; }

        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        public string VenueCode { get; set; }

        public DateTime WhenMade { get; set; }

        public string StaffId { get; set; }

        public static ReservationPostDto FromModel(Data.Reservation reservation)
        {
            return new ReservationPostDto
            {
                Reference = reservation.Reference,
                EventDate = reservation.EventDate,
                VenueCode = reservation.VenueCode,
                WhenMade = reservation.WhenMade,
                StaffId = reservation.StaffId
            };
        }
    }
}
