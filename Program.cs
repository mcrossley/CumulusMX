﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.ServiceProcess;
using System.IO;
using System.Threading.Tasks;

//#error version

namespace CumulusMX
{
	internal class Program
	{
		public static Cumulus cumulus;
		public static bool exitSystem = false;
		public static bool service = false;
		public static TextWriterTraceListener svcTextListener;
		const string AppGuid = "57190d2e-7e45-4efb-8c09-06a176cef3f3";
		public static Mutex appMutex;
		public static DateTime StartTime;
		public static byte[] InstanceId;

		public static int httpport = 8998;
		public static bool debug = false;

		private static async Task Main(string[] args)
		{
			StartTime = DateTime.Now;
			var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			if (windows)
			{
				// set the working path to the exe location
				Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			}

			try
			{
				if (!Directory.Exists("MXdiags"))
				{
					Directory.CreateDirectory("MXdiags");
				}
			}
			catch (UnauthorizedAccessException)
			{
				Console.WriteLine("Error, no permission to read/create folder /MXdiags");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error while attempting to read/create folder /MXdiags, error message: {ex.Message}");
			}


			var logfile = "MXdiags" + Path.DirectorySeparatorChar + "ServiceConsoleLog.txt";
			var logfileOld = "MXdiags" + Path.DirectorySeparatorChar + "ServiceConsoleLog-Old.txt";
			try
			{
				if (File.Exists(logfileOld))
					File.Delete(logfileOld);

				if (File.Exists(logfile))
					File.Move(logfile, logfileOld);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed to roll-over the Console log file: " + ex.Message);
			}

			svcTextListener = new TextWriterTraceListener(logfile);
			svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Starting on " + (windows ? "Windows" : "Linux"));
			svcTextListener.Flush();

			// Add an exit handler
			AppDomain.CurrentDomain.ProcessExit += (s, e) =>
			{
				if (cumulus != null)
				{
					Cumulus.LogConsoleMessage("Cumulus terminating", ConsoleColor.Red);
					Cumulus.LogMessage("Cumulus terminating");
					cumulus.Stop();
					svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Cumulus has shutdown");
					svcTextListener.Flush();
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Cumulus stopped");
					Console.ResetColor();
					exitSystem = true;
				}
				else
				{
					Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Cumulus terminating");
					svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Cumulus terminating");
					Console.WriteLine("Cumulus has not finished initialising, a clean exit is not possible, forcing exit");
					svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Cumulus has not finished initialising, a clean exit is not possible, forcing exit");
				}
			};


			// Now we need to catch the console Ctrl-C
			Console.CancelKeyPress += (s, ev) =>
			{
				if (cumulus != null)
				{
					Cumulus.LogConsoleMessage("Ctrl+C pressed", ConsoleColor.Red);
					Cumulus.LogMessage("Ctrl + C pressed");
				}
				ev.Cancel = true;
				exitSystem = true;
			};

			debug = true;
#if DEBUG
			debug = true;
			//Debugger.Launch();
#endif

			// Create a unique instance ID if it does not exist
			CheckInstanceId();

			for (int i = 0; i < args.Length; i++)
			{
				try
				{
					switch (args[i])
					{
						case "-lang" when args.Length >= i:
						{
							var lang = args[++i];

							CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(lang);
							CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(lang);
							break;
						}
						case "-port" when args.Length >= i:
							httpport = Convert.ToInt32(args[++i]);
							break;
						case "-debug":
							// Switch on debug and data logging from the start
							debug = true;
							break;
						case "-wsport":
							i++;
							Console.WriteLine("The use of the -wsport command line parameter is deprecated");
							svcTextListener.WriteLine("The use of the -wsport command line parameter is deprecated");
							break;
						case "-install" when windows:
						{
							if (SelfInstaller.InstallWin())
							{
								Console.WriteLine("Cumulus MX is now installed to run as service");
								Environment.Exit(0);
							}
							else
							{
								Console.WriteLine("Cumulus MX failed to install as service");
								Environment.Exit(1);
							}

							break;
						}
						case "-install":
							if (SelfInstaller.InstallLinux())
							{
								Console.WriteLine("Cumulus MX is now installed to run as service");
								Environment.Exit(0);
							}
							else
							{
								Console.WriteLine("Cumulus MX failed to install as service");
								Environment.Exit(1);
							}

							break;
						case "-uninstall" when windows:
						{
							if (SelfInstaller.UninstallWin())
							{
								Console.WriteLine("Cumulus MX is no longer installed to run as service");
								Environment.Exit(0);
							}
							else
							{
								Console.WriteLine("Cumulus MX failed uninstall itself as service");
								Environment.Exit(1);
							}

							break;
						}
						case "-uninstall":
							if (SelfInstaller.UninstallLinux())
							{
								Console.WriteLine("Cumulus MX is no longer installed to run as service");
								Environment.Exit(0);
							}
							else
							{
								Console.WriteLine("Cumulus MX failed uninstall itself as service");
								Environment.Exit(1);
							}

							break;
						case "-service":
							service = true;
							break;
						default:
							Console.WriteLine($"Invalid command line argument \"{args[i]}\"");
							svcTextListener.WriteLine($"Invalid command line argument \"{args[i]}\"");
							Usage();
							break;
					}
				}
				catch
				{
					Usage();
				}
			}

			using (appMutex = new Mutex(false, "Global\\" + AppGuid))
			{
				// Interactive seems to be always false under mono :(

				if (windows && !Environment.UserInteractive)
				{
					// Windows and not interactive - must be a service
					svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Running as a Windows service");
					svcTextListener.Flush();
					service = true;
					// Launch as a Windows Service
					ServiceBase.Run(new CumulusService());
				}
				else
				{
					if (Environment.UserInteractive || (!windows && !service))
					{
						// Windows interactive or Linux and no service flag
						svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Running interactively");
						service = false;
					}
					else
					{
						// Must be a Linux service
						svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Running as a Linux service");
						service = true;
					}
					svcTextListener.Flush();
					// Launch normally - Linux Service runs like this too
					RunAsAConsole(httpport, debug);
				}

				while (!exitSystem)
				{
					await Task.Delay(500);
				}

				try
				{
					if (appMutex.WaitOne(0, false))
					{
						appMutex.ReleaseMutex();
					}
				}
				catch {}
			}
		}

