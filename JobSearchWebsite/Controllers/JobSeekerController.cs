using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using JobSearchWebsite.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace JobSearchWebsite.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class JobSeekerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<JobSeekerController> _logger;
        private readonly NotificationService _notificationService;

        public JobSeekerController(ApplicationDbContext context,
                                   UserManager<ApplicationUser> userManager,
                                   IWebHostEnvironment hostEnvironment,
                                   IEmailSender emailSender,
                                   ILogger<JobSeekerController> logger,
                                   NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(string searchString, string location, string category, int page = 1, int pageSize = 10)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var userId = _userManager.GetUserId(User);
            var savedJobs = await _context.JobSaveds
                .Include(js => js.Job)
                .Where(js => js.JobSeekerId == userId)
                .OrderByDescending(js => js.SavedDate)
                .Select(js => js.Job)
                .Take(10)
                .ToListAsync();
            ViewBag.SavedJobs = savedJobs;

            var jobsQuery = _context.Jobs
                .Include(j => j.User)
                .Where(j => j.Status == "Đã duyệt")
                .AsQueryable();

            var totalJobsBeforeFilter = await jobsQuery.CountAsync();
            Console.WriteLine($"Total jobs before filter: {totalJobsBeforeFilter}");

            if (!string.IsNullOrEmpty(searchString))
            {
                jobsQuery = jobsQuery.Where(j =>
                    j.Title.Contains(searchString) ||
                    j.Description.Contains(searchString) ||
                    j.Company.Contains(searchString) ||
                    j.Location.Contains(searchString));
                Console.WriteLine($"Filtering by searchString: {searchString}");
            }

            if (!string.IsNullOrEmpty(location))
            {
                jobsQuery = jobsQuery.Where(j => j.Location != null && j.Location.Trim() == location.Trim());
                Console.WriteLine($"Filtering by location: {location}");
            }

            if (!string.IsNullOrEmpty(category))
            {
                jobsQuery = jobsQuery.Where(j =>
                    j.Title.Contains(category) ||
                    j.Description.Contains(category));
                Console.WriteLine($"Filtering by category: {category}");
            }

            var totalItems = await jobsQuery.CountAsync();
            var jobList = await jobsQuery
                .OrderByDescending(j => j.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var locations = await _context.Jobs
                .Where(j => j.Status == "Đã duyệt" && !string.IsNullOrWhiteSpace(j.Location))
                .Select(j => j.Location.Trim())
                .Distinct()
                .ToListAsync();
            Console.WriteLine($"Available locations: {string.Join(", ", locations)}");

            ViewBag.UnreadNotificationCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.Locations = locations;
            ViewBag.SelectedLocation = location;
            ViewBag.SearchString = searchString;
            ViewBag.Category = category;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;

            Console.WriteLine("Jobs returned in Index: " + string.Join(", ", jobList.Select(j => j.Id)));
            return View(jobList);
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

        [HttpGet]
        public IActionResult Apply(int id)
        {
            var job = _context.Jobs
                .FirstOrDefault(j => j.Id == id && j.Status == "Đã duyệt");
            if (job == null)
            {
                return NotFound();
            }

            ViewBag.JobTitle = job.Title;

            var model = new ApplyModel
            {
                JobId = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.JobTitle = _context.Jobs.FirstOrDefault(j => j.Id == model.JobId)?.Title;
                return View(model);
            }

            var job = await _context.Jobs
                .Include(j => j.User)
                .FirstOrDefaultAsync(j => j.Id == model.JobId && j.Status == "Đã duyệt");
            if (job == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy công việc hoặc công việc chưa được duyệt.";
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return View(model);
            }

            var application = new JobApplication
            {
                JobId = model.JobId,
                CoverLetter = model.CoverLetter,
                AppliedDate = DateTime.Now,
                Status = "Pending",
                UserId = user.Id
            };

            if (model.CVFile != null && model.CVFile.Length > 0)
            {
                if (!model.CVFile.FileName.EndsWith(".pdf"))
                {
                    ModelState.AddModelError("CVFile", "Chỉ chấp nhận file PDF.");
                    ViewBag.JobTitle = job.Title;
                    return View(model);
                }

                var uploadsDir = Path.Combine(_hostEnvironment.WebRootPath, "Uploads", "cvs");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.CVFile.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CVFile.CopyToAsync(stream);
                }

                application.CVUrl = "/Uploads/cvs/" + fileName;
            }

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            if (job.User != null && !string.IsNullOrEmpty(job.User.Email))
            {
                var subject = "Có đơn ứng tuyển mới";
                var body = $"<p>Công việc: <strong>{job.Title}</strong></p>" +
                           $"<p>Ứng viên: {user.Email}</p>" +
                           "<p>Hãy đăng nhập hệ thống để xem chi tiết.</p>";

                try
                {
                    await _emailSender.SendEmailAsync(job.User.Email, subject, body);
                    await _notificationService.SendNotificationAsync(user.Id, $"Đơn ứng tuyển cho '{job.Title}' đã được gửi.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email đến employer {Email}", job.User.Email);
                }
            }

            TempData["SuccessMessage"] = "Đơn ứng tuyển đã được gửi thành công!";
            return RedirectToAction("Details", new { id = model.JobId });
        }

        public async Task<IActionResult> Applications()
        {
            var user = await _userManager.GetUserAsync(User);
            var applications = await _context.JobApplications
                .Include(ja => ja.Job)
                .Where(ja => ja.UserId == user.Id)
                .OrderByDescending(ja => ja.AppliedDate)
                .ToListAsync();

            var userId = _userManager.GetUserId(User);
            var savedJobs = await _context.JobSaveds
                .Include(js => js.Job)
                .Where(js => js.JobSeekerId == userId)
                .OrderByDescending(js => js.SavedDate)
                .Select(js => js.Job)
                .Take(10)
                .ToListAsync();
            ViewBag.SavedJobs = savedJobs;

            return View(applications);
        }

        public async Task<IActionResult> SavedJobs()
        {
            var userId = _userManager.GetUserId(User);
            var savedJobsWithDates = await _context.JobSaveds
                .Include(js => js.Job)
                .Where(js => js.JobSeekerId == userId)
                .OrderByDescending(js => js.SavedDate)
                .Select(js => new
                {
                    Job = js.Job,
                    SavedDate = js.SavedDate
                })
                .ToListAsync();

            var model = savedJobsWithDates.Select(x => new Job
            {
                Id = x.Job.Id,
                Title = x.Job.Title,
                Company = x.Job.Company,
                Location = x.Job.Location,
                Description = x.Job.Description,
                SavedDate = x.SavedDate
            }).ToList();

            ViewBag.SavedJobs = model;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveJob([FromBody] SaveJobRequest request)
        {
            var userId = _userManager.GetUserId(User);
            var roles = await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(userId));
            Console.WriteLine($"Debug: userId = {userId}, Roles = {string.Join(", ", roles)}, jobId received = {request.JobId}");

            if (request.JobId <= 0)
            {
                Console.WriteLine($"Debug: Invalid jobId received: {request.JobId}");
                return Json(new { success = false, message = "ID công việc không hợp lệ!" });
            }

            if (await _context.JobSaveds.AnyAsync(js => js.JobSeekerId == userId && js.JobId == request.JobId))
            {
                return Json(new { success = false, message = "Công việc đã được lưu trước đó!" });
            }

            var validJobIds = Enumerable.Range(1, 60).ToList();
            if (!validJobIds.Contains(request.JobId))
            {
                Console.WriteLine($"Debug: Job with Id {request.JobId} does not exist in valid job IDs.");
                return Json(new { success = false, message = "Công việc không tồn tại!" });
            }

            var jobSaved = new JobSaved
            {
                JobSeekerId = userId,
                JobId = request.JobId,
                SavedDate = DateTime.Now
            };
            _context.JobSaveds.Add(jobSaved);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã lưu công việc thành công!" });
        }

        public class SaveJobRequest
        {
            public int JobId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSavedJob(int jobId)
        {
            var userId = _userManager.GetUserId(User);
            Console.WriteLine($"Debug: userId = {userId}, jobId received = {jobId}");

            // Kiểm tra jobId có trong danh sách validJobIds
            var validJobIds = Enumerable.Range(1, 60).ToList();
            if (!validJobIds.Contains(jobId))
            {
                Console.WriteLine($"Debug: Job with Id {jobId} does not exist in valid job IDs.");
                return Json(new { success = false, message = "Đã xóa thành công!" });
            }
            // Kiểm tra jobId hợp lệ
            if (jobId <= 0)
            {
                Console.WriteLine($"Debug: Invalid jobId received: {jobId}");
                return Json(new { success = false, message = "Đã xóa thành công!" });
            }


            // Kiểm tra xem bản ghi JobSaved có tồn tại không
            var jobSavedExists = await _context.JobSaveds
                .AnyAsync(js => js.JobSeekerId == userId && js.JobId == jobId);
            if (!jobSavedExists)
            {
                return Json(new { success = false, message = "Không tìm thấy công việc để xóa!" });
            }

            // Xóa bản ghi JobSaved
            var jobSaved = await _context.JobSaveds
                .FirstOrDefaultAsync(js => js.JobSeekerId == userId && js.JobId == jobId);
            _context.JobSaveds.Remove(jobSaved);
            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationAsync(userId, $"Đã xóa công việc khỏi danh sách lưu.");
            return Json(new { success = true, message = "Đã xóa công việc khỏi danh sách lưu!" });
        }

        [HttpPost]
        public async Task<IActionResult> NotifyJob(int jobId)
        {
            var userId = _userManager.GetUserId(User);
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null || job.Status != "Đã duyệt")
            {
                return Json(new { success = false, message = "Công việc không tồn tại hoặc chưa được duyệt!" });
            }

            await _notificationService.SendNotificationAsync(userId, $"Nhận thông báo cho công việc '{job.Title}'.");
            return Json(new { success = true, message = "Đã bật thông báo cho công việc này!" });
        }

        public async Task<IActionResult> AllNotifications(int page = 1, int pageSize = 10)
        {
            var userId = _userManager.GetUserId(User);
            var notificationsQuery = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .AsQueryable();

            var totalItems = await notificationsQuery.CountAsync();
            var notifications = await notificationsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var notificationList = new List<dynamic>();
            foreach (var notification in notifications)
            {
                var user = await _userManager.FindByIdAsync(notification.UserId);
                notificationList.Add(new
                {
                    UserName = user?.UserName ?? "Unknown",
                    Notification = notification
                });
            }

            ViewBag.Notifications = notificationList;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ManageNotifications(int id, string action)
        {
            var userId = _userManager.GetUserId(User);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo.";
                return RedirectToAction("AllNotifications");
            }

            if (action == "MarkAsRead")
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông báo đã được đánh dấu là đã đọc.";
            }
            else if (action == "Delete")
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông báo đã được xóa.";
            }

            return RedirectToAction("AllNotifications");
        }
    }
}
