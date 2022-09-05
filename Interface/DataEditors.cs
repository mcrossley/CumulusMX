﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EmbedIO;
using ServiceStack;

namespace CumulusMX
{
	internal class DataEditors
	{
		private WeatherStation station;
		private readonly Cumulus cumulus;
		private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;

		internal DataEditors(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}


		internal void SetStation(WeatherStation stat)
		{
			station = stat;
		}

		#region Dayfile

		/// <summary>
		/// Return entries from DayData in json format
		/// </summary>
		/// <param name="draw"></param>
		/// <param name="start"></param>
		/// <param name="length"></param>
		/// <returns>JSON encoded section of the dayfile as a string</returns>
		internal string GetDailyData(string draw, int start, int length, string search)
		{
			try
			{
				var total = station.Database.ExecuteScalar<int>("select count(*) from DayData");
				//var rows = station.Database.Query<DayData>("select * from DayData order by Timestamp limit ?,?", start, length);
				var rows = station.Database.Query<DayData>("select * from DayData order by Timestamp");
				var json = new StringBuilder(350 * rows.Count);

				json.Append("{\"recordsTotal\":");
				json.Append(total);
				json.Append(",\"data\":[");

				var lineNum = start; // Start is zero relative
				var filtered = 0;  // Total number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					lineNum++;

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append($"[{lineNum},");
						json.Append(text);
						json.Append("],");
					}
				}

				// trim last ","
				if (filtered > 0) json.Length--;


				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}


