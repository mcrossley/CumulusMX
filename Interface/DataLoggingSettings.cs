using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using EmbedIO;
using ServiceStack.Text;


namespace CumulusMX
{
	internal class DataLoggingSettings
	{
		private readonly Cumulus cumulus;


		public DataLoggingSettings(Cumulus cuml)
		{
			cumulus = cuml;

		}

		public string GetAlpacaFormData()
		{
			// Build the settings data, convert to JSON, and return it
			var legacy = new LegacyLogsJson()
			{
				// the v3 log files
				mainstation = cumulus.StationOptions.LogMainStation,
				extrasensors = cumulus.StationOptions.LogExtraSensors,
			};

			var extrasensors = new ExtraSensorsJson()
			{
				// the extra sensors
				extratemp = cumulus.ExtraDataLogging.Temperature,
				extrahum = cumulus.ExtraDataLogging.Humidity,
				extradewpoint = cumulus.ExtraDataLogging.Dewpoint,
				extrausertemp = cumulus.ExtraDataLogging.UserTemp,
				extrasoiltemp = cumulus.ExtraDataLogging.SoilTemp,
				extrasoilmoist = cumulus.ExtraDataLogging.SoilMoisture,
				extraleaftemp = cumulus.ExtraDataLogging.LeafTemp,
				extraleafwet = cumulus.ExtraDataLogging.LeafWetness,
				extraairqual = cumulus.ExtraDataLogging.AirQual,
				extraco2 = cumulus.ExtraDataLogging.CO2
			};

			var settings = new SettingsJson()
			{
				legacylogs = legacy,
				extrasensors = extrasensors
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
				Cumulus.LogMessage("Updating Data Logging settings");

				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				json = WebUtility.UrlDecode(data[5..]);

				// de-serialize it to the settings structure
				settings = JsonSerializer.DeserializeFromString<SettingsJson>(json);
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing Data Logging Settings JSON";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Program Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}

			// process the settings
			try
			{
				// the v3 log files
				cumulus.StationOptions.LogMainStation = settings.legacylogs.mainstation;
				cumulus.StationOptions.LogExtraSensors = settings.legacylogs.extrasensors;
				// the extra sensors
				cumulus.ExtraDataLogging.Temperature = settings.extrasensors.extratemp;
				cumulus.ExtraDataLogging.Humidity = settings.extrasensors.extrahum;
				cumulus.ExtraDataLogging.Dewpoint = settings.extrasensors.extradewpoint;
				cumulus.ExtraDataLogging.UserTemp = settings.extrasensors.extrausertemp;
				cumulus.ExtraDataLogging.SoilTemp = settings.extrasensors.extrasoiltemp;
				cumulus.ExtraDataLogging.SoilMoisture = settings.extrasensors.extrasoilmoist;
				cumulus.ExtraDataLogging.LeafTemp = settings.extrasensors.extraleaftemp;
				cumulus.ExtraDataLogging.LeafWetness = settings.extrasensors.extraleafwet;
				cumulus.ExtraDataLogging.AirQual = settings.extrasensors.extraairqual;
				cumulus.ExtraDataLogging.CO2 = settings.extrasensors.extraco2;
			}
			catch (Exception ex)
			{
				var msg = "Error processing Data Logging Options";
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
			public LegacyLogsJson legacylogs { get; set; }
			public ExtraSensorsJson extrasensors { get; set; }
		}

		private class LegacyLogsJson
		{
			public bool mainstation { get; set; }
			public bool extrasensors { get; set; }
		}

		private class ExtraSensorsJson
		{
			public bool extratemp { get; set; }
			public bool extrahum { get; set; }
			public bool extradewpoint { get; set; }
			public bool extrausertemp { get; set; }
			public bool extrasoiltemp { get; set; }
			public bool extrasoilmoist { get; set; }
			public bool extraleaftemp { get; set; }
			public bool extraleafwet { get; set; }
			public bool extraairqual { get; set; }
			public bool extraco2 { get; set; }
		}
	}
}
