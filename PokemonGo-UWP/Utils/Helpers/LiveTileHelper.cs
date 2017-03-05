using NotificationsExtensions;
using NotificationsExtensions.Tiles;
using POGOProtos.Settings.Master;
using PokemonGo_UWP.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonGo_UWP.Utils
{

    /// <summary>
    /// A set of helpers to easily create Live Tiles.
    /// </summary>
    public static class LiveTileHelper
    {
        private const string PokemonBasePath = "Assets/Pokemons/";
        private const string ImageBasePath = "Assets/LiveTiles/";

        #region Public Methods

        #region ImageTile Helpers

        /// <summary>
        /// Gets a Live Tile containing a static template that renders images from a local folder.
        /// </summary>
        /// <param name="imageSet"></param>
        /// <returns></returns>
        public static TileContent GetImageTile(string imageset)
        {
            var tile = GetTile(ImageBasePath);

            tile.Visual.TileSmall = GetImageBindingSmall(imageset);
            tile.Visual.TileMedium = GetImageBindingMedium(imageset);
            tile.Visual.TileWide = GetImageBindingWide(imageset);
            tile.Visual.TileLarge = GetImageBindingLarge(imageset);

            return tile;
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for the small size of image inside a local folder. 
        /// </summary>
        /// <returns></returns>
        private static TileBinding GetImageBindingSmall(string imageSet)
        {
            var content = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = imageSet + "/Square71x71Logo.png"
                }
            };

            return new TileBinding() { Content = content };
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for the medium size of image inside a local folder. 
        /// </summary>
        /// <returns></returns>
        private static TileBinding GetImageBindingMedium(string imageSet)
        {
            var content = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = imageSet + "/Square150x150Logo.png"
                }
            };

            return new TileBinding() { Content = content };
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for the wide size of image inside a local folder. 
        /// </summary>
        /// <returns></returns>
        private static TileBinding GetImageBindingWide(string imageSet)
        {
            var content = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = imageSet + "/Wide310x150Logo.png"
                }
            };

            return new TileBinding() { Content = content };
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for the large size of image inside a local folder. 
        /// </summary>
        /// <returns></returns>
        private static TileBinding GetImageBindingLarge(string imageSet)
        {
            var content = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = imageSet + "/Square310x310Logo.png"
                }
            };

            return new TileBinding() { Content = content };
        }
        #endregion

        #region PeekTile Helpers

        /// <summary>
        /// Gets a Live Tile containing a "peek" template that renders like the Me tile.
        /// </summary>
        /// <param name="pokemon">
        ///     A <see cref="PokemonDataWrapper"/> containing the Pokemon to use for the tile image.
        /// </param>
        /// <returns>A populated <see cref="TileContent"/> object suitable for submitting to a TileUpdateManager.</returns>
        /// <remarks>https://msdn.microsoft.com/windows/uwp/controls-and-patterns/tiles-and-notifications-special-tile-templates-catalog</remarks>
        public static TileContent GetPeekTile(PokemonDataWrapper pokemon)
        {
            var tile = GetTile(PokemonBasePath);

            tile.Visual.TileSmall = GetPeekBindingSmall(pokemon);
            tile.Visual.TileMedium = GetPeekBindingMedium(pokemon);
            tile.Visual.TileWide = GetPeekBindingWide(pokemon);
            tile.Visual.TileLarge = GetPeekBindingLarge(pokemon);

            return tile;
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for a given list of image URLs. 
        /// </summary>
        /// <param name="pokemon">
        ///     A <see cref="PokemonDataWrapper"/> containing the Pokemon to generate a tile for.
        /// </param>
        /// <returns></returns>
        /// <remarks>Original contribution from sam9116 (https://github.com/ST-Apps/PoGo-UWP/pull/626/files)</remarks>
        private static TileBinding GetPeekBinding(string imageSource, params ITileAdaptiveChild[] children)
        {
            var content = new TileBindingContentAdaptive()
            {
                PeekImage = new TilePeekImage()
                {
                    Source = imageSource
                }
            };

            foreach (var child in children)
            {
                content.Children.Add(child);
            }

            return new TileBinding()
            {
                Content = content
            };

        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for a given list of image URLs. 
        /// </summary>
        /// <param name="pokemon">
        ///     A <see cref="PokemonDataWrapper"/> containing the Pokemon to generate a tile for.
        /// </param>
        /// <returns></returns>
        /// <remarks>Original contribution from sam9116 (https://github.com/ST-Apps/PoGo-UWP/pull/626/files)</remarks>
        private static TileBinding GetPeekBindingSmall(PokemonDataWrapper pokemon)
        {
            return GetPeekBinding(
                $"{(int)pokemon.PokemonId}.png",
                GetCenteredAdaptiveText($"CP: {pokemon.Cp}", AdaptiveTextStyle.Caption),
                GetCenteredAdaptiveText($"{(pokemon.Stamina / pokemon.StaminaMax) * 100}%", AdaptiveTextStyle.Caption)
            );
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for a given list of image URLs. 
        /// </summary>
        /// <param name="pokemon">
        ///     A <see cref="PokemonDataWrapper"/> containing the Pokemon to generate a tile for.
        /// </param>
        /// <returns></returns>
        /// <remarks>Original contribution from sam9116 (https://github.com/ST-Apps/PoGo-UWP/pull/626/files)</remarks>
        private static TileBinding GetPeekBindingMedium(PokemonDataWrapper pokemon)
        {
            return GetPeekBinding(
                $"{(int)pokemon.PokemonId}.png",
                GetCenteredAdaptiveText(GetPokemonName(pokemon), AdaptiveTextStyle.Body),
                GetCenteredAdaptiveText($"CP: {pokemon.Cp}"),
                GetCenteredAdaptiveText($"HP: {(pokemon.Stamina / pokemon.StaminaMax) * 100}%")
            );
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for a given list of image URLs. 
        /// </summary>
        /// <param name="pokemon">
        ///     A <see cref="PokemonDataWrapper"/> containing the Pokemon to generate a tile for.
        /// </param>
        /// <returns></returns>
        /// <remarks>Original contribution from sam9116 (https://github.com/ST-Apps/PoGo-UWP/pull/626/files)</remarks>
        private static TileBinding GetPeekBindingWide(PokemonDataWrapper pokemon)
        {
            return GetPeekBinding(
                $"{(int)pokemon.PokemonId}.png",
                GetCenteredAdaptiveText(GetPokemonName(pokemon), AdaptiveTextStyle.Body),
                GetCenteredAdaptiveText($"Combat Points: {pokemon.Cp}"),
                GetCenteredAdaptiveText($"Stamina: {(pokemon.Stamina / pokemon.StaminaMax) * 100}%")
            );
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentAdaptive"/> for a given list of image URLs. 
        /// </summary>
        /// <param name="pokemon">
        ///     A <see cref="PokemonDataWrapper"/> containing the Pokemon to generate a tile for.
        /// </param>
        /// <returns></returns>
        /// <remarks>Original contribution from sam9116 (https://github.com/ST-Apps/PoGo-UWP/pull/626/files)</remarks>
        private static TileBinding GetPeekBindingLarge(PokemonDataWrapper pokemon)
        {
            return GetPeekBinding(
                $"{(int)pokemon.PokemonId}.png",
                GetCenteredAdaptiveText(GetPokemonName(pokemon), AdaptiveTextStyle.Body),
                GetCenteredAdaptiveText($"Combat Points: {pokemon.Cp}", AdaptiveTextStyle.BodySubtle),
                GetCenteredAdaptiveText($"Stamina: {(pokemon.Stamina / pokemon.StaminaMax) * 100}%", AdaptiveTextStyle.BodySubtle)
            );
        }

        #endregion

        #region PeopleTile Helpers

        /// <summary>
        /// Gets a Live Tile containing multiple cropped-circle images that render like the People Hub tile.
        /// </summary>
        /// <param name="urls">
        ///     A <see cref="List{string}"/> containing the URLs to use for the tile images. May be app-relative or internet URLs.
        /// </param>
        /// <returns>A populated <see cref="TileContent"/> object suitable for submitting to a TileUpdateManager.</returns>
        /// <remarks>https://msdn.microsoft.com/windows/uwp/controls-and-patterns/tiles-and-notifications-special-tile-templates-catalog</remarks>
        public static TileContent GetPeopleTile(List<string> urls)
        {
            var tile = GetTile(PokemonBasePath);

            // Recommended to use 9 photos on Medium
            tile.Visual.TileMedium = GetPeopleBinding(urls, 9);
            // Recommended to use 15 photos on Wide
            tile.Visual.TileWide = GetPeopleBinding(urls, 15);
            // Recommended to use 20 photos on Large
            tile.Visual.TileLarge = GetPeopleBinding(urls, 20);

            return tile;
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentPeople"/> for a given list of image URLs. 
        /// </summary>
        /// <param name="urls">
        ///     A <see cref="List{string}"/> containing the URLs to use for the tile images. May be app-relative or internet URLs.
        /// </param>
        /// <param name="maxCount">The maximum number of images to use for this particular binding size.</param>
        /// <returns></returns>
        private static TileBinding GetPeopleBinding(List<string> urls, int maxCount = 25)
        {
            var content = new TileBindingContentPeople();

            foreach (var url in urls.Take(maxCount))
            {
                content.Images.Add(new TileImageSource(url));
            }

            return new TileBinding()
            {
                Content = content
            };
        }
        #endregion

        #region PhotosTile Helpers

        /// <summary>
        /// Gets a Live Tile containing images that render like the Photos Hub tile.
        /// </summary>
        /// <param name="urls">
        ///     A <see cref="List{string}"/> containing the URLs to use for the tile images. May be app-relative or internet URLs.
        /// </param>
        /// <returns>A populated <see cref="TileContent"/> object suitable for submitting to a TileUpdateManager.</returns>
        /// <remarks>https://msdn.microsoft.com/windows/uwp/controls-and-patterns/tiles-and-notifications-special-tile-templates-catalog</remarks>
        public static TileContent GetPhotosTile(List<string> urls)
        {
            var tile = GetTile(PokemonBasePath);
            var binding = GetPhotosBinding(urls);

            tile.Visual.TileMedium = binding;
            tile.Visual.TileWide = binding;
            tile.Visual.TileLarge = binding;

            return tile;
        }

        /// <summary>
        /// Generates a <see cref="TileBindingContentPhotos"/> for a given list of image URLs. 
        /// </summary>
        /// <param name="urls">
        ///     A <see cref="List{string}"/> containing the URLs to use for the tile images. May be app-relative or internet URLs.
        /// </param>
        /// <param name="maxCount">The maximum number of images to use for this particular binding size.</param>
        /// <returns></returns>
        private static TileBinding GetPhotosBinding(List<string> urls, int maxCount = 12)
        {

            var content = new TileBindingContentPhotos();

            foreach (var url in urls.Take(maxCount))
            {
                content.Images.Add(new TileImageSource(url));
            }

            return new TileBinding()
            {
                Content = content
            };

        }

        #endregion

        #endregion

        #region Private Methods

        #region Adaptive Helpers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static AdaptiveText GetCenteredAdaptiveText(string text, AdaptiveTextStyle style = AdaptiveTextStyle.CaptionSubtle)
        {
            return new AdaptiveText()
            {
                Text = text,
                HintWrap = true,
                HintAlign = AdaptiveTextAlign.Center,
                HintStyle = style
            };
        }

        #endregion

        #region Default Templates

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static TileContent GetTile(string BasePath = "Assets/Pokemons/")
        {
            // Create the notification content
            return new TileContent()
            {
                Visual = new TileVisual()
                {
                    Branding = TileBranding.Name,
                    BaseUri = new Uri(BasePath, UriKind.Relative)
                }
            };
        }

        #endregion

        private static string GetPokemonName(PokemonDataWrapper pokemon)
        {
            string PokemonName = String.Empty;

            try
            {
                PokemonName = Resources.Pokemon.GetString(pokemon.PokemonId.ToString());
            }
            catch { }

            if (PokemonName == String.Empty)
            {
                try
                {
                    PokemonSettings currentPokemon = GameClient.PokemonSettings.Where(x => x.PokemonId == pokemon.PokemonId).FirstOrDefault();
                    PokemonName = currentPokemon.PokemonId.ToString();
                }
                catch { }
            }

            return PokemonName;
        }

        #endregion

    }
}