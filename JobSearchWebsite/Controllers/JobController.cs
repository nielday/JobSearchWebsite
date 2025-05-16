using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using System.Threading.Tasks;

namespace JobSearchWebsite.Controllers
{
    [Authorize(Roles = "Employer,Admin")]
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job job)
        {
            ModelState.Remove("User");
            if (!ModelState.IsValid)
            {
                return View(job);
            }

            var user = await _userManager.GetUserAsync(User);
            job.UserId = user.Id;
            job.CreatedDate = DateTime.Now;
            job.Status = "Chờ duyệt";

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Công việc đã được tạo và đang chờ duyệt!";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy công việc.";
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (job.UserId != user.Id && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa công việc này.";
                return Forbid();
            }

            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job job)
        {
            if (id != job.Id)
            {
                TempData["ErrorMessage"] = "ID công việc không khớp.";
                return BadRequest();
            }

            ModelState.Remove("User");
            if (!ModelState.IsValid)
            {
                return View(job);
            }

            var existingJob = await _context.Jobs.FindAsync(id);
            if (existingJob == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy công việc.";
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (existingJob.UserId != user.Id && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa công việc này.";
                return Forbid();
            }

            existingJob.Title = job.Title;
            existingJob.Company = job.Company;
            existingJob.Location = job.Location;
            existingJob.Description = job.Description;
            existingJob.Status = "Chờ duyệt";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Công việc đã được cập nhật và đang chờ duyệt!";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Applications) // Bao gồm Applications để hiển thị thông tin nếu cần
                .FirstOrDefaultAsync(j => j.Id == id);
            if (job == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy công việc.";
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (job.UserId != user.Id && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa công việc này.";
                return Forbid();
            }

            return View(job);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Applications) // Bao gồm Applications để kiểm tra
                .FirstOrDefaultAsync(j => j.Id == id);
            if (job == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy công việc.";
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (job.UserId != user.Id && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa công việc này.";
                return Forbid();
            }

            // Kiểm tra nếu có đơn ứng tuyển
            if (job.Applications.Any())
            {
                TempData["ErrorMessage"] = "Việc này đã có người ứng tuyển.";
                return RedirectToAction("Index", "Home");
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Công việc đã được xóa thành công!";
            return RedirectToAction("Index", "Home");
        }
    }
}