		private static void Usage()
		{
			Console.WriteLine();
			Console.WriteLine("Valid arguments are:");
			Console.WriteLine(" -port <http_portnum> - Sets the HTTP port Cumulus will use (default 8998)");
			Console.WriteLine(" -lang <culture_name> - Sets the Language Cumulus will use (defaults to current user language)");
			Console.WriteLine(" -debug               - Switches on debug and data logging from Cumulus start");
			Console.WriteLine(" -install             - Installs Cumulus as a system service (Windows only)");
			Console.WriteLine(" -uninstall           - Removes Cumulus as a system service (Windows only)");
			Console.WriteLine(" -service             - Must be used when running as a mono-service (Linux only)");
			Console.WriteLine("\nCumulus terminating");
			Environment.Exit(1);
		}

		private static void RunAsAConsole(int port, bool debug)
		{
			//Console.WriteLine("Current culture: " + CultureInfo.CurrentCulture.DisplayName);
			/*
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Creating Windows Exit Handler");
				svcTextListener.Flush();

				_ = new ExitHandler();
			}
			*/

			cumulus = new Cumulus();

			cumulus.Initialise(port, debug, "");

			if (!exitSystem)
			{
				Console.WriteLine(DateTime.Now.ToString("G"));
				Console.WriteLine("Type Ctrl-C to terminate");
			}
		}

		private static async void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				Trace.WriteLine("!!! Unhandled Exception !!!");
				Trace.WriteLine(e.ExceptionObject.ToString());

				if (service)
				{
					svcTextListener.WriteLine(e.ExceptionObject.ToString());
					svcTextListener.WriteLine("**** An error has occurred - please zip up the MXdiags folder and post it in the forum ****");
					svcTextListener.Flush();
				}
				else
				{
					Console.WriteLine(e.ExceptionObject.ToString());
					Console.WriteLine("**** An error has occurred - please zip up the MXdiags folder and post it in the forum ****");
					Console.WriteLine("Press Enter to terminate");
					Console.ReadLine();
				}
				await Task.Delay(1000);
				Environment.Exit(1);
			}
			catch (Exception)
			{
			}
		}

		private static async void CheckInstanceId()
		{
			// check if instance file exists, if it exists, read the contents
			if (File.Exists("UniqueId.txt"))
			{
				string txt;
				using (var sr = File.OpenText("UniqueId.txt"))
					txt = await sr.ReadLineAsync();

				InstanceId = Convert.FromBase64String(txt);
				// Check the length, and ends in "="
				if (txt.Length < 30 || txt[^1] != '=')
				{
					var msg = "Error: Your UniqueId.txt file appears to be corrupt, please restore it from a backup.";
					Console.WriteLine(msg);
					svcTextListener.WriteLine(msg);
					svcTextListener.Flush();
				}
			}
			else
			{
				// otherwise, create it with a newly generated id
				InstanceId = Crypto.GenerateKey();
				await File.WriteAllTextAsync("UniqueId.txt", Convert.ToBase64String(InstanceId));
			}

		}
	}
}