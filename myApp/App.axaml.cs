using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using myApp.Services;

namespace myApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public static PromptAssistantService? PromptAssistant { get; set; }
    
    public static ApiService? ApiService { get; set; }

    public static void InitializeApiService()
    {
        ApiService = new ApiService();
    }

    public enum RunMode
    {
        Local,
        RemoteServer,
        RemoteClient
    }

    public static class AppConfig
    {
        public static RunMode Mode { get; set; }
        public static string RemoteAddress { get; set; } =  "http://127.0.0.1:7861";
    }
    
    public static class BackendLauncher
    {
        private static string GetBatchPath()
        {
            // Base directory of the running app (frontend publish folder)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Candidate: installed layout - batch scripts are placed into the application root by the installer
            var installedRoot = Path.GetFullPath(Path.Combine(baseDir, "..")); // {app}
            var installedLaunchBackend = Path.Combine(installedRoot, "launch_backend.bat");
            var installedLaunchApp = Path.Combine(installedRoot, "launch_app.bat");
            var installedSetupScript = Path.Combine(installedRoot, "setup_scripts", "launch_sdapi_server.bat");

            if (File.Exists(installedLaunchBackend)) return installedLaunchBackend;
            if (File.Exists(installedLaunchApp)) return installedLaunchApp;
            if (File.Exists(installedSetupScript)) return installedSetupScript;

            // Fallback: development layout (project root relative to output folder)
            return Path.Combine(baseDir, "..", "..", "..", "..", "setup_scripts", "launch_sdapi_server.bat");
        }

        private static string GetProjectRoot()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var installedRoot = Path.GetFullPath(Path.Combine(baseDir, "..")); // {app}\
            // If installer layout exists (there's a launch_backend.bat in the app root), run from app root
            if (File.Exists(Path.Combine(installedRoot, "launch_backend.bat")) || File.Exists(Path.Combine(installedRoot, "launch_app.bat")))
            {
                return installedRoot;
            }
            // Otherwise fallback to project root for development
            return Path.Combine(baseDir, "..", "..", "..", ".."); // folder containing backend, myApp, setup_scripts
        }

        public static void LaunchLocalBackend()
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetBatchPath(),
                Arguments = "",
                WorkingDirectory = GetProjectRoot(),
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);
        }

        public static void LaunchRemoteServer()
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetBatchPath(),
                Arguments = "--listen",
                WorkingDirectory = GetProjectRoot(),
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);
        }

        public static void ConnectToRemoteServer()
        {
            //AppConfig.RemoteAddress = $"http://{ip}:{port}";
        }
    }

    
    private void StartBackend(RunMode mode)
    {
        switch (mode)
        {
            case RunMode.Local:
                // For now use old macos setup (Copy the backend into Application support and run it)
                BackendManager.EnsureBackendFromBundleMac();
                BackendManager.DownloadDreamshaperMac();
                BackendManager.RunBackendMac();

                // Start backend locally with -listen so frontend can connect
                BackendLauncher.LaunchLocalBackend();
                break;

            case RunMode.RemoteServer:
                // Start backend on this machine as a server
                BackendLauncher.LaunchRemoteServer();
                break;

            case RunMode.RemoteClient:
                // Connect to remote backend server
                //BackendLauncher.ConnectToRemoteServer();
                break;
        }
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ConfigManager.Load();

            var apiKey = ConfigManager.Settings.ApiKey;
            //var apiKey = "sk-xLB2fdKiR0vHflZWF2az-Q";
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                PromptAssistant = new PromptAssistantService(apiKey);
            }

            var dummyWindow = new Window
            {
                Opacity = 0, ShowInTaskbar = false, Width = 0, Height = 0, WindowStartupLocation = WindowStartupLocation.CenterScreen, Background = Brushes.Transparent,
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome, ExtendClientAreaToDecorationsHint = true, TransparencyLevelHint = new List<WindowTransparencyLevel>{WindowTransparencyLevel.Transparent}
            };
            dummyWindow.Show();
            desktop.MainWindow = dummyWindow;

            var selector = new ModeSelectorWindow();
            var chosenMode = await selector.ShowDialog<RunMode?>(desktop.MainWindow);

            if (chosenMode is RunMode mode)
            {
                AppConfig.Mode = mode;
                StartBackend(AppConfig.Mode);


                switch (AppConfig.Mode)
                {
                    case RunMode.Local:
                    case RunMode.RemoteClient:
                        desktop.MainWindow = new MainWindow();
                        desktop.MainWindow.Show();
                        break;
                        
                    case RunMode.RemoteServer:
                        //desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        break;
                }
                
            }
            
            dummyWindow.Close();
        }

        base.OnFrameworkInitializationCompleted();
    }
}