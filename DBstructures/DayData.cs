using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	public class DayData
	{
		private DateTime _Date;
		private long _Timestamp;


		[Ignore]
		public DateTime Date                            // 0  Date
		{
			get { return _Date; }
			set
			{
				if (value.Kind == DateTimeKind.Unspecified)
					_Date = DateTime.SpecifyKind(value, DateTimeKind.Local);
				else
					_Date = value;
				_Timestamp = _Date.ToUnixTime();
			}
		}

		[PrimaryKey]
		public long Timestamp                            // 1  Timestamp
		{
			get { return _Timestamp; }
			set
			{
				_Timestamp = value;
				_Date = value.FromUnixTime().ToLocalTime();
			}
		}
		public double? HighGust { get; set; }           // 2  Highest wind gust
		public int? HighGustBearing { get; set; }       // 3  Bearing of highest wind gust

		private DateTime? _HighGustDateTime;
		[Ignore]
		public DateTime? HighGustDateTime              // 4  Time of highest wind gust
		{
			get { return _HighGustDateTime; }
			set
			{
				_HighGustDateTime = value;
				_HighGustTime = _HighGustDateTime.HasValue ? _HighGustDateTime.Value.ToUnixTime() : null;
			}
		}
		private long? _HighGustTime;
		public long? HighGustTime
		{
			get { return _HighGustTime;  }
			set
			{
				_HighGustTime = value;
				_HighGustDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? LowTemp { get; set; }            // 5  Minimum temperature

		private DateTime? _LowTempDateTime;
		[Ignore]
		public DateTime? LowTempDateTime                // 6  Time of minimum temperature
		{
			get { return _LowTempDateTime; }
			set
			{
				_LowTempDateTime = value;
				_LowTempTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _LowTempTime;
		public long? LowTempTime
		{
			get { return _LowTempTime; }
			set
			{
				_LowTempTime = value;
				_LowTempDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}


		public double? HighTemp { get; set; }           // 7  Maximum temperature

		private DateTime? _HighTempDateTime;
		[Ignore]
		public DateTime? HighTempDateTime               // 8  Time of maximum temperature
		{
			get { return _HighTempDateTime; }
			set
			{
				_HighTempDateTime = value;
				_HighTempTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighTempTime;
		public long? HighTempTime
		{
			get { return _HighTempTime; }
			set
			{
				_HighTempTime = value;
				_HighTempDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? LowPress { get; set; }           // 9  Minimum sea level pressure
		private DateTime? _LowPressDateTime;
		[Ignore]
		public DateTime? LowPressDateTime               // 10  Time of minimum pressure
		{
			get { return _LowPressDateTime; }
			set
			{
				_LowPressDateTime = value;
				_LowPressTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _LowPressTime;
		public long? LowPressTime
		{
			get { return _LowPressTime; }
			set
			{
				_LowPressTime = value;
				_LowPressDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighPress { get; set; }          // 11  Maximum sea level pressure
		private DateTime? _HighPressDateTime;
		[Ignore]
		public DateTime? HighPressDateTime              // 12  Time of maximum pressure
		{
			get { return _HighPressDateTime; }
			set
			{
				_HighPressDateTime = value;
				_HighPressTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighPressTime;
		public long? HighPressTime
		{
			get { return _HighPressTime; }
			set
			{
				_HighPressTime = value;
				_HighPressDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighRainRate { get; set; }       // 13  Maximum rainfall rate
		private DateTime? _HighRainRateDateTime;
		[Ignore]
		public DateTime? HighRainRateDateTime           // 14  Time of maximum rainfall rate
		{
			get { return _HighRainRateDateTime; }
			set
			{
				_HighRainRateDateTime = value;
				_HighRainRateTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighRainRateTime;
		public long? HighRainRateTime
		{
			get { return _HighRainRateTime; }
			set
			{
				_HighRainRateTime = value;
				_HighRainRateDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? TotalRain { get; set; }          // 15  Total rainfall for the day
		public double? AvgTemp { get; set; }            // 16  Average temperature for the day
		public double? WindRun { get; set; }            // 17  Total wind run
		public double? HighAvgWind { get; set; }        // 18  Highest average wind speed

		private DateTime? _HighAvgWindDateTime;
		[Ignore]
		public DateTime? HighAvgWindDateTime            // 19  Time of highest average wind speed
		{
			get { return _HighAvgWindDateTime; }
			set
			{
				_HighAvgWindDateTime = value;
				_HighAvgWindTime = value.HasValue? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighAvgWindTime;
		public long? HighAvgWindTime
		{
			get { return _HighAvgWindTime; }
			set
			{
				_HighAvgWindTime = value;
				_HighAvgWindDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public int? LowHumidity { get; set; }           // 20  Lowest humidity
		private DateTime? _LowHumidityDateTime;
		[Ignore]
		public DateTime? LowHumidityDateTime            // 21  Time of lowest humidity
		{
			get { return _LowHumidityDateTime; }
			set
			{
				_LowHumidityDateTime = value;
				_LowHumidityTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _LowHumidityTime;
		public long? LowHumidityTime
		{
			get { return _LowHumidityTime; }
			set
			{
				_LowHumidityTime = value;
				_LowHumidityDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public int? HighHumidity { get; set; }          // 22  Highest humidity
		private DateTime? _HighHumidityDateTime;
		[Ignore]
		public DateTime? HighHumidityDateTime           // 23  Time of highest humidity
		{
			get { return _HighHumidityDateTime; }
			set
			{
				_HighHumidityDateTime = value;
				_HighHumidityTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighHumidityTime;
		public long? HighHumidityTime
		{
			get { return _HighHumidityTime; }
			set
			{
				_HighHumidityTime = value;
				_HighHumidityDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? ET { get; set; }                 // 24  Total evapotranspiration
		public double? SunShineHours { get; set; }      // 25  Total hours of sunshine
		public double? HighHeatIndex { get; set; }      // 26  High heat index
		private DateTime? _HighHeatIndexDateTime;
		[Ignore]
		public DateTime? HighHeatIndexDateTime          // 27  Time of high heat index
		{
			get { return _HighHeatIndexDateTime; }
			set
			{
				_HighHeatIndexDateTime = value;
				_HighHeatIndexTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighHeatIndexTime;
		public long? HighHeatIndexTime
		{
			get { return _HighHeatIndexTime; }
			set
			{
				_HighHeatIndexTime = value;
				_HighHeatIndexDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighAppTemp { get; set; }        // 28  High apparent temperature
		private DateTime? _HighAppTempDateTime;
		[Ignore]
		public DateTime? HighAppTempDateTime            // 29  Time of high apparent temperature
		{
			get { return _HighAppTempDateTime; }
			set
			{
				_HighAppTempDateTime = value;
				_HighAppTempTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighAppTempTime;
		public long? HighAppTempTime
		{
			get { return _HighAppTempTime; }
			set
			{
				_HighAppTempTime = value;
				_HighAppTempDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? LowAppTemp { get; set; }         // 30  Low apparent temperature
		private DateTime? _LowAppTempDateTime;
		[Ignore]
		public DateTime? LowAppTempDateTime             // 31  Time of low apparent temperature
		{
			get { return _LowAppTempDateTime; }
			set
			{
				_LowAppTempDateTime = value;
				_LowAppTempTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _LowAppTempTime;
		public long? LowAppTempTime
		{
			get { return _LowAppTempTime; }
			set
			{
				_LowAppTempTime = value;
				_LowAppTempDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighHourlyRain { get; set; }     // 32  High hourly rain
		private DateTime? _HighHourlyRainDateTime;
		[Ignore]
		public DateTime? HighHourlyRainDateTime         // 33  Time of high hourly rain
		{
			get { return _HighHourlyRainDateTime; }
			set
			{
				_HighHourlyRainDateTime = value;
				_HighHourlyRainTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighHourlyRainTime;
		public long? HighHourlyRainTime
		{
			get { return _HighHourlyRainTime; }
			set
			{
				_HighHourlyRainTime = value;
				_HighHourlyRainDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? LowWindChill { get; set; }       // 34  Low wind chill
		private DateTime? _LowWindChillDateTime;
		[Ignore]
		public DateTime? LowWindChillDateTime           // 35  Time of low wind chill
		{
			get { return _LowWindChillDateTime; }
			set
			{
				_LowWindChillDateTime = value;
				_LowWindChillTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _LowWindChillTime;
		public long? LowWindChillTime
		{
			get { return _LowWindChillTime; }
			set
			{
				_LowWindChillTime = value;
				_LowWindChillDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighDewPoint { get; set; }       // 36  High dew point
		private DateTime? _HighDewPointDateTime;
		[Ignore]
		public DateTime? HighDewPointDateTime           // 37  Time of high dew point
		{
			get { return _HighDewPointDateTime; }
			set
			{
				_HighDewPointDateTime = value;
				_HighDewPointTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighDewPointTime;
		public long? HighDewPointTime
		{
			get { return _HighDewPointTime; }
			set
			{
				_HighDewPointTime = value;
				_HighDewPointDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? LowDewPoint { get; set; }        // 38  Low dew point
		private DateTime? _LowDewPointDateTime;
		[Ignore]
		public DateTime? LowDewPointDateTime            // 39  Time of low dew point
		{
			get { return _LowDewPointDateTime; }
			set
			{
				_LowDewPointDateTime = value;
				_LowDewPointTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _LowDewPointTime;
		public long? LowDewPointTime
		{
			get { return _LowDewPointTime; }
			set
			{
				_LowDewPointTime = value;
				_LowDewPointDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public int? DominantWindBearing { get; set; }   // 40  Dominant wind bearing
		public double? HeatingDegreeDays { get; set; }  // 41  Heating degree days
		public double? CoolingDegreeDays { get; set; }  // 42  Cooling degree days
		public int? HighSolar { get; set; }             // 43  High solar radiation
		private DateTime? _HighSolarDateTime;
		[Ignore]
		public DateTime? HighSolarDateTime              // 44  Time of high solar radiation
		{
			get { return _HighSolarDateTime; }
			set
			{
				_HighSolarDateTime = value;
				_HighSolarTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighSolarTime;
		public long? HighSolarTime
		{
			get { return _HighSolarTime; }
			set
			{
				_HighSolarTime = value;
				_HighSolarDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighUv { get; set; }             // 45  High UV Index
		private DateTime? _HighUvDateTime;
		[Ignore]
		public DateTime? HighUvDateTime                 // 46  Time of high UV Index
		{
			get { return _HighUvDateTime; }
			set
			{
				_HighUvDateTime = value;
				_HighUvTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighUvTime;
		public long? HighUvTime
		{
			get { return _HighUvTime; }
			set
			{
				_HighUvTime = value;
				_HighUvDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighFeelsLike { get; set; }      // 47  High Feels like
		private DateTime? _HighFeelsLikeDateTime;
		[Ignore]
		public DateTime? HighFeelsLikeDateTime          // 48  Time of high feels like
		{
			get { return _HighFeelsLikeDateTime; }
			set
			{
				_HighFeelsLikeDateTime = value;
				_HighFeelsLikeTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighFeelsLikeTime;
		public long? HighFeelsLikeTime
		{
			get { return _HighFeelsLikeTime; }
			set
			{
				_HighFeelsLikeTime = value;
				_HighFeelsLikeDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? LowFeelsLike { get; set; }       // 49  Low feels like
		private DateTime? _LowFeelsLikeDateTime;
		[Ignore]
		public DateTime? LowFeelsLikeDateTime           // 50  Time of low feels like
		{
			get { return _LowFeelsLikeDateTime; }
			set
			{
				_LowFeelsLikeDateTime = value;
				_LowFeelsLikeTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _LowFeelsLikeTime;
		public long? LowFeelsLikeTime
		{
			get { return _LowFeelsLikeTime; }
			set
			{
				_LowFeelsLikeTime = value;
				_LowFeelsLikeDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? HighHumidex { get; set; }        // 51  High Humidex
		private DateTime? _HighHumidexDateTime;
		[Ignore]
		public DateTime? HighHumidexDateTime            // 52  Time of high Humidex
		{
			get { return _HighHumidexDateTime; }
			set
			{
				_HighHumidexDateTime = value;
				_HighHumidexTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighHumidexTime;
		public long? HighHumidexTime
		{
			get { return _HighHumidexTime; }
			set
			{
				_HighHumidexTime = value;
				_HighHumidexDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public double? ChillHours { get; set; }         // 53  Total chill hours
		public double? HighRain24Hours { get; set; }    // 54  Highest rain in 24h value
		private DateTime? _HighRain24HrDateTime;
		[Ignore]
		public DateTime? HighRain24HrDateTime           // 55  Time of highest rain in 24h
		{
			get { return _HighRain24HrDateTime; }
			set
			{
				_HighRain24HrDateTime = value;
				_HighRain24HrTime = value.HasValue ? value.Value.ToUnixTime() : null;
			}
		}
		private long? _HighRain24HrTime;
		public long? HighRain24HrTime
		{
			get { return _HighRain24HrTime; }
			set
			{
				_HighRain24HrTime = value;
				_HighRain24HrDateTime = value.HasValue ? value.Value.FromUnixTime().ToLocalTime() : null;
			}
		}

		public string ToCSV(bool Tofile=false)
		{
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			var invDate = CultureInfo.InvariantCulture.NumberFormat;

			var dateformat = Tofile ? "dd/MM/yy" : "'\"'dd/MM/yy'\"'";
			var timForm = Tofile ? "HH:mm" : "'\"'HH:mm'\"'";
			var blank = Tofile ? "" : "\"\"";
			var sep = ',';

			var sb = new StringBuilder(350);
			sb.Append(Date.ToString(dateformat, invDate)).Append(sep);
			sb.Append(HighGust.HasValue ? HighGust.Value.ToString(Program.cumulus.WindFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighGustBearing.HasValue ? HighGustBearing.Value : blank);
			sb.Append(sep);
			sb.Append(HighGustDateTime.HasValue ? HighGustDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowTemp.HasValue ? LowTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowTempDateTime.HasValue ? LowTempDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighTemp.HasValue ? HighTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighTempDateTime.HasValue ? HighTempDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowPress.HasValue ? LowPress.Value.ToString(Program.cumulus.PressFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowPressDateTime.HasValue ? LowPressDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighPress.HasValue ? HighPress.Value.ToString(Program.cumulus.PressFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighPressDateTime.HasValue ? HighPressDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighRainRate.HasValue ? HighRainRate.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighRainRateDateTime.HasValue ? HighRainRateDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(TotalRain.HasValue ? TotalRain.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(AvgTemp.HasValue ? AvgTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(WindRun.HasValue ? WindRun.Value.ToString(Program.cumulus.WindRunFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighAvgWind.HasValue ? HighAvgWind.Value.ToString(Program.cumulus.WindFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighAvgWindDateTime.HasValue ? HighAvgWindDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowHumidity.HasValue ? LowHumidity : blank);
			sb.Append(sep);
			sb.Append(LowHumidityDateTime.HasValue ? LowHumidityDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighHumidity.HasValue ? HighHumidity : blank);
			sb.Append(sep);
			sb.Append(HighHumidityDateTime.HasValue ? HighHumidityDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(ET.HasValue ? ET.Value.ToString(Program.cumulus.ETFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(SunShineHours.HasValue ? SunShineHours.Value.ToString(Program.cumulus.SunFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHeatIndex.HasValue ? HighHeatIndex.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHeatIndexDateTime.HasValue ? HighHeatIndexDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighAppTemp.HasValue ? HighAppTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighAppTempDateTime.HasValue ? HighAppTempDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowAppTemp.HasValue ? LowAppTemp.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowAppTempDateTime.HasValue ? LowAppTempDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighHourlyRain.HasValue ? HighHourlyRain.Value.ToString(Program.cumulus.RainFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHourlyRainDateTime.HasValue ? HighHourlyRainDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowWindChill.HasValue ? LowWindChill.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowWindChillDateTime.HasValue ? LowWindChillDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighDewPoint.HasValue ? HighDewPoint.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighDewPointDateTime.HasValue ? HighDewPointDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowDewPoint.HasValue ? LowDewPoint.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowDewPointDateTime.HasValue ? LowDewPointDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(DominantWindBearing.HasValue ? DominantWindBearing.Value : blank);
			sb.Append(sep);
			sb.Append(HeatingDegreeDays.HasValue ? HeatingDegreeDays.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(CoolingDegreeDays.HasValue ? CoolingDegreeDays.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighSolar.HasValue ? HighSolar : blank);
			sb.Append(sep);
			sb.Append(HighSolarDateTime.HasValue ? HighSolarDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighUv.HasValue ? HighUv.Value.ToString(Program.cumulus.UVFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighUvDateTime.HasValue ? HighUvDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighFeelsLike.HasValue ? HighFeelsLike.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighFeelsLikeDateTime.HasValue ? HighFeelsLikeDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(LowFeelsLike.HasValue ? LowFeelsLike.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(LowFeelsLikeDateTime.HasValue ? LowFeelsLikeDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(HighHumidex.HasValue ? HighHumidex.Value.ToString(Program.cumulus.TempFormat, invNum) : blank);
			sb.Append(sep);
			sb.Append(HighHumidexDateTime.HasValue ? HighHumidexDateTime.Value.ToString(timForm, invDate) : blank);
			sb.Append(sep);
			sb.Append(ChillHours.HasValue ? ChillHours.Value.ToString("F1", invNum) : blank);
			sb.Append(sep);
			sb.Append(HighRain24Hours.HasValue ? HighRain24Hours.Value.ToString(Program.cumulus.RainFormat, invNum): blank);
			sb.Append(sep);
			sb.Append(HighRain24HrDateTime.HasValue ? HighRain24HrDateTime.Value.ToString(timForm, invDate) : blank);

			return sb.ToString();
		}

		public bool FromString(string[] data)
		{
			var invDate = CultureInfo.InvariantCulture.DateTimeFormat;
			// Add the 9am (or 10am in summer) offset if required
			var hrInc = Program.cumulus.GetHourInc(Date);

			// Make sure we always have the correct number of fields
			var data2 = new string[Cumulus.DayfileFields];
			Array.Copy(data, data2, data.Length);
			var i = 0;
			Date = DateTime.ParseExact(data2[i++], "dd/MM/yy", invDate);
			Date = DateTime.SpecifyKind(Date, DateTimeKind.Local);

			HighGust = Utils.TryParseNullDouble(data2[i++]);
			HighGustBearing = Utils.TryParseNullInt(data2[i++]);
			HighGustDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			LowTemp = Utils.TryParseNullDouble(data2[i++]);
			LowTempDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighTemp = Utils.TryParseNullDouble(data2[i++]);
			HighTempDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			LowPress = Utils.TryParseNullDouble(data2[i++]);
			LowPressDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighPress = Utils.TryParseNullDouble(data2[i++]);
			HighPressDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighRainRate = Utils.TryParseNullDouble(data2[i++]);
			HighRainRateDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			TotalRain = Utils.TryParseNullDouble(data2[i++]);
			AvgTemp = Utils.TryParseNullDouble(data2[i++]);
			WindRun = Utils.TryParseNullDouble(data2[i++]);
			HighAvgWind = Utils.TryParseNullDouble(data2[i++]);
			HighAvgWindDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			LowHumidity = Utils.TryParseNullInt(data2[i++]);
			LowHumidityDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighHumidity = Utils.TryParseNullInt(data2[i++]);
			HighHumidityDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			ET = Utils.TryParseNullDouble(data2[i++]);
			SunShineHours = Utils.TryParseNullDouble(data2[i++]);
			HighHeatIndex = Utils.TryParseNullDouble(data2[i++]);
			HighHeatIndexDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighAppTemp = Utils.TryParseNullDouble(data2[i++]);
			HighAppTempDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			LowAppTemp = Utils.TryParseNullDouble(data2[i++]);
			LowAppTempDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighHourlyRain = Utils.TryParseNullDouble(data2[i++]);
			HighHourlyRainDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			LowWindChill = Utils.TryParseNullDouble(data2[i++]);
			LowWindChillDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighDewPoint = Utils.TryParseNullDouble(data2[i++]);
			HighDewPointDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			LowDewPoint = Utils.TryParseNullDouble(data2[i++]);
			LowDewPointDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			DominantWindBearing = Utils.TryParseNullInt(data2[i++]);
			HeatingDegreeDays = Utils.TryParseNullDouble(data2[i++]);
			CoolingDegreeDays = Utils.TryParseNullDouble(data2[i++]);
			HighSolar = Utils.TryParseNullInt(data2[i++]);
			HighSolarDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighUv = Utils.TryParseNullDouble(data2[i++]);
			HighUvDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighFeelsLike = Utils.TryParseNullDouble(data2[i++]);
			HighFeelsLikeDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			LowFeelsLike = Utils.TryParseNullDouble(data2[i++]);
			LowFeelsLikeDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			HighHumidex = Utils.TryParseNullDouble(data2[i++]);
			HighHumidexDateTime = Utils.AddTimeToDate(Date, data2[i++], hrInc);
			ChillHours = Utils.TryParseNullDouble(data2[i++]);
			HighRain24Hours = Utils.TryParseNullDouble(data[i++]);
			HighRain24HrDateTime = Utils.AddTimeToDate(Date, data[i++], hrInc);

			return true;
		}

		public bool ParseDayFileRecV4(string data)
		{
			var st = new List<string>(data.Split(','));
			var invNum = CultureInfo.InvariantCulture.NumberFormat;
			double varDbl;
			int varInt;
			int idx = 0;
			// Add the 9am (or 10am in summer) offset if required
			var hrInc = Program.cumulus.GetHourInc(Date);


			try
			{
				Date = Utils.ddmmyyStrToDate(st[idx++]);
				HighGust = Convert.ToDouble(st[idx++], invNum);
				HighGustBearing = Convert.ToInt32(st[idx++]);
				HighGustDateTime = Utils.AddTimeToDate(Date, st[idx++], hrInc);
				LowTemp = Convert.ToDouble(st[idx++], invNum);
				LowTempDateTime = Utils.AddTimeToDate(Date, st[idx++], hrInc);
				HighTemp = Convert.ToDouble(st[idx++], invNum);
				HighTempDateTime = Utils.AddTimeToDate(Date, st[idx++], hrInc);
				LowPress = Convert.ToDouble(st[idx++], invNum);
				LowPressDateTime = Utils.AddTimeToDate(Date, st[idx++], hrInc);
				HighPress = Convert.ToDouble(st[idx++], invNum);
				HighPressDateTime = Utils.AddTimeToDate(Date, st[idx++], hrInc);
				HighRainRate = Convert.ToDouble(st[idx++], invNum);
				HighRainRateDateTime = Utils.AddTimeToDate(Date, st[idx++], hrInc);
				TotalRain = Convert.ToDouble(st[idx++], invNum);
				AvgTemp = Convert.ToDouble(st[idx++], invNum);

				if (st.Count > idx++)
					WindRun = Utils.TryParseNullDouble(st[idx - 1]);

				if (st.Count > idx++)
					HighAvgWind = Utils.TryParseNullDouble(st[idx - 1]);

				if (st.Count > idx++)
					HighAvgWindDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++)
					LowHumidity = Utils.TryParseNullInt(st[idx - 1]);

				if (st.Count > idx++)
					LowHumidityDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++)
					HighHumidity = Utils.TryParseNullInt(st[idx - 1]);

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighHumidityDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++)
					ET = Utils.TryParseNullDouble(st[idx - 1]);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					SunShineHours = varDbl;

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighHeatIndex = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighHeatIndexDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighAppTemp = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighAppTempDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					LowAppTemp = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					LowAppTempDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighHourlyRain = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighHourlyRainDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					LowWindChill = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					LowWindChillDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighDewPoint = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighDewPointDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					LowDewPoint = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					LowDewPointDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && int.TryParse(st[idx - 1], out varInt))
					DominantWindBearing = varInt;

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HeatingDegreeDays = varDbl;

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					CoolingDegreeDays = varDbl;

				if (st.Count > idx++ && int.TryParse(st[idx - 1], out varInt))
					HighSolar = varInt;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighSolarDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighUv = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighUvDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighFeelsLike = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighFeelsLikeDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					LowFeelsLike = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					LowFeelsLikeDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighHumidex = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighHumidexDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					ChillHours = varDbl;

				if (st.Count > idx++ && double.TryParse(st[idx - 1], NumberStyles.Float, invNum, out varDbl))
					HighRain24Hours = varDbl;

				if (st.Count > idx++ && st[idx - 1].Length == 5)
					HighRain24HrDateTime = Utils.AddTimeToDate(Date, st[idx - 1], hrInc);
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
