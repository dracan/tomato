using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Tomato.Helpers;

namespace Tomato;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionInCorner();
    }

    /// <summary>
    /// Positions the window in the bottom-right corner of the primary screen.
    /// </summary>
    private void PositionInCorner()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        DragMove();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        DwmHelper.SetDarkTitleBar(hwnd, enable: true);
    }
}
