﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThAmCo.Events.Data
{
    public class Reservation
    {
        public int Id { get; set; }

        [Key, MinLength(13), MaxLength(13)]
        public string Reference { get; set; }

        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required]
        public string VenueCode { get; set; }

        public DateTime WhenMade { get; set; }

        [Required]
        public string StaffId { get; set; }
    }
}