		internal string EditDailyData(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new DayData();
				newRec.FromString(newData.data[0]);

				station.Database.Update(newRec);

				// Update the dayfile
				if (Program.cumulus.ProgramOptions.UpdateDayfile)
				{
					// read dayfile into a List
					var lines = File.ReadAllLines(cumulus.DayFileName).ToList();

					var lineNum = 0;

					// Find the line using the date string
					foreach (var line in lines)
					{
						if (line.Contains(newData.data[0][0]))
							break;

						lineNum++;
					}

					var orgLine = lines[lineNum];

					// replace the edited line
					var newLine = string.Join(",", newData.data);

					lines[lineNum] = newLine;

					// write dayfile back again
					File.WriteAllLines(cumulus.DayFileName, lines);

					Cumulus.LogMessage($"EditDailyData: Edit line {lineNum + 1}, original = {orgLine}");
					Cumulus.LogMessage($"EditDailyData: Edit line {lineNum + 1},      new = {newLine}");
				}


				// Update the MySQL record
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Server) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.UserID) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Password) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Database) &&
					cumulus.MySqlStuff.Settings.UpdateOnEdit
					)
				{
					var updateStr = "";

					try
					{
						var updt = new StringBuilder(1024);

						updt.Append($"UPDATE {cumulus.MySqlStuff.Settings.Dayfile.TableName} SET ");
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
						if (newRec.HighGustBearing.HasValue) updt.Append($"HWindGBearSym='{WeatherStation.CompassPoint(newRec.HighGustBearing.Value)}',");
						if (newRec.DominantWindBearing.HasValue) updt.Append($"DomWindDirSym='{WeatherStation.CompassPoint(newRec.DominantWindBearing.Value)}',");
						if (newRec.HighFeelsLike.HasValue) updt.Append($"MaxFeelsLike={newRec.HighFeelsLike.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighFeelsLikeTime.HasValue) updt.Append($"TMaxFeelsLike={newRec.HighFeelsLikeTime.Value:\\'HH:mm\\'},");
						if (newRec.LowFeelsLike.HasValue) updt.Append($"MinFeelsLike={newRec.LowFeelsLike.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.LowFeelsLikeTime.HasValue) updt.Append($"TMinFeelsLike={newRec.LowFeelsLikeTime.Value:\\'HH:mm\\'},");
						if (newRec.HighHumidex.HasValue) updt.Append($"MaxHumidex={newRec.HighHumidex.Value.ToString(cumulus.TempFormat, invNum)},");
						if (newRec.HighHumidexTime.HasValue) updt.Append($"TMaxHumidex={newRec.HighHumidexTime.Value:\\'HH:mm\\'} ");

						updt.Append($"WHERE LogDate='{newRec.Timestamp:yyyy-MM-dd}';");
						updateStr = updt.ToString();

						cumulus.MySqlStuff.CommandSync(updateStr, "EditDayFile");
						Cumulus.LogMessage($"EditDayFile: SQL Updated");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditDayFile: Failed, to update MySQL. Error");
						Cumulus.LogMessage($"EditDayFile: SQL Update statement = {updateStr}");
						context.Response.StatusCode = 501;  // Use 501 to signal that SQL failed but file update was OK
						var thisrec = new List<string>(newData.data[0]);

						return "{\"errors\":{\"Dayfile\":[\"<br>Updated the dayfile OK\"], \"MySQL\":[\"<br>Failed to update MySQL\"]}, \"data\":" + thisrec.ToJson() + "}";
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				var newRec = new DayData();
				newRec.FromString(newData.data[0]);

				station.Database.Delete(newRec);


				// Update the dayfile
				if (Program.cumulus.ProgramOptions.UpdateDayfile && Program.cumulus.StationOptions.LogMainStation)
				{
					// read dayfile into a List
					var lines = File.ReadAllLines(cumulus.DayFileName).ToList();
					var lineNum = 0;

					// Find the line using the timestamp
					foreach (var line in lines)
					{
						if (line.Contains(newData.data[0][1]))
							break;

						lineNum++;
					}

					var orgLine = lines[lineNum];

					// update the dayfile
					lines.RemoveAt(lineNum);

					// write dayfile back again
					File.WriteAllLines(cumulus.DayFileName, lines);

					Cumulus.LogMessage($"EditDailyData: Delete line {lineNum + 1}, original = {orgLine}");
				}

				// Update the MySQL record
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Server) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.UserID) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Password) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Database) &&
					cumulus.MySqlStuff.Settings.UpdateOnEdit
					)
				{

					var thisRec = new List<string>(newData.data[0]);

					try
					{
						// Update the in database  record
						station.Database.Delete<DayData>(thisRec);

					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditDayFile: Entry deletion failed. Error");
						Cumulus.LogMessage($"EditDayFile: Entry data = " + thisRec.ToJson());
						context.Response.StatusCode = 500;
						return "{\"errors\":{\"Logfile\":[\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditDayFile: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		private class DailyDataEditor
		{
			public string action { get; set; }
			public long[] dates { get; set; }
			public List<string[]> data { get; set; }
		}


		#endregion Dayfile

		#region Interval Data
		/// <summary>
		/// Return lines from the interval data in JSON format
		/// </summary>
		/// <param name="date"></param>
		/// <param name="draw"></param>
		/// <param name="start"></param>
		/// <param name="length"></param>
		/// <param name="extra"></param>
		/// <returns>JSON encoded section of the log file as a string</returns>
		internal string GetIntervalData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // Total number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetLogfile: Logfiles parse = {elapsed} ms");
				cumulus.LogDebugMessage($"GetLogfile: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}


		internal string EditIntervalData(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<IntervalDataEditor2>();


			if (newData.action == "Edit")
			{
				var logDateStr = newData.data[0][0];
				var newRec = new IntervalData();
				newRec.FromString(newData.data[0]);
				var logDate = Utils.FromUnixTime(newData.dates[0]);

				try
				{
					var res = station.Database.Update(newRec);

					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditIntervalData: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditIntervalData: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the log file
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogMainStation)
				{
					var logfile = cumulus.GetLogFileName(logDate);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();
					var lineNum = 0;

					// Find the line using the timestamp
					var found = false;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditIntervalData: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}


					// replace the edited line
					var orgLine = lines[lineNum];
					var newLine = String.Join(",", newData.data[0]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditIntervalData: Changed Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditIntervalData: Changed Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditIntervalData: Failed, error");
						Cumulus.LogMessage("EditIntervalData: Data received - " + newLine);
						context.Response.StatusCode = 500;

						//return "{\"errors\":{\"Logfile\":[\"<br>Failed to update, error = " + ex.Message + "\"]}}";
					}
				}


				// Update the MySQL record
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Server) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.UserID) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Password) &&
					!string.IsNullOrEmpty(cumulus.MySqlStuff.ConnSettings.Database) &&
					cumulus.MySqlStuff.Settings.UpdateOnEdit
					)
				{
					// Only the monthly log file is stored in MySQL
					var updateStr = "";
					//var newLine = String.Join(",", newData.data[0]);

					try
					{
						var updt = new StringBuilder(1024);

						updt.Append($"UPDATE {cumulus.MySqlStuff.Settings.Monthly.TableName} SET ");
						updt.Append($"Temp={(newRec.Temp.HasValue ? newRec.Temp.Value.ToString(cumulus.TempFormat, invNum) : "null")},");
						updt.Append($"Humidity={(newRec.Humidity.HasValue ? newRec.Humidity.Value : "null")},");
						updt.Append($"Dewpoint={(newRec.DewPoint.HasValue ? newRec.DewPoint.Value.ToString(cumulus.TempFormat, invNum) : "null")},");
						updt.Append($"Windspeed={(newRec.WindAvg.HasValue ? newRec.WindAvg.Value.ToString(cumulus.WindAvgFormat, invNum) : "null")},");
						updt.Append($"Windgust={(newRec.WindGust10m.HasValue ? newRec.WindGust10m.Value.ToString(cumulus.WindFormat, invNum) : "null")},");
						updt.Append($"Windbearing={(newRec.WindAvgDir.HasValue ? newRec.WindAvgDir : "null")},");
						updt.Append($"RainRate={(newRec.RainRate.HasValue ? newRec.RainRate.Value.ToString(cumulus.RainFormat, invNum) : "null")},");
						updt.Append($"TodayRainSoFar={(newRec.RainToday.HasValue ? newRec.RainToday.Value.ToString(cumulus.RainFormat, invNum) : "null")},");
						updt.Append($"Pressure={(newRec.Pressure.HasValue ? newRec.Pressure.Value.ToString(cumulus.PressFormat, invNum) : "null")},");
						updt.Append($"Raincounter={(newRec.RainCounter.HasValue ? newRec.RainCounter.Value.ToString(cumulus.RainFormat, invNum) : "null")},");
						updt.Append($"InsideTemp={(newRec.InsideTemp.HasValue ? newRec.InsideTemp.Value.ToString(cumulus.TempFormat, invNum) : "null")},");
						updt.Append($"InsideHumidity={(newRec.InsideHumidity.HasValue ? newRec.InsideHumidity.Value : "null")},");
						updt.Append($"LatestWindGust={(newRec.WindLatest.HasValue ? newRec.WindLatest.Value.ToString(cumulus.WindFormat, invNum) : "null")},");
						updt.Append($"WindChill={(newRec.WindChill.HasValue ? newRec.WindChill.Value.ToString(cumulus.TempFormat, invNum) : "null")},");
						updt.Append($"HeatIndex={(newRec.HeatIndex.HasValue ? newRec.HeatIndex.Value.ToString(cumulus.TempFormat, invNum) : "null")},");
						updt.Append($"UVindex={(newRec.UV.HasValue ? newRec.UV.Value.ToString(cumulus.UVFormat, invNum) : "null")},");
						updt.Append($"SolarRad={(newRec.SolarRad.HasValue ? newRec.SolarRad.Value : "null")},");
						updt.Append($"Evapotrans={(newRec.ET.HasValue ? newRec.ET.Value.ToString(cumulus.ETFormat, invNum) : "null")},");
						updt.Append($"AnnualEvapTran={(newRec.AnnualET.HasValue ? newRec.AnnualET.Value.ToString(cumulus.ETFormat, invNum) : "null")},");
						updt.Append($"ApparentTemp={(newRec.Apparent.HasValue ? newRec.Apparent.Value.ToString(cumulus.TempFormat, invNum) : "null")},");
						updt.Append($"MaxSolarRad={(newRec.SolarMax.HasValue ? newRec.SolarMax.Value : "null")},");
						updt.Append($"HrsSunShine={(newRec.Sunshine.HasValue ? newRec.Sunshine.Value.ToString(cumulus.SunFormat, invNum) : "null")},");
						updt.Append($"CurrWindBearing={(newRec.WindDir.HasValue ? newRec.WindDir.Value : "null")},");
						updt.Append($"RG11rain={(newRec.RG11Rain.HasValue ? newRec.RG11Rain.Value.ToString(cumulus.RainFormat, invNum) : "null")},");
						updt.Append($"RainSinceMidnight={(newRec.RainMidnight.HasValue ? newRec.RainMidnight.Value.ToString(cumulus.RainFormat, invNum) : "null")},");
						updt.Append($"WindbearingSym='{(newRec.WindAvgDir.HasValue ? WeatherStation.CompassPoint(newRec.WindAvgDir.Value) : "null")}',");
						updt.Append($"CurrWindBearingSym='{(newRec.WindDir.HasValue ? WeatherStation.CompassPoint(newRec.WindDir.Value) : "null")}',");
						updt.Append($"FeelsLike={(newRec.FeelsLike.HasValue ? newRec.FeelsLike.Value.ToString(cumulus.TempFormat, invNum) : "null")},");
						updt.Append($"Humidex={(newRec.Humidex.HasValue ? newRec.Humidex.Value.ToString(cumulus.TempFormat, invNum) : "null")} ");

						updt.Append($"WHERE LogDateTime='{newRec.Timestamp:yyyy-MM-dd HH:mm}';");
						updateStr = updt.ToString();


						cumulus.MySqlStuff.CommandSync(updateStr, "EditLogFile");
						Cumulus.LogMessage($"EditIntervalData: SQL Updated");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditIntervalData: Failed, to update MySQL. Error");
						Cumulus.LogMessage($"EditIntervalData: SQL Update statement = {updateStr}");
						context.Response.StatusCode = 501; // Use 501 to signal that SQL failed but file update was OK
						var thisrec = new List<string>(newData.data[0]);

						return "{\"errors\": { \"Logfile\":[\"<br>Updated the log file OK\"], \"MySQL\":[\"<br>Failed to update MySQL. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new IntervalData();

					newRec.FromString(row);
					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);

						if (res == 1)
						{
							Cumulus.LogMessage($"EditIntervalData: Deleted database entry {(newRec.Timestamp.ToString("dd/MM/yy hh:mm", CultureInfo.InvariantCulture))}");
						}
						else
						{
							Cumulus.LogMessage($"EditIntervalData: ERROR - Faied to update database entry {(newRec.Timestamp.ToString("dd/MM/yy hh:mm", CultureInfo.InvariantCulture))}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";

						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}


				// Update the log file
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogMainStation)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line current using the timestamp
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								var orgLine = line;
								try
								{
									lines.RemoveAt(lineNum);

									// write logfile back again
									File.WriteAllLines(logfile, lines);
									Cumulus.LogMessage($"EditDataLog: Entry deleted - {orgLine}");
								}
								catch (Exception ex)
								{
									cumulus.LogExceptionMessage(ex, "EditDataLog: Entry deletion failed. Error");
									Cumulus.LogMessage($"EditDataLog: Entry data = - {orgLine}");
									context.Response.StatusCode = 500;
									return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
								}

								break;
							}
							lineNum++;
						}
					}
				}

				//return "";
			}


			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		/*
		private class IntervalDataEditor
		{
			public string action { get; set; }
			public int line { get; set; }
			public long logdate { get; set; }
			public bool extra { get; set; }
			public string[] data { get; set; }
		}
		*/

		private class IntervalDataEditor2
		{
			public string action { get; set; }
			public long[] dates { get; set; }
			public List<string[]> data { get; set; }
		}

		#endregion Interval Data

		#region ExtraTemps

		internal string GetExtraTempData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // Total number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<ExtraTemp>("select * from ExtraTemp where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetExtraTemp: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetExtraTemp: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditExtraTemp(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{
				// Update the MX database
				var newRec = new ExtraTemp();
				newRec.FromString(newData.data[0]);
				var logDate = Utils.FromUnixTime(newData.dates[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);

					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditExtraTemp: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditExtraTemp: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}


				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(logDate);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();
					var lineNum = 0;

					// Find the line using the date string
					var found = false;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditExtraTemp: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the extra temp fields (2-11) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', newData.data[0]) + ',' + String.Join(',', orgFields[12..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditExtraTemp: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditExtraTemp: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditExtraTemp: Failed, error");
						Cumulus.LogMessage("EditExtraTemp: Data received - " + newLine);
						context.Response.StatusCode = 500;

						//return "{\"errors\":{\"Logfile\":[\"<br>Failed to update, error = " + ex.Message + "\"]}}";
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new ExtraTemp();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditExtraTemp: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditExtraTemp: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";

						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}


				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var lineNum = 0;
						var found = false;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditExtraTemp: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..2]) + ',' + ",,,,,,,,," + ',' + String.Join(',', orgFields[12..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditExtraTemp: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditExtraTemp: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditExtraTemp: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditExtraTemp: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditExtraTemp: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		#endregion ExtraTemps

		#region ExtraHums

		internal string GetExtraHumData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // Total number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<ExtraHum>("select * from ExtraHum where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetExtraHum: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetExtraHum: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditExtraHum(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{
				// Update the MX database
				var newRec = new ExtraHum();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];
				try
				{
					var res = station.Database.Update(newRec);

					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditExtraHum: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditExtraHum: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();
					var lineNum = 0;

					// Find the line using the date string
					var found = false;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditExtraHum: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the extra hum fields (12-21) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..12]) + ',' + String.Join(',', newData.data[0][2..]) + ',' + String.Join(',', orgFields[22..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditExtraHum: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditExtraHum: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditExtraHum: Failed, error");
						Cumulus.LogMessage("EditExtraHum: Data received - " + newLine);
						context.Response.StatusCode = 500;

						//return "{\"errors\":{\"Logfile\":[\"<br>Failed to update, error = " + ex.Message + "\"]}}";
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new ExtraHum();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditExtraHum: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditExtraHum: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditExtraHum: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						// replace the extra hum fields (12-21) edited line
						var newLine = String.Join(',', orgFields[0..12]) + ',' + ",,,,,,,,," + ',' + String.Join(',', orgFields[22..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditExtraHum: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditExtraHum: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditDataLog: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditExtraHum: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditExtraHum: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		#endregion ExtraHums

		#region ExtraDewpoint

		internal string GetExtraDewData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // otal number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<ExtraDewPoint>("select * from ExtraDewPoint where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetExtraDewData: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetExtraDewData: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditExtraDew(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{
				// Update the MX database
				var newRec = new ExtraDewPoint();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditExtraDew: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditExtraDew: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();
					var lineNum = 0;

					// Find the line using the date string
					var found = false;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditExtraDew: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the extra dew point fields (22-31) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..22]) + ',' + String.Join(',', newData.data[0][2..]) + ',' + String.Join(',', orgFields[32..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditExtraDew: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditExtraDew: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditExtraDew: Failed, error");
						Cumulus.LogMessage("EditExtraDew: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new ExtraDewPoint();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditExtraDew: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditExtraDew: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditExtraDew: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..22]) + ',' + ",,,,,,,,," + ',' + String.Join(',', orgFields[32..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditExtraDew: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditExtraDew: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditExtraDew: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditExtraDew: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditExtraDew: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}


		#endregion ExtraDewpoint

		#region UserTemps

		internal string GetUserTempData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // otal number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<UserTemp>("select * from UserTemp where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetUserTempData: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetUserTempData: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditUserTemp(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{
				// Update the MX database
				var newRec = new UserTemp();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditUserTemp: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditUserTemp: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					// Update the extralogfile
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();

					// Find the line using the date string
					var found = false;
					var lineNum = 0;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditUserTemp: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the extra temp fields (76-83) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..76]) + ',' + String.Join(',', newData.data[0][2..]) + ',' + String.Join(',', orgFields[84..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditUserTemp: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditUserTemp: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditUserTemp: Failed, error");
						Cumulus.LogMessage("EditUserTemp: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new UserTemp();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditUserTemp: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditUserTemp: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
						catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditUserTemp: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..76]) + ',' + ",,,,,,,,," + ',' + String.Join(',', orgFields[84..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditUserTemp: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditUserTemp: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditUserTemp: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditUserTemp: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditUserTemp: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}


		#endregion UserTemps

		#region SoilTemps

		internal string GetSoilTempData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // otal number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<SoilTemp>("select * from SoilTemp where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetSoilTempData: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetSoilTempData: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditSoilTemp(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new SoilTemp();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditSoilTemp: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditSoilTemp: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();
					var lineNum = 0;

					// Find the line using the date string
					var found = false;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditSoilTemp: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the extra temp fields (32-35 & 44-55) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..32]) + ',' + String.Join(',', newData.data[0][2..5]) + ',' + String.Join(',', orgFields[36..44]) + ',' + String.Join(',', newData.data[0][6..]) + ',' + String.Join(',', orgFields[56..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditSoilTemp: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditSoilTemp: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditUserTemp: Failed, error");
						Cumulus.LogMessage("EditSoilTemp: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new SoilTemp();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditSoilTemp: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditSoilTemp: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditSoilTemp: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..32]) + ',' + ",,," + ',' + String.Join(',', orgFields[36..44]) + ',' + ",,,,,,,,,,," + ',' + String.Join(',', orgFields[56..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditSoilTemp: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditSoilTemp: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditSoilTemp: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditSoilTemp: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditUserTemp: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}


		#endregion SoilTemps

		#region SoilMoist

		internal string GetSoilMoistData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // otal number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<SoilMoist>("select * from SoilMoist where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetSoilMoistData: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetSoilMoistData: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditSoilMoist(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new SoilMoist();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditSoilMoist: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditSoilMoist: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();

					// Find the line using the date string
					var found = false;
					var lineNum = 0;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditSoilMoist: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the soil moist fields (36-39 & 56-67)edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..36]) + ',' + String.Join(',', newData.data[0][2..5]) + ',' + String.Join(',', orgFields[40..56]) + ',' + String.Join(',', newData.data[0][6..]) + ',' + String.Join(',', orgFields[68..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditSoilMoist: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditSoilMoist: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditSoilMoist: Failed, error");
						Cumulus.LogMessage("EditSoilMoist: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new SoilTemp();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditSoilMoist: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditSoilMoist: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{

					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditSoilMoist: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..36]) + ',' + ",,," + ',' + String.Join(',', orgFields[40..56]) + ',' + ",,,,,,,,,,," + ',' + String.Join(',', orgFields[68..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditSoilMoist: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditSoilMoist: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditSoilMoist: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditSoilMoist: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditSoilMoist: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		#endregion SoilMoist

		#region LeafTemp

		internal string GetLeafTempData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // otal number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<LeafTemp>("select * from LeafTemp where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetLeafTempData: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetLeafTempData: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditLeafTemp(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new LeafTemp();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditLeafTemp: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditLeafTemp: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();

					// Find the line using the date string
					var found = false;
					var lineNum = 0;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditLeafTemp: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the extra temp fields (40-41) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..40]) + ',' + String.Join(',', newData.data[0][2..4]) + ',' + String.Join(',', orgFields[42..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditLeafTemp: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditLeafTemp: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditLeafTemp: Failed, error");
						Cumulus.LogMessage("EditLeafTemp: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new SoilTemp();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditLeafTemp: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditLeafTemp: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditLeafTemp: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..40]) + ',' + "," + ',' + String.Join(',', orgFields[42..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditLeafTemp: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditLeafTemp: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditLeafTemp: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditLeafTemp: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditLeafTemp: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}


		#endregion LeafTemp

		#region LeafWet

		internal string GetLeafWetData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // otal number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<LeafWet>("select * from LeafWet where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetLeafWetData: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetLeafWetData: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditLeafWet(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new LeafWet();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditLeafWet: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditLeafWet: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();

					// Find the line using the date string
					var found = false;
					var lineNum = 0;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditLeafWet: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the leaf wetness fields (42-43) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..42]) + ',' + String.Join(',', newData.data[0][2..3]) + ',' + String.Join(',', orgFields[44..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditLeafWet: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditLeafWet: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditLeafWet: Failed, error");
						Cumulus.LogMessage("EditLeafWet: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new UserTemp();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditLeafWet: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditLeafWet: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditLeafWet: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..42]) + ',' + "," + ',' + String.Join(',', orgFields[44..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditLeafWet: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditLeafWet: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");

						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditLeafWet: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditLeafWet: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditLeafWet: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		#endregion LeafTemp

		#region AirQuality

		internal string GetAirQualData(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // Total number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<AirQuality>("select * from AirQuality where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetAirQualData: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetAirQualData: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditAirQual(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new AirQuality();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditAirQual: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditAirQual: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();

					// Find the line using the date string
					var found = false;
					var lineNum = 0;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditAirQual: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the leaf wetness fields (68-75) edited line
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					// We do not have the fields in the editor in the same order as the file - for clarity
					// so construct a custom string
					var vals = newData.data[0][2] + ',' + newData.data[0][4] + ',' + newData.data[0][6] + ',' + newData.data[0][8];
					vals += ',' + newData.data[0][1] + ',' + newData.data[0][3] + ',' + newData.data[0][5] + ',' + newData.data[0][7];

					var newLine = String.Join(',', orgFields[0..68]) + ',' + vals + ',' + String.Join(',', orgFields[76..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditAirQual: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditAirQual: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditAirQual: Failed, error");
						Cumulus.LogMessage("EditAirQual: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new AirQuality();
					newRec.FromString(row);

					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditAirQual: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditAirQual: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}


						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditAirQual: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..68]) + ',' + ",,,,,,," + ',' + String.Join(',', orgFields[76..]);

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditAirQual: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditAirQual: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");

						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditAirQual: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditAirQual: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditAirQual: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		#endregion AirQuality

		#region CO2

		internal string GetCo2Data(string from, string to, string draw, int start, int length, string search)
		{
			try
			{
				// date will be in format "yyyy-mm-dd"
				var stDate = from.Split('-');
				var enDate = to.Split('-');

				var filtered = 0;  // otal number of filtered rows available
				var thisDraw = 0;  // count of the rows we are returning

				// Get a time stamp, use 15th day to avoid wrap issues
				var ts = new DateTime(int.Parse(stDate[0]), int.Parse(stDate[1]), int.Parse(stDate[2]));
				var ts1 = new DateTime(int.Parse(enDate[0]), int.Parse(enDate[1]), int.Parse(enDate[2]));
				ts1 = ts1.AddDays(1);

				if (ts > ts1)
				{
					// this cannot be, the end is earlier than the start!
					return "";
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();

				var rows = station.Database.Query<CO2Data>("select * from CO2Data where Timestamp >= ? and Timestamp < ? order by Timestamp", ts, ts1);
				var json = new StringBuilder(350 * length);

				json.Append("{\"recordsTotal\":");
				json.Append(rows.Count);
				json.Append(",\"data\":[");

				foreach (var row in rows)
				{
					var text = row.ToCSV();

					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !text.Contains(search))
					{
						continue;
					}

					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append('[');
						json.Append(text);
						json.Append("],");
					}
				}

				// trim trailing ","
				if (thisDraw > 0)
					json.Length--;

				json.Append("],\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(filtered);
				json.Append('}');

				watch.Stop();
				var elapsed = watch.ElapsedMilliseconds;
				cumulus.LogDebugMessage($"GetCo2Data: Parse time = {elapsed} ms");
				cumulus.LogDebugMessage($"GetCo2Data: Found={rows.Count}, filtered={filtered} (filter='{search}'), start={start}, return={thisDraw}");

				return json.ToString();
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage(ex.ToString());
			}

			return "";
		}

		internal string EditCo2(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<DailyDataEditor>();

			if (newData.action == "Edit")
			{

				// Update the MX database
				var newRec = new CO2Data();
				newRec.FromString(newData.data[0]);
				var logDateStr = newData.data[0][0];

				try
				{
					var res = station.Database.Update(newRec);
					//await station.DatabaseAsync.UpdateAsync(newRec);
					if (res == 1)
					{
						Cumulus.LogMessage($"EditCo2: Changed database entry {logDateStr}");
					}
					else
					{
						Cumulus.LogMessage($"EditCo2: ERROR - Faied to update database entry {logDateStr}");
						context.Response.StatusCode = 500;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					var thisrec = new List<string>(newData.data[0]);

					return "{\"errors\": { \"SQLite\":[<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + thisrec.ToJson() + "}";
				}

				// Update the extralogfile
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var logfile = cumulus.GetExtraLogFileName(newRec.Timestamp);

					// read the log file into a List
					var lines = File.ReadAllLines(logfile).ToList();

					// Find the line using the date string
					var found = false;
					var lineNum = 0;
					foreach (var line in lines)
					{
						if (line.Contains(logDateStr))
						{
							found = true;
							break;
						}

						lineNum++;
					}

					if (!found)
					{
						Cumulus.LogMessage($"EditCo2: Error editing entry, line not found for - {logDateStr}");
						return "{\"errors\": { \"Logfile\": [\"<br>Failed to edit record. Error: Log file line not found - " + logDateStr + "]}}";
					}

					// replace the leaf wetness fields (84-91) edited line (currently the last records)
					var orgLine = lines[lineNum];
					var orgFields = orgLine.Split(',');

					var newLine = String.Join(',', orgFields[0..84]) + ',' + String.Join(',', newData.data[0][2..]);

					lines[lineNum] = newLine;

					try
					{
						// write logfile back again
						File.WriteAllLines(logfile, lines);
						Cumulus.LogMessage($"EditCo2: Edit Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
						Cumulus.LogMessage($"EditCo2: Edit Log file [{logfile}] line {lineNum + 1},      new = {newLine}");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "EditCo2: Failed, error");
						Cumulus.LogMessage("EditCo2: Data received - " + newLine);
						context.Response.StatusCode = 500;
					}
				}
			}
			else if (newData.action == "Delete")
			{
				// Update the MX database
				foreach (var row in newData.data)
				{
					var newRec = new CO2Data();
					newRec.FromString(row);

					try
					{
						var res = station.Database.Delete(newRec);
						//await station.DatabaseAsync.DeleteAsync(newRec);
						if (res == 1)
						{
							Cumulus.LogMessage($"EditCo2: Deleted database entry {row[0]}");
						}
						else
						{
							Cumulus.LogMessage($"EditCo2: ERROR - Faied to update database entry {row[0]}");
							context.Response.StatusCode = 502;
							return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database.\"] }, \"data\":" + newRec.ToJson() + "}";
						}
					}
					catch (Exception ex)
					{
						context.Response.StatusCode = 502;
						return "{\"errors\": { \"SQLite\":[\"<br>Failed to update database. Error: " + ex.Message + "\"] }, \"data\":" + newRec.ToJson() + "}";
					}
				}

				// Update the log file - we do not delete the line as it contains other data, but just blank the extra temp fields
				if (Program.cumulus.ProgramOptions.UpdateLogfile && Program.cumulus.StationOptions.LogExtraSensors)
				{
					var lastMonth = -1;
					string logfile = "";

					var lines = new List<string>();

					foreach (var row in newData.data)
					{
						var logDate = Utils.FromUnixTime(long.Parse(row[1]));

						if (lastMonth != logDate.Month)
						{
							logfile = cumulus.GetExtraLogFileName(logDate);

							// read the log file into a List
							lines = File.ReadAllLines(logfile).ToList();
						}

						// Find the line using the timestamp
						var found = false;
						var lineNum = 0;
						foreach (var line in lines)
						{
							if (line.Contains(row[1]))
							{
								found = true;
								break;
							}

							lineNum++;
						}

						if (!found)
						{
							Cumulus.LogMessage($"EditCo2: Error deleting entry, line not found for - {row[0]}");
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: Log file line not found - " + row[0] + "]}}";
						}

						var orgLine = lines[lineNum];
						var orgFields = orgLine.Split(',');

						var newLine = String.Join(',', orgFields[0..84]) + ',' + ",,,,,,,";

						lines[lineNum] = newLine;

						try
						{
							// write logfile back again
							File.WriteAllLines(logfile, lines);
							Cumulus.LogMessage($"EditCo2: Delete Log file [{logfile}] line {lineNum + 1}, original = {orgLine}");
							Cumulus.LogMessage($"EditCo2: Delete Log file [{logfile}] line {lineNum + 1},      new = {newLine}");

						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, "EditCo2: Entry deletion failed. Error");
							Cumulus.LogMessage($"EditCo2: Entry data = - {orgLine}");
							context.Response.StatusCode = 500;
							return "{\"errors\": { \"Logfile\": [\"<br>Failed to delete record. Error: " + ex.Message + "\"]}}";
						}
					}
				}
			}
			else
			{
				Cumulus.LogMessage($"EditCo2: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"Logfile\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}

			// return the updated record
			var rec = new List<string>(newData.data[0]);
			return rec.ToJson();
		}

		#endregion CO2

	}
}