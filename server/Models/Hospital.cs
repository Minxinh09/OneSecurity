using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneSecurity.Server.Models
{
    [Table("hospitals")]
    public class Hospital
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty; // e.g. TỔNG, HOSP_A, HOSP_A1, HOSP_B

        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public Hospital? Parent { get; set; }

        public ICollection<Hospital> Children { get; set; } = new List<Hospital>();
    }
}