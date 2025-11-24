using System;
using System.Runtime.InteropServices;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading.Tasks;
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
        public static string BackendPassword { get; set; } = "";
        public static bool EnableTranslationLayer { get; set; } = true;
    }
    
    public static class BackendLauncher
    {
        private static string AppName = "SDApp";
        
        private static string GetBatchPath() // Windows only
        {
            // Base directory of the running app (frontend publish folder)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Candidate: installed layout - batch scripts are placed into the application root by the installer
            var installedRoot = Path.GetFullPath(Path.Combine(baseDir, "..")); // {app}
            var installedLaunchBackend = Path.Combine(installedRoot, "launch_backend.bat");            var installedSetupScript = Path.Combine(installedRoot, "setup_scripts", "launch_sdapi_server.bat");

            if (File.Exists(installedLaunchBackend)) return installedLaunchBackend;
            if (File.Exists(installedSetupScript)) return installedSetupScript;

            // Fallback: development layout (project root relative to output folder)
            return Path.Combine(baseDir, "..", "..", "..", "..", "setup_scripts", "launch_sdapi_server.bat");
        }
        private static string GetShellPathMac()
        {
          #if DEBUG
            // Use local scripts in development (Debug)
            var scriptsDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "../../../../setup_scripts");
          #else
            // Use bundled scripts in Application Support for release
            var scriptsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppName,
                "setup_scripts");
            // Base directory of the running app (frontend publish folder)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
          #endif
            
            Console.WriteLine($"Using scripts directory: {scriptsDir}");

            return Path.Combine(scriptsDir, "launch_sdapi_server.sh");

        }
        private static string GetShellPathLinux()
        {
            // TODO: Linux implementation
            return "";
        }

        private static string GetProjectRootWindows()
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
        private static string GetProjectRootMac()
        {
          #if DEBUG
            // Use local scripts in development (Debug)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "..", "..", "..", "..");
          #else
            // Use bundled scripts in Application Support for release
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppName
            );
          #endif
        }
        private static string GetProjectRootLinux()
        {
            // TODO: Linux implementation
            return "";
        }


        public static void LaunchLocalBackend()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                LaunchLocalBackendMac();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                LaunchLocalBackendWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                LaunchLocalBackendLinux();
            }
            else
            {
                throw new NotSupportedException("Local backend launch is not supported on this platform.");
            }
        }
        private static void LaunchLocalBackendWindows()
        {
            var args = "";
            if (!AppConfig.EnableTranslationLayer)
            {
                args += " --disable-translation-layer";
            }

            var psi = new ProcessStartInfo
            {
                FileName = GetBatchPath(),
                Arguments = args,
                WorkingDirectory = GetProjectRootWindows(),
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);
        }
        private static void LaunchLocalBackendMac()
        {
            try
            {
                var scriptPath = GetShellPathMac();
                Console.WriteLine($"Launching local backend script: {scriptPath}");
                
                var args = $"\"{scriptPath}\" --local-backend";
                
                if (!AppConfig.EnableTranslationLayer)
                {
                    args += " --disable-translation-layer";
                }
                
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = args,
                    WorkingDirectory = GetProjectRootMac(),
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR launching local backend: {ex.Message}");
                // Try to log to a file in AppData since Console might not be visible
                try {
                    var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SDApp", "launcher_error.log");
                    File.AppendAllText(logPath, $"{DateTime.Now}: Error launching backend: {ex.Message}\n{ex.StackTrace}\n");
                } catch { /* ignore */ }
            }
        }
        private static void LaunchLocalBackendLinux()
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetShellPathLinux(),
                Arguments = "",
                WorkingDirectory = GetProjectRootLinux(),
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);
        }

        public static void LaunchRemoteServer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                LaunchRemoteServerMac();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                LaunchRemoteServerWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                LaunchRemoteServerLinux();
            }
            else
            {
                throw new NotSupportedException("Remote server launch is not supported on this platform.");
            }
        }
        private static void LaunchRemoteServerWindows()
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetBatchPath(),
                Arguments = "--listen",
                WorkingDirectory = GetProjectRootWindows(),
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);
        }
        private static void LaunchRemoteServerLinux()
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetShellPathLinux(),
                Arguments = "--listen",
                WorkingDirectory = GetProjectRootLinux(),
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);
        }
        private static void LaunchRemoteServerMac()
        {
            var scriptPath = GetShellPathMac();
            var workingDir = GetProjectRootMac();
            
            // Use osascript to open Terminal and execute the script
            var escapedScriptPath = scriptPath.Replace("'", "'\\''");
            var escapedWorkingDir = workingDir.Replace("'", "'\\''");
            
            // Command to run in Terminal: cd to dir, then run script
            var command = $"cd '{escapedWorkingDir}' && '{escapedScriptPath}' --remote-server";
            var escapedCommand = command.Replace("\\", "\\\\").Replace("\"", "\\\"");
            
            var osaPsi = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add("tell application \"Terminal\"");
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add("activate");
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add($"do script \"{escapedCommand}\"");
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add("end tell");
            
            try
            {
                Process.Start(osaPsi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not open Terminal via osascript: {ex.Message}");
            }
        }

        private static string GetInstallerScriptPathMac()
        {
            // In app bundle, setup_scripts is in Contents/setup_scripts
            // BaseDirectory is Contents/MacOS, so we need to go up one level
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            #if DEBUG
                return Path.Combine(baseDir, "..", "..", "..", "..", "setup_scripts", "setup_app_support.sh");
            #else
                return Path.Combine(baseDir, "..", "setup_scripts", "setup_app_support.sh");
            #endif
        }
        
        private static string GetAppBundleRootMac()
        {
            // Get the app bundle root (Contents directory parent)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory; // Contents/MacOS
            #if DEBUG
                return Path.Combine(baseDir, "..", "..", "..", ".."); // project root
            #else
                return Path.Combine(baseDir, "..", ".."); // .app/Contents
            #endif
        }
        
        public static void FinishInstallMac()
        {
            var scriptPath = GetInstallerScriptPathMac();
            var appBundleRoot = GetAppBundleRootMac();
            
            // Verify script exists
            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"ERROR: Setup script not found at: {scriptPath}");
                return;
            }
            
            // Get absolute paths
            var absoluteScriptPath = Path.GetFullPath(scriptPath);
            var absoluteAppBundleRoot = Path.GetFullPath(appBundleRoot);
            
            // Create a wrapper script that opens in Terminal and runs the setup
            var wrapperScript = Path.Combine(Path.GetTempPath(), $"setup_backend_{Guid.NewGuid()}.sh");
            var escapedScriptPath = absoluteScriptPath.Replace("'", "'\\''");
            var escapedAppBundleRoot = absoluteAppBundleRoot.Replace("'", "'\\''");
            
            var wrapperContent = $"#!/bin/bash\n" +
                               $"echo '=== SDApp Backend Setup ==='\n" +
                               $"echo ''\n" +
                               $"echo 'App Bundle Root: {escapedAppBundleRoot}'\n" +
                               $"echo 'Script Path: {escapedScriptPath}'\n" +
                               $"echo ''\n" +
                               $"cd '{escapedAppBundleRoot}'\n" +
                               $"if [ ! -f '{escapedScriptPath}' ]; then\n" +
                               $"  echo 'ERROR: Script not found at {escapedScriptPath}'\n" +
                               $"  exit 1\n" +
                               $"fi\n" +
                               $"bash '{escapedScriptPath}' '{escapedAppBundleRoot}'\n" +
                               $"EXIT_CODE=$?\n" +
                               $"echo ''\n" +
                               $"if [ $EXIT_CODE -eq 0 ]; then\n" +
                               $"  echo 'Setup completed successfully!'\n" +
                               $"else\n" +
                               $"  echo 'Setup failed with exit code:' $EXIT_CODE\n" +
                               $"fi\n" +
                               $"echo ''\n" +
                               $"echo 'Press Enter to close...'\n" +
                               $"read\n";
            
            File.WriteAllText(wrapperScript, wrapperContent);
            
            // Make it executable
            var chmod = new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x \"{wrapperScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Process.Start(chmod)?.WaitForExit();
            
            // Use osascript to open Terminal and execute the script
            // This is the most reliable method from app bundles
            var escapedWrapperScript = wrapperScript.Replace("\\", "\\\\").Replace("\"", "\\\"");
            
            var osaPsi = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add("tell application \"Terminal\"");
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add("activate");
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add($"do script \"{escapedWrapperScript}\"");
            osaPsi.ArgumentList.Add("-e");
            osaPsi.ArgumentList.Add("end tell");
            
            try
            {
                Process.Start(osaPsi);
            }
            catch (Exception ex)
            {
                // If osascript fails (e.g., permission issues), log error but continue
                Console.WriteLine($"Warning: Could not open Terminal via osascript: {ex.Message}");
                Console.WriteLine($"You may need to run the setup manually:");
                Console.WriteLine($"  {wrapperScript}");
            }
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    BackendLauncher.FinishInstallMac();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running Mac install setup: {ex.Message}");
                }
            }

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