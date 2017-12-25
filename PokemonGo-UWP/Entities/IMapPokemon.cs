using System.ComponentModel;
using Windows.Devices.Geolocation;
using POGOProtos.Enums;

namespace PokemonGo_UWP.Entities
{
    public interface IMapPokemon : IUpdatable<IMapPokemon>, INotifyPropertyChanged
    {
        Geopoint Geoposition { get; set; }

        PokemonId PokemonId { get; }

        ulong EncounterId { get; }

        string SpawnpointId { get; }
    }
}
