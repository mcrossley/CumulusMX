using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

using EmbedIO;

using MySql.Data.MySqlClient;

using ServiceStack;

namespace CumulusMX
{
	public class MysqlSettings
	{
		private readonly Cumulus cumulus;

		private static string hidden = "*****";

		public MysqlSettings(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		public string GetAlpacaFormData()
		{
			var server = new ServerJson()
			{
				database = cumulus.MySqlFunction.ConnSettings.Database,
				host = cumulus.MySqlFunction.ConnSettings.Server,
				pass = cumulus.ProgramOptions.DisplayPasswords ? cumulus.MySqlFunction.ConnSettings.Password : hidden,
				port = cumulus.MySqlFunction.ConnSettings.Port,
				user = cumulus.MySqlFunction.ConnSettings.UserID
			};

			var monthly = new MonthlyJson()
			{
				enabled = cumulus.MySqlFunction.Settings.Monthly.Enabled,
				table = cumulus.MySqlFunction.Settings.Monthly.TableName
			};

			var reten = cumulus.MySqlFunction.Settings.RealtimeRetention.Split(' ');
			var retenVal = string.IsNullOrEmpty(reten[0]) ? 7 : int.Parse(reten[0]);
			var retenUnit = reten.Length > 1 && !string.IsNullOrEmpty(reten[1]) ? reten[1].ToUpper().TrimEnd('S') : "DAY";

			var realtime = new RealtimeJson()
			{
				enabled = cumulus.MySqlFunction.Settings.Realtime.Enabled,
				retentionVal = retenVal,
				retentionUnit = retenUnit,
				table = cumulus.MySqlFunction.Settings.Realtime.TableName,
				limit1min = cumulus.MySqlFunction.Settings.RealtimeLimit1Minute && cumulus.RealtimeInterval < 60000  // do not enable if real time interval is greater than 1 minute
			};

			var dayfile = new DayfileJson()
			{
				enabled = cumulus.MySqlFunction.Settings.Dayfile.Enabled,
				table = cumulus.MySqlFunction.Settings.Dayfile.TableName
			};

			var customseconds = new CustomSecondsJson()
			{
				enabled = cumulus.MySqlFunction.Settings.CustomSecs.Enabled,
				interval = cumulus.MySqlFunction.Settings.CustomSecs.Interval
			};

			var cmdCnt = 1;
			for (var i = 1; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomSecs.Commands[i]))
					cmdCnt++;
			}
			customseconds.command = new string[cmdCnt];

