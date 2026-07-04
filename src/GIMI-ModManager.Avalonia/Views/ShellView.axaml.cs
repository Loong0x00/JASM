using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using GIMI_ModManager.Avalonia.ViewModels;

namespace GIMI_ModManager.Avalonia.Views;

public partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (DataContext is not ShellViewModel vm)
            return;

        if (e.IsSettingsInvoked)
        {
            vm.NavigateSettingsCommand.Execute(null);
            return;
        }

        var tag = (e.InvokedItemContainer as NavigationViewItem)?.Tag as string;
        if (tag == "characters")
            vm.NavigateCharactersCommand.Execute(null);
    }

    private void OnBackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
    {
        (DataContext as ShellViewModel)?.GoBackCommand.Execute(null);
    }
}
