using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using EmbedIO;
using ServiceStack;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	internal class DataEditor
	{
		private WeatherStation station;
		private readonly Cumulus cumulus;

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
			const string timeStampFormat = "g";
			const string dateStampFormat = "d";
			const string monthFormat = "MMM yyyy";

			// Records - Temperature values
			return new JsonObject
			{
				// Records - Temperature values
				["highTempVal"] = station.AllTime.HighTemp.GetValString(cumulus.TempFormat),
				["lowTempVal"] = station.AllTime.LowTemp.GetValString(cumulus.TempFormat),
				["highDewPointVal"] = station.AllTime.HighDewPoint.GetValString(cumulus.TempFormat),
				["lowDewPointVal"] = station.AllTime.LowDewPoint.GetValString(cumulus.TempFormat),
				["highApparentTempVal"] = station.AllTime.HighAppTemp.GetValString(cumulus.TempFormat),
				["lowApparentTempVal"] = station.AllTime.LowAppTemp.GetValString(cumulus.TempFormat),
				["highFeelsLikeVal"] = station.AllTime.HighFeelsLike.GetValString(cumulus.TempFormat),
				["lowFeelsLikeVal"] = station.AllTime.LowFeelsLike.GetValString(cumulus.TempFormat),
				["highHumidexVal"] = station.AllTime.HighHumidex.GetValString(cumulus.TempFormat),
				["lowWindChillVal"] = station.AllTime.LowChill.GetValString(cumulus.TempFormat),
				["highHeatIndexVal"] = station.AllTime.HighHeatIndex.GetValString(cumulus.TempFormat),
				["highMinTempVal"] = station.AllTime.HighMinTemp.GetValString(cumulus.TempFormat),
				["lowMaxTempVal"] = station.AllTime.LowMaxTemp.GetValString(cumulus.TempFormat),
				["highDailyTempRangeVal"] = station.AllTime.HighDailyTempRange.GetValString(cumulus.TempFormat),
				["lowDailyTempRangeVal"] = station.AllTime.LowDailyTempRange.GetValString(cumulus.TempFormat),
				// Records - Temperature timestamps
				["highTempTime"] = station.AllTime.HighTemp.GetTsString(timeStampFormat),
				["lowTempTime"] = station.AllTime.LowTemp.GetTsString(timeStampFormat),
				["highDewPointTime"] = station.AllTime.HighDewPoint.GetTsString(timeStampFormat),
				["lowDewPointTime"] = station.AllTime.LowDewPoint.GetTsString(timeStampFormat),
				["highApparentTempTime"] = station.AllTime.HighAppTemp.GetTsString(timeStampFormat),
				["lowApparentTempTime"] = station.AllTime.LowAppTemp.GetTsString(timeStampFormat),
				["highFeelsLikeTime"] = station.AllTime.HighFeelsLike.GetTsString(timeStampFormat),
				["lowFeelsLikeTime"] = station.AllTime.LowFeelsLike.GetTsString(timeStampFormat),
				["highHumidexTime"] = station.AllTime.HighHumidex.GetTsString(timeStampFormat),
				["lowWindChillTime"] = station.AllTime.LowChill.GetTsString(timeStampFormat),
				["highHeatIndexTime"] = station.AllTime.HighHeatIndex.GetTsString(timeStampFormat),
				["highMinTempTime"] = station.AllTime.HighMinTemp.GetTsString(timeStampFormat),
				["lowMaxTempTime"] = station.AllTime.LowMaxTemp.GetTsString(timeStampFormat),
				["highDailyTempRangeTime"] = station.AllTime.HighDailyTempRange.GetTsString(dateStampFormat),
				["lowDailyTempRangeTime"] = station.AllTime.LowDailyTempRange.GetTsString(dateStampFormat),
				// Records - Humidity values
				["highHumidityVal"] = station.AllTime.HighHumidity.GetValString(cumulus.HumFormat),
				["lowHumidityVal"] = station.AllTime.LowHumidity.GetValString(cumulus.HumFormat),
				// Records - Humidity times
				["highHumidityTime"] = station.AllTime.HighHumidity.GetTsString(timeStampFormat),
				["lowHumidityTime"] = station.AllTime.LowHumidity.GetTsString(timeStampFormat),
				// Records - Pressure values
				["highBarometerVal"] = station.AllTime.HighPress.GetValString(cumulus.PressFormat),
				["lowBarometerVal"] = station.AllTime.LowPress.GetValString(cumulus.PressFormat),
				// Records - Pressure times
				["highBarometerTime"] = station.AllTime.HighPress.GetTsString(timeStampFormat),
				["lowBarometerTime"] = station.AllTime.LowPress.GetTsString(timeStampFormat),
				// Records - Wind values
				["highGustVal"] = station.AllTime.HighGust.GetValString(cumulus.WindFormat),
				["highWindVal"] = station.AllTime.HighWind.GetValString(cumulus.WindAvgFormat),
				["highWindRunVal"] = station.AllTime.HighWindRun.GetValString(cumulus.WindRunFormat),
				// Records - Wind times
				["highGustTime"] = station.AllTime.HighGust.GetTsString(timeStampFormat),
				["highWindTime"] = station.AllTime.HighWind.GetTsString(timeStampFormat),
				["highWindRunTime"] = station.AllTime.HighWindRun.GetTsString(dateStampFormat),
				// Records - Rain values
				["highRainRateVal"] = station.AllTime.HighRainRate.GetValString(cumulus.RainFormat),
				["highHourlyRainVal"] = station.AllTime.HourlyRain.GetValString(cumulus.RainFormat),
				["highDailyRainVal"] = station.AllTime.DailyRain.GetValString(cumulus.RainFormat),
				["highRain24hVal"] = station.AllTime.HighRain24Hours.GetValString(cumulus.RainFormat),
				["highMonthlyRainVal"] = station.AllTime.MonthlyRain.GetValString(cumulus.RainFormat),
				["longestDryPeriodVal"] = station.AllTime.LongestDryPeriod.GetValString("f0"),
				["longestWetPeriodVal"] = station.AllTime.LongestWetPeriod.GetValString("f0"),
				// Records - Rain times
				["highRainRateTime"] = station.AllTime.HighRainRate.GetTsString(timeStampFormat),
				["highHourlyRainTime"] = station.AllTime.HourlyRain.GetTsString(timeStampFormat),
				["highDailyRainTime"] = station.AllTime.DailyRain.GetTsString(dateStampFormat),
				["highRain24hTime"] = station.AllTime.HighRain24Hours.GetTsString(timeStampFormat),
				["highMonthlyRainTime"] = station.AllTime.MonthlyRain.GetTsString(monthFormat),
				["longestDryPeriodTime"] = station.AllTime.LongestDryPeriod.GetTsString(dateStampFormat),
				["longestWetPeriodTime"] = station.AllTime.LongestWetPeriod.GetTsString(dateStampFormat)
			}.ToJson();
		}

		internal string GetRecordsDayFile(string recordType, DateTime? start = null, DateTime? end = null)
		{
			var timeStampFormat = "g";
			var dateStampFormat = "d";
			var monthFormat = "MMM yyyy";

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
			var highRain24h = new LocalRec(true);
			var dryPeriod = new LocalRec(true);
			var wetPeriod = new LocalRec(true);

			var thisDate = DateTime.MinValue;
			long startDate;
			long endDate = DateTime.Now.ToUnixTime();

			switch (recordType)
			{
				case "alltime":
					startDate = DateTime.MinValue.ToUnixTime();
					break;
				case "thisyear":
					var now = DateTime.Now;
					startDate = new DateTime(now.Year, 1, 1).ToUnixTime();
					break;
				case "thismonth":
					now = DateTime.Now;
					startDate = new DateTime(now.Year, now.Month, 1).ToUnixTime();
					break;
				case "thisperiod":
					startDate = start.Value.ToUnixTime();
					endDate = end.Value.ToUnixTime();
					timeStampFormat = "f";
					dateStampFormat = "D";
					break;
				default:
					startDate = DateTime.MinValue.ToUnixTime();
					break;
			}

			var rainThisMonth = 0.0;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;
			var isDryNow = false;
			var thisDateDry = DateTime.MinValue;
			var thisDateWet = DateTime.MinValue;

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
			var data = station.Database.Query<DayData>("select * from DayData where Timestamp >= ? and TimeStamp <= ? order by Timestamp", startDate, endDate);

			if (data.Count > 0)
			{
				thisDate = data[0].Date.Date;

				foreach (var rec in data)
				{
					// This assumes the day file is in date order!
					if (thisDate.Month != rec.Date.Month)
					{
						// reset the date and counter for a new month
						thisDate = rec.Date;
						rainThisMonth = 0;
					}
					// hi gust
					if (rec.HighGust.HasValue && rec.HighGust.Value > highGust.Value && rec.HighGustDateTime.HasValue)
					{
						highGust.Value = rec.HighGust.Value;
						highGust.Ts = rec.HighGustDateTime.Value;
					}
					// hi temp
					if (rec.HighTemp.HasValue && rec.HighTemp.Value > highTemp.Value && rec.HighTempDateTime.HasValue)
					{
						highTemp.Value = rec.HighTemp.Value;
						highTemp.Ts = rec.HighTempDateTime.Value;
					}
					// lo temp
					if (rec.LowTemp.HasValue && rec.LowTemp.Value < lowTemp.Value && rec.LowTempDateTime.HasValue)
					{
						lowTemp.Value = rec.LowTemp.Value;
						lowTemp.Ts = rec.LowTempDateTime.Value;
					}
					// hi min temp
					if (rec.LowTemp.HasValue && rec.LowTemp.Value > highMinTemp.Value && rec.LowTempDateTime.HasValue)
					{
						highMinTemp.Value = rec.LowTemp.Value;
						highMinTemp.Ts = rec.LowTempDateTime.Value;
					}
					// lo max temp
					if (rec.HighTemp.HasValue && rec.HighTemp.Value < lowMaxTemp.Value && rec.HighTempDateTime.HasValue)
					{
						lowMaxTemp.Value = rec.HighTemp.Value;
						lowMaxTemp.Ts = rec.HighTempDateTime.Value;
					}
					// hi temp range
					if (rec.LowTemp.HasValue && rec.HighTemp.HasValue && (rec.HighTemp.Value - rec.LowTemp.Value) > highTempRange.Value)
					{
						highTempRange.Value = rec.HighTemp.Value - rec.LowTemp.Value;
						highTempRange.Ts = rec.Date;
					}
					// lo temp range
					if (rec.LowTemp.HasValue && rec.HighTemp.HasValue && (rec.HighTemp.Value - rec.LowTemp.Value) < lowTempRange.Value)
					{
						lowTempRange.Value = rec.HighTemp.Value - rec.LowTemp.Value;
						lowTempRange.Ts = rec.Date;
					}
					// lo baro
					if (rec.LowPress.HasValue && rec.LowPress.Value < lowBaro.Value && rec.LowPressDateTime.HasValue)
					{
						lowBaro.Value = rec.LowPress.Value;
						lowBaro.Ts = rec.LowPressDateTime.Value;
					}
					// hi baro
					if (rec.HighPress.HasValue && rec.HighPress.Value > highBaro.Value && rec.HighPressDateTime.HasValue)
					{
						highBaro.Value = rec.HighPress.Value;
						highBaro.Ts = rec.HighPressDateTime.Value;
					}
					// hi rain rate
					if (rec.HighRainRate.HasValue && rec.HighRainRate.Value > highRainRate.Value && rec.HighRainRateDateTime.HasValue)
					{
						highRainRate.Value = rec.HighRainRate.Value;
						highRainRate.Ts = rec.HighRainRateDateTime.Value;
					}
					// hi rain day
					if (rec.TotalRain.HasValue)
					{
						if (rec.TotalRain.HasValue && rec.TotalRain.Value > highRainDay.Value)
						{
							highRainDay.Value = rec.TotalRain.Value;
							highRainDay.Ts = rec.Date;
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
					// 24h rain
					if (rec.HighRain24Hours.HasValue && rec.HighRain24Hours.Value > highRain24h.Value)
					{
						highRain24h.Value = rec.HighRain24Hours.Value;
						highRain24h.Ts = rec.Date;
					}

					// dry/wet period
					if (Convert.ToInt32(rec.TotalRain * 1000) >= rainThreshold)
					{
						if (isDryNow)
						{
							currentWetPeriod = 1;
							thisDateWet = rec.Date;
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
							thisDateWet = rec.Date;
						}
					}
					else
					{
						if (isDryNow)
						{
							currentDryPeriod++;
							thisDateDry = rec.Date;
						}
						else
						{
							currentDryPeriod = 1;
							thisDateDry = rec.Date;
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
						highWindRun.Ts = rec.Date;
					}
					// hi wind
					if (rec.HighAvgWind.HasValue && rec.HighAvgWind.Value > highWind.Value && rec.HighAvgWindDateTime.HasValue)
					{
						highWind.Value = rec.HighAvgWind.Value;
						highWind.Ts = rec.HighAvgWindDateTime.Value;
					}
					// lo humidity
					if (rec.LowHumidity.HasValue && rec.LowHumidity.Value < lowHum.Value && rec.LowHumidityDateTime.HasValue)
					{
						lowHum.Value = rec.LowHumidity.Value;
						lowHum.Ts = rec.LowHumidityDateTime.Value;
					}
					// hi humidity
					if (rec.HighHumidity.HasValue && rec.HighHumidity > highHum.Value && rec.HighHumidityDateTime.HasValue)
					{
						highHum.Value = rec.HighHumidity.Value;
						highHum.Ts = rec.HighHumidityDateTime.Value;
					}
					// hi heat index
					if (rec.HighHeatIndex.HasValue && rec.HighHeatIndex.Value > highHeatInd.Value && rec.HighHeatIndexDateTime.HasValue)
					{
						highHeatInd.Value = rec.HighHeatIndex.Value;
						highHeatInd.Ts = rec.HighHeatIndexDateTime.Value;
					}
					// hi app temp
					if (rec.HighAppTemp.HasValue && rec.HighAppTemp.Value > highAppTemp.Value && rec.HighAppTempDateTime.HasValue)
					{
						highAppTemp.Value = rec.HighAppTemp.Value;
						highAppTemp.Ts = rec.HighAppTempDateTime.Value;
					}
					// lo app temp
					if (rec.LowAppTemp.HasValue && rec.LowAppTemp < lowAppTemp.Value && rec.LowAppTempDateTime.HasValue)
					{
						lowAppTemp.Value = rec.LowAppTemp.Value;
						lowAppTemp.Ts = rec.LowAppTempDateTime.Value;
					}
					// hi rain hour
					if (rec.HighHourlyRain.HasValue && rec.HighHourlyRain.Value > highRainHour.Value && rec.HighHourlyRainDateTime.HasValue)
					{
						highRainHour.Value = rec.HighHourlyRain.Value;
						highRainHour.Ts = rec.HighHourlyRainDateTime.Value;
					}
					// lo wind chill
					if (rec.LowWindChill.HasValue && rec.LowWindChill.Value < lowWindChill.Value && rec.LowWindChillDateTime.HasValue)
					{
						lowWindChill.Value = rec.LowWindChill.Value;
						lowWindChill.Ts = rec.LowWindChillDateTime.Value;
					}
					// hi dewpt
					if (rec.HighDewPoint.HasValue && rec.HighDewPoint.Value > highDewPt.Value && rec.HighDewPointDateTime.HasValue)
					{
						highDewPt.Value = rec.HighDewPoint.Value;
						highDewPt.Ts = rec.HighDewPointDateTime.Value;
					}
					// lo dewpt
					if (rec.LowDewPoint.HasValue && rec.LowDewPoint.Value < lowDewPt.Value && rec.LowDewPointDateTime.HasValue)
					{
						lowDewPt.Value = rec.LowDewPoint.Value;
						lowDewPt.Ts = rec.LowDewPointDateTime.Value;
					}
					// hi feels like
					if (rec.HighFeelsLike.HasValue && rec.HighFeelsLike.Value > highFeelsLike.Value && rec.HighFeelsLikeDateTime.HasValue)
					{
						highFeelsLike.Value = rec.HighFeelsLike.Value;
						highFeelsLike.Ts = rec.HighFeelsLikeDateTime.Value;
					}
					// lo feels like
					if (rec.LowFeelsLike.HasValue && rec.LowFeelsLike < lowFeelsLike.Value && rec.LowFeelsLikeDateTime.HasValue)
					{
						lowFeelsLike.Value = rec.LowFeelsLike.Value;
						lowFeelsLike.Ts = rec.LowFeelsLikeDateTime.Value;
					}
					// hi humidex
					if (rec.HighHumidex.HasValue && rec.HighHumidex.Value > highHumidex.Value && rec.HighHumidexDateTime.HasValue)
					{
						highHumidex.Value = rec.HighHumidex.Value;
						highHumidex.Ts = rec.HighHumidexDateTime.Value;
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

			}
			else
			{
				cumulus.LogWarningMessage("GetRecordsDayFile: Error no day file records found");
			}

			return new JsonObject
			{
				["highTempValDayfile"] = highTemp.GetValString(cumulus.TempFormat),
				["highTempTimeDayfile"] = highTemp.GetTsString(timeStampFormat),
				["lowTempValDayfile"] = lowTemp.GetValString(cumulus.TempFormat),
				["lowTempTimeDayfile"] = lowTemp.GetTsString(timeStampFormat),
				["highDewPointValDayfile"] = highDewPt.GetValString(cumulus.TempFormat),
				["highDewPointTimeDayfile"] = highDewPt.GetTsString(timeStampFormat),
				["lowDewPointValDayfile"] = lowDewPt.GetValString(cumulus.TempFormat),
				["lowDewPointTimeDayfile"] = lowDewPt.GetTsString(timeStampFormat),
				["highApparentTempValDayfile"] = highAppTemp.GetValString(cumulus.TempFormat),
				["highApparentTempTimeDayfile"] = highAppTemp.GetTsString(timeStampFormat),
				["lowApparentTempValDayfile"] = lowAppTemp.GetValString(cumulus.TempFormat),
				["lowApparentTempTimeDayfile"] = lowAppTemp.GetTsString(timeStampFormat),
				["highFeelsLikeValDayfile"] = highFeelsLike.GetValString(cumulus.TempFormat),
				["highFeelsLikeTimeDayfile"] = highFeelsLike.GetTsString(timeStampFormat),
				["lowFeelsLikeValDayfile"] = lowFeelsLike.GetValString(cumulus.TempFormat),
				["lowFeelsLikeTimeDayfile"] = lowFeelsLike.GetTsString(timeStampFormat),
				["highHumidexValDayfile"] = highHumidex.GetValString(cumulus.TempFormat),
				["highHumidexTimeDayfile"] = highHumidex.GetTsString(timeStampFormat),
				["lowWindChillValDayfile"] = lowWindChill.GetValString(cumulus.TempFormat),
				["lowWindChillTimeDayfile"] = lowWindChill.GetTsString(timeStampFormat),
				["highHeatIndexValDayfile"] = highHeatInd.GetValString(cumulus.TempFormat),
				["highHeatIndexTimeDayfile"] = highHeatInd.GetTsString(timeStampFormat),
				["highMinTempValDayfile"] = highMinTemp.GetValString(cumulus.TempFormat),
				["highMinTempTimeDayfile"] = highMinTemp.GetTsString(timeStampFormat),
				["lowMaxTempValDayfile"] = lowMaxTemp.GetValString(cumulus.TempFormat),
				["lowMaxTempTimeDayfile"] = lowMaxTemp.GetTsString(timeStampFormat),
				["highDailyTempRangeValDayfile"] = highTempRange.GetValString(cumulus.TempFormat),
				["highDailyTempRangeTimeDayfile"] = highTempRange.GetTsString(dateStampFormat),
				["lowDailyTempRangeValDayfile"] = lowTempRange.GetValString(cumulus.TempFormat),
				["lowDailyTempRangeTimeDayfile"] = lowTempRange.GetTsString(dateStampFormat),
				["highHumidityValDayfile"] = highHum.GetValString(cumulus.HumFormat),
				["highHumidityTimeDayfile"] = highHum.GetTsString(timeStampFormat),
				["lowHumidityValDayfile"] = lowHum.GetValString(cumulus.HumFormat),
				["lowHumidityTimeDayfile"] = lowHum.GetTsString(timeStampFormat),
				["highBarometerValDayfile"] = highBaro.GetValString(cumulus.PressFormat),
				["highBarometerTimeDayfile"] = highBaro.GetTsString(timeStampFormat),
				["lowBarometerValDayfile"] = lowBaro.GetValString(cumulus.PressFormat),
				["lowBarometerTimeDayfile"] = lowBaro.GetTsString(timeStampFormat),
				["highGustValDayfile"] = highGust.GetValString(cumulus.WindFormat),
				["highGustTimeDayfile"] = highGust.GetTsString(timeStampFormat),
				["highWindValDayfile"] = highWind.GetValString(cumulus.WindRunFormat),
				["highWindTimeDayfile"] = highWind.GetTsString(timeStampFormat),
				["highWindRunValDayfile"] = highWindRun.GetValString(cumulus.WindRunFormat),
				["highWindRunTimeDayfile"] = highWindRun.GetTsString(dateStampFormat),
				["highRainRateValDayfile"] = highRainRate.GetValString(cumulus.RainFormat),
				["highRainRateTimeDayfile"] = highRainRate.GetTsString(timeStampFormat),
				["highHourlyRainValDayfile"] = highRainHour.GetValString(cumulus.RainFormat),
				["highHourlyRainTimeDayfile"] = highRainHour.GetTsString(timeStampFormat),
				["highDailyRainValDayfile"] = highRainDay.GetValString(cumulus.RainFormat),
				["highDailyRainTimeDayfile"] = highRainDay.GetTsString(dateStampFormat),
				["highMonthlyRainValDayfile"] = highRainMonth.GetValString(cumulus.RainFormat),
				["highMonthlyRainTimeDayfile"] = highRainMonth.GetTsString(monthFormat),
				["highRain24hValDayfile"] = highRain24h.GetValString(cumulus.RainFormat),
				["highRain24hTimeDayfile"] = highRain24h.GetTsString(timeStampFormat),
				["longestDryPeriodValDayfile"] = dryPeriod.GetValString(),
				["longestDryPeriodTimeDayfile"] = dryPeriod.GetTsString(dateStampFormat),
				["longestWetPeriodValDayfile"] = wetPeriod.GetValString(),
				["longestWetPeriodTimeDayfile"] = wetPeriod.GetTsString(dateStampFormat)
			}.ToJson();
		}

		internal string GetRecordsLogFile(string recordType)
		{
			const string timeStampFormat = "g";
			const string dateStampFormat = "d";
			const string monthFormat = "MMM yyyy";

			DateTime reqDate;

			switch (recordType)
			{
				case "alltime":
					reqDate = new DateTime(1900, 1, 1);
					break;
				case "thisyear":
					var now = DateTime.Now;
					// subtract a day to calculate 24 rain value
					reqDate = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
					break;
				case "thismonth":
					now = DateTime.Now;
					// subtract a day to calculate 24 rain value
					reqDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local);
					break;
				default:
					reqDate = DateTime.MinValue;
					break;
			}

			// subtract a day to calculate 24 rain value
			var datefrom = DateTime.SpecifyKind(reqDate.Date.AddDays(-1), DateTimeKind.Local);

			var started = false;
			var lastentrydate = datefrom;
			double? lastentryrain = null;
			var lastentrycounter = 0.0;

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

			// what do we deem to be too large a jump in the rainfall counter to be true? use 20 mm or 0.8 inches
			var counterJumpTooBig = cumulus.Units.Rain == 0 ? 20 : 0.8;

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
			var highRain24h = new LocalRec(true);
			var highRainMonth = new LocalRec(true);
			var dryPeriod = new LocalRec(true);
			var wetPeriod = new LocalRec(true);

			var currentDay = datefrom;
			var dayHighTemp = new LocalRec(true);
			var dayLowTemp = new LocalRec(false);
			double dayWindRun = Cumulus.DefaultHiVal;
			double dayRain = Cumulus.DefaultHiVal;

			highRainHour.Value = 0;
			highRain24h.Value = 0;

			var thisDateDry = DateTime.MinValue;
			var thisDateWet = DateTime.MinValue;

			var rain1hLog = new Queue<LastHourRainLog>();
			var rain24hLog = new Queue<LastHourRainLog>();

			var monthlyRain = 0.0;
			var totalRainfall = 0.0;

			var watch = System.Diagnostics.Stopwatch.StartNew();

			double _day24h = 0;
			DateTime _dayTs;

			cumulus.LogDebugMessage($"GetAllTimeRecLogFile: Processing - {recordType}");

			try
			{
				var rows = station.Database.Query<IntervalData>("select * from IntervalData where Timestamp >= ? order by Timestamp", datefrom.ToUnixTime());
				var cnt = 0;
				foreach (var rec in rows)
				{
					cnt++;
					// We need to work in meteo dates not clock dates for day hi/lows
					var metoDate = rec.StationTime.AddHours(cumulus.GetHourInc());
					var recTime = rec.StationTime;

					if (!started)
					{
						lastentrydate = recTime;
						currentDay = metoDate;

						if (metoDate >= reqDate)
						{
							started = true;
							totalRainfall = lastentryrain ?? 0;
						}
						else
						{
							// OK we are within 24 hours of the start date, so record rain values
							AddLastHoursRainEntry(recTime, totalRainfall + rec.RainToday ?? 0, ref rain1hLog, ref rain24hLog);
							lastentryrain = rec.RainToday;
							lastentrycounter = rec.RainCounter ?? 0;
							continue;
						}
					}

					// low chill
					if (rec.WindChill.HasValue && rec.WindChill.Value < lowWindChill.Value)
					{
						lowWindChill.Value = rec.WindChill.Value;
						lowWindChill.Ts = recTime;
					}
					// hi heat
					if (rec.HeatIndex.HasValue && rec.HeatIndex.Value > highHeatInd.Value)
					{
						highHeatInd.Value = rec.HeatIndex.Value;
						highHeatInd.Ts = recTime;
					}
					// hi/low appt
					if (rec.Apparent.HasValue)
					{
						if (rec.Apparent.Value > highAppTemp.Value)
						{
							highAppTemp.Value = rec.Apparent.Value;
							highAppTemp.Ts = recTime;
						}
						if (rec.Apparent.Value < lowAppTemp.Value)
						{
							lowAppTemp.Value = rec.Apparent.Value;
							lowAppTemp.Ts = recTime;
						}
					}
					// hi/low feels like
					if (rec.FeelsLike.HasValue)
					{
						if (rec.FeelsLike.Value > highFeelsLike.Value)
						{
							highFeelsLike.Value = rec.FeelsLike.Value;
							highFeelsLike.Ts = recTime;
						}
						if (rec.FeelsLike.Value < lowFeelsLike.Value)
						{
							lowFeelsLike.Value = rec.FeelsLike.Value;
							lowFeelsLike.Ts = recTime;
						}
					}

					// hi/low humidex
					if (rec.Humidex.HasValue)
					{
						if (rec.Humidex.Value > highHumidex.Value)
						{
							highHumidex.Value = rec.Humidex.Value;
							highHumidex.Ts = recTime;
						}
					}

					// hi/low temp
					if (rec.Temp.HasValue)
					{
						if (rec.Temp.Value > highTemp.Value)
						{
							highTemp.Value = rec.Temp.Value;
							highTemp.Ts = recTime;
						}
						// lo temp
						if (rec.Temp.Value < lowTemp.Value)
						{
							lowTemp.Value = rec.Temp.Value;
							lowTemp.Ts = recTime;
						}
					}
					// hi/low dewpoint
					if (rec.DewPoint.HasValue)
					{
						if (rec.DewPoint.Value > highDewPt.Value)
						{
							highDewPt.Value = rec.DewPoint.Value;
							highDewPt.Ts = recTime;
						}
						// low dewpoint
						if (rec.DewPoint.Value < lowDewPt.Value)
						{
							lowDewPt.Value = rec.DewPoint.Value;
							lowDewPt.Ts = recTime;
						}
					}
					// hi/low hum
					if (rec.Humidity.HasValue)
					{
						if (rec.Humidity.Value > highHum.Value)
						{
							highHum.Value = rec.Humidity.Value;
							highHum.Ts = recTime;
						}
						// lo hum
						if (rec.Humidity.Value < lowHum.Value)
						{
							lowHum.Value = rec.Humidity.Value;
							lowHum.Ts = recTime;
						}
					}
					// hi/Low baro
					if (rec.Pressure.HasValue)
					{
						if (rec.Pressure.Value > highBaro.Value)
						{
							highBaro.Value = rec.Pressure.Value;
							highBaro.Ts = recTime;
						}
						// lo hum
						if (rec.Pressure < lowBaro.Value)
						{
							lowBaro.Value = rec.Pressure.Value;
							lowBaro.Ts = recTime;
						}
					}
					// hi gust
					if (rec.WindGust10m.HasValue && rec.WindGust10m.Value > highGust.Value)
					{
						highGust.Value = rec.WindGust10m.Value;
						highGust.Ts = recTime;
					}
					// hi wind
					if (rec.WindAvg.HasValue && rec.WindAvg.Value > highWind.Value)
					{
						highWind.Value = rec.WindAvg.Value;
						highWind.Ts = recTime;
					}
					// hi rain rate
					if (rec.RainRate.HasValue && rec.RainRate.Value > highRainRate.Value)
					{
						highRainRate.Value = rec.RainRate.Value;
						highRainRate.Ts = recTime;
					}

					if (rec.Temp.HasValue)
					{
						if (rec.Temp.Value > dayHighTemp.Value)
						{
							dayHighTemp.Value = rec.Temp.Value;
							dayHighTemp.Ts = recTime;
						}

						if (rec.Temp.Value < dayLowTemp.Value)
						{
							dayLowTemp.Value = rec.Temp.Value;
							dayLowTemp.Ts = recTime;
						}
					}

					dayWindRun += recTime.Subtract(lastentrydate).TotalHours * rec.WindAvg ?? 0;

					if (dayWindRun > highWindRun.Value)
					{
						highWindRun.Value = dayWindRun;
						highWindRun.Ts = currentDay;
					}

					// new meteo day
					if (currentDay.Date != metoDate.Date)
					{
						if (dayHighTemp.Value != Cumulus.DefaultHiVal && dayHighTemp.Value < lowMaxTemp.Value)
						{
							lowMaxTemp.Value = dayHighTemp.Value;
							lowMaxTemp.Ts = dayHighTemp.Ts;
						}
						if (dayLowTemp.Value != Cumulus.DefaultLoVal && dayLowTemp.Value > highMinTemp.Value)
						{
							highMinTemp.Value = dayLowTemp.Value;
							highMinTemp.Ts = dayLowTemp.Ts;
						}
						if (dayHighTemp.Value != Cumulus.DefaultHiVal && dayLowTemp.Value != Cumulus.DefaultLoVal && dayHighTemp.Value - dayLowTemp.Value > highTempRange.Value)
						{
							highTempRange.Value = dayHighTemp.Value - dayLowTemp.Value;
							highTempRange.Ts = currentDay;
						}
						if (dayHighTemp.Value != Cumulus.DefaultHiVal && dayLowTemp.Value != Cumulus.DefaultLoVal && dayHighTemp.Value - dayLowTemp.Value < lowTempRange.Value)
						{
							lowTempRange.Value = dayHighTemp.Value - dayLowTemp.Value;
							lowTempRange.Ts = currentDay;
						}

						// logging format changed on with C1 v1.9.3 b1055 in Dec 2012
						// before that date the 00:00 log entry contained the rain total for the day before and the next log entry was reset to zero
						// after that build the total was reset to zero in the entry
						// messy!
						// no final rainfall entry after this date (approx). The best we can do is add in the increase in rain counter during this preiod
						var rollovertime = new TimeSpan(-cumulus.GetHourInc(), 0, 0);
						if (rec.RainToday.HasValue && rec.RainToday > 0 && rec.StationTime.TimeOfDay == rollovertime)
						{
							dayRain = rec.RainToday ?? 0;
						}
						else if (rec.RainCounter.HasValue && (rec.RainCounter - lastentrycounter > 0) && (rec.RainCounter - lastentrycounter < counterJumpTooBig))
						{
							dayRain += (rec.RainCounter ?? 0 - lastentrycounter) * cumulus.Calib.Rain.Mult;
						}

						if (dayRain > highRainDay.Value)
						{
							highRainDay.Value = dayRain;
							highRainDay.Ts = currentDay;
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
						dayHighTemp.Value = rec.Temp ?? Cumulus.DefaultHiVal;
						dayLowTemp.Value = rec.Temp ?? Cumulus.DefaultLoVal;
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
						dayWindRun += rec.StationTime.Subtract(lastentrydate).TotalHours * rec.WindAvg.Value;
					}

					if (dayWindRun > highWindRun.Value)
					{
						highWindRun.Value = dayWindRun;
						highWindRun.Ts = currentDay;
					}

					// hourly rain
					/*
					* need to track what the rainfall has been in the last rolling hour and 24 hours
					* across day rollovers where the count resets
					*/
					AddLastHoursRainEntry(recTime, totalRainfall + dayRain, ref rain1hLog, ref rain24hLog);

					var rainThisHour = rain1hLog.Last().Raincounter - rain1hLog.Peek().Raincounter;
					if (rainThisHour > highRainHour.Value)
					{
						highRainHour.Value = rainThisHour;
						highRainHour.Ts = recTime;
					}

					var rain24h = rain24hLog.Last().Raincounter - rain24hLog.Peek().Raincounter;
					if (rain24h > highRain24h.Value)
					{
						highRain24h.Value = rain24h;
						highRain24h.Ts = recTime;
					}

					if (rain24h > _day24h)
					{
						_day24h = rain24h;
						_dayTs = recTime;
					}

					// new meteo day, part 2
					if (currentDay.Date != metoDate.Date)
					{
						currentDay = metoDate;
						dayHighTemp.Value = rec.Temp ?? Cumulus.DefaultHiVal;
						dayLowTemp.Value = rec.Temp ?? Cumulus.DefaultLoVal;
						dayWindRun = 0;
						totalRainfall += dayRain;

						_day24h = rain24h;
						_dayTs = recTime;
					}

					lastentrydate = recTime;
					lastentrycounter = rec.RainCounter ?? 0;
					}
			}
			catch (Exception e)
			{
				cumulus.LogExceptionMessage(e, $"GetRecordsLogFile: Error");
			}

			cumulus.LogDebugMessage($"GetAllTimeRecLogFile: Finished processing - {recordType}");

			rain1hLog.Clear();
			rain24hLog.Clear();

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

			watch.Stop();
			var elapsed = watch.ElapsedMilliseconds;
			cumulus.LogDebugMessage($"GetRecordsLogFile: Logfiles parse = {elapsed} ms");

			return new JsonObject
			{
				["highTempValLogfile"] = highTemp.GetValString(cumulus.TempFormat),
				["highTempTimeLogfile"] = highTemp.GetTsString(timeStampFormat),
				["lowTempValLogfile"] = lowTemp.GetValString(timeStampFormat),
				["lowTempTimeLogfile"] = lowTemp.GetTsString(timeStampFormat),
				["highDewPointValLogfile"] = highDewPt.GetValString(cumulus.TempFormat),
				["highDewPointTimeLogfile"] = highDewPt.GetTsString(timeStampFormat),
				["lowDewPointValLogfile"] = lowDewPt.GetValString(cumulus.TempFormat),
				["lowDewPointTimeLogfile"] = lowDewPt.GetTsString(timeStampFormat),
				["highApparentTempValLogfile"] = highAppTemp.GetValString(cumulus.TempFormat),
				["highApparentTempTimeLogfile"] = highAppTemp.GetTsString(timeStampFormat),
				["lowApparentTempValLogfile"] = lowAppTemp.GetValString(cumulus.TempFormat),
				["lowApparentTempTimeLogfile"] = lowAppTemp.GetTsString(timeStampFormat),
				["highFeelsLikeValLogfile"] = highFeelsLike.GetValString(cumulus.TempFormat),
				["highFeelsLikeTimeLogfile"] = highFeelsLike.GetTsString(timeStampFormat),
				["lowFeelsLikeValLogfile"] = lowFeelsLike.GetValString(cumulus.TempFormat),
				["lowFeelsLikeTimeLogfile"] = lowFeelsLike.GetTsString(timeStampFormat),
				["highHumidexValLogfile"] = highHumidex.GetValString(cumulus.TempFormat),
				["highHumidexTimeLogfile"] = highHumidex.GetTsString(timeStampFormat),
				["lowWindChillValLogfile"] = lowWindChill.GetValString(cumulus.TempFormat),
				["lowWindChillTimeLogfile"] = lowWindChill.GetTsString(timeStampFormat),
				["highHeatIndexValLogfile"] = highHeatInd.GetValString(cumulus.TempFormat),
				["highHeatIndexTimeLogfile"] = highHeatInd.GetTsString(timeStampFormat),
				["highMinTempValLogfile"] = highMinTemp.GetValString(cumulus.TempFormat),
				["highMinTempTimeLogfile"] = highMinTemp.GetTsString(timeStampFormat),
				["lowMaxTempValLogfile"] = lowMaxTemp.GetValString(cumulus.TempFormat),
				["lowMaxTempTimeLogfile"] = lowMaxTemp.GetTsString(timeStampFormat),
				["highDailyTempRangeValLogfile"] = highTempRange.GetValString(cumulus.TempFormat),
				["highDailyTempRangeTimeLogfile"] = highTempRange.GetTsString(dateStampFormat),
				["lowDailyTempRangeValLogfile"] = lowTempRange.GetValString(cumulus.TempFormat),
				["lowDailyTempRangeTimeLogfile"] = lowTempRange.GetTsString(dateStampFormat),
				["highHumidityValLogfile"] = highHum.GetValString(cumulus.HumFormat),
				["highHumidityTimeLogfile"] = highHum.GetTsString(timeStampFormat),
				["lowHumidityValLogfile"] = lowHum.GetValString(cumulus.HumFormat),
				["lowHumidityTimeLogfile"] = lowHum.GetTsString(timeStampFormat),
				["highBarometerValLogfile"] = highBaro.GetValString(cumulus.PressFormat),
				["highBarometerTimeLogfile"] = highBaro.GetTsString(timeStampFormat),
				["lowBarometerValLogfile"] = lowBaro.GetValString(cumulus.PressFormat),
				["lowBarometerTimeLogfile"] = lowBaro.GetTsString(timeStampFormat),
				["highGustValLogfile"] = highGust.GetValString(cumulus.WindFormat),
				["highGustTimeLogfile"] = highGust.GetTsString(timeStampFormat),
				["highWindValLogfile"] = highWind.GetValString(cumulus.WindAvgFormat),
				["highWindTimeLogfile"] = highWind.GetTsString(timeStampFormat),
				["highWindRunValLogfile"] = highWindRun.GetValString(cumulus.WindRunFormat),
				["highWindRunTimeLogfile"] = highWindRun.GetTsString(dateStampFormat),
				["highRainRateValLogfile"] = highRainRate.GetValString(cumulus.RainFormat),
				["highRainRateTimeLogfile"] = highRainRate.GetTsString(timeStampFormat),
				["highHourlyRainValLogfile"] = highRainHour.GetValString(cumulus.RainFormat),
				["highHourlyRainTimeLogfile"] = highRainHour.GetTsString(timeStampFormat),
				["highDailyRainValLogfile"] = highRainDay.GetValString(cumulus.RainFormat),
				["highDailyRainTimeLogfile"] = highRainDay.GetTsString(dateStampFormat),
				["highRain24hValLogfile"] = highRain24h.GetValString(cumulus.RainFormat),
				["highRain24hTimeLogfile"] = highRain24h.GetTsString(timeStampFormat),
				["highMonthlyRainValLogfile"] = highRainMonth.GetValString(cumulus.RainFormat),
				["highMonthlyRainTimeLogfile"] = highRainMonth.GetTsString(monthFormat),
				["longestDryPeriodValLogfile"] = dryPeriod.GetValString(),
				["longestDryPeriodTimeLogfile"] = dryPeriod.GetTsString(dateStampFormat),
				["longestWetPeriodValLogfile"] = wetPeriod.GetValString(),
				["longestWetPeriodTimeLogfile"] = wetPeriod.GetTsString(dateStampFormat)
			}.ToJson();
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

			var txtValue = newData[1].Split('=')[1];
			var value = double.Parse(txtValue);

			var txtTime = newData[2].Split('=')[1];
			var time = localeDateTimeStrToDate(txtTime);

			try
			{
				switch (field)
				{
					case "highTemp":
						station.SetAlltime(station.AllTime.HighTemp, value, time);
						break;
					case "lowTemp":
						station.SetAlltime(station.AllTime.LowTemp, value, time);
						break;
					case "highDewPoint":
						station.SetAlltime(station.AllTime.HighDewPoint, value, time);
						break;
					case "lowDewPoint":
						station.SetAlltime(station.AllTime.LowDewPoint, value, time);
						break;
					case "highApparentTemp":
						station.SetAlltime(station.AllTime.HighAppTemp, value, time);
						break;
					case "lowApparentTemp":
						station.SetAlltime(station.AllTime.LowAppTemp, value, time);
						break;
					case "highFeelsLike":
						station.SetAlltime(station.AllTime.HighFeelsLike, value, time);
						break;
					case "lowFeelsLike":
						station.SetAlltime(station.AllTime.LowFeelsLike, value, time);
						break;
					case "highHumidex":
						station.SetAlltime(station.AllTime.HighHumidex, value, time);
						break;
					case "lowWindChill":
						station.SetAlltime(station.AllTime.LowChill, value, time);
						break;
					case "highHeatIndex":
						station.SetAlltime(station.AllTime.HighHeatIndex, value, time);
						break;
					case "highMinTemp":
						station.SetAlltime(station.AllTime.HighMinTemp, value, time);
						break;
					case "lowMaxTemp":
						station.SetAlltime(station.AllTime.LowMaxTemp, value, time);
						break;
					case "highDailyTempRange":
						station.SetAlltime(station.AllTime.HighDailyTempRange, value, time);
						break;
					case "lowDailyTempRange":
						station.SetAlltime(station.AllTime.LowDailyTempRange, value, time);
						break;
					case "highHumidity":
						station.SetAlltime(station.AllTime.HighHumidity, int.Parse(txtValue), time);
						break;
					case "lowHumidity":
						station.SetAlltime(station.AllTime.LowHumidity, int.Parse(txtValue), time);
						break;
					case "highBarometer":
						station.SetAlltime(station.AllTime.HighPress, value, time);
						break;
					case "lowBarometer":
						station.SetAlltime(station.AllTime.LowPress, value, time);
						break;
					case "highGust":
						station.SetAlltime(station.AllTime.HighGust, value, time);
						break;
					case "highWind":
						station.SetAlltime(station.AllTime.HighWind, value, time);
						break;
					case "highWindRun":
						station.SetAlltime(station.AllTime.HighWindRun, value, time);
						break;
					case "highRainRate":
						station.SetAlltime(station.AllTime.HighRainRate, value, time);
						break;
					case "highHourlyRain":
						station.SetAlltime(station.AllTime.HourlyRain, value, time);
						break;
					case "highDailyRain":
						station.SetAlltime(station.AllTime.DailyRain, value, time);
						break;
					case "highRain24h":
						station.SetAlltime(station.AllTime.HighRain24Hours, value, time);
						break;
					case "highMonthlyRain":
						station.SetAlltime(station.AllTime.MonthlyRain, value, localeMonthYearStrToDate(txtTime));
						break;
					case "longestDryPeriod":
						station.SetAlltime(station.AllTime.LongestDryPeriod, int.Parse(txtValue), time);
						break;
					case "longestWetPeriod":
						station.SetAlltime(station.AllTime.LongestWetPeriod, int.Parse(txtValue), time);
						break;
					default:
						return "Data index not recognised";
				}
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
			return "Success";
		}

		internal string EditMonthlyRecs(IHttpContext context)
		{
			var request = context.Request;
			string text;

			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = Uri.UnescapeDataString(reader.ReadToEnd());
			}

			// Eg "name=2-highTemp&value=134.6&time=29/01/23 08:07"
			var newData = text.Split('&');

			var monthField = newData[0].Split('=')[1].Split('-');
			var month = int.Parse(monthField[0]);
			var field = monthField[1];

			var txtValue = newData[1].Split('=')[1];
			var value = double.Parse(txtValue);

			var txtTime = newData[2].Split('=')[1];
			var time = localeDateTimeStrToDate(txtTime);

			try
			{
				lock (station.monthlyalltimeIniThreadLock)
				{
					switch (field)
					{
						case "highTemp":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighTemp, value, time);
							break;
						case "lowTemp":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowTemp, value, time);
							break;
						case "highDewPoint":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighDewPoint, value, time);
							break;
						case "lowDewPoint":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowDewPoint, value, time);
							break;
						case "highApparentTemp":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighAppTemp, value, time);
							break;
						case "lowApparentTemp":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowAppTemp, value, time);
							break;
						case "highFeelsLike":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighFeelsLike, value, time);
							break;
						case "lowFeelsLike":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowFeelsLike, value, time);
							break;
						case "highHumidex":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHumidex, value, time);
							break;
						case "lowWindChill":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowChill, value, time);
							break;
						case "highHeatIndex":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHeatIndex, value, time);
							break;
						case "highMinTemp":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighMinTemp, value, time);
							break;
						case "lowMaxTemp":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowMaxTemp, value, time);
							break;
						case "highDailyTempRange":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighDailyTempRange, value, time);
							break;
						case "lowDailyTempRange":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowDailyTempRange, value, time);
							break;
						case "highHumidity":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighHumidity, int.Parse(txtValue), time);
							break;
						case "lowHumidity":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowHumidity, int.Parse(txtValue), time);
							break;
						case "highBarometer":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighPress, value, time);
							break;
						case "lowBarometer":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LowPress, value, time);
							break;
						case "highGust":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighGust, value, time);
							break;
						case "highWind":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighWind, value, time);
							break;
						case "highWindRun":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighWindRun, value, time);
							break;
						case "highRainRate":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighRainRate, value, time);
							break;
						case "highHourlyRain":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HourlyRain, value, time);
							break;
						case "highDailyRain":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].DailyRain, value, time);
							break;
						case "highRain24h":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].HighRain24Hours, value, time);
							break;
						case "highMonthlyRain":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].MonthlyRain, value, localeMonthYearStrToDate(txtTime));
							break;
						case "longestDryPeriod":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LongestDryPeriod, int.Parse(txtValue), time);
							break;
						case "longestWetPeriod":
							station.SetMonthlyAlltime(station.MonthlyRecs[month].LongestWetPeriod, int.Parse(txtValue), time);
							break;
						default:
							return "Data index not recognised";
					}
				}
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
			return "Success";
		}

		internal string GetMonthlyRecData()
		{
			const string timeStampFormat = "g";
			const string dateStampFormat = "d";
			const string monthFormat = "MMM yyyy";

			var jsonObj = new JsonObject();

			for (var m = 1; m <= 12; m++)
			{
				// Records - Temperature values
				jsonObj.Add($"{m}-highTempVal", station.MonthlyRecs[m].HighTemp.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowTempVal", station.MonthlyRecs[m].LowTemp.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highDewPointVal", station.MonthlyRecs[m].HighDewPoint.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowDewPointVal", station.MonthlyRecs[m].LowDewPoint.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highApparentTempVal", station.MonthlyRecs[m].HighAppTemp.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowApparentTempVal", station.MonthlyRecs[m].LowAppTemp.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highFeelsLikeVal", station.MonthlyRecs[m].HighFeelsLike.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowFeelsLikeVal", station.MonthlyRecs[m].LowFeelsLike.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highHumidexVal", station.MonthlyRecs[m].HighHumidex.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowWindChillVal", station.MonthlyRecs[m].LowChill.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highHeatIndexVal", station.MonthlyRecs[m].HighHeatIndex.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highMinTempVal", station.MonthlyRecs[m].HighMinTemp.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowMaxTempVal", station.MonthlyRecs[m].LowMaxTemp.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highDailyTempRangeVal", station.MonthlyRecs[m].HighDailyTempRange.GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowDailyTempRangeVal", station.MonthlyRecs[m].LowDailyTempRange.GetValString(cumulus.TempFormat));
				// Records - Temperature timestamps
				jsonObj.Add($"{m}-highTempTime", station.MonthlyRecs[m].HighTemp.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowTempTime", station.MonthlyRecs[m].LowTemp.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highDewPointTime", station.MonthlyRecs[m].HighDewPoint.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowDewPointTime", station.MonthlyRecs[m].LowDewPoint.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highApparentTempTime", station.MonthlyRecs[m].HighAppTemp.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowApparentTempTime", station.MonthlyRecs[m].LowAppTemp.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highFeelsLikeTime", station.MonthlyRecs[m].HighFeelsLike.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowFeelsLikeTime", station.MonthlyRecs[m].LowFeelsLike.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highHumidexTime", station.MonthlyRecs[m].HighHumidex.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowWindChillTime", station.MonthlyRecs[m].LowChill.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highHeatIndexTime", station.MonthlyRecs[m].HighHeatIndex.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highMinTempTime", station.MonthlyRecs[m].HighMinTemp.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowMaxTempTime", station.MonthlyRecs[m].LowMaxTemp.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highDailyTempRangeTime", station.MonthlyRecs[m].HighDailyTempRange.GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-lowDailyTempRangeTime", station.MonthlyRecs[m].LowDailyTempRange.GetTsString(dateStampFormat));
				// Records - Humidity values
				jsonObj.Add($"{m}-highHumidityVal", station.MonthlyRecs[m].HighHumidity.GetValString(cumulus.HumFormat));
				jsonObj.Add($"{m}-lowHumidityVal", station.MonthlyRecs[m].LowHumidity.GetValString(cumulus.HumFormat));
				// Records - Humidity times
				jsonObj.Add($"{m}-highHumidityTime", station.MonthlyRecs[m].HighHumidity.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowHumidityTime", station.MonthlyRecs[m].LowHumidity.GetTsString(timeStampFormat));
				// Records - Pressure values
				jsonObj.Add($"{m}-highBarometerVal", station.MonthlyRecs[m].HighPress.GetValString(cumulus.PressFormat));
				jsonObj.Add($"{m}-lowBarometerVal", station.MonthlyRecs[m].LowPress.GetValString(cumulus.PressFormat));
				// Records - Pressure times
				jsonObj.Add($"{m}-highBarometerTime", station.MonthlyRecs[m].HighPress.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowBarometerTime", station.MonthlyRecs[m].LowPress.GetTsString(timeStampFormat));
				// Records - Wind values
				jsonObj.Add($"{m}-highGustVal", station.MonthlyRecs[m].HighGust.GetValString(cumulus.WindFormat));
				jsonObj.Add($"{m}-highWindVal", station.MonthlyRecs[m].HighWind.GetValString(cumulus.WindAvgFormat));
				jsonObj.Add($"{m}-highWindRunVal", station.MonthlyRecs[m].HighWindRun.GetValString(cumulus.WindRunFormat));
				// Records - Wind times
				jsonObj.Add($"{m}-highGustTime", station.MonthlyRecs[m].HighGust.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highWindTime", station.MonthlyRecs[m].HighWind.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highWindRunTime", station.MonthlyRecs[m].HighWindRun.GetTsString(dateStampFormat));
				// Records - Rain values
				jsonObj.Add($"{m}-highRainRateVal", station.MonthlyRecs[m].HighRainRate.GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highHourlyRainVal", station.MonthlyRecs[m].HourlyRain.GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highDailyRainVal", station.MonthlyRecs[m].DailyRain.GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highRain24hVal", station.MonthlyRecs[m].HighRain24Hours.GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highMonthlyRainVal", station.MonthlyRecs[m].MonthlyRain.GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-longestDryPeriodVal", station.MonthlyRecs[m].LongestDryPeriod.GetValString("f0"));
				jsonObj.Add($"{m}-longestWetPeriodVal", station.MonthlyRecs[m].LongestWetPeriod.GetValString("f0"));
				// Records - Rain times
				jsonObj.Add($"{m}-highRainRateTime", station.MonthlyRecs[m].HighRainRate.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highHourlyRainTime", station.MonthlyRecs[m].HourlyRain.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highDailyRainTime", station.MonthlyRecs[m].DailyRain.GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-highRain24hTime", station.MonthlyRecs[m].HighRain24Hours.GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highMonthlyRainTime", station.MonthlyRecs[m].MonthlyRain.GetTsString(monthFormat));
				jsonObj.Add($"{m}-longestDryPeriodTime", station.MonthlyRecs[m].LongestDryPeriod.GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-longestWetPeriodTime", station.MonthlyRecs[m].LongestWetPeriod.GetTsString(dateStampFormat));
			}

			return jsonObj.ToJson();
		}

		internal string GetMonthlyRecDayFile()
		{
			const string timeStampFormat = "g";
			const string dateStampFormat = "d";
			const string monthFormat = "MMM yyyy";

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
			var highRain24h = new LocalRec[12];
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
				highRain24h[i] = new LocalRec(true);
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

			try
			{
				// get all the data from the database
				var data = station.Database.Query<DayData>("select * from DayData order by Timestamp");

				if (data.Count > 0)
				{
					for (var i = 0; i < data.Count; i++)
					{
						var loggedDate = data[i].Date;
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
						if (data[i].HighGust.HasValue && data[i].HighGustDateTime.HasValue &&
							(data[i].HighGust.Value > highGust[monthOffset].Value))
						{
							highGust[monthOffset].Value = data[i].HighGust.Value;
							highGust[monthOffset].Ts = data[i].HighGustDateTime.Value;
						}
						if (data[i].LowTemp.HasValue && data[i].LowTempDateTime.HasValue)
						{
							// lo temp
							if (data[i].LowTemp.Value < lowTemp[monthOffset].Value)
							{
								lowTemp[monthOffset].Value = data[i].LowTemp.Value;
								lowTemp[monthOffset].Ts = data[i].LowTempDateTime.Value;
							}
							// hi min temp
							if (data[i].LowTemp.Value > highMinTemp[monthOffset].Value)
							{
								highMinTemp[monthOffset].Value = data[i].LowTemp.Value;
								highMinTemp[monthOffset].Ts = data[i].LowTempDateTime.Value;
							}
						}
						if (data[i].HighTemp.HasValue && data[i].HighTempDateTime.HasValue)
						{
							// hi temp
							if (data[i].HighTemp.Value > highTemp[monthOffset].Value)
							{
								highTemp[monthOffset].Value = data[i].HighTemp.Value;
								highTemp[monthOffset].Ts = data[i].HighTempDateTime.Value;
							}
							// lo max temp
							if (data[i].HighTemp.Value < lowMaxTemp[monthOffset].Value)
							{
								lowMaxTemp[monthOffset].Value = data[i].HighTemp.Value;
								lowMaxTemp[monthOffset].Ts = data[i].HighTempDateTime.Value;
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
						if (data[i].LowPress.HasValue && data[i].LowPressDateTime.HasValue &&
							(data[i].LowPress.Value < lowBaro[monthOffset].Value))
						{
							lowBaro[monthOffset].Value = data[i].LowPress.Value;
							lowBaro[monthOffset].Ts = data[i].LowPressDateTime.Value;
						}
						// hi baro
						if (data[i].HighPress.HasValue && data[i].HighPressDateTime.HasValue &&
							(data[i].HighPress.Value > highBaro[monthOffset].Value))
						{
							highBaro[monthOffset].Value = data[i].HighPress.Value;
							highBaro[monthOffset].Ts = data[i].HighPressDateTime.Value;
						}
						// hi rain rate
						if (data[i].HighRainRate.HasValue && data[i].HighRainRateDateTime.HasValue &&
							(data[i].HighRainRate.Value > highRainRate[monthOffset].Value))
						{
							highRainRate[monthOffset].Value = data[i].HighRainRate.Value;
							highRainRate[monthOffset].Ts = data[i].HighRainRateDateTime.Value;
						}

						// hi 24h rain
						if (data[i].HighRain24Hours.HasValue && data[i].HighRain24Hours.Value > highRain24h[monthOffset].Value)
						{
							highRain24h[monthOffset].Value = data[i].HighRain24Hours.Value;
							highRain24h[monthOffset].Ts = data[i].HighRain24HrDateTime.Value;
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
						if (data[i].HighAvgWind.HasValue && data[i].HighAvgWindDateTime.HasValue && data[i].HighAvgWind.Value > highWind[monthOffset].Value)
						{
							highWind[monthOffset].Value = data[i].HighAvgWind.Value;
							highWind[monthOffset].Ts = data[i].HighAvgWindDateTime.Value;
						}

						// lo humidity
						if (data[i].LowHumidity.HasValue && data[i].LowHumidityDateTime.HasValue && data[i].LowHumidity.Value < lowHum[monthOffset].Value)
						{
							lowHum[monthOffset].Value = data[i].LowHumidity.Value;
							lowHum[monthOffset].Ts = data[i].LowHumidityDateTime.Value;
						}
						// hi humidity
						if (data[i].HighHumidity.HasValue && data[i].HighHumidityDateTime.HasValue && data[i].HighHumidity > highHum[monthOffset].Value)
						{
							highHum[monthOffset].Value = data[i].HighHumidity.Value;
							highHum[monthOffset].Ts = data[i].HighHumidityDateTime.Value;
						}

						// hi heat index
						if (data[i].HighHeatIndex.HasValue && data[i].HighHeatIndexDateTime.HasValue && data[i].HighHeatIndex.Value > highHeatInd[monthOffset].Value)
						{
							highHeatInd[monthOffset].Value = data[i].HighHeatIndex.Value;
							highHeatInd[monthOffset].Ts = data[i].HighHeatIndexDateTime.Value;
						}
						// hi app temp
						if (data[i].HighAppTemp.HasValue && data[i].HighAppTempDateTime.HasValue && data[i].HighAppTemp.Value > highAppTemp[monthOffset].Value)
						{
							highAppTemp[monthOffset].Value = data[i].HighAppTemp.Value;
							highAppTemp[monthOffset].Ts = data[i].HighAppTempDateTime.Value;
						}
						// lo app temp
						if (data[i].LowAppTemp.HasValue && data[i].LowAppTempDateTime.HasValue && data[i].LowAppTemp.Value < lowAppTemp[monthOffset].Value)
						{
							lowAppTemp[monthOffset].Value = data[i].LowAppTemp.Value;
							lowAppTemp[monthOffset].Ts = data[i].LowAppTempDateTime.Value;
						}

						// hi rain hour
						if (data[i].HighHourlyRain.HasValue && data[i].HighHourlyRainDateTime.HasValue && data[i].HighHourlyRain > highRainHour[monthOffset].Value)
						{
							highRainHour[monthOffset].Value = data[i].HighHourlyRain.Value;
							highRainHour[monthOffset].Ts = data[i].HighHourlyRainDateTime.Value;
						}

						// lo wind chill
						if (data[i].LowWindChill.HasValue && data[i].LowWindChillDateTime.HasValue && data[i].LowWindChill.Value < lowWindChill[monthOffset].Value)
						{
							lowWindChill[monthOffset].Value = data[i].LowWindChill.Value;
							lowWindChill[monthOffset].Ts = data[i].LowWindChillDateTime.Value;
						}

						// hi dewpt
						if (data[i].HighDewPoint.HasValue && data[i].HighDewPointDateTime.HasValue && data[i].HighDewPoint.Value > highDewPt[monthOffset].Value)
						{
							highDewPt[monthOffset].Value = data[i].HighDewPoint.Value;
							highDewPt[monthOffset].Ts = data[i].HighDewPointDateTime.Value;
						}
						// lo dewpt
						if (data[i].LowDewPoint.HasValue && data[i].LowDewPointDateTime.HasValue && data[i].LowDewPoint.Value < lowDewPt[monthOffset].Value)
						{
							lowDewPt[monthOffset].Value = data[i].LowDewPoint.Value;
							lowDewPt[monthOffset].Ts = data[i].LowDewPointDateTime.Value;
						}

						// hi feels like
						if (data[i].HighFeelsLike.HasValue && data[i].HighFeelsLikeDateTime.HasValue && data[i].HighFeelsLike.Value > highFeelsLike[monthOffset].Value)
						{
							highFeelsLike[monthOffset].Value = data[i].HighFeelsLike.Value;
							highFeelsLike[monthOffset].Ts = data[i].HighFeelsLikeDateTime.Value;
						}
						// lo feels like
						if (data[i].LowFeelsLike.HasValue && data[i].LowFeelsLikeDateTime.HasValue && data[i].LowFeelsLike.Value < lowFeelsLike[monthOffset].Value)
						{
							lowFeelsLike[monthOffset].Value = data[i].LowFeelsLike.Value;
							lowFeelsLike[monthOffset].Ts = data[i].LowFeelsLikeDateTime.Value;
						}

						// hi humidex
						if (data[i].HighHumidex.HasValue && data[i].HighHumidexDateTime.HasValue && data[i].HighHumidex.Value > highHumidex[monthOffset].Value)
						{
							highHumidex[monthOffset].Value = data[i].HighHumidex.Value;
							highHumidex[monthOffset].Ts = data[i].HighHumidexDateTime.Value;
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
				}
				else
				{
					cumulus.LogWarningMessage("Error failed to find day records");
				}

				var jsonObj = new JsonObject();

				for (var i = 0; i < 12; i++)
				{
					var m = i + 1;
					jsonObj.Add($"{m}-highTempValDayfile", highTemp[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highTempTimeDayfile", highTemp[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowTempValDayfile", lowTemp[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-lowTempTimeDayfile", lowTemp[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highDewPointValDayfile", highDewPt[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highDewPointTimeDayfile", highDewPt[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowDewPointValDayfile", lowDewPt[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-lowDewPointTimeDayfile", lowDewPt[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highApparentTempValDayfile", highAppTemp[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highApparentTempTimeDayfile", highAppTemp[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowApparentTempValDayfile", lowAppTemp[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-lowApparentTempTimeDayfile", lowAppTemp[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highFeelsLikeValDayfile", highFeelsLike[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highFeelsLikeTimeDayfile", highFeelsLike[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowFeelsLikeValDayfile", lowFeelsLike[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-lowFeelsLikeTimeDayfile", lowFeelsLike[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highHumidexValDayfile", highHumidex[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highHumidexTimeDayfile", highHumidex[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowWindChillValDayfile", lowWindChill[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-lowWindChillTimeDayfile", lowWindChill[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highHeatIndexValDayfile", highHeatInd[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highHeatIndexTimeDayfile", highHeatInd[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highMinTempValDayfile", highMinTemp[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highMinTempTimeDayfile", highMinTemp[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowMaxTempValDayfile", lowMaxTemp[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-lowMaxTempTimeDayfile", lowMaxTemp[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highDailyTempRangeValDayfile", highTempRange[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-highDailyTempRangeTimeDayfile", highTempRange[i].GetTsString(dateStampFormat));
					jsonObj.Add($"{m}-lowDailyTempRangeValDayfile", lowTempRange[i].GetValString(cumulus.TempFormat));
					jsonObj.Add($"{m}-lowDailyTempRangeTimeDayfile", lowTempRange[i].GetTsString(dateStampFormat));
					jsonObj.Add($"{m}-highHumidityValDayfile", highHum[i].GetValString(cumulus.HumFormat));
					jsonObj.Add($"{m}-highHumidityTimeDayfile", highHum[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowHumidityValDayfile", lowHum[i].GetValString(cumulus.HumFormat));
					jsonObj.Add($"{m}-lowHumidityTimeDayfile", lowHum[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highBarometerValDayfile", highBaro[i].GetValString(cumulus.PressFormat));
					jsonObj.Add($"{m}-highBarometerTimeDayfile", highBaro[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-lowBarometerValDayfile", lowBaro[i].GetValString(cumulus.PressFormat));
					jsonObj.Add($"{m}-lowBarometerTimeDayfile", lowBaro[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highGustValDayfile", highGust[i].GetValString(cumulus.WindFormat));
					jsonObj.Add($"{m}-highGustTimeDayfile", highGust[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highWindValDayfile", highWind[i].GetValString(cumulus.WindAvgFormat));
					jsonObj.Add($"{m}-highWindTimeDayfile", highWind[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highWindRunValDayfile", highWindRun[i].GetValString(cumulus.WindRunFormat));
					jsonObj.Add($"{m}-highWindRunTimeDayfile", highWindRun[i].GetTsString(dateStampFormat));
					jsonObj.Add($"{m}-highRainRateValDayfile", highRainRate[i].GetValString(cumulus.RainFormat));
					jsonObj.Add($"{m}-highRainRateTimeDayfile", highRainRate[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highHourlyRainValDayfile", highRainHour[i].GetValString(cumulus.RainFormat));
					jsonObj.Add($"{m}-highHourlyRainTimeDayfile", highRainHour[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highDailyRainValDayfile", highRainDay[i].GetValString(cumulus.RainFormat));
					jsonObj.Add($"{m}-highDailyRainTimeDayfile", highRainDay[i].GetTsString(dateStampFormat));
					jsonObj.Add($"{m}-highRain24hValDayfile", highRain24h[i].GetValString(cumulus.RainFormat));
					jsonObj.Add($"{m}-highRain24hTimeDayfile", highRain24h[i].GetTsString(timeStampFormat));
					jsonObj.Add($"{m}-highMonthlyRainValDayfile", highRainMonth[i].GetValString(cumulus.RainFormat));
					jsonObj.Add($"{m}-highMonthlyRainTimeDayfile", highRainMonth[i].GetTsString(monthFormat));
					jsonObj.Add($"{m}-longestDryPeriodValDayfile", dryPeriod[i].GetValString());
					jsonObj.Add($"{m}-longestDryPeriodTimeDayfile", dryPeriod[i].GetTsString(dateStampFormat));
					jsonObj.Add($"{m}-longestWetPeriodValDayfile", wetPeriod[i].GetValString());
					jsonObj.Add($"{m}-longestWetPeriodTimeDayfile", wetPeriod[i].GetTsString(dateStampFormat));
				}

				return jsonObj.ToJson();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error processing day records");
				return ex.Message;
			}
		}

		internal string GetMonthlyRecLogFile()
		{
			const string timeStampFormat = "g";
			const string dateStampFormat = "d";
			const string monthFormat = "MMM yyyy";

			var started = false;
			var lastentrydate = DateTime.MinValue;

			var isDryNow = false;
			var currentDryPeriod = 0;
			var currentWetPeriod = 0;
			var lastentrycounter = 0.0;

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

			// what do we deem to be too large a jump in the rainfall counter to be true? use 20 mm or 0.8 inches
			var counterJumpTooBig = cumulus.Units.Rain == 0 ? 20 : 0.8;

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
			var highRain24h = new LocalRec[12];
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
				highRain24h[i] = new LocalRec(true);
				highRainMonth[i] = new LocalRec(true);
				dryPeriod[i] = new LocalRec(true);
				wetPeriod[i] = new LocalRec(true);
			}


			var thisDateDry = DateTime.MinValue;
			var thisDateWet = DateTime.MinValue;

			var currentDay = DateTime.MinValue;
			var dayHighTemp = new LocalRec(true);
			var dayLowTemp = new LocalRec(false);
			double dayWindRun = 0;
			double dayRain = 0;

			var monthOffset = 0;
			var monthlyRain = 0.0;
			var totalRainfall = 0.0;

			var hourRainLog = new Queue<LastHourRainLog>();
			var rain24hLog = new Queue<LastHourRainLog>();

			var watch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				var rows = station.Database.Query<IntervalData>("select * from IntervalData order by timestamp");
				foreach (var row in rows)
				{
					// We need to work in meteo dates not clock dates for day hi/lows
					var metoDate = row.StationTime.AddHours(cumulus.GetHourInc());
					var rowTime = row.StationTime;
					monthOffset = metoDate.Month - 1;

					if (!started)
					{
						lastentrydate = row.StationTime;
						currentDay = metoDate;
						started = true;
					}

					// low chill
					if (row.WindChill.HasValue && row.WindChill.Value < lowWindChill[monthOffset].Value)
					{
						lowWindChill[monthOffset].Value = row.WindChill.Value;
						lowWindChill[monthOffset].Ts = rowTime;
					}
					// hi heat
					if (row.HeatIndex.HasValue && row.HeatIndex.Value > highHeatInd[monthOffset].Value)
					{
						highHeatInd[monthOffset].Value = row.HeatIndex.Value;
						highHeatInd[monthOffset].Ts = rowTime;
					}

					if (row.Apparent.HasValue)
					{
						// hi appt
						if (row.Apparent.Value > highAppTemp[monthOffset].Value)
						{
							highAppTemp[monthOffset].Value = row.Apparent.Value;
							highAppTemp[monthOffset].Ts = rowTime;
						}
						// lo appt
						if (row.Apparent.Value < lowAppTemp[monthOffset].Value)
						{
							lowAppTemp[monthOffset].Value = row.Apparent.Value;
							lowAppTemp[monthOffset].Ts = rowTime;
						}
					}

					if (row.FeelsLike.HasValue)
					{
						// hi feels like
						if (row.FeelsLike.Value > highFeelsLike[monthOffset].Value)
						{
							highFeelsLike[monthOffset].Value = row.FeelsLike.Value;
							highFeelsLike[monthOffset].Ts = rowTime;
						}
						// lo feels like
						if (row.FeelsLike.Value < lowFeelsLike[monthOffset].Value)
						{
							lowFeelsLike[monthOffset].Value = row.FeelsLike.Value;
							lowFeelsLike[monthOffset].Ts = rowTime;
						}
					}

					// hi humidex
					if (row.Humidex.HasValue && row.Humidex.Value > highHumidex[monthOffset].Value)
					{
						highHumidex[monthOffset].Value = row.Humidex.Value;
						highHumidex[monthOffset].Ts = rowTime;
					}

					if (row.Temp.HasValue)
					{
						// hi temp
						if (row.Temp.Value > highTemp[monthOffset].Value)
						{
							highTemp[monthOffset].Value = row.Temp.Value;
							highTemp[monthOffset].Ts = rowTime;
						}
						if (row.Temp.Value > dayHighTemp.Value)
						{
							dayHighTemp.Value = row.Temp.Value;
							dayHighTemp.Ts = rowTime;
						}
						// lo temp
						if (row.Temp.Value < lowTemp[monthOffset].Value)
						{
							lowTemp[monthOffset].Value = row.Temp.Value;
							lowTemp[monthOffset].Ts = rowTime;
						}
						if (row.Temp.Value < dayLowTemp.Value)
						{
							dayLowTemp.Value = row.Temp.Value;
							dayLowTemp.Ts = rowTime;
						}

					}
					if (row.DewPoint.HasValue)
					{
						// hi dewpoint
						if (row.DewPoint.Value > highDewPt[monthOffset].Value)
						{
							highDewPt[monthOffset].Value = row.DewPoint.Value;
							highDewPt[monthOffset].Ts = rowTime;
						}
						// low dewpoint
						if (row.DewPoint.Value < lowDewPt[monthOffset].Value)
						{
							lowDewPt[monthOffset].Value = row.DewPoint.Value;
							lowDewPt[monthOffset].Ts = rowTime;
						}
					}
					if (row.Humidity.HasValue)
					{
						// hi hum
						if (row.Humidity.Value > highHum[monthOffset].Value)
						{
							highHum[monthOffset].Value = row.Humidity.Value;
							highHum[monthOffset].Ts = rowTime;
						}
						// lo hum
						if (row.Humidity.Value < lowHum[monthOffset].Value)
						{
							lowHum[monthOffset].Value = row.Humidity.Value;
							lowHum[monthOffset].Ts = rowTime;
						}
					}
					if (row.Pressure.HasValue)
					{
						// hi baro
						if (row.Pressure.Value > highBaro[monthOffset].Value)
						{
							highBaro[monthOffset].Value = row.Pressure.Value;
							highBaro[monthOffset].Ts = rowTime;
						}
						// lo baro
						if (row.Pressure.Value < lowBaro[monthOffset].Value)
						{
							lowBaro[monthOffset].Value = row.Pressure.Value;
							lowBaro[monthOffset].Ts = rowTime;
						}
					}
					// hi gust
					if (row.WindGust10m.HasValue && row.WindGust10m.Value > highGust[monthOffset].Value)
					{
						highGust[monthOffset].Value = row.WindGust10m.Value;
						highGust[monthOffset].Ts = rowTime;
					}
					// hi wind
					if (row.WindAvg.HasValue && row.WindAvg.Value > highWind[monthOffset].Value)
					{
						highWind[monthOffset].Value = row.WindAvg.Value;
						highWind[monthOffset].Ts = rowTime;
					}
					// hi rain rate
					if (row.RainRate.HasValue && row.RainRate.Value > highRainRate[monthOffset].Value)
					{
						highRainRate[monthOffset].Value = row.RainRate.Value;
						highRainRate[monthOffset].Ts = rowTime;
					}

					// daily wind run
					dayWindRun += row.StationTime.Subtract(lastentrydate).TotalHours * row.WindAvg ?? 0;

					if (dayWindRun > highWindRun[monthOffset].Value)
					{
						highWindRun[monthOffset].Value = dayWindRun;
						highWindRun[monthOffset].Ts = currentDay;
					}

					// new meteo day
					if (currentDay.Date != metoDate.Date)
					{
						var lastEntryMonthOffset = currentDay.Month - 1;
						if (dayHighTemp.Value != Cumulus.DefaultHiVal && dayHighTemp.Value < lowMaxTemp[lastEntryMonthOffset].Value)
						{
							lowMaxTemp[lastEntryMonthOffset].Value = dayHighTemp.Value;
							lowMaxTemp[lastEntryMonthOffset].Ts = dayHighTemp.Ts;
						}
						if (dayLowTemp.Value != Cumulus.DefaultLoVal && dayLowTemp.Value > highMinTemp[lastEntryMonthOffset].Value)
						{
							highMinTemp[lastEntryMonthOffset].Value = dayLowTemp.Value;
							highMinTemp[lastEntryMonthOffset].Ts = dayLowTemp.Ts;
						}
						if (dayHighTemp.Value != Cumulus.DefaultHiVal && dayLowTemp.Value != Cumulus.DefaultLoVal && dayHighTemp.Value - dayLowTemp.Value > highTempRange[lastEntryMonthOffset].Value)
						{
							highTempRange[lastEntryMonthOffset].Value = dayHighTemp.Value - dayLowTemp.Value;
							highTempRange[lastEntryMonthOffset].Ts = currentDay;
						}
						if (dayHighTemp.Value != Cumulus.DefaultHiVal && dayLowTemp.Value != Cumulus.DefaultLoVal && dayHighTemp.Value - dayLowTemp.Value < lowTempRange[lastEntryMonthOffset].Value)
						{
							lowTempRange[lastEntryMonthOffset].Value = dayHighTemp.Value - dayLowTemp.Value;
							lowTempRange[lastEntryMonthOffset].Ts = currentDay;
						}

						// logging format changed on with C1 v1.9.3 b1055 in Dec 2012
						// before that date the 00:00 log entry contained the rain total for the day before and the next log entry was reset to zero
						// after that build the total was reset to zero in the entry
						// messy!
						// no final rainfall entry after this date (approx). The best we can do is add in the increase in rain counter during this period
						var rollovertime = new TimeSpan(-cumulus.GetHourInc(), 0, 0);
						if (row.RainToday > 0 && row.StationTime.TimeOfDay == rollovertime)
						{
							dayRain = row.RainToday ?? 0;
						}
						else if ((row.RainCounter - lastentrycounter > 0) && (row.RainCounter - lastentrycounter < counterJumpTooBig))
						{
							dayRain += ((double)row.RainCounter - lastentrycounter) * cumulus.Calib.Rain.Mult;
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

						if (row.Temp.HasValue)
						{
							dayHighTemp.Value = row.Temp.Value;
							dayLowTemp.Value = row.Temp.Value;
						}
						dayRain = 0;
					}
					else
					{
						dayRain = row.RainToday ?? 0;
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

					dayWindRun += row.StationTime.Subtract(lastentrydate).TotalHours * row.WindAvg ?? 0;

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

					AddLastHoursRainEntry(rowTime, totalRainfall + dayRain, ref hourRainLog, ref rain24hLog);

					var rainThisHour = hourRainLog.Last().Raincounter - hourRainLog.Peek().Raincounter;
					if (rainThisHour > highRainHour[monthOffset].Value)
					{
						highRainHour[monthOffset].Value = rainThisHour;
						highRainHour[monthOffset].Ts = rowTime;
					}

					var rain24h = rain24hLog.Last().Raincounter - rain24hLog.Peek().Raincounter;
					if (rain24h > highRain24h[monthOffset].Value)
					{
						highRain24h[monthOffset].Value = rain24h;
						highRain24h[monthOffset].Ts = rowTime;
					}

					// new meteo day, part 2
					if (currentDay.Date != metoDate.Date)
					{
						currentDay = metoDate;
						dayHighTemp.Value = row.Temp ?? Cumulus.DefaultHiVal;
						dayHighTemp.Ts = rowTime;
						dayLowTemp.Value = row.Temp ?? Cumulus.DefaultLoVal;
						dayLowTemp.Ts = rowTime;
						dayWindRun = 0;
						totalRainfall += dayRain;
					}

					lastentrydate = rowTime;
					lastentrycounter = row.RainCounter ?? 0;
				}

				// for the final entry - check the monthly rain
				if (rows.Count > 0 && monthlyRain > highRainMonth[monthOffset].Value)
				{
					highRainMonth[monthOffset].Value = monthlyRain;
					highRainMonth[monthOffset].Ts = currentDay;
				}

			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "GetMonthlyRecLogFile: Error processing log data");
				return ex.Message;
			}

			cumulus.LogDebugMessage("GetMonthlyRecLogFile: Finished processing log data");

			hourRainLog.Clear();
			rain24hLog.Clear();

			var jsonObj = new JsonObject();

			for (var i = 0; i < 12; i++)
			{
				var m = i + 1;
				jsonObj.Add($"{m}-highTempValLogfile", highTemp[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highTempTimeLogfile", highTemp[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowTempValLogfile", lowTemp[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowTempTimeLogfile", lowTemp[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highDewPointValLogfile", highDewPt[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highDewPointTimeLogfile", highDewPt[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowDewPointValLogfile", lowDewPt[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowDewPointTimeLogfile", lowDewPt[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highApparentTempValLogfile", highAppTemp[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highApparentTempTimeLogfile", highAppTemp[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowApparentTempValLogfile", lowAppTemp[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowApparentTempTimeLogfile", lowAppTemp[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highFeelsLikeValLogfile", highFeelsLike[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highFeelsLikeTimeLogfile", highFeelsLike[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowFeelsLikeValLogfile", lowFeelsLike[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowFeelsLikeTimeLogfile", lowFeelsLike[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highHumidexValLogfile", highHumidex[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highHumidexTimeLogfile", highHumidex[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowWindChillValLogfile", lowWindChill[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowWindChillTimeLogfile", lowWindChill[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highHeatIndexValLogfile", highHeatInd[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highHeatIndexTimeLogfile", highHeatInd[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highMinTempValLogfile", highMinTemp[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highMinTempTimeLogfile", highMinTemp[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowMaxTempValLogfile", lowMaxTemp[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowMaxTempTimeLogfile", lowMaxTemp[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highDailyTempRangeValLogfile", highTempRange[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-highDailyTempRangeTimeLogfile", highTempRange[i].GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-lowDailyTempRangeValLogfile", lowTempRange[i].GetValString(cumulus.TempFormat));
				jsonObj.Add($"{m}-lowDailyTempRangeTimeLogfile", lowTempRange[i].GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-highHumidityValLogfile", highHum[i].GetValString(cumulus.HumFormat));
				jsonObj.Add($"{m}-highHumidityTimeLogfile", highHum[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowHumidityValLogfile", lowHum[i].GetValString(cumulus.HumFormat));
				jsonObj.Add($"{m}-lowHumidityTimeLogfile", lowHum[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highBarometerValLogfile", highBaro[i].GetValString(cumulus.PressFormat));
				jsonObj.Add($"{m}-highBarometerTimeLogfile", highBaro[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-lowBarometerValLogfile", lowBaro[i].GetValString(cumulus.PressFormat));
				jsonObj.Add($"{m}-lowBarometerTimeLogfile", lowBaro[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highGustValLogfile", highGust[i].GetValString(cumulus.WindFormat));
				jsonObj.Add($"{m}-highGustTimeLogfile", highGust[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highWindValLogfile", highWind[i].GetValString(cumulus.WindAvgFormat));
				jsonObj.Add($"{m}-highWindTimeLogfile", highWind[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highWindRunValLogfile", highWindRun[i].GetValString(cumulus.WindRunFormat));
				jsonObj.Add($"{m}-highWindRunTimeLogfile", highWindRun[i].GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-highRainRateValLogfile", highRainRate[i].GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highRainRateTimeLogfile", highRainRate[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highHourlyRainValLogfile", highRainHour[i].GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highHourlyRainTimeLogfile", highRainHour[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highDailyRainValLogfile", highRainDay[i].GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highDailyRainTimeLogfile", highRainDay[i].GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-highRain24hValLogfile", highRain24h[i].GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highRain24hTimeLogfile", highRain24h[i].GetTsString(timeStampFormat));
				jsonObj.Add($"{m}-highMonthlyRainValLogfile", highRainMonth[i].GetValString(cumulus.RainFormat));
				jsonObj.Add($"{m}-highMonthlyRainTimeLogfile", highRainMonth[i].GetTsString(monthFormat));
				jsonObj.Add($"{m}-longestDryPeriodValLogfile", dryPeriod[i].GetValString());
				jsonObj.Add($"{m}-longestDryPeriodTimeLogfile", dryPeriod[i].GetTsString(dateStampFormat));
				jsonObj.Add($"{m}-longestWetPeriodValLogfile", wetPeriod[i].GetValString());
				jsonObj.Add($"{m}-longestWetPeriodTimeLogfile", wetPeriod[i].GetTsString(dateStampFormat));
			}

			watch.Stop();
			var elapsed = watch.ElapsedMilliseconds;
			cumulus.LogDebugMessage($"Monthly recs editor Logfiles load = {elapsed} ms");

			return jsonObj.ToJson();
		}

		internal string GetThisMonthRecData()
		{
			const string timeStampFormat = "g";
			const string dateStampFormat = "d";

			return new JsonObject
			{
				// Records - Temperature
				["highTempVal"] = station.ThisMonth.HighTemp.GetValString(cumulus.TempFormat),
				["highTempTime"] = station.ThisMonth.HighTemp.GetTsString(timeStampFormat),
				["lowTempVal"] = station.ThisMonth.LowTemp.GetValString(cumulus.TempFormat),
				["lowTempTime"] = station.ThisMonth.LowTemp.GetTsString(timeStampFormat),
				["highDewPointVal"] = station.ThisMonth.HighDewPoint.GetValString(cumulus.TempFormat),
				["highDewPointTime"] = station.ThisMonth.HighDewPoint.GetTsString(timeStampFormat),
				["lowDewPointVal"] = station.ThisMonth.LowDewPoint.GetValString(cumulus.TempFormat),
				["lowDewPointTime"] = station.ThisMonth.LowDewPoint.GetTsString(timeStampFormat),
				["highApparentTempVal"] = station.ThisMonth.HighAppTemp.GetValString(cumulus.TempFormat),
				["highApparentTempTime"] = station.ThisMonth.HighAppTemp.GetTsString(timeStampFormat),
				["lowApparentTempVal"] = station.ThisMonth.LowAppTemp.GetValString(cumulus.TempFormat),
				["lowApparentTempTime"] = station.ThisMonth.LowAppTemp.GetTsString(timeStampFormat),
				["highFeelsLikeVal"] = station.ThisMonth.HighFeelsLike.GetValString(cumulus.TempFormat),
				["highFeelsLikeTime"] = station.ThisMonth.HighFeelsLike.GetTsString(timeStampFormat),
				["lowFeelsLikeVal"] = station.ThisMonth.LowFeelsLike.GetValString(cumulus.TempFormat),
				["lowFeelsLikeTime"] = station.ThisMonth.LowFeelsLike.GetTsString(timeStampFormat),
				["highHumidexVal"] = station.ThisMonth.HighHumidex.GetValString(cumulus.TempFormat),
				["highHumidexTime"] = station.ThisMonth.HighHumidex.GetTsString(timeStampFormat),
				["lowWindChillVal"] = station.ThisMonth.LowChill.GetValString(cumulus.TempFormat),
				["lowWindChillTime"] = station.ThisMonth.LowChill.GetTsString(timeStampFormat),
				["highHeatIndexVal"] = station.ThisMonth.HighHeatIndex.GetValString(cumulus.TempFormat),
				["highHeatIndexTime"] = station.ThisMonth.HighHeatIndex.GetTsString(timeStampFormat),
				["highMinTempVal"] = station.ThisMonth.HighMinTemp.GetValString(cumulus.TempFormat),
				["highMinTempTime"] = station.ThisMonth.HighMinTemp.GetTsString(timeStampFormat),
				["lowMaxTempVal"] = station.ThisMonth.LowMaxTemp.GetValString(cumulus.TempFormat),
				["lowMaxTempTime"] = station.ThisMonth.LowMaxTemp.GetTsString(timeStampFormat),
				["highDailyTempRangeVal"] = station.ThisMonth.HighDailyTempRange.GetValString(cumulus.TempFormat),
				["highDailyTempRangeTime"] = station.ThisMonth.HighDailyTempRange.GetTsString(dateStampFormat),
				["lowDailyTempRangeVal"] = station.ThisMonth.LowDailyTempRange.GetValString(cumulus.TempFormat),
				["lowDailyTempRangeTime"] = station.ThisMonth.LowDailyTempRange.GetTsString(dateStampFormat),
				// Records - Humidity
				["highHumidityVal"] = station.ThisMonth.HighHumidity.GetValString(cumulus.HumFormat),
				["highHumidityTime"] = station.ThisMonth.HighHumidity.GetTsString(timeStampFormat),
				["lowHumidityVal"] = station.ThisMonth.LowHumidity.GetValString(cumulus.HumFormat),
				["lowHumidityTime"] = station.ThisMonth.LowHumidity.GetTsString(timeStampFormat),
				// Records - Pressure
				["highBarometerVal"] = station.ThisMonth.HighPress.GetValString(cumulus.PressFormat),
				["highBarometerTime"] = station.ThisMonth.HighPress.GetTsString(timeStampFormat),
				["lowBarometerVal"] = station.ThisMonth.LowPress.GetValString(cumulus.PressFormat),
				["lowBarometerTime"] = station.ThisMonth.LowPress.GetTsString(timeStampFormat),
				// Records - Wind
				["highGustVal"] = station.ThisMonth.HighGust.GetValString(cumulus.WindFormat),
				["highGustTime"] = station.ThisMonth.HighGust.GetTsString(timeStampFormat),
				["highWindVal"] = station.ThisMonth.HighWind.GetValString(cumulus.WindAvgFormat),
				["highWindTime"] = station.ThisMonth.HighWind.GetTsString(timeStampFormat),
				["highWindRunVal"] = station.ThisMonth.HighWindRun.GetValString(cumulus.WindRunFormat),
				["highWindRunTime"] = station.ThisMonth.HighWindRun.GetTsString(dateStampFormat),
				// Records - Rain
				["highRainRateVal"] = station.ThisMonth.HighRainRate.GetValString(cumulus.RainFormat),
				["highRainRateTime"] = station.ThisMonth.HighRainRate.GetTsString(timeStampFormat),
				["highHourlyRainVal"] = station.ThisMonth.HourlyRain.GetValString(cumulus.RainFormat),
				["highHourlyRainTime"] = station.ThisMonth.HourlyRain.GetTsString(timeStampFormat),
				["highDailyRainVal"] = station.ThisMonth.DailyRain.GetValString(cumulus.RainFormat),
				["highDailyRainTime"] = station.ThisMonth.DailyRain.GetTsString(dateStampFormat),
				["highRain24hVal"] = station.ThisMonth.HighRain24Hours.GetValString(cumulus.RainFormat),
				["highRain24hTime"] = station.ThisMonth.HighRain24Hours.GetTsString(timeStampFormat),
				["longestDryPeriodVal"] = station.ThisMonth.LongestDryPeriod.GetValString("F0"),
				["longestDryPeriodTime"] = station.ThisMonth.LongestDryPeriod.GetTsString(dateStampFormat),
				["longestWetPeriodVal"] = station.ThisMonth.LongestWetPeriod.GetValString("F0"),
				["longestWetPeriodTime"] = station.ThisMonth.LongestWetPeriod.GetTsString(dateStampFormat)
			}.ToJson();
		}

		internal string EditThisMonthRecs(IHttpContext context)
		{
			var request = context.Request;
			string text;

			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = Uri.UnescapeDataString(reader.ReadToEnd());
			}
			// Eg name=highTempVal&value=134.6&pk=1                   - From direct editing
			// Eg name=highTempTime&value="04/11/2023 06:58"&pk=1     - From direct editing
			// Eg name=highTemp&value=134.6&time="04/11/2023 06:58"   - From recorder "clicker"

			var newData = text.Split('&');
			var field = newData[0].Split('=')[1];

			var txtValue = newData[1].Split('=')[1];
			var value = double.Parse(txtValue);

			var txtTime = newData[2].Split('=')[1];
			var time = localeDateTimeStrToDate(txtTime);

			try
			{
				switch (field)
				{
					case "highTemp":
						station.ThisMonth.HighTemp.Val = value;
						station.ThisMonth.HighTemp.Ts = time;
						break;
					case "lowTemp":
						station.ThisMonth.LowTemp.Val = value;
						station.ThisMonth.LowTemp.Ts = time;
						break;
					case "highDewPoint":
						station.ThisMonth.HighDewPoint.Val = value;
						station.ThisMonth.HighDewPoint.Ts = time;
						break;
					case "lowDewPoint":
						station.ThisMonth.LowDewPoint.Val = value;
						station.ThisMonth.LowDewPoint.Ts = time;
						break;
					case "highApparentTemp":
						station.ThisMonth.HighAppTemp.Val = value;
						station.ThisMonth.HighAppTemp.Ts = time;
						break;
					case "lowApparentTemp":
						station.ThisMonth.LowAppTemp.Val = value;
						station.ThisMonth.LowAppTemp.Ts = time;
						break;
					case "highFeelsLike":
						station.ThisMonth.HighFeelsLike.Val = value;
						station.ThisMonth.HighFeelsLike.Ts = time;
						break;
					case "lowFeelsLike":
						station.ThisMonth.LowFeelsLike.Val = value;
						station.ThisMonth.LowFeelsLike.Ts = time;
						break;
					case "highHumidex":
						station.ThisMonth.HighHumidex.Val = value;
						station.ThisMonth.HighHumidex.Ts = time;
						break;
					case "lowWindChill":
						station.ThisMonth.LowChill.Val = value;
						station.ThisMonth.LowChill.Ts = time;
						break;
					case "highHeatIndex":
						station.ThisMonth.HighHeatIndex.Val = value;
						station.ThisMonth.HighHeatIndex.Ts = time;
						break;
					case "highMinTemp":
						station.ThisMonth.HighMinTemp.Val = value;
						station.ThisMonth.HighMinTemp.Ts = time;
						break;
					case "lowMaxTemp":
						station.ThisMonth.LowMaxTemp.Val = value;
						station.ThisMonth.LowMaxTemp.Ts = time;
						break;
					case "highDailyTempRange":
						station.ThisMonth.HighDailyTempRange.Val = value;
						station.ThisMonth.HighDailyTempRange.Ts = time;
						break;
					case "lowDailyTempRange":
						station.ThisMonth.LowDailyTempRange.Val = value;
						station.ThisMonth.LowDailyTempRange.Ts = time;
						break;
					case "highHumidity":
						station.ThisMonth.HighHumidity.Val = int.Parse(txtValue);
						station.ThisMonth.HighHumidity.Ts = time;
						break;
					case "lowHumidity":
						station.ThisMonth.LowHumidity.Val = int.Parse(txtValue);
						station.ThisMonth.LowHumidity.Ts = time;
						break;
					case "highBarometer":
						station.ThisMonth.HighPress.Val = value;
						station.ThisMonth.HighPress.Ts = time;
						break;
					case "lowBarometer":
						station.ThisMonth.LowPress.Val = value;
						station.ThisMonth.LowPress.Ts = time;
						break;
					case "highGust":
						station.ThisMonth.HighGust.Val = value;
						station.ThisMonth.HighGust.Ts = time;
						break;
					case "highWind":
						station.ThisMonth.HighWind.Val = value;
						station.ThisMonth.HighWind.Ts = time;
						break;
					case "highWindRun":
						station.ThisMonth.HighWindRun.Val = value;
						station.ThisMonth.HighWindRun.Ts = time;
						break;
					case "highRainRate":
						station.ThisMonth.HighRainRate.Val = value;
						station.ThisMonth.HighRainRate.Ts = time;
						break;
					case "highHourlyRain":
						station.ThisMonth.HourlyRain.Val = value;
						station.ThisMonth.HourlyRain.Ts = time;
						break;
					case "highDailyRain":
						station.ThisMonth.DailyRain.Val = value;
						station.ThisMonth.DailyRain.Ts = time;
						break;
					case "highRain24h":
						station.ThisMonth.HighRain24Hours.Val = value;
						station.ThisMonth.HighRain24Hours.Ts = time;
						break;
					case "longestDryPeriod":
						station.ThisMonth.LongestDryPeriod.Val = int.Parse(txtValue);
						station.ThisMonth.LongestDryPeriod.Ts = time;
						break;
					case "longestWetPeriod":
						station.ThisMonth.LongestWetPeriod.Val = int.Parse(txtValue);
						station.ThisMonth.LongestWetPeriod.Ts = time;
						break;
					default:
						return "Data index not recognised";
				}
				station.WriteMonthIniFile();
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
			return "Success";
		}

		internal string GetThisYearRecData()
		{
			const string timeStampFormat = "g";
			const string dateStampFormat = "d";
			const string monthFormat = "MMM yyyy";

			return new JsonObject
			{
				["highTempVal"] = station.ThisYear.HighTemp.GetValString(cumulus.TempFormat),
				["highTempTime"] = station.ThisYear.HighTemp.GetTsString(timeStampFormat),
				["lowTempVal"] = station.ThisYear.LowTemp.GetValString(cumulus.TempFormat),
				["lowTempTime"] = station.ThisYear.LowTemp.GetTsString(timeStampFormat),
				["highDewPointVal"] = station.ThisYear.HighDewPoint.GetValString(cumulus.TempFormat),
				["highDewPointTime"] = station.ThisYear.HighDewPoint.GetTsString(timeStampFormat),
				["lowDewPointVal"] = station.ThisYear.LowDewPoint.GetValString(cumulus.TempFormat),
				["lowDewPointTime"] = station.ThisYear.LowDewPoint.GetTsString(timeStampFormat),
				["highApparentTempVal"] = station.ThisYear.HighAppTemp.GetValString(cumulus.TempFormat),
				["highApparentTempTime"] = station.ThisYear.HighAppTemp.GetTsString(timeStampFormat),
				["lowApparentTempVal"] = station.ThisYear.LowAppTemp.GetValString(cumulus.TempFormat),
				["lowApparentTempTime"] = station.ThisYear.LowAppTemp.GetTsString(timeStampFormat),
				["highFeelsLikeVal"] = station.ThisYear.HighFeelsLike.GetValString(cumulus.TempFormat),
				["highFeelsLikeTime"] = station.ThisYear.HighFeelsLike.GetTsString(timeStampFormat),
				["lowFeelsLikeVal"] = station.ThisYear.LowFeelsLike.GetValString(cumulus.TempFormat),
				["lowFeelsLikeTime"] = station.ThisYear.LowFeelsLike.GetTsString(timeStampFormat),
				["highHumidexVal"] = station.ThisYear.HighHumidex.GetValString(cumulus.TempFormat),
				["highHumidexTime"] = station.ThisYear.HighHumidex.GetTsString(timeStampFormat),
				["lowWindChillVal"] = station.ThisYear.LowChill.GetValString(cumulus.TempFormat),
				["lowWindChillTime"] = station.ThisYear.LowChill.GetTsString(timeStampFormat),
				["highHeatIndexVal"] = station.ThisYear.HighHeatIndex.GetValString(cumulus.TempFormat),
				["highHeatIndexTime"] = station.ThisYear.HighHeatIndex.GetTsString(timeStampFormat),
				["highMinTempVal"] = station.ThisYear.HighMinTemp.GetValString(cumulus.TempFormat),
				["highMinTempTime"] = station.ThisYear.HighMinTemp.GetTsString(timeStampFormat),
				["lowMaxTempVal"] = station.ThisYear.LowMaxTemp.GetValString(cumulus.TempFormat),
				["lowMaxTempTime"] = station.ThisYear.LowMaxTemp.GetTsString(timeStampFormat),
				["highDailyTempRangeVal"] = station.ThisYear.HighDailyTempRange.GetValString(cumulus.TempFormat),
				["highDailyTempRangeTime"] = station.ThisYear.HighDailyTempRange.GetTsString(dateStampFormat),
				["lowDailyTempRangeVal"] = station.ThisYear.LowDailyTempRange.GetValString(cumulus.TempFormat),
				["lowDailyTempRangeTime"] = station.ThisYear.LowDailyTempRange.GetTsString(dateStampFormat),
				// Records - Humidity
				["highHumidityVal"] = station.ThisYear.HighHumidity.GetValString(cumulus.HumFormat),
				["highHumidityTime"] = station.ThisYear.HighHumidity.GetTsString(timeStampFormat),
				["lowHumidityVal"] = station.ThisYear.LowHumidity.GetValString(cumulus.HumFormat),
				["lowHumidityTime"] = station.ThisYear.LowHumidity.GetTsString(timeStampFormat),
				// Records - Pressure
				["highBarometerVal"] = station.ThisYear.HighPress.GetValString(cumulus.PressFormat),
				["highBarometerTime"] = station.ThisYear.HighPress.GetTsString(timeStampFormat),
				["lowBarometerVal"] = station.ThisYear.LowPress.GetValString(cumulus.PressFormat),
				["lowBarometerTime"] = station.ThisYear.LowPress.GetTsString(timeStampFormat),
				// Records - Wind
				["highGustVal"] = station.ThisYear.HighGust.GetValString(cumulus.WindFormat),
				["highGustTime"] = station.ThisYear.HighGust.GetTsString(timeStampFormat),
				["highWindVal"] = station.ThisYear.HighWind.GetValString(cumulus.WindAvgFormat),
				["highWindTime"] = station.ThisYear.HighWind.GetTsString(timeStampFormat),
				["highWindRunVal"] = station.ThisYear.HighWindRun.GetValString(cumulus.WindRunFormat),
				["highWindRunTime"] = station.ThisYear.HighWindRun.GetTsString(dateStampFormat),
				// Records - Rain
				["highRainRateVal"] = station.ThisYear.HighRainRate.GetValString(cumulus.RainFormat),
				["highRainRateTime"] = station.ThisYear.HighRainRate.GetTsString(timeStampFormat),
				["highHourlyRainVal"] = station.ThisYear.HourlyRain.GetValString(cumulus.RainFormat),
				["highHourlyRainTime"] = station.ThisYear.HourlyRain.GetTsString(timeStampFormat),
				["highDailyRainVal"] = station.ThisYear.DailyRain.GetValString(cumulus.RainFormat),
				["highDailyRainTime"] = station.ThisYear.DailyRain.GetTsString(dateStampFormat),
				["highRain24hVal"] = station.ThisYear.HighRain24Hours.GetValString(cumulus.RainFormat),
				["highRain24hTime"] = station.ThisYear.HighRain24Hours.GetTsString(timeStampFormat),
				["highMonthlyRainVal"] = station.ThisYear.MonthlyRain.GetValString(cumulus.RainFormat),
				["highMonthlyRainTime"] = station.ThisYear.MonthlyRain.GetTsString(monthFormat),
				["longestDryPeriodVal"] = station.ThisYear.LongestDryPeriod.GetValString("F0"),
				["longestDryPeriodTime"] = station.ThisYear.LongestDryPeriod.GetTsString(dateStampFormat),
				["longestWetPeriodVal"] = station.ThisYear.LongestWetPeriod.GetValString("F0"),
				["longestWetPeriodTime"] = station.ThisYear.LongestWetPeriod.GetTsString(dateStampFormat)
			}.ToJson();
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

			var txtValue = newData[1].Split('=')[1];
			var value = double.Parse(txtValue);

			var txtTime = newData[2].Split('=')[1];
			var time = localeDateTimeStrToDate(txtTime);

			try
			{
				switch (field)
				{
					case "highTemp":
						station.ThisYear.HighTemp.Val = value;
						station.ThisYear.HighTemp.Ts = time;
						break;
					case "lowTemp":
						station.ThisYear.LowTemp.Val = value;
						station.ThisYear.LowTemp.Ts = time;
						break;
					case "highDewPoint":
						station.ThisYear.HighDewPoint.Val = value;
						station.ThisYear.HighDewPoint.Ts = time;
						break;
					case "lowDewPoint":
						station.ThisYear.LowDewPoint.Val = value;
						station.ThisYear.LowDewPoint.Ts = time;
						break;
					case "highApparentTemp":
						station.ThisYear.HighAppTemp.Val = value;
						station.ThisYear.HighAppTemp.Ts = time;
						break;
					case "lowApparentTemp":
						station.ThisYear.LowAppTemp.Val = value;
						station.ThisYear.LowAppTemp.Ts = time;
						break;
					case "highFeelsLike":
						station.ThisYear.HighFeelsLike.Val = value;
						station.ThisYear.HighFeelsLike.Ts = time;
						break;
					case "lowFeelsLike":
						station.ThisYear.LowFeelsLike.Val = value;
						station.ThisYear.LowFeelsLike.Ts = time;
						break;
					case "highHumidex":
						station.ThisYear.HighHumidex.Val = value;
						station.ThisYear.HighHumidex.Ts = time;
						break;
					case "lowWindChill":
						station.ThisYear.LowChill.Val = value;
						station.ThisYear.LowChill.Ts = time;
						break;
					case "highHeatIndex":
						station.ThisYear.HighHeatIndex.Val = value;
						station.ThisYear.HighHeatIndex.Ts = time;
						break;
					case "highMinTemp":
						station.ThisYear.HighMinTemp.Val = value;
						station.ThisYear.HighMinTemp.Ts = time;
						break;
					case "lowMaxTemp":
						station.ThisYear.LowMaxTemp.Val = value;
						station.ThisYear.LowMaxTemp.Ts = time;
						break;
					case "highDailyTempRange":
						station.ThisYear.HighDailyTempRange.Val = value;
						station.ThisYear.HighDailyTempRange.Ts = time;
						break;
					case "lowDailyTempRange":
						station.ThisYear.LowDailyTempRange.Val = value;
						station.ThisYear.LowDailyTempRange.Ts = time;
						break;
					case "highHumidity":
						station.ThisYear.HighHumidity.Val = int.Parse(txtValue);
						station.ThisYear.HighHumidity.Ts = time;
						break;
					case "lowHumidity":
						station.ThisYear.LowHumidity.Val = int.Parse(txtValue);
						station.ThisYear.LowHumidity.Ts = time;
						break;
					case "highBarometer":
						station.ThisYear.HighPress.Val = value;
						station.ThisYear.HighPress.Ts = time;
						break;
					case "lowBarometer":
						station.ThisYear.LowPress.Val = value;
						station.ThisYear.LowPress.Ts = time;
						break;
					case "highGust":
						station.ThisYear.HighGust.Val = value;
						station.ThisYear.HighGust.Ts = time;
						break;
					case "highWind":
						station.ThisYear.HighWind.Val = value;
						station.ThisYear.HighWind.Ts = time;
						break;
					case "highWindRun":
						station.ThisYear.HighWindRun.Val = value;
						station.ThisYear.HighWindRun.Ts = time;
						break;
					case "highRainRate":
						station.ThisYear.HighRainRate.Val = value;
						station.ThisYear.HighRainRate.Ts = time;
						break;
					case "highHourlyRain":
						station.ThisYear.HourlyRain.Val = value;
						station.ThisYear.HourlyRain.Ts = time;
						break;
					case "highDailyRain":
						station.ThisYear.DailyRain.Val = value;
						station.ThisYear.DailyRain.Ts = time;
						break;
					case "highRain24h":
						station.ThisYear.HighRain24Hours.Val = value;
						station.ThisYear.HighRain24Hours.Ts = time;
						break;
					case "highMonthlyRain":
						station.ThisYear.MonthlyRain.Val = value;
						// MM/yyyy
						station.ThisYear.MonthlyRain.Ts = localeMonthYearStrToDate(txtTime);
						break;
					case "longestDryPeriod":
						station.ThisYear.LongestDryPeriod.Val = int.Parse(txtValue);
						station.ThisYear.LongestDryPeriod.Ts = time;
						break;
					case "longestWetPeriod":
						station.ThisYear.LongestWetPeriod.Val = int.Parse(txtValue);
						station.ThisYear.LongestWetPeriod.Ts = time;
						break;
					default:
						return "Data index not recognised";
				}
				station.WriteYearIniFile();
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
			return "Success";
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
				cumulus.LogMessage("Writing current conditions to file...");

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
					cumulus.LogMessage("Before rain today edit, raintoday=" + (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum) + " Raindaystart=" + station.RainCounterDayStart.ToString(cumulus.RainFormat, invNum));
					station.RainToday = raintoday;
					station.RainCounterDayStart = station.RainCounter - ((station.RainToday ?? 0) / cumulus.Calib.Rain.Mult);
					cumulus.LogMessage("After rain today edit,  raintoday=" + (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum) + " Raindaystart=" + station.RainCounterDayStart.ToString(cumulus.RainFormat, invNum));
					// force the rainthismonth/rainthisyear values to be recalculated
					station.UpdateYearMonthRainfall();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Edit rain today");
				}
			}

			return new JsonObject
			{
				["raintoday"] = (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum),
				["raincounter"] = station.RainCounter.ToString(cumulus.RainFormat, invNum),
				["startofdayrain"] = station.RainCounterDayStart.ToString(cumulus.RainFormat, invNum),
				["rainmult"] = cumulus.Calib.Rain.Mult.ToString("F3", invNum)
			}.ToJson();
		}

		internal string GetRainTodayEditData()
		{
			var step = (cumulus.RainDPlaces == 1 ? "0.1" : "0.01");
			return new JsonObject
			{
				["raintoday"] = (station.RainToday ?? 0).ToString(cumulus.RainFormat, invNum),
				["raincounter"] = station.RainCounter.ToString(cumulus.RainFormat, invNum),
				["startofdayrain"] = station.RainCounterDayStart.ToString(cumulus.RainFormat, invNum),
				["rainmult"] = cumulus.Calib.Rain.Mult.ToString("F3", invNum),
				["step"] = step
			}.ToJson();
		}

		private static void AddLastHoursRainEntry(DateTime ts, double rain, ref Queue<LastHourRainLog> hourQueue, ref Queue<LastHourRainLog> h24Queue)
		{
			var lastrain = new LastHourRainLog(ts, rain);

			hourQueue.Enqueue(lastrain);

			var hoursago = ts.AddHours(-1);

			while ((hourQueue.Count > 0) && (hourQueue.Peek().Timestamp < hoursago))
			{
				// the oldest entry is older than 1 hour ago, delete it
				hourQueue.Dequeue();
			}

			h24Queue.Enqueue(lastrain);

			hoursago = ts.AddHours(-24);

			while ((h24Queue.Count > 0) && (h24Queue.Peek().Timestamp < hoursago))
			{
				// the oldest entry is older than 24 hours ago, delete it
				h24Queue.Dequeue();
			}
		}

		/*
		private void Add24HourRainEntry(DateTime ts, double rain, ref Queue<LastHourRainLog> h24Queue)
		{
			var lastrain = new LastHourRainLog(ts, rain);
			h24Queue.Enqueue(lastrain);
		}
		*/

		internal string EditMySqlCache(IHttpContext context)
		{
			var request = context.Request;
			string text;
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				text = reader.ReadToEnd();
			}

			var newData = text.FromJson<MySqlCacheEditor>();

			if (newData.action == "Edit")
			{
				SqlCache newRec = null;

				try
				{
					newRec = new SqlCache()
					{
						key = newData.keys[0],
						statement = newData.statements[0]
					};

					station.Database.Update(newRec);
					station.ReloadFailedMySQLCommands();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, $"EditMySqlCache: Failed, to update MySQL statement");
					context.Response.StatusCode = 500;

					return "{\"errors\":{\"MySqlCache\":[\"Failed to update MySQL cache\"]}, \"data\":[\"" + newRec.statement + "\"]";
				}

				// return the updated record
				return $"[{newData.keys[0]},\"{newData.statements[0]}\"]";
			}
			else if (newData.action == "Delete")
			{
				var newRec = new SqlCache();

				try
				{
					for (var i = 0; i < newData.keys.Length; i++)
					{
						newRec.key = newData.keys[i];
						newRec.statement = newData.statements[i];

						station.Database.Delete(newRec);
					}

					station.ReloadFailedMySQLCommands();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, $"EditMySqlCache: Failed, to delete MySQL statement");
					context.Response.StatusCode = 500;

					return "{\"errors\":{\"MySqlCache\":[\"Failed to update MySQL cache\"]}, \"data\":[\"" + newRec.statement + "\"]";
				}

				return "{\"errors\":null}";

			}
			else
			{
				cumulus.LogWarningMessage($"EditMySqlCache: Unrecognised action = " + newData.action);
				context.Response.StatusCode = 500;
				return "{\"errors\":{\"SQL cache\":[\"<br>Failed, unrecognised action = " + newData.action + "\"]}}";
			}
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

		private static DateTime localeDateTimeStrToDate(string dt)
		{
			dt = dt.Replace('+', ' ');

			// let this throw on invalid input
			return DateTime.Parse(dt);
		}

		private static DateTime localeMonthYearStrToDate(string dt)
		{
			dt = dt.Replace('+', ' ');

			// let this throw on invalid input
			return DateTime.ParseExact("01 " + dt, "dd MMM yyyy", CultureInfo.CurrentCulture);
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

		private class MySqlCacheEditor
		{
			public string action { get; set; }
			public long[] keys { get; set; }
			public string[] statements { get; set; }
		}
	}
}
