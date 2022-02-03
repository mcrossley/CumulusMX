using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack;
using ServiceStack.Text;

namespace CumulusMX
{
	internal class EcowittApi
	{
		private Cumulus cumulus;
		private WeatherStation station;
		private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;

		private static readonly HttpClientHandler httpHandler = new HttpClientHandler();
		private readonly HttpClient httpClient = new HttpClient(httpHandler);

		private static string historyUrl = "https://api.ecowitt.net/api/v3/device/history?";

		public EcowittApi(Cumulus cuml, WeatherStation stn)
		{
			cumulus = cuml;
			station = stn;

			// Configure a web proxy if required
			if (!string.IsNullOrEmpty(cumulus.HTTPProxyName))
			{
				httpHandler.Proxy = new WebProxy(cumulus.HTTPProxyName, cumulus.HTTPProxyPort);
				httpHandler.UseProxy = true;
				if (!string.IsNullOrEmpty(cumulus.HTTPProxyUser))
				{
					httpHandler.Credentials = new NetworkCredential(cumulus.HTTPProxyUser, cumulus.HTTPProxyPassword);
				}
			}

			// Let's decode the Unix ts to DateTime
			JsConfig.Init(new Config { 
				DateHandler = DateHandler.UnixTime
			});

			// override the default deserializer which returns a UTC time to return a local time
			JsConfig<DateTime>.DeSerializeFn = datetimeStr =>
			{
				if (string.IsNullOrWhiteSpace(datetimeStr))
				{
					return DateTime.MinValue;
				}

				if (long.TryParse(datetimeStr, out var date))
				{
					return Utils.FromUnixTime(date);
				}

				return DateTime.MinValue;
			};
		}


