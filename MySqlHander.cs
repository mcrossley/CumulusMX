using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MySql.Data.MySqlClient;

namespace CumulusMX
{
	internal class MySqlHander
	{
		internal MySqlConnectionStringBuilder ConnSettings = new MySqlConnectionStringBuilder();

		internal MySqlGeneralSettings Settings = new MySqlGeneralSettings();

		internal DateTime MySqlLastRealtimeTime;
		internal DateTime MySqlLastIntervalTime;

		internal MySqlTable RealtimeTable;
		internal MySqlTable MonthlyTable;
		internal MySqlTable DayfileTable;

		internal int CustomMinutesIntervalIndex;

		private readonly Cumulus cumulus;
		private WeatherStation station;

		private bool customSecondsUpdateInProgress;
		private bool customMinutesUpdateInProgress;
		private bool customRolloverUpdateInProgress;

		private readonly TokenParser customSecondsTokenParser = new TokenParser();
		private readonly TokenParser customMinutesTokenParser = new TokenParser();
		private readonly TokenParser customRolloverTokenParser = new TokenParser();

		internal System.Timers.Timer CustomSecondsTimer;

		// Use thread safe queues for the MySQL command lists
		internal readonly ConcurrentQueue<SqlCache> CatchUpList = new ConcurrentQueue<SqlCache>();
		internal ConcurrentQueue<SqlCache> FailedList = new ConcurrentQueue<SqlCache>();

		private readonly static DateTimeFormatInfo invDate = CultureInfo.InvariantCulture.DateTimeFormat;
		private readonly static NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;

		private bool SqlCatchingUp;

		internal MySqlHander(Cumulus cuml)
		{
			cumulus = cuml;
		}

		internal void InitialConfig(WeatherStation stn)
		{
			station = stn;

			customSecondsTokenParser.OnToken += cumulus.TokenParserOnToken;
			CustomSecondsTimer = new System.Timers.Timer { Interval = Settings.CustomSecs.Interval * 1000 };
			CustomSecondsTimer.Elapsed += CustomSecondsTimerTick;
			CustomSecondsTimer.AutoReset = true;

			customMinutesTokenParser.OnToken += cumulus.TokenParserOnToken;

			customRolloverTokenParser.OnToken += cumulus.TokenParserOnToken;

			SetupRealtimeTable();
			SetupMonthlyTable();
			SetupDayfileTable();
		}

