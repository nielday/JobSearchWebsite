using System;
using System.ComponentModel.DataAnnotations;

namespace JobSearchWebsite.Models
{
    public class ResumeTemplate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; }

        public string Content { get; set; }

        public string FilePath { get; set; }

        public string FileType { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public ApplicationUser CreatedByUser { get; set; }
    }
}