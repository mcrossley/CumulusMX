﻿using System;
using System.IO;
using System.Net;
using EmbedIO;
using ServiceStack;
using ServiceStack.Text;

namespace CumulusMX
{
	public class Wizard
	{
		private readonly Cumulus cumulus;

		public Wizard(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		public string GetAlpacaFormData()
		{
			var location = new LocationJson()
			{
				sitename = cumulus.LocationName,
				description = cumulus.LocationDesc,
				latitude = cumulus.Latitude,
				longitude = cumulus.Longitude,
				altitude = (int)cumulus.Altitude,
				altitudeunit = cumulus.AltitudeInFeet ? "feet" : "metres",
			};

			var units = new UnitsJson()
			{
				wind = cumulus.Units.Wind,
				pressure = cumulus.Units.Press,
				temp = cumulus.Units.Temp,
				rain = cumulus.Units.Rain
			};

			var logs = new LogsJson()
			{
				loginterval = cumulus.DataLogInterval,
				logrollover = new StationSettings.LogRolloverJson()
				{
					time = cumulus.RolloverHour == 9 ? "9am" : "midnight",
					summer10am = cumulus.Use10amInSummer
				}
			};

			var davisvp = new DavisVpJson()
			{
				conntype = cumulus.DavisOptions.ConnectionType,
				comportname = cumulus.ComportName,
				tcpsettings = new StationSettings.TcpSettingsJson()
				{
					ipaddress = cumulus.DavisOptions.IPAddr,
					disconperiod = cumulus.DavisOptions.PeriodicDisconnectInterval
				}
			};

			var daviswll = new DavisWllJson()
			{
				network = new StationSettings.WLLNetworkJson()
				{
					autoDiscover = cumulus.WLLAutoUpdateIpAddress,
					ipaddress = cumulus.DavisOptions.IPAddr
				},
				api = new StationSettings.WLLApiJson()
				{
					apiKey = cumulus.WllApiKey,
					apiSecret = cumulus.WllApiSecret,
					apiStationId = cumulus.WllStationId
				},
				primary = new StationSettings.WllPrimaryJson()
				{
					wind = cumulus.WllPrimaryWind,
					temphum = cumulus.WllPrimaryTempHum,
					rain = cumulus.WllPrimaryRain,
					solar = cumulus.WllPrimarySolar,
					uv = cumulus.WllPrimaryUV
				}
			};

			var weatherflow = new StationSettings.WeatherFlowJson()
				{deviceid = cumulus.WeatherFlowOptions.WFDeviceId, tcpport = cumulus.WeatherFlowOptions.WFTcpPort, token = cumulus.WeatherFlowOptions.WFToken, dayshistory = cumulus.WeatherFlowOptions.WFDaysHist};

			var gw1000 = new StationSettings.Gw1000ConnJson()
			{
				ipaddress = cumulus.Gw1000IpAddress,
				autoDiscover = cumulus.Gw1000AutoUpdateIpAddress,
				macaddress = cumulus.Gw1000MacAddress
			};

			var fineoffset = new FineOffsetJson()
			{
				syncreads = cumulus.FineOffsetOptions.SyncReads,
				readavoid = cumulus.FineOffsetOptions.ReadAvoidPeriod
			};

			var easyweather = new EasyWeatherJson()
			{
				interval = cumulus.EwOptions.Interval,
				filename = cumulus.EwOptions.Filename
			};

			var imet = new ImetJson()
			{
				comportname = cumulus.ComportName,
				baudrate = cumulus.ImetOptions.BaudRate
			};

			var wmr = new StationSettings.WMR928Json()
			{
				comportname = cumulus.ComportName
			};


			var station = new StationJson()
			{
				stationtype = cumulus.StationType,
				stationmodel = cumulus.StationModel,
				davisvp2 = davisvp,
				daviswll = daviswll,
				gw1000 = gw1000,
				fineoffset = fineoffset,
				easyw = easyweather,
				imet = imet,
				wmr928 = wmr,
				weatherflow = weatherflow
			};

			var copy = new InternetCopyJson()
			{
				localcopy = cumulus.FtpOptions.LocalCopyEnabled,
				localcopyfolder = cumulus.FtpOptions.LocalCopyFolder,
			};

			var ftp = new InternetFtpJson()
			{
				enabled = cumulus.FtpOptions.Enabled,
				directory = cumulus.FtpOptions.Directory,
				ftpport = cumulus.FtpOptions.Port,
				sslftp = (int)cumulus.FtpOptions.FtpMode,
				hostname = cumulus.FtpOptions.Hostname,
				password = cumulus.FtpOptions.Password,
				username = cumulus.FtpOptions.Username,
				sshAuth = cumulus.FtpOptions.SshAuthen,
				pskFile = cumulus.FtpOptions.SshPskFile
			};
			var internet = new InternetJson()
			{
				copy = copy,
				ftp = ftp
			};

			var website = new WebSiteJson()
			{
				interval = new WebIntervalJson()
				{
					enabled = cumulus.WebIntervalEnabled,
					enableintervalftp = cumulus.FtpOptions.IntervalEnabled,
					ftpinterval = cumulus.UpdateInterval
				},
				realtime = new WebRealtimeJson()
				{
					enabled = cumulus.RealtimeIntervalEnabled,
					enablerealtimeftp = cumulus.FtpOptions.RealtimeEnabled,
					realtimeinterval = cumulus.RealtimeInterval / 1000
				}
			};

			var settings = new WizardJson()
			{
				location = location,
				units = units,
				station = station,
				logs = logs,
				internet = internet,
				website = website
			};

			return JsonSerializer.SerializeToString(settings);
		}

		public string UpdateConfig(IHttpContext context)
		{
			var errorMsg = "";
			var json = "";
			WizardJson settings;

			Cumulus.LogMessage("Updating settings from the First Time Wizard");

			context.Response.StatusCode = 200;

			try
			{
				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				json = WebUtility.UrlDecode(data[5..]);

				// de-serialize it to the settings structure
				settings = json.FromJson<WizardJson>();
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing Set-up Wizard Settings JSON";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Wizard Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}

			// process the settings
			try
			{
				Cumulus.LogMessage("Updating internet settings");

				// website settings
				try
				{
					cumulus.FtpOptions.Enabled = settings.internet.ftp.enabled;
					if (cumulus.FtpOptions.Enabled)
					{
						cumulus.FtpOptions.Directory = settings.internet.ftp.directory ?? string.Empty;
						cumulus.FtpOptions.Port = settings.internet.ftp.ftpport;
						cumulus.FtpOptions.Hostname = settings.internet.ftp.hostname ?? string.Empty;
						cumulus.FtpOptions.FtpMode = (Cumulus.FtpProtocols)settings.internet.ftp.sslftp;
						cumulus.FtpOptions.Password = settings.internet.ftp.password ?? string.Empty;
						cumulus.FtpOptions.Username = settings.internet.ftp.username ?? string.Empty;
						cumulus.FtpOptions.SshAuthen = settings.internet.ftp.sshAuth ?? string.Empty;
						cumulus.FtpOptions.SshPskFile = settings.internet.ftp.pskFile ?? string.Empty;
					}

					cumulus.FtpOptions.LocalCopyEnabled = settings.internet.copy.localcopy;
					if (cumulus.FtpOptions.LocalCopyEnabled)
					{
						cumulus.FtpOptions.LocalCopyFolder = settings.internet.copy.localcopyfolder;
					}

					// Now flag all the standard files to FTP/Copy or not
					// do not process last entry = wxnow.txt, it is not used by the standard site
					for (var i = 0; i < cumulus.StdWebFiles.Length - 1; i++)
					{
						cumulus.StdWebFiles[i].Create = cumulus.FtpOptions.Enabled || cumulus.FtpOptions.LocalCopyEnabled;
						cumulus.StdWebFiles[i].FTP = cumulus.FtpOptions.Enabled;
						cumulus.StdWebFiles[i].Copy = cumulus.FtpOptions.LocalCopyEnabled;
					}
					// and graph data files
					for (var i = 0; i < cumulus.GraphDataFiles.Length; i++)
					{
						cumulus.GraphDataFiles[i].Create = cumulus.FtpOptions.Enabled || cumulus.FtpOptions.LocalCopyEnabled;
						cumulus.GraphDataFiles[i].FTP = cumulus.FtpOptions.Enabled;
						cumulus.GraphDataFiles[i].Copy = cumulus.FtpOptions.LocalCopyEnabled;
					}
					// and EOD data files
					for (var i = 0; i < cumulus.GraphDataEodFiles.Length; i++)
					{
						cumulus.GraphDataEodFiles[i].Create = cumulus.FtpOptions.Enabled || cumulus.FtpOptions.LocalCopyEnabled;
						cumulus.GraphDataEodFiles[i].FTP = cumulus.FtpOptions.Enabled;
						cumulus.GraphDataEodFiles[i].Copy = cumulus.FtpOptions.LocalCopyEnabled;
					}
					// and Realtime files

					// realtime.txt is not used by the standard site
					//cumulus.RealtimeFiles[0].Create = cumulus.FtpOptions.Enabled || cumulus.FtpOptions.LocalCopyEnabled;
					//cumulus.RealtimeFiles[0].FTP = cumulus.FtpOptions.Enabled;
					//cumulus.RealtimeFiles[0].Copy = cumulus.FtpOptions.LocalCopyEnabled;

					// realtimegauges.txt IS used by the standard site
					cumulus.RealtimeFiles[1].Create = cumulus.FtpOptions.Enabled || cumulus.FtpOptions.LocalCopyEnabled;
					cumulus.RealtimeFiles[1].FTP = cumulus.FtpOptions.Enabled;
					cumulus.RealtimeFiles[1].Copy = cumulus.FtpOptions.LocalCopyEnabled;

					// and Moon image
					cumulus.MoonImage.Enabled = cumulus.FtpOptions.Enabled || cumulus.FtpOptions.LocalCopyEnabled;
					cumulus.MoonImage.Ftp = cumulus.FtpOptions.Enabled;
					cumulus.MoonImage.Copy = cumulus.FtpOptions.LocalCopyEnabled;
					if (cumulus.MoonImage.Enabled)
						cumulus.MoonImage.CopyDest = cumulus.FtpOptions.LocalCopyFolder + "images" + cumulus.DirectorySeparator + "moon.png";

					// and NOAA reports
					cumulus.NOAAconf.Create = cumulus.FtpOptions.Enabled || cumulus.FtpOptions.LocalCopyEnabled;
					cumulus.NOAAconf.AutoFtp = cumulus.FtpOptions.Enabled;
					cumulus.NOAAconf.AutoCopy = cumulus.FtpOptions.LocalCopyEnabled;
					if (cumulus.NOAAconf.AutoCopy)
					{
						cumulus.NOAAconf.CopyFolder = cumulus.FtpOptions.LocalCopyFolder + "Reports";
					}
					if (cumulus.NOAAconf.AutoFtp)
					{
						cumulus.NOAAconf.FtpFolder = cumulus.FtpOptions.Directory + (cumulus.FtpOptions.Directory.EndsWith("/") ? "" : "/") + "Reports";
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing internet settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// web settings
				try
				{
					cumulus.RealtimeIntervalEnabled = settings.website.realtime.enabled;
					if (cumulus.RealtimeIntervalEnabled)
					{
						cumulus.FtpOptions.RealtimeEnabled = settings.website.realtime.enablerealtimeftp;
						cumulus.RealtimeInterval = settings.website.realtime.realtimeinterval * 1000;
						if (cumulus.RealtimeTimer.Interval != cumulus.RealtimeInterval)
							cumulus.RealtimeTimer.Interval = cumulus.RealtimeInterval;
					}
					cumulus.RealtimeTimer.Enabled = cumulus.RealtimeIntervalEnabled;
					if (!cumulus.RealtimeTimer.Enabled || !cumulus.FtpOptions.RealtimeEnabled)
					{
						cumulus.RealtimeTimer.Stop();
						cumulus.RealtimeFTPDisconnect();
					}

					cumulus.WebIntervalEnabled = settings.website.interval.enabled;
					if (cumulus.WebIntervalEnabled)
					{
						cumulus.FtpOptions.IntervalEnabled = settings.website.interval.enableintervalftp;
						cumulus.UpdateInterval = settings.website.interval.ftpinterval;
						if (cumulus.WebTimer.Interval != cumulus.UpdateInterval * 60 * 1000)
							cumulus.WebTimer.Interval = cumulus.UpdateInterval * 60 * 1000;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing web settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Location
				try
				{
					cumulus.Altitude = settings.location.altitude;
					cumulus.AltitudeInFeet = (settings.location.altitudeunit == "feet");
					cumulus.LocationName = settings.location.sitename ?? string.Empty;
					cumulus.LocationDesc = settings.location.description ?? string.Empty;

					cumulus.Latitude = settings.location.latitude;

					cumulus.LatTxt = degToString(cumulus.Latitude, true);

					cumulus.Longitude = settings.location.longitude;

					cumulus.LonTxt = degToString(cumulus.Longitude, false);
				}
				catch (Exception ex)
				{
					var msg = "Error processing Location settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Units
				try
				{
					if (cumulus.Units.Wind != settings.units.wind)
					{
						cumulus.Units.Wind = settings.units.wind;
						cumulus.ChangeWindUnits();
						cumulus.WindDPlaces = cumulus.StationOptions.RoundWindSpeed ? 0 : cumulus.WindDPlaceDefaults[cumulus.Units.Wind];
						cumulus.WindAvgDPlaces = cumulus.WindDPlaces;
					}
					if (cumulus.Units.Press != settings.units.pressure)
					{
						cumulus.Units.Press = settings.units.pressure;
						cumulus.ChangePressureUnits();
						cumulus.PressDPlaces = cumulus.PressDPlaceDefaults[cumulus.Units.Press];
					}
					if (cumulus.Units.Temp != settings.units.temp)
					{
						cumulus.Units.Temp = settings.units.temp;
						cumulus.ChangeTempUnits();
					}
					if (cumulus.Units.Rain != settings.units.rain)
					{
						cumulus.Units.Rain = settings.units.rain;
						cumulus.ChangeRainUnits();
						cumulus.RainDPlaces = cumulus.RainDPlaceDefaults[cumulus.Units.Rain];
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing Units settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// data logging
				try
				{
					cumulus.DataLogInterval = settings.logs.loginterval;
					cumulus.RolloverHour = settings.logs.logrollover.time == "9am" ? 9 : 0;
					if (cumulus.RolloverHour == 9)
						cumulus.Use10amInSummer = settings.logs.logrollover.summer10am;

				}
				catch (Exception ex)
				{
					var msg = "Error processing Logging setting";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Station type
				try
				{
					if (cumulus.StationType != settings.station.stationtype)
					{
						Cumulus.LogMessage("Station type changed, restart required");
						Cumulus.LogConsoleMessage("*** Station type changed, restart required ***", ConsoleColor.Yellow);
					}
					cumulus.StationType = settings.station.stationtype;
					cumulus.StationModel = settings.station.stationmodel;
				}
				catch (Exception ex)
				{
					var msg = "Error processing Station Type setting";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Davis VP/VP2/Vue
				try
				{
					if (settings.station.davisvp2 != null)
					{
						cumulus.DavisOptions.ConnectionType = settings.station.davisvp2.conntype;
						if (settings.station.davisvp2.tcpsettings != null)
						{
							cumulus.DavisOptions.IPAddr = settings.station.davisvp2.tcpsettings.ipaddress ?? string.Empty;
							cumulus.DavisOptions.PeriodicDisconnectInterval = settings.station.davisvp2.tcpsettings.disconperiod;
						}
						if (cumulus.DavisOptions.ConnectionType == 0)
						{
							cumulus.ComportName = settings.station.davisvp2.comportname;
						}

						// set defaults for Davis
						cumulus.UVdecimals = 1;

						if (settings.units.rain == 1)
						{
							cumulus.RainDPlaces = 2;
						}
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing Davis VP/VP2/Vue settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// WLL
				try
				{
					if (settings.station.daviswll != null)
					{
						cumulus.DavisOptions.ConnectionType = 2; // Always TCP/IP for WLL
						cumulus.WLLAutoUpdateIpAddress = settings.station.daviswll.network.autoDiscover;
						cumulus.DavisOptions.IPAddr = settings.station.daviswll.network.ipaddress ?? string.Empty;

						cumulus.WllApiKey = settings.station.daviswll.api.apiKey;
						cumulus.WllApiSecret = settings.station.daviswll.api.apiSecret;
						cumulus.WllStationId = settings.station.daviswll.api.apiStationId;

						cumulus.WllPrimaryRain = settings.station.daviswll.primary.rain;
						cumulus.WllPrimarySolar = settings.station.daviswll.primary.solar;
						cumulus.WllPrimaryTempHum = settings.station.daviswll.primary.temphum;
						cumulus.WllPrimaryUV = settings.station.daviswll.primary.uv;
						cumulus.WllPrimaryWind = settings.station.daviswll.primary.wind;

					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing WLL settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// GW1000 connection details
				try
				{
					if (settings.station.gw1000 != null)
					{
						cumulus.Gw1000IpAddress = settings.station.gw1000.ipaddress;
						cumulus.Gw1000AutoUpdateIpAddress = settings.station.gw1000.autoDiscover;
						cumulus.Gw1000MacAddress = settings.station.gw1000.macaddress;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing GW1000 settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// weatherflow connection details
				try
				{
					if (settings.station.weatherflow != null)
					{
						cumulus.WeatherFlowOptions.WFDeviceId = settings.station.weatherflow.deviceid;
						cumulus.WeatherFlowOptions.WFTcpPort = settings.station.weatherflow.tcpport;
						cumulus.WeatherFlowOptions.WFToken = settings.station.weatherflow.token;
						cumulus.WeatherFlowOptions.WFDaysHist = settings.station.weatherflow.dayshistory;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing WeatherFlow settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// FineOffset
				try
				{
					if (settings.station.fineoffset != null)
					{
						cumulus.FineOffsetOptions.SyncReads = settings.station.fineoffset.syncreads;
						cumulus.FineOffsetOptions.ReadAvoidPeriod = settings.station.fineoffset.readavoid;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing Fine Offset settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// EasyWeather
				try
				{
					if (settings.station.easyw != null)
					{
						cumulus.EwOptions.Interval = settings.station.easyw.interval;
						cumulus.EwOptions.Filename = settings.station.easyw.filename;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing EasyWeather settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Instromet
				try
				{
					if (settings.station.imet != null)
					{
						cumulus.ComportName = settings.station.imet.comportname ?? cumulus.ComportName;
						cumulus.ImetOptions.BaudRate = settings.station.imet.baudrate;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing Instromet settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// WMR928
				try
				{
					if (settings.station.wmr928 != null)
					{
						cumulus.ComportName = settings.station.wmr928.comportname ?? cumulus.ComportName;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing WMR928 settings";
					cumulus.LogExceptionMessage(ex, msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Save the settings
				cumulus.WriteIniFile();
			}
			catch (Exception ex)
			{
				var msg = "Error processing Wizard settings";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Station Data: " + json);
				errorMsg += msg;
				context.Response.StatusCode = 500;
			}

			return context.Response.StatusCode == 200 ? "success" : errorMsg;
		}

		private static string degToString(double degrees, bool lat)
		{
			var degs = (int)Math.Floor(Math.Abs(degrees));
			var minsF = (Math.Abs(degrees) - degs) * 60.0;
			var secs = (int)Math.Round((minsF - Math.Floor(minsF)) * 60.0);
			var mins = (int)Math.Floor(minsF);
			string hemi;
			if (lat)
				hemi = degrees >= 0 ? "N" : "S";
			else
				hemi = degrees <= 0 ? "W" : "E";

			return $"{hemi}&nbsp;{degs:D2}&deg;&nbsp;{mins:D2}&#39;&nbsp;{secs:D2}&quot;";
		}

		private class WizardJson
		{
			public LocationJson location { get; set; }
			public UnitsJson units { get; set; }
			public StationJson station { get; set; }
			public LogsJson logs { get; set; }
			public InternetJson internet { get; set; }
			public WebSiteJson website { get; set; }
		}

		private class LocationJson
		{
			public double latitude { get; set; }
			public double longitude { get; set; }
			public int altitude { get; set; }
			public string altitudeunit { get; set; }
			public string sitename { get; set; }
			public string description { get; set; }
		}


		private class UnitsJson
		{
			public int wind { get; set; }
			public int pressure { get; set; }
			public int temp { get; set; }
			public int rain { get; set; }
		}

		private class LogsJson
		{
			public int loginterval { get; set; }
			public StationSettings.LogRolloverJson logrollover { get; set; }
		}

		private class StationJson
		{
			public int stationtype { get; set; }
			public string stationmodel { get; set; }
			public DavisVpJson davisvp2 { get; set; }
			public DavisWllJson daviswll { get; set; }
			public StationSettings.Gw1000ConnJson gw1000 { get; set; }
			public FineOffsetJson fineoffset { get; set; }
			public EasyWeatherJson easyw { get; set; }
			public ImetJson imet { get; set; }
			public StationSettings.WMR928Json wmr928 { get; set; }
			public StationSettings.WeatherFlowJson weatherflow { get; set; }
		}

		private class DavisVpJson
		{
			public int conntype { get; set; }
			public string comportname { get; set; }
			public StationSettings.TcpSettingsJson tcpsettings { get; set; }
		}

		private class DavisWllJson
		{
			public StationSettings.WLLNetworkJson network { get; set; }
			public StationSettings.WLLApiJson api { get; set; }
			public StationSettings.WllPrimaryJson primary { get; set; }
		}

		private class FineOffsetJson
		{
			public bool syncreads { get; set; }
			public int readavoid { get; set; }
		}

		private class EasyWeatherJson
		{
			public double interval { get; set; }
			public string filename { get; set; }
		}

		private class ImetJson
		{
			public string comportname { get; set; }
			public int baudrate { get; set; }
		}

		private class InternetJson
		{
			public InternetCopyJson copy { get; set; }
			public InternetFtpJson ftp { get; set; }
		}

		private class InternetCopyJson
		{
			public bool localcopy { get; set; }
			public string localcopyfolder { get; set; }

		}

		private class InternetFtpJson
		{
			public bool enabled { get; set; }
			public string hostname { get; set; }
			public int ftpport { get; set; }
			public int sslftp { get; set; }
			public string directory { get; set; }
			public string username { get; set; }
			public string password { get; set; }
			public string sshAuth { get; set; }
			public string pskFile { get; set; }

		}

		private class WebSiteJson
		{
			public WebIntervalJson interval { get; set; }
			public WebRealtimeJson realtime { get; set; }
		}

		private class WebIntervalJson
		{
			public bool enabled { get; set; }
			public bool enableintervalftp { get; set; }
			public int ftpinterval { get; set; }
		}

		private class WebRealtimeJson
		{
			public bool enabled { get; set; }
			public bool enablerealtimeftp { get; set; }
			public int realtimeinterval { get; set; }
		}
	}
}