using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EmbedIO;
using System.Threading.Tasks;
using SQLite;

namespace CumulusMX
{
	internal class DataEditor
	{
		private WeatherStation station;
		private readonly Cumulus cumulus;

		private readonly List<LastHourRainLog> hourRainLog = new List<LastHourRainLog>();
		private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;

		internal DataEditor(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		internal void SetStation(WeatherStation station)
		{
			this.station = station;
		}


		internal string GetAllTimeRecData()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";
			// Records - Temperature values
			var json = new StringBuilder("{", 1700);
			json.Append($"\"highTempVal\":\"{station.AllTime.HighTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempVal\":\"{station.AllTime.LowTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointVal\":\"{station.AllTime.HighDewPoint.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointVal\":\"{station.AllTime.LowDewPoint.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempVal\":\"{station.AllTime.HighAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempVal\":\"{station.AllTime.LowAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeVal\":\"{station.AllTime.HighFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeVal\":\"{station.AllTime.LowFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexVal\":\"{station.AllTime.HighHumidex.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillVal\":\"{station.AllTime.LowChill.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexVal\":\"{station.AllTime.HighHeatIndex.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempVal\":\"{station.AllTime.HighMinTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempVal\":\"{station.AllTime.LowMaxTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeVal\":\"{station.AllTime.HighDailyTempRange.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeVal\":\"{station.AllTime.LowDailyTempRange.GetValString(cumulus.TempFormat)}\",");
			// Records - Temperature timestamps
			json.Append($"\"highTempTime\":\"{station.AllTime.HighTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowTempTime\":\"{station.AllTime.LowTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDewPointTime\":\"{station.AllTime.HighDewPoint.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointTime\":\"{station.AllTime.LowDewPoint.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempTime\":\"{station.AllTime.HighAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempTime\":\"{station.AllTime.LowAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeTime\":\"{station.AllTime.HighFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeTime\":\"{station.AllTime.LowFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHumidexTime\":\"{station.AllTime.HighHumidex.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillTime\":\"{station.AllTime.LowChill.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexTime\":\"{station.AllTime.HighHeatIndex.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highMinTempTime\":\"{station.AllTime.HighMinTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempTime\":\"{station.AllTime.LowMaxTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeTime\":\"{station.AllTime.HighDailyTempRange.GetTsString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeTime\":\"{station.AllTime.LowDailyTempRange.GetTsString(dateStampFormat)}\",");
			// Records - Humidity values
			json.Append($"\"highHumidityVal\":\"{station.AllTime.HighHumidity.GetValString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityVal\":\"{station.AllTime.LowHumidity.GetValString(cumulus.HumFormat)}\",");
			// Records - Humidity times
			json.Append($"\"highHumidityTime\":\"{station.AllTime.HighHumidity.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityTime\":\"{station.AllTime.LowHumidity.GetTsString(timeStampFormat)}\",");
			// Records - Pressure values
			json.Append($"\"highBarometerVal\":\"{station.AllTime.HighPress.GetValString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerVal\":\"{station.AllTime.LowPress.GetValString(cumulus.PressFormat)}\",");
			// Records - Pressure times
			json.Append($"\"highBarometerTime\":\"{station.AllTime.HighPress.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerTime\":\"{station.AllTime.LowPress.GetTsString(timeStampFormat)}\",");
			// Records - Wind values
			json.Append($"\"highGustVal\":\"{station.AllTime.HighGust.GetValString(cumulus.WindFormat)}\",");
			json.Append($"\"highWindVal\":\"{station.AllTime.HighWind.GetValString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindRunVal\":\"{station.AllTime.HighWindRun.GetValString(cumulus.WindRunFormat)}\",");
			// Records - Wind times
			json.Append($"\"highGustTime\":\"{station.AllTime.HighGust.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindTime\":\"{station.AllTime.HighWind.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindRunTime\":\"{station.AllTime.HighWindRun.GetTsString(dateStampFormat)}\",");
			// Records - Rain values
			json.Append($"\"highRainRateVal\":\"{station.AllTime.HighRainRate.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainVal\":\"{station.AllTime.HourlyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainVal\":\"{station.AllTime.DailyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highMonthlyRainVal\":\"{station.AllTime.MonthlyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"longestDryPeriodVal\":\"{station.AllTime.LongestDryPeriod.GetValString("f0")}\",");
			json.Append($"\"longestWetPeriodVal\":\"{station.AllTime.LongestWetPeriod.GetValString("f0")}\",");
			// Records - Rain times
			json.Append($"\"highRainRateTime\":\"{station.AllTime.HighRainRate.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainTime\":\"{station.AllTime.HourlyRain.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainTime\":\"{station.AllTime.DailyRain.GetTsString(dateStampFormat)}\",");
			json.Append($"\"highMonthlyRainTime\":\"{station.AllTime.MonthlyRain.GetTsString("MM/yyyy")}\",");
			json.Append($"\"longestDryPeriodTime\":\"{station.AllTime.LongestDryPeriod.GetTsString(dateStampFormat)}\",");
			json.Append($"\"longestWetPeriodTime\":\"{station.AllTime.LongestWetPeriod.GetTsString(dateStampFormat)}\"");
			json.Append('}');

			return json.ToString();
		}

		internal string GetRecordsDayFile(string recordType)
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var highTemp = new LocalRec(true);
			var lowTemp = new LocalRec(false);
			var highDewPt = new LocalRec(true);
			var lowDewPt = new LocalRec(false);
			var highAppTemp = new LocalRec(true);
			var lowAppTemp = new LocalRec(false);
			var highFeelsLike = new LocalRec(true);
			var lowFeelsLike = new LocalRec(false);
			var highHumidex = new LocalRec(true);
			var lowWindChill = new LocalRec(false);
			var highHeatInd = new LocalRec(true);
			var highMinTemp = new LocalRec(true);
			var lowMaxTemp = new LocalRec(false);
			var highTempRange = new LocalRec(true);
			var lowTempRange = new LocalRec(false);
			var highHum = new LocalRec(true);
			var lowHum = new LocalRec(false);
			var highBaro = new LocalRec(true);
			var lowBaro = new LocalRec(false);
			var highGust = new LocalRec(true);
			var highWind = new LocalRec(true);
			var highWindRun = new LocalRec(true);
			var highRainRate = new LocalRec(true);
			var highRainHour = new LocalRec(true);
			var highRainDay = new LocalRec(true);
			var highRainMonth = new LocalRec(true);
			var dryPeriod = new LocalRec(true);
			var wetPeriod = new LocalRec(true);

			var thisDate = DateTime.MinValue;
			DateTime startDate;

			switch (recordType)
			{
				case "alltime":
					startDate = DateTime.MinValue;
					break;
				case "thisyear":
					var now = DateTime.Now;
					startDate = new DateTime(now.Year, 1, 1);
					break;
				case "thismonth":
					now = DateTime.Now;
					startDate = new DateTime(now.Year, now.Month, 1);
					break;
				default:
					startDate = DateTime.MinValue;
					break;
			}

			var rainThisMonth = 0.0;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;
			var isDryNow = false;
			var thisDateDry = DateTime.MinValue;
			var thisDateWet = DateTime.MinValue;
			var firstRec = true;

			var json = new StringBuilder("{", 2048);

			int rainThreshold;
			if (cumulus.RainDayThreshold > 0)
			{
				rainThreshold = Convert.ToInt32(cumulus.RainDayThreshold * 1000);
			}
			else
			{
				// default
				if (cumulus.Units.Rain == 0)
				{
					rainThreshold = 200; // 0.2mm *1000
				}
				else
				{
					rainThreshold = 10;  // 0.01in *1000
				}
			}

			// Get all the dayfile records from the Database
			var data = station.Database.Query<DayData>("select * from DayData where Timestamp >= ? order by Timestamp", startDate);

			if (data.Count > 0)
			{
				foreach (var rec in data)
				{
					if (firstRec)
					{
						thisDate = rec.Timestamp.Date;
						firstRec = false;
					}

					// This assumes the day file is in date order!
					if (thisDate.Month != rec.Timestamp.Month)
					{
						// reset the date and counter for a new month
						thisDate = rec.Timestamp;
						rainThisMonth = 0;
					}
					// hi gust
					if (rec.HighGust.HasValue && rec.HighGust.Value > highGust.Value && rec.HighGustTime.HasValue)
					{
						highGust.Value = rec.HighGust.Value;
						highGust.Ts = rec.HighGustTime.Value;
					}
					// hi temp
					if (rec.HighTemp.HasValue && rec.HighTemp.Value > highTemp.Value && rec.HighTempTime.HasValue)
					{
						highTemp.Value = rec.HighTemp.Value;
						highTemp.Ts = rec.HighTempTime.Value;
					}
					// lo temp
					if (rec.LowTemp.HasValue && rec.LowTemp.Value < lowTemp.Value && rec.LowTempTime.HasValue)
					{
						lowTemp.Value = rec.LowTemp.Value;
						lowTemp.Ts= rec.LowTempTime.Value;
					}
					// hi min temp
					if (rec.LowTemp.HasValue && rec.LowTemp.Value > highMinTemp.Value && rec.LowTempTime.HasValue)
					{
						highMinTemp.Value = rec.LowTemp.Value;
						highMinTemp.Ts = rec.LowTempTime.Value;
					}
					// lo max temp
					if (rec.HighTemp.HasValue && rec.HighTemp.Value < lowMaxTemp.Value && rec.HighTempTime.HasValue)
					{
						lowMaxTemp.Value = rec.HighTemp.Value;
						lowMaxTemp.Ts = rec.HighTempTime.Value;
					}
					// hi temp range
					if (rec.LowTemp.HasValue && rec.HighTemp.HasValue && (rec.HighTemp.Value - rec.LowTemp.Value) > highTempRange.Value)
					{
						highTempRange.Value = rec.HighTemp.Value - rec.LowTemp.Value;
						highTempRange.Ts = rec.Timestamp;
					}
					// lo temp range
					if (rec.LowTemp.HasValue && rec.HighTemp.HasValue && (rec.HighTemp.Value - rec.LowTemp.Value) < lowTempRange.Value)
					{
						lowTempRange.Value = rec.HighTemp.Value - rec.LowTemp.Value;
						lowTempRange.Ts = rec.Timestamp;
					}
					// lo baro
					if (rec.LowPress.HasValue && rec.LowPress.Value < lowBaro.Value && rec.LowPressTime.HasValue)
					{
						lowBaro.Value = rec.LowPress.Value;
						lowBaro.Ts = rec.LowPressTime.Value;
					}
					// hi baro
					if (rec.HighPress.HasValue && rec.HighPress.Value > highBaro.Value && rec.HighPressTime.HasValue)
					{
						highBaro.Value = rec.HighPress.Value;
						highBaro.Ts = rec.HighPressTime.Value;
					}
					// hi rain rate
					if (rec.HighRainRate.HasValue && rec.HighRainRate.Value > highRainRate.Value && rec.HighRainRateTime.HasValue)
					{
						highRainRate.Value = rec.HighRainRate.Value;
						highRainRate.Ts = rec.HighRainRateTime.Value;
					}
					// hi rain day
					if (rec.TotalRain.HasValue)
					{
						if (rec.TotalRain.HasValue && rec.TotalRain.Value > highRainDay.Value)
						{
							highRainDay.Value = rec.TotalRain.Value;
							highRainDay.Ts = rec.Timestamp;
						}

						// monthly rain
						rainThisMonth += rec.TotalRain.Value;
					}
					// monthly rain
					if (rainThisMonth > highRainMonth.Value)
					{
						highRainMonth.Value = rainThisMonth;
						highRainMonth.Ts = thisDate;
					}

					// dry/wet period
					if (Convert.ToInt32(rec.TotalRain * 1000) >= rainThreshold)
					{
						if (isDryNow)
						{
							currentWetPeriod = 1;
							isDryNow = false;
							if (!(dryPeriod.Value == Cumulus.DefaultHiVal && currentDryPeriod == 0) && currentDryPeriod > dryPeriod.Value)
							{
								dryPeriod.Value = currentDryPeriod;
								dryPeriod.Ts = thisDateDry;
							}
							currentDryPeriod = 0;
						}
						else
						{
							currentWetPeriod++;
							thisDateWet = rec.Timestamp;
						}
					}
					else
					{
						if (isDryNow)
						{
							currentDryPeriod++;
							thisDateDry = rec.Timestamp;
						}
						else
						{
							currentDryPeriod = 1;
							isDryNow = true;
							if (!(wetPeriod.Value == Cumulus.DefaultHiVal && currentWetPeriod == 0) && currentWetPeriod > wetPeriod.Value)
							{
								wetPeriod.Value = currentWetPeriod;
								wetPeriod.Ts = thisDateWet;
							}
							currentWetPeriod = 0;
						}
					}

					// hi wind run
					if (rec.WindRun.HasValue && rec.WindRun.Value > highWindRun.Value)
					{
						highWindRun.Value = rec.WindRun.Value;
						highWindRun.Ts= rec.Timestamp;
					}
					// hi wind
					if (rec.HighAvgWind.HasValue && rec.HighAvgWind.Value > highWind.Value && rec.HighAvgWindTime.HasValue)
					{
						highWind.Value = rec.HighAvgWind.Value;
						highWind.Ts= rec.HighAvgWindTime.Value;
					}
					// lo humidity
					if (rec.LowHumidity.HasValue && rec.LowHumidity.Value < lowHum.Value && rec.LowHumidityTime.HasValue)
					{
						lowHum.Value = rec.LowHumidity.Value;
						lowHum.Ts = rec.LowHumidityTime.Value;
					}
					// hi humidity
					if (rec.HighHumidity.HasValue && rec.HighHumidity > highHum.Value && rec.HighHumidityTime.HasValue)
					{
						highHum.Value = rec.HighHumidity.Value;
						highHum.Ts = rec.HighHumidityTime.Value;
					}
					// hi heat index
					if (rec.HighHeatIndex.HasValue && rec.HighHeatIndex.Value > highHeatInd.Value && rec.HighHeatIndexTime.HasValue)
					{
						highHeatInd.Value = rec.HighHeatIndex.Value;
						highHeatInd.Ts = rec.HighHeatIndexTime.Value;
					}
					// hi app temp
					if (rec.HighAppTemp.HasValue && rec.HighAppTemp.Value > highAppTemp.Value && rec.HighAppTempTime.HasValue)
					{
						highAppTemp.Value = rec.HighAppTemp.Value;
						highAppTemp.Ts = rec.HighAppTempTime.Value;
					}
					// lo app temp
					if (rec.LowAppTemp.HasValue && rec.LowAppTemp < lowAppTemp.Value && rec.LowAppTempTime.HasValue)
					{
						lowAppTemp.Value = rec.LowAppTemp.Value;
						lowAppTemp.Ts = rec.LowAppTempTime.Value;
					}
					// hi rain hour
					if (rec.HighHourlyRain.HasValue && rec.HighHourlyRain.Value > highRainHour.Value && rec.HighHourlyRainTime.HasValue)
					{
						highRainHour.Value = rec.HighHourlyRain.Value;
						highRainHour.Ts = rec.HighHourlyRainTime.Value;
					}
					// lo wind chill
					if (rec.LowWindChill.HasValue && rec.LowWindChill.Value < lowWindChill.Value && rec.LowWindChillTime.HasValue)
					{
						lowWindChill.Value = rec.LowWindChill.Value;
						lowWindChill.Ts = rec.LowWindChillTime.Value;
					}
					// hi dewpt
					if (rec.HighDewPoint.HasValue && rec.HighDewPoint.Value > highDewPt.Value && rec.HighDewPointTime.HasValue)
					{
						highDewPt.Value = rec.HighDewPoint.Value;
						highDewPt.Ts = rec.HighDewPointTime.Value;
					}
					// lo dewpt
					if (rec.LowDewPoint.HasValue && rec.LowDewPoint.Value < lowDewPt.Value && rec.LowDewPointTime.HasValue)
					{
						lowDewPt.Value = rec.LowDewPoint.Value;
						lowDewPt.Ts = rec.LowDewPointTime.Value;
					}
					// hi feels like
					if (rec.HighFeelsLike.HasValue && rec.HighFeelsLike.Value > highFeelsLike.Value && rec.HighFeelsLikeTime.HasValue)
					{
						highFeelsLike.Value = rec.HighFeelsLike.Value;
						highFeelsLike.Ts = rec.HighFeelsLikeTime.Value;
					}
					// lo feels like
					if (rec.LowFeelsLike.HasValue && rec.LowFeelsLike < lowFeelsLike.Value && rec.LowFeelsLikeTime.HasValue)
					{
						lowFeelsLike.Value = rec.LowFeelsLike.Value;
						lowFeelsLike.Ts = rec.LowFeelsLikeTime.Value;
					}
					// hi humidex
					if (rec.HighHumidex.HasValue && rec.HighHumidex.Value > highHumidex.Value && rec.HighHumidexTime.HasValue)
					{
						highHumidex.Value = rec.HighHumidex.Value;
						highHumidex.Ts = rec.HighHumidexTime.Value;
					}
				}

				// We need to check if the run or wet/dry days at the end of logs exceeds any records
				if (!(wetPeriod.Value == Cumulus.DefaultHiVal && currentWetPeriod == 0) && currentWetPeriod > wetPeriod.Value)
				{
					wetPeriod.Value = currentWetPeriod;
					wetPeriod.Ts = thisDateWet;
				}
				if (currentDryPeriod > dryPeriod.Value)
				{
					dryPeriod.Value = currentDryPeriod;
					dryPeriod.Ts = thisDateDry;
				}

				// need to do the final monthly rainfall
				if (!(dryPeriod.Value == Cumulus.DefaultHiVal && currentDryPeriod == 0) && currentDryPeriod > dryPeriod.Value)
				{
					highRainMonth.Value = rainThisMonth;
					highRainMonth.Ts = thisDate;
				}

				json.Append($"\"highTempValDayfile\":\"{highTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highTempTimeDayfile\":\"{highTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowTempValDayfile\":\"{lowTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"lowTempTimeDayfile\":\"{lowTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highDewPointValDayfile\":\"{highDewPt.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highDewPointTimeDayfile\":\"{highDewPt.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowDewPointValDayfile\":\"{lowDewPt.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"lowDewPointTimeDayfile\":\"{lowDewPt.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highApparentTempValDayfile\":\"{highAppTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highApparentTempTimeDayfile\":\"{highAppTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowApparentTempValDayfile\":\"{lowAppTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"lowApparentTempTimeDayfile\":\"{lowAppTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highFeelsLikeValDayfile\":\"{highFeelsLike.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highFeelsLikeTimeDayfile\":\"{highFeelsLike.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowFeelsLikeValDayfile\":\"{lowFeelsLike.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"lowFeelsLikeTimeDayfile\":\"{lowFeelsLike.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highHumidexValDayfile\":\"{highHumidex.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highHumidexTimeDayfile\":\"{highHumidex.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowWindChillValDayfile\":\"{lowWindChill.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"lowWindChillTimeDayfile\":\"{lowWindChill.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highHeatIndexValDayfile\":\"{highHeatInd.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highHeatIndexTimeDayfile\":\"{highHeatInd.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highMinTempValDayfile\":\"{highMinTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highMinTempTimeDayfile\":\"{highMinTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowMaxTempValDayfile\":\"{lowMaxTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"lowMaxTempTimeDayfile\":\"{lowMaxTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highDailyTempRangeValDayfile\":\"{highTempRange.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"highDailyTempRangeTimeDayfile\":\"{highTempRange.GetTsString(dateStampFormat)}\",");
				json.Append($"\"lowDailyTempRangeValDayfile\":\"{lowTempRange.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"lowDailyTempRangeTimeDayfile\":\"{lowTempRange.GetTsString(dateStampFormat)}\",");
				json.Append($"\"highHumidityValDayfile\":\"{highHum.GetValString(cumulus.HumFormat)}\",");
				json.Append($"\"highHumidityTimeDayfile\":\"{highHum.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowHumidityValDayfile\":\"{lowHum.GetValString(cumulus.HumFormat)}\",");
				json.Append($"\"lowHumidityTimeDayfile\":\"{lowHum.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highBarometerValDayfile\":\"{highBaro.GetValString(cumulus.PressFormat)}\",");
				json.Append($"\"highBarometerTimeDayfile\":\"{highBaro.GetTsString(timeStampFormat)}\",");
				json.Append($"\"lowBarometerValDayfile\":\"{lowBaro.GetValString(cumulus.PressFormat)}\",");
				json.Append($"\"lowBarometerTimeDayfile\":\"{lowBaro.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highGustValDayfile\":\"{highGust.GetValString(cumulus.WindFormat)}\",");
				json.Append($"\"highGustTimeDayfile\":\"{highGust.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highWindValDayfile\":\"{highWind.GetValString(cumulus.WindAvgFormat)}\",");
				json.Append($"\"highWindTimeDayfile\":\"{highWind.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highWindRunValDayfile\":\"{highWindRun.GetValString(cumulus.WindRunFormat)}\",");
				json.Append($"\"highWindRunTimeDayfile\":\"{highWindRun.GetTsString(dateStampFormat)}\",");
				json.Append($"\"highRainRateValDayfile\":\"{highRainRate.GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"highRainRateTimeDayfile\":\"{highRainRate.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highHourlyRainValDayfile\":\"{highRainHour.GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"highHourlyRainTimeDayfile\":\"{highRainHour.GetTsString(timeStampFormat)}\",");
				json.Append($"\"highDailyRainValDayfile\":\"{highRainDay.GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"highDailyRainTimeDayfile\":\"{highRainDay.GetTsString(dateStampFormat)}\",");
				if (recordType != "thismonth")
				{
					json.Append($"\"highMonthlyRainValDayfile\":\"{highRainMonth.GetValString(cumulus.RainFormat)}\",");
					json.Append($"\"highMonthlyRainTimeDayfile\":\"{highRainMonth.GetTsString("MM/yyyy")}\",");
				}
				json.Append($"\"longestDryPeriodValDayfile\":\"{dryPeriod.GetValString()}\",");
				json.Append($"\"longestDryPeriodTimeDayfile\":\"{dryPeriod.GetTsString(dateStampFormat)}\",");
				json.Append($"\"longestWetPeriodValDayfile\":\"{wetPeriod.GetValString()}\",");
				json.Append($"\"longestWetPeriodTimeDayfile\":\"{wetPeriod.GetTsString(dateStampFormat)}\"");
				json.Append('}');
			}
			else
			{
				Cumulus.LogMessage("GetRecordsDayFile: Error no day file records found");
			}

