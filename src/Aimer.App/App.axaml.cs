using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aimer.App.ViewModels;
using Aimer.App.Views;

namespace Aimer.App;

public partial class App : Application
{
    private bool _exiting;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The tray menu shares the window's view model, so Apply uses the current settings.
            var viewModel = new MainViewModel();
            DataContext = viewModel;

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            // Closing the window only hides it to the tray.
            desktop.MainWindow.Closing += (_, e) =>
            {
                if (_exiting)
                    return;
                e.Cancel = true;
                desktop.MainWindow.Hide();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayClicked(object? sender, EventArgs e) => ShowMainWindow();

    private void OnShowClicked(object? sender, EventArgs e) => ShowMainWindow();

    private void OnExitClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _exiting = true;
            desktop.Shutdown();
        }
    }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return;

        window.Show();
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
