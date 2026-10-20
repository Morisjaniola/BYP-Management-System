using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace BYPManagementSystem.Tests.Infrastructure
{
    // =========================================================
    // HTTP TEST HELPERS
    // =========================================================

    public static class HttpTestHelper
    {
        private static readonly Regex AntiForgeryInputRegex =
            new(
                "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled
            );

        private static readonly Regex ValueAttributeRegex =
            new(
                "value=\"([^\"]*)\"",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled
            );

        // ======================================================
        // GET A PAGE AND RETURN ITS ANTI-FORGERY TOKEN
        // ======================================================

        public static async Task<string> GetAntiForgeryTokenAsync(
            HttpClient client,
            string path)
        {
            var html = await client.GetStringAsync(path);

            return ExtractAntiForgeryToken(html);
        }


        public static string ExtractAntiForgeryToken(
            string html)
        {
            var inputMatch =
                AntiForgeryInputRegex.Match(html);

            Assert.True(
                inputMatch.Success,
                "Anti-forgery token input was not found in the page HTML."
            );


            var valueMatch =
                ValueAttributeRegex.Match(inputMatch.Value);

            Assert.True(
                valueMatch.Success,
                "Anti-forgery token value was not found."
            );


            return WebUtility.HtmlDecode(
                valueMatch.Groups[1].Value
            );
        }


        // ======================================================
        // NORMALIZE HTML SO WHITESPACE IN RAZOR SOURCE DOES NOT
        // BREAK TEXT ASSERTIONS
        // ======================================================

        public static string CollapseWhitespace(string html)
        {
            return Regex.Replace(html, @"\s+", " ");
        }
    }
}
