using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using System.IO;
using System.Threading.Tasks;

namespace JobSearchWebsite.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class JobSeekerResumeTemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public JobSeekerResumeTemplatesController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var templates = await _context.ResumeTemplates
                .Include(rt => rt.CreatedByUser)
                .OrderByDescending(rt => rt.CreatedDate)
                .ToListAsync();
            return View(templates);
        }

        public async Task<IActionResult> Details(int id)
        {
            var template = await _context.ResumeTemplates
                .Include(rt => rt.CreatedByUser)
                .FirstOrDefaultAsync(rt => rt.Id == id);
            if (template == null)
            {
                return NotFound();
            }
            return View(template);
        }

        public async Task<IActionResult> Download(int id)
        {
            var template = await _context.ResumeTemplates.FindAsync(id);
            if (template == null || string.IsNullOrEmpty(template.FilePath))
            {
                return NotFound();
            }

            var filePath = Path.Combine(_hostEnvironment.WebRootPath, template.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, GetMimeType(template.FileType), Path.GetFileName(filePath));
        }

        private string GetMimeType(string fileType)
        {
            switch (fileType?.ToLower())
            {
                case "pdf": return "application/pdf";
                case "docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "doc": return "application/msword";
                default: return "application/octet-stream";
            }
        }
    }
}