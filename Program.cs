using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

public class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private const UInt32 WM_CLOSE = 0x0010;
    private static List<string> blockedProcessNames = new List<string>();
    private static bool blockNewWindows = false;

    public static void Main()
    {
        Console.Title = "NoWindowSpam | Made by https://github.com/GabryB03/";

        if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator))
        {
            Console.WriteLine("The program is not run with Administrator privileges. Please, run it again with them.");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Welcome to NoWindowSpam, a program realized in order to preventively block window(s) spamming in your system coming from possible malwares.");
        Console.WriteLine("Currently listening. Do not close this window if you want protection.");

        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

        foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
        {
            thread.PriorityLevel = ThreadPriorityLevel.Highest;
        }

        Thread executeProtectionThread = new Thread(ExecuteProtection);
        executeProtectionThread.Priority = ThreadPriority.Highest;
        executeProtectionThread.Start();

        while (true)
        {
            Console.ReadLine();
        }
    }

    public static void ExecuteProtection()
    {
        while (true)
        {
            Thread.Sleep(10);
            List<IntPtr> allWindows = new List<IntPtr>();

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (process.Id == Process.GetCurrentProcess().Id)
                    {
                        continue;
                    }

                    List<IntPtr> handles = new List<IntPtr>();
                    handles = GetWindowHandles(process.Id);
                    allWindows.AddRange(handles);

                    foreach (Process process1 in Process.GetProcesses())
                    {
                        try
                        {
                            if (process.Id == process1.Id)
                            {
                                continue;
                            }

                            if (process.ProcessName.ToLower() == process1.ProcessName.ToLower())
                            {
                                handles.AddRange(GetWindowHandles(process1.Id));
                                allWindows.AddRange(handles);
                            }
                        }
                        catch
                        {

                        }
                    }

                    if (blockNewWindows)
                    {
                        continue;
                    }

                    if (handles.Count > 5 && !blockedProcessNames.Contains(process.ProcessName.ToLower()))
                    {
                        blockedProcessNames.Add(process.ProcessName.ToLower());
                    }

                    if (handles.Count > 5 || blockedProcessNames.Contains(process.ProcessName.ToLower()))
                    {
                        foreach (IntPtr handle in handles)
                        {
                            CloseWindow(handle);
                        }
                    }
                }
                catch
                {

                }
            }

            if (allWindows.Count > 100 || blockNewWindows)
            {
                blockNewWindows = true;

                foreach (IntPtr handle in allWindows)
                {
                    CloseWindow(handle);
                }
            }
        }
    }

    private static void CloseWindow(IntPtr hwnd)
    {
        SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    private static List<IntPtr> GetWindowHandles(int processId)
    {
        List<IntPtr> windowHandles = new List<IntPtr>();

        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                if (process.Id != processId)
                {
                    continue;
                }

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    if (process.MainWindowTitle != null && process.MainWindowTitle.Replace(" ", "").Replace('\t'.ToString(), "") != "")
                    {
                        if (IsWindowVisible(process.MainWindowHandle))
                        {
                            windowHandles.Add(process.MainWindowHandle);
                        }
                    }
                }
            }
            catch
            {

            }
        }

        return windowHandles;
    }
}