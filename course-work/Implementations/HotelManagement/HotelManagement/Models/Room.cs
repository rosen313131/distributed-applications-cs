using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HotelManagement.Models
{
    public class Room
    {
        public int Id { get; set; }


        [Required]
        public int RoomNumber { get; set; } // int


        [Required, StringLength(50)]
        public string Type { get; set; } // varchar(50)


        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerNight { get; set; } // decimal


        [Required, Range(1, 20)]
        public int Capacity { get; set; } // int


        [Required]
        public bool HasWifi { get; set; } // bool


        [StringLength(255)]
        public string? Description { get; set; } // varchar(255)


        public DateTime? LastRenovated { get; set; } // DateTime (nullable)
    }
}
