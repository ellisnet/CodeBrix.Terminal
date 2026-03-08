using Microsoft.UI.Xaml;
using RemoteTerminal.Client.ViewModels;

#pragma warning disable IDE0003

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RemoteTerminal.Client.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        (this.RootUiElement.DataContext as IXamlRootGetter)?
            .SetXamlRootGetter(() => this.RootUiElement.XamlRoot);
        if (this.RootUiElement.DataContext is IScrollOutputToEnd scroll)
        {
            scroll.ScrollOutputToEnd = () => this.TerminalOutput.ScrollToEnd(onlyIfFull: true);
        }
        if (this.RootUiElement.DataContext is ITerminalOutput terminalOutput)
        {
            terminalOutput.FeedTerminalOutput = (text) => this.TerminalOutput.Feed(text);
        }
    }
}
