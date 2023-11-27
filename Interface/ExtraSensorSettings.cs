using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using EmbedIO;

using ServiceStack;

namespace CumulusMX
{
	public class ExtraSensorSettings
	{
		private readonly Cumulus cumulus;
		private WeatherStation station;

		public ExtraSensorSettings(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		internal void SetStation(WeatherStation station)
		{
			this.station = station;
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

			var ecowittwn34map = new StationSettings.EcowittMappingsJson()
			{
				primaryTHsensor = cumulus.Gw1000PrimaryTHSensor,

				wn34chan1 = cumulus.EcowittSettings.MapWN34[1],
				wn34chan2 = cumulus.EcowittSettings.MapWN34[2],
				wn34chan3 = cumulus.EcowittSettings.MapWN34[3],
				wn34chan4 = cumulus.EcowittSettings.MapWN34[4],
				wn34chan5 = cumulus.EcowittSettings.MapWN34[5],
				wn34chan6 = cumulus.EcowittSettings.MapWN34[6],
				wn34chan7 = cumulus.EcowittSettings.MapWN34[7],
				wn34chan8 = cumulus.EcowittSettings.MapWN34[8]
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
				useCamera = cumulus.EcowittSettings.ExtraUseCamera,
				setcustom = cumulus.EcowittSettings.ExtraSetCustomServer,
				gwaddr = cumulus.EcowittSettings.ExtraGatewayAddr,
				localaddr = cumulus.EcowittSettings.ExtraLocalAddr,
				interval = cumulus.EcowittSettings.ExtraCustomInterval,

				mappings = ecowittwn34map
			};

			ecowitt.forwarders = new ExtraSensorForwardersJson()
			{
				usemain = cumulus.EcowittSettings.ExtraUseMainForwarders
			};

			ecowitt.forwarders.forward = new List<EcowittForwardListJson>();
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.EcowittSettings.ExtraForwarders[i]))
				{
					ecowitt.forwarders.forward.Add(new EcowittForwardListJson() { url = cumulus.EcowittSettings.ExtraForwarders[i] });
				}
			}

			var ecowittapi = new StationSettings.EcowittApi()
			{
				applicationkey = cumulus.EcowittSettings.AppKey,
				userkey = cumulus.EcowittSettings.UserApiKey,
				mac = cumulus.EcowittSettings.MacAddress
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
				ecowittapi = ecowittapi,
				ambient = ambient
			};

			if (cumulus.EcowittSettings.ExtraEnabled)
				httpStation.extraStation = 0;
			else if (cumulus.AmbientExtraEnabled)
				httpStation.extraStation = 1;
			else if (cumulus.EcowittSettings.CloudExtraEnabled)
				httpStation.extraStation = 2;
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
				cumulus.LogMessage("Updating extra sensor settings");

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
					cumulus.AirLinkApiKey = (settings.airLink.apiKey ?? string.Empty).Trim();
					cumulus.AirLinkApiSecret = (settings.airLink.apiSecret ?? string.Empty).Trim();
					cumulus.AirLinkAutoUpdateIpAddress = settings.airLink.autoUpdateIp;

