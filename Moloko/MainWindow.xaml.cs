using System.Windows;
using Moloko.ViewModels;

namespace Moloko;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
