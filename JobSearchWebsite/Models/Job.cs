using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobSearchWebsite.Models
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Company { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string Description { get; set; }

        // Cập nhật trạng thái mặc định
        public string Status { get; set; } = "Chờ duyệt"; // Thay "Mới tạo" bằng "Chờ duyệt"

        public string UserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public ApplicationUser User { get; set; }
        public List<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public DateTime? SavedDate { get; set; }
    }
}