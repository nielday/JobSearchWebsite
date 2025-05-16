using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace JobSearchWebsite.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime RegisteredDate { get; set; } = DateTime.Now;
        public UserProfile Profile { get; set; }

        // Navigation properties
        public List<Job> Jobs { get; set; }
        public List<JobApplication> Applications { get; set; } = new List<JobApplication>();
    }
}