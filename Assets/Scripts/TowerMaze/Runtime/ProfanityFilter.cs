namespace TowerMaze
{
    /// <summary>
    /// Static substring blocklist for nickname submissions. Names are normalized to
    /// uppercase A-Z/0-9/underscore before this runs, so the blocklist is uppercase
    /// roots only — partial matches catch leetspeak (S1KT1R contains SIKTIR after
    /// digits get treated as letters via the substring scan? no — only literal match).
    /// Keeping the list short and rooted on common stems is enough for casual cover.
    /// </summary>
    public static class ProfanityFilter
    {
        private static readonly string[] BlockedSubstrings =
        {
            // Turkish — common slurs / curses (uppercased roots)
            "AMK", "AMQ", "AMINA", "AMCIK", "AMCK", "ANAMI", "ANANI", "ANAS", "ANNE",
            "OROSPU", "ORSPU", "PIC", "PIIC", "PUST", "PUSHT", "GOTVERE", "GOTVRN",
            "SIKTIR", "SIKIY", "SIKI", "SIKEY", "SIKERIM", "SIKIM", "SIKM", "YARRAK",
            "YARAK", "YRRAK", "TASAK", "TASAGI", "GAVAT", "KAHPE", "KAHBE", "FAHISE",
            "ANCIK", "ANCK", "MAL", "MALL", "GERIZEKAL", "SALAK", "APTAL", "IBNE",
            "IBNEE", "PEZEVENK", "DOMALT", "GOTVERIR", "OC", "OCC",
            // English — top family
            "FUCK", "FUK", "FCK", "PHUCK", "FCKR", "MOTHERF", "MTHRFCK", "BITCH",
            "BTCH", "B1TCH", "ASSHOLE", "ASSHL", "ARSEHOLE", "SHIT", "SH1T",
            "DICK", "D1CK", "PUSSY", "PUSY", "C0CK", "COCK", "CUNT", "WHORE", "SLUT",
            "FAGGOT", "FAGOT", "FAG", "RETARD", "RETRD", "NIGGA", "NIGGER", "N1GGA",
            "N1GG3R", "BASTARD", "BSTRD", "TWAT",
            // Spanish — top family
            "PUTA", "PVTA", "PUT0", "PUTO", "MIERDA", "MIERD", "CABRON", "CABR0N",
            "POLLA", "VERGA", "PENDEJO", "PENDEJ", "CONO", "JOTO", "MARICON",
            "MARICN", "MAMON", "CULERO", "CULER0", "GILIPOLLAS", "JODER", "JODR",
            "CHINGA", "CHING0", "CABRA",
            // Hate / slur stems (multi-language)
            "NAZI", "HITLER", "KKK", "K1KE",
        };

        public static bool IsProfane(string normalizedName)
        {
            if (string.IsNullOrEmpty(normalizedName)) return false;
            for (int i = 0; i < BlockedSubstrings.Length; i++)
            {
                if (normalizedName.Contains(BlockedSubstrings[i])) return true;
            }
            return false;
        }
    }
}
