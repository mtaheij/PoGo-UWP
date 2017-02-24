using Google.Protobuf.Collections;
using Newtonsoft.Json;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.ViewModels
{
    class TutorialPageViewModel : ViewModelBase
    {
        #region Lifecycle Handlers

        /// <summary>
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="mode"></param>
        /// <param name="suspensionState"></param>
        /// <returns></returns>
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> suspensionState)
        {
            if (suspensionState.Any())
            {
                // Recovering the state     
                CurrentMessage = JsonConvert.DeserializeObject<MessageEntry>((string)suspensionState[nameof(CurrentMessage)]);
                Messages = JsonConvert.DeserializeObject<ObservableCollection<MessageEntry>>((string)suspensionState[nameof(Messages)]);
                RaisePropertyChanged(() => CurrentMessage);
            }
            else
            {
                // Navigating from game page, so load and start the Tutorial
                SelectTutorialStep(GameClient.PlayerData.TutorialState);
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

        #endregion

        #region Management Vars

        TutorialState currentState;
        EncounterTutorialCompleteResponse encounterTutorialCompleteResponse;

        #region Events
        public event EventHandler ShowLegalScreen;
        #endregion

        #endregion

        #region Logic
        private void SelectTutorialStep(RepeatedField<TutorialState> tutorialState)
        {
            if (!tutorialState.Contains(TutorialState.LegalScreen))
            {
                currentState = TutorialState.LegalScreen;
                ShowLegalScreen?.Invoke(this, null);
            }
            if (!tutorialState.Contains(TutorialState.AvatarSelection))
            {
                currentState = TutorialState.AvatarSelection;
            }
            if (!tutorialState.Contains(TutorialState.PokemonCapture))
            {
                currentState = TutorialState.PokemonCapture;
            }
            if (!tutorialState.Contains(TutorialState.NameSelection))
            {
                currentState = TutorialState.NameSelection;
            }
            if (!tutorialState.Contains(TutorialState.FirstTimeExperienceComplete))
            {
                currentState = TutorialState.FirstTimeExperienceComplete;
            }
        }

        private DelegateCommand _tappedCommand;

        /// <summary>
        ///     The screen is tapped, advance to the next message
        /// </summary>
        public DelegateCommand TappedCommand => _tappedCommand ?? (
            _tappedCommand = new DelegateCommand(async () =>
            {
                Messages.RemoveAt(0);
                if (Messages.Count > 0)
                {
                    CurrentMessage = Messages.FirstOrDefault();
                    RaisePropertyChanged(() => CurrentMessage);

                }

            }, () => true));

        #endregion
    }
}
