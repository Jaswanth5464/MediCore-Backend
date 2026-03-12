using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCore.API.Modules.Laboratory.Models
{
    [Table("LabTestMasters")]
    public class LabTestMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string TestName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string NormalRange { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int TurnaroundTimeHours { get; set; } = 24;

        public bool IsActive { get; set; } = true;
    }
}
