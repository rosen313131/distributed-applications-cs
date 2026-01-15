using System.ComponentModel.DataAnnotations;
namespace HotelManagement.Models;

public class Guest
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(255)]
    public string? Notes { get; set; }
}