			return json.ToString();
		}

		internal string GetRecordsLogFile(string recordType)
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var json = new StringBuilder("{", 2048);
			DateTime datefrom;

			switch (recordType)
			{
				case "alltime":
					datefrom = cumulus.RecordsBeganDate;
					break;
				case "thisyear":
					var now = DateTime.Now;
					datefrom = new DateTime(now.Year, 1, 1);
					break;
				case "thismonth":
					now = DateTime.Now;
					datefrom = new DateTime(now.Year, now.Month, 1);
					break;
				default:
					datefrom = cumulus.RecordsBeganDate;
					break;
			}
			datefrom = datefrom.Date;
			var dateto = DateTime.Now.Date;
			var monthDate = datefrom;

			//var logFile = cumulus.GetLogFileName(filedate);
			var firstRow = true;
			var finished = false;
			var lastentrydate = datefrom;

			var isDryNow = false;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;

			int rainThreshold;
			if (cumulus.RainDayThreshold > 0)
			{
				rainThreshold = Convert.ToInt32(cumulus.RainDayThreshold * 1000);
			}
			else
			{
				// default
				if (cumulus.Units.Rain == 0)
				{
					rainThreshold = 200; // 0.2mm *1000
				}
				else
				{
					rainThreshold = 10;  // 0.01in *1000
				}
			}


			var highTemp = new LocalRec(true);
			var lowTemp = new LocalRec(false);
			var highDewPt = new LocalRec(true);
			var lowDewPt = new LocalRec(false);
			var highAppTemp = new LocalRec(true);
			var lowAppTemp = new LocalRec(false);
			var highFeelsLike = new LocalRec(true);
			var lowFeelsLike = new LocalRec(false);
			var highHumidex = new LocalRec(true);
			var lowWindChill = new LocalRec(false);
			var highHeatInd = new LocalRec(true);
			var highMinTemp = new LocalRec(true);
			var lowMaxTemp = new LocalRec(false);
			var highTempRange = new LocalRec(true);
			var lowTempRange = new LocalRec(false);
			var highHum = new LocalRec(true);
			var lowHum = new LocalRec(false);
			var highBaro = new LocalRec(true);
			var lowBaro = new LocalRec(false);
			var highGust = new LocalRec(true);
			var highWind = new LocalRec(true);
			var highWindRun = new LocalRec(true);
			var highRainRate = new LocalRec(true);
			var highRainHour = new LocalRec(true);
			var highRainDay = new LocalRec(true);
			var highRainMonth = new LocalRec(true);
			var dryPeriod = new LocalRec(true);
			var wetPeriod = new LocalRec(true);

			var currentDay = datefrom;
			var dayHighTemp = new LocalRec(true);
			var dayLowTemp = new LocalRec(false);
			double dayWindRun = Cumulus.DefaultHiVal;
			double dayRain = Cumulus.DefaultHiVal;


			var thisDateDry = DateTime.MinValue;
			var thisDateWet = DateTime.MinValue;

			var monthlyRain = 0.0;
			var totalRainfall = 0.0;

			var watch = System.Diagnostics.Stopwatch.StartNew();

			hourRainLog.Clear();

			while (!finished)
			{
				cumulus.LogDebugMessage($"GetAllTimeRecLogFile: Processing month - {monthDate:yyyy-MM}");
				var linenum = 0;

				try
				{
					var rows = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >= ? and Timestamp < ?", monthDate, monthDate.AddMonths(1));

					foreach (var rec in rows)
					{
						// process each record in the file
						linenum++;

						//var rec = station.ParseLogFileRec(line, true);

						// We need to work in meteo dates not clock dates for day hi/lows
						var metoDate = rec.Timestamp.AddHours(cumulus.GetHourInc());

						if (firstRow)
						{
							lastentrydate = rec.Timestamp;
							currentDay = metoDate;
							firstRow = false;
						}

						// low chill
						if (rec.WindChill.HasValue && rec.WindChill.Value < lowWindChill.Value)
						{
							lowWindChill.Value = rec.WindChill.Value;
							lowWindChill.Ts = rec.Timestamp;
						}
						// hi heat
						if (rec.HeatIndex.HasValue && rec.HeatIndex.Value > highHeatInd.Value)
						{
							highHeatInd.Value = rec.HeatIndex.Value;
							highHeatInd.Ts = rec.Timestamp;
						}
						// hi/low appt
						if (rec.Apparent.HasValue)
						{
							if (rec.Apparent.Value > highAppTemp.Value)
							{
								highAppTemp.Value = rec.Apparent.Value;
								highAppTemp.Ts = rec.Timestamp;
							}
							if (rec.Apparent.Value < lowAppTemp.Value)
							{
								lowAppTemp.Value = rec.Apparent.Value;
								lowAppTemp.Ts = rec.Timestamp;
							}
						}
						// hi/low feels like
						if (rec.FeelsLike.HasValue)
						{
							if (rec.FeelsLike.Value > highFeelsLike.Value)
							{
								highFeelsLike.Value = rec.FeelsLike.Value;
								highFeelsLike.Ts = rec.Timestamp;
							}
							if (rec.FeelsLike.Value < lowFeelsLike.Value)
							{
								lowFeelsLike.Value = rec.FeelsLike.Value;
								lowFeelsLike.Ts = rec.Timestamp;
							}
						}

						// hi/low humidex
						if (rec.Humidex.HasValue)
						{
							if (rec.Humidex.Value > highHumidex.Value)
							{
								highHumidex.Value = rec.Humidex.Value;
								highHumidex.Ts = rec.Timestamp;
							}
						}

						// hi/low temp
						if (rec.Temp.HasValue)
						{
							if (rec.Temp.Value > highTemp.Value)
							{
								highTemp.Value = rec.Temp.Value;
								highTemp.Ts = rec.Timestamp;
							}
							// lo temp
							if (rec.Temp.Value < lowTemp.Value)
							{
								lowTemp.Value = rec.Temp.Value;
								lowTemp.Ts = rec.Timestamp;
							}
						}
						// hi/low dewpoint
						if (rec.DewPoint.HasValue)
						{
							if (rec.DewPoint.Value > highDewPt.Value)
							{
								highDewPt.Value = rec.DewPoint.Value;
								highDewPt.Ts = rec.Timestamp;
							}
							// low dewpoint
							if (rec.DewPoint.Value < lowDewPt.Value)
							{
								lowDewPt.Value = rec.DewPoint.Value;
								lowDewPt.Ts = rec.Timestamp;
							}
						}
						// hi/low hum
						if (rec.Humidity.HasValue)
						{
							if (rec.Humidity.Value > highHum.Value)
							{
								highHum.Value = rec.Humidity.Value;
								highHum.Ts = rec.Timestamp;
							}
							// lo hum
							if (rec.Humidity.Value < lowHum.Value)
							{
								lowHum.Value = rec.Humidity.Value;
								lowHum.Ts = rec.Timestamp;
							}
						}
						// hi/Low baro
						if (rec.Pressure.HasValue)
						{
							if (rec.Pressure.Value > highBaro.Value)
							{
								highBaro.Value = rec.Pressure.Value;
								highBaro.Ts = rec.Timestamp;
							}
							// lo hum
							if (rec.Pressure < lowBaro.Value)
							{
								lowBaro.Value = rec.Pressure.Value;
								lowBaro.Ts = rec.Timestamp;
							}
						}
						// hi gust
						if (rec.WindGust10m.HasValue && rec.WindGust10m.Value > highGust.Value)
						{
							highGust.Value = rec.WindGust10m.Value;
							highGust.Ts = rec.Timestamp;
						}
						// hi wind
						if (rec.WindAvg.HasValue && rec.WindAvg.Value > highWind.Value)
						{
							highWind.Value = rec.WindAvg.Value;
							highWind.Ts = rec.Timestamp;
						}
						// hi rain rate
						if (rec.RainRate.HasValue && rec.RainRate.Value > highRainRate.Value)
						{
							highRainRate.Value = rec.RainRate.Value;
							highRainRate.Ts = rec.Timestamp;
						}

						if (rec.Temp.HasValue)
						{
							if (rec.Temp.Value > dayHighTemp.Value)
							{
								dayHighTemp.Value = rec.Temp.Value;
								dayHighTemp.Ts = rec.Timestamp;
							}

							if (rec.Temp.Value < dayLowTemp.Value)
							{
								dayLowTemp.Value = rec.Temp.Value;
								dayLowTemp.Ts = rec.Timestamp;
							}
						}

						// new meteo day
						if (currentDay.Date != metoDate.Date)
						{
							if (dayHighTemp.Value < lowMaxTemp.Value)
							{
								lowMaxTemp.Value = dayHighTemp.Value;
								lowMaxTemp.Ts = dayHighTemp.Ts;
							}
							if (dayLowTemp.Value > highMinTemp.Value)
							{
								highMinTemp.Value = dayLowTemp.Value;
								highMinTemp.Ts = dayLowTemp.Ts;
							}
							if (dayHighTemp.Value - dayLowTemp.Value > highTempRange.Value)
							{
								highTempRange.Value = dayHighTemp.Value - dayLowTemp.Value;
								highTempRange.Ts = currentDay;
							}
							if (dayHighTemp.Value - dayLowTemp.Value < lowTempRange.Value)
							{
								lowTempRange.Value = dayHighTemp.Value - dayLowTemp.Value;
								lowTempRange.Ts = currentDay;
							}

							monthlyRain += dayRain;

							if (monthlyRain > highRainMonth.Value)
							{
								highRainMonth.Value = monthlyRain;
								highRainMonth.Ts = currentDay;
							}

							if (currentDay.Month != metoDate.Month)
							{
								monthlyRain = 0;
							}

							// dry/wet period
							if (Convert.ToInt32(dayRain * 1000) >= rainThreshold)
							{
								if (isDryNow)
								{
									currentWetPeriod = 1;
									isDryNow = false;
									if (!(dryPeriod.Value == Cumulus.DefaultHiVal && currentDryPeriod == 0) && currentDryPeriod > dryPeriod.Value)
									{
										dryPeriod.Value = currentDryPeriod;
										dryPeriod.Ts = thisDateDry;
									}
									currentDryPeriod = 0;
								}
								else
								{
									currentWetPeriod++;
									thisDateWet = currentDay;
								}
							}
							else
							{
								if (isDryNow)
								{
									currentDryPeriod++;
									thisDateDry = currentDay;
								}
								else
								{
									currentDryPeriod = 1;
									isDryNow = true;
									if (!(wetPeriod.Value == Cumulus.DefaultHiVal && currentWetPeriod == 0) && currentWetPeriod > wetPeriod.Value)
									{
										wetPeriod.Value = currentWetPeriod;
										wetPeriod.Ts = thisDateWet;
									}
									currentWetPeriod = 0;
								}
							}

							currentDay = metoDate;
							dayHighTemp.Value = rec.Temp.Value;
							dayLowTemp.Value = rec.Temp.Value;
							dayWindRun = 0;
							totalRainfall += dayRain;
							dayRain = 0;
						}

						if (rec.RainToday.HasValue && dayRain < rec.RainToday.Value)
						{
							dayRain = rec.RainToday.Value;
						}

						if (dayRain > highRainDay.Value)
						{
							highRainDay.Value = dayRain;
							highRainDay.Ts = currentDay;
						}

						if (rec.WindAvg.HasValue)
						{
							dayWindRun += rec.Timestamp.Subtract(lastentrydate).TotalHours * rec.WindAvg.Value;
						}

						if (dayWindRun > highWindRun.Value)
						{
							highWindRun.Value = dayWindRun;
							highWindRun.Ts = currentDay;
						}

						// hourly rain
						/*
						* need to track what the rainfall has been in the last rolling hour
						* across day rollovers where the count resets
						*/
						AddLastHourRainEntry(rec.Timestamp, totalRainfall + dayRain);
						RemoveOldRainData(rec.Timestamp);

						var rainThisHour = hourRainLog.Last().Raincounter - hourRainLog.First().Raincounter;
						if (rainThisHour > highRainHour.Value)
						{
							highRainHour.Value = rainThisHour;
							highRainHour.Ts = rec.Timestamp;
						}

						lastentrydate = rec.Timestamp;
						//lastRainMidnight = rainMidnight;
						}
				}
				catch (Exception e)
				{
					cumulus.LogExceptionMessage(e, $"GetRecordsLogFile: Error");
				}

				if (monthDate >= dateto)
				{
					finished = true;
				}
				else
				{
					cumulus.LogDebugMessage($"GetAllTimeRecLogFile: Finished processing month - {monthDate:yyyy-MM}");
					monthDate = monthDate.AddMonths(1);
					//logFile = cumulus.GetLogFileName(filedate);
				}
			}

