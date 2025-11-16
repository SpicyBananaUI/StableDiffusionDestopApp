using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace myApp
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboardView = new DashboardView();
        private readonly SettingsView _settingsView = new SettingsView();
        private readonly ModelsView _modelsView = new ModelsView();
        private readonly GalleryView _galleryView = new GalleryView();

        public MainWindow()
        {
            InitializeComponent();

            // Load DashboardView by default
            MainContent.Content = _dashboardView;
            
            
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
            MainContent.Content = _dashboardView;
        
        }
        
        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _settingsView;
        }
        
        private void ShowModels_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _modelsView;
        }
        
        private void ShowGallery_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _galleryView;
        }
    }
}
