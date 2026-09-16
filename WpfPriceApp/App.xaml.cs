using System.Windows;
using System.Windows.Threading;

namespace WpfPriceApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // 기본값(OnLastWindowClose)이면 로그인 창이 "마지막 창"이라서, 비밀번호를 맞게
            // 입력해 로그인 창이 닫히는 순간 메인 창을 띄우기도 전에 앱이 종료돼버린다.
            // 메인 창을 띄우기 전까지는 창이 닫혀도 앱이 안 꺼지도록 명시적으로 바꿔둔다.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 프로그램 실행 시 비밀번호를 한 번 확인한다. 틀리거나 취소하면 바로 종료.
            var login = new Views.PasswordDialog("로그인", "프로그램을 사용하려면 비밀번호를 입력하세요.", AppConfig.Password);
            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var main = new MainWindow();
            MainWindow = main;
            ShutdownMode = ShutdownMode.OnMainWindowClose; // 이제부터는 메인 창을 닫으면 앱 종료
            main.Show();
        }

        // 처리되지 않은 예외로 프로그램이 아무 안내 없이 조용히 꺼지는 것을 막는다.
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("예기치 않은 오류가 발생했습니다.\n" + e.Exception.Message, "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
