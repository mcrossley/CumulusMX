using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack;
using EmbedIO;
using System.Threading.Tasks;

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
			json.Append($"\"highTempVal\":\"{station.AllTime.HighTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempVal\":\"{station.AllTime.LowTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointVal\":\"{station.AllTime.HighDewPoint.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointVal\":\"{station.AllTime.LowDewPoint.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempVal\":\"{station.AllTime.HighAppTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempVal\":\"{station.AllTime.LowAppTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeVal\":\"{station.AllTime.HighFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeVal\":\"{station.AllTime.LowFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexVal\":\"{station.AllTime.HighHumidex.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillVal\":\"{station.AllTime.LowChill.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexVal\":\"{station.AllTime.HighHeatIndex.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempVal\":\"{station.AllTime.HighMinTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempVal\":\"{station.AllTime.LowMaxTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeVal\":\"{station.AllTime.HighDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeVal\":\"{station.AllTime.LowDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
			// Records - Temperature timestamps
			json.Append($"\"highTempTime\":\"{station.AllTime.HighTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowTempTime\":\"{station.AllTime.LowTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDewPointTime\":\"{station.AllTime.HighDewPoint.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointTime\":\"{station.AllTime.LowDewPoint.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempTime\":\"{station.AllTime.HighAppTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempTime\":\"{station.AllTime.LowAppTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeTime\":\"{station.AllTime.HighFeelsLike.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeTime\":\"{station.AllTime.LowFeelsLike.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHumidexTime\":\"{station.AllTime.HighHumidex.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillTime\":\"{station.AllTime.LowChill.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexTime\":\"{station.AllTime.HighHeatIndex.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highMinTempTime\":\"{station.AllTime.HighMinTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempTime\":\"{station.AllTime.LowMaxTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeTime\":\"{station.AllTime.HighDailyTempRange.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeTime\":\"{station.AllTime.LowDailyTempRange.Ts.ToString(dateStampFormat)}\",");
			// Records - Humidity values
			json.Append($"\"highHumidityVal\":\"{station.AllTime.HighHumidity.Val.ToString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityVal\":\"{station.AllTime.LowHumidity.Val.ToString(cumulus.HumFormat)}\",");
			// Records - Humidity times
			json.Append($"\"highHumidityTime\":\"{station.AllTime.HighHumidity.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityTime\":\"{station.AllTime.LowHumidity.Ts.ToString(timeStampFormat)}\",");
			// Records - Pressure values
			json.Append($"\"highBarometerVal\":\"{station.AllTime.HighPress.Val.ToString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerVal\":\"{station.AllTime.LowPress.Val.ToString(cumulus.PressFormat)}\",");
			// Records - Pressure times
			json.Append($"\"highBarometerTime\":\"{station.AllTime.HighPress.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerTime\":\"{station.AllTime.LowPress.Ts.ToString(timeStampFormat)}\",");
			// Records - Wind values
			json.Append($"\"highGustVal\":\"{station.AllTime.HighGust.Val.ToString(cumulus.WindFormat)}\",");
			json.Append($"\"highWindVal\":\"{station.AllTime.HighWind.Val.ToString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindRunVal\":\"{station.AllTime.HighWindRun.Val.ToString(cumulus.WindRunFormat)}\",");
			// Records - Wind times
			json.Append($"\"highGustTime\":\"{station.AllTime.HighGust.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindTime\":\"{station.AllTime.HighWind.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindRunTime\":\"{station.AllTime.HighWindRun.Ts.ToString(dateStampFormat)}\",");
			// Records - Rain values
			json.Append($"\"highRainRateVal\":\"{station.AllTime.HighRainRate.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainVal\":\"{station.AllTime.HourlyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainVal\":\"{station.AllTime.DailyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highMonthlyRainVal\":\"{station.AllTime.MonthlyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"longestDryPeriodVal\":\"{station.AllTime.LongestDryPeriod.Val:f0}\",");
			json.Append($"\"longestWetPeriodVal\":\"{station.AllTime.LongestWetPeriod.Val:f0}\",");
			// Records - Rain times
			json.Append($"\"highRainRateTime\":\"{station.AllTime.HighRainRate.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainTime\":\"{station.AllTime.HourlyRain.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainTime\":\"{station.AllTime.DailyRain.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"highMonthlyRainTime\":\"{station.AllTime.MonthlyRain.Ts:MM/yyyy}\",");
			json.Append($"\"longestDryPeriodTime\":\"{station.AllTime.LongestDryPeriod.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"longestWetPeriodTime\":\"{station.AllTime.LongestWetPeriod.Ts.ToString(dateStampFormat)}\"");
			json.Append('}');

			return json.ToString();
		}

		internal async Task<string> GetRecordsDayFile(string recordType)
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var highTempVal = -999.0;
			var lowTempVal = 999.0;
			var highDewPtVal = highTempVal;
			var lowDewPtVal = lowTempVal;
			var highAppTempVal = highTempVal;
			var lowAppTempVal = lowTempVal;
			var highFeelsLikeVal = highTempVal;
			var lowFeelsLikeVal = lowTempVal;
			var highHumidexVal = highTempVal;
			var lowWindChillVal = lowTempVal;
			var highHeatIndVal = highTempVal;
			var highMinTempVal = highTempVal;
			var lowMaxTempVal = lowTempVal;
			var highTempRangeVal = highTempVal;
			var lowTempRangeVal = lowTempVal;
			var highHumVal = highTempVal;
			var lowHumVal = lowTempVal;
			var highBaroVal = highTempVal;
			var lowBaroVal = 99999.0;
			var highGustVal = highTempVal;
			var highWindVal = highTempVal;
			var highWindRunVal = highTempVal;
			var highRainRateVal = highTempVal;
			var highRainHourVal = highTempVal;
			var highRainDayVal = highTempVal;
			var highRainMonthVal = highTempVal;
			var dryPeriodVal = 0;
			var wetPeriodVal = 0;
			var highTempTime = new DateTime(1900, 01, 01);
			var lowTempTime = highTempTime;
			var highDewPtTime = highTempTime;
			var lowDewPtTime = highTempTime;
			var highAppTempTime = highTempTime;
			var lowAppTempTime = highTempTime;
			var highFeelsLikeTime = highTempTime;
			var lowFeelsLikeTime = highTempTime;
			var highHumidexTime = highTempTime;
			var lowWindChillTime = highTempTime;
			var highHeatIndTime = highTempTime;
			var highMinTempTime = highTempTime;
			var lowMaxTempTime = highTempTime;
			var highTempRangeTime = highTempTime;
			var lowTempRangeTime = highTempTime;
			var highHumTime = highTempTime;
			var lowHumTime = highTempTime;
			var highBaroTime = highTempTime;
			var lowBaroTime = highTempTime;
			var highGustTime = highTempTime;
			var highWindTime = highTempTime;
			var highWindRunTime = highTempTime;
			var highRainRateTime = highTempTime;
			var highRainHourTime = highTempTime;
			var highRainDayTime = highTempTime;
			var highRainMonthTime = highTempTime;
			var dryPeriodTime = highTempTime;
			var wetPeriodTime = highTempTime;

			var thisDate = highTempTime;
			DateTime startDate;
			switch (recordType)
			{
				case "alltime":
					startDate = highTempTime;
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
					startDate = highTempTime;
					break;
			}

			var rainThisMonth = 0.0;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;
			var isDryNow = false;
			var thisDateDry = highTempTime;
			var thisDateWet = highTempTime;
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
			var data = await station.DatabaseAsync.QueryAsync<DailyData>("select * from DayData where Timestamp >= ? order by Timestamp", startDate);

			if (data.Count > 0)
			{
				foreach (var rec in data)
				{
					// This assumes the day file is in date order!
					if (thisDate.Month != rec.Timestamp.Month)
					{
						// monthly rain
						if (rainThisMonth > highRainMonthVal)
						{
							highRainMonthVal = rainThisMonth;
							highRainMonthTime = thisDate;
						}
						// reset the date and counter for a new month
						thisDate = rec.Timestamp;
						rainThisMonth = 0;
					}
					// hi gust
					if (rec.HighGust.HasValue && rec.HighGust.Value > highGustVal && rec.HighGustTime.HasValue)
					{
						highGustVal = rec.HighGust.Value;
						highGustTime = rec.HighGustTime.Value;
					}
					// hi temp
					if (rec.HighTemp.HasValue && rec.HighTemp.Value > highTempVal && rec.HighTempTime.HasValue)
					{
						highTempVal = rec.HighTemp.Value;
						highTempTime = rec.HighTempTime.Value;
					}
					// lo temp
					if (rec.LowTemp.HasValue && rec.LowTemp.Value < lowTempVal && rec.LowTempTime.HasValue)
					{
						lowTempVal = rec.LowTemp.Value;
						lowTempTime = rec.LowTempTime.Value;
					}
					// hi min temp
					if (rec.LowTemp.HasValue && rec.LowTemp.Value > highMinTempVal && rec.LowTempTime.HasValue)
					{
						highMinTempVal = rec.LowTemp.Value;
						highMinTempTime = rec.LowTempTime.Value;
					}
					// lo max temp
					if (rec.HighTemp.HasValue && rec.HighTemp.Value < lowMaxTempVal && rec.HighTempTime.HasValue)
					{
						lowMaxTempVal = rec.HighTemp.Value;
						lowMaxTempTime = rec.HighTempTime.Value;
					}
					// hi temp range
					if (rec.LowTemp.HasValue && rec.HighTemp.HasValue && (rec.HighTemp.Value - rec.LowTemp.Value) > highTempRangeVal)
					{
						highTempRangeVal = rec.HighTemp.Value - rec.LowTemp.Value;
						highTempRangeTime = rec.Timestamp;
					}
					// lo temp range
					if (rec.LowTemp.HasValue && rec.HighTemp.HasValue && (rec.HighTemp.Value - rec.LowTemp.Value) < lowTempRangeVal)
					{
						lowTempRangeVal = rec.HighTemp.Value - rec.LowTemp.Value;
						lowTempRangeTime = rec.Timestamp;
					}
					// lo baro
					if (rec.LowPress.HasValue && rec.LowPress.Value < lowBaroVal && rec.LowPressTime.HasValue)
					{
						lowBaroVal = rec.LowPress.Value;
						lowBaroTime = rec.LowPressTime.Value;
					}
					// hi baro
					if (rec.HighPress.HasValue && rec.HighPress.Value > highBaroVal && rec.HighPressTime.HasValue)
					{
						highBaroVal = rec.HighPress.Value;
						highBaroTime = rec.HighPressTime.Value;
					}
					// hi rain rate
					if (rec.HighRainRate.HasValue && rec.HighRainRate.Value > highRainRateVal && rec.HighRainRateTime.HasValue)
					{
						highRainRateVal = rec.HighRainRate.Value;
						highRainRateTime = rec.HighRainRateTime.Value;
					}
					// hi rain day
					if (rec.TotalRain.HasValue)
					{
						// monthly rain
						rainThisMonth += rec.TotalRain.Value;

						if (rec.TotalRain.Value > highRainDayVal)
						{
							highRainDayVal = rec.TotalRain.Value;
							highRainDayTime = rec.Timestamp;
						}
					}


					// dry/wet period
					if (Convert.ToInt32(rec.TotalRain * 1000) >= rainThreshold)
					{
						if (isDryNow)
						{
							currentWetPeriod = 1;
							isDryNow = false;
							if (currentDryPeriod > dryPeriodVal)
							{
								dryPeriodVal = currentDryPeriod;
								dryPeriodTime = thisDateDry;
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
							if (currentWetPeriod > wetPeriodVal)
							{
								wetPeriodVal = currentWetPeriod;
								wetPeriodTime = thisDateWet;
							}
							currentWetPeriod = 0;
						}
					}
					// hi wind run
					if (rec.WindRun.HasValue && rec.WindRun.Value > highWindRunVal)
					{
						highWindRunVal = rec.WindRun.Value;
						highWindRunTime = rec.Timestamp;
					}
					// hi wind
					if (rec.HighAvgWind.HasValue && rec.HighAvgWind.Value > highWindVal && rec.HighAvgWindTime.HasValue)
					{
						highWindVal = rec.HighAvgWind.Value;
						highWindTime = rec.HighAvgWindTime.Value;
					}
					// lo humidity
					if (rec.LowHumidity.HasValue && rec.LowHumidity.Value < lowHumVal && rec.LowHumidityTime.HasValue)
					{
						lowHumVal = rec.LowHumidity.Value;
						lowHumTime = rec.LowHumidityTime.Value;
					}
					// hi humidity
					if (rec.HighHumidity.HasValue && rec.HighHumidity > highHumVal && rec.HighHumidityTime.HasValue)
					{
						highHumVal = rec.HighHumidity.Value;
						highHumTime = rec.HighHumidityTime.Value;
					}
					// hi heat index
					if (rec.HighHeatIndex.HasValue && rec.HighHeatIndex.Value > highHeatIndVal && rec.HighHeatIndexTime.HasValue)
					{
						highHeatIndVal = rec.HighHeatIndex.Value;
						highHeatIndTime = rec.HighHeatIndexTime.Value;
					}
					// hi app temp
					if (rec.HighAppTemp.HasValue && rec.HighAppTemp.Value > highAppTempVal && rec.HighAppTempTime.HasValue)
					{
						highAppTempVal = rec.HighAppTemp.Value;
						highAppTempTime = rec.HighAppTempTime.Value;
					}
					// lo app temp
					if (rec.LowAppTemp.HasValue && rec.LowAppTemp < lowAppTempVal && rec.LowAppTempTime.HasValue)
					{
						lowAppTempVal = rec.LowAppTemp.Value;
						lowAppTempTime = rec.LowAppTempTime.Value;
					}
					// hi rain hour
					if (rec.HighHourlyRain.HasValue && rec.HighHourlyRain.Value > highRainHourVal && rec.HighHourlyRainTime.HasValue)
					{
						highRainHourVal = rec.HighHourlyRain.Value;
						highRainHourTime = rec.HighHourlyRainTime.Value;
					}
					// lo wind chill
					if (rec.LowWindChill.HasValue && rec.LowWindChill.Value < lowWindChillVal && rec.LowWindChillTime.HasValue)
					{
						lowWindChillVal = rec.LowWindChill.Value;
						lowWindChillTime = rec.LowWindChillTime.Value;
					}
					// hi dewpt
					if (rec.HighDewPoint.HasValue && rec.HighDewPoint.Value > highDewPtVal && rec.HighDewPointTime.HasValue)
					{
						highDewPtVal = rec.HighDewPoint.Value;
						highDewPtTime = rec.HighDewPointTime.Value;
					}
					// lo dewpt
					if (rec.LowDewPoint.HasValue && rec.LowDewPoint.Value < lowDewPtVal && rec.LowDewPointTime.HasValue)
					{
						lowDewPtVal = rec.LowDewPoint.Value;
						lowDewPtTime = rec.LowDewPointTime.Value;
					}
					// hi feels like
					if (rec.HighFeelsLike.HasValue && rec.HighFeelsLike.Value > highFeelsLikeVal && rec.HighFeelsLikeTime.HasValue)
					{
						highFeelsLikeVal = rec.HighFeelsLike.Value;
						highFeelsLikeTime = rec.HighFeelsLikeTime.Value;
					}
					// lo feels like
					if (rec.LowFeelsLike.HasValue && rec.LowFeelsLike < lowFeelsLikeVal && rec.LowFeelsLikeTime.HasValue)
					{
						lowFeelsLikeVal = rec.LowFeelsLike.Value;
						lowFeelsLikeTime = rec.LowFeelsLikeTime.Value;
					}
					// hi humidex
					if (rec.HighHumidex.HasValue && rec.HighHumidex.Value > highHumidexVal && rec.HighHumidexTime.HasValue)
					{
						highHumidexVal = rec.HighHumidex.Value;
						highHumidexTime = rec.HighHumidexTime.Value;
					}
				}

				// We need to check if the run or wet/dry days at the end of logs exceeds any records
				if (currentWetPeriod > wetPeriodVal)
				{
					wetPeriodVal = currentWetPeriod;
					wetPeriodTime = thisDateWet;
				}
				if (currentDryPeriod > dryPeriodVal)
				{
					dryPeriodVal = currentDryPeriod;
					dryPeriodTime = thisDateDry;
				}

				json.Append($"\"highTempValDayfile\":\"{highTempVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highTempTimeDayfile\":\"{highTempTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowTempValDayfile\":\"{lowTempVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"lowTempTimeDayfile\":\"{lowTempTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highDewPointValDayfile\":\"{highDewPtVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highDewPointTimeDayfile\":\"{highDewPtTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowDewPointValDayfile\":\"{lowDewPtVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"lowDewPointTimeDayfile\":\"{lowDewPtTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highApparentTempValDayfile\":\"{highAppTempVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highApparentTempTimeDayfile\":\"{highAppTempTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowApparentTempValDayfile\":\"{lowAppTempVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"lowApparentTempTimeDayfile\":\"{lowAppTempTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highFeelsLikeValDayfile\":\"{highFeelsLikeVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highFeelsLikeTimeDayfile\":\"{highFeelsLikeTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowFeelsLikeValDayfile\":\"{lowFeelsLikeVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"lowFeelsLikeTimeDayfile\":\"{lowFeelsLikeTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highHumidexValDayfile\":\"{highHumidexVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highHumidexTimeDayfile\":\"{highHumidexTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowWindChillValDayfile\":\"{lowWindChillVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"lowWindChillTimeDayfile\":\"{lowWindChillTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highHeatIndexValDayfile\":\"{highHeatIndVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highHeatIndexTimeDayfile\":\"{highHeatIndTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highMinTempValDayfile\":\"{highMinTempVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highMinTempTimeDayfile\":\"{highMinTempTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowMaxTempValDayfile\":\"{lowMaxTempVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"lowMaxTempTimeDayfile\":\"{lowMaxTempTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highDailyTempRangeValDayfile\":\"{highTempRangeVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"highDailyTempRangeTimeDayfile\":\"{highTempRangeTime.ToString(dateStampFormat)}\",");
				json.Append($"\"lowDailyTempRangeValDayfile\":\"{lowTempRangeVal.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"lowDailyTempRangeTimeDayfile\":\"{lowTempRangeTime.ToString(dateStampFormat)}\",");
				json.Append($"\"highHumidityValDayfile\":\"{highHumVal.ToString(cumulus.HumFormat)}\",");
				json.Append($"\"highHumidityTimeDayfile\":\"{highHumTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowHumidityValDayfile\":\"{lowHumVal.ToString(cumulus.HumFormat)}\",");
				json.Append($"\"lowHumidityTimeDayfile\":\"{lowHumTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highBarometerValDayfile\":\"{highBaroVal.ToString(cumulus.PressFormat)}\",");
				json.Append($"\"highBarometerTimeDayfile\":\"{highBaroTime.ToString(timeStampFormat)}\",");
				json.Append($"\"lowBarometerValDayfile\":\"{lowBaroVal.ToString(cumulus.PressFormat)}\",");
				json.Append($"\"lowBarometerTimeDayfile\":\"{lowBaroTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highGustValDayfile\":\"{highGustVal.ToString(cumulus.WindFormat)}\",");
				json.Append($"\"highGustTimeDayfile\":\"{highGustTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highWindValDayfile\":\"{highWindVal.ToString(cumulus.WindAvgFormat)}\",");
				json.Append($"\"highWindTimeDayfile\":\"{highWindTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highWindRunValDayfile\":\"{highWindRunVal.ToString(cumulus.WindRunFormat)}\",");
				json.Append($"\"highWindRunTimeDayfile\":\"{highWindRunTime.ToString(dateStampFormat)}\",");
				json.Append($"\"highRainRateValDayfile\":\"{highRainRateVal.ToString(cumulus.RainFormat)}\",");
				json.Append($"\"highRainRateTimeDayfile\":\"{highRainRateTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highHourlyRainValDayfile\":\"{highRainHourVal.ToString(cumulus.RainFormat)}\",");
				json.Append($"\"highHourlyRainTimeDayfile\":\"{highRainHourTime.ToString(timeStampFormat)}\",");
				json.Append($"\"highDailyRainValDayfile\":\"{highRainDayVal.ToString(cumulus.RainFormat)}\",");
				json.Append($"\"highDailyRainTimeDayfile\":\"{highRainDayTime.ToString(dateStampFormat)}\",");
				if (recordType != "thismonth")
				{
					json.Append($"\"highMonthlyRainValDayfile\":\"{highRainMonthVal.ToString(cumulus.RainFormat)}\",");
					json.Append($"\"highMonthlyRainTimeDayfile\":\"{highRainMonthTime:MM/yyyy}\",");
				}
				json.Append($"\"longestDryPeriodValDayfile\":\"{dryPeriodVal}\",");
				json.Append($"\"longestDryPeriodTimeDayfile\":\"{dryPeriodTime.ToString(dateStampFormat)}\",");
				json.Append($"\"longestWetPeriodValDayfile\":\"{wetPeriodVal}\",");
				json.Append($"\"longestWetPeriodTimeDayfile\":\"{wetPeriodTime.ToString(dateStampFormat)}\"");
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
					datefrom = new DateTime(now.Year, 1, 2);
					break;
				case "thismonth":
					now = DateTime.Now;
					datefrom = new DateTime(now.Year, now.Month, 2);
					break;
				default:
					datefrom = cumulus.RecordsBeganDate;
					break;
			}
			datefrom = new DateTime(datefrom.Year, datefrom.Month, datefrom.Day, 0, 0, 0);
			var dateto = DateTime.Now;
			dateto = new DateTime(dateto.Year, dateto.Month, 2, 0, 0, 0);
			var filedate = datefrom;

			var logFile = cumulus.GetLogFileName(filedate);
			var started = false;
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

			var highTempVal = -999.0;
			var lowTempVal = 999.0;
			var highDewPtVal = highTempVal;
			var lowDewPtVal = lowTempVal;
			var highAppTempVal = highTempVal;
			var lowAppTempVal = lowTempVal;
			var highFeelsLikeVal = highTempVal;
			var lowFeelsLikeVal = lowTempVal;
			var highHumidexVal = highTempVal;
			var lowWindChillVal = lowTempVal;
			var highHeatIndVal = highTempVal;
			var highMinTempVal = highTempVal;
			var lowMaxTempVal = lowTempVal;
			var highTempRangeVal = highTempVal;
			var lowTempRangeVal = lowTempVal;
			var highHumVal = highTempVal;
			var lowHumVal = lowTempVal;
			var highBaroVal = highTempVal;
			var lowBaroVal = 99999.0;
			var highGustVal = highTempVal;
			var highWindVal = highTempVal;
			var highWindRunVal = highTempVal;
			var highRainRateVal = highTempVal;
			var highRainHourVal = highTempVal;
			var highRainDayVal = highTempVal;
			var highRainMonthVal = highTempVal;
			var dryPeriodVal = 0;
			var wetPeriodVal = 0;

			var highTempTime = new DateTime(1900, 01, 01);
			var lowTempTime = highTempTime;
			var highDewPtTime = highTempTime;
			var lowDewPtTime = highTempTime;
			var highAppTempTime = highTempTime;
			var lowAppTempTime = highTempTime;
			var highFeelsLikeTime = highTempTime;
			var lowFeelsLikeTime = highTempTime;
			var highHumidexTime = highTempTime;
			var lowWindChillTime = highTempTime;
			var highHeatIndTime = highTempTime;
			var highMinTempTime = highTempTime;
			var lowMaxTempTime = highTempTime;
			var highTempRangeTime = highTempTime;
			var lowTempRangeTime = highTempTime;
			var highHumTime = highTempTime;
			var lowHumTime = highTempTime;
			var highBaroTime = highTempTime;
			var lowBaroTime = highTempTime;
			var highGustTime = highTempTime;
			var highWindTime = highTempTime;
			var highWindRunTime = highTempTime;
			var highRainRateTime = highTempTime;
			var highRainHourTime = highTempTime;
			var highRainDayTime = highTempTime;
			var highRainMonthTime = highTempTime;
			var dryPeriodTime = highTempTime;
			var wetPeriodTime = highTempTime;

			var currentDay = datefrom;
			var dayHighTemp = highTempVal;
			DateTime dayHighTempTime = highTempTime;
			double dayLowTemp = lowTempVal;
			DateTime dayLowTempTime = highTempTime;
			double dayWindRun = 0;
			double dayRain = 0;


			var thisDateDry = highTempTime;
			var thisDateWet = highTempTime;

			var totalRainfall = 0.0;

			var watch = System.Diagnostics.Stopwatch.StartNew();

			hourRainLog.Clear();

			while (!finished)
			{
				double monthlyRain = 0;

				if (File.Exists(logFile))
				{
					cumulus.LogDebugMessage($"GetAllTimeRecLogFile: Processing log file - {logFile}");
					var linenum = 0;
					try
					{
						var logfile = File.ReadAllLines(logFile);

						foreach (var line in logfile)
						{
							// process each record in the file
							linenum++;

							var rec = station.ParseLogFileRec(line, true);

							// We need to work in meteo dates not clock dates for day hi/lows
							var metoDate = rec.Date.AddHours(cumulus.GetHourInc());

							if (!started)
							{
								lastentrydate = rec.Date;
								currentDay = metoDate;
								started = true;
							}

							// low chill
							if (rec.WindChill > -9999 && rec.WindChill < lowWindChillVal)
							{
								lowWindChillVal = rec.WindChill;
								lowWindChillTime = rec.Date;
							}
							// hi heat
							if (rec.HeatIndex > -9999 && rec.HeatIndex > highHeatIndVal)
							{
								highHeatIndVal = rec.HeatIndex;
								highHeatIndTime = rec.Date;
							}
							// hi/low appt
							if (rec.ApparentTemperature > -9999)
							{
								if (rec.ApparentTemperature > highAppTempVal)
								{
									highAppTempVal = rec.ApparentTemperature;
									highAppTempTime = rec.Date;
								}
								if (rec.ApparentTemperature < lowAppTempVal)
								{
									lowAppTempVal = rec.ApparentTemperature;
									lowAppTempTime = rec.Date;
								}
							}
							// hi/low feels like
							if (rec.FeelsLike > -9999)
							{
								if (rec.FeelsLike > highFeelsLikeVal)
								{
									highFeelsLikeVal = rec.FeelsLike;
									highFeelsLikeTime = rec.Date;
								}
								if (rec.FeelsLike < lowFeelsLikeVal)
								{
									lowFeelsLikeVal = rec.FeelsLike;
									lowFeelsLikeTime = rec.Date;
								}
							}

							// hi/low humidex
							if (rec.Humidex > -9999)
							{
								if (rec.Humidex > highHumidexVal)
								{
									highHumidexVal = rec.Humidex;
									highHumidexTime = rec.Date;
								}
							}

							// hi temp
							if (rec.OutdoorTemperature > highTempVal)
							{
								highTempVal = rec.OutdoorTemperature;
								highTempTime = rec.Date;
							}
							// lo temp
							if (rec.OutdoorTemperature < lowTempVal)
							{
								lowTempVal = rec.OutdoorTemperature;
								lowTempTime = rec.Date;
							}
							// hi dewpoint
							if (rec.OutdoorDewpoint > highDewPtVal)
							{
								highDewPtVal = rec.OutdoorDewpoint;
								highDewPtTime = rec.Date;
							}
							// low dewpoint
							if (rec.OutdoorDewpoint < lowDewPtVal)
							{
								lowDewPtVal = rec.OutdoorDewpoint;
								lowDewPtTime = rec.Date;
							}
							// hi hum
							if (rec.OutdoorHumidity > highHumVal)
							{
								highHumVal = rec.OutdoorHumidity;
								highHumTime = rec.Date;
							}
							// lo hum
							if (rec.OutdoorHumidity < lowHumVal)
							{
								lowHumVal = rec.OutdoorHumidity;
								lowHumTime = rec.Date;
							}
							// hi baro
							if (rec.Pressure > highBaroVal)
							{
								highBaroVal = rec.Pressure;
								highBaroTime = rec.Date;
							}
							// lo hum
							if (rec.Pressure < lowBaroVal)
							{
								lowBaroVal = rec.Pressure;
								lowBaroTime = rec.Date;
							}
							// hi gust
							if (rec.RecentMaxGust > highGustVal)
							{
								highGustVal = rec.RecentMaxGust;
								highGustTime = rec.Date;
							}
							// hi wind
							if (rec.WindAverage > highWindVal)
							{
								highWindVal = rec.WindAverage;
								highWindTime = rec.Date;
							}
							// hi rain rate
							if (rec.RainRate > highRainRateVal)
							{
								highRainRateVal = rec.RainRate;
								highRainRateTime = rec.Date;
							}

							if (monthlyRain > highRainMonthVal)
							{
								highRainMonthVal = monthlyRain;
								highRainMonthTime = rec.Date;
							}

							// same meteo day
							if (currentDay.Day == metoDate.Day && currentDay.Month == metoDate.Month && currentDay.Year == metoDate.Year)
							{
								if (rec.OutdoorTemperature > dayHighTemp)
								{
									dayHighTemp = rec.OutdoorTemperature;
									dayHighTempTime = rec.Date;
								}

								if (rec.OutdoorTemperature < dayLowTemp)
								{
									dayLowTemp = rec.OutdoorTemperature;
									dayLowTempTime = rec.Date;
								}

								if (dayRain < rec.RainToday)
								{
									dayRain = rec.RainToday;
								}

								dayWindRun += rec.Date.Subtract(lastentrydate).TotalHours * rec.WindAverage;
							}
							else // new meteo day
							{
								if (dayHighTemp < lowMaxTempVal)
								{
									lowMaxTempVal = dayHighTemp;
									lowMaxTempTime = dayHighTempTime;
								}
								if (dayLowTemp > highMinTempVal)
								{
									highMinTempVal = dayLowTemp;
									highMinTempTime = dayLowTempTime;
								}
								if (dayHighTemp - dayLowTemp > highTempRangeVal)
								{
									highTempRangeVal = dayHighTemp - dayLowTemp;
									highTempRangeTime = currentDay;
								}
								if (dayHighTemp - dayLowTemp < lowTempRangeVal)
								{
									lowTempRangeVal = dayHighTemp - dayLowTemp;
									lowTempRangeTime = currentDay;
								}
								if (dayWindRun > highWindRunVal)
								{
									highWindRunVal = dayWindRun;
									highWindRunTime = currentDay;
								}
								if (dayRain > highRainDayVal)
								{
									highRainDayVal = dayRain;
									highRainDayTime = currentDay;
								}
								monthlyRain += dayRain;

								// dry/wet period
								if (Convert.ToInt32(dayRain * 1000) >= rainThreshold)
								{
									if (isDryNow)
									{
										currentWetPeriod = 1;
										isDryNow = false;
										if (currentDryPeriod > dryPeriodVal)
										{
											dryPeriodVal = currentDryPeriod;
											dryPeriodTime = thisDateDry;
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
										if (currentWetPeriod > wetPeriodVal)
										{
											wetPeriodVal = currentWetPeriod;
											wetPeriodTime = thisDateWet;
										}
										currentWetPeriod = 0;
									}
								}

								currentDay = metoDate;
								dayHighTemp = rec.OutdoorTemperature;
								dayLowTemp = rec.OutdoorTemperature;
								dayWindRun = 0;
								totalRainfall += dayRain;
								dayRain = 0;
							}

							// hourly rain
							/*
							 * need to track what the rainfall has been in the last rolling hour
							 * across day rollovers where the count resets
							 */
							AddLastHourRainEntry(rec.Date, totalRainfall + dayRain);
							RemoveOldRainData(rec.Date);

							var rainThisHour = hourRainLog.Last().Raincounter - hourRainLog.First().Raincounter;
							if (rainThisHour > highRainHourVal)
							{
								highRainHourVal = rainThisHour;
								highRainHourTime = rec.Date;
							}

							lastentrydate = rec.Date;
							//lastRainMidnight = rainMidnight;
						}
					}
					catch (Exception e)
					{
						cumulus.LogExceptionMessage(e, $"GetRecordsLogFile: Error at line {linenum} of {logFile}");
						Cumulus.LogMessage("Please edit the file to correct the error");
					}
				}
				else
				{
					cumulus.LogDebugMessage($"GetAllTimeRecLogFile: Log file  not found - {logFile}");
				}
				if (filedate >= dateto)
				{
					finished = true;
					cumulus.LogDebugMessage("GetAllTimeRecLogFile: Finished processing the log files");
				}
				else
				{
					cumulus.LogDebugMessage($"GetAllTimeRecLogFile: Finished processing log file - {logFile}");
					filedate = filedate.AddMonths(1);
					logFile = cumulus.GetLogFileName(filedate);
				}
			}

			// We need to check if the run or wet/dry days at the end of logs exceeds any records
			if (currentWetPeriod > wetPeriodVal)
			{
				wetPeriodVal = currentWetPeriod;
				wetPeriodTime = thisDateWet;
			}
			if (currentDryPeriod > dryPeriodVal)
			{
				dryPeriodVal = currentDryPeriod;
				dryPeriodTime = thisDateDry;
			}

			json.Append($"\"highTempValLogfile\":\"{highTempVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highTempTimeLogfile\":\"{highTempTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowTempValLogfile\":\"{lowTempVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempTimeLogfile\":\"{lowTempTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highDewPointValLogfile\":\"{highDewPtVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointTimeLogfile\":\"{highDewPtTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointValLogfile\":\"{lowDewPtVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointTimeLogfile\":\"{lowDewPtTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempValLogfile\":\"{highAppTempVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempTimeLogfile\":\"{highAppTempTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempValLogfile\":\"{lowAppTempVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempTimeLogfile\":\"{lowAppTempTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeValLogfile\":\"{highFeelsLikeVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeTimeLogfile\":\"{highFeelsLikeTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeValLogfile\":\"{lowFeelsLikeVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeTimeLogfile\":\"{lowFeelsLikeTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highHumidexValLogfile\":\"{highHumidexVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexTimeLogfile\":\"{highHumidexTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillValLogfile\":\"{lowWindChillVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillTimeLogfile\":\"{lowWindChillTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexValLogfile\":\"{highHeatIndVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexTimeLogfile\":\"{highHeatIndTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highMinTempValLogfile\":\"{highMinTempVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempTimeLogfile\":\"{highMinTempTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempValLogfile\":\"{lowMaxTempVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempTimeLogfile\":\"{lowMaxTempTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeValLogfile\":\"{highTempRangeVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeTimeLogfile\":\"{highTempRangeTime.ToString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeValLogfile\":\"{lowTempRangeVal.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeTimeLogfile\":\"{lowTempRangeTime.ToString(dateStampFormat)}\",");
			json.Append($"\"highHumidityValLogfile\":\"{highHumVal.ToString(cumulus.HumFormat)}\",");
			json.Append($"\"highHumidityTimeLogfile\":\"{highHumTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityValLogfile\":\"{lowHumVal.ToString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityTimeLogfile\":\"{lowHumTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highBarometerValLogfile\":\"{highBaroVal.ToString(cumulus.PressFormat)}\",");
			json.Append($"\"highBarometerTimeLogfile\":\"{highBaroTime.ToString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerValLogfile\":\"{lowBaroVal.ToString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerTimeLogfile\":\"{lowBaroTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highGustValLogfile\":\"{highGustVal.ToString(cumulus.WindFormat)}\",");
			json.Append($"\"highGustTimeLogfile\":\"{highGustTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindValLogfile\":\"{highWindVal.ToString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindTimeLogfile\":\"{highWindTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindRunValLogfile\":\"{highWindRunVal.ToString(cumulus.WindRunFormat)}\",");
			json.Append($"\"highWindRunTimeLogfile\":\"{highWindRunTime.ToString(dateStampFormat)}\",");
			json.Append($"\"highRainRateValLogfile\":\"{highRainRateVal.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highRainRateTimeLogfile\":\"{highRainRateTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainValLogfile\":\"{highRainHourVal.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainTimeLogfile\":\"{highRainHourTime.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainValLogfile\":\"{highRainDayVal.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainTimeLogfile\":\"{highRainDayTime.ToString(dateStampFormat)}\",");
			json.Append($"\"highMonthlyRainValLogfile\":\"{highRainMonthVal.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highMonthlyRainTimeLogfile\":\"{highRainMonthTime.ToString($"MM/yyyy")}\",");
			if (recordType == "alltime")
			{
				json.Append($"\"longestDryPeriodValLogfile\":\"{dryPeriodVal}\",");
				json.Append($"\"longestDryPeriodTimeLogfile\":\"{dryPeriodTime.ToString(dateStampFormat)}\",");
				json.Append($"\"longestWetPeriodValLogfile\":\"{wetPeriodVal}\",");
				json.Append($"\"longestWetPeriodTimeLogfile\":\"{wetPeriodTime.ToString(dateStampFormat)}\"");
			}
			else
			{
				json.Append($"\"longestDryPeriodValLogfile\":\"{dryPeriodVal}*\",");
				json.Append($"\"longestDryPeriodTimeLogfile\":\"{dryPeriodTime.ToString(dateStampFormat)}*\",");
				json.Append($"\"longestWetPeriodValLogfile\":\"{wetPeriodVal}*\",");
				json.Append($"\"longestWetPeriodTimeLogfile\":\"{wetPeriodTime.ToString(dateStampFormat)}*\"");
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
							var datstr = "01/" + dt[0] + "/" + dt[1].Substring(2, 2);
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
				json.Append($"\"{m}-highTempVal\":\"{station.MonthlyRecs[m].HighTemp.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowTempVal\":\"{station.MonthlyRecs[m].LowTemp.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDewPointVal\":\"{station.MonthlyRecs[m].HighDewPoint.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDewPointVal\":\"{station.MonthlyRecs[m].LowDewPoint.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highApparentTempVal\":\"{station.MonthlyRecs[m].HighAppTemp.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowApparentTempVal\":\"{station.MonthlyRecs[m].LowAppTemp.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeVal\":\"{station.MonthlyRecs[m].HighFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeVal\":\"{station.MonthlyRecs[m].LowFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHumidexVal\":\"{station.MonthlyRecs[m].HighHumidex.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowWindChillVal\":\"{station.MonthlyRecs[m].LowChill.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHeatIndexVal\":\"{station.MonthlyRecs[m].HighHeatIndex.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highMinTempVal\":\"{station.MonthlyRecs[m].HighMinTemp.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowMaxTempVal\":\"{station.MonthlyRecs[m].LowMaxTemp.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeVal\":\"{station.MonthlyRecs[m].HighDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeVal\":\"{station.MonthlyRecs[m].LowDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
				// Records - Temperature timestamps
				json.Append($"\"{m}-highTempTime\":\"{station.MonthlyRecs[m].HighTemp.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowTempTime\":\"{station.MonthlyRecs[m].LowTemp.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDewPointTime\":\"{station.MonthlyRecs[m].HighDewPoint.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowDewPointTime\":\"{station.MonthlyRecs[m].LowDewPoint.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highApparentTempTime\":\"{station.MonthlyRecs[m].HighAppTemp.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowApparentTempTime\":\"{station.MonthlyRecs[m].LowAppTemp.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeTime\":\"{station.MonthlyRecs[m].HighFeelsLike.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeTime\":\"{station.MonthlyRecs[m].LowFeelsLike.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHumidexTime\":\"{station.MonthlyRecs[m].HighHumidex.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowWindChillTime\":\"{station.MonthlyRecs[m].LowChill.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHeatIndexTime\":\"{station.MonthlyRecs[m].HighHeatIndex.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highMinTempTime\":\"{station.MonthlyRecs[m].HighMinTemp.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowMaxTempTime\":\"{station.MonthlyRecs[m].LowMaxTemp.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeTime\":\"{station.MonthlyRecs[m].HighDailyTempRange.Ts.ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeTime\":\"{station.MonthlyRecs[m].LowDailyTempRange.Ts.ToString(dateStampFormat)}\",");
				// Records - Humidity values
				json.Append($"\"{m}-highHumidityVal\":\"{station.MonthlyRecs[m].HighHumidity.Val.ToString(cumulus.HumFormat)}\",");
				json.Append($"\"{m}-lowHumidityVal\":\"{station.MonthlyRecs[m].LowHumidity.Val.ToString(cumulus.HumFormat)}\",");
				// Records - Humidity times
				json.Append($"\"{m}-highHumidityTime\":\"{station.MonthlyRecs[m].HighHumidity.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowHumidityTime\":\"{station.MonthlyRecs[m].LowHumidity.Ts.ToString(timeStampFormat)}\",");
				// Records - Pressure values
				json.Append($"\"{m}-highBarometerVal\":\"{station.MonthlyRecs[m].HighPress.Val.ToString(cumulus.PressFormat)}\",");
				json.Append($"\"{m}-lowBarometerVal\":\"{station.MonthlyRecs[m].LowPress.Val.ToString(cumulus.PressFormat)}\",");
				// Records - Pressure times
				json.Append($"\"{m}-highBarometerTime\":\"{station.MonthlyRecs[m].HighPress.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowBarometerTime\":\"{station.MonthlyRecs[m].LowPress.Ts.ToString(timeStampFormat)}\",");
				// Records - Wind values
				json.Append($"\"{m}-highGustVal\":\"{station.MonthlyRecs[m].HighGust.Val.ToString(cumulus.WindFormat)}\",");
				json.Append($"\"{m}-highWindVal\":\"{station.MonthlyRecs[m].HighWind.Val.ToString(cumulus.WindAvgFormat)}\",");
				json.Append($"\"{m}-highWindRunVal\":\"{station.MonthlyRecs[m].HighWindRun.Val.ToString(cumulus.WindRunFormat)}\",");
				// Records - Wind times
				json.Append($"\"{m}-highGustTime\":\"{station.MonthlyRecs[m].HighGust.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindTime\":\"{station.MonthlyRecs[m].HighWind.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindRunTime\":\"{station.MonthlyRecs[m].HighWindRun.Ts.ToString(dateStampFormat)}\",");
				// Records - Rain values
				json.Append($"\"{m}-highRainRateVal\":\"{station.MonthlyRecs[m].HighRainRate.Val.ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highHourlyRainVal\":\"{station.MonthlyRecs[m].HourlyRain.Val.ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highDailyRainVal\":\"{station.MonthlyRecs[m].DailyRain.Val.ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainVal\":\"{station.MonthlyRecs[m].MonthlyRain.Val.ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-longestDryPeriodVal\":\"{station.MonthlyRecs[m].LongestDryPeriod.Val:f0}\",");
				json.Append($"\"{m}-longestWetPeriodVal\":\"{station.MonthlyRecs[m].LongestWetPeriod.Val:f0}\",");
				// Records - Rain times
				json.Append($"\"{m}-highRainRateTime\":\"{station.MonthlyRecs[m].HighRainRate.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHourlyRainTime\":\"{station.MonthlyRecs[m].HourlyRain.Ts.ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyRainTime\":\"{station.MonthlyRecs[m].DailyRain.Ts.ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainTime\":\"{station.MonthlyRecs[m].MonthlyRain.Ts:MM/yyyy}\",");
				json.Append($"\"{m}-longestDryPeriodTime\":\"{station.MonthlyRecs[m].LongestDryPeriod.Ts.ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-longestWetPeriodTime\":\"{station.MonthlyRecs[m].LongestWetPeriod.Ts.ToString(dateStampFormat)}\",");
			}
			json.Remove(json.Length - 1, 1);
			json.Append('}');

			return json.ToString();
		}

		internal async Task<string> GetMonthlyRecDayFile()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var highTempVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowTempVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highDewPtVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowDewPtVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highAppTempVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowAppTempVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highFeelsLikeVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowFeelsLikeVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highHumidexVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowWindChillVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highHeatIndVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highMinTempVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowMaxTempVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highTempRangeVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowTempRangeVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highHumVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowHumVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highBaroVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowBaroVal = new double[] { 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999 };
			var highGustVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highWindVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highWindRunVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainRateVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainHourVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainDayVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainMonthVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var dryPeriodVal = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			var wetPeriodVal = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			var thisDate = new DateTime(1900, 01, 01);
			var highTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highDewPtTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowDewPtTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highAppTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowAppTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highFeelsLikeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowFeelsLikeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highHumidexTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowWindChillTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highHeatIndTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highMinTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowMaxTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highTempRangeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowTempRangeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highHumTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowHumTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highBaroTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowBaroTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highGustTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highWindTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highWindRunTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainRateTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainHourTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainDayTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainMonthTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var dryPeriodTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var wetPeriodTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };

			var rainThisMonth = 0.0;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;
			var isDryNow = false;
			var thisDateDry = thisDate;
			var thisDateWet = thisDate;
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
			var data = await station.DatabaseAsync.QueryAsync<DailyData>("select * from DayData order by Timestamp");

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
					}

					// This assumes the day file is in date order!
					if (thisDate.Month != loggedDate.Month)
					{
						var offset = thisDate.Month - 1;
						// monthly rain
						if (rainThisMonth > highRainMonthVal[offset])
						{
							highRainMonthVal[offset] = rainThisMonth;
							highRainMonthTime[offset] = thisDate;
						}
						// reset the date and counter for a new month
						thisDate = loggedDate;
						rainThisMonth = 0;
					}
					// hi gust
					if (data[i].HighGust.HasValue && data[i].HighGust.Value > highGustVal[monthOffset] && data[i].HighGustTime.HasValue)
					{
						highGustVal[monthOffset] = data[i].HighGust.Value;
						highGustTime[monthOffset] = data[i].HighGustTime.Value;
					}
					// lo temp
					if (data[i].LowTemp.HasValue && data[i].LowTemp.Value < lowTempVal[monthOffset] && data[i].LowTempTime.HasValue)
					{
						lowTempVal[monthOffset] = data[i].LowTemp.Value;
						lowTempTime[monthOffset] = data[i].LowTempTime.Value;
					}
					// hi min temp
					if (data[i].LowTemp.HasValue && data[i].LowTemp.Value > highMinTempVal[monthOffset] && data[i].LowTempTime.HasValue)
					{
						highMinTempVal[monthOffset] = data[i].LowTemp.Value;
						highMinTempTime[monthOffset] = data[i].LowTempTime.Value;
					}
					// hi temp
					if (data[i].HighTemp.HasValue && data[i].HighTemp > highTempVal[monthOffset] && data[i].HighTempTime.HasValue)
					{
						highTempVal[monthOffset] = data[i].HighTemp.Value;
						highTempTime[monthOffset] = data[i].HighTempTime.Value;
					}
					// lo max temp
					if (data[i].HighTemp.HasValue && data[i].HighTemp.Value < lowMaxTempVal[monthOffset] && data[i].HighTempTime.HasValue)
					{
						lowMaxTempVal[monthOffset] = data[i].HighTemp.Value;
						lowMaxTempTime[monthOffset] = data[i].HighTempTime.Value;
					}

					// temp ranges
					// hi temp range
					if (data[i].LowTemp.HasValue && data[i].HighTemp.HasValue && (data[i].HighTemp - data[i].LowTemp.Value) > highTempRangeVal[monthOffset])
					{
						highTempRangeVal[monthOffset] = data[i].HighTemp.Value - data[i].LowTemp.Value;
						highTempRangeTime[monthOffset] = loggedDate;
					}
					// lo temp range
					if (data[i].LowTemp.HasValue && data[i].HighTemp.HasValue && (data[i].HighTemp - data[i].LowTemp.Value) < lowTempRangeVal[monthOffset])
					{
						lowTempRangeVal[monthOffset] = data[i].HighTemp.Value - data[i].LowTemp.Value;
						lowTempRangeTime[monthOffset] = loggedDate;
					}

					// lo baro
					if (data[i].LowPress.HasValue && data[i].LowPress.Value < lowBaroVal[monthOffset] && data[i].LowPressTime.HasValue)
					{
						lowBaroVal[monthOffset] = data[i].LowPress.Value;
						lowBaroTime[monthOffset] = data[i].LowPressTime.Value;
					}
					// hi baro
					if (data[i].HighPress.HasValue && data[i].HighPress.Value > highBaroVal[monthOffset] && data[i].HighPressTime.HasValue)
					{
						highBaroVal[monthOffset] = data[i].HighPress.Value;
						highBaroTime[monthOffset] = data[i].HighPressTime.Value;
					}
					// hi rain rate
					if (data[i].HighRainRate.HasValue && data[i].HighRainRate.Value > highRainRateVal[monthOffset] && data[i].HighRainRateTime.HasValue)
					{
						highRainRateVal[monthOffset] = data[i].HighRainRate.Value;
						highRainRateTime[monthOffset] = data[i].HighRainRateTime.Value;
					}
					// hi rain day
					if (data[i].TotalRain.HasValue)
					{
						// monthly rain
						rainThisMonth += data[i].TotalRain.Value;

						if (data[i].TotalRain.Value > highRainDayVal[monthOffset])
						{
							highRainDayVal[monthOffset] = data[i].TotalRain.Value;
							highRainDayTime[monthOffset] = loggedDate;
						}
					}


					// dry/wet period
					if (Convert.ToInt32(data[i].TotalRain * 100) >= rainThreshold)
					{
						if (isDryNow)
						{
							currentWetPeriod = 1;
							isDryNow = false;
							var dryMonthOffset = thisDateWet.Month - 1;
							if (currentDryPeriod > dryPeriodVal[dryMonthOffset])
							{
								dryPeriodVal[dryMonthOffset] = currentDryPeriod;
								dryPeriodTime[dryMonthOffset] = thisDateDry;
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
							if (currentWetPeriod > wetPeriodVal[wetMonthOffset])
							{
								wetPeriodVal[wetMonthOffset] = currentWetPeriod;
								wetPeriodTime[wetMonthOffset] = thisDateWet;
							}
							currentWetPeriod = 0;
						}
					}

					// hi wind run
					if (data[i].WindRun.HasValue && data[i].WindRun.Value > highWindRunVal[monthOffset])
					{
						highWindRunVal[monthOffset] = data[i].WindRun.Value;
						highWindRunTime[monthOffset] = loggedDate;
					}
					// hi wind
					if (data[i].HighAvgWind.HasValue && data[i].HighAvgWindTime.HasValue && data[i].HighAvgWind.Value > highWindVal[monthOffset])
					{
						highWindVal[monthOffset] = data[i].HighAvgWind.Value;
						highWindTime[monthOffset] = data[i].HighAvgWindTime.Value;
					}

					// lo humidity
					if (data[i].LowHumidity.HasValue && data[i].LowHumidity.Value < lowHumVal[monthOffset] && data[i].LowHumidityTime.HasValue)
					{
						lowHumVal[monthOffset] = data[i].LowHumidity.Value;
						lowHumTime[monthOffset] = data[i].LowHumidityTime.Value;
					}
					// hi humidity
					if (data[i].HighHumidity.HasValue && data[i].HighHumidity > highHumVal[monthOffset] && data[i].HighHumidityTime.HasValue)
					{
						highHumVal[monthOffset] = data[i].HighHumidity.Value;
						highHumTime[monthOffset] = data[i].HighHumidityTime.Value;
					}

					// hi heat index
					if (data[i].HighHeatIndex.HasValue && data[i].HighHeatIndex.Value > highHeatIndVal[monthOffset] && data[i].HighHeatIndexTime.HasValue)
					{
						highHeatIndVal[monthOffset] = data[i].HighHeatIndex.Value;
						highHeatIndTime[monthOffset] = data[i].HighHeatIndexTime.Value;
					}
					// hi app temp
					if (data[i].HighAppTemp.HasValue && data[i].HighAppTemp.Value > highAppTempVal[monthOffset] && data[i].HighAppTempTime.HasValue)
					{
						highAppTempVal[monthOffset] = data[i].HighAppTemp.Value;
						highAppTempTime[monthOffset] = data[i].HighAppTempTime.Value;
					}
					// lo app temp
					if (data[i].LowAppTemp.HasValue && data[i].LowAppTemp.Value < lowAppTempVal[monthOffset] && data[i].LowAppTempTime.HasValue)
					{
						lowAppTempVal[monthOffset] = data[i].LowAppTemp.Value;
						lowAppTempTime[monthOffset] = data[i].LowAppTempTime.Value;
					}

					// hi rain hour
					if (data[i].HighHourlyRain.HasValue && data[i].HighHourlyRain > highRainHourVal[monthOffset] && data[i].HighHourlyRainTime.HasValue)
					{
						highRainHourVal[monthOffset] = data[i].HighHourlyRain.Value;
						highRainHourTime[monthOffset] = data[i].HighHourlyRainTime.Value;
					}

					// lo wind chill
					if (data[i].LowWindChill.HasValue && data[i].LowWindChill.Value < lowWindChillVal[monthOffset] && data[i].LowWindChillTime.HasValue)
					{
						lowWindChillVal[monthOffset] = data[i].LowWindChill.Value;
						lowWindChillTime[monthOffset] = data[i].LowWindChillTime.Value;
					}

					// hi dewpt
					if (data[i].HighDewPoint.HasValue && data[i].HighDewPoint.Value > highDewPtVal[monthOffset] && data[i].HighDewPointTime.HasValue)
					{
						highDewPtVal[monthOffset] = data[i].HighDewPoint.Value;
						highDewPtTime[monthOffset] = data[i].HighDewPointTime.Value;
					}
					// lo dewpt
					if (data[i].LowDewPoint.HasValue && data[i].LowDewPoint.Value < lowDewPtVal[monthOffset] && data[i].LowDewPointTime.HasValue)
					{
						lowDewPtVal[monthOffset] = data[i].LowDewPoint.Value;
						lowDewPtTime[monthOffset] = data[i].LowDewPointTime.Value;
					}

					// hi feels like
					if (data[i].HighFeelsLike.HasValue && data[i].HighFeelsLike.Value > highFeelsLikeVal[monthOffset] && data[i].HighFeelsLikeTime.HasValue)
					{
						highFeelsLikeVal[monthOffset] = data[i].HighFeelsLike.Value;
						highFeelsLikeTime[monthOffset] = data[i].HighFeelsLikeTime.Value;
					}
					// lo feels like
					if (data[i].LowFeelsLike.HasValue && data[i].LowFeelsLike.Value < lowFeelsLikeVal[monthOffset] && data[i].LowFeelsLikeTime.HasValue)
					{
						lowFeelsLikeVal[monthOffset] = data[i].LowFeelsLike.Value;
						lowFeelsLikeTime[monthOffset] = data[i].LowFeelsLikeTime.Value;
					}

					// hi humidex
					if (data[i].HighHumidex.HasValue && data[i].HighHumidex.Value > highHumidexVal[monthOffset] && data[i].HighHumidexTime.HasValue)
					{
						highHumidexVal[monthOffset] = data[i].HighHumidex.Value;
						highHumidexTime[monthOffset] = data[i].HighHumidexTime.Value;
					}
				}


				for (var i = 0; i < 12; i++)
				{
					var m = i + 1;
					json.Append($"\"{m}-highTempValDayfile\":\"{highTempVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highTempTimeDayfile\":\"{highTempTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowTempValDayfile\":\"{lowTempVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowTempTimeDayfile\":\"{lowTempTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highDewPointValDayfile\":\"{highDewPtVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highDewPointTimeDayfile\":\"{highDewPtTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowDewPointValDayfile\":\"{lowDewPtVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowDewPointTimeDayfile\":\"{lowDewPtTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highApparentTempValDayfile\":\"{highAppTempVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highApparentTempTimeDayfile\":\"{highAppTempTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowApparentTempValDayfile\":\"{lowAppTempVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowApparentTempTimeDayfile\":\"{lowAppTempTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highFeelsLikeValDayfile\":\"{highFeelsLikeVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highFeelsLikeTimeDayfile\":\"{highFeelsLikeTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowFeelsLikeValDayfile\":\"{lowFeelsLikeVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowFeelsLikeTimeDayfile\":\"{lowFeelsLikeTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highHumidexValDayfile\":\"{highHumidexVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highHumidexTimeDayfile\":\"{highHumidexTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowWindChillValDayfile\":\"{lowWindChillVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowWindChillTimeDayfile\":\"{lowWindChillTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highHeatIndexValDayfile\":\"{highHeatIndVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highHeatIndexTimeDayfile\":\"{highHeatIndTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highMinTempValDayfile\":\"{highMinTempVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highMinTempTimeDayfile\":\"{highMinTempTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowMaxTempValDayfile\":\"{lowMaxTempVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowMaxTempTimeDayfile\":\"{lowMaxTempTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highDailyTempRangeValDayfile\":\"{highTempRangeVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-highDailyTempRangeTimeDayfile\":\"{highTempRangeTime[i].ToString(dateStampFormat)}\",");
					json.Append($"\"{m}-lowDailyTempRangeValDayfile\":\"{lowTempRangeVal[i].ToString(cumulus.TempFormat)}\",");
					json.Append($"\"{m}-lowDailyTempRangeTimeDayfile\":\"{lowTempRangeTime[i].ToString(dateStampFormat)}\",");
					json.Append($"\"{m}-highHumidityValDayfile\":\"{highHumVal[i].ToString(cumulus.HumFormat)}\",");
					json.Append($"\"{m}-highHumidityTimeDayfile\":\"{highHumTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowHumidityValDayfile\":\"{lowHumVal[i].ToString(cumulus.HumFormat)}\",");
					json.Append($"\"{m}-lowHumidityTimeDayfile\":\"{lowHumTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highBarometerValDayfile\":\"{highBaroVal[i].ToString(cumulus.PressFormat)}\",");
					json.Append($"\"{m}-highBarometerTimeDayfile\":\"{highBaroTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-lowBarometerValDayfile\":\"{lowBaroVal[i].ToString(cumulus.PressFormat)}\",");
					json.Append($"\"{m}-lowBarometerTimeDayfile\":\"{lowBaroTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highGustValDayfile\":\"{highGustVal[i].ToString(cumulus.WindFormat)}\",");
					json.Append($"\"{m}-highGustTimeDayfile\":\"{highGustTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highWindValDayfile\":\"{highWindVal[i].ToString(cumulus.WindAvgFormat)}\",");
					json.Append($"\"{m}-highWindTimeDayfile\":\"{highWindTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highWindRunValDayfile\":\"{highWindRunVal[i].ToString(cumulus.WindRunFormat)}\",");
					json.Append($"\"{m}-highWindRunTimeDayfile\":\"{highWindRunTime[i].ToString(dateStampFormat)}\",");
					json.Append($"\"{m}-highRainRateValDayfile\":\"{highRainRateVal[i].ToString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highRainRateTimeDayfile\":\"{highRainRateTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highHourlyRainValDayfile\":\"{highRainHourVal[i].ToString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highHourlyRainTimeDayfile\":\"{highRainHourTime[i].ToString(timeStampFormat)}\",");
					json.Append($"\"{m}-highDailyRainValDayfile\":\"{highRainDayVal[i].ToString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highDailyRainTimeDayfile\":\"{highRainDayTime[i].ToString(dateStampFormat)}\",");
					json.Append($"\"{m}-highMonthlyRainValDayfile\":\"{highRainMonthVal[i].ToString(cumulus.RainFormat)}\",");
					json.Append($"\"{m}-highMonthlyRainTimeDayfile\":\"{highRainMonthTime[i]:MM/yyyy}\",");
					json.Append($"\"{m}-longestDryPeriodValDayfile\":\"{dryPeriodVal[i]}\",");
					json.Append($"\"{m}-longestDryPeriodTimeDayfile\":\"{dryPeriodTime[i].ToString(dateStampFormat)}\",");
					json.Append($"\"{m}-longestWetPeriodValDayfile\":\"{wetPeriodVal[i]}\",");
					json.Append($"\"{m}-longestWetPeriodTimeDayfile\":\"{wetPeriodTime[i].ToString(dateStampFormat)}\",");
				}
				json.Remove(json.Length - 1, 1);
				json.Append('}');
			}
			else
			{
				Cumulus.LogMessage("Error failed to find day records");
			}

			return json.ToString();
		}

		internal string GetMonthlyRecLogFile()
		{
			const string timeStampFormat = "dd/MM/yyyy HH:mm";
			const string dateStampFormat = "dd/MM/yyyy";

			var json = new StringBuilder("{", 25500);
			var datefrom = cumulus.RecordsBeganDate;
			datefrom = new DateTime(datefrom.Year, datefrom.Month, 1, 0, 0, 0);
			var dateto = DateTime.Now;
			dateto = new DateTime(dateto.Year, dateto.Month, 1, 0, 0, 0);
			var filedate = datefrom;

			var logFile = cumulus.GetLogFileName(filedate);
			var started = false;
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


			var highTempVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowTempVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highDewPtVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowDewPtVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highAppTempVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowAppTempVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highFeelsLikeVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowFeelsLikeVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highHumidexVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowWindChillVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highHeatIndVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highMinTempVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowMaxTempVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highTempRangeVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowTempRangeVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highHumVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowHumVal = new double[] { 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999, 999 };
			var highBaroVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var lowBaroVal = new double[] { 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999 };
			var highGustVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highWindVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highWindRunVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainRateVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainHourVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainDayVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var highRainMonthVal = new double[] { -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999, -999 };
			var dryPeriodVal = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			var wetPeriodVal = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			var thisDate = new DateTime(1900, 01, 01);
			var highTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highDewPtTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowDewPtTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highAppTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowAppTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highFeelsLikeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowFeelsLikeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highHumidexTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowWindChillTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highHeatIndTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highMinTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowMaxTempTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highTempRangeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowTempRangeTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highHumTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowHumTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highBaroTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var lowBaroTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highGustTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highWindTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highWindRunTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainRateTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainHourTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainDayTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var highRainMonthTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var dryPeriodTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };
			var wetPeriodTime = new[] { thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate, thisDate };

			var thisDateDry = thisDate;
			var thisDateWet = thisDate;

			var currentDay = datefrom;
			double dayHighTemp = -999;
			var dayHighTempTime = thisDate;
			double dayLowTemp = 999;
			var dayLowTempTime = thisDate;
			double dayWindRun = 0;
			double dayRain = 0;

			var monthlyRain = 0.0;

			var totalRainfall = 0.0;

			hourRainLog.Clear();

			var watch = System.Diagnostics.Stopwatch.StartNew();

			while (!finished)
			{
				if (File.Exists(logFile))
				{
					cumulus.LogDebugMessage($"GetMonthlyTimeRecLogFile: Processing log file - {logFile}");
					var linenum = 0;
					try
					{
						var logfile = File.ReadAllLines(logFile);
						foreach (var line in logfile)
						{
							// process each record in the file
							linenum++;

							var rec = station.ParseLogFileRec(line, true);

							// We need to work in meteo dates not clock dates for day hi/lows
							var metoDate = rec.Date.AddHours(cumulus.GetHourInc());
							var monthOffset = metoDate.Month - 1;

							if (!started)
							{
								lastentrydate = rec.Date;
								currentDay = metoDate;
								started = true;
							}

							// low chill
							if (rec.WindChill > -9999 && rec.WindChill < lowWindChillVal[monthOffset])
							{
								lowWindChillVal[monthOffset] = rec.WindChill;
								lowWindChillTime[monthOffset] = rec.Date;
							}
							// hi heat
							if (rec.HeatIndex > -9999 && rec.HeatIndex > highHeatIndVal[monthOffset])
							{
								highHeatIndVal[monthOffset] = rec.HeatIndex;
								highHeatIndTime[monthOffset] = rec.Date;
							}

							if (rec.ApparentTemperature > -9999)
							{
								// hi appt
								if (rec.ApparentTemperature > highAppTempVal[monthOffset])
								{
									highAppTempVal[monthOffset] = rec.ApparentTemperature;
									highAppTempTime[monthOffset] = rec.Date;
								}
								// lo appt
								if (rec.ApparentTemperature < lowAppTempVal[monthOffset])
								{
									lowAppTempVal[monthOffset] = rec.ApparentTemperature;
									lowAppTempTime[monthOffset] = rec.Date;
								}
							}

							if (rec.FeelsLike > -9999)
							{
								// hi feels like
								if (rec.FeelsLike > highFeelsLikeVal[monthOffset])
								{
									highFeelsLikeVal[monthOffset] = rec.FeelsLike;
									highFeelsLikeTime[monthOffset] = rec.Date;
								}
								// lo feels like
								if (rec.FeelsLike < lowFeelsLikeVal[monthOffset])
								{
									lowFeelsLikeVal[monthOffset] = rec.FeelsLike;
									lowFeelsLikeTime[monthOffset] = rec.Date;
								}
							}

							// hi humidex
							if (rec.Humidex > -9999 && rec.Humidex > highHumidexVal[monthOffset])
							{
								highHumidexVal[monthOffset] = rec.Humidex;
								highHumidexTime[monthOffset] = rec.Date;
							}

							// hi temp
							if (rec.OutdoorTemperature > highTempVal[monthOffset])
							{
								highTempVal[monthOffset] = rec.OutdoorTemperature;
								highTempTime[monthOffset] = rec.Date;
							}
							// lo temp
							if (rec.OutdoorTemperature < lowTempVal[monthOffset])
							{
								lowTempVal[monthOffset] = rec.OutdoorTemperature;
								lowTempTime[monthOffset] = rec.Date;
							}
							// hi dewpoint
							if (rec.OutdoorDewpoint > highDewPtVal[monthOffset])
							{
								highDewPtVal[monthOffset] = rec.OutdoorDewpoint;
								highDewPtTime[monthOffset] = rec.Date;
							}
							// low dewpoint
							if (rec.OutdoorDewpoint < lowDewPtVal[monthOffset])
							{
								lowDewPtVal[monthOffset] = rec.OutdoorDewpoint;
								lowDewPtTime[monthOffset] = rec.Date;
							}
							// hi hum
							if (rec.OutdoorHumidity > highHumVal[monthOffset])
							{
								highHumVal[monthOffset] = rec.OutdoorHumidity;
								highHumTime[monthOffset] = rec.Date;
							}
							// lo hum
							if (rec.OutdoorHumidity < lowHumVal[monthOffset])
							{
								lowHumVal[monthOffset] = rec.OutdoorHumidity;
								lowHumTime[monthOffset] = rec.Date;
							}
							// hi baro
							if (rec.Pressure > highBaroVal[monthOffset])
							{
								highBaroVal[monthOffset] = rec.Pressure;
								highBaroTime[monthOffset] = rec.Date;
							}
							// lo hum
							if (rec.Pressure < lowBaroVal[monthOffset])
							{
								lowBaroVal[monthOffset] = rec.Pressure;
								lowBaroTime[monthOffset] = rec.Date;
							}
							// hi gust
							if (rec.RecentMaxGust > highGustVal[monthOffset])
							{
								highGustVal[monthOffset] = rec.RecentMaxGust;
								highGustTime[monthOffset] = rec.Date;
							}
							// hi wind
							if (rec.WindAverage > highWindVal[monthOffset])
							{
								highWindVal[monthOffset] = rec.WindAverage;
								highWindTime[monthOffset] = rec.Date;
							}
							// hi rain rate
							if (rec.RainRate > highRainRateVal[monthOffset])
							{
								highRainRateVal[monthOffset] = rec.RainRate;
								highRainRateTime[monthOffset] = rec.Date;
							}

							// same meteo day
							if (currentDay.Day == metoDate.Day && currentDay.Month == metoDate.Month && currentDay.Year == metoDate.Year)
							{
								if (rec.OutdoorTemperature > dayHighTemp)
								{
									dayHighTemp = rec.OutdoorTemperature;
									dayHighTempTime = rec.Date;
								}

								if (rec.OutdoorTemperature < dayLowTemp)
								{
									dayLowTemp = rec.OutdoorTemperature;
									dayLowTempTime = rec.Date;
								}

								if (dayRain < rec.RainToday)
								{
									dayRain = rec.RainToday;
								}

								dayWindRun += rec.Date.Subtract(lastentrydate).TotalHours * rec.WindAverage;
							}
							else // new meteo day
							{
								var lastEntryMonthOffset = currentDay.Month - 1;
								if (dayHighTemp < lowMaxTempVal[lastEntryMonthOffset])
								{
									lowMaxTempVal[lastEntryMonthOffset] = dayHighTemp;
									lowMaxTempTime[lastEntryMonthOffset] = dayHighTempTime;
								}
								if (dayLowTemp > highMinTempVal[lastEntryMonthOffset])
								{
									highMinTempVal[lastEntryMonthOffset] = dayLowTemp;
									highMinTempTime[lastEntryMonthOffset] = dayLowTempTime;
								}
								if (dayHighTemp - dayLowTemp > highTempRangeVal[lastEntryMonthOffset])
								{
									highTempRangeVal[lastEntryMonthOffset] = dayHighTemp - dayLowTemp;
									highTempRangeTime[lastEntryMonthOffset] = currentDay;
								}
								if (dayHighTemp - dayLowTemp < lowTempRangeVal[lastEntryMonthOffset])
								{
									lowTempRangeVal[lastEntryMonthOffset] = dayHighTemp - dayLowTemp;
									lowTempRangeTime[lastEntryMonthOffset] = currentDay;
								}
								if (dayWindRun > highWindRunVal[lastEntryMonthOffset])
								{
									highWindRunVal[lastEntryMonthOffset] = dayWindRun;
									highWindRunTime[lastEntryMonthOffset] = currentDay;
								}
								if (dayRain > highRainDayVal[lastEntryMonthOffset])
								{
									highRainDayVal[lastEntryMonthOffset] = dayRain;
									highRainDayTime[lastEntryMonthOffset] = currentDay;
								}

								// dry/wet period
								if (Convert.ToInt32(dayRain * 1000) >= rainThreshold)
								{
									if (isDryNow)
									{
										currentWetPeriod = 1;
										isDryNow = false;
										if (currentDryPeriod > dryPeriodVal[monthOffset])
										{
											dryPeriodVal[monthOffset] = currentDryPeriod;
											dryPeriodTime[monthOffset] = thisDateDry;
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
										if (currentWetPeriod > wetPeriodVal[monthOffset])
										{
											wetPeriodVal[monthOffset] = currentWetPeriod;
											wetPeriodTime[monthOffset] = thisDateWet;
										}
										currentWetPeriod = 0;
									}
								}

								// new month ?
								if (currentDay.Month != metoDate.Month)
								{
									monthlyRain += dayRain;
									var offset = currentDay.Month - 1;
									if (monthlyRain > highRainMonthVal[offset])
									{
										highRainMonthVal[offset] = monthlyRain;
										highRainMonthTime[offset] = currentDay;
									}
									monthlyRain = 0.0;
								}
								else
								{
									monthlyRain += dayRain;
								}

								currentDay = metoDate;
								dayHighTemp = rec.OutdoorTemperature;
								dayLowTemp = rec.OutdoorTemperature;
								dayWindRun = 0.0;
								totalRainfall += dayRain;
								dayRain = 0.0;
							}

							// hourly rain
							/*
								* need to track what the rainfall has been in the last rolling hour
								* across day rollovers where the count resets
								*/
							AddLastHourRainEntry(rec.Date, totalRainfall + dayRain);
							RemoveOldRainData(rec.Date);

							var rainThisHour = hourRainLog.Last().Raincounter - hourRainLog.First().Raincounter;
							if (rainThisHour > highRainHourVal[monthOffset])
							{
								highRainHourVal[monthOffset] = rainThisHour;
								highRainHourTime[monthOffset] = rec.Date;
							}

							lastentrydate = rec.Date;
							//lastRainMidnight = rainMidnight;
						}
					}
					catch (Exception e)
					{
						cumulus.LogExceptionMessage(e, $"Error at line {linenum} of {logFile}");
						Cumulus.LogMessage("Please edit the file to correct the error");
					}
				}
				else
				{
					cumulus.LogDebugMessage($"GetMonthlyRecLogFile: Log file  not found - {logFile}");
				}
				if (filedate >= dateto)
				{
					finished = true;
					cumulus.LogDebugMessage("GetMonthlyRecLogFile: Finished processing the log files");
				}
				else
				{
					cumulus.LogDebugMessage($"GetMonthlyRecLogFile: Finished processing log file - {logFile}");
					filedate = filedate.AddMonths(1);
					logFile = cumulus.GetLogFileName(filedate);
				}
			}
			for (var i = 0; i < 12; i++)
			{
				var m = i + 1;
				json.Append($"\"{m}-highTempValLogfile\":\"{highTempVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highTempTimeLogfile\":\"{highTempTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowTempValLogfile\":\"{lowTempVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowTempTimeLogfile\":\"{lowTempTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDewPointValLogfile\":\"{highDewPtVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDewPointTimeLogfile\":\"{highDewPtTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowDewPointValLogfile\":\"{lowDewPtVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDewPointTimeLogfile\":\"{lowDewPtTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highApparentTempValLogfile\":\"{highAppTempVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highApparentTempTimeLogfile\":\"{highAppTempTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowApparentTempValLogfile\":\"{lowAppTempVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowApparentTempTimeLogfile\":\"{lowAppTempTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeValLogfile\":\"{highFeelsLikeVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highFeelsLikeTimeLogfile\":\"{highFeelsLikeTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeValLogfile\":\"{lowFeelsLikeVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowFeelsLikeTimeLogfile\":\"{lowFeelsLikeTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHumidexValLogfile\":\"{highHumidexVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHumidexTimeLogfile\":\"{highHumidexTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowWindChillValLogfile\":\"{lowWindChillVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowWindChillTimeLogfile\":\"{lowWindChillTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHeatIndexValLogfile\":\"{highHeatIndVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highHeatIndexTimeLogfile\":\"{highHeatIndTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highMinTempValLogfile\":\"{highMinTempVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highMinTempTimeLogfile\":\"{highMinTempTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowMaxTempValLogfile\":\"{lowMaxTempVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowMaxTempTimeLogfile\":\"{lowMaxTempTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeValLogfile\":\"{highTempRangeVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-highDailyTempRangeTimeLogfile\":\"{highTempRangeTime[i].ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeValLogfile\":\"{lowTempRangeVal[i].ToString(cumulus.TempFormat)}\",");
				json.Append($"\"{m}-lowDailyTempRangeTimeLogfile\":\"{lowTempRangeTime[i].ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-highHumidityValLogfile\":\"{highHumVal[i].ToString(cumulus.HumFormat)}\",");
				json.Append($"\"{m}-highHumidityTimeLogfile\":\"{highHumTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowHumidityValLogfile\":\"{lowHumVal[i].ToString(cumulus.HumFormat)}\",");
				json.Append($"\"{m}-lowHumidityTimeLogfile\":\"{lowHumTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highBarometerValLogfile\":\"{highBaroVal[i].ToString(cumulus.PressFormat)}\",");
				json.Append($"\"{m}-highBarometerTimeLogfile\":\"{highBaroTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-lowBarometerValLogfile\":\"{lowBaroVal[i].ToString(cumulus.PressFormat)}\",");
				json.Append($"\"{m}-lowBarometerTimeLogfile\":\"{lowBaroTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highGustValLogfile\":\"{highGustVal[i].ToString(cumulus.WindFormat)}\",");
				json.Append($"\"{m}-highGustTimeLogfile\":\"{highGustTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindValLogfile\":\"{highWindVal[i].ToString(cumulus.WindAvgFormat)}\",");
				json.Append($"\"{m}-highWindTimeLogfile\":\"{highWindTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highWindRunValLogfile\":\"{highWindRunVal[i].ToString(cumulus.WindRunFormat)}\",");
				json.Append($"\"{m}-highWindRunTimeLogfile\":\"{highWindRunTime[i].ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-highRainRateValLogfile\":\"{highRainRateVal[i].ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highRainRateTimeLogfile\":\"{highRainRateTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highHourlyRainValLogfile\":\"{highRainHourVal[i].ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highHourlyRainTimeLogfile\":\"{highRainHourTime[i].ToString(timeStampFormat)}\",");
				json.Append($"\"{m}-highDailyRainValLogfile\":\"{highRainDayVal[i].ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highDailyRainTimeLogfile\":\"{highRainDayTime[i].ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainValLogfile\":\"{highRainMonthVal[i].ToString(cumulus.RainFormat)}\",");
				json.Append($"\"{m}-highMonthlyRainTimeLogfile\":\"{highRainMonthTime[i]:MM/yyyy}\",");
				json.Append($"\"{m}-longestDryPeriodValLogfile\":\"{dryPeriodVal[i]}\",");
				json.Append($"\"{m}-longestDryPeriodTimeLogfile\":\"{dryPeriodTime[i].ToString(dateStampFormat)}\",");
				json.Append($"\"{m}-longestWetPeriodValLogfile\":\"{wetPeriodVal[i]}\",");
				json.Append($"\"{m}-longestWetPeriodTimeLogfile\":\"{wetPeriodTime[i].ToString(dateStampFormat)}\",");
			}

			json.Remove(json.Length - 1, 1);
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
			json.Append($"\"highTempVal\":\"{station.ThisMonth.HighTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highTempTime\":\"{station.ThisMonth.HighTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowTempVal\":\"{station.ThisMonth.LowTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempTime\":\"{station.ThisMonth.LowTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDewPointVal\":\"{station.ThisMonth.HighDewPoint.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointTime\":\"{station.ThisMonth.HighDewPoint.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointVal\":\"{station.ThisMonth.LowDewPoint.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointTime\":\"{station.ThisMonth.LowDewPoint.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempVal\":\"{station.ThisMonth.HighAppTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempTime\":\"{station.ThisMonth.HighAppTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempVal\":\"{station.ThisMonth.LowAppTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempTime\":\"{station.ThisMonth.LowAppTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeVal\":\"{station.ThisMonth.HighFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeTime\":\"{station.ThisMonth.HighFeelsLike.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeVal\":\"{station.ThisMonth.LowFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeTime\":\"{station.ThisMonth.LowFeelsLike.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHumidexVal\":\"{station.ThisMonth.HighHumidex.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexTime\":\"{station.ThisMonth.HighHumidex.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillVal\":\"{station.ThisMonth.LowChill.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillTime\":\"{station.ThisMonth.LowChill.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexVal\":\"{station.ThisMonth.HighHeatIndex.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexTime\":\"{station.ThisMonth.HighHeatIndex.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highMinTempVal\":\"{station.ThisMonth.HighMinTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempTime\":\"{station.ThisMonth.HighMinTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempVal\":\"{station.ThisMonth.LowMaxTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempTime\":\"{station.ThisMonth.LowMaxTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeVal\":\"{station.ThisMonth.HighDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeTime\":\"{station.ThisMonth.HighDailyTempRange.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeVal\":\"{station.ThisMonth.LowDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeTime\":\"{station.ThisMonth.LowDailyTempRange.Ts.ToString(dateStampFormat)}\",");
			// Records - Humidity
			json.Append($"\"highHumidityVal\":\"{station.ThisMonth.HighHumidity.Val.ToString(cumulus.HumFormat)}\",");
			json.Append($"\"highHumidityTime\":\"{station.ThisMonth.HighHumidity.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityVal\":\"{station.ThisMonth.LowHumidity.Val.ToString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityTime\":\"{station.ThisMonth.LowHumidity.Ts.ToString(timeStampFormat)}\",");
			// Records - Pressure
			json.Append($"\"highBarometerVal\":\"{station.ThisMonth.HighPress.Val.ToString(cumulus.PressFormat)}\",");
			json.Append($"\"highBarometerTime\":\"{station.ThisMonth.HighPress.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerVal\":\"{station.ThisMonth.LowPress.Val.ToString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerTime\":\"{station.ThisMonth.LowPress.Ts.ToString(timeStampFormat)}\",");
			// Records - Wind
			json.Append($"\"highGustVal\":\"{station.ThisMonth.HighGust.Val.ToString(cumulus.WindFormat)}\",");
			json.Append($"\"highGustTime\":\"{station.ThisMonth.HighGust.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindVal\":\"{station.ThisMonth.HighWind.Val.ToString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindTime\":\"{station.ThisMonth.HighWind.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindRunVal\":\"{station.ThisMonth.HighWindRun.Val.ToString(cumulus.WindRunFormat)}\",");
			json.Append($"\"highWindRunTime\":\"{station.ThisMonth.HighWindRun.Ts.ToString(dateStampFormat)}\",");
			// Records - Rain
			json.Append($"\"highRainRateVal\":\"{station.ThisMonth.HighRainRate.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highRainRateTime\":\"{station.ThisMonth.HighRainRate.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainVal\":\"{station.ThisMonth.HourlyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainTime\":\"{station.ThisMonth.HourlyRain.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainVal\":\"{station.ThisMonth.DailyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainTime\":\"{station.ThisMonth.DailyRain.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"longestDryPeriodVal\":\"{station.ThisMonth.LongestDryPeriod.Val:F0}\",");
			json.Append($"\"longestDryPeriodTime\":\"{station.ThisMonth.LongestDryPeriod.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"longestWetPeriodVal\":\"{station.ThisMonth.LongestWetPeriod.Val:F0}\",");
			json.Append($"\"longestWetPeriodTime\":\"{station.ThisMonth.LongestWetPeriod.Ts.ToString(dateStampFormat)}\"");

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
			json.Append($"\"highTempVal\":\"{station.ThisYear.HighTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highTempTime\":\"{station.ThisYear.HighTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowTempVal\":\"{station.ThisYear.LowTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowTempTime\":\"{station.ThisYear.LowTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDewPointVal\":\"{station.ThisYear.HighDewPoint.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDewPointTime\":\"{station.ThisYear.HighDewPoint.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowDewPointVal\":\"{station.ThisYear.LowDewPoint.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDewPointTime\":\"{station.ThisYear.LowDewPoint.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highApparentTempVal\":\"{station.ThisYear.HighAppTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highApparentTempTime\":\"{station.ThisYear.HighAppTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowApparentTempVal\":\"{station.ThisYear.LowAppTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowApparentTempTime\":\"{station.ThisYear.LowAppTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highFeelsLikeVal\":\"{station.ThisYear.HighFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highFeelsLikeTime\":\"{station.ThisYear.HighFeelsLike.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowFeelsLikeVal\":\"{station.ThisYear.LowFeelsLike.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowFeelsLikeTime\":\"{station.ThisYear.LowFeelsLike.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHumidexVal\":\"{station.ThisYear.HighHumidex.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHumidexTime\":\"{station.ThisYear.HighHumidex.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowWindChillVal\":\"{station.ThisYear.LowChill.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowWindChillTime\":\"{station.ThisYear.LowChill.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHeatIndexVal\":\"{station.ThisYear.HighHeatIndex.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highHeatIndexTime\":\"{station.ThisYear.HighHeatIndex.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highMinTempVal\":\"{station.ThisYear.HighMinTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highMinTempTime\":\"{station.ThisYear.HighMinTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowMaxTempVal\":\"{station.ThisYear.LowMaxTemp.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowMaxTempTime\":\"{station.ThisYear.LowMaxTemp.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyTempRangeVal\":\"{station.ThisYear.HighDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"highDailyTempRangeTime\":\"{station.ThisYear.HighDailyTempRange.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"lowDailyTempRangeVal\":\"{station.ThisYear.LowDailyTempRange.Val.ToString(cumulus.TempFormat)}\",");
			json.Append($"\"lowDailyTempRangeTime\":\"{station.ThisYear.LowDailyTempRange.Ts.ToString(dateStampFormat)}\",");
			// Records - Humidity
			json.Append($"\"highHumidityVal\":\"{station.ThisYear.HighHumidity.Val.ToString(cumulus.HumFormat)}\",");
			json.Append($"\"highHumidityTime\":\"{station.ThisYear.HighHumidity.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowHumidityVal\":\"{station.ThisYear.LowHumidity.Val.ToString(cumulus.HumFormat)}\",");
			json.Append($"\"lowHumidityTime\":\"{station.ThisYear.LowHumidity.Ts.ToString(timeStampFormat)}\",");
			// Records - Pressure
			json.Append($"\"highBarometerVal\":\"{station.ThisYear.HighPress.Val.ToString(cumulus.PressFormat)}\",");
			json.Append($"\"highBarometerTime\":\"{station.ThisYear.HighPress.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"lowBarometerVal\":\"{station.ThisYear.LowPress.Val.ToString(cumulus.PressFormat)}\",");
			json.Append($"\"lowBarometerTime\":\"{station.ThisYear.LowPress.Ts.ToString(timeStampFormat)}\",");
			// Records - Wind
			json.Append($"\"highGustVal\":\"{station.ThisYear.HighGust.Val.ToString(cumulus.WindFormat)}\",");
			json.Append($"\"highGustTime\":\"{station.ThisYear.HighGust.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindVal\":\"{station.ThisYear.HighWind.Val.ToString(cumulus.WindAvgFormat)}\",");
			json.Append($"\"highWindTime\":\"{station.ThisYear.HighWind.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highWindRunVal\":\"{station.ThisYear.HighWindRun.Val.ToString(cumulus.WindRunFormat)}\",");
			json.Append($"\"highWindRunTime\":\"{station.ThisYear.HighWindRun.Ts.ToString(dateStampFormat)}\",");
			// Records - Rain
			json.Append($"\"highRainRateVal\":\"{station.ThisYear.HighRainRate.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highRainRateTime\":\"{station.ThisYear.HighRainRate.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highHourlyRainVal\":\"{station.ThisYear.HourlyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highHourlyRainTime\":\"{station.ThisYear.HourlyRain.Ts.ToString(timeStampFormat)}\",");
			json.Append($"\"highDailyRainVal\":\"{station.ThisYear.DailyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highDailyRainTime\":\"{station.ThisYear.DailyRain.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"highMonthlyRainVal\":\"{station.ThisYear.MonthlyRain.Val.ToString(cumulus.RainFormat)}\",");
			json.Append($"\"highMonthlyRainTime\":\"{station.ThisYear.MonthlyRain.Ts:MM/yyyy}\",");
			json.Append($"\"longestDryPeriodVal\":\"{station.ThisYear.LongestDryPeriod.Val:F0}\",");
			json.Append($"\"longestDryPeriodTime\":\"{station.ThisYear.LongestDryPeriod.Ts.ToString(dateStampFormat)}\",");
			json.Append($"\"longestWetPeriodVal\":\"{station.ThisYear.LongestWetPeriod.Val:F0}\",");
			json.Append($"\"longestWetPeriodTime\":\"{station.ThisYear.LongestWetPeriod.Ts.ToString(dateStampFormat)}\"");

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
					Cumulus.LogMessage("Before rain today edit, raintoday=" + station.RainToday.ToString(cumulus.RainFormat, invNum) + " Raindaystart=" + station.raindaystart.ToString(cumulus.RainFormat, invNum));
					station.RainToday = raintoday;
					station.raindaystart = station.Raincounter - (station.RainToday / cumulus.Calib.Rain.Mult);
					Cumulus.LogMessage("After rain today edit,  raintoday=" + station.RainToday.ToString(cumulus.RainFormat, invNum) + " Raindaystart=" + station.raindaystart.ToString(cumulus.RainFormat, invNum));
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Edit rain today");
				}
			}

			var json = "{\"raintoday\":\"" + station.RainToday.ToString(cumulus.RainFormat, invNum) +
				"\",\"raincounter\":\"" + station.Raincounter.ToString(cumulus.RainFormat, invNum) +
				"\",\"startofdayrain\":\"" + station.raindaystart.ToString(cumulus.RainFormat, invNum) +
				"\",\"rainmult\":\"" + cumulus.Calib.Rain.Mult.ToString("F3", invNum) + "\"}";

			return json;
		}

		internal string GetRainTodayEditData()
		{
			var step = (cumulus.RainDPlaces == 1 ? "0.1" : "0.01");
			var json = "{\"raintoday\":\"" + station.RainToday.ToString(cumulus.RainFormat, invNum) +
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
	}
}
