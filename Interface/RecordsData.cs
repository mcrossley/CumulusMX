using System.Text;

namespace CumulusMX
{
	internal class RecordsData
	{
		private readonly Cumulus cumulus;
		private readonly WeatherStation station;

		internal RecordsData(Cumulus cuml, WeatherStation stn)
		{
			cumulus = cuml;
			station = stn;
		}

		#region AllTime records

		private static string alltimejsonformat(AllTimeRec item, string unit, string valueformat, string dateformat)
		{
			return $"[\"{item.Desc}\",\"{item.GetValString(valueformat)} {unit}\",\"{item.GetTsString(dateformat)}\"]";
		}

		internal string GetTempRecords()
		{
			var json = new StringBuilder("{\"data\":[", 2048);

			json.Append(alltimejsonformat(station.AllTime.HighTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.LowTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.HighDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.LowDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.HighAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.LowAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.HighFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.LowFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.HighHumidex, "&nbsp;", cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.LowChill, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.HighHeatIndex, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.HighMinTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.LowMaxTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f") + ",");
			json.Append(alltimejsonformat(station.AllTime.HighDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D") + ",");
			json.Append(alltimejsonformat(station.AllTime.LowDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetHumRecords()
		{
			var json = new StringBuilder("{\"data\":[", 512);

			json.Append(alltimejsonformat(station.AllTime.HighHumidity, "%", cumulus.HumFormat, "f"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.LowHumidity, "%", cumulus.HumFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetPressRecords()
		{
			var json = new StringBuilder("{\"data\":[", 512);

			json.Append(alltimejsonformat(station.AllTime.HighPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.LowPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetWindRecords()
		{
			var json = new StringBuilder("{\"data\":[", 512);

			json.Append(alltimejsonformat(station.AllTime.HighGust, cumulus.Units.WindText, cumulus.WindFormat, "f"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.HighWind, cumulus.Units.WindText, cumulus.WindAvgFormat, "f"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.HighWindRun, cumulus.Units.WindRunText, cumulus.WindRunFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetRainRecords()
		{
			var json = new StringBuilder("{\"data\":[", 512);

			json.Append(alltimejsonformat(station.AllTime.HighRainRate, cumulus.Units.RainText + "/hr", cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.HourlyRain, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.DailyRain, cumulus.Units.RainText, cumulus.RainFormat, "D"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.HighRain24Hours, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.MonthlyRain, cumulus.Units.RainText, cumulus.RainFormat, "Y"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.LongestDryPeriod, "days", "f0", "D"));
			json.Append(',');
			json.Append(alltimejsonformat(station.AllTime.LongestWetPeriod, "days", "f0", "D"));
			json.Append("]}");
			return json.ToString();
		}

		#endregion AllTime records

		#region Monthly records

		private static string monthlyjsonformat(AllTimeRec item, string unit, string valueformat, string dateformat)
		{
			return $"[\"{item.Desc}\",\"{item.GetValString(valueformat)} {unit}\",\"{item.GetTsString(dateformat)}\"]";
		}

		internal string GetMonthlyTempRecords(int month)
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighHumidex, "&nbsp;", cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowChill, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighHeatIndex, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighMinTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowMaxTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetMonthlyHumRecords(int month)
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighHumidity, "%", cumulus.HumFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowHumidity, "%", cumulus.HumFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetMonthlyPressRecords(int month)
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LowPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetMonthlyWindRecords(int month)
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighGust, cumulus.Units.WindText, cumulus.WindFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighWind, cumulus.Units.WindText, cumulus.WindAvgFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighWindRun, cumulus.Units.WindRunText, cumulus.WindRunFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetMonthlyRainRecords(int month)
		{
			var json = new StringBuilder("{\"data\":[", 512);

			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighRainRate, cumulus.Units.RainText + "/hr", cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HourlyRain, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].DailyRain, cumulus.Units.RainText, cumulus.RainFormat, "D"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].HighRain24Hours, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].MonthlyRain, cumulus.Units.RainText, cumulus.RainFormat, "Y"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LongestDryPeriod, "days", "f0", "D"));
			json.Append(',');
			json.Append(monthlyjsonformat(station.MonthlyRecs[month].LongestWetPeriod, "days", "f0", "D"));
			json.Append("]}");
			return json.ToString();
		}

		#endregion Monthly records

		#region ThisMonth records

		private static string monthyearjsonformat(AllTimeRec item, string unit, string valueformat, string dateformat)
		{
			return $"[\"{item.Desc}\",\"{item.GetValString(valueformat)} {unit}\",\"{item.GetTsString(dateformat)}\"]";
		}

		internal string GetThisMonthTempRecords()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			json.Append(monthyearjsonformat(station.ThisMonth.HighTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighHumidex, "&nbsp;", cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowChill, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighHeatIndex, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighMinTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowMaxTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisMonthHumRecords()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthyearjsonformat(station.ThisMonth.HighHumidity, "%", cumulus.HumFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowHumidity, "%", cumulus.HumFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisMonthPressRecords()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthyearjsonformat(station.ThisMonth.HighPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LowPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisMonthWindRecords()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthyearjsonformat(station.ThisMonth.HighGust, cumulus.Units.WindText, cumulus.WindFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighWind, cumulus.Units.WindText, cumulus.WindAvgFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighWindRun, cumulus.Units.WindRunText, cumulus.WindRunFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisMonthRainRecords()
		{
			var json = new StringBuilder("{\"data\":[", 512);

			json.Append(monthyearjsonformat(station.ThisMonth.HighRainRate, cumulus.Units.RainText + "/hr", cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HourlyRain, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.DailyRain, cumulus.Units.RainText, cumulus.RainFormat, "D"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.HighRain24Hours, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			//json.Append(monthyearjsonformat(ThisMonth.WetMonth.Desc, month, cumulus.Units.RainText, cumulus.RainFormat, "Y"));
			//json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LongestDryPeriod, "days", "f0", "D"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisMonth.LongestWetPeriod, "days", "f0", "D"));
			json.Append("]}");
			return json.ToString();
		}

		#endregion ThisMonth records

		#region ThisYear records

		internal string GetThisYearTempRecords()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			json.Append(monthyearjsonformat(station.ThisYear.HighTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowDewPoint, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowAppTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowFeelsLike, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighHumidex, "&nbsp;", cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowChill, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighHeatIndex, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighMinTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowMaxTemp, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowDailyTempRange, "&deg;" + cumulus.Units.TempText[1].ToString(), cumulus.TempFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisYearHumRecords()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthyearjsonformat(station.ThisYear.HighHumidity, "%", cumulus.HumFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowHumidity, "%", cumulus.HumFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisYearPressRecords()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthyearjsonformat(station.ThisYear.HighPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LowPress, cumulus.Units.PressText, cumulus.PressFormat, "f"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisYearWindRecords()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append(monthyearjsonformat(station.ThisYear.HighGust, cumulus.Units.WindText, cumulus.WindFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighWind, cumulus.Units.WindText, cumulus.WindAvgFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighWindRun, cumulus.Units.WindRunText, cumulus.WindRunFormat, "D"));
			json.Append("]}");
			return json.ToString();
		}

		internal string GetThisYearRainRecords()
		{
			var json = new StringBuilder("{\"data\":[", 512);

			json.Append(monthyearjsonformat(station.ThisYear.HighRainRate, cumulus.Units.RainText + "/hr", cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HourlyRain, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.DailyRain, cumulus.Units.RainText, cumulus.RainFormat, "D"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.HighRain24Hours, cumulus.Units.RainText, cumulus.RainFormat, "f"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.MonthlyRain, cumulus.Units.RainText, cumulus.RainFormat, "Y"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LongestDryPeriod, "days", "f0", "D"));
			json.Append(',');
			json.Append(monthyearjsonformat(station.ThisYear.LongestWetPeriod, "days", "f0", "D"));
			json.Append("]}");
			return json.ToString();
		}

		#endregion ThisYear records

	}
}
