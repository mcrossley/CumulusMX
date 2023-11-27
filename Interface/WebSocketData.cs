using System.Runtime.Serialization;

namespace CumulusMX
{
	[DataContract]
	public class WebSocketData // The annotations on this class are so it can be serialised as JSON
	{
		private readonly Cumulus cumulus;
		private static readonly string nullVal = "-";
		private static readonly string nullTime = "--:--";

		public WebSocketData(Cumulus cumulus, double? outdoorTemp, int? outdoorHum, double avgTempToday, double? indoorTemp, double? outdoorDewpoint, double? windChill,
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
							bool dataStopped, double stormRain, string stormRainStart, int? cloudbase, string cloudbaseUnit, double last24hourRain, bool alarmLowTemp,
							bool alarmHighTemp, bool alarmTempUp, bool alarmTempDown, bool alarmRain, bool alarmRainRate, bool alarmLowPress, bool alarmHighPress,
							bool alarmPressUp, bool alarmPressDown, bool alarmGust, bool alarmWind, bool alarmSensor, bool alarmBattery, bool alarmSpike, bool alarmUpgrade,
							bool alarmHttp, bool alarmMySql, bool alarmRaining,
							double? feelsLike, double? highFeelsLikeToday, string highFeelsLikeTodayTime, double? lowFeelsLikeToday, string lowFeelsLikeTodayTime,
							double? highHumidexToday, string highHumidexTodayTime, bool alarmRecord, bool alarmFtp)
		{
			this.cumulus = cumulus;
			OutdoorTemp = outdoorTemp;
			HighTempToday = highTempToday;
			LowTempToday = lowTempToday;
			HighTempTodayTime = highTempTodayToday;
			LowTempTodayTime = lowTempTodayTime;
			OutdoorHum = outdoorHum;
			AvgTempToday = avgTempToday;
			IndoorTemp = indoorTemp;
			OutdoorDewpoint = outdoorDewpoint;
			WindChill = windChill;
			LowWindChillToday = lowWindChillToday;
			LowWindChillTodayTime = lowWindChillTodayTime;
			IndoorHum = indoorHum;
			Pressure = pressure;
			HighPressToday = highPressToday;
			LowPressToday = lowPressToday;
			HighPressTodayTime = highPressTodayTime;
			LowPressTodayTime = lowPressTodayTime;
			WindLatest = windLatest;
			WindAverage = windAverage;
			Recentmaxgust = recentmaxgust;
			WindRunToday = windRunToday;
			Bearing = bearing;
			Avgbearing = avgbearing;
			RainToday = rainToday;
			RainYesterday = rainYesterday;
			RainMonth = rainMonth;
			RainYear = rainYear;
			RainRate = rainRate;
			HighRainRateToday = highRainRateToday;
			HighRainRateTodayTime = highRainRateTodayTime;
			HighHourlyRainToday = highHourlyRainToday;
			HighHourlyRainTodayTime = highHourlyRainTodayTime;
			RainLastHour = rainLastHour;
			RainLast24Hour = last24hourRain;
			HeatIndex = heatIndex;
			HighHeatIndexToday = highHeatIndexToday;
			HighHeatIndexTodayTime = highHeatIndexTodayTime;
			Humidex = humidex;
			HighHumidexToday = highHumidexToday;
			HighHumidexTodayTime = highHumidexTodayTime;
			AppTemp = appTemp;
			HighAppTempToday = highAppTempToday;
			LowAppTempToday = lowAppTempToday;
			HighAppTempTodayTime = highAppTempTodayTime;
			LowAppTempTodayTime = lowAppTempTodayTime;
			FeelsLike = feelsLike;
			HighFeelsLikeToday = highFeelsLikeToday;
			LowFeelsLikeToday = lowFeelsLikeToday;
			HighFeelsLikeTodayTime = highFeelsLikeTodayTime;
			LowFeelsLikeTodayTime = lowFeelsLikeTodayTime;
			TempTrend = tempTrend;
			PressTrend = pressTrend;
			HighGustToday = highGustToday;
			HighGustTodayTime = highGustTodayTime;
			HighWindToday = highWindToday;
			HighGustBearingToday = highGustBearingToday;
			BearingRangeFrom10 = bearingRangeFrom10;
			BearingRangeTo10 = bearingRangeTo10;
			WindRoseData = windRoseData;
			WindUnit = windUnit;
			PressUnit = pressUnit;
			TempUnit = tempUnit;
			RainUnit = rainUnit;
			HighHumToday = highHumToday;
			LowHumToday = lowHumToday;
			HighHumTodayTime = highHumTodayTime;
			LowHumTodayTime = lowHumTodayTime;
			HighDewpointToday = highDewpointToday;
			LowDewpointToday = lowDewpointToday;
			HighDewpointTodayTime = highDewpointTodayTime;
			LowDewpointTodayTime = lowDewpointTodayTime;
			SolarRad = solarRad;
			HighSolarRadToday = highSolarRadToday;
			HighSolarRadTodayTime = highSolarRadTodayTime;
			CurrentSolarMax = currentSolarMax;
			UVindex = uvindex;
			HighUVindexToday = highUVindexToday;
			HighUVindexTodayTime = highUVindexTodayTime;
			AlltimeHighPressure = alltimeHighPressure;
			AlltimeLowPressure = alltimeLowPressure;
			SunshineHours = sunshineHours;
			Forecast = forecast;
			Sunrise = sunrise;
			Sunset = sunset;
			Moonrise = moonrise;
			Moonset = moonset;
			DominantWindDirection = domWindDir;
			LastRainTipISO = lastRainTipISO;
			HighBeaufortToday = highBeaufortToday;
			Beaufort = beaufort;
			BeaufortDesc = beaufortDesc;
			LastDataRead = lastDataRead;
			DataStopped = dataStopped;
			StormRain = stormRain;
			StormRainStart = stormRainStart;
			Cloudbase = cloudbase;
			CloudbaseUnit = cloudbaseUnit;
			AlarmLowTemp = alarmLowTemp;
			AlarmHighTemp = alarmHighTemp;
			AlarmTempUp = alarmTempUp;
			AlarmTempDn = alarmTempDown;
			AlarmRain = alarmRain;
			AlarmRainRate = alarmRainRate;
			AlarmLowPress = alarmLowPress;
			AlarmHighPress = alarmHighPress;
			AlarmPressUp = alarmPressUp;
			AlarmPressDn = alarmPressDown;
			AlarmGust = alarmGust;
			AlarmWind = alarmWind;
			AlarmSensor = alarmSensor;
			AlarmBattery = alarmBattery;
			AlarmSpike = alarmSpike;
			AlarmUpgrade = alarmUpgrade;
			AlarmHttp = alarmHttp;
			AlarmMySql = alarmMySql;
			AlarmIsRaining = alarmRaining;
			AlarmNewRec = alarmRecord;
			AlarmFtp = alarmFtp;
		}

