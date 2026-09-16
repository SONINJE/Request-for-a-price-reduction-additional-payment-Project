using System.Windows;

namespace WpfPriceApp.Views
{
    public partial class TextInputDialog : Window
    {
        public string InputText => InputBox.Text.Trim();

        public TextInputDialog(string prompt, string title, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputBox.Text = defaultValue;
            Loaded += (_, _) => { InputBox.Focus(); InputBox.SelectAll(); };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputBox.Text))
            {
                MessageBox.Show("값을 입력하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
