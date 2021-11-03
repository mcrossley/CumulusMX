using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using ServiceStack;

namespace CumulusMX
{
	internal class LogFileEditors
	{
		private WeatherStation station;
		private readonly Cumulus cumulus;
		private readonly System.Globalization.NumberFormatInfo invNum = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

		internal LogFileEditors(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}


		internal void SetStation(WeatherStation stat)
		{
			station = stat;
		}

		#region Dayfile

		/// <summary>
		/// Return lines from dayfile.txt in json format
		/// </summary>
		/// <param name="draw"></param>
		/// <param name="start"></param>
		/// <param name="length"></param>
		/// <returns>JSON encoded section of the dayfile as a string</returns>
		internal async Task<string> GetDayfile(string draw, int start, int length)
		{
			try
			{
				var total = await station.DatabaseAsync.ExecuteScalarAsync<int>("select count(*) from DayData");
				var rows = await station.DatabaseAsync.QueryAsync<DailyData>("select * from DayData order by Timestamp limit ?,?", start, length);
				var json = new StringBuilder(350 * rows.Count);

				json.Append("{\"draw\":" + draw);
				json.Append(",\"recordsTotal\":" + total);
				json.Append(",\"recordsFiltered\":" + total);
				json.Append(",\"data\":[");

				var lineNum = start + 1; // Start is zero relative

				foreach (var row in rows)
				{
					json.Append($"[{lineNum++},");
					json.Append(row.ToString());
					json.Append("],");
				}

				// trim last ","
				json.Length--;
				json.Append("]}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}


		internal async Task<string> EditDayFile(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DayFileEditor>();



			// read dayfile into a List
			var lines = File.ReadAllLines(cumulus.DayFileName).ToList();

			var lineNum = newData.line - 1; // our List is zero relative

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new DailyData();
				newRec.FromString(newData.data);

				_ = station.Database.Update(newRec);

				// Update the dayfile
				if (Program.cumulus.ProgramOptions.UpdateDayfile)
				{
					// replace the edited line
					//var orgLine = lines[lineNum];
					var newLine = string.Join(",", newData.data);

					lines[lineNum] = newLine;

					// write dayfile back again
					File.WriteAllLines(cumulus.DayFileName, lines);
				}


				// Update the MySQL record
				if (!string.IsNullOrEmpty(cumulus.MySqlConnSettings.Server) &&
					!string.IsNullOrEmpty(cumulus.MySqlConnSettings.UserID) &&
					!string.IsNullOrEmpty(cumulus.MySqlConnSettings.Password) &&
					!string.IsNullOrEmpty(cumulus.MySqlConnSettings.Database) &&
					cumulus.MySqlSettings.UpdateOnEdit
					)
				{
					var updateStr = "";

					try
					{
						var updt = new StringBuilder(1024);

						updt.Append($"UPDATE {cumulus.MySqlSettings.Dayfile.TableName} SET ");
						if (newRec.HighGust.HasValue) updt.Append($"HighWindGust={newRec.HighGust.Value.ToString(cumulus.WindFormat, invNum)},");
						if (newRec.HighGustBearing.HasValue) updt.Append($"HWindGBear={newRec.HighGustBearing.Value},");
						if (newRec.HighGustTime.HasValue) updt.Append($"THWindG={newRec.HighGustTime.Value:\\'HH:mm\\'},");
						if (newRec.LowTemp.HasValue) updt.Append($"MinTemp={newRec.LowTemp.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.LowTempTime.HasValue) updt.Append($"TMinTemp={newRec.LowTempTime.Value:\\'HH:mm\\'},");
						if (newRec.HighTemp.HasValue) updt.Append($"MaxTemp={newRec.HighTemp.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighTempTime.HasValue) updt.Append($"TMaxTemp={newRec.HighTempTime.Value:\\'HH:mm\\'},");
						if (newRec.LowPress.HasValue) updt.Append($"MinPress={newRec.LowPress.Value.ToString(cumulus.PressFormat, invNum)},");
						if (newRec.LowPressTime.HasValue) updt.Append($"TMinPress={newRec.LowPressTime.Value:\\'HH:mm\\'},");
						if (newRec.HighPress.HasValue) updt.Append($"MaxPress={newRec.HighPress.Value.ToString(cumulus.PressFormat, invNum)},");
						if (newRec.HighPressTime.HasValue) updt.Append($"TMaxPress={newRec.HighPressTime:\\'HH:mm\\'},");
						if (newRec.HighRainRate.HasValue) updt.Append($"MaxRainRate={newRec.HighRainRate.Value.ToString(cumulus.RainFormat, invNum)},");
						if (newRec.HighRainRateTime.HasValue) updt.Append($"TMaxRR={newRec.HighRainRateTime.Value:\\'HH:mm\\'},");
						if (newRec.TotalRain.HasValue) updt.Append($"TotRainFall={newRec.TotalRain.Value.ToString(cumulus.RainFormat, invNum)},");
						if (newRec.AvgTemp.HasValue) updt.Append($"AvgTemp={newRec.AvgTemp.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.WindRun.HasValue) updt.Append($"TotWindRun={newRec.WindRun.Value.ToString("F1", invNum)},");
						if (newRec.HighAvgWind.HasValue) updt.Append($"HighAvgWSpeed={newRec.HighAvgWind.Value.ToString(cumulus.WindAvgFormat, invNum)},");
						if (newRec.HighAvgWindTime.HasValue) updt.Append($"THAvgWSpeed={newRec.HighAvgWindTime.Value:\\'HH:mm\\'},");
						if (newRec.LowHumidity.HasValue) updt.Append($"LowHum={newRec.LowHumidity.Value},");
						if (newRec.LowHumidityTime.HasValue) updt.Append($"TLowHum={newRec.LowHumidityTime.Value:\\'HH:mm\\'},");
						if (newRec.HighHumidity.HasValue) updt.Append($"HighHum={newRec.HighHumidity.Value},");
						if (newRec.HighHumidityTime.HasValue) updt.Append($"THighHum={newRec.HighHumidityTime.Value:\\'HH:mm\\'},");
						if (newRec.ET.HasValue) updt.Append($"TotalEvap={newRec.ET.Value.ToString(cumulus.ETFormat, invNum)},");
						if (newRec.SunShineHours.HasValue) updt.Append($"HoursSun={newRec.SunShineHours.Value.ToString(cumulus.SunFormat, invNum)},");
						if (newRec.HighHeatIndex.HasValue) updt.Append($"HighHeatInd={newRec.HighHeatIndex.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighHeatIndexTime.HasValue) updt.Append($"THighHeatInd={newRec.HighHeatIndexTime.Value:\\'HH:mm\\'},");
						if (newRec.HighAppTemp.HasValue) updt.Append($"HighAppTemp={newRec.HighAppTemp.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighAppTempTime.HasValue) updt.Append($"THighAppTemp={newRec.HighAppTempTime.Value:\\'HH:mm\\'},");
						if (newRec.LowAppTemp.HasValue) updt.Append($"LowAppTemp={newRec.LowAppTemp.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.LowAppTempTime.HasValue) updt.Append($"TLowAppTemp={newRec.LowAppTempTime.Value:\\'HH:mm\\'},");
						if (newRec.HighHourlyRain.HasValue) updt.Append($"HighHourRain={newRec.HighHourlyRain.Value.ToString(cumulus.RainFormat, invNum)},");
						if (newRec.HighHourlyRainTime.HasValue) updt.Append($"THighHourRain={newRec.HighHourlyRainTime.Value:\\'HH:mm\\'},");
						if (newRec.LowWindChill.HasValue) updt.Append($"LowWindChill={newRec.LowWindChill.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.LowWindChillTime.HasValue) updt.Append($"TLowWindChill={newRec.LowWindChillTime.Value:\\'HH:mm\\'},");
						if (newRec.HighDewPoint.HasValue) updt.Append($"HighDewPoint={newRec.HighDewPoint.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighDewPointTime.HasValue) updt.Append($"THighDewPoint={newRec.HighDewPointTime.Value:\\'HH:mm\\'},");
						if (newRec.LowDewPoint.HasValue) updt.Append($"LowDewPoint={newRec.LowDewPoint.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.LowDewPointTime.HasValue) updt.Append($"TLowDewPoint={newRec.LowDewPointTime.Value:\\'HH:mm\\'},");
						if (newRec.DominantWindBearing.HasValue) updt.Append($"DomWindDir={newRec.DominantWindBearing.Value},");
						if (newRec.HeatingDegreeDays.HasValue) updt.Append($"HeatDegDays={newRec.HeatingDegreeDays.Value.ToString("F1", invNum)},");
						if (newRec.CoolingDegreeDays.HasValue) updt.Append($"CoolDegDays={newRec.CoolingDegreeDays.Value.ToString("F1", invNum)},");
						if (newRec.HighSolar.HasValue) updt.Append($"HighSolarRad={newRec.HighSolar.Value},");
						if (newRec.HighSolarTime.HasValue) updt.Append($"THighSolarRad={newRec.HighSolarTime.Value:\\'HH:mm\\'},");
						if (newRec.HighUv.HasValue) updt.Append($"HighUV={newRec.HighUv.Value.ToString(cumulus.UVFormat, invNum)},");
						if (newRec.HighUvTime.HasValue) updt.Append($"THighUV={newRec.HighUvTime.Value:\\'HH:mm\\'},");
						if (newRec.HighGustBearing.HasValue) updt.Append($"HWindGBearSym='{station.CompassPoint(newRec.HighGustBearing.Value)}',");
						if (newRec.DominantWindBearing.HasValue) updt.Append($"DomWindDirSym='{station.CompassPoint(newRec.DominantWindBearing.Value)}',");
						if (newRec.HighFeelsLike.HasValue) updt.Append($"MaxFeelsLike={newRec.HighFeelsLike.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighFeelsLikeTime.HasValue) updt.Append($"TMaxFeelsLike={newRec.HighFeelsLikeTime.Value:\\'HH:mm\\'},");
						if (newRec.LowFeelsLike.HasValue) updt.Append($"MinFeelsLike={newRec.LowFeelsLike.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.LowFeelsLikeTime.HasValue) updt.Append($"TMinFeelsLike={newRec.LowFeelsLikeTime.Value:\\'HH:mm\\'},");
						if (newRec.HighHumidex.HasValue) updt.Append($"MaxHumidex={newRec.HighHumidex.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighHumidexTime.HasValue) updt.Append($"TMaxHumidex={newRec.HighHumidexTime.Value:\\'HH:mm\\'} ");

						updt.Append($"WHERE LogDate='{newRec.Timestamp:yyyy-MM-dd}';");
						updateStr = updt.ToString();

						cumulus.MySqlCommandSync(updateStr, "EditDayFile");
						Cumulus.LogMessage($"EditDayFile: SQL Updated");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditDayFile: Failed, to update MySQL. Error");
						Cumulus.LogMessage($"EditDayFile: SQL Update statement = {updateStr}");
						context.Response.StatusCode = 501;  // Use 501 to signal that SQL failed but file update was OK
						var thisrec = new List<string>(newData.data);
						thisrec.Insert(0, newData.line.ToString());

						return "{\"errors\":{\"Dayfile\":[\"<br>Updated the dayfile OK\"], \"MySQL\":[\"<br>Failed to update MySQL\"]}, \"data\":" + thisrec.ToJson() + "}";
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Just double check we are deleting the correct line - see if the dates match
				var lineData = lines[lineNum].Split(',');
				if (lineData[0] == newData.data[0])
				{
					var thisRec = new List<string>(newData.data);
					thisRec.Insert(0, newData.line.ToString());

					try
					{
						// Update the in database  record
						await station.DatabaseAsync.DeleteAsync<DailyData>(thisRec);

						// Update the dayfile
						if (Program.cumulus.ProgramOptions.UpdateDayfile)
						{
							// update the dayfile
							lines.RemoveAt(lineNum);

							// write dayfile back again
							File.WriteAllLines(cumulus.DayFileName, lines);
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditDayFile: Entry deletion failed. Error");
						Cumulus.LogMessage($"EditDayFile: Entry data = " + thisRec.ToJson());
						context.Response.StatusCode = 500;
						return "{\"errors\":{\"Logfile\":[\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
					}
				}
				else
				{
					Cumulus.LogMessage($"EditDayFile: Entry deletion failed. Line to delete does not match the file contents");
					context.Response.StatusCode = 500;
					return "{\"errors\":{\"Logfile\":[\"<br>Failed, line to delete does not match the file contents\"]}}";
				}
			}
			else
			{
				Cumulus.LogMessage($"EditDayFile: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data);
			rec.Insert(0, newData.line.ToString());
			return rec.ToJson();
		}

		private class DayFileEditor
		{
			public string action { get; set; }
			public int line { get; set; }
			public string[] data { get; set; }
		}


		#endregion Dayfile

		#region Log Files
		/// <summary>
		/// Return lines from log file in JSON format
		/// </summary>
		/// <param name="date"></param>
		/// <param name="draw"></param>
		/// <param name="start"></param>
		/// <param name="length"></param>
		/// <param name="extra"></param>
		/// <returns>JSON encoded section of the log file as a string</returns>
		internal string GetLogfile(string date, string draw, int start, int length, bool extra)
		{
			try
			{
				// date will (hopefully) be in format "m-yyyy" or "mm-yyyy"
				int month = Convert.ToInt32(date.Split('-')[0]);
				int year = Convert.ToInt32(date.Split('-')[1]);

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(year, month, 15);

				var logfile = extra ? cumulus.GetExtraLogFileName(ts) : cumulus.GetLogFileName(ts);
				var numFields = extra ? Cumulus.NumExtraLogFileFields : Cumulus.NumLogFileFields;

				if (!File.Exists(logfile))
				{
					Cumulus.LogMessage($"GetLogFile: Error, file does not exist: {logfile}");
					return "";
				}

				var allLines = File.ReadAllLines(logfile);
				var total = allLines.Length;
				var lines = allLines.Skip(start).Take(length);

				var json = new StringBuilder(220 * lines.Count());

				json.Append("{\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsTotal\":");
				json.Append(total);
				json.Append(",\"recordsFiltered\":");
				json.Append(total);
				json.Append(",\"data\":[");

				var lineNum = start + 1; // Start is zero relative

				foreach (var line in lines)
				{
					var fields = line.Split(',');
					json.Append($"[{lineNum++},");
					for (var i = 0; i < numFields; i++)
					{
						if (i < fields.Length)
						{
							// field exists
							json.Append('"');
							json.Append(fields[i]);
							json.Append('"');
						}
						else
						{
							// add padding
							json.Append("\" \"");
						}

						if (i < numFields - 1)
						{
							json.Append(',');
						}
					}
					json.Append("],");
				}

				// trim trailing ","
				json.Length--;
				json.Append("]}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}


		internal string EditDatalog(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DatalogEditor>();

			// date will (hopefully) be in format "m-yyyy" or "mm-yyyy"
			int month = Convert.ToInt32(newData.month.Split('-')[0]);
			int year = Convert.ToInt32(newData.month.Split('-')[1]);

			// Get a timestamp, use 15th day to avoid wrap issues
			var ts = new DateTime(year, month, 15);

			var logfile = (newData.extra ? cumulus.GetExtraLogFileName(ts) : cumulus.GetLogFileName(ts));

			// read the log file into a List
			var lines = File.ReadAllLines(logfile).ToList();

			var lineNum = newData.line - 1; // our List is zero relative

			if (newData.action == "Edit")
			{
				// replace the edited line
				var orgLine = lines[lineNum];
				var newLine = String.Join(",", newData.data);

				lines[lineNum] = newLine;

				try
				{
					// write logfile back again
					File.WriteAllLines(logfile, lines);
					Cumulus.LogMessage($"EditDataLog: Changed Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
					Cumulus.LogMessage($"EditDataLog: Changed Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "EditDataLog: Failed, error");
					Cumulus.LogMessage("EditDataLog: Data received - " + newLine);
					context.Response.StatusCode = 500;

					return "{\"errors\":{\"Logfile\":[\"<br>Failed to update, error = " + ex.Message + "\"]}}";
				}



				// Update the MySQL record
				if (!string.IsNullOrEmpty(cumulus.MySqlConnSettings.Server) &&
					!string.IsNullOrEmpty(cumulus.MySqlConnSettings.UserID) &&
					!string.IsNullOrEmpty(cumulus.MySqlConnSettings.Password) &&
					!string.IsNullOrEmpty(cumulus.MySqlConnSettings.Database) &&
					cumulus.MySqlSettings.UpdateOnEdit
					)
				{
					// Only the monthly log file is stored in MySQL
					if (!newData.extra)
					{
						var updateStr = "";

						try
						{
							var updt = new StringBuilder(1024);

							var LogRec = station.ParseLogFileRec(newLine, false);

							updt.Append($"UPDATE {cumulus.MySqlSettings.Monthly.TableName} SET ");
							updt.Append($"Temp={LogRec.OutdoorTemperature.ToString(cumulus.TempFormat, invNum)},");
							updt.Append($"Humidity={ LogRec.OutdoorHumidity},");
							updt.Append($"Dewpoint={LogRec.OutdoorDewpoint.ToString(cumulus.TempFormat, invNum)},");
							updt.Append($"Windspeed={LogRec.WindAverage.ToString(cumulus.WindAvgFormat, invNum)},");
							updt.Append($"Windgust={LogRec.RecentMaxGust.ToString(cumulus.WindFormat, invNum)},");
							updt.Append($"Windbearing={LogRec.AvgBearing},");
							updt.Append($"RainRate={LogRec.RainRate.ToString(cumulus.RainFormat, invNum)},");
							updt.Append($"TodayRainSoFar={LogRec.RainToday.ToString(cumulus.RainFormat, invNum)},");
							updt.Append($"Pressure={LogRec.Pressure.ToString(cumulus.PressFormat, invNum)},");
							updt.Append($"Raincounter={LogRec.Raincounter.ToString(cumulus.RainFormat, invNum)},");
							updt.Append($"InsideTemp={LogRec.IndoorTemperature.ToString(cumulus.TempFormat, invNum)},");
							updt.Append($"InsideHumidity={LogRec.IndoorHumidity},");
							updt.Append($"LatestWindGust={LogRec.WindLatest.ToString(cumulus.WindFormat, invNum)},");
							updt.Append($"WindChill={LogRec.WindChill.ToString(cumulus.TempFormat, invNum)},");
							updt.Append($"HeatIndex={LogRec.HeatIndex.ToString(cumulus.TempFormat, invNum)},");
							updt.Append($"UVindex={LogRec.UV.ToString(cumulus.UVFormat, invNum)},");
							updt.Append($"SolarRad={LogRec.SolarRad},");
							updt.Append($"Evapotrans={LogRec.ET.ToString(cumulus.ETFormat, invNum)},");
							updt.Append($"AnnualEvapTran={LogRec.AnnualETTotal.ToString(cumulus.ETFormat, invNum)},");
							updt.Append($"ApparentTemp={LogRec.ApparentTemperature.ToString(cumulus.TempFormat, invNum)},");
							updt.Append($"MaxSolarRad={(Math.Round(LogRec.CurrentSolarMax))},");
							updt.Append($"HrsSunShine={LogRec.SunshineHours.ToString(cumulus.SunFormat, invNum)},");
							updt.Append($"CurrWindBearing={LogRec.Bearing},");
							updt.Append($"RG11rain={LogRec.RG11RainToday.ToString(cumulus.RainFormat, invNum)},");
							updt.Append($"RainSinceMidnight={LogRec.RainSinceMidnight.ToString(cumulus.RainFormat, invNum)},");
							updt.Append($"WindbearingSym='{station.CompassPoint(LogRec.AvgBearing)}',");
							updt.Append($"CurrWindBearingSym='{station.CompassPoint(LogRec.Bearing)}',");
							updt.Append($"FeelsLike={LogRec.FeelsLike.ToString(cumulus.TempFormat, invNum)},");
							updt.Append($"Humidex={LogRec.Humidex.ToString(cumulus.TempFormat, invNum)} ");

							updt.Append($"WHERE LogDateTime='{LogRec.Date:yyyy-MM-dd HH:mm}';");
							updateStr = updt.ToString();


							cumulus.MySqlCommandSync(updateStr, "EditLogFile");
							Cumulus.LogMessage($"EditDataLog: SQL Updated");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditDataLog: Failed, to update MySQL. Error");
							Cumulus.LogMessage($"EditDataLog: SQL Update statement = {updateStr}");
							context.Response.StatusCode = 501; // Use 501 to signal that SQL failed but file update was OK
							var thisrec = new List<string>(newData.data);
							thisrec.Insert(0, newData.line.ToString());

							return "{\"errors\": { \"Logfile\":[\"<br>Updated the log file OK\"], \"MySQL\":[\"<br>Failed to update MySQL. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
						}

					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Just double check we are deleting the correct line - see if the dates match
				var lineData = lines[lineNum].Split(',');
				if (lineData[1] == newData.data[1])
				{
					var thisrec = new List<string>(newData.data);
					thisrec.Insert(0, newData.line.ToString());

					try
					{
						lines.RemoveAt(lineNum);
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditDataLog: Entry deleted - " + thisrec.ToJson());
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditDataLog: Entry deletion failed. Error");
						Cumulus.LogMessage($"EditDataLog: Entry data = - " + thisrec.ToJson());
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
					}
				}
				else
				{
					Cumulus.LogMessage($"EditDataLog: Entry deletion failed. Line to delete does not match the file contents");
					context.Response.StatusCode = 500;
					return "{\"errors\":{\"Logfile\":[\"Failed, line to delete does not match the file contents\"]}}";
				}
			}


			// return the updated record
			var rec = new List<string>(newData.data);
			rec.Insert(0, newData.line.ToString());
			return rec.ToJson();
		}

		private class DatalogEditor
		{
			public string action { get; set; }
			public int line { get; set; }
			public string month { get; set; }
			public bool extra { get; set; }
			public string[] data { get; set; }
		}

		#endregion Log Files
	}
}
