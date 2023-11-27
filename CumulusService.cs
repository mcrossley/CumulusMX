using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace CumulusMX
{
	partial class CumulusService : ServiceBase
	{
		public CumulusService()
		{
			InitializeComponent();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				CanShutdown = true;
				CanHandlePowerEvent = true;
				CanPauseAndContinue = false;
				CanShutdown = true;
			}
		}

		protected override void OnStart(string[] args)
		{
			int httpport = Program.httpport;
			bool debug = Program.debug;
			string startParams = "";

			for (int i = 0; i < args.Length; i++)
			{
				startParams += args[i] + " ";
				try
				{
					if (args[i] == "-lang" && args.Length >= i)
					{
						var lang = args[++i];
						startParams += args[i] + " ";

						CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(lang);
						CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(lang);
					}
					else if (args[i] == "-port" && args.Length >= i)
					{
						httpport = Convert.ToInt32(args[++i]);
						startParams += args[i] + " ";
					}
					else if (args[i] == "-debug")
					{
						// Switch on debug and data logging from the start
						debug = true;
					}
					else if (args[i] == "-wsport" && args.Length >= i)
					{
						i++;
						startParams += args[i] + " ";
					}
				}
				catch
				{ }
			}

			Program.cumulus = new Cumulus();
			Program.cumulus.Initialise(httpport, debug, startParams);
		}

		protected override void OnStop()
		{
			Program.cumulus.LogMessage("Shutting down due to SERVICE STOP");
			Cumulus.LogConsoleMessage("Shutting down due to SERVICE STOP");
			Program.cumulus.Stop();
			Program.exitSystem = true;
		}

		protected override void OnShutdown()
		{
			Program.cumulus.LogMessage("Shutting down due to SYSTEM SHUTDOWN");
			Cumulus.LogConsoleMessage("Shutting down due to SYSTEM SHUTDOWN");
			Program.cumulus.Stop();
			Program.exitSystem = true;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) base.OnShutdown();
		}


		protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return true;

			switch (powerStatus)
			{
				case PowerBroadcastStatus.BatteryLow:
					Program.cumulus.LogWarningMessage("POWER: Detected system BATTERY LOW");
					break;
				case PowerBroadcastStatus.OemEvent:
					Program.cumulus.LogWarningMessage("POWER: Detected system OEM EVENT");
					break;
				case PowerBroadcastStatus.PowerStatusChange:
					Program.cumulus.LogWarningMessage("POWER: Detected system POWER STATUS CHANGE");
					break;
				case PowerBroadcastStatus.QuerySuspend:
					Program.cumulus.LogWarningMessage("POWER: Detected system QUERY SUSPEND");
					break;
				case PowerBroadcastStatus.QuerySuspendFailed:
					Program.cumulus.LogWarningMessage("POWER: Detected system QUERY SUSPEND FAILED");
					break;
				case PowerBroadcastStatus.ResumeAutomatic:
					Program.cumulus.LogWarningMessage("POWER: Detected system RESUME AUTOMATIC");
					break;
				case PowerBroadcastStatus.ResumeCritical:
					Program.cumulus.LogCriticalMessage("POWER: Detected system RESUME CRITICAL, stopping service");
					Cumulus.LogConsoleMessage("Detected system RESUME CRITICAL, stopping service");
					// A critical suspend will not have shutdown Cumulus, so do it now
					Stop();
					Program.exitSystem = true;
					break;
				case PowerBroadcastStatus.ResumeSuspend:
					Program.cumulus.LogWarningMessage("POWER: Detected system RESUMING FROM STANDBY");
					break;
				case PowerBroadcastStatus.Suspend:
					Program.cumulus.LogWarningMessage("POWER: Detected system GOING TO STANDBY, stopping service");
					Cumulus.LogConsoleMessage("Detected system GOING TO STANDBY, stopping service");
					Stop();
					Program.exitSystem = true;
					break;
			}

			return true;
		}
	}
}
