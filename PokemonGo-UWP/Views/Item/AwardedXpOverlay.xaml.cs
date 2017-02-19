using PokemonGo_UWP.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Template10.Common;
using Template10.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class AwardedXpOverlay : UserControl
    {
        public AwardedXpOverlay()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private int _xpCount;
        public int XpCount
        {
            get { return _xpCount; }
            set { _xpCount = value; }
        }

        /// <summary>
        /// Displays the selection menu modally
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

                // animate
                Storyboard sb = this.Resources["ShowAwardedXPStoryBoard"] as Storyboard;
                sb.Completed += Sb_Completed;
                sb.Begin();

                AudioUtils.PlaySound(AudioUtils.MAIN_XP);
            });
        }

        private void Sb_Completed(object sender, object e)
        {
            Storyboard sb = this.Resources["MoveAwardedXPStoryBoard"] as Storyboard;
            sb.Begin();

            Hide();
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
                Storyboard sb = this.Resources["HideAwardedXPStoryBoard"] as Storyboard;
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
        }
    }
}
