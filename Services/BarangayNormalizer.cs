namespace BYPManagementSystem.Services
{
    // ======================================================
    // BARANGAY NORMALIZER
    // ======================================================
    //
    // One canonical normalization for matching YouthParticipant
    // Barangay values against YouthProgram Barangay values.
    //
    // Both matching directions (program -> youth and youth ->
    // program) must use the SAME comparison so harmless
    // differences such as capitalization, surrounding or
    // repeated whitespace, and the optional "Barangay " prefix
    // can never cause a participant to be missed - while
    // genuinely different Barangay names are never treated as
    // equivalent.
    //
    //   "  Barangay  San   Jose  "  ->  "san jose"
    //   "san jose"                  ->  "san jose"
    //   "SANTA  FE"                 ->  "santa fe"
    //
    //   "San Jose" != "Santa  Jose"   (different Barangays)
    //
    // ======================================================

    public static class BarangayNormalizer
    {
        private const string Prefix =
            "Barangay ";


        // ==================================================
        // STORAGE
        //
        // Keep the readable value for display. Only collapse
        // surrounding and repeated whitespace - the users'
        // capitalization and the "Barangay " prefix are
        // preserved exactly as typed.
        // ==================================================

        public static string ForStorage(string? barangay)
        {
            if (string.IsNullOrWhiteSpace(
                    barangay))
            {
                return string.Empty;
            }


            return string.Join(
                " ",
                barangay
                    .Trim()
                    .Split(
                        (char[]?)null,
                        StringSplitOptions
                            .RemoveEmptyEntries
                    )
            );
        }


        // ==================================================
        // COMPARISON
        //
        // Storage normalization plus case folding and removal
        // of the optional leading "Barangay " prefix, so
        // "Barangay San Jose" and "San Jose" compare equal.
        // ==================================================

        public static string ForComparison(string? barangay)
        {
            var value =
                ForStorage(
                    barangay
                );


            if (value.Length == 0)
            {
                return string.Empty;
            }


            if (value.StartsWith(
                    Prefix,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                value =
                    value
                        .Substring(
                            Prefix.Length
                        )
                        .Trim();
            }


            return value
                .ToLowerInvariant();
        }


        // ==================================================
        // EQUALITY HELPER
        // ==================================================

        public static bool SameBarangay(
            string? a,
            string? b) =>
            !string.IsNullOrWhiteSpace(a) &&
            !string.IsNullOrWhiteSpace(b) &&
            ForComparison(a) ==
                ForComparison(b);
    }
}