					cumulus.AirLinkInEnabled = settings.airLink.indoor.enabled;
					if (cumulus.AirLinkInEnabled)
					{
						cumulus.AirLinkInIPAddr = (settings.airLink.indoor.ipAddress ?? string.Empty).Trim();
						cumulus.AirLinkInHostName = (settings.airLink.indoor.hostname ?? string.Empty).Trim();
						cumulus.AirLinkInStationId = settings.airLink.indoor.stationId;
						if (cumulus.AirLinkInStationId < 10 && cumulus.AirLinkIsNode)
						{
							cumulus.AirLinkInStationId = cumulus.WllStationId;
						}
					}
					cumulus.AirLinkOutEnabled = settings.airLink.outdoor.enabled;
					if (cumulus.AirLinkOutEnabled)
					{
						cumulus.AirLinkOutIPAddr = (settings.airLink.outdoor.ipAddress ?? string.Empty).Trim();
						cumulus.AirLinkOutHostName = (settings.airLink.outdoor.hostname ?? string.Empty).Trim();
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
					cumulus.EcowittSettings.ExtraEnabled = settings.httpSensors.extraStation == 0;
					cumulus.EcowittSettings.ExtraEnabled = settings.httpSensors.extraStation == 0;

					if (cumulus.EcowittSettings.ExtraEnabled || cumulus.EcowittSettings.CloudExtraEnabled)
					{
						cumulus.EcowittSettings.ExtraUseSolar = settings.httpSensors.ecowitt.useSolar;
						cumulus.EcowittSettings.ExtraUseUv = settings.httpSensors.ecowitt.useUv;
						cumulus.EcowittSettings.ExtraUseTempHum = settings.httpSensors.ecowitt.useTempHum;
						//cumulus.EcowittSettings.ExtraUseSoilTemp = settings.httpSensors.ecowitt.useSoilTemp;
						cumulus.EcowittSettings.ExtraUseSoilMoist = settings.httpSensors.ecowitt.useSoilMoist;
						cumulus.EcowittSettings.ExtraUseLeafWet = settings.httpSensors.ecowitt.useLeafWet;
						cumulus.EcowittSettings.ExtraUseUserTemp = settings.httpSensors.ecowitt.useUserTemp;
						cumulus.EcowittSettings.ExtraUseAQI = settings.httpSensors.ecowitt.useAQI;
						cumulus.EcowittSettings.ExtraUseCo2 = settings.httpSensors.ecowitt.useCo2;
						cumulus.EcowittSettings.ExtraUseLightning = settings.httpSensors.ecowitt.useLightning;
						cumulus.EcowittSettings.ExtraUseLeak = settings.httpSensors.ecowitt.useLeak;
						cumulus.EcowittSettings.ExtraUseCamera = settings.httpSensors.ecowitt.useCamera;

						cumulus.EcowittSettings.ExtraSetCustomServer = settings.httpSensors.ecowitt.setcustom;
						if (cumulus.EcowittSettings.ExtraSetCustomServer)
						{
							cumulus.EcowittSettings.ExtraGatewayAddr = settings.httpSensors.ecowitt.gwaddr;
						cumulus.EcowittSettings.ExtraLocalAddr = settings.httpSensors.ecowitt.localaddr;
						cumulus.EcowittSettings.ExtraCustomInterval = settings.httpSensors.ecowitt.interval;
}

						cumulus.Gw1000PrimaryTHSensor = settings.httpSensors.ecowitt.mappings.primaryTHsensor;

						if (cumulus.EcowittSettings.MapWN34[1] != settings.httpSensors.ecowitt.mappings.wn34chan1)
						{
							if (cumulus.EcowittSettings.MapWN34[1] == 0)
								station.UserTemp[1] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[1]] = null;

							cumulus.EcowittSettings.MapWN34[1] = settings.httpSensors.ecowitt.mappings.wn34chan1;
						}

						if (cumulus.EcowittSettings.MapWN34[2] != settings.httpSensors.ecowitt.mappings.wn34chan2)
						{
							if (cumulus.EcowittSettings.MapWN34[2] == 0)
								station.UserTemp[2] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[2]] = null;

							cumulus.EcowittSettings.MapWN34[2] = settings.httpSensors.ecowitt.mappings.wn34chan2;
						}

						if (cumulus.EcowittSettings.MapWN34[3] != settings.httpSensors.ecowitt.mappings.wn34chan3)
						{
							if (cumulus.EcowittSettings.MapWN34[3] == 0)
								station.UserTemp[3] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[3]] = null;

