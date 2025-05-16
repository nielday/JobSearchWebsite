using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using JobSearchWebsite.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace JobSearchWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment hostEnvironment,
            IEmailSender emailSender,
            ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (await _userManager.IsInRoleAsync(user, "JobSeeker"))
                {
                    return RedirectToAction("Index", "JobSeeker");
                }
            }

            var jobs = _context.Jobs
                .Include(j => j.User)
                .AsQueryable();

            // Nếu không phải Admin, chỉ hiển thị công việc đã duyệt
            if (!User.IsInRole("Admin"))
            {
                jobs = jobs.Where(j => j.Status == "Đã duyệt");
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                jobs = jobs.Where(j =>
                    j.Title.Contains(searchString) ||
                    j.Description.Contains(searchString) ||
                    j.Company.Contains(searchString));
            }

            // Sắp xếp công việc theo ngày tạo (mới nhất trước)
            jobs = jobs.OrderByDescending(j => j.CreatedDate);

            // Phân trang
            int jobsPerPage = 9;
            int totalJobs = await jobs.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalJobs / jobsPerPage);
            page = Math.Max(1, Math.Min(page, totalPages)); // Đảm bảo page hợp lệ

            var paginatedJobs = await jobs
                .Skip((page - 1) * jobsPerPage)
                .Take(jobsPerPage)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentFilter = searchString;

            return View(paginatedJobs);
        }

        public IActionResult Details(int id)
        {
            var job = _context.Jobs
                .Include(j => j.User)
                .FirstOrDefault(j => j.Id == id && j.Status == "Đã duyệt");
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveJob(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.User)
                .FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();

            job.Status = "Đã duyệt";
            _context.Notifications.Add(new Notification
            {
                UserId = job.UserId,
                Message = $"Công việc '{job.Title}' đã được duyệt.",
                IsRead = false,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            if (job.User != null && !string.IsNullOrEmpty(job.User.Email))
            {
                var subject = "Công việc đã được duyệt";
                var body = $"<p>Công việc <strong>{job.Title}</strong> đã được duyệt và hiển thị công khai.</p>";
                await _emailSender.SendEmailAsync(job.User.Email, subject, body);
            }

            TempData["SuccessMessage"] = "Duyệt công việc thành công!";
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}