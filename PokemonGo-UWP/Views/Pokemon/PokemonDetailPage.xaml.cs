using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.System.Threading;
using Windows.UI.Core;
using PokemonGo_UWP.Utils;

namespace PokemonGo_UWP.Views
{
    public enum PokemonDetailPageViewMode
    {
        Normal,
        ReceivedPokemon
    }

    public sealed partial class PokemonDetailPage : Page
    {
        public PokemonDetailPage()
        {
            this.InitializeComponent();
        }

        #region Overrides of Page

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Animation to prevent flickering when setting the selected Pokemon
            // TODO: Find more elegant and better looking solution
            TimeSpan delay = TimeSpan.FromMilliseconds(100);

            ThreadPoolTimer DelayThread = ThreadPoolTimer.CreateTimer(
            (source1) =>
            {
                Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    PokeDetailFlip.Visibility = Visibility.Visible;
                });
            }, delay);
            SubscribeToCaptureEvents();

            AudioUtils.SoundEnded += AudioUtils_SoundEnded;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            UnsubscribeToCaptureEvents();

            AudioUtils.SoundEnded -= AudioUtils_SoundEnded;
        }

        #endregion

        #region Handlers

        private void SubscribeToCaptureEvents()
        {
            ViewModel.PokemonEvolved += ViewModelOnPokemonEvolved;
            ShowEvolveMenuStoryboard.Completed += async (s, e) =>
            {
                await Task.Delay(1000);
                EvolvePokemonStoryboard.Begin();
            };
            EvolvePokemonStoryboard.Completed += async (s, e) =>
            {
                await Task.Delay(1000);
                ViewModel.EvolveAnimationIsRunning = false;
                ViewModel.NavigateToEvolvedPokemonCommand.Execute();
            };
        }

        private void UnsubscribeToCaptureEvents()
        {
            ViewModel.PokemonEvolved -= ViewModelOnPokemonEvolved;
        }

        private void ViewModelOnPokemonEvolved(object sender, EventArgs evolvePokemonResponse)
        {
            ViewModel.EvolveAnimationIsRunning = true;
            ShowEvolveMenuStoryboard.Begin();
            AudioUtils.StopSounds();
            AudioUtils.PlaySound(AudioUtils.EVOLUTION);
        }

        private void AudioUtils_SoundEnded(object sender, EventArgs e)
        {
            AudioUtils.PlaySound(AudioUtils.GAMEPLAY);
        }

        #endregion

    }
}
