using System.Windows;

namespace WpfPriceApp.Views
{
    /// <summary>비밀번호를 맞출 때까지(또는 취소할 때까지) 닫히지 않는 확인 창.
    /// Enter 키 제출은 "확인" 버튼의 IsDefault="True" 하나로만 처리한다 — 별도로
    /// PasswordBox 에 KeyDown 핸들러까지 달면 Enter 를 눌렀을 때 Ok_Click 이 두 번
    /// 호출되어(IsDefault 경로 + 수동 핸들러), 이미 닫히는 중인 창에 DialogResult 를
    /// 다시 설정하려다 예외가 나면서 프로그램이 아무 반응 없이 종료되는 문제가 있었다.</summary>
    public partial class PasswordDialog : Window
    {
        private readonly string _expectedPassword;

        public PasswordDialog(string title, string prompt, string expectedPassword)
        {
            InitializeComponent();
            Title = title;
            TitleText.Text = title;
            PromptText.Text = prompt;
            _expectedPassword = expectedPassword;
            Loaded += (_, _) => PasswordBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password == _expectedPassword)
            {
                DialogResult = true;
                return;
            }
            ErrorText.Visibility = Visibility.Visible;
            PasswordBox.Clear();
            PasswordBox.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
