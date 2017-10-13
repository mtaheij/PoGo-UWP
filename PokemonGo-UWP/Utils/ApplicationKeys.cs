using System.Collections.Generic;

namespace PokemonGo_UWP.Utils
{
    public static class ApplicationKeys
    {
        public static bool ForceRelogin = true;
        public static string HockeyAppToken { get; } = "";
        public static string MapServiceToken { get; } = "";
        public static string[] MapBoxTokens { get; } = new string[0];
        public static string MapBoxStylesLight { get; } = "";
        public static string MapBoxStylesDark { get; } = "";
    }
}