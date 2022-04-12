﻿using System;
using System.IO;
using System.Web;
using System.Globalization;
using System.Collections.Specialized;
using EmbedIO;
using System.Text;
using System.Threading.Tasks;

namespace CumulusMX
{

	class HttpStationEcowitt : WeatherStation
	{
		private readonly WeatherStation station;
		private bool starting = true;
		private bool stopping = false;
		private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;
		private bool reportStationType = true;
		private EcowittApi api;
		private int maxArchiveRuns = 1;

		public HttpStationEcowitt(Cumulus cumulus, WeatherStation station = null) : base(cumulus)
		{
			this.station = station;

			var mainStation = station == null;

			if (mainStation)
			{
				Cumulus.LogMessage("Creating HTTP Station (Ecowitt)");
			}
			else
			{
				Cumulus.LogMessage("Creating Extra Sensors - HTTP Station (Ecowitt)");
			}

			// Do not set these if we are only using extra sensors
			if (mainStation)
			{
				// does not provide average wind speeds
				cumulus.StationOptions.CalcWind10MinAve = true;

				// does not send DP, so force MX to calculate it
				cumulus.StationOptions.CalculatedDP = true;
				// Same for Wind Chill
				cumulus.StationOptions.CalculatedWC = true;
				// does not provide a forecast, force MX to provide it
				cumulus.UseCumulusForecast = true;
				// does not provide pressure trend strings
				cumulus.StationOptions.UseCumulusPresstrendstr = true;

				if (cumulus.Gw1000PrimaryTHSensor != 0)
				{
					// We are not using the primary T/H sensor
					Cumulus.LogMessage("Overriding the default outdoor temp/hum data with Extra temp/hum sensor #" + cumulus.Gw1000PrimaryTHSensor);
				}

				if (cumulus.EcowittSettings.SetCustomServer)
				{
					Cumulus.LogMessage("Checking Ecowitt Gateway Custom Server configuration...");
					var api = new Stations.GW1000Api(cumulus);
					api.OpenTcpPort(cumulus.EcowittSettings.GatewayAddr, 45000);
					SetCustomServer(api, mainStation);
					api.CloseTcpPort();
					Cumulus.LogMessage("Ecowitt Gateway Custom Server configuration complete");
				}
			}
			else if (cumulus.EcowittSettings.SetCustomServer)
			{
				Cumulus.LogMessage("Checking Ecowitt Extra Gateway Custom Server configuration...");
				var api = new Stations.GW1000Api(cumulus);
				api.OpenTcpPort(cumulus.EcowittSettings.GatewayAddr, 45000);
				SetCustomServer(api, mainStation);
				api.CloseTcpPort();
				Cumulus.LogMessage("Ecowitt Extra Gateway Custom Server configuration complete");
			}

			if (mainStation || (!mainStation && cumulus.EcowittSettings.ExtraUseAQI))
			{
				cumulus.Units.AirQualityUnitText = "µg/m³";
			}
			if (mainStation || (!mainStation && cumulus.EcowittSettings.ExtraUseSoilMoist))
			{
				cumulus.Units.SoilMoistureUnitText = "%";
			}
			if (mainStation || (!mainStation && cumulus.EcowittSettings.ExtraUseLeafWet))
			{
				cumulus.Units.LeafWetnessUnitText = "%";
			}



			// Only perform the Start-up if we are a proper station, not a Extra Sensor
			if (mainStation)
			{
				LoadLastHoursFromDataLogs(cumulus.LastUpdateTime);
			}
			else
			{
				Cumulus.LogMessage("Extra Sensors - HTTP Station (Ecowitt) - Waiting for data...");
			}
		}

		public override void DoStartup()
		{
			// Only perform the Start-up if we are a proper station, not a Extra Sensor
			if (station == null)
			{
				Cumulus.LogMessage("Starting HTTP Station (Ecowitt)");
				Task.Run(getAndProcessHistoryData);// grab old data, then start the station
			}
		}

		public override void Start()
		{
			if (station == null)
			{
				DoDayResetIfNeeded();
				DoTrendValues(DateTime.Now);
				Cumulus.LogMessage("Starting HTTP Station (Ecowitt)");
				cumulus.StartTimersAndSensors();
				starting = false;
			}
			else
			{
				Cumulus.LogMessage("Starting Extra Sensors - HTTP Station (Ecowitt)");
			}
		}

		public override void Stop()
		{
			stopping = true;
			if (station == null)
			{
				StopMinuteTimer();
				// Call the common code in the base class
				base.Stop();
			}
		}

