using System.Windows;
using System.Windows.Input;
using Petek.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<LoginViewModel>();

        // Listen for login success
        if (DataContext is LoginViewModel vm)
        {
            vm.OnLoginSuccess += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                });
            };
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
        {
            if (vm.LoginWithCredentialsCommand.CanExecute(null))
            {
                vm.LoginWithCredentialsCommand.Execute(null);
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        // Allow dragging the borderless window
        this.DragMove();
    }
}
