using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Text;

namespace CumulusMX
{
	internal class EcowittApi
	{
		private readonly Cumulus cumulus;
		private readonly WeatherStation station;
		//private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;

		private static readonly HttpClientHandler httpHandler = new HttpClientHandler();
		private readonly HttpClient httpClient = new HttpClient(httpHandler);

		private static readonly string historyUrl = "https://api.ecowitt.net/api/v3/device/history?";

		private static readonly int EcowittApiFudgeFactor = 5; // Number of minutes that Ecowitt API data is delayed

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


		internal bool GetHistoricData(DateTime startTime, DateTime endTime, CancellationToken token)
		{
			Cumulus.LogMessage("API.GetHistoricData: Get Ecowitt Historic Data");

			if (string.IsNullOrEmpty(cumulus.EcowittSettings.AppKey) || string.IsNullOrEmpty(cumulus.EcowittSettings.UserApiKey) || string.IsNullOrEmpty(cumulus.EcowittSettings.MacAddress))
			{
				Cumulus.LogMessage("API.GetHistoricData: Missing Ecowitt API data in the configuration, aborting!");
				cumulus.LastUpdateTime = DateTime.Now;
				return false;
			}

			var apiStartDate = startTime.AddMinutes(-EcowittApiFudgeFactor);
			var apiEndDate = endTime;

			var sb = new StringBuilder(historyUrl);

			sb.Append($"application_key={cumulus.EcowittSettings.AppKey}");
			sb.Append($"&api_key={cumulus.EcowittSettings.UserApiKey}");
			sb.Append($"&mac={cumulus.EcowittSettings.MacAddress}");
			sb.Append($"&start_date={apiStartDate.ToString("yyyy-MM-dd'%20'HH:mm:ss")}");
			sb.Append($"&end_date={apiEndDate.ToString("yyyy-MM-dd'%20'HH:mm:ss")}");

			// Request the data in the correct units
			sb.Append($"&temp_unitid={cumulus.Units.Temp + 1}"); // 1=C, 2=F
			sb.Append($"&pressure_unitid={(cumulus.Units.Press == 2 ? "4" : "3")}"); // 3=hPa, 4=inHg, 5=mmHg
			var windUnit = cumulus.Units.Wind switch
			{
				// m/s
				0 => "6",
				// mph
				1 => "9",
				// km/h
				2 => "7",
				// knots
				3 => "8",
				_ => "?",
			};
			sb.Append($"&wind_speed_unitid={windUnit}");
			sb.Append($"&rainfall_unitid={cumulus.Units.Rain + 12}");

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
				"rainfall",
				"rainfall_piezo",
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

			var msg = $"Processing history data from {startTime:yyyy-MM-dd HH:mm} to {endTime.AddMinutes(5):yyyy-MM-dd HH:mm}...";
			Cumulus.LogMessage($"API.GetHistoricData: " + msg);
			Cumulus.LogConsoleMessage(msg);

			var logUrl = url.Replace(cumulus.EcowittSettings.AppKey, "<<App-key>>").Replace(cumulus.EcowittSettings.UserApiKey, "<<User-key>>");
			cumulus.LogDebugMessage($"Ecowitt URL = {logUrl}");

			EcowittHistoricResp histObj;
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
										cumulus.LastUpdateTime = endTime;
										return false;
									}
								}
								catch (Exception ex)
								{
									cumulus.LogExceptionMessage(ex, "API.GetHistoricData: Error decoding the response");
									cumulus.LastUpdateTime = endTime;
									return false;
								}
							}
							else if (histObj.code == -1 || histObj.code == 45001)
							{
								// -1 = system busy, 45001 = rate limited

								// have we reached the retry limit?
								if (--retries <= 0)
								{
									cumulus.LastUpdateTime = endTime;
									return false;
								}

								Cumulus.LogMessage("API.GetHistoricData: System Busy or Rate Limited, waiting 5 seconds before retry...");
								Task.Delay(5000, token).Wait();
							}
							else
							{
								cumulus.LastUpdateTime = endTime;
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

				if (histObj.data.Count == 0)
				{
					// no data for this period, skip the last update time to the end of the period
					cumulus.LastUpdateTime = endTime;
				}
				else if (!token.IsCancellationRequested)
				{
					ProcessHistoryData(histObj.data, token);
				}

				return true;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "API.GetHistoricData: Exception");
				cumulus.LastUpdateTime = endTime;
				return false;
			}

		}

		private void ProcessHistoryData(Dictionary<string,string> data, CancellationToken token)
		{
			// allocate a dictionary of data objects, keyed on the timestamp
			var buffer = new SortedDictionary<DateTime, HistoricData>();

			// process each sensor type, and store them for adding to the system later
			foreach (var entry in data)
			{
				try
				{
					int chan = 0;

					switch (entry.Key)
					{
						// Indoor Data
						case "indoor":
							try
							{
								var indoor = entry.Value.FromJsv<EcowittHistoricTempHum>();

								// do the temperature
								if (indoor.temperature != null && indoor.temperature.list != null)
								{
									foreach (var item in indoor.temperature.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										// not present value = 140
										if (!item.Value.HasValue || item.Value == 140 || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].IndoorTemp = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												IndoorTemp = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
								// do the humidity
								if (indoor.humidity != null && indoor.humidity.list != null)
								{
									foreach (var item in indoor.humidity.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].IndoorHum = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												IndoorHum = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing indoor data. Exception");
							}
							continue;

						// Outdoor Data
						case "outdoor":
							try
							{
								var outdoor = entry.Value.FromJsv<EcowittHistoricOutdoor>();

								// Temperature
								if (outdoor.temperature != null && outdoor.temperature.list != null)
								{
									foreach (var item in outdoor.temperature.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										// not present value = 140
										if (!item.Value.HasValue || item.Value == 140 || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].Temp = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												Temp = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// Humidity
								if (outdoor.humidity != null && outdoor.humidity.list != null)
								{
									foreach (var item in outdoor.humidity.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].Humidity = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												Humidity = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// Dewpoint
								if (outdoor.dew_point != null && outdoor.dew_point.list != null)
								{
									foreach (var item in outdoor.dew_point.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].DewPoint = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												DewPoint = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing outdoor data. Exception");
							}
							continue;

						// Wind Data
						case "wind":
							try
							{
								var wind = entry.Value.FromJsv<EcowittHistoricDataWind>();

								// Speed
								if (wind.wind_speed != null && wind.wind_speed.list != null)
								{
									foreach (var item in wind.wind_speed.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].WindSpd = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												WindSpd = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// Gust
								if (wind.wind_gust != null && wind.wind_gust.list != null)
								{
									foreach (var item in wind.wind_gust.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].WindGust = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												WindGust = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// Direction
								if (wind.wind_direction != null && wind.wind_direction.list != null)
								{
									foreach (var item in wind.wind_direction.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].WindDir = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												WindDir = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing wind data. Exception");
							}
							continue;

						// Pressure Data
						case "pressure":
							try
							{
								var pressure = entry.Value.FromJsv<EcowittHistoricDataPressure>();

								// relative
								if (pressure.relative != null && pressure.relative.list != null)
								{
									foreach (var item in pressure.relative.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].Pressure = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												Pressure = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing pressure data. Exception");
							}
							continue;

						// tipping bucket rainfall
						case "rainfall":
							try
							{
								if (cumulus.Gw1000PrimaryRainSensor == 0)
								{
									var rain = entry.Value.FromJsv<EcowittHistoricDataRainfall>();

									// rain rate
									if (rain.rain_rate != null && rain.rain_rate.list != null)
									{
										foreach (var item in rain.rain_rate.list)
										{
											var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

											if (!item.Value.HasValue || itemDate < cumulus.LastUpdateTime)
												continue;

											if (buffer.ContainsKey(itemDate))
											{
												buffer[itemDate].RainRate = item.Value;
											}
											else
											{
												var newItem = new HistoricData
												{
													RainRate = item.Value
												};
												buffer.Add(itemDate, newItem);
											}
										}
									}

									// yearly rain
									if (rain.yearly != null && rain.yearly.list != null)
									{
										foreach (var item in rain.yearly.list)
										{
											var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

											if (!item.Value.HasValue || itemDate < cumulus.LastUpdateTime)
												continue;

											if (buffer.ContainsKey(itemDate))
											{
												buffer[itemDate].RainYear = item.Value;
											}
											else
											{
												var newItem = new HistoricData
												{
													RainYear = item.Value
												};
												buffer.Add(itemDate, newItem);
											}
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing rainfall data. Exception");
							}
							continue;

						// rainfall piezo
						case "rainfall_piezo":
							try
							{
								if (cumulus.Gw1000PrimaryRainSensor == 1)
								{
									var rain = entry.Value.FromJsv<EcowittHistoricDataRainfall>();

									// rain rate
									if (rain.rain_rate != null && rain.rain_rate.list != null)
									{
										foreach (var item in rain.rain_rate.list)
										{
											var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

											if (!item.Value.HasValue || itemDate < cumulus.LastUpdateTime)
												continue;

											if (buffer.ContainsKey(itemDate))
											{
												buffer[itemDate].RainRate = item.Value;
											}
											else
											{
												var newItem = new HistoricData
												{
													RainRate = item.Value
												};
												buffer.Add(itemDate, newItem);
											}
										}
									}

									// yearly rain
									if (rain.yearly != null && rain.yearly.list != null)
									{
										foreach (var item in rain.yearly.list)
										{
											var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

											if (!item.Value.HasValue || itemDate < cumulus.LastUpdateTime)
												continue;

											if (buffer.ContainsKey(itemDate))
											{
												buffer[itemDate].RainYear = item.Value;
											}
											else
											{
												var newItem = new HistoricData
												{
													RainYear = item.Value
												};
												buffer.Add(itemDate, newItem);
											}
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing rainfall_piezo data. Exception");
							}
							continue;

						// Solar Data
						case "solar_and_uvi":
							try
							{
								var solar = entry.Value.FromJsv<EcowittHistoricDataSolar>();

								// solar
								if (solar.solar != null && solar.solar.list != null)
								{
									foreach (var item in solar.solar.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].Solar = (int)item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												Solar = (int)item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// uvi
								if (solar.uvi != null && solar.uvi.list != null)
								{
									foreach (var item in solar.uvi.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].UVI = (int)item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												UVI = (int)item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing solar data. Exception");
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
							try
							{
								var tandh = entry.Value.FromJsv<EcowittHistoricTempHum>();
								chan = int.Parse(entry.Key[^1..]);

								// temperature
								if (tandh.temperature != null && tandh.temperature.list != null)
								{
									foreach (var item in tandh.temperature.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].ExtraTemp[chan] = item.Value;
										}
										else
										{
											var newItem = new HistoricData();
											newItem.ExtraTemp[chan] = item.Value;
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// humidity
								if (tandh.humidity != null && tandh.humidity.list != null)
								{
									foreach (var item in tandh.humidity.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].ExtraHumidity[chan] = item.Value;
										}
										else
										{
											var newItem = new HistoricData();
											newItem.ExtraHumidity[chan] = item.Value;
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, $"API.ProcessHistoryData: Error pre-processing extra T/H data - chan[{chan}]. Exception");
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
							try
							{
								var soilm = entry.Value.FromJsv<EcowittHistoricDataSoil>();
								chan = int.Parse(entry.Key[^1..]);

								if (soilm.soilmoisture != null && soilm.soilmoisture.list != null)
								{
									// moisture
									foreach (var item in soilm.soilmoisture.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].SoilMoist[chan] = item.Value;
										}
										else
										{
											var newItem = new HistoricData();
											newItem.SoilMoist[chan] = item.Value;
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, $"API.ProcessHistoryData: Error pre-processing soil moisture data - chan[{chan}]. Exception");
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
							try
							{
								var usert = entry.Value.FromJsv<EcowittHistoricDataTemp>();
								chan = int.Parse(entry.Key[^1..]);

								if (usert.temperature != null && usert.temperature.list != null)
								{
									// temperature
									foreach (var item in usert.temperature.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].UserTemp[chan] = item.Value;
										}
										else
										{
											var newItem = new HistoricData();
											newItem.UserTemp[chan] = item.Value;
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, $"API.ProcessHistoryData: Error pre-processing user temperature data - chan[{chan}]. Exception");
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
							try
							{
								var leaf = entry.Value.FromJsv<EcowittHistoricDataLeaf>();
								chan = int.Parse(entry.Key[^1..]);

								if (leaf.leaf_wetness != null && leaf.leaf_wetness.list != null)
								{
									// wetness
									foreach (var item in leaf.leaf_wetness.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].LeafWetness[chan] = item.Value;
										}
										else
										{
											var newItem = new HistoricData();
											newItem.LeafWetness[chan] = item.Value;
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, $"API.ProcessHistoryData: Error pre-processing leaf wetness data - chan[{chan}]. Exception");
							}
							continue;

						// 4 channel PM 2.5 sensors
						case "pm25_ch1":
						case "pm25_ch2":
						case "pm25_ch3":
						case "pm25_ch4":
							try
							{
								var pm25 = entry.Value.FromJsv<EcowittHistoricDataPm25Aqi>();
								chan = int.Parse(entry.Key[^1..]);

								if (pm25.pm25 != null && pm25.pm25.list != null)
								{
									foreach (var item in pm25.pm25.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].pm25[chan] = item.Value;
										}
										else
										{
											var newItem = new HistoricData();
											newItem.pm25[chan] = item.Value;
											buffer.Add(itemDate, newItem);
										}
									}

								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, $"API.ProcessHistoryData: Error pre-processing pm 2.5 data - chan[{chan}]. Exception");
							}
							continue;

						// Indoor CO2
						case "indoor_co2":
							try
							{
								var indoorCo2 = entry.Value.FromJsv<EcowittHistoricDataCo2>();

								// CO2
								if (indoorCo2.co2 != null && indoorCo2.co2.list != null)
								{
									foreach (var item in indoorCo2.co2.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].IndoorCo2 = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												IndoorCo2 = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// 24 Avg
								if (indoorCo2.average24h != null && indoorCo2.average24h.list != null)
								{
									foreach (var item in indoorCo2.average24h.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].IndoorCo2hr24 = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												IndoorCo2hr24 = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing indoor CO2 data. Exception");
							}
							continue;

						// CO2 Combi
						case "co2_aqi_combo":
							try
							{
								var co2combo = entry.Value.FromJsv<EcowittHistoricDataCo2>();

								// CO2
								if (co2combo.co2 != null && co2combo.co2.list != null)
								{
									foreach (var item in co2combo.co2.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].CO2pm2p5 = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												CO2pm2p5 = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// 24 Avg
								if (co2combo.average24h != null && co2combo.average24h.list != null)
								{
									foreach (var item in co2combo.average24h.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].CO2pm2p5hr24 = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												CO2pm2p5hr24 = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing CO2 AQI combo data. Exception");
							}
							continue;

						// pm2.5 Combi
						case "pm25_aqi_combo":
							try
							{
								var pm25combo = entry.Value.FromJsv<EcowittHistoricDataPm25Aqi>();

								if (pm25combo.pm25 != null && pm25combo.pm25.list != null)
								{
									foreach (var item in pm25combo.pm25.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].AqiComboPm25 = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												AqiComboPm25 = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing pm 2.5 AQI combo data. Exception");
							}
							continue;

						// pm10 Combi
						case "pm10_aqi_combo":
							try
							{
								var pm10combo = entry.Value.FromJsv<EcowittHistoricDataPm10Aqi>();

								if (pm10combo.pm10 != null && pm10combo.pm10.list != null)
								{
									foreach (var item in pm10combo.pm10.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].AqiComboPm10 = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												AqiComboPm10 = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing pm 10 AQI combo data. Exception");
							}
							continue;

						// Lightning Data
						case "lightning":
							try
							{
								var lightning = entry.Value.FromJsv<EcowittHistoricDataLightning>();

								// Distance
								if (lightning.distance != null && lightning.distance.list != null)
								{
									foreach (var item in lightning.distance.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].LightningDist = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												LightningDist = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}

								// Strikes
								if (lightning.count != null && lightning.count.list != null)
								{
									foreach (var item in lightning.count.list)
									{
										var itemDate = item.Key.AddMinutes(EcowittApiFudgeFactor);

										if (!item.Value.HasValue || itemDate <= cumulus.LastUpdateTime)
											continue;

										if (buffer.ContainsKey(itemDate))
										{
											buffer[itemDate].LightningCount = item.Value;
										}
										else
										{
											var newItem = new HistoricData
											{
												LightningCount = item.Value
											};
											buffer.Add(itemDate, newItem);
										}
									}
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "API.ProcessHistoryData: Error pre-processing lightning data. Exception");
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
				if (token.IsCancellationRequested)
				{
					return;
				}

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
					station.ResetSunshineHours(rec.Key);
					station.ResetMidnightTemperatures(rec.Key);
					midnightraindone = true;
				}

				// finally apply this data
				ApplyHistoricData(rec);

				// add in archive period worth of sunshine, if sunny
				if (station.CurrentSolarMax > 0 &&
					station.SolarRad > station.CurrentSolarMax * cumulus.SolarOptions.SunThreshold / 100 &&
					station.SolarRad >= cumulus.SolarOptions.SolarMinimum &&
					!cumulus.SolarOptions.UseBlakeLarsen)
				{
					station.SunshineHours += 5 / 60.0;
					cumulus.LogDebugMessage("Adding 5 minutes to Sunshine Hours");
				}

				// add in archive period minutes worth of temperature to the temp samples
				if (station.Temperature.HasValue)
				{
					station.tempsamplestoday += 5;
					station.TempTotalToday += (station.Temperature.Value * 5);
				}

				// add in 'following interval' minutes worth of wind speed to windrun
				if (station.WindAverage.HasValue)
				{
					Cumulus.LogMessage("Windrun: " + station.WindAverage.Value.ToString(cumulus.WindFormat) + cumulus.Units.WindText + " for " + 5 + " minutes = " +
									(station.WindAverage.Value * station.WindRunHourMult[cumulus.Units.Wind] * 5 / 60.0).ToString(cumulus.WindRunFormat) + cumulus.Units.WindRunText);

					station.WindRunToday += station.WindAverage.Value * station.WindRunHourMult[cumulus.Units.Wind] * 5 / 60.0;
				}

				// update heating/cooling degree days
				station.UpdateDegreeDays(5);

				// update dominant wind bearing
				station.CalculateDominantWindBearing(station.Bearing, station.WindAverage, 5);

				station.CheckForWindrunHighLow(rec.Key);

				//bw?.ReportProgress((totalentries - datalist.Count) * 100 / totalentries, "processing");

				//UpdateDatabase(timestamp.ToUniversalTime(), historydata.interval, false);

				_ = cumulus.DoLogFile(rec.Key, false);

				_ = cumulus.DoCustomIntervalLogs(rec.Key);

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
				station.DoWind(rec.Value.WindGust, rec.Value.WindDir, rec.Value.WindSpd, rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Wind data");
			}

			// === Humidity ===
			try
			{
				station.DoIndoorHumidity(rec.Value.IndoorHum);
				if (cumulus.Gw1000PrimaryTHSensor == 0)
				{
					station.DoHumidity(rec.Value.Humidity, rec.Key);
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Humidity data");
			}

			// === Pressure ===
			try
			{
				station.DoPressure(rec.Value.Pressure, rec.Key);
				station.UpdatePressureTrendString();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Pressure data");
			}

			// === Indoor temp ===
			try
			{
				station.DoIndoorTemp(rec.Value.IndoorTemp);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Indoor temp data");
			}

			// === Outdoor temp ===
			try
			{
				if (cumulus.Gw1000PrimaryTHSensor == 0)
				{
					station.DoTemperature(rec.Value.Temp, rec.Key);
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Outdoor temp data");
			}

			// === Rain ===
			try
			{
				double? rRate = null;
				if (rec.Value.RainRate.HasValue)
				{
					// we have a rain rate, so we will NOT calculate it
					station.calculaterainrate = false;
					rRate = rec.Value.RainRate;
				}
				else
				{
					// No rain rate, so we will calculate it
					station.calculaterainrate = true;
				}

				if (rec.Value.RainYear.HasValue)
				{
					station.DoRain(rec.Value.RainYear.Value, rRate, rec.Key);
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Rain data");
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
					var tempVal = rec.Value.ExtraTemp[i];
					if (i == cumulus.Gw1000PrimaryTHSensor)
					{
						station.DoTemperature(tempVal, rec.Key);
					}

					station.DoExtraTemp(tempVal, i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra temperature data");
				}
				// === Extra Humidity ===
				try
				{
					if (i == cumulus.Gw1000PrimaryTHSensor)
					{
						station.DoHumidity(rec.Value.ExtraHumidity[i].Value, rec.Key);
					}

					station.DoExtraHum(rec.Value.ExtraHumidity[i], i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra humidity data");
				}


				// === User Temperature ===
				try
				{
					if (cumulus.EcowittSettings.MapWN34[i] == 0)
					{
						station.DoUserTemp(rec.Value.UserTemp[i], i);
					}
					else
					{
						station.DoSoilTemp((double)rec.Value.UserTemp[i], cumulus.EcowittSettings.MapWN34[i]);
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra user temperature data");
				}

				// === Soil Moisture ===
				try
				{
					station.DoSoilMoisture(rec.Value.SoilMoist[i], i);
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
					station.DoAirQuality(rec.Value.pm25[i], i);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in extra temperature data ");
				}
			}

			// Do all the derived values after the primary data

			// === Dewpoint ===
			try
			{
				station.DoDewpoint(rec.Value.DewPoint, rec.Key);
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
				station.DoCloudBaseHeatIndex(rec.Key);

				// === Apparent & Feels Like ===
				station.DoApparentTemp(rec.Key);
				station.DoFeelsLike(rec.Key);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ApplyHistoricData: Error in Humidex/Apparant/Feels Like");
			}


		}

		/*
		private static string ErrorCode(int code)
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
		*/

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
				pm25 = new double?[5];
				ExtraTemp = new double?[9];
				ExtraHumidity = new int?[9];
				SoilMoist = new int?[9];
				UserTemp = new double?[9];
				LeafWetness = new int?[9];
			}
		}
	}
}
