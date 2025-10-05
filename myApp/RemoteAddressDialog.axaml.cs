using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace myApp;

public partial class RemoteAddressDialog : Window
{
    public RemoteAddressDialog()
    {
        InitializeComponent();
    }
    
    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close(AddressBox.Text); // return user input
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null); // user cancelled
    }
}