		public override void getAndProcessHistoryData()
		{
			cumulus.LogDebugMessage("Lock: Station waiting for the lock");
			Cumulus.syncInit.Wait();
			cumulus.LogDebugMessage("Lock: Station has the lock");

			if (string.IsNullOrEmpty(cumulus.EcowittSettings.AppKey) || string.IsNullOrEmpty(cumulus.EcowittSettings.UserApiKey) || string.IsNullOrEmpty(cumulus.EcowittSettings.MacAddress))
			{
				Cumulus.LogMessage("API.GetHistoricData: Missing Ecowitt API data in the configuration, aborting!");
				cumulus.LastUpdateTime = DateTime.Now;
			}
			else
			{
				int archiveRun = 0;

				try
				{

					api = new EcowittApi(cumulus, this);

					do
					{
						GetHistoricData();
						archiveRun++;
					} while (archiveRun < maxArchiveRuns);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Exception occurred reading archive data");
				}
			}

			cumulus.LogDebugMessage("Lock: Station releasing the lock");
			_ = Cumulus.syncInit.Release();

			StartLoop();
		}

		private void GetHistoricData()
		{
			Cumulus.LogMessage("GetHistoricData: Starting Historic Data Process");

			// add one minute to avoid duplicating the last log entry
			var startTime = cumulus.LastUpdateTime.AddMinutes(1);
			var endTime = DateTime.Now;

			// The API call is limited to fetching 24 hours of data
			if ((endTime - startTime).TotalHours > 24.0)
			{
				// only fetch 24 hours worth of data, and schedule another run to fetch the rest
				endTime = startTime.AddHours(24);
				maxArchiveRuns++;
			}

			api.GetHistoricData(startTime, endTime);

		}

