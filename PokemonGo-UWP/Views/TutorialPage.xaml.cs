using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class TutorialPage : Page
    {
        public TutorialPage()
        {
            this.InitializeComponent();

            SetBackground(BackGroundType.Dark);
            SetSpotlight(false);
            SetProfessorImage(ProfessorImageType.Dark);
        }

        #region Overrides of Page

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SubscribeToEvents();
        }


        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            UnsubscribeToEvents();
        }

        #endregion

        #region Handlers

        private void SubscribeToEvents()
        {
            ViewModel.ShowLegalScreen += GameManagerViewModelOnShowLegalScreen;
        }

        private void UnsubscribeToEvents()
        {
            ViewModel.ShowLegalScreen -= GameManagerViewModelOnShowLegalScreen;
        }

        private void GameManagerViewModelOnShowLegalScreen(object sender, EventArgs eventArgs)
        {
        }

        #endregion

        public void SetBackground(BackGroundType backgroundType)
        {
            switch (backgroundType)
            {
                case BackGroundType.Dark:
                    LightBackgroundRect.Visibility = Visibility.Collapsed;
                    DarkBackgroundRect.Visibility = Visibility.Visible;
                    break;
                case BackGroundType.Light:
                    LightBackgroundRect.Visibility = Visibility.Visible;
                    DarkBackgroundRect.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        public void SetProfessorImage(ProfessorImageType professorImageType)
        {
            switch (professorImageType)
            {
                case ProfessorImageType.Dark:
                    ProfessorLight.Visibility = Visibility.Collapsed;
                    ProfessorDark.Visibility = Visibility.Visible;
                    break;
                case ProfessorImageType.Light:
                    ProfessorLight.Visibility = Visibility.Visible;
                    ProfessorDark.Visibility = Visibility.Collapsed;
                    break;
            }
        }
        public void SetSpotlight(bool spotlight)
        {
            Spotlight.Visibility = spotlight ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    public enum ProfessorImageType
    {
        Light,
        Dark
    }

}