		[IgnoreDataMember]
		public double StormRain { get; set; }

		[DataMember(Name = "StormRain")]
		public string StormRainRounded
		{
			get => StormRain.ToString(cumulus.RainFormat);
			set { }
		}

		[DataMember]
		public string StormRainStart { get; set; }

		[IgnoreDataMember]
		public int? CurrentSolarMax { get; set; }

		[DataMember(Name = "CurrentSolarMax")]
		public string CurrentSolarMaxStr
		{
			get => CurrentSolarMax.HasValue ? CurrentSolarMax.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighHeatIndexToday { get; set; }

		[DataMember(Name = "HighHeatIndexToday")]
		public string HighHeatIndexTodayStr
		{
			get => HighHeatIndexToday.HasValue ? HighHeatIndexToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighHeatIndexTodayTime { get; set; }

		[DataMember (Name = "HighHeatIndexTodayTime")]
		public string HighHeatIndexTodayTimeStr
		{
			get => HighHeatIndexToday.HasValue ? HighHeatIndexTodayTime : nullTime;
			set { }
		}


		[DataMember]
		public string Sunrise { get; set; }

		[DataMember]
		public string Sunset { get; set; }

		[DataMember]
		public string Moonrise { get; set; }

		[DataMember]
		public string Moonset { get; set; }

		[DataMember]
		public string Forecast { get; set; }

		[IgnoreDataMember]
		public double? UVindex { get; set; }

		[DataMember(Name = "UVindex")]
		public string UVindexStr
		{
			get => UVindex.HasValue ? UVindex.Value.ToString(cumulus.UVFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighUVindexToday { get; set; }

		[DataMember(Name = "HighUVindexToday")]
		public string HighUVindexTodayStr
		{
			get => HighUVindexToday.HasValue ? HighUVindexToday.Value.ToString(cumulus.UVFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighUVindexTodayTime { get; set; }

		[DataMember(Name = "HighUVindexTodayTime")]
		public string HighUVindexTodayTimeStr
		{
			get => HighUVindexToday.HasValue ? HighUVindexTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighSolarRadTodayTime { get; set; }

		[DataMember(Name = "HighSolarRadTodayTime")]
		public string HighSolarRadTodayTimeStr
		{
			get => HighSolarRadToday.HasValue ? HighSolarRadTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public int? HighSolarRadToday { get; set; }

		[DataMember(Name = "HighSolarRadToday")]
		public string HighSolarRadTodayStr
		{
			get => HighSolarRadToday.HasValue ? HighSolarRadToday.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? SolarRad { get; set; }

		[DataMember(Name = "SolarRad")]
		public string SolarRadStr
		{
			get => SolarRad.HasValue ? SolarRad.Value.ToString() : nullVal;
			set { }
		}


		[IgnoreDataMember]
		public double? IndoorTemp { get; set; }

		[DataMember(Name = "IndoorTemp")]
		public string IndoorTempRounded
		{
			get => IndoorTemp.HasValue ? IndoorTemp.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? OutdoorDewpoint { get; set; }

		[DataMember(Name = "OutdoorDewpoint")]
		public string OutdoorDewpointRounded
		{
			get => OutdoorDewpoint.HasValue ? OutdoorDewpoint.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? LowDewpointToday { get; set; }

		[DataMember(Name = "LowDewpointToday")]
		public string LowDewpointTodayRounded
		{
			get => LowDewpointToday.HasValue ? LowDewpointToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighDewpointToday { get; set; }

		[DataMember(Name = "HighDewpointToday")]
		public string HighDewpointTodayRounded
		{
			get => HighDewpointToday.HasValue ? HighDewpointToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowDewpointTodayTime { get; set; }

		[DataMember(Name = "LowDewpointTodayTime")]
		public string LowDewpointTodayTimeStr
		{
			get => LowDewpointToday.HasValue ? LowDewpointTodayTime : nullTime;
			set { }
		}


		[IgnoreDataMember]
		public string HighDewpointTodayTime { get; set; }

		[DataMember(Name = "HighDewpointTodayTime")]
		public string HighDewpointTodayTimeStr
		{
			get => HighDewpointToday.HasValue ? HighDewpointTodayTime : nullTime;
			set { }
		}


		[IgnoreDataMember]
		public double? WindChill { get; set; }

		[DataMember(Name = "WindChill")]
		public string WindChillRounded
		{
			get => WindChill.HasValue ? WindChill.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? LowWindChillToday { get; set; }

		[DataMember(Name = "LowWindChillToday")]
		public string LowWindChillTodayRounded
		{
			get => LowWindChillToday.HasValue ? LowWindChillToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowWindChillTodayTime { get; set; }

		[DataMember(Name = "LowWindChillTodayTime")]
		public string LowWindChillTodayTimeStr
		{
			get => LowWindChillToday.HasValue ? LowWindChillTodayTime : nullTime;
			set { }
		}

		[DataMember]
		public string WindUnit;

		[DataMember]
		public string RainUnit { get; set; }

		[DataMember]
		public string TempUnit { get; set; }

		[DataMember]
		public string PressUnit { get; set; }

		[DataMember]
		public string CloudbaseUnit { get; set; }

		[IgnoreDataMember]
		public int? Cloudbase { get; set; }

		[DataMember(Name="Cloudbase")]
		public string CloudbaseStr
		{
			get => Cloudbase.HasValue ? Cloudbase.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowHumTodayTime { get; set; }

		[DataMember(Name = "LowHumTodayTime")]
		public string LowHumTodayTimeStr
		{
			get => LowHumToday.HasValue ? LowHumTodayTime : nullTime;
			set { }
		}


		[IgnoreDataMember]
		public string HighHumTodayTime { get; set; }

		[DataMember(Name = "HighHumTodayTime")]
		public string HighHumTodayTimeStr
		{
			get => HighHumToday.HasValue ? HighHumTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public int? LowHumToday { get; set; }

		[DataMember(Name = "LowHumToday")]
		public string LowHumTodayStr
		{
			get => LowHumToday.HasValue ? LowHumToday.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? HighHumToday { get; set; }

		[DataMember(Name = "HighHumToday")]
		public string HighHumTodayStr
		{
			get => HighHumToday.HasValue ? HighHumToday.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighRainRateTodayTime { get; set; }

		[DataMember(Name = "HighRainRateTodayTime")]
		public string HighRainRateTodayTimeStr
		{
			get => HighRainRateToday.HasValue ? HighRainRateTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? HighRainRateToday { get; set; }

		[DataMember(Name="HighRainRateToday")]
		public string HighRainRateTodayRounded
		{
			get => HighRainRateToday.HasValue ? HighRainRateToday.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[DataMember]
		public string HighHourlyRainTodayTime { get; set; }

		[IgnoreDataMember]
		public double HighHourlyRainToday { get; set; }

		[DataMember(Name="HighHourlyRainToday")]
		public string HighHourlyRainTodayRounded
		{
			get => HighHourlyRainToday.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public string LowPressTodayTime { get; set; }

		[DataMember(Name = "LowPressTodayTime")]
		public string LowPressTodayTimeStr
		{
			get => LowPressToday.HasValue ? LowPressTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighPressTodayTime { get; set; }

		[DataMember(Name = "HighPressTodayTime")]
		public string HighPressTodayTimeStr
		{
			get => HighPressToday.HasValue ? HighPressTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowPressToday { get; set; }

		[DataMember(Name="LowPressToday")]
		public string LowPressTodayRounded
		{
			get => LowPressToday.HasValue ? LowPressToday.Value.ToString(cumulus.PressFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighPressToday { get; set; }

		[DataMember(Name="HighPressToday")]
		public string HighPressTodayRounded
		{
			get => HighPressToday.HasValue ? HighPressToday.Value.ToString(cumulus.PressFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowTempTodayTime { get; set; }

		[DataMember(Name = "LowTempTodayTime")]
		public string LowTempTodayTimeStr
		{
			get => LowTempToday.HasValue ? LowTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighTempTodayTime { get; set; }

		[DataMember(Name = "HighTempTodayTime")]
		public string HighTempTodayTimeStr
		{
			get => HighTempToday.HasValue ? HighTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowTempToday { get; set; }

		[DataMember(Name = "LowTempToday")]
		public string LowTempTodayRounded
		{
			get => LowTempToday.HasValue ? LowTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighTempToday { get; set; }

		[DataMember(Name = "HighTempToday")]
		public string HighTempTodayRounded
		{
			get => HighTempToday.HasValue ? HighTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[DataMember]
		public string WindRoseData { get; set; }

		[DataMember]
		public int BearingRangeTo10 { get; set; }

		[DataMember]
		public int BearingRangeFrom10 { get; set; }

		[IgnoreDataMember]
		public int? HighGustBearingToday { get; set; }

		[DataMember(Name = "HighGustBearingToday")]
		public string HighGustBearingTodayStr
		{
			get => HighGustBearingToday.HasValue ? HighGustBearingToday.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighWindToday { get; set; }

		[DataMember(Name = "HighWindToday")]
		public string HighWindTodayRounded
		{
			get => HighWindToday.HasValue ? HighWindToday.Value.ToString(cumulus.WindAvgFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string HighGustTodayTime { get; set; }

		[DataMember(Name = "HighGustTodayTime")]
		public string HighGustTodayTimeStr
		{
			get => HighGustToday.HasValue ? HighGustTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? HighGustToday { get; set; }

		[DataMember(Name = "HighGustToday")]
		public string HighGustTodayRounded
		{
			get => HighGustToday.HasValue ? HighGustToday.Value.ToString(cumulus.WindFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? OutdoorTemp { get; set; }

		[DataMember(Name = "OutdoorTemp")]
		public string OutdoorTempRounded
		{
			get => OutdoorTemp.HasValue ? OutdoorTemp.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? OutdoorHum { get; set; }

		[DataMember(Name = "OutdoorHum")]
		public string OutdoorHumFormatted
		{
			get => OutdoorHum.HasValue ? OutdoorHum.Value.ToString() : nullVal;
			set { }
		}


		[IgnoreDataMember]
		public double AvgTempToday { get; set; }

		[DataMember(Name = "AvgTempToday")]
		public string AvgTempRounded
		{
			get => AvgTempToday.ToString(cumulus.TempFormat);
			set { }
		}

		[IgnoreDataMember]
		public int? IndoorHum { get; set; }
		[DataMember(Name = "IndoorHum")]
		public string IndoorHumStr
		{
			get => IndoorHum.HasValue ? IndoorHum.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? Pressure { get; set; }

		[DataMember(Name = "Pressure")]
		public string PressureRounded
		{
			get => Pressure.HasValue ? Pressure.Value.ToString(cumulus.PressFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double AlltimeHighPressure { get; set; }

		[DataMember(Name = "AlltimeHighPressure")]
		public string AlltimeHighPressureRounded
		{
			get => AlltimeHighPressure.ToString(cumulus.PressFormat);
			set { }
		}

		[IgnoreDataMember]
		public double AlltimeLowPressure { get; set; }

		[DataMember(Name = "AlltimeLowPressure")]
		public string AlltimeLowPressureRounded
		{
			get => AlltimeLowPressure.ToString(cumulus.PressFormat);
			set { }
		}

		[IgnoreDataMember]
		public double? WindLatest { get; set; }

		[DataMember(Name = "WindLatest")]
		public string WindLatestRounded
		{
			get => WindLatest.HasValue ? WindLatest.Value.ToString(cumulus.WindFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? WindAverage { get; set; }

		[DataMember(Name = "WindAverage")]
		public string WindAverageRounded
		{
			get => WindAverage.HasValue ? WindAverage.Value.ToString(cumulus.WindAvgFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? Recentmaxgust { get; set; }

		[DataMember(Name = "Recentmaxgust")]
		public string RecentmaxgustRounded
		{
			get => Recentmaxgust.HasValue ? Recentmaxgust.Value.ToString(cumulus.WindFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double WindRunToday { get; set; }

		[DataMember(Name = "WindRunToday")]
		public string WindRunTodayRounded
		{
			get => WindRunToday.ToString(cumulus.WindRunFormat);
			set { }
		}

		[IgnoreDataMember]
		public int? Bearing { get; set; }

		[DataMember(Name = "Bearing")]
		public string BearingFormatted
		{
			get => Bearing.HasValue ? Bearing.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public int? Avgbearing { get; set; }
		[DataMember(Name = "Avgbearing")]
		public string AvgbearingFormatted
		{
			get => Avgbearing.HasValue ? Avgbearing.Value.ToString() : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? RainToday { get; set; }

		[DataMember(Name = "RainToday")]
		public string RainTodayRounded
		{
			get => RainToday.HasValue ? RainToday.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? RainYesterday { get; set; }

		[DataMember(Name = "RainYesterday")]
		public string RainYesterdayRounded
		{
			get => RainYesterday.HasValue ? RainYesterday.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double RainMonth { get; set; }

		[DataMember(Name = "RainMonth")]
		public string RainMonthRounded
		{
			get => RainMonth.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double RainYear { get; set; }

		[DataMember(Name = "RainYear")]
		public string RainYearRounded
		{
			get => RainYear.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double? RainRate { get; set; }

		[DataMember(Name = "RainRate")]
		public string RainRateRounded
		{
			get => RainRate.HasValue ? RainRate.Value.ToString(cumulus.RainFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double RainLastHour { get; set; }

		[DataMember(Name = "RainLastHour")]
		public string RainLastHourRounded
		{
			get => RainLastHour.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double RainLast24Hour { get; set; }

		[DataMember(Name = "RainLast24Hour")]
		public string RainLast24HourRounded
		{
			get => RainLast24Hour.ToString(cumulus.RainFormat);
			set { }
		}

		[IgnoreDataMember]
		public double? HeatIndex { get; set; }

		[DataMember(Name = "HeatIndex")]
		public string HeatIndexRounded
		{
			get => HeatIndex.HasValue ? HeatIndex.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? Humidex { get; set; }

		[DataMember(Name = "Humidex")]
		public string HumidexRounded
		{
			get => Humidex.HasValue ? Humidex.Value.ToString(cumulus.TempFormat) : "-";
			set { }
		}

		[IgnoreDataMember]
		public string HighHumidexTodayTime { get; set; }

		[DataMember(Name = "HighHumidexTodayTime")]
		public string HighHumidexTodayTimeStr
		{
			get => HighHumidexToday.HasValue ? HighHumidexTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? HighHumidexToday { get; set; }

		[DataMember(Name = "HighHumidexToday")]
		public string HighHumidexTodayRounded
		{
			get => HighHumidexToday.HasValue ? HighHumidexToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? AppTemp { get; set; }

		[DataMember(Name = "AppTemp")]
		public string AppTempRounded
		{
			get => AppTemp.HasValue ? AppTemp.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowAppTempTodayTime { get; set; }

		[DataMember(Name = "LowAppTempTodayTime")]
		public string LowAppTempTodayTimeStr
		{
			get => LowAppTempToday.HasValue ? LowAppTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighAppTempTodayTime { get; set; }

		[DataMember(Name = "HighAppTempTodayTime")]
		public string HighAppTempTodayTimeStr
		{
			get => HighAppTempToday.HasValue ? HighAppTempTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowAppTempToday { get; set; }

		[DataMember(Name = "LowAppTempToday")]
		public string LowAppTempTodayRounded
		{
			get => LowAppTempToday.HasValue ? LowAppTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighAppTempToday { get; set; }

		[DataMember(Name = "HighAppTempToday")]
		public string HighAppTempTodayRounded
		{
			get => HighAppTempToday.HasValue ? HighAppTempToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? FeelsLike { get; set; }

		[DataMember(Name = "FeelsLike")]
		public string FeelsLikeRounded
		{
			get => FeelsLike.HasValue ? FeelsLike.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public string LowFeelsLikeTodayTime { get; set; }

		[DataMember(Name = "LowFeelsLikeTodayTime")]
		public string LowFeelsLikeTodayTimeStr
		{
			get => LowFeelsLikeToday.HasValue ? LowFeelsLikeTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public string HighFeelsLikeTodayTime { get; set; }

		[DataMember(Name = "HighFeelsLikeTodayTime")]
		public string HighFeelsLikeTodayTimeStr
		{
			get => HighFeelsLikeToday.HasValue ? HighFeelsLikeTodayTime : nullTime;
			set { }
		}

		[IgnoreDataMember]
		public double? LowFeelsLikeToday { get; set; }

		[DataMember(Name = "LowFeelsLikeToday")]
		public string LowFeelsLikeTodayRounded
		{
			get => LowFeelsLikeToday.HasValue ? LowFeelsLikeToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double? HighFeelsLikeToday { get; set; }

		[DataMember(Name = "HighFeelsLikeToday")]
		public string HighFeelsLikeTodayRounded
		{
			get => HighFeelsLikeToday.HasValue ? HighFeelsLikeToday.Value.ToString(cumulus.TempFormat) : nullVal;
			set { }
		}

		[IgnoreDataMember]
		public double TempTrend { get; set; }

		[DataMember(Name = "TempTrend")]
		public string TempTrendRounded
		{
			get => TempTrend.ToString(cumulus.TempTrendFormat);
			set { }
		}

		[IgnoreDataMember]
		public double PressTrend { get; set; }

		[DataMember(Name = "PressTrend")]
		public string PressTrendRounded
		{
			get => PressTrend.ToString(cumulus.PressTrendFormat);
			set { }
		}

		[IgnoreDataMember]
		public double SunshineHours { get; set; }

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
		public string DominantWindDirection { get; set; }

		[DataMember]
		public string LastRainTipISO { get; set; }

		[DataMember]
		public string HighBeaufortToday { get; set; }

		[DataMember]
		public string Beaufort { get; set; }

		[DataMember]
		public string BeaufortDesc { get; set; }

		[DataMember]
		public string LastDataRead { get; set; }

		[DataMember]
		public bool DataStopped { get; set; }

		[DataMember]
		public bool AlarmLowTemp { get; set; }

		[DataMember]
		public bool AlarmHighTemp { get; set; }

		[DataMember]
		public bool AlarmTempUp { get; set; }

		[DataMember]
		public bool AlarmTempDn { get; set; }

		[DataMember]
		public bool AlarmRain { get; set; }

		[DataMember]
		public bool AlarmRainRate { get; set; }

		[DataMember]
		public bool AlarmLowPress { get; set; }

		[DataMember]
		public bool AlarmHighPress { get; set; }

		[DataMember]
		public bool AlarmPressUp { get; set; }

		[DataMember]
		public bool AlarmPressDn { get; set; }

		[DataMember]
		public bool AlarmGust { get; set; }

		[DataMember]
		public bool AlarmWind { get; set; }

		[DataMember]
		public bool AlarmSensor { get; set; }

		[DataMember]
		public bool AlarmBattery { get; set; }

		[DataMember]
		public bool AlarmSpike { get; set; }

		[DataMember]
		public bool AlarmUpgrade { get; set; }

		[DataMember]
		public bool AlarmHttp { get; set; }

		[DataMember]
		public bool AlarmMySql { get; set; }

		[DataMember]
		public bool AlarmIsRaining { get; set; }

		[DataMember]
		public bool AlarmNewRec { get; set; }

		[DataMember]
		public bool AlarmFtp { get; set; }
	}
}
