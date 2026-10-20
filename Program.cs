using BYPManagementSystem.Data;
using BYPManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// APPLICATION TIME ZONE
// ============================================================
// The attendance module evaluates program dates and Time In /
// Time Out windows, records timestamps and labels every
// user-facing time using ONE application time zone, configured
// here from "AttendanceTime:TimeZoneId" (appsettings.json).
// When the setting is missing, the zone defaults to Asia/
// Manila (UTC+8) - the documented policy for this system.
// This must be configured before any request or background
// service runs, so it happens first in startup.

AttendanceSchedule.Configure(
    builder.Configuration[
        "AttendanceTime:TimeZoneId"]);

// ============================================================
// DATABASE
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found."
    );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);


// ============================================================
// ASP.NET CORE IDENTITY
// ============================================================

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // ----------------------------------------------------
        // PASSWORD
        // ----------------------------------------------------

        options.Password.RequireDigit = true;

        options.Password.RequireLowercase = true;

        options.Password.RequireUppercase = true;

        options.Password.RequireNonAlphanumeric = false;

        options.Password.RequiredLength = 8;


        // ----------------------------------------------------
        // USER
        // ----------------------------------------------------

        options.User.RequireUniqueEmail = true;


        // ----------------------------------------------------
        // SIGN IN
        // ----------------------------------------------------
        // This application has no outbound email or
        // confirmation-token workflow, so confirmed accounts
        // cannot be required: accounts must be able to sign in
        // immediately after registration.
        //
        // EmailConfirmed therefore stays FALSE for self-registered
        // accounts (see AccountController.Register). The system
        // never claims an unverified address was confirmed; only
        // trusted sources may set it (for example development
        // seed accounts created by the developer).
        // ----------------------------------------------------

        options.SignIn.RequireConfirmedAccount = false;


        // ----------------------------------------------------
        // LOCKOUT
        // ----------------------------------------------------

        options.Lockout.MaxFailedAccessAttempts = 5;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// ============================================================
// APPLICATION COOKIE
// ============================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";

    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan =
        TimeSpan.FromHours(8);

    options.SlidingExpiration = true;
});


// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews();


// ============================================================
// ATTENDANCE PHOTO STORAGE
// Photos are stored outside wwwroot and are only served
// through controller actions that verify ownership.
// ============================================================

builder.Services.Configure<AttendancePhotoOptions>(
    builder.Configuration.GetSection(
        AttendancePhotoOptions.SectionName));

builder.Services.AddScoped<AttendancePhotoStorage>();


// ============================================================
// ATTENDANCE SCHEDULE ANNOUNCEMENTS
// Background scheduler that sends the configured attendance
// window announcements (Time In / Time Out open and closing
// soon) to approved participants without requiring any browser
// to stay open.
// ============================================================

builder.Services.AddHostedService<AttendanceAnnouncementService>();


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// ERROR HANDLING
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}


// ============================================================
// HTTPS
// ============================================================

app.UseHttpsRedirection();


// ============================================================
// STATIC FILES
// ============================================================

app.UseStaticFiles();


// ============================================================
// STATUS CODE HANDLING
// ============================================================

app.UseStatusCodePagesWithReExecute(
    "/Home/StatusCode",
    "?code={0}"
);


// ============================================================
// ROUTING
// ============================================================

app.UseRouting();


// ============================================================
// AUTHENTICATION
// IMPORTANT: Authentication must run before Authorization.
// ============================================================

app.UseAuthentication();

app.UseAuthorization();


// ============================================================
// DEFAULT MVC ROUTE
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