			// We need to check if the run or wet/dry days at the end of logs exceeds any records
			if (!(wetPeriod.Value == Cumulus.DefaultHiVal && currentWetPeriod == 0) && currentWetPeriod > wetPeriod.Value)
			{
				wetPeriod.Value = currentWetPeriod;
				wetPeriod.Ts = currentDay;
			}
			if (!(dryPeriod.Value == Cumulus.DefaultHiVal && currentDryPeriod == 0) && currentDryPeriod > dryPeriod.Value)
			{
				dryPeriod.Value = currentDryPeriod;
				dryPeriod.Ts = currentDay;
			}

			cumulus.LogDebugMessage("GetAllTimeRecLogFile: Finished all processing");

			json.Append($"\"highTempValLogfile\":\"{highTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highTempTimeLogfile\":\"{highTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowTempValLogfile\":\"{lowTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempTimeLogfile\":\"{lowTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDewPointValLogfile\":\"{highDewPt.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointTimeLogfile\":\"{highDewPt.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointValLogfile\":\"{lowDewPt.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointTimeLogfile\":\"{lowDewPt.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempValLogfile\":\"{highAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempTimeLogfile\":\"{highAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempValLogfile\":\"{lowAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempTimeLogfile\":\"{lowAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeValLogfile\":\"{highFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeTimeLogfile\":\"{highFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeValLogfile\":\"{lowFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeTimeLogfile\":\"{lowFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHumidexValLogfile\":\"{highHumidex.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexTimeLogfile\":\"{highHumidex.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillValLogfile\":\"{lowWindChill.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillTimeLogfile\":\"{lowWindChill.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexValLogfile\":\"{highHeatInd.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexTimeLogfile\":\"{highHeatInd.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highMinTempValLogfile\":\"{highMinTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempTimeLogfile\":\"{highMinTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempValLogfile\":\"{lowMaxTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempTimeLogfile\":\"{lowMaxTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeValLogfile\":\"{highTempRange.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeTimeLogfile\":\"{highTempRange.GetTsString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeValLogfile\":\"{lowTempRange.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeTimeLogfile\":\"{lowTempRange.GetTsString(dateStampFormat)}\",");
			json.Append($"\"highHumidityValLogfile\":\"{highHum.GetValString(cumulus.HumFormat)}\",");
			json.Append($"\"highHumidityTimeLogfile\":\"{highHum.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityValLogfile\":\"{lowHum.GetValString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityTimeLogfile\":\"{lowHum.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highBarometerValLogfile\":\"{highBaro.GetValString(cumulus.PressFormat)}\",");
			json.Append($"\"highBarometerTimeLogfile\":\"{highBaro.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerValLogfile\":\"{lowBaro.GetValString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerTimeLogfile\":\"{lowBaro.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highGustValLogfile\":\"{highGust.GetValString(cumulus.WindFormat)}\",");
			json.Append($"\"highGustTimeLogfile\":\"{highGust.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindValLogfile\":\"{highWind.GetValString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindTimeLogfile\":\"{highWind.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindRunValLogfile\":\"{highWindRun.GetValString(cumulus.WindRunFormat)}\",");
			json.Append($"\"highWindRunTimeLogfile\":\"{highWindRun.GetTsString(dateStampFormat)}\",");
			json.Append($"\"highRainRateValLogfile\":\"{highRainRate.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highRainRateTimeLogfile\":\"{highRainRate.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainValLogfile\":\"{highRainHour.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainTimeLogfile\":\"{highRainHour.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainValLogfile\":\"{highRainDay.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainTimeLogfile\":\"{highRainDay.GetTsString(dateStampFormat)}\",");
			json.Append($"\"highMonthlyRainValLogfile\":\"{highRainMonth.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highMonthlyRainTimeLogfile\":\"{highRainMonth.GetTsString("MM/yyyy")}\",");
			if (recordType == "alltime")
			{
				json.Append($"\"longestDryPeriodValLogfile\":\"{dryPeriod.GetValString()}\",");
				json.Append($"\"longestDryPeriodTimeLogfile\":\"{dryPeriod.GetTsString(dateStampFormat)}\",");
				json.Append($"\"longestWetPeriodValLogfile\":\"{wetPeriod.GetValString()}\",");
				json.Append($"\"longestWetPeriodTimeLogfile\":\"{wetPeriod.GetTsString(dateStampFormat)}\"");
			}
			else
			{
				json.Append($"\"longestDryPeriodValLogfile\":\"{dryPeriod.GetValString()}\",");
				json.Append($"\"longestDryPeriodTimeLogfile\":\"{dryPeriod.GetTsString(dateStampFormat)}\",");
				json.Append($"\"longestWetPeriodValLogfile\":\"{wetPeriod.GetValString()}\",");
				json.Append($"\"longestWetPeriodTimeLogfile\":\"{wetPeriod.GetTsString(dateStampFormat)}\"");
			}
			json.Append('}');

			watch.Stop();
			var elapsed = watch.ElapsedMilliseconds;
			cumulus.LogDebugMessage($"GetRecordsLogFile: Logfiles parse = {elapsed} ms");

