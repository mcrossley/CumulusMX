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
using Swan.Formatters;

namespace CumulusMX
{
	public static class Api
	{
		internal static WeatherStation Station { get; set; }
		internal static ProgramSettings programSettings { private get; set; }
		internal static StationSettings stationSettings { get; set; }
		internal static InternetSettings internetSettings { private get; set; }
		internal static DataLoggingSettings dataLoggingSettings { private get; set; }
		internal static ThirdPartySettings  thirdpartySettings { private get; set; }
		internal static ExtraSensorSettings extraSensorSettings { private get; set; }
		internal static CalibrationSettings calibrationSettings { private get; set; }
		internal static NOAASettings noaaSettings { private get; set; }
		internal static MysqlSettings mySqlSettings { private get; set; }
		internal static CustomLogs customLogs {private get; set; }
		internal static Wizard wizard { private get; set; }
		internal static AlarmSettings alarmSettings { private get; set; }
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
					sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "\\u{0:x4}", (int)ch);
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
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
						await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetEditData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Post, "/edit/{req}")]
			public async Task EditDataPost(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
						case "leaftemp":
							await writer.WriteAsync(logfileEditor.EditLeafTemp(HttpContext));
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "EditDataPost: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					// read the last segment of the URL to determine what data the caller wants
					var lastSegment = Request.Url.Segments.Last();

					var query = HttpUtility.ParseQueryString(Request.Url.Query);
					var date = query["date"];
					var year = query["year"];
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
						//case "extralogfile":
						//	await writer.WriteAsync(logfileEditor.GetIntervalData(from, to, draw, start, length, search, true));
						//	break;
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
						case "leaftemp":
							await writer.WriteAsync(logfileEditor.GetLeafTempData(from, to, draw, start, length, search));
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
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "PostTags: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 404;
				}
			}

			[Route(HttpVerbs.Get, "/tags/{req}")]
			public async Task GetTags(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
							throw new KeyNotFoundException("Key Not Found: " + req);
							//throw new KeyNotFoundException("Key Not Found: " + lastSegment);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetTags: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "tempdata.json":
							await writer.WriteAsync(Station.Graphs.GetTempGraphData(DateTime.Now));
							break;
						case "winddata.json":
							await writer.WriteAsync(Station.Graphs.GetWindGraphData(DateTime.Now));
							break;
						case "raindata.json":
							await writer.WriteAsync(Station.Graphs.GetRainGraphData(DateTime.Now));
							break;
						case "pressdata.json":
							await writer.WriteAsync(Station.Graphs.GetPressGraphData(DateTime.Now));
							break;
						case "wdirdata.json":
							await writer.WriteAsync(Station.Graphs.GetWindDirGraphData(DateTime.Now));
							break;
						case "humdata.json":
							await writer.WriteAsync(Station.Graphs.GetHumGraphData(DateTime.Now));
							break;
						case "solardata.json":
							await writer.WriteAsync(Station.Graphs.GetSolarGraphData(DateTime.Now));
							break;
						case "dailyrain.json":
							await writer.WriteAsync(Station.Graphs.GetDailyRainGraphData());
							break;
						case "sunhours.json":
							await writer.WriteAsync(Station.Graphs.GetSunHoursGraphData());
							break;
						case "dailytemp.json":
							await writer.WriteAsync(Station.Graphs.GetDailyTempGraphData());
							break;
						case "units.json":
							await writer.WriteAsync(WeatherStation.GetUnits());
							break;
						case "graphconfig.json":
							await writer.WriteAsync(Station.Graphs.GetGraphConfig());
							break;
						case "airqualitydata.json":
							await writer.WriteAsync(Station.Graphs.GetAqGraphData(DateTime.Now));
							break;
						case "availabledata.json":
							await writer.WriteAsync(Station.Graphs.GetAvailGraphData());
							break;
						case "selectachart.json":
							await writer.WriteAsync(Station.Graphs.GetSelectaChartOptions());
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetGraphData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Post, "/graphdata/{req}")]
			public async Task SetGraphData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SetGraphData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Get, "/dailygraphdata/{req}")]
			public async Task GetDailyGraphData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					switch (req)
					{
						case "tempdata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDailyTempGraphData());
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
							await writer.WriteAsync(Station.Graphs.GetAllDailySolarGraphData());
							break;
						case "degdaydata.json":
							await writer.WriteAsync(Station.Graphs.GetAllDegreeDaysGraphData());
							break;
						case "tempsumdata.json":
							await writer.WriteAsync(Station.Graphs.GetAllTempSumGraphData());
							break;
						case "units.json":
							await writer.WriteAsync(WeatherStation.GetUnits());
							break;
						case "graphconfig.json":
							await writer.WriteAsync(Station.Graphs.GetGraphConfig());
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetDailyGraphData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetAlltimeData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Get, "/records/month/{mon}/{req}")]
			public async Task GetMonthlyRecordData(string mon, string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					int month = Convert.ToInt32(mon);

					using (var writer = HttpContext.OpenResponseText(new UTF8Encoding(false)))
					{
						if (month < 1 || month > 12)
						{
							await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"406\",\"Description\":\"Month value is out of range\"}}");
							Response.StatusCode = 406;
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
								throw new KeyNotFoundException("Key Not Found: " + req);
						}
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetMonthlyRecordData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Get, "/records/thismonth/{req}")]
			public async Task GetThisMonthRecordData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetThisMonthrecordData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Get, "/records/thisyear/{req}")]
			public async Task GetThisYearRecordData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetThisYearRecordData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Get, "/records/thisperiod")]
			public async Task GetThisPeriodRecordData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("{}");
					return;
				}

				try
				{
					int startday, startmonth, startyear;
					int endday, endmonth, endyear;

					var query = HttpUtility.ParseQueryString(Request.Url.Query);

					using (var writer = HttpContext.OpenResponseText(new UTF8Encoding(false)))
					{
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
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "api/records/thisperiod: Unexpected Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetYesterdayData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
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
						case "leaf.json":
							await writer.WriteAsync(Station.GetLeaf());
							break;
						case "leaf4.json":
							await writer.WriteAsync(Station.GetLeaf4());
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
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetExtraData: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}
		}


		public class SettingsController : WebApiController
		{
			[Route(HttpVerbs.Get, "/settings/{req}")]
			public async Task SettingsGet(string req)
			{
				/* string authorization = context.Request.Headers["Authorization"];
				 string userInfo;
				 string username = "";
				 string password = "";
				 if (authorization != null)
				 {
					 byte[] tempConverted = Convert.FromBase64String(authorization.Replace("Basic ", "").Trim());
					 userInfo = System.Text.Encoding.UTF8.GetString(tempConverted);
					 string[] usernamePassword = userInfo.Split(new string[] {":"}, StringSplitOptions.RemoveEmptyEntries);
					 username = usernamePassword[0];
					 password = usernamePassword[1];
					 Console.WriteLine("username = "+username+" password = "+password);
				 }
				 else
				 {
					 var errorResponse = new
					 {
						 Title = "Authentication required",
						 ErrorCode = "Authentication required",
						 Description = "You must authenticate",
					 };

					 context.Response.StatusCode = 401;
					 return context.JsonResponse(errorResponse);
				 }*/

				try
				{
					Response.ContentType = "application/json";

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
						case "noaadata.json":
							await writer.WriteAsync(noaaSettings.GetAlpacaFormData());
							break;
						case "wsport.json":
							await writer.WriteAsync(stationSettings.GetWSport());
							break;
						case "version.json":
							await writer.WriteAsync(stationSettings.GetVersion());
							break;
						case "mysqldata.json":
							await writer.WriteAsync(mySqlSettings.GetAlpacaFormData());
							break;
						case "alarms.json":
							await writer.WriteAsync(alarmSettings.GetSettings());
							break;
						case "wizard.json":
							await writer.WriteAsync(wizard.GetAlpacaFormData());
							break;
						case "datalogging.json":
							await writer.WriteAsync(dataLoggingSettings.GetAlpacaFormData());
							break;
						case "dateformat.txt":
							Response.ContentType = "text/plain";
							await writer.WriteAsync(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SettingsGet: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Post, "/setsettings/{req}")]
			public async Task SettingsSet(string req)
			{
				try
				{
					Response.ContentType = "application/json";

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
						case "testemail.json":
							await writer.WriteAsync(alarmSettings.TestEmail(HttpContext));
							break;
						case "wizard.json":
							await writer.WriteAsync(wizard.UpdateConfig(HttpContext));
							break;
						case "updatedatalogging.json":
							await writer.WriteAsync(dataLoggingSettings.UpdateConfig(HttpContext));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SettingsSet: Error");
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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

					using (var writer = HttpContext.OpenResponseText(new UTF8Encoding(false)))
					{
						if (!Int32.TryParse(query["year"], out year) || year < 2000 || year > 2050)
						{
							await writer.WriteAsync("Invalid year supplied: " + year);
							Response.StatusCode = 406;
							return;
						}

						switch (req)
						{
							case "noaayear":
								await writer.WriteAsync(String.Join("\n", noaarpts.GetNoaaYearReport(year).ToArray()));
								break;
							case "noaamonth":
								if (!Int32.TryParse(query["month"], out month) || month < 1 || month > 12)
								{
									await writer.WriteAsync("Invalid month supplied: " + month);
									Response.StatusCode = 406;
									return;
								}
								await writer.WriteAsync(String.Join("\n", noaarpts.GetNoaaMonthReport(year, month).ToArray()));
								break;
							default:
								throw new KeyNotFoundException("Key Not Found: " + req);
						}
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
					var query = HttpUtility.ParseQueryString(Request.Url.Query);
					int month, year;
					Response.ContentType = "text/plain";


					using (var writer = HttpContext.OpenResponseText(new UTF8Encoding(false)))
					{
						if (!Int32.TryParse(query["year"], out year) || year < 2000 || year > 2050)
						{
							await writer.WriteAsync("Invalid year supplied: " + year);
							Response.StatusCode = 406;
							return;
						}

						switch (req)
						{
							case "noaayear":
								await writer.WriteAsync(String.Join("\n", noaarpts.GenerateNoaaYearReport(year).ToArray()));
								break;
							case "noaamonth":
								if (!Int32.TryParse(query["month"], out month) || month < 1 || month > 12)
								{
									await writer.WriteAsync("Invalid month supplied: " + month);
									Response.StatusCode = 406;
									return;
								}
								await writer.WriteAsync(String.Join("\n", noaarpts.GenerateNoaaMonthReport(year, month).ToArray()));
								break;
							default:
								throw new KeyNotFoundException("Key Not Found: " + req);
						}
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GenReports: Error");
					// using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					//	await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
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
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("The station is not running");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using (var writer = HttpContext.OpenResponseText(new UTF8Encoding(false)))
					{
						switch (req)
						{
							case "reloaddayfile":
								await writer.WriteAsync(Station.LoadDayFileToDb());
								break;
							case "purgemysql":
								var cnt = 0;
								while (Program.cumulus.MySqlFailedList.TryDequeue(out var item))
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
					Response.StatusCode = 500;
					using var writer = HttpContext.OpenResponseText(new UTF8Encoding(false));
					await writer.WriteAsync("The station is not running");
					return;
				}

				try
				{
					using (var writer = HttpContext.OpenResponseText(new UTF8Encoding(false)))
					{
						switch (req)
						{
							case "ftpnow.json":
								await writer.WriteAsync(stationSettings.FtpNow(HttpContext));
								break;
							default:
								Response.StatusCode = 404;
								break;
						}
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "api/edit: Unexpected Error");
					Response.StatusCode = 500;
				}
			}
		}
	}
}
