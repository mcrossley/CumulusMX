﻿using System;
using System.IO;
using System.Net;
using EmbedIO;
using ServiceStack;

namespace CumulusMX
{
	public class ExtraSensorSettings
	{
		private readonly Cumulus cumulus;

		public ExtraSensorSettings(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		public string GetAlpacaFormData()
		{
			var indoor = new AirLinkDeviceJson()
			{
				enabled = cumulus.AirLinkInEnabled,
				ipAddress = cumulus.AirLinkInIPAddr,
				hostname = cumulus.AirLinkInHostName,
				stationId = cumulus.AirLinkInStationId
			};

			var outdoor = new AirLinkDeviceJson()
			{
				enabled = cumulus.AirLinkOutEnabled,
				ipAddress = cumulus.AirLinkOutIPAddr,
				hostname = cumulus.AirLinkOutHostName,
				stationId = cumulus.AirLinkOutStationId
			};

			var airlink = new AirLinkSettingsJson()
			{
				isNode = cumulus.AirLinkIsNode,
				apiKey = cumulus.AirLinkApiKey,
				apiSecret = cumulus.AirLinkApiSecret,
				autoUpdateIp = cumulus.AirLinkAutoUpdateIpAddress,
				indoor = indoor,
				outdoor = outdoor
			};

			var ecowitt = new EcowittJson()
			{
				useSolar = cumulus.EcowittSettings.ExtraUseSolar,
				useUv = cumulus.EcowittSettings.ExtraUseUv,
				useTempHum = cumulus.EcowittSettings.ExtraUseTempHum,
				//useSoilTemp = cumulus.EcowittExtraUseSoilTemp,
				useSoilMoist = cumulus.EcowittSettings.ExtraUseSoilMoist,
				useLeafWet = cumulus.EcowittSettings.ExtraUseLeafWet,
				useUserTemp = cumulus.EcowittSettings.ExtraUseUserTemp,
				useAQI = cumulus.EcowittSettings.ExtraUseAQI,
				useCo2 = cumulus.EcowittSettings.ExtraUseCo2,
				useLightning = cumulus.EcowittSettings.ExtraUseLightning,
				useLeak = cumulus.EcowittSettings.ExtraUseLeak,

				setcustom = cumulus.EcowittSettings.ExtraSetCustomServer,
				gwaddr = cumulus.EcowittSettings.ExtraGatewayAddr,
				localaddr = cumulus.EcowittSettings.ExtraLocalAddr,
				interval = cumulus.EcowittSettings.ExtraCustomInterval,
				primaryTHsensor = cumulus.Gw1000PrimaryTHSensor
			};

			var ambient = new AmbientJson()
			{
				useSolar = cumulus.AmbientExtraUseSolar,
				useUv = cumulus.AmbientExtraUseUv,
				useTempHum = cumulus.AmbientExtraUseTempHum,
				//useSoilTemp = cumulus.AmbientExtraUseSoilTemp,
				useSoilMoist = cumulus.AmbientExtraUseSoilMoist,
				//useLeafWet = cumulus.AmbientExtraUseLeafWet,
				useAQI = cumulus.AmbientExtraUseAQI,
				useCo2 = cumulus.AmbientExtraUseCo2,
				useLightning = cumulus.AmbientExtraUseLightning,
				useLeak = cumulus.AmbientExtraUseLeak
			};

			var httpStation = new HttpStationsJson()
			{
				ecowitt = ecowitt,
				ambient = ambient
			};

			if (cumulus.EcowittSettings.ExtraEnabled)
				httpStation.extraStation = 0;
			else if (cumulus.AmbientExtraEnabled)
				httpStation.extraStation = 1;
			else
				httpStation.extraStation = -1;


			var bl = new BlakeLarsenJson()
			{
				enabled = cumulus.SolarOptions.UseBlakeLarsen
			};

			var rg11port1 = new RG11devicejson()
			{
				enabled = cumulus.RG11Enabled,
				commPort = cumulus.RG11Port,
				tipMode = cumulus.RG11TBRmode,
				tipSize = cumulus.RG11tipsize,
				dtrMode = cumulus.RG11TBRmode
			};

			var rg11port2 = new RG11devicejson()
			{
				enabled = cumulus.RG11Enabled2,
				commPort = cumulus.RG11Port2,
				tipMode = cumulus.RG11TBRmode2,
				tipSize = cumulus.RG11tipsize2,
				dtrMode = cumulus.RG11TBRmode2
			};

			var rg11 = new RG11Json()
			{
				port1 = rg11port1,
				port2 = rg11port2
			};

			var aq = new AirQualityJson()
			{
				primaryaqsensor = cumulus.StationOptions.PrimaryAqSensor,
				aqi = cumulus.airQualityIndex,
			};

			var data = new SettingsJson()
			{
				accessible = cumulus.ProgramOptions.EnableAccessibility,
				airquality = aq,
				airLink = airlink,
				httpSensors = httpStation,
				blakeLarsen = bl,
				rg11 = rg11
			};

			return data.ToJson();
		}

		public string UpdateConfig(IHttpContext context)
		{
			var errorMsg = "";
			var json = "";
			SettingsJson settings;
			context.Response.StatusCode = 200;

			try
			{
				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				json = WebUtility.UrlDecode(data[5..]);

				// de-serialize it to the settings structure
				settings = json.FromJson<SettingsJson>();
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing ExtraSensor Settings JSON";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("ExtraSensor Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}

			// process the settings
			try
			{
				Cumulus.LogMessage("Updating extra sensor settings");

				// General settings
				try
				{
					cumulus.StationOptions.PrimaryAqSensor = settings.airquality.primaryaqsensor;
					cumulus.airQualityIndex = settings.airquality.aqi;
				}
				catch (Exception ex)
				{
					var msg = "Error processing General settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// AirLink settings
				try
				{
					cumulus.AirLinkIsNode = settings.airLink.isNode;
					cumulus.AirLinkApiKey = settings.airLink.apiKey;
					cumulus.AirLinkApiSecret = settings.airLink.apiSecret;
					cumulus.AirLinkAutoUpdateIpAddress = settings.airLink.autoUpdateIp;

					cumulus.AirLinkInEnabled = settings.airLink.indoor.enabled;
					if (cumulus.AirLinkInEnabled)
					{
						cumulus.AirLinkInIPAddr = settings.airLink.indoor.ipAddress;
						cumulus.AirLinkInHostName = settings.airLink.indoor.hostname;
						cumulus.AirLinkInStationId = settings.airLink.indoor.stationId;
						if (cumulus.AirLinkInStationId < 10 && cumulus.AirLinkIsNode)
						{
							cumulus.AirLinkInStationId = cumulus.WllStationId;
						}
					}
					cumulus.AirLinkOutEnabled = settings.airLink.outdoor.enabled;
					if (cumulus.AirLinkOutEnabled)
					{
						cumulus.AirLinkOutIPAddr = settings.airLink.outdoor.ipAddress;
						cumulus.AirLinkOutHostName = settings.airLink.outdoor.hostname;
						cumulus.AirLinkOutStationId = settings.airLink.outdoor.stationId;
						if (cumulus.AirLinkOutStationId < 10 && cumulus.AirLinkIsNode)
						{
							cumulus.AirLinkOutStationId = cumulus.WllStationId;
						}
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing AirLink settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Ecowitt Extra settings
				try
				{
					if (settings.httpSensors.extraStation == 0)
					{
						cumulus.EcowittSettings.ExtraEnabled = true;
						cumulus.EcowittSettings.ExtraUseSolar = settings.httpSensors.ecowitt.useSolar;
						cumulus.EcowittSettings.ExtraUseUv = settings.httpSensors.ecowitt.useUv;
						cumulus.EcowittSettings.ExtraUseTempHum = settings.httpSensors.ecowitt.useTempHum;
						cumulus.EcowittSettings.ExtraUseSoilMoist = settings.httpSensors.ecowitt.useSoilMoist;
						cumulus.EcowittSettings.ExtraUseLeafWet = settings.httpSensors.ecowitt.useLeafWet;
						cumulus.EcowittSettings.ExtraUseUserTemp = settings.httpSensors.ecowitt.useUserTemp;
						cumulus.EcowittSettings.ExtraUseAQI = settings.httpSensors.ecowitt.useAQI;
						cumulus.EcowittSettings.ExtraUseCo2 = settings.httpSensors.ecowitt.useCo2;
						cumulus.EcowittSettings.ExtraUseLightning = settings.httpSensors.ecowitt.useLightning;
						cumulus.EcowittSettings.ExtraUseLeak = settings.httpSensors.ecowitt.useLeak;

						cumulus.EcowittSettings.ExtraSetCustomServer = settings.httpSensors.ecowitt.setcustom;
						cumulus.EcowittSettings.ExtraGatewayAddr = settings.httpSensors.ecowitt.gwaddr;
						cumulus.EcowittSettings.ExtraLocalAddr = settings.httpSensors.ecowitt.localaddr;
						cumulus.EcowittSettings.ExtraCustomInterval = settings.httpSensors.ecowitt.interval;
						cumulus.Gw1000PrimaryTHSensor = settings.httpSensors.ecowitt.primaryTHsensor;

						// Also enable extra logging if applicable
						if (cumulus.EcowittSettings.ExtraUseTempHum || cumulus.EcowittSettings.ExtraUseSoilTemp || cumulus.EcowittSettings.ExtraUseSoilMoist || cumulus.EcowittSettings.ExtraUseLeafWet || cumulus.EcowittSettings.ExtraUseUserTemp || cumulus.EcowittSettings.ExtraUseAQI || cumulus.EcowittSettings.ExtraUseCo2)
						{
							cumulus.StationOptions.LogExtraSensors = true;
						}
						if (cumulus.EcowittSettings.ExtraUseTempHum)
						{
							cumulus.ExtraDataLogging.Temperature = true;
							cumulus.ExtraDataLogging.Humidity = true;
							cumulus.ExtraDataLogging.Dewpoint = true;
						}
						if (cumulus.EcowittSettings.ExtraUseSoilTemp)
						{
							cumulus.ExtraDataLogging.SoilTemp = true;
						}
						if (cumulus.EcowittSettings.ExtraUseSoilMoist)
						{
							cumulus.ExtraDataLogging.SoilMoisture = true;
						}
						if (cumulus.EcowittSettings.ExtraUseLeafWet)
						{
							cumulus.ExtraDataLogging.LeafWetness = true;
						}
						if (cumulus.EcowittSettings.ExtraUseUserTemp)
						{
							cumulus.ExtraDataLogging.UserTemp = true;
						}
						if (cumulus.EcowittSettings.ExtraUseAQI)
						{
							cumulus.ExtraDataLogging.AirQual = true;
						}
						if (cumulus.EcowittSettings.ExtraUseCo2)
						{
							cumulus.ExtraDataLogging.CO2 = true;
						}
					}
					else
						cumulus.EcowittSettings.ExtraEnabled = false;
				}
				catch (Exception ex)
				{
					var msg = "Error processing Ecowitt settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Ambient Extra settings
				try
				{
					if (settings.httpSensors.extraStation == 1)
					{
						cumulus.AmbientExtraEnabled = true;
						cumulus.AmbientExtraUseSolar = settings.httpSensors.ambient.useSolar;
						cumulus.AmbientExtraUseUv = settings.httpSensors.ambient.useUv;
						cumulus.AmbientExtraUseTempHum = settings.httpSensors.ambient.useTempHum;
						//cumulus.AmbientExtraUseSoilTemp = settings.httpSensors.ambient.useSoilTemp;
						cumulus.AmbientExtraUseSoilMoist = settings.httpSensors.ambient.useSoilMoist;
						//cumulus.AmbientExtraUseLeafWet = settings.httpSensors.ambient.useLeafWet;
						cumulus.AmbientExtraUseAQI = settings.httpSensors.ambient.useAQI;
						cumulus.AmbientExtraUseCo2 = settings.httpSensors.ambient.useCo2;
						cumulus.AmbientExtraUseLightning = settings.httpSensors.ambient.useLightning;
						cumulus.AmbientExtraUseLeak = settings.httpSensors.ambient.useLeak;

						// Also enable extra logging if applicable
						if (cumulus.AmbientExtraUseTempHum || cumulus.AmbientExtraUseSoilTemp || cumulus.AmbientExtraUseSoilMoist || cumulus.AmbientExtraUseAQI || cumulus.AmbientExtraUseCo2)
						{
							cumulus.StationOptions.LogExtraSensors = true;
						}
					}
					else
						cumulus.AmbientExtraEnabled = false;
				}
				catch (Exception ex)
				{
					var msg = "Error processing Ambient settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Blake-Larsen settings
				try
				{
					cumulus.SolarOptions.UseBlakeLarsen = settings.blakeLarsen.enabled;
				}
				catch (Exception ex)
				{
					var msg = "Error processing Blake-Larsen settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// RG-11 settings
				try
				{
					cumulus.RG11Enabled = settings.rg11.port1.enabled;
					if (cumulus.RG11Enabled)
					{
						cumulus.RG11Port = settings.rg11.port1.commPort;
						cumulus.RG11TBRmode = settings.rg11.port1.tipMode;
						cumulus.RG11tipsize = settings.rg11.port1.tipSize;
						cumulus.RG11IgnoreFirst = settings.rg11.port1.ignoreFirst;
						cumulus.RG11DTRmode = settings.rg11.port1.dtrMode;
					}

					cumulus.RG11Enabled2 = settings.rg11.port2.enabled;
					if (cumulus.RG11Enabled2)
					{
						cumulus.RG11Port2 = settings.rg11.port2.commPort;
						cumulus.RG11TBRmode2 = settings.rg11.port2.tipMode;
						cumulus.RG11tipsize2 = settings.rg11.port2.tipSize;
						cumulus.RG11IgnoreFirst2 = settings.rg11.port2.ignoreFirst;
						cumulus.RG11DTRmode2 = settings.rg11.port2.dtrMode;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing RG-11 settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Save the settings
				cumulus.WriteIniFile();
			}
			catch (Exception ex)
			{
				var msg = "Error processing Extra Sensor settings";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Extra Sensor Data: " + json);
				errorMsg += msg;
				context.Response.StatusCode = 500;
			}

			return context.Response.StatusCode == 200 ? "success" : errorMsg;
		}



		private class SettingsJson
		{
			public bool accessible { get; set; }
			public AirQualityJson airquality { get; set; }
			public AirLinkSettingsJson airLink { get; set; }
			public HttpStationsJson httpSensors { get; set; }
			public BlakeLarsenJson blakeLarsen { get; set; }
			public RG11Json rg11 { get; set; }
		}

		private class AirQualityJson
		{
			public int primaryaqsensor { get; set; }
			public int aqi { get; set; }
		}

		private class AirLinkSettingsJson
		{
			public bool isNode { get; set; }
			public string apiKey { get; set; }
			public string apiSecret { get; set; }
			public bool autoUpdateIp { get; set; }
			public AirLinkDeviceJson indoor { get; set; }
			public AirLinkDeviceJson outdoor { get; set; }
		}

		private class AirLinkDeviceJson
		{
			public bool enabled { get; set; }
			public string ipAddress { get; set; }
			public string hostname { get; set; }
			public int stationId { get; set; }
		}

		private class HttpStationsJson
		{
			public int extraStation { get; set; }
			public EcowittJson ecowitt { get; set; }
			public AmbientJson ambient { get; set; }
		}

		private class AmbientJson
		{
			public bool useSolar { get; set; }
			public bool useUv { get; set; }
			public bool useTempHum { get; set; }
			//public bool useSoilTemp { get; set; }
			public bool useSoilMoist { get; set; }
			public bool useLeafWet { get; set; }
			public bool useUserTemp { get; set; }
			public bool useAQI { get; set; }
			public bool useCo2 { get; set; }
			public bool useLightning { get; set; }
			public bool useLeak { get; set; }
		}

		private class EcowittJson : AmbientJson
		{
			public bool setcustom { get; set; }
			public string gwaddr { get; set; }
			public string localaddr { get; set; }
			public int interval { get; set; }
			public int primaryTHsensor { get; set; }
		}

		private class BlakeLarsenJson
		{
			public bool enabled { get; set; }
		}

		private class RG11Json
		{
			public RG11devicejson port1 { get; set; }
			public RG11devicejson port2 { get; set; }
		}

		private class RG11devicejson
		{
			public bool enabled { get; set; }
			public string commPort { get; set; }
			public bool tipMode { get; set; }
			public double tipSize { get; set; }
			public bool ignoreFirst { get; set; }
			public bool dtrMode { get; set; }
		}
	}
}
