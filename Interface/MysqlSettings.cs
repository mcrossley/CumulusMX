using System;
using System.IO;
using System.Net;
using System.Text;
using MySql.Data.MySqlClient;
using ServiceStack;
using EmbedIO;
using System.Collections.Generic;

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
				database = cumulus.MySqlStuff.ConnSettings.Database,
				host = cumulus.MySqlStuff.ConnSettings.Server,
				pass = cumulus.ProgramOptions.DisplayPasswords ? cumulus.MySqlStuff.ConnSettings.Password : hidden,
				port = cumulus.MySqlStuff.ConnSettings.Port,
				user = cumulus.MySqlStuff.ConnSettings.UserID
			};

			var monthly = new MonthlyJson()
			{
				enabled = cumulus.MySqlStuff.Settings.Monthly.Enabled,
				table = cumulus.MySqlStuff.Settings.Monthly.TableName
			};

			var reten = cumulus.MySqlStuff.Settings.RealtimeRetention.Split(' ');
			var retenVal = string.IsNullOrEmpty(reten[0]) ? 7 : int.Parse(reten[0]);
			var retenUnit = reten.Length > 1 && !string.IsNullOrEmpty(reten[1]) ? reten[1].ToUpper().TrimEnd('S') : "DAY";

			var realtime = new RealtimeJson()
			{
				enabled = cumulus.MySqlStuff.Settings.Realtime.Enabled,
				retentionVal = retenVal,
				retentionUnit = retenUnit,
				table = cumulus.MySqlStuff.Settings.Realtime.TableName,
				limit1min = cumulus.MySqlStuff.Settings.RealtimeLimit1Minute && cumulus.RealtimeInterval < 60000  // do not enable if real time interval is greater than 1 minute
			};

			var dayfile = new DayfileJson()
			{
				enabled = cumulus.MySqlStuff.Settings.Dayfile.Enabled,
				table = cumulus.MySqlStuff.Settings.Dayfile.TableName
			};

			var customseconds = new CustomSecondsJson()
			{
				enabled = cumulus.MySqlStuff.Settings.CustomSecs.Enabled,
				interval = cumulus.MySqlStuff.Settings.CustomSecs.Interval
			};

			var cmdCnt = 1;
			for (var i = 1; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.Settings.CustomSecs.Commands[i]))
					cmdCnt++;
			}
			customseconds.command = new string[cmdCnt];

			var index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.Settings.CustomSecs.Commands[i]))
					customseconds.command[index++] = cumulus.MySqlStuff.Settings.CustomSecs.Commands[i];
			}


			var customminutes = new CustomMinutesJson()
			{
				enabled = cumulus.MySqlStuff.Settings.CustomMins.Enabled,
				intervalindex = cumulus.MySqlStuff.CustomMinutesIntervalIndex
			};

			cmdCnt = 1;
			for (var i = 1; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.Settings.CustomMins.Commands[i]))
					cmdCnt++;
			}
			customminutes.command = new string[cmdCnt];

			index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.Settings.CustomMins.Commands[i]))
					customminutes.command[index++] = cumulus.MySqlStuff.Settings.CustomMins.Commands[i];
			}

			var customrollover = new CustomRolloverJson()
			{
				enabled = cumulus.MySqlStuff.Settings.CustomRollover.Enabled,
			};

			cmdCnt = 1;
			for (var i = 1; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.Settings.CustomRollover.Commands[i]))
					cmdCnt++;
			}
			customrollover.command = new string[cmdCnt];

			index = 0;
			for (var i = 0; i < 10; i++)
			{
				if (!string.IsNullOrEmpty(cumulus.MySqlStuff.Settings.CustomRollover.Commands[i]))
					customrollover.command[index++] = cumulus.MySqlStuff.Settings.CustomRollover.Commands[i];
			}

			var options = new OptionsJson()
			{
				updateonedit = cumulus.MySqlStuff.Settings.UpdateOnEdit,
				bufferonerror = cumulus.MySqlStuff.Settings.BufferOnfailure,
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
				customrollover = customrollover
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
				cumulus.MySqlStuff.ConnSettings.Server = settings.server.host;
				if (settings.server.port > 0 && settings.server.port < 65536)
				{
					cumulus.MySqlStuff.ConnSettings.Port = settings.server.port;
				}
				else
				{
					cumulus.MySqlStuff.ConnSettings.Port = 3306;
				}
				cumulus.MySqlStuff.ConnSettings.Database = settings.server.database;
				cumulus.MySqlStuff.ConnSettings.UserID = settings.server.user;
				if (settings.server.pass != hidden)
					cumulus.MySqlStuff.ConnSettings.Password = settings.server.pass;

				// options
				cumulus.MySqlStuff.Settings.UpdateOnEdit = settings.options.updateonedit;
				cumulus.MySqlStuff.Settings.BufferOnfailure = settings.options.bufferonerror;

				//monthly
				cumulus.MySqlStuff.Settings.Monthly.Enabled = settings.monthly.enabled;
				if (cumulus.MySqlStuff.Settings.Monthly.Enabled)
				{
					cumulus.MySqlStuff.Settings.Monthly.TableName = String.IsNullOrWhiteSpace(settings.monthly.table) ? "Monthly" : settings.monthly.table;
					if (settings.monthly.table != cumulus.MySqlStuff.Settings.Monthly.TableName)
					{
						cumulus.MySqlStuff.MonthlyTable.Name = settings.monthly.table;
						cumulus.MySqlStuff.MonthlyTable.Rebuild();
					}
				}
				//realtime
				cumulus.MySqlStuff.Settings.Realtime.Enabled = settings.realtime.enabled;
				if (cumulus.MySqlStuff.Settings.Realtime.Enabled)
				{
					cumulus.MySqlStuff.Settings.RealtimeRetention = settings.realtime.retentionVal + " " + settings.realtime.retentionUnit;
					cumulus.MySqlStuff.Settings.Realtime.TableName = String.IsNullOrWhiteSpace(settings.realtime.table) ? "Realtime" : settings.realtime.table;
					cumulus.MySqlStuff.Settings.RealtimeLimit1Minute = settings.realtime.limit1min;
					if (settings.realtime.table != cumulus.MySqlStuff.Settings.Realtime.TableName)
					{
						cumulus.MySqlStuff.RealtimeTable.Name = settings.realtime.table;
						cumulus.MySqlStuff.RealtimeTable.Rebuild();
					}
				}
				//dayfile
				cumulus.MySqlStuff.Settings.Dayfile.Enabled = settings.dayfile.enabled;
				if (cumulus.MySqlStuff.Settings.Dayfile.Enabled)
				{
					cumulus.MySqlStuff.Settings.Dayfile.TableName = String.IsNullOrWhiteSpace(settings.dayfile.table) ? "Dayfile" : settings.dayfile.table;
					if (settings.dayfile.table != cumulus.MySqlStuff.Settings.Dayfile.TableName)
					{
						cumulus.MySqlStuff.DayfileTable.Name = settings.dayfile.table;
						cumulus.MySqlStuff.DayfileTable.Rebuild();
					}
				}
				// custom seconds
				cumulus.MySqlStuff.Settings.CustomSecs.Enabled = settings.customseconds.enabled;
				if (cumulus.MySqlStuff.Settings.CustomSecs.Enabled)
				{
					cumulus.MySqlStuff.Settings.CustomSecs.Commands[0] = settings.customseconds.command[0] ?? string.Empty;
					for (var i = 1; i < 10; i++)
					{
						if (i < settings.customseconds.command.Length)
							cumulus.MySqlStuff.Settings.CustomSecs.Commands[i] = settings.customseconds.command[i] ?? null;
						else
							cumulus.MySqlStuff.Settings.CustomSecs.Commands[i] = null;
					}
					cumulus.MySqlStuff.Settings.CustomSecs.Interval = settings.customseconds.interval;
				}
				// custom minutes
				cumulus.MySqlStuff.Settings.CustomMins.Enabled = settings.customminutes.enabled;
				if (cumulus.MySqlStuff.Settings.CustomMins.Enabled)
				{
					cumulus.MySqlStuff.Settings.CustomMins.Commands[0] = settings.customminutes.command[0] ?? string.Empty;
					for (var i = 1; i < 10; i++)
					{
						if (i < settings.customminutes.command.Length)
							cumulus.MySqlStuff.Settings.CustomMins.Commands[i] = settings.customminutes.command[i] ?? null;
						else
							cumulus.MySqlStuff.Settings.CustomMins.Commands[i] = null;
					}
					cumulus.MySqlStuff.CustomMinutesIntervalIndex = settings.customminutes.intervalindex;
					if (cumulus.MySqlStuff.CustomMinutesIntervalIndex >= 0 && cumulus.MySqlStuff.CustomMinutesIntervalIndex < cumulus.FactorsOf60.Length)
					{
						cumulus.MySqlStuff.Settings.CustomMins.Interval = cumulus.FactorsOf60[cumulus.MySqlStuff.CustomMinutesIntervalIndex];
					}
					else
					{
						cumulus.MySqlStuff.Settings.CustomMins.Interval = 10;
					}
				}
				// custom roll-over
				cumulus.MySqlStuff.Settings.CustomRollover.Enabled = settings.customrollover.enabled;
				if (cumulus.MySqlStuff.Settings.CustomRollover.Enabled)
				{
					cumulus.MySqlStuff.Settings.CustomRollover.Commands[0] = settings.customrollover.command[0];
					for (var i = 1; i < 10; i++)
					{
						if (i < settings.customrollover.command.Length)
							cumulus.MySqlStuff.Settings.CustomRollover.Commands[i] = settings.customrollover.command[i] ?? null;
						else
							cumulus.MySqlStuff.Settings.CustomRollover.Commands[i] = null;
					}
				}

				// Save the settings
				cumulus.WriteIniFile();

				cumulus.MySqlStuff.CustomSecondsTimer.Interval = cumulus.MySqlStuff.Settings.CustomSecs.Interval * 1000;
				cumulus.MySqlStuff.CustomSecondsTimer.Enabled = cumulus.MySqlStuff.Settings.CustomSecs.Enabled;

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
				using var mySqlConn = new MySqlConnection(cumulus.MySqlStuff.ConnSettings.ToString());
				mySqlConn.Open();

				// first get a list of the columns the table currenty has
				var currCols = new List<string>();
				using (MySqlCommand cmd = new MySqlCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{table.Name}' AND TABLE_SCHEMA='{cumulus.MySqlStuff.ConnSettings.Database}'", mySqlConn))
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
			using (var mySqlConn = new MySqlConnection(cumulus.MySqlStuff.ConnSettings.ToString()))
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
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlStuff.MonthlyTable.CreateCommand) + "\"}";
		}

		//public string CreateDayfileSQL(HttpListenerContext context)
		public string CreateDayfileSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlStuff.DayfileTable.CreateCommand) + "\"}";
		}

		//public string CreateRealtimeSQL(HttpListenerContext context)
		public string CreateRealtimeSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			return "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlStuff.RealtimeTable.CreateCommand) + "\"}";
		}

		public string UpdateMonthlySQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlStuff.MonthlyTable) + "\"}";
		}

		public string UpdateDayfileSQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlStuff.DayfileTable) + "\"}";
		}

		public string UpdateRealtimeSQL(IHttpContext context)
		{
			return "{\"result\":\"" + UpdateMySQLTable(cumulus.MySqlStuff.RealtimeTable) + "\"}";
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
	}
}
