using PokemonGo_UWP.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class GymMemberControl : UserControl
    {
        public GymMemberControl()
        {
            this.InitializeComponent();
        }

        #region Properties

        public static readonly DependencyProperty CurrentGymProperty =
            DependencyProperty.Register(nameof(CurrentGym), typeof(bool), typeof(FortDataWrapper),
                new PropertyMetadata(null));

        public FortDataWrapper CurrentGym
        {
            get { return (FortDataWrapper)GetValue(CurrentGymProperty); }
            set { SetValue(CurrentGymProperty, value); }
        }

        public string PokemonName
        {
            get
            {
                BindingExpression bindingExpression = PokemonNameTextBlock.GetBindingExpression(TextBox.TextProperty);
                return string.Empty;
            }
        }
        #endregion
    }
}
