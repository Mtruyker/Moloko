using System.Windows;
using Moloko.Models;
using Moloko.Services;
using Moloko.ViewModels;

namespace Moloko;

public partial class MainWindow : Window
{
    public MainWindow(IAppDataStore store, UserAccount currentUser)
    {
        InitializeComponent();
        DataContext = new MainViewModel(store, currentUser);
    }
}
