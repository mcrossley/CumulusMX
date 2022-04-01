using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SQLite;

namespace CumulusMX
{
	public class DayData
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }         // 0  Date
		public double? HighGust { get; set; }           // 1  Highest wind gust
		public int? HighGustBearing { get; set; }       // 2  Bearing of highest wind gust
		public DateTime? HighGustTime { get; set; }     // 3  Time of highest wind gust
		public double? LowTemp { get; set; }            // 4  Minimum temperature
		public DateTime? LowTempTime { get; set; }      // 5  Time of minimum temperature
		public double? HighTemp { get; set; }           // 6  Maximum temperature
		public DateTime? HighTempTime { get; set; }     // 7  Time of maximum temperature
		public double? LowPress { get; set; }           // 8  Minimum sea level pressure
		public DateTime? LowPressTime { get; set; }     // 9  Time of minimum pressure
		public double? HighPress { get; set; }          // 10  Maximum sea level pressure
		public DateTime? HighPressTime { get; set; }    // 11  Time of maximum pressure
		public double? HighRainRate { get; set; }       // 12  Maximum rainfall rate
		public DateTime? HighRainRateTime { get; set; } // 13  Time of maximum rainfall rate
		public double? TotalRain { get; set; }          // 14  Total rainfall for the day
		public double? AvgTemp { get; set; }            // 15  Average temperature for the day
		public double? WindRun { get; set; }            // 16  Total wind run
		public double? HighAvgWind { get; set; }        // 17  Highest average wind speed
		public DateTime? HighAvgWindTime { get; set; }  // 18  Time of highest average wind speed
		public int? LowHumidity { get; set; }           // 19  Lowest humidity
		public DateTime? LowHumidityTime { get; set; }  // 20  Time of lowest humidity
		public int? HighHumidity { get; set; }          // 21  Highest humidity
		public DateTime? HighHumidityTime { get; set; } // 22  Time of highest humidity
		public double? ET { get; set; }                 // 23  Total evapotranspiration
		public double? SunShineHours { get; set; }      // 24  Total hours of sunshine
		public double? HighHeatIndex { get; set; }      // 25  High heat index
		public DateTime? HighHeatIndexTime { get; set; }// 26  Time of high heat index
		public double? HighAppTemp { get; set; }        // 27  High apparent temperature
		public DateTime? HighAppTempTime { get; set; }  // 28  Time of high apparent temperature
		public double? LowAppTemp { get; set; }         // 29  Low apparent temperature
		public DateTime? LowAppTempTime { get; set; }   // 30  Time of low apparent temperature
		public double? HighHourlyRain { get; set; }     // 31  High hourly rain
		public DateTime? HighHourlyRainTime { get; set; }   // 32  Time of high hourly rain
		public double? LowWindChill { get; set; }       // 33  Low wind chill
		public DateTime? LowWindChillTime { get; set; } // 34  Time of low wind chill
		public double? HighDewPoint { get; set; }       // 35  High dew point
		public DateTime? HighDewPointTime { get; set; } // 36  Time of high dew point
		public double? LowDewPoint { get; set; }        // 37  Low dew point
		public DateTime? LowDewPointTime { get; set; }  // 38  Time of low dew point
		public int? DominantWindBearing { get; set; }   // 39  Dominant wind bearing
		public double? HeatingDegreeDays { get; set; }  // 40  Heating degree days
		public double? CoolingDegreeDays { get; set; }  // 41  Cooling degree days
		public int? HighSolar { get; set; }             // 42  High solar radiation
		public DateTime? HighSolarTime { get; set; }    // 43  Time of high solar radiation
		public double? HighUv { get; set; }             // 44  High UV Index
		public DateTime? HighUvTime { get; set; }       // 45  Time of high UV Index
		public double? HighFeelsLike { get; set; }      // 46  High Feels like
		public DateTime? HighFeelsLikeTime { get; set; }// 47  Time of high feels like
		public double? LowFeelsLike { get; set; }       // 48  Low feels like
		public DateTime? LowFeelsLikeTime { get; set; } // 49  Time of low feels like
		public double? HighHumidex { get; set; }        // 50  High Humidex
		public DateTime? HighHumidexTime { get; set; }  // 51  Time of high Humidex
		public double? ChillHours { get; set; }         // 52  Total chill hours

		public string ToCSV(bool Tofile=false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = Tofile ? "dd/MM/yy" : "'\"'dd/MM/yy'\"'";
			var timForm = Tofile ? "HH:mm" : "'\"'HH:mm'\"'";
			var blank = Tofile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Timestamp.ToString(dateformat, invDate)).Append(sep);
			sb.Append(HighGust.HasValue ? HighGust.Value.ToString(Program.cumulus.WindFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighGustBearing.HasValue ? HighGustBearing.Value : blank);
			sb.Append(sep);
			sb.Append(HighGustTime.HasValue ? HighGustTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowTemp.HasValue ? LowTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowTempTime.HasValue ? LowTempTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighTemp.HasValue ? HighTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighTempTime.HasValue ? HighTempTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowPress.HasValue ? LowPress.Value.ToString(Program.cumulus.PressFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowPressTime.HasValue ? LowPressTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighPress.HasValue ? HighPress.Value.ToString(Program.cumulus.PressFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighPressTime.HasValue ? HighPressTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighRainRate.HasValue ? HighRainRate.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighRainRateTime.HasValue ? HighRainRateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(TotalRain.HasValue ? TotalRain.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(AvgTemp.HasValue ? AvgTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(WindRun.HasValue ? WindRun.Value.ToString(Program.cumulus.WindRunFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighAvgWind.HasValue ? HighAvgWind.Value.ToString(Program.cumulus.WindFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighAvgWindTime.HasValue ? HighAvgWindTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowHumidity.HasValue ? LowHumidity : blank);
			sb.Append(sep);
			sb.Append(LowHumidityTime.HasValue ? LowHumidityTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighHumidity.HasValue ? HighHumidity : blank);
			sb.Append(sep);
			sb.Append(HighHumidityTime.HasValue ? HighHumidityTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(ET.HasValue ? ET.Value.ToString(Program.cumulus.ETFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(SunShineHours.HasValue ? SunShineHours.Value.ToString(Program.cumulus.SunFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHeatIndex.HasValue ? HighHeatIndex.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHeatIndexTime.HasValue ? HighHeatIndexTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighAppTemp.HasValue ? HighAppTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighAppTempTime.HasValue ? HighAppTempTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowAppTemp.HasValue ? LowAppTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowAppTempTime.HasValue ? LowAppTempTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighHourlyRain.HasValue ? HighHourlyRain.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHourlyRainTime.HasValue ? HighHourlyRainTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowWindChill.HasValue ? LowWindChill.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowWindChillTime.HasValue ? LowWindChillTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighDewPoint.HasValue ? HighDewPoint.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighDewPointTime.HasValue ? HighDewPointTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowDewPoint.HasValue ? LowDewPoint.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowDewPointTime.HasValue ? LowDewPointTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(DominantWindBearing.HasValue ? DominantWindBearing.Value : blank);
			sb.Append(sep);
			sb.Append(HeatingDegreeDays.HasValue ? HeatingDegreeDays.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(CoolingDegreeDays.HasValue ? CoolingDegreeDays.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighSolar.HasValue ? HighSolar : blank);
			sb.Append(sep);
			sb.Append(HighSolarTime.HasValue ? HighSolarTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighUv.HasValue ? HighUv.Value.ToString(Program.cumulus.UVFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighUvTime.HasValue ? HighUvTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighFeelsLike.HasValue ? HighFeelsLike.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighFeelsLikeTime.HasValue ? HighFeelsLikeTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowFeelsLike.HasValue ? LowFeelsLike.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowFeelsLikeTime.HasValue ? LowFeelsLikeTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighHumidex.HasValue ? HighHumidex.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHumidexTime.HasValue ? HighHumidexTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(ChillHours.HasValue ? ChillHours.Value.ToString("F1", invNum) : blank);

			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			var invDate = CultureInfo.InvariantCulture.NumberFormat;
			var timForm = "hh\\:mm";

			// Make sure we always have the correct number of fields
			var data2 = new string[Cumulus.DayfileFields];
			Array.Copy(data, data2, data.Length);

			Timestamp = DateTime.ParseExact(data2[0], "dd/MM/yy", invDate);
			HighGust = Utils.TryParseNullDouble(data2[1]);
			HighGustBearing = Utils.TryParseNullInt(data2[2]);
			HighGustTime = Utils.TryParseNullTimeSpan(Timestamp, data2[3], timForm);
			LowTemp = Utils.TryParseNullDouble(data2[4]);
			LowTempTime = Utils.TryParseNullTimeSpan(Timestamp, data2[5], timForm);
			HighTemp = Utils.TryParseNullDouble(data2[6]);
			HighTempTime = Utils.TryParseNullTimeSpan(Timestamp, data2[7], timForm);
			LowPress = Utils.TryParseNullDouble(data2[8]);
			LowPressTime = Utils.TryParseNullTimeSpan(Timestamp, data2[9], timForm);
			HighPress = Utils.TryParseNullDouble(data2[10]);
			HighPressTime = Utils.TryParseNullTimeSpan(Timestamp, data2[11], timForm);
			HighRainRate = Utils.TryParseNullDouble(data2[12]);
			HighRainRateTime = Utils.TryParseNullTimeSpan(Timestamp, data2[13], timForm);
			TotalRain = Utils.TryParseNullDouble(data2[14]);
			AvgTemp = Utils.TryParseNullDouble(data2[15]);
			WindRun = Utils.TryParseNullDouble(data2[16]);
			HighAvgWind = Utils.TryParseNullDouble(data2[17]);
			HighAvgWindTime = Utils.TryParseNullTimeSpan(Timestamp, data2[18], timForm);
			LowHumidity = Utils.TryParseNullInt(data2[19]);
			LowHumidityTime = Utils.TryParseNullTimeSpan(Timestamp, data2[20], timForm);
			HighHumidity = Utils.TryParseNullInt(data2[21]);
			HighHumidityTime = Utils.TryParseNullTimeSpan(Timestamp, data2[22], timForm);
			ET = Utils.TryParseNullDouble(data2[23]);
			SunShineHours = Utils.TryParseNullDouble(data2[24]);
			HighHeatIndex = Utils.TryParseNullDouble(data2[25]);
			HighHeatIndexTime = Utils.TryParseNullTimeSpan(Timestamp, data2[26], timForm);
			HighAppTemp = Utils.TryParseNullDouble(data2[27]);
			HighAppTempTime = Utils.TryParseNullTimeSpan(Timestamp, data2[28], timForm);
			LowAppTemp = Utils.TryParseNullDouble(data2[29]);
			LowAppTempTime = Utils.TryParseNullTimeSpan(Timestamp, data2[30], timForm);
			HighHourlyRain = Utils.TryParseNullDouble(data2[31]);
			HighHourlyRainTime = Utils.TryParseNullTimeSpan(Timestamp, data2[32], timForm);
			LowWindChill = Utils.TryParseNullDouble(data2[33]);
			LowWindChillTime = Utils.TryParseNullTimeSpan(Timestamp, data2[34], timForm);
			HighDewPoint = Utils.TryParseNullDouble(data2[35]);
			HighDewPointTime = Utils.TryParseNullTimeSpan(Timestamp, data2[36], timForm);
			LowDewPoint = Utils.TryParseNullDouble(data2[37]);
			LowDewPointTime = Utils.TryParseNullTimeSpan(Timestamp, data2[38], timForm);
			DominantWindBearing = Utils.TryParseNullInt(data2[39]);
			HeatingDegreeDays = Utils.TryParseNullDouble(data2[40]);
			CoolingDegreeDays = Utils.TryParseNullDouble(data2[41]);
			HighSolar = Utils.TryParseNullInt(data2[42]);
			HighSolarTime = Utils.TryParseNullTimeSpan(Timestamp, data2[43], timForm);
			HighUv = Utils.TryParseNullDouble(data2[44]);
			HighUvTime = Utils.TryParseNullTimeSpan(Timestamp, data2[45], timForm);
			HighFeelsLike = Utils.TryParseNullDouble(data2[46]);
			HighFeelsLikeTime = Utils.TryParseNullTimeSpan(Timestamp, data2[47], timForm);
			LowFeelsLike = Utils.TryParseNullDouble(data2[48]);
			LowFeelsLikeTime = Utils.TryParseNullTimeSpan(Timestamp, data2[49], timForm);
			HighHumidex = Utils.TryParseNullDouble(data2[50]);
			HighHumidexTime = Utils.TryParseNullTimeSpan(Timestamp, data2[51], timForm);
			ChillHours = Utils.TryParseNullDouble(data2[52]);

			return true;
		}

		public bool ParseDayFileRecv4(string data)
		{
			var st = new List<string>(data.Split(','));
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			double varDbl;
			int varInt;
			int idx = 0;
			var timeFormat = "hh\\:mm";

			try
			{
				Timestamp = Utils.ddmmyyStrToDate(st[idx++]);
				HighGust = Convert.ToDouble(st[idx++], invNum);
				HighGustBearing = Convert.ToInt32(st[idx++]);
				HighGustTime = Utils.AddTimeToDate(Timestamp, st[idx++]);
				LowTemp = Convert.ToDouble(st[idx++], invNum);
				LowTempTime = Utils.AddTimeToDate(Timestamp, st[idx++]);
				HighTemp = Convert.ToDouble(st[idx++], invNum);
				HighTempTime = Utils.AddTimeToDate(Timestamp, st[idx++]);
				LowPress = Convert.ToDouble(st[idx++], invNum);
				LowPressTime = Utils.AddTimeToDate(Timestamp, st[idx++]);
				HighPress = Convert.ToDouble(st[idx++], invNum);
				HighPressTime = Utils.AddTimeToDate(Timestamp, st[idx++]);
				HighRainRate = Convert.ToDouble(st[idx++], invNum);
				HighRainRateTime = Utils.AddTimeToDate(Timestamp, st[idx++]);
				TotalRain = Convert.ToDouble(st[idx++], invNum);
				AvgTemp = Convert.ToDouble(st[idx++], invNum);

				if (st.Count > idx++)
					WindRun = Utils.TryParseNullDouble(st[16]);

				if (st.Count > idx++)
					HighAvgWind = Utils.TryParseNullDouble(st[17]);

				if (st.Count > idx++)
					HighAvgWindTime = Utils.TryParseNullTimeSpan(Timestamp, st[18], timeFormat);

				if (st.Count > idx++)
					LowHumidity = Utils.TryParseNullInt(st[19]);

				if (st.Count > idx++)
					LowHumidityTime = Utils.TryParseNullTimeSpan(Timestamp, st[20], timeFormat);

				if (st.Count > idx++)
					HighHumidity = Utils.TryParseNullInt(st[21]);

				if (st.Count > idx++ && st[22].Length == 5)
					HighHumidityTime = Utils.AddTimeToDate(Timestamp, st[22]);

				if (st.Count > idx++)
					ET = Utils.TryParseNullDouble(st[23]);

				if (st.Count > idx++ && double.TryParse(st[24], NumberStyles.Float, invNum, out varDbl))
					SunShineHours = varDbl;

				if (st.Count > idx++ && double.TryParse(st[25], NumberStyles.Float, invNum, out varDbl))
					HighHeatIndex = varDbl;
				else
					HighHeatIndex = Cumulus.DefaultHiVal;

				if (st.Count > idx++ && st[26].Length == 5)
					HighHeatIndexTime = Utils.AddTimeToDate(Timestamp, st[26]);

				if (st.Count > idx++ && double.TryParse(st[27], NumberStyles.Float, invNum, out varDbl))
					HighAppTemp = varDbl;
				else
					HighAppTemp = Cumulus.DefaultHiVal;

				if (st.Count > idx++ && st[28].Length == 5)
					HighAppTempTime = Utils.AddTimeToDate(Timestamp, st[28]);

				if (st.Count > idx++ && double.TryParse(st[29], NumberStyles.Float, invNum, out varDbl))
					LowAppTemp = varDbl;
				else
					LowAppTemp = Cumulus.DefaultLoVal;

				if (st.Count > idx++ && st[30].Length == 5)
					LowAppTempTime = Utils.AddTimeToDate(Timestamp, st[30]);

				if (st.Count > idx++ && double.TryParse(st[31], NumberStyles.Float, invNum, out varDbl))
					HighHourlyRain = varDbl;

				if (st.Count > idx++ && st[32].Length == 5)
					HighHourlyRainTime = Utils.AddTimeToDate(Timestamp, st[32]);

				if (st.Count > idx++ && double.TryParse(st[33], NumberStyles.Float, invNum, out varDbl))
					LowWindChill = varDbl;
				else
					LowWindChill = Cumulus.DefaultLoVal;

				if (st.Count > idx++ && st[34].Length == 5)
					LowWindChillTime = Utils.AddTimeToDate(Timestamp, st[34]);

				if (st.Count > idx++ && double.TryParse(st[35], NumberStyles.Float, invNum, out varDbl))
					HighDewPoint = varDbl;
				else
					HighDewPoint = Cumulus.DefaultHiVal;

				if (st.Count > idx++ && st[36].Length == 5)
					HighDewPointTime = Utils.AddTimeToDate(Timestamp, st[36]);

				if (st.Count > idx++ && double.TryParse(st[37], NumberStyles.Float, invNum, out varDbl))
					LowDewPoint = varDbl;
				else
					LowDewPoint = Cumulus.DefaultLoVal;

				if (st.Count > idx++ && st[38].Length == 5)
					LowDewPointTime = Utils.AddTimeToDate(Timestamp, st[38]);

				if (st.Count > idx++ && int.TryParse(st[39], out varInt))
					DominantWindBearing = varInt;

				if (st.Count > idx++ && double.TryParse(st[40], NumberStyles.Float, invNum, out varDbl))
					HeatingDegreeDays = varDbl;

				if (st.Count > idx++ && double.TryParse(st[41], NumberStyles.Float, invNum, out varDbl))
					CoolingDegreeDays = varDbl;

				if (st.Count > idx++ && int.TryParse(st[42], out varInt))
					HighSolar = varInt;

				if (st.Count > idx++ && st[43].Length == 5)
					HighSolarTime = Utils.AddTimeToDate(Timestamp, st[43]);

				if (st.Count > idx++ && double.TryParse(st[44], NumberStyles.Float, invNum, out varDbl))
					HighUv = varDbl;

				if (st.Count > idx++ && st[45].Length == 5)
					HighUvTime = Utils.AddTimeToDate(Timestamp, st[45]);

				if (st.Count > idx++ && double.TryParse(st[46], NumberStyles.Float, invNum, out varDbl))
					HighFeelsLike = varDbl;
				else
					HighFeelsLike = Cumulus.DefaultHiVal;

				if (st.Count > idx++ && st[47].Length == 5)
					HighFeelsLikeTime = Utils.AddTimeToDate(Timestamp, st[47]);

				if (st.Count > idx++ && double.TryParse(st[48], NumberStyles.Float, invNum, out varDbl))
					LowFeelsLike = varDbl;
				else
					LowFeelsLike = Cumulus.DefaultLoVal;

				if (st.Count > idx++ && st[49].Length == 5)
					LowFeelsLikeTime = Utils.AddTimeToDate(Timestamp, st[49]);

				if (st.Count > idx++ && double.TryParse(st[50], NumberStyles.Float, invNum, out varDbl))
					HighHumidex = varDbl;
				else
					HighHumidex = Cumulus.DefaultHiVal;

				if (st.Count > idx++ && st[51].Length == 5)
					HighHumidexTime = Utils.AddTimeToDate(Timestamp, st[51]);

				if (st.Count > idx++ && double.TryParse(st[52], NumberStyles.Float, invNum, out varDbl))
					ChillHours = varDbl;
			}
			catch (Exception ex)
			{
				Program.cumulus.LogExceptionMessage(ex, $"ParseDayFileRec: Error at record {idx}");
				var e = new Exception($"Error at record {idx} = \"{st[idx - 1]}\" - {ex.Message}");
				throw e;
			}
			return true;
		}
	}
}
