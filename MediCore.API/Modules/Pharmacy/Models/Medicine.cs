using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCore.API.Modules.Pharmacy.Models
{
    [Table("Medicines")]
    public class Medicine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string GenericName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = "Tablet"; // Tablet, Syrup, Injection, etc.

        [MaxLength(200)]
        public string Manufacturer { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }
        
        public int LowStockThreshold { get; set; } = 50;
        
        public DateTime? ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
