using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobSearchWebsite.Models
{
    public class JobApplication
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ Cột tự tăng
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }  // được gán từ controller

        public string UserId { get; set; }  // từ _userManager

        public string CVUrl { get; set; }  // có thể null

        [Required]
        [StringLength(1000)]
        public string CoverLetter { get; set; }

        public string Status { get; set; }  // gán thủ công: "Pending"

        public DateTime AppliedDate { get; set; }

        [ForeignKey("JobId")]
        public Job Job { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
