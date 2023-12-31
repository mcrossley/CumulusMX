using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentFTP.Helpers;
using MailKit;
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

		internal async Task CreateGraphDataFiles()
		{
			// Chart data for Highcharts graphs
			string json = "";
			for (var i = 0; i < cumulus.GraphDataFiles.Length; i++)
			{
				json = CreateGraphDataJson(cumulus.GraphDataFiles[i].LocalFileName, false);

				try
				{
						var dest = cumulus.GraphDataFiles[i].LocalPath + cumulus.GraphDataFiles[i].LocalFileName;
						using (var file = new StreamWriter(dest, false))
						{
							await file.WriteLineAsync(json);
							file.Close();
						}

						// The config files only need creating once per change
						// 0=graphconfig, 1=availabledata, 8=dailyrain, 9=dailytemp, 11=sunhours
						if (i == 0 || i == 1 || i == 8 || i == 9 || i == 11)
						{
							cumulus.GraphDataFiles[i].CreateRequired = false;
					}
				}
				catch (Exception ex)
				{
					cumulus.LogMessage($"Error writing {cumulus.GraphDataFiles[i].LocalFileName}: {ex}");
				}
			}
		}

		internal string CreateGraphDataJson(string filename, bool incremental)
		{
			// Chart data for Highcharts graphs

			switch (filename)
			{
				case "graphconfig.json":
					return GetGraphConfig(false);
				case "availabledata.json":
					return GetAvailGraphData(false);
				case "tempdata.json":
					return GetTempGraphData(incremental, false);
				case "pressdata.json":
					return GetPressGraphData(incremental);
				case "winddata.json":
					return GetWindGraphData(incremental);
				case "wdirdata.json":
					return GetWindDirGraphData(incremental);
				case "humdata.json":
					return GetHumGraphData(incremental, false);
				case "raindata.json":
					return GetRainGraphData(incremental);
				case "dailyrain.json":
					return GetDailyRainGraphData();
				case "dailytemp.json":
					return GetDailyTempGraphData(false);
				case "solardata.json":
					return GetSolarGraphData(incremental, false);
				case "sunhours.json":
					return GetSunHoursGraphData(false);
				case "airquality.json":
					return GetAqGraphData(incremental);
				case "extratempdata.json":
					return GetExtraTempGraphData(incremental, false);
				case "extrahumdata.json":
					return GetExtraHumGraphData(incremental, false);
				case "extradewdata.json":
					return GetExtraDewPointGraphData(incremental, false);
				case "soiltempdata.json":
					return GetSoilTempGraphData(incremental, false);
				case "soilmoistdata.json":
					return GetSoilMoistGraphData(incremental, false);
				case "leafwetdata.json":
					return GetLeafWetnessGraphData(incremental, false);
				case "usertempdata.json":
					return GetUserTempGraphData(incremental, false);
				case "co2sensordata.json":
					return GetCo2SensorGraphData(incremental, false);
			}
			return "{}";
		}

		internal void CreateEodGraphDataFiles()
		{
			for (var i = 0; i < cumulus.GraphDataEodFiles.Length; i++)
			{
				if (cumulus.GraphDataEodFiles[i].Create)
				{
					var json = CreateEodGraphDataJson(cumulus.GraphDataEodFiles[i].LocalFileName);

					try
					{
						var dest = cumulus.GraphDataEodFiles[i].LocalPath + cumulus.GraphDataEodFiles[i].LocalFileName;
						File.WriteAllText(dest, json);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"Error writing {cumulus.GraphDataEodFiles[i].LocalFileName}");
					}
				}

				// Now set the flag that upload is required (if enabled)
				cumulus.GraphDataEodFiles[i].FtpRequired = true;
				cumulus.GraphDataEodFiles[i].CopyRequired = true;
			}
		}

		internal void CreateDailyGraphDataFiles()
		{
			// skip 0 & 1 = config files
			// daily rain = 8
			// daily temp = 9
			// sun hours = 11
			var eod = new int[] { 8, 9, 11 };

			foreach (var i in eod)
			{
				if (cumulus.GraphDataFiles[i].Create)
				{
					var json = CreateGraphDataJson(cumulus.GraphDataFiles[i].LocalFileName, false);

					try
					{
						var dest = cumulus.GraphDataFiles[i].LocalPath + cumulus.GraphDataFiles[i].LocalFileName;
						File.WriteAllText(dest, json);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"Error writing {cumulus.GraphDataFiles[i].LocalFileName}");
					}
				}

				cumulus.GraphDataFiles[i].CopyRequired = true;
				cumulus.GraphDataFiles[i].FtpRequired = true;
			}
		}

		internal string CreateEodGraphDataJson(string filename)
		{
			switch (filename)
			{
				case "alldailytempdata.json":
					return GetAllDailyTempGraphData(false);
				case "alldailypressdata.json":
					return GetAllDailyPressGraphData();
				case "alldailywinddata.json":
					return GetAllDailyWindGraphData();
				case "alldailyhumdata.json":
					return GetAllDailyHumGraphData();
				case "alldailyraindata.json":
					return GetAllDailyRainGraphData();
				case "alldailysolardata.json":
					return GetAllDailySolarGraphData(false);
				case "alldailydegdaydata.json":
					return GetAllDegreeDaysGraphData(false);
				case "alltempsumdata.json":
					return GetAllTempSumGraphData(false);
			}
			return "{}";
		}

		//		internal string GetSolarGraphData(DateTime ts)
		internal string GetSolarGraphData(bool incremental, bool local, DateTime? start = null)
		{
			var sb = new StringBuilder("{");
			var sbUv = new StringBuilder("\"UV\":[");
			var sbSol = new StringBuilder("\"SolarRad\":[");
			var sbMax = new StringBuilder("\"CurrentSolarMax\":[");

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[10].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp > ? order by Timestamp", dateFrom);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
				{
					sbUv.Append($"[{recDate},{(data[i].UV.HasValue ? data[i].UV.Value.ToString(cumulus.UVFormat, InvC) : "null")}],");
				}

				if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
				{
					sbSol.Append($"[{recDate},{(data[i].SolarRad.HasValue ? data[i].SolarRad.Value : "null")}],");
					sbMax.Append($"[{recDate},{(data[i].SolarMax.HasValue ? data[i].SolarMax.Value : "null")}],");
				}
			}


			if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
			{
				if (sbUv[^1] == ',')
					sbUv.Length--;

				sbUv.Append(']');
				sb.Append(sbUv);
			}
			if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
			{
				if (sbSol[^1] == ',')
				{
					sbSol.Length--;
					sbMax.Length--;
				}

				sbSol.Append(']');
				sbMax.Append(']');
				if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
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

		internal string GetRainGraphData(bool incremental, DateTime? start = null)
		{
			var sb = new StringBuilder("{");
			var sbRain = new StringBuilder("\"rfall\":[");
			var sbRate = new StringBuilder("\"rrate\":[");

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[7].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp > ? order by Timestamp", dateFrom);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				sbRain.Append($"[{recDate},{(data[i].RainToday.HasValue ? data[i].RainToday.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
				sbRate.Append($"[{recDate},{(data[i].RainRate.HasValue ? data[i].RainRate.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
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

		internal string GetHumGraphData(bool incremental, bool local, DateTime? start = null)
		{
			var sb = new StringBuilder("{", 10240);
			var sbOut = new StringBuilder("\"hum\":[");
			var sbIn = new StringBuilder("\"inhum\":[");

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[6].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp > ? order by Timestamp", dateFrom);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
				{
					sbOut.Append($"[{recDate},{(data[i].Humidity.HasValue ? data[i].Humidity.Value : "null")}],");
				}
				if (cumulus.GraphOptions.Visible.InHum.IsVisible(local))
				{
					sbIn.Append($"[{recDate},{(data[i].IndoorHumidity.HasValue ? data[i].IndoorHumidity.Value : "null")}],");
				}
			}

			if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
			{
				if (sbOut[^1] == ',')
					sbOut.Length--;

				sbOut.Append(']');

				sb.Append(sbOut);
			}

			if (cumulus.GraphOptions.Visible.InHum.IsVisible(local))
			{
				if (sbIn[^1] == ',')
					sbIn.Length--;

				sbIn.Append(']');

				if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
					sb.Append(',');

				sb.Append(sbIn);
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetWindDirGraphData(bool incremental, DateTime? start = null)
		{
			var sb = new StringBuilder("{\"bearing\":[");
			var sbAvg = new StringBuilder("\"avgbearing\":[");

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[5].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp > ? order by Timestamp", dateFrom);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				sb.Append($"[{recDate},{(data[i].WindDir.HasValue ? data[i].WindDir : "null")}],");
				sbAvg.Append($"[{recDate},{(data[i].WindAvgDir.HasValue ? data[i].WindAvgDir : "null")}],");
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

		internal string GetWindGraphData(bool incremental, DateTime? start = null)
		{
			var sb = new StringBuilder("{\"wgust\":[");
			var sbSpd = new StringBuilder("\"wspeed\":[");

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[4].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp > ? order by Timestamp", dateFrom);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				sb.Append($"[{recDate},{(data[i].WindGust.HasValue ? data[i].WindGust.Value.ToString(cumulus.WindFormat, InvC) : "null")}],");
				sbSpd.Append($"[{recDate},{(data[i].WindSpeed.HasValue ? data[i].WindSpeed.Value.ToString(cumulus.WindAvgFormat, InvC) : "null")}],");
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

		internal string GetPressGraphData(bool incremental, DateTime? start = null)
		{
			StringBuilder sb = new StringBuilder("{\"press\":[");

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[3].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}


			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=? order by Timestamp", dateFrom);

			for (var i = 0; i < data.Count; i++)
			{
				sb.Append($"[{data[i].Timestamp * 1000},{(data[i].Pressure.HasValue ? data[i].Pressure.Value.ToString(cumulus.PressFormat, InvC) : "null")}],");
			}

			if (sb[^1] == ',')
				sb.Length--;

			sb.Append("]}");
			return sb.ToString();
		}

		internal string GetTempGraphData(bool incremental, bool local, DateTime? start = null)
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

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[2].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp >=? order by Timestamp", dateFrom);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				if (cumulus.GraphOptions.Visible.InTemp.IsVisible(local))
					sbIn.Append($"[{recDate},{(data[i].IndoorTemp.HasValue ? data[i].IndoorTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
					sbDew.Append($"[{recDate},{(data[i].DewPoint.HasValue ? data[i].DewPoint.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
					sbApp.Append($"[{recDate},{(data[i].AppTemp.HasValue ? data[i].AppTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
					sbFeel.Append($"[{recDate},{(data[i].FeelsLike.HasValue ? data[i].FeelsLike.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
					sbChill.Append($"[{recDate},{(data[i].WindChill.HasValue ? data[i].WindChill.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
					sbHeat.Append($"[{recDate},{(data[i].HeatIndex.HasValue ? data[i].HeatIndex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.Temp.IsVisible(local))
					sbTemp.Append($"[{recDate},{(data[i].OutsideTemp.HasValue ? data[i].OutsideTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
					sbHumidex.Append($"[{recDate},{(data[i].Humidex.HasValue ? data[i].Humidex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
			}

			if (cumulus.GraphOptions.Visible.InTemp.IsVisible(local))
			{
				if (sbIn[^1] == ',')
					sbIn.Length--;

				sbIn.Append(']');
				sb.Append(sbIn);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
			{
				if (sbDew[^1] == ',')
					sbDew.Length--;

				sbDew.Append(']');
				sb.Append((append ? "," : "") + sbDew);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
			{
				if (sbApp[^1] == ',')
					sbApp.Length--;

				sbApp.Append(']');
				sb.Append((append ? "," : "") + sbApp);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
			{
				if (sbFeel[^1] == ',')
					sbFeel.Length--;

				sbFeel.Append(']');
				sb.Append((append ? "," : "") + sbFeel);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
			{
				if (sbChill[^1] == ',')
					sbChill.Length--;

				sbChill.Append(']');
				sb.Append((append ? "," : "") + sbChill);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
			{
				if (sbHeat[^1] == ',')
					sbHeat.Length--;

				sbHeat.Append(']');
				sb.Append((append ? "," : "") + sbHeat);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.Temp.IsVisible(local))
			{
				if (sbTemp[^1] == ',')
					sbTemp.Length--;

				sbTemp.Append(']');
				sb.Append((append ? "," : "") + sbTemp);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
			{
				if (sbHumidex[^1] == ',')
					sbHumidex.Length--;

				sbHumidex.Append(']');
				sb.Append((append ? "," : "") + sbHumidex);
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetAqGraphData(bool incremental, DateTime? start = null)
		{
			bool append = false;
			var sb = new StringBuilder("{");
			var sb2p5 = new StringBuilder("\"pm2p5\":[");
			var sb10 = new StringBuilder(",\"pm10\":[");

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[12].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			// Check if we are to generate AQ data at all. Only if a primary sensor is defined and it isn't the Indoor AirLink
			if (cumulus.StationOptions.PrimaryAqSensor > (int) Cumulus.PrimaryAqSensor.Undefined
				&& cumulus.StationOptions.PrimaryAqSensor != (int) Cumulus.PrimaryAqSensor.AirLinkIndoor)
			{
				var data = station.Database.Query<RecentData>("select * from RecentData where Timestamp > ? order by Timestamp", dateFrom);

				for (var i = 0; i < data.Count; i++)
				{
					var recDate = data[i].Timestamp * 1000;

					sb2p5.Append($"[{recDate},{(data[i].Pm2p5.HasValue ? data[i].Pm2p5.Value.ToString("F1", InvC) : "null")}],");

					// Only the AirLink and Ecowitt CO2 servers provide PM10 values at the moment
					append = true;
					sb10.Append($"[{recDate},{(data[i].Pm10.HasValue ? data[i].Pm10.Value.ToString("F1", InvC) : "null")}],");
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

		internal string GetExtraTempGraphData(bool incremental, bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var InvC = new CultureInfo("");
			var sb = new StringBuilder("{", 10240);

			/* returns data in the form of an object with properties for each data series
			{
				"sensor 1": [[time,val],[time,val],...],
				"sensor 4": [[time,val],[time,val],...],
			}
			*/


			StringBuilder[] sbExt = new StringBuilder[cumulus.GraphOptions.Visible.ExtraTemp.Vals.Length];

			for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraTemp.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.ExtraTemp.ValVisible(i, local))
					sbExt[i] = new StringBuilder($"\"{cumulus.Trans.ExtraTempCaptions[i]}\":[");
			}

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[13].LastDataTime;
			}
			else if (start.HasValue && end.HasValue)
			{
				dateFrom = start.Value;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var dateTo = end ?? DateTime.Now;

			var data = station.Database.Query<ExtraTemp>("select * from ExtraTemp where Timestamp > ? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			foreach (var row in data)
			{
				for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraTemp.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.ExtraTemp.ValVisible(i, local))
					{
						var temp = (double?) row.GetType().GetProperty("Temp" + (i + 1)).GetValue(row);
						sbExt[i].Append($"[{row.Timestamp * 1000},{(temp.HasValue ? temp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
				}
			}

			for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraTemp.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.ExtraTemp.ValVisible(i, local))
				{
					if (sbExt[i][^1] == ',')
						sbExt[i].Length--;

					sbExt[i].Append(']');
					sb.Append((append ? "," : "") + sbExt[i]);
					append = true;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetExtraDewPointGraphData(bool incremental, bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var InvC = new CultureInfo("");
			var sb = new StringBuilder("{", 10240);

			/* returns data in the form of an object with properties for each data series
			{
				"sensor 1": [[time,val],[time,val],...],
				"sensor 4": [[time,val],[time,val],...],
			}
			*/

			StringBuilder[] sbExt = new StringBuilder[cumulus.GraphOptions.Visible.ExtraDewPoint.Vals.Length];

			for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraDewPoint.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.ExtraDewPoint.ValVisible(i, local))
					sbExt[i] = new StringBuilder($"\"{cumulus.Trans.ExtraDPCaptions[i]}\":[");
			}

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[15].LastDataTime;
			}
			else if (start.HasValue && end.HasValue)
			{
				dateFrom = start.Value;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var dateTo = end ?? DateTime.Now;

			var data = station.Database.Query<ExtraDewPoint>("select * from ExtraDewPoint where Timestamp > ? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			foreach (var row in data)
			{
				for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraDewPoint.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.ExtraDewPoint.ValVisible(i, local))
					{
						var temp = (double?)row.GetType().GetProperty("DewPoint" + (i + 1)).GetValue(row);
						sbExt[i].Append($"[{row.Timestamp * 1000},{(temp.HasValue ? temp.Value.ToString(cumulus.HumFormat, InvC) : "null")}],");
					}
				}
			}

			for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraDewPoint.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.ExtraDewPoint.ValVisible(i, local))
				{
					if (sbExt[i][^1] == ',')
						sbExt[i].Length--;

					sbExt[i].Append(']');
					sb.Append((append ? "," : "") + sbExt[i]);
					append = true;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetExtraHumGraphData(bool incremental, bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var InvC = new CultureInfo("");
			var sb = new StringBuilder("{", 10240);

			/* returns data in the form of an object with properties for each data series
			{
				"sensor 1": [[time,val],[time,val],...],
				"sensor 4": [[time,val],[time,val],...],
			}
			*/

			StringBuilder[] sbExt = new StringBuilder[cumulus.GraphOptions.Visible.ExtraHum.Vals.Length];

			for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraHum.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.ExtraHum.ValVisible(i, local))
					sbExt[i] = new StringBuilder($"\"{cumulus.Trans.ExtraHumCaptions[i]}\":[");
			}

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[14].LastDataTime;
			}
			else if (start.HasValue && end.HasValue)
			{
				dateFrom = start.Value;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var dateTo = end ?? DateTime.Now;

			var data = station.Database.Query<ExtraHum>("select * from ExtraHum where Timestamp > ? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			foreach (var row in data)
			{
				for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraHum.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.ExtraHum.ValVisible(i, local))
					{
						var temp = (double?)row.GetType().GetProperty("Hum" + (i + 1)).GetValue(row);
						sbExt[i].Append($"[{row.Timestamp * 1000},{(temp.HasValue ? temp.Value.ToString(cumulus.HumFormat, InvC) : "null")}],");
					}
				}
			}

			for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraHum.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.ExtraHum.ValVisible(i, local))
				{
					if (sbExt[i][^1] == ',')
						sbExt[i].Length--;

					sbExt[i].Append(']');
					sb.Append((append ? "," : "") + sbExt[i]);
					append = true;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetSoilTempGraphData(bool incremental, bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var InvC = new CultureInfo("");
			var sb = new StringBuilder("{", 10240);

			/* returns data in the form of an object with properties for each data series
			{
				"sensor 1": [[time,val],[time,val],...],
				"sensor 4": [[time,val],[time,val],...],
			}
			*/

			StringBuilder[] sbExt = new StringBuilder[cumulus.GraphOptions.Visible.SoilTemp.Vals.Length];

			for (var i = 0; i < cumulus.GraphOptions.Visible.SoilTemp.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.SoilTemp.ValVisible(i, local))
					sbExt[i] = new StringBuilder($"\"{cumulus.Trans.SoilTempCaptions[i]}\":[");
			}

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[16].LastDataTime;
			}
			else if (start.HasValue && end.HasValue)
			{
				dateFrom = start.Value;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var dateTo = end ?? DateTime.Now;

			var data = station.Database.Query<SoilTemp>("select * from SoilTemp where Timestamp > ? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			foreach (var row in data)
			{
				for (var i = 0; i < cumulus.GraphOptions.Visible.SoilTemp.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.SoilTemp.ValVisible(i, local))
					{
						var temp = (double?)row.GetType().GetProperty("Temp" + (i + 1)).GetValue(row);
						sbExt[i].Append($"[{row.Timestamp * 1000},{(temp.HasValue ? temp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
				}
			}

			for (var i = 0; i < cumulus.GraphOptions.Visible.SoilTemp.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.SoilTemp.ValVisible(i, local))
				{
					if (sbExt[i][^1] == ',')
						sbExt[i].Length--;

					sbExt[i].Append(']');
					sb.Append((append ? "," : "") + sbExt[i]);
					append = true;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetSoilMoistGraphData(bool incremental, bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var sb = new StringBuilder("{", 10240);

			/* returns data in the form of an object with properties for each data series
			{
				"sensor 1": [[time,val],[time,val],...],
				"sensor 4": [[time,val],[time,val],...],
			}
			*/

			StringBuilder[] sbExt = new StringBuilder[cumulus.GraphOptions.Visible.SoilMoist.Vals.Length];

			for (var i = 0; i < cumulus.GraphOptions.Visible.SoilMoist.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.SoilMoist.ValVisible(i, local))
					sbExt[i] = new StringBuilder($"\"{cumulus.Trans.SoilMoistureCaptions[i]}\":[");
			}

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[17].LastDataTime;
			}
			else if (start.HasValue && end.HasValue)
			{
				dateFrom = start.Value;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var dateTo = end ?? DateTime.Now;

			var data = station.Database.Query<SoilMoist>("select * from SoilMoist where Timestamp > ? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			foreach (var row in data)
			{
				for (var i = 0; i < 16; i++)
				{
					var temp = (int?)row.GetType().GetProperty("Moist" + (i + 1)).GetValue(row);
					sbExt[i].Append($"[{row.Timestamp * 1000},{(temp.HasValue ? temp.Value : "null")}],");
				}
			}

			for (var i = 0; i < cumulus.GraphOptions.Visible.SoilMoist.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.SoilMoist.ValVisible(i, local))
				{
					if (sbExt[i][^1] == ',')
						sbExt[i].Length--;

					sbExt[i].Append(']');
					sb.Append((append ? "," : "") + sbExt[i]);
					append = true;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetLeafWetnessGraphData(bool incremental, bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var sb = new StringBuilder("{", 10240);

			/* returns data in the form of an object with properties for each data series
			{
				"sensor 1": [[time,val],[time,val],...],
				"sensor 4": [[time,val],[time,val],...],
			}
			*/

			StringBuilder[] sbExt = new StringBuilder[cumulus.GraphOptions.Visible.LeafWetness.Vals.Length];

			for (var i = 0; i < cumulus.GraphOptions.Visible.LeafWetness.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.LeafWetness.ValVisible(i, local))
					sbExt[i] = new StringBuilder($"\"{cumulus.Trans.LeafWetnessCaptions[i]}\":[");
			}

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[20].LastDataTime;
			}
			else if (start.HasValue && end.HasValue)
			{
				dateFrom = start.Value;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var dateTo = end ?? DateTime.Now;

			var data = station.Database.Query<LeafWet>("select * from LeafWet where Timestamp > ? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			foreach (var row in data)
			{
				for (var i = 0; i < 16; i++)
				{
					var temp = (int?)row.GetType().GetProperty("Wet" + (i + 1)).GetValue(row);
					sbExt[i].Append($"[{row.Timestamp * 1000},{(temp.HasValue ? temp.Value : "null")}],");
				}
			}

			for (var i = 0; i < cumulus.GraphOptions.Visible.LeafWetness.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.LeafWetness.ValVisible(i, local))
				{
					if (sbExt[i][^1] == ',')
						sbExt[i].Length--;

					sbExt[i].Append(']');
					sb.Append((append ? "," : "") + sbExt[i]);
					append = true;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetUserTempGraphData(bool incremental, bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var InvC = new CultureInfo("");
			var sb = new StringBuilder("{", 10240);

			/* returns data in the form of an object with properties for each data series
						{
							"sensor 1": [[time,val],[time,val],...],
							"sensor 4": [[time,val],[time,val],...],

						}
						*/

			StringBuilder[] sbExt = new StringBuilder[cumulus.GraphOptions.Visible.UserTemp.Vals.Length];

			for (var i = 0; i < cumulus.GraphOptions.Visible.UserTemp.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.UserTemp.ValVisible(i, local))
					sbExt[i] = new StringBuilder($"\"{cumulus.Trans.UserTempCaptions[i]}\":[");
			}

			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[18].LastDataTime;
			}
			else if (start.HasValue && end.HasValue)
			{
				dateFrom = start.Value;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var dateTo = end ?? DateTime.Now;

			var data = station.Database.Query<UserTemp>("select * from UserTemp where Timestamp > ? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			foreach (var row in data)
			{
				for (var i = 0; i < cumulus.GraphOptions.Visible.UserTemp.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.UserTemp.ValVisible(i, local))
					{
						var temp = (double?)row.GetType().GetProperty("Temp" + (i + 1)).GetValue(row);
						sbExt[i].Append($"[{row.Timestamp * 1000},{(temp.HasValue ? temp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
				}
			}

			for (var i = 0; i < cumulus.GraphOptions.Visible.UserTemp.Vals.Length; i++)
			{
				if (cumulus.GraphOptions.Visible.UserTemp.ValVisible(i, local))
				{
					if (sbExt[i][^1] == ',')
						sbExt[i].Length--;

					sbExt[i].Append(']');
					sb.Append((append ? "," : "") + sbExt[i]);
					append = true;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetCo2SensorGraphData(bool incremental, bool local, DateTime? start = null)
		{
			bool append = false;
			var InvC = new CultureInfo("");
			var sb = new StringBuilder("{", 10240);

			var sbCo2 = new StringBuilder("\"CO2\":[");
			var sbCo2Avg = new StringBuilder("\"CO2 Average\":[");
			var sbPm25 = new StringBuilder("\"PM2.5\":[");
			var sbPm25Avg = new StringBuilder("\"PM 2.5 Average\":[");
			var sbPm10 = new StringBuilder("\"PM 10\":[");
			var sbPm10Avg = new StringBuilder("\"PM 10 Average\":[");
			var sbTemp = new StringBuilder("\"Temperature\":[");
			var sbHum = new StringBuilder("\"Humidity\":[");


			DateTime dateFrom;
			if (incremental)
			{
				dateFrom = start ?? cumulus.GraphDataFiles[19].LastDataTime;
			}
			else
			{
				dateFrom = DateTime.Now.AddHours(-cumulus.GraphHours);
			}

			var data = station.Database.Query<CO2Data>("select * from CO2Data where Timestamp > ? order by Timestamp", dateFrom);

			foreach (var row in data)
			{
				var tim = row.Timestamp * 1000;

				if (cumulus.GraphOptions.Visible.CO2Sensor.CO2.IsVisible(local))
					sbCo2.Append($"[{tim},{(row.CO2now.HasValue ? row.CO2now : "null")}],");

				if (cumulus.GraphOptions.Visible.CO2Sensor.CO2Avg.IsVisible(local))
					sbCo2Avg.Append($"[{tim},{(row.CO2avg.HasValue ? row.CO2avg : "null")}],");

				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25.IsVisible(local))
					sbPm25.Append($"[{tim},{(row.Pm2p5.HasValue ? row.Pm2p5.Value.ToString("F1", InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25Avg.IsVisible(local))
					sbPm25Avg.Append($"[{tim},{(row.Pm2p5avg.HasValue ? row.Pm2p5avg.Value.ToString("F1", InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10.IsVisible(local))
					sbPm10.Append($"[{tim},{(row.Pm10.HasValue ? row.Pm10.Value.ToString("F1", InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10Avg.IsVisible(local))
					sbPm10Avg.Append($"[{tim},{(row.Pm10avg.HasValue ? row.Pm10avg.Value.ToString("F1", InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.CO2Sensor.Temp.IsVisible(local))
					sbTemp.Append($"[{tim},{(row.Temp.HasValue ? row.Temp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.CO2Sensor.Hum.IsVisible(local))
					sbHum.Append($"[{tim},{(row.Hum.HasValue ? row.Hum.Value.ToString(cumulus.HumFormat, InvC) : "null")}],");
			}


			if (cumulus.GraphOptions.Visible.CO2Sensor.CO2.IsVisible(local))
			{
				if (sbCo2[^1] == ',')
					sbCo2.Length--;

				sbCo2.Append(']');
				sb.Append(sbCo2);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.CO2Sensor.CO2Avg.IsVisible(local))
			{
				if (sbCo2Avg[^1] == ',')
				sbCo2Avg.Length--;

				sbCo2Avg.Append(']');
				sb.Append((append ? "," : "") + sbCo2Avg);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25.IsVisible(local))
			{
				if (sbPm25[^1] == ',')
				sbPm25.Length--;

				sbPm25.Append(']');
				sb.Append((append ? "," : "") + sbPm25);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25Avg.IsVisible(local))
			{
				if (sbPm25Avg[^1] == ',')
				sbPm25Avg.Length--;

				sbPm25Avg.Append(']');
				sb.Append((append ? "," : "") + sbPm25Avg);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10.IsVisible(local))
			{
				if (sbPm10[^1] == ',')
				sbPm10.Length--;

				sbPm10.Append(']');
				sb.Append((append ? "," : "") + sbPm10);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10Avg.IsVisible(local))
			{
				if (sbPm10Avg[^1] == ',')
				sbPm10Avg.Length--;

				sbPm10Avg.Append(']');
				sb.Append((append ? "," : "") + sbPm10Avg);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.CO2Sensor.Temp.IsVisible(local))
			{
				if (sbTemp[^1] == ',')
				sbTemp.Length--;

				sbTemp.Append(']');
				sb.Append((append ? "," : "") + sbTemp);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.CO2Sensor.Hum.IsVisible(local))
			{
				if (sbHum[^1] == ',')
				sbHum.Length--;

				sbHum.Append(']');
				sb.Append((append ? "," : "") + sbHum);
				append = true;
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetIntervalTempGraphData(bool local, DateTime? start = null, DateTime? end = null)
		{
			bool append = false;
			var InvC = new CultureInfo("");
			var sb = new StringBuilder("{", 10240);
			var sbIn = new StringBuilder("\"intemp\":[");
			var sbDew = new StringBuilder("\"dew\":[");
			var sbApp = new StringBuilder("\"apptemp\":[");
			var sbFeel = new StringBuilder("\"feelslike\":[");
			var sbChill = new StringBuilder("\"wchill\":[");
			var sbHeat = new StringBuilder("\"heatindex\":[");
			var sbTemp = new StringBuilder("\"temp\":[");
			var sbHumidex = new StringBuilder("\"humidex\":[");

			var dateFrom = start ?? cumulus.RecordsBeganDateTime;
			var dateTo = end ?? DateTime.Now.Date;
			dateTo = dateTo.AddDays(1);

			var data = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >=? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				if (cumulus.GraphOptions.Visible.InTemp.IsVisible(local))
					sbIn.Append($"[{recDate},{(data[i].InsideTemp.HasValue ? data[i].InsideTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
					sbDew.Append($"[{recDate},{(data[i].DewPoint.HasValue ? data[i].DewPoint.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
					sbApp.Append($"[{recDate},{(data[i].Apparent.HasValue ? data[i].Apparent.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
					sbFeel.Append($"[{recDate},{(data[i].FeelsLike.HasValue ? data[i].FeelsLike.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
					sbChill.Append($"[{recDate},{(data[i].WindChill.HasValue ? data[i].WindChill.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
					sbHeat.Append($"[{recDate},{(data[i].HeatIndex.HasValue ? data[i].HeatIndex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.Temp.IsVisible(local))
					sbTemp.Append($"[{recDate},{(data[i].Temp.HasValue ? data[i].Temp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

				if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
					sbHumidex.Append($"[{recDate},{(data[i].Humidex.HasValue ? data[i].Humidex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
			}

			if (cumulus.GraphOptions.Visible.InTemp.IsVisible(local))
			{
				if (sbIn[^1] == ',')
					sbIn.Length--;

				sbIn.Append(']');
				sb.Append(sbIn);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
			{
				if (sbDew[^1] == ',')
					sbDew.Length--;

				sbDew.Append(']');
				sb.Append((append ? "," : "") + sbDew);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
			{
				if (sbApp[^1] == ',')
					sbApp.Length--;

				sbApp.Append(']');
				sb.Append((append ? "," : "") + sbApp);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
			{
				if (sbFeel[^1] == ',')
					sbFeel.Length--;

				sbFeel.Append(']');
				sb.Append((append ? "," : "") + sbFeel);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
			{
				if (sbChill[^1] == ',')
					sbChill.Length--;

				sbChill.Append(']');
				sb.Append((append ? "," : "") + sbChill);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
			{
				if (sbHeat[^1] == ',')
					sbHeat.Length--;

				sbHeat.Append(']');
				sb.Append((append ? "," : "") + sbHeat);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.Temp.IsVisible(local))
			{
				if (sbTemp[^1] == ',')
					sbTemp.Length--;

				sbTemp.Append(']');
				sb.Append((append ? "," : "") + sbTemp);
				append = true;
			}

			if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
			{
				if (sbHumidex[^1] == ',')
					sbHumidex.Length--;

				sbHumidex.Append(']');
				sb.Append((append ? "," : "") + sbHumidex);
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetIntervalHumGraphData(bool local, DateTime? start = null, DateTime? end = null)
		{
			var sb = new StringBuilder("{", 10240);
			var sbOut = new StringBuilder("\"hum\":[");
			var sbIn = new StringBuilder("\"inhum\":[");

			var dateFrom = start ?? cumulus.RecordsBeganDateTime;
			var dateTo = end ?? DateTime.Now.Date;
			dateTo = dateTo.AddDays(1);

			var data = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >=? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
				{
					sbOut.Append($"[{recDate},{(data[i].Humidity.HasValue ? data[i].Humidity.Value : "null")}],");
				}
				if (cumulus.GraphOptions.Visible.InHum.IsVisible(local))
				{
					sbIn.Append($"[{recDate},{(data[i].InsideHumidity.HasValue ? data[i].InsideHumidity.Value : "null")}],");
				}
			}

			if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
			{
				if (sbOut[^1] == ',')
					sbOut.Length--;

				sbOut.Append(']');

				sb.Append(sbOut);
			}

			if (cumulus.GraphOptions.Visible.InHum.IsVisible(local))
			{
				if (sbIn[^1] == ',')
					sbIn.Length--;

				sbIn.Append(']');

				if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
					sb.Append(',');

				sb.Append(sbIn);
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetIntervalSolarGraphData(bool local, DateTime? start = null, DateTime? end = null)
		{
			var sb = new StringBuilder("{");
			var sbUv = new StringBuilder("\"UV\":[");
			var sbSol = new StringBuilder("\"SolarRad\":[");
			var sbMax = new StringBuilder("\"CurrentSolarMax\":[");

			var dateFrom = start ?? cumulus.RecordsBeganDateTime;
			var dateTo = end ?? DateTime.Now.Date;
			dateTo = dateTo.AddDays(1);

			var data = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >=? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
				{
					sbUv.Append($"[{recDate},{(data[i].UV.HasValue ? data[i].UV.Value.ToString(cumulus.UVFormat, InvC) : "null")}],");
				}

				if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
				{
					sbSol.Append($"[{recDate},{(data[i].SolarRad.HasValue ? data[i].SolarRad.Value : "null")}],");
					sbMax.Append($"[{recDate},{(data[i].SolarMax.HasValue ? data[i].SolarMax.Value : "null")}],");
				}
			}


			if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
			{
				if (sbUv[^1] == ',')
					sbUv.Length--;

				sbUv.Append(']');
				sb.Append(sbUv);
			}
			if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
			{
				if (sbSol[^1] == ',')
				{
					sbSol.Length--;
					sbMax.Length--;
				}

				sbSol.Append(']');
				sbMax.Append(']');
				if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
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

		internal string GetIntervalPressGraphData(DateTime? start = null, DateTime? end = null)
		{
			StringBuilder sb = new StringBuilder("{\"press\":[");

			var dateFrom = start ?? cumulus.RecordsBeganDateTime;
			var dateTo = end ?? DateTime.Now.Date;
			dateTo = dateTo.AddDays(1);

			var data = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >=? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			for (var i = 0; i < data.Count; i++)
			{
				sb.Append($"[{data[i].Timestamp * 1000},{(data[i].Pressure.HasValue ? data[i].Pressure.Value.ToString(cumulus.PressFormat, InvC) : "null")}],");
			}

			if (sb[^1] == ',')
				sb.Length--;

			sb.Append("]}");
			return sb.ToString();
		}

		internal string GetIntervalWindGraphData(DateTime? start = null, DateTime? end = null)
		{
			var sb = new StringBuilder("{\"wgust\":[");
			var sbSpd = new StringBuilder("\"wspeed\":[");

			var dateFrom = start ?? cumulus.RecordsBeganDateTime;
			var dateTo = end ?? DateTime.Now.Date;
			dateTo = dateTo.AddDays(1);

			var data = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >=? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				sb.Append($"[{recDate},{(data[i].WindGust10m.HasValue ? data[i].WindGust10m.Value.ToString(cumulus.WindFormat, InvC) : "null")}],");
				sbSpd.Append($"[{recDate},{(data[i].WindAvg.HasValue ? data[i].WindAvg.Value.ToString(cumulus.WindAvgFormat, InvC) : "null")}],");
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

		internal string GetIntervalRainGraphData(DateTime? start = null, DateTime? end = null)
		{
			var sb = new StringBuilder("{");
			var sbRain = new StringBuilder("\"rfall\":[");
			var sbRate = new StringBuilder("\"rrate\":[");

			var dateFrom = start ?? cumulus.RecordsBeganDateTime;
			var dateTo = end ?? DateTime.Now.Date;
			dateTo = dateTo.AddDays(1);

			var data = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >=? and Timestamp <= ? order by Timestamp", dateFrom, dateTo);

			for (var i = 0; i < data.Count; i++)
			{
				var recDate = data[i].Timestamp * 1000;

				sbRain.Append($"[{recDate},{(data[i].RainToday.HasValue ? data[i].RainToday.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
				sbRate.Append($"[{recDate},{(data[i].RainRate.HasValue ? data[i].RainRate.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
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

		internal string GetAvailGraphData(bool local = true)
		{
			var json = new StringBuilder(200);

			// Temp values
			json.Append("{\"Temperature\":[");

			if (cumulus.GraphOptions.Visible.Temp.IsVisible(local))
				json.Append("\"Temperature\",");

			if (cumulus.GraphOptions.Visible.InTemp.IsVisible(local))
				json.Append("\"Indoor Temp\",");

			if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
				json.Append("\"Heat Index\",");

			if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
				json.Append("\"Dew Point\",");

			if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
				json.Append("\"Wind Chill\",");

			if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
				json.Append("\"Apparent Temp\",");

			if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
				json.Append("\"Feels Like\",");

			if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
				json.Append("\"Humidex\",");

			if (json[^1] == ',')
				json.Length--;

			// humidity values
			json.Append("],\"Humidity\":[");

			if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
				json.Append("\"Humidity\",");

			if (cumulus.GraphOptions.Visible.InHum.IsVisible(local))
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

			if (cumulus.GraphOptions.Visible.MinTemp.IsVisible(local) || cumulus.GraphOptions.Visible.MaxTemp.IsVisible(local) || cumulus.GraphOptions.Visible.AvgTemp.IsVisible(local))
			{
				json.Append(",\"DailyTemps\":[");

				if (cumulus.GraphOptions.Visible.AvgTemp.IsVisible(local))
					json.Append("\"AvgTemp\",");
				if (cumulus.GraphOptions.Visible.MaxTemp.IsVisible(local))
					json.Append("\"MaxTemp\",");
				if (cumulus.GraphOptions.Visible.MinTemp.IsVisible(local))
					json.Append("\"MinTemp\",");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// solar values
			if (cumulus.GraphOptions.Visible.Solar.IsVisible(local) || cumulus.GraphOptions.Visible.UV.IsVisible(local))
			{
				json.Append(",\"Solar\":[");

				if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
					json.Append("\"Solar Rad\",");

				if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
					json.Append("\"UV Index\",");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Sunshine
			if (cumulus.GraphOptions.Visible.Sunshine.IsVisible(local))
			{
				json.Append(",\"Sunshine\":[\"sunhours\"]");
			}

			// air quality
			// Check if we are to generate AQ data at all. Only if a primary sensor is defined and it isn't the Indoor AirLink
			if (cumulus.StationOptions.PrimaryAqSensor > (int) Cumulus.PrimaryAqSensor.Undefined
				&& cumulus.StationOptions.PrimaryAqSensor != (int) Cumulus.PrimaryAqSensor.AirLinkIndoor)
			{
				json.Append(",\"AirQuality\":[");
				json.Append("\"PM 2.5\"");

				// Only the AirLink and Ecowitt CO2 servers provide PM10 values at the moment
				if (cumulus.StationOptions.PrimaryAqSensor == (int) Cumulus.PrimaryAqSensor.AirLinkOutdoor ||
					cumulus.StationOptions.PrimaryAqSensor == (int) Cumulus.PrimaryAqSensor.AirLinkIndoor ||
					cumulus.StationOptions.PrimaryAqSensor == (int) Cumulus.PrimaryAqSensor.EcowittCO2)
				{
					json.Append(",\"PM 10\"");
				}
				json.Append(']');
			}

			// Degree Days
			if (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local) || cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local))
			{
				json.Append(",\"DegreeDays\":[");
				if (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local))
					json.Append("\"GDD1\",");

				if (cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local))
					json.Append("\"GDD2\"");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Temp Sum
			if (cumulus.GraphOptions.Visible.TempSum0.IsVisible(local) || cumulus.GraphOptions.Visible.TempSum1.IsVisible(local) || cumulus.GraphOptions.Visible.TempSum2.IsVisible(local))
			{
				json.Append(",\"TempSum\":[");
				if (cumulus.GraphOptions.Visible.TempSum0.IsVisible(local))
					json.Append("\"Sum0\",");
				if (cumulus.GraphOptions.Visible.TempSum1.IsVisible(local))
					json.Append("\"Sum1\",");
				if (cumulus.GraphOptions.Visible.TempSum2.IsVisible(local))
					json.Append("\"Sum2\"");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Extra temperature
			if (cumulus.GraphOptions.Visible.ExtraTemp.IsVisible(local))
			{
				json.Append(",\"ExtraTemp\":[");
				for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraTemp.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.ExtraTemp.ValVisible(i, local))
						json.Append($"\"{cumulus.Trans.ExtraTempCaptions[i + 1]}\",");
				}
				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Extra humidity
			if (cumulus.GraphOptions.Visible.ExtraHum.IsVisible(local))
			{
				json.Append(",\"ExtraHum\":[");
				for (var i = 0; i < cumulus.GraphOptions.Visible.ExtraHum.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.ExtraHum.ValVisible(i, local))
						json.Append($"\"{cumulus.Trans.ExtraHumCaptions[i + 1]}\",");
				}
				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Soil Temp
			if (cumulus.GraphOptions.Visible.SoilTemp.IsVisible(local))
			{
				json.Append(",\"SoilTemp\":[");
				for (var i = 0; i < cumulus.GraphOptions.Visible.SoilTemp.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.SoilTemp.ValVisible(i, local))
						json.Append($"\"{cumulus.Trans.SoilTempCaptions[i + 1]}\",");
				}
				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Soil Moisture
			if (cumulus.GraphOptions.Visible.SoilMoist.IsVisible(local))
			{
				json.Append(",\"SoilMoist\":[");
				for (var i = 0; i < cumulus.GraphOptions.Visible.SoilMoist.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.SoilMoist.ValVisible(i, local))
						json.Append($"\"{cumulus.Trans.SoilMoistureCaptions[i + 1]}\",");
				}
				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// User Temp
			if (cumulus.GraphOptions.Visible.UserTemp.IsVisible(local))
			{
				json.Append(",\"UserTemp\":[");
				for (var i = 0; i < cumulus.GraphOptions.Visible.UserTemp.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.UserTemp.ValVisible(i, local))
						json.Append($"\"{cumulus.Trans.UserTempCaptions[i + 1]}\",");
				}
				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// Leaf wetness
			if (cumulus.GraphOptions.Visible.LeafWetness.IsVisible(local))
			{
				json.Append(",\"LeafWetness\":[");
				for (var i = 0; i < cumulus.GraphOptions.Visible.LeafWetness.Vals.Length; i++)
				{
					if (cumulus.GraphOptions.Visible.LeafWetness.ValVisible(i, local))
						json.Append($"\"{cumulus.Trans.LeafWetnessCaptions[i]}\",");
				}
				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			// CO2
			if (cumulus.GraphOptions.Visible.CO2Sensor.IsVisible(local))
			{
				json.Append(",\"CO2\":[");
				if (cumulus.GraphOptions.Visible.CO2Sensor.CO2.IsVisible(local))
					json.Append("\"CO2\",");
				if (cumulus.GraphOptions.Visible.CO2Sensor.CO2Avg.IsVisible(local))
					json.Append("\"CO2Avg\",");
				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25.IsVisible(local))
					json.Append("\"PM25\",");
				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25Avg.IsVisible(local))
					json.Append("\"PM25Avg\",");
				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10.IsVisible(local))
					json.Append("\"PM10\",");
				if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10Avg.IsVisible(local))
					json.Append("\"PM10Avg\",");
				if (cumulus.GraphOptions.Visible.CO2Sensor.Temp.IsVisible(local))
					json.Append("\"Temp\",");
				if (cumulus.GraphOptions.Visible.CO2Sensor.Hum.IsVisible(local))
					json.Append("\"Hum\"");

				if (json[^1] == ',')
					json.Length--;

				json.Append(']');
			}

			json.Append('}');
			return json.ToString();
		}

		internal string GetGraphConfig(bool local)
		{
			var json = new StringBuilder(200);
			json.Append('{');
			json.Append($"\"temp\":{{\"units\":\"{cumulus.Units.TempText[1]}\",\"decimals\":{cumulus.TempDPlaces}}},");
			json.Append($"\"wind\":{{\"units\":\"{cumulus.Units.WindText}\",\"avgdecimals\":{cumulus.WindAvgDPlaces},\"gustdecimals\":{cumulus.WindDPlaces},\"rununits\":\"{cumulus.Units.WindRunText}\"}},");
			json.Append($"\"rain\":{{\"units\":\"{cumulus.Units.RainText}\",\"decimals\":{cumulus.RainDPlaces}}},");
			json.Append($"\"press\":{{\"units\":\"{cumulus.Units.PressText}\",\"decimals\":{cumulus.PressDPlaces}}},");
			json.Append($"\"hum\":{{\"decimals\":{cumulus.HumDPlaces}}},");
			json.Append($"\"uv\":{{\"decimals\":{cumulus.UVDPlaces}}},");
			json.Append($"\"soilmoisture\":{{\"units\":\"{cumulus.Units.SoilMoistureUnitText}\"}},");
			json.Append($"\"co2\":{{\"units\":\"{cumulus.Units.CO2UnitText}\"}},");
			json.Append($"\"leafwet\":{{\"units\":\"{cumulus.Units.LeafWetnessUnitText}\",\"decimals\":{cumulus.LeafWetDPlaces}}},");
			json.Append($"\"aq\":{{\"units\":\"{cumulus.Units.AirQualityUnitText}\"}},");
			json.Append($"\"timezone\":\"{cumulus.StationOptions.TimeZone}\",");

			#region data series

			json.Append("\"series\":{");

			#region recent

			// temp
			if (cumulus.GraphOptions.Visible.Temp.IsVisible(local))
				json.Append($"\"temp\":{{\"name\":\"Temperature\",\"colour\":\"{cumulus.GraphOptions.Colour.Temp}\"}},");
			if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
				json.Append($"\"apptemp\":{{\"name\":\"Apparent Temperature\",\"colour\":\"{cumulus.GraphOptions.Colour.AppTemp}\"}},");
			if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
				json.Append($"\"feelslike\":{{\"name\":\"Feels Like\",\"colour\":\"{cumulus.GraphOptions.Colour.FeelsLike}\"}},");
			if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
				json.Append($"\"wchill\":{{\"name\":\"Wind Chill\",\"colour\":\"{cumulus.GraphOptions.Colour.WindChill}\"}},");
			if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
				json.Append($"\"heatindex\":{{\"name\":\"Heat Index\",\"colour\":\"{cumulus.GraphOptions.Colour.HeatIndex}\"}},");
			if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
				json.Append($"\"dew\":{{\"name\":\"Dew Point\",\"colour\":\"{cumulus.GraphOptions.Colour.DewPoint}\"}},");
			if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
				json.Append($"\"humidex\":{{\"name\":\"Humidex\",\"colour\":\"{cumulus.GraphOptions.Colour.Humidex}\"}},");
			if (cumulus.GraphOptions.Visible.InTemp.IsVisible(local))
				json.Append($"\"intemp\":{{\"name\":\"Indoor Temp\",\"colour\":\"{cumulus.GraphOptions.Colour.InTemp}\"}},");
			// hum
			if (cumulus.GraphOptions.Visible.OutHum.IsVisible(local))
				json.Append($"\"hum\":{{\"name\":\"Humidity\",\"colour\":\"{cumulus.GraphOptions.Colour.OutHum}\"}},");
			if (cumulus.GraphOptions.Visible.InHum.IsVisible(local))
				json.Append($"\"inhum\":{{\"name\":\"Humidity\",\"colour\":\"{cumulus.GraphOptions.Colour.InHum}\"}},");
			// press
			json.Append($"\"press\":{{\"name\":\"Pressure\",\"colour\":\"{cumulus.GraphOptions.Colour.Press}\"}},");
			// wind
			json.Append($"\"wspeed\":{{\"name\":\"Wind Speed\",\"colour\":\"{cumulus.GraphOptions.Colour.WindAvg}\"}},");
			json.Append($"\"wgust\":{{\"name\":\"Wind Gust\",\"colour\":\"{cumulus.GraphOptions.Colour.WindGust}\"}},");
			json.Append($"\"windrun\":{{\"name\":\"Wind Run\",\"colour\":\"{cumulus.GraphOptions.Colour.WindRun}\"}},");
			json.Append($"\"bearing\":{{\"name\":\"Bearing\",\"colour\":\"{cumulus.GraphOptions.Colour.WindBearing}\"}},");
			json.Append($"\"avgbearing\":{{\"name\":\"Average Bearing\",\"colour\":\"{cumulus.GraphOptions.Colour.WindBearingAvg}\"}},");
			// rain
			json.Append($"\"rfall\":{{\"name\":\"Rainfall\",\"colour\":\"{cumulus.GraphOptions.Colour.Rainfall}\"}},");
			json.Append($"\"rrate\":{{\"name\":\"Rainfall Rate\",\"colour\":\"{cumulus.GraphOptions.Colour.RainRate}\"}},");
			// solar
			if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
				json.Append($"\"solarrad\":{{\"name\":\"Solar Irradiation\",\"colour\":\"{cumulus.GraphOptions.Colour.Solar}\"}},");
			json.Append($"\"currentsolarmax\":{{\"name\":\"Solar theoretical\",\"colour\":\"{cumulus.GraphOptions.Colour.SolarTheoretical}\"}},");
			if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
				json.Append($"\"uv\":{{\"name\":\"UV-I\",\"colour\":\"{cumulus.GraphOptions.Colour.UV}\"}},");
			if (cumulus.GraphOptions.Visible.Sunshine.IsVisible(local))
				json.Append($"\"sunshine\":{{\"name\":\"Sunshine\",\"colour\":\"{cumulus.GraphOptions.Colour.Sunshine}\"}},");
			// aq
			json.Append($"\"pm2p5\":{{\"name\":\"PM 2.5\",\"colour\":\"{cumulus.GraphOptions.Colour.Pm2p5}\"}},");
			json.Append($"\"pm10\":{{\"name\":\"PM 10\",\"colour\":\"{cumulus.GraphOptions.Colour.Pm10}\"}},");

			#endregion recent

			#region daily

			// growing deg days
			if (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local))
				json.Append($"\"growingdegreedays1\":{{\"name\":\"GDD#1\"}},");
			if (cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local))
				json.Append($"\"growingdegreedays2\":{{\"name\":\"GDD#2\"}},");
			// TODO: temp sum

			// daily temps
			if (cumulus.GraphOptions.Visible.AvgTemp.IsVisible(local))
				json.Append($"\"avgtemp\":{{\"name\":\"Average Temp\",\"colour\":\"{cumulus.GraphOptions.Colour.AvgTemp}\"}},");
			if (cumulus.GraphOptions.Visible.MaxTemp.IsVisible(local))
				json.Append($"\"maxtemp\":{{\"name\":\"Maximum Temp\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxTemp}\"}},");
			if (cumulus.GraphOptions.Visible.MinTemp.IsVisible(local))
				json.Append($"\"mintemp\":{{\"name\":\"Minimum Temp\",\"colour\":\"{cumulus.GraphOptions.Colour.MinTemp}\"}},");

			json.Append($"\"maxpress\":{{\"name\":\"High Pressure\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxPress}\"}},");
			json.Append($"\"minpress\":{{\"name\":\"Low Pressure\",\"colour\":\"{cumulus.GraphOptions.Colour.MinPress}\"}},");

			json.Append($"\"maxhum\":{{\"name\":\"Maximum Humidity\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxOutHum}\"}},");
			json.Append($"\"minhum\":{{\"name\":\"Minimum Humidity\",\"colour\":\"{cumulus.GraphOptions.Colour.MinOutHum}\"}},");

			if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
			{
				json.Append($"\"mindew\":{{\"name\":\"Minumim Dew Point\",\"colour\":\"{cumulus.GraphOptions.Colour.MinDew}\"}},");
				json.Append($"\"maxdew\":{{\"name\":\"Maximum Dew Point\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxDew}\"}},");
			}
			if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
				json.Append($"\"minwindchill\":{{\"name\":\"Wind Chill\",\"colour\":\"{cumulus.GraphOptions.Colour.MinWindChill}\"}},");
			if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
			{
				json.Append($"\"minapp\":{{\"name\":\"Minumim Apparent\",\"colour\":\"{cumulus.GraphOptions.Colour.MinApp}\"}},");
				json.Append($"\"maxapp\":{{\"name\":\"Maximum Apparent\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxApp}\"}},");
			}
			if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
			{
				json.Append($"\"minfeels\":{{\"name\":\"Minumim Feels Like\",\"colour\":\"{cumulus.GraphOptions.Colour.MinFeels}\"}},");
				json.Append($"\"maxfeels\":{{\"name\":\"Maximum Feels Like\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxFeels}\"}},");
			}
			if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
				json.Append($"\"maxheatindex\":{{\"name\":\"Heat Index\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxHeatIndex}\"}},");
			if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
				json.Append($"\"maxhumidex\":{{\"name\":\"Humidex\",\"colour\":\"{cumulus.GraphOptions.Colour.MaxHumidex}\"}},");

			#endregion daily

			#region extra sensors

			// extra temp
			if (cumulus.GraphOptions.Visible.ExtraTemp.IsVisible(local))
				json.Append($"\"extratemp\":{{\"name\":[\"{string.Join("\",\"", cumulus.Trans.ExtraTempCaptions)}\"],\"colour\":[\"{string.Join("\",\"", cumulus.GraphOptions.Colour.ExtraTemp)}\"]}},");
			// extra hum
			if (cumulus.GraphOptions.Visible.ExtraHum.IsVisible(local))
				json.Append($"\"extrahum\":{{\"name\":[\"{string.Join("\",\"", cumulus.Trans.ExtraHumCaptions)}\"],\"colour\":[\"{string.Join("\",\"", cumulus.GraphOptions.Colour.ExtraHum)}\"]}},");
			// extra dewpoint
			if (cumulus.GraphOptions.Visible.ExtraDewPoint.IsVisible(local))
				json.Append($"\"extradew\":{{\"name\":[\"{string.Join("\",\"", cumulus.Trans.ExtraDPCaptions)}\"],\"colour\":[\"{string.Join("\",\"", cumulus.GraphOptions.Colour.ExtraDewPoint)}\"]}},");
			// extra user temps
			if (cumulus.GraphOptions.Visible.UserTemp.IsVisible(local))
				json.Append($"\"usertemp\":{{\"name\":[\"{string.Join("\",\"", cumulus.Trans.UserTempCaptions)}\"],\"colour\":[\"{string.Join("\",\"", cumulus.GraphOptions.Colour.UserTemp)}\"]}},");
			// soil temps
			if (cumulus.GraphOptions.Visible.SoilTemp.IsVisible(local))
				json.Append($"\"soiltemp\":{{\"name\":[\"{string.Join("\",\"", cumulus.Trans.SoilTempCaptions)}\"],\"colour\":[\"{string.Join("\",\"", cumulus.GraphOptions.Colour.SoilTemp)}\"]}},");
			// soil temps
			if (cumulus.GraphOptions.Visible.SoilMoist.IsVisible(local))
				json.Append($"\"soilmoist\":{{\"name\":[\"{string.Join("\",\"", cumulus.Trans.SoilMoistureCaptions)}\"],\"colour\":[\"{string.Join("\",\"", cumulus.GraphOptions.Colour.SoilMoist)}\"]}},");
			// leaf wetness
			if (cumulus.GraphOptions.Visible.LeafWetness.IsVisible(local))
				json.Append($"\"leafwet\":{{\"name\":[\"{string.Join("\",\"", cumulus.Trans.LeafWetnessCaptions)}\"],\"colour\":[\"{string.Join("\",\"", cumulus.GraphOptions.Colour.LeafWetness)}\"]}},");

			// CO2
			json.Append("\"co2\":{");
			if (cumulus.GraphOptions.Visible.CO2Sensor.CO2.IsVisible(local))
				json.Append($"\"co2\":{{\"name\":\"CO₂\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.CO2}\"}},");
			if (cumulus.GraphOptions.Visible.CO2Sensor.CO2Avg.IsVisible(local))
				json.Append($"\"co2average\":{{\"name\":\"CO₂ Average\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.CO2Avg}\"}},");
			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10.IsVisible(local))
				json.Append($"\"pm10\":{{\"name\":\"PM 10\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.Pm10}\"}},");
			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm10Avg.IsVisible(local))
				json.Append($"\"pm10average\":{{\"name\":\"PM 10 Avg\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.Pm10Avg}\"}},");
			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25.IsVisible(local))
				json.Append($"\"pm2.5\":{{\"name\":\"PM 2.5\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.Pm25}\"}},");
			if (cumulus.GraphOptions.Visible.CO2Sensor.Pm25.IsVisible(local))
				json.Append($"\"pm2.5average\":{{\"name\":\"PM 2.5 Avg\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.Pm25Avg}\"}},");
			if (cumulus.GraphOptions.Visible.CO2Sensor.Hum.IsVisible(local))
				json.Append($"\"humidity\":{{\"name\":\"Humidity\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.Hum}\"}},");
			if (cumulus.GraphOptions.Visible.CO2Sensor.Temp.IsVisible(local))
				json.Append($"\"temperature\":{{\"name\":\"Temperature\",\"colour\":\"{cumulus.GraphOptions.Colour.CO2Sensor.Temp}\"}}");
			// remove trailing comma
			if (json[^1] == ',')
				json.Length--;
			json.Append("},");

			#endregion extra sensors

			// remove trailing comma
			json.Length--;
			json.Append('}');

			#endregion data series

			json.Append('}');
			return json.ToString();
		}

		internal string GetSelectaChartOptions()
		{
			return JsonSerializer.SerializeToString(cumulus.SelectaChartOptions);
		}

		internal string GetSelectaPeriodOptions()
		{
			return JsonSerializer.SerializeToString(cumulus.SelectaPeriodOptions);
		}

		internal string GetDailyRainGraphData()
		{
		var dataFrom = DateTime.Now.ToUnixTime() - (cumulus.GraphDays + 1) * 86400;

		StringBuilder sb = new StringBuilder("{\"dailyrain\":[", 10000);

		var data = station.Database.Query<DayData>("select Timestamp, TotalRain from DayData where Timestamp >= ? and TotalRain is not null order by Timestamp", dataFrom);

		for (var i = 0; i < data.Count; i++)
		{
				sb.Append($"[{data[i].Timestamp * 1000},{(data[i].TotalRain.HasValue ? data[i].TotalRain.Value.ToString(cumulus.RainFormat, InvC) : "null")}],");
		}
		// remove trailing comma
		if (data.Count > 0)
			sb.Length--;

		sb.Append("]}");
		return sb.ToString();
		}

		internal string GetSunHoursGraphData(bool local)
		{
			StringBuilder sb = new StringBuilder("{", 10000);
			if (cumulus.GraphOptions.Visible.Sunshine.IsVisible(local))
			{
				var dataFrom = DateTime.Now.ToUnixTime() - (cumulus.GraphDays + 1) * 86400;
				var data = station.Database.Query<DayData>("select Timestamp, SunShineHours from Daydata where Timestamp >= ? and SunShineHours is not null order by Timestamp", dataFrom);

				sb.Append("\"sunhours\":[");
				if (data.Count > 0)
				{
					for (var i = 0; i < data.Count; i++)
					{
						sb.Append($"[{data[i].Timestamp * 1000},{data[i].SunShineHours.Value.ToString(cumulus.SunFormat, InvC)}],");
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

		internal string GetDailyTempGraphData(bool local)
		{
			var dataFrom = DateTime.Now.ToUnixTime() - (cumulus.GraphDays + 1) * 86400;
			var data = station.Database.Query<DayData>("select Timestamp, HighTemp, LowTemp, AvgTemp from DayData where Timestamp >= ? order by Timestamp", dataFrom);
			var append = false;
			StringBuilder sb = new StringBuilder("{");

			if (data.Count > 0)
			{
				if (cumulus.GraphOptions.Visible.MinTemp.IsVisible(local))
				{
					sb.Append("\"mintemp\":[");

					for (var i = 0; i < data.Count; i++)
					{
						sb.Append($"[{data[i].Timestamp * 1000},{(data[i].LowTemp.HasValue ? data[i].LowTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
					// remove trailing comma
					if (data.Count > 0)
						sb.Length--;

					sb.Append(']');
					append = true;
				}

				if (cumulus.GraphOptions.Visible.MaxTemp.IsVisible(local))
				{
					if (append)
						sb.Append(',');

					sb.Append("\"maxtemp\":[");

					for (var i = 0; i < data.Count; i++)
					{
						sb.Append($"[{data[i].Timestamp * 1000},{(data[i].HighTemp.HasValue ? data[i].HighTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}				// remove trailing comma
					if (data.Count > 0)
						sb.Length--;

					sb.Append(']');
					append = true;
				}

				if (cumulus.GraphOptions.Visible.AvgTemp.IsVisible(local))
				{
					if (append)
						sb.Append(',');

					sb.Append("\"avgtemp\":[");
					for (var i = 0; i < data.Count; i++)
					{
						sb.Append($"[{data[i].Timestamp * 1000},{(data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
					// remove trailing comma
					if (data.Count > 0)
						sb.Length--;

					sb.Append(']');
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		internal string GetAllDailyTempGraphData(bool local)
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
					var recDate = data[i].Timestamp * 1000;
					// lo temp
					if (cumulus.GraphOptions.Visible.MinTemp.IsVisible(local))
						minTemp.Append($"[{recDate},{(data[i].LowTemp.HasValue ? data[i].LowTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					// hi temp
					if (cumulus.GraphOptions.Visible.MaxTemp.IsVisible(local))
						maxTemp.Append($"[{recDate},{(data[i].HighTemp.HasValue ? data[i].HighTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					// avg temp
					if (cumulus.GraphOptions.Visible.AvgTemp.IsVisible(local))
						avgTemp.Append($"[{recDate},{(data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");


					if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
					{
						// hi heat index
						heatIdx.Append($"[{recDate},{(data[i].HighHeatIndex.HasValue ? data[i].HighHeatIndex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
					if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
					{
						// hi app temp
						maxApp.Append($"[{recDate},{(data[i].HighAppTemp.HasValue ? data[i].HighAppTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

						// lo app temp
						minApp.Append($"[{recDate},{(data[i].LowAppTemp.HasValue ? data[i].LowAppTemp.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
					// lo wind chill
					if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
					{
						windChill.Append($"[{recDate},{(data[i].LowWindChill.HasValue ? data[i].LowWindChill.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
					{
						// hi dewpt
						maxDew.Append($"[{recDate},{(data[i].HighDewPoint.HasValue ? data[i].HighDewPoint.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

						// lo dewpt
						minDew.Append($"[{recDate},{(data[i].LowDewPoint.HasValue ? data[i].LowDewPoint.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
					{
						// hi feels like
						maxFeels.Append($"[{recDate},{(data[i].HighFeelsLike.HasValue ? data[i].HighFeelsLike.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");

						// lo feels like
						minFeels.Append($"[{recDate},{(data[i].LowFeelsLike.HasValue ? data[i].LowFeelsLike.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
					{
						// hi humidex
						humidex.Append($"[{recDate},{(data[i].HighHumidex.HasValue ? data[i].HighHumidex.Value.ToString(cumulus.TempFormat, InvC) : "null")}],");
					}
				}
			}
			if (cumulus.GraphOptions.Visible.MinTemp.IsVisible(local))
			{
				if (minTemp.Length > 1) minTemp.Length--;
				sb.Append("\"minTemp\":" + minTemp.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.MaxTemp.IsVisible(local))
			{
				if (maxTemp.Length > 1) maxTemp.Length--;
				sb.Append("\"maxTemp\":" + maxTemp.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.AvgTemp.IsVisible(local))
			{
				if (avgTemp.Length > 1) avgTemp.Length--;
				sb.Append("\"avgTemp\":" + avgTemp.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.HeatIndex.IsVisible(local))
			{
				if (heatIdx.Length > 1) heatIdx.Length--;
				sb.Append("\"heatIndex\":" + heatIdx.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.AppTemp.IsVisible(local))
			{
				if (maxApp.Length > 1) maxApp.Length--;
				if (minApp.Length > 1) minApp.Length--;
				sb.Append("\"maxApp\":" + maxApp.ToString() + "],");
				sb.Append("\"minApp\":" + minApp.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.WindChill.IsVisible(local))
			{
				if (windChill.Length > 1) windChill.Length--;
				sb.Append("\"windChill\":" + windChill.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.DewPoint.IsVisible(local))
			{
				if (maxDew.Length > 1) maxDew.Length--;
				if (minDew.Length > 1) minDew.Length--;
				sb.Append("\"maxDew\":" + maxDew.ToString() + "],");
				sb.Append("\"minDew\":" + minDew.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.FeelsLike.IsVisible(local))
			{
				if (maxFeels.Length > 1) maxFeels.Length--;
				if (minFeels.Length > 1) minFeels.Length--;
				sb.Append("\"maxFeels\":" + maxFeels.ToString() + "],");
				sb.Append("\"minFeels\":" + minFeels.ToString() + "],");
			}
			if (cumulus.GraphOptions.Visible.Humidex.IsVisible(local))
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
					var recDate = data[i].Timestamp * 1000;

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

					var recDate = data[i].Timestamp * 1000;

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

					var recDate = data[i].Timestamp * 1000;

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
					var recDate = data[i].Timestamp * 1000;

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
					var recDate = data[i].Timestamp * 1000;

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

		internal string GetAllDailySolarGraphData(bool local)
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
					var recDate = data[i].Timestamp * 1000;

					if (cumulus.GraphOptions.Visible.Sunshine.IsVisible(local))
					{
						// sunshine hours
						sunHours.Append($"[{recDate},{(data[i].SunShineHours.HasValue ? data[i].SunShineHours.Value.ToString(InvC) : "null")}],");
					}

					if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
					{
						// hi solar rad
						solarRad.Append($"[{recDate},{(data[i].HighSolar.HasValue ? data[i].HighSolar.Value : "null")}],");
					}

					if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
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
			if (cumulus.GraphOptions.Visible.Sunshine.IsVisible(local))
				sb.Append("\"sunHours\":" + sunHours.ToString() + "]");

			if (cumulus.GraphOptions.Visible.Solar.IsVisible(local))
			{
				if (cumulus.GraphOptions.Visible.Sunshine.IsVisible(local))
					sb.Append(',');

				sb.Append("\"solarRad\":" + solarRad.ToString() + "]");
			}

			if (cumulus.GraphOptions.Visible.UV.IsVisible(local))
			{
				if (cumulus.GraphOptions.Visible.Sunshine.IsVisible(local) || cumulus.GraphOptions.Visible.Solar.IsVisible(local))
					sb.Append(',');

				sb.Append("\"uvi\":" + uvi.ToString() + "]");
			}
			sb.Append('}');

			return sb.ToString();
		}

		internal string GetAllDegreeDaysGraphData(bool local)
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

			if (data.Count > 0 && (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local) || cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local)))
			{
				// we have to detect a new growing deg day year is starting
				nextYear = new DateTime(data[0].Date.Year, cumulus.GrowingYearStarts, 1);

				if (data[0].Date >= nextYear)
				{
					nextYear = nextYear.AddYears(1);
				}

				// are we starting part way through a year that does not start in January?
				if (data[0].Date.Year == nextYear.Year)
				{
					startYear = data[0].Date.Year - 1;
				}
				else
				{
					startYear = data[0].Date.Year;
				}

				if (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local))
				{
					growdegdaysYears1.Append($"\"{startYear}\":");
				}
				if (cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local))
				{
					growdegdaysYears2.Append($"\"{startYear}\":");
				}


				for (var i = 0; i < data.Count; i++)
				{
					// we have rolled over into a new GDD year, write out what we have and reset
					if (data[i].Date >= nextYear)
					{
						if (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local) && growYear1.Length > 10)
						{
							// remove last comma
							growYear1.Length--;
							// close the year data
							growYear1.Append("],");
							// append to years array
							growdegdaysYears1.Append(growYear1);

							growYear1.Clear().Append($"\"{data[i].Date.Year}\":[");
						}
						if (cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local) && growYear2.Length > 10)
						{
							// remove last comma
							growYear2.Length--;
							// close the year data
							growYear2.Append("],");
							// append to years array
							growdegdaysYears2.Append(growYear2);

							growYear2.Clear().Append($"\"{data[i].Date.Year}\":[");
						}

						// reset the plot year for Southern hemisphere
						plotYear = cumulus.GrowingYearStarts < 3 ? 2000 : 1999;

						annualGrowingDegDays1 = 0;
						annualGrowingDegDays2 = 0;
						do
						{
							nextYear = nextYear.AddYears(1);
						}
						while (data[i].Date >= nextYear);
					}

					// make all series the same year so they plot together
					// 2000 was a leap year, so make sure February falls in 2000
					// for Southern hemisphere this means the start year must be 1999
					if (cumulus.GrowingYearStarts > 2 && plotYear == 1999 && data[i].Date.Month == 1)
					{
						plotYear++;
					}

					// make all series the same year so they plot together
					var recDate = Utils.ToGraphTime(new DateTime(plotYear, data[i].Date.Month, data[i].Date.Day));

					if (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local) && data[i].LowTemp.HasValue && data[i].HighTemp.HasValue)
					{
						// growing degree days
						var gdd = MeteoLib.GrowingDegreeDays(ConvertUnits.UserTempToC(data[i].HighTemp).Value, ConvertUnits.UserTempToC(data[i].LowTemp).Value, ConvertUnits.UserTempToC(cumulus.GrowingBase1).Value, cumulus.GrowingCap30C);

						// annual accumulation
						annualGrowingDegDays1 += gdd;

						growYear1.Append($"[{recDate},{annualGrowingDegDays1.ToString("F1", InvC)}],");
					}

					if (cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local) && data[i].LowTemp.HasValue && data[i].HighTemp.HasValue)
					{
						// growing degree days
						var gdd = MeteoLib.GrowingDegreeDays(ConvertUnits.UserTempToC(data[i].HighTemp).Value, ConvertUnits.UserTempToC(data[i].LowTemp).Value, ConvertUnits.UserTempToC(cumulus.GrowingBase2).Value, cumulus.GrowingCap30C);

						// annual accumulation
						annualGrowingDegDays2 += gdd;

						growYear2.Append($"[{recDate},{annualGrowingDegDays2.ToString("F1", InvC)}],");
					}
				}
			}

			// remove last commas from the years arrays and close them off
			if (cumulus.GraphOptions.Visible.GrowingDegreeDays1.IsVisible(local))
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
			if (cumulus.GraphOptions.Visible.GrowingDegreeDays2.IsVisible(local))
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

		internal string GetAllTempSumGraphData(bool local)
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

			if (data.Count > 0 && (cumulus.GraphOptions.Visible.TempSum0.IsVisible(local) || cumulus.GraphOptions.Visible.TempSum1.IsVisible(local) || cumulus.GraphOptions.Visible.TempSum2.IsVisible(local)))
			{
				// we have to detect a new year is starting
				nextYear = new DateTime(data[0].Date.Year, cumulus.TempSumYearStarts, 1);

				if (data[0].Date >= nextYear)
				{
					nextYear = nextYear.AddYears(1);
				}

				// are we starting part way through a year that does not start in January?
				if (data[0].Date.Year == nextYear.Year)
				{
					startYear = data[0].Date.Year - 1;
				}
				else
				{
					startYear = data[0].Date.Year;
				}

				if (cumulus.GraphOptions.Visible.TempSum0.IsVisible(local))
				{
					tempSumYears0.Append($"\"{startYear}\":");
				}
				if (cumulus.GraphOptions.Visible.TempSum1.IsVisible(local))
				{
					tempSumYears1.Append($"\"{startYear}\":");
				}
				if (cumulus.GraphOptions.Visible.TempSum2.IsVisible(local))
				{
					tempSumYears2.Append($"\"{startYear}\":");
				}

				for (var i = 0; i < data.Count; i++)
				{
					// we have rolled over into a new GDD year, write out what we have and reset
					if (data[i].Date >= nextYear)
					{
						if (cumulus.GraphOptions.Visible.TempSum0.IsVisible(local) && tempSum0.Length > 10)
						{
							// remove last comma
							tempSum0.Length--;
							// close the year data
							tempSum0.Append("],");
							// append to years array
							tempSumYears0.Append(tempSum0);

							tempSum0.Clear().Append($"\"{data[i].Date.Year}\":[");
						}
						if (cumulus.GraphOptions.Visible.TempSum1.IsVisible(local) && tempSum1.Length > 10)
						{
							// remove last comma
							tempSum1.Length--;
							// close the year data
							tempSum1.Append("],");
							// append to years array
							tempSumYears1.Append(tempSum1);

							tempSum1.Clear().Append($"\"{data[i].Date.Year}\":[");
						}
						if (cumulus.GraphOptions.Visible.TempSum2.IsVisible(local) && tempSum2.Length > 10)
						{
							// remove last comma
							tempSum2.Length--;
							// close the year data
							tempSum2.Append("],");
							// append to years array
							tempSumYears2.Append(tempSum2);

							tempSum2.Clear().Append($"\"{data[i].Date.Year}\":[");
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
						while (data[i].Date >= nextYear);
					}
					// make all series the same year so they plot together
					// 2000 was a leap year, so make sure February falls in 2000
					// for Southern hemisphere this means the start year must be 1999
					if (cumulus.TempSumYearStarts > 2 && plotYear == 1999 && data[i].Date.Month == 1)
					{
						plotYear++;
					}

					var recDate = Utils.ToPseudoJSTime(new DateTime(plotYear, data[i].Date.Month, data[i].Date.Day));

					if (cumulus.GraphOptions.Visible.TempSum0.IsVisible(local))
					{
						// annual accumulation
						annualTempSum0 += data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value : 0;
						tempSum0.Append($"[{recDate},{annualTempSum0.ToString("F0", InvC)}],");
					}
					if (cumulus.GraphOptions.Visible.TempSum1.IsVisible(local))
					{
						// annual accumulation
						annualTempSum1 += (data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value : 0) - cumulus.TempSumBase1;
						tempSum1.Append($"[{recDate},{annualTempSum1.ToString("F0", InvC)}],");
					}
					if (cumulus.GraphOptions.Visible.TempSum2.IsVisible(local))
					{
						// annual accumulation
						annualTempSum2 += (data[i].AvgTemp.HasValue ? data[i].AvgTemp.Value : 0) - cumulus.TempSumBase2;
						tempSum2.Append($"[{recDate},{annualTempSum2.ToString("F0", InvC)}],");
					}
				}
			}

			// remove last commas from the years arrays and close them off
			if (cumulus.GraphOptions.Visible.TempSum0.IsVisible(local))
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

				if (cumulus.GraphOptions.Visible.TempSum1.IsVisible(local) || cumulus.GraphOptions.Visible.TempSum2.IsVisible(local))
					sb.Append(',');
			}
			if (cumulus.GraphOptions.Visible.TempSum1.IsVisible(local))
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
			if (cumulus.GraphOptions.Visible.TempSum2.IsVisible(local))
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