		internal bool GetHistoricData(DateTime startTime, DateTime endTime)
		{
			Cumulus.LogMessage("API.GetHistoricData: Get Ecowitt Historic Data");

			if (string.IsNullOrEmpty(cumulus.EcowittAppKey) || string.IsNullOrEmpty(cumulus.EcowittUserApiKey) || string.IsNullOrEmpty(cumulus.EcowittMacAddress))
			{
				Cumulus.LogMessage("API.GetHistoricData: Missing Ecowitt API data in the configuration, aborting!");
				cumulus.LastUpdateTime = DateTime.Now;
				return false;
			}

			var sb = new StringBuilder(historyUrl);

			sb.Append($"application_key={cumulus.EcowittAppKey}");
			sb.Append($"&api_key={cumulus.EcowittUserApiKey}");
			sb.Append($"&mac={cumulus.EcowittMacAddress}");
			sb.Append($"&start_date={startTime.ToString("yyyy-MM-dd'%20'HH:mm:ss")}");
			sb.Append($"&end_date={endTime.ToString("yyyy-MM-dd'%20'HH:mm:ss")}");

			// available callbacks:
			//	outdoor, indoor, solar_and_uvi, rainfall, wind, pressure, lightning
			//	indoor_co2, co2_aqi_combo, pm25_aqi_combo, pm10_aqi_combo, temp_and_humidity_aqi_combo
			//	pm25_ch[1-4]
			//	temp_and_humidity_ch[1-8]
			//	soil_ch[1-8]
			//	temp_ch[1-8]
			//	leaf_ch[1-8]
			//	batt
			var callbacks = new string[]
			{
				"indoor",
				"outdoor",
				"wind",
				"pressure",
				"solar_and_uvi",
				"temp_and_humidity_ch1",
				"temp_and_humidity_ch2",
				"temp_and_humidity_ch3",
				"temp_and_humidity_ch4",
				"temp_and_humidity_ch5",
				"temp_and_humidity_ch6",
				"temp_and_humidity_ch7",
				"temp_and_humidity_ch8",
				"soil_ch1",
				"soil_ch2",
				"soil_ch3",
				"soil_ch4",
				"soil_ch5",
				"soil_ch6",
				"soil_ch7",
				"soil_ch8",
				"temp_ch1",
				"temp_ch2",
				"temp_ch3",
				"temp_ch4",
				"temp_ch5",
				"temp_ch6",
				"temp_ch7",
				"temp_ch8",
				"leaf_ch1",
				"leaf_ch2",
				"leaf_ch3",
				"leaf_ch4",
				"leaf_ch5",
				"leaf_ch6",
				"leaf_ch7",
				"leaf_ch8",
				"indoor_co2",
				"co2_aqi_combo",
				"pm25_ch1",
				"pm25_ch2",
				"pm25_ch3",
				"pm25_ch4",
				"lightning"
			};

			sb.Append("&call_back=");
			foreach (var cb in callbacks)
				sb.Append(cb + ",");
			sb.Length--;

			//TODO: match time to logging interval
			// available times 5min, 30min, 4hour, 1day
			sb.Append($"&cycle_type=5min");

			var url = sb.ToString();

			var logUrl = url.Replace(cumulus.EcowittAppKey, "<<App-key>>").Replace(cumulus.EcowittUserApiKey, "<<User-key>>");
			cumulus.LogDebugMessage($"Ecowitt URL = {logUrl}");

			Cumulus.LogConsoleMessage($"Processing history data from {startTime.ToString("yyyy-MM-dd HH:mm")} to {endTime.ToString("yyyy-MM-dd HH:mm")}...");

			var histObj = new EcowittHistoricResp();
			try
			{
				string responseBody;
				int responseCode;
				int retries = 3;
				bool success = false;
				do
				{
					// we want to do this synchronously, so .Result
					using (HttpResponseMessage response = httpClient.GetAsync(url).Result)
					{
						responseBody = response.Content.ReadAsStringAsync().Result;
						responseCode = (int)response.StatusCode;
						cumulus.LogDebugMessage($"API.GetHistoricData: Ecowitt API Historic Response code: {responseCode}");
						cumulus.LogDataMessage($"API.GetHistoricData: Ecowitt API Historic Response: {responseBody}");
					}

					if (responseCode != 200)
					{
						var historyError = responseBody.FromJson<EcowitHistErrorResp>();
						Cumulus.LogMessage($"API.GetHistoricData: Ecowitt API Historic Error: {historyError.code}, {historyError.msg}");
						Cumulus.LogConsoleMessage($" - Error {historyError.code}: {historyError.msg}", ConsoleColor.Red);
						cumulus.LastUpdateTime = endTime;
						return false;
					}


					if (responseBody == "{}")
					{
						Cumulus.LogMessage("API.GetHistoricData: Ecowitt API Historic: No data was returned.");
						Cumulus.LogConsoleMessage(" - No historic data available");
						cumulus.LastUpdateTime = endTime;
						return false;
					}
					else if (responseBody.StartsWith("{\"code\":")) // sanity check
					{
						// get the sensor data
						histObj = responseBody.FromJson<EcowittHistoricResp>();

						if (histObj != null) 
						{
							// success
							if (histObj.code == 0)
							{
								try
								{
									if (histObj.data != null)
									{
										success = true;
									}
									else
									{
										// There was no data returned.
										return false;
									}
								}
								catch (Exception ex)
								{
									cumulus.LogExceptionMessage(ex, "API.GetHistoricData: Error decoding the response");
									return false;
								}
							}
							else if (histObj.code == -1 || histObj.code == 45001)
							{
								// -1 = system busy, 45001 = rate limited

								// have we reached the retry limit?
								if (--retries <= 0)
									return false;

								Cumulus.LogMessage("API.GetHistoricData: System Busy or Rate Limited, waiting before retry...");
								System.Threading.Thread.Sleep(1500);
							}
							else
							{
								return false;
							}
						}
						else
						{
							return false;
						}

					}
					else // No idea what we got, dump it to the log
					{
						Cumulus.LogMessage("API.GetHistoricData: Invalid historic message received");
						cumulus.LogDataMessage("API.GetHistoricData: Received: " + responseBody);
						cumulus.LastUpdateTime = endTime;
						return false;
					}

					//
				} while (!success);

				ProcessHistoryData(histObj.data);

				return true;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "API.GetHistoricData: Exception");
				cumulus.LastUpdateTime = endTime;
				return false;
			}

		}

