using System;
using System.IO;
using System.Net;
using MySql.Data.MySqlClient;
using ServiceStack;
using EmbedIO;

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
				command = cumulus.MySqlStuff.Settings.CustomSecs.Command,
				interval = cumulus.MySqlStuff.Settings.CustomSecs.Interval
			};

			var customminutes = new CustomMinutesJson()
			{
				enabled = cumulus.MySqlStuff.Settings.CustomMins.Enabled,
				command = cumulus.MySqlStuff.Settings.CustomMins.Command,
				intervalindex = cumulus.MySqlStuff.CustomMinutesIntervalIndex
			};

			var customrollover = new CustomRolloverJson()
			{
				enabled = cumulus.MySqlStuff.Settings.CustomRollover.Enabled,
				command = cumulus.MySqlStuff.Settings.CustomRollover.Command,
			};

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
				}
				//realtime
				cumulus.MySqlStuff.Settings.Realtime.Enabled = settings.realtime.enabled;
				if (cumulus.MySqlStuff.Settings.Realtime.Enabled)
				{
					cumulus.MySqlStuff.Settings.RealtimeRetention = settings.realtime.retentionVal + " " + settings.realtime.retentionUnit;
					cumulus.MySqlStuff.Settings.Realtime.TableName = String.IsNullOrWhiteSpace(settings.realtime.table) ? "Realtime" : settings.realtime.table;
					cumulus.MySqlStuff.Settings.RealtimeLimit1Minute = settings.realtime.limit1min;
				}
				//dayfile
				cumulus.MySqlStuff.Settings.Dayfile.Enabled = settings.dayfile.enabled;
				if (cumulus.MySqlStuff.Settings.Dayfile.Enabled)
				{
					cumulus.MySqlStuff.Settings.Dayfile.TableName = String.IsNullOrWhiteSpace(settings.dayfile.table) ? "Dayfile" : settings.dayfile.table;
				}
				// custom seconds
				cumulus.MySqlStuff.Settings.CustomSecs.Enabled = settings.customseconds.enabled;
				if (cumulus.MySqlStuff.Settings.CustomSecs.Enabled)
				{
					cumulus.MySqlStuff.Settings.CustomSecs.Command = settings.customseconds.command;
					cumulus.MySqlStuff.Settings.CustomSecs.Interval = settings.customseconds.interval;
				}
				// custom minutes
				cumulus.MySqlStuff.Settings.CustomMins.Enabled = settings.customminutes.enabled;
				if (cumulus.MySqlStuff.Settings.CustomMins.Enabled)
				{
					cumulus.MySqlStuff.Settings.CustomMins.Command = settings.customminutes.command;
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
					cumulus.MySqlStuff.Settings.CustomRollover.Command = settings.customrollover.command;
				}

				// Save the settings
				cumulus.WriteIniFile();

				cumulus.MySqlStuff.SetIntervalDataCreateString();
				cumulus.MySqlStuff.SetStartOfMonthlyInsertSQL();

				cumulus.MySqlStuff.SetDailyDataCreateString();
				cumulus.MySqlStuff.SetStartOfDayfileInsertSQL();

				cumulus.MySqlStuff.SetRealtimeCreateString();
				cumulus.MySqlStuff.SetStartOfRealtimeInsertSQL();

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
			string json = "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlStuff.CreateMonthlySQL) + "\"}";
			return json;
		}

		//public string CreateDayfileSQL(HttpListenerContext context)
		public string CreateDayfileSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			string json = "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlStuff.CreateDayfileSQL) + "\"}";
			return json;
		}

		//public string CreateRealtimeSQL(HttpListenerContext context)
		public string CreateRealtimeSQL(IHttpContext context)
		{
			context.Response.StatusCode = 200;
			string json = "{\"result\":\"" + CreateMySQLTable(cumulus.MySqlStuff.CreateRealtimeSQL) + "\"}";
			return json;
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
			public string command { get; set; }
			public int interval { get; set; }
		}

		private class CustomMinutesJson
		{
			public bool enabled { get; set; }
			public string command { get; set; }
			public int intervalindex { get; set; }
		}

		private class CustomRolloverJson
		{
			public bool enabled { get; set; }
			public string command { get; set; }
		}
	}
}
