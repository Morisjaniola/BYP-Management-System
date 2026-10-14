using BYPManagementSystem.Data;
using BYPManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

        options.Password.RequiredLength = 6;


        // ----------------------------------------------------
        // USER
        // ----------------------------------------------------

        options.User.RequireUniqueEmail = true;


        // ----------------------------------------------------
        // SIGN IN
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

        const string adminPassword =
            "Admin@12345";


        var adminUser =
            await userManager.FindByEmailAsync(
                adminEmail
            );


        if (adminUser == null)
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


        // ----------------------------------------------------
        // ENSURE ADMIN ROLE
        // ----------------------------------------------------

        if (!await userManager.IsInRoleAsync(
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

        const string staffPassword =
            "Staff@12345";


        var staffUser =
            await userManager.FindByEmailAsync(
                staffEmail
            );


        if (staffUser == null)
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


        // ----------------------------------------------------
        // ENSURE STAFF ROLE
        // ----------------------------------------------------

        if (!await userManager.IsInRoleAsync(
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
