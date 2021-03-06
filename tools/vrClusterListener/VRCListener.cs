﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.IO;

//using System.Threading;
using System.Collections.Generic;

namespace UnrealTools
{
	class VRCListener
	{
		const int MessageMaxLength = 2048;

		const string CmdStart  = "start";
		const string CmdKill   = "kill";
		const string CmdStatus = "status";

		const int DefaultPort = 41000;

		static HashSet<int> ProcIDs     = new HashSet<int>();
		static int          LastProcID  = -1;

		// The listener will kill all names listed below each time it receives KILL command. This is
		// similar to black list. Just enumerate your enemies.
		static List<string> ProcessesToAlwaysKill = new List<string> {
			//"UnrealEngine.exe"
		};

		static void Main(string[] args)
		{
			Console.SetWindowSize(120, 24);
			Console.SetBufferSize(120, 240);

			TcpListener server = null;
			try
			{
				server = new TcpListener(IPAddress.Any, DefaultPort);
				server.Start();

				PrintColorText("Pixela Labs LLC, 2018", ConsoleColor.Cyan);
				PrintColorText(string.Format("VRCListener is listening to port {0}", DefaultPort.ToString()), ConsoleColor.Cyan);
				Console.WriteLine("---------------------------------------------------------");

				// Processing commands
				while (true)
				{
					TcpClient client = server.AcceptTcpClient();
					ProcessingRequest(client);
					client.Close();

					Console.WriteLine("---------------------------------------------------------\n\n");
				}
			}
			catch (SocketException e)
			{
				Console.WriteLine("SocketException: {0}", e);
			}
			finally
			{
				if(server != null)
					server.Stop();
			}
		}

		private static void PrintColorText(string message, ConsoleColor foregroundColor)
		{
			PrintColorText(message, foregroundColor, Console.BackgroundColor);
		}

		private static void PrintColorText(string message, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
		{
			ConsoleColor oldClrF = Console.ForegroundColor;
			ConsoleColor oldClrB = Console.BackgroundColor;

			Console.ForegroundColor = foregroundColor;
			Console.BackgroundColor = backgroundColor;

			Console.WriteLine(message);

			Console.ForegroundColor = oldClrF;
			Console.BackgroundColor = oldClrB;
		}

		private static void ProcessingRequest(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
			Console.WriteLine("Client [{0}] connected.", clientIP);
			PrintColorText(string.Format("New message from [{0}]", clientIP), ConsoleColor.Green);

			// We don't expect to receive too long strings
			if (stream.Length > MessageMaxLength)
			{
				PrintColorText(string.Format("Incoming message is too long. Maximum allowed length is {0}. Dropping the message.", MessageMaxLength), ConsoleColor.Red);
				return;
			}

			Byte[] bytes = new Byte[stream.Length];
			if(stream.Read(bytes, 0, (int)stream.Length) != stream.Length)
			{
				PrintColorText("An internal error has occurred", ConsoleColor.Red);
				return;
			}

			string cmd = Encoding.ASCII.GetString(bytes, 0, (int)stream.Length);
			ParseData(cmd);
		}

		private static void ParseData(string data)
		{
			data = data.Trim();
			if (string.IsNullOrEmpty(data))
			{
				PrintColorText("Empty message received.", ConsoleColor.Yellow);
				return;
			}

			Console.WriteLine("Received command: [{0}]", data);

			if (data.StartsWith(CmdStart, StringComparison.OrdinalIgnoreCase))
			{
				data = data.Substring(CmdStart.Length);
				StartApplication(data);
			}
			else if (data.StartsWith(CmdKill, StringComparison.OrdinalIgnoreCase))
			{
				KillAll();
			}
			else if (data.StartsWith(CmdStatus, StringComparison.OrdinalIgnoreCase))
			{
				PrintStatus();
			}
			else
			{
				PrintColorText("Unknown command", ConsoleColor.Red);
			}
		}

		private static string ExtractApplicationPath(string data)
		{
			if (string.IsNullOrWhiteSpace(data))
				return string.Empty;

			data = data.Trim();

			int idxStart = 0;
			int idxEnd = 0;

			if (data.StartsWith("\"") && data.Length > 2)
			{
				idxEnd = data.IndexOf("\"", 1);
			}
			else
			{
				idxEnd = data.IndexOf(" ", 1);
			}

			if (idxEnd > 0)
				return data.Substring(idxStart, idxEnd - idxStart + 1);
			else
				return string.Empty;

		}

		private static void StartApplication(string data)
		{
			data = data.Trim();
			if (data == String.Empty)
			{
				PrintColorText("Application path hasn't been specified.", ConsoleColor.Red);
				return;
			}

			try
			{
				string appPath = ExtractApplicationPath(data);
				string argList = data.Substring(appPath.Length).Trim();

				// For now we just forward arguments list as is.
				Process proc = new Process();
				proc.StartInfo.FileName = appPath;
				proc.StartInfo.Arguments = argList;
				proc.Start();

				ProcIDs.Add(proc.Id);
				LastProcID = proc.Id;

				PrintColorText(string.Format("Process started: {0} | {1}" + proc.Id, proc.ProcessName), ConsoleColor.White);
			}
			catch (Exception e)
			{
				PrintColorText(string.Format("Couldn't start application. {0}", e.ToString()), ConsoleColor.Red);
			}
		}

		private static void KillAll()
		{
			// Kill processes from 'black list'
			if (ProcessesToAlwaysKill.Count > 0)
			{
				foreach (string name in ProcessesToAlwaysKill)
					KillProcessesByName(name);
			}

			// Check if we start any process before
			if (LastProcID <= 0)
				return;

			// Always try to Kill the latest process
			KillProcessByPID(LastProcID);

			foreach (int pid in ProcIDs)
				KillProcessByPID(pid);

		}

		private static void KillProcessByPID(int PID)
		{
			try
			{
				Process proc = Process.GetProcessById(PID);
				proc.Kill();
				PrintColorText("Killed " + proc.ProcessName, ConsoleColor.White);
			}
			catch
			{
				// Enclose any exceptions here
			}
		}

		private static void KillProcessesByName(string procName)
		{
			Process[] processes = Process.GetProcessesByName(procName);
			foreach (Process proc in processes)
			{
				try
				{
					proc.Kill();
					PrintColorText("Killed " + proc.ProcessName, ConsoleColor.White);
				}
				catch
				{
					// Enclose any exceptions here
				}
			}
		}

		private static void PrintStatus()
		{
			//@todo: We need to send status report in response to 'status' command.
			PrintColorText("STATUS command has not been implemented yet.", ConsoleColor.Magenta);
		}
	}
}
