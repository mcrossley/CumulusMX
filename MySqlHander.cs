using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

		internal int RealtimeLastMinute = -1;

		internal string StartOfMonthlyInsertSQL;
		internal string StartOfDayfileInsertSQL;
		internal string StartOfRealtimeInsertSQL;

		internal string CreateMonthlySQL;
		internal string CreateDayfileSQL;
		internal string CreateRealtimeSQL;

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
		internal readonly ConcurrentQueue<string> CatchUpList = new ConcurrentQueue<string>();
		internal ConcurrentQueue<string> FailedList = new ConcurrentQueue<string>();

		private readonly static DateTimeFormatInfo invDate = CultureInfo.InvariantCulture.DateTimeFormat;
		private readonly static NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;


		internal MySqlHander(Cumulus cuml)
		{
			cumulus = cuml;
		}

		internal void InitialConfig(WeatherStation stn)
		{
			station = stn;

			if (Settings.Monthly.Enabled)
			{
				SetStartOfMonthlyInsertSQL();
			}

			if (Settings.Dayfile.Enabled)
			{
				SetStartOfDayfileInsertSQL();
			}

			if (Settings.Realtime.Enabled)
			{
				SetStartOfRealtimeInsertSQL();
			}

			SetIntervalDataCreateString();

			SetDailyDataCreateString();

			SetRealtimeCreateString();


			customSecondsTokenParser.OnToken += cumulus.TokenParserOnToken;
			CustomSecondsTimer = new System.Timers.Timer { Interval = Settings.CustomSecs.Interval * 1000 };
			CustomSecondsTimer.Elapsed += CustomSecondsTimerTick;
			CustomSecondsTimer.AutoReset = true;

			customMinutesTokenParser.OnToken += cumulus.TokenParserOnToken;

			customRolloverTokenParser.OnToken += cumulus.TokenParserOnToken;


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
			var Cmds = new ConcurrentQueue<string>();
			Cmds.Enqueue(Cmd);
			await CommandAsync(Cmds, CallingFunction, false);
		}

		internal async Task CommandAsync(List<string> Cmds, string CallingFunction)
		{
			var tempQ = new ConcurrentQueue<string>(Cmds);
			await CommandAsync(tempQ, CallingFunction, false);
		}

		internal async Task CommandAsync(ConcurrentQueue<string> Cmds, string CallingFunction, bool UseFailedList)
		{
			await Task.Run(() =>
			{
				ConcurrentQueue<string> queue = UseFailedList ? ref FailedList : ref Cmds;
				var  cmdStr = "";

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
								if (queue.TryPeek(out cmdStr))
								{
									using MySqlCommand cmd = new MySqlCommand(cmdStr, mySqlConn);

									cumulus.LogDebugMessage($"{CallingFunction}: MySQL executing - {cmdStr}");

									if (transaction != null)
									{
										cmd.Transaction = transaction;
									}

									updated += cmd.ExecuteNonQuery();
									
									cumulus.LogDebugMessage($"{CallingFunction}: MySQL {updated} rows were affected.");

									// Success, pop the value from the queue
									queue.TryDequeue(out cmdStr);
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
						Cumulus.LogMessage($"{CallingFunction}: SQL = {cmdStr}");
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
			var Cmds = new ConcurrentQueue<string>();
			Cmds.Enqueue(Cmd);
			CommandSync(Cmds, CallingFunction, false);
		}

		internal void CommandSync(ConcurrentQueue<string> Cmds, string CallingFunction, bool UseFailedList)
		{
			ConcurrentQueue<string> queue = UseFailedList ? ref FailedList : ref Cmds;
			var cmdStr = "";

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
							if (queue.TryPeek(out cmdStr))
							{
								using (MySqlCommand cmd = new MySqlCommand(cmdStr, mySqlConn))
								{
									cumulus.LogDebugMessage($"{CallingFunction}: MySQL executing - {cmdStr}");

									if (transaction != null)
									{
										cmd.Transaction = transaction;
									}

									updated += cmd.ExecuteNonQuery();

									cumulus.LogDebugMessage($"{CallingFunction}: MySQL {updated} rows were affected.");
								}

								cumulus.MySqlUploadAlarm.Triggered = false;
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
					Cumulus.LogMessage($"{CallingFunction}: SQL = {cmdStr}");
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

		internal void CommandErrorHandler(string CallingFunction, int ErrorCode, ConcurrentQueue<string> Cmds)
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
					Cmds.TryDequeue(out var cmd);
					if (!cmd.StartsWith("DELETE IGNORE FROM"))
					{
						FailedList.Enqueue(cmd);
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
									FailedList.Enqueue(cmds[i]);
							}
						}
						else
						{
							FailedList = (ConcurrentQueue<string>)FailedList.Concat<string>(cmds);
						}
					}
					else
					{
						connectionOK = false;
					}
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
			}
		}

		internal void DoRealtimeData(int cycle, bool live, DateTime? logdate = null)
		{
			DateTime timestamp = (DateTime)(live ? DateTime.Now : logdate);

			if (!Settings.Realtime.Enabled)
				return;

			if (Settings.RealtimeLimit1Minute && RealtimeLastMinute == timestamp.Minute)
				return;

			RealtimeLastMinute = timestamp.Minute;

			StringBuilder values = new StringBuilder(StartOfRealtimeInsertSQL, 1024);
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
				CatchUpList.Enqueue(cmds[0]);
			}
		}

		internal void DoIntervalData(DateTime timestamp, bool live)
		{
			StringBuilder values = new StringBuilder(StartOfMonthlyInsertSQL, 600);
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
				CatchUpList.Enqueue(queryString);
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

		internal void SetRealtimeCreateString()
		{
			CreateRealtimeSQL = "CREATE TABLE " + Settings.Realtime.TableName + " (LogDateTime DATETIME NOT NULL," +
				"temp decimal(4," + cumulus.TempDPlaces + ")," +
				"hum decimal(4," + cumulus.HumDPlaces + ")," +
				"dew decimal(4," + cumulus.TempDPlaces + ")," +
				"wspeed decimal(4," + cumulus.WindDPlaces + ")," +
				"wlatest decimal(4," + cumulus.WindDPlaces + ")," +
				"bearing VARCHAR(3)," +
				"rrate decimal(4," + cumulus.RainDPlaces + ")," +
				"rfall decimal(4," + cumulus.RainDPlaces + ")," +
				"press decimal(6," + cumulus.PressDPlaces + ")," +
				"currentwdir varchar(3)," +
				"beaufortnumber varchar(2)," +
				"windunit varchar(4)," +
				"tempunitnodeg varchar(1)," +
				"pressunit varchar(3)," +
				"rainunit varchar(2)," +
				"windrun decimal(4," + cumulus.WindRunDPlaces + ")," +
				"presstrendval varchar(6)," +
				"rmonth decimal(4," + cumulus.RainDPlaces + ")," +
				"ryear decimal(4," + cumulus.RainDPlaces + ")," +
				"rfallY decimal(4," + cumulus.RainDPlaces + ")," +
				"intemp decimal(4," + cumulus.TempDPlaces + ")," +
				"inhum decimal(4," + cumulus.HumDPlaces + ")," +
				"wchill decimal(4," + cumulus.TempDPlaces + ")," +
				"temptrend varchar(5)," +
				"tempTH decimal(4," + cumulus.TempDPlaces + ")," +
				"TtempTH varchar(5)," +
				"tempTL decimal(4," + cumulus.TempDPlaces + ")," +
				"TtempTL varchar(5)," +
				"windTM decimal(4," + cumulus.WindDPlaces + ")," +
				"TwindTM varchar(5)," +
				"wgustTM decimal(4," + cumulus.WindDPlaces + ")," +
				"TwgustTM varchar(5)," +
				"pressTH decimal(6," + cumulus.PressDPlaces + ")," +
				"TpressTH varchar(5)," +
				"pressTL decimal(6," + cumulus.PressDPlaces + ")," +
				"TpressTL varchar(5)," +
				"version varchar(8)," +
				"build varchar(5)," +
				"wgust decimal(4," + cumulus.WindDPlaces + ")," +
				"heatindex decimal(4," + cumulus.TempDPlaces + ")," +
				"humidex decimal(4," + cumulus.TempDPlaces + ")," +
				"UV decimal(3," + cumulus.UVDPlaces + ")," +
				"ET decimal(4," + cumulus.RainDPlaces + ")," +
				"SolarRad decimal(5,1)," +
				"avgbearing varchar(3)," +
				"rhour decimal(4," + cumulus.RainDPlaces + ")," +
				"forecastnumber varchar(2)," +
				"isdaylight varchar(1)," +
				"SensorContactLost varchar(1," +
				"wdir varchar(3)," +
				"cloudbasevalue varchar(5)," +
				"cloudbaseunit varchar(2," +
				"apptemp decimal(4," + cumulus.TempDPlaces + ")," +
				"SunshineHours decimal(3," + cumulus.SunshineDPlaces + "," +
				"CurrentSolarMax decimal(5,1)," +
				"IsSunny varchar(1)," +
				"FeelsLike decimal(4," + cumulus.TempDPlaces + ")," +
				"PRIMARY KEY (LogDateTime)) COMMENT = \"Realtime log\"";
		}

		internal void SetIntervalDataCreateString()
				{
					StringBuilder strb = new StringBuilder("CREATE TABLE " + Settings.Monthly.TableName + " (", 1500);
					strb.Append("LogDateTime DATETIME NOT NULL,");
					strb.Append("Temp decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("Humidity decimal(4," + cumulus.HumDPlaces + "),");
					strb.Append("Dewpoint decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("Windspeed decimal(4," + cumulus.WindAvgDPlaces + "),");
					strb.Append("Windgust decimal(4," + cumulus.WindDPlaces + "),");
					strb.Append("Windbearing VARCHAR(3),");
					strb.Append("RainRate decimal(4," + cumulus.RainDPlaces + "),");
					strb.Append("TodayRainSoFar decimal(4," + cumulus.RainDPlaces + "),");
					strb.Append("Pressure decimal(6," + cumulus.PressDPlaces + "),");
					strb.Append("Raincounter decimal(6," + cumulus.RainDPlaces + "),");
					strb.Append("InsideTemp decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("InsideHumidity decimal(4," + cumulus.HumDPlaces + "),");
					strb.Append("LatestWindGust decimal(5," + cumulus.WindDPlaces + "),");
					strb.Append("WindChill decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("HeatIndex decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("UVindex decimal(4," + cumulus.UVDPlaces + "),");
					strb.Append("SolarRad decimal(5,1),");
					strb.Append("Evapotrans decimal(4," + cumulus.RainDPlaces + "),");
					strb.Append("AnnualEvapTran decimal(5," + cumulus.RainDPlaces + "),");
					strb.Append("ApparentTemp decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("MaxSolarRad decimal(5,1),");
					strb.Append("HrsSunShine decimal(3," + cumulus.SunshineDPlaces + "),");
					strb.Append("CurrWindBearing varchar(3),");
					strb.Append("RG11rain decimal(4," + cumulus.RainDPlaces + "),");
					strb.Append("RainSinceMidnight decimal(4," + cumulus.RainDPlaces + "),");
					strb.Append("WindbearingSym varchar(3),");
					strb.Append("CurrWindBearingSym varchar(3),");
					strb.Append("FeelsLike decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("Humidex decimal(4," + cumulus.TempDPlaces + "),");
					strb.Append("PRIMARY KEY (LogDateTime)) COMMENT = \"Monthly logs from Cumulus\"");
					CreateMonthlySQL = strb.ToString();
				}

		internal void SetDailyDataCreateString()
		{
			StringBuilder strb = new StringBuilder("CREATE TABLE " + Settings.Dayfile.TableName + " (", 2048);
			strb.Append("LogDate date NOT NULL ,");
			strb.Append("HighWindGust decimal(4," + cumulus.WindDPlaces + "),");
			strb.Append("HWindGBear varchar(3),");
			strb.Append("THWindG varchar(5),");
			strb.Append("MinTemp decimal(5," + cumulus.TempDPlaces + "),");
			strb.Append("TMinTemp varchar(5),");
			strb.Append("MaxTemp decimal(5," + cumulus.TempDPlaces + "),");
			strb.Append("TMaxTemp varchar(5),");
			strb.Append("MinPress decimal(6," + cumulus.PressDPlaces + "),");
			strb.Append("TMinPress varchar(5),");
			strb.Append("MaxPress decimal(6," + cumulus.PressDPlaces + "),");
			strb.Append("TMaxPress varchar(5),");
			strb.Append("MaxRainRate decimal(4," + cumulus.RainDPlaces + "),");
			strb.Append("TMaxRR varchar(5),");
			strb.Append("TotRainFall decimal(6," + cumulus.RainDPlaces + "),");
			strb.Append("AvgTemp decimal(4," + cumulus.TempDPlaces + ") ,");
			strb.Append("TotWindRun decimal(5," + cumulus.WindRunDPlaces + "),");
			strb.Append("HighAvgWSpeed decimal(3," + cumulus.WindAvgDPlaces + "),");
			strb.Append("THAvgWSpeed varchar(5),");
			strb.Append("LowHum decimal(4," + cumulus.HumDPlaces + "),");
			strb.Append("TLowHum varchar(5),");
			strb.Append("HighHum decimal(4," + cumulus.HumDPlaces + "),");
			strb.Append("THighHum varchar(5),");
			strb.Append("TotalEvap decimal(5," + cumulus.RainDPlaces + "),");
			strb.Append("HoursSun decimal(3," + cumulus.SunshineDPlaces + "),");
			strb.Append("HighHeatInd decimal(4," + cumulus.TempDPlaces + "),");
			strb.Append("THighHeatInd varchar(5),");
			strb.Append("HighAppTemp decimal(4," + cumulus.TempDPlaces + "),");
			strb.Append("THighAppTemp varchar(5),");
			strb.Append("LowAppTemp decimal(4," + cumulus.TempDPlaces + "),");
			strb.Append("TLowAppTemp varchar(5),");
			strb.Append("HighHourRain decimal(4," + cumulus.RainDPlaces + "),");
			strb.Append("THighHourRain varchar(5),");
			strb.Append("LowWindChill decimal(4," + cumulus.TempDPlaces + "),");
			strb.Append("TLowWindChill varchar(5),");
			strb.Append("HighDewPoint decimal(4," + cumulus.TempDPlaces + "),");
			strb.Append("THighDewPoint varchar(5),");
			strb.Append("LowDewPoint decimal(4," + cumulus.TempDPlaces + "),");
			strb.Append("TLowDewPoint varchar(5),");
			strb.Append("DomWindDir varchar(3),");
			strb.Append("HeatDegDays decimal(4,1),");
			strb.Append("CoolDegDays decimal(4,1),");
			strb.Append("HighSolarRad decimal(5,1),");
			strb.Append("THighSolarRad varchar(5),");
			strb.Append("HighUV decimal(3," + cumulus.UVDPlaces + "),");
			strb.Append("THighUV varchar(5),");
			strb.Append("HWindGBearSym varchar(3),");
			strb.Append("DomWindDirSym varchar(3),");
			strb.Append("MaxFeelsLike decimal(5," + cumulus.TempDPlaces + "),");
			strb.Append("TMaxFeelsLike varchar(5),");
			strb.Append("MinFeelsLike decimal(5," + cumulus.TempDPlaces + "),");
			strb.Append("TMinFeelsLike varchar(5),");
			strb.Append("MaxHumidex decimal(5," + cumulus.TempDPlaces + "),");
			strb.Append("TMaxHumidex varchar(5),");
			strb.Append("ChillHours decimal(7," + cumulus.TempDPlaces + "),");
			strb.Append("HighRain24h decimal(6," + cumulus.RainDPlaces + ")");
			strb.Append("THighRain24h varchar(5),");
			strb.Append("PRIMARY KEY(LogDate)) COMMENT = \"Dayfile from Cumulus\"");
			CreateDayfileSQL = strb.ToString();
		}

		internal void SetStartOfRealtimeInsertSQL()
		{
			StartOfRealtimeInsertSQL = "INSERT IGNORE INTO " + Settings.Realtime.TableName + " (" +
				"LogDateTime,temp,hum,dew,wspeed,wlatest,bearing,rrate,rfall,press," +
				"currentwdir,beaufortnumber,windunit,tempunitnodeg,pressunit,rainunit," +
				"windrun,presstrendval,rmonth,ryear,rfallY,intemp,inhum,wchill,temptrend," +
				"tempTH,TtempTH,tempTL,TtempTL,windTM,TwindTM,wgustTM,TwgustTM," +
				"pressTH,TpressTH,pressTL,TpressTL,version,build,wgust,heatindex,humidex," +
				"UV,ET,SolarRad,avgbearing,rhour,forecastnumber,isdaylight,SensorContactLost," +
				"wdir,cloudbasevalue,cloudbaseunit,apptemp,SunshineHours,CurrentSolarMax,IsSunny," +
				"FeelsLike)";
		}

		internal void SetStartOfDayfileInsertSQL()
		{
			StartOfDayfileInsertSQL = "INSERT IGNORE INTO " + Settings.Dayfile.TableName + " (" +
				"LogDate,HighWindGust,HWindGBear,THWindG,MinTemp,TMinTemp,MaxTemp,TMaxTemp," +
				"MinPress,TMinPress,MaxPress,TMaxPress,MaxRainRate,TMaxRR,TotRainFall,AvgTemp," +
				"TotWindRun,HighAvgWSpeed,THAvgWSpeed,LowHum,TLowHum,HighHum,THighHum,TotalEvap," +
				"HoursSun,HighHeatInd,THighHeatInd,HighAppTemp,THighAppTemp,LowAppTemp,TLowAppTemp," +
				"HighHourRain,THighHourRain,LowWindChill,TLowWindChill,HighDewPoint,THighDewPoint," +
				"LowDewPoint,TLowDewPoint,DomWindDir,HeatDegDays,CoolDegDays,HighSolarRad," +
				"THighSolarRad,HighUV,THighUV,HWindGBearSym,DomWindDirSym," +
				"MaxFeelsLike,TMaxFeelsLike,MinFeelsLike,TMinFeelsLike,MaxHumidex,TMaxHumidex," +
				"HighRain24h, THighRain24h)";
		}

		internal void SetStartOfMonthlyInsertSQL()
		{
			StartOfMonthlyInsertSQL = "INSERT IGNORE INTO " + Settings.Monthly.TableName + " (" +
				"LogDateTime,Temp,Humidity,Dewpoint,Windspeed,Windgust,Windbearing,RainRate,TodayRainSoFar," +
				"Pressure,Raincounter,InsideTemp,InsideHumidity,LatestWindGust,WindChill,HeatIndex,UVindex," +
				"SolarRad,Evapotrans,AnnualEvapTran,ApparentTemp,MaxSolarRad,HrsSunShine,CurrWindBearing," +
				"RG11rain,RainSinceMidnight,WindbearingSym,CurrWindBearingSym,FeelsLike,Humidex)";
		}


		internal void SetupRealtimeMySqlTable()
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

		internal void SetupMonthlyMySqlTable()
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

		internal void SetupDayfileMySqlTable()
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
