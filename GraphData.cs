using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace CumulusMX
{
	internal class GraphData
	{
		private readonly Cumulus cumulus;
		private readonly WeatherStation station;
		private readonly CultureInfo InvC = new CultureInfo("");

		internal GraphData(Cumulus cuml, WeatherStation stn)
		{
			cumulus = cuml;
			station = stn;
		}

		internal async Task CreateGraphDataFiles(DateTime ts)
		{
			// Chart data for Highcharts graphs
			string json = "";
			for (var i = 0; i < cumulus.GraphDataFiles.Length; i++)
			{
				// We double up the meaning of .FtpRequired to creation as well.
				// The FtpRequired flag is only cleared for the config files that are pretty static so it is pointless
				// recreating them every update too.
				if (cumulus.GraphDataFiles[i].Create && cumulus.GraphDataFiles[i].CreateRequired)
				{
					switch (cumulus.GraphDataFiles[i].LocalFileName)
					{
						case "graphconfig.json":
							json = GetGraphConfig();
							break;
						case "availabledata.json":
							json = GetAvailGraphData();
							break;
						case "tempdata.json":
							json = GetTempGraphData(ts);
							break;
						case "pressdata.json":
							json = GetPressGraphData(ts);
							break;
						case "winddata.json":
							json = GetWindGraphData(ts);
							break;
						case "wdirdata.json":
							json = GetWindDirGraphData(ts);
							break;
						case "humdata.json":
							json = GetHumGraphData(ts);
							break;
						case "raindata.json":
							json = GetRainGraphData(ts);
							break;
						case "dailyrain.json":
							json = GetDailyRainGraphData();
							break;
						case "dailytemp.json":
							json = GetDailyTempGraphData();
							break;
						case "solardata.json":
							json = GetSolarGraphData(ts);
							break;
						case "sunhours.json":
							json = GetSunHoursGraphData();
							break;
						case "airquality.json":
							json = GetAqGraphData(ts);
							break;
					}

					try
					{
						var dest = cumulus.GraphDataFiles[i].LocalPath + cumulus.GraphDataFiles[i].LocalFileName;
						using (var file = new StreamWriter(dest, false))
						{
							await file.WriteLineAsync(json);
							file.Close();
						}

						// The config files only need creating once per change
						if (cumulus.GraphDataFiles[i].LocalFileName == "availabledata.json" || cumulus.GraphDataFiles[i].LocalFileName == "graphconfig.json")
						{
							cumulus.GraphDataFiles[i].CreateRequired = false;
						}
					}
					catch (Exception ex)
					{
						Cumulus.LogMessage($"Error writing {cumulus.GraphDataFiles[i].LocalFileName}: {ex}");
					}
				}
			}
		}

		internal void CreateEodGraphDataFiles()
		{
			string json = "";
			for (var i = 0; i < cumulus.GraphDataEodFiles.Length; i++)
			{
				if (cumulus.GraphDataEodFiles[i].Create)
				{
					switch (cumulus.GraphDataEodFiles[i].LocalFileName)
					{
						case "alldailytempdata.json":
							json = GetAllDailyTempGraphData();
							break;
						case "alldailypressdata.json":
							json = GetAllDailyPressGraphData();
							break;
						case "alldailywinddata.json":
							json = GetAllDailyWindGraphData();
							break;
						case "alldailyhumdata.json":
							json = GetAllDailyHumGraphData();
							break;
						case "alldailyraindata.json":
							json = GetAllDailyRainGraphData();
							break;
						case "alldailysolardata.json":
							json = GetAllDailySolarGraphData();
							break;
						case "alldailydegdaydata.json":
							json = GetAllDegreeDaysGraphData();
							break;
						case "alltempsumdata.json":
							json = GetAllTempSumGraphData();
							break;
					}

					try
					{
						var dest = cumulus.GraphDataEodFiles[i].LocalPath + cumulus.GraphDataEodFiles[i].LocalFileName;
						using (var file = new StreamWriter(dest, false))
						{
							file.WriteLine(json);
							file.Close();
						}
						// Now set the flag that upload is required (if enabled)
						cumulus.GraphDataEodFiles[i].FtpRequired = true;
						cumulus.GraphDataEodFiles[i].CopyRequired = true;
					}
					catch (Exception ex)
					{
						Cumulus.LogMessage($"Error writing {cumulus.GraphDataEodFiles[i].LocalFileName}: {ex}");
					}
				}
			}
		}

		internal string GetSolarGraphData(DateTime ts)
		{
			var sb = new StringBuilder("{");
			var sbUv = new StringBuilder("\"UV\":[");
			var sbSol = new StringBuilder("\"SolarRad\":[");
			var sbMax = new StringBuilder("\"CurrentSolarMax\":[");

			var dataFrom = ts.AddHours(-cumulus.GraphHours);

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

			for (var i = 0; i < data.Count; i++)
			{
				if (cumulus.GraphOptions.UVVisible)
				{
					sbUv.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].UV.HasValue ? data[i].UV.Value.ToString(cumulus.UVFormat, InvC) : "null")}],");
				}

				if (cumulus.GraphOptions.SolarVisible)
				{
					sbSol.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].SolarRad.HasValue ? data[i].SolarRad.Value : "null")}],");
					sbMax.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].SolarMax.HasValue ? data[i].SolarMax.Value : "null")}],");
				}
			}


			if (cumulus.GraphOptions.UVVisible)
			{
				if (sbUv[^1] == ',')
					sbUv.Length--;

				sbUv.Append(']');
				sb.Append(sbUv);
			}
			if (cumulus.GraphOptions.SolarVisible)
			{
				if (sbSol[^1] == ',')
				{
					sbSol.Length--;
					sbMax.Length--;
				}

				sbSol.Append(']');
				sbMax.Append(']');
				if (cumulus.GraphOptions.UVVisible)
				{
					sb.Append(',');
				}
				sb.Append(sbSol);
				sb.Append(',');
				sb.Append(sbMax);
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetRainGraphData(DateTime ts)
		{
			var sb = new StringBuilder("{");
			var sbRain = new StringBuilder("\"rfall\":[");
			var sbRate = new StringBuilder("\"rrate\":[");

			var dataFrom = ts.AddHours(-cumulus.GraphHours);

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

			for (var i = 0; i < data.Count; i++)
			{
				sbRain.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].RainToday.HasValue ? data[i].RainToday.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
				sbRate.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].RainRate.HasValue ? data[i].RainRate.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
			}

			if (sbRain[^1] == ',')
			{
				sbRain.Length--;
				sbRate.Length--;
			}
			sbRain.Append("],");
			sbRate.Append(']');
			sb.Append(sbRain);
			sb.Append(sbRate);
			sb.Append('}');
			return sb.ToString();
		}

		internal string GetHumGraphData(DateTime ts)
		{
			var sb = new StringBuilder("{", 10240);
			var sbOut = new StringBuilder("\"hum\":[");
			var sbIn = new StringBuilder("\"inhum\":[");

			var dataFrom = ts.AddHours(-cumulus.GraphHours);

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

			for (var i = 0; i < data.Count; i++)
			{
				if (cumulus.GraphOptions.OutHumVisible)
				{
					sbOut.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].Humidity.HasValue ? data[i].Humidity.Value : "null")}],");
				}
				if (cumulus.GraphOptions.InHumVisible)
				{
					sbIn.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].IndoorHumidity.HasValue ? data[i].IndoorHumidity.Value : "null")}],");
				}
			}

			if (cumulus.GraphOptions.OutHumVisible)
			{
				if (sbOut[^1] == ',')
					sbOut.Length--;

				sbOut.Append(']');

				sb.Append(sbOut);
			}

			if (cumulus.GraphOptions.InHumVisible)
			{
				if (sbIn[^1] == ',')
					sbIn.Length--;

				sbIn.Append(']');

				if (cumulus.GraphOptions.OutHumVisible)
					sb.Append(',');

				sb.Append(sbIn);
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetWindDirGraphData(DateTime ts)
		{
			var sb = new StringBuilder("{\"bearing\":[");
			var sbAvg = new StringBuilder("\"avgbearing\":[");
			var dataFrom = ts.AddHours(-cumulus.GraphHours);

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

			for (var i = 0; i < data.Count; i++)
			{
				sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].WindDir.HasValue ? data[i].WindDir : "null")}],");

				sbAvg.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].WindAvgDir.HasValue ? data[i].WindAvgDir : "null")}],");
			}

			if (sb[^1] == ',')
			{
				sb.Length--;
				sbAvg.Length--;
				sbAvg.Append(']');
			}

			sb.Append("],");
			sb.Append(sbAvg);
			sb.Append('}');
			return sb.ToString();
		}

		internal string GetWindGraphData(DateTime ts)
		{
			var sb = new StringBuilder("{\"wgust\":[");
			var sbSpd = new StringBuilder("\"wspeed\":[");
			var dataFrom = ts.AddHours(-cumulus.GraphHours);

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

			for (var i = 0; i < data.Count; i++)
			{
				sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].WindGust.HasValue ? data[i].WindGust.Value.ToString(cumulus.WindFormat, InvC) : "null")}],");
				sbSpd.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].WindSpeed.HasValue ? data[i].WindSpeed.Value.ToString(cumulus.WindAvgFormat, InvC) : "null")}],");
			}

			if (sb[^1] == ',')
			{
				sb.Length--;
				sbSpd.Length--;
				sbSpd.Append(']');
			}

			sb.Append("],");
			sb.Append(sbSpd);
			sb.Append('}');
			return sb.ToString();
		}

		internal string GetPressGraphData(DateTime ts)
		{
			StringBuilder sb = new StringBuilder("{\"press\":[");
			var dataFrom = ts.AddHours(-cumulus.GraphHours);

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

			for (var i = 0; i < data.Count; i++)
			{
				sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].Pressure.HasValue ? data[i].Pressure.Value.ToString(cumulus.PressFormat, InvC) : "null")}],");
			}

			if (sb[^1] == ',')
				sb.Length--;

			sb.Append("]}");
			return sb.ToString();
		}

		internal string GetTempGraphData(DateTime ts)
		{
			bool append = false;
			var sb = new StringBuilder("{", 10240);
			var sbIn = new StringBuilder("\"intemp\":[");
			var sbDew = new StringBuilder("\"dew\":[");
			var sbApp = new StringBuilder("\"apptemp\":[");
			var sbFeel = new StringBuilder("\"feelslike\":[");
			var sbChill = new StringBuilder("\"wchill\":[");
			var sbHeat = new StringBuilder("\"heatindex\":[");
			var sbTemp = new StringBuilder("\"temp\":[");
			var sbHumidex = new StringBuilder("\"humidex\":[");
			var dataFrom = ts.AddHours(-cumulus.GraphHours);

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

			for (var i = 0; i < data.Count; i++)
			{
				if (cumulus.GraphOptions.InTempVisible)
					sbIn.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].IndoorTemp.HasValue ? data[i].IndoorTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.DPVisible)
					sbDew.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].DewPoint.HasValue ? data[i].DewPoint.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.AppTempVisible)
					sbApp.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].AppTemp.HasValue ? data[i].AppTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.FeelsLikeVisible)
					sbFeel.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].FeelsLike.HasValue ? data[i].FeelsLike.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.WCVisible)
					sbChill.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].WindChill.HasValue ? data[i].WindChill.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.HIVisible)
					sbHeat.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].HeatIndex.HasValue ? data[i].HeatIndex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.TempVisible)
					sbTemp.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].OutsideTemp.HasValue ? data[i].OutsideTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.HumidexVisible)
					sbHumidex.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].Humidex.HasValue ? data[i].Humidex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
			}

			if (cumulus.GraphOptions.InTempVisible)
			{
				if (sbIn[^1] == ',')
					sbIn.Length--;

				sbIn.Append(']');
				sb.Append(sbIn);
				append = true;
			}

			if (cumulus.GraphOptions.DPVisible)
			{
				if (sbDew[^1] == ',')
					sbDew.Length--;

				sbDew.Append(']');
				sb.Append((append ? "," : "") + sbDew);
				append = true;
			}

			if (cumulus.GraphOptions.AppTempVisible)
			{
				if (sbApp[^1] == ',')
					sbApp.Length--;

				sbApp.Append(']');
				sb.Append((append ? "," : "") + sbApp);
				append = true;
			}

			if (cumulus.GraphOptions.FeelsLikeVisible)
			{
				if (sbFeel[^1] == ',')
					sbFeel.Length--;

				sbFeel.Append(']');
				sb.Append((append ? "," : "") + sbFeel);
				append = true;
			}

			if (cumulus.GraphOptions.WCVisible)
			{
				if (sbChill[^1] == ',')
					sbChill.Length--;

				sbChill.Append(']');
				sb.Append((append ? "," : "") + sbChill);
				append = true;
			}

			if (cumulus.GraphOptions.HIVisible)
			{
				if (sbHeat[^1] == ',')
					sbHeat.Length--;

				sbHeat.Append(']');
				sb.Append((append ? "," : "") + sbHeat);
				append = true;
			}

			if (cumulus.GraphOptions.TempVisible)
			{
				if (sbTemp[^1] == ',')
					sbTemp.Length--;

				sbTemp.Append(']');
				sb.Append((append ? "," : "") + sbTemp);
				append = true;
			}

			if (cumulus.GraphOptions.HumidexVisible)
			{
				if (sbHumidex[^1] == ',')
					sbHumidex.Length--;

				sbHumidex.Append(']');
				sb.Append((append ? "," : "") + sbHumidex);
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetAqGraphData(DateTime ts)
		{
			bool append = false;
			var sb = new StringBuilder("{");
			var sb2p5 = new StringBuilder("\"pm2p5\":[");
			var sb10 = new StringBuilder(",\"pm10\":[");
			var dataFrom = ts.AddHours(-cumulus.GraphHours);


			// Check if we are to generate AQ data at all. Only if a primary sensor is defined and it isn't the Indoor AirLink
			if (cumulus.StationOptions.PrimaryAqSensor > (int)Cumulus.PrimaryAqSensor.Undefined
				&& cumulus.StationOptions.PrimaryAqSensor != (int)Cumulus.PrimaryAqSensor.AirLinkIndoor)
			{
				var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=?", dataFrom);

				for (var i = 0; i < data.Count; i++)
				{
					sb2p5.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].Pm2p5.HasValue ? data[i].Pm2p5.Value.ToString("F1", InvC) : "null")}],");

					// Only the AirLink and Ecowitt CO2 servers provide PM10 values at the moment
					append = true;
					sb10.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].Pm10.HasValue ? data[i].Pm10.Value.ToString("F1", InvC) : "null")}],");
				}

				if (sb2p5[^1] == ',')
					sb2p5.Length--;

				sb2p5.Append(']');
				sb.Append(sb2p5);

				if (append)
				{
					if (sb10[^1] == ',')
						sb10.Length--;

					sb10.Append(']');
					sb.Append(sb10);
				}

			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetAvailGraphData()
		{
			var json = new StringBuilder(200);

			// Temp values
			json.Append("{\"Temperature\":[");

			if (cumulus.GraphOptions.TempVisible)
				json.Append("\"Temperature\",");

			if (cumulus.GraphOptions.InTempVisible)
				json.Append("\"Indoor Temp\",");

			if (cumulus.GraphOptions.HIVisible)
				json.Append("\"Heat Index\",");

			if (cumulus.GraphOptions.DPVisible)
				json.Append("\"Dew Point\",");

			if (cumulus.GraphOptions.WCVisible)
				json.Append("\"Wind Chill\",");

			if (cumulus.GraphOptions.AppTempVisible)
				json.Append("\"Apparent Temp\",");

			if (cumulus.GraphOptions.FeelsLikeVisible)
				json.Append("\"Feels Like\",");

			//if (cumulus.GraphOptions.HumidexVisible)
			//	json.Append("\"Humidex\",");

			if (json[^1] == ',')
				json.Length--;

			// humidity values
			json.Append("],\"Humidity\":[");

			if (cumulus.GraphOptions.OutHumVisible)
				json.Append("\"Humidity\",");

			if (cumulus.GraphOptions.InHumVisible)
				json.Append("\"Indoor Hum\",");

			if (json[^1] == ',')
				json.Length--;

			// fixed values
			// pressure
			json.Append("],\"Pressure\":[\"Pressure\"],");

			// wind
			json.Append("\"Wind\":[\"Wind Speed\",\"Wind Gust\",\"Wind Bearing\"],");

			// rain
			json.Append("\"Rain\":[\"Rainfall\",\"Rainfall Rate\"]");

			if (cumulus.GraphOptions.DailyAvgTempVisible || cumulus.GraphOptions.DailyMaxTempVisible || cumulus.GraphOptions.DailyMinTempVisible)
			{
				json.Append(",\"DailyTemps\":[");

				if (cumulus.GraphOptions.DailyAvgTempVisible)
					json.Append("\"AvgTemp\",");
				if (cumulus.GraphOptions.DailyMaxTempVisible)
					json.Append("\"MaxTemp\",");
				if (cumulus.GraphOptions.DailyMinTempVisible)
					json.Append("\"MinTemp\",");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}


			// solar values
			if (cumulus.GraphOptions.SolarVisible || cumulus.GraphOptions.UVVisible)
			{
				json.Append(",\"Solar\":[");

				if (cumulus.GraphOptions.SolarVisible)
					json.Append("\"Solar Rad\",");

				if (cumulus.GraphOptions.UVVisible)
					json.Append("\"UV Index\",");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Sunshine
			if (cumulus.GraphOptions.SunshineVisible)
			{
				json.Append(",\"Sunshine\":[\"sunhours\"]");
			}

			// air quality
			// Check if we are to generate AQ data at all. Only if a primary sensor is defined and it isn't the Indoor AirLink
			if (cumulus.StationOptions.PrimaryAqSensor > (int)Cumulus.PrimaryAqSensor.Undefined
				&& cumulus.StationOptions.PrimaryAqSensor != (int)Cumulus.PrimaryAqSensor.AirLinkIndoor)
			{
				json.Append(",\"AirQuality\":[");
				json.Append("\"PM 2.5\"");

				// Only the AirLink and Ecowitt CO2 servers provide PM10 values at the moment
				if (cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkOutdoor ||
					cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkIndoor ||
					cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.EcowittCO2)
				{
					json.Append(",\"PM 10\"");
				}
				json.Append(']');
			}

			// Degree Days
			if (cumulus.GraphOptions.GrowingDegreeDaysVisible1 || cumulus.GraphOptions.GrowingDegreeDaysVisible2)
			{
				json.Append(",\"DegreeDays\":[");
				if (cumulus.GraphOptions.GrowingDegreeDaysVisible1)
					json.Append("\"GDD1\",");

				if (cumulus.GraphOptions.GrowingDegreeDaysVisible2)
					json.Append("\"GDD2\"");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Temp Sum
			if (cumulus.GraphOptions.TempSumVisible0 || cumulus.GraphOptions.TempSumVisible1 || cumulus.GraphOptions.TempSumVisible2)
			{
				json.Append(",\"TempSum\":[");
				if (cumulus.GraphOptions.TempSumVisible0)
					json.Append("\"Sum0\",");
				if (cumulus.GraphOptions.TempSumVisible1)
					json.Append("\"Sum1\",");
				if (cumulus.GraphOptions.TempSumVisible2)
					json.Append("\"Sum2\"");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			json.Append('}');
			return json.ToString();
		}

		internal string GetGraphConfig()
		{
			var json = new StringBuilder(200);
			json.Append('{');
			json.Append($"\"temp\":{{\"units\":\"{cumulus.Units.TempText[1]}\",\"decimals\":{cumulus.TempDPlaces}}},");
			json.Append($"\"wind\":{{\"units\":\"{cumulus.Units.WindText}\",\"decimals\":{cumulus.WindAvgDPlaces},\"rununits\":\"{cumulus.Units.WindRunText}\"}},");
			json.Append($"\"rain\":{{\"units\":\"{cumulus.Units.RainText}\",\"decimals\":{cumulus.RainDPlaces}}},");
			json.Append($"\"press\":{{\"units\":\"{cumulus.Units.PressText}\",\"decimals\":{cumulus.PressDPlaces}}},");
			json.Append($"\"hum\":{{\"decimals\":{cumulus.HumDPlaces}}},");
			json.Append($"\"uv\":{{\"decimals\":{cumulus.UVDPlaces}}},");
			json.Append($"\"soilmoisture\":{{\"units\":\"{cumulus.Units.SoilMoistureUnitText}\"}},");
			json.Append($"\"co2\":{{\"units\":\"{cumulus.Units.CO2UnitText}\"}},");
			json.Append($"\"leafwet\":{{\"units\":\"{cumulus.Units.LeafWetnessUnitText}\",\"decimals\":{cumulus.LeafWetDPlaces}}},");
			json.Append($"\"aq\":{{\"units\":\"{cumulus.Units.AirQualityUnitText}\"}},");
			json.Append($"\"timezone\":\"{cumulus.StationOptions.TimeZone}\"");
			json.Append('}');
			return json.ToString();
		}

		internal string GetSelectaChartOptions()
		{
			return JsonSerializer.SerializeToString(cumulus.SelectaChartOptions);
		}

		internal string GetDailyRainGraphData()
		{
			var datefrom = DateTime.Now.AddDays(-cumulus.GraphDays - 1);

			StringBuilder sb = new StringBuilder("{\"dailyrain\":[", 10000);

			var data = station.Database.Query<DayData>("select Timestamp, TotalRain from DayData where Timestamp >= ? and TotalRain is not null order by Timestamp", datefrom);

			for (var i = 0; i < data.Count; i++)
			{
				sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].TotalRain.HasValue ? data[i].TotalRain.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
			}
			// remove trailing comma
			if (data.Count > 0)
				sb.Length--;

			sb.Append("]}");
			return sb.ToString();
		}

		internal string GetSunHoursGraphData()
		{
			StringBuilder sb = new StringBuilder("{", 10000);
			if (cumulus.GraphOptions.SunshineVisible)
			{
				var datefrom = DateTime.Now.AddDays(-cumulus.GraphDays - 1);
				var data = station.Database.Query<DayData>("select Timestamp, SunShineHours from Daydata where Timestamp >= ? and SunShineHours is not null order by Timestamp", datefrom);

				sb.Append("\"sunhours\":[");
				if (data.Count > 0)
				{
					for (var i = 0; i < data.Count; i++)
					{
						sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{data[i].SunShineHours.Value.ToString(cumulus.SunFormat, InvC)}],");
					}

					// remove trailing comma
					if (data.Count > 0)
						sb.Length--;
				}
				sb.Append(']');
			}
			sb.Append('}');
			return sb.ToString();
		}

		internal string GetDailyTempGraphData()
		{
			var datefrom = DateTime.Now.AddDays(-cumulus.GraphDays - 1);
			var data = station.Database.Query<DayData>("select Timestamp, HighTemp, LowTemp, AvgTemp from DayData where Timestamp >= ? order by Timestamp", datefrom);
			var append = false;
			StringBuilder sb = new StringBuilder("{");

			if (cumulus.GraphOptions.DailyMinTempVisible)
			{
				sb.Append("\"mintemp\":[");

				for (var i = 0; i < data.Count; i++)
				{
					sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].LowTemp.HasValue ? data[i].LowTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
				}
				// remove trailing comma
				if (data.Count > 0)
					sb.Length--;

				sb.Append(']');
				append = true;
			}

			if (cumulus.GraphOptions.DailyMaxTempVisible)
			{
				if (append)
					sb.Append(',');

				sb.Append("\"maxtemp\":[");

				for (var i = 0; i < data.Count; i++)
				{
					sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].HighTemp.HasValue ? data[i].HighTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
				}				// remove trailing comma
				if (data.Count > 0)
					sb.Length--;

				sb.Append(']');
				append = true;
			}

			if (cumulus.GraphOptions.DailyAvgTempVisible)
			{
				if (append)
					sb.Append(',');

				sb.Append("\"avgtemp\":[");
				for (var i = 0; i < data.Count; i++)
				{
					sb.Append($"[{Utils.ToGraphTime(data[i].Timestamp)},{(data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
				}
				// remove trailing comma
				if (data.Count > 0)
					sb.Length--;

				sb.Append(']');
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetAllDailyTempGraphData()
		{
			/* returns:
			 *	{
			 *		highgust:[[date1,val1],[date2,val2]...],
			 *		mintemp:[[date1,val1],[date2,val2]...],
			 *		etc
			 *	}
			 */

			StringBuilder sb = new StringBuilder("{");
			StringBuilder minTemp = new StringBuilder("[");
			StringBuilder maxTemp = new StringBuilder("[");
			StringBuilder avgTemp = new StringBuilder("[");
			StringBuilder heatIdx = new StringBuilder("[");
			StringBuilder maxApp = new StringBuilder("[");
			StringBuilder minApp = new StringBuilder("[");
			StringBuilder windChill = new StringBuilder("[");
			StringBuilder maxDew = new StringBuilder("[");
			StringBuilder minDew = new StringBuilder("[");
			StringBuilder maxFeels = new StringBuilder("[");
			StringBuilder minFeels = new StringBuilder("[");
			StringBuilder humidex = new StringBuilder("[");

			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select * from DayData order by Timestamp");
			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{
					var recDate = Utils.ToGraphTime(data[i].Timestamp);
					// lo temp
					if (cumulus.GraphOptions.DailyMinTempVisible)
						minTemp.Append($"[{recDate},{(data[i].LowTemp.HasValue ? data[i].LowTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					// hi temp
					if (cumulus.GraphOptions.DailyMaxTempVisible)
						maxTemp.Append($"[{recDate},{(data[i].HighTemp.HasValue ? data[i].HighTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					// avg temp
					if (cumulus.GraphOptions.DailyAvgTempVisible)
						avgTemp.Append($"[{recDate},{(data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");


					if (cumulus.GraphOptions.HIVisible)
					{
						// hi heat index
						heatIdx.Append($"[{recDate},{(data[i].HighHeatIndex.HasValue ? data[i].HighHeatIndex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
					if (cumulus.GraphOptions.AppTempVisible)
					{
						// hi app temp
						maxApp.Append($"[{recDate},{(data[i].HighAppTemp.HasValue ? data[i].HighAppTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

						// lo app temp
						minApp.Append($"[{recDate},{(data[i].LowAppTemp.HasValue ? data[i].LowAppTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
					// lo wind chill
					if (cumulus.GraphOptions.WCVisible)
					{
						windChill.Append($"[{recDate},{(data[i].LowWindChill.HasValue ? data[i].LowWindChill.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.DPVisible)
					{
						// hi dewpt
						maxDew.Append($"[{recDate},{(data[i].HighDewPoint.HasValue ? data[i].HighDewPoint.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

						// lo dewpt
						minDew.Append($"[{recDate},{(data[i].LowDewPoint.HasValue ? data[i].LowDewPoint.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.FeelsLikeVisible)
					{
						// hi feels like
						maxFeels.Append($"[{recDate},{(data[i].HighFeelsLike.HasValue ? data[i].HighFeelsLike.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

						// lo feels like
						minFeels.Append($"[{recDate},{(data[i].LowFeelsLike.HasValue ? data[i].LowFeelsLike.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.HumidexVisible)
					{
						// hi humidex
						humidex.Append($"[{recDate},{(data[i].HighHumidex.HasValue ? data[i].HighHumidex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
				}
			}
			if (cumulus.GraphOptions.DailyMinTempVisible)
			{
				if (minTemp.Length > 1) minTemp.Length--;
				sb.Append("\"minTemp\":" + minTemp.ToString() + "],");
			}
			if (cumulus.GraphOptions.DailyMaxTempVisible)
			{
				if (maxTemp.Length > 1) maxTemp.Length--;
				sb.Append("\"maxTemp\":" + maxTemp.ToString() + "],");
			}
			if (cumulus.GraphOptions.DailyAvgTempVisible)
			{
				if (avgTemp.Length > 1) avgTemp.Length--;
				sb.Append("\"avgTemp\":" + avgTemp.ToString() + "],");
			}
			if (cumulus.GraphOptions.HIVisible)
			{
				if (heatIdx.Length > 1) heatIdx.Length--;
				sb.Append("\"heatIndex\":" + heatIdx.ToString() + "],");
			}
			if (cumulus.GraphOptions.AppTempVisible)
			{
				if (maxApp.Length > 1) maxApp.Length--;
				if (minApp.Length > 1) minApp.Length--;
				sb.Append("\"maxApp\":" + maxApp.ToString() + "],");
				sb.Append("\"minApp\":" + minApp.ToString() + "],");
			}
			if (cumulus.GraphOptions.WCVisible)
			{
				if (windChill.Length > 1) windChill.Length--;
				sb.Append("\"windChill\":" + windChill.ToString() + "],");
			}
			if (cumulus.GraphOptions.DPVisible)
			{
				if (maxDew.Length > 1) maxDew.Length--;
				if (minDew.Length > 1) minDew.Length--;
				sb.Append("\"maxDew\":" + maxDew.ToString() + "],");
				sb.Append("\"minDew\":" + minDew.ToString() + "],");
			}
			if (cumulus.GraphOptions.FeelsLikeVisible)
			{
				if (maxFeels.Length > 1) maxFeels.Length--;
				if (minFeels.Length > 1) minFeels.Length--;
				sb.Append("\"maxFeels\":" + maxFeels.ToString() + "],");
				sb.Append("\"minFeels\":" + minFeels.ToString() + "],");
			}
			if (cumulus.GraphOptions.HumidexVisible)
			{
				if (humidex.Length > 1) humidex.Length--;
				sb.Append("\"humidex\":" + humidex.ToString() + "],");
			}
			sb.Length--;
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDailyWindGraphData()
		{

			/* returns:
			 *	{
			 *		highgust:[[date1,val1],[date2,val2]...],
			 *		mintemp:[[date1,val1],[date2,val2]...],
			 *		etc
			 *	}
			 */

			StringBuilder sb = new StringBuilder("{");
			StringBuilder maxGust = new StringBuilder("[");
			StringBuilder windRun = new StringBuilder("[");
			StringBuilder maxWind = new StringBuilder("[");

			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, HighGust, WindRun, HighAvgWind from DayData order by Timestamp");
			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{
					var recDate = Utils.ToGraphTime(data[i].Timestamp);

					// hi gust
					maxGust.Append($"[{recDate},{(data[i].HighGust.HasValue ? data[i].HighGust.Value.ToString(cumulus.WindFormat, InvC) : "null")}],");
					// hi wind run
					windRun.Append($"[{recDate},{(data[i].WindRun.HasValue ? data[i].WindRun.Value.ToString(cumulus.WindRunFormat, InvC) : "null")}],");
					// hi wind
					maxWind.Append($"[{recDate},{(data[i].HighAvgWind.HasValue ? data[i].HighAvgWind.Value.ToString(cumulus.WindAvgFormat, InvC) : "null")}],");
				}
				// strip trailing commas
				maxGust.Length--;
				windRun.Length--;
				maxWind.Length--;
			}


			sb.Append("\"maxGust\":" + maxGust.ToString() + "],");
			sb.Append("\"windRun\":" + windRun.ToString() + "],");
			sb.Append("\"maxWind\":" + maxWind.ToString() + "]");
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDailyRainGraphData()
		{
			/* returns:
			 *	{
			 *		highgust:[[date1,val1],[date2,val2]...],
			 *		mintemp:[[date1,val1],[date2,val2]...],
			 *		etc
			 *	}
			 */

			StringBuilder sb = new StringBuilder("{");
			StringBuilder maxRRate = new StringBuilder("[");
			StringBuilder rain = new StringBuilder("[");

			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, HighRainRate, Totalrain from Daydata order by Timestamp");

			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{

					var recDate = Utils.ToGraphTime(data[i].Timestamp);

					// hi rain rate
					maxRRate.Append($"[{recDate},{(data[i].HighRainRate.HasValue ? data[i].HighRainRate.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
					// total rain
					rain.Append($"[{recDate},{(data[i].TotalRain.HasValue ? data[i].TotalRain.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
				}
				// strip trailing commas
				maxRRate.Length--;
				rain.Length--;
			}


			sb.Append("\"maxRainRate\":" + maxRRate.ToString() + "],");
			sb.Append("\"rain\":" + rain.ToString() + "]");
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDailyPressGraphData()
		{
			/* returns:
			 *	{
			 *		highgust:[[date1,val1],[date2,val2]...],
			 *		mintemp:[[date1,val1],[date2,val2]...],
			 *		etc
			 *	}
			 */

			StringBuilder sb = new StringBuilder("{");
			StringBuilder minBaro = new StringBuilder("[");
			StringBuilder maxBaro = new StringBuilder("[");


			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, LowPress, HighPress from Daydata order by Timestamp");

			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{

					var recDate = Utils.ToGraphTime(data[i].Timestamp);

					// lo baro
					if (data[i].LowPress.HasValue)
						minBaro.Append($"[{recDate},{data[i].LowPress.Value.ToString(cumulus.PressFormat, InvC)}],");
					// hi baro
					if (data[i].HighPress.HasValue)
						maxBaro.Append($"[{recDate},{data[i].HighPress.Value.ToString(cumulus.PressFormat, InvC)}],");
				}
				// strip trailing commas
				minBaro.Length--;
				maxBaro.Length--;
			}
			sb.Append("\"minBaro\":" + minBaro.ToString() + "],");
			sb.Append("\"maxBaro\":" + maxBaro.ToString() + "]");
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDailyWindDirGraphData()
		{
			/* returns:
			 *	{
			 *		highgust:[[date1,val1],[date2,val2]...],
			 *		mintemp:[[date1,val1],[date2,val2]...],
			 *		etc
			 *	}
			 */

			StringBuilder sb = new StringBuilder("{");
			StringBuilder windDir = new StringBuilder("[");

			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, HighRainRate, Totalrain from Daydata order by Timestamp");
			
			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{
					var recDate = Utils.ToGraphTime(data[i].Timestamp);

					windDir.Append($"[{recDate},{(data[i].DominantWindBearing.HasValue ? data[i].DominantWindBearing.Value : "null")}],");
				}
				// strip trailing comma
				windDir.Length--;
			}

			sb.Append("\"windDir\":" + windDir.ToString() + "]");
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDailyHumGraphData()
		{
			/* returns:
			 *	{
			 *		highgust:[[date1,val1],[date2,val2]...],
			 *		mintemp:[[date1,val1],[date2,val2]...],
			 *		etc
			 *	}
			 */

			StringBuilder sb = new StringBuilder("{");
			StringBuilder minHum = new StringBuilder("[");
			StringBuilder maxHum = new StringBuilder("[");

			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, LowHumidity, HighHumidity from DayData order by Timestamp");

			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{
					var recDate = Utils.ToGraphTime(data[i].Timestamp);

					// lo humidity
					minHum.Append($"[{recDate},{(data[i].LowHumidity.HasValue ? data[i].LowHumidity : "null")}],");
					// hi humidity
					maxHum.Append($"[{recDate},{(data[i].HighHumidity.HasValue ? data[i].HighHumidity : "null")}],");
				}
				// strip trailing commas
				minHum.Length--;
				maxHum.Length--;
			}
			sb.Append("\"minHum\":" + minHum.ToString() + "],");
			sb.Append("\"maxHum\":" + maxHum.ToString() + "]");
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDailySolarGraphData()
		{
			/* returns:
			 *	{
			 *		highgust:[[date1,val1],[date2,val2]...],
			 *		mintemp:[[date1,val1],[date2,val2]...],
			 *		etc
			 *	}
			 */

			StringBuilder sb = new StringBuilder("{");
			StringBuilder sunHours = new StringBuilder("[");
			StringBuilder solarRad = new StringBuilder("[");
			StringBuilder uvi = new StringBuilder("[");

			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, SunShineHours, HighSolar, HighUv from Daydata order by Timestamp");

			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{
					var recDate = Utils.ToGraphTime(data[i].Timestamp);

					if (cumulus.GraphOptions.SunshineVisible)
					{
						// sunshine hours
						sunHours.Append($"[{recDate},{(data[i].SunShineHours.HasValue ? data[i].SunShineHours.Value.ToString(InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.SolarVisible)
					{
						// hi solar rad
						solarRad.Append($"[{recDate},{(data[i].HighSolar.HasValue ? data[i].HighSolar.Value : "null")}],");
					}

					if (cumulus.GraphOptions.UVVisible)
					{
						// hi UV-I
						uvi.Append($"[{recDate},{(data[i].HighUv.HasValue ? data[i].HighUv.Value.ToString(cumulus.UVFormat, InvC) : "null")}],");
					}
				}
				// strip trailing commas
				sunHours.Length--;
				solarRad.Length--;
				uvi.Length--;
			}
			if (cumulus.GraphOptions.SunshineVisible)
				sb.Append("\"sunHours\":" + sunHours.ToString() + "]");

			if (cumulus.GraphOptions.SolarVisible)
			{
				if (cumulus.GraphOptions.SunshineVisible)
					sb.Append(',');

				sb.Append("\"solarRad\":" + solarRad.ToString() + "]");
			}

			if (cumulus.GraphOptions.UVVisible)
			{
				if (cumulus.GraphOptions.SunshineVisible || cumulus.GraphOptions.SolarVisible)
					sb.Append(',');

				sb.Append("\"uvi\":" + uvi.ToString() + "]");
			}
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDegreeDaysGraphData()
		{
			StringBuilder sb = new StringBuilder("{");
			StringBuilder growdegdaysYears1 = new StringBuilder("{", 32768);
			StringBuilder growdegdaysYears2 = new StringBuilder("{", 32768);

			StringBuilder growYear1 = new StringBuilder("[", 8600);
			StringBuilder growYear2 = new StringBuilder("[", 8600);

			var options = $"\"options\":{{\"gddBase1\":{cumulus.GrowingBase1},\"gddBase2\":{cumulus.GrowingBase2},\"startMon\":{cumulus.GrowingYearStarts}}}";

			DateTime nextYear;

			// 2000 was a leap year, so make sure February falls in 2000
			// for Southern hemisphere this means the start year must be 1999
			var plotYear = cumulus.GrowingYearStarts < 3 ? 2000 : 1999;

			int startYear;

			var annualGrowingDegDays1 = 0.0;
			var annualGrowingDegDays2 = 0.0;

			// Read the database and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, HighTemp, LowTemp from Daydata order by Timestamp");

			if (data.Count > 0 && (cumulus.GraphOptions.GrowingDegreeDaysVisible1 || cumulus.GraphOptions.GrowingDegreeDaysVisible2))
			{
				// we have to detect a new growing deg day year is starting
				nextYear = new DateTime(data[0].Timestamp.Year, cumulus.GrowingYearStarts, 1);

				if (data[0].Timestamp >= nextYear)
				{
					nextYear = nextYear.AddYears(1);
				}

				// are we starting part way through a year that does not start in January?
				if (data[0].Timestamp.Year == nextYear.Year)
				{
					startYear = data[0].Timestamp.Year - 1;
				}
				else
				{
					startYear = data[0].Timestamp.Year;
				}

				if (cumulus.GraphOptions.GrowingDegreeDaysVisible1)
				{
					growdegdaysYears1.Append($"\"{startYear}\":");
				}
				if (cumulus.GraphOptions.GrowingDegreeDaysVisible2)
				{
					growdegdaysYears2.Append($"\"{startYear}\":");
				}


				for (var i = 0; i < data.Count; i++)
				{
					// we have rolled over into a new GDD year, write out what we have and reset
					if (data[i].Timestamp >= nextYear)
					{
						if (cumulus.GraphOptions.GrowingDegreeDaysVisible1 && growYear1.Length > 10)
						{
							// remove last comma
							growYear1.Length--;
							// close the year data
							growYear1.Append("],");
							// append to years array
							growdegdaysYears1.Append(growYear1);

							growYear1.Clear().Append($"\"{data[i].Timestamp.Year}\":[");
						}
						if (cumulus.GraphOptions.GrowingDegreeDaysVisible2 && growYear2.Length > 10)
						{
							// remove last comma
							growYear2.Length--;
							// close the year data
							growYear2.Append("],");
							// append to years array
							growdegdaysYears2.Append(growYear2);

							growYear2.Clear().Append($"\"{data[i].Timestamp.Year}\":[");
						}

						// reset the plot year for Southern hemisphere
						plotYear = cumulus.GrowingYearStarts < 3 ? 2000 : 1999;

						annualGrowingDegDays1 = 0;
						annualGrowingDegDays2 = 0;
						do
						{
							nextYear = nextYear.AddYears(1);
						}
						while (data[i].Timestamp >= nextYear);
					}

					// make all series the same year so they plot together
					// 2000 was a leap year, so make sure February falls in 2000
					// for Southern hemisphere this means the start year must be 1999
					if (cumulus.GrowingYearStarts > 2 && plotYear == 1999 && data[i].Timestamp.Month == 1)
					{
						plotYear++;
					}

					// make all series the same year so they plot together
					var recDate = Utils.ToGraphTime(new DateTime(plotYear, data[i].Timestamp.Month, data[i].Timestamp.Day));

					if (cumulus.GraphOptions.GrowingDegreeDaysVisible1 && data[i].LowTemp.HasValue && data[i].HighTemp.HasValue)
					{
						// growing degree days
						var gdd = MeteoLib.GrowingDegreeDays(WeatherStation.ConvertUserTempToC(data[i].HighTemp).Value, WeatherStation.ConvertUserTempToC(data[i].LowTemp).Value, WeatherStation.ConvertUserTempToC(cumulus.GrowingBase1).Value, cumulus.GrowingCap30C);

						// annual accumulation
						annualGrowingDegDays1 += gdd;

						growYear1.Append($"[{recDate},{annualGrowingDegDays1.ToString("F1", InvC)}],");
					}

					if (cumulus.GraphOptions.GrowingDegreeDaysVisible2 && data[i].LowTemp.HasValue && data[i].HighTemp.HasValue)
					{
						// growing degree days
						var gdd = MeteoLib.GrowingDegreeDays(WeatherStation.ConvertUserTempToC(data[i].HighTemp).Value, WeatherStation.ConvertUserTempToC(data[i].LowTemp).Value, WeatherStation.ConvertUserTempToC(cumulus.GrowingBase2).Value, cumulus.GrowingCap30C);

						// annual accumulation
						annualGrowingDegDays2 += gdd;

						growYear2.Append($"[{recDate},{annualGrowingDegDays2.ToString("F1", InvC)}],");
					}
				}
			}

			// remove last commas from the years arrays and close them off
			if (cumulus.GraphOptions.GrowingDegreeDaysVisible1)
			{
				if (growYear1[^1] == ',')
				{
					growYear1.Length--;
				}

				// have previous years been appended?
				if (growdegdaysYears1[^1] == ']')
				{
					growdegdaysYears1.Append(',');
				}

				growdegdaysYears1.Append(growYear1 + "]");

				// add to main json
				sb.Append("\"GDD1\":" + growdegdaysYears1 + "},");
			}
			if (cumulus.GraphOptions.GrowingDegreeDaysVisible2)
			{
				if (growYear2[^1] == ',')
				{
					growYear2.Length--;
				}

				// have previous years been appended?
				if (growdegdaysYears2[^1] == ']')
				{
					growdegdaysYears2.Append(',');
				}
				growdegdaysYears2.Append(growYear2 + "]");

				// add to main json
				sb.Append("\"GDD2\":" + growdegdaysYears2 + "},");
			}

			sb.Append(options);

			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllTempSumGraphData()
		{
			StringBuilder sb = new StringBuilder("{");
			StringBuilder tempSumYears0 = new StringBuilder("{", 32768);
			StringBuilder tempSumYears1 = new StringBuilder("{", 32768);
			StringBuilder tempSumYears2 = new StringBuilder("{", 32768);

			StringBuilder tempSum0 = new StringBuilder("[", 8600);
			StringBuilder tempSum1 = new StringBuilder("[", 8600);
			StringBuilder tempSum2 = new StringBuilder("[", 8600);

			DateTime nextYear;

			// 2000 was a leap year, so make sure February falls in 2000
			// for Southern hemisphere this means the start year must be 1999
			var plotYear = cumulus.TempSumYearStarts < 3 ? 2000 : 1999;

			int startYear;
			var annualTempSum0 = 0.0;
			var annualTempSum1 = 0.0;
			var annualTempSum2 = 0.0;

			var options = $"\"options\":{{\"sumBase1\":{cumulus.TempSumBase1},\"sumBase2\":{cumulus.TempSumBase2},\"startMon\":{cumulus.TempSumYearStarts}}}";

			// Read the day file list and extract the data from there
			var data = station.Database.Query<DayData>("select Timestamp, AvgTemp from DayData order by Timestamp");

			if (data.Count > 0 && (cumulus.GraphOptions.TempSumVisible0 || cumulus.GraphOptions.TempSumVisible1 || cumulus.GraphOptions.TempSumVisible2))
			{
				// we have to detect a new year is starting
				nextYear = new DateTime(data[0].Timestamp.Year, cumulus.TempSumYearStarts, 1);

				if (data[0].Timestamp >= nextYear)
				{
					nextYear = nextYear.AddYears(1);
				}

				// are we starting part way through a year that does not start in January?
				if (data[0].Timestamp.Year == nextYear.Year)
				{
					startYear = data[0].Timestamp.Year - 1;
				}
				else
				{
					startYear = data[0].Timestamp.Year;
				}

				if (cumulus.GraphOptions.TempSumVisible0)
				{
					tempSumYears0.Append($"\"{startYear}\":");
				}
				if (cumulus.GraphOptions.TempSumVisible1)
				{
					tempSumYears1.Append($"\"{startYear}\":");
				}
				if (cumulus.GraphOptions.TempSumVisible2)
				{
					tempSumYears2.Append($"\"{startYear}\":");
				}

				for (var i = 0; i < data.Count; i++)
				{
					// we have rolled over into a new GDD year, write out what we have and reset
					if (data[i].Timestamp >= nextYear)
					{
						if (cumulus.GraphOptions.TempSumVisible0 && tempSum0.Length > 10)
						{
							// remove last comma
							tempSum0.Length--;
							// close the year data
							tempSum0.Append("],");
							// append to years array
							tempSumYears0.Append(tempSum0);

							tempSum0.Clear().Append($"\"{data[i].Timestamp.Year}\":[");
						}
						if (cumulus.GraphOptions.TempSumVisible1 && tempSum1.Length > 10)
						{
							// remove last comma
							tempSum1.Length--;
							// close the year data
							tempSum1.Append("],");
							// append to years array
							tempSumYears1.Append(tempSum1);

							tempSum1.Clear().Append($"\"{data[i].Timestamp.Year}\":[");
						}
						if (cumulus.GraphOptions.TempSumVisible2 && tempSum2.Length > 10)
						{
							// remove last comma
							tempSum2.Length--;
							// close the year data
							tempSum2.Append("],");
							// append to years array
							tempSumYears2.Append(tempSum2);

							tempSum2.Clear().Append($"\"{data[i].Timestamp.Year}\":[");
						}

						// reset the plot year for Southern hemisphere
						plotYear = cumulus.TempSumYearStarts < 3 ? 2000 : 1999;

						annualTempSum0 = 0;
						annualTempSum1 = 0;
						annualTempSum2 = 0;

						do
						{
							nextYear = nextYear.AddYears(1);
						}
						while (data[i].Timestamp >= nextYear);
					}
					// make all series the same year so they plot together
					// 2000 was a leap year, so make sure February falls in 2000
					// for Southern hemisphere this means the start year must be 1999
					if (cumulus.TempSumYearStarts > 2 && plotYear == 1999 && data[i].Timestamp.Month == 1)
					{
						plotYear++;
					}

					var recDate = Utils.ToGraphTime(new DateTime(plotYear, data[i].Timestamp.Month, data[i].Timestamp.Day));

					if (cumulus.GraphOptions.TempSumVisible0)
					{
						// annual accumulation
						annualTempSum0 += data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value : 0;
						tempSum0.Append($"[{recDate},{annualTempSum0.ToString("F0", InvC)}],");
					}
					if (cumulus.GraphOptions.TempSumVisible1)
					{
						// annual accumulation
						annualTempSum1 += (data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value : 0) - cumulus.TempSumBase1;
						tempSum1.Append($"[{recDate},{annualTempSum1.ToString("F0", InvC)}],");
					}
					if (cumulus.GraphOptions.TempSumVisible2)
					{
						// annual accumulation
						annualTempSum2 += (data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value : 0) - cumulus.TempSumBase2;
						tempSum2.Append($"[{recDate},{annualTempSum2.ToString("F0", InvC)}],");
					}
				}
			}

			// remove last commas from the years arrays and close them off
			if (cumulus.GraphOptions.TempSumVisible0)
			{
				if (tempSum0[^1] == ',')
				{
					tempSum0.Length--;
				}

				// have previous years been appended?
				if (tempSumYears0[^1] == ']')
				{
					tempSumYears0.Append(',');
				}

				tempSumYears0.Append(tempSum0 + "]");

				// add to main json
				sb.Append("\"Sum0\":" + tempSumYears0 + "}");

				if (cumulus.GraphOptions.TempSumVisible1 || cumulus.GraphOptions.TempSumVisible2)
					sb.Append(',');
			}
			if (cumulus.GraphOptions.TempSumVisible1)
			{
				if (tempSum1[^1] == ',')
				{
					tempSum1.Length--;
				}

				// have previous years been appended?
				if (tempSumYears1[^1] == ']')
				{
					tempSumYears1.Append(',');
				}

				tempSumYears1.Append(tempSum1 + "]");

				// add to main json
				sb.Append("\"Sum1\":" + tempSumYears1 + "},");
			}
			if (cumulus.GraphOptions.TempSumVisible2)
			{
				if (tempSum2[^1] == ',')
				{
					tempSum2.Length--;
				}

				// have previous years been appended?
				if (tempSumYears2[^1] == ']')
				{
					tempSumYears2.Append(',');
				}

				tempSumYears2.Append(tempSum2 + "]");

				// add to main json
				sb.Append("\"Sum2\":" + tempSumYears2 + "},");
			}

			sb.Append(options);

			sb.Append('}');

			return sb.ToString();
		}
	}
}
