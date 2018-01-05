using System.Threading.Tasks;
using Windows.UI.Popups;
using PokemonGo_UWP.Views;
using Template10.Common;
using System;
using Windows.UI.Xaml;
using Microsoft.HockeyApp;
using POGOLib.Official.Util.Hash;

namespace PokemonGo_UWP.Utils
{
    public static class ExceptionHandler
    {
        public static async Task HandleException(Exception e = null)
        {
            try
            {
                // Some exceptions can be caught and repaired
                if (e != null && (e.GetType() == typeof(HashVersionMismatchException)))
                {

                }
                else
                {
                    bool showDebug = false;
                    try
                    {
                        //get inside try/catch in case exception comes from settings instance (storage access issue, ...)
                        showDebug = SettingsService.Instance.ShowDebugInfoInErrorMessage;
                    }
                    catch { }

                    string message = Resources.CodeResources.GetString("SomethingWentWrongText");
                    if (showDebug)
                    {
                        message += $"\nException";
                        message += $"\n Message:[{e?.Message}]";
                        message += $"\n InnerMessage:[{e?.InnerException?.Message}]";
                        message += $"\n StackTrace:[{e?.StackTrace}]";
                    }

                    var dialog = new MessageDialog(message);
                    dialog.Commands.Add(new UICommand(Resources.CodeResources.GetString("YesText")) { Id = 0 });
                    dialog.Commands.Add(new UICommand(Resources.CodeResources.GetString("NoText")) { Id = 1 });
                    dialog.DefaultCommandIndex = 0;
                    dialog.CancelCommandIndex = 1;
                    var result = await dialog.ShowAsyncQueue();
                    if ((int)result.Id == 0)
                    {
                        GameClient.DoLogout();
                        BootStrapper.Current.NavigationService.Navigate(typeof(MainPage));
                        Busy.SetBusy(false);
                    }
                }
            }
            catch (Exception ex)
            {
                HockeyClient.Current.TrackException(ex);
                Application.Current.Exit();
            }
        }
    }
}