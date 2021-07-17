#pragma warning disable CS0219

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using DiscordRPC;
using DiscordRPC.Logging;
using DiscordRPC.Message;

namespace RobloxStudioRPC
{
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            int uFlags);

        private const int HWND_TOPMOST = -1;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;

        private static DiscordRpcClient client;
        private static string ClientID = "865768504580767754";

        private enum FileType
        {
            None,
            RBXL,
            Lua
        }

        private static string currentFile = "";
        private static FileType currentFileType = FileType.None;
        private static bool isStudioOpen = false;
        private static TimeSpan runTime;

        static void Main(string[] args)
        {
            /*IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
            SetWindowPos(hWnd,
                new IntPtr(HWND_TOPMOST),
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE);*/
            client = new DiscordRpcClient(ClientID);
            client.Logger = new ConsoleLogger
            {
                Level = LogLevel.Info,
                Coloured = true
            };
            client.OnReady += delegate (object sender, ReadyMessage msg)
            {
                Console.WriteLine("Connected to discord with user {0}", msg.User.Username);
            };
            client.OnPresenceUpdate += delegate (object sender, PresenceMessage msg)
            {
                Console.WriteLine("Presence has been updated");
            };
            System.Timers.Timer t = new System.Timers.Timer(150.0);
            t.Elapsed += delegate (object sender, ElapsedEventArgs evt)
            {
                client.Invoke();
            };
            t.Start();
            client.Initialize();
            client.SetPresence(new RichPresence
            {
                Details = "Idling",
                State = "Idle",
                Timestamps = Timestamps.FromTimeSpan(10.0),
                Assets = new Assets
                {
                    LargeImageKey = "logo",
                    LargeImageText = "Roblox Studio"
                }
            });
            Console.Title = "RoStudio RPC";
            Timer timer = new Timer(5 * 1000);
            timer.Elapsed += Main_Loop;
            timer.Start();
            Main_Loop(null, null);
            while (timer != null) ;
        }

        static void Main_Loop(object source, ElapsedEventArgs e)
        {
            var old = (file: currentFile, type: currentFileType, studioOpen: isStudioOpen);
            Process[] processlist = Process.GetProcesses();
            bool foundStudioTick = false;
            string fileName = "";
            FileType fileType = FileType.None;
            foreach (Process process in processlist)
            {
                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    string title = process.MainWindowTitle;
                    if (title.IndexOf(" - Roblox Studio") > 0)
                    {
                        foundStudioTick = true;
                        runTime = (DateTime.Now - process.StartTime);
                        Console.Title = $"Studio PID: {process.Id}";
                        Match editName = new Regex(@"\w+.rbxl").Match(title);
                        if (editName != null && editName.Value != null && editName.Value != "")
                        {
                            fileName = editName.Value.Replace(".rbxl", "");
                            fileType = FileType.RBXL;
                        } else
                        {
                            editName = new Regex(@"\w+ \-").Match(title);
                            if (editName != null && editName.Value != null)
                            {
                                fileName = editName.Value.Replace(" -", "");
                                fileType = FileType.Lua;
                            }
                        }
                    }
                }
            }
            if (!foundStudioTick)
            {
                Console.Title = "Studio is not running";
                client.ClearPresence();
            } else
            {
                currentFileType = fileType;
                currentFile = fileName;
                isStudioOpen = foundStudioTick;
                if (old.file != currentFile || old.type != currentFileType || old.studioOpen != isStudioOpen)
                {
                    Console.WriteLine($"Currently editing: {currentFile}.{currentFileType.ToString().ToLower()}");
                    client.SetPresence(new RichPresence
                    {
                        Details = "Editing",
                        State = $"{currentFile}.{currentFileType.ToString().ToLower()}",
                        Timestamps = Timestamps.FromTimeSpan(runTime),
                        Assets = new Assets
                        {
                            LargeImageKey = "logo",
                            LargeImageText = "Roblox Studio"
                        }
                    });
                }
            }
        }
    }
}