							cumulus.EcowittSettings.MapWN34[3] = settings.httpSensors.ecowitt.mappings.wn34chan3;
						}

						if (cumulus.EcowittSettings.MapWN34[4] != settings.httpSensors.ecowitt.mappings.wn34chan4)
						{
							if (cumulus.EcowittSettings.MapWN34[4] == 0)
								station.UserTemp[4] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[4]] = null;

							cumulus.EcowittSettings.MapWN34[4] = settings.httpSensors.ecowitt.mappings.wn34chan4;
						}

						if (cumulus.EcowittSettings.MapWN34[5] != settings.httpSensors.ecowitt.mappings.wn34chan5)
						{
							if (cumulus.EcowittSettings.MapWN34[5] == 0)
								station.UserTemp[5] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[5]] = null;

							cumulus.EcowittSettings.MapWN34[5] = settings.httpSensors.ecowitt.mappings.wn34chan5;
						}

						if (cumulus.EcowittSettings.MapWN34[6] != settings.httpSensors.ecowitt.mappings.wn34chan6)
						{
							if (cumulus.EcowittSettings.MapWN34[6] == 0)
								station.UserTemp[6] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[6]] = null;

							cumulus.EcowittSettings.MapWN34[6] = settings.httpSensors.ecowitt.mappings.wn34chan6;
						}

						if (cumulus.EcowittSettings.MapWN34[7] != settings.httpSensors.ecowitt.mappings.wn34chan7)
						{
							if (cumulus.EcowittSettings.MapWN34[7] == 0)
								station.UserTemp[7] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[7]] = null;

							cumulus.EcowittSettings.MapWN34[7] = settings.httpSensors.ecowitt.mappings.wn34chan7;
						}

						if (cumulus.EcowittSettings.MapWN34[8] != settings.httpSensors.ecowitt.mappings.wn34chan8)
						{
							if (cumulus.EcowittSettings.MapWN34[8] == 0)
								station.UserTemp[8] = null;
							else
								station.SoilTemp[cumulus.EcowittSettings.MapWN34[8]] = null;

							cumulus.EcowittSettings.MapWN34[8] = settings.httpSensors.ecowitt.mappings.wn34chan8;
						}

						cumulus.EcowittSettings.ExtraUseMainForwarders = settings.httpSensors.ecowitt.forwarders == null ? true : settings.httpSensors.ecowitt.forwarders.usemain;

						if (!cumulus.EcowittSettings.ExtraUseMainForwarders)
						{
							for (var i = 0; i < 10; i++)
							{
								if (i < settings.httpSensors.ecowitt.forwarders.forward.Count)
								{
									cumulus.EcowittSettings.ExtraForwarders[i] = string.IsNullOrWhiteSpace(settings.httpSensors.ecowitt.forwarders.forward[i].url) ? null : settings.httpSensors.ecowitt.forwarders.forward[i].url.Trim();
								}
								else
								{
									cumulus.EcowittSettings.ExtraForwarders[i] = null;
								}
							}
						}
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

				// Ecowitt API
				try
				{
					if (settings.httpSensors.ecowittapi != null)
					{
						cumulus.EcowittSettings.AppKey = string.IsNullOrWhiteSpace(settings.httpSensors.ecowittapi.applicationkey) ? null : settings.httpSensors.ecowittapi.applicationkey.Trim();
						cumulus.EcowittSettings.UserApiKey = string.IsNullOrWhiteSpace(settings.httpSensors.ecowittapi.userkey) ? null : settings.httpSensors.ecowittapi.userkey.Trim();
						cumulus.EcowittSettings.MacAddress = string.IsNullOrWhiteSpace(settings.httpSensors.ecowittapi.mac) ? null : settings.httpSensors.ecowittapi.mac.Trim();
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing Ecowitt API settings: " + ex.Message;
					cumulus.LogErrorMessage(msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Ambient Extra settings
				try
				{
					cumulus.AmbientExtraEnabled = settings.httpSensors.extraStation == 1;
					if (cumulus.AmbientExtraEnabled)
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
						cumulus.RG11Port = (settings.rg11.port1.commPort ?? string.Empty).Trim();
						cumulus.RG11TBRmode = settings.rg11.port1.tipMode;
						cumulus.RG11tipsize = settings.rg11.port1.tipSize;
						cumulus.RG11IgnoreFirst = settings.rg11.port1.ignoreFirst;
						cumulus.RG11DTRmode = settings.rg11.port1.dtrMode;
					}

					cumulus.RG11Enabled2 = settings.rg11.port2.enabled;
					if (cumulus.RG11Enabled2)
					{
						cumulus.RG11Port2 = (settings.rg11.port2.commPort ?? string.Empty).Trim();
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
			public StationSettings.EcowittApi ecowittapi { get; set; }
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
			public bool useCamera { get; set; }
		}

		private class EcowittJson : AmbientJson
		{
			public bool setcustom { get; set; }
			public string gwaddr { get; set; }
			public string localaddr { get; set; }
			public int interval { get; set; }
			internal StationSettings.EcowittMappingsJson mappings { get; set; }
			public ExtraSensorForwardersJson forwarders { get; set; }
		}

		public class ExtraSensorForwardersJson
		{
			public bool usemain { get; set; }
			public List<EcowittForwardListJson> forward { get; set; }
		}

		public class EcowittForwardListJson
		{
			public string url { get; set; }
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
