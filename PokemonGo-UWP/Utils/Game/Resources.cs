using Windows.ApplicationModel.Resources;

namespace PokemonGo_UWP.Utils
{
    public static class Resources
    {
        public static readonly ResourceLoader Pokemon = ResourceLoader.GetForViewIndependentUse("Pokemon");
        public static readonly ResourceLoader Items = ResourceLoader.GetForViewIndependentUse("Items");
        public static readonly ResourceLoader CodeResources = ResourceLoader.GetForViewIndependentUse("CodeResources");
        public static readonly ResourceLoader Achievements = ResourceLoader.GetForViewIndependentUse("Achievements");
        public static readonly ResourceLoader PokemonMoves = ResourceLoader.GetForViewIndependentUse("PokemonMoves");
        public static readonly ResourceLoader PokemonTypes = ResourceLoader.GetForViewIndependentUse("PokemonTypes");
        public static readonly ResourceLoader Pokedex = ResourceLoader.GetForViewIndependentUse("Pokedex");
    }
}