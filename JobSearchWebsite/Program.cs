using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using Microsoft.AspNetCore.Authentication.Google;
using JobSearchWebsite.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Đảm bảo claim NameIdentifier được sử dụng
builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailSender, IdentityEmailSender>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Seeding data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Starting seeding data...");

        string[] roleNames = { "JobSeeker", "Employer", "Admin" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogInformation($"Creating role: {roleName}");
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminEmail = "admin@example.com";
        var adminPassword = "Admin@123";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                RegisteredDate = DateTime.Now
            };
            logger.LogInformation($"Creating admin user: {adminEmail}");
            await userManager.CreateAsync(adminUser, adminPassword);
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        if (!context.Jobs.Any())
        {
            logger.LogInformation("Seeding jobs...");
            context.Jobs.AddRange(
                new Job
                {
                    Title = "Lập trình viên .NET",
                    Company = "FPT Software",
                    Location = "Hà Nội",
                    Description = "Yêu cầu 2 năm kinh nghiệm .NET",
                    Status = "Chờ duyệt",
                    UserId = adminUser.Id,
                    CreatedDate = DateTime.Now
                },
                new Job
                {
                    Title = "Kỹ sư AI",
                    Company = "VinAI",
                    Location = "TP.HCM",
                    Description = "Yêu cầu 3 năm kinh nghiệm AI",
                    Status = "Đã duyệt",
                    UserId = adminUser.Id,
                    CreatedDate = DateTime.Now.AddDays(-10)
                },
                new Job
                {
                    Title = "Nhân viên kinh doanh",
                    Company = "VNG",
                    Location = "Đà Nẵng",
                    Description = "Yêu cầu kỹ năng giao tiếp tốt",
                    Status = "Bị từ chối",
                    UserId = adminUser.Id,
                    CreatedDate = DateTime.Now.AddDays(-20)
                }
            );
            context.SaveChanges();
        }

        logger.LogInformation("Seeding completed successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during seeding.");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();