// ============================================================
// ROLE + DEVELOPMENT ACCOUNT SEEDING
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();


    // ========================================================
    // REQUIRED ROLES
    // ========================================================

    string[] roles =
    {
        "Admin",
        "Staff",
        "User"
    };


    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(role)
                );

            if (!roleResult.Succeeded)
            {
                var errors =
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            e => e.Description
                        )
                    );

                throw new InvalidOperationException(
                    $"Failed to create role '{role}': {errors}"
                );
            }
        }
    }


    // ========================================================
    // DEVELOPMENT ADMIN ACCOUNT
    // ========================================================

    if (app.Environment.IsDevelopment())
    {
        const string adminEmail =
            "admin@test.com";

        // Password comes from local configuration only
        // (User Secrets, environment variable, or local
        // appsettings) — never from committed source code.
        var adminPassword =
            builder.Configuration[
                "SeedAccounts:AdminPassword"];


        var adminUser =
            await userManager.FindByEmailAsync(
                adminEmail
            );


        if (adminUser == null)
        {
            if (string.IsNullOrWhiteSpace(
                    adminPassword))
            {
                Console.WriteLine(
                    "WARNING: SeedAccounts:AdminPassword is not configured. Skipping development admin account creation. Set it with: dotnet user-secrets set \"SeedAccounts:AdminPassword\" <value>"
                );
            }

            else
            {
                adminUser =
                    new IdentityUser
                    {
                        UserName = adminEmail,

                        Email = adminEmail,

                        EmailConfirmed = true
                    };


                var adminCreateResult =
                    await userManager.CreateAsync(
                        adminUser,
                        adminPassword
                    );


                if (!adminCreateResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            "; ",
                            adminCreateResult.Errors.Select(
                                e => e.Description
                            )
                        );

                    throw new InvalidOperationException(
                        $"Failed to create development admin account: {errors}"
                    );
                }
            }
        }


        // ----------------------------------------------------
        // ENSURE ADMIN ROLE
        // ----------------------------------------------------

        if (adminUser != null &&

            !await userManager.IsInRoleAsync(
                adminUser,
                "Admin"))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    adminUser,
                    "Admin"
                );


            if (!roleResult.Succeeded)
            {
                var errors =
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            e => e.Description
                        )
                    );

                throw new InvalidOperationException(
                    $"Failed to assign Admin role: {errors}"
                );
            }
        }


        // ====================================================
        // DEVELOPMENT STAFF ACCOUNT
        // ====================================================

        const string staffEmail =
            "staff@test.com";

        // Password comes from local configuration only
        // (User Secrets, environment variable, or local
        // appsettings) — never from committed source code.
        var staffPassword =
            builder.Configuration[
                "SeedAccounts:StaffPassword"];


        var staffUser =
            await userManager.FindByEmailAsync(
                staffEmail
            );


        if (staffUser == null)
        {
            if (string.IsNullOrWhiteSpace(
                    staffPassword))
            {
                Console.WriteLine(
                    "WARNING: SeedAccounts:StaffPassword is not configured. Skipping development staff account creation. Set it with: dotnet user-secrets set \"SeedAccounts:StaffPassword\" <value>"
                );
            }

            else
            {
                staffUser =
                    new IdentityUser
                    {
                        UserName = staffEmail,

                        Email = staffEmail,

                        EmailConfirmed = true
                    };


                var staffCreateResult =
                    await userManager.CreateAsync(
                        staffUser,
                        staffPassword
                    );


                if (!staffCreateResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            "; ",
                            staffCreateResult.Errors.Select(
                                e => e.Description
                            )
                    );

                    throw new InvalidOperationException(
                        $"Failed to create development staff account: {errors}"
                    );
                }
            }
        }


        // ----------------------------------------------------
        // ENSURE STAFF ROLE
        // ----------------------------------------------------

        if (staffUser != null &&

            !await userManager.IsInRoleAsync(
                staffUser,
                "Staff"))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    staffUser,
                    "Staff"
                );


            if (!roleResult.Succeeded)
            {
                var errors =
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            e => e.Description
                        )
                    );

                throw new InvalidOperationException(
                    $"Failed to assign Staff role: {errors}"
                );
            }
        }
    }
}


// ============================================================
// RUN APPLICATION
// ============================================================

app.Run();


// ============================================================
// TESTING HOOK
// ============================================================
// Exposes the implicit top-level entry point type to the
// integration test project (WebApplicationFactory<Program>).
// This does not change runtime behavior.
// ============================================================

public partial class Program
{
}
