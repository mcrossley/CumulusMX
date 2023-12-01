using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CumulusMX
{
	[DataContract]
	public class WebSocketData(Cumulus cumulus, double? outdoorTemp, int? outdoorHum, double avgTempToday, double? indoorTemp, double? outdoorDewpoint, double? windChill,
						int? indoorHum, double? pressure, double? windLatest, double? windAverage, double? recentmaxgust, double windRunToday, int? bearing, int? avgbearing,
						double? rainToday, double? rainYesterday, double rainMonth, double rainYear, double? rainRate, double rainLastHour, double? heatIndex, double? humidex,
						double? appTemp, double tempTrend, double pressTrend, double? highGustToday, string highGustTodayTime, double? highWindToday, int? highGustBearingToday,
						string windUnit, int bearingRangeFrom10, int bearingRangeTo10, string windRoseData, double? highTempToday, double? lowTempToday, string highTempTodayToday,
						string lowTempTodayTime, double? highPressToday, double? lowPressToday, string highPressTodayTime, string lowPressTodayTime, double? highRainRateToday,
						string highRainRateTodayTime, int? highHumToday, int? lowHumToday, string highHumTodayTime, string lowHumTodayTime, string pressUnit, string tempUnit,
						string rainUnit, double? highDewpointToday, double? lowDewpointToday, string highDewpointTodayTime, string lowDewpointTodayTime, double? lowWindChillToday,
						string lowWindChillTodayTime, int? solarRad, int? highSolarRadToday, string highSolarRadTodayTime, double? uvindex, double? highUVindexToday,
						string highUVindexTodayTime, string forecast, string sunrise, string sunset, string moonrise, string moonset, double? highHeatIndexToday,
						string highHeatIndexTodayTime, double? highAppTempToday, double? lowAppTempToday, string highAppTempTodayTime, string lowAppTempTodayTime,
						int? currentSolarMax, double alltimeHighPressure, double alltimeLowPressure, double sunshineHours, string domWindDir, string lastRainTipISO,
						double highHourlyRainToday, string highHourlyRainTodayTime, string highBeaufortToday, string beaufort, string beaufortDesc, string lastDataRead,
						bool dataStopped, double stormRain, string stormRainStart, int? cloudbase, string cloudbaseUnit, double last24hourRain,
						double? feelsLike, double? highFeelsLikeToday, string highFeelsLikeTodayTime, double? lowFeelsLikeToday, string lowFeelsLikeTodayTime,
						double? highHumidexToday, string highHumidexTodayTime, List<DashboardAlarms> alarms) // The annotations on this class are so it can be serialised as JSON
	{
		private readonly Cumulus cumulus = cumulus;
		private static readonly string nullVal = "-";
		private static readonly string nullTime = "--:--";

		[IgnoreDataMember]
		public double StormRain { get; set; } = stormRain;

		[DataMember(Name = "StormRain")]
		public string StormRainRounded
		{
			get => StormRain.ToString(cumulus.RainFormat);
			set { }
		}

		[DataMember]
		public string StormRainStart { get; set; } = stormRainStart;

		[IgnoreDataMember]
		public int? CurrentSolarMax { get; set; } = currentSolarMax;

		[DataMember(Name = "CurrentSolarMax")]
		public string CurrentSolarMaxStr
		{
			get => CurrentSolarMax.HasValue ? CurrentSolarMax.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighHeatIndexToday { get; set; } = highHeatIndexToday;

		[DataMember(Name = "HighHeatIndexToday")]
		public string HighHeatIndexTodayStr
		{
			get => HighHeatIndexToday.HasValue ? HighHeatIndexToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighHeatIndexTodayTime { get; set; } = highHeatIndexTodayTime;

		[DataMember (Name = "HighHeatIndexTodayTime")]
		public string HighHeatIndexTodayTimeStr
		{
			get => HighHeatIndexToday.HasValue ? HighHeatIndexTodayTime : nullTime;
			set { }
		}


		[DataMember]
		public string Sunrise { get; set; } = sunrise;

		[DataMember]
		public string Sunset { get; set; } = sunset;

		[DataMember]
		public string Moonrise { get; set; } = moonrise;

		[DataMember]
		public string Moonset { get; set; } = moonset;

		[DataMember]
		public string Forecast { get; set; } = forecast;

		[IgnoreDataMember]
		public double? UVindex { get; set; } = uvindex;

		[DataMember(Name = "UVindex")]
		public string UVindexStr
		{
			get => UVindex.HasValue ? UVindex.Value.ToString(cumulus.UVFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighUVindexToday { get; set; } = highUVindexToday;

		[DataMember(Name = "HighUVindexToday")]
		public string HighUVindexTodayStr
		{
			get => HighUVindexToday.HasValue ? HighUVindexToday.Value.ToString(cumulus.UVFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighUVindexTodayTime { get; set; } = highUVindexTodayTime;

		[DataMember(Name = "HighUVindexTodayTime")]
		public string HighUVindexTodayTimeStr
		{
			get => HighUVindexToday.HasValue ? HighUVindexTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighSolarRadTodayTime { get; set; } = highSolarRadTodayTime;

		[DataMember(Name = "HighSolarRadTodayTime")]
		public string HighSolarRadTodayTimeStr
		{
			get => HighSolarRadToday.HasValue ? HighSolarRadTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public int? HighSolarRadToday { get; set; } = highSolarRadToday;

		[DataMember(Name = "HighSolarRadToday")]
		public string HighSolarRadTodayStr
		{
			get => HighSolarRadToday.HasValue ? HighSolarRadToday.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? SolarRad { get; set; } = solarRad;

		[DataMember(Name = "SolarRad")]
		public string SolarRadStr
		{
			get => SolarRad.HasValue ? SolarRad.Value.ToString() : nullVal;
			set { }
		}


		[IgnoreDataMember]
		public double? IndoorTemp { get; set; } = indoorTemp;

		[DataMember(Name = "IndoorTemp")]
		public string IndoorTempRounded
		{
			get => IndoorTemp.HasValue ? IndoorTemp.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? OutdoorDewpoint { get; set; } = outdoorDewpoint;

		[DataMember(Name = "OutdoorDewpoint")]
		public string OutdoorDewpointRounded
		{
			get => OutdoorDewpoint.HasValue ? OutdoorDewpoint.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? LowDewpointToday { get; set; } = lowDewpointToday;

		[DataMember(Name = "LowDewpointToday")]
		public string LowDewpointTodayRounded
		{
			get => LowDewpointToday.HasValue ? LowDewpointToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighDewpointToday { get; set; } = highDewpointToday;

		[DataMember(Name = "HighDewpointToday")]
		public string HighDewpointTodayRounded
		{
			get => HighDewpointToday.HasValue ? HighDewpointToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowDewpointTodayTime { get; set; } = lowDewpointTodayTime;

		[DataMember(Name = "LowDewpointTodayTime")]
		public string LowDewpointTodayTimeStr
		{
			get => LowDewpointToday.HasValue ? LowDewpointTodayTime : nullTime;
			set { }
		}


		[IgnoreDataMember]
		public string HighDewpointTodayTime { get; set; } = highDewpointTodayTime;

		[DataMember(Name = "HighDewpointTodayTime")]
		public string HighDewpointTodayTimeStr
		{
			get => HighDewpointToday.HasValue ? HighDewpointTodayTime : nullTime;
			set { }
		}


		[IgnoreDataMember]
		public double? WindChill { get; set; } = windChill;

		[DataMember(Name = "WindChill")]
		public string WindChillRounded
		{
			get => WindChill.HasValue ? WindChill.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? LowWindChillToday { get; set; } = lowWindChillToday;

		[DataMember(Name = "LowWindChillToday")]
		public string LowWindChillTodayRounded
		{
			get => LowWindChillToday.HasValue ? LowWindChillToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowWindChillTodayTime { get; set; } = lowWindChillTodayTime;

		[DataMember(Name = "LowWindChillTodayTime")]
		public string LowWindChillTodayTimeStr
		{
			get => LowWindChillToday.HasValue ? LowWindChillTodayTime : nullTime;
			set { }
		}

		[DataMember]
		public string WindUnit = windUnit;

		[DataMember]
		public string RainUnit { get; set; } = rainUnit;

		[DataMember]
		public string TempUnit { get; set; } = tempUnit;

		[DataMember]
		public string PressUnit { get; set; } = pressUnit;

		[DataMember]
		public string CloudbaseUnit { get; set; } = cloudbaseUnit;

		[IgnoreDataMember]
		public int? Cloudbase { get; set; } = cloudbase;

		[DataMember(Name="Cloudbase")]
		public string CloudbaseStr
		{
			get => Cloudbase.HasValue ? Cloudbase.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowHumTodayTime { get; set; } = lowHumTodayTime;

		[DataMember(Name = "LowHumTodayTime")]
		public string LowHumTodayTimeStr
		{
			get => LowHumToday.HasValue ? LowHumTodayTime : nullTime;
			set { }
		}


		[IgnoreDataMember]
		public string HighHumTodayTime { get; set; } = highHumTodayTime;

		[DataMember(Name = "HighHumTodayTime")]
		public string HighHumTodayTimeStr
		{
			get => HighHumToday.HasValue ? HighHumTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public int? LowHumToday { get; set; } = lowHumToday;

		[DataMember(Name = "LowHumToday")]
		public string LowHumTodayStr
		{
			get => LowHumToday.HasValue ? LowHumToday.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? HighHumToday { get; set; } = highHumToday;

		[DataMember(Name = "HighHumToday")]
		public string HighHumTodayStr
		{
			get => HighHumToday.HasValue ? HighHumToday.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighRainRateTodayTime { get; set; } = highRainRateTodayTime;

		[DataMember(Name = "HighRainRateTodayTime")]
		public string HighRainRateTodayTimeStr
		{
			get => HighRainRateToday.HasValue ? HighRainRateTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? HighRainRateToday { get; set; } = highRainRateToday;

		[DataMember(Name="HighRainRateToday")]
		public string HighRainRateTodayRounded
		{
			get => HighRainRateToday.HasValue ? HighRainRateToday.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[DataMember]
		public string HighHourlyRainTodayTime { get; set; } = highHourlyRainTodayTime;

		[IgnoreDataMember]
		public double HighHourlyRainToday { get; set; } = highHourlyRainToday;

		[DataMember(Name="HighHourlyRainToday")]
		public string HighHourlyRainTodayRounded
		{
			get => HighHourlyRainToday.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public string LowPressTodayTime { get; set; } = lowPressTodayTime;

		[DataMember(Name = "LowPressTodayTime")]
		public string LowPressTodayTimeStr
		{
			get => LowPressToday.HasValue ? LowPressTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighPressTodayTime { get; set; } = highPressTodayTime;

		[DataMember(Name = "HighPressTodayTime")]
		public string HighPressTodayTimeStr
		{
			get => HighPressToday.HasValue ? HighPressTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowPressToday { get; set; } = lowPressToday;

		[DataMember(Name="LowPressToday")]
		public string LowPressTodayRounded
		{
			get => LowPressToday.HasValue ? LowPressToday.Value.ToString(cumulus.PressFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighPressToday { get; set; } = highPressToday;

		[DataMember(Name="HighPressToday")]
		public string HighPressTodayRounded
		{
			get => HighPressToday.HasValue ? HighPressToday.Value.ToString(cumulus.PressFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowTempTodayTime { get; set; } = lowTempTodayTime;

		[DataMember(Name = "LowTempTodayTime")]
		public string LowTempTodayTimeStr
		{
			get => LowTempToday.HasValue ? LowTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighTempTodayTime { get; set; } = highTempTodayToday;

		[DataMember(Name = "HighTempTodayTime")]
		public string HighTempTodayTimeStr
		{
			get => HighTempToday.HasValue ? HighTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowTempToday { get; set; } = lowTempToday;

		[DataMember(Name = "LowTempToday")]
		public string LowTempTodayRounded
		{
			get => LowTempToday.HasValue ? LowTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighTempToday { get; set; } = highTempToday;

		[DataMember(Name = "HighTempToday")]
		public string HighTempTodayRounded
		{
			get => HighTempToday.HasValue ? HighTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[DataMember]
		public string WindRoseData { get; set; } = windRoseData;

		[DataMember]
		public int BearingRangeTo10 { get; set; } = bearingRangeTo10;

		[DataMember]
		public int BearingRangeFrom10 { get; set; } = bearingRangeFrom10;

		[IgnoreDataMember]
		public int? HighGustBearingToday { get; set; } = highGustBearingToday;

		[DataMember(Name = "HighGustBearingToday")]
		public string HighGustBearingTodayStr
		{
			get => HighGustBearingToday.HasValue ? HighGustBearingToday.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighWindToday { get; set; } = highWindToday;

		[DataMember(Name = "HighWindToday")]
		public string HighWindTodayRounded
		{
			get => HighWindToday.HasValue ? HighWindToday.Value.ToString(cumulus.WindAvgFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighGustTodayTime { get; set; } = highGustTodayTime;

		[DataMember(Name = "HighGustTodayTime")]
		public string HighGustTodayTimeStr
		{
			get => HighGustToday.HasValue ? HighGustTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? HighGustToday { get; set; } = highGustToday;

		[DataMember(Name = "HighGustToday")]
		public string HighGustTodayRounded
		{
			get => HighGustToday.HasValue ? HighGustToday.Value.ToString(cumulus.WindFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? OutdoorTemp { get; set; } = outdoorTemp;

		[DataMember(Name = "OutdoorTemp")]
		public string OutdoorTempRounded
		{
			get => OutdoorTemp.HasValue ? OutdoorTemp.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? OutdoorHum { get; set; } = outdoorHum;

		[DataMember(Name = "OutdoorHum")]
		public string OutdoorHumFormatted
		{
			get => OutdoorHum.HasValue ? OutdoorHum.Value.ToString() : nullVal;
			set { }
		}


		[IgnoreDataMember]
		public double AvgTempToday { get; set; } = avgTempToday;

		[DataMember(Name = "AvgTempToday")]
		public string AvgTempRounded
		{
			get => AvgTempToday.ToString(cumulus.TempFormat);
			set { }
		}

		[IgnoreDataMember]
		public int? IndoorHum { get; set; } = indoorHum;
		[DataMember(Name = "IndoorHum")]
		public string IndoorHumStr
		{
			get => IndoorHum.HasValue ? IndoorHum.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? Pressure { get; set; } = pressure;

		[DataMember(Name = "Pressure")]
		public string PressureRounded
		{
			get => Pressure.HasValue ? Pressure.Value.ToString(cumulus.PressFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double AlltimeHighPressure { get; set; } = alltimeHighPressure;

		[DataMember(Name = "AlltimeHighPressure")]
		public string AlltimeHighPressureRounded
		{
			get => AlltimeHighPressure.ToString(cumulus.PressFormat);
			set { }
		}

		[IgnoreDataMember]
		public double AlltimeLowPressure { get; set; } = alltimeLowPressure;

		[DataMember(Name = "AlltimeLowPressure")]
		public string AlltimeLowPressureRounded
		{
			get => AlltimeLowPressure.ToString(cumulus.PressFormat);
			set { }
		}

		[IgnoreDataMember]
		public double? WindLatest { get; set; } = windLatest;

		[DataMember(Name = "WindLatest")]
		public string WindLatestRounded
		{
			get => WindLatest.HasValue ? WindLatest.Value.ToString(cumulus.WindFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? WindAverage { get; set; } = windAverage;

		[DataMember(Name = "WindAverage")]
		public string WindAverageRounded
		{
			get => WindAverage.HasValue ? WindAverage.Value.ToString(cumulus.WindAvgFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? Recentmaxgust { get; set; } = recentmaxgust;

		[DataMember(Name = "Recentmaxgust")]
		public string RecentmaxgustRounded
		{
			get => Recentmaxgust.HasValue ? Recentmaxgust.Value.ToString(cumulus.WindFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double WindRunToday { get; set; } = windRunToday;

		[DataMember(Name = "WindRunToday")]
		public string WindRunTodayRounded
		{
			get => WindRunToday.ToString(cumulus.WindRunFormat);
			set { }
		}

		[IgnoreDataMember]
		public int? Bearing { get; set; } = bearing;

		[DataMember(Name = "Bearing")]
		public string BearingFormatted
		{
			get => Bearing.HasValue ? Bearing.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? Avgbearing { get; set; } = avgbearing;
		[DataMember(Name = "Avgbearing")]
		public string AvgbearingFormatted
		{
			get => Avgbearing.HasValue ? Avgbearing.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? RainToday { get; set; } = rainToday;

		[DataMember(Name = "RainToday")]
		public string RainTodayRounded
		{
			get => RainToday.HasValue ? RainToday.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? RainYesterday { get; set; } = rainYesterday;

		[DataMember(Name = "RainYesterday")]
		public string RainYesterdayRounded
		{
			get => RainYesterday.HasValue ? RainYesterday.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double RainMonth { get; set; } = rainMonth;

		[DataMember(Name = "RainMonth")]
		public string RainMonthRounded
		{
			get => RainMonth.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double RainYear { get; set; } = rainYear;

		[DataMember(Name = "RainYear")]
		public string RainYearRounded
		{
			get => RainYear.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double? RainRate { get; set; } = rainRate;

		[DataMember(Name = "RainRate")]
		public string RainRateRounded
		{
			get => RainRate.HasValue ? RainRate.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double RainLastHour { get; set; } = rainLastHour;

		[DataMember(Name = "RainLastHour")]
		public string RainLastHourRounded
		{
			get => RainLastHour.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double RainLast24Hour { get; set; } = last24hourRain;

		[DataMember(Name = "RainLast24Hour")]
		public string RainLast24HourRounded
		{
			get => RainLast24Hour.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double? HeatIndex { get; set; } = heatIndex;

		[DataMember(Name = "HeatIndex")]
		public string HeatIndexRounded
		{
			get => HeatIndex.HasValue ? HeatIndex.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? Humidex { get; set; } = humidex;

		[DataMember(Name = "Humidex")]
		public string HumidexRounded
		{
			get => Humidex.HasValue ? Humidex.Value.ToString(cumulus.TempFormat) : "-";
			set { }
		}

		[IgnoreDataMember]
		public string HighHumidexTodayTime { get; set; } = highHumidexTodayTime;

		[DataMember(Name = "HighHumidexTodayTime")]
		public string HighHumidexTodayTimeStr
		{
			get => HighHumidexToday.HasValue ? HighHumidexTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? HighHumidexToday { get; set; } = highHumidexToday;

		[DataMember(Name = "HighHumidexToday")]
		public string HighHumidexTodayRounded
		{
			get => HighHumidexToday.HasValue ? HighHumidexToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? AppTemp { get; set; } = appTemp;

		[DataMember(Name = "AppTemp")]
		public string AppTempRounded
		{
			get => AppTemp.HasValue ? AppTemp.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowAppTempTodayTime { get; set; } = lowAppTempTodayTime;

		[DataMember(Name = "LowAppTempTodayTime")]
		public string LowAppTempTodayTimeStr
		{
			get => LowAppTempToday.HasValue ? LowAppTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighAppTempTodayTime { get; set; } = highAppTempTodayTime;

		[DataMember(Name = "HighAppTempTodayTime")]
		public string HighAppTempTodayTimeStr
		{
			get => HighAppTempToday.HasValue ? HighAppTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowAppTempToday { get; set; } = lowAppTempToday;

		[DataMember(Name = "LowAppTempToday")]
		public string LowAppTempTodayRounded
		{
			get => LowAppTempToday.HasValue ? LowAppTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighAppTempToday { get; set; } = highAppTempToday;

		[DataMember(Name = "HighAppTempToday")]
		public string HighAppTempTodayRounded
		{
			get => HighAppTempToday.HasValue ? HighAppTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? FeelsLike { get; set; } = feelsLike;

		[DataMember(Name = "FeelsLike")]
		public string FeelsLikeRounded
		{
			get => FeelsLike.HasValue ? FeelsLike.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowFeelsLikeTodayTime { get; set; } = lowFeelsLikeTodayTime;

		[DataMember(Name = "LowFeelsLikeTodayTime")]
		public string LowFeelsLikeTodayTimeStr
		{
			get => LowFeelsLikeToday.HasValue ? LowFeelsLikeTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighFeelsLikeTodayTime { get; set; } = highFeelsLikeTodayTime;

		[DataMember(Name = "HighFeelsLikeTodayTime")]
		public string HighFeelsLikeTodayTimeStr
		{
			get => HighFeelsLikeToday.HasValue ? HighFeelsLikeTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowFeelsLikeToday { get; set; } = lowFeelsLikeToday;

		[DataMember(Name = "LowFeelsLikeToday")]
		public string LowFeelsLikeTodayRounded
		{
			get => LowFeelsLikeToday.HasValue ? LowFeelsLikeToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighFeelsLikeToday { get; set; } = highFeelsLikeToday;

		[DataMember(Name = "HighFeelsLikeToday")]
		public string HighFeelsLikeTodayRounded
		{
			get => HighFeelsLikeToday.HasValue ? HighFeelsLikeToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double TempTrend { get; set; } = tempTrend;

		[DataMember(Name = "TempTrend")]
		public string TempTrendRounded
		{
			get => TempTrend.ToString(cumulus.TempTrendFormat);
			set { }
		}

		[IgnoreDataMember]
		public double PressTrend { get; set; } = pressTrend;

		[DataMember(Name = "PressTrend")]
		public string PressTrendRounded
		{
			get => PressTrend.ToString(cumulus.PressTrendFormat);
			set { }
		}

		[IgnoreDataMember]
		public double SunshineHours { get; set; } = sunshineHours;

		[DataMember(Name = "SunshineHours")]
		public string SunshineHoursRounded
		{
			get => SunshineHours.ToString(cumulus.SunFormat);
			set { }
		}

		[DataMember]
		public string Version
		{
			get => cumulus.Version;
			set { }
		}

		[DataMember]
		public string Build
		{
			get => cumulus.Build;
			set { }
		}

		[DataMember]
		public string DominantWindDirection { get; set; } = domWindDir;

		[DataMember]
		public string LastRainTipISO { get; set; } = lastRainTipISO;

		[DataMember]
		public string HighBeaufortToday { get; set; } = highBeaufortToday;

		[DataMember]
		public string Beaufort { get; set; } = beaufort;

		[DataMember]
		public string BeaufortDesc { get; set; } = beaufortDesc;

		[DataMember]
		public string LastDataRead { get; set; } = lastDataRead;

		[DataMember]
		public bool DataStopped { get; set; } = dataStopped;

		[DataMember]
		public List<DashboardAlarms> Alarms { get; set; } = alarms;
	}
}
