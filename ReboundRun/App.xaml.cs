﻿using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundRun
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        public static WindowEx bkgWindow;

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            CreateShortcut();

            try
            {
                // Background window creation
                bkgWindow = new WindowEx();
                bkgWindow.SystemBackdrop = new TransparentTintBackdrop();
                bkgWindow.IsMaximizable = false;
                bkgWindow.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
                bkgWindow.SetWindowStyle(WindowStyle.Visible);
                bkgWindow.Activate();  // Activate the background window
                bkgWindow.MoveAndResize(0, 0, 0, 0);
                bkgWindow.Minimize();  // Minimize to make sure it's not in focus
                bkgWindow.SetWindowOpacity(0);  // Set opacity to 0
            }
            catch
            {
                // Handle errors here
            }

            m_window = new MainWindow();

            // Register any background tasks or hooks
            StartHook();

            // If started with the "STARTUP" argument, exit early
            if (string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("STARTUP"))
            {
                return;
            }

            // Activate the main window
            m_window.Activate();

            // Ensure m_window is brought to the front with more delay
            await Task.Delay(100);  // Increase delay slightly
            m_window.Activate();  // Reactivate the main window to ensure focus
            m_window.Show();  // Show the window

            ((WindowEx)m_window).BringToFront();
        }

        private void CreateShortcut()
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = System.IO.Path.Combine(startupFolderPath, "ReboundRun.lnk");
            string appPath = "C:\\Rebound11\\rrunSTARTUP.exe";

            if (!System.IO.File.Exists(shortcutPath))
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.Description = "Rebound Run";
                shortcut.TargetPath = appPath;
                shortcut.Save();
            }
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int VK_R = 0x52;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private static IntPtr hookId = IntPtr.Zero;
        private static LowLevelKeyboardProc keyboardProc;

        public static void StartHook()
        {
            keyboardProc = HookCallback;
            hookId = SetHook(keyboardProc);
        }

        public static void StopHook()
        {
            UnhookWindowsHookEx(hookId);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static bool winKeyPressed = false;
        private static bool rKeyPressed = false;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Check for keydown events
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    // Check if Windows key is pressed
                    if (vkCode is VK_LWIN or VK_RWIN)
                    {
                        winKeyPressed = true;
                    }

                    // Check if 'R' key is pressed
                    if (vkCode is VK_R)
                    {
                        rKeyPressed = true;

                        // If both Win and R are pressed, show the window
                        if (winKeyPressed)
                        {
                            ((WindowEx)m_window).Show();
                            ((WindowEx)m_window).BringToFront();
                            try
                            {
                                ((WindowEx)m_window).Activate();
                            }
                            catch
                            {
                                m_window = new MainWindow();
                                m_window.Show();
                                ((WindowEx)m_window).Activate();
                                ((WindowEx)m_window).BringToFront();
                            }
                            finally
                            {
                                //(m_window as MainWindow).CloseRunBoxMethod();
                            }

                            // Prevent default behavior of Win + R
                            return (IntPtr)1;
                        }
                    }
                }

                // Check for keyup events
                if (wParam == (IntPtr)WM_KEYUP)
                {
                    /*switch (vkCode)
                    {
                        case VK_LWIN | VK_RWIN:
                            {
                                winKeyPressed = false;

                                // Suppress the Windows Start menu if 'R' is still pressed
                                if (rKeyPressed)
                                {
                                    return (IntPtr)1; // Prevent Windows menu from appearing
                                }
                                return 0;
                            }
                        case VK_R:
                            {
                                rKeyPressed = false;

                                // Suppress the Windows Start menu if 'R' is still pressed
                                if (winKeyPressed)
                                {
                                    return (IntPtr)1; // Prevent Windows menu from appearing
                                }
                                return 0;
                            }
                        default:
                            {
                                return 0;
                            }
                    }*/

                    // Check if Windows key is released
                    if (vkCode is VK_LWIN or VK_RWIN)
                    {
                        winKeyPressed = false;

                        // Suppress the Windows Start menu if 'R' is still pressed
                        if (rKeyPressed == true)
                        {
                            ForceReleaseWin();
                            return (IntPtr)1; // Prevent Windows menu from appearing
                        }
                    }

                    // Check if 'R' key is released
                    if (vkCode is VK_R)
                    {
                        rKeyPressed = false;
                    }
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public static async void ForceReleaseWin()
        {
            await Task.Delay(10);

            var inj = InputInjector.TryCreate();
            var info = new InjectedInputKeyboardInfo();
            info.VirtualKey = (ushort)VirtualKey.LeftWindows;
            info.KeyOptions = InjectedInputKeyOptions.KeyUp;
            var infocol = new[] { info };

            inj.InjectKeyboardInput(infocol);
        }

        public const int INPUT_KEYBOARD = 1;
        public const int KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEKEYBDHARDWAREINPUT_KBD ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEKEYBDHARDWAREINPUT_KBD
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void ReleaseKey(ushort keyCode)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].mkhi.ki.wVk = keyCode;
            inputs[0].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;
            inputs[0].mkhi.ki.time = 0;
            inputs[0].mkhi.ki.dwExtraInfo = IntPtr.Zero;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        private void RegisterBackgroundTask()
        {
            var taskRegistered = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "ReboundRunBackgroundActivation")
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder
                {
                    Name = "ReboundRunBackgroundActivation",
                    TaskEntryPoint = "ReboundRun.ReboundRunBackgroundActivation"
                };

                // Set a trigger for your background task
                builder.SetTrigger(new TimeTrigger(15, false)); // For example, trigger every 15 minutes

                // Register the background task
                builder.Register();
            }
        }

        public static Window m_window;
    }

    public sealed class ReboundRunBackgroundActivation : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            // Run a loop in the background
            try
            {
                while (true)
                {
                    // Perform some background work
                    // Example: Log the current time
                    System.Diagnostics.Debug.WriteLine($"Background task running at {DateTime.Now}");

                    // Sleep for a specified interval before running the loop again
                    await Task.Delay(TimeSpan.FromMinutes(1)); // Adjust as needed
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                System.Diagnostics.Debug.WriteLine($"Error in background task: {ex.Message}");
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
