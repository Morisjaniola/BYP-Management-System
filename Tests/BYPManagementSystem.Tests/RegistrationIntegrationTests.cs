using System.Net;
using BYPManagementSystem.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BYPManagementSystem.Tests
{
    // =========================================================
    // REGISTRATION WORKFLOW INTEGRATION TESTS
    // =========================================================
    // These tests run the real BYProgram HTTP pipeline
    // (routing, model binding, anti-forgery, Identity, cookie
    // sign-in) against an isolated in-memory database.
    // =========================================================

    public class RegistrationIntegrationTests
    {
        private const string ValidPassword = "BypTest123";


        // =====================================================
        // HELPERS
        // =====================================================

        private static string UniqueEmail()
        {
            return $"youth-{Guid.NewGuid():N}@example.com";
        }


        private static HttpClient CreateClient(
            WebApplicationFactory<Program> factory)
        {
            return factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                }
            );
        }


        private static async Task<HttpResponseMessage>
            PostRegisterAsync(
                HttpClient client,
                string email,
                string password,
                string confirmPassword,
                string? returnUrl = null,
                bool includeAntiForgeryToken = true)
        {
            var form = new Dictionary<string, string>
            {
                ["Email"] = email,

                ["Password"] = password,

                ["ConfirmPassword"] = confirmPassword,

                ["ReturnUrl"] = returnUrl ?? string.Empty
            };


            if (includeAntiForgeryToken)
            {
                form["__RequestVerificationToken"] =
                    await HttpTestHelper
                        .GetAntiForgeryTokenAsync(
                            client,
                            "/Account/Register"
                        );
            }


            return await client.PostAsync(
                "/Account/Register",
                new FormUrlEncodedContent(form)
            );
        }


        private static async Task<IdentityUser?> FindUserAsync(
            WebApplicationFactory<Program> factory,
            string email)
        {
            using var scope = factory.Services.CreateScope();

            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<IdentityUser>>();

            return await userManager.FindByEmailAsync(email);
        }


        // =====================================================
        // 1. VALID REGISTRATION
        // =====================================================

        [Fact]
        public async Task Register_WithValidCredentials_CreatesAccountAssignsUserRoleAndSignsIn()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                ValidPassword
            );


            // Redirect to the role-based home destination.
            Assert.Equal(
                HttpStatusCode.Redirect,
                response.StatusCode
            );

            Assert.Equal(
                "/",
                response.Headers.Location?.OriginalString
            );


            // Identity cookie was issued by the automatic sign-in.
            Assert.True(
                response.Headers.Contains("Set-Cookie"),
                "Expected the sign-in cookie to be issued."
            );


            // Account persisted with a hashed password, the
            // default User role, and an honest EmailConfirmed.
            using (var scope = factory.Services.CreateScope())
            {
                var userManager =
                    scope.ServiceProvider
                        .GetRequiredService<
                            UserManager<IdentityUser>>();


                var user =
                    await userManager.FindByEmailAsync(email);


                Assert.NotNull(user);

                Assert.False(user!.EmailConfirmed);

                Assert.False(
                    string.IsNullOrEmpty(user.PasswordHash),
                    "Expected the password to be hashed by Identity."
                );

                Assert.True(
                    await userManager.CheckPasswordAsync(
                        user,
                        ValidPassword
                    )
                );

                Assert.True(
                    await userManager.IsInRoleAsync(
                        user,
                        "User"
                    )
                );
            }


            // The signed-in youth can reach the protected
            // dashboard and sees a clear path to complete the
            // required profile information.
            var dashboard =
                await client.GetAsync("/Youth/Dashboard");

            Assert.Equal(
                HttpStatusCode.OK,
                dashboard.StatusCode
            );


            var dashboardHtml =
                await dashboard.Content.ReadAsStringAsync();


            Assert.Contains(
                "Complete your youth profile",
                dashboardHtml
            );

            Assert.Contains(
                "Account created successfully",
                dashboardHtml
            );
        }


        // =====================================================
        // 2. PASSWORD POLICY (SHORT / NO UPPER / NO LOWER / NO DIGIT)
        // =====================================================

        [Theory]
        [InlineData("Short1", "at least 8 characters")]
        [InlineData("alllowercase1", "uppercase letter")]
        [InlineData("ALLUPPERCASE1", "lowercase letter")]
        [InlineData("NoDigitsHere", "number")]
        public async Task Register_WithPolicyViolatingPassword_ShowsSpecificErrorAndDoesNotCreateAccount(
            string password,
            string expectedMessage)
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();


            var response = await PostRegisterAsync(
                client,
                email,
                password,
                password
            );


            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );


            var html =
                HttpTestHelper.CollapseWhitespace(
                    await response.Content.ReadAsStringAsync()
                );


            Assert.Contains(expectedMessage, html);

            // Field-level errors must not leave an empty
            // validation-summary alert above the form.
            Assert.DoesNotContain(
                "validation-summary",
                html
            );

            Assert.Null(
                await FindUserAsync(factory, email)
            );
        }


        // =====================================================
        // 3. CONFIRM PASSWORD MISMATCH
        // =====================================================

        [Fact]
        public async Task Register_WithMismatchedConfirmPassword_ShowsErrorAndDoesNotCreateAccount()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                "DifferentPass123"
            );


            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );


            var html =
                HttpTestHelper.CollapseWhitespace(
                    await response.Content.ReadAsStringAsync()
                );


            Assert.Contains(
                "The passwords do not match.",
                html
            );

            Assert.Null(
                await FindUserAsync(factory, email)
            );
        }


        // =====================================================
        // 4. MISSING / INVALID EMAIL
        // =====================================================

        [Theory]
        [InlineData("", "Email is required.")]
        [InlineData(
            "not-an-email",
            "Please enter a valid email address.")]
        public async Task Register_WithMissingOrInvalidEmail_ShowsFieldErrorAndDoesNotCreateAccount(
            string email,
            string expectedMessage)
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                ValidPassword
            );


            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );


            var html =
                HttpTestHelper.CollapseWhitespace(
                    await response.Content.ReadAsStringAsync()
                );


            Assert.Contains(expectedMessage, html);


            if (!string.IsNullOrEmpty(email))
            {
                Assert.Null(
                    await FindUserAsync(factory, email)
                );
            }
        }


        // =====================================================
        // 5. DUPLICATE EMAIL
        // =====================================================

        [Fact]
        public async Task Register_WithDuplicateEmail_ShowsFieldErrorBesideEmail()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();

            string seededUserId;


            using (var scope = factory.Services.CreateScope())
            {
                var userManager =
                    scope.ServiceProvider
                        .GetRequiredService<
                            UserManager<IdentityUser>>();


                var seededUser = new IdentityUser
                {
                    UserName = email,

                    Email = email,

                    EmailConfirmed = false
                };


                var createResult =
                    await userManager.CreateAsync(
                        seededUser,
                        "SeedPassword123"
                    );


                Assert.True(createResult.Succeeded);

                seededUserId = seededUser.Id;
            }


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                ValidPassword
            );


            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );


            var html =
                await response.Content.ReadAsStringAsync();


            Assert.Contains(
                "An account with this email already exists.",
                html
            );

            Assert.Contains(
                "data-valmsg-for=\"Email\"",
                html
            );


            // The original account is untouched and no second
            // account was created for the same email.
            using var verifyScope =
                factory.Services.CreateScope();

            var verifyManager =
                verifyScope.ServiceProvider
                    .GetRequiredService<
                        UserManager<IdentityUser>>();


            var storedUser =
                await verifyManager.FindByEmailAsync(email);


            Assert.NotNull(storedUser);

            Assert.Equal(seededUserId, storedUser!.Id);
        }


        // =====================================================
        // 6. FAILED VALIDATION PRESERVES EMAIL, CLEARS PASSWORDS
        // =====================================================

        [Fact]
        public async Task Register_WhenValidationFails_PreservesEmailAndClearsPasswords()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();

            const string attemptedPassword = "Short1";


            var response = await PostRegisterAsync(
                client,
                email,
                attemptedPassword,
                attemptedPassword
            );


            var html =
                await response.Content.ReadAsStringAsync();


            // Safe submitted value preserved.
            Assert.Contains(
                $"value=\"{email}\"",
                html
            );


            // Password values are not reflected back.
            Assert.DoesNotContain(
                attemptedPassword,
                html
            );


            Assert.Null(
                await FindUserAsync(factory, email)
            );
        }


        // =====================================================
        // 7. ROLE-ASSIGNMENT FAILURE + SAFE CLEANUP
        // =====================================================

        [Fact]
        public async Task Register_WhenDefaultRoleCannotBeAssigned_RollsBackAccountAndShowsError()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();


            // Remove the default User role so role assignment
            // fails while the account itself could still be
            // created (the half-set-up state under test).
            using (var scope = factory.Services.CreateScope())
            {
                var roleManager =
                    scope.ServiceProvider
                        .GetRequiredService<
                            RoleManager<IdentityRole>>();


                var role =
                    await roleManager.FindByNameAsync("User");


                Assert.NotNull(role);

                var deleteResult =
                    await roleManager.DeleteAsync(role!);


                Assert.True(deleteResult.Succeeded);
            }


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                ValidPassword
            );


            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );


            var html =
                HttpTestHelper.CollapseWhitespace(
                    await response.Content.ReadAsStringAsync()
                );


            Assert.Contains(
                "could not be fully set up",
                html
            );

            // This is a model-level failure, so the summary
            // must be rendered.
            Assert.Contains(
                "validation-summary",
                html
            );


            // The account was rolled back, not left behind
            // without its required role.
            Assert.Null(
                await FindUserAsync(factory, email)
            );
        }


        // =====================================================
        // 8. DATABASE FAILURE HANDLING
        // =====================================================

        [Fact]
        public async Task Register_WhenDatabaseSaveFails_ShowsFriendlyErrorAndDoesNotCreateAccount()
        {
            using var factory = new FailingDatabaseFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();


            // Startup and seeding completed; now simulate the
            // database going down while the form is submitted.
            factory.FailureState.FailSaves = true;


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                ValidPassword
            );


            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );


            var html =
                HttpTestHelper.CollapseWhitespace(
                    await response.Content.ReadAsStringAsync()
                );


            Assert.Contains(
                "unexpected server error",
                html
            );

            // This is a model-level failure, so the summary
            // must be rendered.
            Assert.Contains(
                "validation-summary",
                html
            );


            factory.FailureState.FailSaves = false;


            Assert.Null(
                await FindUserAsync(factory, email)
            );
        }


        // =====================================================
        // 9. ANTI-FORGERY PROTECTION
        // =====================================================

        [Fact]
        public async Task Register_WithoutAntiForgeryToken_IsRejected()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);

            var email = UniqueEmail();


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                ValidPassword,
                includeAntiForgeryToken: false
            );


            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode
            );


            Assert.Null(
                await FindUserAsync(factory, email)
            );
        }


        // =====================================================
        // 10. INTENDED DESTINATION (RETURN URL)
        // =====================================================

        [Fact]
        public async Task Register_WithReturnUrl_RedirectsToIntendedDestination()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = CreateClient(factory);


            var registerPage =
                await client.GetStringAsync(
                    "/Account/Register?returnUrl=%2FYouth%2FDashboard"
                );


            Assert.Contains(
                "name=\"ReturnUrl\"",
                registerPage
            );

            Assert.Contains(
                "value=\"/Youth/Dashboard\"",
                registerPage
            );


            var email = UniqueEmail();


            var response = await PostRegisterAsync(
                client,
                email,
                ValidPassword,
                ValidPassword,
                returnUrl: "/Youth/Dashboard"
            );


            Assert.Equal(
                HttpStatusCode.Redirect,
                response.StatusCode
            );

            Assert.Equal(
                "/Youth/Dashboard",
                response.Headers.Location?.OriginalString
            );


            var dashboard =
                await client.GetAsync("/Youth/Dashboard");


            Assert.Equal(
                HttpStatusCode.OK,
                dashboard.StatusCode
            );
        }
    }
}
