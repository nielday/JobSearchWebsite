using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace JobSearchWebsite.Models
{
    public class ApplyModel
    {
        [Required]
        public int JobId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thư ứng tuyển")]
        [StringLength(1000, ErrorMessage = "Thư ứng tuyển không được vượt quá 1000 ký tự")]
        public string CoverLetter { get; set; }

        [Display(Name = "Upload CV (PDF)")]
        public IFormFile CVFile { get; set; }
    }
}
