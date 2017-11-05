using Google.Protobuf.Collections;
using Microsoft.HockeyApp;
using Newtonsoft.Json;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Utils.Game;
using PokemonGo_UWP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Windows.Devices.Geolocation;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.ViewModels
{
    class TutorialPageViewModel : ViewModelBase
    {
        #region ctor
        public TutorialPageViewModel()
        {
        }
        #endregion

        #region Lifecycle Handlers

        /// <summary>
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="mode"></param>
        /// <param name="suspensionState"></param>
        /// <returns></returns>
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> suspensionState)
        {
            AudioUtils.StopSounds();

            var tutorialNavigationMode = (TutorialNavigationModes)parameter;
            if (tutorialNavigationMode == TutorialNavigationModes.StarterPokemonCatched)
            {
                await UpdateTutorialStateAfterCatch();
                return;
            }

            if (suspensionState.Any())
            {
                // Recovering the state     
                CurrentMessage = JsonConvert.DeserializeObject<MessageEntry>((string)suspensionState[nameof(CurrentMessage)]);
                Messages = JsonConvert.DeserializeObject<ObservableCollection<MessageEntry>>((string)suspensionState[nameof(Messages)]);
                RaisePropertyChanged(() => CurrentMessage);
            }
            else
            {
                // Load and start the Tutorial at the point where we left of (or at the beginning)
                SelectTutorialScreen(GameClient.PlayerData.TutorialState);
            }
        }

        /// <summary>
        ///     Save state before navigating
        /// </summary>
        /// <param name="suspensionState"></param>
        /// <param name="suspending"></param>
        /// <returns></returns>
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> suspensionState, bool suspending)
        {
            if (suspending)
            {
                suspensionState[nameof(CurrentMessage)] = JsonConvert.SerializeObject(CurrentMessage);
                suspensionState[nameof(Messages)] = JsonConvert.SerializeObject(Messages);
            }

            HideAllScreens?.Invoke(this, null);

            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        #endregion

        #region Bindable vars
        private MessageEntry _currentMessage;
        public MessageEntry CurrentMessage
        {
            get { return _currentMessage; }
            set { Set(ref _currentMessage, value); }
        }

        public ObservableCollection<MessageEntry> Messages { get; set; } = new ObservableCollection<MessageEntry>();

        private bool _legalCommsCheck = true;
        public bool LegalCommsCheck
        {
            get { return _legalCommsCheck; }
            set { Set(ref _legalCommsCheck, value); }
        }

        #endregion

        #region Management Vars

        TutorialState currentState;
        EncounterTutorialCompleteResponse encounterTutorialCompleteResponse;

        #region Events
        public event EventHandler HideAllScreens;
        public event EventHandler ShowLegalScreen;
        public event EventHandler ShowSelectAvatarScreen;
        public event EventHandler ShowPokemonCaptureScreen;
        public event EventHandler ShowNameSelectionScreen;
        public event EventHandler ShowItsTimeToWalkScreen;
        public event EventHandler<string> RequestError;
        #endregion

        #endregion

        #region Logic
        private void SelectTutorialScreen(RepeatedField<TutorialState> tutorialState)
        {
            HideAllScreens?.Invoke(this, null);

            if (!tutorialState.Contains(TutorialState.LegalScreen))
            {
                currentState = TutorialState.LegalScreen;
                LegalCommsCheck = true;
                RaisePropertyChanged(nameof(LegalCommsCheck));
                ShowLegalScreen?.Invoke(this, null);
                return;
            }
            if (!tutorialState.Contains(TutorialState.AvatarSelection))
            {
                currentState = TutorialState.AvatarSelection;
                ShowSelectAvatarScreen?.Invoke(this, null);
                return;
            }
            if (!tutorialState.Contains(TutorialState.PokemonCapture))
            {
                currentState = TutorialState.PokemonCapture;
                ShowPokemonCaptureScreen?.Invoke(this, null);
                return;
            }
            if (!tutorialState.Contains(TutorialState.NameSelection))
            {
                currentState = TutorialState.NameSelection;
                ShowNameSelectionScreen?.Invoke(this, null);
                return;
            }
            if (!tutorialState.Contains(TutorialState.FirstTimeExperienceComplete))
            {
                currentState = TutorialState.FirstTimeExperienceComplete;
                ShowItsTimeToWalkScreen?.Invoke(this, null);
                return;
            }
            currentState = TutorialState.PokestopTutorial;
        }

        //private DelegateCommand _tappedCommand;

        ///// <summary>
        /////     The screen is tapped, advance to the next message
        ///// </summary>
        //public DelegateCommand TappedCommand => _tappedCommand ?? (
        //    _tappedCommand = new DelegateCommand(() =>
        //    {
        //        Messages.RemoveAt(0);
        //        if (Messages.Count > 0)
        //        {
        //            CurrentMessage = Messages.FirstOrDefault();
        //            RaisePropertyChanged(() => CurrentMessage);

        //        }

        //    }, () => true));

        #region LegalScreen Commands
        private DelegateCommand _legalAcceptCommand;
        public DelegateCommand LegalAcceptCommand => _legalAcceptCommand ?? (
            _legalAcceptCommand = new DelegateCommand(async () =>
            {
                var res = await GameClient.MarkTutorialComplete(new TutorialState[] { TutorialState.LegalScreen }, LegalCommsCheck, false);
                if (!res.Success)
                {
                    RequestError?.Invoke(this, "Error accepting the Niantic ToS");
                }

                await GameClient.UpdateProfile();
                SelectTutorialScreen(GameClient.PlayerData.TutorialState);
            }, () => true));

        private DelegateCommand _legalDeclineCommand;
        public DelegateCommand LegalDeclineCommand => _legalDeclineCommand ?? (
            _legalDeclineCommand = new DelegateCommand(async () =>
            {
                var dialog = new MessageDialog("By declining the disclaimer, you will not be able to play this game. The app will now close.", "Declined");

                var result = await dialog.ShowAsyncQueue();

                App.Current.Exit();

            }, () => true));
        #endregion

        #region Select Avatar Commands
        public event EventHandler AvatarMaleSelected;
        private DelegateCommand _selectAvatarMaleCommand;
        public DelegateCommand SelectAvatarMaleCommand => _selectAvatarMaleCommand ?? (
            _selectAvatarMaleCommand = new DelegateCommand(() =>
            {
                SelectedAvatar = Gender.Male;
                AvatarMaleSelected?.Invoke(this, null);
            }));

        public event EventHandler AvatarFemaleSelected;
        private DelegateCommand _selectAvatarFemaleCommand;
        public DelegateCommand SelectAvatarFemaleCommand => _selectAvatarFemaleCommand ?? (
            _selectAvatarFemaleCommand = new DelegateCommand(() =>
            {
                SelectedAvatar = Gender.Female;
                AvatarFemaleSelected?.Invoke(this, null);
            }));

        public Gender SelectedAvatar { get; set; }

        public event EventHandler AvatarOkSelected;
        private DelegateCommand _selectAvatarOkCommand;
        public DelegateCommand SelectAvatarOkCommand => _selectAvatarOkCommand ?? (
            _selectAvatarOkCommand = new DelegateCommand( async() =>
            {
                var avatarRes = await GameClient.SetAvatar(new PlayerAvatar()
                {
                    Backpack = 0,
                    Eyes = 0,
                    Avatar = SelectedAvatar == Gender.Male ? 0: 1,
                    Hair = 0,
                    Hat = 0,
                    Pants = 0,
                    Shirt = 0,
                    Shoes = 0,
                    Skin = 0
                });

                if (avatarRes.Status == SetAvatarResponse.Types.Status.AvatarAlreadySet ||
                    avatarRes.Status == SetAvatarResponse.Types.Status.Success)
                {
                    var res = await GameClient.MarkTutorialComplete(new TutorialState[] { TutorialState.AvatarSelection }, LegalCommsCheck, false);
                    if (!res.Success)
                    {
                        RequestError?.Invoke(this, "Error completing Avatarselect tutorial");
                    }

                    AvatarOkSelected?.Invoke(this, null);
                    await GameClient.UpdateProfile();
                    SelectTutorialScreen(GameClient.PlayerData.TutorialState);
                }
            }));

        #endregion

        #region Catch Pokémon Commands
        /// <summary>
        ///     Key for Bing's Map Service (not included in GIT, you need to get your own token to use maps!)
        /// </summary>
        public string MapServiceToken => ApplicationKeys.MapServiceToken;

        /// <summary>
        ///     Collection of Starter Pokemon in 1 step from current position
        /// </summary>
        public ObservableCollection<MapPokemonWrapper> CatchablePokemons { get; set; } =
            new ObservableCollection<MapPokemonWrapper>();

        public void CreateStarterPokemons(Geopoint playerLocation)
        {
            var poke1 = new MapPokemon()
            {
                PokemonId = PokemonId.Bulbasaur,
                Latitude = playerLocation.Position.Latitude + 0.0005,
                Longitude = playerLocation.Position.Longitude + 0.0005
            };
            var poke2 = new MapPokemon()
            {
                PokemonId = PokemonId.Charmander,
                Latitude = playerLocation.Position.Latitude - 0.0005,
                Longitude = playerLocation.Position.Longitude + 0.0005
            };
            var poke3 = new MapPokemon()
            {
                PokemonId = PokemonId.Squirtle,
                Latitude = playerLocation.Position.Latitude - 0.0005,
                Longitude = playerLocation.Position.Longitude - 0.0005
            };
            CatchablePokemons.Add(new MapPokemonWrapper(poke1));
            CatchablePokemons.Add(new MapPokemonWrapper(poke2));
            CatchablePokemons.Add(new MapPokemonWrapper(poke3));
            RaisePropertyChanged(nameof(CatchablePokemons));
        }

        public void ClearStarterPokemons()
        {
            CatchablePokemons.Clear();
            RaisePropertyChanged(nameof(CatchablePokemons));
        }

        public event EventHandler PokemonCaptured;
        private async Task UpdateTutorialStateAfterCatch()
        {
            var res = await GameClient.MarkTutorialComplete(new TutorialState[] { TutorialState.PokemonCapture }, LegalCommsCheck, false);
            if (!res.Success)
            {
                //RequestError?.Invoke(this, "Error completing PokemonCapture tutorial");
            }

            PokemonCaptured?.Invoke(this, null);
            await GameClient.UpdateProfile();
            SelectTutorialScreen(GameClient.PlayerData.TutorialState);
        }
        #endregion

        #region Choose Nickname commands
        public event EventHandler NicknameEntered;
        private DelegateCommand _submitNicknameCommand;
        public DelegateCommand SubmitNicknameCommand => _submitNicknameCommand ?? (
            _submitNicknameCommand = new DelegateCommand(() =>
            {
                NicknameEntered?.Invoke(this, null);
            }));

        public string SelectedNickname { get; set; }

        public event EventHandler NicknameOkSubmitted;
        private DelegateCommand _submitNicknameOkCommand;
        public DelegateCommand SubmitNicknameOkCommand => _submitNicknameOkCommand ?? (
            _submitNicknameOkCommand = new DelegateCommand(async () =>
            {
                if (SelectedNickname.Length == 0)
                {
                    RequestError?.Invoke(this, "You have to choose a Nickname!");
                    NicknameCancelled?.Invoke(this, null);
                    return;
                }

                var res = await GameClient.ClaimCodename(SelectedNickname);

                switch (res.Status)
                {
                    case ClaimCodenameResponse.Types.Status.Success:
                        await MarkNameSelectionComplete();
                        NicknameOkSubmitted?.Invoke(this, null);
                        return;
                    case ClaimCodenameResponse.Types.Status.CodenameNotAvailable:
                        RequestError?.Invoke(this, "That nickname isn't available, pick another one!");
                        break;
                    case ClaimCodenameResponse.Types.Status.CodenameNotValid:
                        RequestError?.Invoke(this, "That nickname is not valid, pick another one!");
                        break;
                    case ClaimCodenameResponse.Types.Status.CurrentOwner:
                        //RequestError?.Invoke(this, "You already own that nickname!");
                        await MarkNameSelectionComplete();
                        NicknameOkSubmitted?.Invoke(this, null);
                        break;
                    case ClaimCodenameResponse.Types.Status.CodenameChangeNotAllowed:
                        //RequestError?.Invoke(this, "You can't change your nickname anymore!");
                        await MarkNameSelectionComplete();
                        NicknameOkSubmitted?.Invoke(this, null);
                        break;
                    default:
                        RequestError?.Invoke(this, "Unknown response while setting Nickname");
                        break;
                }
                NicknameCancelled?.Invoke(this, null);
            }));

        public async Task MarkNameSelectionComplete()
        {
            var response = await GameClient.MarkTutorialComplete(new TutorialState[] { TutorialState.NameSelection }, LegalCommsCheck, false);

            if (!response.Success)
            {
                RequestError?.Invoke(this, "Error completing NameSelection tutorial");
            }
        }

        public event EventHandler NicknameCancelled;
        private DelegateCommand _submitNicknameCancelCommand;
        public DelegateCommand SubmitNicknameCancelCommand => _submitNicknameCancelCommand ?? (
            _submitNicknameCancelCommand = new DelegateCommand(() =>
            {
                NicknameCancelled?.Invoke(this, null);
            }));

        #endregion

        #region Let's GO commands
        private DelegateCommand _letsGoCommand;
        public DelegateCommand LetsGoCommand => _letsGoCommand ?? (
            _letsGoCommand = new DelegateCommand(async () =>
            {
                var response = await GameClient.MarkTutorialComplete(new TutorialState[] { TutorialState.FirstTimeExperienceComplete }, LegalCommsCheck, false);

                if (!response.Success)
                {
                    RequestError?.Invoke(this, "Error completing FirstTimeExperience tutorial");
                }
                AudioUtils.StopSounds();

                HockeyClient.Current.TrackPageView("GameMapPage");
                await NavigationService.NavigateAsync(typeof(GameMapPage), GameMapNavigationModes.AppStart);
            }, () => true));
        #endregion

        #region Skip tutorial commands
        public event EventHandler TutorialSkipRequested;
        private DelegateCommand _skipTutorialCommand;
        public DelegateCommand SkipTutorialCommand => _skipTutorialCommand ?? (
            _skipTutorialCommand = new DelegateCommand(() =>
            {
                TutorialSkipRequested?.Invoke(this, null);
            }));

        private DelegateCommand _submitSkipTutorialOkCommand;
        public DelegateCommand SubmitSkipTutorialOkCommand => _submitSkipTutorialOkCommand ?? (
            _submitSkipTutorialOkCommand = new DelegateCommand(async() =>
            {
                SettingsService.Instance.SkipTutorial = true;

                AudioUtils.StopSounds();

                HockeyClient.Current.TrackPageView("GameMapPage");
                await NavigationService.NavigateAsync(typeof(GameMapPage), GameMapNavigationModes.AppStart);

            }, () => true));

        public event EventHandler TutorialSkipCancelled;
        private DelegateCommand _submitSkipTutorialCancelCommand;
        public DelegateCommand SubmitSkipTutorialCancelCommand => _submitSkipTutorialCancelCommand ?? (
            _submitSkipTutorialCancelCommand = new DelegateCommand(() =>
            {
                TutorialSkipCancelled?.Invoke(this, null);
            }));
        #endregion

        #endregion
    }
}
