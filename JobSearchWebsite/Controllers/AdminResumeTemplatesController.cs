using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace JobSearchWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminResumeTemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminResumeTemplatesController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
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

        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResumeTemplate model, IFormFile file)
        {
            Console.WriteLine($"Debug: ModelState.IsValid = {ModelState.IsValid}");
            Console.WriteLine($"Debug: Model - Title = {model.Title}, Content = {model.Content}, File = {(file != null ? file.FileName : "null")}");
            ModelState.Remove("FilePath");
            ModelState.Remove("FileType");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedByUser");
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Debug: Validation Error - {error.ErrorMessage}");
                }
                Console.WriteLine("Debug: ModelState invalid, returning to view");
                return View(model);
            }

            model.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.CreatedDate = DateTime.Now;

            if (file != null && file.Length > 0)
            {
                var uploadsDir = Path.Combine(_hostEnvironment.WebRootPath, "Uploads", "resume-templates");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                Console.WriteLine($"Debug: Saving file to {filePath}");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                model.FilePath = "/Uploads/resume-templates/" + fileName;
                model.FileType = Path.GetExtension(file.FileName).ToLower().TrimStart('.');
            }

            _context.Add(model);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Debug: CV mẫu created with Id = {model.Id}");
            TempData["SuccessMessage"] = "Tạo CV mẫu thành công!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var template = await _context.ResumeTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound();
            }
            return View(template);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResumeTemplate model, IFormFile file)
        {
            if (id != model.Id)
            {
                return NotFound();
            }
            ModelState.Remove("FilePath");
            ModelState.Remove("FileType");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedByUser");
            if (ModelState.IsValid)
            {
                try
                {
                    var template = await _context.ResumeTemplates.FindAsync(id);
                    if (template == null)
                    {
                        return NotFound();
                    }

                    template.Title = model.Title;
                    template.Content = model.Content;
                    template.UpdatedDate = DateTime.Now;

                    if (file != null && file.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(template.FilePath))
                        {
                            var oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, template.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        var uploadsDir = Path.Combine(_hostEnvironment.WebRootPath, "Uploads", "resume-templates");
                        if (!Directory.Exists(uploadsDir))
                            Directory.CreateDirectory(uploadsDir);

                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        template.FilePath = "/Uploads/resume-templates/" + fileName;
                        template.FileType = Path.GetExtension(file.FileName).ToLower().TrimStart('.');
                    }

                    _context.Update(template);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật CV mẫu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.ResumeTemplates.AnyAsync(rt => rt.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.ResumeTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound();
            }
            return View(template);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var template = await _context.ResumeTemplates.FindAsync(id);
            if (template != null)
            {
                if (!string.IsNullOrEmpty(template.FilePath))
                {
                    var filePath = Path.Combine(_hostEnvironment.WebRootPath, template.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.ResumeTemplates.Remove(template);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa CV mẫu thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}