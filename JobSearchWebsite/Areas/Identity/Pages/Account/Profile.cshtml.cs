using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JobSearchWebsite.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Data;

namespace JobSearchWebsite.Areas.Identity.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUser AppUser { get; set; }
        public UserProfile Profile { get; set; }
        public bool IsEmailConfirmed { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin người dùng.");
            }

            AppUser = user;

            // Kiểm tra xem UserProfile đã tồn tại trong cơ sở dữ liệu chưa
            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == user.Id);

            if (existingProfile != null)
            {
                Profile = existingProfile;
                user.Profile = existingProfile;
            }
            else
            {
                Profile = user.Profile ?? new UserProfile
                {
                    Id = user.Id,
                    FullName = user.UserName
                };
                user.Profile = Profile;
                await _userManager.UpdateAsync(user);
            }

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            return Page();
        }
    }
}