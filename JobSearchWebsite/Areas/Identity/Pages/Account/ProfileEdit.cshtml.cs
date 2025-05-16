using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JobSearchWebsite.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Data;

namespace JobSearchWebsite.Areas.Identity.Pages.Account
{
    public class ProfileEditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileEditModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(100, MinimumLength = 2)]
            public string FullName { get; set; }

            [Phone]
            [StringLength(15)]
            public string? PhoneNumber { get; set; }

            [DataType(DataType.Date)]
            [CustomDateValidation(ErrorMessage = "Ngày sinh không được lớn hơn ngày hiện tại.")]
            public DateTime? DateOfBirth { get; set; }

            [StringLength(200)]
            public string Address { get; set; }

            public string? Education { get; set; }

            public string? Experience { get; set; }

            public string? Skills { get; set; }

            [Url]
            public string? CVUrl { get; set; }

            public bool IsPublic { get; set; }
        }

        public class CustomDateValidationAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                if (value == null)
                {
                    return ValidationResult.Success; // Cho phép null
                }

                DateTime dateOfBirth = (DateTime)value;
                DateTime currentDate = DateTime.Now;

                if (dateOfBirth > currentDate)
                {
                    return new ValidationResult(ErrorMessage);
                }

                return ValidationResult.Success;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin người dùng.");
            }

            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == user.Id);

            if (existingProfile != null)
            {
                user.Profile = existingProfile;
            }
            else
            {
                user.Profile = new UserProfile { Id = user.Id, FullName = user.UserName };
                await _userManager.UpdateAsync(user);
            }

            var profile = user.Profile;
            Input = new InputModel
            {
                FullName = profile.FullName ?? "",
                PhoneNumber = profile.PhoneNumber,
                DateOfBirth = profile.DateOfBirth,
                Address = profile.Address ?? "",
                Education = profile.Education,
                Experience = profile.Experience,
                Skills = profile.Skills,
                CVUrl = profile.CVUrl,
                IsPublic = profile.IsPublic
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin người dùng.");
            }

            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == user.Id);

            UserProfile profile;
            if (existingProfile != null)
            {
                profile = existingProfile;
            }
            else
            {
                profile = new UserProfile { Id = user.Id, FullName = Input.FullName };
            }

            profile.FullName = Input.FullName;
            profile.PhoneNumber = Input.PhoneNumber;
            profile.DateOfBirth = Input.DateOfBirth;
            profile.Address = Input.Address ?? string.Empty;
            profile.Education = Input.Education;
            profile.Experience = Input.Experience;
            profile.Skills = Input.Skills;
            profile.CVUrl = Input.CVUrl ?? string.Empty;
            profile.IsPublic = Input.IsPublic;

            user.Profile = profile;
            await _userManager.UpdateAsync(user);

            return RedirectToPage("./Profile");
        }
    }
}