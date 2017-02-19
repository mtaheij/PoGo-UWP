using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace PokemonGo_UWP.Utils
{
    public static class AudioUtils
    {

        #region Audio Files

        public const string GAMEPLAY = "Gameplay.mp3";
        public const string ENCOUNTER_POKEMON = "EncounterPokemon.mp3";
        public const string POKEMON_FOUND_DING = "pokemon_found_ding.wav";
        public const string EVOLUTION = "Evolution.mp3";
        public const string GOTCHA = "Gotcha.mp3";
        public const string LEVELUP = "Levelup.mp3";
        public const string PROFESSOR = "Professor.mp3";
        public const string TITLE = "Title.mp3";
        public const string BEFORE_THE_FIGHT = "BeforeTheFight.mp3";

        public const string MESSAGE = "System_Message.mp3";
        public const string MAIN_XP = "Main_xp.mp3";

        #endregion

        #region Media Elements

        private static readonly MediaPlayer GameplaySound = new MediaPlayer();
        private static readonly MediaPlayer EncounterSound = new MediaPlayer();
        private static readonly MediaPlayer PokemonFoundSound = new MediaPlayer();
        private static readonly MediaPlayer EvolutionSound = new MediaPlayer();
        private static readonly MediaPlayer GotchaSound = new MediaPlayer();
        private static readonly MediaPlayer LevelupSound = new MediaPlayer();
        private static readonly MediaPlayer ProfessorSound = new MediaPlayer();
        private static readonly MediaPlayer TitleSound = new MediaPlayer();
        private static readonly MediaPlayer BeforeTheFightSound = new MediaPlayer();

        private static readonly MediaPlayer SingleSound = new MediaPlayer();

        #endregion

        /// <summary>
        /// Initializes audio assets by loading them from disk
        /// </summary>
        /// <returns></returns>
        static AudioUtils()
        {
            // Get files and create elements   
            GameplaySound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{GAMEPLAY}"));
            EncounterSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{ENCOUNTER_POKEMON}"));
            PokemonFoundSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{POKEMON_FOUND_DING}"));
            EvolutionSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{EVOLUTION}"));
            GotchaSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{GOTCHA}"));
            LevelupSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{LEVELUP}"));
            ProfessorSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{PROFESSOR}"));
            TitleSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{TITLE}"));
            BeforeTheFightSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{BEFORE_THE_FIGHT}"));

            SingleSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{MESSAGE}"));

            EncounterSound.MediaEnded += Sound_Ended;
            EvolutionSound.MediaEnded += Sound_Ended;
            GotchaSound.MediaEnded += Sound_Ended;
            LevelupSound.MediaEnded += Sound_Ended;

            // Set mode and volume
            GameplaySound.AudioCategory =   EncounterSound.AudioCategory = 
                                            PokemonFoundSound.AudioCategory =
                                            EvolutionSound.AudioCategory =
                                            GotchaSound.AudioCategory =
                                            LevelupSound.AudioCategory =
                                            ProfessorSound.AudioCategory =
                                            TitleSound.AudioCategory =
                                            BeforeTheFightSound.AudioCategory =
                                            SingleSound.AudioCategory = 
                                            MediaPlayerAudioCategory.GameMedia;

            // Enable loop only for gameplay sounds
            GameplaySound.IsLoopingEnabled = true;
            ProfessorSound.IsLoopingEnabled = true;
            BeforeTheFightSound.IsLoopingEnabled = true;

            ToggleSounds();
        }

        public static event EventHandler SoundEnded;

        private static void Sound_Ended(MediaPlayer sender, object args)
        {
            SoundEnded?.Invoke(null, null);
        }

        /// <summary>
        /// Sets volume based on settings
        /// </summary>
        public static void ToggleSounds()
        {
            GameplaySound.IsMuted =
                EncounterSound.IsMuted = 
                PokemonFoundSound.IsMuted =
                EvolutionSound.IsMuted =
                GotchaSound.IsMuted =
                LevelupSound.IsMuted =
                ProfessorSound.IsMuted =
                TitleSound.IsMuted =
                BeforeTheFightSound.IsMuted =
                SingleSound.IsMuted =
                !SettingsService.Instance.IsMusicEnabled;

        }

        /// <summary>
        /// Plays the selected asset
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static void PlaySound(string asset)
        {
            switch (asset)
            {
                case GAMEPLAY:
                    if (GameplaySound.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                        GameplaySound.Play();
                    StopSound(ENCOUNTER_POKEMON);
                    break;
                case ENCOUNTER_POKEMON:
                    GameplaySound.Pause();
                    EncounterSound.Play();
                    break;
                case POKEMON_FOUND_DING:
                    PokemonFoundSound.Play();
                    break;
                case EVOLUTION:
                    GameplaySound.Pause();
                    EvolutionSound.Play();
                    break;
                case GOTCHA:
                    GameplaySound.Pause();
                    GotchaSound.Play();
                    break;
                case LEVELUP:
                    GameplaySound.Pause();
                    LevelupSound.Play();
                    break;
                case PROFESSOR:
                    GameplaySound.Pause();
                    ProfessorSound.Play();
                    break;
                case BEFORE_THE_FIGHT:
                    GameplaySound.Pause();
                    BeforeTheFightSound.Play();
                    break;
                case TITLE:
                    TitleSound.Play();
                    break;
                default:
                    SingleSound.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Audio/{asset}"));
                    SingleSound.Play();
                    break;
            }
        }

        /// <summary>
        /// Stops the selected asset
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static void StopSound(string asset)
        {
            switch (asset)
            {
                case GAMEPLAY:
                    GameplaySound.Pause();
                    GameplaySound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case ENCOUNTER_POKEMON:
                    EncounterSound.Pause();
                    EncounterSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case POKEMON_FOUND_DING:
                    PokemonFoundSound.Pause();
                    PokemonFoundSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case EVOLUTION:
                    EvolutionSound.Pause();
                    EvolutionSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case GOTCHA:
                    GotchaSound.Pause();
                    GotchaSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case LEVELUP:
                    LevelupSound.Pause();
                    LevelupSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case PROFESSOR:
                    ProfessorSound.Pause();
                    ProfessorSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case BEFORE_THE_FIGHT:
                    BeforeTheFightSound.Pause();
                    BeforeTheFightSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
                case TITLE:
                    TitleSound.Pause();
                    TitleSound.PlaybackSession.Position = TimeSpan.Zero;
                    break;
            }
        }

        /// <summary>
        /// Stops all playing sounds
        /// </summary>
        public static void StopSounds()
        {
            GameplaySound.Pause();
            GameplaySound.PlaybackSession.Position = TimeSpan.Zero;
            EncounterSound.Pause();
            EncounterSound.PlaybackSession.Position = TimeSpan.Zero;
            PokemonFoundSound.Pause();
            PokemonFoundSound.PlaybackSession.Position = TimeSpan.Zero;
            EvolutionSound.Pause();
            EvolutionSound.PlaybackSession.Position = TimeSpan.Zero;
            GotchaSound.Pause();
            GotchaSound.PlaybackSession.Position = TimeSpan.Zero;
            LevelupSound.Pause();
            LevelupSound.PlaybackSession.Position = TimeSpan.Zero;
            ProfessorSound.Pause();
            ProfessorSound.PlaybackSession.Position = TimeSpan.Zero;
            BeforeTheFightSound.Pause();
            BeforeTheFightSound.PlaybackSession.Position = TimeSpan.Zero;
            TitleSound.Pause();
            TitleSound.PlaybackSession.Position = TimeSpan.Zero;
            SingleSound.Pause();
            SingleSound.PlaybackSession.Position = TimeSpan.Zero;
        }
    }
}