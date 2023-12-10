using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;


namespace CumulusMX
{
	public static class Api
	{
		internal static WeatherStation Station { get; set; }
		internal static Cumulus cumulus { get; set; }
		internal static ProgramSettings programSettings { private get; set; }
		internal static StationSettings stationSettings { get; set; }
		internal static InternetSettings internetSettings { private get; set; }
		internal static DataLoggingSettings dataLoggingSettings { private get; set; }
		internal static ThirdPartySettings thirdpartySettings { private get; set; }
		internal static ExtraSensorSettings extraSensorSettings { private get; set; }
		internal static CalibrationSettings calibrationSettings { private get; set; }
		internal static NOAASettings noaaSettings { private get; set; }
		internal static MysqlSettings mySqlSettings { private get; set; }
		internal static MqttSettings mqttSettings;
		internal static CustomLogsSettings customLogs {private get; set; }

		internal static HttpFiles httpFiles;
		internal static Wizard wizard { private get; set; }

		internal static LangSettings langSettings;

		internal static DisplaySettings displaySettings;
		internal static AlarmSettings alarmSettings { private get; set; }
		internal static AlarmUserSettings alarmUserSettings;
		internal static DataEditor dataEditor { get; set; }
		internal static DataEditors logfileEditor { get; set; }
		internal static ApiTagProcessor tagProcessor { get; set; }

		internal static RecordsData RecordsJson { get; set; }

		private static string EscapeUnicode(string input)
		{
			StringBuilder sb = new StringBuilder(input.Length);
			foreach (char ch in input)
			{
				if (ch <= 0x7f)
					sb.Append(ch);
				else
					sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "\\u{0:x4}", (int) ch);
			}
			return sb.ToString();
		}