		public string ProcessData(IHttpContext context, bool main)
		{
			/*
			 * Ecowitt doc:
			 *
			POST Parameters - all fields are URL escaped

			PASSKEY=<redacted>&stationtype=GW1000A_V1.6.8&dateutc=2021-07-23+17:13:34&tempinf=80.6&humidityin=50&baromrelin=29.940&baromabsin=29.081&tempf=81.3&humidity=43&winddir=296&windspeedmph=2.46&windgustmph=4.25&maxdailygust=14.09&solarradiation=226.28&uv=1&rainratein=0.000&eventrainin=0.000&hourlyrainin=0.000&dailyrainin=0.000&weeklyrainin=0.000&monthlyrainin=4.118&yearlyrainin=29.055&totalrainin=29.055&temp1f=83.48&humidity1=39&temp2f=87.98&humidity2=40&temp3f=82.04&humidity3=40&temp4f=93.56&humidity4=34&temp5f=-11.38&temp6f=87.26&humidity6=38&temp7f=45.50&humidity7=40&soilmoisture1=51&soilmoisture2=65&soilmoisture3=72&soilmoisture4=36&soilmoisture5=48&pm25_ch1=11.0&pm25_avg_24h_ch1=10.8&pm25_ch2=13.0&pm25_avg_24h_ch2=15.0&tf_co2=80.8&humi_co2=48&pm25_co2=4.8&pm25_24h_co2=6.1&pm10_co2=4.9&pm10_24h_co2=6.5&co2=493&co2_24h=454&lightning_time=1627039348&lightning_num=3&lightning=24&wh65batt=0&wh80batt=3.06&batt1=0&batt2=0&batt3=0&batt4=0&batt5=0&batt6=0&batt7=0&soilbatt1=1.5&soilbatt2=1.4&soilbatt3=1.5&soilbatt4=1.5&soilbatt5=1.6&pm25batt1=4&pm25batt2=4&wh57batt=4&co2_batt=6&freq=868M&model=GW1000_Pro
			PASSKEY=<redacted>&stationtype=GW1100A_V2.0.2&dateutc=2021-09-08+11:58:39&tempinf=80.8&humidityin=42&baromrelin=29.864&baromabsin=29.415&temp1f=87.8&tf_ch1=64.4&batt1=0&tf_batt1=1.48&freq=868M&model=GW1100A

			 */

			DateTime recDate;

			var procName = main ? "ProcessData" : "ProcessExtraData";
			var thisStation = main ? this : station;


			if (starting || stopping)
			{
				context.Response.StatusCode = 200;
				return "success";
			}

			try
			{
				// PASSKEY
				// dateutc
				// freq
				// model

				cumulus.LogDebugMessage($"{procName}: Processing posted data");

				var text = new StreamReader(context.Request.InputStream).ReadToEnd();
				text = System.Text.RegularExpressions.Regex.Replace(text, "PASSKEY=[^&]+", "PASSKEY=<PassKey>");

				cumulus.LogDataMessage($"{procName}: Payload = {text}");
				if (main)
					LogRawStationData(text, false);
				else
					LogRawExtraData(text, false);

				var data = HttpUtility.ParseQueryString(text);

				// We will ignore the dateutc field, this is "live" data so just use "now" to avoid any clock issues
				recDate = DateTime.Now;

				// we only really want to do this once
				if (reportStationType)
				{
					cumulus.LogDebugMessage($"{procName}: StationType = {data["stationtype"]}, Model = {data["model"]}, Frequency = {data["freq"]}Hz");
					reportStationType = false;
				}

				string thisTemp = null;
				string thisHum = null;

				// Only do the primary sensors if running as the main station
				if (main)
				{
					thisTemp = cumulus.Gw1000PrimaryTHSensor == 0 ? data["tempf"] : data["temp" + cumulus.Gw1000PrimaryTHSensor + "f"];
					thisHum = cumulus.Gw1000PrimaryTHSensor == 0 ? data["humidity"] : data["humidity" + cumulus.Gw1000PrimaryTHSensor];

					// === Wind ==
					try
					{
						// winddir
						// winddir_avg10m ??
						// windgustmph
						// windspeedmph
						// windspdmph_avg2m ??
						// windspdmph_avg10m ??
						// windgustmph_10m ??
						// maxdailygust

						var gust = data["windgustmph"];
						var dir = data["winddir"];
						var spd = data["windspeedmph"];


						if (gust == null || dir == null || spd == null)
						{
							Cumulus.LogMessage($"ProcessData: Error, missing wind data");
						}
						var gustVal = gust == null ? null : ConvertWindMPHToUser(Convert.ToDouble(gust, invNum));
						int? dirVal = dir == null ? null : Convert.ToInt32(dir, invNum);
						var spdVal = spd == null ? null : ConvertWindMPHToUser(Convert.ToDouble(spd, invNum));

						// The protocol does not provide an average value
						// so feed in current MX average
						DoWind(spdVal, dirVal, (WindAverage ?? 0) / cumulus.Calib.WindSpeed.Mult, recDate);

						var gustLastCal = gustVal * cumulus.Calib.WindGust.Mult;
						if (gustLastCal > RecentMaxGust)
						{
							cumulus.LogDebugMessage("Setting max gust from current value: " + gustLastCal.Value.ToString(cumulus.WindFormat));
							CheckHighGust(gustLastCal, dirVal, recDate);

							// add to recent values so normal calculation includes this value
							WindRecent[nextwind].Gust = gustVal.Value; // use uncalibrated value
							WindRecent[nextwind].Speed = (WindAverage ?? 0) / cumulus.Calib.WindSpeed.Mult;
							WindRecent[nextwind].Timestamp = recDate;
							nextwind = (nextwind + 1) % MaxWindRecent;

							RecentMaxGust = gustLastCal;
						}

					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Wind data ");
						context.Response.StatusCode = 500;
						return "Failed: Error in wind data - " + ex.Message;
					}


					// === Humidity ===
					try
					{
						// humidity
						// humidityin

						var humIn = data["humidityin"];

						if (humIn == null)
						{
							DoIndoorHumidity(null);
							Cumulus.LogMessage("ProcessData: Error, missing indoor humidity");
						}
						else
						{
							var humVal = Convert.ToInt32(humIn, invNum);
							DoIndoorHumidity(humVal);
						}

						if (cumulus.Gw1000PrimaryTHSensor == 0)
						{
							if (thisHum == null)
							{
								DoHumidity(null, recDate);
								Cumulus.LogMessage("ProcessData: Error, missing outdoor humidity");
							}
							else
							{
								var humVal = Convert.ToInt32(thisHum, invNum);
								DoHumidity(humVal, recDate);
							}
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Humidity data");
						context.Response.StatusCode = 500;
						return "Failed: Error in humidity data - " + ex.Message;
					}


					// === Pressure ===
					try
					{
						// baromabsin
						// baromrelin

						var press = data["baromrelin"];

						if (press == null)
						{
							DoPressure(null, recDate);
							Cumulus.LogMessage($"ProcessData: Error, missing baro pressure");
						}
						else
						{
							var pressVal = ConvertPressINHGToUser(Convert.ToDouble(press, invNum));
							DoPressure(pressVal, recDate);
							UpdatePressureTrendString();
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Pressure data");
						context.Response.StatusCode = 500;
						return "Failed: Error in baro pressure data - " + ex.Message;
					}


					// === Indoor temp ===
					try
					{
						// tempinf

						var temp = data["tempinf"];

						if (temp == null)
						{
							DoIndoorTemp(null);
							Cumulus.LogMessage($"ProcessData: Error, missing indoor temp");
						}
						else
						{
							var tempVal = ConvertTempFToUser(Convert.ToDouble(temp, invNum));
							DoIndoorTemp(tempVal);
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Indoor temp data");
						context.Response.StatusCode = 500;
						return "Failed: Error in indoor temp data - " + ex.Message;
					}


					// === Outdoor temp ===
					try
					{
						// tempf
						if (cumulus.Gw1000PrimaryTHSensor == 0)
						{
							if (thisTemp == null)
							{
								DoTemperature(null, recDate);
								Cumulus.LogMessage($"ProcessData: Error, missing outdoor temp");
							}
							else
							{
								var tempVal = ConvertTempFToUser(Convert.ToDouble(thisTemp, invNum));
								DoTemperature(tempVal, recDate);
							}
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Outdoor temp data");
						context.Response.StatusCode = 500;
						return "Failed: Error in outdoor temp data - " + ex.Message;
					}


					// === Rain ===
					try
					{
						// rainin
						// hourlyrainin
						// dailyrainin
						// weeklyrainin
						// monthlyrainin
						// yearlyrainin - also turns out that not all stations send this :(
						// totalrainin - not reliable, depends on console and firmware version as to whether this is available or not.
						// rainratein
						// 24hourrainin Ambient only?
						// eventrainin

						// same for piezo data
						// ​rrain_piezo
						// erain_piezo - event rain
						// hrain_piezo
						// drain_piezo
						// wrain_piezo
						// mrain_piezo
						// yrain_piezo

						string rain, rRate;

						// if no yearly counter, try the total counter
						if (cumulus.Gw1000PrimaryRainSensor == 0)
						{
							rain = data["yearlyrainin"] ?? data["totalrainin"];
							rRate = data["rainratein"];
						}
						else
						{
							rain = data["yrain_piezo"];
							rRate = data["​rrain_piezo"];
						}


						if (rRate == null)
						{
							// No rain rate, so we will calculate it
							calculaterainrate = true;
							rRate = "0";
						}
						else
						{
							// we have a rain rate, so we will NOT calculate it
							calculaterainrate = false;
						}

						if (rain == null)
						{
							Cumulus.LogMessage($"ProcessData: Error, missing rainfall");
							var rateVal = ConvertRainINToUser(Convert.ToDouble(rRate, invNum));
							DoRain(null, rateVal, recDate);
						}
						else
						{
							var rainVal = ConvertRainINToUser(Convert.ToDouble(rain, invNum));
							var rateVal = ConvertRainINToUser(Convert.ToDouble(rRate, invNum));
							DoRain(rainVal, rateVal, recDate);
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Rain data");
						context.Response.StatusCode = 500;
						return "Failed: Error in rainfall data - " + ex.Message;
					}
				}

				// === Extra Temperature ===
				if (main || cumulus.EcowittSettings.ExtraUseTempHum)
				{
					try
					{
						// temp[1-10]f
						ProcessExtraTemps(data, thisStation, recDate);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in extra temperature data");
					}
				}

				// === Extra Humidity ===
				if (main || cumulus.EcowittSettings.ExtraUseTempHum)
				{
					try
					{
						// humidity[1-10]
						ProcessExtraHumidity(data, thisStation, recDate);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in extra humidity data");
					}
				}


				// === Solar ===
				if (main || cumulus.EcowittSettings.ExtraUseSolar)
				{
					try
					{
						// solarradiation
						ProcessSolar(data, thisStation, recDate);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in solar data");
					}
				}


				// === UV ===
				if (main || cumulus.EcowittSettings.ExtraUseUv)
				{
					try
					{
						// uv
						ProcessUv(data, thisStation, recDate);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in UV data");
					}
				}


				// === Soil Temp ===
				if (main || cumulus.EcowittSettings.ExtraUseSoilTemp)
				{
					try
					{
						// soiltempf
						// soiltemp[2-16]f
						ProcessSoilTemps(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in Soil temp data");
					}
				}


				// === Soil Moisture ===
				if (main || cumulus.EcowittSettings.ExtraUseSoilMoist)
				{
					try
					{
						// soilmoisture[1-16]
						ProcessSoilMoist(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in Soil moisture data");
					}
				}


				// === Leaf Wetness ===
				if (main || cumulus.EcowittSettings.ExtraUseLeafWet)
				{
					try
					{
						// leafwetness
						// leafwetness[2-8]
						ProcessLeafWetness(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in Leaf wetness data");
					}
				}


				// === User Temp (Soil or Water) ===
				if (main || cumulus.EcowittSettings.ExtraUseUserTemp)
				{
					try
					{
						// tf_ch[1-8]
						ProcessUserTemp(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in User Temp (soil or water) data");
					}
				}


				// === Air Quality ===
				if (main || cumulus.EcowittSettings.ExtraUseAQI)
				{
					try
					{
						// pm25_ch[1-4]
						// pm25_avg_24h_ch[1-4]
						ProcessAirQuality(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in Air Quality data");
					}
				}


				// === CO₂ ===
				if (main || cumulus.EcowittSettings.ExtraUseCo2)
				{
					try
					{
						// tf_co2
						// humi_co2
						// pm25_co2
						// pm25_24h_co2
						// pm10_co2
						// pm10_24h_co2
						// co2
						// co2_24h
						ProcessCo2(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in CO₂ data");
					}
				}


				// === Lightning ===
				if (main || cumulus.EcowittSettings.ExtraUseLightning)
				{
					try
					{
						// lightning
						// lightning_time
						// lightning_num
						ProcessLightning(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in Lightning data");
					}
				}


				// === Leak ===
				if (main || cumulus.EcowittSettings.ExtraUseLeak)
				{
					try
					{
						// leak[1 - 4]
						ProcessLeak(data, thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error in Leak data");
					}
				}


				// === Batteries ===
				try
				{
					/*
					wh25batt
					wh26batt
					wh32batt
					wh40batt
					wh57batt
					wh65batt
					wh68batt
					wh80batt
					batt[1-8] (wh31)
					soilbatt[1-8] (wh51)
					pm25batt[1-4] (wh41/wh43)
					leakbatt[1-4] (wh55)
					co2_batt
					*/

					ProcessBatteries(data);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, $"{procName}: Error in Battery data");
				}


				// === Extra Dew point ===
				if (main || cumulus.EcowittSettings.ExtraUseTempHum)
				{
					try
					{
						ProcessExtraDewPoint(thisStation);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{procName}: Error calculating extra sensor dew points");
					}
				}

				// === Firmware Version ===
				try
				{
					var fwString = data["stationtype"].Split("_V", StringSplitOptions.None);
					if (fwString.Length > 1)
					{
						// bug fix for WS90 which sends "stationtype=GW2000A_V2.1.0, runtime=253500"
						var str = fwString[1].Split(new string[] { ", " }, StringSplitOptions.None)[0];
						GW1000FirmwareVersion = str;
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, $"{procName}: Error extracting firmware version");
				}

				// Do derived values after the primary values

				if (main)
				{
					// === Dewpoint ===
					try
					{
						// dewptf
						var dewpnt = data["dewptf"];
						var val = dewpnt == null ? null : ConvertTempFToUser(Convert.ToDouble(dewpnt, invNum));
						DoDewpoint(val, recDate);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Dew point data");
						context.Response.StatusCode = 500;
						return "Failed: Error in dew point data - " + ex.Message;
					}


					// === Wind Chill ===
					try
					{
						// windchillf
						if (cumulus.StationOptions.CalculatedWC)
						{
							DoWindChill(0, recDate);
						}
						else
						{
							var chill = data["windchillf"];
							var val = chill == null ? null : ConvertTempFToUser(Convert.ToDouble(chill, invNum));
							DoWindChill(val, recDate);
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "ProcessData: Error in Dew point data");
						context.Response.StatusCode = 500;
						return "Failed: Error in dew point data - " + ex.Message;
					}


					// === Humidex ===
					if (data["tempf"] != null && data["humidity"] != null)
					{
						DoHumidex(recDate);

						// === Apparent === - requires temp, hum, and windspeed
						if (data["windspeedmph"] != null)
						{
							DoApparentTemp(recDate);
							DoFeelsLike(recDate);
						}
						else
						{
							Cumulus.LogMessage("ProcessData: Insufficient data to calculate Apparent/Feels Like temps");
						}
					}
					else
					{
						Cumulus.LogMessage("ProcessData: Insufficient data to calculate Humidex and Apparent/Feels Like temps");
					}
				}

				DoForecast(string.Empty, false);

				UpdateStatusPanel(recDate);
				UpdateMQTT();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, $"{procName}: Error");
				context.Response.StatusCode = 500;
				return "Failed: General error - " + ex.Message;
			}

			cumulus.LogDebugMessage($"{procName}: Complete");

			context.Response.StatusCode = 200;
			return "success";
		}

		private static void ProcessExtraTemps(NameValueCollection data, WeatherStation station, DateTime ts)
		{
			for (var i = 1; i <= 10; i++)
			{
				if (i == cumulus.Gw1000PrimaryTHSensor)
				{
					station.DoTemperature(ConvertTempFToUser(Utils.TryParseNullDouble(data["temp" + i + "f"])), ts);
				}
				station.DoExtraTemp(ConvertTempFToUser(Utils.TryParseNullDouble(data["temp" + i + "f"])), i);
			}
		}

		private void ProcessExtraHumidity(NameValueCollection data, WeatherStation station, DateTime ts)
		{
			for (var i = 1; i <= 10; i++)
			{
				if (data["humidity" + i] != null)
				{
					if (i == cumulus.Gw1000PrimaryTHSensor)
					{
						station.DoHumidity(Convert.ToInt32(data["humidity" + i], invNum), ts);
					}
					station.DoExtraHum(Convert.ToDouble(data["humidity" + i], invNum), i);
				}
				else
				{
					station.DoExtraHum(null, i);
				}
			}
		}

		private void ProcessSolar(NameValueCollection data, WeatherStation station, DateTime recDate)
		{
			if (data["solarradiation"] != null)
			{
				station.DoSolarRad((int)Convert.ToDouble(data["solarradiation"], invNum), recDate);
			}
			else
			{
				station.DoSolarRad(null, recDate);
			}
		}

		private void ProcessUv(NameValueCollection data, WeatherStation station, DateTime recDate)
		{
			if (data["uv"] != null)
			{
				station.DoUV(Convert.ToDouble(data["uv"], invNum), recDate);
			}
			else
			{
				station.DoUV(null, recDate);
			}
		}

		private void ProcessSoilTemps(NameValueCollection data, WeatherStation station)
		{
			if (data["soiltempf"] != null)
			{
				station.DoSoilTemp(ConvertTempFToUser(Convert.ToDouble(data["soiltempf"], invNum)), 1);
			}
			else
			{
				station.DoSoilTemp(null, 1);
			}

			for (var i = 2; i <= 16; i++)
			{
				if (data["soiltemp" + i + "f"] != null)
				{
					station.DoSoilTemp(ConvertTempFToUser(Convert.ToDouble(data["soiltemp" + i + "f"], invNum)), i - 1);
				}
				else
				{
					station.DoSoilTemp(null, i - 1);
				}
			}
		}

		private void ProcessSoilMoist(NameValueCollection data, WeatherStation station)
		{
			for (var i = 1; i <= 16; i++)
			{
				if (data["soilmoisture" + i] != null)
				{
					station.DoSoilMoisture((int)Convert.ToDouble(data["soilmoisture" + i], invNum), i);
				}
				else
				{
					station.DoSoilMoisture(null, i);
				}
			}
		}

		private void ProcessLeafWetness(NameValueCollection data, WeatherStation station)
		{
			if (data["leafwetness"] != null)
			{
				station.DoLeafWetness(Convert.ToInt32(data["leafwetness"], invNum), 1);
			}
			// Though Ecowitt supports up to 8 sensors, MX only supports the first 4
			for (var i = 1; i <= 8; i++)
			{
				if (data["leafwetness_ch" + i] != null)
				{
					station.DoLeafWetness(Convert.ToInt32(data["leafwetness_ch" + i], invNum), i);
				}
				else
				{
					station.DoLeafWetness(null, i);
				}
			}

		}

		private void ProcessUserTemp(NameValueCollection data, WeatherStation station)
		{
			for (var i = 1; i <= 8; i++)
			{
				if (data["tf_ch" + i] != null)
				{
					station.DoUserTemp(ConvertTempFToUser(Convert.ToDouble(data["tf_ch" + i], invNum)), i);
				}
				else
				{
					station.DoUserTemp(null, i);
				}
			}
		}

		private void ProcessAirQuality(NameValueCollection data, WeatherStation station)
		{
			// pm25_ch[1-4]
			// pm25_avg_24h_ch[1-4]

			for (var i = 1; i <= 4; i++)
			{
				var pm = data["pm25_ch" + i];
				var pmAvg = data["pm25_avg_24h_ch" + i];
				if (pm != null)
				{
					station.DoAirQuality(Convert.ToDouble(pm, invNum), i);
				}
				if (pmAvg != null)
				{
					station.DoAirQualityAvg(Convert.ToDouble(pmAvg, invNum), i);
				}
			}
		}

		private void ProcessCo2(NameValueCollection data, WeatherStation station)
		{
			// tf_co2
			// humi_co2
			// pm25_co2
			// pm25_24h_co2
			// pm10_co2
			// pm10_24h_co2
			// co2
			// co2_24h

			if (data["tf_co2"] != null)
			{
				station.CO2_temperature = ConvertTempFToUser(Convert.ToDouble(data["tf_co2"], invNum));
			}
			if (data["humi_co2"] != null)
			{
				station.CO2_humidity = Convert.ToInt32(data["humi_co2"], invNum);
			}
			if (data["pm25_co2"] != null)
			{
				station.CO2_pm2p5 = Convert.ToDouble(data["pm25_co2"], invNum);
			}
			if (data["pm25_24h_co2"] != null)
			{
				station.CO2_pm2p5_24h = Convert.ToDouble(data["pm25_24h_co2"], invNum);
			}
			if (data["pm10_co2"] != null)
			{
				station.CO2_pm10 = Convert.ToDouble(data["pm10_co2"], invNum);
			}
			if (data["pm10_24h_co2"] != null)
			{
				station.CO2_pm10_24h = Convert.ToDouble(data["pm10_24h_co2"], invNum);
			}
			if (data["co2"] != null)
			{
				station.CO2 = Convert.ToInt32(data["co2"], invNum);
			}
			if (data["co2_24h"] != null)
			{
				station.CO2_24h = Convert.ToInt32(data["co2_24h"], invNum);
			}
		}

		private void ProcessLightning(NameValueCollection data, WeatherStation station)
		{
			var dist = data["lightning"];
			var time = data["lightning_time"];
			var num = data["lightning_num"];

			if (!string.IsNullOrEmpty(dist) && !string.IsNullOrEmpty(time))
			{
				// Only set the lightning time/distance if it is newer than what we already have - the GW1000 seems to reset this value
				var valDist = Convert.ToDouble(dist, invNum);
				if (valDist != 255)
				{
					station.LightningDistance = ConvertKmtoUserUnits(valDist);
				}

				var valTime = Convert.ToDouble(time, invNum);
				// Sends a default value until the first strike is detected of 0xFFFFFFFF
				if (valTime != 0xFFFFFFFF)
				{
					var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					dtDateTime = dtDateTime.AddSeconds(valTime).ToLocalTime();

					if (dtDateTime > LightningTime)
					{
						station.LightningTime = dtDateTime;
					}
				}
			}

			if (!string.IsNullOrEmpty(num))
			{
				station.LightningStrikesToday = Convert.ToInt32(num, invNum);
			}
		}

		private void ProcessLeak(NameValueCollection data, WeatherStation station)
		{
			for (var i = 1; i <= 4; i++)
			{
				if (data["leak" + i] != null)
				{
					station.DoLeakSensor(Convert.ToInt32(data["leak" + i], invNum), i);
				}
			}
		}

		private void ProcessBatteries(NameValueCollection data)
		{
			var lowBatt = false;
			lowBatt = lowBatt || (data["wh25batt"] != null && data["wh25batt"] == "1");
			lowBatt = lowBatt || (data["wh26batt"] != null && data["wh26batt"] == "1");
			lowBatt = lowBatt || (data["wh40batt"] != null && data["wh40batt"] == "1");
			lowBatt = lowBatt || (data["wh40batt"] != null && data["wh40batt"] == "1");
			lowBatt = lowBatt || (data["wh57batt"] != null && data["wh57batt"] == "1");
			lowBatt = lowBatt || (data["wh65batt"] != null && data["wh65batt"] == "1");
			lowBatt = lowBatt || (data["wh68batt"] != null && Convert.ToDouble(data["wh68batt"], invNum) <= 1.2);
			lowBatt = lowBatt || (data["wh80batt"] != null && Convert.ToDouble(data["wh80batt"], invNum) <= 1.2);
			lowBatt = lowBatt || (data["wh90batt"] != null && Convert.ToDouble(data["wh90batt"], invNum) <= 1.2);
			for (var i = 1; i < 5; i++)
			{
				lowBatt = lowBatt || (data["batt" + i]     != null && data["batt" + i] == "1");
				lowBatt = lowBatt || (data["soilbatt" + i] != null && Convert.ToDouble(data["soilbatt" + i], invNum) <= 1.2);
				lowBatt = lowBatt || (data["pm25batt" + i] != null && data["pm25batt" + i] == "1");
				lowBatt = lowBatt || (data["leakbatt" + i] != null && data["leakbatt" + i] == "1");
				lowBatt = lowBatt || (data["tf_batt" + i]  != null && Convert.ToDouble(data["tf_batt" + i], invNum) <= 1.2);
				lowBatt = lowBatt || (data["leaf_batt" + i] != null && Convert.ToDouble(data["leaf_batt" + i], invNum) <= 1.2);
			}
			for (var i = 5; i < 9; i++)
			{
				lowBatt = lowBatt || (data["batt" + i]     != null && data["batt" + i] == "1");
				lowBatt = lowBatt || (data["soilbatt" + i] != null && Convert.ToDouble(data["soilbatt" + i], invNum) <= 1.2);
				lowBatt = lowBatt || (data["tf_batt" + i]  != null && Convert.ToDouble(data["tf_batt" + i], invNum) <= 1.2);
				lowBatt = lowBatt || (data["leaf_batt" + i] != null && Convert.ToDouble(data["leaf_batt" + i], invNum) <= 1.2);
			}

			cumulus.BatteryLowAlarm.Triggered = lowBatt;
		}

		private static void ProcessExtraDewPoint(WeatherStation station)
		{
			for (var i = 1; i <= 10; i++)
			{
				var dp = MeteoLib.DewPoint(ConvertUserTempToC(station.ExtraTemp[i]), station.ExtraHum[i]);
				station.DoExtraDP(dp, i);
			}
		}

		private static void SetCustomServer(Stations.GW1000Api api, bool main)
		{
			Cumulus.LogMessage("Reading Ecowitt Gateway Custom Server config");

			var customPath = main ? "/station/ecowitt" : "/station/ecowittextra";
			var customServer = main ? cumulus.EcowittSettings.LocalAddr : cumulus.EcowittSettings.ExtraLocalAddr;
			var customPort = cumulus.wsPort;
			var customIntv = main ? cumulus.EcowittSettings.CustomInterval : cumulus.EcowittSettings.ExtraCustomInterval;

			var data = api.DoCommand(Stations.GW1000Api.Commands.CMD_READ_CUSTOMIZED);

			// expected response
			// 0     - 0xff - header
			// 1     - 0xff - header
			// 2     - 0x2A - read customized
			// 3     - 0x?? - size of response
			// 4     - 0x?? - ID length
			// 5-len - ID field (max 40)
			// a     - 0x?? - password length
			// a1+   - Password field (max 40)
			// b     - 0x?? - server length
			// b1+   - Server field (max 64)
			// c-d   - Port Id (0-65536)
			// e-f   - Interval (5-600)
			// g     - 0x?? - Active (0-disabled, 1-active)
			// h  - 0x?? - checksum

			if (data != null)
			{
				try
				{
					//  ID field
					var id = Encoding.ASCII.GetString(data, 5, data[4]);
					// Password field
					var idx = 5 + data[4];
					var pass = Encoding.ASCII.GetString(data, idx + 1, data[idx]);
					// get server string
					idx += data[idx] + 1;
					var server = Encoding.ASCII.GetString(data, idx + 1, data[idx]);
					// get port id
					idx += data[idx] + 1;
					var port = Stations.GW1000Api.ConvertBigEndianUInt16(data, idx);
					// interval
					idx += 2;
					var intv = Stations.GW1000Api.ConvertBigEndianUInt16(data, idx);
					// type
					idx += 2;
					var type = data[idx] == 0 ? "Ecowitt" : "WUnderground";
					idx += 1;
					var active = data[idx];

					var data2 = api.DoCommand(Stations.GW1000Api.Commands.CMD_READ_USER_PATH);
					var ecPath = Encoding.ASCII.GetString(data2, 5, data2[4]);
					idx = 5 + data2[4];
					var wuPath = Encoding.ASCII.GetString(data2, idx + 1, data2[idx]);


					Cumulus.LogMessage($"Ecowitt Gateway Custom Server config: Server={server}, Port={port}, Path={ecPath}, Interval={intv}, Protocol={type}, Enabled={active}");

					if (server != customServer || port != customPort || intv != customIntv || type != "Ecowitt" || active != 1)
					{
						Cumulus.LogMessage("Ecowitt Gateway Custom Server config does not match the required config, reconfiguring it...");

						// Payload
						// 1    - ID length
						// n+   - ID
						// 1    - Password length
						// n+   - Password
						// 0	- Server length
						// 1+   - Server Name
						// a-b  - Port
						// c-d  - Interval
						// e    - Type (EC=0, WU=1)
						// f    - Active

						var length = 1 + id.Length + 1 + pass.Length; // id.len + id + pass.len + pass
						length += customServer.Length + 1; // Server name + length byte
						length += 2 + 2 + 1 + 1; // + port + interval + type + active
						var send = new byte[length];
						// set ID
						send[0] = (byte)id.Length;
						Encoding.ASCII.GetBytes(id).CopyTo(send, 1);

						// set password
						idx = 1 + id.Length;
						send[idx] = (byte)pass.Length;
						Encoding.ASCII.GetBytes(id).CopyTo(send, idx + 1);

						// set server string length
						idx += 1 + pass.Length;
						send[idx] = (byte)customServer.Length;
						// set server string
						Encoding.ASCII.GetBytes(customServer).CopyTo(send, idx + 1);
						idx += 1 + server.Length;
						// set the port id
						Stations.GW1000Api.ConvertUInt16ToLittleEndianByteArray((ushort)customPort).CopyTo(send, idx);
						// set the interval
						idx += 2;
						Stations.GW1000Api.ConvertUInt16ToLittleEndianByteArray((ushort)customIntv).CopyTo(send, idx);
						// set type
						idx += 2;
						send[idx] = 0;
						// set enabled
						idx += 1;
						send[idx] = 1;

						// do the config
						var retData = api.DoCommand(Stations.GW1000Api.Commands.CMD_WRITE_CUSTOMIZED, send);

						if (retData == null || retData[4] != 0)
						{
							Cumulus.LogMessage("Error - failed to set the Ecowitt Gateway main config");
						}
						else
						{
							Cumulus.LogMessage($"Set Ecowitt Gateway Custom Server config to: Server={customServer}, Port={customPort}, Interval={customIntv}, Protocol={0}, Enabled={1}");
						}
					}

					// does the path need setting as well?
					if (ecPath != customPath)
					{
						ecPath = customPath;
						var path = new byte[ecPath.Length + wuPath.Length + 2];
						path[0] = (byte)ecPath.Length;
						Encoding.ASCII.GetBytes(ecPath).CopyTo(path, 1);
						idx = 1 + ecPath.Length;
						path[idx] = (byte)wuPath.Length;
						Encoding.ASCII.GetBytes(wuPath).CopyTo(path, idx + 1);

						var retData = api.DoCommand(Stations.GW1000Api.Commands.CMD_WRITE_USER_PATH, path);

						if (retData == null || retData[4] != 0)
						{
							Cumulus.LogMessage("Error - failed to set the Ecowitt Gateway Path");
						}
						else
						{
							Cumulus.LogMessage($"Set Ecowitt Gateway Custom Server path={path}");
						}
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Error setting Ecowitt Gateway Custom Server config");
				}
			}
			else
			{
				Cumulus.LogMessage("Error reading Ecowitt Gateway Custom Server config, cannot configure it");
			}
		}
	}
}
