using PokemonGo_UWP.Entities;
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
    public sealed partial class GymMemberControl : UserControl
    {
        public GymMemberControl()
        {
            this.InitializeComponent();
        }

        public FortDataWrapper CurrentGym { get; set; }

        public void SetCurrentGym(FortDataWrapper currentGym)
        {
            CurrentGym = currentGym;
        }
    }
}