			return json.ToString();
		}


		internal string EditAllTimeRecs(IHttpContext context)
		{
			var request = context.Request;
			string text;

			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = Uri.UnescapeDataString(reader.ReadToEnd());
			}
			// Eg "name=highTempValvalue=134.6&pk=1"
			var newData = text.Split('&');
			var field = newData[0].Split('=')[1];
			var value = newData[1].Split('=')[1];
			var result = 1;
			try
			{
				switch (field)
				{
					case "highTempVal":
						station.SetAlltime(station.AllTime.HighTemp, double.Parse(value), station.AllTime.HighTemp.Ts);
						break;
					case "highTempTime":
						station.SetAlltime(station.AllTime.HighTemp, station.AllTime.HighTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowTempVal":
						station.SetAlltime(station.AllTime.LowTemp, double.Parse(value), station.AllTime.LowTemp.Ts);
						break;
					case "lowTempTime":
						station.SetAlltime(station.AllTime.LowTemp, station.AllTime.LowTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highDewPointVal":
						station.SetAlltime(station.AllTime.HighDewPoint, double.Parse(value), station.AllTime.HighDewPoint.Ts);
						break;
					case "highDewPointTime":
						station.SetAlltime(station.AllTime.HighDewPoint, station.AllTime.HighDewPoint.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowDewPointVal":
						station.SetAlltime(station.AllTime.LowDewPoint, double.Parse(value), station.AllTime.LowDewPoint.Ts);
						break;
					case "lowDewPointTime":
						station.SetAlltime(station.AllTime.LowDewPoint, station.AllTime.LowDewPoint.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highApparentTempVal":
						station.SetAlltime(station.AllTime.HighAppTemp, double.Parse(value), station.AllTime.HighAppTemp.Ts);
						break;
					case "highApparentTempTime":
						station.SetAlltime(station.AllTime.HighAppTemp, station.AllTime.HighAppTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowApparentTempVal":
						station.SetAlltime(station.AllTime.LowAppTemp, double.Parse(value), station.AllTime.LowAppTemp.Ts);
						break;
					case "lowApparentTempTime":
						station.SetAlltime(station.AllTime.LowAppTemp, station.AllTime.LowAppTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highFeelsLikeVal":
						station.SetAlltime(station.AllTime.HighFeelsLike, double.Parse(value), station.AllTime.HighFeelsLike.Ts);
						break;
					case "highFeelsLikeTime":
						station.SetAlltime(station.AllTime.HighFeelsLike, station.AllTime.HighFeelsLike.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowFeelsLikeVal":
						station.SetAlltime(station.AllTime.LowFeelsLike, double.Parse(value), station.AllTime.LowFeelsLike.Ts);
						break;
					case "lowFeelsLikeTime":
						station.SetAlltime(station.AllTime.LowFeelsLike, station.AllTime.LowFeelsLike.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highHumidexVal":
						station.SetAlltime(station.AllTime.HighHumidex, double.Parse(value), station.AllTime.HighHumidex.Ts);
						break;
					case "highHumidexTime":
						station.SetAlltime(station.AllTime.HighHumidex, station.AllTime.HighHumidex.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowWindChillVal":
						station.SetAlltime(station.AllTime.LowChill, double.Parse(value), station.AllTime.LowChill.Ts);
						break;
					case "lowWindChillTime":
						station.SetAlltime(station.AllTime.LowChill, station.AllTime.LowChill.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highHeatIndexVal":
						station.SetAlltime(station.AllTime.HighHeatIndex, double.Parse(value), station.AllTime.HighHeatIndex.Ts);
						break;
					case "highHeatIndexTime":
						station.SetAlltime(station.AllTime.HighHeatIndex, station.AllTime.HighHeatIndex.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highMinTempVal":
						station.SetAlltime(station.AllTime.HighMinTemp, double.Parse(value), station.AllTime.HighMinTemp.Ts);
						break;
					case "highMinTempTime":
						station.SetAlltime(station.AllTime.HighMinTemp, station.AllTime.HighMinTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowMaxTempVal":
						station.SetAlltime(station.AllTime.LowMaxTemp, double.Parse(value), station.AllTime.LowMaxTemp.Ts);
						break;
					case "lowMaxTempTime":
						station.SetAlltime(station.AllTime.LowMaxTemp, station.AllTime.LowMaxTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highDailyTempRangeVal":
						station.SetAlltime(station.AllTime.HighDailyTempRange, double.Parse(value), station.AllTime.HighDailyTempRange.Ts);
						break;
					case "highDailyTempRangeTime":
						station.SetAlltime(station.AllTime.HighDailyTempRange, station.AllTime.HighDailyTempRange.Val, Utils.ddmmyyyyStrToDate(value));
						break;
					case "lowDailyTempRangeVal":
						station.SetAlltime(station.AllTime.LowDailyTempRange, double.Parse(value), station.AllTime.LowDailyTempRange.Ts);
						break;
					case "lowDailyTempRangeTime":
						station.SetAlltime(station.AllTime.LowDailyTempRange, station.AllTime.LowDailyTempRange.Val, Utils.ddmmyyyyStrToDate(value));
						break;
					case "highHumidityVal":
						station.SetAlltime(station.AllTime.HighHumidity, double.Parse(value), station.AllTime.HighHumidity.Ts);
						break;
					case "highHumidityTime":
						station.SetAlltime(station.AllTime.HighHumidity, station.AllTime.HighHumidity.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowHumidityVal":
						station.SetAlltime(station.AllTime.LowHumidity, double.Parse(value), station.AllTime.LowHumidity.Ts);
						break;
					case "lowHumidityTime":
						station.SetAlltime(station.AllTime.LowHumidity, station.AllTime.LowHumidity.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highBarometerVal":
						station.SetAlltime(station.AllTime.HighPress, double.Parse(value), station.AllTime.HighPress.Ts);
						break;
					case "highBarometerTime":
						station.SetAlltime(station.AllTime.HighPress, station.AllTime.HighPress.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "lowBarometerVal":
						station.SetAlltime(station.AllTime.LowPress, double.Parse(value), station.AllTime.LowPress.Ts);
						break;
					case "lowBarometerTime":
						station.SetAlltime(station.AllTime.LowPress, station.AllTime.LowPress.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highGustVal":
						station.SetAlltime(station.AllTime.HighGust, double.Parse(value), station.AllTime.HighGust.Ts);
						break;
					case "highGustTime":
						station.SetAlltime(station.AllTime.HighGust, station.AllTime.HighGust.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highWindVal":
						station.SetAlltime(station.AllTime.HighWind, double.Parse(value), station.AllTime.HighWind.Ts);
						break;
					case "highWindTime":
						station.SetAlltime(station.AllTime.HighWind, station.AllTime.HighWind.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highWindRunVal":
						station.SetAlltime(station.AllTime.HighWindRun, double.Parse(value), station.AllTime.HighWindRun.Ts);
						break;
					case "highWindRunTime":
						station.SetAlltime(station.AllTime.HighWindRun, station.AllTime.HighWindRun.Val, Utils.ddmmyyyyStrToDate(value));
						break;
					case "highRainRateVal":
						station.SetAlltime(station.AllTime.HighRainRate, double.Parse(value), station.AllTime.HighRainRate.Ts);
						break;
					case "highRainRateTime":
						station.SetAlltime(station.AllTime.HighRainRate, station.AllTime.HighRainRate.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highHourlyRainVal":
						station.SetAlltime(station.AllTime.HourlyRain, double.Parse(value), station.AllTime.HourlyRain.Ts);
						break;
					case "highHourlyRainTime":
						station.SetAlltime(station.AllTime.HourlyRain, station.AllTime.HourlyRain.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
						break;
					case "highDailyRainVal":
						station.SetAlltime(station.AllTime.DailyRain, double.Parse(value), station.AllTime.DailyRain.Ts);
						break;
					case "highDailyRainTime":
						station.SetAlltime(station.AllTime.DailyRain, station.AllTime.DailyRain.Val, Utils.ddmmyyyyStrToDate(value));
						break;
					case "highMonthlyRainVal":
						station.SetAlltime(station.AllTime.MonthlyRain, double.Parse(value), station.AllTime.MonthlyRain.Ts);
						break;
					case "highMonthlyRainTime":
						// MM/yyy
						var datstr = "01/" + value;
						station.SetAlltime(station.AllTime.MonthlyRain, station.AllTime.MonthlyRain.Val, Utils.ddmmyyyyStrToDate(datstr));
						break;
					case "longestDryPeriodVal":
						station.SetAlltime(station.AllTime.LongestDryPeriod, double.Parse(value), station.AllTime.LongestDryPeriod.Ts);
						break;
					case "longestDryPeriodTime":
						station.SetAlltime(station.AllTime.LongestDryPeriod, station.AllTime.LongestDryPeriod.Val, Utils.ddmmyyyyStrToDate(value));
						break;
					case "longestWetPeriodVal":
						station.SetAlltime(station.AllTime.LongestWetPeriod, double.Parse(value), station.AllTime.LongestWetPeriod.Ts);
						break;
					case "longestWetPeriodTime":
						station.SetAlltime(station.AllTime.LongestWetPeriod, station.AllTime.LongestWetPeriod.Val, Utils.ddmmyyyyStrToDate(value));
						break;
					default:
						result = 0;
						break;
				}
			}
			catch
			{
				result = 0;
			}
			return "{\"result\":\"" + ((result == 1) ? "Success" : "Failed") + "\"}";
		}

		internal string EditMonthlyRecs(IHttpContext context)
		{
			var request = context.Request;
			string text;

			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = Uri.UnescapeDataString(reader.ReadToEnd());
			}
			// Eg "name=2-highTempValvalue=134.6&pk=1"
			var newData = text.Split('&');
			var monthField = newData[0].Split('=')[1].Split('-');
			var month = int.Parse(monthField[0]);
			var field = monthField[1];
			var value = newData[1].Split('=')[1];
			var result = 1;
			try
			{
				lock (station.monthlyalltimeIniThreadLock)
				{
					string[] dt;
					switch (field)
					{
						case "highTempVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighTemp, double.Parse(value), station.MonthlyRecs[month].HighTemp.Ts);
							break;
						case "highTempTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighTemp, station.MonthlyRecs[month].HighTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowTempVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowTemp, double.Parse(value), station.MonthlyRecs[month].LowTemp.Ts);
							break;
						case "lowTempTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowTemp, station.MonthlyRecs[month].LowTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highDewPointVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighDewPoint, double.Parse(value), station.MonthlyRecs[month].HighDewPoint.Ts);
							break;
						case "highDewPointTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighDewPoint, station.MonthlyRecs[month].HighDewPoint.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowDewPointVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowDewPoint, double.Parse(value), station.MonthlyRecs[month].LowDewPoint.Ts);
							break;
						case "lowDewPointTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowDewPoint, station.MonthlyRecs[month].LowDewPoint.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highApparentTempVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighAppTemp, double.Parse(value), station.MonthlyRecs[month].HighAppTemp.Ts);
							break;
						case "highApparentTempTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighAppTemp, station.MonthlyRecs[month].HighAppTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowApparentTempVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowAppTemp, double.Parse(value), station.MonthlyRecs[month].LowAppTemp.Ts);
							break;
						case "lowApparentTempTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowAppTemp, station.MonthlyRecs[month].LowAppTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highFeelsLikeVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighFeelsLike, double.Parse(value), station.MonthlyRecs[month].HighFeelsLike.Ts);
							break;
						case "highFeelsLikeTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighFeelsLike, station.MonthlyRecs[month].HighFeelsLike.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowFeelsLikeVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowFeelsLike, double.Parse(value), station.MonthlyRecs[month].LowFeelsLike.Ts);
							break;
						case "lowFeelsLikeTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowFeelsLike, station.MonthlyRecs[month].LowFeelsLike.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highHumidexVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHumidex, double.Parse(value), station.MonthlyRecs[month].HighHumidex.Ts);
							break;
						case "highHumidexTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHumidex, station.MonthlyRecs[month].HighHumidex.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowWindChillVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowChill, double.Parse(value), station.MonthlyRecs[month].LowChill.Ts);
							break;
						case "lowWindChillTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowChill, station.MonthlyRecs[month].LowChill.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highHeatIndexVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHeatIndex, double.Parse(value), station.MonthlyRecs[month].HighHeatIndex.Ts);
							break;
						case "highHeatIndexTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHeatIndex, station.MonthlyRecs[month].HighHeatIndex.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highMinTempVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighMinTemp, double.Parse(value), station.MonthlyRecs[month].HighMinTemp.Ts);
							break;
						case "highMinTempTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighMinTemp, station.MonthlyRecs[month].HighMinTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowMaxTempVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowMaxTemp, double.Parse(value), station.MonthlyRecs[month].LowMaxTemp.Ts);
							break;
						case "lowMaxTempTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowMaxTemp, station.MonthlyRecs[month].LowMaxTemp.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highDailyTempRangeVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighDailyTempRange, double.Parse(value), station.MonthlyRecs[month].HighDailyTempRange.Ts);
							break;
						case "highDailyTempRangeTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighDailyTempRange, station.MonthlyRecs[month].HighDailyTempRange.Val, Utils.ddmmyyyyStrToDate(value));
							break;
						case "lowDailyTempRangeVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowDailyTempRange, double.Parse(value), station.MonthlyRecs[month].LowDailyTempRange.Ts);
							break;
						case "lowDailyTempRangeTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowDailyTempRange, station.MonthlyRecs[month].LowDailyTempRange.Val, Utils.ddmmyyyyStrToDate(value));
							break;
						case "highHumidityVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHumidity, double.Parse(value), station.MonthlyRecs[month].HighHumidity.Ts);
							break;
						case "highHumidityTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHumidity, station.MonthlyRecs[month].HighHumidity.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowHumidityVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowHumidity, double.Parse(value), station.MonthlyRecs[month].LowHumidity.Ts);
							break;
						case "lowHumidityTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowHumidity, station.MonthlyRecs[month].LowHumidity.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highBarometerVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighPress, double.Parse(value), station.MonthlyRecs[month].HighPress.Ts);
							break;
						case "highBarometerTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighPress, station.MonthlyRecs[month].HighPress.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "lowBarometerVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowPress, double.Parse(value), station.MonthlyRecs[month].LowPress.Ts);
							break;
						case "lowBarometerTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowPress, station.MonthlyRecs[month].LowPress.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highGustVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighGust, double.Parse(value), station.MonthlyRecs[month].HighGust.Ts);
							break;
						case "highGustTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighGust, station.MonthlyRecs[month].HighGust.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highWindVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighWind, double.Parse(value), station.MonthlyRecs[month].HighWind.Ts);
							break;
						case "highWindTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighWind, station.MonthlyRecs[month].HighWind.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highWindRunVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighWindRun, double.Parse(value), station.MonthlyRecs[month].HighWindRun.Ts);
							break;
						case "highWindRunTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighWindRun, station.MonthlyRecs[month].HighWindRun.Val, Utils.ddmmyyyyStrToDate(value));
							break;
						case "highRainRateVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighRainRate, double.Parse(value), station.MonthlyRecs[month].HighRainRate.Ts);
							break;
						case "highRainRateTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighRainRate, station.MonthlyRecs[month].HighRainRate.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highHourlyRainVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HourlyRain, double.Parse(value), station.MonthlyRecs[month].HourlyRain.Ts);
							break;
						case "highHourlyRainTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HourlyRain, station.MonthlyRecs[month].HourlyRain.Val, Utils.ddmmyyyy_hhmmStrToDate(value));
							break;
						case "highDailyRainVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].DailyRain, double.Parse(value), station.MonthlyRecs[month].DailyRain.Ts);
							break;
						case "highDailyRainTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].DailyRain, station.MonthlyRecs[month].DailyRain.Val, Utils.ddmmyyyyStrToDate(value));
							break;
						case "highMonthlyRainVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].MonthlyRain, double.Parse(value), station.MonthlyRecs[month].MonthlyRain.Ts);
							break;
						case "highMonthlyRainTime":
							dt = value.Split('/');
							var datstr = string.Concat("01/", dt[0], "/", dt[1].AsSpan(2, 2));
							station.SetMonthlyAlltime(station.MonthlyRecs[month].MonthlyRain, station.MonthlyRecs[month].MonthlyRain.Val, Utils.ddmmyyyyStrToDate(datstr));
							break;
						case "longestDryPeriodVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LongestDryPeriod, double.Parse(value), station.MonthlyRecs[month].LongestDryPeriod.Ts);
							break;
						case "longestDryPeriodTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LongestDryPeriod, station.MonthlyRecs[month].LongestDryPeriod.Val, Utils.ddmmyyyyStrToDate(value));
							break;
						case "longestWetPeriodVal":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LongestWetPeriod, double.Parse(value), station.MonthlyRecs[month].LongestWetPeriod.Ts);
							break;
						case "longestWetPeriodTime":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LongestWetPeriod, station.MonthlyRecs[month].LongestWetPeriod.Val, Utils.ddmmyyyyStrToDate(value));
							break;
						default:
							result = 0;
							break;
					}
				}
			}
			catch
			{
				result = 0;
			}
			return "{\"result\":\"" + ((result == 1) ? "Success" : "Failed") + "\"}";
		}

		internal string GetMonthlyRecData()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var json = new StringBuilder("{", 21000);
			for (var m = 1; m <= 12; m++)
			{
				// Records - Temperature values
				json.Append($"\"{m}-highTempVal\":\"{station.MonthlyRecs[m].HighTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowTempVal\":\"{station.MonthlyRecs[m].LowTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDewPointVal\":\"{station.MonthlyRecs[m].HighDewPoint.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDewPointVal\":\"{station.MonthlyRecs[m].LowDewPoint.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highApparentTempVal\":\"{station.MonthlyRecs[m].HighAppTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowApparentTempVal\":\"{station.MonthlyRecs[m].LowAppTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeVal\":\"{station.MonthlyRecs[m].HighFeelsLike.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeVal\":\"{station.MonthlyRecs[m].LowFeelsLike.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHumidexVal\":\"{station.MonthlyRecs[m].HighHumidex.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowWindChillVal\":\"{station.MonthlyRecs[m].LowChill.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHeatIndexVal\":\"{station.MonthlyRecs[m].HighHeatIndex.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highMinTempVal\":\"{station.MonthlyRecs[m].HighMinTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowMaxTempVal\":\"{station.MonthlyRecs[m].LowMaxTemp.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeVal\":\"{station.MonthlyRecs[m].HighDailyTempRange.GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeVal\":\"{station.MonthlyRecs[m].LowDailyTempRange.GetValString(cumulus.TempFormat)}\",");
				// Records - Temperature timestamps
				json.Append($"\"{m}-highTempTime\":\"{station.MonthlyRecs[m].HighTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowTempTime\":\"{station.MonthlyRecs[m].LowTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDewPointTime\":\"{station.MonthlyRecs[m].HighDewPoint.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowDewPointTime\":\"{station.MonthlyRecs[m].LowDewPoint.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highApparentTempTime\":\"{station.MonthlyRecs[m].HighAppTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowApparentTempTime\":\"{station.MonthlyRecs[m].LowAppTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeTime\":\"{station.MonthlyRecs[m].HighFeelsLike.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeTime\":\"{station.MonthlyRecs[m].LowFeelsLike.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHumidexTime\":\"{station.MonthlyRecs[m].HighHumidex.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowWindChillTime\":\"{station.MonthlyRecs[m].LowChill.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHeatIndexTime\":\"{station.MonthlyRecs[m].HighHeatIndex.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highMinTempTime\":\"{station.MonthlyRecs[m].HighMinTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowMaxTempTime\":\"{station.MonthlyRecs[m].LowMaxTemp.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeTime\":\"{station.MonthlyRecs[m].HighDailyTempRange.GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeTime\":\"{station.MonthlyRecs[m].LowDailyTempRange.GetTsString(dateStampFormat)}\",");
				// Records - Humidity values
				json.Append($"\"{m}-highHumidityVal\":\"{station.MonthlyRecs[m].HighHumidity.GetValString(cumulus.HumFormat)}\",");
				json.Append($"\"{m}-lowHumidityVal\":\"{station.MonthlyRecs[m].LowHumidity.GetValString(cumulus.HumFormat)}\",");
				// Records - Humidity times
				json.Append($"\"{m}-highHumidityTime\":\"{station.MonthlyRecs[m].HighHumidity.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowHumidityTime\":\"{station.MonthlyRecs[m].LowHumidity.GetTsString(timeStampFormat)}\",");
				// Records - Pressure values
				json.Append($"\"{m}-highBarometerVal\":\"{station.MonthlyRecs[m].HighPress.GetValString(cumulus.PressFormat)}\",");
				json.Append($"\"{m}-lowBarometerVal\":\"{station.MonthlyRecs[m].LowPress.GetValString(cumulus.PressFormat)}\",");
				// Records - Pressure times
				json.Append($"\"{m}-highBarometerTime\":\"{station.MonthlyRecs[m].HighPress.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowBarometerTime\":\"{station.MonthlyRecs[m].LowPress.GetTsString(timeStampFormat)}\",");
				// Records - Wind values
				json.Append($"\"{m}-highGustVal\":\"{station.MonthlyRecs[m].HighGust.GetValString(cumulus.WindFormat)}\",");
				json.Append($"\"{m}-highWindVal\":\"{station.MonthlyRecs[m].HighWind.GetValString(cumulus.WindAvgFormat)}\",");
				json.Append($"\"{m}-highWindRunVal\":\"{station.MonthlyRecs[m].HighWindRun.GetValString(cumulus.WindRunFormat)}\",");
				// Records - Wind times
				json.Append($"\"{m}-highGustTime\":\"{station.MonthlyRecs[m].HighGust.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindTime\":\"{station.MonthlyRecs[m].HighWind.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindRunTime\":\"{station.MonthlyRecs[m].HighWindRun.GetTsString(dateStampFormat)}\",");
				// Records - Rain values
				json.Append($"\"{m}-highRainRateVal\":\"{station.MonthlyRecs[m].HighRainRate.GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highHourlyRainVal\":\"{station.MonthlyRecs[m].HourlyRain.GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highDailyRainVal\":\"{station.MonthlyRecs[m].DailyRain.GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainVal\":\"{station.MonthlyRecs[m].MonthlyRain.GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-longestDryPeriodVal\":\"{station.MonthlyRecs[m].LongestDryPeriod.GetValString("f0")}\",");
				json.Append($"\"{m}-longestWetPeriodVal\":\"{station.MonthlyRecs[m].LongestWetPeriod.GetValString("f0")}\",");
				// Records - Rain times
				json.Append($"\"{m}-highRainRateTime\":\"{station.MonthlyRecs[m].HighRainRate.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHourlyRainTime\":\"{station.MonthlyRecs[m].HourlyRain.GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyRainTime\":\"{station.MonthlyRecs[m].DailyRain.GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainTime\":\"{station.MonthlyRecs[m].MonthlyRain.GetTsString("MM/yyyy")}\",");
				json.Append($"\"{m}-longestDryPeriodTime\":\"{station.MonthlyRecs[m].LongestDryPeriod.GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-longestWetPeriodTime\":\"{station.MonthlyRecs[m].LongestWetPeriod.GetTsString(dateStampFormat)}\",");
			}
			json.Length--;
			json.Append('}');

			return json.ToString();
		}

		internal string GetMonthlyRecDayFile()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var highTemp = new LocalRec[12];
			var lowTemp = new LocalRec[12];
			var highDewPt = new LocalRec[12];
			var lowDewPt = new LocalRec[12];
			var highAppTemp = new LocalRec[12];
			var lowAppTemp = new LocalRec[12];
			var highFeelsLike = new LocalRec[12];
			var lowFeelsLike = new LocalRec[12];
			var highHumidex = new LocalRec[12];
			var lowWindChill = new LocalRec[12];
			var highHeatInd = new LocalRec[12];
			var highMinTemp = new LocalRec[12];
			var lowMaxTemp = new LocalRec[12];
			var highTempRange = new LocalRec[12];
			var lowTempRange = new LocalRec[12];
			var highHum = new LocalRec[12];
			var lowHum = new LocalRec[12];
			var highBaro = new LocalRec[12];
			var lowBaro = new LocalRec[12];
			var highGust = new LocalRec[12];
			var highWind = new LocalRec[12];
			var highWindRun = new LocalRec[12];
			var highRainRate = new LocalRec[12];
			var highRainHour = new LocalRec[12];
			var highRainDay = new LocalRec[12];
			var highRainMonth = new LocalRec[12];
			var dryPeriod = new LocalRec[12];
			var wetPeriod = new LocalRec[12];

			for (var i = 0; i < 12; i++)
			{
				highTemp[i] = new LocalRec(true);
				lowTemp[i] = new LocalRec(false);
				highDewPt[i] = new LocalRec(true);
				lowDewPt[i] = new LocalRec(false);
				highAppTemp[i] = new LocalRec(true);
				lowAppTemp[i] = new LocalRec(false);
				highFeelsLike[i] = new LocalRec(true);
				lowFeelsLike[i] = new LocalRec(false);
				highHumidex[i] = new LocalRec(true);
				lowWindChill[i] = new LocalRec(false);
				highHeatInd[i] = new LocalRec(true);
				highMinTemp[i] = new LocalRec(true);
				lowMaxTemp[i] = new LocalRec(false);
				highTempRange[i] = new LocalRec(true);
				lowTempRange[i] = new LocalRec(false);
				highHum[i] = new LocalRec(true);
				lowHum[i] = new LocalRec(false);
				highBaro[i] = new LocalRec(true);
				lowBaro[i] = new LocalRec(false);
				highGust[i] = new LocalRec(true);
				highWind[i] = new LocalRec(true);
				highWindRun[i] = new LocalRec(true);
				highRainRate[i] = new LocalRec(true);
				highRainHour[i] = new LocalRec(true);
				highRainDay[i] = new LocalRec(true);
				highRainMonth[i] = new LocalRec(true);
				dryPeriod[i] = new LocalRec(true);
				wetPeriod[i] = new LocalRec(true);
			}

			var thisDate = DateTime.MinValue;
			var rainThisMonth = 0.0;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;
			var isDryNow = false;
			var thisDateDry = DateTime.MinValue;
			var thisDateWet = DateTime.MinValue;
			var firstEntry = true;
			var json = new StringBuilder("{", 25500);

			int rainThreshold;
			if (cumulus.RainDayThreshold > 0)
			{
				rainThreshold = Convert.ToInt32(cumulus.RainDayThreshold * 1000);
			}
			else
			{
				// default
				if (cumulus.Units.Rain == 0)
				{
					rainThreshold = 200; // 0.2mm *1000
				}
				else
				{
					rainThreshold = 10;  // 0.01in *1000
				}
			}

			// get all the data from the database
			var data = station.Database.Query<DayData>("select * from DayData order by Timestamp");

			if (data.Count > 0)
			{
				for (var i = 0; i < data.Count; i++)
				{
					var loggedDate = data[i].Timestamp;
					var monthOffset = loggedDate.Month - 1;

					// for the very first record we need to record the date
					if (firstEntry)
					{
						thisDate = loggedDate;
						firstEntry = false;
						thisDateDry = loggedDate;
						thisDateWet = loggedDate;
					}
					else
					{
						if (thisDate.Month != loggedDate.Month)
						{
							// reset the date and counter for a new month
							rainThisMonth = 0.0;
							thisDate = loggedDate;
						}
					}
					// hi gust
					if (data[i].HighGust.HasValue && data[i].HighGustTime.HasValue &&
						(data[i].HighGust.Value > highGust[monthOffset].Value))
					{
						highGust[monthOffset].Value = data[i].HighGust.Value;
						highGust[monthOffset].Ts = data[i].HighGustTime.Value;
					}
					if (data[i].LowTemp.HasValue && data[i].LowTempTime.HasValue)
					{
						// lo temp
						if (data[i].LowTemp.Value < lowTemp[monthOffset].Value)
						{
							lowTemp[monthOffset].Value = data[i].LowTemp.Value;
							lowTemp[monthOffset].Ts = data[i].LowTempTime.Value;
						}
						// hi min temp
						if (data[i].LowTemp.HasValue && data[i].LowTemp.Value > highMinTemp[monthOffset].Value)
						{
							highMinTemp[monthOffset].Value = data[i].LowTemp.Value;
							highMinTemp[monthOffset].Ts = data[i].LowTempTime.Value;
						}
					}
					if (data[i].HighTemp.HasValue && data[i].HighTempTime.HasValue)
					{
						// hi temp
						if (data[i].HighTemp.Value > highTemp[monthOffset].Value)
						{
							highTemp[monthOffset].Value = data[i].HighTemp.Value;
							highTemp[monthOffset].Ts = data[i].HighTempTime.Value;
						}
						// lo max temp
						if (data[i].HighTemp.Value < lowMaxTemp[monthOffset].Value)
						{
							lowMaxTemp[monthOffset].Value = data[i].HighTemp.Value;
							lowMaxTemp[monthOffset].Ts = data[i].HighTempTime.Value;
						}
					}

					// temp ranges
					// hi temp range
					if (data[i].LowTemp.HasValue && data[i].HighTemp.HasValue)
					{
						if ((data[i].HighTemp - data[i].LowTemp.Value) > highTempRange[monthOffset].Value)
						{
							highTempRange[monthOffset].Value = data[i].HighTemp.Value - data[i].LowTemp.Value;
							highTempRange[monthOffset].Ts = loggedDate;
						}
						// lo temp range
						if ((data[i].HighTemp - data[i].LowTemp.Value) < lowTempRange[monthOffset].Value)
						{
							lowTempRange[monthOffset].Value = data[i].HighTemp.Value - data[i].LowTemp.Value;
							lowTempRange[monthOffset].Ts = loggedDate;
						}
					}

					// lo baro
					if (data[i].LowPress.HasValue && data[i].LowPressTime.HasValue &&
						(data[i].LowPress.Value < lowBaro[monthOffset].Value))
					{
						lowBaro[monthOffset].Value = data[i].LowPress.Value;
						lowBaro[monthOffset].Ts = data[i].LowPressTime.Value;
					}
					// hi baro
					if (data[i].HighPress.HasValue && data[i].HighPressTime.HasValue &&
						(data[i].HighPress.Value > highBaro[monthOffset].Value))
					{
						highBaro[monthOffset].Value = data[i].HighPress.Value;
						highBaro[monthOffset].Ts = data[i].HighPressTime.Value;
					}
					// hi rain rate
					if (data[i].HighRainRate.HasValue && data[i].HighRainRateTime.HasValue &&
						(data[i].HighRainRate.Value > highRainRate[monthOffset].Value))
					{
						highRainRate[monthOffset].Value = data[i].HighRainRate.Value;
						highRainRate[monthOffset].Ts = data[i].HighRainRateTime.Value;
					}

					if (data[i].TotalRain.HasValue)
					{
						// monthly rain
						rainThisMonth += data[i].TotalRain.Value;

						if (rainThisMonth > highRainMonth[monthOffset].Value)
						{
							highRainMonth[monthOffset].Value = rainThisMonth;
							highRainMonth[monthOffset].Ts = thisDate;
						}

						// hi rain day
						if (data[i].TotalRain.Value > highRainDay[monthOffset].Value)
						{
							highRainDay[monthOffset].Value = data[i].TotalRain.Value;
							highRainDay[monthOffset].Ts = loggedDate;
						}

						// dry/wet period
						if (Convert.ToInt32(data[i].TotalRain * 1000) >= rainThreshold)
						{
							if (isDryNow)
							{
								currentWetPeriod = 1;
								isDryNow = false;
								var dryMonthOffset = thisDateDry.Month - 1;
								if (!(dryPeriod[dryMonthOffset].Value == Cumulus.DefaultHiVal && currentDryPeriod == 0) && currentDryPeriod > dryPeriod[dryMonthOffset].Value)
								{
									dryPeriod[dryMonthOffset].Value = currentDryPeriod;
									dryPeriod[dryMonthOffset].Ts = thisDateDry;
								}
								currentDryPeriod = 0;
							}
							else
							{
								currentWetPeriod++;
								thisDateWet = loggedDate;
							}
						}
						else
						{
							if (isDryNow)
							{
								currentDryPeriod++;
								thisDateDry = loggedDate;
							}
							else
							{
								currentDryPeriod = 1;
								isDryNow = true;
								var wetMonthOffset = thisDateWet.Month - 1;
								if (!(wetPeriod[wetMonthOffset].Value == Cumulus.DefaultHiVal && currentWetPeriod == 0) && currentWetPeriod > wetPeriod[wetMonthOffset].Value)
								{
									wetPeriod[wetMonthOffset].Value = currentWetPeriod;
									wetPeriod[wetMonthOffset].Ts = thisDateWet;
								}
								currentWetPeriod = 0;
							}
						}
					}

					// hi wind run
					if (data[i].WindRun.HasValue && data[i].WindRun.Value > highWindRun[monthOffset].Value)
					{
						highWindRun[monthOffset].Value = data[i].WindRun.Value;
						highWindRun[monthOffset].Ts = loggedDate;
					}
					// hi wind
					if (data[i].HighAvgWind.HasValue && data[i].HighAvgWindTime.HasValue && data[i].HighAvgWind.Value > highWind[monthOffset].Value)
					{
						highWind[monthOffset].Value = data[i].HighAvgWind.Value;
						highWind[monthOffset].Ts = data[i].HighAvgWindTime.Value;
					}

					// lo humidity
					if (data[i].LowHumidity.HasValue && data[i].LowHumidityTime.HasValue && data[i].LowHumidity.Value < lowHum[monthOffset].Value)
					{
						lowHum[monthOffset].Value = data[i].LowHumidity.Value;
						lowHum[monthOffset].Ts = data[i].LowHumidityTime.Value;
					}
					// hi humidity
					if (data[i].HighHumidity.HasValue && data[i].HighHumidityTime.HasValue && data[i].HighHumidity > highHum[monthOffset].Value)
					{
						highHum[monthOffset].Value = data[i].HighHumidity.Value;
						highHum[monthOffset].Ts = data[i].HighHumidityTime.Value;
					}

					// hi heat index
					if (data[i].HighHeatIndex.HasValue && data[i].HighHeatIndexTime.HasValue && data[i].HighHeatIndex.Value > highHeatInd[monthOffset].Value)
					{
						highHeatInd[monthOffset].Value = data[i].HighHeatIndex.Value;
						highHeatInd[monthOffset].Ts = data[i].HighHeatIndexTime.Value;
					}
					// hi app temp
					if (data[i].HighAppTemp.HasValue && data[i].HighAppTempTime.HasValue && data[i].HighAppTemp.Value > highAppTemp[monthOffset].Value)
					{
						highAppTemp[monthOffset].Value = data[i].HighAppTemp.Value;
						highAppTemp[monthOffset].Ts = data[i].HighAppTempTime.Value;
					}
					// lo app temp
					if (data[i].LowAppTemp.HasValue && data[i].LowAppTempTime.HasValue && data[i].LowAppTemp.Value < lowAppTemp[monthOffset].Value)
					{
						lowAppTemp[monthOffset].Value = data[i].LowAppTemp.Value;
						lowAppTemp[monthOffset].Ts = data[i].LowAppTempTime.Value;
					}

					// hi rain hour
					if (data[i].HighHourlyRain.HasValue && data[i].HighHourlyRainTime.HasValue && data[i].HighHourlyRain > highRainHour[monthOffset].Value)
					{
						highRainHour[monthOffset].Value = data[i].HighHourlyRain.Value;
						highRainHour[monthOffset].Ts = data[i].HighHourlyRainTime.Value;
					}

					// lo wind chill
					if (data[i].LowWindChill.HasValue && data[i].LowWindChillTime.HasValue && data[i].LowWindChill.Value < lowWindChill[monthOffset].Value)
					{
						lowWindChill[monthOffset].Value = data[i].LowWindChill.Value;
						lowWindChill[monthOffset].Ts = data[i].LowWindChillTime.Value;
					}

					// hi dewpt
					if (data[i].HighDewPoint.HasValue && data[i].HighDewPointTime.HasValue && data[i].HighDewPoint.Value > highDewPt[monthOffset].Value)
					{
						highDewPt[monthOffset].Value = data[i].HighDewPoint.Value;
						highDewPt[monthOffset].Ts = data[i].HighDewPointTime.Value;
					}
					// lo dewpt
					if (data[i].LowDewPoint.HasValue && data[i].LowDewPointTime.HasValue && data[i].LowDewPoint.Value < lowDewPt[monthOffset].Value)
					{
						lowDewPt[monthOffset].Value = data[i].LowDewPoint.Value;
						lowDewPt[monthOffset].Ts = data[i].LowDewPointTime.Value;
					}

					// hi feels like
					if (data[i].HighFeelsLike.HasValue && data[i].HighFeelsLikeTime.HasValue && data[i].HighFeelsLike.Value > highFeelsLike[monthOffset].Value)
					{
						highFeelsLike[monthOffset].Value = data[i].HighFeelsLike.Value;
						highFeelsLike[monthOffset].Ts = data[i].HighFeelsLikeTime.Value;
					}
					// lo feels like
					if (data[i].LowFeelsLike.HasValue && data[i].LowFeelsLikeTime.HasValue && data[i].LowFeelsLike.Value < lowFeelsLike[monthOffset].Value)
					{
						lowFeelsLike[monthOffset].Value = data[i].LowFeelsLike.Value;
						lowFeelsLike[monthOffset].Ts = data[i].LowFeelsLikeTime.Value;
					}

					// hi humidex
					if (data[i].HighHumidex.HasValue && data[i].HighHumidexTime.HasValue && data[i].HighHumidex.Value > highHumidex[monthOffset].Value)
					{
						highHumidex[monthOffset].Value = data[i].HighHumidex.Value;
						highHumidex[monthOffset].Ts = data[i].HighHumidexTime.Value;
					}
				}

				// We need to check if the run or wet/dry days at the end of log exceeds any records
				if (!(wetPeriod[thisDateWet.Month - 1].Value == Cumulus.DefaultHiVal && currentWetPeriod == 0) && currentWetPeriod > wetPeriod[thisDateWet.Month - 1].Value)
				{
					wetPeriod[thisDateWet.Month - 1].Value = currentWetPeriod;
					wetPeriod[thisDateWet.Month - 1].Ts = thisDateWet;
				}
				if (!(dryPeriod[thisDateDry.Month - 1].Value == Cumulus.DefaultHiVal && currentDryPeriod == 0) && currentDryPeriod > dryPeriod[thisDateDry.Month - 1].Value)
				{
					dryPeriod[thisDateDry.Month - 1].Value = currentDryPeriod;
					dryPeriod[thisDateDry.Month - 1].Ts = thisDateDry;
				}

				for (var i = 0; i < 12; i++)
				{
					var m = i + 1;
					json.Append($"\"{m}-highTempValDayfile\":\"{highTemp[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highTempTimeDayfile\":\"{highTemp[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowTempValDayfile\":\"{lowTemp[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowTempTimeDayfile\":\"{lowTemp[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highDewPointValDayfile\":\"{highDewPt[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highDewPointTimeDayfile\":\"{highDewPt[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowDewPointValDayfile\":\"{lowDewPt[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowDewPointTimeDayfile\":\"{lowDewPt[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highApparentTempValDayfile\":\"{highAppTemp[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highApparentTempTimeDayfile\":\"{highAppTemp[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowApparentTempValDayfile\":\"{lowAppTemp[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowApparentTempTimeDayfile\":\"{lowAppTemp[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highFeelsLikeValDayfile\":\"{highFeelsLike[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highFeelsLikeTimeDayfile\":\"{highFeelsLike[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowFeelsLikeValDayfile\":\"{lowFeelsLike[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowFeelsLikeTimeDayfile\":\"{lowFeelsLike[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highHumidexValDayfile\":\"{highHumidex[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highHumidexTimeDayfile\":\"{highHumidex[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowWindChillValDayfile\":\"{lowWindChill[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowWindChillTimeDayfile\":\"{lowWindChill[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highHeatIndexValDayfile\":\"{highHeatInd[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highHeatIndexTimeDayfile\":\"{highHeatInd[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highMinTempValDayfile\":\"{highMinTemp[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highMinTempTimeDayfile\":\"{highMinTemp[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowMaxTempValDayfile\":\"{lowMaxTemp[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowMaxTempTimeDayfile\":\"{lowMaxTemp[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highDailyTempRangeValDayfile\":\"{highTempRange[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highDailyTempRangeTimeDayfile\":\"{highTempRange[i].GetTsString(dateStampFormat)}\",");
					json.Append($"\"{m}-lowDailyTempRangeValDayfile\":\"{lowTempRange[i].GetValString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowDailyTempRangeTimeDayfile\":\"{lowTempRange[i].GetTsString(dateStampFormat)}\",");
					json.Append($"\"{m}-highHumidityValDayfile\":\"{highHum[i].GetValString(cumulus.HumFormat)}\",");
					json.Append($"\"{m}-highHumidityTimeDayfile\":\"{highHum[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowHumidityValDayfile\":\"{lowHum[i].GetValString(cumulus.HumFormat)}\",");
					json.Append($"\"{m}-lowHumidityTimeDayfile\":\"{lowHum[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highBarometerValDayfile\":\"{highBaro[i].GetValString(cumulus.PressFormat)}\",");
					json.Append($"\"{m}-highBarometerTimeDayfile\":\"{highBaro[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowBarometerValDayfile\":\"{lowBaro[i].GetValString(cumulus.PressFormat)}\",");
					json.Append($"\"{m}-lowBarometerTimeDayfile\":\"{lowBaro[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highGustValDayfile\":\"{highGust[i].GetValString(cumulus.WindFormat)}\",");
					json.Append($"\"{m}-highGustTimeDayfile\":\"{highGust[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highWindValDayfile\":\"{highWind[i].GetValString(cumulus.WindAvgFormat)}\",");
					json.Append($"\"{m}-highWindTimeDayfile\":\"{highWind[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highWindRunValDayfile\":\"{highWindRun[i].GetValString(cumulus.WindRunFormat)}\",");
					json.Append($"\"{m}-highWindRunTimeDayfile\":\"{highWindRun[i].GetTsString(dateStampFormat)}\",");
					json.Append($"\"{m}-highRainRateValDayfile\":\"{highRainRate[i].GetValString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highRainRateTimeDayfile\":\"{highRainRate[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highHourlyRainValDayfile\":\"{highRainHour[i].GetValString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highHourlyRainTimeDayfile\":\"{highRainHour[i].GetTsString(timeStampFormat)}\",");
					json.Append($"\"{m}-highDailyRainValDayfile\":\"{highRainDay[i].GetValString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highDailyRainTimeDayfile\":\"{highRainDay[i].GetTsString(dateStampFormat)}\",");
					json.Append($"\"{m}-highMonthlyRainValDayfile\":\"{highRainMonth[i].GetValString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highMonthlyRainTimeDayfile\":\"{highRainMonth[i].GetTsString("MM/yyyy")}\",");
					json.Append($"\"{m}-longestDryPeriodValDayfile\":\"{dryPeriod[i].GetValString()}\",");
					json.Append($"\"{m}-longestDryPeriodTimeDayfile\":\"{dryPeriod[i].GetTsString(dateStampFormat)}\",");
					json.Append($"\"{m}-longestWetPeriodValDayfile\":\"{wetPeriod[i].GetValString()}\",");
					json.Append($"\"{m}-longestWetPeriodTimeDayfile\":\"{wetPeriod[i].GetTsString(dateStampFormat)}\",");
				}
				json.Length--;
			}
			else
			{
				Cumulus.LogMessage("Error failed to find day records");
			}

			json.Append('}');
			return json.ToString();
		}

		internal string GetMonthlyRecLogFile()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var json = new StringBuilder("{", 25500);
			var datefrom = cumulus.RecordsBeganDate;
			datefrom = new DateTime(datefrom.Year, datefrom.Month, 1);
			var dateto = DateTime.Now;
			dateto = new DateTime(dateto.Year, dateto.Month, 1);
			var currDate = datefrom;

			var firstRow = true;
			var finished = false;
			var lastentrydate = datefrom;

			var isDryNow = false;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;

			int rainThreshold;
			if (cumulus.RainDayThreshold > 0)
			{
				rainThreshold = Convert.ToInt32(cumulus.RainDayThreshold * 1000);
			}
			else
			{
				// default
				if (cumulus.Units.Rain == 0)
				{
					rainThreshold = 200; // 0.2mm *1000
				}
				else
				{
					rainThreshold = 10;  // 0.01in *1000
				}
			}

			var highTemp = new LocalRec[12];
			var lowTemp = new LocalRec[12];
			var highDewPt = new LocalRec[12];
			var lowDewPt = new LocalRec[12];
			var highAppTemp = new LocalRec[12];
			var lowAppTemp = new LocalRec[12];
			var highFeelsLike = new LocalRec[12];
			var lowFeelsLike = new LocalRec[12];
			var highHumidex = new LocalRec[12];
			var lowWindChill = new LocalRec[12];
			var highHeatInd = new LocalRec[12];
			var highMinTemp = new LocalRec[12];
			var lowMaxTemp = new LocalRec[12];
			var highTempRange = new LocalRec[12];
			var lowTempRange = new LocalRec[12];
			var highHum = new LocalRec[12];
			var lowHum = new LocalRec[12];
			var highBaro = new LocalRec[12];
			var lowBaro = new LocalRec[12];
			var highGust = new LocalRec[12];
			var highWind = new LocalRec[12];
			var highWindRun = new LocalRec[12];
			var highRainRate = new LocalRec[12];
			var highRainHour = new LocalRec[12];
			var highRainDay = new LocalRec[12];
			var highRainMonth = new LocalRec[12];
			var dryPeriod = new LocalRec[12];
			var wetPeriod = new LocalRec[12];


			for (var i = 0; i < 12; i++)
			{
				highTemp[i] = new LocalRec(true);
				lowTemp[i] = new LocalRec(false);
				highDewPt[i] = new LocalRec(true);
				lowDewPt[i] = new LocalRec(false);
				highAppTemp[i] = new LocalRec(true);
				lowAppTemp[i] = new LocalRec(false);
				highFeelsLike[i] = new LocalRec(true);
				lowFeelsLike[i] = new LocalRec(false);
				highHumidex[i] = new LocalRec(true);
				lowWindChill[i] = new LocalRec(false);
				highHeatInd[i] = new LocalRec(true);
				highMinTemp[i] = new LocalRec(true);
				lowMaxTemp[i] = new LocalRec(false);
				highTempRange[i] = new LocalRec(true);
				lowTempRange[i] = new LocalRec(false);
				highHum[i] = new LocalRec(true);
				lowHum[i] = new LocalRec(false);
				highBaro[i] = new LocalRec(true);
				lowBaro[i] = new LocalRec(false);
				highGust[i] = new LocalRec(true);
				highWind[i] = new LocalRec(true);
				highWindRun[i] = new LocalRec(true);
				highRainRate[i] = new LocalRec(true);
				highRainHour[i] = new LocalRec(true);
				highRainDay[i] = new LocalRec(true);
				highRainMonth[i] = new LocalRec(true);
				dryPeriod[i] = new LocalRec(true);
				wetPeriod[i] = new LocalRec(true);
			}


			var thisDateDry = DateTime.MinValue;
			var thisDateWet = DateTime.MinValue;

			var currentDay = datefrom;
			var dayHighTemp = new LocalRec(true);
			var dayLowTemp = new LocalRec(false);
			double dayWindRun = 0;
			double dayRain = 0;

			var monthOffset = 0;
			var monthlyRain = 0.0;
			var totalRainfall = 0.0;

			hourRainLog.Clear();

			var watch = System.Diagnostics.Stopwatch.StartNew();

			while (!finished)
			{
				try
				{
					var rows = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >= ? and Timestamp < ?", currDate, currDate.AddMonths(1));
					foreach (var row in rows)
					{
						// We need to work in meteo dates not clock dates for day hi/lows
						var metoDate = row.Timestamp.AddHours(cumulus.GetHourInc());
						monthOffset = metoDate.Month - 1;

						if (firstRow)
						{
							lastentrydate = row.Timestamp;
							currentDay = metoDate;
							firstRow = false;
						}
						else
						{

						}

						// low chill
						if (row.WindChill.HasValue && row.WindChill.Value < lowWindChill[monthOffset].Value)
						{
							lowWindChill[monthOffset].Value = row.WindChill.Value;
							lowWindChill[monthOffset].Ts = row.Timestamp;
						}
						// hi heat
						if (row.HeatIndex.HasValue && row.HeatIndex.Value > highHeatInd[monthOffset].Value)
						{
							highHeatInd[monthOffset].Value = row.HeatIndex.Value;
							highHeatInd[monthOffset].Ts = row.Timestamp;
						}

						if (row.Apparent.HasValue)
						{
							// hi appt
							if (row.Apparent.Value > highAppTemp[monthOffset].Value)
							{
								highAppTemp[monthOffset].Value = row.Apparent.Value;
								highAppTemp[monthOffset].Ts = row.Timestamp;
							}
							// lo appt
							if (row.Apparent.Value < lowAppTemp[monthOffset].Value)
							{
								lowAppTemp[monthOffset].Value = row.Apparent.Value;
								lowAppTemp[monthOffset].Ts = row.Timestamp;
							}
						}

						if (row.FeelsLike.HasValue)
						{
							// hi feels like
							if (row.FeelsLike.Value > highFeelsLike[monthOffset].Value)
							{
								highFeelsLike[monthOffset].Value = row.FeelsLike.Value;
								highFeelsLike[monthOffset].Ts = row.Timestamp;
							}
							// lo feels like
							if (row.FeelsLike.Value < lowFeelsLike[monthOffset].Value)
							{
								lowFeelsLike[monthOffset].Value = row.FeelsLike.Value;
								lowFeelsLike[monthOffset].Ts = row.Timestamp;
							}
						}

						// hi humidex
						if (row.Humidex.HasValue && row.Humidex.Value > highHumidex[monthOffset].Value)
						{
							highHumidex[monthOffset].Value = row.Humidex.Value;
							highHumidex[monthOffset].Ts = row.Timestamp;
						}

						if (row.Temp.HasValue)
						{
							// hi temp
							if (row.Temp.Value > highTemp[monthOffset].Value)
							{
								highTemp[monthOffset].Value = row.Temp.Value;
								highTemp[monthOffset].Ts = row.Timestamp;
							}
							// lo temp
							if (row.Temp.Value < lowTemp[monthOffset].Value)
							{
								lowTemp[monthOffset].Value = row.Temp.Value;
								lowTemp[monthOffset].Ts = row.Timestamp;
							}
						}
						if (row.DewPoint.HasValue)
						{
							// hi dewpoint
							if (row.DewPoint.Value > highDewPt[monthOffset].Value)
							{
								highDewPt[monthOffset].Value = row.DewPoint.Value;
								highDewPt[monthOffset].Ts = row.Timestamp;
							}
							// low dewpoint
							if (row.DewPoint.Value < lowDewPt[monthOffset].Value)
							{
								lowDewPt[monthOffset].Value = row.DewPoint.Value;
								lowDewPt[monthOffset].Ts = row.Timestamp;
							}
						}
						if (row.Humidity.HasValue)
						{
							// hi hum
							if (row.Humidity.Value > highHum[monthOffset].Value)
							{
								highHum[monthOffset].Value = row.Humidity.Value;
								highHum[monthOffset].Ts = row.Timestamp;
							}
							// lo hum
							if (row.Humidity.Value < lowHum[monthOffset].Value)
							{
								lowHum[monthOffset].Value = row.Humidity.Value;
								lowHum[monthOffset].Ts = row.Timestamp;
							}
						}
						if (row.Pressure.HasValue)
						{
							// hi baro
							if (row.Pressure.Value > highBaro[monthOffset].Value)
							{
								highBaro[monthOffset].Value = row.Pressure.Value;
								highBaro[monthOffset].Ts = row.Timestamp;
							}
							// lo baro
							if (row.Pressure.Value < lowBaro[monthOffset].Value)
							{
								lowBaro[monthOffset].Value = row.Pressure.Value;
								lowBaro[monthOffset].Ts = row.Timestamp;
							}
						}
						// hi gust
						if (row.WindGust10m.HasValue && row.WindGust10m.Value > highGust[monthOffset].Value)
						{
							highGust[monthOffset].Value = row.WindGust10m.Value;
							highGust[monthOffset].Ts = row.Timestamp;
						}
						// hi wind
						if (row.WindAvg.HasValue && row.WindAvg.Value > highWind[monthOffset].Value)
						{
							highWind[monthOffset].Value = row.WindAvg.Value;
							highWind[monthOffset].Ts = row.Timestamp;
						}
						// hi rain rate
						if (row.RainRate.HasValue && row.RainRate.Value > highRainRate[monthOffset].Value)
						{
							highRainRate[monthOffset].Value = row.RainRate.Value;
							highRainRate[monthOffset].Ts = row.Timestamp;
						}

						/*
						// same meteo day
						if (currentDay.Date == metoDate.Date)
						{
							if (row.Temp.HasValue)
							{
								if (row.Temp.Value > dayHighTemp.Value)
								{
									dayHighTemp.Value = row.Temp.Value;
									dayHighTemp.Ts = row.Timestamp;
								}

								if (row.Temp.Value < dayLowTemp.Value)
								{
									dayLowTemp.Value = row.Temp.Value;
									dayLowTemp.Ts = row.Timestamp;
								}
							}
							if (row.WindAvg.HasValue)
								dayWindRun += row.Timestamp.Subtract(lastentrydate).TotalHours * row.WindAvg.Value;
						}
						*/

						// new meteo day
						if (currentDay.Date != metoDate.Date)
						{
							var lastEntryMonthOffset = currentDay.Month - 1;
							if (dayHighTemp.Value < lowMaxTemp[lastEntryMonthOffset].Value)
							{
								lowMaxTemp[lastEntryMonthOffset].Value = dayHighTemp.Value;
								lowMaxTemp[lastEntryMonthOffset].Ts = dayHighTemp.Ts;
							}
							if (dayLowTemp.Value > highMinTemp[lastEntryMonthOffset].Value)
							{
								highMinTemp[lastEntryMonthOffset].Value = dayLowTemp.Value;
								highMinTemp[lastEntryMonthOffset].Ts = dayLowTemp.Ts;
							}
							if (dayHighTemp.Value - dayLowTemp.Value > highTempRange[lastEntryMonthOffset].Value)
							{
								highTempRange[lastEntryMonthOffset].Value = dayHighTemp.Value - dayLowTemp.Value;
								highTempRange[lastEntryMonthOffset].Ts = currentDay;
							}
							if (dayHighTemp.Value - dayLowTemp.Value < lowTempRange[lastEntryMonthOffset].Value)
							{
								lowTempRange[lastEntryMonthOffset].Value = dayHighTemp.Value - dayLowTemp.Value;
								lowTempRange[lastEntryMonthOffset].Ts = currentDay;
							}

							if (dayRain > highRainDay[lastEntryMonthOffset].Value)
							{
								highRainDay[lastEntryMonthOffset].Value = dayRain;
								highRainDay[lastEntryMonthOffset].Ts = currentDay;
							}

							// new month ?
							if (currentDay.Month != metoDate.Month)
							{
								var offset = currentDay.Month - 1;
								if (monthlyRain > highRainMonth[offset].Value)
								{
									highRainMonth[offset].Value = monthlyRain;
									highRainMonth[offset].Ts = currentDay;
								}
								monthlyRain = 0;
							}

							monthlyRain += dayRain;
							totalRainfall += dayRain;

							if (dayWindRun > highWindRun[lastEntryMonthOffset].Value)
							{
								highWindRun[lastEntryMonthOffset].Value = dayWindRun;
								highWindRun[lastEntryMonthOffset].Ts = currentDay;
							}

							// dry/wet period
							if (Convert.ToInt32(dayRain * 1000) >= rainThreshold)
							{
								if (isDryNow)
								{
									currentWetPeriod = 1;
									isDryNow = false;
									if (currentDryPeriod > dryPeriod[monthOffset].Value)
									{
										dryPeriod[monthOffset].Value = currentDryPeriod;
										dryPeriod[monthOffset].Ts = thisDateDry;
									}
									currentDryPeriod = 0;
								}
								else
								{
									currentWetPeriod++;
									thisDateWet = currentDay;
								}
							}
							else
							{
								if (isDryNow)
								{
									currentDryPeriod++;
									thisDateDry = currentDay;
								}
								else
								{
									currentDryPeriod = 1;
									isDryNow = true;
									if (currentWetPeriod > wetPeriod[monthOffset].Value)
									{
										wetPeriod[monthOffset].Value = currentWetPeriod;
										wetPeriod[monthOffset].Ts = thisDateWet;
									}
									currentWetPeriod = 0;
								}
							}


							currentDay = metoDate;
							if (row.Temp.HasValue)
							{
								dayHighTemp.Value = row.Temp.Value;
								dayLowTemp.Value = row.Temp.Value;
							}
							dayRain = 0;
						}

						if (row.RainToday.HasValue && dayRain < row.RainToday.Value)
						{
							dayRain = row.RainToday.Value;
						}

						if (dayRain > highRainDay[monthOffset].Value)
						{
							highRainDay[monthOffset].Value = dayRain;
							highRainDay[monthOffset].Ts = currentDay;
						}

						if (monthlyRain > highRainMonth[monthOffset].Value)
						{
							highRainMonth[monthOffset].Value = monthlyRain;
							highRainMonth[monthOffset].Ts = currentDay;
						}

						dayWindRun += row.Timestamp.Subtract(lastentrydate).TotalHours * row.WindAvg.Value;

						if (dayWindRun > highWindRun[monthOffset].Value)
						{
							highWindRun[monthOffset].Value = dayWindRun;
							highWindRun[monthOffset].Ts = currentDay;
						}

						// hourly rain
						/*
						* need to track what the rainfall has been in the last rolling hour
						* across day rollovers where the count resets
						*/

						AddLastHourRainEntry(row.Timestamp, totalRainfall + dayRain);
						RemoveOldRainData(row.Timestamp);

						var rainThisHour = hourRainLog.Last().Raincounter - hourRainLog.First().Raincounter;
						if (rainThisHour > highRainHour[monthOffset].Value)
						{
							highRainHour[monthOffset].Value = rainThisHour;
							highRainHour[monthOffset].Ts = row.Timestamp;
						}

						lastentrydate = row.Timestamp;
						//lastRainMidnight = rainMidnight;
					}

					// for the final entry - check the monthly rain
					if (rows.Count > 0 && monthlyRain > highRainMonth[monthOffset].Value)
					{
						highRainMonth[monthOffset].Value = monthlyRain;
						highRainMonth[monthOffset].Ts = currentDay;
					}

				}
				catch (Exception e)
				{
					cumulus.LogExceptionMessage(e, "GetMonthlyLogRec: Error processing log data");
				}

				if (currDate >= dateto)
				{
					finished = true;
					cumulus.LogDebugMessage("GetMonthlyRecLogFile: Finished processing the log data");
				}
				else
				{
					cumulus.LogDebugMessage($"GetMonthlyRecLogFile: Finished processing log data for {currDate.Date}");
					currDate = currDate.AddMonths(1);
				}
			}



			for (var i = 0; i < 12; i++)
			{
				var m = i + 1;
				json.Append($"\"{m}-highTempValLogfile\":\"{highTemp[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highTempTimeLogfile\":\"{highTemp[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowTempValLogfile\":\"{lowTemp[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowTempTimeLogfile\":\"{lowTemp[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDewPointValLogfile\":\"{highDewPt[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDewPointTimeLogfile\":\"{highDewPt[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowDewPointValLogfile\":\"{lowDewPt[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDewPointTimeLogfile\":\"{lowDewPt[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highApparentTempValLogfile\":\"{highAppTemp[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highApparentTempTimeLogfile\":\"{highAppTemp[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowApparentTempValLogfile\":\"{lowAppTemp[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowApparentTempTimeLogfile\":\"{lowAppTemp[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeValLogfile\":\"{highFeelsLike[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeTimeLogfile\":\"{highFeelsLike[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeValLogfile\":\"{lowFeelsLike[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeTimeLogfile\":\"{lowFeelsLike[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHumidexValLogfile\":\"{highHumidex[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHumidexTimeLogfile\":\"{highHumidex[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowWindChillValLogfile\":\"{lowWindChill[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowWindChillTimeLogfile\":\"{lowWindChill[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHeatIndexValLogfile\":\"{highHeatInd[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHeatIndexTimeLogfile\":\"{highHeatInd[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highMinTempValLogfile\":\"{highMinTemp[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highMinTempTimeLogfile\":\"{highMinTemp[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowMaxTempValLogfile\":\"{lowMaxTemp[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowMaxTempTimeLogfile\":\"{lowMaxTemp[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeValLogfile\":\"{highTempRange[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeTimeLogfile\":\"{highTempRange[i].GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeValLogfile\":\"{lowTempRange[i].GetValString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeTimeLogfile\":\"{lowTempRange[i].GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-highHumidityValLogfile\":\"{highHum[i].GetValString(cumulus.HumFormat)}\",");
				json.Append($"\"{m}-highHumidityTimeLogfile\":\"{highHum[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowHumidityValLogfile\":\"{lowHum[i].GetValString(cumulus.HumFormat)}\",");
				json.Append($"\"{m}-lowHumidityTimeLogfile\":\"{lowHum[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highBarometerValLogfile\":\"{highBaro[i].GetValString(cumulus.PressFormat)}\",");
				json.Append($"\"{m}-highBarometerTimeLogfile\":\"{highBaro[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowBarometerValLogfile\":\"{lowBaro[i].GetValString(cumulus.PressFormat)}\",");
				json.Append($"\"{m}-lowBarometerTimeLogfile\":\"{lowBaro[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highGustValLogfile\":\"{highGust[i].GetValString(cumulus.WindFormat)}\",");
				json.Append($"\"{m}-highGustTimeLogfile\":\"{highGust[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindValLogfile\":\"{highWind[i].GetValString(cumulus.WindAvgFormat)}\",");
				json.Append($"\"{m}-highWindTimeLogfile\":\"{highWind[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindRunValLogfile\":\"{highWindRun[i].GetValString(cumulus.WindRunFormat)}\",");
				json.Append($"\"{m}-highWindRunTimeLogfile\":\"{highWindRun[i].GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-highRainRateValLogfile\":\"{highRainRate[i].GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highRainRateTimeLogfile\":\"{highRainRate[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHourlyRainValLogfile\":\"{highRainHour[i].GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highHourlyRainTimeLogfile\":\"{highRainHour[i].GetTsString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyRainValLogfile\":\"{highRainDay[i].GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highDailyRainTimeLogfile\":\"{highRainDay[i].GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainValLogfile\":\"{highRainMonth[i].GetValString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainTimeLogfile\":\"{highRainMonth[i].GetTsString("MM/yyyy")}\",");
				json.Append($"\"{m}-longestDryPeriodValLogfile\":\"{dryPeriod[i].GetValString()}\",");
				json.Append($"\"{m}-longestDryPeriodTimeLogfile\":\"{dryPeriod[i].GetTsString(dateStampFormat)}\",");
				json.Append($"\"{m}-longestWetPeriodValLogfile\":\"{wetPeriod[i].GetValString()}\",");
				json.Append($"\"{m}-longestWetPeriodTimeLogfile\":\"{wetPeriod[i].GetTsString(dateStampFormat)}\",");
			}

			json.Length--;
			json.Append('}');

			watch.Stop();
			var elapsed = watch.ElapsedMilliseconds;
			cumulus.LogDebugMessage($"Monthly recs editor Logfiles load = {elapsed} ms");

			return json.ToString();
		}

		internal string GetThisMonthRecData()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var json = new StringBuilder("{", 1700);
			// Records - Temperature
			json.Append($"\"highTempVal\":\"{station.ThisMonth.HighTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highTempTime\":\"{station.ThisMonth.HighTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowTempVal\":\"{station.ThisMonth.LowTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempTime\":\"{station.ThisMonth.LowTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDewPointVal\":\"{station.ThisMonth.HighDewPoint.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointTime\":\"{station.ThisMonth.HighDewPoint.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointVal\":\"{station.ThisMonth.LowDewPoint.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointTime\":\"{station.ThisMonth.LowDewPoint.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempVal\":\"{station.ThisMonth.HighAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempTime\":\"{station.ThisMonth.HighAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempVal\":\"{station.ThisMonth.LowAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempTime\":\"{station.ThisMonth.LowAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeVal\":\"{station.ThisMonth.HighFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeTime\":\"{station.ThisMonth.HighFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeVal\":\"{station.ThisMonth.LowFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeTime\":\"{station.ThisMonth.LowFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHumidexVal\":\"{station.ThisMonth.HighHumidex.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexTime\":\"{station.ThisMonth.HighHumidex.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillVal\":\"{station.ThisMonth.LowChill.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillTime\":\"{station.ThisMonth.LowChill.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexVal\":\"{station.ThisMonth.HighHeatIndex.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexTime\":\"{station.ThisMonth.HighHeatIndex.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highMinTempVal\":\"{station.ThisMonth.HighMinTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempTime\":\"{station.ThisMonth.HighMinTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempVal\":\"{station.ThisMonth.LowMaxTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempTime\":\"{station.ThisMonth.LowMaxTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeVal\":\"{station.ThisMonth.HighDailyTempRange.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeTime\":\"{station.ThisMonth.HighDailyTempRange.GetTsString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeVal\":\"{station.ThisMonth.LowDailyTempRange.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeTime\":\"{station.ThisMonth.LowDailyTempRange.GetTsString(dateStampFormat)}\",");
			// Records - Humidity
			json.Append($"\"highHumidityVal\":\"{station.ThisMonth.HighHumidity.GetValString(cumulus.HumFormat)}\",");
			json.Append($"\"highHumidityTime\":\"{station.ThisMonth.HighHumidity.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityVal\":\"{station.ThisMonth.LowHumidity.GetValString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityTime\":\"{station.ThisMonth.LowHumidity.GetTsString(timeStampFormat)}\",");
			// Records - Pressure
			json.Append($"\"highBarometerVal\":\"{station.ThisMonth.HighPress.GetValString(cumulus.PressFormat)}\",");
			json.Append($"\"highBarometerTime\":\"{station.ThisMonth.HighPress.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerVal\":\"{station.ThisMonth.LowPress.GetValString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerTime\":\"{station.ThisMonth.LowPress.GetTsString(timeStampFormat)}\",");
			// Records - Wind
			json.Append($"\"highGustVal\":\"{station.ThisMonth.HighGust.GetValString(cumulus.WindFormat)}\",");
			json.Append($"\"highGustTime\":\"{station.ThisMonth.HighGust.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindVal\":\"{station.ThisMonth.HighWind.GetValString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindTime\":\"{station.ThisMonth.HighWind.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindRunVal\":\"{station.ThisMonth.HighWindRun.GetValString(cumulus.WindRunFormat)}\",");
			json.Append($"\"highWindRunTime\":\"{station.ThisMonth.HighWindRun.GetTsString(dateStampFormat)}\",");
			// Records - Rain
			json.Append($"\"highRainRateVal\":\"{station.ThisMonth.HighRainRate.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highRainRateTime\":\"{station.ThisMonth.HighRainRate.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainVal\":\"{station.ThisMonth.HourlyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainTime\":\"{station.ThisMonth.HourlyRain.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainVal\":\"{station.ThisMonth.DailyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainTime\":\"{station.ThisMonth.DailyRain.GetTsString(dateStampFormat)}\",");
			json.Append($"\"longestDryPeriodVal\":\"{station.ThisMonth.LongestDryPeriod.GetValString("F0")}\",");
			json.Append($"\"longestDryPeriodTime\":\"{station.ThisMonth.LongestDryPeriod.GetTsString(dateStampFormat)}\",");
			json.Append($"\"longestWetPeriodVal\":\"{station.ThisMonth.LongestWetPeriod.GetValString("F0")}\",");
			json.Append($"\"longestWetPeriodTime\":\"{station.ThisMonth.LongestWetPeriod.GetTsString(dateStampFormat)}\"");

			json.Append('}');

			return json.ToString();
		}

		internal string EditThisMonthRecs(IHttpContext context)
		{
			var request = context.Request;
			string text;

			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = Uri.UnescapeDataString(reader.ReadToEnd());
			}
			// Eg "name=highTempValvalue=134.6&pk=1"
			var newData = text.Split('&');
			var field = newData[0].Split('=')[1];
			var value = newData[1].Split('=')[1];
			var result = 1;
			try
			{
				switch (field)
				{
					case "highTempVal":
						station.ThisMonth.HighTemp.Val = double.Parse(value);
						break;
					case "highTempTime":
						station.ThisMonth.HighTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowTempVal":
						station.ThisMonth.LowTemp.Val = double.Parse(value);
						break;
					case "lowTempTime":
						station.ThisMonth.LowTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highDewPointVal":
						station.ThisMonth.HighDewPoint.Val = double.Parse(value);
						break;
					case "highDewPointTime":
						station.ThisMonth.HighDewPoint.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowDewPointVal":
						station.ThisMonth.LowDewPoint.Val = double.Parse(value);
						break;
					case "lowDewPointTime":
						station.ThisMonth.LowDewPoint.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highApparentTempVal":
						station.ThisMonth.HighAppTemp.Val = double.Parse(value);
						break;
					case "highApparentTempTime":
						station.ThisMonth.HighAppTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowApparentTempVal":
						station.ThisMonth.LowAppTemp.Val = double.Parse(value);
						break;
					case "lowApparentTempTime":
						station.ThisMonth.LowAppTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highFeelsLikeVal":
						station.ThisMonth.HighFeelsLike.Val = double.Parse(value);
						break;
					case "highFeelsLikeTime":
						station.ThisMonth.HighFeelsLike.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowFeelsLikeVal":
						station.ThisMonth.LowFeelsLike.Val = double.Parse(value);
						break;
					case "lowFeelsLikeTime":
						station.ThisMonth.LowFeelsLike.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highHumidexVal":
						station.ThisMonth.HighHumidex.Val = double.Parse(value);
						break;
					case "highHumidexTime":
						station.ThisMonth.HighHumidex.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowWindChillVal":
						station.ThisMonth.LowChill.Val = double.Parse(value);
						break;
					case "lowWindChillTime":
						station.ThisMonth.LowChill.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highHeatIndexVal":
						station.ThisMonth.HighHeatIndex.Val = double.Parse(value);
						break;
					case "highHeatIndexTime":
						station.ThisMonth.HighHeatIndex.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highMinTempVal":
						station.ThisMonth.HighMinTemp.Val = double.Parse(value);
						break;
					case "highMinTempTime":
						station.ThisMonth.HighMinTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowMaxTempVal":
						station.ThisMonth.LowMaxTemp.Val = double.Parse(value);
						break;
					case "lowMaxTempTime":
						station.ThisMonth.LowMaxTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highDailyTempRangeVal":
						station.ThisMonth.HighDailyTempRange.Val = double.Parse(value);
						break;
					case "highDailyTempRangeTime":
						station.ThisMonth.HighDailyTempRange.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "lowDailyTempRangeVal":
						station.ThisMonth.LowDailyTempRange.Val = double.Parse(value);
						break;
					case "lowDailyTempRangeTime":
						station.ThisMonth.LowDailyTempRange.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "highHumidityVal":
						station.ThisMonth.HighHumidity.Val = int.Parse(value);
						break;
					case "highHumidityTime":
						station.ThisMonth.HighHumidity.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowHumidityVal":
						station.ThisMonth.LowHumidity.Val = int.Parse(value);
						break;
					case "lowHumidityTime":
						station.ThisMonth.LowHumidity.Ts =  Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highBarometerVal":
						station.ThisMonth.HighPress.Val = double.Parse(value);
						break;
					case "highBarometerTime":
						station.ThisMonth.HighPress.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowBarometerVal":
						station.ThisMonth.LowPress.Val = double.Parse(value);
						break;
					case "lowBarometerTime":
						station.ThisMonth.LowPress.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highGustVal":
						station.ThisMonth.HighGust.Val = double.Parse(value);
						break;
					case "highGustTime":
						station.ThisMonth.HighGust.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highWindVal":
						station.ThisMonth.HighWind.Val = double.Parse(value);
						break;
					case "highWindTime":
						station.ThisMonth.HighWind.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highWindRunVal":
						station.ThisMonth.HighWindRun.Val = double.Parse(value);
						break;
					case "highWindRunTime":
						station.ThisMonth.HighWindRun.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "highRainRateVal":
						station.ThisMonth.HighRainRate.Val = double.Parse(value);
						break;
					case "highRainRateTime":
						station.ThisMonth.HighRainRate.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highHourlyRainVal":
						station.ThisMonth.HourlyRain.Val = double.Parse(value);
						break;
					case "highHourlyRainTime":
						station.ThisMonth.HourlyRain.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highDailyRainVal":
						station.ThisMonth.DailyRain.Val = double.Parse(value);
						break;
					case "highDailyRainTime":
						station.ThisMonth.DailyRain.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "longestDryPeriodVal":
						station.ThisMonth.LongestDryPeriod.Val = int.Parse(value);
						break;
					case "longestDryPeriodTime":
						station.ThisMonth.LongestDryPeriod.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "longestWetPeriodVal":
						station.ThisMonth.LongestWetPeriod.Val = int.Parse(value);
						break;
					case "longestWetPeriodTime":
						station.ThisMonth.LongestWetPeriod.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					default:
						result = 0;
						break;
				}
				station.WriteMonthIniFile();
			}
			catch
			{
				result = 0;
			}
			return $"{{\"result\":\"{((result == 1) ? "Success" : "Failed")}\"}}";
		}

		internal string GetThisYearRecData()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var json = new StringBuilder("{", 1800);
			// Records - Temperature
			json.Append($"\"highTempVal\":\"{station.ThisYear.HighTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highTempTime\":\"{station.ThisYear.HighTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowTempVal\":\"{station.ThisYear.LowTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempTime\":\"{station.ThisYear.LowTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDewPointVal\":\"{station.ThisYear.HighDewPoint.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointTime\":\"{station.ThisYear.HighDewPoint.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointVal\":\"{station.ThisYear.LowDewPoint.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointTime\":\"{station.ThisYear.LowDewPoint.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempVal\":\"{station.ThisYear.HighAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempTime\":\"{station.ThisYear.HighAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempVal\":\"{station.ThisYear.LowAppTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempTime\":\"{station.ThisYear.LowAppTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeVal\":\"{station.ThisYear.HighFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeTime\":\"{station.ThisYear.HighFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeVal\":\"{station.ThisYear.LowFeelsLike.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeTime\":\"{station.ThisYear.LowFeelsLike.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHumidexVal\":\"{station.ThisYear.HighHumidex.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexTime\":\"{station.ThisYear.HighHumidex.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillVal\":\"{station.ThisYear.LowChill.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillTime\":\"{station.ThisYear.LowChill.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexVal\":\"{station.ThisYear.HighHeatIndex.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexTime\":\"{station.ThisYear.HighHeatIndex.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highMinTempVal\":\"{station.ThisYear.HighMinTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempTime\":\"{station.ThisYear.HighMinTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempVal\":\"{station.ThisYear.LowMaxTemp.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempTime\":\"{station.ThisYear.LowMaxTemp.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeVal\":\"{station.ThisYear.HighDailyTempRange.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeTime\":\"{station.ThisYear.HighDailyTempRange.GetTsString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeVal\":\"{station.ThisYear.LowDailyTempRange.GetValString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeTime\":\"{station.ThisYear.LowDailyTempRange.GetTsString(dateStampFormat)}\",");
			// Records - Humidity
			json.Append($"\"highHumidityVal\":\"{station.ThisYear.HighHumidity.GetValString(cumulus.HumFormat)}\",");
			json.Append($"\"highHumidityTime\":\"{station.ThisYear.HighHumidity.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityVal\":\"{station.ThisYear.LowHumidity.GetValString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityTime\":\"{station.ThisYear.LowHumidity.GetTsString(timeStampFormat)}\",");
			// Records - Pressure
			json.Append($"\"highBarometerVal\":\"{station.ThisYear.HighPress.GetValString(cumulus.PressFormat)}\",");
			json.Append($"\"highBarometerTime\":\"{station.ThisYear.HighPress.GetTsString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerVal\":\"{station.ThisYear.LowPress.GetValString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerTime\":\"{station.ThisYear.LowPress.GetTsString(timeStampFormat)}\",");
			// Records - Wind
			json.Append($"\"highGustVal\":\"{station.ThisYear.HighGust.GetValString(cumulus.WindFormat)}\",");
			json.Append($"\"highGustTime\":\"{station.ThisYear.HighGust.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindVal\":\"{station.ThisYear.HighWind.GetValString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindTime\":\"{station.ThisYear.HighWind.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highWindRunVal\":\"{station.ThisYear.HighWindRun.GetValString(cumulus.WindRunFormat)}\",");
			json.Append($"\"highWindRunTime\":\"{station.ThisYear.HighWindRun.GetTsString(dateStampFormat)}\",");
			// Records - Rain
			json.Append($"\"highRainRateVal\":\"{station.ThisYear.HighRainRate.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highRainRateTime\":\"{station.ThisYear.HighRainRate.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainVal\":\"{station.ThisYear.HourlyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainTime\":\"{station.ThisYear.HourlyRain.GetTsString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainVal\":\"{station.ThisYear.DailyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainTime\":\"{station.ThisYear.DailyRain.GetTsString(dateStampFormat)}\",");
			json.Append($"\"highMonthlyRainVal\":\"{station.ThisYear.MonthlyRain.GetValString(cumulus.RainFormat)}\",");
			json.Append($"\"highMonthlyRainTime\":\"{station.ThisYear.MonthlyRain.GetTsString("MM/yyyy")}\",");
			json.Append($"\"longestDryPeriodVal\":\"{station.ThisYear.LongestDryPeriod.GetValString("F0")}\",");
			json.Append($"\"longestDryPeriodTime\":\"{station.ThisYear.LongestDryPeriod.GetTsString(dateStampFormat)}\",");
			json.Append($"\"longestWetPeriodVal\":\"{station.ThisYear.LongestWetPeriod.GetValString("F0")}\",");
			json.Append($"\"longestWetPeriodTime\":\"{station.ThisYear.LongestWetPeriod.GetTsString(dateStampFormat)}\"");

			json.Append('}');

			return json.ToString();
		}

		internal string EditThisYearRecs(IHttpContext context)
		{
			var request = context.Request;
			string text;

			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = Uri.UnescapeDataString(reader.ReadToEnd());
			}
			// Eg "name=highTempValvalue=134.6&pk=1"
			var newData = text.Split('&');
			var field = newData[0].Split('=')[1];
			var value = newData[1].Split('=')[1];
			var result = 1;
			try
			{
				switch (field)
				{
					case "highTempVal":
						station.ThisYear.HighTemp.Val = double.Parse(value);
						break;
					case "highTempTime":
						station.ThisYear.HighTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowTempVal":
						station.ThisYear.LowTemp.Val = double.Parse(value);
						break;
					case "lowTempTime":
						station.ThisYear.LowTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highDewPointVal":
						station.ThisYear.HighDewPoint.Val = double.Parse(value);
						break;
					case "highDewPointTime":
						station.ThisYear.HighDewPoint.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowDewPointVal":
						station.ThisYear.LowDewPoint.Val = double.Parse(value);
						break;
					case "lowDewPointTime":
						station.ThisYear.LowDewPoint.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highApparentTempVal":
						station.ThisYear.HighAppTemp.Val = double.Parse(value);
						break;
					case "highApparentTempTime":
						station.ThisYear.HighAppTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowApparentTempVal":
						station.ThisYear.LowAppTemp.Val = double.Parse(value);
						break;
					case "lowApparentTempTime":
						station.ThisYear.LowAppTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highFeelsLikeVal":
						station.ThisYear.HighFeelsLike.Val = double.Parse(value);
						break;
					case "highFeelsLikeTime":
						station.ThisYear.HighFeelsLike.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowFeelsLikeVal":
						station.ThisYear.LowFeelsLike.Val = double.Parse(value);
						break;
					case "lowFeelsLikeTime":
						station.ThisYear.LowFeelsLike.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highHumidexVal":
						station.ThisYear.HighHumidex.Val = double.Parse(value);
						break;
					case "highHumidexTime":
						station.ThisYear.HighHumidex.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowWindChillVal":
						station.ThisYear.LowChill.Val = double.Parse(value);
						break;
					case "lowWindChillTime":
						station.ThisYear.LowChill.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highHeatIndexVal":
						station.ThisYear.HighHeatIndex.Val = double.Parse(value);
						break;
					case "highHeatIndexTime":
						station.ThisYear.HighHeatIndex.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highMinTempVal":
						station.ThisYear.HighMinTemp.Val = double.Parse(value);
						break;
					case "highMinTempTime":
						station.ThisYear.HighMinTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowMaxTempVal":
						station.ThisYear.LowMaxTemp.Val = double.Parse(value);
						break;
					case "lowMaxTempTime":
						station.ThisYear.LowMaxTemp.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highDailyTempRangeVal":
						station.ThisYear.HighDailyTempRange.Val = double.Parse(value);
						break;
					case "highDailyTempRangeTime":
						station.ThisYear.HighDailyTempRange.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "lowDailyTempRangeVal":
						station.ThisYear.LowDailyTempRange.Val = double.Parse(value);
						break;
					case "lowDailyTempRangeTime":
						station.ThisYear.LowDailyTempRange.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "highHumidityVal":
						station.ThisYear.HighHumidity.Val = int.Parse(value);
						break;
					case "highHumidityTime":
						station.ThisYear.HighHumidity.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowHumidityVal":
						station.ThisYear.LowHumidity.Val = int.Parse(value);
						break;
					case "lowHumidityTime":
						station.ThisYear.LowHumidity.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highBarometerVal":
						station.ThisYear.HighPress.Val = double.Parse(value);
						break;
					case "highBarometerTime":
						station.ThisYear.HighPress.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "lowBarometerVal":
						station.ThisYear.LowPress.Val = double.Parse(value);
						break;
					case "lowBarometerTime":
						station.ThisYear.LowPress.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highGustVal":
						station.ThisYear.HighGust.Val = double.Parse(value);
						break;
					case "highGustTime":
						station.ThisYear.HighGust.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highWindVal":
						station.ThisYear.HighWind.Val = double.Parse(value);
						break;
					case "highWindTime":
						station.ThisYear.HighWind.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highWindRunVal":
						station.ThisYear.HighWindRun.Val = double.Parse(value);
						break;
					case "highWindRunTime":
						station.ThisYear.HighWindRun.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "highRainRateVal":
						station.ThisYear.HighRainRate.Val = double.Parse(value);
						break;
					case "highRainRateTime":
						station.ThisYear.HighRainRate.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highHourlyRainVal":
						station.ThisYear.HourlyRain.Val = double.Parse(value);
						break;
					case "highHourlyRainTime":
						station.ThisYear.HourlyRain.Ts = Utils.ddmmyyyy_hhmmStrToDate(value);
						break;
					case "highDailyRainVal":
						station.ThisYear.DailyRain.Val = double.Parse(value);
						break;
					case "highDailyRainTime":
						station.ThisYear.DailyRain.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "highMonthlyRainVal":
						station.ThisYear.MonthlyRain.Val = double.Parse(value);
						break;
					case "highMonthlyRainTime":
						var dat = value.Split('/');  // MM/yyyy
						station.ThisYear.MonthlyRain.Ts = new DateTime(int.Parse(dat[1]), int.Parse(dat[0]), 1);
						break;
					case "longestDryPeriodVal":
						station.ThisYear.LongestDryPeriod.Val = int.Parse(value);
						break;
					case "longestDryPeriodTime":
						station.ThisYear.LongestDryPeriod.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					case "longestWetPeriodVal":
						station.ThisYear.LongestWetPeriod.Val = int.Parse(value);
						break;
					case "longestWetPeriodTime":
						station.ThisYear.LongestWetPeriod.Ts = Utils.ddmmyyyyStrToDate(value);
						break;
					default:
						result = 0;
						break;
				}
				station.WriteYearIniFile();
			}
			catch
			{
				result = 0;
			}
			return $"{{\"result\":\"{((result == 1) ? "Success" : "Failed")}\"}}";
		}



		internal string GetCurrentCond()
		{
			string fileName = cumulus.AppDir + "currentconditions.txt";
			var res = File.Exists(fileName) ? WebTags.ReadFileIntoString(fileName) : string.Empty;

			return $"{{\"data\":\"{res}\"}}";
		}

		internal string EditCurrentCond(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var result = SetCurrCondText(text);

			return $"{{\"result\":\"{(result ? "Success" : "Failed")}\"}}";
		}

		private bool SetCurrCondText(string currCondText)
		{
			var fileName = cumulus.AppDir + "currentconditions.txt";
			try
			{
				Cumulus.LogMessage("Writing current conditions to file...");

				File.WriteAllText(fileName, currCondText);
				return true;
			}
			catch (Exception e)
			{
				cumulus.LogExceptionMessage(e, "Error writing current conditions to file");
				return false;
			}
		}




		internal string EditRainToday(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var kvPair = text.Split('=');
			var raintodaystring = kvPair[1];

			if (!string.IsNullOrEmpty(raintodaystring))
			{
				try
				{
					var raintoday = double.Parse(raintodaystring, CultureInfo.InvariantCulture.NumberFormat);
					Cumulus.LogMessage("Before rain today edit, raintoday=" + (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum) + " Raindaystart=" + station.raindaystart.ToString(cumulus.RainFormat, invNum));
					station.RainToday = raintoday;
					station.raindaystart = station.Raincounter - ((station.RainToday ?? 0) / cumulus.Calib.Rain.Mult);
					Cumulus.LogMessage("After rain today edit,  raintoday=" + (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum) + " Raindaystart=" + station.raindaystart.ToString(cumulus.RainFormat, invNum));
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Edit rain today");
				}
			}

			var json = "{\"raintoday\":\"" + (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum) +
				"\",\"raincounter\":\"" + station.Raincounter.ToString(cumulus.RainFormat, invNum) +
				"\",\"startofdayrain\":\"" + station.raindaystart.ToString(cumulus.RainFormat, invNum) +
				"\",\"rainmult\":\"" + cumulus.Calib.Rain.Mult.ToString("F3", invNum) + "\"}";

			return json;
		}

		internal string GetRainTodayEditData()
		{
			var step = (cumulus.RainDPlaces == 1 ? "0.1" : "0.01");
			var json = "{\"raintoday\":\"" + (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum) +
				"\",\"raincounter\":\"" + station.Raincounter.ToString(cumulus.RainFormat, invNum) +
				"\",\"startofdayrain\":\"" + station.raindaystart.ToString(cumulus.RainFormat, invNum) +
				"\",\"rainmult\":\"" + cumulus.Calib.Rain.Mult.ToString("F3", invNum) +
				"\",\"step\":\"" + step + "\"}";

			return json;
		}



		private void AddLastHourRainEntry(DateTime ts, double rain)
		{
			var lasthourrain = new LastHourRainLog(ts, rain);

			hourRainLog.Add(lasthourrain);
		}

		private class LastHourRainLog
		{
			public readonly DateTime Timestamp;
			public readonly double Raincounter;

			public LastHourRainLog(DateTime ts, double rain)
			{
				Timestamp = ts;
				Raincounter = rain;
			}
		}

		private void RemoveOldRainData(DateTime ts)
		{
			var onehourago = ts.AddHours(-1);

			if (hourRainLog.Count <= 0) return;

			// there are entries to consider
			while ((hourRainLog.Count > 0) && (hourRainLog.First().Timestamp < onehourago))
			{
				// the oldest entry is older than 1 hour ago, delete it
				hourRainLog.RemoveAt(0);
			}
		}


		private class DbDateTimeDouble
		{
			[PrimaryKey]
			public DateTime Timestamp { get; set; }
			public double var { get; set; }
		}

		private class DbDateTimeInt
		{
			[PrimaryKey]
			public DateTime Timestamp { get; set; }
			public int var { get; set; }
		}


		private class DailyLogStuff
		{
			[PrimaryKey]
			public DateTime Timestamp { get; set; }
			public double? Temp { get; set; }
			public double? RainToday { get; set; }
			public double? WindAvg { get; set; }
		}

		private class LocalRec
		{
			public double Value { get; set; }
			public DateTime Ts { get; set; }

			public LocalRec(bool HighVal)
			{
				Value = HighVal ? Cumulus.DefaultHiVal : Cumulus.DefaultLoVal;
				Ts = DateTime.MinValue;
			}

			public string GetValString(string format = "")
			{
				if (Value == Cumulus.DefaultHiVal || Value == Cumulus.DefaultLoVal)
					return "-";
				else
					return Value.ToString(format);
			}
			public string GetTsString(string format = "")
			{
				if (Ts == DateTime.MinValue)
					return "-";
				else
					return Ts.ToString(format);
			}
		}
	}
}