		// Get/Post Edit data
		public class EditController : WebApiController
		{
			[Route(HttpVerbs.Get, "/edit/{req}")]
			public async Task GetEditData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
						await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "raintodayeditdata.json":
							await writer.WriteAsync(dataEditor.GetRainTodayEditData());
							break;
						case "raintoday":
							await writer.WriteAsync(dataEditor.EditRainToday(HttpContext));
							break;
						case "currentcond.json":
							await writer.WriteAsync(dataEditor.GetCurrentCond());
							break;
						case "alltimerecords.json":
							await writer.WriteAsync(dataEditor.GetAllTimeRecData());
							break;
						case "alltimerecordsdayfile.json":
							await writer.WriteAsync(dataEditor.GetRecordsDayFile("alltime"));
							break;
						case "alltimerecordslogfile.json":
							await writer.WriteAsync(dataEditor.GetRecordsLogFile("alltime"));
							break;
						case "monthlyrecords.json":
							await writer.WriteAsync(dataEditor.GetMonthlyRecData());
							break;
						case "monthlyrecordsdayfile.json":
							await writer.WriteAsync(dataEditor.GetMonthlyRecDayFile());
							break;
						case "monthlyrecordslogfile.json":
							await writer.WriteAsync(dataEditor.GetMonthlyRecLogFile());
							break;
						case "thismonthrecords.json":
							await writer.WriteAsync(dataEditor.GetThisMonthRecData());
							break;
						case "thismonthrecordsdayfile.json":
							await writer.WriteAsync(dataEditor.GetRecordsDayFile("thismonth"));
							break;
						case "thismonthrecordslogfile.json":
							await writer.WriteAsync(dataEditor.GetRecordsLogFile("thismonth"));
							break;
						case "thisyearrecords.json":
							await writer.WriteAsync(dataEditor.GetThisYearRecData());
							break;
						case "thisyearrecordsdayfile.json":
							await writer.WriteAsync(dataEditor .GetRecordsDayFile("thisyear"));
							break;
						case "thisyearrecordslogfile.json":
							await writer.WriteAsync(dataEditor .GetRecordsLogFile("thisyear"));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetEditData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Post, "/edit/{req}")]
			public async Task EditDataPost(string req)
			{
				if (!(await Authenticate(HttpContext)))
				{
					return;
				}

				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					string res;

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "raintodayeditdata.json":
							await writer.WriteAsync(dataEditor.GetRainTodayEditData());
							break;
						case "raintoday":
							await writer.WriteAsync(dataEditor.EditRainToday(HttpContext));
							break;
						case "diarydata":
							await writer.WriteAsync(DiaryDataEditor.EditDiary(HttpContext));
							break;
						case "diarydelete":
							await writer.WriteAsync(DiaryDataEditor.DeleteDiary(HttpContext));
							break;
						case "currcond":
							await writer.WriteAsync(dataEditor.EditCurrentCond(HttpContext));
							break;
						case "alltime":
							res = dataEditor.EditAllTimeRecs(HttpContext);
							if (res != "Success")
							{
								Response.StatusCode = 500;
							}
							await writer.WriteAsync(res);
							break;
						case "monthly":
							res = dataEditor.EditMonthlyRecs(HttpContext);
							if (res != "Success")
							{
								Response.StatusCode = 500;
							}
							await writer.WriteAsync(res);
							break;
						case "thismonth":
							res = dataEditor.EditThisMonthRecs(HttpContext);
							if (res != "Success")
							{
								Response.StatusCode = 500;
							}
							await writer.WriteAsync(res);
							break;
						case "thisyear":
							res = dataEditor.EditThisYearRecs(HttpContext);
							if (res != "Success")
							{
								Response.StatusCode = 500;
							}
							await writer.WriteAsync(res);
							break;
						case "dayfile":
							await writer.WriteAsync(logfileEditor.EditDailyData(HttpContext));
							break;
						case "datalogs":
							await writer.WriteAsync(logfileEditor.EditIntervalData(HttpContext));
							break;
						case "mysqlcache":
							await writer.WriteAsync(dataEditor.EditMySqlCache(HttpContext));
							break;
						case "extratemp":
							await writer.WriteAsync(logfileEditor.EditExtraTemp(HttpContext));
							break;
						case "extrahum":
							await writer.WriteAsync(logfileEditor.EditExtraHum(HttpContext));
							break;
						case "extradew":
							await writer.WriteAsync(logfileEditor.EditExtraDew(HttpContext));
							break;
						case "usertemp":
							await writer.WriteAsync(logfileEditor.EditUserTemp(HttpContext));
							break;
						case "soiltemp":
							await writer.WriteAsync(logfileEditor.EditSoilTemp(HttpContext));
							break;
						case "soilmoist":
							await writer.WriteAsync(logfileEditor.EditSoilMoist(HttpContext));
							break;
						case "leafwet":
							await writer.WriteAsync(logfileEditor.EditLeafWet(HttpContext));
							break;
						case "airqual":
							await writer.WriteAsync(logfileEditor.EditAirQual(HttpContext));
							break;
						case "co2":
							await writer.WriteAsync(logfileEditor.EditCo2(HttpContext));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "EditDataPost: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}


		public class DataController : WebApiController
		{
			[Route(HttpVerbs.Get, "/data/{req}")]
			public async Task GetData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					// read the last segment of the URL to determine what data the caller wants
					var lastSegment = Request.Url.Segments.Last();

					var query = HttpUtility.ParseQueryString(Request.Url.Query);
					var date = query["date"];
					var from = query["from"];
					var to = query["to"];
					var draw = query["draw"];
					int start = Convert.ToInt32(query["start"]);
					int length = Convert.ToInt32(query["length"]);
					var search = query["search[value]"];

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (lastSegment)
					{
						case "dayfile":
							await writer.WriteAsync(logfileEditor.GetDailyData(draw, start, length, search));
							break;
						case "logfile":
							await writer.WriteAsync(logfileEditor.GetIntervalData(from, to, draw, start, length, search));
							break;
						case "extratemp":
							await writer.WriteAsync(logfileEditor.GetExtraTempData(from, to, draw, start, length, search));
							break;
						case "extrahum":
							await writer.WriteAsync(logfileEditor.GetExtraHumData(from, to, draw, start, length, search));
							break;
						case "extradew":
							await writer.WriteAsync(logfileEditor.GetExtraDewData(from, to, draw, start, length, search));
							break;
						case "usertemp":
							await writer.WriteAsync(logfileEditor.GetUserTempData(from, to, draw, start, length, search));
							break;
						case "soiltemp":
							await writer.WriteAsync(logfileEditor.GetSoilTempData(from, to, draw, start, length, search));
							break;
						case "soilmoist":
							await writer.WriteAsync(logfileEditor.GetSoilMoistData(from, to, draw, start, length, search));
							break;
						case "leafwet":
							await writer.WriteAsync(logfileEditor.GetLeafWetData(from, to, draw, start, length, search));
							break;
						case "airqual":
							await writer.WriteAsync(logfileEditor.GetAirQualData(from, to, draw, start, length, search));
							break;
						case "co2":
							await writer.WriteAsync(logfileEditor.GetCo2Data(from, to, draw, start, length, search));
							break;
						case "currentdata":
							await writer.WriteAsync(Station.GetCurrentData());
							break;
						case "diarydata":
							await writer.WriteAsync(DiaryDataEditor.GetDiaryData(date));
							break;
						case "diarysummary":
							await writer.WriteAsync(DiaryDataEditor.GetDiarySummary());
							break;
						case "mysqlcache.json":
							if (await Authenticate(HttpContext))
							{
								await writer.WriteAsync(WeatherStation.GetCachedSqlCommands(draw, start, length, search));
							}
							break;
						case "errorlog.json":
							if (await Authenticate(HttpContext))
							{
								await writer.WriteAsync(cumulus.GetErrorLog());
							}
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}


		// Get/Post Tag body data
		public class TagController : WebApiController
		{
			[Route(HttpVerbs.Post, "/tags/{req}")]
			public async Task PostTags(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "process.txt":
							Response.ContentType = "text/plain";
							await writer.WriteAsync(tagProcessor.ProcessText(HttpContext.Request));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "PostTags: Error");
					Response.StatusCode = 404;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Get, "/tags/{req}")]
			public async Task GetTags(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "process.json":
							await writer.WriteAsync(tagProcessor.ProcessJson(HttpContext.Request));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
							//throw new KeyNotFoundException("Key Not Found: " + lastSegment);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetTags: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}


		public class GraphDataController : WebApiController
		{
			[Route(HttpVerbs.Get, "/graphdata/{req}")]
			public async Task GetGraphData(string req)
			{
				Response.ContentType = "application/json";
				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				var incremental = false;
				DateTime? start = null;
				DateTime? end = null;

				if (Request.QueryString.AllKeys.Contains("start") && long.TryParse(Request.QueryString.Get("start"), out long ts))
				{
					start = Utils.FromUnixTime(ts);
					if (!Request.QueryString.AllKeys.Contains("end"))
						incremental = true;
				}

				if (Request.QueryString.AllKeys.Contains("end") && long.TryParse(Request.QueryString.Get("end"), out ts))
				{
					end = Utils.FromUnixTime(ts);
					if (end > DateTime.Now)
						end = DateTime.Now;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						// recent data
						case "tempdata.json":
							await writer.WriteAsync(Station.Graphs.GetTempGraphData(incremental, true, start));
							break;
						case "winddata.json":
							await writer.WriteAsync(Station.Graphs.GetWindGraphData(incremental, start));
							break;
						case "raindata.json":
							await writer.WriteAsync(Station.Graphs.GetRainGraphData(incremental, start));
							break;
						case "pressdata.json":
							await writer.WriteAsync(Station.Graphs.GetPressGraphData(incremental, start));
							break;
						case "wdirdata.json":
							await writer.WriteAsync(Station.Graphs.GetWindDirGraphData(incremental, start));
							break;
						case "humdata.json":
							await writer.WriteAsync(Station.Graphs.GetHumGraphData(incremental, true, start));
							break;
						case "solardata.json":
							await writer.WriteAsync(Station.Graphs.GetSolarGraphData(incremental, true, start));
							break;
						case "airqualitydata.json":
							await writer.WriteAsync(Station.Graphs.GetAqGraphData(incremental, start));
							break;
						case "extratemp.json":
							await writer.WriteAsync(Station.Graphs.GetExtraTempGraphData(incremental, true, start));
							break;
						case "extrahum.json":
							await writer.WriteAsync(Station.Graphs.GetExtraHumGraphData(incremental, true, start));
							break;
						case "extradew.json":
							await writer.WriteAsync(Station.Graphs.GetExtraDewPointGraphData(incremental, true, start));
							break;
						case "soiltemp.json":
							await writer.WriteAsync(Station.Graphs.GetSoilTempGraphData(incremental, true, start));
							break;
						case "soilmoist.json":
							await writer.WriteAsync(Station.Graphs.GetSoilMoistGraphData(incremental, true, start));
							break;
						case "leafwetness.json":
							await writer.WriteAsync(Station.Graphs.GetLeafWetnessGraphData(incremental, true, start));
							break;
						case "usertemp.json":
							await writer.WriteAsync(Station.Graphs.GetUserTempGraphData(incremental, true, start));
							break;
						case "co2sensor.json":
							await writer.WriteAsync(Station.Graphs.GetCo2SensorGraphData(incremental, true, start));
							break;

						// daily data
						case "dailyrain.json":
							await writer.WriteAsync(Station.Graphs.GetDailyRainGraphData());
							break;
						case "sunhours.json":
							await writer.WriteAsync(Station.Graphs.GetSunHoursGraphData(true));
							break;
						case "dailytemp.json":
							await writer.WriteAsync(Station.Graphs.GetDailyTempGraphData(true));
							break;

						// interval data
						case "intvtemp.json":
							await writer.WriteAsync(Station.Graphs.GetIntervalTempGraphData(true, start, end));
							break;
						case "intvwind.json":
							await writer.WriteAsync(Station.Graphs.GetIntervalWindGraphData(start, end));
							break;
						case "intvrain.json":
							await writer.WriteAsync(Station.Graphs.GetIntervalRainGraphData(start, end));
							break;
						case "intvpress.json":
							await writer.WriteAsync(Station.Graphs.GetIntervalPressGraphData(start, end));
							break;
						case "intvhum.json":
							await writer.WriteAsync(Station.Graphs.GetIntervalHumGraphData(true, start, end));
							break;
						case "intvsolar.json":
							await writer.WriteAsync(Station.Graphs.GetIntervalSolarGraphData(true, start, end));
							break;
						case "intvairquality.json":
							// TODO
							break;
						case "intvextratemp.json":
							await writer.WriteAsync(Station.Graphs.GetExtraTempGraphData(false, true, start, end));
							break;
						case "intvextrahum.json":
							await writer.WriteAsync(Station.Graphs.GetExtraHumGraphData(false, true, start, end));
							break;
						case "intvextradew.json":
							await writer.WriteAsync(Station.Graphs.GetExtraDewPointGraphData(false, true, start, end));
							break;
						case "intvsoiltemp.json":
							await writer.WriteAsync(Station.Graphs.GetSoilTempGraphData(false, true, start, end));
							break;
						case "intvsoilmoist.json":
							await writer.WriteAsync(Station.Graphs.GetSoilMoistGraphData(false, true, start, end));
							break;
						case "intvleafwetness.json":
							await writer.WriteAsync(Station.Graphs.GetLeafWetnessGraphData(false, true, start, end));
							break;
						case "intvusertemp.json":
							await writer.WriteAsync(Station.Graphs.GetUserTempGraphData(false, true, start, end));
							break;
						case "intvco2sensor.json":
							// TODO
							break;

						// config data
						case "units.json":
							await writer.WriteAsync(WeatherStation.GetUnits());
							break;
						case "graphconfig.json":
							await writer.WriteAsync(Station.Graphs.GetGraphConfig(true));
							break;
						case "availabledata.json":
							await writer.WriteAsync(Station.Graphs.GetAvailGraphData());
							break;
						case "selectachart.json":
							await writer.WriteAsync(Station.Graphs.GetSelectaChartOptions());
							break;
						case "selectaperiod.json":
							await writer.WriteAsync(Station.Graphs.GetSelectaPeriodOptions());
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetGraphData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Post, "/graphdata/{req}")]
			public async Task SetGraphData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "selectachart.json":
							await writer.WriteAsync(stationSettings.SetSelectaChartOptions(HttpContext));
							break;
						case "selectaperiod.json":
							await writer.WriteAsync(stationSettings.SetSelectaPeriodOptions(HttpContext));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SetGraphData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Get, "/dailygraphdata/{req}")]
			public async Task GetDailyGraphData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "tempdata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailyTempGraphData(true));
							break;
						case "winddata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailyWindGraphData());
							break;
						case "raindata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailyRainGraphData());
							break;
						case "pressdata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailyPressGraphData());
							break;
						case "wdirdata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailyWindDirGraphData());
							break;
						case "humdata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailyHumGraphData());
							break;
						case "solardata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailySolarGraphData(true));
							break;
						case "degdaydata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDegreeDaysGraphData(true));
							break;
						case "tempsumdata.json":
							await writer.WriteAsync(Station.Graphs.GetAllTempSumGraphData(true));
							break;
						case "units.json":
							await writer.WriteAsync(WeatherStation.GetUnits());
							break;
						case "graphconfig.json":
							await writer.WriteAsync(Station.Graphs.GetGraphConfig(true));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetDailyGraphData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}

		public class RecordsController : WebApiController
		{
			[Route(HttpVerbs.Get, "/records/alltime/{req}")]
			public async Task GetAlltimeData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("{}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetTempRecords()));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetHumRecords()));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetPressRecords()));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetWindRecords()));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetRainRecords()));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetAlltimeData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Get, "/records/month/{mon}/{req}")]
			public async Task GetMonthlyRecordData(string mon, string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					int month = Convert.ToInt32(mon);

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));

					if (month < 1 || month > 12)
					{
						Response.StatusCode = 406;
						await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"406\",\"Description\":\"Month value is out of range\"}}");
					}

					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetMonthlyTempRecords(month)));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetMonthlyHumRecords(month)));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetMonthlyPressRecords(month)));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetMonthlyWindRecords(month)));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetMonthlyRainRecords(month)));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetMonthlyRecordData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Get, "/records/thismonth/{req}")]
			public async Task GetThisMonthRecordData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisMonthTempRecords()));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisMonthHumRecords()));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisMonthPressRecords()));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisMonthWindRecords()));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisMonthRainRecords()));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetThisMonthrecordData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Get, "/records/thisyear/{req}")]
			public async Task GetThisYearRecordData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("{}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisYearTempRecords()));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisYearHumRecords()));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisYearPressRecords()));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisYearWindRecords()));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(RecordsJson.GetThisYearRainRecords()));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetThisYearRecordData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Get, "/records/thisperiod")]
			public async Task GetThisPeriodRecordData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("{}");
					return;
				}

				try
				{
					int startday, startmonth, startyear;
					int endday, endmonth, endyear;

					var query = HttpUtility.ParseQueryString(Request.Url.Query);

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					if (query.AllKeys.Contains("startdate"))
					{
						// we expect "yyyy-mm-dd"
						var start = query["startdate"].Split('-');

						if (!Int32.TryParse(start[0], out startyear) || startyear < 2000 || startyear > 2050)
						{
							await writer.WriteAsync("Invalid start year supplied: " + startyear);
							Response.StatusCode = 406;
							return;
						}

						if (!Int32.TryParse(start[1], out startmonth) || startmonth < 1 || startmonth > 12)
						{
							await writer.WriteAsync("Invalid start month supplied: " + startmonth);
							Response.StatusCode = 406;
							return;
						}

						if (!Int32.TryParse(start[2], out startday) || startday < 1 || startday > 31)
						{
							await writer.WriteAsync("Invalid start day supplied: " + startday);
							Response.StatusCode = 406;
							return;
						}
					}
					else
					{
						await writer.WriteAsync("No start date supplied: ");
						Response.StatusCode = 406;
						return;
					}

					if (query.AllKeys.Contains("enddate"))
					{
						// we expect "yyyy-mm-dd"
						var end = query["enddate"].Split('-');

						if (!Int32.TryParse(end[0], out endyear) || endyear < 2000 || endyear > 2050)
						{
							await writer.WriteAsync("Invalid end year supplied: " + endyear);
							Response.StatusCode = 406;
							return;
						}

						if (!Int32.TryParse(end[1], out endmonth) || endmonth < 1 || endmonth > 12)
						{
							await writer.WriteAsync("Invalid end month supplied: " + endmonth);
							Response.StatusCode = 406;
							return;
						}

						if (!Int32.TryParse(end[2], out endday) || endday < 1 || endday > 31)
						{
							await writer.WriteAsync("Invalid end day supplied: " + endday);
							Response.StatusCode = 406;
							return;
						}
					}
					else
					{
						await writer.WriteAsync("No start date supplied: ");
						Response.StatusCode = 406;
						return;
					}

					var startDate = new DateTime(startyear, startmonth, startday);
					var endDate = new DateTime(endyear, endmonth, endday);

					await writer.WriteAsync(EscapeUnicode(dataEditor.GetRecordsDayFile("thisperiod", startDate, endDate)));
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "api/records/thisperiod: Unexpected Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}


		public class TodayYestDataController : WebApiController
		{
			[Route(HttpVerbs.Get, "/todayyest/{req}")]
			public async Task GetYesterdayData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "temp.json":
							await writer.WriteAsync(Station.GetTodayYestTemp());
							break;
						case "hum.json":
							await writer.WriteAsync(Station.GetTodayYestHum());
							break;
						case "rain.json":
							await writer.WriteAsync(Station.GetTodayYestRain());
							break;
						case "wind.json":
							await writer.WriteAsync(Station.GetTodayYestWind());
							break;
						case "pressure.json":
							await writer.WriteAsync(Station.GetTodayYestPressure());
							break;
						case "solar.json":
							await writer.WriteAsync(Station.GetTodayYestSolar());
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetYesterdayData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}


		public class ExtraDataController : WebApiController
		{
			[Route(HttpVerbs.Get, "/extra/{req}")]
			public async Task GetExtraData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"503\",\"Description\":\"The station is not running\"}}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "temp.json":
							await writer.WriteAsync(Station.GetExtraTemp());
							break;
						case "hum.json":
							await writer.WriteAsync(Station.GetExtraHum());
							break;
						case "dew.json":
							await writer.WriteAsync(Station.GetExtraDew());
							break;
						case "soiltemp.json":
							await writer.WriteAsync(Station.GetSoilTemp());
							break;
						case "soilmoisture.json":
							await writer.WriteAsync(Station.GetSoilMoisture());
							break;
						case "leaf8.json":
							await writer.WriteAsync(Station.GetLeaf8());
							break;
						case "airqual.json":
							await writer.WriteAsync(Station.GetAirQuality());
							break;
						case "lightning.json":
							await writer.WriteAsync(Station.GetLightning());
							break;
						case "usertemp.json":
							await writer.WriteAsync(Station.GetUserTemp());
							break;

						case "airLinkCountsOut.json":
							await writer.WriteAsync(WeatherStation.GetAirLinkCountsOut());
							break;
						case "airLinkAqiOut.json":
							await writer.WriteAsync(WeatherStation.GetAirLinkAqiOut());
							break;
						case "airLinkPctOut.json":
							await writer.WriteAsync(WeatherStation.GetAirLinkPctOut());
							break;
						case "airLinkCountsIn.json":
							await writer.WriteAsync(WeatherStation.GetAirLinkCountsIn());
							break;
						case "airLinkAqiIn.json":
							await writer.WriteAsync(WeatherStation.GetAirLinkAqiIn());
							break;
						case "airLinkPctIn.json":
							await writer.WriteAsync(WeatherStation.GetAirLinkPctIn());
							break;

						case "co2sensor.json":
							await writer.WriteAsync(Station.GetCO2sensor());
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetExtraData: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}


		public class SettingsController : WebApiController
		{
			[Route(HttpVerbs.Get, "/settings/{req}")]
			public async Task SettingsGet(string req)
			{
				try
				{
					Response.ContentType = "application/json";

					if (!(await Authenticate(HttpContext)))
					{
						return;
					}

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "programdata.json":
							await writer.WriteAsync(programSettings.GetAlpacaFormData());
							break;
						case "stationdata.json":
							await writer.WriteAsync(stationSettings.GetAlpacaFormData());
							break;
						case "internetdata.json":
							await writer.WriteAsync(internetSettings.GetAlpacaFormData());
							break;
						case "thirdpartydata.json":
							await writer.WriteAsync(thirdpartySettings.GetAlpacaFormData());
							break;
						case "extrasensordata.json":
							await writer.WriteAsync(extraSensorSettings.GetAlpacaFormData());
							break;
						case "extrawebfiles.json":
							await writer.WriteAsync(internetSettings.GetExtraWebFilesData());
							break;
						case "calibrationdata.json":
							await writer.WriteAsync(calibrationSettings.GetAlpacaFormData());
							break;
						case "langdata.json":
							await writer.WriteAsync(langSettings.GetAlpacaFormData());
							break;
						case "noaadata.json":
							await writer.WriteAsync(noaaSettings.GetAlpacaFormData());
							break;
						case "mysqldata.json":
							await writer.WriteAsync(mySqlSettings.GetAlpacaFormData());
							break;
						case "alarms.json":
							await writer.WriteAsync(alarmSettings.GetSettings());
							break;
						case "useralarms.json":
							await writer.WriteAsync(alarmUserSettings.GetAlpacaFormData());
							break;
						case "wizard.json":
							await writer.WriteAsync(wizard.GetAlpacaFormData());
							break;
						case "datalogging.json":
							await writer.WriteAsync(dataLoggingSettings.GetAlpacaFormData());
							break;
						case "customlogsintvl.json":
							await writer.WriteAsync(customLogs.GetAlpacaFormDataIntvl());
							break;
						case "customlogsdaily.json":
							await writer.WriteAsync(customLogs.GetAlpacaFormDataDaily());
							break;
						case "displayoptions.json":
							await writer.WriteAsync(displaySettings.GetAlpacaFormData());
							break;
						case "httpfiles.json":
							await writer.WriteAsync(httpFiles.GetAlpacaFormData());
							break;
						case "mqttdata.json":
							await writer.WriteAsync(mqttSettings.GetAlpacaFormData());
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SettingsGet: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

			[Route(HttpVerbs.Post, "/setsettings/{req}")]
			public async Task SettingsSet(string req)
			{
				try
				{
					Response.ContentType = "application/json";

					if (!(await Authenticate(HttpContext)))
					{
						return;
					}

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "updateprogramconfig.json":
							await writer.WriteAsync(programSettings.UpdateConfig(HttpContext));
							break;
						case "updatestationconfig.json":
							await writer.WriteAsync(stationSettings.UpdateConfig(HttpContext));
							break;
						case "updateinternetconfig.json":
							await writer.WriteAsync(internetSettings.UpdateConfig(HttpContext));
							break;
						case "updatethirdpartyconfig.json":
							await writer.WriteAsync(thirdpartySettings.UpdateConfig(HttpContext));
							break;
						case "updateextrasensorconfig.json":
							await writer.WriteAsync(extraSensorSettings.UpdateConfig(HttpContext));
							break;
						case "updatecalibrationconfig.json":
							await writer.WriteAsync(calibrationSettings.UpdateConfig(HttpContext));
							break;
						case "updatenoaaconfig.json":
							await writer.WriteAsync(noaaSettings.UpdateConfig(HttpContext));
							break;
						case "updateextrawebfiles.html":
							await writer.WriteAsync(internetSettings.UpdateExtraWebFiles(HttpContext));
							break;
						case "updatemysqlconfig.json":
							await writer.WriteAsync(mySqlSettings.UpdateConfig(HttpContext));
							break;
						case "createmonthlysql.json":
							await writer.WriteAsync(mySqlSettings.CreateMonthlySQL(HttpContext));
							break;
						case "createdayfilesql.json":
							await writer.WriteAsync(mySqlSettings.CreateDayfileSQL(HttpContext));
							break;
						case "createrealtimesql.json":
							await writer.WriteAsync(mySqlSettings.CreateRealtimeSQL(HttpContext));
							break;
						case "updatemonthlysql.json":
							await writer.WriteAsync(mySqlSettings.UpdateMonthlySQL(HttpContext));
							break;
						case "updatedayfilesql.json":
							await writer.WriteAsync(mySqlSettings.UpdateDayfileSQL(HttpContext));
							break;
						case "updaterealtimesql.json":
							await writer.WriteAsync(mySqlSettings.UpdateRealtimeSQL(HttpContext));
							break;
						case "updatealarmconfig.json":
							await writer.WriteAsync(alarmSettings.UpdateSettings(HttpContext));
							break;
						case "updateuseralarms.json":
							await writer.WriteAsync(alarmUserSettings.UpdateConfig(HttpContext));
							break;
						case "testemail.json":
							await writer.WriteAsync(alarmSettings.TestEmail(HttpContext));
							break;
						case "wizard.json":
							await writer.WriteAsync(wizard.UpdateConfig(HttpContext));
							break;
						case "updatecustomlogsintvl.json":
							await writer.WriteAsync(customLogs.UpdateConfigIntvl(HttpContext));
							break;
						case "updatecustomlogsdaily.json":
							await writer.WriteAsync(customLogs.UpdateConfigDaily(HttpContext));
							break;
						case "updatedatalogging.json":
							await writer.WriteAsync(dataLoggingSettings.UpdateConfig(HttpContext));
							break;
						case "updatedisplay.json":
							await writer.WriteAsync(displaySettings.UpdateConfig(HttpContext));
							break;
						case "updatelanguage.json":
							await writer.WriteAsync(langSettings.UpdateConfig(HttpContext));
							break;
						case "updatehttpfiles.json":
							await writer.WriteAsync(httpFiles.UpdateConfig(HttpContext));
							break;
						case "updatemqttconfig.json":
							await writer.WriteAsync(mqttSettings.UpdateConfig(HttpContext));
							break;
						default:
							Response.StatusCode = 404;
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SettingsSet: Error");
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}

		}


		public class ReportsController : WebApiController
		{
			[Route(HttpVerbs.Get, "/reports/{req}")]
			public async Task GetData(string req)
			{
				NOAAReports noaarpts = new NOAAReports(Program.cumulus, Station);
				try
				{
					var query = HttpUtility.ParseQueryString(Request.Url.Query);
					int month, year;

					Response.ContentType = "text/plain";

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));

					if (!Int32.TryParse(query["year"], out year) || year < 2000 || year > 2050)
					{
						await writer.WriteAsync("Invalid year supplied: " + year);
						Response.StatusCode = 406;
						return;
					}

					switch (req)
					{
						case "noaayear":
							await writer.WriteAsync(noaarpts.GetNoaaYearReport(year));
							break;
						case "noaamonth":
							if (!Int32.TryParse(query["month"], out month) || month < 1 || month > 12)
							{
								await writer.WriteAsync("Invalid month supplied: " + month);
								Response.StatusCode = 406;
								return;
							}
							await writer.WriteAsync(noaarpts.GetNoaaMonthReport(year, month));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "Reports GetData: Error");
					//using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					//await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Get, "/genreports/{req}")]
			public async Task GenReports(string req)
			{
				NOAAReports noaarpts = new NOAAReports(Program.cumulus, Station);
				try
				{
					if (!(await Authenticate(HttpContext)))
					{
						return;
					}

					var query = HttpUtility.ParseQueryString(Request.Url.Query);
					int month, year;
					Response.ContentType = "text/plain";

					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));

					switch (req)
					{
						case "noaayear":
							if (!Int32.TryParse(query["year"], out year) || year < 2000 || year > 2050)
							{
								await writer.WriteAsync(noaarpts.GenerateNoaaYearReport(year));
							}
							break;
						case "noaamonth":
							if (!Int32.TryParse(query["year"], out year) || year < 2000 || year > 2050)
							{
								await writer.WriteAsync("Invalid year supplied: " + year);
								Response.StatusCode = 406;
								return;
							}
							if (!Int32.TryParse(query["month"], out month) || month < 1 || month > 12)
							{
								await writer.WriteAsync("Invalid month supplied: " + month);
								Response.StatusCode = 406;
								return;
							}
							await writer.WriteAsync(noaarpts.GenerateNoaaMonthReport(year, month));
							break;
						case "all":
							await writer.WriteAsync(noaarpts.GenerateMissing());
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GenReports: Error");
					Response.StatusCode = 500;
					// using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					//	await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
				}
			}
		}

		public class UtilsController : WebApiController
		{
			[Route(HttpVerbs.Get, "/utils/{req}")]
			public async Task GetUtilData(string req)
			{
				Response.ContentType = "plain/text";

				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("The station is not running");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));

					switch (req)
					{
						case "reloaddayfile":
							await writer.WriteAsync(LoadDatabase.LoadDayFileToDb(Station));
							break;
						case "purgemysql":
							var cnt = 0;
							while (Program.cumulus.MySqlFunction.FailedList.TryDequeue(out var item))
							{
								cnt++;
							};
							_ = Station.Database.Execute("DELETE FROM SqlCache");
							string msg;
							if (cnt == 0)
							{
								msg = "The MySQL cache is already empty!";
							}
							else
							{
								msg = $"Cached MySQL queue cleared of {cnt} commands";
							}
							await writer.WriteAsync(msg);
							break;
						default:
							Response.StatusCode = 404;
							break;
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "api/utils: Unexpected Error");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Post, "/utils/{req}")]
			public async Task PostUtilsData(string req)
			{
				if (Station == null)
				{
					Response.StatusCode = 503;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("The station is not running");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));

					switch (req)
					{
						case "ftpnow.json":
							await writer.WriteAsync(stationSettings.UploadNow(HttpContext));
							break;
						case "clearerrorlog.json":
							await writer.WriteAsync(cumulus.ClearErrorLog());
							break;
						default:
							Response.StatusCode = 404;
							break;
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "api/edit: Unexpected Error");
					Response.StatusCode = 500;
				}
			}
		}

		// Info
		public class InfoController : WebApiController
		{
			[Route(HttpVerbs.Get, "/info/{req}")]
			public async Task InfoGet(string req)
			{
				try
				{
					Response.ContentType = "application/json";
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));

					switch (req)
					{
						case "wsport.json":
							await writer.WriteAsync(stationSettings.GetWSport());
							break;
						case "version.json":
							await writer.WriteAsync(stationSettings.GetVersion());
							break;
						case "dateformat.txt":
							Response.ContentType = "text/plain";
							await writer.WriteAsync(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
							break;
						case "csvseparator.txt":
							Response.ContentType = "text/plain";
							await writer.WriteAsync(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
							break;
						case "alarms.json":
							await writer.WriteAsync(alarmSettings.GetAlarmInfo());
							break;
						case "units.json":
							await writer.WriteAsync(WeatherStation.GetUnits());
							break;
						default:
							Response.StatusCode = 404;
							break;
					}
				}
				catch (Exception ex)
				{
					cumulus.LogErrorMessage($"api/info: Unexpected Error, Description: \"{ex.Message}\"");
					Response.StatusCode = 500;
				}
			}
		}

		private static async Task<bool> Authenticate(IHttpContext context)
		{
			string authorization = context.Request.Headers["Authorization"];
			string userInfo;
			string username;
			string password;

			if (cumulus.ProgramOptions.SecureSettings)
			{
				if (authorization != null)
				{
					byte[] tempConverted = Convert.FromBase64String(authorization.Replace("Basic ", "").Trim());
					userInfo = Encoding.UTF8.GetString(tempConverted);
					string[] usernamePassword = userInfo.Split([':']);
					username = usernamePassword[0] ?? string.Empty;
					password = usernamePassword[1] ?? string.Empty;

					if (username == cumulus.ProgramOptions.SettingsUsername && password == cumulus.ProgramOptions.SettingsPassword)
					{
						return true;
					}
					else
					{
						context.Response.StatusCode = 401;
						context.Response.ContentType = "application/json";
						context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"My Realm\"");
						using var writer = context.OpenResponseText(new UTF8Encoding(false));
						await writer.WriteAsync("{\"Title\":\"Authentication required\",\"ErrorCode\":\"Authentication required\",\"Description\":\"You must authenticate\"}");

						return false;
					}
				}
				else
				{
					context.Response.StatusCode = 401;
					context.Response.ContentType = "application/json";
					context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"My Realm\"");
					using var writer = context.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("{\"Title\":\"Authentication required\",\"ErrorCode\":\"Authentication required\",\"Description\":\"You must authenticate\"}");

					return false;
				}
			}

			return true;
		}
	}
}