			var index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomSecs.Commands[i]))
					customseconds.command[index++] = cumulus.MySqlFunction.Settings.CustomSecs.Commands[i];
			}


			var customminutes = new CustomMinutesJson()
			{
				enabled = cumulus.MySqlFunction.Settings.CustomMins.Enabled,
				intervalindex = cumulus.MySqlFunction.CustomMinutesIntervalIndex
			};

			cmdCnt = 1;
			for (var i = 1; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomMins.Commands[i]))
					cmdCnt++;
			}
			customminutes.command = new string[cmdCnt];

			index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomMins.Commands[i]))
					customminutes.command[index++] = cumulus.MySqlFunction.Settings.CustomMins.Commands[i];
			}

			var customrollover = new CustomRolloverStartJson()
			{
				enabled = cumulus.MySqlFunction.Settings.CustomRollover.Enabled,
			};

			cmdCnt = 1;
			for (var i = 1; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomRollover.Commands[i]))
					cmdCnt++;
			}
			customrollover.command = new string[cmdCnt];

			index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomRollover.Commands[i]))
					customrollover.command[index++] = cumulus.MySqlFunction.Settings.CustomRollover.Commands[i];
			}

			var customtimed = new SettingsCustomTimedJson()
			{
				enabled = cumulus.MySqlFunction.Settings.CustomTimed.Enabled,
				entries = Array.Empty<CustomTimedJson>()
			};

			cmdCnt = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomTimed.Commands[i]))
					cmdCnt++;
			}
			if (cmdCnt > 0)
			{
				customtimed.entries = new CustomTimedJson[cmdCnt];

				index = 0;
				for (var i = 0; i < 10; i++)
				{
					customtimed.entries[index] = new CustomTimedJson();

					if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomTimed.Commands[i]))
					{
						customtimed.entries[index].command = cumulus.MySqlFunction.Settings.CustomTimed.Commands[i];
						customtimed.entries[index].starttime = cumulus.MySqlFunction.Settings.CustomTimed.StartTimes[i];
						customtimed.entries[index].interval = cumulus.MySqlFunction.Settings.CustomTimed.Intervals[i];
						customtimed.entries[index].repeat = customtimed.entries[index].interval != 1440;
						index++;

						if (index == cmdCnt)
							break;
					}
				}
			}

			var customstartup = new CustomRolloverStartJson()
			{
				enabled = cumulus.MySqlFunction.Settings.CustomStartUp.Enabled
			};

			cmdCnt = 1;
			for (var i = 1; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomStartUp.Commands[i]))
					cmdCnt++;
			}
			customstartup.command = new string[cmdCnt];

			index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlFunction.Settings.CustomStartUp.Commands[i]))
					customstartup.command[index++] = cumulus.MySqlFunction.Settings.CustomStartUp.Commands[i];
			}

			var options = new OptionsJson()
			{
				updateonedit = cumulus.MySqlFunction.Settings.UpdateOnEdit,
				bufferonerror = cumulus.MySqlFunction.Settings.BufferOnfailure,
			};

			var data = new SettingsJson()
			{
				accessible = cumulus.ProgramOptions.EnableAccessibility,
				server = server,
				options = options,
				monthly = monthly,
				realtime = realtime,
				dayfile = dayfile,
				customseconds = customseconds,
				customminutes = customminutes,
				customrollover = customrollover,
				customtimed = customtimed,
				customstart = customstartup
			};

			return data.ToJson();
		}

		//public object UpdateMysqlConfig(HttpListenerContext context)
		public string UpdateConfig(IHttpContext context)
		{
			string json = "";
			SettingsJson settings;
			try
			{
				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				json = WebUtility.UrlDecode(data[5..]);

				// de-serialize it to the settings structure
				settings = json.FromJson<SettingsJson>();
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing MySQL Settings JSON";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("MySQL Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}


			// process the settings
			try
			{
				cumulus.LogMessage("Updating MySQL settings");

				// server
				cumulus.MySqlFunction.ConnSettings.Server = String.IsNullOrWhiteSpace(settings.server.host) ? null : settings.server.host.Trim();
				if (settings.server.port > 0 && settings.server.port < 65536)
				{
					cumulus.MySqlFunction.ConnSettings.Port = settings.server.port;
				}
				else
				{
					cumulus.MySqlFunction.ConnSettings.Port = 3306;
				}
				cumulus.MySqlFunction.ConnSettings.Database = String.IsNullOrWhiteSpace(settings.server.database) ? null : settings.server.database.Trim();
				cumulus.MySqlFunction.ConnSettings.UserID = String.IsNullOrWhiteSpace(settings.server.user) ? null : settings.server.user.Trim();
				if (settings.server.pass != hidden)
					cumulus.MySqlFunction.ConnSettings.Password = String.IsNullOrWhiteSpace(settings.server.pass) ? null : settings.server.pass.Trim();

				// options
				cumulus.MySqlFunction.Settings.UpdateOnEdit = settings.options.updateonedit;
				cumulus.MySqlFunction.Settings.BufferOnfailure = settings.options.bufferonerror;

				//monthly
				cumulus.MySqlFunction.Settings.Monthly.Enabled = settings.monthly.enabled;
				if (cumulus.MySqlFunction.Settings.Monthly.Enabled)
				{
					cumulus.MySqlFunction.Settings.Monthly.TableName = String.IsNullOrWhiteSpace(settings.monthly.table) ? "Monthly" : settings.monthly.table.Trim();
					if (cumulus.MySqlFunction.MonthlyTable.Name != cumulus.MySqlFunction.Settings.Monthly.TableName)
					{
						cumulus.MySqlFunction.MonthlyTable.Name = cumulus.MySqlFunction.Settings.Monthly.TableName;
						cumulus.MySqlFunction.MonthlyTable.Rebuild();
					}
				}
				//realtime
				cumulus.MySqlFunction.Settings.Realtime.Enabled = settings.realtime.enabled;
				if (cumulus.MySqlFunction.Settings.Realtime.Enabled)
				{
					cumulus.MySqlFunction.Settings.RealtimeRetention = settings.realtime.retentionVal + " " + settings.realtime.retentionUnit.Trim();
					cumulus.MySqlFunction.Settings.RealtimeLimit1Minute = settings.realtime.limit1min;
					cumulus.MySqlFunction.Settings.Realtime.TableName = String.IsNullOrWhiteSpace(settings.realtime.table) ? "Realtime" : settings.realtime.table.Trim();
					if (cumulus.MySqlFunction.RealtimeTable.Name != cumulus.MySqlFunction.Settings.Realtime.TableName)
					{
						cumulus.MySqlFunction.RealtimeTable.Name = cumulus.MySqlFunction.Settings.Realtime.TableName;
						cumulus.MySqlFunction.RealtimeTable.Rebuild();
					}
				}
				//dayfile
				cumulus.MySqlFunction.Settings.Dayfile.Enabled = settings.dayfile.enabled;
				if (cumulus.MySqlFunction.Settings.Dayfile.Enabled)
				{
					cumulus.MySqlFunction.Settings.Dayfile.TableName = String.IsNullOrWhiteSpace(settings.dayfile.table) ? "Dayfile" : settings.dayfile.table.Trim();
					if (cumulus.MySqlFunction.DayfileTable.Name != cumulus.MySqlFunction.Settings.Dayfile.TableName)
					{
						cumulus.MySqlFunction.DayfileTable.Name = cumulus.MySqlFunction.Settings.Dayfile.TableName;
						cumulus.MySqlFunction.DayfileTable.Rebuild();
					}
				}
				// custom seconds
				cumulus.MySqlFunction.Settings.CustomSecs.Enabled = settings.customseconds.enabled;
				if (cumulus.MySqlFunction.Settings.CustomSecs.Enabled)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customseconds.command.Length)
							cumulus.MySqlFunction.Settings.CustomSecs.Commands[i] = String.IsNullOrWhiteSpace(settings.customseconds.command[i]) ? null : settings.customseconds.command[i].Trim();
						else
							cumulus.MySqlFunction.Settings.CustomSecs.Commands[i] = null;
					}

					cumulus.MySqlFunction.Settings.CustomSecs.Interval = settings.customseconds.interval;
				}
				// custom minutes
				cumulus.MySqlFunction.Settings.CustomMins.Enabled = settings.customminutes.enabled;
				if (cumulus.MySqlFunction.Settings.CustomMins.Enabled)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customminutes.command.Length)
							cumulus.MySqlFunction.Settings.CustomMins.Commands[i] = String.IsNullOrWhiteSpace(settings.customminutes.command[i]) ? null : settings.customminutes.command[i].Trim();
						else
							cumulus.MySqlFunction.Settings.CustomMins.Commands[i] = null;
					}

					cumulus.MySqlFunction.CustomMinutesIntervalIndex = settings.customminutes.intervalindex;
					if (cumulus.MySqlFunction.CustomMinutesIntervalIndex >= 0 && cumulus.MySqlFunction.CustomMinutesIntervalIndex < cumulus.FactorsOf60.Length)
					{
						cumulus.MySqlFunction.Settings.CustomMins.Interval = cumulus.FactorsOf60[cumulus.MySqlFunction.CustomMinutesIntervalIndex];
					}
					else
					{
						cumulus.MySqlFunction.Settings.CustomMins.Interval = 10;
					}
				}
				// custom roll-over
				cumulus.MySqlFunction.Settings.CustomRollover.Enabled = settings.customrollover.enabled;
				if (cumulus.MySqlFunction.Settings.CustomRollover.Enabled)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customrollover.command.Length)
							cumulus.MySqlFunction.Settings.CustomRollover.Commands[i] = String.IsNullOrWhiteSpace(settings.customrollover.command[i]) ? null : settings.customrollover.command[i].Trim();
						else
							cumulus.MySqlFunction.Settings.CustomRollover.Commands[i] = null;
					}
				}
				// custom timed
				cumulus.MySqlFunction.Settings.CustomTimed.Enabled = settings.customtimed.enabled;
				if (cumulus.MySqlFunction.Settings.CustomTimed.Enabled && null != settings.customtimed.entries)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customtimed.entries.Length)
						{
							cumulus.MySqlFunction.Settings.CustomTimed.Commands[i] = String.IsNullOrWhiteSpace(settings.customtimed.entries[i].command) ? null : settings.customtimed.entries[i].command.Trim();
							cumulus.MySqlFunction.Settings.CustomTimed.StartTimes[i] = settings.customtimed.entries[i].starttime;
							cumulus.MySqlFunction.Settings.CustomTimed.Intervals[i] = settings.customtimed.entries[i].interval;

						}
						else
						{
							cumulus.MySqlFunction.Settings.CustomTimed.Commands[i] = null;
							cumulus.MySqlFunction.Settings.CustomTimed.StartTimes[i] = TimeSpan.Zero;
							cumulus.MySqlFunction.Settings.CustomTimed.Intervals[i] = 0;

						}
					}
				}
				// custom start-up
				cumulus.MySqlFunction.Settings.CustomStartUp.Enabled = settings.customstart.enabled;
				if (cumulus.MySqlFunction.Settings.CustomStartUp.Enabled)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customstart.command.Length)
							cumulus.MySqlFunction.Settings.CustomStartUp.Commands[i] = String.IsNullOrWhiteSpace(settings.customstart.command[i]) ? null : settings.customstart.command[i].Trim();
						else
							cumulus.MySqlFunction.Settings.CustomStartUp.Commands[i] = null;
					}
				}

				// Save the settings
				cumulus.WriteIniFile();

				cumulus.MySqlFunction.CustomSecondsTimer.Interval = cumulus.MySqlFunction.Settings.CustomSecs.Interval * 1000;
				cumulus.MySqlFunction.CustomSecondsTimer.Enabled = cumulus.MySqlFunction.Settings.CustomSecs.Enabled;

				context.Response.StatusCode = 200;
			}
			catch (Exception ex)
			{
				var msg = "Error processing settings";
				cumulus.LogExceptionMessage(ex, msg);
				context.Response.StatusCode = 500;
				return msg;
			}
			return "success";
		}

		private string UpdateMySQLTable(MySqlTable table)
		{
			string res;
			int cnt = 0;

			try
			{
				using var mySqlConn = new MySqlConnection(cumulus.MySqlFunction.ConnSettings.ToString());
				mySqlConn.Open();

				// first get a list of the columns the table currenty has
				var currCols = new List<string>();
				using (MySqlCommand cmd = new MySqlCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{table.Name}' AND TABLE_SCHEMA='{cumulus.MySqlFunction.ConnSettings.Database}'", mySqlConn))
				using (MySqlDataReader reader = cmd.ExecuteReader())
				{
					if (reader.HasRows)
					{
						while (reader.Read())
						{
							var col = reader.GetString(0);
							currCols.Add(col);
						}
					}
				}

				var update = new StringBuilder("ALTER TABLE " + table.Name, 1024);
				foreach (var newCol in table.Columns)
				{
					if (!currCols.Contains(newCol.Name))
					{
						update.Append($" ADD COLUMN {newCol.Name} {newCol.Attributes},");
						cnt++;
					}
				}

				if (cnt > 0)
				{
					// strip trailing comma
					update.Length--;

					using (MySqlCommand cmd = new MySqlCommand(update.ToString(), mySqlConn))
					{
						int aff = cmd.ExecuteNonQuery();
						res = $"Added {cnt} columns to {table.Name} table";
						cumulus.LogMessage($"MySQL Update Table: " + res);
					}
				}
				else
				{
					res = $"The {table.Name} table already has all the required columns. Required = {table.Columns.Count}, actual = {currCols.Count}";
					cumulus.LogMessage("MySQL Update Table: " + res);
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "MySQL Update Table: Error encountered during MySQL operation.");
				res = "Error: " + ex.Message;
			}

			return res;
		}

		private string CreateMySQLTable(string createSQL)
		{
			string res;
			using (var mySqlConn = new MySqlConnection(cumulus.MySqlFunction.ConnSettings.ToString()))
			using (MySqlCommand cmd = new MySqlCommand(createSQL, mySqlConn))
			{
				cumulus.LogMessage($"MySQL Create Table: {createSQL}");

				try
				{
					mySqlConn.Open();
					int aff = cmd.ExecuteNonQuery();
					cumulus.LogMessage($"MySQL Create Table: {aff} items were affected.");
					res = "Database table created successfully";
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "MySQL Create Table: Error encountered during MySQL operation.");
					res = "Error: " + ex.Message;
				}
				finally
				{
					try
					{
						mySqlConn.Close();
					}
					catch
					{ }
				}
			}
			return res;
		}

		//public string CreateMonthlySQL(HttpListenerContext context)
		public string CreateMonthlySQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlFunction.MonthlyTable.CreateCommand) + "\"}";
		}

		//public string CreateDayfileSQL(HttpListenerContext context)
		public string CreateDayfileSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlFunction.DayfileTable.CreateCommand) + "\"}";
		}

		//public string CreateRealtimeSQL(HttpListenerContext context)
		public string CreateRealtimeSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlFunction.RealtimeTable.CreateCommand) + "\"}";
		}

		public string UpdateMonthlySQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlFunction.MonthlyTable) + "\"}";
		}

		public string UpdateDayfileSQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlFunction.DayfileTable) + "\"}";
		}

		public string UpdateRealtimeSQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlFunction.RealtimeTable) + "\"}";
		}

		private class SettingsJson
		{
			public bool accessible { get; set; }
			public ServerJson server { get; set; }
			public OptionsJson options { get; set; }
			public MonthlyJson monthly { get; set; }
			public RealtimeJson realtime { get; set; }
			public DayfileJson dayfile { get; set; }
			public CustomSecondsJson customseconds { get; set; }
			public CustomMinutesJson customminutes { get; set; }
			public CustomRolloverStartJson customrollover { get; set; }
			public SettingsCustomTimedJson customtimed { get; set; }
			public CustomRolloverStartJson customstart { get; set; }
		}

		private class ServerJson
		{
			public string host { get; set; }
			public uint port { get; set; }
			public string user { get; set; }
			public string pass { get; set; }
			public string database { get; set; }
		}

		private class OptionsJson
		{
			public bool updateonedit { get; set; }
			public bool bufferonerror { get; set; }
		}

		private class MonthlyJson
		{
			public bool enabled { get; set; }
			public string table { get; set; }
		}

		private class RealtimeJson
		{
			public bool enabled { get; set; }
			public string table { get; set; }
			public int retentionVal { get; set; }
			public string retentionUnit { get; set; }
			public bool limit1min { get; set; }
		}

		private class DayfileJson
		{
			public bool enabled { get; set; }
			public string table { get; set; }
		}

		private class CustomSecondsJson
		{
			public bool enabled { get; set; }
			public string[] command { get; set; }
			public int interval { get; set; }
		}

		private class CustomMinutesJson
		{
			public bool enabled { get; set; }
			public string[] command { get; set; }
			public int intervalindex { get; set; }
		}

		private class CustomRolloverStartJson
		{
			public bool enabled { get; set; }
			public string[] command { get; set; }
		}

		private class SettingsCustomTimedJson
		{
			public bool enabled { get; set; }
			public CustomTimedJson[] entries { get; set; }
		}

		private class CustomTimedJson
		{
			public string command { get; set; }
			public int interval { get; set; }
			[IgnoreDataMember]
			public TimeSpan starttime { get; set; }

			[DataMember(Name = "starttimestr")]
			public string starttimestring
			{
				get => starttime.ToString("hh\\:mm", CultureInfo.InvariantCulture);
				set => starttime = TimeSpan.ParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture);
			}
			public bool repeat { get; set; }
		}
	}
}
