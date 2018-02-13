using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Timers;
using System.IO;
using Fougerite;
using UnityEngine;

namespace ShutDown
{
    public class ShutDown : Fougerite.Module
    {
        public override string Name { get { return "ShutDown"; } }
        public override string Author { get { return "Salva/Juli"; } }
        public override string Description { get { return "ShutDown"; } }
        public override Version Version { get { return new Version("1.0"); } }

        public string red = "[color #B40404]";
        public string blue = "[color #81F7F3]";
        public string green = "[color #82FA58]";
        public string yellow = "[color #F4FA58]";
        public string orange = "[color #FF8000]";
        public string pink = "[color #FA58F4]";
        public string white = "[color #FFFFFF]";
        public List<string> IDS = new List<string>();
        public string pathIDS = Directory.GetCurrentDirectory() + "\\save\\ShutDown\\IDS.txt";
        public string pathLog = Directory.GetCurrentDirectory() + "\\save\\ShutDown\\Log.txt";
        public bool UnderRestarting = false;
        public override void Initialize()
        {
            ReloadCfg();
            Fougerite.Hooks.OnCommand += OnCommand;
            Hooks.OnConsoleReceived += OnConsoleReceived;
        }
        public override void DeInitialize()
        {
            Fougerite.Hooks.OnCommand -= OnCommand;
            Hooks.OnConsoleReceived -= OnConsoleReceived;
        }
        public void OnConsoleReceived(ref ConsoleSystem.Arg arg, bool external)
        {
            if (arg.Class == "help" && arg.Function == "help" && ((arg.argUser != null && arg.argUser.admin) || arg.argUser == null))
            {
                Logger.LogError("PLUGIN: " + Name + " " + Version);
                Logger.Log("shutdown.start - START SHUTDOWN SEQUENCE");
                Logger.Log("shutdown.stop - STOP SHUTDOWN SEQUENCE");
                Logger.LogError("");
            }
            if (arg.Class == "shutdown" && arg.Function == "start" && ((arg.argUser != null && arg.argUser.admin) || arg.argUser == null))
            {
                Logger.Log("Shutdown Started");
                UnderRestarting = true;
                string argumentos = "Restart by RCON";
                Fougerite.Player pl = new Fougerite.Player();
                pl.Name = "RCON";
                RegisterShutDown(pl, argumentos);
                ExecuteShutDown();
            }
            if (arg.Class == "shutdown" && arg.Function == "stop" && ((arg.argUser != null && arg.argUser.admin) || arg.argUser == null))
            {
                Logger.Log("Shutdown Stopped");
                UnderRestarting = false;
            }
        }
        public void OnCommand(Fougerite.Player pl, string cmd, string[] args)
        {
            if (cmd == "shutdown")
            {
                if (IDS.Contains(pl.SteamID))
                {
                    pl.MessageFrom("AutoRestart", green + "USE " + white + "/shutdown" + green + " TO SEE THIS HELP");
                    pl.MessageFrom("AutoRestart", green + "USE " + white + "/sdstart WriteReason" + green + " Start the restart sequence");
                    pl.MessageFrom("AutoRestart", green + "USE " + white + "/sdstop" + green + " Stop the restart sequence");
                    pl.MessageFrom("AutoRestart", green + "USE " + white + "/sdreload" + green + " TO RELOAD ID LIST (only Admin)");
                }
                else
                {
                    pl.MessageFrom("AutoRestart", red + "You are not authorized to use this command :(");
                }
                
            }
            else if (cmd == "sdstart")
            {
                if (IDS.Contains(pl.SteamID))
                {
                    if (args.Length > 0)
                    {
                        if (UnderRestarting == true)
                        {
                            pl.MessageFrom("AutoRestart", orange + "A restart sequence is already active ... use " + white + "/sdstop" + orange + " to stop it");
                        }
                        else
                        {
                            UnderRestarting = true;
                            string argumentos = string.Join(" ", args);
                            RegisterShutDown(pl,argumentos);
                            ExecuteShutDown();
                        }
                    }
                    else
                    {
                        pl.MessageFrom("AutoRestart", orange + "You need to write a reason to restart the server");
                        pl.MessageFrom("AutoRestart", green + "Example: " + white + "/sdstart juli server bug !!!");
                    }
                }
                else
                {
                    pl.MessageFrom("AutoRestart", red + "You are not authorized to use this command :(");
                }
            }
            else if (cmd == "sdstop")
            {
                if (IDS.Contains(pl.SteamID))
                {
                    UnderRestarting = false;
                    pl.MessageFrom("AutoRestart", green + "You have stopped Restart sequence..");
                }
                else
                {
                    pl.MessageFrom("AutoRestart", red + "You are not authorized to use this command :(");
                }
            }
            else if (cmd == "sdreload")
            {
                if (pl.Admin)
                {
                    ReloadCfg();
                    pl.MessageFrom("AutoRestart", green + "Config reloaded!!!");
                }
            }
        }
        public void ReloadCfg()
        {
            if (!File.Exists(pathIDS))
            {
                File.Create(pathIDS).Dispose();
                StreamWriter WriteReportFile = File.AppendText(pathIDS);
                WriteReportFile.WriteLine("123456789");
                WriteReportFile.Close();
            }
            IDS.Clear();
            foreach (string str in File.ReadAllLines(pathIDS))
            {
                IDS.Add(str);
            }
            return;
        }
        public void ExecuteShutDown()
        {
            ConsoleSystem.Run("save.all", false);
            Server.GetServer().BroadcastFrom("AutoRestart", orange + "The Server has saved all the advances, Initializing the RESTART sequence...");
            Logger.Log("The Server has saved all the advances, Initializing the RESTART sequence...");
            int Ciclos = 12;

            Timer reloj = new Timer();
            reloj.Interval = 10000;
            reloj.AutoReset = true;
            reloj.Elapsed += (x, y) =>
            {
                if (Ciclos == 0)
                {
                    Server.GetServer().BroadcastFrom("AutoRestart", orange + "RESTARTING SERVER...");
                    Logger.Log("RESTARTING SERVER...");
                    Process.GetCurrentProcess().Kill();
                }
                else
                {
                    if (UnderRestarting == false)
                    {
                        reloj.Dispose();
                        Server.GetServer().BroadcastFrom("AutoRestart", green + "The restart sequence has been Cancelled :)");
                        Logger.Log("The restart sequence has been Cancelled :)");
                    }
                    else
                    {
                        Ciclos -= 1;
                        Server.GetServer().BroadcastFrom("AutoRestart", red + "The Server will RESTART in " + white + (Ciclos * 10).ToString() + red + " seconds.");
                        ConsoleSystem.Print("The Server will RESTART in " + (Ciclos * 10).ToString() + " seconds.");
                    }
                }

                if (Ciclos == 1)
                {
                    try
                    {
                        ConsoleSystem.Run("save.all", false);// EVITAR DUPLICAR ITEMS
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(Name + " [ERROR] " + ex.ToString());
                    }

                    Server.GetServer().BroadcastFrom("AutoRestart", orange + "KICKING ALL PLAYERS...");
                    Logger.Log("KICKING ALL PLAYERS...");
                    foreach (var play in Server.GetServer().Players)
                    {
                        play.Disconnect();
                    }

                    
                    File.Copy(Directory.GetCurrentDirectory() + "\\save\\server_data\\rust_island_2013.sav", Directory.GetCurrentDirectory() + "\\save\\ShutDown\\rust_island_2013.savCOPIA ANTES DE ULTIMO REINICIO",true);
                }

                if (UnderRestarting == false)
                {
                    reloj.Dispose();
                }
            };
            reloj.Start();
        }
        public void RegisterShutDown(Fougerite.Player pl,string LogText)
        {
            if (!File.Exists(pathLog))
            {
                File.Create(pathLog).Dispose();
                StreamWriter WriteReportFile = File.AppendText(pathLog);
                WriteReportFile.WriteLine("Registry of Restarts");
                WriteReportFile.WriteLine(" ");
                WriteReportFile.Close();
            }
            StreamWriter WriteReportFile2 = File.AppendText(pathLog);
            WriteReportFile2.WriteLine("(" + System.DateTime.Now + ") (" + pl.Name + ") (REASON: " + LogText + ")");
            WriteReportFile2.Close();

            return;
        }
    }
}
