using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace myApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Load DashboardView by default
            MainContent.Content = new DashboardView();
            
            
        }

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle visibility
            if (SideMenu != null)
                SideMenu.IsVisible = !SideMenu.IsVisible;
        }

        private void ShowIntro_Click(object sender, RoutedEventArgs e)
        {
            var window = this.VisualRoot as Window;
            if (window == null) return;

            var IntWindow = new IntroWindow();
            IntWindow.Title = "Intro";
            IntWindow.ShowDialog(window);
        }

        private void ShowDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        
        }
        
        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SettingsView();
        }
        
        private void ShowModels_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ModelsView();
        }
        
        private void ShowGallery_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new GalleryView();
        }
    }
}
