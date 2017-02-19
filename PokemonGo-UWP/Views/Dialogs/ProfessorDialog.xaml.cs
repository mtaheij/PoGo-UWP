using Microsoft.Graphics.Canvas.Effects;
using POGOProtos.Enums;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Template10.Common;
using Template10.Controls;
using Template10.Mvvm;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Effects;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class ProfessorDialog : UserControl
    {
        private bool IgnoreGridTapped;

        public ProfessorDialog(BackGroundType BackGroundType, bool SpotLight)
        {
            this.InitializeComponent();

            SetBackground(BackGroundType);
            SetSpotlight(SpotLight);
            this.DataContext = this;
        }

        public MessageEntry CurrentMessage { get; set; }

        public ObservableCollection<MessageEntry> Messages { get; set; } = new ObservableCollection<MessageEntry>();

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

        public void SetSpotlight(bool spotlight)
        {
            Spotlight.Visibility = spotlight ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Displays the dialog modally
        /// </summary>
        public void Show()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                var modal = Window.Current.Content as ModalDialog;
                if (modal == null)
                {
                    return;
                }

                _formerModalBrush = modal.ModalBackground;
                modal.ModalBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                modal.ModalContent = this;
                modal.IsModal = true;

                CurrentMessage = Messages.FirstOrDefault();

                // animate
                Storyboard sb = this.Resources["ShowDialogStoryboard"] as Storyboard;
                sb.Begin();

                AudioUtils.PlaySound(AudioUtils.MESSAGE);
            });
        }

        private Brush _formerModalBrush = null;

        private void Hide()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                var modal = Window.Current.Content as ModalDialog;
                if (modal == null)
                {
                    return;
                }

                // animate
                Storyboard sb = this.Resources["HideDialogStoryboard"] as Storyboard;
                sb.Begin();
                sb.Completed += Cleanup;
            });
        }

        private void Cleanup(object sender, object e)
        {
            var modal = Window.Current.Content as ModalDialog;
            if (modal == null)
            {
                return;
            }

            modal.ModalBackground = _formerModalBrush;
            modal.ModalContent = null;
            modal.IsModal = false;

            Closed?.Invoke(this, null);
        }

        public event EventHandler Closed;

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Messages.RemoveAt(0);
            if (Messages.Count > 0)
            {
                CurrentMessage = Messages.FirstOrDefault();

                DataContext = null;
                DataContext = this;

                AudioUtils.PlaySound(AudioUtils.MESSAGE);
            }
            else
            {
                Hide();
            }
        }
    }

    public enum BackGroundType
    {
        Light,
        Dark
    }

    public class MessageEntry
    {
        public string Message { get; set; }
        public int DialogHeight { get; set; }

        public MessageEntry(string message, int dialogHeight)
        {
            Message = message;
            DialogHeight = dialogHeight;
        }
    }
}