		internal bool CheckConnection()
		{
			try
			{
				using var mySqlConn = new MySqlConnection(ConnSettings.ToString());
				mySqlConn.Open();
				// get the database name to check 100% we have a connection
				var db = mySqlConn.Database;
				Cumulus.LogMessage("MySqlCheckConnection: Connected to server ok, default database = " + db);
				mySqlConn.Close();
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal async Task CommandAsync(string Cmd, string CallingFunction)
		{
			var Cmds = new ConcurrentQueue<SqlCache>();
			Cmds.Enqueue(new SqlCache() { statement = Cmd });
			await CommandAsync(Cmds, CallingFunction, false);
		}

		internal async Task CommandAsync(List<string> Cmds, string CallingFunction)
		{
			var tempQ = new ConcurrentQueue<SqlCache>();

			foreach(var cmd in Cmds)
			{
				tempQ.Enqueue(new SqlCache() { statement = cmd });
			}
			await CommandAsync(tempQ, CallingFunction, false);
		}

		internal async Task CommandAsync(ConcurrentQueue<SqlCache> Cmds, string CallingFunction, bool UseFailedList)
		{
			await Task.Run(() =>
			{
				var queue = UseFailedList ? ref FailedList : ref Cmds;
				SqlCache cmdSql = null;

				try
				{
					using (var mySqlConn = new MySqlConnection(ConnSettings.ToString()))
					{
						var updated = 0;

						mySqlConn.Open();

						using var transaction = Cmds.Count > 2 ? mySqlConn.BeginTransaction() : null;
						{
							do
							{
								// Do not remove the item from the stack until we know the command worked
								if (queue.TryPeek(out cmdSql))
								{
									using MySqlCommand cmd = new MySqlCommand(cmdSql.statement, mySqlConn);

									cumulus.LogDebugMessage($"{CallingFunction}: MySQL executing - {cmdSql}");

									if (transaction != null)
									{
										cmd.Transaction = transaction;
									}

									updated += cmd.ExecuteNonQuery();

									cumulus.LogDebugMessage($"{CallingFunction}: MySQL {updated} rows were affected.");

									// Success, if using the failed list, delete from the databasec
									if (UseFailedList)
									{
										station.Database.Delete<SqlCache>(cmdSql.key);
									}
									// and pop the value from the queue
									queue.TryDequeue(out cmdSql);
								}
							} while (!queue.IsEmpty);

							if (transaction != null)
							{
								cumulus.LogDebugMessage($"{CallingFunction}: Committing {updated} updates to DB");
								transaction.Commit();
								cumulus.LogDebugMessage($"{CallingFunction}: Commit complete");
								transaction.Dispose();
							}
						}

						mySqlConn.Close();
						mySqlConn.Dispose();
					}

					cumulus.MySqlUploadAlarm.Triggered = false;
				}
				catch (Exception ex)
				{
					// if debug logging is disabled, then log the failing statement anyway
					if (!cumulus.DebuggingEnabled)
					{
						Cumulus.LogMessage($"{CallingFunction}: SQL = {cmdSql.statement}");
					}

					cumulus.LogExceptionMessage(ex, $"{CallingFunction}: Error encountered during MySQL operation");

					cumulus.MySqlUploadAlarm.LastError = ex.Message;
					cumulus.MySqlUploadAlarm.Triggered = true;

					// do we save this command/commands on failure to be resubmitted?
					// if we have a syntax error, it is never going to work so do not save it for retry
					if (Settings.BufferOnfailure && !UseFailedList)
					{
						// do we save this command/commands on failure to be resubmitted?
						// if we have a syntax error, it is never going to work so do not save it for retry
						// A selection of the more common(?) errors to ignore...
						var errorCode = (int) ex.Data["Server Error Code"];
						CommandErrorHandler(CallingFunction, errorCode, queue);
					}
				}
			});
		}

		internal void CommandSync(string Cmd, string CallingFunction)
		{
			var Cmds = new ConcurrentQueue<SqlCache>();
			Cmds.Enqueue(new SqlCache() { statement = Cmd });
			CommandSync(Cmds, CallingFunction, false);
		}

		internal void CommandSync(ConcurrentQueue<SqlCache> Cmds, string CallingFunction, bool UseFailedList)
		{
			var queue = UseFailedList ? ref FailedList : ref Cmds;
			SqlCache cmdSql = null;

			try
			{
				using var mySqlConn = new MySqlConnection(ConnSettings.ToString());
				{
					mySqlConn.Open();

					using var transaction = Cmds.Count > 2 ? mySqlConn.BeginTransaction() : null;
					{
						var updated = 0;

						do
						{
							// Do not remove the item from the stack until we know the command worked
							if (queue.TryPeek(out cmdSql))
							{
								using (MySqlCommand cmd = new MySqlCommand(cmdSql.statement, mySqlConn))
								{
									cumulus.LogDebugMessage($"{CallingFunction}: MySQL executing - {cmdSql}");

									if (transaction != null)
									{
										cmd.Transaction = transaction;
									}

									updated += cmd.ExecuteNonQuery();

									cumulus.LogDebugMessage($"{CallingFunction}: MySQL {updated} rows were affected.");
								}

								cumulus.MySqlUploadAlarm.Triggered = false;

								// Success, if using the failed list, delete from the databasec
								if (UseFailedList)
								{
									station.Database.Delete<SqlCache>(cmdSql.key);
								}
								// and pop the value from the queue
								queue.TryDequeue(out cmdSql);

							}
						} while (!queue.IsEmpty);

						if (transaction != null)
						{
							cumulus.LogDebugMessage($"{CallingFunction}: Committing {updated} updates to DB");
							transaction.Commit();
							cumulus.LogDebugMessage($"{CallingFunction}: Commit complete");
							transaction.Dispose();
						}
					}

					mySqlConn.Close();
					mySqlConn.Dispose();
				}
			}
			catch (Exception ex)
			{
				// if debug logging is disabled, then log the failing statement anyway
				if (!cumulus.DebuggingEnabled)
				{
					Cumulus.LogMessage($"{CallingFunction}: SQL = {cmdSql}");
				}

				cumulus.LogExceptionMessage(ex, $"{CallingFunction}: Error encountered during MySQL operation");

				cumulus.MySqlUploadAlarm.LastError = ex.Message;
				cumulus.MySqlUploadAlarm.Triggered = true;

				// do we save this command/commands on failure to be resubmitted?
				// if we have a syntax error, it is never going to work so do not save it for retry
				if (Settings.BufferOnfailure && !UseFailedList)
				{
					var errorCode = (int)ex.Data["Server Error Code"];
					CommandErrorHandler(CallingFunction, errorCode, queue);
				}

				throw;
			}
		}

		internal void CommandErrorHandler(string CallingFunction, int ErrorCode, ConcurrentQueue<SqlCache> Cmds)
		{
			var ignore = ErrorCode == (int)MySqlErrorCode.ParseError ||
						 ErrorCode == (int)MySqlErrorCode.EmptyQuery ||
						 ErrorCode == (int)MySqlErrorCode.TooBigSelect ||
						 ErrorCode == (int)MySqlErrorCode.InvalidUseOfNull ||
						 ErrorCode == (int)MySqlErrorCode.MixOfGroupFunctionAndFields ||
						 ErrorCode == (int)MySqlErrorCode.SyntaxError ||
						 ErrorCode == (int)MySqlErrorCode.TooLongString ||
						 ErrorCode == (int)MySqlErrorCode.WrongColumnName ||
						 ErrorCode == (int)MySqlErrorCode.DuplicateUnique ||
						 ErrorCode == (int)MySqlErrorCode.PrimaryCannotHaveNull ||
						 ErrorCode == (int)MySqlErrorCode.DivisionByZero ||
						 ErrorCode == (int)MySqlErrorCode.DuplicateKeyEntry;

			if (ignore)
			{
				cumulus.LogDebugMessage($"{CallingFunction}: Not buffering this command due to a problem with the query");
			}
			else
			{
				while (!Cmds.IsEmpty)
				{
					try
					{
						Cmds.TryDequeue(out var cmd);
						if (!cmd.statement.StartsWith("DELETE IGNORE FROM"))
						{
							cumulus.LogDebugMessage($"{CallingFunction}: Buffering command to failed list");

							_ = station.Database.Insert(cmd);
							FailedList.Enqueue(cmd);
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"{CallingFunction}: Error buffering command");
					}
				}
			}
		}

		internal async Task CheckMySQLFailedUploads(string callingFunction, string cmd)
		{
			await CheckMySQLFailedUploads(callingFunction, new List<string>() { cmd });
		}

		internal async Task CheckMySQLFailedUploads(string callingFunction, List<string> cmds)
		{
			var connectionOK = true;

			try
			{
				if (!FailedList.IsEmpty)
				{
					// flag we are processing the queue so the next task doesn't try as well
					SqlCatchingUp = true;

					Cumulus.LogMessage($"{callingFunction}: Failed MySQL updates are present");

					if (CheckConnection())
					{
						Thread.Sleep(500);
						Cumulus.LogMessage($"{callingFunction}: Connection to MySQL server is OK, trying to upload {FailedList.Count} failed commands");

						await CommandAsync(FailedList, callingFunction, true);
						Cumulus.LogMessage($"{callingFunction}: Upload of failed MySQL commands complete");
					}
					else if (Settings.BufferOnfailure)
					{
						connectionOK = false;
						Cumulus.LogMessage($"{callingFunction}: Connection to MySQL server has failed, adding this update to the failed list");
						if (callingFunction.StartsWith("Realtime["))
						{
							// don't bother buffering the realtime deletes - if present
							for (var i = 0; i < cmds.Count; i++)
							{
								if (!cmds[i].StartsWith("DELETE"))
								{
									var tmp = new SqlCache() { statement = cmds[i] };

									_ = station.Database.Insert(tmp);

									FailedList.Enqueue(tmp);
								}
							}
						}
						else
						{
							for (var i = 0; i < cmds.Count; i++)
							{
								var tmp = new SqlCache() { statement = cmds[i] };

								_ = station.Database.Insert(tmp);

								FailedList.Enqueue(tmp);
							}
						}
					}
					else
					{
						connectionOK = false;
					}

					SqlCatchingUp = false;
				}

				// now do what we came here to do
				if (connectionOK)
				{
					await CommandAsync(cmds, callingFunction);
				}
			}
			catch (Exception ex)
			{
				Cumulus.LogMessage($"{callingFunction}: Error - " + ex.Message);
				SqlCatchingUp = false;
			}
		}

		internal void DoRealtimeData(int cycle, bool live, DateTime? logdate = null)
		{
			DateTime timestamp = (DateTime)(live ? DateTime.Now : logdate);

			if (!Settings.Realtime.Enabled)
				return;

			if (Settings.RealtimeLimit1Minute && MySqlLastRealtimeTime.Minute == timestamp.Minute)
				return;

			MySqlLastRealtimeTime = timestamp;

			StringBuilder values = new StringBuilder(RealtimeTable.StartOfInsert, 1024);
			values.Append(" Values('");
			values.Append(timestamp.ToString("yy-MM-dd HH:mm:ss", invDate) + "',");
			values.Append((station.Temperature.HasValue ? station.Temperature.Value.ToString(cumulus.TempFormat, invNum) : "null") + ',');
			values.Append((station.Humidity.HasValue ? station.Humidity.Value.ToString() : "null") + ',');
			values.Append((station.Dewpoint.HasValue ? station.Dewpoint.Value.ToString(cumulus.TempFormat, invNum) : "null") + ',');
			values.Append((station.WindAverage.HasValue ? station.WindAverage.Value.ToString(cumulus.WindAvgFormat, invNum) : "null") + ',');
			values.Append((station.WindLatest.HasValue ? station.WindLatest.Value.ToString(cumulus.WindFormat, invNum) : "null") + ',');
			values.Append(station.Bearing.ToString() + ',');
			values.Append((station.RainRate.HasValue ? station.RainRate.Value.ToString(cumulus.RainFormat, invNum) : "null") + ',');
			values.Append((station.RainToday.HasValue ? station.RainToday.Value.ToString(cumulus.RainFormat, invNum) : "null") + ',');
			values.Append((station.Pressure.HasValue ? station.Pressure.Value.ToString(cumulus.PressFormat, invNum) : "null") + ",'");
			values.Append(WeatherStation.CompassPoint(station.Bearing) + "','");
			values.Append((station.WindAverage.HasValue ? cumulus.Beaufort(station.WindAverage.Value) : "null") + "','");
			values.Append(cumulus.Units.WindText + "','");
			values.Append(cumulus.Units.TempText[1].ToString() + "','");
			values.Append(cumulus.Units.PressText + "','");
			values.Append(cumulus.Units.RainText + "',");
			values.Append(station.WindRunToday.ToString(cumulus.WindRunFormat, invNum) + ",'");
			values.Append((station.presstrendval > 0 ? '+' + station.presstrendval.ToString(cumulus.PressFormat, invNum) : station.presstrendval.ToString(cumulus.PressFormat, invNum)) + "',");
			values.Append(station.RainMonth.ToString(cumulus.RainFormat, invNum) + ',');
			values.Append(station.RainYear.ToString(cumulus.RainFormat, invNum) + ',');
			values.Append((station.RainYesterday.HasValue ? station.RainYesterday.Value.ToString(cumulus.RainFormat, invNum) : "null") + ',');
			values.Append((station.IndoorTemp.HasValue ? station.IndoorTemp.Value.ToString(cumulus.TempFormat, invNum) : "null") + ',');
			values.Append((station.IndoorHum.HasValue ? station.IndoorHum.Value.ToString() : "null") + ',');
			values.Append((station.WindChill.HasValue ? station.WindChill.Value.ToString(cumulus.TempFormat, invNum) : "null") + ',');
			values.Append(station.temptrendval.ToString(cumulus.TempTrendFormat, invNum) + ',');
			values.Append((station.HiLoToday.HighTemp.HasValue ? station.HiLoToday.HighTemp.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",'");
			values.Append((station.HiLoToday.HighTemp.HasValue ? station.HiLoToday.HighTempTime.ToString("HH:mm", invDate) : "null") + "',");
			values.Append((station.HiLoToday.LowTemp.HasValue ? station.HiLoToday.LowTemp.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",'");
			values.Append((station.HiLoToday.LowTemp.HasValue ? station.HiLoToday.LowTempTime.ToString("HH:mm", invDate) : "null") + "',");
			values.Append((station.HiLoToday.HighWind.HasValue ? station.HiLoToday.HighWind.Value.ToString(cumulus.WindAvgFormat, invNum) : "null") + ",'");
			values.Append((station.HiLoToday.HighWind.HasValue ? station.HiLoToday.HighWindTime.ToString("HH:mm", invDate) : "null") + "',");
			values.Append((station.HiLoToday.HighGust.HasValue ? station.HiLoToday.HighGust.Value.ToString(cumulus.WindFormat, invNum) : "null") + ",'");
			values.Append((station.HiLoToday.HighGust.HasValue ? station.HiLoToday.HighGustTime.ToString("HH:mm", invDate) : "null") + "',");
			values.Append((station.HiLoToday.HighPress.HasValue ? station.HiLoToday.HighPress.Value.ToString(cumulus.PressFormat, invNum) : "null") + ",'");
			values.Append((station.HiLoToday.HighPress.HasValue ? station.HiLoToday.HighPressTime.ToString("HH:mm", invDate) : "null") + "',");
			values.Append((station.HiLoToday.LowPress.HasValue ? station.HiLoToday.LowPress.Value.ToString(cumulus.PressFormat, invNum) : "null") + ",'");
			values.Append((station.HiLoToday.LowPress.HasValue ? station.HiLoToday.LowPressTime.ToString("HH:mm", invDate) : "null") + "','");
			values.Append(cumulus.Version + "','");
			values.Append(cumulus.Build + "',");
			values.Append((station.RecentMaxGust.HasValue ? station.RecentMaxGust.Value.ToString(cumulus.WindFormat, invNum) : "null") + ',');
			values.Append((station.HeatIndex.HasValue ? station.HeatIndex.Value.ToString(cumulus.TempFormat, invNum) : "null") + ',');
			values.Append((station.Humidex.HasValue ? station.Humidex.Value.ToString(cumulus.TempFormat, invNum) : "null") + ',');
			values.Append((station.UV.HasValue ? station.UV.Value.ToString(cumulus.UVFormat, invNum) : "null") + ',');
			values.Append(station.ET.ToString(cumulus.ETFormat, invNum) + ',');
			values.Append((station.SolarRad.HasValue ? station.SolarRad.Value.ToString() : "null") + ',');
			values.Append(station.AvgBearing.ToString() + ',');
			values.Append(station.RainLastHour.ToString(cumulus.RainFormat, invNum) + ',');
			values.Append(station.Forecastnumber.ToString() + ",'");
			values.Append((cumulus.IsDaylight() ? "1" : "0") + "','");
			values.Append((station.SensorContactLost ? "1" : "0") + "','");
			values.Append(WeatherStation.CompassPoint(station.AvgBearing) + "',");
			values.Append((station.CloudBase.HasValue ? station.CloudBase.Value.ToString() : "null") + ",'");
			values.Append((cumulus.CloudBaseInFeet ? "ft" : "m") + "',");
			values.Append((station.ApparentTemp.HasValue ? station.ApparentTemp.Value.ToString(cumulus.TempFormat, invNum) : "null") + ',');
			values.Append(station.SunshineHours.ToString(cumulus.SunFormat, invNum) + ',');
			values.Append((station.CurrentSolarMax.HasValue ? station.CurrentSolarMax.Value.ToString() : "null") + ",'");
			values.Append((station.IsSunny ? "1" : "0") + "',");
			values.Append((station.FeelsLike.HasValue ? station.FeelsLike.Value.ToString(cumulus.TempFormat, invNum) : "null"));
			values.Append(')');

			string valuesString = values.ToString();
			List<string> cmds = new List<string>() { valuesString };

			if (live)
			{
				if (!string.IsNullOrEmpty(Settings.RealtimeRetention))
				{
					cmds.Add($"DELETE IGNORE FROM {Settings.Realtime.TableName} WHERE LogDateTime < DATE_SUB('{DateTime.Now:yyyy-MM-dd HH:mm}', INTERVAL {Settings.RealtimeRetention});");
				}

				// do the update, and let it run in background
				_ = CheckMySQLFailedUploads($"Realtime[{cycle}]", cmds);
			}
			else
			{
				// not live, buffer the command for later
				CatchUpList.Enqueue(new SqlCache() { statement = cmds[0] });
			}
		}

		internal void DoIntervalData(DateTime timestamp, bool live)
		{
			MySqlLastIntervalTime= timestamp;

			StringBuilder values = new StringBuilder(MonthlyTable.StartOfInsert, 600);
			values.Append(" Values('");
			values.Append(timestamp.ToString("yy-MM-dd HH:mm", invDate) + "',");
			values.Append((station.Temperature.HasValue ? station.Temperature.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",");
			values.Append((station.Humidity.HasValue ? station.Humidity.Value : "null") + ",");
			values.Append((station.Dewpoint.HasValue ? station.Dewpoint.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",");
			values.Append((station.WindAverage.HasValue ? station.WindAverage.Value.ToString(cumulus.WindAvgFormat, invNum) : "null") + ",");
			values.Append((station.RecentMaxGust.HasValue ? station.RecentMaxGust.Value.ToString(cumulus.WindFormat, invNum) : "null") + ",");
			values.Append(station.AvgBearing + ",");
			values.Append((station.RainRate.HasValue ? station.RainRate.Value.ToString(cumulus.RainFormat, invNum) : "null") + ",");
			values.Append((station.RainToday.HasValue ? station.RainToday.Value.ToString(cumulus.RainFormat, invNum) : "null") + ",");
			values.Append((station.Pressure.HasValue ? station.Pressure.Value.ToString(cumulus.PressFormat, invNum) : "null") + ",");
			values.Append(station.Raincounter.ToString(cumulus.RainFormat, invNum) + ",");
			values.Append((station.IndoorTemp.HasValue ? station.IndoorTemp.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",");
			values.Append((station.IndoorHum.HasValue ? station.IndoorHum.Value : "null") + ",");
			values.Append((station.WindLatest.HasValue ? station.WindLatest.Value.ToString(cumulus.WindFormat, invNum) : "null") + ",");
			values.Append((station.WindChill.HasValue ? station.WindChill.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",");
			values.Append((station.HeatIndex.HasValue ? station.HeatIndex.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",");
			values.Append((station.UV.HasValue ? station.UV.Value.ToString(cumulus.UVFormat, invNum) : "null") + ",");
			values.Append((station.SolarRad.HasValue ? station.SolarRad.Value : "null") + ",");
			values.Append(station.ET.ToString(cumulus.ETFormat, invNum) + ",");
			values.Append(station.AnnualETTotal.ToString(cumulus.ETFormat, invNum) + ",");
			values.Append((station.ApparentTemp.HasValue ? station.ApparentTemp.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",");
			values.Append((station.CurrentSolarMax.HasValue ? station.CurrentSolarMax.Value : "null") + ",");
			values.Append(station.SunshineHours.ToString(cumulus.SunFormat, invNum) + ",");
			values.Append(station.Bearing + ",");
			values.Append(station.RG11RainToday.ToString(cumulus.RainFormat, invNum) + ",");
			values.Append(station.RainSinceMidnight.ToString(cumulus.RainFormat, invNum) + ",'");
			values.Append(WeatherStation.CompassPoint(station.AvgBearing) + "','");
			values.Append(WeatherStation.CompassPoint(station.Bearing) + "',");
			values.Append((station.FeelsLike.HasValue ? station.FeelsLike.Value.ToString(cumulus.TempFormat, invNum) : "null") + ",");
			values.Append((station.Humidex.HasValue ? station.Humidex.Value.ToString(cumulus.TempFormat, invNum) : "null"));
			values.Append(')');

			string queryString = values.ToString();

			if (live)
			{
				// do the update
				_ = CheckMySQLFailedUploads("DoLogFile", queryString);
			}
			else
			{
				// save the string for later
				CatchUpList.Enqueue(new SqlCache() { statement = queryString });
			}
		}

		internal void DoDailyData(DateTime timestamp, double AvgTemp)
		{
			StringBuilder queryString = new StringBuilder(cumulus.MySqlStuff.DayfileTable.StartOfInsert, 1024);
			queryString.Append(" Values('");
			queryString.Append(timestamp.AddDays(-1).ToString("yy-MM-dd") + "',");
			if (station.HiLoToday.HighGust.HasValue)
			{
				queryString.Append(station.HiLoToday.HighGust.Value.ToString(cumulus.WindFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighGustBearing + ",");
				queryString.Append(station.HiLoToday.HighGustTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,null,");
			}
			if (station.HiLoToday.LowTemp.HasValue)
			{
				queryString.Append(station.HiLoToday.LowTemp.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.LowTempTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.HighTemp.HasValue)
			{
				queryString.Append(station.HiLoToday.HighTemp.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighTempTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.LowPress.HasValue)
			{
				queryString.Append(station.HiLoToday.LowPress.Value.ToString(cumulus.PressFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.LowPressTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.HighPress.HasValue)
			{
				queryString.Append(station.HiLoToday.HighPress.Value.ToString(cumulus.PressFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighPressTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.HighRainRate.HasValue)
			{
				queryString.Append(station.HiLoToday.HighRainRate.Value.ToString(cumulus.RainFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighRainRateTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			queryString.Append((station.RainToday.HasValue ? station.RainToday.Value.ToString(cumulus.RainFormat, invNum) : "null") + ",");
			queryString.Append(AvgTemp.ToString(cumulus.TempFormat, invNum) + ",");
			queryString.Append(station.WindRunToday.ToString("F1", invNum) + ",");
			if (station.HiLoToday.HighWind.HasValue)
			{
				queryString.Append(station.HiLoToday.HighWind.Value.ToString(cumulus.WindAvgFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighWindTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.LowHumidity.HasValue)
			{
				queryString.Append(station.HiLoToday.LowHumidity + ",");
				queryString.Append(station.HiLoToday.LowHumidityTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.HighHumidity.HasValue)
			{
				queryString.Append(station.HiLoToday.HighHumidity + ",");
				queryString.Append(station.HiLoToday.HighHumidityTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			queryString.Append(station.ET.ToString(cumulus.ETFormat, invNum) + ",");
			queryString.Append((cumulus.RolloverHour == 0 ? station.SunshineHours.ToString(cumulus.SunFormat, invNum) : station.SunshineToMidnight.ToString(cumulus.SunFormat, invNum)) + ",");
			if (station.HiLoToday.HighHeatIndex.HasValue)
			{
				queryString.Append(station.HiLoToday.HighHeatIndex.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighHeatIndexTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.HighAppTemp.HasValue)
			{
				queryString.Append(station.HiLoToday.HighAppTemp.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighAppTempTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.LowAppTemp.HasValue)
			{
				queryString.Append(station.HiLoToday.LowAppTemp.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.LowAppTempTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			queryString.Append(station.HiLoToday.HighHourlyRain.ToString(cumulus.RainFormat, invNum) + ",");
			queryString.Append(station.HiLoToday.HighHourlyRainTime.ToString("\\'HH:mm\\'") + ",");
			if (station.HiLoToday.LowWindChill.HasValue)
			{
				queryString.Append(station.HiLoToday.LowWindChill.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.LowWindChillTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.HighDewPoint.HasValue)
			{
				queryString.Append(station.HiLoToday.HighDewPoint.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighDewPointTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.LowDewPoint.HasValue)
			{
				queryString.Append(station.HiLoToday.LowDewPoint.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.LowDewPointTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			queryString.Append(station.DominantWindBearing + ",");
			queryString.Append(station.HeatingDegreeDays.ToString("F1", invNum) + ",");
			queryString.Append(station.CoolingDegreeDays.ToString("F1", invNum) + ",");
			queryString.Append(station.HiLoToday.HighSolar + ",");
			queryString.Append(station.HiLoToday.HighSolarTime.ToString("\\'HH:mm\\'") + ",");
			if (station.HiLoToday.HighUv.HasValue)
			{
				queryString.Append(station.HiLoToday.HighUv.Value.ToString(cumulus.UVFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighUvTime.ToString("\\'HH:mm\\'") + ",'");
			}
			else
			{
				queryString.Append("null,null,");
			}
			queryString.Append(WeatherStation.CompassPoint(station.HiLoToday.HighGustBearing) + "','");
			queryString.Append(WeatherStation.CompassPoint(station.DominantWindBearing) + "',");
			if (station.HiLoToday.HighFeelsLike.HasValue)
			{
				queryString.Append(station.HiLoToday.HighFeelsLike.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighFeelsLikeTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.LowFeelsLike.HasValue)
			{
				queryString.Append(station.HiLoToday.LowFeelsLike.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.LowFeelsLikeTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			if (station.HiLoToday.HighHumidex.HasValue)
			{
				queryString.Append(station.HiLoToday.HighHumidex.Value.ToString(cumulus.TempFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighHumidexTime.ToString("\\'HH:mm\\'") + ",");
			}
			else
			{
				queryString.Append("null,null,");
			}
			queryString.Append(station.ChillHours.ToString(cumulus.TempFormat, invNum) + ",");
			if (station.HiLoToday.HighRain24h.HasValue)
			{
				queryString.Append(station.HiLoToday.HighRain24h.Value.ToString(cumulus.RainFormat, invNum) + ",");
				queryString.Append(station.HiLoToday.HighRain24hTime.ToString("\\'HH:mm\\'"));
			}
			else
			{
				queryString.Append("null,null");
			}

			queryString.Append(')');

			// run the query async so we do not block the main EOD processing
			_ = CommandAsync(queryString.ToString(), "MySQL Dayfile");
		}

		internal async void CustomSecondsTimerTick(object sender, ElapsedEventArgs e)
		{
			if (station.DataStopped)
			{
				// No data coming in, do not do anything
				return;
			}

			if (station.DataStopped)
			{
				// No data coming in, do not do anything
				return;
			}

			if (!customSecondsUpdateInProgress)
			{
				customSecondsUpdateInProgress = true;

				for (var i = 0; i < 10; i++)
				{
					try
					{
						if (!string.IsNullOrEmpty(Settings.CustomSecs.Commands[i]))
						{
							customSecondsTokenParser.InputText = Settings.CustomSecs.Commands[i];
							await CheckMySQLFailedUploads($"CustomSqlSecs[{i}]", customSecondsTokenParser.ToStringFromString());
						}

					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"CustomSqlSecs[{i}]: Error");
					}
				}

				customSecondsUpdateInProgress = false;
			}
		}

		internal async Task CustomMinutesTimerTick()
		{
			if (station.DataStopped)
			{
				// No data coming in, do not do anything
				return;
			}

			if (!customMinutesUpdateInProgress)
			{
				customMinutesUpdateInProgress = true;

				for (var i = 0; i < 10; i++)
				{
					try
					{
						if (!string.IsNullOrEmpty(Settings.CustomMins.Commands[i]))
						{
							customMinutesTokenParser.InputText = Settings.CustomMins.Commands[i];
							await CheckMySQLFailedUploads($"CustomSqlMins[{i}]", customMinutesTokenParser.ToStringFromString());
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"CustomSqlMins[{i}]: Error");
					}

					// now do what we came here to do
					await CommandAsync(customMinutesTokenParser.ToStringFromString(), $"CustomSqlMins[{i}]");
				}

				customMinutesUpdateInProgress = false;
			}
		}

		internal async Task CustomRolloverTimerTick()
		{
			if (station.DataStopped)
			{
				// No data coming in, do not do anything
				return;
			}

			if (!customRolloverUpdateInProgress)
			{
				customRolloverUpdateInProgress = true;

				for (var i = 0; i < 10; i++)
				{

					try
					{
						if (!string.IsNullOrEmpty(Settings.CustomRollover.Commands[i]))
						{
							customRolloverTokenParser.InputText = Settings.CustomRollover.Commands[i];
							await CheckMySQLFailedUploads($"CustomSqlRollover[{i}]", customRolloverTokenParser.ToStringFromString());
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"CustomSqlRollover[{i}]: Error");
					}
				}

				customRolloverUpdateInProgress = false;
			}
		}

		internal void SetupRealtimeTable()
		{
			RealtimeTable = new MySqlTable(Settings.Realtime.TableName);
			RealtimeTable.AddColumn("LogDateTime", "DATETIME NOT NULL");
			RealtimeTable.AddColumn("temp", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("hum", "decimal(4," + cumulus.HumDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("dew", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("wspeed", "decimal(4," + cumulus.WindDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("wlatest", "decimal(4," + cumulus.WindDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("bearing", "VARCHAR(3) NOT NULL");
			RealtimeTable.AddColumn("rrate", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("rfall", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("press", "decimal(6," + cumulus.PressDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("currentwdir", "VARCHAR(3) NOT NULL");
			RealtimeTable.AddColumn("beaufortnumber", "varchar(2) NOT NULL");
			RealtimeTable.AddColumn("windunit", "varchar(4) NOT NULL");
			RealtimeTable.AddColumn("tempunitnodeg", "varchar(1) NOT NULL");
			RealtimeTable.AddColumn("pressunit", "varchar(3) NOT NULL");
			RealtimeTable.AddColumn("rainunit", "varchar(2) NOT NULL");
			RealtimeTable.AddColumn("windrun", "decimal(4," + cumulus.WindRunDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("presstrendval", "varchar(6) NOT NULL");
			RealtimeTable.AddColumn("rmonth", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("ryear", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("rfallY", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("intemp", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("inhum", "decimal(4," + cumulus.HumDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("wchill", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("temptrend", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("tempTH", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("TtempTH", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("tempTL", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("TtempTL", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("windTM", "decimal(4," + cumulus.WindDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("TwindTM", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("wgustTM", "decimal(4," + cumulus.WindDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("TwgustTM", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("pressTH", "decimal(6," + cumulus.PressDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("TpressTH", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("pressTL", "decimal(6," + cumulus.PressDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("TpressTL", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("version", "varchar(8) NOT NULL");
			RealtimeTable.AddColumn("build", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("wgust", "decimal(4," + cumulus.WindDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("heatindex", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("humidex", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("UV", "decimal(3," + cumulus.UVDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("ET", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("SolarRad", "decimal(5,1) NOT NULL");
			RealtimeTable.AddColumn("avgbearing", "varchar(3) NOT NULL");
			RealtimeTable.AddColumn("rhour", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("forecastnumber", "varchar(2) NOT NULL");
			RealtimeTable.AddColumn("isdaylight", "varchar(1) NOT NULL");
			RealtimeTable.AddColumn("SensorContactLost", "varchar(1) NOT NULL");
			RealtimeTable.AddColumn("wdir", "varchar(3) NOT NULL");
			RealtimeTable.AddColumn("cloudbasevalue", "varchar(5) NOT NULL");
			RealtimeTable.AddColumn("cloudbaseunit", "varchar(2) NOT NULL");
			RealtimeTable.AddColumn("apptemp", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("SunshineHours", "decimal(3," + cumulus.SunshineDPlaces + ") NOT NULL");
			RealtimeTable.AddColumn("CurrentSolarMax", "decimal(5,1) NOT NULL");
			RealtimeTable.AddColumn("IsSunny", "varchar(1) NOT NULL");
			RealtimeTable.AddColumn("FeelsLike", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			RealtimeTable.PrimaryKey = "LogDateTime";
			RealtimeTable.Comment = "\"Realtime log\"";
		}

		internal void SetupMonthlyTable()
		{
			MonthlyTable = new MySqlTable(Settings.Monthly.TableName);
			MonthlyTable.AddColumn("LogDateTime", "DATETIME NOT NULL");
			MonthlyTable.AddColumn("Temp", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("Humidity", "decimal(4," + cumulus.HumDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("Dewpoint", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("Windspeed", "decimal(4," + cumulus.WindAvgDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("Windgust", "decimal(4," + cumulus.WindDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("Windbearing", "VARCHAR(3) NOT NULL");
			MonthlyTable.AddColumn("RainRate", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("TodayRainSoFar", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("Pressure", "decimal(6," + cumulus.PressDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("Raincounter", "decimal(6," + cumulus.RainDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("InsideTemp", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("InsideHumidity", "decimal(4," + cumulus.HumDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("LatestWindGust", "decimal(5," + cumulus.WindDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("WindChill", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("HeatIndex", "decimal(4," + cumulus.TempDPlaces + ") NOT NULL");
			MonthlyTable.AddColumn("UVindex", "decimal(4," + cumulus.UVDPlaces + ")");
			MonthlyTable.AddColumn("SolarRad", "decimal(5,1)");
			MonthlyTable.AddColumn("Evapotrans", "decimal(4," + cumulus.RainDPlaces + ")");
			MonthlyTable.AddColumn("AnnualEvapTran", "decimal(5," + cumulus.RainDPlaces + ")");
			MonthlyTable.AddColumn("ApparentTemp", "decimal(4," + cumulus.TempDPlaces + ")");
			MonthlyTable.AddColumn("MaxSolarRad", "decimal(5,1)");
			MonthlyTable.AddColumn("HrsSunShine", "decimal(3," + cumulus.SunshineDPlaces + ")");
			MonthlyTable.AddColumn("CurrWindBearing", "varchar(3)");
			MonthlyTable.AddColumn("RG11rain", "decimal(4," + cumulus.RainDPlaces + ")");
			MonthlyTable.AddColumn("RainSinceMidnight", "decimal(4," + cumulus.RainDPlaces + ")");
			MonthlyTable.AddColumn("WindbearingSym", "varchar(3)");
			MonthlyTable.AddColumn("CurrWindBearingSym", "varchar(3)");
			MonthlyTable.AddColumn("FeelsLike", "decimal(4," + cumulus.TempDPlaces + ")");
			MonthlyTable.AddColumn("Humidex", "decimal(4," + cumulus.TempDPlaces + ")");
			MonthlyTable.PrimaryKey = "LogDateTime";
			MonthlyTable.Comment = "\"Monthly logs from Cumulus\"";
		}

		internal void SetupDayfileTable()
		{
			DayfileTable = new MySqlTable(Settings.Dayfile.TableName);
			DayfileTable.AddColumn("LogDate", "date NOT NULL");
			DayfileTable.AddColumn("HighWindGust", "decimal(4," + cumulus.WindDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("HWindGBear", "varchar(3) NOT NULL");
			DayfileTable.AddColumn("THWindG", "varchar(5) NOT NULL");
			DayfileTable.AddColumn("MinTemp", "decimal(5," + cumulus.TempDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("TMinTemp", "varchar(5) NOT NULL");
			DayfileTable.AddColumn("MaxTemp", "decimal(5," + cumulus.TempDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("TMaxTemp", "varchar(5) NOT NULL");
			DayfileTable.AddColumn("MinPress", "decimal(6," + cumulus.PressDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("TMinPress", "varchar(5) NOT NULL");
			DayfileTable.AddColumn("MaxPress", "decimal(6," + cumulus.PressDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("TMaxPress", "varchar(5) NOT NULL");
			DayfileTable.AddColumn("MaxRainRate", "decimal(4," + cumulus.RainDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("TMaxRR", "varchar(5) NOT NULL");
			DayfileTable.AddColumn("TotRainFall", "decimal(6," + cumulus.RainDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("AvgTemp", "decimal(5," + cumulus.TempDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("TotWindRun", "decimal(5," + cumulus.WindRunDPlaces + ") NOT NULL");
			DayfileTable.AddColumn("HighAvgWSpeed", "decimal(3," + cumulus.WindAvgDPlaces + ")");
			DayfileTable.AddColumn("THAvgWSpeed", "varchar(5)");
			DayfileTable.AddColumn("LowHum", "decimal(4," + cumulus.HumDPlaces + ")");
			DayfileTable.AddColumn("TLowHum", "varchar(5)");
			DayfileTable.AddColumn("HighHum", "decimal(4," + cumulus.HumDPlaces + ")");
			DayfileTable.AddColumn("THighHum", "varchar(5)");
			DayfileTable.AddColumn("TotalEvap", "decimal(5," + cumulus.RainDPlaces + ")");
			DayfileTable.AddColumn("HoursSun", "decimal(3," + cumulus.SunshineDPlaces + ")");
			DayfileTable.AddColumn("HighHeatInd", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("THighHeatInd", "varchar(5)");
			DayfileTable.AddColumn("HighAppTemp", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("THighAppTemp", "varchar(5)");
			DayfileTable.AddColumn("LowAppTemp", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("TLowAppTemp", "varchar(5)");
			DayfileTable.AddColumn("HighHourRain", "decimal(4," + cumulus.RainDPlaces + ")");
			DayfileTable.AddColumn("THighHourRain", "varchar(5)");
			DayfileTable.AddColumn("LowWindChill", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("TLowWindChill", "varchar(5)");
			DayfileTable.AddColumn("HighDewPoint", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("THighDewPoint", "varchar(5)");
			DayfileTable.AddColumn("LowDewPoint", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("TLowDewPoint", "varchar(5)");
			DayfileTable.AddColumn("DomWindDir", "varchar(3)");
			DayfileTable.AddColumn("HeatDegDays", "decimal(4,1)");
			DayfileTable.AddColumn("CoolDegDays", "decimal(4,1)");
			DayfileTable.AddColumn("HighSolarRad", "decimal(5,1)");
			DayfileTable.AddColumn("THighSolarRad", "varchar(5)");
			DayfileTable.AddColumn("HighUV", "decimal(3," + cumulus.UVDPlaces + ")");
			DayfileTable.AddColumn("THighUV", "varchar(5)");
			DayfileTable.AddColumn("HWindGBearSym", "varchar(3)");
			DayfileTable.AddColumn("DomWindDirSym", "varchar(3)");
			DayfileTable.AddColumn("MaxFeelsLike", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("TMaxFeelsLike", "varchar(5)");
			DayfileTable.AddColumn("MinFeelsLike", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("TMinFeelsLike", "varchar(5)");
			DayfileTable.AddColumn("MaxHumidex", "decimal(5," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("TMaxHumidex", "varchar(5)");
			DayfileTable.AddColumn("ChillHours", "decimal(7," + cumulus.TempDPlaces + ")");
			DayfileTable.AddColumn("HighRain24h", "decimal(6," + cumulus.RainDPlaces + ")");
			DayfileTable.AddColumn("THighRain24h", "varchar(5)");
			DayfileTable.PrimaryKey = "LogDate";
			DayfileTable.Comment = "\"Dayfile from Cumulus\"";
		}
	}
}
