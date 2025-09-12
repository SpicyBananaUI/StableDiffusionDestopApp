using Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace myApp;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        RunBackendMac();
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    
    private static void RunBackendMac()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("Not macOS, skipping backend launch.");
                return;
            }
            
            string backendScriptPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "../../../../setup_scripts/launch_sdapi_server.sh"
                );


            // Start the shell script (ensure it's executable)
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = backendScriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true, // Capture output for debugging
                RedirectStandardError = true
            };

            // Start the process and capture any errors/output
            var process = Process.Start(startInfo);
            if (process != null)
            {
                bool isReady = false;
                Int16 timeoutCounter = 0;

                // Handle output for debugging
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };

                // Handle error output for debugging and readiness check
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.Error.WriteLine(e.Data);
                        if (e.Data.Contains("Application startup complete"))
                        {
                            isReady = true;
                        }
                    }
                };

                // Begin reading output and error asynchronously
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for the backend to signal it's ready
                while (!isReady)
                {
                    // Wait for the backend process to finish or be ready
                    Thread.Sleep(500); // Check every 500ms, adjust as needed
                    timeoutCounter++;
                    if (timeoutCounter >= 500)
                    {
                        throw new TimeoutException();
                    }
                }
            }
            else
            {
                Console.WriteLine("Failed to start MacOS backend process.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting MacOS backend: " + ex.Message);
        }
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
