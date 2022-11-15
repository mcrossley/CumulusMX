﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	class IntervalData
	{
		private DateTime time;
		private long timestamp;

		[Ignore]
		public DateTime StationTime                      // 0  DateTime
		{
			get { return time; }
			set
			{
				time = value;
				timestamp = StationTime.ToUnixTime();
			}
		}
		[PrimaryKey]
		public long Timestamp                            // N/A 1  Current Unix timestamp
		{
			get { return timestamp; }
			set
			{
				timestamp = value;
				time = value.FromUnixTime().ToLocalTime();
			}
		}
		public double? Temp { get; set; }               // 2  Current temperature
		public int? Humidity { get; set; }              // 3  Current humidity
		public double? DewPoint { get; set; }           // 4  Current dewpoint
		public double? WindAvg { get; set; }            // 5  Current wind speed
		public double? WindGust10m { get; set; }        // 6  Recent (10-minute) high gust
		public int? WindAvgDir { get; set; }            // 7  Average wind bearing
		public double? RainRate { get; set; }           // 8  Current rainfall rate
		public double? RainToday { get; set; }          // 9  Total rainfall today so far
		public double? Pressure { get; set; }           // 10  Current sea level pressure
		public double? RainCounter { get; set; }        // 11  Total rainfall counter as held by the station
		public double? InsideTemp { get; set; }         // 12  Inside temperature
		public int? InsideHumidity { get; set; }        // 13  Inside humidity
		public double? WindLatest { get; set; }         // 14  Current gust (i.e. 'Latest')
		public double? WindChill { get; set; }			// 15  Wind chill
		public double? HeatIndex { get; set; }			// 16  Heat Index
		public double? UV { get; set; }					// 17  UV Index
		public int? SolarRad { get; set; }				// 18  Solar Radiation
		public double? ET { get; set; }					// 19  Evapotranspiration
		public double? AnnualET { get; set; }			// 20  Annual Evapotranspiration
		public double? Apparent { get; set; }			// 21  Apparent temperature
		public int? SolarMax { get; set; }				// 22  Current theoretical max solar radiation
		public double? Sunshine { get; set; }			// 23  Hours of sunshine so far today
		public int? WindDir { get; set; }				// 24  Current wind bearing
		public double? RG11Rain { get; set; }			// 25  RG-11 rain total
		public double? RainMidnight { get; set; }		// 26  Rain since midnight
		public double? FeelsLike { get; set; }			// 27  Feels like
		public double? Humidex { get; set; }            // 28  Humidex

		public string ToCSV(bool ToFile=false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var blank = ToFile ? "" : "\"\"";
			var datetimeformat = ToFile ? "dd/MM/yy HH:mm" : "'\"'dd/MM/yy HH:mm'\"'";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(StationTime.ToString(datetimeformat, invDate)).Append(',');
			sb.Append(Timestamp).Append(',');
			sb.Append(Temp.HasValue ? Temp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Humidity.HasValue ? Humidity : blank);
			sb.Append(sep);
			sb.Append(DewPoint.HasValue ? DewPoint.Value.ToString(Program.cumulus.TempFormat, invNum): blank);
			sb.Append(sep);
			sb.Append(WindAvg.HasValue ? WindAvg.Value.ToString(Program.cumulus.WindAvgFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(WindGust10m.HasValue ? WindGust10m.Value.ToString(Program.cumulus.WindFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(WindAvgDir.HasValue ? WindAvgDir : blank);
			sb.Append(sep);
			sb.Append(RainRate.HasValue ? RainRate.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(RainToday.HasValue ? RainToday.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Pressure.HasValue ? Pressure.Value.ToString(Program.cumulus.PressFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(RainCounter.HasValue ? RainCounter.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(InsideTemp.HasValue ? InsideTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(InsideHumidity.HasValue ? InsideHumidity : blank);
			sb.Append(sep);
			sb.Append(WindLatest.HasValue ? WindLatest.Value.ToString(Program.cumulus.WindFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(WindChill.HasValue ? WindChill.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HeatIndex.HasValue ? HeatIndex.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(UV.HasValue ? UV.Value.ToString(Program.cumulus.UVFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(SolarRad.HasValue ? SolarRad : blank);
			sb.Append(sep);
			sb.Append(ET.HasValue ? ET.Value.ToString(Program.cumulus.ETFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(AnnualET.HasValue ? AnnualET.Value.ToString(Program.cumulus.ETFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Apparent.HasValue ? Apparent.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(SolarMax.HasValue ? SolarMax : blank);
			sb.Append(sep);
			sb.Append(Sunshine.HasValue ? Sunshine.Value.ToString(Program.cumulus.SunFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(WindDir.HasValue ? WindDir : blank);
			sb.Append(sep);
			sb.Append(RG11Rain.HasValue ? RG11Rain.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(RainMidnight.HasValue ? RainMidnight.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(FeelsLike.HasValue ? FeelsLike.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(Humidex.HasValue ? Humidex.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			// Make sure we always have the correct number of fields - we have
			var data2 = new string[Cumulus.NumLogFileFields];
			Array.Copy(data, data2, data.Length);

			// we ignore the date/time string in field zero
			Timestamp = long.Parse(data2[1]);
			Temp = Utils.TryParseNullDouble(data2[2]);
			Humidity = Utils.TryParseNullInt(data2[3]);
			DewPoint = Utils.TryParseNullDouble(data2[4]);
			WindAvg = Utils.TryParseNullDouble(data2[5]);
			WindGust10m = Utils.TryParseNullDouble(data2[6]);
			WindAvgDir = Utils.TryParseNullInt(data2[7]);
			RainRate = Utils.TryParseNullDouble(data2[8]);
			RainToday = Utils.TryParseNullDouble(data2[9]);
			Pressure = Utils.TryParseNullDouble(data2[10]);
			RainCounter = Utils.TryParseNullDouble(data2[11]);
			InsideTemp = Utils.TryParseNullDouble(data2[12]);
			InsideHumidity = Utils.TryParseNullInt(data2[13]);
			WindLatest = Utils.TryParseNullDouble(data2[14]);
			WindChill = Utils.TryParseNullDouble(data2[15]);
			HeatIndex = Utils.TryParseNullDouble(data2[16]);
			UV = Utils.TryParseNullDouble(data2[17]);
			SolarRad = Utils.TryParseNullInt(data2[18]);
			ET = Utils.TryParseNullDouble(data2[19]);
			AnnualET = Utils.TryParseNullDouble(data2[20]);
			Apparent = Utils.TryParseNullDouble(data2[21]);
			SolarMax = Utils.TryParseNullInt(data2[22]);
			Sunshine = Utils.TryParseNullDouble(data2[23]);
			WindDir = Utils.TryParseNullInt(data2[24]);
			RG11Rain = Utils.TryParseNullDouble(data2[25]);
			RainMidnight = Utils.TryParseNullDouble(data2[26]);
			FeelsLike = Utils.TryParseNullDouble(data2[27]);
			Humidex = Utils.TryParseNullDouble(data2[28]);

			return true;
		}
	}
}
