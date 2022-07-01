using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using EmbedIO;
using ServiceStack.Text;

namespace CumulusMX
{
	public class ProgramSettings
	{
		private readonly Cumulus cumulus;

		public ProgramSettings(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		public string GetAlpacaFormData()
		{
			// Build the settings data, convert to JSON, and return it
			var startup = new StartupOptionsJson()
			{
				startuphostping = cumulus.ProgramOptions.StartupPingHost,
				startuppingescape = cumulus.ProgramOptions.StartupPingEscapeTime,
				startupdelay = cumulus.ProgramOptions.StartupDelaySecs,
				startupdelaymaxuptime = cumulus.ProgramOptions.StartupDelayMaxUptime
			};

			var logging = new LoggingOptionsJson()
			{
				debuglogging = cumulus.ProgramOptions.DebugLogging,
				datalogging = cumulus.ProgramOptions.DataLogging,
				ftplogging = cumulus.FtpOptions.Logging,
				emaillogging = cumulus.SmtpOptions.Logging,
				spikelogging = cumulus.ErrorLogSpikeRemoval,
				rawextralogging = cumulus.ProgramOptions.LogRawExtraData,
				rawstationlogging = cumulus.ProgramOptions.LogRawStationData
			};

			var options = new GeneralOptionsJson()
			{
				stopsecondinstance = cumulus.ProgramOptions.WarnMultiple,
				listwebtags = cumulus.ProgramOptions.ListWebTags,
				displaypasswords = cumulus.ProgramOptions.DisplayPasswords
			};

			var culture = new CultureOptionsJson()
			{
				removespacefromdateseparator = cumulus.ProgramOptions.Culture.RemoveSpaceFromDateSeparator
			};

			var settings = new SettingsJson()
			{
				accessible = cumulus.ProgramOptions.EnableAccessibility,
				startup = startup,
				logging = logging,
				options = options,
				culture = culture
			};

			return JsonSerializer.SerializeToString(settings);
		}

		public string UpdateConfig(IHttpContext context)
		{
			var errorMsg = "";
			var json = "";
			SettingsJson settings;
			context.Response.StatusCode = 200;

			// get the response
			try
			{
				Cumulus.LogMessage("Updating Program settings");

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
				cumulus.ProgramOptions.StartupPingHost = settings.startup.startuphostping;
				cumulus.ProgramOptions.StartupPingEscapeTime = settings.startup.startuppingescape;
				cumulus.ProgramOptions.StartupDelaySecs = settings.startup.startupdelay;
				cumulus.ProgramOptions.StartupDelayMaxUptime = settings.startup.startupdelaymaxuptime;
				cumulus.ProgramOptions.DebugLogging = settings.logging.debuglogging;
				cumulus.ProgramOptions.DataLogging = settings.logging.datalogging;
				cumulus.SmtpOptions.Logging = settings.logging.emaillogging;
				cumulus.ErrorLogSpikeRemoval = settings.logging.spikelogging;
				cumulus.ProgramOptions.WarnMultiple = settings.options.stopsecondinstance;
				cumulus.ProgramOptions.ListWebTags = settings.options.listwebtags;
				cumulus.ProgramOptions.DisplayPasswords = settings.options.displaypasswords;

				cumulus.ProgramOptions.Culture.RemoveSpaceFromDateSeparator = settings.culture.removespacefromdateseparator;

				if (cumulus.ProgramOptions.Culture.RemoveSpaceFromDateSeparator && CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator.Contains(' '))
				{
					// get the existing culture
					var newCulture = CultureInfo.CurrentCulture;
					// change the date separator
					newCulture.DateTimeFormat.DateSeparator = CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator.Replace(" ", "");
					// set current thread culture
					Thread.CurrentThread.CurrentCulture = newCulture;
					// set the default culture for other threads
					CultureInfo.DefaultThreadCurrentCulture = newCulture;
				}
				else
				{
					var newCulture = CultureInfo.GetCultureInfo(CultureInfo.CurrentCulture.Name);

					if (!cumulus.ProgramOptions.Culture.RemoveSpaceFromDateSeparator && newCulture.DateTimeFormat.DateSeparator.Contains(' ') && !CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator.Contains(' '))
					{
						// set current thread culture
						Thread.CurrentThread.CurrentCulture = newCulture;
						// set the default culture for other threads
						CultureInfo.DefaultThreadCurrentCulture = newCulture;
					}
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

			return context.Response.StatusCode == 200 ? "success" : errorMsg;
		}

		private class SettingsJson
		{
			public bool accessible { get; set; }
			public StartupOptionsJson startup { get; set; }
			public LoggingOptionsJson logging { get; set; }
			public GeneralOptionsJson options { get; set; }
			public CultureOptionsJson culture { get; set; }
		}

		private class StartupOptionsJson
		{
			public string startuphostping { get; set; }
			public int startuppingescape { get; set; }
			public int startupdelay { get; set; }
			public int startupdelaymaxuptime { get; set; }
		}

		private class LoggingOptionsJson
		{
			public bool debuglogging { get; set; }
			public bool datalogging { get; set; }
			public bool ftplogging { get; set; }
			public bool emaillogging { get; set; }
			public bool spikelogging { get; set; }
			public bool rawstationlogging { get; set; }
			public bool rawextralogging { get; set; }
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
		}

	}
}
