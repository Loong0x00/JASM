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
        switch (tag)
        {
            case "characters":
                vm.NavigateCharactersCommand.Execute(null);
                break;
            case "presets":
                vm.NavigatePresetsCommand.Execute(null);
                break;
            case "overview":
                vm.NavigateOverviewCommand.Execute(null);
                break;
        }
    }

    private void OnBackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
    {
        (DataContext as ShellViewModel)?.GoBackCommand.Execute(null);
    }
}
