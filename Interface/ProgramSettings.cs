﻿using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;

using EmbedIO;
using ServiceStack.Text;

namespace CumulusMX
{
	public class ProgramSettings(Cumulus cumulus)
	{
		private readonly Cumulus cumulus = cumulus;

		public string GetAlpacaFormData()
		{
			// Build the settings data, convert to JSON, and return it

			var startuptask = new ProgramTask()
			{
				task = cumulus.ProgramOptions.StartupTask,
				taskparams = cumulus.ProgramOptions.StartupTaskParams,
				wait = cumulus.ProgramOptions.StartupTaskWait
			};

			var startup = new StartupOptionsJson()
			{
				startuphostping = cumulus.ProgramOptions.StartupPingHost,
				startuppingescape = cumulus.ProgramOptions.StartupPingEscapeTime,
				startupdelay = cumulus.ProgramOptions.StartupDelaySecs,
				startupdelaymaxuptime = cumulus.ProgramOptions.StartupDelayMaxUptime,
				startuptask = startuptask
			};

			var shutdowntask = new ProgramTask()
			{
				task = cumulus.ProgramOptions.ShutdownTask,
				taskparams = cumulus.ProgramOptions.ShutdownTaskParams
			};

			var shutdown = new ShutdownOptions()
			{
				datastoppedexit = cumulus.ProgramOptions.DataStoppedExit,
				datastoppedmins = cumulus.ProgramOptions.DataStoppedMins,
				shutdowntask = shutdowntask
			};

			var logging = new LoggingOptionsJson()
			{
				debuglogging = cumulus.ProgramOptions.DebugLogging,
				datalogging = cumulus.ProgramOptions.DataLogging,
				ftplogging = cumulus.FtpOptions.Logging,
				ftplogginglevel = cumulus.FtpOptions.LoggingLevel,
				emaillogging = cumulus.SmtpOptions.Logging,
				spikelogging = cumulus.ErrorLogSpikeRemoval,
				rawextralogging = cumulus.ProgramOptions.LogRawExtraData,
				rawstationlogging = cumulus.ProgramOptions.LogRawStationData,
				errorlistlevel = (int) cumulus.ErrorListLoggingLevel
			};

			var options = new GeneralOptionsJson()
			{
				stopsecondinstance = cumulus.ProgramOptions.WarnMultiple,
				listwebtags = cumulus.ProgramOptions.ListWebTags,
				displaypasswords = cumulus.ProgramOptions.DisplayPasswords
			};

			var culture = new CultureOptionsJson()
			{
				removespacefromdateseparator = cumulus.ProgramOptions.Culture.RemoveSpaceFromDateSeparator,
				timeFormat = cumulus.ProgramOptions.TimeFormat
			};

			var security = new Security()
			{
				securesettings = cumulus.ProgramOptions.SecureSettings,
				username = cumulus.ProgramOptions.SettingsUsername,
				password = cumulus.ProgramOptions.SettingsPassword,
			};

			var settings = new SettingsJson()
			{
				accessible = cumulus.ProgramOptions.EnableAccessibility,
				startup = startup,
				shutdown = shutdown,
				logging = logging,
				options = options,
				culture = culture,
				security = security
			};

			return JsonSerializer.SerializeToString(settings);
		}

