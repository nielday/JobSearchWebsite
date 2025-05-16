using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.IO;

namespace JobSearchWebsite.Controllers
{
    public class CareerGuidesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CareerGuidesController> _logger;

        public CareerGuidesController(ApplicationDbContext context, ILogger<CareerGuidesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var careerGuides = await _context.CareerGuides
                .Include(cg => cg.Author)
                .OrderByDescending(cg => cg.CreatedDate)
                .ToListAsync();
            return View(careerGuides);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CareerGuide careerGuide, IFormFile imageFile)
        {
            careerGuide.AuthorId = "9b8743d6-1353-4c3a-9c30-077f8f32c74a";
            careerGuide.CreatedDate = DateTime.Now; // Gán thời gian hiện tại

            ModelState.Remove("Author");
            ModelState.Remove("AuthorId");
            ModelState.Remove("ImageUrl");

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/careerguides", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                careerGuide.ImageUrl = "/images/careerguides/" + fileName;
            }
            else
            {
                careerGuide.ImageUrl = null;
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {0}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(careerGuide);
            }

            try
            {
                _context.Add(careerGuide);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Career guide '{careerGuide.Title}' created successfully by Admin.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving career guide to database.");
                ViewData["ErrorMessage"] = "Có lỗi xảy ra khi lưu cẩm nang. Vui lòng thử lại.";
                return View(careerGuide);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var careerGuide = await _context.CareerGuides
                .Include(cg => cg.Author)
                .FirstOrDefaultAsync(cg => cg.Id == id);
            if (careerGuide == null)
            {
                return NotFound();
            }
            return View(careerGuide);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CareerGuide careerGuide, IFormFile imageFile)
        {
            if (id != careerGuide.Id)
            {
                return NotFound();
            }

            careerGuide.AuthorId = "9b8743d6-1353-4c3a-9c30-077f8f32c74a";

            ModelState.Remove("Author");
            ModelState.Remove("AuthorId");
            ModelState.Remove("ImageUrl");

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/careerguides", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                careerGuide.ImageUrl = "/images/careerguides/" + fileName;
            }
            else
            {
                var existingGuide = await _context.CareerGuides.AsNoTracking().FirstOrDefaultAsync(cg => cg.Id == id);
                careerGuide.ImageUrl = existingGuide.ImageUrl ?? "/images/default-career-guide.jpg";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingGuide = await _context.CareerGuides.AsNoTracking().FirstOrDefaultAsync(cg => cg.Id == id);
                    careerGuide.CreatedDate = existingGuide.CreatedDate;
                    _context.Update(careerGuide);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Career guide '{careerGuide.Title}' updated successfully.");
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CareerGuides.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating career guide in database.");
                    ViewData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật cẩm nang. Vui lòng thử lại.";
                    return View(careerGuide);
                }
            }

            _logger.LogWarning("ModelState is invalid. Errors: {0}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return View(careerGuide);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var careerGuide = await _context.CareerGuides
                .Include(cg => cg.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (careerGuide == null)
            {
                return NotFound();
            }

            return View(careerGuide);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var careerGuide = await _context.CareerGuides.FindAsync(id);
            if (careerGuide != null)
            {
                _context.CareerGuides.Remove(careerGuide);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Career guide with ID {id} deleted successfully.");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}