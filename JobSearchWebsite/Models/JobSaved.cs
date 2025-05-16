using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace JobSearchWebsite.Models
{
    public class JobSaved
    {
        public int Id { get; set; }

        [Required]
        public string JobSeekerId { get; set; }
        public ApplicationUser JobSeeker { get; set; }

        [Required]
        public int JobId { get; set; }
        public Job Job { get; set; }

        public DateTime SavedDate { get; set; }
    }
}