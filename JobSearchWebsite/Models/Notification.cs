using System;
using System.ComponentModel.DataAnnotations;

namespace JobSearchWebsite.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "UserId là bắt buộc")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Message là bắt buộc")]
        [StringLength(4000, ErrorMessage = "Message không được vượt quá 4000 ký tự")]
        public string Message { get; set; }

        public bool IsRead { get; set; }

        [Required(ErrorMessage = "CreatedDate là bắt buộc")]
        public DateTime CreatedDate { get; set; }

        public ApplicationUser User { get; set; }
    }
}