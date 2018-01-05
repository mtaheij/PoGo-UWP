using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PokemonGo_UWP.Controls
{
    public sealed partial class TextboxMessageDialog : UserControl
    {
        public TextboxMessageDialog()
        {
            this.InitializeComponent();

            InputField.GotFocus += InputField_GotFocus;
        }

        public TextboxMessageDialog(string text, int maxLength) : this()
        {
            Text = text;
            MaxLength = maxLength; 
        }

        #region Propertys

        public static readonly DependencyProperty TextProperty = 
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextboxMessageDialog),
                new PropertyMetadata(""));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(TextboxMessageDialog),
                new PropertyMetadata(50));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        private bool _selectAllOnTextBoxFocus;
        public bool SelectAllOnTextBoxFocus
        {
            get { return _selectAllOnTextBoxFocus; }
            set { _selectAllOnTextBoxFocus = value; }
        }

        #endregion

        public void FocusTextbox(FocusState focusState)
        {
            InputField.Focus(focusState);
        }

        private void InputField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_selectAllOnTextBoxFocus)
            {
                InputField.SelectAll();
            }
        }
    }
}
