using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobSearchWebsite.Models
{
    public class UserProfile
    {
        [Key]
        [ForeignKey("User")]
        public string Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Education { get; set; }

        public string? Experience { get; set; }

        public string? Skills { get; set; }

        public string? CVUrl { get; set; }

        public bool IsPublic { get; set; } = false;

        // Thuộc tính mới: Danh mục ưa thích
        public string? PreferredCategories { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}