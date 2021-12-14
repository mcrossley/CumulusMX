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
		internal static ThirdPartySettings  thirdpartySettings { private get; set; }
		internal static ExtraSensorSettings extraSensorSettings { private get; set; }
		internal static CalibrationSettings calibrationSettings { private get; set; }
		internal static NOAASettings noaaSettings { private get; set; }
		internal static MysqlSettings mySqlSettings { private get; set; }
		internal static Wizard wizard { private get; set; }
		internal static AlarmSettings alarmSettings { private get; set; }
		internal static DataEditor dataEditor { get; set; }
		internal static DataEditors logfileEditor { get; set; }
		internal static ApiTagProcessor tagProcessor { get; set; }

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

		public class EditController : WebApiController
		{
			[Route(HttpVerbs.Get, "/edit/{req}")]
			public async Task GetEditData(string req)
			{
				Response.ContentType = "application/json";

				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
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
							await writer.WriteAsync(await dataEditor.GetRecordsDayFile("alltime"));
							break;
						case "alltimerecordslogfile.json":
							await writer.WriteAsync(await dataEditor.GetRecordsLogFile("alltime"));
							break;
						case "monthlyrecords.json":
							await writer.WriteAsync(dataEditor.GetMonthlyRecData());
							break;
						case "monthlyrecordsdayfile.json":
							await writer.WriteAsync(await dataEditor.GetMonthlyRecDayFile());
							break;
						case "monthlyrecordslogfile.json":
							await writer.WriteAsync(await dataEditor.GetMonthlyRecLogFile());
							break;
						case "thismonthrecords.json":
							await writer.WriteAsync(dataEditor.GetThisMonthRecData());
							break;
						case "thismonthrecordsdayfile.json":
							await writer.WriteAsync(await dataEditor.GetRecordsDayFile("thismonth"));
							break;
						case "thismonthrecordslogfile.json":
							await writer.WriteAsync(await dataEditor.GetRecordsLogFile("thismonth"));
							break;
						case "thisyearrecords.json":
							await writer.WriteAsync(dataEditor.GetThisYearRecData());
							break;
						case "thisyearrecordsdayfile.json":
							await writer.WriteAsync(await dataEditor .GetRecordsDayFile("thisyear"));
							break;
						case "thisyearrecordslogfile.json":
							await writer.WriteAsync(await dataEditor .GetRecordsLogFile("thisyear"));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetEditData: Error");
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
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
							await writer.WriteAsync(dataEditor.EditAllTimeRecs(HttpContext));
							break;
						case "monthly":
							await writer.WriteAsync(dataEditor.EditMonthlyRecs(HttpContext));
							break;
						case "thismonth":
							await writer.WriteAsync(dataEditor.EditThisMonthRecs(HttpContext));
							break;
						case "thisyear":
							await writer.WriteAsync(dataEditor.EditThisYearRecs(HttpContext));
							break;
						case "dayfile":
							await writer.WriteAsync(await logfileEditor.EditDailyData(HttpContext));
							break;
						case "datalogs":
							await writer.WriteAsync(await logfileEditor.EditIntervalData(HttpContext));
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
					using (var writer = HttpContext.OpenResponseText())
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
					using var writer = HttpContext.OpenResponseText();
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

					using var writer = HttpContext.OpenResponseText();
					switch (lastSegment)
					{
						case "dayfile":
							await writer.WriteAsync(await logfileEditor.GetDailyData(draw, start, length));
							break;
						case "logfile":
							await writer.WriteAsync(await logfileEditor.GetIntervalData(from, to, draw, start, length, search));
							break;
						//case "extralogfile":
						//	await writer.WriteAsync(await logfileEditor.GetIntervalData(from, to, draw, start, length, search, true));
						//	break;
						case "extratemp":
							await writer.WriteAsync(await logfileEditor.GetExtraTempData(from, to, draw, start, length, search));
							break;
						case "extrahum":
							await writer.WriteAsync(await logfileEditor.GetExtraHumData(from, to, draw, start, length, search));
							break;
						case "extradew":
							await writer.WriteAsync(await logfileEditor.GetExtraDewData(from, to, draw, start, length, search));
							break;
						case "usertemp":
							await writer.WriteAsync(await logfileEditor.GetUserTempData(from, to, draw, start, length, search));
							break;
						case "soiltemp":
							await writer.WriteAsync(await logfileEditor.GetSoilTempData(from, to, draw, start, length, search));
							break;
						case "soilmoist":
							await writer.WriteAsync(await logfileEditor.GetSoilMoistData(from, to, draw, start, length, search));
							break;
						case "leaftemp":
							await writer.WriteAsync(await logfileEditor.GetLeafTempData(from, to, draw, start, length, search));
							break;
						case "leafwet":
							await writer.WriteAsync(await logfileEditor.GetLeafWetData(from, to, draw, start, length, search));
							break;
						case "airqual":
							await writer.WriteAsync(await logfileEditor.GetAirQualData(from, to, draw, start, length, search));
							break;
						case "co2":
							await writer.WriteAsync(await logfileEditor.GetCo2Data(from, to, draw, start, length, search));
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
					using (var writer = HttpContext.OpenResponseText())
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
				if (Station == null)
				{
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "process.txt":
							Response.ContentType = "text/plain";
							await writer.WriteAsync(tagProcessor.ProcessText(HttpContext));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "PostTags: Error");
					using (var writer = HttpContext.OpenResponseText())
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "process.json":
							await writer.WriteAsync(tagProcessor.ProcessJson(Request.Url.Query));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
							//throw new KeyNotFoundException("Key Not Found: " + lastSegment);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetTags: Error");
					using (var writer = HttpContext.OpenResponseText())
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "tempdata.json":
							await writer.WriteAsync(await Station.GetTempGraphData());
							break;
						case "winddata.json":
							await writer.WriteAsync(await Station.GetWindGraphData());
							break;
						case "raindata.json":
							await writer.WriteAsync(await Station.GetRainGraphData());
							break;
						case "pressdata.json":
							await writer.WriteAsync(await Station.GetPressGraphData());
							break;
						case "wdirdata.json":
							await writer.WriteAsync(await Station.GetWindDirGraphData());
							break;
						case "humdata.json":
							await writer.WriteAsync(await Station.GetHumGraphData());
							break;
						case "solardata.json":
							await writer.WriteAsync(await Station.GetSolarGraphData());
							break;
						case "dailyrain.json":
							await writer.WriteAsync(await Station.GetDailyRainGraphData());
							break;
						case "sunhours.json":
							await writer.WriteAsync(await Station.GetSunHoursGraphData());
							break;
						case "dailytemp.json":
							await writer.WriteAsync(await Station.GetDailyTempGraphData());
							break;
						case "units.json":
							await writer.WriteAsync(Station.GetUnits());
							break;
						case "graphconfig.json":
							await writer.WriteAsync(Station.GetGraphConfig());
							break;
						case "airqualitydata.json":
							await writer.WriteAsync(await Station.GetAqGraphData());
							break;
						case "availabledata.json":
							await writer.WriteAsync(Station.GetAvailGraphData());
							break;
						case "selectachart.json":
							await writer.WriteAsync(Station.GetSelectaChartOptions());
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetGraphData: Error");
					using var writer = HttpContext.OpenResponseText();
						await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}


			[Route(HttpVerbs.Post, "/graphdata/{req}")]
			public async Task SetGraphData(string req)
			{
				try
				{
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "tempdata.json":
							await writer.WriteAsync(await Station.GetAllDailyTempGraphData());
							break;
						case "winddata.json":
							await writer.WriteAsync(await Station.GetAllDailyWindGraphData());
							break;
						case "raindata.json":
							await writer.WriteAsync(await Station.GetAllDailyRainGraphData());
							break;
						case "pressdata.json":
							await writer.WriteAsync(await Station.GetAllDailyPressGraphData());
							break;
						//case "wdirdata.json":
						//	await writer.WriteAsync(await Station.GetAllDailyWindDirGraphData());
						// break;
						case "humdata.json":
							await writer.WriteAsync(await Station.GetAllDailyHumGraphData());
							break;
						case "solardata.json":
							await writer.WriteAsync(await Station.GetAllDailySolarGraphData());
							break;
						case "degdaydata.json":
							await writer.WriteAsync(await Station.GetAllDegreeDaysGraphData());
							break;
						case "tempsumdata.json":
							await writer.WriteAsync(await Station.GetAllTempSumGraphData());
							break;
						case "units.json":
							await writer.WriteAsync(Station.GetUnits());
							break;
						case "graphconfig.json":
							await writer.WriteAsync(Station.GetGraphConfig());
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetDailyGraphData: Error");
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync("{}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetTempRecords()));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetHumRecords()));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetPressRecords()));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetWindRecords()));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetRainRecords()));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetAlltimeData: Error");
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					int month = Convert.ToInt32(mon);

					using var writer = HttpContext.OpenResponseText();
					if (month < 1 || month > 12)
					{
						await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"406\",\"Description\":\"Month value is out of range\"}}");
						Response.StatusCode = 406;
					}

					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetMonthlyTempRecords(month)));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetMonthlyHumRecords(month)));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetMonthlyPressRecords(month)));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetMonthlyWindRecords(month)));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetMonthlyRainRecords(month)));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetMonthlyRecordData: Error");
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisMonthTempRecords()));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisMonthHumRecords()));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisMonthPressRecords()));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisMonthWindRecords()));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisMonthRainRecords()));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetThisMonthrecordData: Error");
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync("{}");
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "temperature.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisYearTempRecords()));
							break;
						case "humidity.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisYearHumRecords()));
							break;
						case "pressure.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisYearPressRecords()));
							break;
						case "wind.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisYearWindRecords()));
							break;
						case "rain.json":
							await writer.WriteAsync(EscapeUnicode(Station.GetThisYearRainRecords()));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetThisYearRecordData: Error");
					using (var writer = HttpContext.OpenResponseText())
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
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
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"500\",\"Description\":\"The station is not running\"}}");
					Response.StatusCode = 500;
					return;
				}

				try
				{
					using var writer = HttpContext.OpenResponseText();
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
							await writer.WriteAsync(Station.GetAirLinkCountsOut());
							break;
						case "airLinkAqiOut.json":
							await writer.WriteAsync(Station.GetAirLinkAqiOut());
							break;
						case "airLinkPctOut.json":
							await writer.WriteAsync(Station.GetAirLinkPctOut());
							break;
						case "airLinkCountsIn.json":
							await writer.WriteAsync(Station.GetAirLinkCountsIn());
							break;
						case "airLinkAqiIn.json":
							await writer.WriteAsync(Station.GetAirLinkAqiIn());
							break;
						case "airLinkPctIn.json":
							await writer.WriteAsync(Station.GetAirLinkPctIn());
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
					using var writer = HttpContext.OpenResponseText();
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

					using var writer = HttpContext.OpenResponseText();
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
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SettingsGet: Error");
					using var writer = HttpContext.OpenResponseText();
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

					using var writer = HttpContext.OpenResponseText();
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
						case "updatealarmconfig.json":
							await writer.WriteAsync(alarmSettings.UpdateSettings(HttpContext));
							break;
						case "ftpnow.json":
							await writer.WriteAsync(stationSettings.FtpNow(HttpContext));
							break;
						case "testemail.json":
							await writer.WriteAsync(alarmSettings.TestEmail(HttpContext));
							break;
						case "wizard.json":
							await writer.WriteAsync(wizard.UpdateConfig(HttpContext));
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "SettingsSet: Error");
					using var writer = HttpContext.OpenResponseText();
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
					// read the last segment of the URL to determine what data the caller wants
					var lastSegment = Request.Url.Segments.Last();

					var query = HttpUtility.ParseQueryString(Request.Url.Query);
					int month, year;

					Response.ContentType = "application/json";

					using var writer = HttpContext.OpenResponseText();
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
								await writer.WriteAsync(Json.Serialize(noaarpts.GetNoaaYearReport(year)));
								break;
							case "noaamonth":
								if (!Int32.TryParse(query["month"], out month) || month < 1 || month > 12)
								{
									await writer.WriteAsync("Invalid month supplied: " + month);
									Response.StatusCode = 406;
									return;
								}
								await writer.WriteAsync(Json.Serialize(noaarpts.GetNoaaMonthReport(year, month)));
								break;
							default:
								throw new KeyNotFoundException("Key Not Found: " + req);
						}
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "Reports GetData: Error");
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
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
					Response.ContentType = "application/json";


					using var writer = HttpContext.OpenResponseText();
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
								await writer.WriteAsync(Json.Serialize(noaarpts.GenerateNoaaYearReport(year)));
								break;
							case "noaamonth":
								if (!Int32.TryParse(query["month"], out month) || month < 1 || month > 12)
								{
									await writer.WriteAsync("Invalid month supplied: " + month);
									Response.StatusCode = 406;
									return;
								}
								await writer.WriteAsync(Json.Serialize(await noaarpts.GenerateNoaaMonthReport(year, month)));
								break;
							default:
								throw new KeyNotFoundException("Key Not Found: " + req);
						}
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GenReports: Error");
					using var writer = HttpContext.OpenResponseText();
						await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}
		}

	}
}
