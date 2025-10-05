// Copyright (c) 2025 Spicy Banana
// SPDX-License-Identifier: AGPL-3.0-only


using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace myApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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
        private static string GetBatchPath() =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "setup_scripts", "launch_sdapi_server.bat");

        private static string GetProjectRoot() =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."); // folder containing backend, myApp, setup_scripts

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

            var dummyWindow = new Window {Opacity = 0, ShowInTaskbar = false, Width = 0, Height = 0};
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
                        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        break;
                }
                
            }
            
            dummyWindow.Close();
        }

        base.OnFrameworkInitializationCompleted();
    }
}