using System;
using System.IO;
using System.Net;
using System.Text;
using MySql.Data.MySqlClient;
using ServiceStack;
using EmbedIO;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml.Linq;

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
				database = cumulus.MySqlSettings.ConnSettings.Database,
				host = cumulus.MySqlSettings.ConnSettings.Server,
				pass = cumulus.ProgramOptions.DisplayPasswords ? cumulus.MySqlSettings.ConnSettings.Password : hidden,
				port = cumulus.MySqlSettings.ConnSettings.Port,
				user = cumulus.MySqlSettings.ConnSettings.UserID
			};

			var monthly = new MonthlyJson()
			{
				enabled = cumulus.MySqlSettings.Settings.Monthly.Enabled,
				table = cumulus.MySqlSettings.Settings.Monthly.TableName
			};

			var reten = cumulus.MySqlSettings.Settings.RealtimeRetention.Split(' ');
			var retenVal = string.IsNullOrEmpty(reten[0]) ? 7 : int.Parse(reten[0]);
			var retenUnit = reten.Length > 1 && !string.IsNullOrEmpty(reten[1]) ? reten[1].ToUpper().TrimEnd('S') : "DAY";

			var realtime = new RealtimeJson()
			{
				enabled = cumulus.MySqlSettings.Settings.Realtime.Enabled,
				retentionVal = retenVal,
				retentionUnit = retenUnit,
				table = cumulus.MySqlSettings.Settings.Realtime.TableName,
				limit1min = cumulus.MySqlSettings.Settings.RealtimeLimit1Minute && cumulus.RealtimeInterval < 60000  // do not enable if real time interval is greater than 1 minute
			};

			var dayfile = new DayfileJson()
			{
				enabled = cumulus.MySqlSettings.Settings.Dayfile.Enabled,
				table = cumulus.MySqlSettings.Settings.Dayfile.TableName
			};

			var customseconds = new CustomSecondsJson()
			{
				enabled = cumulus.MySqlSettings.Settings.CustomSecs.Enabled,
				interval = cumulus.MySqlSettings.Settings.CustomSecs.Interval
			};

			var cmdCnt = 1;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomSecs.Commands[i]))
					cmdCnt++;
			}
			customseconds.command = new string[cmdCnt];

			var index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomSecs.Commands[i]))
					customseconds.command[index++] = cumulus.MySqlSettings.Settings.CustomSecs.Commands[i];
			}


			var customminutes = new CustomMinutesJson()
			{
				enabled = cumulus.MySqlSettings.Settings.CustomMins.Enabled,
				intervalindex = cumulus.MySqlSettings.CustomMinutesIntervalIndex
			};

			cmdCnt = 1;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomMins.Commands[i]))
					cmdCnt++;
			}
			customminutes.command = new string[cmdCnt];

			index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomMins.Commands[i]))
					customminutes.command[index++] = cumulus.MySqlSettings.Settings.CustomMins.Commands[i];
			}

			var customrollover = new CustomRolloverJson()
			{
				enabled = cumulus.MySqlSettings.Settings.CustomRollover.Enabled,
			};

			cmdCnt = 1;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomRollover.Commands[i]))
					cmdCnt++;
			}
			customrollover.command = new string[cmdCnt];

			index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomRollover.Commands[i]))
					customrollover.command[index++] = cumulus.MySqlSettings.Settings.CustomRollover.Commands[i];
			}

			var customtimed = new SettingsCustomTimedJson()
			{
				enabled = cumulus.MySqlSettings.Settings.CustomTimed.Enabled,
				entries = Array.Empty<CustomTimedJson>()
			};

			cmdCnt = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomTimed.Commands[i]))
					cmdCnt++;
			}
			if (cmdCnt > 0)
			{
				customtimed.entries = new CustomTimedJson[cmdCnt];

				index = 0;
				for (var i = 0; i < 10; i++)
				{
					customtimed.entries[index] = new CustomTimedJson();

					if (!string.IsNullOrEmpty(cumulus.MySqlSettings.Settings.CustomTimed.Commands[i]))
					{
						customtimed.entries[index].command = cumulus.MySqlSettings.Settings.CustomTimed.Commands[i];
						customtimed.entries[index].starttime = cumulus.MySqlSettings.Settings.CustomTimed.StartTimes[i];
						customtimed.entries[index].interval = cumulus.MySqlSettings.Settings.CustomTimed.Intervals[i];
						customtimed.entries[index].repeat = customtimed.entries[index].interval != 1440;
						index++;

						if (index == cmdCnt)
							break;
					}
				}
			}

			var options = new OptionsJson()
			{
				updateonedit = cumulus.MySqlSettings.Settings.UpdateOnEdit,
				bufferonerror = cumulus.MySqlSettings.Settings.BufferOnfailure,
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
				customtimed = customtimed
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
				Cumulus.LogMessage("Updating MySQL settings");

				// server
				cumulus.MySqlSettings.ConnSettings.Server = String.IsNullOrWhiteSpace(settings.server.host) ? null : settings.server.host.Trim();
				if (settings.server.port > 0 && settings.server.port < 65536)
				{
					cumulus.MySqlSettings.ConnSettings.Port = settings.server.port;
				}
				else
				{
					cumulus.MySqlSettings.ConnSettings.Port = 3306;
				}
				cumulus.MySqlSettings.ConnSettings.Database = String.IsNullOrWhiteSpace(settings.server.database) ? null : settings.server.database.Trim();
				cumulus.MySqlSettings.ConnSettings.UserID = String.IsNullOrWhiteSpace(settings.server.user) ? null : settings.server.user.Trim();
				if (settings.server.pass != hidden)
					cumulus.MySqlSettings.ConnSettings.Password = String.IsNullOrWhiteSpace(settings.server.pass) ? null : settings.server.pass.Trim();

				// options
				cumulus.MySqlSettings.Settings.UpdateOnEdit = settings.options.updateonedit;
				cumulus.MySqlSettings.Settings.BufferOnfailure = settings.options.bufferonerror;

				//monthly
				cumulus.MySqlSettings.Settings.Monthly.Enabled = settings.monthly.enabled;
				if (cumulus.MySqlSettings.Settings.Monthly.Enabled)
				{
					cumulus.MySqlSettings.Settings.Monthly.TableName = String.IsNullOrWhiteSpace(settings.monthly.table) ? "Monthly" : settings.monthly.table.Trim();
					if (cumulus.MySqlSettings.MonthlyTable.Name != cumulus.MySqlSettings.Settings.Monthly.TableName)
					{
						cumulus.MySqlSettings.MonthlyTable.Name = cumulus.MySqlSettings.Settings.Monthly.TableName;
						cumulus.MySqlSettings.MonthlyTable.Rebuild();
					}
				}
				//realtime
				cumulus.MySqlSettings.Settings.Realtime.Enabled = settings.realtime.enabled;
				if (cumulus.MySqlSettings.Settings.Realtime.Enabled)
				{
					cumulus.MySqlSettings.Settings.RealtimeRetention = settings.realtime.retentionVal + " " + settings.realtime.retentionUnit.Trim();
					cumulus.MySqlSettings.Settings.RealtimeLimit1Minute = settings.realtime.limit1min;
					cumulus.MySqlSettings.Settings.Realtime.TableName = String.IsNullOrWhiteSpace(settings.realtime.table) ? "Realtime" : settings.realtime.table.Trim();
					if (cumulus.MySqlSettings.RealtimeTable.Name != cumulus.MySqlSettings.Settings.Realtime.TableName)
					{
						cumulus.MySqlSettings.RealtimeTable.Name = cumulus.MySqlSettings.Settings.Realtime.TableName;
						cumulus.MySqlSettings.RealtimeTable.Rebuild();
					}
				}
				//dayfile
				cumulus.MySqlSettings.Settings.Dayfile.Enabled = settings.dayfile.enabled;
				if (cumulus.MySqlSettings.Settings.Dayfile.Enabled)
				{
					cumulus.MySqlSettings.Settings.Dayfile.TableName = String.IsNullOrWhiteSpace(settings.dayfile.table) ? "Dayfile" : settings.dayfile.table.Trim();
					if (cumulus.MySqlSettings.DayfileTable.Name != cumulus.MySqlSettings.Settings.Dayfile.TableName)
					{
						cumulus.MySqlSettings.DayfileTable.Name = cumulus.MySqlSettings.Settings.Dayfile.TableName;
						cumulus.MySqlSettings.DayfileTable.Rebuild();
					}
				}
				// custom seconds
				cumulus.MySqlSettings.Settings.CustomSecs.Enabled = settings.customseconds.enabled;
				if (cumulus.MySqlSettings.Settings.CustomSecs.Enabled)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customseconds.command.Length)
							cumulus.MySqlSettings.Settings.CustomSecs.Commands[i] = String.IsNullOrWhiteSpace(settings.customseconds.command[i]) ? null : settings.customseconds.command[i].Trim();
						else
							cumulus.MySqlSettings.Settings.CustomSecs.Commands[i] = null;
					}
					cumulus.MySqlSettings.Settings.CustomSecs.Interval = settings.customseconds.interval;
				}
				// custom minutes
				cumulus.MySqlSettings.Settings.CustomMins.Enabled = settings.customminutes.enabled;
				if (cumulus.MySqlSettings.Settings.CustomMins.Enabled)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customminutes.command.Length)
							cumulus.MySqlSettings.Settings.CustomMins.Commands[i] = String.IsNullOrWhiteSpace(settings.customminutes.command[i]) ? null : settings.customminutes.command[i].Trim();
						else
							cumulus.MySqlSettings.Settings.CustomMins.Commands[i] = null;
					}
					cumulus.MySqlSettings.CustomMinutesIntervalIndex = settings.customminutes.intervalindex;
					if (cumulus.MySqlSettings.CustomMinutesIntervalIndex >= 0 && cumulus.MySqlSettings.CustomMinutesIntervalIndex < cumulus.FactorsOf60.Length)
					{
						cumulus.MySqlSettings.Settings.CustomMins.Interval = cumulus.FactorsOf60[cumulus.MySqlSettings.CustomMinutesIntervalIndex];
					}
					else
					{
						cumulus.MySqlSettings.Settings.CustomMins.Interval = 10;
					}
				}
				// custom roll-over
				cumulus.MySqlSettings.Settings.CustomRollover.Enabled = settings.customrollover.enabled;
				if (cumulus.MySqlSettings.Settings.CustomRollover.Enabled)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customrollover.command.Length)
							cumulus.MySqlSettings.Settings.CustomRollover.Commands[i] = String.IsNullOrWhiteSpace(settings.customrollover.command[i]) ? null : settings.customrollover.command[i].Trim();
						else
							cumulus.MySqlSettings.Settings.CustomRollover.Commands[i] = null;
					}
				}
				// custom timed
				cumulus.MySqlSettings.Settings.CustomTimed.Enabled = settings.customtimed.enabled;
				if (cumulus.MySqlSettings.Settings.CustomTimed.Enabled && null != settings.customtimed.entries)
				{
					for (var i = 0; i < 10; i++)
					{
						if (i < settings.customtimed.entries.Length)
						{
							cumulus.MySqlSettings.Settings.CustomTimed.Commands[i] = String.IsNullOrWhiteSpace(settings.customtimed.entries[i].command) ? null : settings.customtimed.entries[i].command.Trim();
							cumulus.MySqlSettings.Settings.CustomTimed.StartTimes[i] = settings.customtimed.entries[i].starttime;
							cumulus.MySqlSettings.Settings.CustomTimed.Intervals[i] = settings.customtimed.entries[i].interval;

						}
						else
						{
							cumulus.MySqlSettings.Settings.CustomTimed.Commands[i] = null;
							cumulus.MySqlSettings.Settings.CustomTimed.StartTimes[i] = TimeSpan.Zero;
							cumulus.MySqlSettings.Settings.CustomTimed.Intervals[i] = 0;

						}
					}
				}

				// Save the settings
				cumulus.WriteIniFile();

				cumulus.MySqlSettings.CustomSecondsTimer.Interval = cumulus.MySqlSettings.Settings.CustomSecs.Interval * 1000;
				cumulus.MySqlSettings.CustomSecondsTimer.Enabled = cumulus.MySqlSettings.Settings.CustomSecs.Enabled;

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
				using var mySqlConn = new MySqlConnection(cumulus.MySqlSettings.ConnSettings.ToString());
				mySqlConn.Open();

				// first get a list of the columns the table currenty has
				var currCols = new List<string>();
				using (MySqlCommand cmd = new MySqlCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{table.Name}' AND TABLE_SCHEMA='{cumulus.MySqlSettings.ConnSettings.Database}'", mySqlConn))
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
						Cumulus.LogMessage($"MySQL Update Table: " + res);
					}
				}
				else
				{
					res = $"The {table.Name} table already has all the required columns. Required = {table.Columns.Count}, actual = {currCols.Count}";
					Cumulus.LogMessage("MySQL Update Table: " + res);
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
			using (var mySqlConn = new MySqlConnection(cumulus.MySqlSettings.ConnSettings.ToString()))
			using (MySqlCommand cmd = new MySqlCommand(createSQL, mySqlConn))
			{
				Cumulus.LogMessage($"MySQL Create Table: {createSQL}");

				try
				{
					mySqlConn.Open();
					int aff = cmd.ExecuteNonQuery();
					Cumulus.LogMessage($"MySQL Create Table: {aff} items were affected.");
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
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlSettings.MonthlyTable.CreateCommand) + "\"}";
		}

		//public string CreateDayfileSQL(HttpListenerContext context)
		public string CreateDayfileSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlSettings.DayfileTable.CreateCommand) + "\"}";
		}

		//public string CreateRealtimeSQL(HttpListenerContext context)
		public string CreateRealtimeSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlSettings.RealtimeTable.CreateCommand) + "\"}";
		}

		public string UpdateMonthlySQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlSettings.MonthlyTable) + "\"}";
		}

		public string UpdateDayfileSQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlSettings.DayfileTable) + "\"}";
		}

		public string UpdateRealtimeSQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlSettings.RealtimeTable) + "\"}";
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
			public CustomRolloverJson customrollover { get; set; }
			public SettingsCustomTimedJson customtimed { get; set; }
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

		private class CustomRolloverJson
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
