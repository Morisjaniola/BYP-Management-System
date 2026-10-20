using BYPManagementSystem.Tests.Infrastructure;
using Xunit;

namespace BYPManagementSystem.Tests
{
    // =========================================================
    // AUTH PAGE MARKUP TESTS
    // =========================================================
    // Guards the accessibility fix (labels must target their
    // real input IDs) and the browser-visible password policy.
    // =========================================================

    public class AuthPageMarkupTests
    {
        [Fact]
        public async Task RegisterPage_PasswordLabelsReferenceTheirInputs()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = factory.CreateClient();


            var html =
                await client.GetStringAsync(
                    "/Account/Register"
                );


            // Labels target the explicit IDs used by the
            // password visibility toggles.
            Assert.Matches(
                "<label[^>]*for=\"passwordInput\"[^>]*>",
                html
            );

            Assert.Matches(
                "<label[^>]*for=\"confirmPasswordInput\"[^>]*>",
                html
            );

            Assert.Matches(
                "id=\"passwordInput\"",
                html
            );

            Assert.Matches(
                "id=\"confirmPasswordInput\"",
                html
            );


            // No stale label associations (the previous
            // for="Password" / for="ConfirmPassword" values did
            // not match any element).
            Assert.DoesNotMatch(
                "<label[^>]*for=\"Password\"",
                html
            );

            Assert.DoesNotMatch(
                "<label[^>]*for=\"ConfirmPassword\"",
                html
            );


            // Browser-visible password policy matches Identity.
            Assert.Contains(
                "minlength=\"8\"",
                html
            );

            Assert.Contains(
                "(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])",
                html
            );

            Assert.Contains(
                "maxlength=\"100\"",
                html
            );


            var collapsed =
                HttpTestHelper.CollapseWhitespace(html);


            Assert.Contains(
                "Use at least 8 characters, including an uppercase letter, a lowercase letter, and a number.",
                collapsed
            );
        }


        [Fact]
        public async Task LoginPage_PasswordLabelReferencesItsInput()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = factory.CreateClient();


            var html =
                await client.GetStringAsync("/Account/Login");


            Assert.Matches(
                "<label[^>]*for=\"passwordInput\"[^>]*>",
                html
            );

            Assert.Matches(
                "id=\"passwordInput\"",
                html
            );

            Assert.DoesNotMatch(
                "<label[^>]*for=\"Password\"",
                html
            );
        }


        [Fact]
        public async Task AuthPages_EmailLabelMatchesInput()
        {
            using var factory = new BypWebApplicationFactory();

            using var client = factory.CreateClient();


            foreach (
                var path in
                new[] { "/Account/Login", "/Account/Register" })
            {
                var html = await client.GetStringAsync(path);

                Assert.Matches(
                    "<label[^>]*for=\"Email\"[^>]*>",
                    html
                );

                Assert.Matches(
                    "id=\"Email\"",
                    html
                );
            }
        }
    }
}
