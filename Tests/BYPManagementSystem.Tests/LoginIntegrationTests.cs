using System.Net;
using BYPManagementSystem.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BYPManagementSystem.Tests
{
    // =========================================================
    // LOGIN INTEGRATION TESTS
    // =========================================================

    public class LoginIntegrationTests
    {
        [Fact]
        public async Task Login_WithUnknownEmail_ShowsModelLevelValidationSummary()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                }
            );


            var token =
                await HttpTestHelper.GetAntiForgeryTokenAsync(
                    client,
                    "/Account/Login"
                );


            var response = await client.PostAsync(
                "/Account/Login",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["__RequestVerificationToken"] = token,

                        ["Email"] = "missing@example.com",

                        ["Password"] = "SomePassword123",

                        ["RememberMe"] = "false",

                        ["ReturnUrl"] = string.Empty
                    }
                )
            );


            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );


            var html =
                await response.Content.ReadAsStringAsync();


            Assert.Contains(
                "Invalid email or password.",
                html
            );

            Assert.Contains(
                "validation-summary",
                html
            );
        }
    }
}