		public string UpdateConfig(IHttpContext context)
		{
			var errorMsg = "";
			var json = "";
			var returnMessage = "success";
			SettingsJson settings;
			context.Response.StatusCode = 200;

			// get the response
			try
			{
				cumulus.LogMessage("Updating Program settings");

				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				json = WebUtility.UrlDecode(data[5..]);

				// de-serialize it to the settings structure
				settings = JsonSerializer.DeserializeFromString<SettingsJson>(json);
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing Program Settings JSON";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Program Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}

			// process the settings
			try
			{
				cumulus.ProgramOptions.EnableAccessibility = settings.accessible;
				cumulus.ProgramOptions.StartupPingHost = (settings.startup.startuphostping ?? string.Empty).Trim();
				cumulus.ProgramOptions.StartupPingEscapeTime = settings.startup.startuppingescape;
				cumulus.ProgramOptions.StartupDelaySecs = settings.startup.startupdelay;
				cumulus.ProgramOptions.StartupDelayMaxUptime = settings.startup.startupdelaymaxuptime;

				cumulus.ProgramOptions.StartupTask = (settings.startup.startuptask.task ?? string.Empty).Trim();
				cumulus.ProgramOptions.StartupTaskParams = (settings.startup.startuptask.taskparams ?? string.Empty).Trim();
				cumulus.ProgramOptions.StartupTaskWait = settings.startup.startuptask.wait;

				cumulus.ProgramOptions.ShutdownTask = (settings.shutdown.shutdowntask.task ?? string.Empty).Trim();
				cumulus.ProgramOptions.ShutdownTaskParams = (settings.shutdown.shutdowntask.taskparams ?? string.Empty).Trim();

				cumulus.ProgramOptions.DataStoppedExit = settings.shutdown.datastoppedexit;
				cumulus.ProgramOptions.DataStoppedMins = settings.shutdown.datastoppedmins;

				cumulus.ProgramOptions.DebugLogging = settings.logging.debuglogging;
				cumulus.ProgramOptions.DataLogging = settings.logging.datalogging;
				cumulus.SmtpOptions.Logging = settings.logging.emaillogging;
				cumulus.ErrorLogSpikeRemoval = settings.logging.spikelogging;
				cumulus.ErrorListLoggingLevel = (Microsoft.Extensions.Logging.LogLevel) settings.logging.errorlistlevel;

				cumulus.ProgramOptions.WarnMultiple = settings.options.stopsecondinstance;
				cumulus.ProgramOptions.ListWebTags = settings.options.listwebtags;
				cumulus.ProgramOptions.TimeFormat = settings.culture.timeFormat;
				cumulus.ProgramOptions.DisplayPasswords = settings.options.displaypasswords;

				// Does the culture need to be tweaked - either way
				if (cumulus.ProgramOptions.Culture.RemoveSpaceFromDateSeparator != settings.culture.removespacefromdateseparator)
				{
					cumulus.ProgramOptions.Culture.RemoveSpaceFromDateSeparator = settings.culture.removespacefromdateseparator;
					returnMessage = "You must restart Cumulus for the Locale setting change to take effect";
				}

				cumulus.ProgramOptions.SecureSettings = settings.security.securesettings;
				cumulus.ProgramOptions.SettingsUsername = (settings.security.username ?? string.Empty).Trim();
				cumulus.ProgramOptions.SettingsPassword = (settings.security.password ?? string.Empty).Trim();

				if (cumulus.ProgramOptions.TimeFormat == "t")
					cumulus.ProgramOptions.TimeFormatLong = "T";
				else if (cumulus.ProgramOptions.TimeFormat == "h:mm tt")
					cumulus.ProgramOptions.TimeFormatLong = "h:mm:ss tt";
				else
					cumulus.ProgramOptions.TimeFormatLong = "HH:mm:ss";

				if (settings.logging.ftplogginglevel.HasValue && settings.logging.ftplogginglevel != cumulus.FtpOptions.LoggingLevel)
				{
					cumulus.FtpOptions.LoggingLevel = settings.logging.ftplogginglevel.Value;
					cumulus.SetupFtpLogging();
					cumulus.SetFtpLogging(settings.logging.ftplogging);
				}

				if (settings.logging.ftplogging != cumulus.FtpOptions.Logging)
				{
					cumulus.FtpOptions.Logging = settings.logging.ftplogging;
					cumulus.SetFtpLogging(cumulus.FtpOptions.Logging);
				}

				cumulus.ProgramOptions.LogRawExtraData = settings.logging.rawextralogging;
				cumulus.ProgramOptions.LogRawStationData = settings.logging.rawstationlogging;

				if (settings.logging.rawextralogging || settings.logging.rawstationlogging)
				{
					cumulus.RollOverDataLogs();
				}
				if (!settings.logging.rawextralogging && cumulus.RawDataExtraLog != null)
				{
					cumulus.RawDataExtraLog.Dispose();
				}
				if (!settings.logging.rawstationlogging && cumulus.RawDataStation != null)
				{
					cumulus.RawDataStation.Dispose();
				}
			}
			catch (Exception ex)
			{
				var msg = "Error processing Program Options";
				cumulus.LogExceptionMessage(ex, msg);
				errorMsg += msg + "\n\n";
				context.Response.StatusCode = 500;
			}

			// Save the settings
			cumulus.WriteIniFile();

			return context.Response.StatusCode == 200 ? returnMessage : errorMsg;
		}

		private class SettingsJson
		{
			public bool accessible { get; set; }
			public StartupOptionsJson startup { get; set; }
			public ShutdownOptions shutdown { get; set; }
			public LoggingOptionsJson logging { get; set; }
			public GeneralOptionsJson options { get; set; }
			public CultureOptionsJson culture { get; set; }
			public Security security { get; set; }
		}

		public class ProgramTask
		{
			public string task { get; set; }
			public string taskparams { get; set; }
			public bool wait { get; set; }
		}

		private class StartupOptionsJson
		{
			public string startuphostping { get; set; }
			public int startuppingescape { get; set; }
			public int startupdelay { get; set; }
			public int startupdelaymaxuptime { get; set; }
			public ProgramTask startuptask { get; set; }

		}

		private class LoggingOptionsJson
		{
			public bool debuglogging { get; set; }
			public bool datalogging { get; set; }
			public bool ftplogging { get; set; }
			public int? ftplogginglevel { get; set; }
			public bool emaillogging { get; set; }
			public bool spikelogging { get; set; }
			public bool rawstationlogging { get; set; }
			public bool rawextralogging { get; set; }
			public int errorlistlevel { get; set; }
		}

		private class GeneralOptionsJson
		{
			public bool stopsecondinstance { get; set; }
			public bool listwebtags { get; set; }
			public bool displaypasswords { get; set; }
		}

		private class CultureOptionsJson
		{
			public bool removespacefromdateseparator { get; set; }
			public string timeFormat { get; set; }
		}

		public class ShutdownOptions
		{
			public bool datastoppedexit { get; set; }
			public int datastoppedmins { get; set; }
			public ProgramTask shutdowntask { get; set; }
		}
		private class Security
		{
			public bool securesettings { get; set;}
			public string username { get; set; }
			public string password { get; set; }
		}
	}
}