		private void ProcessHistoryData(Dictionary<string,string> data)
		{
			// allocate a dictionary of data objects, keyed on the timestamp
			var buffer = new SortedDictionary<DateTime, HistoricData>();

			// process each sensor type, and store them for adding to the system later
			foreach (var entry in data)
			{
				try
				{
					switch (entry.Key)
					{
						// Indoor Data
						case "indoor":
							var indoor = entry.Value.FromJsv<EcowittHistoricTempHum>();

							// do the temperature
							if (indoor.temperature.list != null)
							{
								foreach (var item in indoor.temperature.list)
								{
									// not present value = 140
									if (!item.Value.HasValue || item.Value == 140 || item.Key <= cumulus.LastUpdateTime)
										continue;

									var newItem = new HistoricData();
									newItem.IndoorTemp = item.Value;
									buffer.Add(item.Key, newItem);
								}
							}
							// do the humidity
							if (indoor.humidity.list != null)
							{
								foreach (var item in indoor.humidity.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].IndoorHum = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.IndoorHum = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// Outdoor Data
						case "outdoor":
							var outdoor = entry.Value.FromJsv<EcowittHistoricOutdoor>();

							// Temperature
							if (outdoor.temperature.list != null)
							{
								foreach (var item in outdoor.temperature.list)
								{
									// not present value = 140
									if (!item.Value.HasValue || item.Value == 140 || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].Temp = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.Temp = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}

							// Humidity
							if (outdoor.humidity.list != null)
							{
								foreach (var item in outdoor.humidity.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].Humidity = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.Humidity = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							// Dewpoint
							if (outdoor.dew_point.list != null)
							{
								foreach (var item in outdoor.dew_point.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].DewPoint = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.DewPoint = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// Wind Data
						case "wind":
							var wind = entry.Value.FromJsv<EcowittHistoricDataWind>();

							// Speed
							if (wind.wind_speed.list != null)
							{
								foreach (var item in wind.wind_speed.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].WindSpd = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.WindSpd = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}

							// Gust
							if (wind.wind_gust.list != null)
							{
								foreach (var item in wind.wind_gust.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].WindGust = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.WindGust = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							// Direction
							if (wind.wind_direction.list != null)
							{
								foreach (var item in wind.wind_direction.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].WindDir = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.WindDir = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// Pressure Data
						case "pressure":
							var pressure = entry.Value.FromJsv<EcowittHistoricDataPressure>();
							// relative
							if (pressure.relative.list != null)
							{
								foreach (var item in pressure.relative.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].Pressure = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.Pressure = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// Solar Data
						case "solar_and_uvi":
							var solar = entry.Value.FromJsv<EcowittHistoricDataSolar>();

							// solar
							if (solar.solar.list != null)
							{
								foreach (var item in solar.solar.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].Solar = (int)item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.Solar = (int)item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							// uvi
							if (solar.uvi.list != null)
							{
								foreach (var item in solar.uvi.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].UVI = (int)item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.UVI = (int)item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// ----- Extra multi-channel sensors-----

						// Extra Temp/Hum Data
						case "temp_and_humidity_ch1":
						case "temp_and_humidity_ch2":
						case "temp_and_humidity_ch3":
						case "temp_and_humidity_ch4":
						case "temp_and_humidity_ch5":
						case "temp_and_humidity_ch6":
						case "temp_and_humidity_ch7":
						case "temp_and_humidity_ch8":
							var tandh = entry.Value.FromJsv<EcowittHistoricTempHum>();
							int chan = entry.Key[-1];

							// temperature
							if (tandh.temperature.list != null)
							{
								foreach (var item in tandh.temperature.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].ExtraTemp[chan] = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.ExtraTemp[chan] = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							// humidity
							if (tandh.humidity.list != null)
							{
								foreach (var item in tandh.humidity.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].ExtraHumidity[chan] = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.ExtraHumidity[chan] = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// Extra Soil Moisture Data
						case "soil_ch1":
						case "soil_ch2":
						case "soil_ch3":
						case "soil_ch4":
						case "soil_ch5":
						case "soil_ch6":
						case "soil_ch7":
						case "soil_ch8":
							var soilm = entry.Value.FromJsv<EcowittHistoricDataSoil>();
							chan = entry.Key[-1];

							if (soilm.soilmoisture.list != null)
							{
								// moisture
								foreach (var item in soilm.soilmoisture.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].SoilMoist[chan] = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.SoilMoist[chan] = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// User Temp Data
						case "temp_ch1":
						case "temp_ch2":
						case "temp_ch3":
						case "temp_ch4":
						case "temp_ch5":
						case "temp_ch6":
						case "temp_ch7":
						case "temp_ch8":
							var usert = entry.Value.FromJsv<EcowittHistoricDataTemp>();
							chan = entry.Key[-1];

							if (usert.temperature.list != null)
							{
								// temperature
								foreach (var item in usert.temperature.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].UserTemp[chan] = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.UserTemp[chan] = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// Leaf Wetness Data
						case "leaf_ch1":
						case "leaf_ch2":
						case "leaf_ch3":
						case "leaf_ch4":
						case "leaf_ch5":
						case "leaf_ch6":
						case "leaf_ch7":
						case "leaf_ch8":
							var leaf = entry.Value.FromJsv<EcowittHistoricDataLeaf>();
							chan = entry.Key[-1];

							if (leaf.leaf_wetness.list != null)
							{
								// wetness
								foreach (var item in leaf.leaf_wetness.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].LeafWetness[chan] = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.LeafWetness[chan] = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// 4 channel PM 2.5 sensors
						case "pm25_ch1":
						case "pm25_ch2":
						case "pm25_ch3":
						case "pm25_ch4":
							var pm25 = entry.Value.FromJsv<EcowittHistoricDataPm25Aqi>();
							chan = entry.Key[-1];

							if (pm25.pm25.list != null)
							{
								foreach (var item in pm25.pm25.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].pm25[chan] = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.pm25[chan] = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}

							}
							continue;

						// Indoor CO2
						case "indoor_co2":
							var indoorCo2 = entry.Value.FromJsv<EcowittHistoricDataCo2>();

							// CO2
							if (indoorCo2.co2.list != null)
							{
								foreach (var item in indoorCo2.co2.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].IndoorCo2 = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.IndoorCo2 = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							// 24 Avg
							if (indoorCo2.average24h.list != null)
							{
								foreach (var item in indoorCo2.average24h.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].IndoorCo2hr24 = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.IndoorCo2hr24 = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// CO2 Combi
						case "co2_aqi_combo":
							var co2combo = entry.Value.FromJsv<EcowittHistoricDataCo2>();

							// CO2
							if (co2combo.co2.list != null)
							{
								foreach (var item in co2combo.co2.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].CO2pm2p5 = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.CO2pm2p5 = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							// 24 Avg
							if (co2combo.average24h.list != null)
							{
								foreach (var item in co2combo.average24h.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].CO2pm2p5hr24 = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.CO2pm2p5hr24 = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// pm2.5 Combi
						case "pm25_aqi_combo":
							var pm25combo = entry.Value.FromJsv<EcowittHistoricDataPm25Aqi>();

							if (pm25combo.pm25.list != null)
							{
								foreach (var item in pm25combo.pm25.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].AqiComboPm25 = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.AqiComboPm25 = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// pm10 Combi
						case "pm10_aqi_combo":
							var pm10combo = entry.Value.FromJsv<EcowittHistoricDataPm10Aqi>();

							if (pm10combo.pm10.list != null)
							{
								foreach (var item in pm10combo.pm10.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].AqiComboPm10 = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.AqiComboPm10 = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;

						// Lightning Data
						case "lightning":
							var lightning = entry.Value.FromJsv<EcowittHistoricDataLightning>();

							// Distance
							if (lightning.distance.list != null)
							{
								foreach (var item in lightning.distance.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].LightningDist = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.LightningDist = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							// Strikes
							if (lightning.count.list != null)
							{
								foreach (var item in lightning.count.list)
								{
									if (!item.Value.HasValue || item.Key <= cumulus.LastUpdateTime)
										continue;

									if (buffer.ContainsKey(item.Key))
									{
										buffer[item.Key].LightningCount = item.Value;
									}
									else
									{
										var newItem = new HistoricData();
										newItem.LightningCount = item.Value;
										buffer.Add(item.Key, newItem);
									}
								}
							}
							continue;


						default:
							if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Key != "]")
								Cumulus.LogMessage($"ProcessHistoryData: Unknown sensor type found [{entry.Key}]");
							break;
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessHistoryData: Error parsing sensor " + entry.Key);
				}
			}
			// now we have all the data for this period, for each record create the string expected by ProcessData and get it processed
			var rollHour = Math.Abs(cumulus.GetHourInc());
			var luhour = cumulus.LastUpdateTime.Hour;
			var rolloverdone = luhour == rollHour;
			var midnightraindone = luhour == 0;

			foreach (var rec in buffer)
			{
				Cumulus.LogMessage("Processing data for " + rec.Key);

				var h = rec.Key.Hour;

				//  if outside rollover hour, rollover yet to be done
				if (h != rollHour) rolloverdone = false;

				// In rollover hour and rollover not yet done
				if (h == rollHour && !rolloverdone)
				{
					// do rollover
					Cumulus.LogMessage("Day rollover " + rec.Key.ToShortTimeString());
					station.DayReset(rec.Key);

					rolloverdone = true;
				}

				// Not in midnight hour, midnight rain yet to be done
				if (h != 0) midnightraindone = false;

				// In midnight hour and midnight rain (and sun) not yet done
				if (h == 0 && !midnightraindone)
				{
					station.ResetMidnightRain(rec.Key);
					station.ResetSunshineHours();
					midnightraindone = true;
				}

				// finally apply this data
				ApplyHistoricData(rec);

				// add in archive period worth of sunshine, if sunny
				if (station.SolarRad > station.CurrentSolarMax * cumulus.SolarOptions.SunThreshold / 100 &&
					station.SolarRad >= cumulus.SolarOptions.SolarMinimum)
					station.SunshineHours += 5 / 60.0;



				// add in 'following interval' minutes worth of wind speed to windrun
				Cumulus.LogMessage("Windrun: " + station.WindAverage.Value.ToString(cumulus.WindFormat) + cumulus.Units.WindText + " for " + 5 + " minutes = " +
								   (station.WindAverage.Value * station.WindRunHourMult[cumulus.Units.Wind] * 5 / 60.0).ToString(cumulus.WindRunFormat) + cumulus.Units.WindRunText);

				station.WindRunToday += station.WindAverage.Value * station.WindRunHourMult[cumulus.Units.Wind] * 5 / 60.0;

				// update heating/cooling degree days
				station.UpdateDegreeDays(5);

				// update dominant wind bearing
				station.CalculateDominantWindBearing(station.Bearing, station.WindAverage, 5);

				station.CheckForWindrunHighLow(rec.Key);

				//bw?.ReportProgress((totalentries - datalist.Count) * 100 / totalentries, "processing");

				//UpdateDatabase(timestamp.ToUniversalTime(), historydata.interval, false);

				_ = cumulus.DoLogFile(rec.Key, false);

				if (cumulus.StationOptions.LogExtraSensors)
					_ = cumulus.DoExtraLogFile(rec.Key);

				//AddRecentDataEntry(timestamp, WindAverage, RecentMaxGust, WindLatest, Bearing, AvgBearing,
				//    OutdoorTemperature, WindChill, OutdoorDewpoint, HeatIndex,
				//    OutdoorHumidity, Pressure, RainToday, SolarRad, UV, Raincounter, FeelsLike, Humidex);

				station.AddRecentDataWithAq(rec.Key, station.WindAverage, station.RecentMaxGust, station.WindLatest, station.Bearing, station.AvgBearing, station.Temperature, station.WindChill, station.Dewpoint, station.HeatIndex,
					station.Humidity, station.Pressure, station.RainToday, station.SolarRad, station.UV, station.Raincounter, station.FeelsLike, station.Humidex, station.ApparentTemp, station.IndoorTemp, station.IndoorHum, station.CurrentSolarMax, station.RainRate);

				if (cumulus.StationOptions.CalculatedET && rec.Key.Minute == 0)
				{
					// Start of a new hour, and we want to calculate ET in Cumulus
					station.CalculateEvaoptranspiration(rec.Key);
				}

				station.DoTrendValues(rec.Key);
				station.UpdatePressureTrendString();
				station.UpdateStatusPanel(rec.Key);
				cumulus.AddToWebServiceLists(rec.Key);

			}
		}

		private void ApplyHistoricData(KeyValuePair<DateTime, HistoricData> rec)
		{
			// === Wind ==
			try
			{
				var gustVal = station.ConvertWindMPHToUser(rec.Value.WindGust);
				var spdVal = station.ConvertWindMPHToUser(rec.Value.WindSpd);
				var dirVal = rec.Value.WindDir;

				// The protocol does not provide an average value
				// so feed in current MX average
				station.DoWind(spdVal, dirVal, (station.WindAverage ?? 0) / cumulus.Calib.WindSpeed.Mult, rec.Key);

				if (rec.Value.WindGust.HasValue && rec.Value.WindDir.HasValue)
				{
					var gustLastCal = gustVal * cumulus.Calib.WindGust.Mult;
					if (gustLastCal > station.RecentMaxGust)
					{
						cumulus.LogDebugMessage("Setting max gust from current value: " + gustLastCal.Value.ToString(cumulus.WindFormat));
						station.CheckHighGust(gustLastCal, dirVal, rec.Key);

						// add to recent values so normal calculation includes this value
						station.WindRecent[station.nextwind].Gust = gustVal.Value; // use uncalibrated value
						station.WindRecent[station.nextwind].Speed = station.WindAverage.Value / cumulus.Calib.WindSpeed.Mult;
						station.WindRecent[station.nextwind].Timestamp = rec.Key;
						station.nextwind = (station.nextwind + 1) % WeatherStation.MaxWindRecent;

						station.RecentMaxGust = gustLastCal;
					}
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Wind data");
			}

			// === Humidity ===
			try
			{
				station.DoIndoorHumidity(rec.Value.IndoorHum);
				station.DoHumidity(rec.Value.Humidity, rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Humidity data");
			}

			// === Pressure ===
			try
			{
				var pressVal = station.ConvertPressINHGToUser(rec.Value.Pressure);
				station.DoPressure(pressVal, rec.Key);
				station.UpdatePressureTrendString();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Pressure data");
			}

			// === Indoor temp ===
			try
			{
				var tempVal = station.ConvertTempFToUser(rec.Value.IndoorTemp);
				station.DoIndoorTemp(tempVal);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Indoor temp data");
			}

			// === Outdoor temp ===
			try
			{
				var tempVal = station.ConvertTempFToUser(rec.Value.Temp);
				station.DoTemperature(tempVal, rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Outdoor temp data");
			}

			// === Rain ===
			try
			{
				double rRate = 0;
				if (rec.Value.RainRate.HasValue)
				{
					// we have a rain rate, so we will NOT calculate it
					station.calculaterainrate = false;
					rRate = (double)rec.Value.RainRate;
				}
				else
				{
					// No rain rate, so we will calculate it
					station.calculaterainrate = true;
				}

				if (rec.Value.RainYear.HasValue)
				{
					var rainVal = station.ConvertRainINToUser(rec.Value.RainYear.Value);
					var rateVal = station.ConvertRainINToUser(rRate);
					station.DoRain(rainVal, rateVal, rec.Key);
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Rain data");
			}

			// === Dewpoint ===
			try
			{
				var val = station.ConvertTempFToUser(rec.Value.DewPoint);
				station.DoDewpoint(val, rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Dew point data");
			}

			// === Wind Chill ===
			try
			{
				if (cumulus.StationOptions.CalculatedWC)
				{
					station.DoWindChill(0, rec.Key);
				}
				else
				{
					// historic API does not provide Wind Chill so force calculation
					cumulus.StationOptions.CalculatedWC = true;
					station.DoWindChill(0, rec.Key);
					cumulus.StationOptions.CalculatedWC = false;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Dew point data");
			}

			// === Humidex ===
			try
			{
				station.DoHumidex(rec.Key);

				// === Apparent & Feels Like ===
				station.DoApparentTemp(rec.Key);
				station.DoFeelsLike(rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Humidex/Apparant/Feels Like");
			}

			// === Solar ===
			try
			{
				station.DoSolarRad(rec.Value.Solar.Value, rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Solar data");
			}

			// === UVI ===
			try
			{
				station.DoUV(rec.Value.UVI, rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Solar data");
			}

			// === Extra Sensors ===
			for (var i = 1; i <= 8; i++)
			{
				// === Extra Temperature ===
				try
				{
					station.DoExtraTemp(station.ConvertTempFToUser(rec.Value.ExtraTemp[i - 1]), i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra temperature data");
				}
				// === Extra Humidity ===
				try
				{
					station.DoExtraHum(rec.Value.ExtraHumidity[i - 1], i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra humidity data");
				}


				// === User Temperature ===
				try
				{
					station.DoUserTemp(station.ConvertTempFToUser(rec.Value.UserTemp[i - 1]), i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra user temperature data");
				}

				// === Soil Moisture ===
				try
				{
					station.DoSoilMoisture(rec.Value.SoilMoist[i - 1], i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in soil moisture data");
				}
			}

			// === Indoor CO2 ===
			try
			{
				station.CO2_pm2p5 = rec.Value.IndoorCo2;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in CO2 data");
			}

			// === Indoor CO2 24hr avg ===
			try
			{
				station.CO2_pm2p5_24h = rec.Value.CO2pm2p5hr24;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in CO2 24hr avg data");
			}

			// === PM 2.5 Combo
			try
			{
				station.CO2_pm2p5 = rec.Value.AqiComboPm25;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in AQI Combo pm2.5 data");
			}

			// === PM 10 Combo
			try
			{
				station.CO2_pm10 = rec.Value.AqiComboPm10;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in AQI Combo pm10 data");
			}

			// === 4 channel pm 2.5 ===
			for (var i = 1; i <= 4; i++)
			{
				try
				{
					station.DoAirQuality(rec.Value.pm25[i - 1], i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra temperature data ");
				}
			}
		}



		private string ErrorCode(int code)
		{
			switch (code)
			{
				case -1: return "System is busy";
				case 0: return "Success!";
				case 40000: return "Illegal parameter";
				case 40010: return "Illegal Application_Key Parameter";
				case 40011: return "Illegal Api_Key Parameter";
				case 40012: return "Illegal MAC/IMEI Parameter";
				case 40013: return "Illegal start_date Parameter";
				case 40014: return "Illegal end_date Parameter";
				case 40015: return "Illegal cycle_type Parameter";
				case 40016: return "Illegal call_back Parameter";
				case 40017: return "Missing Application_Key Parameter";
				case 40018: return "Missing Api_Key Parameter";
				case 40019: return "Missing MAC Parameter";
				case 40020: return "Missing start_date Parameter";
				case 40021: return "Missing end_date Parameter";
				case 40022: return "Illegal Voucher type";
				case 43001: return "Needs other service support";
				case 44001: return "Media file or data packet is null";
				case 45001: return "Over the limit or other error";
				case 46001: return "No existing request";
				case 47001: return "Parse JSON/XML contents error";
				case 48001: return "Privilege Problem";
				default: return "Unknown error code";
			}
		}

		private class EcowitHistErrorResp
		{
			public int code { get; set; }
			public string msg { get; set; }
			public DateTime time { get; set; }
			public object data { get; set; }
		}

		internal class EcowittHistoricResp
		{
			public int code { get; set; }
			public string msg { get; set; }
			public DateTime time { get; set; }
			//public EcowittHistoricData data { get; set; }
			public Dictionary<string, string> data { get; set; }
		}



		/*
		internal class EcowittHistoricData
		{
			public EcowittHistoricTempHum indoor { get; set; }
			public EcowittHistoricDataPressure pressure { get; set; }
			public EcowittHistoricOutdoor outdoor { get; set; }
			public EcowittHistoricDataWind wind { get; set; }
			public EcowittHistoricDataSolar solar_and_uvi { get; set; }
			public EcowittHistoricDataRainfall rainfall { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch1 { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch2 { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch3 { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch4 { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch5 { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch6 { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch7 { get; set; }
			public EcowittHistoricTempHum temp_and_humidity_ch8 { get; set; }
			public EcowittHistoricDataSoil soil_ch1 { get; set; }
			public EcowittHistoricDataSoil soil_ch2 { get; set; }
			public EcowittHistoricDataSoil soil_ch3 { get; set; }
			public EcowittHistoricDataSoil soil_ch4 { get; set; }
			public EcowittHistoricDataSoil soil_ch5 { get; set; }
			public EcowittHistoricDataSoil soil_ch6 { get; set; }
			public EcowittHistoricDataSoil soil_ch7 { get; set; }
			public EcowittHistoricDataSoil soil_ch8 { get; set; }
			public EcowittHistoricDataTemp temp_ch1 { get; set; }
			public EcowittHistoricDataTemp temp_ch2 { get; set; }
			public EcowittHistoricDataTemp temp_ch3 { get; set; }
			public EcowittHistoricDataTemp temp_ch4 { get; set; }
			public EcowittHistoricDataTemp temp_ch5 { get; set; }
			public EcowittHistoricDataTemp temp_ch6 { get; set; }
			public EcowittHistoricDataTemp temp_ch7 { get; set; }
			public EcowittHistoricDataTemp temp_ch8 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch1 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch2 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch3 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch4 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch5 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch6 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch7 { get; set; }
			public EcowittHistoricDataLeaf leaf_ch8 { get; set; }
			public EcowittHistoricDataLightning lightning { get; set; }
			public EcowittHistoricDataCo2 indoor_co2 { get; set; }
			public EcowittHistoricDataCo2 co2_aqi_combo { get; set; }
			public EcowittHistoricDataPm25Aqi pm25_aqi_combo { get; set; }
			public EcowittHistoricDataPm10Aqi pm10_aqi_combo { get; set; }
			public EcowittHistoricDataPm25Aqi pm25_ch1 { get; set; }
			public EcowittHistoricDataPm25Aqi pm25_ch2 { get; set; }
			public EcowittHistoricDataPm25Aqi pm25_ch3 { get; set; }
			public EcowittHistoricDataPm25Aqi pm25_ch4 { get; set; }

		}
		*/

		internal class EcowittHistoricDataTypeInt
		{
			public string unit { get; set; }
			public Dictionary<DateTime, int?> list { get; set; }
		}

		internal class EcowittHistoricDataTypeDbl
		{
			public string unit { get; set; }
			public Dictionary<DateTime, double?> list { get; set; }
		}

		internal class EcowittHistoricTempHum
		{
			public EcowittHistoricDataTypeDbl temperature { get; set; }
			public EcowittHistoricDataTypeInt humidity { get; set; }
		}

		internal class EcowittHistoricOutdoor : EcowittHistoricTempHum
		{
			public EcowittHistoricDataTypeDbl dew_point { get; set; }
		}

		internal class EcowittHistoricDataPressure
		{
			public EcowittHistoricDataTypeDbl relative { get; set; }
		}

		internal class EcowittHistoricDataWind
		{
			public EcowittHistoricDataTypeInt wind_direction { get; set; }
			public EcowittHistoricDataTypeDbl wind_speed { get; set; }
			public EcowittHistoricDataTypeDbl wind_gust { get; set; }
		}

		internal class EcowittHistoricDataSolar
		{
			public EcowittHistoricDataTypeDbl solar { get; set; }
			public EcowittHistoricDataTypeDbl uvi { get; set; }
		}

		internal class EcowittHistoricDataRainfall
		{
			public EcowittHistoricDataTypeDbl rain_rate { get; set; }
			public EcowittHistoricDataTypeDbl yearly { get; set; }
		}

		internal class EcowittHistoricDataSoil
		{
			public EcowittHistoricDataTypeInt soilmoisture { get; set; }
		}
		internal class EcowittHistoricDataTemp
		{
			public EcowittHistoricDataTypeDbl temperature { get; set; }
		}

		internal class EcowittHistoricDataLeaf
		{
			public EcowittHistoricDataTypeInt leaf_wetness { get; set; }
		}

		internal class EcowittHistoricDataLightning
		{
			public EcowittHistoricDataTypeDbl distance { get; set; }
			public EcowittHistoricDataTypeInt count	{ get; set; }	
		}

		[DataContract]
		internal class EcowittHistoricDataCo2
		{
			public EcowittHistoricDataTypeInt co2 { get; set; }
			[DataMember(Name= "24_hours_average")]
			public EcowittHistoricDataTypeInt average24h { get; set; }
		}

		internal class EcowittHistoricDataPm25Aqi
		{
			public EcowittHistoricDataTypeDbl pm25 { get; set; }
		}

		internal class EcowittHistoricDataPm10Aqi
		{
			public EcowittHistoricDataTypeDbl pm10 { get; set; }
		}

		internal class HistoricData
		{
			public double? IndoorTemp { get; set; }
			public int? IndoorHum { get; set; }
			public double? Temp { get; set; }
			public double? DewPoint { get; set; }
			public double? FeelsLike { get; set; }
			public int? Humidity { get; set; }
			public double? RainRate { get; set; }
			public double? RainYear { get; set; }
			public double? WindSpd { get; set; }
			public double? WindGust { get; set; }
			public int? WindDir { get; set; }
			public double? Pressure { get; set; }
			public int? Solar { get; set; }
			public double? UVI { get; set; }
			public double? LightningDist { get; set; }
			public int? LightningCount { get; set; }
			public double?[] ExtraTemp { get; set; }
			public int?[] ExtraHumidity { get; set; }
			public int?[] SoilMoist { get; set; }
			public double?[] UserTemp { get; set; }
			public int?[] LeafWetness { get; set; }
			public double?[] pm25 { get; set; }
			public double? AqiComboPm25 { get; set; }
			public double? AqiComboPm10 { get; set; }
			public double? AqiComboTemp { get; set; }
			public int? AqiComboHum { get; set; }
			public int? CO2pm2p5 { get; set; }
			public int? CO2pm2p5hr24 { get; set; }
			public int? IndoorCo2 { get; set; }
			public int? IndoorCo2hr24 { get; set; }

			public HistoricData()
			{
				pm25 = new double?[4];
				ExtraTemp = new double?[8];
				ExtraHumidity = new int?[8];
				SoilMoist = new int?[8];
				UserTemp = new double?[8];
				LeafWetness = new int?[8];
			}
		}

	}
}
