using Microsoft.HockeyApp;
using POGOProtos.Data.Logs;
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
using Template10.Utils;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.ViewModels
{
    class ActionLogPageViewModel : ViewModelBase
    {

        #region Lifecycle Handlers

        /// <summary>
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="mode"></param>
        /// <param name="suspensionState"></param>
        /// <returns></returns>
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode,
            IDictionary<string, object> suspensionState)
        {
            if (suspensionState.Any())
            {
                // Recovering the state
                ActionLog = new ObservableCollection<ActionLogEntry>();
                RaisePropertyChanged(() => ActionLog);
            }
            else
            {
                // No saved state, get them from the client
                ReadActionLog();
            }
            await Task.CompletedTask;
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
                //suspensionState[nameof(ActionLog)] = ActionLog.ToByteString().ToBase64();
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        #endregion

        #region Bindable Game Vars
        public ObservableCollection<ActionLogEntry> ActionLog { get; private set; } =
            new ObservableCollection<ActionLogEntry>();

        #endregion

        #region Read Action Log
        private async void ReadActionLog()
        {
            ActionLog.Clear();

            var sfidaActionLogResponse = await GameClient.GetSfidaActionLog();
            switch (sfidaActionLogResponse.Result)
            {
                case SfidaActionLogResponse.Types.Result.Success:
                    break;
                default:
                    return;
            }

            ActionLog.AddRange(sfidaActionLogResponse.LogEntries.Where(i => i != null).OrderByDescending(o => o.TimestampMs));
        }

        #endregion

        #region Game Logic

        #region Shared Logic

        private DelegateCommand _returnToProfileScreen;

        /// <summary>
        ///     Going back to profile page
        /// </summary>
        public DelegateCommand ReturnToProfileScreen => 
            _returnToProfileScreen ?? (_returnToProfileScreen = new DelegateCommand(() => 
            {
                HockeyClient.Current.TrackEvent("GoBack from ActionLogPage");
                NavigationService.GoBack();
            }, 
                () => true));

        #endregion

        #endregion
    }
}
