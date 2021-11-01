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
				/*
				var allLines = File.ReadAllLines(cumulus.DayFileName);
				var total = allLines.Length;
				var lines = allLines.Skip(start).Take(length);

				var json = new StringBuilder(350 * lines.Count());

				json.Append("{\"draw\":" + draw);
				json.Append(",\"recordsTotal\":" + total);
				json.Append(",\"recordsFiltered\":" + total);
				json.Append(",\"data\":[");

				var lineNum = start + 1; // Start is zero relative

				foreach (var line in lines)
				{
					var fields = line.Split(',');
					var numFields = fields.Length;
					json.Append($"[{lineNum++},");
					for (var i = 0; i < numFields; i++)
					{
						json.Append($"\"{fields[i]}\"");
						if (i < fields.Length - 1)
						{
							json.Append(',');
						}
					}

					if (numFields < Cumulus.DayfileFields)
					{
						// insufficient fields, pad with empty fields
						for (var i = numFields; i < Cumulus.DayfileFields; i++)
						{
							json.Append(",\"\"");
						}
					}
					json.Append("],");
				}

				// trim last ","
				json.Length--;
				json.Append("]}");

				return json.ToString();
				*/


				var allRows = await station.DatabaseAsync.QueryAsync<DayData>("select * from DayData order by Timestamp");
				var total = allRows.Count;
				var lines = allRows.Skip(start).Take(length);
				var json = new StringBuilder(350 * lines.Count());

				json.Append("{\"draw\":" + draw);
				json.Append(",\"recordsTotal\":" + total);
				json.Append(",\"recordsFiltered\":" + total);
				json.Append(",\"data\":[");

				var lineNum = start + 1; // Start is zero relative

				foreach (var row in lines)
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


		internal string EditDayFile(IHttpContext context)
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

				/*
				// Update the in memory record
				try
				{
					station.DayFile[lineNum] = station.ParseDayFileRec(newLine);

					// write dayfile back again
					File.WriteAllLines(cumulus.DayFileName, lines);
					Cumulus.LogMessage($"EditDayFile: Changed dayfile line {lineNum + 1}, original = {orgLine}");
					Cumulus.LogMessage($"EditDayFile: Changed dayfile line {lineNum + 1},      new = {newLine}");
				}
				catch
				{
					Cumulus.LogMessage("EditDayFile: Failed, new data does not match required values");
					Cumulus.LogMessage("EditDayFile: Data received - " + newLine);
					context.Response.StatusCode = 500;

					return "{\"errors\":{\"Logfile\":[\"<br>Failed, new data does not match required values\"]}}";
				}
				*/

				// Update the MX database
				var newRec = new DayData();
				newRec.FromString(newData.data);

				var updated = station.Database.Update(newRec);

				// Update the dayfile
				if (Program.cumulus.ProgramOptions.UpdateDayfile)
				{
					// replace the edited line
					var orgLine = lines[lineNum];
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
						updt.Append($"HighWindGust={station.DayFile[lineNum].HighGust.ToString(cumulus.WindFormat, invNum)},");
						updt.Append($"HWindGBear={station.DayFile[lineNum].HighGustBearing},");
						updt.Append($"THWindG={station.DayFile[lineNum].HighGustTime:\\'HH:mm\\'},");
						updt.Append($"MinTemp={station.DayFile[lineNum].LowTemp.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TMinTemp={station.DayFile[lineNum].LowTempTime:\\'HH:mm\\'},");
						updt.Append($"MaxTemp={station.DayFile[lineNum].HighTemp.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TMaxTemp={station.DayFile[lineNum].HighTempTime:\\'HH:mm\\'},");
						updt.Append($"MinPress={station.DayFile[lineNum].LowPress.ToString(cumulus.PressFormat, invNum)},");
						updt.Append($"TMinPress={station.DayFile[lineNum].LowPressTime:\\'HH:mm\\'},");
						updt.Append($"MaxPress={station.DayFile[lineNum].HighPress.ToString(cumulus.PressFormat, invNum)},");
						updt.Append($"TMaxPress={station.DayFile[lineNum].HighPressTime:\\'HH:mm\\'},");
						updt.Append($"MaxRainRate={station.DayFile[lineNum].HighRainRate.ToString(cumulus.RainFormat, invNum)},");
						updt.Append($"TMaxRR={station.DayFile[lineNum].HighRainRateTime:\\'HH:mm\\'},");
						updt.Append($"TotRainFall={station.DayFile[lineNum].TotalRain.ToString(cumulus.RainFormat, invNum)},");
						updt.Append($"AvgTemp={station.DayFile[lineNum].AvgTemp.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TotWindRun={station.DayFile[lineNum].WindRun.ToString("F1", invNum)},");
						updt.Append($"HighAvgWSpeed={station.DayFile[lineNum].HighAvgWind.ToString(cumulus.WindAvgFormat, invNum)},");
						updt.Append($"THAvgWSpeed={station.DayFile[lineNum].HighAvgWindTime:\\'HH:mm\\'},");
						updt.Append($"LowHum={station.DayFile[lineNum].LowHumidity},");
						updt.Append($"TLowHum={station.DayFile[lineNum].LowHumidityTime:\\'HH:mm\\'},");
						updt.Append($"HighHum={station.DayFile[lineNum].HighHumidity},");
						updt.Append($"THighHum={station.DayFile[lineNum].HighHumidityTime:\\'HH:mm\\'},");
						updt.Append($"TotalEvap={station.DayFile[lineNum].ET.ToString(cumulus.ETFormat, invNum)},");
						updt.Append($"HoursSun={station.DayFile[lineNum].SunShineHours.ToString(cumulus.SunFormat, invNum)},");
						updt.Append($"HighHeatInd={station.DayFile[lineNum].HighHeatIndex.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"THighHeatInd={station.DayFile[lineNum].HighHeatIndexTime:\\'HH:mm\\'},");
						updt.Append($"HighAppTemp={station.DayFile[lineNum].HighAppTemp.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"THighAppTemp={station.DayFile[lineNum].HighAppTempTime:\\'HH:mm\\'},");
						updt.Append($"LowAppTemp={station.DayFile[lineNum].LowAppTemp.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TLowAppTemp={station.DayFile[lineNum].LowAppTempTime:\\'HH:mm\\'},");
						updt.Append($"HighHourRain={station.DayFile[lineNum].HighHourlyRain.ToString(cumulus.RainFormat, invNum)},");
						updt.Append($"THighHourRain={station.DayFile[lineNum].HighHourlyRainTime:\\'HH:mm\\'},");
						updt.Append($"LowWindChill={station.DayFile[lineNum].LowWindChill.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TLowWindChill={station.DayFile[lineNum].LowWindChillTime:\\'HH:mm\\'},");
						updt.Append($"HighDewPoint={station.DayFile[lineNum].HighDewPoint.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"THighDewPoint={station.DayFile[lineNum].HighDewPointTime:\\'HH:mm\\'},");
						updt.Append($"LowDewPoint={station.DayFile[lineNum].LowDewPoint.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TLowDewPoint={station.DayFile[lineNum].LowDewPointTime:\\'HH:mm\\'},");
						updt.Append($"DomWindDir={station.DayFile[lineNum].DominantWindBearing},");
						updt.Append($"HeatDegDays={station.DayFile[lineNum].HeatingDegreeDays.ToString("F1", invNum)},");
						updt.Append($"CoolDegDays={station.DayFile[lineNum].CoolingDegreeDays.ToString("F1", invNum)},");
						updt.Append($"HighSolarRad={station.DayFile[lineNum].HighSolar},");
						updt.Append($"THighSolarRad={station.DayFile[lineNum].HighSolarTime:\\'HH:mm\\'},");
						updt.Append($"HighUV={station.DayFile[lineNum].HighUv.ToString(cumulus.UVFormat, invNum)},");
						updt.Append($"THighUV={station.DayFile[lineNum].HighUvTime:\\'HH:mm\\'},");
						updt.Append($"HWindGBearSym='{station.CompassPoint(station.DayFile[lineNum].HighGustBearing)}',");
						updt.Append($"DomWindDirSym='{station.CompassPoint(station.DayFile[lineNum].DominantWindBearing)}',");
						updt.Append($"MaxFeelsLike={station.DayFile[lineNum].HighFeelsLike.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TMaxFeelsLike={station.DayFile[lineNum].HighFeelsLikeTime:\\'HH:mm\\'},");
						updt.Append($"MinFeelsLike={station.DayFile[lineNum].LowFeelsLike.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TMinFeelsLike={station.DayFile[lineNum].LowFeelsLikeTime:\\'HH:mm\\'},");
						updt.Append($"MaxHumidex={station.DayFile[lineNum].HighHumidex.ToString(cumulus.TempFormat, invNum)},");
						updt.Append($"TMaxHumidex={station.DayFile[lineNum].HighFeelsLikeTime:\\'HH:mm\\'} ");

						updt.Append($"WHERE LogDate='{station.DayFile[lineNum].Date:yyyy-MM-dd}';");
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
					var thisrec = new List<string>(newData.data);
					thisrec.Insert(0, newData.line.ToString());

					try
					{
						lines.RemoveAt(lineNum);
						// Update the in memory record
						station.DayFile.RemoveAt(lineNum);

						// write dayfile back again
						File.WriteAllLines(cumulus.DayFileName, lines);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditDayFile: Entry deletion failed. Error");
						Cumulus.LogMessage($"EditDayFile: Entry data = " + thisrec.ToJson());
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
