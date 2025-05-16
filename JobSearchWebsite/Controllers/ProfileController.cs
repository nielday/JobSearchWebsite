using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace JobSearchWebsite.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // GET: Profile/Applications
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Applications()
        {
            var user = await _userManager.GetUserAsync(User);
            var applications = await _context.JobApplications
                .Include(ja => ja.Job) // Lấy thông tin công việc
                .Where(ja => ja.UserId == user.Id) // Chỉ lấy đơn của user hiện tại
                .OrderByDescending(ja => ja.AppliedDate) // Sắp xếp theo ngày nộp
                .ToListAsync();
            return View(applications);
        }
        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.Id == user.Id);
            if (profile == null)
            {
                profile = new UserProfile(); // Gửi model trống
            }
            return View(profile);
        }

        // POST: Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserProfile model, IFormFile CVFile)
        {
            ModelState.Remove("Id");
            ModelState.Remove("User");
            ModelState.Remove("CVUrl");
            ModelState.Remove("CVFile");

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            var existing = await _context.UserProfiles.FindAsync(user.Id);

            string cvUrl = null;
            if (CVFile != null && CVFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cvs");
                Directory.CreateDirectory(uploadsFolder); // Đảm bảo thư mục tồn tại

                var fileName = $"{user.Id}_{Path.GetFileName(CVFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await CVFile.CopyToAsync(stream);
                }
                cvUrl = "/uploads/cvs/" + fileName;
            }

            if (existing != null)
            {
                // Cập nhật
                existing.FullName = model.FullName;
                existing.DateOfBirth = model.DateOfBirth;
                existing.Address = model.Address;
                existing.PhoneNumber = model.PhoneNumber;
                existing.Education = model.Education;
                existing.Experience = model.Experience;
                existing.Skills = model.Skills;
                existing.IsPublic = model.IsPublic;
                if (cvUrl != null) existing.CVUrl = cvUrl;
            }
            else
            {
                // Tạo mới
                model.Id = user.Id;
                if (cvUrl != null) model.CVUrl = cvUrl;
                _context.UserProfiles.Add(model);
            }

            await _context.SaveChangesAsync();
            ViewBag.Message = "Bạn đã tạo CV thành công!";
            return View(model);
        }
    }
}
