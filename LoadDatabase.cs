using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ServiceStack.Text;

namespace CumulusMX
{
	internal static class LoadDatabase
	{
		private static NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;
		private static DateTimeFormatInfo invDate = CultureInfo.InvariantCulture.DateTimeFormat;

		public static string LoadDayFileToDb(WeatherStation station)
		{
			Cumulus cumulus = Program.cumulus;
			int addedEntries = 0;
			long start;

			var rowsToAdd = new List<DayData>();

			Cumulus.LogMessage($"LoadDayFileToDb: Attempting to load the daily data");

			var watch = Stopwatch.StartNew();

			// try and find the first entry in the database that has a "blank" AQ entry (PM2.5 or PM10 = -1)
			try
			{
				start = station.Database.ExecuteScalar<long>("select MAX(Timestamp) from DayData");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "LoadDayFileToDb: Error querying database for latest record");
				start = 0;
			}


			if (File.Exists(cumulus.DayFileName))
			{
				int linenum = 0;
				int errorCount = 0;

				try
				{
					using var sr = new StreamReader(cumulus.DayFileName);
					var doMore = true;
					do
					{
						try
						{
							// process each record in the file

							linenum++;
							string Line = sr.ReadLine();

							var newRec = new DayData();
							var ok = newRec.ParseDayFileRecV4(Line);

							if (ok && newRec.Timestamp > start)
							{
								rowsToAdd.Add(newRec);
								addedEntries++;
							}
							else
							{
								doMore = false;
							}


						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, $"LoadDayFileToDb: Error at line {linenum} of {cumulus.DayFileName}");
							Cumulus.LogMessage("Please edit the file to correct the error");
							errorCount++;
							if (errorCount >= 20)
							{
								Cumulus.LogMessage($"LoadDayFileToDb: Too many errors reading {cumulus.DayFileName} - aborting load of daily data");
							}
						}
					} while (!(sr.EndOfStream || errorCount >= 20) && doMore);
					sr.Close();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, $"LoadDayFileToDb: Error at line {linenum} of {cumulus.DayFileName}");
					Cumulus.LogMessage("Please edit the file to correct the error");
				}

