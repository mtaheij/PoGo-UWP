using Google.Protobuf;
using Newtonsoft.Json;
using POGOLib.Official.LoginProviders;
using POGOLib.Official.Net.Authentication.Data;
using POGOLib.Official.Util.Hash;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using PokemonGo_UWP.Exceptions;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using POGOLib.Official.Util.Hash.PokeHash;

namespace PokemonGo_UWP.ViewModels
{
    public class PokehashKeyPageViewModel : ViewModelBase
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
            // Prevent from going back to other pages
            NavigationService.ClearHistory();
            if (suspensionState.Any())
            {
                // Recovering the state
                PokehashKey = (string)suspensionState[nameof(PokehashKey)];
            }
            else
            {
                PokehashKey = SettingsService.Instance.PokehashAuthKey;
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
                suspensionState[nameof(PokehashKey)] = PokehashKey;
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        #endregion

        #region Game Management Vars

        private string _pokehashkey;

        #endregion

        #region Bindable Game Vars

        public string CurrentVersion => GameClient.CurrentVersion;

        public string PokehashKey
        {
            get { return _pokehashkey; }
            set
            {
                Set(ref _pokehashkey, value);
                DoStoreCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region Game Logic

        private DelegateCommand _doStoreCommand;

        public DelegateCommand DoStoreCommand => _doStoreCommand ?? (
            _doStoreCommand = new DelegateCommand(async () =>
            {
                // Store Pokehash auth key
                SettingsService.Instance.PokehashAuthKey = PokehashKey;

                // Check if accesstoken is available and still valid
                var tokenString = SettingsService.Instance.AccessTokenString;
                if (tokenString != null && tokenString.Length > 0)
                {
                    AccessToken accessToken = GameClient.GetAccessToken();

                    if (accessToken != null)
                    {
                        GameClient.SetCredentialsFromSettings();
                        try
                        {
                            await GameClient.InitializeSession();
                            if (GameClient.IsInitialized)
                            {
                                await NavigationService.NavigateAsync(typeof(GameMapPage), GameMapNavigationModes.AppStart);
                            }
                        }
                        catch (PtcLoginException ex)
                        {
                            var errorMessage = ex.Message ?? Utils.Resources.CodeResources.GetString("PtcLoginFailed");
                            ConfirmationDialog dialog = new Views.ConfirmationDialog(errorMessage);
                            dialog.Show();

                            await NavigationService.NavigateAsync(typeof(MainPage));
                        }
                        catch (LocationException)
                        {

                        }
                        catch (PokeHashException)
                        {
                            var errorMessage = Utils.Resources.CodeResources.GetString("PokeHashException");
                            ConfirmationDialog dialog = new Views.ConfirmationDialog(errorMessage);
                            dialog.Closed += (ss, ee) => { Application.Current.Exit(); };
                            dialog.Show();
                        }
                        catch (Exception ex)
                        {
                            var errorMessage = ex.Message ?? Utils.Resources.CodeResources.GetString("NoValidHashKey");
                            ConfirmationDialog dialog = new Views.ConfirmationDialog(errorMessage);
                            dialog.Show();
                        }
                    }
                    else
                    {
                        await NavigationService.NavigateAsync(typeof(MainPage));
                    }
                }
                else
                {
                    await NavigationService.NavigateAsync(typeof(MainPage));
                }

            }, () => !string.IsNullOrEmpty(PokehashKey))
            );
        #endregion
    }
}