				// Anything to add to the data base?
				try
				{
					station.Database.InsertAll(rowsToAdd);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "LoadDayFileToDb: Error inserting daily data into database");
				}


				watch.Stop();
				cumulus.LogDebugMessage($"LoadDayFileToDb: Dayfile load = {watch.ElapsedMilliseconds} ms");
				var msg = $"LoadDayFileToDb: Loaded {addedEntries} entries to the daily data table";
				Cumulus.LogMessage(msg);
				return msg;

			}
			else
			{
				var msg = "LoadDayFileToDb: No Dayfile found - No entries added to the daily data table";
				Cumulus.LogMessage(msg);
				return msg;
			}
		}


		public static void LoadLogFilesToDb(WeatherStation station)
		{
			Cumulus cumulus = Program.cumulus;
			DateTime lastLogDate;
			long lastLogTimestamp;

			Cumulus.LogMessage("LoadLogFilesToDb: Starting Process");

			try
			{
				// get the last date time from the database - if any
				lastLogTimestamp = station.Database.ExecuteScalar<long>("select max(Timestamp) from IntervalData");
				lastLogDate = lastLogTimestamp.FromUnixTime().ToLocalTime();
				Cumulus.LogMessage($"LoadLogFilesToDb: Last data logged in database = {lastLogDate.ToString("yyyy-MM-dd HH:mm", invDate)}");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "LoadLogFilesToDb: Error querying the database for the last logged data time");
				return;
			}


			if (lastLogDate == DateTime.MinValue)
				lastLogDate = cumulus.RecordsBeganDate;

			// Check the last data time against the time now and see it is within a logging period window
			if (lastLogDate.AddMinutes(cumulus.logints[cumulus.DataLogInterval]) > cumulus.LastUpdateTime)
			{
				// the database is up to date - nothing to do here
				Cumulus.LogMessage("LoadLogFilesToDb: The database is up to date");
				return;
			}


			var finished = false;
			var dataToLoad = new List<IntervalData>();
			var fileDate = lastLogDate;
			var logFile = cumulus.GetLogFileName(fileDate);
			int totalInserted = 0;

			Console.WriteLine();

			while (!finished)
			{
				if (File.Exists(logFile))
				{

					cumulus.LogDebugMessage($"LoadLogFilesToDb: Processing log file - {logFile}");

					Console.Write($"\rLoading log file for {fileDate:yyyy-MM} to the database");

					var linenum = 0;
					try
					{
						var logfile = File.ReadAllLines(logFile);

						foreach (var line in logfile)
						{
							// process each record in the file
							linenum++;
							var rec = new IntervalData();
							rec.FromString(line.Split(','));
							if (rec.Timestamp >= lastLogTimestamp)
								dataToLoad.Add(rec);
						}

						// load the data a month at a time into the database so we do not hold it all in memory
						// now load the data into the database
						if (dataToLoad.Count > 0)
						{
							try
							{
								cumulus.LogDebugMessage($"LoadLogFilesToDb: Loading {dataToLoad.Count} rows into the database");
								var inserted = station.Database.InsertAll(dataToLoad, "OR IGNORE");
								totalInserted += inserted;
								cumulus.LogDebugMessage($"LoadLogFilesToDb: Inserted {inserted} rows into the database");
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "LoadLogFilesToDb: Error inserting the data into the database");
							}
						}

						// clear the db List
						dataToLoad.Clear();
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"LoadLogFilesToDb: Error at line {linenum} of {logFile}");
						Cumulus.LogMessage("Please edit the file to correct the error");
					}
				}
				else
				{
					cumulus.LogDebugMessage($"LoadLogFilesToDb: Log file  not found - {logFile}");
				}
				if (fileDate >= DateTime.Now)
				{
					finished = true;
					cumulus.LogDebugMessage("LoadLogFilesToDb: Finished processing the log files");
				}
				else
				{
					cumulus.LogDebugMessage($"LoadLogFilesToDb: Finished processing log file - {logFile}");
					fileDate = fileDate.AddMonths(1);
					logFile = cumulus.GetLogFileName(fileDate);
				}
			}
			Console.WriteLine($"\rCompleted loading the log files to the database. {totalInserted} rows added\n");
		}

		public static void LoadExtraFilesToDb(WeatherStation station)
		{
			Cumulus cumulus = Program.cumulus;
			DateTime lastLogDate;
			long lastLogTimestamp;

			var Temps = new List<ExtraTemp>();
			var Humidities = new List<ExtraHum>();
			var DewPoints = new List<ExtraDewPoint>();
			var SoilTemps = new List<SoilTemp>();
			var SoilMoists = new List<SoilMoist>();
			var LeafTemps = new List<LeafTemp>();
			var LeafWets = new List<LeafWet>();
			var AirQuals = new List<AirQuality>();
			var UserTemps = new List<UserTemp>();
			var CO2Datas = new List<CO2Data>();

			Cumulus.LogMessage("LoadExtraFilesToDb: Starting Process");

			try
			{
				// get the last date time from the database - if any
				lastLogTimestamp = station.Database.ExecuteScalar<long>("select max(Timestamp) from ExtraTemp");
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from ExtraHum"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from ExtraDewPoint"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from SoilTemp"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from SoilMoist"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from LeafTemp"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from LeafWet"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from AirQuality"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from UserTemp"));
				lastLogTimestamp = Math.Max(lastLogTimestamp, station.Database.ExecuteScalar<long>("select max(Timestamp) from CO2Data"));
				lastLogDate = lastLogTimestamp.FromUnixTime().ToLocalTime();
				Cumulus.LogMessage($"LoadExtraFilesToDb: Last data logged in database = {lastLogDate.ToString("yyyy-MM-dd HH:mm", invDate)}");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "LoadExtraFilesToDb: Error querying the database for the last logged data time");
				return;
			}


			if (lastLogDate == DateTime.MinValue)
			{
				lastLogDate = cumulus.RecordsBeganDate;
				lastLogTimestamp = 0;
			}

			// Check the last data time against the time now and see it is within a logging period window
			if (lastLogDate.AddMinutes(cumulus.logints[cumulus.DataLogInterval]) > cumulus.LastUpdateTime)
			{
				// the database is up to date - nothing to do here
				Cumulus.LogMessage("LoadExtraFilesToDb: The database is up to date");
				return;
			}


			var finished = false;
			var fileDate = lastLogDate;
			var logFile = cumulus.GetExtraLogFileName(fileDate);
			int totalInserted = 0;

			Console.WriteLine();

			while (!finished)
			{
				if (File.Exists(logFile))
				{

					cumulus.LogDebugMessage($"LoadExtraFilesToDb: Processing log file - {logFile}");

					Console.Write($"\rLoading Extra log file for {fileDate:yyyy-MM} to the database");

					var linenum = 0;
					try
					{
						var logfile = File.ReadAllLines(logFile);

						foreach (var line in logfile)
						{
							// process each record in the file
							linenum++;
							var data = line.Split(',');
							var _timestamp = long.Parse(data[1]);

							if (_timestamp >= lastLogTimestamp)
							{
								var temp = new ExtraTemp();
								temp.FromExtraLogFile(data);
								Temps.Add(temp);

								var hum = new ExtraHum();
								hum.FromExtraLogFile(data);
								Humidities.Add(hum);

								var dewpt = new ExtraDewPoint();
								dewpt.FromExtraLogFile(data);
								DewPoints.Add(dewpt);

								var soiltemp = new SoilTemp();
								soiltemp.FromExtraLogFile(data);
								SoilTemps.Add(soiltemp);

								var soilmoist = new SoilMoist();
								soilmoist.FromExtraLogFile(data);
								SoilMoists.Add(soilmoist);

								var leaftemp = new LeafTemp();
								leaftemp.FromExtraLogFile(data);
								LeafTemps.Add(leaftemp);

								var leafwet = new LeafWet();
								leafwet.FromExtraLogFile(data);
								LeafWets.Add(leafwet);

								var airqual = new AirQuality();
								airqual.FromExtraLogFile(data);
								AirQuals.Add(airqual);

								var usertemp = new UserTemp();
								usertemp.FromExtraLogFile(data);
								UserTemps.Add(usertemp);

								var co2 = new CO2Data();
								co2.FromExtraLogFile(data);
								CO2Datas.Add(co2);
							}
						}

						// load the data a month at a time into the database so we do not hold it all in memory
						// now load the data into the database
						if (Temps.Count > 0)
						{
							try
							{
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading ExtraTemp {Temps.Count} rows into the database");
								var inserted = station.Database.InsertAll(Temps, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading ExtraHumidity {Humidities.Count} rows into the database");
								inserted = station.Database.InsertAll(Humidities, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading ExtraDewPoints {DewPoints.Count} rows into the database");
								inserted = station.Database.InsertAll(DewPoints, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading SoilTemps {SoilTemps.Count} rows into the database");
								inserted = station.Database.InsertAll(SoilTemps, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading SoilMoist {SoilMoists.Count} rows into the database");
								inserted = station.Database.InsertAll(SoilMoists, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading LeafTemp {LeafTemps.Count} rows into the database");
								inserted = station.Database.InsertAll(LeafTemps, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading LeafWet {LeafWets.Count} rows into the database");
								inserted = station.Database.InsertAll(LeafWets, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading AirQuality {AirQuals.Count} rows into the database");
								inserted = station.Database.InsertAll(AirQuals, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading UserTemp {UserTemps.Count} rows into the database");
								inserted = station.Database.InsertAll(UserTemps, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;

								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Loading CO2 {CO2Datas.Count} rows into the database");
								inserted = station.Database.InsertAll(CO2Datas, "OR IGNORE");
								cumulus.LogDebugMessage($"LoadExtraFilesToDb: Inserted {inserted} rows into the database");
								totalInserted += inserted;
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "LoadExtraFilesToDb: Error inserting the data into the database");
							}
						}

						// clear the db Lists
						Temps.Clear();
						Humidities.Clear();
						DewPoints.Clear();
						SoilTemps.Clear();
						SoilMoists.Clear();
						LeafTemps.Clear();
						LeafWets.Clear();
						AirQuals.Clear();
						UserTemps.Clear();
						CO2Datas.Clear();
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"LoadExtraFilesToDb: Error at line {linenum} of {logFile}");
						Cumulus.LogMessage("Please edit the file to correct the error");
					}
				}
				else
				{
					cumulus.LogDebugMessage($"LoadExtraFilesToDb: Log file  not found - {logFile}");
				}
				if (fileDate >= DateTime.Now)
				{
					finished = true;
					cumulus.LogDebugMessage("LoadExtraFilesToDb: Finished processing the log files");
				}
				else
				{
					cumulus.LogDebugMessage($"LoadExtraFilesToDb: Finished processing log file - {logFile}");
					fileDate = fileDate.AddMonths(1);
					logFile = cumulus.GetLogFileName(fileDate);
				}
			}
			Console.WriteLine($"\rCompleted loading the Extra files to the database. {totalInserted} rows added\n");
		}


	}
}
