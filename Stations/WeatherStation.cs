using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using SQLite;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CumulusMX
{
	internal abstract class WeatherStation
	{
		public struct TWindRecent
		{
			public double Gust; // uncalibrated "gust" as read from station
			public double Speed; // uncalibrated "speed" as read from station
			public DateTime Timestamp;
		}

		public struct TWindVec
		{
			public double X;
			public double Y;
			public int Bearing;
			public DateTime Timestamp;
		}

		private readonly Object monthIniThreadLock = new Object();
		public readonly Object yearIniThreadLock = new Object();
		public readonly Object alltimeIniThreadLock = new Object();
		public readonly Object monthlyalltimeIniThreadLock = new Object();
		private static readonly SemaphoreSlim webSocketSemaphore = new SemaphoreSlim(1, 1);

		// holds all time highs and lows
		public AllTimeRecords AllTime = new AllTimeRecords();

		// holds monthly all time highs and lows
		private AllTimeRecords[] monthlyRecs = new AllTimeRecords[13];
		public AllTimeRecords[] MonthlyRecs
		{
			get
			{
				if (monthlyRecs == null)
				{
					monthlyRecs = new AllTimeRecords[13];
				}

				return monthlyRecs;
			}
		}


		// this month highs and lows
		public AllTimeRecords ThisMonth = new AllTimeRecords();

		public AllTimeRecords ThisYear = new AllTimeRecords();


		//public DateTime lastArchiveTimeUTC;

		public string LatestFOReading { get; set; }

		//public int LastDailySummaryOADate;

		public static Cumulus cumulus;
		private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;
		private readonly DateTimeFormatInfo invDate = CultureInfo.InvariantCulture.DateTimeFormat;

		private int lastMinute;
		private int lastHour;

		public bool[] WMR928ChannelPresent = new[] { false, false, false, false };
		public bool[] WMR928ExtraTempValueOnly = new[] { false, false, false, false };
		public double[] WMR928ExtraTempValues = new[] { 0.0, 0.0, 0.0, 0.0 };
		public double[] WMR928ExtraDPValues = new[] { 0.0, 0.0, 0.0, 0.0 };
		public int[] WMR928ExtraHumValues = new[] { 0, 0, 0, 0 };

		// random number generator - used for things like random back-off delays
		internal Random random = new Random();

		public DateTime AlltimeRecordTimestamp { get; set; }

		public BackgroundWorker bw;

		//public bool importingData = false;

		public bool calculaterainrate = false;

		protected List<int> buffer = new List<int>();

		//private readonly List<Last3HourData> Last3HourDataList = new List<Last3HourData>();
		//private readonly List<LastHourData> LastHourDataList = new List<LastHourData>();
		private readonly List<Last10MinWind> Last10MinWindList = new List<Last10MinWind>();
		//private readonly List<RecentDailyData> RecentDailyDataList = new List<RecentDailyData>();

		//public WeatherDataCollection weatherDataCollection = new WeatherDataCollection();

		// Current values

		public double? THWIndex = null;
		public double? THSWIndex = null;

		public double raindaystart = 0.0;
		public double Raincounter = 0.0;
		public bool gotraindaystart = false;
		protected double prevraincounter = 0.0;

		public struct DailyHighLow
		{
			public double? HighGust;
			public int? HighGustBearing;
			public DateTime HighGustTime;
			public double? HighWind;
			public DateTime HighWindTime;
			public double? HighTemp;
			public DateTime HighTempTime;
			public double? LowTemp;
			public DateTime LowTempTime;
			public double? TempRange;
			public double? HighAppTemp;
			public DateTime HighAppTempTime;
			public double? LowAppTemp;
			public DateTime LowAppTempTime;
			public double? HighFeelsLike;
			public DateTime HighFeelsLikeTime;
			public double? LowFeelsLike;
			public DateTime LowFeelsLikeTime;
			public double? HighHumidex;
			public DateTime HighHumidexTime;
			public double? HighPress;
			public DateTime HighPressTime;
			public double? LowPress;
			public DateTime LowPressTime;
			public double? HighRainRate;
			public DateTime HighRainRateTime;
			public double HighHourlyRain;
			public DateTime HighHourlyRainTime;
			public int? HighHumidity;
			public DateTime HighHumidityTime;
			public int? LowHumidity;
			public DateTime LowHumidityTime;
			public double? HighHeatIndex;
			public DateTime HighHeatIndexTime;
			public double? LowWindChill;
			public DateTime LowWindChillTime;
			public double? HighDewPoint;
			public DateTime HighDewPointTime;
			public double? LowDewPoint;
			public DateTime LowDewPointTime;
			public int? HighSolar;
			public DateTime HighSolarTime;
			public double? HighUv;
			public DateTime HighUvTime;
			public double? HighRain24h;
			public DateTime HighRain24hTime;

		};

		// today highs and lows
		public DailyHighLow HiLoToday = new DailyHighLow();

		// yesterdays highs and lows
		public DailyHighLow HiLoYest = new DailyHighLow();

		// todays midnight highs and lows
		public DailyHighLow HiLoTodayMidnight = new DailyHighLow();

		// todays midnight highs and lows
		public DailyHighLow HiLoYestMidnight = new DailyHighLow();


		public int IndoorBattStatus;
		public int WindBattStatus;
		public int RainBattStatus;
		public int TempBattStatus;
		public int UVBattStatus;

		public double[] WMR200ExtraDPValues { get; set; }

		public bool[] WMR200ChannelPresent { get; set; }

		public double[] WMR200ExtraHumValues { get; set; }

		public double[] WMR200ExtraTempValues { get; set; }

		public DateTime lastDataReadTime;
		public bool haveReadData = false;

		private readonly DataReceivedFlags dataValuesUpdated;

		public bool ExtraSensorsDetected = false;

		// Should Cumulus find the peak gust?
		// This gets set to false for Davis stations after logger download
		// if 10-minute gust period is in use, so we use the Davis value instead.
		public bool CalcRecentMaxGust = true;

		public SerialPort comport;

		//private TextWriterTraceListener myTextListener;

		private Thread t;

		public Timer secondTimer;
		public double presstrendval;
		public double temptrendval;

		public int multicastsGood, multicastsBad;

		public bool timerStartNeeded = false;

		private readonly DateTime versionCheckTime;

		public SQLiteConnection Database;
		//public SQLiteAsyncConnection DatabaseAsync;
		// Extra sensors

		public GraphData Graphs;

		public double SolarElevation;

		public double SolarFactor = -1;  // used to adjust solar transmission factor (range 0-1), disabled = -1

		public bool WindReadyToPlot = false;
		public bool TempReadyToPlot = false;
		private bool first_temp = true;
		public double RG11RainYesterday { get; set; }

		public abstract void Start();

		public WeatherStation(Cumulus cumuls)
		{
			// save the reference to the owner
			cumulus = cumuls;

			// initialise the monthly array of records - element zero is not used
			for (var i = 1; i <= 12; i++)
			{
				MonthlyRecs[i] = new AllTimeRecords();
			}

			CumulusForecast = cumulus.ForecastNotAvailable;
			wsforecast = cumulus.ForecastNotAvailable;

			ExtraTemp = new double?[11];
			ExtraHum = new double?[11];
			ExtraDewPoint = new double?[11];
			UserTemp = new double?[11];
			SoilMoisture = new int?[17];
			SoilTemp = new double?[17];
			LeafTemp = new double?[5];
			LeafWetness = new double?[9];
			AirQuality = new double?[5];
			AirQualityAvg = new double?[5];

			windcounts = new double[16];
			WindRecent = new TWindRecent[MaxWindRecent];
			WindVec = new TWindVec[MaxWindRecent];

			dataValuesUpdated = new DataReceivedFlags(this, cumulus);

			Database = new SQLiteConnection(cumulus.dbfile, true);

			// We only use the Async connection for reading data
			//DatabaseAsync = new SQLiteAsyncConnection(cumulus.dbfile, true);

			//Database.EnableWriteAheadLogging();

			_ = Database.ExecuteScalar<int>("PRAGMA synchronous=FULL");
			_ = Database.ExecuteScalar<int>("PRAGMA journal_mode=DELETE");
			_ = Database.ExecuteScalar<int>("PRAGMA temp_store=MEMORY");
			_ = Database.ExecuteScalar<int>("PRAGMA auto_vacuum=NONE");

			// Lock the database in EXCLUSIVE mode which prevent any other process opening the database
			//Database.ExecuteScalar<int>("PRAGMA locking_mode=EXCLUSIVE");
			// Lock the database in NORMAL mode which allows other processes to open the database - but they could lock it and prevent us from updating it
			_ = Database.ExecuteScalar<int>("PRAGMA locking_mode=NORMAL");

			_ = Database.CreateTable<RecentData>();
			_ = Database.CreateTable<DayData>();
			_ = Database.CreateTable<IntervalData>();
			_ = Database.CreateTable<ExtraTemp>();
			_ = Database.CreateTable<ExtraHum>();
			_ = Database.CreateTable<ExtraDewPoint>();
			_ = Database.CreateTable<UserTemp>();
			_ = Database.CreateTable<SoilTemp>();
			_ = Database.CreateTable<SoilMoist>();
			_ = Database.CreateTable<LeafTemp>();
			_ = Database.CreateTable<LeafWet>();
			_ = Database.CreateTable<AirQuality>();
			_ = Database.CreateTable<CO2Data>();
			_ = Database.CreateTable<SqlCache>();

			// preload the failed sql cache - if any
			ReloadFailedMySQLCommands();

			// now vacuum the database
			/*
			Cumulus.LogMessage("Compressing the database");
			var dbOrigSize = new FileInfo(cumulus.dbfile).Length / 1048576.0;
			Database.Execute("VACUUM");
			var dbNewSize = new FileInfo(cumulus.dbfile).Length / 1048576.0;
			Cumulus.LogMessage($"Completed compressing the database, was {dbOrigSize:F2} MB,  now {dbNewSize:F2} MB");
			*/

			Graphs = new GraphData(cumulus, this);

			ReadTodayFile();
			ReadYesterdayFile();
			ReadAlltimeIniFile();
			ReadMonthlyAlltimeIniFile();
			ReadMonthIniFile();
			ReadYearIniFile();
			LoadDayFileToDb();
			LoadLogFilesToDb();

			GetRainCounter();
			GetRainFallTotals();

			var rnd = new Random();
			versionCheckTime = new DateTime(1, 1, 1, rnd.Next(0, 23), rnd.Next(0, 59), 0);

			// rollover/create raw data log files if required
			cumulus.RollOverDataLogs();
		}

		public void ReloadFailedMySQLCommands()
		{
			while (cumulus.MySqlStuff.FailedList.TryDequeue(out var tmp))
			{
				// do nothing
			};

			// preload the failed sql cache - if any
			var data = Database.Query<SqlCache>("SELECT * FROM SqlCache ORDER BY key");

			foreach (var rec in data)
			{
				cumulus.MySqlStuff.FailedList.Enqueue(rec);
			}
		}

		private void GetRainCounter()
		{
			// Find today's rain so far from last log record in the database
			bool midnightrainfound = false;
			double raincount = 0;
			DateTime logdate = DateTime.MinValue;

			Cumulus.LogMessage("Finding raintoday from database");
			try
			{
				var rec = Database.Query<IntervalData>("select min(Timestamp) Timestamp, RainCounter from IntervalData where Timestamp >= ?", cumulus.LastUpdateTime.Date.ToUniversalTime());

				if (rec[0].RainCounter.HasValue)
				{
					// this is the first entry of a new day AND the new day is today
					midnightrainfound = true;
					Cumulus.LogMessage($"Midnight rain found in the following entry: {rec[0].Timestamp}, RainCounter = {rec[0].RainCounter}");
					raincount = rec[0].RainCounter.Value;
					logdate = rec[0].Timestamp.ToLocalTime();
					RainToday = Raincounter - raindaystart >= 0 ? (Raincounter - raindaystart) * cumulus.Calib.Rain.Mult : 0;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error finding midnight rainfall counter");
			}

			if (midnightrainfound)
			{
				if (logdate.Day == 1 && logdate.Month == cumulus.RainSeasonStart && cumulus.Manufacturer == cumulus.DAVIS)
				{
					// special case: rain counter is about to be reset
					//TODO: MC: Hmm are there issues here, what if the console clock is wrong and it does not reset for another hour, or it already reset and we have had rain since?
					var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(cumulus.RainSeasonStart);
					Cumulus.LogMessage($"Special case, Davis station on 1st of {month}. Set midnight rain count to zero");
					midnightraincount = 0;
				}
				else
				{
					Cumulus.LogMessage("Midnight rain found, setting midnight rain count = " + raincount);
					midnightraincount = raincount;
				}
			}
			else
			{
				Cumulus.LogMessage("Midnight rain not found, setting midnight count to raindaystart = " + raindaystart);
				midnightraincount = raindaystart;
			}

			// If we do not have a rain counter value for start of day from Today.ini, then use the midnight counter
			if (initialiseRainCounterOnFirstData)
			{
				Raincounter = midnightraincount + ((RainToday ?? 0) / cumulus.Calib.Rain.Mult);
			}
			else
			{
				// Otherwise use the counter value from today.ini plus total so far today to infer the counter value
				Raincounter = raindaystart + ((RainToday ?? 0) / cumulus.Calib.Rain.Mult);
			}

			Cumulus.LogMessage("Checking rain counter = " + Raincounter);
			if (Raincounter < 0)
			{
				Cumulus.LogMessage("Rain counter negative, setting to zero");
				Raincounter = 0;
			}
			else
			{
				Cumulus.LogMessage("Rain counter set to = " + Raincounter);
			}
		}

		public void GetRainFallTotals()
		{
			Cumulus.LogMessage("Getting rain totals, rain season start = " + cumulus.RainSeasonStart);
			rainthismonth = 0;
			rainthisyear = 0;
			// get today"s date for month check; allow for 0900 roll-over
			var hourInc = cumulus.GetHourInc();
			var ModifiedNow = DateTime.Now.AddHours(hourInc);
			// set to the first of the month
			ModifiedNow = ModifiedNow.AddDays(-ModifiedNow.Day + 1);
			// avoid any funny locale peculiarities on date formats
			string Today = ModifiedNow.ToString("dd/MM/yy", CultureInfo.InvariantCulture);
			Cumulus.LogMessage("This Month = " + Today);
			// get today's date offset by rain season start for year check
			var yearStartDate = new DateTime(ModifiedNow.Year, cumulus.RainSeasonStart, 1, 0, 0, 0, DateTimeKind.Local);
			if (yearStartDate > ModifiedNow)
				yearStartDate = yearStartDate.AddYears(-1);

			rainthisyear = Database.ExecuteScalar<double>("select sum(TotalRain) from DayData where Timestamp >= ?", yearStartDate.ToUniversalTime());
			rainthismonth = Database.ExecuteScalar<double>("select sum(TotalRain) from DayData where Timestamp >= ?", ModifiedNow.ToUniversalTime());

			Cumulus.LogMessage("Rainthismonth from daily data: " + rainthismonth);
			Cumulus.LogMessage("Rainthisyear from daily data: " + rainthisyear);

			// Add in year-to-date rain (if necessary)
			if (cumulus.YTDrainyear == Convert.ToInt32(Today.Substring(6, 2)) + 2000)
			{
				Cumulus.LogMessage("Adding YTD rain: " + cumulus.YTDrain);
				rainthisyear += cumulus.YTDrain;
				Cumulus.LogMessage("Rainthisyear: " + rainthisyear);
			}
		}

		public void ReadTodayFile()
		{
			if (!File.Exists(cumulus.TodayIniFile))
			{
				FirstRun = true;
			}

			int? nullInt = null;
			double? nullDbl = null;

			IniFile ini = new IniFile(cumulus.TodayIniFile);

			Cumulus.LogConsoleMessage("Today.ini = " + cumulus.TodayIniFile);

			var todayfiledate = ini.GetValue("General", "Date", "00/00/00");
			var timestampstr = ini.GetValue("General", "Timestamp", DateTime.Now.ToString("s"));

			Cumulus.LogConsoleMessage("Last update=" + timestampstr);

			cumulus.LastUpdateTime = DateTime.Parse(timestampstr);
			var todayDate = cumulus.LastUpdateTime.Date;

			Cumulus.LogMessage("Last update time from today.ini: " + cumulus.LastUpdateTime);

			DateTime meteoTodayDate = cumulus.LastUpdateTime.AddHours(cumulus.GetHourInc()).Date;

			int defaultyear = meteoTodayDate.Year;
			int defaultmonth = meteoTodayDate.Month;
			int defaultday = meteoTodayDate.Day;

			CurrentYear = ini.GetValue("General", "CurrentYear", defaultyear);
			CurrentMonth = ini.GetValue("General", "CurrentMonth", defaultmonth);
			CurrentDay = ini.GetValue("General", "CurrentDay", defaultday);

			Cumulus.LogMessage("Read today file: Date = " + todayfiledate + ", LastUpdateTime = " + cumulus.LastUpdateTime + ", Month = " + CurrentMonth);

			LastRainTip = ini.GetValue("Rain", "LastTip", "0000-00-00 00:00");

			FOSensorClockTime = ini.GetValue("FineOffset", "FOSensorClockTime", DateTime.MinValue);
			FOStationClockTime = ini.GetValue("FineOffset", "FOStationClockTime", DateTime.MinValue);
			FOSolarClockTime = ini.GetValue("FineOffset", "FOSolarClockTime", DateTime.MinValue);
			if (cumulus.FineOffsetOptions.SyncReads)
			{
				Cumulus.LogMessage("Sensor clock  " + FOSensorClockTime.ToLongTimeString());
				Cumulus.LogMessage("Station clock " + FOStationClockTime.ToLongTimeString());
			}
			ConsecutiveRainDays = ini.GetValue("Rain", "ConsecutiveRainDays", 0);
			ConsecutiveDryDays = ini.GetValue("Rain", "ConsecutiveDryDays", 0);

			AnnualETTotal = ini.GetValue("ET", "Annual", 0.0);
			StartofdayET = ini.GetValue("ET", "Startofday", -1.0);
			if (StartofdayET < 0)
			{
				Cumulus.LogMessage("ET not initialised");
				noET = true;
			}
			else
			{
				ET = AnnualETTotal - StartofdayET;
				Cumulus.LogMessage("ET today = " + ET.ToString(cumulus.ETFormat));
			}
			ChillHours = ini.GetValue("Temp", "ChillHours", 0.0);

			// NOAA report names
			cumulus.NOAAconf.LatestMonthReport = ini.GetValue("NOAA", "LatestMonthlyReport", "");
			cumulus.NOAAconf.LatestYearReport = ini.GetValue("NOAA", "LatestYearlyReport", "");

			// Solar
			HiLoToday.HighSolar = ini.GetValue("Solar", "HighSolarRad", nullInt);
			HiLoToday.HighSolarTime = ini.GetValue("Solar", "HighSolarRadTime", todayDate);
			HiLoToday.HighUv = ini.GetValue("Solar", "HighUV", nullDbl);
			HiLoToday.HighUvTime = ini.GetValue("Solar", "HighUVTime", meteoTodayDate);
			StartOfDaySunHourCounter = ini.GetValue("Solar", "SunStart", Cumulus.DefaultHiVal);
			RG11RainToday = ini.GetValue("Rain", "RG11Today", 0.0);

			// Wind
			HiLoToday.HighWind = ini.GetValue("Wind", "Speed", nullDbl);
			HiLoToday.HighWindTime = ini.GetValue("Wind", "SpTime", meteoTodayDate);
			HiLoToday.HighGust = ini.GetValue("Wind", "Gust", nullDbl);
			HiLoToday.HighGustTime = ini.GetValue("Wind", "Time", meteoTodayDate);
			HiLoToday.HighGustBearing = ini.GetValue("Wind", "Bearing", (int)Cumulus.DefaultHiVal);
			HiLoToday.HighGustBearing = HiLoToday.HighGustBearing == Cumulus.DefaultHiVal ? null : HiLoToday.HighGustBearing;
			WindRunToday = ini.GetValue("Wind", "Windrun", 0.0);
			DominantWindBearing = ini.GetValue("Wind", "DominantWindBearing", 0);
			DominantWindBearingMinutes = ini.GetValue("Wind", "DominantWindBearingMinutes", 0);
			DominantWindBearingX = ini.GetValue("Wind", "DominantWindBearingX", 0.0);
			DominantWindBearingY = ini.GetValue("Wind", "DominantWindBearingY", 0.0);

			// Temperature
			HiLoToday.LowTemp = ini.GetValue("Temp", "Low", nullDbl);
			HiLoToday.LowTempTime = ini.GetValue("Temp", "LTime", meteoTodayDate);
			HiLoToday.HighTemp = ini.GetValue("Temp", "High", nullDbl);
			HiLoToday.HighTempTime = ini.GetValue("Temp", "HTime", meteoTodayDate);

			if (HiLoToday.HighTemp.HasValue && HiLoToday.LowTemp.HasValue)
				HiLoToday.TempRange = HiLoToday.HighTemp.Value - HiLoToday.LowTemp.Value;
			else
				HiLoToday.TempRange = null;
			TempTotalToday = ini.GetValue("Temp", "Total", 0.0);
			tempsamplestoday = ini.GetValue("Temp", "Samples", 1);

			// Temperature midnight rollover
			HiLoTodayMidnight.LowTemp = ini.GetValue("TempMidnight", "Low", nullDbl);
			HiLoTodayMidnight.LowTempTime = ini.GetValue("TempMidnight", "LTime", meteoTodayDate);
			HiLoTodayMidnight.HighTemp = ini.GetValue("TempMidnight", "High", nullDbl);
			HiLoTodayMidnight.HighTempTime = ini.GetValue("TempMidnight", "HTime", meteoTodayDate);

			HeatingDegreeDays = ini.GetValue("Temp", "HeatingDegreeDays", 0.0);
			CoolingDegreeDays = ini.GetValue("Temp", "CoolingDegreeDays", 0.0);

			GrowingDegreeDaysThisYear1 = ini.GetValue("Temp", "GrowingDegreeDaysThisYear1", 0.0);
			GrowingDegreeDaysThisYear2 = ini.GetValue("Temp", "GrowingDegreeDaysThisYear2", 0.0);
			// Pressure
			HiLoToday.LowPress = ini.GetValue("Pressure", "Low", nullDbl);
			HiLoToday.LowPressTime = ini.GetValue("Pressure", "LTime", meteoTodayDate);
			HiLoToday.HighPress = ini.GetValue("Pressure", "High", nullDbl);
			HiLoToday.HighPressTime = ini.GetValue("Pressure", "HTime", meteoTodayDate);
			// rain
			HiLoToday.HighRainRate = ini.GetValue("Rain", "High", nullDbl);
			HiLoToday.HighRainRateTime = ini.GetValue("Rain", "HTime", meteoTodayDate);
			HiLoToday.HighHourlyRain = ini.GetValue("Rain", "HourlyHigh", 0.0);
			HiLoToday.HighHourlyRainTime = ini.GetValue("Rain", "HHourlyTime", meteoTodayDate);
			raindaystart = ini.GetValue("Rain", "Start", -1.0);
			Raincounter = ini.GetValue("Rain", "Last", -1.0);
			Cumulus.LogMessage($"ReadTodayfile: Rain day start = {raindaystart}, last = {Raincounter}");
			if (raindaystart >= 0)
			{
				Cumulus.LogMessage("ReadTodayfile: set initialiseRainCounterOnFirstData false");
				initialiseRainCounterOnFirstData = false;
			}
			RainYesterday = ini.GetValue("Rain", "Yesterday", nullDbl);
			// humidity
			HiLoToday.LowHumidity = ini.GetValue("Humidity", "Low", nullInt);
			HiLoToday.HighHumidity = ini.GetValue("Humidity", "High", nullInt);
			HiLoToday.LowHumidityTime = ini.GetValue("Humidity", "LTime", meteoTodayDate);
			HiLoToday.HighHumidityTime = ini.GetValue("Humidity", "HTime", meteoTodayDate);
			// Solar
			SunshineHours = ini.GetValue("Solar", "SunshineHours", 0.0);
			SunshineToMidnight = ini.GetValue("Solar", "SunshineHoursToMidnight", 0.0);
			// heat index
			HiLoToday.HighHeatIndex = ini.GetValue("HeatIndex", "High", nullDbl);
			HiLoToday.HighHeatIndexTime = ini.GetValue("HeatIndex", "HTime", meteoTodayDate);
			// Apparent temp
			HiLoToday.HighAppTemp = ini.GetValue("AppTemp", "High", nullDbl);
			HiLoToday.HighAppTempTime = ini.GetValue("AppTemp", "HTime", meteoTodayDate);
			HiLoToday.LowAppTemp = ini.GetValue("AppTemp", "Low", nullDbl);
			HiLoToday.LowAppTempTime = ini.GetValue("AppTemp", "LTime", meteoTodayDate);
			// wind chill
			HiLoToday.LowWindChill = ini.GetValue("WindChill", "Low", nullDbl);
			HiLoToday.LowWindChillTime = ini.GetValue("WindChill", "LTime", meteoTodayDate);
			// Dew point
			HiLoToday.HighDewPoint = ini.GetValue("Dewpoint", "High", nullDbl);
			HiLoToday.HighDewPointTime = ini.GetValue("Dewpoint", "HTime", meteoTodayDate);
			HiLoToday.LowDewPoint = ini.GetValue("Dewpoint", "Low", nullDbl);
			HiLoToday.LowDewPointTime = ini.GetValue("Dewpoint", "LTime", meteoTodayDate);
			// Feels like
			HiLoToday.HighFeelsLike = ini.GetValue("FeelsLike", "High", nullDbl);
			HiLoToday.HighFeelsLikeTime = ini.GetValue("FeelsLike", "HTime", meteoTodayDate);
			HiLoToday.LowFeelsLike = ini.GetValue("FeelsLike", "Low", nullDbl);
			HiLoToday.LowFeelsLikeTime = ini.GetValue("FeelsLike", "LTime", meteoTodayDate);
			// Humidex
			HiLoToday.HighHumidex = ini.GetValue("Humidex", "High", nullDbl);
			HiLoToday.HighHumidexTime = ini.GetValue("Humidex", "HTime", meteoTodayDate);

			// Records
			AlltimeRecordTimestamp = ini.GetValue("Records", "Alltime", DateTime.MinValue);

			// Lightning (GW1000 for now)
			LightningDistance = ini.GetValue("Lightning", "Distance", -1.0);
			LightningTime = ini.GetValue("Lightning", "LastStrike", DateTime.MinValue);
		}

		public void WriteTodayFile(DateTime timestamp, bool Log)
		{
			try
			{
				var hourInc = cumulus.GetHourInc();

				IniFile ini = new IniFile(cumulus.TodayIniFile);

				// Date
				ini.SetValue("General", "Date", timestamp.AddHours(hourInc).ToShortDateString());
				// Timestamp
				ini.SetValue("General", "Timestamp", cumulus.LastUpdateTime.ToString("s"));
				ini.SetValue("General", "CurrentYear", CurrentYear);
				ini.SetValue("General", "CurrentMonth", CurrentMonth);
				ini.SetValue("General", "CurrentDay", CurrentDay);
				// Wind
				ini.SetValue("Wind", "Speed", HiLoToday.HighWind);
				ini.SetValue("Wind", "SpTime", HiLoToday.HighWindTime.ToString("HH:mm"));
				ini.SetValue("Wind", "Gust", HiLoToday.HighGust);
				ini.SetValue("Wind", "Time", HiLoToday.HighGustTime.ToString("HH:mm"));
				ini.SetValue("Wind", "Bearing", HiLoToday.HighGustBearing);
				ini.SetValue("Wind", "Direction", CompassPoint(HiLoToday.HighGustBearing));
				ini.SetValue("Wind", "Windrun", WindRunToday);
				ini.SetValue("Wind", "DominantWindBearing", DominantWindBearing);
				ini.SetValue("Wind", "DominantWindBearingMinutes", DominantWindBearingMinutes);
				ini.SetValue("Wind", "DominantWindBearingX", DominantWindBearingX);
				ini.SetValue("Wind", "DominantWindBearingY", DominantWindBearingY);
				// Temperature
				ini.SetValue("Temp", "Low", HiLoToday.LowTemp);
				ini.SetValue("Temp", "LTime", HiLoToday.LowTempTime.ToString("HH:mm"));
				ini.SetValue("Temp", "High", HiLoToday.HighTemp);
				ini.SetValue("Temp", "HTime", HiLoToday.HighTempTime.ToString("HH:mm"));
				ini.SetValue("Temp", "Total", TempTotalToday);
				ini.SetValue("Temp", "Samples", tempsamplestoday);
				ini.SetValue("Temp", "ChillHours", ChillHours);
				ini.SetValue("Temp", "HeatingDegreeDays", HeatingDegreeDays);
				ini.SetValue("Temp", "CoolingDegreeDays", CoolingDegreeDays);
				ini.SetValue("Temp", "GrowingDegreeDaysThisYear1", GrowingDegreeDaysThisYear1);
				ini.SetValue("Temp", "GrowingDegreeDaysThisYear2", GrowingDegreeDaysThisYear2);
				// Temperature midnight rollover
				ini.SetValue("TempMidnight", "Low", HiLoTodayMidnight.LowTemp);
				ini.SetValue("TempMidnight", "LTime", HiLoTodayMidnight.LowTempTime);
				ini.SetValue("TempMidnight", "High", HiLoTodayMidnight.HighTemp);
				ini.SetValue("TempMidnight", "HTime", HiLoTodayMidnight.HighTempTime);
				// Pressure
				ini.SetValue("Pressure", "Low", HiLoToday.LowPress);
				ini.SetValue("Pressure", "LTime", HiLoToday.LowPressTime.ToString("HH:mm"));
				ini.SetValue("Pressure", "High", HiLoToday.HighPress);
				ini.SetValue("Pressure", "HTime", HiLoToday.HighPressTime.ToString("HH:mm"));
				// rain
				ini.SetValue("Rain", "High", HiLoToday.HighRainRate);
				ini.SetValue("Rain", "HTime", HiLoToday.HighRainRateTime.ToString("HH:mm"));
				ini.SetValue("Rain", "HourlyHigh", HiLoToday.HighHourlyRain);
				ini.SetValue("Rain", "HHourlyTime", HiLoToday.HighHourlyRainTime.ToString("HH:mm"));
				ini.SetValue("Rain", "High24h", HiLoToday.HighRain24h);
				ini.SetValue("Rain", "High24hTime", HiLoToday.HighRain24hTime.ToString("HH:mm"));
				ini.SetValue("Rain", "Start", raindaystart);
				ini.SetValue("Rain", "Last", Raincounter);

				ini.SetValue("Rain", "Yesterday", RainYesterday);
				ini.SetValue("Rain", "LastTip", LastRainTip);
				ini.SetValue("Rain", "ConsecutiveRainDays", ConsecutiveRainDays);
				ini.SetValue("Rain", "ConsecutiveDryDays", ConsecutiveDryDays);
				ini.SetValue("Rain", "RG11Today", RG11RainToday);
				// ET
				ini.SetValue("ET", "Annual", AnnualETTotal);
				ini.SetValue("ET", "Startofday", StartofdayET);
				// humidity
				ini.SetValue("Humidity", "Low", HiLoToday.LowHumidity);
				ini.SetValue("Humidity", "High", HiLoToday.HighHumidity);
				ini.SetValue("Humidity", "LTime", HiLoToday.LowHumidityTime.ToString("HH:mm"));
				ini.SetValue("Humidity", "HTime", HiLoToday.HighHumidityTime.ToString("HH:mm"));
				// Solar
				ini.SetValue("Solar", "SunshineHours", SunshineHours);
				ini.SetValue("Solar", "SunshineHoursToMidnight", SunshineToMidnight);
				// heat index
				ini.SetValue("HeatIndex", "High", HiLoToday.HighHeatIndex);
				ini.SetValue("HeatIndex", "HTime", HiLoToday.HighHeatIndexTime.ToString("HH:mm"));
				// App temp
				ini.SetValue("AppTemp", "Low", HiLoToday.LowAppTemp);
				ini.SetValue("AppTemp", "LTime", HiLoToday.LowAppTempTime.ToString("HH:mm"));
				ini.SetValue("AppTemp", "High", HiLoToday.HighAppTemp);
				ini.SetValue("AppTemp", "HTime", HiLoToday.HighAppTempTime.ToString("HH:mm"));
				// Feels like
				ini.SetValue("FeelsLike", "Low", HiLoToday.LowFeelsLike);
				ini.SetValue("FeelsLike", "LTime", HiLoToday.LowFeelsLikeTime.ToString("HH:mm"));
				ini.SetValue("FeelsLike", "High", HiLoToday.HighFeelsLike);
				ini.SetValue("FeelsLike", "HTime", HiLoToday.HighFeelsLikeTime.ToString("HH:mm"));
				// Humidex
				ini.SetValue("Humidex", "High", HiLoToday.HighHumidex);
				ini.SetValue("Humidex", "HTime", HiLoToday.HighHumidexTime.ToString("HH:mm"));
				// wind chill
				ini.SetValue("WindChill", "Low", HiLoToday.LowWindChill);
				ini.SetValue("WindChill", "LTime", HiLoToday.LowWindChillTime.ToString("HH:mm"));
				// Dewpoint
				ini.SetValue("Dewpoint", "Low", HiLoToday.LowDewPoint);
				ini.SetValue("Dewpoint", "LTime", HiLoToday.LowDewPointTime.ToString("HH:mm"));
				ini.SetValue("Dewpoint", "High", HiLoToday.HighDewPoint);
				ini.SetValue("Dewpoint", "HTime", HiLoToday.HighDewPointTime.ToString("HH:mm"));

				// NOAA report names
				ini.SetValue("NOAA", "LatestMonthlyReport", cumulus.NOAAconf.LatestMonthReport);
				ini.SetValue("NOAA", "LatestYearlyReport", cumulus.NOAAconf.LatestYearReport);

				// Solar
				ini.SetValue("Solar", "HighSolarRad", HiLoToday.HighSolar);
				ini.SetValue("Solar", "HighSolarRadTime", HiLoToday.HighSolarTime.ToString("HH:mm"));
				ini.SetValue("Solar", "HighUV", HiLoToday.HighUv);
				ini.SetValue("Solar", "HighUVTime", HiLoToday.HighUvTime.ToString("HH:mm"));
				ini.SetValue("Solar", "SunStart", StartOfDaySunHourCounter);

				// Special Fine Offset data
				ini.SetValue("FineOffset", "FOSensorClockTime", FOSensorClockTime);
				ini.SetValue("FineOffset", "FOStationClockTime", FOStationClockTime);
				ini.SetValue("FineOffset", "FOSolarClockTime", FOSolarClockTime);

				// Records
				ini.SetValue("Records", "Alltime", AlltimeRecordTimestamp);

				// Lightning (GW1000 for now)
				ini.SetValue("Lightning", "Distance", LightningDistance);
				ini.SetValue("Lightning", "LastStrike", LightningTime);


				if (Log)
				{
					Cumulus.LogMessage($"Writing today.ini, LastUpdateTime = {cumulus.LastUpdateTime} raindaystart = {raindaystart} rain counter = {Raincounter}");

					if (cumulus.FineOffsetStation)
					{
						Cumulus.LogMessage("Latest reading: " + LatestFOReading);
					}
					else if (cumulus.StationType == StationTypes.Instromet)
					{
						Cumulus.LogMessage("Latest reading: " + cumulus.LatestImetReading);
					}
				}

				ini.Flush();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error writing today.ini:");
			}
		}

		/// <summary>
		/// calculate the start of today in UTC
		/// </summary>
		/// <returns>timestamp of start of today UTC</returns>
		/*
		private DateTime StartOfTodayUTC()
		{
			DateTime now = DateTime.Now;

			int y = now.Year;
			int m = now.Month;
			int d = now.Day;

			return new DateTime(y, m, d, 0, 0, 0).ToUniversalTime();
		}
		*/

		/// <summary>
		/// calculate the start of yesterday in UTC
		/// </summary>
		/// <returns>timestamp of start of yesterday UTC</returns>
		/*
		private DateTime StartOfYesterdayUTC()
		{
			DateTime yesterday = DateTime.Now.AddDays(-1);

			int y = yesterday.Year;
			int m = yesterday.Month;
			int d = yesterday.Day;

			return new DateTime(y, m, d, 0, 0, 0).ToUniversalTime();
		}
		*/

		/// <summary>
		/// calculate the start of this year in UTC
		/// </summary>
		/// <returns>timestamp of start of year in UTC</returns>
		/*
		private DateTime StartOfYearUTC()
		{
			DateTime now = DateTime.Now;
			int y = now.Year;

			return new DateTime(y, 1, 1, 0, 0, 0).ToUniversalTime();
		}
		*/

		/// <summary>
		/// calculate the start of this month in UTC
		/// </summary>
		/// <returns>timestamp of start of month in UTC</returns>
		/*
		private DateTime StartOfMonthUTC()
		{
			DateTime now = DateTime.Now;
			int y = now.Year;
			int m = now.Month;

			return new DateTime(y, m, 1, 0, 0, 0).ToUniversalTime();
		}
		*/

		/// <summary>
		/// calculate the start of this year in OAdate
		/// </summary>
		/// <returns>timestamp of start of year in OAdate</returns>
		/*
		private int StartOfYearOADate()
		{
			DateTime now = DateTime.Now;
			int y = now.Year;

			return (int) new DateTime(y, 1, 1, 0, 0, 0).ToOADate();
		}
		*/

		/// <summary>
		/// calculate the start of this month in OADate
		/// </summary>
		/// <returns>timestamp of start of month in OADate</returns>
		/*
		private int StartOfMonthOADate()
		{
			DateTime now = DateTime.Now;
			int y = now.Year;
			int m = now.Month;

			return (int) new DateTime(y, m, 1, 0, 0, 0).ToOADate();
		}
		*/

		public void UpdatePressureTrendString()
		{
			double threeHourlyPressureChangeMb = 0;

			switch (cumulus.Units.Press)
			{
				case 0:
				case 1:
					threeHourlyPressureChangeMb = presstrendval * 3;
					break;
				case 2:
					threeHourlyPressureChangeMb = presstrendval * 3 / 0.0295333727;
					break;
			}

			if (threeHourlyPressureChangeMb > 6) Presstrendstr = cumulus.Risingveryrapidly;
			else if (threeHourlyPressureChangeMb > 3.5) Presstrendstr = cumulus.Risingquickly;
			else if (threeHourlyPressureChangeMb > 1.5) Presstrendstr = cumulus.Rising;
			else if (threeHourlyPressureChangeMb > 0.1) Presstrendstr = cumulus.Risingslowly;
			else if (threeHourlyPressureChangeMb > -0.1) Presstrendstr = cumulus.Steady;
			else if (threeHourlyPressureChangeMb > -1.5) Presstrendstr = cumulus.Fallingslowly;
			else if (threeHourlyPressureChangeMb > -3.5) Presstrendstr = cumulus.Falling;
			else if (threeHourlyPressureChangeMb > -6) Presstrendstr = cumulus.Fallingquickly;
			else
				Presstrendstr = cumulus.Fallingveryrapidly;
		}

		public string Presstrendstr { get; set; }

		public void CheckMonthlyAlltime(string index, double? value, bool higher, DateTime timestamp)
		{
			if (value == null)
				return;

			lock (monthlyalltimeIniThreadLock)
			{
				bool recordbroken;

				// Make the delta relate to the precision for derived values such as feels like
				string[] derivedVals = { "HighHeatIndex", "HighAppTemp", "LowAppTemp", "LowChill", "HighHumidex", "HighDewPoint", "LowDewPoint", "HighFeelsLike", "LowFeelsLike" };

				double epsilon = derivedVals.Contains(index) ? Math.Pow(10, -cumulus.TempDPlaces) : 0.001; // required difference for new record

				int month;
				int day;
				int year;


				// Determine month day and year
				if (cumulus.RolloverHour == 0)
				{
					month = timestamp.Month;
					day = timestamp.Day;
					year = timestamp.Year;
				}
				else
				{
					TimeZoneInfo tz = TimeZoneInfo.Local;
					DateTime adjustedTS;

					if (cumulus.Use10amInSummer && tz.IsDaylightSavingTime(timestamp))
					{
						// Locale is currently on Daylight (summer) time
						adjustedTS = timestamp.AddHours(-10);
					}
					else
					{
						// Locale is currently on Standard time or unknown
						adjustedTS = timestamp.AddHours(-9);
					}

					month = adjustedTS.Month;
					day = adjustedTS.Day;
					year = adjustedTS.Year;
				}

				AllTimeRec rec = MonthlyRecs[month][index];

				double oldvalue = rec.Val;
				//DateTime oldts = monthlyrecarray[index, month].timestamp;

				if (higher)
				{
					// check new value is higher than existing record
					recordbroken = (value - oldvalue >= epsilon);
				}
				else
				{
					// check new value is lower than existing record
					recordbroken = (oldvalue - value >= epsilon);
				}

				if (recordbroken)
				{
					// records which apply to whole days or months need their timestamps adjusting
					if ((index == "MonthlyRain") || (index == "DailyRain"))
					{
						DateTime CurrentMonthTS = new DateTime(year, month, day);
						SetMonthlyAlltime(rec, value.Value, CurrentMonthTS);
					}
					else
					{
						SetMonthlyAlltime(rec, value.Value, timestamp);
					}
				}
			}
		}

		private static string FormatDateTime(string fmt, DateTime timestamp)
		{
			return timestamp.ToString(fmt);
		}

		private static string FormatDateTime(string fmt, DateOnly timestamp)
		{
			return timestamp.ToString(fmt);
		}

		public int CurrentDay { get; set; }

		public int CurrentMonth { get; set; }

		public int CurrentYear { get; set; }

		/// <summary>
		/// Indoor relative humidity in %
		/// </summary>
		public int? IndoorHum { get; set; } = null;

		/// <summary>
		/// Indoor temperature in C
		/// </summary>
		public double? IndoorTemp { get; set; } = null;

		/// <summary>
		/// Solar Radiation in W/m2
		/// </summary>
		public int? SolarRad { get; set; } = null;

		/// <summary>
		/// UV index
		/// </summary>
		public double? UV { get; set; } = null;


		/// <summary>
		/// Sea-level pressure
		/// </summary>
		public double? Pressure { get; set; } = null;

		public double? StationPressure { get; set; } = null;

		public string Forecast { get; set; } = "Forecast: ";

		/// <summary>
		/// Outdoor temp
		/// </summary>
		public double? Temperature = null;

		/// <summary>
		/// Outdoor dew point
		/// </summary>
		public double? Dewpoint { get; set; } = null;

		/// <summary>
		/// Wind chill
		/// </summary>
		public double? WindChill { get; set; } = null;

		/// <summary>
		/// Outdoor relative humidity in %
		/// </summary>
		public int? Humidity { get; set; } = null;

		/// <summary>
		/// Apparent temperature
		/// </summary>
		public double? ApparentTemp { get; set; } = null;

		/// <summary>
		/// Heat index
		/// </summary>
		public double? HeatIndex { get; set; } = null;

		/// <summary>
		/// Humidex
		/// </summary>
		public double? Humidex { get; set; } = null;

		/// <summary>
		/// Feels like (JAG/TI)
		/// </summary>
		public double? FeelsLike { get; set; } = null;


		/// <summary>
		/// Latest wind speed/gust
		/// </summary>
		public double? WindLatest { get; set; } = null;

		/// <summary>
		/// Average wind speed
		/// </summary>
		public double? WindAverage { get; set; } = null;

		/// <summary>
		/// Peak wind gust in last 10 minutes
		/// </summary>
		public double? RecentMaxGust { get; set; } = null;

		/// <summary>
		/// Wind direction in degrees
		/// </summary>
		public int? Bearing { get; set; } = null;

		/// <summary>
		/// Wind direction as compass points
		/// </summary>
		public string BearingText { get; set; } = "-";

		/// <summary>
		/// Wind direction in degrees
		/// </summary>
		public int? AvgBearing { get; set; } = null;

		/// <summary>
		/// Wind direction as compass points
		/// </summary>
		public string AvgBearingText { get; set; } = "-";

		/// <summary>
		/// Rainfall today
		/// </summary>
		public double? RainToday { get; set; } = null;

		/// <summary>
		/// Rain this month
		/// </summary>
		public double RainMonth { get; set; } = 0;

		/// <summary>
		/// Rain this year
		/// </summary>
		public double RainYear { get; set; } = 0;

		/// <summary>
		/// Current rain rate
		/// </summary>
		public double? RainRate { get; set; } = 0;

		public double ET { get; set; }

		public double LightValue { get; set; }

		public double HeatingDegreeDays { get; set; }

		public double CoolingDegreeDays { get; set; }

		public double GrowingDegreeDaysThisYear1 { get; set; }
		public double GrowingDegreeDaysThisYear2 { get; set; }

		public int tempsamplestoday { get; set; }

		public double TempTotalToday { get; set; }

		public double ChillHours { get; set; }

		public double midnightraincount { get; set; }

		public int MidnightRainResetDay { get; set; }


		public DateTime lastSpikeRemoval = DateTime.MinValue;
		private double previousPress = 9999;
		public double previousGust = 999;
		private double previousWind = 999;
		private int previousHum = 999;
		private double previousTemp = 999;


		public void UpdateDegreeDays(int interval)
		{
			if (!Temperature.HasValue)
				return;

			if (Temperature.Value < cumulus.NOAAconf.HeatThreshold)
				HeatingDegreeDays += (((cumulus.NOAAconf.HeatThreshold - Temperature.Value) * interval) / 1440);

			if (Temperature > cumulus.NOAAconf.CoolThreshold)
				CoolingDegreeDays += (((Temperature.Value - cumulus.NOAAconf.CoolThreshold) * interval) / 1440);
		}

		/// <summary>
		/// Wind run for today
		/// </summary>
		public double WindRunToday { get; set; } = 0;

		/// <summary>
		/// Extra Temps
		/// </summary>
		public double?[] ExtraTemp { get; set; }

		/// <summary>
		/// User allocated Temps
		/// </summary>
		public double?[] UserTemp { get; set; }

		/// <summary>
		/// Extra Humidity
		/// </summary>
		public double?[] ExtraHum { get; set; }

		/// <summary>
		/// Extra dewpoint
		/// </summary>
		public double?[] ExtraDewPoint { get; set; }

		/// <summary>
		/// Soil Temp 1-16 in C
		/// </summary>
		public double?[] SoilTemp { get; set; }

		public double? RainYesterday { get; set; }

		public double RainLastHour { get; set; }

		public int?[] SoilMoisture { get; set; }

		public double?[] AirQuality { get; set; }
		public double?[] AirQualityAvg { get; set; }

		public int? CO2 { get; set; }
		public int? CO2_24h { get; set; }
		public double? CO2_pm2p5 { get; set; }
		public double? CO2_pm2p5_24h { get; set; }
		public double? CO2_pm10 { get; set; }
		public double? CO2_pm10_24h { get; set; }
		public double? CO2_temperature { get; set; }
		public double? CO2_humidity { get; set; }

		public int LeakSensor1 { get; set; }
		public int LeakSensor2 { get; set; }
		public int LeakSensor3 { get; set; }
		public int LeakSensor4 { get; set; }

		public double LightningDistance { get; set; }
		public DateTime LightningTime { get; set; }
		public int LightningStrikesToday { get; set; }

		public double?[] LeafTemp { get; set; }

		public double?[] LeafWetness { get; set; }

		public double SunshineHours { get; set; } = 0;

		public double YestSunshineHours { get; set; } = 0;

		public double SunshineToMidnight { get; set; }

		public double SunHourCounter { get; set; }

		public double StartOfDaySunHourCounter { get; set; }

		public int? CurrentSolarMax { get; set; } = null;

		public double RG11RainToday { get; set; }

		public double RainSinceMidnight { get; set; }

		/// <summary>
		/// Checks whether a new day has started and does a roll-over if necessary
		/// </summary>
		/// <param name="oadate"></param>
		/*
		public void CheckForRollover(int oadate)
		{
			if (oadate != LastDailySummaryOADate)
			{
				DoRollover();
			}
		}
		*/

		/*
		private void DoRollover()
		{
			//throw new NotImplementedException();
		}
		*/

		/// <summary>
		///
		/// </summary>
		/// <param name="later"></param>
		/// <param name="earlier"></param>
		/// <returns>Difference in minutes</returns>
		/*
		private int TimeDiff(DateTime later, DateTime earlier)
		{
			TimeSpan diff = later - earlier;

			return (int) Math.Round(diff.TotalMinutes);
		}
		*/

		public void StartMinuteTimer()
		{
			lastMinute = DateTime.Now.Minute;
			lastHour = DateTime.Now.Hour;
			secondTimer = new Timer(500);
			secondTimer.Elapsed += SecondTimer;
			secondTimer.Start();
		}

		public void StopMinuteTimer()
		{
			if (secondTimer != null) secondTimer.Stop();
		}

		public void SecondTimer(object sender, ElapsedEventArgs e)
		{
			var timeNow = DateTime.Now; // b3085 change to using a single fixed point in time to make it independent of how long the process takes

			if (timeNow.Minute != lastMinute)
			{
				lastMinute = timeNow.Minute;

				if ((timeNow.Minute % 10) == 0)
				{
					//TenMinuteChanged(timeNow);
					TenMinuteChanged();
				}

				if (timeNow.Hour != lastHour)
				{
					lastHour = timeNow.Hour;
					HourChanged(timeNow);
					MinuteChanged(timeNow);

					// If it is rollover do the backup
					if (timeNow.Hour == Math.Abs(cumulus.GetHourInc()))
					{
						cumulus.BackupData(true, timeNow);
					}
				}
				else
				{
					MinuteChanged(timeNow);
				}

				if (DataStopped)
				{
					// No data coming in, do not do anything else
					// check if we want to exit on data stopped
					if (cumulus.ProgramOptions.DataStoppedExit && DataStoppedTime.AddMinutes(cumulus.ProgramOptions.DataStoppedMins) < DateTime.Now)
					{
						Cumulus.LogMessage($"*** Exiting Cumulus due to Data Stopped condition for > {cumulus.ProgramOptions.DataStoppedMins} minutes");
						Program.exitSystem = true;
					}
					return;
				}
			}

			// send current data to web-socket every 5 seconds, unless it has already been sent within the 10 seconds
			if (LastDataReadTimestamp.AddSeconds(5) < timeNow && (int)timeNow.TimeOfDay.TotalMilliseconds % 10000 <= 500)
			{
				_ = sendWebSocketData();
			}
		}

		internal async Task sendWebSocketData(bool wait = false)
		{
			// Don't do anything if there are no clients connected
			if (cumulus.WebSock.ConnectedClients == 0)
			{
				if (webSocketSemaphore.CurrentCount == 0)
				{
					webSocketSemaphore.Release();
				}
				return;
			}

			// Return control to the calling method immediately.
			await Task.Yield();

			// send current data to web-socket
			try
			{
				// if we already have an update queued, don't add to the wait queue. Otherwise we get hundreds queued up during catch-up
				// Zero wait time for the ws lock object unless wait = true
				if (!webSocketSemaphore.Wait(wait ? 0 : 600))
				{
					cumulus.LogDebugMessage("sendWebSocketData: Update already running, skipping this one");
					return;
				}

				StringBuilder windRoseData = new StringBuilder(80);

				lock (windcounts)
				{
					// no need to use multiplier as rose data is all relative
					windRoseData.Append(windcounts[0].ToString(cumulus.WindFormat, invNum));

					for (var i = 1; i < cumulus.NumWindRosePoints; i++)
					{
						windRoseData.Append(',');
						windRoseData.Append(windcounts[i].ToString(cumulus.WindFormat, invNum));
					}
				}

				string stormRainStart = StartOfStorm == DateTime.MinValue ? "-----" : StartOfStorm.ToString("d");

				var data = new WebSocketData(cumulus, Temperature, Humidity, TempTotalToday / tempsamplestoday, IndoorTemp, Dewpoint, WindChill, IndoorHum,
					Pressure, WindLatest, WindAverage, RecentMaxGust, WindRunToday, Bearing, AvgBearing, RainToday, RainYesterday, RainMonth, RainYear, RainRate,
					RainLastHour, HeatIndex, Humidex, ApparentTemp, temptrendval, presstrendval, HiLoToday.HighGust, HiLoToday.HighGustTime.ToString("HH:mm"), HiLoToday.HighWind,
					HiLoToday.HighGustBearing, cumulus.Units.WindText, BearingRangeFrom10, BearingRangeTo10, windRoseData.ToString(), HiLoToday.HighTemp, HiLoToday.LowTemp,
					HiLoToday.HighTempTime.ToString("HH:mm"), HiLoToday.LowTempTime.ToString("HH:mm"), HiLoToday.HighPress, HiLoToday.LowPress, HiLoToday.HighPressTime.ToString("HH:mm"),
					HiLoToday.LowPressTime.ToString("HH:mm"), HiLoToday.HighRainRate, HiLoToday.HighRainRateTime.ToString("HH:mm"), HiLoToday.HighHumidity, HiLoToday.LowHumidity,
					HiLoToday.HighHumidityTime.ToString("HH:mm"), HiLoToday.LowHumidityTime.ToString("HH:mm"), cumulus.Units.PressText, cumulus.Units.TempText, cumulus.Units.RainText,
					HiLoToday.HighDewPoint, HiLoToday.LowDewPoint, HiLoToday.HighDewPointTime.ToString("HH:mm"), HiLoToday.LowDewPointTime.ToString("HH:mm"), HiLoToday.LowWindChill,
					HiLoToday.LowWindChillTime.ToString("HH:mm"), SolarRad, HiLoToday.HighSolar, HiLoToday.HighSolarTime.ToString("HH:mm"), UV, HiLoToday.HighUv,
					HiLoToday.HighUvTime.ToString("HH:mm"), forecaststr, getTimeString(cumulus.SunRiseTime), getTimeString(cumulus.SunSetTime),
					getTimeString(cumulus.MoonRiseTime), getTimeString(cumulus.MoonSetTime), HiLoToday.HighHeatIndex, HiLoToday.HighHeatIndexTime.ToString("HH:mm"), HiLoToday.HighAppTemp,
					HiLoToday.LowAppTemp, HiLoToday.HighAppTempTime.ToString("HH:mm"), HiLoToday.LowAppTempTime.ToString("HH:mm"), CurrentSolarMax,
					AllTime.HighPress.Val, AllTime.LowPress.Val, SunshineHours, CompassPoint(DominantWindBearing), LastRainTip,
					HiLoToday.HighHourlyRain, HiLoToday.HighHourlyRainTime.ToString("HH:mm"), "F" + cumulus.Beaufort(HiLoToday.HighWind), "F" + cumulus.Beaufort(WindAverage ?? 0), cumulus.BeaufortDesc(WindAverage ?? 0),
					LastDataReadTimestamp.ToString("HH:mm:ss"), DataStopped, StormRain, stormRainStart, CloudBase, cumulus.CloudBaseInFeet ? "ft" : "m", RainLast24Hour,
					cumulus.LowTempAlarm.Triggered, cumulus.HighTempAlarm.Triggered, cumulus.TempChangeAlarm.UpTriggered, cumulus.TempChangeAlarm.DownTriggered, cumulus.HighRainTodayAlarm.Triggered, cumulus.HighRainRateAlarm.Triggered,
					cumulus.LowPressAlarm.Triggered, cumulus.HighPressAlarm.Triggered, cumulus.PressChangeAlarm.UpTriggered, cumulus.PressChangeAlarm.DownTriggered, cumulus.HighGustAlarm.Triggered, cumulus.HighWindAlarm.Triggered,
					cumulus.SensorAlarm.Triggered, cumulus.BatteryLowAlarm.Triggered, cumulus.SpikeAlarm.Triggered, cumulus.UpgradeAlarm.Triggered,
					cumulus.HttpUploadAlarm.Triggered, cumulus.MySqlUploadAlarm.Triggered, cumulus.IsRainingAlarm.Triggered,
					FeelsLike, HiLoToday.HighFeelsLike, HiLoToday.HighFeelsLikeTime.ToString("HH:mm"), HiLoToday.LowFeelsLike, HiLoToday.LowFeelsLikeTime.ToString("HH:mm"),
					HiLoToday.HighHumidex, HiLoToday.HighHumidexTime.ToString("HH:mm"));


				var ser = new DataContractJsonSerializer(typeof(WebSocketData));

				using var stream = new MemoryStream();
				ser.WriteObject(stream, data);
				stream.Position = 0;
				cumulus.WebSock.SendMessage(new StreamReader(stream).ReadToEnd());

				// ** CMX 3 - We can't be sure when the broadcast completes, so the best we can do is wait a short time
				// ** CMX 4 - the broadacst is now awaitable, so we can run it synchronously - therefore now no need to add an artifical delay
				//Thread.Sleep(500);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "sendWebSocketData: Error");
			}
			finally
			{
				webSocketSemaphore.Release();
			}
		}

		private static string getTimeString(DateTime time)
		{
			if (time <= DateTime.MinValue)
			{
				return "-----";
			}
			else
			{
				return time.ToString("HH:mm");
			}
		}

		private static string getTimeString(TimeSpan timespan)
		{
			try
			{
				if (timespan.TotalSeconds < 0)
				{
					return "-----";
				}

				DateTime dt = DateTime.MinValue.Add(timespan);

				return getTimeString(dt);
			}
			catch (Exception ex)
			{
				Program.cumulus.LogExceptionMessage(ex, "getTimeString: Exception caught");
				return "-----";
			}
		}

		private void RemoveOldRecentData(DateTime ts)
		{
			var deleteTime = ts.AddDays(-7);

			_ = Database.Execute("delete from RecentData where Timestamp < ?", deleteTime.ToUniversalTime());
		}

		private static void ClearAlarms()
		{
			cumulus.DataStoppedAlarm.Clear();
			cumulus.BatteryLowAlarm.Clear();
			cumulus.SensorAlarm.Clear();
			cumulus.SpikeAlarm.Clear();
			cumulus.UpgradeAlarm.Clear();
			cumulus.HttpUploadAlarm.Clear();
			cumulus.MySqlUploadAlarm.Clear();
			cumulus.HighWindAlarm.Clear();
			cumulus.HighGustAlarm.Clear();
			cumulus.HighRainRateAlarm.Clear();
			cumulus.HighRainTodayAlarm.Clear();
			cumulus.IsRainingAlarm.Clear();
			cumulus.HighPressAlarm.Clear();
			cumulus.LowPressAlarm.Clear();
			cumulus.HighTempAlarm.Clear();
			cumulus.LowTempAlarm.Clear();
			cumulus.TempChangeAlarm.Clear();
			cumulus.PressChangeAlarm.Clear();
		}

		private void MinuteChanged(DateTime now)
		{
			//TODO: Do we need to alter this?
			CheckForDataStopped();

			// Reset the values to null if no data received for 5 minutes - at 1 minute before the 5 minutes on the clock
			if ((now.Minute + 1) % 5 == 0)
				dataValuesUpdated.CheckDataValuesForUpdate();

			CurrentSolarMax = AstroLib.SolarMax(now, cumulus.Longitude, cumulus.Latitude, AltitudeM(cumulus.Altitude), out SolarElevation, cumulus.SolarOptions);

			if (!DataStopped)
			{
				//if (cumulus.StationOptions.NoSensorCheck)

				// increment wind run by one minute's worth of average speed
				if (WindAverage.HasValue)
				{
					WindRunToday += WindAverage.Value * WindRunHourMult[cumulus.Units.Wind] / 60.0;

					CheckForWindrunHighLow(now);

					CalculateDominantWindBearing(AvgBearing, WindAverage, 1);
				}

				if (Temperature.HasValue && Temperature.Value < cumulus.ChillHourThreshold)
				{
					// add 1 minute to chill hours
					ChillHours += 1.0 / 60.0;
				}

				// update sunshine hours
				if (cumulus.SolarOptions.UseBlakeLarsen)
				{
					ReadBlakeLarsenData();
				}
				else if ((SolarRad > (CurrentSolarMax * cumulus.SolarOptions.SunThreshold / 100.0)) && (SolarRad >= cumulus.SolarOptions.SolarMinimum))
				{
					SunshineHours += 1.0 / 60.0;
				}

				// update heating/cooling degree days
				UpdateDegreeDays(1);

				/*
				weatherDataCollection.Add(new WeatherData
				{
					//station = this,
					DT = DateTime.Now,
					WindSpeed = WindLatest,
					WindAverage = WindAverage,
					OutdoorTemp = OutdoorTemperature,
					Pressure = Pressure,
					Raintotal = RainToday
				});

				while (weatherDataCollection[0].DT < now.AddHours(-1))
				{
					weatherDataCollection.RemoveAt(0);
				}
				*/

				if (!first_temp && Temperature.HasValue)
				{
					// update temperature average items
					tempsamplestoday++;
					TempTotalToday += Temperature.Value;
				}

				AddRecentDataWithAq(now, WindAverage, RecentMaxGust, WindLatest, Bearing, AvgBearing, Temperature, WindChill, Dewpoint, HeatIndex, Humidity,
					Pressure, RainToday, SolarRad, UV, Raincounter, FeelsLike, Humidex, ApparentTemp, IndoorTemp, IndoorHum, CurrentSolarMax, RainRate);
				DoTrendValues(now);
				DoPressTrend("Enable Cumulus pressure trend");

				// calculate ET just before the hour so it is included in the correct day at roll over - only affects 9am met days really
				if (cumulus.StationOptions.CalculatedET && now.Minute == 59)
				{
					CalculateEvaoptranspiration(now);
				}


				if (now.Minute % cumulus.logints[cumulus.DataLogInterval] == 0)
				{
					_ = cumulus.DoLogFile(now, true);  // let this run in background

					_ = cumulus.DoExtraLogFile(now);  // let this run in background

					if (cumulus.AirLinkInEnabled || cumulus.AirLinkOutEnabled)
					{
						_ = cumulus.DoAirLinkLogFile(now);  // let this run in background
					}
				}

				// Custom MySQL update - minutes interval
				if (cumulus.MySqlStuff.Settings.CustomMins.Enabled && now.Minute % cumulus.MySqlStuff.Settings.CustomMins.Interval == 0)
				{
					_ = cumulus.MySqlStuff.CustomMinutesTimerTick();  // let this run in background
				}

				// Custom HTTP update - minutes interval
				if (cumulus.CustomHttpMinutesEnabled && now.Minute % cumulus.CustomHttpMinutesInterval == 0)
				{
					_ = cumulus.CustomHttpMinutesUpdate();  // let this run in background
				}

				// Custom Log files - interval logs
				_ = cumulus.DoCustomIntervalLogs(now);

				if (cumulus.WebIntervalEnabled && cumulus.SynchronisedWebUpdate && (now.Minute % cumulus.UpdateInterval == 0))
				{
					if (cumulus.WebUpdating == 1)
					{
						// Skip this update interval
						Cumulus.LogMessage("Warning, previous web update is still in progress, first chance, skipping this interval");
						cumulus.WebUpdating++;
					}
					else if (cumulus.WebUpdating >= 2)
					{
						Cumulus.LogMessage("Warning, previous web update is still in progress,second chance, aborting connection");
						if (cumulus.ftpThread.ThreadState == System.Threading.ThreadState.Running)
							cumulus.ftpThread.Interrupt();
						Cumulus.LogMessage("Trying new web update");
						cumulus.WebUpdating = 1;
						cumulus.ftpThread = new Thread(() => { cumulus.DoHTMLFiles(now); })
						{
							IsBackground = true
						};
						cumulus.ftpThread.Start();
					}
					else
					{
						cumulus.WebUpdating = 1;
						cumulus.ftpThread = new Thread(() => { cumulus.DoHTMLFiles(now); })
						{
							IsBackground = true
						};
						cumulus.ftpThread.Start();
					}
				}
				// We also want to kick off DoHTMLFiles if local copy is enabled
				else if (cumulus.FtpOptions.LocalCopyEnabled && cumulus.SynchronisedWebUpdate && (now.Minute % cumulus.UpdateInterval == 0))
				{
					cumulus.ftpThread = new Thread(() => { cumulus.DoHTMLFiles(now); })
					{
						IsBackground = true
					};
					cumulus.ftpThread.Start();
				}

				if (cumulus.Wund.Enabled && (now.Minute % cumulus.Wund.Interval == 0) && cumulus.Wund.SynchronisedUpdate && !String.IsNullOrWhiteSpace(cumulus.Wund.ID))
				{
					_ = cumulus.Wund.DoUpdate(now);
				}

				if (cumulus.Windy.Enabled && (now.Minute % cumulus.Windy.Interval == 0) && !String.IsNullOrWhiteSpace(cumulus.Windy.ApiKey))
				{
					_ = cumulus.Windy.DoUpdate(now);
				}

				if (cumulus.WindGuru.Enabled && (now.Minute % cumulus.WindGuru.Interval == 0) && !String.IsNullOrWhiteSpace(cumulus.WindGuru.ID))
				{
					_  = cumulus.WindGuru.DoUpdate(now);
				}

				if (cumulus.AWEKAS.Enabled && (now.Minute % ((double)cumulus.AWEKAS.Interval / 60) == 0) && cumulus.AWEKAS.SynchronisedUpdate && !String.IsNullOrWhiteSpace(cumulus.AWEKAS.ID))
				{
					_ = cumulus.AWEKAS.DoUpdate(now);
				}

				if (cumulus.WCloud.Enabled && (now.Minute % cumulus.WCloud.Interval == 0) && !String.IsNullOrWhiteSpace(cumulus.WCloud.ID))
				{
					_ = cumulus.WCloud.DoUpdate(now);
				}

				if (cumulus.OpenWeatherMap.Enabled && (now.Minute % cumulus.OpenWeatherMap.Interval == 0) && !string.IsNullOrWhiteSpace(cumulus.OpenWeatherMap.ID))
				{
					_ = cumulus.OpenWeatherMap.DoUpdate(now);
				}

				if (cumulus.PWS.Enabled && (now.Minute % cumulus.PWS.Interval == 0) && !String.IsNullOrWhiteSpace(cumulus.PWS.ID) && !String.IsNullOrWhiteSpace(cumulus.PWS.PW))
				{
					_ = cumulus.PWS.DoUpdate(now);
				}

				if (cumulus.WOW.Enabled && (now.Minute % cumulus.WOW.Interval == 0) && !String.IsNullOrWhiteSpace(cumulus.WOW.ID) && !String.IsNullOrWhiteSpace(cumulus.WOW.PW))
				{
					_ = cumulus.WOW.DoUpdate(now);
				}

				if (cumulus.APRS.Enabled && (now.Minute % cumulus.APRS.Interval == 0) && !String.IsNullOrWhiteSpace(cumulus.APRS.ID))
				{
					_ = cumulus.APRS.DoUpdate(now);
				}

				/*
				if (cumulus.Twitter.Enabled && (now.Minute % cumulus.Twitter.Interval == 0) && !String.IsNullOrWhiteSpace(cumulus.Twitter.ID) && !String.IsNullOrWhiteSpace(cumulus.Twitter.PW))
				{
					cumulus.UpdateTwitter();
				}
				*/

				if (cumulus.xapEnabled)
				{
					using Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, cumulus.xapPort);

					byte[] data = Encoding.ASCII.GetBytes(cumulus.xapHeartbeat);

					sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

					sock.SendTo(data, iep1);

					var timeUTC = now.ToUniversalTime().ToString("HH:mm");
					var dateISO = now.ToUniversalTime().ToString("yyyyMMdd");

					var xapReport = new StringBuilder("", 1024);
					xapReport.Append("xap-header\n{\nv=12\nhop=1\n");
					xapReport.Append($"uid=FF{cumulus.xapUID}00\n");
					xapReport.Append("class=weather.report\n");
					xapReport.Append($"source={cumulus.xapsource}\n");
					xapReport.Append("}\n");
					xapReport.Append("weather.report\n{\n");
					xapReport.Append($"UTC={timeUTC}\nDATE={dateISO}\n");
					if (WindAverage.HasValue)
					{
						xapReport.Append($"WindM={ConvertUserWindToMPH(WindAverage):F1}\n");
						xapReport.Append($"WindK={ConvertUserWindToKPH(WindAverage):F1}\n");
					}
					if (RecentMaxGust.HasValue)
					{
						xapReport.Append($"WindGustsM={ConvertUserWindToMPH(RecentMaxGust):F1}\n");
						xapReport.Append($"WindGustsK={ConvertUserWindToKPH(RecentMaxGust):F1}\n");
					}
					xapReport.Append($"WindDirD={Bearing}\n");
					xapReport.Append($"WindDirC={AvgBearing}\n");
					if (Temperature.HasValue)
					{
						xapReport.Append($"TempC={ConvertUserTempToC(Temperature):F1}\n");
						xapReport.Append($"TempF={ConvertUserTempToF(Temperature):F1}\n");
					}
					xapReport.Append($"DewC={ConvertUserTempToC(Dewpoint):F1}\n");
					xapReport.Append($"DewF={ConvertUserTempToF(Dewpoint):F1}\n");
					if (Pressure.HasValue)
						xapReport.Append($"AirPressure={ConvertUserPressToMB(Pressure):F1}\n");
					if (RainToday.HasValue)
						xapReport.Append($"Rain={ConvertUserRainToMM(RainToday.Value):F1}\n");
					xapReport.Append('}');

					data = Encoding.ASCII.GetBytes(xapReport.ToString());

					sock.SendTo(data, iep1);

					sock.Close();
				}

				var wxfile = cumulus.StdWebFiles.SingleOrDefault(item => item.LocalFileName == "wxnow.txt");
				if (wxfile.Create)
				{
					CreateWxnowFile();
				}
			}
			else
			{
				Cumulus.LogMessage("Minimum data set of pressure, temperature, and wind is not available and NoSensorCheck is not enabled. Skip processing");
			}

			// Check for a new version of Cumulus once a day
			if (now.Minute == versionCheckTime.Minute && now.Hour == versionCheckTime.Hour)
			{
				Cumulus.LogMessage("Checking for latest Cumulus MX version...");
				_ = cumulus.GetLatestVersion();
			}

			// If not on windows, check for CPU temp
			try
			{
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists("/sys/class/thermal/thermal_zone0/temp"))
				{
					var raw = File.ReadAllText(@"/sys/class/thermal/thermal_zone0/temp");
					if (double.TryParse(raw, out var val))
					{
						cumulus.CPUtemp = ConvertTempCToUser(val / 1000).Value;
						cumulus.LogDebugMessage($"Current CPU temp = {cumulus.CPUtemp.ToString(cumulus.TempFormat)}{cumulus.Units.TempText}");
					}
					else
					{
						cumulus.LogDebugMessage($"Current CPU temp file '/sys/class/thermal/thermal_zone0/temp' does not contain a number = [{raw}]");
					}
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error reading CPU temperature");
			}
		}

		//private void TenMinuteChanged(DateTime now)
		private static void TenMinuteChanged()
		{
			cumulus.DoMoonPhase();
			cumulus.MoonAge = MoonriseMoonset.MoonAge();

			cumulus.RotateLogFiles();

			ClearAlarms();
		}

		private void HourChanged(DateTime now)
		{
			Cumulus.LogMessage("Hour changed: " + now.Hour);
			cumulus.DoSunriseAndSunset();
			cumulus.DoMoonImage();

			if (cumulus.HourlyForecast)
			{
				DoForecast("", true);
			}

			if (now.Hour == 0)
			{
				ResetMidnightRain(now);
				//RecalcSolarFactor(now);
			}

			int rollHour = Math.Abs(cumulus.GetHourInc());

			if (now.Hour == rollHour)
			{
				DayReset(now);
			}

			if (now.Hour == 0)
			{
				ResetSunshineHours(now);
				ResetMidnightTemperatures(now);
			}

			RemoveOldRecentData(now);
		}

		private void CheckForDataStopped()
		{
			// Check whether we have read data since the last clock minute.
			if ((LastDataReadTimestamp != DateTime.MinValue) && (LastDataReadTimestamp == SavedLastDataReadTimestamp) && (LastDataReadTimestamp < DateTime.Now))
			{
				// Data input appears to have has stopped
				if (!DataStopped)
				{
					DataStoppedTime = DateTime.Now;
					DataStopped = true;
				}
				cumulus.DataStoppedAlarm.Triggered = true;
				/*if (RestartIfDataStops)
				{
					Cumulus.LogMessage("*** Data input appears to have stopped, restarting");
					ApplicationExec(ParamStr(0), '', SW_SHOW);
					TerminateProcess(GetCurrentProcess, 0);
				}*/
				if (cumulus.ReportDataStoppedErrors)
				{
					Cumulus.LogMessage("*** Data input appears to have stopped");
				}
			}
			else
			{
				DataStopped = false;
				cumulus.DataStoppedAlarm.Triggered = false;
			}

			// save the time that data was last read so we can check in a minute's time that it's changed
			SavedLastDataReadTimestamp = LastDataReadTimestamp;
		}

		private void ReadBlakeLarsenData()
		{
			var blFile = cumulus.AppDir + "SRsunshine.dat";

			if (File.Exists(blFile))
			{
				try
				{
					using var sr = new StreamReader(blFile);
					string line = sr.ReadLine();
					SunshineHours = double.Parse(line, invNum);
					sr.ReadLine();
					sr.ReadLine();
					line = sr.ReadLine();
					IsSunny = (line == "True");
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Error reading SRsunshine.dat");
				}
			}
		}

		/*
		internal void UpdateDatabase(DateTime timestamp, int interval, bool updateHighsAndLows)
		// Add an entry to the database
		{
			double raininterval;

			if (prevraincounter == 0)
			{
				raininterval = 0;
			}
			else
			{
				raininterval = Raincounter - prevraincounter;
			}

			//using (cumulusEntities dataContext = new cumulusEntities())
			//{
			//    dataContext.AddToStandardData(newdata);

			//    // Submit the change to the database.
			//    try
			//    {
			//        dataContext.SaveChanges();
			//    }
			//    catch (Exception ex)
			//    {
			//        Trace.WriteLine(ex.ToString());
			//        Trace.Flush();
			//    }

			// reset highs and lows since last update
			loOutdoorTemperature = OutdoorTemperature;
			hiOutdoorTemperature = OutdoorTemperature;
			loIndoorTemperature = IndoorTemperature;
			hiIndoorTemperature = IndoorTemperature;
			loIndoorHumidity = IndoorHumidity;
			hiIndoorHumidity = IndoorHumidity;
			loOutdoorHumidity = Humidity;
			hiOutdoorHumidity = Humidity;
			loPressure = Pressure;
			hiPressure = Pressure;
			hiWind = WindAverage;
			hiGust = WindLatest;
			hiWindBearing = Bearing;
			hiGustBearing = Bearing;
			prevraincounter = Raincounter;
			hiRainRate = RainRate;
			hiDewPoint = OutdoorDewpoint;
			loDewPoint = OutdoorDewpoint;
			hiHeatIndex = HeatIndex;
			hiHumidex = Humidex;
			loWindChill = WindChill;
			hiApparentTemperature = ApparentTemperature;
			loApparentTemperature = ApparentTemperature;
		}
		*/


		public void CalculateEvaoptranspiration(DateTime date)
		{
			cumulus.LogDebugMessage("Calculating ET from data");

			try
			{
				var dateFrom = date.AddHours(-1);

				// get the min and max temps, humidity, pressure, and mean solar rad and wind speed for the last hour
				var result = Database.Query<EtData>("select avg(OutsideTemp) avgTemp, avg(Humidity) avgHum, avg(Pressure) avgPress, avg(SolarRad) avgSol, avg(SolarMax) avgSolMax, avg(WindSpeed) avgWind from RecentData where Timestamp >= ?", dateFrom.ToUniversalTime());

				if (result.Count == 0 || !result[0].avgTemp.HasValue || !result[0].avgHum.HasValue || !result[0].avgPress.HasValue || !result[0].avgSol.HasValue || !result[0].avgSolMax.HasValue || !result[0].avgWind.HasValue)
				{
					cumulus.LogDebugMessage($"No recent data found to calculate ET");
					return;
				}
				// finally calculate the ETo
				var newET = MeteoLib.Evapotranspiration(
					ConvertUserTempToC(result[0].avgTemp).Value,
					result[0].avgHum.Value,
					result[0].avgSol.Value,
					result[0].avgSolMax.Value,
					ConvertUserWindToMS(result[0].avgWind).Value,
					cumulus.StationOptions.AnemometerHeightM,
					ConvertUserPressureToHPa(result[0].avgPress).Value / 10
				);

				// convert to user units
				newET = ConvertRainMMToUser(newET);

				// DoET expects the running annual total to be sent
				DoET(AnnualETTotal + newET, date);
				cumulus.LogDebugMessage($"Calculated ET for the last hour={newET:0.000}, new values: today={ET:0.000}, annual={AnnualETTotal:0.000}");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error calculating hourly ET");
			}
		}


		public void AddRecentDataEntry(DateTime timestamp, double? windAverage, double? recentMaxGust, double? windLatest, int? bearing, int? avgBearing, double? outsidetemp,
			double? windChill, double? dewpoint, double? heatIndex, int? humidity, double? pressure, double? rainToday, int? solarRad, double? uv, double rainCounter, double? feelslike, double? humidex,
			double? appTemp, double? insideTemp, int? insideHum, int? solarMax, double? rainrate, double? pm2p5, double? pm10)
		{
			try
			{
				Database.InsertOrReplace(new RecentData()
				{
					Timestamp = timestamp,
					DewPoint = dewpoint,
					HeatIndex = heatIndex,
					Humidity = humidity,
					OutsideTemp = outsidetemp,
					Pressure = pressure,
					RainToday = rainToday,
					SolarRad = solarRad,
					UV = uv,
					WindAvgDir = avgBearing,
					WindGust = recentMaxGust,
					WindLatest = windLatest,
					WindChill = windChill,
					WindDir = bearing,
					WindSpeed = windAverage,
					raincounter = rainCounter,
					FeelsLike = feelslike,
					Humidex = humidex,
					AppTemp = appTemp,
					IndoorTemp = insideTemp,
					IndoorHumidity = insideHum,
					SolarMax = solarMax,
					RainRate = rainrate,
					Pm2p5 = pm2p5,
					Pm10 = pm10
				});
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "AddRecentDataEntry:");
			}
		}

		private void CreateWxnowFile()
		{
			// Jun 01 2003 08:07
			// 272/000g006t069r010p030P020h61b10150

			// 272 - wind direction - 272 degrees
			// 010 - wind speed - 10 mph

			// g015 - wind gust - 15 mph
			// t069 - temperature - 69 degrees F
			// r010 - rain in last hour in hundredths of an inch - 0.1 inches
			// p030 - rain in last 24 hours in hundredths of an inch - 0.3 inches
			// P020 - rain since midnight in hundredths of an inch - 0.2 inches
			// h61 - humidity 61% (00 = 100%)
			// b10153 - barometric pressure in tenths of a millibar - 1015.3 millibars

			var filename = cumulus.AppDir + cumulus.WxnowFile;
			var timestamp = DateTime.Now.ToString(@"MMM dd yyyy HH\:mm");

			int mphwind = Convert.ToInt32(ConvertUserWindToMPH(WindAverage));
			int mphgust = Convert.ToInt32(ConvertUserWindToMPH(RecentMaxGust ?? 0));
			// ftemp = trunc(TempF(OutsideTemp));
			string ftempstr = ThirdParty.WebUploadAprs.APRStemp(Temperature ?? 0, cumulus.Units.Temp);
			int in100rainlasthour = Convert.ToInt32(ConvertUserRainToIn(RainLastHour) * 100);
			int in100rainlast24hours = Convert.ToInt32(ConvertUserRainToIn(RainLast24Hour) * 100);
			int in100raintoday;
			if (cumulus.RolloverHour == 0)
				// use today's rain for safety
				in100raintoday = Convert.ToInt32(ConvertUserRainToIn(RainToday ?? 0) * 100);
			else
				// 0900 day, use midnight calculation
				in100raintoday = Convert.ToInt32(ConvertUserRainToIn(RainSinceMidnight) * 100);
			int mb10press = Convert.ToInt32(ConvertUserPressToMB(AltimeterPressure ?? 0) * 10);
			// For 100% humidity, send zero. For zero humidity, send 1
			int hum;
			if (Humidity.HasValue)
			{
				if (Humidity.Value == 0)
					hum = 1;
				else if (Humidity.Value == 100)
					hum = 0;
				else
					hum = Humidity.Value;
			}
			else
			{
				hum = 1;
			}

			string data = String.Format("{0:000}/{1:000}g{2:000}t{3}r{4:000}p{5:000}P{6:000}h{7:00}b{8:00000}", AvgBearing, mphwind, mphgust, ftempstr, in100rainlasthour,
				in100rainlast24hours, in100raintoday, hum, mb10press);

			if (cumulus.APRS.SendSolar && SolarRad.HasValue)
			{
				data += ThirdParty.WebUploadAprs.APRSsolarradStr(SolarRad.Value);
			}

			using StreamWriter file = new StreamWriter(filename, false);
			file.WriteLine(timestamp);
			file.WriteLine(data);
			file.Close();
		}

		private static double ConvertUserRainToIn(double value)
		{
			return cumulus.Units.Rain == 1 ? value : value / 25.4;
		}

		public static double? ConvertUserWindToMPH(double? value)
		{
			if (value == null)
				return null;

			return cumulus.Units.Wind switch
			{
				0 => value * 2.23693629,
				1 => value,
				2 => value * 0.621371,
				3 => value * 1.15077945,
				_ => 0,
			};
		}

		public static double ConvertUserWindToKnots(double value)
		{
			return cumulus.Units.Wind switch
			{
				0 => value * 1.943844,
				1 => value * 0.8689758,
				2 => value * 0.5399565,
				3 => value,
				_ => 0,
			};
		}

		public void ResetSunshineHours(DateTime logdate) // called at midnight irrespective of roll-over time
		{
			YestSunshineHours = SunshineHours;

			Cumulus.LogMessage("Reset sunshine hours, yesterday = " + YestSunshineHours);

			SunshineToMidnight = SunshineHours;
			SunshineHours = 0;
			StartOfDaySunHourCounter = SunHourCounter;
			WriteYesterdayFile(logdate);
		}

		public void ResetMidnightTemperatures(DateTime logdate) // called at midnight irrespective of roll-over time
		{
			HiLoYestMidnight.LowTemp = HiLoTodayMidnight.LowTemp;
			HiLoYestMidnight.HighTemp = HiLoTodayMidnight.HighTemp;
			HiLoYestMidnight.LowTempTime = HiLoTodayMidnight.LowTempTime;
			HiLoYestMidnight.HighTempTime = HiLoTodayMidnight.HighTempTime;

			HiLoTodayMidnight.LowTemp = 999;
			HiLoTodayMidnight.HighTemp = -999;

			WriteYesterdayFile(logdate);
		}

		/*
		private void RecalcSolarFactor(DateTime now) // called at midnight irrespective of roll-over time
		{
			if (cumulus.SolarFactorSummer > 0 && cumulus.SolarFactorWinter > 0)
			{
				// Calculate the solar factor from the day of the year
				// Use a cosine of the difference between summer and winter values
				int doy = now.DayOfYear;
				// take summer solstice as June 21 or December 21 (N & S hemispheres) - ignore leap years
				// sol = day 172 (North)
				// sol = day 355 (South)
				int sol = cumulus.Latitude >= 0 ? 172 : 355;
				int daysSinceSol = (doy - sol) % 365;
				double multiplier = Math.Cos((daysSinceSol / 365) * 2 * Math.PI);  // range +1/-1
				SolarFactor = (multiplier + 1) / 2;  // bring it into the range 0-1
			}
			else
			{
				SolarFactor = -1;
			}
		}
		*/

		public void SwitchToNormalRunning()
		{
			cumulus.NormalRunning = true;

			DoDayResetIfNeeded();
			DoTrendValues(DateTime.Now);
			cumulus.StartTimersAndSensors();
		}

		public void ResetMidnightRain(DateTime timestamp)
		{
			int mrrday = timestamp.Day;

			int mrrmonth = timestamp.Month;

			if (mrrday != MidnightRainResetDay)
			{
				midnightraincount = Raincounter;
				RainSinceMidnight = 0;
				MidnightRainResetDay = mrrday;
				Cumulus.LogMessage("Midnight rain reset, count = " + Raincounter + " time = " + timestamp.ToShortTimeString());
				if ((mrrday == 1) && (mrrmonth == 1) && (cumulus.StationType == StationTypes.VantagePro))
				{
					// special case: rain counter is about to be reset
					Cumulus.LogMessage("Special case, Davis station on 1st Jan. Set midnight rain count to zero");
					midnightraincount = 0;
				}
			}
		}

		public void DoIndoorHumidity(int? hum)
		{
			IndoorHum = hum;
			dataValuesUpdated.IndoorHum = true;
			HaveReadData = true;
		}

		public void DoIndoorTemp(double? temp)
		{
			IndoorTemp = temp ?? (temp + cumulus.Calib.InTemp.Offset);
			dataValuesUpdated.IndoorTemp = true;
			HaveReadData = true;
		}

		public void DoHumidity(int? humpar, DateTime timestamp)
		{
			if (!humpar.HasValue)
			{
				Humidity = null;
				return;
			}

			// Spike check
			if ((previousHum != 999) && (Math.Abs(humpar.Value - previousHum) > cumulus.Spike.HumidityDiff))
			{
				cumulus.LogSpikeRemoval("Humidity difference greater than specified; reading ignored");
				cumulus.LogSpikeRemoval($"NewVal={humpar.Value} OldVal={previousHum} SpikeHumidityDiff={cumulus.Spike.HumidityDiff:F1}");
				lastSpikeRemoval = DateTime.Now;
				cumulus.SpikeAlarm.LastError = $"Humidity difference greater than spike value - NewVal={humpar.Value} OldVal={previousHum}";
				cumulus.SpikeAlarm.Triggered = true;
				return;
			}
			previousHum = humpar.Value;

			if ((humpar.Value >= 98) && cumulus.StationOptions.Humidity98Fix)
			{
				Humidity = 100;
			}
			else
			{
				Humidity = humpar;
			}

			// apply offset and multipliers and round. This is different to C1, which truncates. I'm not sure why C1 does that
			Humidity = (int)Math.Round((Humidity.Value * Humidity.Value * cumulus.Calib.Hum.Mult2) + (Humidity.Value * cumulus.Calib.Hum.Mult) + cumulus.Calib.Hum.Offset);

			dataValuesUpdated.Humidity = true;

			if (Humidity.Value < 0)
			{
				Humidity = 0;
			}
			if (Humidity.Value > 100)
			{
				Humidity = 100;
			}

			if (Humidity.Value > (HiLoToday.HighHumidity ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighHumidity = Humidity.Value;
				HiLoToday.HighHumidityTime = timestamp;
				WriteTodayFile(timestamp, false);
			}
			if (Humidity.Value < (HiLoToday.LowHumidity ?? Cumulus.DefaultLoVal))
			{
				HiLoToday.LowHumidity = Humidity;
				HiLoToday.LowHumidityTime = timestamp;
				WriteTodayFile(timestamp, false);
			}
			if (Humidity.Value > ThisMonth.HighHumidity.Val)
			{
				ThisMonth.HighHumidity.Val = Humidity.Value;
				ThisMonth.HighHumidity.Ts = timestamp;
				WriteMonthIniFile();
			}
			if (Humidity.Value < ThisMonth.LowHumidity.Val)
			{
				ThisMonth.LowHumidity.Val = Humidity.Value;
				ThisMonth.LowHumidity.Ts = timestamp;
				WriteMonthIniFile();
			}
			if (Humidity.Value > ThisYear.HighHumidity.Val)
			{
				ThisYear.HighHumidity.Val = Humidity.Value;
				ThisYear.HighHumidity.Ts = timestamp;
				WriteYearIniFile();
			}
			if (Humidity.Value < ThisYear.LowHumidity.Val)
			{
				ThisYear.LowHumidity.Val = Humidity.Value;
				ThisYear.LowHumidity.Ts = timestamp;
				WriteYearIniFile();
			}
			if (Humidity.Value > AllTime.HighHumidity.Val)
			{
				SetAlltime(AllTime.HighHumidity, Humidity.Value, timestamp);
			}

			CheckMonthlyAlltime("HighHumidity", Humidity.Value, true, timestamp);

			if (Humidity.Value < AllTime.LowHumidity.Val)
			{
				SetAlltime(AllTime.LowHumidity, Humidity.Value, timestamp);
			}

			CheckMonthlyAlltime("LowHumidity", Humidity.Value, false, timestamp);
			HaveReadData = true;
		}

		public static double CalibrateTemp(double temp)
		{
			return (temp * temp * cumulus.Calib.Temp.Mult2) + (temp * cumulus.Calib.Temp.Mult) + cumulus.Calib.Temp.Offset;
		}

		public void DoTemperature(double? temp, DateTime timestamp)
		{
			if (!temp.HasValue)
			{
				Temperature = null;

				if ((cumulus.StationOptions.CalculatedDP || cumulus.DavisStation) && (Humidity ?? -1) != 0 && !cumulus.FineOffsetStation)
				{
					Dewpoint = null;
				}
				return;
			}

			dataValuesUpdated.Temperature = true;

			// Spike removal is in Celsius
			var tempC = ConvertUserTempToC(temp).Value;
			if (((Math.Abs(tempC - previousTemp) > cumulus.Spike.TempDiff) && (previousTemp != 999)) ||
				tempC >= cumulus.Limit.TempHigh || tempC <= cumulus.Limit.TempLow)
			{
				lastSpikeRemoval = DateTime.Now;
				cumulus.SpikeAlarm.LastError = $"Temp difference greater than spike value - NewVal={tempC.ToString(cumulus.TempFormat)} OldVal={previousTemp.ToString(cumulus.TempFormat)}";
				cumulus.SpikeAlarm.Triggered = true;
				cumulus.LogSpikeRemoval("Temp difference greater than specified; reading ignored");
				cumulus.LogSpikeRemoval($"NewVal={tempC.ToString(cumulus.TempFormat)} OldVal={previousTemp.ToString(cumulus.TempFormat)} SpikeTempDiff={cumulus.Spike.TempDiff.ToString(cumulus.TempFormat)} HighLimit={cumulus.Limit.TempHigh.ToString(cumulus.TempFormat)} LowLimit={cumulus.Limit.TempLow.ToString(cumulus.TempFormat)}");
				return;
			}
			previousTemp = tempC;

			// UpdateStatusPanel;
			// update global temp
			Temperature = CalibrateTemp(temp.Value);

			dataValuesUpdated.Temperature = true;

			first_temp = false;

			// Does this reading set any records or trigger any alarms?
			if (Temperature > AllTime.HighTemp.Val)
				SetAlltime(AllTime.HighTemp, Temperature.Value, timestamp);

			cumulus.HighTempAlarm.CheckAlarm(Temperature.Value);

			if (Temperature < AllTime.LowTemp.Val)
				SetAlltime(AllTime.LowTemp, Temperature.Value, timestamp);

			cumulus.LowTempAlarm.CheckAlarm(Temperature.Value);

			CheckMonthlyAlltime("HighTemp", Temperature.Value, true, timestamp);
			CheckMonthlyAlltime("LowTemp", Temperature.Value, false, timestamp);

			if (Temperature > (HiLoToday.HighTemp ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighTemp = Temperature;
				HiLoToday.HighTempTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (Temperature < (HiLoToday.LowTemp ?? Cumulus.DefaultLoVal))
			{
				HiLoToday.LowTemp = Temperature;
				HiLoToday.LowTempTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (Temperature > (HiLoTodayMidnight.HighTemp ?? Cumulus.DefaultLoVal))
			{
				HiLoTodayMidnight.HighTemp = Temperature;
				HiLoTodayMidnight.HighTempTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (Temperature < (HiLoTodayMidnight.LowTemp ?? Cumulus.DefaultLoVal))
			{
				HiLoTodayMidnight.LowTemp = Temperature;
				HiLoTodayMidnight.LowTempTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (Temperature > ThisMonth.HighTemp.Val)
			{
				ThisMonth.HighTemp.Val = Temperature.Value;
				ThisMonth.HighTemp.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (Temperature < ThisMonth.LowTemp.Val)
			{
				ThisMonth.LowTemp.Val = Temperature.Value;
				ThisMonth.LowTemp.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (Temperature > ThisYear.HighTemp.Val)
			{
				ThisYear.HighTemp.Val = Temperature.Value;
				ThisYear.HighTemp.Ts = timestamp;
				WriteYearIniFile();
			}

			if (Temperature < ThisYear.LowTemp.Val)
			{
				ThisYear.LowTemp.Val = Temperature.Value;
				ThisYear.LowTemp.Ts = timestamp;
				WriteYearIniFile();
			}

			// Calculate temperature range
			HiLoToday.TempRange = HiLoToday.HighTemp - HiLoToday.LowTemp;



			TempReadyToPlot = true;
			HaveReadData = true;
		}

		public void DoCloudBaseHeatIndex(DateTime timestamp)
		{
			if (Temperature is null || Dewpoint is null)
			{
				CloudBase = null;
				HeatIndex = null;
				return;
			}

			var tempinF = ConvertUserTempToF(Temperature).Value;
			var tempinC = ConvertUserTempToC(Temperature).Value;

			// Calculate cloud base
			if (Dewpoint.HasValue)
			{
				CloudBase = (int)Math.Floor((tempinF - ConvertUserTempToF(Dewpoint)).Value / 4.4 * 1000 / (cumulus.CloudBaseInFeet ? 1 : 3.2808399));
				if (CloudBase < 0)
					CloudBase = 0;
			}
			else
			{
				CloudBase = null;
			}


			if (Humidity.HasValue)
			{
				HeatIndex = ConvertTempCToUser(MeteoLib.HeatIndex(tempinC, Humidity.Value));

				if (HeatIndex.Value > (HiLoToday.HighHeatIndex ?? -999.0))
				{
					HiLoToday.HighHeatIndex = HeatIndex;
					HiLoToday.HighHeatIndexTime = timestamp;
					WriteTodayFile(timestamp, false);
				}

				if (HeatIndex > ThisMonth.HighHeatIndex.Val)
				{
					ThisMonth.HighHeatIndex.Val = HeatIndex.Value;
					ThisMonth.HighHeatIndex.Ts = timestamp;
					WriteMonthIniFile();
				}

				if (HeatIndex > ThisYear.HighHeatIndex.Val)
				{
					ThisYear.HighHeatIndex.Val = HeatIndex.Value;
					ThisYear.HighHeatIndex.Ts = timestamp;
					WriteYearIniFile();
				}

				if (HeatIndex > AllTime.HighHeatIndex.Val)
					SetAlltime(AllTime.HighHeatIndex, HeatIndex.Value, timestamp);

				CheckMonthlyAlltime("HighHeatIndex", HeatIndex.Value, true, timestamp);
			}
			else
			{
				HeatIndex = null;
			}




			// Find estimated wet bulb temp. First time this is called, required variables may not have been set up yet
			if (Pressure.HasValue && Dewpoint.HasValue)
				WetBulb = ConvertTempCToUser(MeteoLib.CalculateWetBulbC(tempinC, ConvertUserTempToC(Dewpoint).Value, ConvertUserPressToMB(Pressure).Value));
			else
				WetBulb = null;
		}

		public void DoApparentTemp(DateTime timestamp)
		{
			// Calculates Apparent Temperature
			// See http://www.bom.gov.au/info/thermal_stress/#atapproximation

			// don't try to calculate apparent if we haven't yet had wind and temp readings
			//if (TempReadyToPlot && WindReadyToPlot)
			//{

			if (Temperature == null || Humidity == null || WindAverage == null)
			{
				ApparentTemp = null;
				THWIndex = null;
				return;
			}

			//ApparentTemperature =
			//ConvertTempCToUser(ConvertUserTempToC(OutdoorTemperature) + (0.33 * MeteoLib.ActualVapourPressure(ConvertUserTempToC(OutdoorTemperature), Humidity)) -
			//				   (0.7 * ConvertUserWindToMS(WindAverage)) - 4);
			ApparentTemp = ConvertTempCToUser(MeteoLib.ApparentTemperature(ConvertUserTempToC(Temperature).Value, ConvertUserWindToMS(WindAverage).Value, Humidity.Value));

			// we will tag on the THW Index here
			THWIndex = ConvertTempCToUser(MeteoLib.THWIndex(ConvertUserTempToC(Temperature), Humidity, ConvertUserWindToKPH(WindAverage)));

			if (ApparentTemp > (HiLoToday.HighAppTemp ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighAppTemp = ApparentTemp;
				HiLoToday.HighAppTempTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (ApparentTemp < (HiLoToday.LowAppTemp ?? Cumulus.DefaultLoVal))
			{
				HiLoToday.LowAppTemp = ApparentTemp;
				HiLoToday.LowAppTempTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (ApparentTemp > ThisMonth.HighAppTemp.Val)
			{
				ThisMonth.HighAppTemp.Val = ApparentTemp.Value;
				ThisMonth.HighAppTemp.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (ApparentTemp < ThisMonth.LowAppTemp.Val)
			{
				ThisMonth.LowAppTemp.Val = ApparentTemp.Value;
				ThisMonth.LowAppTemp.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (ApparentTemp > ThisYear.HighAppTemp.Val)
			{
				ThisYear.HighAppTemp.Val = ApparentTemp.Value;
				ThisYear.HighAppTemp.Ts = timestamp;
				WriteYearIniFile();
			}

			if (ApparentTemp < ThisYear.LowAppTemp.Val)
			{
				ThisYear.LowAppTemp.Val = ApparentTemp.Value;
				ThisYear.LowAppTemp.Ts = timestamp;
				WriteYearIniFile();
			}

			if (ApparentTemp > AllTime.HighAppTemp.Val)
				SetAlltime(AllTime.HighAppTemp, ApparentTemp.Value, timestamp);

			if (ApparentTemp < AllTime.LowAppTemp.Val)
				SetAlltime(AllTime.LowAppTemp, ApparentTemp.Value, timestamp);

			CheckMonthlyAlltime("HighAppTemp", ApparentTemp.Value, true, timestamp);
			CheckMonthlyAlltime("LowAppTemp", ApparentTemp.Value, false, timestamp);
			//}
		}

		public void DoWindChill(double? chillpar, DateTime timestamp)
		{
			if (cumulus.StationOptions.CalculatedWC)
			{
				var TempinC = ConvertUserTempToC(Temperature);
				var windinKPH = ConvertUserWindToKPH(WindAverage);
				WindChill = ConvertTempCToUser(MeteoLib.WindChill(TempinC, windinKPH));
			}
			else
			{
				if (WindAverage.HasValue && ConvertUserWindToMS(WindAverage) < 1.5)
					WindChill = chillpar;
				else
					WindChill = Temperature;
			}

			if (WindChill.HasValue)
			{
				dataValuesUpdated.WindChill = true;

				//WindChillReadyToPlot = true;

				if (WindChill < (HiLoToday.LowWindChill ?? Cumulus.DefaultLoVal))
				{
					HiLoToday.LowWindChill = WindChill;
					HiLoToday.LowWindChillTime = timestamp;
					WriteTodayFile(timestamp, false);
				}

				if (WindChill < ThisMonth.LowChill.Val)
				{
					ThisMonth.LowChill.Val = WindChill.Value;
					ThisMonth.LowChill.Ts = timestamp;
					WriteMonthIniFile();
				}

				if (WindChill < ThisYear.LowChill.Val)
				{
					ThisYear.LowChill.Val = WindChill.Value;
					ThisYear.LowChill.Ts = timestamp;
					WriteYearIniFile();
				}

				// All time wind chill
				if (WindChill < AllTime.LowChill.Val)
				{
					SetAlltime(AllTime.LowChill, WindChill.Value, timestamp);
				}

				CheckMonthlyAlltime("LowChill", WindChill.Value, false, timestamp);
			}
		}

		public void DoFeelsLike(DateTime timestamp)
		{
			if (!Temperature.HasValue || !Humidity.HasValue)
			{
				FeelsLike = null;
				return;
			}

			FeelsLike = ConvertTempCToUser(MeteoLib.FeelsLike(ConvertUserTempToC(Temperature.Value), ConvertUserWindToKPH(WindAverage), Humidity.Value));

			if (FeelsLike > (HiLoToday.HighFeelsLike ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighFeelsLike = FeelsLike;
				HiLoToday.HighFeelsLikeTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (FeelsLike < (HiLoToday.LowFeelsLike ?? Cumulus.DefaultLoVal))
			{
				HiLoToday.LowFeelsLike = FeelsLike;
				HiLoToday.LowFeelsLikeTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (FeelsLike > ThisMonth.HighFeelsLike.Val)
			{
				ThisMonth.HighFeelsLike.Val = FeelsLike.Value;
				ThisMonth.HighFeelsLike.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (FeelsLike < ThisMonth.LowFeelsLike.Val)
			{
				ThisMonth.LowFeelsLike.Val = FeelsLike.Value;
				ThisMonth.LowFeelsLike.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (FeelsLike > ThisYear.HighFeelsLike.Val)
			{
				ThisYear.HighFeelsLike.Val = FeelsLike.Value;
				ThisYear.HighFeelsLike.Ts = timestamp;
				WriteYearIniFile();
			}

			if (FeelsLike < ThisYear.LowFeelsLike.Val)
			{
				ThisYear.LowFeelsLike.Val = FeelsLike.Value;
				ThisYear.LowFeelsLike.Ts = timestamp;
				WriteYearIniFile();
			}

			if (FeelsLike > AllTime.HighFeelsLike.Val)
				SetAlltime(AllTime.HighFeelsLike, FeelsLike.Value, timestamp);

			if (FeelsLike < AllTime.LowFeelsLike.Val)
				SetAlltime(AllTime.LowFeelsLike, FeelsLike.Value, timestamp);

			CheckMonthlyAlltime("HighFeelsLike", FeelsLike.Value, true, timestamp);
			CheckMonthlyAlltime("LowFeelsLike", FeelsLike.Value, false, timestamp);
		}

		public void DoHumidex(DateTime timestamp)
		{
			if (!Temperature.HasValue || !Humidity.HasValue)
			{
				Humidex = null;
				return;
			}

			Humidex = MeteoLib.Humidex(ConvertUserTempToC(Temperature), Humidity);

			if (Humidex > (HiLoToday.HighHumidex ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighHumidex = Humidex;
				HiLoToday.HighHumidexTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (Humidex > ThisMonth.HighHumidex.Val)
			{
				ThisMonth.HighHumidex.Val = Humidex.Value;
				ThisMonth.HighHumidex.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (Humidex > ThisYear.HighHumidex.Val)
			{
				ThisYear.HighHumidex.Val = Humidex.Value;
				ThisYear.HighHumidex.Ts = timestamp;
				WriteYearIniFile();
			}

			if (Humidex > AllTime.HighHumidex.Val)
				SetAlltime(AllTime.HighHumidex, Humidex.Value, timestamp);

			CheckMonthlyAlltime("HighHumidex", Humidex.Value, true, timestamp);
		}

		public void CheckForWindrunHighLow(DateTime timestamp)
		{
			DateTime adjustedtimestamp = timestamp.AddHours(cumulus.GetHourInc());

			if (WindRunToday > ThisMonth.HighWindRun.Val)
			{
				ThisMonth.HighWindRun.Val = WindRunToday;
				ThisMonth.HighWindRun.Ts = adjustedtimestamp;
				WriteMonthIniFile();
			}

			if (WindRunToday > ThisYear.HighWindRun.Val)
			{
				ThisYear.HighWindRun.Val = WindRunToday;
				ThisYear.HighWindRun.Ts = adjustedtimestamp;
				WriteYearIniFile();
			}

			if (WindRunToday > AllTime.HighWindRun.Val)
			{
				SetAlltime(AllTime.HighWindRun, WindRunToday, adjustedtimestamp);
			}

			CheckMonthlyAlltime("HighWindRun", WindRunToday, true, adjustedtimestamp);
		}

		public void CheckForDewpointHighLow(DateTime timestamp)
		{
			if (Dewpoint == null)
				return;

			if (Dewpoint > (HiLoToday.HighDewPoint ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighDewPoint = Dewpoint;
				HiLoToday.HighDewPointTime = timestamp;
				WriteTodayFile(timestamp, false);
			}
			if (Dewpoint < (HiLoToday.LowDewPoint ?? Cumulus.DefaultLoVal))
			{
				HiLoToday.LowDewPoint = Dewpoint;
				HiLoToday.LowDewPointTime = timestamp;
				WriteTodayFile(timestamp, false);
			}
			if (Dewpoint > ThisMonth.HighDewPoint.Val)
			{
				ThisMonth.HighDewPoint.Val = Dewpoint.Value;
				ThisMonth.HighDewPoint.Ts = timestamp;
				WriteMonthIniFile();
			}
			if (Dewpoint < ThisMonth.LowDewPoint.Val)
			{
				ThisMonth.LowDewPoint.Val = Dewpoint.Value;
				ThisMonth.LowDewPoint.Ts = timestamp;
				WriteMonthIniFile();
			}
			if (Dewpoint > ThisYear.HighDewPoint.Val)
			{
				ThisYear.HighDewPoint.Val = Dewpoint.Value;
				ThisYear.HighDewPoint.Ts = timestamp;
				WriteYearIniFile();
			}
			if (Dewpoint < ThisYear.LowDewPoint.Val)
			{
				ThisYear.LowDewPoint.Val = Dewpoint.Value;
				ThisYear.LowDewPoint.Ts = timestamp;
				WriteYearIniFile();
			}
			;
			if (Dewpoint > AllTime.HighDewPoint.Val)
			{
				SetAlltime(AllTime.HighDewPoint, Dewpoint.Value, timestamp);
			}
			if (Dewpoint < AllTime.LowDewPoint.Val)
				SetAlltime(AllTime.LowDewPoint, Dewpoint.Value, timestamp);

			CheckMonthlyAlltime("HighDewPoint", Dewpoint.Value, true, timestamp);
			CheckMonthlyAlltime("LowDewPoint", Dewpoint.Value, false, timestamp);
		}

		public void DoPressure(double? sl, DateTime timestamp)
		{
			if (sl == null)
			{
				Pressure = null;
				AltimeterPressure = null;
				return;
			}

			// Spike removal is in mb/hPa
			var pressMB = ConvertUserPressToMB(sl).Value;
			if (((Math.Abs(pressMB - previousPress) > cumulus.Spike.PressDiff) && (previousPress != 9999)) ||
				pressMB >= cumulus.Limit.PressHigh || pressMB <= cumulus.Limit.PressLow)
			{
				cumulus.LogSpikeRemoval("Pressure difference greater than specified; reading ignored");
				cumulus.LogSpikeRemoval($"NewVal={pressMB:F1} OldVal={previousPress:F1} SpikePressDiff={cumulus.Spike.PressDiff:F1} HighLimit={cumulus.Limit.PressHigh:F1} LowLimit={cumulus.Limit.PressLow:F1}");
				lastSpikeRemoval = DateTime.Now;
				cumulus.SpikeAlarm.LastError = $"Pressure difference greater than spike value - NewVal={pressMB:F1} OldVal={previousPress:F1}";
				cumulus.SpikeAlarm.Triggered = true;
				return;
			}

			previousPress = pressMB;

			Pressure = sl * cumulus.Calib.Press.Mult + cumulus.Calib.Press.Offset;
			dataValuesUpdated.Pressure = true;

			if (cumulus.Manufacturer == cumulus.DAVIS)
			{
				if (!cumulus.DavisOptions.UseLoop2)
				{
					// Loop2 data not available, just use sea level (for now, anyway)
					AltimeterPressure = Pressure;
				}
			}
			else
			{
				if (cumulus.Manufacturer == cumulus.OREGONUSB)
				{
					AltimeterPressure = ConvertPressMBToUser(StationToAltimeter(ConvertUserPressureToHPa(StationPressure), AltitudeM(cumulus.Altitude)));
				}
				else
				{
					// For all other stations, altimeter is same as sea-level
					AltimeterPressure = Pressure;
				}
			}

			first_press = false;

			if (Pressure > AllTime.HighPress.Val)
			{
				SetAlltime(AllTime.HighPress, Pressure.Value, timestamp);
			}

			cumulus.HighPressAlarm.CheckAlarm(Pressure.Value);

			if (Pressure < AllTime.LowPress.Val)
			{
				SetAlltime(AllTime.LowPress, Pressure.Value, timestamp);
			}

			cumulus.LowPressAlarm.CheckAlarm(Pressure.Value);

			CheckMonthlyAlltime("LowPress", Pressure.Value, false, timestamp);
			CheckMonthlyAlltime("HighPress", Pressure.Value, true, timestamp);

			if (Pressure > (HiLoToday.HighPress ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighPress = Pressure;
				HiLoToday.HighPressTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (Pressure < (HiLoToday.LowPress ?? Cumulus.DefaultLoVal))
			{
				HiLoToday.LowPress = Pressure;
				HiLoToday.LowPressTime = timestamp;
				WriteTodayFile(timestamp, false);
			}

			if (Pressure > ThisMonth.HighPress.Val)
			{
				ThisMonth.HighPress.Val = Pressure.Value;
				ThisMonth.HighPress.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (Pressure < ThisMonth.LowPress.Val)
			{
				ThisMonth.LowPress.Val = Pressure.Value;
				ThisMonth.LowPress.Ts = timestamp;
				WriteMonthIniFile();
			}

			if (Pressure > ThisYear.HighPress.Val)
			{
				ThisYear.HighPress.Val = Pressure.Value;
				ThisYear.HighPress.Ts = timestamp;
				WriteYearIniFile();
			}

			if (Pressure < ThisYear.LowPress.Val)
			{
				ThisYear.LowPress.Val = Pressure.Value;
				ThisYear.LowPress.Ts = timestamp;
				WriteYearIniFile();
			}

			PressReadyToPlot = true;
			HaveReadData = true;
		}

		protected void DoPressTrend(string trend)
		{
			if (cumulus.StationOptions.UseCumulusPresstrendstr)
			{
				UpdatePressureTrendString();
			}
			else
			{
				Presstrendstr = trend;
			}
		}

		public void DoRain(double? total, double? rate, DateTime timestamp)
		{
			if (rate == null)
			{
				RainRate = null;
			}

			if (total == null)
			{
				return;
			}


			DateTime readingTS = timestamp.AddHours(cumulus.GetHourInc());

			dataValuesUpdated.Rain = true;

#if DEBUG
			cumulus.LogDebugMessage($"DoRain: counter={total:f3}, rate={rate:f3}; RainToday={RainToday:f3}, StartOfDay={raindaystart:f3}");
#endif

			// Spike removal is in mm
			var rainRateMM = ConvertUserRainToMM(rate);
			if ((rainRateMM ?? 0) > cumulus.Spike.MaxRainRate)
			{
				cumulus.LogSpikeRemoval("Rain rate greater than specified; reading ignored");
				cumulus.LogSpikeRemoval($"Rate value = {rainRateMM:F2} SpikeMaxRainRate = {cumulus.Spike.MaxRainRate:F2}");
				lastSpikeRemoval = DateTime.Now;
				cumulus.SpikeAlarm.LastError = $"Rain rate greater than spike value - value = {rainRateMM:F2}mm/hr";
				cumulus.SpikeAlarm.Triggered = true;
				return;
			}

			if ((CurrentDay != readingTS.Day) || (CurrentMonth != readingTS.Month) || (CurrentYear != readingTS.Year))
			{
				// A reading has apparently arrived at the start of a new day, but before we have done the roll-over
				// Ignore it, as otherwise it may cause a new monthly record to be logged using last month's total
				cumulus.LogDebugMessage("DoRain: A reading arrived at the start of a new day, but before we have done the roll-over. Ignoring it");
				return;
			}

			var previoustotal = Raincounter;

			// This is just to stop rounding errors triggering phantom rain days
			double raintipthreshold = cumulus.Units.Rain == 0 ? 0.009 : 0.0003;

			/*
			if (cumulus.Manufacturer == cumulus.DAVIS)  // Davis can have either 0.2mm or 0.01in buckets, and the user could select to measure in mm or inches!
			{
				// If the bucket size is set, use that, otherwise infer from rain units
				var bucketSize = cumulus.DavisOptions.RainGaugeType == -1 ? cumulus.Units.Rain : cumulus.DavisOptions.RainGaugeType;

				switch (bucketSize)
				{
					case 0: // 0.2 mm tips
						// mm/mm (0.2) or mm/in (0.00787)
						raintipthreshold = cumulus.Units.Rain == 0 ? 0.19 : 0.006;
						break;
					case 1: // 0.01 inch tips
						// in/mm (0.254) or in/in (0.01)
						raintipthreshold = cumulus.Units.Rain == 0 ? 0.2 : 0.009;
						break;
					case 2: // 0.01 mm tips
						// mm/mm (0.1) or mm/in (0.0394)
						raintipthreshold = cumulus.Units.Rain == 0 ? 0.09 : 0.003;
						break;
					case 3: // 0.001 inch tips
						// in/mm (0.0254) or in/in (0.001)
						raintipthreshold = cumulus.Units.Rain == 0 ? 0.02 : 0.0009;
						break;
				}
			}
			else
			{
				if (cumulus.Units.Rain == 0)
				{
					// mm
					raintipthreshold = cumulus.Manufacturer == cumulus.INSTROMET ? 0.009 : 0.09;
				}
				else
				{
					// in
					raintipthreshold = cumulus.Manufacturer == cumulus.INSTROMET ? 0.0003 : 0.003;
				}
			}
			*/

			Raincounter = total.Value;

			//first_rain = false;
			if (initialiseRainCounterOnFirstData)
			{
				raindaystart = Raincounter;
				midnightraincount = Raincounter;
				Cumulus.LogMessage(" First rain data, raindaystart = " + raindaystart);

				initialiseRainCounterOnFirstData = false;
				WriteTodayFile(timestamp, false);
				HaveReadData = true;
				return;
			}

			// Has the rain total in the station been reset?
			// raindaystart greater than current total, allow for rounding
			if (raindaystart - Raincounter > raintipthreshold)
			{
				if (FirstChanceRainReset)
				// second consecutive reading with reset value
				{
					Cumulus.LogMessage(" ****Rain counter reset confirmed: raindaystart = " + raindaystart + ", Raincounter = " + Raincounter);

					// set the start of day figure so it reflects the rain
					// so far today
					raindaystart = Raincounter - (RainToday.Value / cumulus.Calib.Rain.Mult);
					Cumulus.LogMessage("Setting raindaystart to " + raindaystart);

					midnightraincount = Raincounter;

					// update any data in the recent data db
					var counterChange = Raincounter - prevraincounter;
					Database.Execute("update RecentData set raincounter=raincounter+?", counterChange);

					FirstChanceRainReset = false;
				}
				else
				{
					Cumulus.LogMessage(" ****Rain reset? First chance: raindaystart = " + raindaystart + ", Raincounter = " + Raincounter);

					// reset the counter to ignore this reading
					Raincounter = previoustotal;
					Cumulus.LogMessage("Leaving counter at " + Raincounter);

					// stash the previous rain counter
					prevraincounter = Raincounter;

					FirstChanceRainReset = true;
				}
			}
			else
			{
				FirstChanceRainReset = false;
			}

			if ((rate ?? -1) > -1)
			// Do rain rate
			{
				// scale rainfall rate
				RainRate = rate * cumulus.Calib.Rain.Mult;
				var roundRate = Math.Round(RainRate.Value, cumulus.RainDPlaces);

				if (cumulus.StationOptions.UseRainForIsRaining)
				{
					IsRaining = RainRate > 0;
					cumulus.IsRainingAlarm.Triggered = IsRaining;
				}

				if (roundRate > AllTime.HighRainRate.Val)
					SetAlltime(AllTime.HighRainRate, roundRate, timestamp);

				CheckMonthlyAlltime("HighRainRate", roundRate, true, timestamp);

				cumulus.HighRainRateAlarm.CheckAlarm(roundRate);

				if (roundRate > (HiLoToday.HighRainRate ?? Cumulus.DefaultHiVal))
				{
					HiLoToday.HighRainRate = roundRate;
					HiLoToday.HighRainRateTime = timestamp;
					WriteTodayFile(timestamp, false);
				}

				if (roundRate > ThisMonth.HighRainRate.Val)
				{
					ThisMonth.HighRainRate.Val = roundRate;
					ThisMonth.HighRainRate.Ts = timestamp;
					WriteMonthIniFile();
				}

				if (roundRate > ThisYear.HighRainRate.Val)
				{
					ThisYear.HighRainRate.Val = roundRate;
					ThisYear.HighRainRate.Ts = timestamp;
					WriteYearIniFile();
				}
			}

			if (!FirstChanceRainReset)
			{
				// Has a tip occurred?
				if (total - previoustotal > raintipthreshold)
				{
					// rain has occurred
					LastRainTip = timestamp.ToString("yyyy-MM-dd HH:mm");

					if (cumulus.StationOptions.UseRainForIsRaining)
					{
						IsRaining = true;
						cumulus.IsRainingAlarm.Triggered = true;
					}
				}
				else if (cumulus.StationOptions.UseRainForIsRaining && RainRate <= 0)
				{
					IsRaining = false;
					cumulus.IsRainingAlarm.Triggered = false;
				}

				// Calculate today"s rainfall
				RainToday = Raincounter - raindaystart;
				//cumulus.LogDebugMessage("Uncalibrated RainToday = " + RainToday);

				// scale for calibration
				RainToday *= cumulus.Calib.Rain.Mult;

				// Calculate rain since midnight for Wunderground etc
				double trendval = Raincounter - midnightraincount;

				// Round value as some values may have been read from log file and already rounded
				trendval = Math.Round(trendval, cumulus.RainDPlaces);

				if (trendval < 0)
				{
					RainSinceMidnight = 0;
				}
				else
				{
					RainSinceMidnight = trendval * cumulus.Calib.Rain.Mult;
				}

				// rain this month so far
				RainMonth = rainthismonth + RainToday.Value;

				// get correct date for rain records
				var offsetdate = timestamp.AddHours(cumulus.GetHourInc());

				// rain this year so far
				RainYear = rainthisyear + RainToday.Value;

				var roundToday = Math.Round(RainToday ?? 0, cumulus.RainDPlaces);
				var roundMonth = Math.Round(RainMonth, cumulus.RainDPlaces);
				var roundYear = Math.Round(RainYear, cumulus.RainDPlaces);

				if (roundToday > AllTime.DailyRain.Val)
					SetAlltime(AllTime.DailyRain, roundToday, offsetdate);

				CheckMonthlyAlltime("DailyRain", roundToday, true, timestamp);

				if (roundToday > ThisMonth.DailyRain.Val)
				{
					ThisMonth.DailyRain.Val = roundToday;
					ThisMonth.DailyRain.Ts = offsetdate;
					WriteMonthIniFile();
				}

				if (roundToday > ThisYear.DailyRain.Val)
				{
					ThisYear.DailyRain.Val = roundToday;
					ThisYear.DailyRain.Ts = offsetdate;
					WriteYearIniFile();
				}

				cumulus.HighRainTodayAlarm.CheckAlarm(roundToday);

				CheckMonthlyAlltime("MonthlyRain", roundMonth, true, timestamp);

				if (roundMonth > ThisYear.MonthlyRain.Val)
				{
					ThisYear.MonthlyRain.Val = roundMonth;
					ThisYear.MonthlyRain.Ts = offsetdate;
					WriteYearIniFile();
				}

				if (roundMonth > AllTime.MonthlyRain.Val)
					SetAlltime(AllTime.MonthlyRain, roundMonth, offsetdate);



				// Yesterday"s rain - Scale for units
				// rainyest = rainyesterday * RainMult;

				//RainReadyToPlot = true;
			}
			HaveReadData = true;
		}

		public void DoDewpoint(double? dp, DateTime timestamp)
		{
			double? newdp;
			if (cumulus.StationOptions.CalculatedDP && !cumulus.FineOffsetStation)
			{
				newdp = MeteoLib.DewPoint(ConvertUserTempToC(Temperature), Humidity);
			}
			else
			{
				newdp = dp;
			}

			if (!newdp.HasValue)
			{
				Dewpoint = newdp;
				return;
			}

			if (ConvertUserTempToC(newdp) <= cumulus.Limit.DewHigh)
			{
				Dewpoint = newdp;
				dataValuesUpdated.DewPoint = true;
				CheckForDewpointHighLow(timestamp);
			}
			else
			{
				var msg = $"Dew point greater than limit ({cumulus.Limit.DewHigh.ToString(cumulus.TempFormat)}); reading ignored: {newdp.Value.ToString(cumulus.TempFormat)}";
				lastSpikeRemoval = DateTime.Now;
				cumulus.SpikeAlarm.LastError = msg;
				cumulus.SpikeAlarm.Triggered = true;
				cumulus.LogSpikeRemoval(msg);
				return;
			}
		}

		public string LastRainTip { get; set; }

		public void DoExtraHum(double? hum, int channel)
		{
			if ((channel > 0) && (channel < ExtraHum.Length - 1))
			{
				ExtraHum[channel] = hum;
				dataValuesUpdated.ExtraHum[channel] = true;
			}
		}

		public void DoExtraTemp(double? temp, int channel)
		{
			if ((channel > 0) && (channel < ExtraTemp.Length - 1))
			{
				ExtraTemp[channel] = temp;
				dataValuesUpdated.ExtraTemp[channel] = true;
			}
		}

		public void DoUserTemp(double? temp, int channel)
		{
			if ((channel > 0) && (channel < UserTemp.Length - 1))
			{
				UserTemp[channel] = temp;
				dataValuesUpdated.ExtraUserTemp[channel] = true;
			}
		}


		public void DoExtraDP(double? dp, int channel)
		{
			if ((channel > 0) && (channel < ExtraDewPoint.Length - 1))
			{
				ExtraDewPoint[channel] = dp;
				dataValuesUpdated.ExtraDewPoint[channel] = true;
			}
		}

		public void DoForecast(string forecast, bool hourly)
		{
			// store weather station forecast if available
			if (forecast != "")
			{
				wsforecast = forecast;
			}

			if (!cumulus.UseCumulusForecast)
			{
				// user wants to display station forecast
				forecaststr = wsforecast;
				FirstForecastDone = true;
			}
			else if (Pressure.HasValue)
			{
				// determine whether we need to update the Cumulus forecast; user may have chosen to only update once an hour, but
				// we still need to do that once to get an initial forecast
				if ((!FirstForecastDone) || (!cumulus.HourlyForecast) || (hourly && cumulus.HourlyForecast))
				{
					int bartrend;
					if ((presstrendval >= -cumulus.FCPressureThreshold) && (presstrendval <= cumulus.FCPressureThreshold))
						bartrend = 0;
					else if (presstrendval < 0)
						bartrend = 2;
					else
						bartrend = 1;

					string windDir;
					if (WindAverage < 0.1)
					{
						windDir = "calm";
					}
					else
					{
						windDir = AvgBearingText;
					}

					double lp;
					double hp;
					if (cumulus.FCpressinMB)
					{
						lp = cumulus.FClowpress;
						hp = cumulus.FChighpress;
					}
					else
					{
						lp = cumulus.FClowpress / 0.0295333727;
						hp = cumulus.FChighpress / 0.0295333727;
					}

					forecaststr = BetelCast(ConvertUserPressureToHPa(Pressure).Value, DateTime.Now.Month, windDir, bartrend, cumulus.Latitude > 0, hp, lp);
				}

				FirstForecastDone = true;
			}
			else
			{
				forecaststr = "";
			}

			HaveReadData = true;
		}

		public string forecaststr { get; set; }

		public string CumulusForecast { get; set; }

		public string wsforecast { get; set; }

		public bool FirstForecastDone = false;

		/// <summary>
		/// Convert altitude from user units to metres
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static double AltitudeM(double altitude)
		{
			if (cumulus.AltitudeInFeet)
			{
				return altitude * 0.3048;
			}
			else
			{
				return altitude;
			}
		}

		/// <summary>
		/// Convert pressure from user units to hPa
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static double? ConvertUserPressureToHPa(double? value)
		{
			if (value == null)
				return null;

			if (cumulus.Units.Press == 2)
				return value / 0.0295333727;
			else
				return value;
		}

		public static double? StationToAltimeter(double? pressureHPa, double elevationM)
		{
			if (pressureHPa == null)
				return null;

			// from MADIS API by NOAA Forecast Systems Lab, see http://madis.noaa.gov/madis_api.html

			double k1 = 0.190284; // discrepancy with calculated k1 probably because Smithsonian used less precise gas constant and gravity values
			double k2 = 8.4184960528E-5; // (standardLapseRate / standardTempK) * (Power(standardSLP, k1)
			return Math.Pow(Math.Pow(pressureHPa.Value - 0.3, k1) + (k2 * elevationM), 1 / k1);
		}

		public bool PressReadyToPlot { get; set; }

		public bool first_press { get; set; }

		public void DoWind(double? gustpar, int? bearingpar, double? speedpar, DateTime timestamp)
		{
#if DEBUG
			cumulus.LogDebugMessage($"DoWind: gust={gustpar:F1}, speed={speedpar:F1}");
#endif
			if (gustpar == null || speedpar == null)
			{
				WindLatest = null;
				Bearing = null;

				if (cumulus.StationOptions.CalcWind10MinAve)
					WindAverage = null;

				return;
			}

			dataValuesUpdated.Wind = true;

			// Spike removal is in m/s
			var windGustMS = ConvertUserWindToMS(gustpar).Value;
			var windAvgMS = ConvertUserWindToMS(speedpar).Value;

			if (((Math.Abs(windGustMS - previousGust) > cumulus.Spike.GustDiff) && (previousGust != 999)) ||
				((Math.Abs(windAvgMS - previousWind) > cumulus.Spike.WindDiff) && (previousWind != 999)) ||
				windGustMS >= cumulus.Limit.WindHigh
				)
			{
				WindLatest = null;

				Bearing = 0;

				cumulus.LogSpikeRemoval("Wind or gust difference greater than specified; reading ignored");
				cumulus.LogSpikeRemoval($"Gust: NewVal={windGustMS:F1} OldVal={previousGust:F1} SpikeGustDiff={cumulus.Spike.GustDiff:F1} HighLimit={cumulus.Limit.WindHigh:F1}");
				cumulus.LogSpikeRemoval($"Wind: NewVal={windAvgMS:F1} OldVal={previousWind:F1} SpikeWindDiff={cumulus.Spike.WindDiff:F1}");
				lastSpikeRemoval = DateTime.Now;
				cumulus.SpikeAlarm.LastError = $"Wind or gust difference greater than spike/limit value - Gust: NewVal={windGustMS:F1}m/s OldVal={previousGust:F1}m/s - Wind: NewVal={windAvgMS:F1}m/s OldVal={previousWind:F1}m/s";
				cumulus.SpikeAlarm.Triggered = true;
				return;
			}

			previousGust = windGustMS;
			previousWind = windAvgMS;

			// use bearing of zero when calm
			if ((Math.Abs(gustpar.Value) < 0.001) && cumulus.StationOptions.UseZeroBearing)
			{
				Bearing = 0;
			}
			else if (bearingpar.HasValue)
			{
				Bearing = (bearingpar.Value + (int)cumulus.Calib.WindDir.Offset) % 360;
				if (Bearing < 0)
				{
					Bearing = 360 + Bearing;
				}

				if (Bearing == 0)
				{
					Bearing = 360;
				}
			}
			var uncalibratedgust = gustpar.Value;
			calibratedgust = uncalibratedgust * cumulus.Calib.WindGust.Mult;
			WindLatest = calibratedgust;
			windspeeds[nextwindvalue] = uncalibratedgust;
			windbears[nextwindvalue] = Bearing.Value;

			// Recalculate wind rose data
			lock (windcounts)
			{
				for (int i = 0; i < cumulus.NumWindRosePoints; i++)
				{
					windcounts[i] = 0;
				}

				for (int i = 0; i < numwindvalues; i++)
				{
					int j = (((windbears[i] * 100) + 1125) % 36000) / (int)Math.Floor(cumulus.WindRoseAngle * 100);
					windcounts[j] += windspeeds[i];
				}
			}

			if (numwindvalues < maxwindvalues)
			{
				numwindvalues++;
			}

			nextwindvalue = (nextwindvalue + 1) % maxwindvalues;
			if (calibratedgust > (HiLoToday.HighGust ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighGust = calibratedgust;
				HiLoToday.HighGustTime = timestamp;
				HiLoToday.HighGustBearing = Bearing;
				WriteTodayFile(timestamp, false);
			}
			if (calibratedgust > ThisMonth.HighGust.Val)
			{
				ThisMonth.HighGust.Val = calibratedgust;
				ThisMonth.HighGust.Ts = timestamp;
				WriteMonthIniFile();
			}
			if (calibratedgust > ThisYear.HighGust.Val)
			{
				ThisYear.HighGust.Val = calibratedgust;
				ThisYear.HighGust.Ts = timestamp;
				WriteYearIniFile();
			}
			// All time high gust?
			if (calibratedgust > AllTime.HighGust.Val)
			{
				SetAlltime(AllTime.HighGust, calibratedgust, timestamp);
			}

			// check for monthly all time records (and set)
			CheckMonthlyAlltime("HighGust", calibratedgust, true, timestamp);

			WindRecent[nextwind].Gust = uncalibratedgust;
			WindRecent[nextwind].Speed = speedpar.Value;
			WindRecent[nextwind].Timestamp = timestamp;
			nextwind = (nextwind + 1) % MaxWindRecent;

			if (cumulus.StationOptions.CalcWind10MinAve)
			{
				int count = 0;
				double totalwind = 0;
				int i = nextwind;



				do
				{
					//for (int i = 0; i < MaxWindRecent; i++)
					//{
					if (timestamp - WindRecent[i].Timestamp <= cumulus.AvgSpeedTime)
					{
						count++;
						if (cumulus.StationOptions.UseSpeedForAvgCalc)
						{
							totalwind += WindRecent[i].Speed;
						}
						else
						{
							totalwind += WindRecent[i].Gust;
						}
					}
					//}
					i = (i + 1) % MaxWindRecent;
				} while (i != nextwind);

				// average the values
				WindAverage = count > 0 ? totalwind / count : null;
				//cumulus.LogDebugMessage("next=" + nextwind + " wind=" + uncalibratedgust + " tot=" + totalwind + " numv=" + numvalues + " avg=" + WindAverage);
			}
			else
			{
				WindAverage = speedpar;
			}

			if (WindAverage.HasValue)
			{
				WindAverage *= cumulus.Calib.WindSpeed.Mult;
			}
			cumulus.HighWindAlarm.CheckAlarm(WindAverage ?? 0);

			if (CalcRecentMaxGust)
			{
				// Find recent max gust
				double maxgust = 0;
				int count = 0;
				for (int i = 0; i <= MaxWindRecent - 1; i++)
				{
					if (timestamp - WindRecent[i].Timestamp <= cumulus.PeakGustTime)
					{
						count++;
						if (WindRecent[i].Gust > maxgust)
						{
							maxgust = WindRecent[i].Gust;
						}
					}
				}
				RecentMaxGust = count > 0 ? maxgust * cumulus.Calib.WindGust.Mult : null;
			}

			cumulus.HighGustAlarm.CheckAlarm(RecentMaxGust ?? 0);

			if (WindAverage > (HiLoToday.HighWind ?? Cumulus.DefaultHiVal))
			{
				HiLoToday.HighWind = WindAverage;
				HiLoToday.HighWindTime = timestamp;
				WriteTodayFile(timestamp, false);
			}
			if (WindAverage > ThisMonth.HighWind.Val)
			{
				ThisMonth.HighWind.Val = WindAverage.Value;
				ThisMonth.HighWind.Ts = timestamp;
				WriteMonthIniFile();
			}
			if (WindAverage > ThisYear.HighWind.Val)
			{
				ThisYear.HighWind.Val = WindAverage.Value;
				ThisYear.HighWind.Ts = timestamp;
				WriteYearIniFile();
			}

			WindVec[nextwindvec].X = calibratedgust * Math.Sin(Trig.DegToRad(Bearing.Value));
			WindVec[nextwindvec].Y = calibratedgust * Math.Cos(Trig.DegToRad(Bearing.Value));
			// save timestamp of this reading
			WindVec[nextwindvec].Timestamp = timestamp;
			// save bearing
			WindVec[nextwindvec].Bearing = Bearing.Value; // savedBearing;
													// increment index for next reading
			nextwindvec = (nextwindvec + 1) % MaxWindRecent;

			// Now add up all the values within the required period
			double totalwindX = 0;
			double totalwindY = 0;
			int numvalues = 0;
			for (int i = 0; i < MaxWindRecent; i++)
			{
				if (timestamp - WindVec[i].Timestamp < cumulus.AvgBearingTime)
				{
					numvalues++;
					totalwindX += WindVec[i].X;
					totalwindY += WindVec[i].Y;
				}
			}
			if (numvalues == 0)
			{
				AvgBearing = null;
				AvgBearingText = "-";
			}
			else
			{
				if (totalwindX == 0)
				{
					AvgBearing = 0;
				}
				else
				{
					AvgBearing = (int)Math.Round(Trig.RadToDeg(Math.Atan(totalwindY / totalwindX)));

					if (totalwindX < 0)
					{
						AvgBearing = 270 - AvgBearing;
					}
					else
					{
						AvgBearing = 90 - AvgBearing;
					}

					if (AvgBearing == 0)
					{
						AvgBearing = 360;
					}
				}

				if ((Math.Abs(WindAverage.Value) < 0.01) && cumulus.StationOptions.UseZeroBearing)
				{
					AvgBearing = 0;
				}

				AvgBearingText = CompassPoint(AvgBearing);

				int diffFrom = 0;
				int diffTo = 0;
				BearingRangeFrom = AvgBearing.Value;
				BearingRangeTo = AvgBearing.Value;
				if (AvgBearing != 0)
				{
					for (int i = 0; i <= MaxWindRecent - 1; i++)
					{
						if ((timestamp - WindVec[i].Timestamp < cumulus.AvgBearingTime) && (WindVec[i].Bearing != 0))
						{
							// this reading was within the last N minutes
							int difference = Trig.getShortestAngle(AvgBearing.Value, WindVec[i].Bearing);
							if ((difference > diffTo))
							{
								diffTo = difference;
								BearingRangeTo = WindVec[i].Bearing;
								// Calculate rounded up value
								BearingRangeTo10 = (int)(Math.Ceiling(WindVec[i].Bearing / 10.0) * 10);
							}
							if ((difference < diffFrom))
							{
								diffFrom = difference;
								BearingRangeFrom = WindVec[i].Bearing;
								BearingRangeFrom10 = (int)(Math.Floor(WindVec[i].Bearing / 10.0) * 10);
							}
						}
					}
				}
				else
				{
					BearingRangeFrom10 = 0;
					BearingRangeTo10 = 0;
				}
			}
			// All time high wind speed?
			if (WindAverage > AllTime.HighWind.Val)
			{
				SetAlltime(AllTime.HighWind, WindAverage.Value, timestamp);
			}

			// check for monthly all time records (and set)
			CheckMonthlyAlltime("HighWind", WindAverage, true, timestamp);

			WindReadyToPlot = true;
			HaveReadData = true;
		}

		public void DoUV(double? value, DateTime timestamp)
		{
			if (value.HasValue)
			{
				UV = (value.Value * cumulus.Calib.UV.Mult) + cumulus.Calib.UV.Offset;
				dataValuesUpdated.UV = true;

				if (UV < 0)
					UV = 0;
				if (UV > 16)
					UV = 16;

				if (UV > (HiLoToday.HighUv ?? Cumulus.DefaultHiVal))
				{
					HiLoToday.HighUv = UV;
					HiLoToday.HighUvTime = timestamp;
				}
			}
			else
				UV = null;

			HaveReadData = true;
		}

		public void DoSolarRad(int? value, DateTime timestamp)
		{
			CurrentSolarMax = AstroLib.SolarMax(timestamp, cumulus.Longitude, cumulus.Latitude, AltitudeM(cumulus.Altitude), out SolarElevation, cumulus.SolarOptions);

			if (value.HasValue)
			{
				SolarRad = (int)Math.Round((value.Value * cumulus.Calib.Solar.Mult) + cumulus.Calib.Solar.Offset);
				if (SolarRad < 0)
					SolarRad = 0;

				dataValuesUpdated.Solar = true;

				if (SolarRad > (HiLoToday.HighSolar ?? Cumulus.DefaultHiVal))
				{
					HiLoToday.HighSolar = SolarRad;
					HiLoToday.HighSolarTime = timestamp;
				}

				if (!cumulus.SolarOptions.UseBlakeLarsen)
				{
					IsSunny = (SolarRad > (CurrentSolarMax * cumulus.SolarOptions.SunThreshold / 100)) && (SolarRad >= cumulus.SolarOptions.SolarMinimum);
				}
			}
			else
			{
				SolarRad = null;
			}

			HaveReadData = true;
		}

		protected void DoSunHours(double hrs)
		{
			if (StartOfDaySunHourCounter < -9998)
			{
				Cumulus.LogMessage("No start of day sun counter. Start counting from now");
				StartOfDaySunHourCounter = hrs;
			}

			// Has the counter reset to a value less than we were expecting. Or has it changed by some infeasibly large value?
			if (hrs < SunHourCounter || Math.Abs(hrs - SunHourCounter) > 20)
			{
				// counter reset
				Cumulus.LogMessage("Sun hour counter reset. Old value = " + SunHourCounter + "New value = " + hrs);
				StartOfDaySunHourCounter = hrs - SunshineHours;
			}
			SunHourCounter = hrs;
			SunshineHours = hrs - StartOfDaySunHourCounter;
		}

		protected void DoWetBulb(double? temp, DateTime timestamp) // Supplied in CELSIUS
		{
			WetBulb = ConvertTempCToUser(temp);
			if (WetBulb.HasValue)
				WetBulb = (WetBulb * cumulus.Calib.WetBulb.Mult) + cumulus.Calib.WetBulb.Offset;

			// calculate RH
			if (Temperature.HasValue)
			{
				double TempDry = ConvertUserTempToC(Temperature).Value;
				double Es = MeteoLib.SaturationVapourPressure1980(TempDry);
				double Ew = MeteoLib.SaturationVapourPressure1980(temp.Value);
				double E = Ew - (0.00066 * (1 + 0.00115 * temp.Value) * (TempDry - temp.Value) * 1013);
				int hum = (int)(100 * (E / Es));
				DoHumidity(hum, timestamp);
				// calculate DP
				// Calculate DewPoint

				// dewpoint = TempinC + ((0.13 * TempinC) + 13.6) * Ln(humidity / 100);
				Dewpoint = ConvertTempCToUser(MeteoLib.DewPoint(TempDry, hum));

				CheckForDewpointHighLow(timestamp);
			}
			else
			{
				DoHumidity(null, timestamp);
				Dewpoint = null;
			}
		}

		public bool IsSunny { get; set; }

		public bool HaveReadData { get; set; } = false;

		public void SetAlltime(AllTimeRec rec, double value, DateTime timestamp)
		{
			lock (alltimeIniThreadLock)
			{
				double oldvalue = rec.Val;
				DateTime oldts = rec.Ts;

				rec.Val = value;

				rec.Ts = timestamp;

				WriteAlltimeIniFile();

				AlltimeRecordTimestamp = timestamp;

				// add an entry to the log. date/time/value/item/old date/old time/old value
				// dates in ISO format, times always have a colon. Example:
				// 2010-02-24 05:19 -7.6 "Lowest temperature" 2009-02-09 04:50 -6.5
				var sb = new StringBuilder("New all-time record: New time = ", 100);
				sb.Append(FormatDateTime("yyyy-MM-dd HH:mm", rec.Ts));
				sb.Append(", new value = ");
				sb.Append(string.Format("{0,7:0.000}", value));
				sb.Append(" \"");
				sb.Append(rec.Desc);
				sb.Append("\" prev time = ");
				sb.Append(FormatDateTime("yyyy-MM-dd HH:mm", oldts));
				sb.Append(", prev value = ");
				sb.Append(string.Format("{0,7:0.000}", oldvalue));

				Cumulus.LogMessage(sb.ToString());

				sb.Append(Environment.NewLine);
				File.AppendAllText(cumulus.Alltimelogfile, sb.ToString());
			}
		}

		public void SetMonthlyAlltime(AllTimeRec rec, double value, DateTime timestamp)
		{
			double oldvalue = rec.Val;
			DateTime oldts = rec.Ts;

			rec.Val = value;
			rec.Ts = timestamp;

			WriteMonthlyAlltimeIniFile();

			var sb = new StringBuilder("New monthly record: month = ", 200);
			sb.Append(timestamp.Month.ToString("D2"));
			sb.Append(": New time = ");
			sb.Append(FormatDateTime("yyyy-MM-dd HH:mm", timestamp));
			sb.Append(", new value = ");
			sb.Append(value.ToString("F3"));
			sb.Append(" \"");
			sb.Append(rec.Desc);
			sb.Append("\" prev time = ");
			sb.Append(FormatDateTime("yyyy-MM-dd HH:mm", oldts));
			sb.Append(", prev value = ");
			sb.Append(oldvalue.ToString("F3"));

			Cumulus.LogMessage(sb.ToString());

			sb.Append(Environment.NewLine);
			File.AppendAllText(cumulus.MonthlyAlltimeLogFile, sb.ToString());
		}

		public int BearingRangeTo10 { get; set; }

		public int BearingRangeFrom10 { get; set; }

		public int BearingRangeTo { get; set; }

		public int BearingRangeFrom { get; set; }

		public const int maxwindvalues = 3600;

		public int[] windbears = new int[maxwindvalues];

		public int numwindvalues { get; set; }

		public double[] windspeeds = new double[maxwindvalues];

		public double[] windcounts { get; set; }

		public int nextwindvalue { get; set; }

		public double calibratedgust { get; set; }

		public int nextwind { get; set; } = 0;

		public int nextwindvec { get; set; } = 0;

		public TWindRecent[] WindRecent { get; set; }

		public TWindVec[] WindVec { get; set; }


		//private bool first_rain = true;
		private bool FirstChanceRainReset = false;
		public bool initialiseRainCounterOnFirstData = true;
		//private bool RainReadyToPlot = false;
		private double rainthismonth = 0;
		private double rainthisyear = 0;
		//private bool WindChillReadyToPlot = false;
		public bool noET = false;
		private int DayResetDay = 0;
		protected bool FirstRun = false;
		public const int MaxWindRecent = 720;
		public readonly double[] WindRunHourMult = { 3.6, 1.0, 1.0, 1.0 };
		public DateTime LastDataReadTimestamp = DateTime.MinValue;
		public DateTime SavedLastDataReadTimestamp = DateTime.MinValue;
		// Create arrays with 9 entries, 0 = VP2, 1-8 = WLL TxIds
		public int DavisTotalPacketsReceived = 0;
		public int[] DavisTotalPacketsMissed = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public int[] DavisNumberOfResynchs = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public int[] DavisMaxInARow = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public int[] DavisNumCRCerrors = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public int[] DavisReceptionPct = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public int[] DavisTxRssi = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public string DavisFirmwareVersion = "???";
		public string GW1000FirmwareVersion = "???";

		//private bool manualftp;

		public void WriteYesterdayFile(DateTime logdate)
		{
			Cumulus.LogMessage("Writing yesterday.ini");
			var hourInc = cumulus.GetHourInc();

			IniFile ini = new IniFile(cumulus.YesterdayFile);

			ini.SetValue("General", "Date", logdate.AddHours(hourInc));
			// Wind
			ini.SetValue("Wind", "Speed", HiLoYest.HighWind);
			ini.SetValue("Wind", "SpTime", HiLoYest.HighWindTime.ToString("HH:mm"));
			ini.SetValue("Wind", "Gust", HiLoYest.HighGust);
			ini.SetValue("Wind", "Time", HiLoYest.HighGustTime.ToString("HH:mm"));
			ini.SetValue("Wind", "Bearing", HiLoYest.HighGustBearing);
			ini.SetValue("Wind", "Direction", CompassPoint(HiLoYest.HighGustBearing));
			ini.SetValue("Wind", "Windrun", YesterdayWindRun);
			ini.SetValue("Wind", "DominantWindBearing", YestDominantWindBearing);
			// Temperature
			ini.SetValue("Temp", "Low", HiLoYest.LowTemp);
			ini.SetValue("Temp", "LTime", HiLoYest.LowTempTime.ToString("HH:mm"));
			ini.SetValue("Temp", "High", HiLoYest.HighTemp);
			ini.SetValue("Temp", "HTime", HiLoYest.HighTempTime.ToString("HH:mm"));
			ini.SetValue("Temp", "ChillHours", YestChillHours);
			ini.SetValue("Temp", "HeatingDegreeDays", YestHeatingDegreeDays);
			ini.SetValue("Temp", "CoolingDegreeDays", YestCoolingDegreeDays);
			ini.SetValue("Temp", "AvgTemp", YestAvgTemp);
			// Temperature midnight
			ini.SetValue("TempMidnight", "Low", HiLoYestMidnight.LowTemp);
			ini.SetValue("TempMidnight", "LTime", HiLoYestMidnight.LowTempTime.ToString("HH:mm"));
			ini.SetValue("TempMidnight", "High", HiLoYestMidnight.HighTemp);
			ini.SetValue("TempMidnight", "HTime", HiLoYestMidnight.HighTempTime.ToString("HH:mm"));
			// Pressure
			ini.SetValue("Pressure", "Low", HiLoYest.LowPress);
			ini.SetValue("Pressure", "LTime", HiLoYest.LowPressTime.ToString("HH:mm"));
			ini.SetValue("Pressure", "High", HiLoYest.HighPress);
			ini.SetValue("Pressure", "HTime", HiLoYest.HighPressTime.ToString("HH:mm"));
			// rain
			ini.SetValue("Rain", "High", HiLoYest.HighRainRate);
			ini.SetValue("Rain", "HTime", HiLoYest.HighRainRateTime.ToString("HH:mm"));
			ini.SetValue("Rain", "HourlyHigh", HiLoYest.HighHourlyRain);
			ini.SetValue("Rain", "HHourlyTime", HiLoYest.HighHourlyRainTime.ToString("HH:mm"));
			ini.SetValue("Rain", "High24h", HiLoYest.HighRain24h);
			ini.SetValue("Rain", "High24hTime", HiLoYest.HighRain24hTime.ToString("HH:mm"));
			ini.SetValue("Rain", "RG11Yesterday", RG11RainYesterday);
			// humidity
			ini.SetValue("Humidity", "Low", HiLoYest.LowHumidity);
			ini.SetValue("Humidity", "High", HiLoYest.HighHumidity);
			ini.SetValue("Humidity", "LTime", HiLoYest.LowHumidityTime.ToString("HH:mm"));
			ini.SetValue("Humidity", "HTime", HiLoYest.HighHumidityTime.ToString("HH:mm"));
			// Solar
			ini.SetValue("Solar", "SunshineHours", YestSunshineHours);
			// heat index
			ini.SetValue("HeatIndex", "High", HiLoYest.HighHeatIndex);
			ini.SetValue("HeatIndex", "HTime", HiLoYest.HighHeatIndexTime.ToString("HH:mm"));
			// App temp
			ini.SetValue("AppTemp", "Low", HiLoYest.LowAppTemp);
			ini.SetValue("AppTemp", "LTime", HiLoYest.LowAppTempTime.ToString("HH:mm"));
			ini.SetValue("AppTemp", "High", HiLoYest.HighAppTemp);
			ini.SetValue("AppTemp", "HTime", HiLoYest.HighAppTempTime.ToString("HH:mm"));
			// wind chill
			ini.SetValue("WindChill", "Low", HiLoYest.LowWindChill);
			ini.SetValue("WindChill", "LTime", HiLoYest.LowWindChillTime.ToString("HH:mm"));
			// Dewpoint
			ini.SetValue("Dewpoint", "Low", HiLoYest.LowDewPoint);
			ini.SetValue("Dewpoint", "LTime", HiLoYest.LowDewPointTime.ToString("HH:mm"));
			ini.SetValue("Dewpoint", "High", HiLoYest.HighDewPoint);
			ini.SetValue("Dewpoint", "HTime", HiLoYest.HighDewPointTime.ToString("HH:mm"));
			// Solar
			ini.SetValue("Solar", "HighSolarRad", HiLoYest.HighSolar);
			ini.SetValue("Solar", "HighSolarRadTime", HiLoYest.HighSolarTime.ToString("HH:mm"));
			ini.SetValue("Solar", "HighUV", HiLoYest.HighUv);
			ini.SetValue("Solar", "HighUVTime", HiLoYest.HighUvTime.ToString("HH:mm"));
			// Feels like
			ini.SetValue("FeelsLike", "Low", HiLoYest.LowFeelsLike);
			ini.SetValue("FeelsLike", "LTime", HiLoYest.LowFeelsLikeTime.ToString("HH:mm"));
			ini.SetValue("FeelsLike", "High", HiLoYest.HighFeelsLike);
			ini.SetValue("FeelsLike", "HTime", HiLoYest.HighFeelsLikeTime.ToString("HH:mm"));
			// Humidex
			ini.SetValue("Humidex", "High", HiLoYest.HighHumidex);
			ini.SetValue("Humidex", "HTime", HiLoYest.HighHumidexTime.ToString("HH:mm"));

			ini.Flush();

			Cumulus.LogMessage("Written yesterday.ini");
		}

		public void ReadYesterdayFile()
		{
			//var hourInc = cumulus.GetHourInc();

			int? nullInt = null;
			double? nullDbl = null;

			IniFile ini = new IniFile(cumulus.YesterdayFile);

			// Wind
			HiLoYest.HighWind = ini.GetValue("Wind", "Speed", nullDbl);
			HiLoYest.HighWindTime = ini.GetValue("Wind", "SpTime", DateTime.MinValue);
			HiLoYest.HighGust = ini.GetValue("Wind", "Gust", nullDbl);
			HiLoYest.HighGustTime = ini.GetValue("Wind", "Time", DateTime.MinValue);
			HiLoYest.HighGustBearing = ini.GetValue("Wind", "Bearing", nullInt);

			YesterdayWindRun = ini.GetValue("Wind", "Windrun", 0.0);
			YestDominantWindBearing = ini.GetValue("Wind", "DominantWindBearing", 0);
			// Temperature
			HiLoYest.LowTemp = ini.GetValue("Temp", "Low", nullDbl);
			HiLoYest.LowTempTime = ini.GetValue("Temp", "LTime", DateTime.MinValue);
			HiLoYest.HighTemp = ini.GetValue("Temp", "High", nullDbl);
			HiLoYest.HighTempTime = ini.GetValue("Temp", "HTime", DateTime.MinValue);
			YestChillHours = ini.GetValue("Temp", "ChillHours", -1.0);
			YestHeatingDegreeDays = ini.GetValue("Temp", "HeatingDegreeDays", 0.0);
			YestCoolingDegreeDays = ini.GetValue("Temp", "CoolingDegreeDays", 0.0);
			YestAvgTemp = ini.GetValue("Temp", "AvgTemp", 0.0);
			HiLoYest.TempRange = HiLoYest.HighTemp - HiLoYest.LowTemp;
			// Temperature midnight
			HiLoYestMidnight.LowTemp = ini.GetValue("TempMidnight", "Low", nullDbl);
			HiLoYestMidnight.LowTempTime = ini.GetValue("TempMidnight", "LTime", DateTime.MinValue);
			HiLoYestMidnight.HighTemp = ini.GetValue("TempMidnight", "High", nullDbl);
			HiLoYestMidnight.HighTempTime = ini.GetValue("TempMidnight", "HTime", DateTime.MinValue);
			// Pressure
			HiLoYest.LowPress = ini.GetValue("Pressure", "Low", nullDbl);
			HiLoYest.LowPressTime = ini.GetValue("Pressure", "LTime", DateTime.MinValue);
			HiLoYest.HighPress = ini.GetValue("Pressure", "High", nullDbl);
			HiLoYest.HighPressTime = ini.GetValue("Pressure", "HTime", DateTime.MinValue);
			// rain
			HiLoYest.HighRainRate = ini.GetValue("Rain", "High", nullDbl);
			HiLoYest.HighRainRateTime = ini.GetValue("Rain", "HTime", DateTime.MinValue);
			HiLoYest.HighHourlyRain = ini.GetValue("Rain", "HourlyHigh", 0.0);
			HiLoYest.HighHourlyRainTime = ini.GetValue("Rain", "HHourlyTime", DateTime.MinValue);
			HiLoYest.HighRain24h = ini.GetValue("Rain", "High24h", 0.0);
			HiLoYest.HighRain24hTime = ini.GetValue("Rain", "High24hTime", DateTime.MinValue);
			RG11RainYesterday = ini.GetValue("Rain", "RG11Yesterday", 0.0);
			// humidity
			HiLoYest.LowHumidity = ini.GetValue("Humidity", "Low", nullInt);
			HiLoYest.HighHumidity = ini.GetValue("Humidity", "High", nullInt);
			HiLoYest.LowHumidityTime = ini.GetValue("Humidity", "LTime", DateTime.MinValue);
			HiLoYest.HighHumidityTime = ini.GetValue("Humidity", "HTime", DateTime.MinValue);
			// Solar
			YestSunshineHours = ini.GetValue("Solar", "SunshineHours", 0.0);
			// heat index
			HiLoYest.HighHeatIndex = ini.GetValue("HeatIndex", "High", nullDbl);
			HiLoYest.HighHeatIndexTime = ini.GetValue("HeatIndex", "HTime", DateTime.MinValue);
			// App temp
			HiLoYest.LowAppTemp = ini.GetValue("AppTemp", "Low", nullDbl);
			HiLoYest.LowAppTempTime = ini.GetValue("AppTemp", "LTime", DateTime.MinValue);
			HiLoYest.HighAppTemp = ini.GetValue("AppTemp", "High", nullDbl);
			HiLoYest.HighAppTempTime = ini.GetValue("AppTemp", "HTime", DateTime.MinValue);
			// wind chill
			HiLoYest.LowWindChill = ini.GetValue("WindChill", "Low", nullDbl);
			HiLoYest.LowWindChillTime = ini.GetValue("WindChill", "LTime", DateTime.MinValue);
			// Dewpoint
			HiLoYest.LowDewPoint = ini.GetValue("Dewpoint", "Low", nullDbl);
			HiLoYest.LowDewPointTime = ini.GetValue("Dewpoint", "LTime", DateTime.MinValue);
			HiLoYest.HighDewPoint = ini.GetValue("Dewpoint", "High", nullDbl);
			HiLoYest.HighDewPointTime = ini.GetValue("Dewpoint", "HTime", DateTime.MinValue);
			// Solar
			HiLoYest.HighSolar = ini.GetValue("Solar", "HighSolarRad", nullInt);
			HiLoYest.HighSolarTime = ini.GetValue("Solar", "HighSolarRadTime", DateTime.MinValue);
			HiLoYest.HighUv = ini.GetValue("Solar", "HighUV", nullDbl);
			HiLoYest.HighUvTime = ini.GetValue("Solar", "HighUVTime", DateTime.MinValue);
			// Feels like
			HiLoYest.LowFeelsLike = ini.GetValue("FeelsLike", "Low", nullDbl);
			HiLoYest.LowFeelsLikeTime = ini.GetValue("FeelsLike", "LTime", DateTime.MinValue);
			HiLoYest.HighFeelsLike = ini.GetValue("FeelsLike", "High", nullDbl);
			HiLoYest.HighFeelsLikeTime = ini.GetValue("FeelsLike", "HTime", DateTime.MinValue);
			// Humidex
			HiLoYest.HighHumidex = ini.GetValue("Humidex", "High", nullDbl);
			HiLoYest.HighHumidexTime = ini.GetValue("Humidex", "HTime", DateTime.MinValue);
		}

		public void DayReset(DateTime timestamp)
		{
			int drday = timestamp.Day;
			DateTime yesterday = timestamp.AddDays(-1);
			Cumulus.LogMessage("=== Day reset, today = " + drday);
			if (drday != DayResetDay)
			{
				Cumulus.LogMessage("=== Day reset for " + yesterday.Date);

				int day = timestamp.Day;
				int month = timestamp.Month;
				DayResetDay = drday;

				// any last updates?
				// subtract 1 minute to keep within the previous met day
				DoTrendValues(timestamp, true);

				if (cumulus.MySqlStuff.Settings.CustomRollover.Enabled)
				{
					_ = cumulus.MySqlStuff.CustomRolloverTimerTick();
				}

				if (cumulus.CustomHttpRolloverEnabled)
				{
					_ = cumulus.CustomHttpRolloverUpdate();
				}

				_ = cumulus.DoCustomIntervalLogs(timestamp);

				// First save today's extremes
				DoDayfile(timestamp).Wait();
				Cumulus.LogMessage("Raincounter = " + Raincounter + " Raindaystart = " + raindaystart);

				// Calculate yesterday"s rain, allowing for the multiplier -
				// raintotal && raindaystart are not calibrated
				//RainYesterday = (Raincounter - raindaystart) * cumulus.Calib.Rain.Mult;
				RainYesterday = RainToday;
				Cumulus.LogMessage("Rainyesterday (calibrated) set to " + RainYesterday);

				//AddRecentDailyData(timestamp.AddDays(-1), RainYesterday, (cumulus.RolloverHour == 0 ? SunshineHours : SunshineToMidnight), HiLoToday.LowTemp, HiLoToday.HighTemp, YestAvgTemp);
				//RemoveOldRecentDailyData();

				int rdthresh1000;
				if (cumulus.RainDayThreshold > 0)
				{
					rdthresh1000 = Convert.ToInt32(cumulus.RainDayThreshold * 1000.0);
				}
				else
				// default
				{
					// 0.2mm *1000, 0.01in *1000
					rdthresh1000 = cumulus.Units.Rain == 0 ? 200 : 10;
				}

				// set up rain yesterday * 1000 for comparison
				int ryest1000 = Convert.ToInt32(RainYesterday * 1000.0);

				Cumulus.LogMessage("RainDayThreshold = " + cumulus.RainDayThreshold);
				Cumulus.LogMessage("rdt1000=" + rdthresh1000 + " ry1000=" + ryest1000);

				if (ryest1000 >= rdthresh1000)
				{
					// It rained yesterday
					Cumulus.LogMessage("Yesterday was a rain day");
					ConsecutiveRainDays++;
					ConsecutiveDryDays = 0;
					Cumulus.LogMessage("Consecutive rain days = " + ConsecutiveRainDays);
					// check for highs
					if (ConsecutiveRainDays > ThisMonth.LongestWetPeriod.Val)
					{
						ThisMonth.LongestWetPeriod.Val = ConsecutiveRainDays;
						ThisMonth.LongestWetPeriod.Ts = yesterday;
						WriteMonthIniFile();
					}

					if (ConsecutiveRainDays > ThisYear.LongestWetPeriod.Val)
					{
						ThisYear.LongestWetPeriod.Val = ConsecutiveRainDays;
						ThisYear.LongestWetPeriod.Ts = yesterday;
						WriteYearIniFile();
					}

					if (ConsecutiveRainDays > AllTime.LongestWetPeriod.Val)
						SetAlltime(AllTime.LongestWetPeriod, ConsecutiveRainDays, yesterday);

					CheckMonthlyAlltime("LongestWetPeriod", ConsecutiveRainDays, true, yesterday);
				}
				else
				{
					// It didn't rain yesterday
					Cumulus.LogMessage("Yesterday was a dry day");
					ConsecutiveDryDays++;
					ConsecutiveRainDays = 0;
					Cumulus.LogMessage("Consecutive dry days = " + ConsecutiveDryDays);

					// check for highs
					if (ConsecutiveDryDays > ThisMonth.LongestDryPeriod.Val)
					{
						ThisMonth.LongestDryPeriod.Val = ConsecutiveDryDays;
						ThisMonth.LongestDryPeriod.Ts = yesterday;
						WriteMonthIniFile();
					}

					if (ConsecutiveDryDays > ThisYear.LongestDryPeriod.Val)
					{
						ThisYear.LongestDryPeriod.Val = ConsecutiveDryDays;
						ThisYear.LongestDryPeriod.Ts = yesterday;
						WriteYearIniFile();
					}

					if (ConsecutiveDryDays > AllTime.LongestDryPeriod.Val)
						SetAlltime(AllTime.LongestDryPeriod, ConsecutiveDryDays, yesterday);

					CheckMonthlyAlltime("LongestDryPeriod", ConsecutiveDryDays, true, yesterday);
				}

				// offset high temp today timestamp to allow for 0900 roll-over
				int hr;
				int mn;
				DateTime ts;
				try
				{
					hr = HiLoToday.HighTempTime.Hour;
					mn = HiLoToday.HighTempTime.Minute;
					ts = timestamp.Date + new TimeSpan(hr, mn, 0);

					if (hr >= cumulus.RolloverHour)
						// time is between roll-over hour && midnight
						// so subtract a day
						ts = ts.AddDays(-1);
				}
				catch
				{
					ts = timestamp.AddDays(-1);
				}

				if ((HiLoToday.HighTemp ?? Cumulus.DefaultLoVal) < AllTime.LowMaxTemp.Val)
				{
					SetAlltime(AllTime.LowMaxTemp, HiLoToday.HighTemp.Value, ts);
				}

				CheckMonthlyAlltime("LowMaxTemp", HiLoToday.HighTemp, false, ts);

				if ((HiLoToday.HighTemp ?? Cumulus.DefaultLoVal) < ThisMonth.LowMaxTemp.Val)
				{
					ThisMonth.LowMaxTemp.Val = HiLoToday.HighTemp.Value;
					try
					{
						hr = HiLoToday.HighTempTime.Hour;
						mn = HiLoToday.HighTempTime.Minute;
						ThisMonth.LowMaxTemp.Ts = timestamp.Date + new TimeSpan(hr, mn, 0);

						if (hr >= cumulus.RolloverHour)
							// time is between roll-over hour && midnight
							// so subtract a day
							ThisMonth.LowMaxTemp.Ts = ThisMonth.LowMaxTemp.Ts.AddDays(-1);
					}
					catch
					{
						ThisMonth.LowMaxTemp.Ts = timestamp.AddDays(-1);
					}

					WriteMonthIniFile();
				}

				if ((HiLoToday.HighTemp ?? Cumulus.DefaultLoVal) < ThisYear.LowMaxTemp.Val)
				{
					ThisYear.LowMaxTemp.Val = HiLoToday.HighTemp.Value;
					try
					{
						hr = HiLoToday.HighTempTime.Hour;
						mn = HiLoToday.HighTempTime.Minute;
						ThisYear.LowMaxTemp.Ts = timestamp.Date + new TimeSpan(hr, mn, 0);

						if (hr >= cumulus.RolloverHour)
							// time is between roll-over hour && midnight
							// so subtract a day
							ThisYear.LowMaxTemp.Ts = ThisYear.LowMaxTemp.Ts.AddDays(-1);
					}
					catch
					{
						ThisYear.LowMaxTemp.Ts = timestamp.AddDays(-1);
					}

					WriteYearIniFile();
				}

				// offset low temp today timestamp to allow for 0900 roll-over
				try
				{
					hr = HiLoToday.LowTempTime.Hour;
					mn = HiLoToday.LowTempTime.Minute;
					ts = timestamp.Date + new TimeSpan(hr, mn, 0);

					if (hr >= cumulus.RolloverHour)
						// time is between roll-over hour && midnight
						// so subtract a day
						ts = ts.AddDays(-1);
				}
				catch
				{
					ts = timestamp.AddDays(-1);
				}

				if ((HiLoToday.LowTemp ?? Cumulus.DefaultHiVal) > AllTime.HighMinTemp.Val)
				{
					SetAlltime(AllTime.HighMinTemp, HiLoToday.LowTemp.Value, ts);
				}

				CheckMonthlyAlltime("HighMinTemp", HiLoToday.LowTemp, true, ts);

				if (HiLoToday.LowTemp > ThisMonth.HighMinTemp.Val)
				{
					ThisMonth.HighMinTemp.Val = HiLoToday.LowTemp.Value;
					try
					{
						hr = HiLoToday.LowTempTime.Hour;
						mn = HiLoToday.LowTempTime.Minute;
						ThisMonth.HighMinTemp.Ts = timestamp.Date + new TimeSpan(hr, mn, 0);

						if (hr >= cumulus.RolloverHour)
							// time is between roll-over hour && midnight
							// so subtract a day
							ThisMonth.HighMinTemp.Ts = ThisMonth.HighMinTemp.Ts.AddDays(-1);
					}
					catch
					{
						ThisMonth.HighMinTemp.Ts = timestamp.AddDays(-1);
					}
					WriteMonthIniFile();
				}

				if ((HiLoToday.LowTemp ?? Cumulus.DefaultHiVal) > ThisYear.HighMinTemp.Val)
				{
					ThisYear.HighMinTemp.Val = HiLoToday.LowTemp.Value;
					try
					{
						hr = HiLoToday.LowTempTime.Hour;
						mn = HiLoToday.LowTempTime.Minute;
						ThisYear.HighMinTemp.Ts = timestamp.Date + new TimeSpan(hr, mn, 0);

						if (hr >= cumulus.RolloverHour)
							// time is between roll-over hour && midnight
							// so subtract a day
							ThisYear.HighMinTemp.Ts = ThisYear.HighMinTemp.Ts.AddDays(-1);
					}
					catch
					{
						ThisYear.HighMinTemp.Ts = timestamp.AddDays(-1);
					}
					WriteYearIniFile();
				}

				// check temp range for highs && lows
				if ((HiLoToday.TempRange ?? Cumulus.DefaultHiVal) > AllTime.HighDailyTempRange.Val)
					SetAlltime(AllTime.HighDailyTempRange, HiLoToday.TempRange.Value, yesterday);

				if ((HiLoToday.TempRange ?? Cumulus.DefaultLoVal) < AllTime.LowDailyTempRange.Val)
					SetAlltime(AllTime.LowDailyTempRange, HiLoToday.TempRange.Value, yesterday);

				CheckMonthlyAlltime("HighDailyTempRange", HiLoToday.TempRange, true, yesterday);
				CheckMonthlyAlltime("LowDailyTempRange", HiLoToday.TempRange, false, yesterday);

				if ((HiLoToday.TempRange ?? Cumulus.DefaultHiVal) > ThisMonth.HighDailyTempRange.Val)
				{
					ThisMonth.HighDailyTempRange.Val = HiLoToday.TempRange.Value;
					ThisMonth.HighDailyTempRange.Ts = yesterday;
					WriteMonthIniFile();
				}

				if ((HiLoToday.TempRange ?? Cumulus.DefaultLoVal) < ThisMonth.LowDailyTempRange.Val)
				{
					ThisMonth.LowDailyTempRange.Val = HiLoToday.TempRange.Value;
					ThisMonth.LowDailyTempRange.Ts = yesterday;
					WriteMonthIniFile();
				}

				if ((HiLoToday.TempRange ?? Cumulus.DefaultHiVal) > ThisYear.HighDailyTempRange.Val)
				{
					ThisYear.HighDailyTempRange.Val = HiLoToday.TempRange.Value;
					ThisYear.HighDailyTempRange.Ts = yesterday;
					WriteYearIniFile();
				}

				if ((HiLoToday.TempRange ?? Cumulus.DefaultLoVal) < ThisYear.LowDailyTempRange.Val)
				{
					ThisYear.LowDailyTempRange.Val = HiLoToday.TempRange.Value;
					ThisYear.LowDailyTempRange.Ts = yesterday;
					WriteYearIniFile();
				}

				RG11RainYesterday = RG11RainToday;
				RG11RainToday = 0;

				if (day == 1)
				{
					// new month starting
					Cumulus.LogMessage(" New month starting - " + month);

					CopyMonthIniFile(timestamp.AddDays(-1)).Wait();

					rainthismonth = 0;

					ThisMonth.HighGust.Val = calibratedgust;
					ThisMonth.HighWind.Val = WindAverage ?? Cumulus.DefaultHiVal;
					ThisMonth.HighTemp.Val = Temperature ?? Cumulus.DefaultHiVal;
					ThisMonth.LowTemp.Val = Temperature ?? Cumulus.DefaultLoVal;
					ThisMonth.HighAppTemp.Val = ApparentTemp ?? Cumulus.DefaultHiVal;
					ThisMonth.LowAppTemp.Val = ApparentTemp ?? Cumulus.DefaultLoVal;
					ThisMonth.HighFeelsLike.Val = FeelsLike ?? Cumulus.DefaultHiVal;
					ThisMonth.LowFeelsLike.Val = FeelsLike ?? Cumulus.DefaultLoVal;
					ThisMonth.HighHumidex.Val = Humidex ?? Cumulus.DefaultHiVal;
					ThisMonth.HighPress.Val = Pressure ?? Cumulus.DefaultHiVal;
					ThisMonth.LowPress.Val = Pressure ?? Cumulus.DefaultLoVal;
					ThisMonth.HighRainRate.Val = RainRate ?? Cumulus.DefaultHiVal;
					ThisMonth.HourlyRain.Val = RainLastHour;
					ThisMonth.HighRain24Hours.Val = RainLast24Hour;
					ThisMonth.DailyRain.Val = Cumulus.DefaultHiVal;
					ThisMonth.HighHumidity.Val = Humidity ?? Cumulus.DefaultHiVal;
					ThisMonth.LowHumidity.Val = Humidity ?? Cumulus.DefaultLoVal;
					ThisMonth.HighHeatIndex.Val = HeatIndex ?? Cumulus.DefaultHiVal;
					ThisMonth.LowChill.Val = WindChill ?? Cumulus.DefaultLoVal;
					ThisMonth.HighMinTemp.Val = Cumulus.DefaultHiVal;
					ThisMonth.LowMaxTemp.Val = Cumulus.DefaultLoVal;
					ThisMonth.HighDewPoint.Val = Dewpoint ?? Cumulus.DefaultHiVal;
					ThisMonth.LowDewPoint.Val = Dewpoint ?? Cumulus.DefaultLoVal;
					ThisMonth.HighWindRun.Val = Cumulus.DefaultHiVal;
					ThisMonth.LongestDryPeriod.Val = 0;
					ThisMonth.LongestWetPeriod.Val = 0;
					ThisMonth.HighDailyTempRange.Val = Cumulus.DefaultHiVal;
					ThisMonth.LowDailyTempRange.Val = Cumulus.DefaultLoVal;

					// this month highs && lows - timestamps
					ThisMonth.HighGust.Ts = timestamp;
					ThisMonth.HighWind.Ts = timestamp;
					ThisMonth.HighTemp.Ts = timestamp;
					ThisMonth.LowTemp.Ts = timestamp;
					ThisMonth.HighAppTemp.Ts = timestamp;
					ThisMonth.LowAppTemp.Ts = timestamp;
					ThisMonth.HighFeelsLike.Ts = timestamp;
					ThisMonth.LowFeelsLike.Ts = timestamp;
					ThisMonth.HighHumidex.Ts = timestamp;
					ThisMonth.HighPress.Ts = timestamp;
					ThisMonth.LowPress.Ts = timestamp;
					ThisMonth.HighRainRate.Ts = timestamp;
					ThisMonth.HourlyRain.Ts = timestamp;
					ThisMonth.HighRain24Hours.Ts = timestamp;
					ThisMonth.DailyRain.Ts = timestamp;
					ThisMonth.HighHumidity.Ts = timestamp;
					ThisMonth.LowHumidity.Ts = timestamp;
					ThisMonth.HighHeatIndex.Ts = timestamp;
					ThisMonth.LowChill.Ts = timestamp;
					ThisMonth.HighMinTemp.Ts = timestamp;
					ThisMonth.LowMaxTemp.Ts = timestamp;
					ThisMonth.HighDewPoint.Ts = timestamp;
					ThisMonth.LowDewPoint.Ts = timestamp;
					ThisMonth.HighWindRun.Ts = timestamp;
					ThisMonth.LongestDryPeriod.Ts = timestamp;
					ThisMonth.LongestWetPeriod.Ts = timestamp;
					ThisMonth.LowDailyTempRange.Ts = timestamp;
					ThisMonth.HighDailyTempRange.Ts = timestamp;
				}
				else
					rainthismonth += RainYesterday ?? 0;

				if ((day == 1) && (month == 1))
				{
					// new year starting
					Cumulus.LogMessage(" New year starting");

					_ = CopyYearIniFile(timestamp.AddDays(-1));

					ThisYear.HighGust.Val = calibratedgust;
					ThisYear.HighWind.Val = WindAverage ?? Cumulus.DefaultHiVal;
					ThisYear.HighTemp.Val = Temperature ?? Cumulus.DefaultHiVal;
					ThisYear.LowTemp.Val = Temperature ?? Cumulus.DefaultLoVal;
					ThisYear.HighAppTemp.Val = ApparentTemp ?? Cumulus.DefaultHiVal;
					ThisYear.LowAppTemp.Val = ApparentTemp ?? Cumulus.DefaultLoVal;
					ThisYear.HighFeelsLike.Val = FeelsLike ?? Cumulus.DefaultHiVal;
					ThisYear.LowFeelsLike.Val = FeelsLike ?? Cumulus.DefaultLoVal;
					ThisYear.HighHumidex.Val = Humidex ?? Cumulus.DefaultHiVal;
					ThisYear.HighPress.Val = Pressure ?? Cumulus.DefaultHiVal;
					ThisYear.LowPress.Val = Pressure ?? Cumulus.DefaultLoVal;
					ThisYear.HighRainRate.Val = RainRate ?? Cumulus.DefaultHiVal;
					ThisYear.HourlyRain.Val = RainLastHour;
					ThisYear.HighRain24Hours.Val = RainLast24Hour;
					ThisYear.DailyRain.Val = Cumulus.DefaultHiVal;
					ThisYear.MonthlyRain.Val = Cumulus.DefaultHiVal;
					ThisYear.HighHumidity.Val = Humidity ?? Cumulus.DefaultHiVal;
					ThisYear.LowHumidity.Val = Humidity ?? Cumulus.DefaultLoVal;
					ThisYear.HighHeatIndex.Val = HeatIndex ?? Cumulus.DefaultHiVal;
					ThisYear.LowChill.Val = WindChill ?? Cumulus.DefaultLoVal;
					ThisYear.HighMinTemp.Val = Cumulus.DefaultHiVal;
					ThisYear.LowMaxTemp.Val = Cumulus.DefaultLoVal;
					ThisYear.HighDewPoint.Val = Dewpoint ?? Cumulus.DefaultHiVal;
					ThisYear.LowDewPoint.Val = Dewpoint ?? Cumulus.DefaultLoVal;
					ThisYear.HighWindRun.Val = Cumulus.DefaultHiVal;
					ThisYear.LongestDryPeriod.Val = 0;
					ThisYear.LongestWetPeriod.Val = 0;
					ThisYear.HighDailyTempRange.Val = Cumulus.DefaultHiVal;
					ThisYear.LowDailyTempRange.Val = Cumulus.DefaultLoVal;

					// this Year highs && lows - timestamps
					ThisYear.HighGust.Ts = timestamp;
					ThisYear.HighWind.Ts = timestamp;
					ThisYear.HighTemp.Ts = timestamp;
					ThisYear.LowTemp.Ts = timestamp;
					ThisYear.HighAppTemp.Ts = timestamp;
					ThisYear.LowAppTemp.Ts = timestamp;
					ThisYear.HighFeelsLike.Ts = timestamp;
					ThisYear.LowFeelsLike.Ts = timestamp;
					ThisYear.HighHumidex.Ts = timestamp;
					ThisYear.HighPress.Ts = timestamp;
					ThisYear.LowPress.Ts = timestamp;
					ThisYear.HighRainRate.Ts = timestamp;
					ThisYear.HourlyRain.Ts = timestamp;
					ThisYear.HighRain24Hours.Ts = timestamp;
					ThisYear.DailyRain.Ts = timestamp;
					ThisYear.MonthlyRain.Ts = timestamp;
					ThisYear.HighHumidity.Ts = timestamp;
					ThisYear.LowHumidity.Ts = timestamp;
					ThisYear.HighHeatIndex.Ts = timestamp;
					ThisYear.LowChill.Ts = timestamp;
					ThisYear.HighMinTemp.Ts = timestamp;
					ThisYear.LowMaxTemp.Ts = timestamp;
					ThisYear.HighDewPoint.Ts = timestamp;
					ThisYear.LowDewPoint.Ts = timestamp;
					ThisYear.HighWindRun.Ts = timestamp;
					ThisYear.LongestDryPeriod.Ts = timestamp;
					ThisYear.LongestWetPeriod.Ts = timestamp;
					ThisYear.HighDailyTempRange.Ts = timestamp;
					ThisYear.LowDailyTempRange.Ts = timestamp;

					// reset the ET annual total for Davis WLL stations only
					// because we mimic the annual total and it is not reset like VP2 stations
					if (cumulus.StationType == StationTypes.WLL || cumulus.StationOptions.CalculatedET)
					{
						Cumulus.LogMessage(" Resetting Annual ET total");
						AnnualETTotal = 0;
					}
				}

				if ((day == 1) && (month == cumulus.RainSeasonStart))
				{
					// new year starting
					Cumulus.LogMessage(" New rain season starting");
					rainthisyear = 0;
				}
				else
				{
					rainthisyear += RainYesterday ?? 0;
				}

				if ((day == 1) && (month == cumulus.ChillHourSeasonStart))
				{
					// new year starting
					Cumulus.LogMessage(" Chill hour season starting");
					ChillHours = 0;
				}

				if ((day == 1) && (month == cumulus.GrowingYearStarts))
				{
					Cumulus.LogMessage(" New growing degree day season starting");
					GrowingDegreeDaysThisYear1 = 0;
					GrowingDegreeDaysThisYear2 = 0;
				}

				if (HiLoToday.HighTemp.HasValue && HiLoToday.LowTemp.HasValue)
				{
					GrowingDegreeDaysThisYear1 += MeteoLib.GrowingDegreeDays(ConvertUserTempToC(HiLoToday.HighTemp).Value, ConvertUserTempToC(HiLoToday.LowTemp).Value, ConvertUserTempToC(cumulus.GrowingBase1).Value, cumulus.GrowingCap30C);
					GrowingDegreeDaysThisYear2 += MeteoLib.GrowingDegreeDays(ConvertUserTempToC(HiLoToday.HighTemp).Value, ConvertUserTempToC(HiLoToday.LowTemp).Value, ConvertUserTempToC(cumulus.GrowingBase2).Value, cumulus.GrowingCap30C);
				}

				// Now reset all values to the current or default ones
				// We may be doing a roll-over from the first logger entry,
				// && as we do the roll-over before processing the entry, the
				// current items may not be set up.

				raindaystart = Raincounter;
				Cumulus.LogMessage("Raindaystart set to " + raindaystart);

				RainToday = dataValuesUpdated.Rain ? 0 : null;

				TempTotalToday = Temperature ?? 0;
				tempsamplestoday = Temperature.HasValue ? 1 : 0;

				// Copy today"s high wind settings to yesterday
				HiLoYest.HighWind = HiLoToday.HighWind;
				HiLoYest.HighWindTime = HiLoToday.HighWindTime;
				HiLoYest.HighGust = HiLoToday.HighGust;
				HiLoYest.HighGustTime = HiLoToday.HighGustTime;
				HiLoYest.HighGustBearing = HiLoToday.HighGustBearing;

				// Reset today"s high wind settings
				HiLoToday.HighGust = calibratedgust;
				HiLoToday.HighGustBearing = Bearing;
				HiLoToday.HighWind = WindAverage;

				HiLoToday.HighWindTime = timestamp;
				HiLoToday.HighGustTime = timestamp;

				// Copy today"s high temp settings to yesterday
				HiLoYest.HighTemp = HiLoToday.HighTemp;
				HiLoYest.HighTempTime = HiLoToday.HighTempTime;
				// Reset today"s high temp settings
				HiLoToday.HighTemp = Temperature;
				HiLoToday.HighTempTime = timestamp;

				// Copy today"s low temp settings to yesterday
				HiLoYest.LowTemp = HiLoToday.LowTemp;
				HiLoYest.LowTempTime = HiLoToday.LowTempTime;
				// Reset today"s low temp settings
				HiLoToday.LowTemp = Temperature;
				HiLoToday.LowTempTime = timestamp;

				HiLoYest.TempRange = HiLoToday.TempRange;
				HiLoToday.TempRange = 0;

				// Copy today"s low pressure settings to yesterday
				HiLoYest.LowPress = HiLoToday.LowPress;
				HiLoYest.LowPressTime = HiLoToday.LowPressTime;
				// Reset today"s low pressure settings
				HiLoToday.LowPress = Pressure;
				HiLoToday.LowPressTime = timestamp;

				// Copy today"s high pressure settings to yesterday
				HiLoYest.HighPress = HiLoToday.HighPress;
				HiLoYest.HighPressTime = HiLoToday.HighPressTime;
				// Reset today"s high pressure settings
				HiLoToday.HighPress = Pressure;
				HiLoToday.HighPressTime = timestamp;

				// Copy today"s high rain rate settings to yesterday
				HiLoYest.HighRainRate = HiLoToday.HighRainRate;
				HiLoYest.HighRainRateTime = HiLoToday.HighRainRateTime;
				// Reset today"s high rain rate settings
				HiLoToday.HighRainRate = RainRate;
				HiLoToday.HighRainRateTime = timestamp;

				HiLoYest.HighHourlyRain = HiLoToday.HighHourlyRain;
				HiLoYest.HighHourlyRainTime = HiLoToday.HighHourlyRainTime;
				HiLoToday.HighHourlyRain = RainLastHour;
				HiLoToday.HighHourlyRainTime = timestamp;

				HiLoYest.HighRain24h = HiLoToday.HighRain24h;
				HiLoYest.HighRain24hTime = HiLoToday.HighRain24hTime;
				HiLoToday.HighRain24h = RainLast24Hour;
				HiLoToday.HighRain24hTime = timestamp;

				YesterdayWindRun = WindRunToday;
				WindRunToday = 0;

				YestDominantWindBearing = DominantWindBearing;

				DominantWindBearing = 0;
				DominantWindBearingX = 0;
				DominantWindBearingY = 0;
				DominantWindBearingMinutes = 0;

				YestChillHours = ChillHours;
				YestHeatingDegreeDays = HeatingDegreeDays;
				YestCoolingDegreeDays = CoolingDegreeDays;
				HeatingDegreeDays = 0;
				CoolingDegreeDays = 0;

				// reset startofdayET value
				StartofdayET = AnnualETTotal;
				Cumulus.LogMessage("StartofdayET set to " + StartofdayET);
				ET = 0;

				// Humidity
				HiLoYest.LowHumidity = HiLoToday.LowHumidity;
				HiLoYest.LowHumidityTime = HiLoToday.LowHumidityTime;
				HiLoToday.LowHumidity = Humidity;
				HiLoToday.LowHumidityTime = timestamp;

				HiLoYest.HighHumidity = HiLoToday.HighHumidity;
				HiLoYest.HighHumidityTime = HiLoToday.HighHumidityTime;
				HiLoToday.HighHumidity = Humidity;
				HiLoToday.HighHumidityTime = timestamp;

				// heat index
				HiLoYest.HighHeatIndex = HiLoToday.HighHeatIndex;
				HiLoYest.HighHeatIndexTime = HiLoToday.HighHeatIndexTime;
				HiLoToday.HighHeatIndex = HeatIndex;
				HiLoToday.HighHeatIndexTime = timestamp;

				// App temp
				HiLoYest.HighAppTemp = HiLoToday.HighAppTemp;
				HiLoYest.HighAppTempTime = HiLoToday.HighAppTempTime;
				HiLoToday.HighAppTemp = ApparentTemp;
				HiLoToday.HighAppTempTime = timestamp;

				HiLoYest.LowAppTemp = HiLoToday.LowAppTemp;
				HiLoYest.LowAppTempTime = HiLoToday.LowAppTempTime;
				HiLoToday.LowAppTemp = ApparentTemp;
				HiLoToday.LowAppTempTime = timestamp;

				// wind chill
				HiLoYest.LowWindChill = HiLoToday.LowWindChill;
				HiLoYest.LowWindChillTime = HiLoToday.LowWindChillTime;
				HiLoToday.LowWindChill = WindChill;
				HiLoToday.LowWindChillTime = timestamp;

				// dew point
				HiLoYest.HighDewPoint = HiLoToday.HighDewPoint;
				HiLoYest.HighDewPointTime = HiLoToday.HighDewPointTime;
				HiLoToday.HighDewPoint = Dewpoint;
				HiLoToday.HighDewPointTime = timestamp;

				HiLoYest.LowDewPoint = HiLoToday.LowDewPoint;
				HiLoYest.LowDewPointTime = HiLoToday.LowDewPointTime;
				HiLoToday.LowDewPoint = Dewpoint;
				HiLoToday.LowDewPointTime = timestamp;

				// solar
				HiLoYest.HighSolar = HiLoToday.HighSolar;
				HiLoYest.HighSolarTime = HiLoToday.HighSolarTime;
				HiLoToday.HighSolar = SolarRad;
				HiLoToday.HighSolarTime = timestamp;

				HiLoYest.HighUv = HiLoToday.HighUv;
				HiLoYest.HighUvTime = HiLoToday.HighUvTime;
				HiLoToday.HighUv = UV;
				HiLoToday.HighUvTime = timestamp;

				// Feels like
				HiLoYest.HighFeelsLike = HiLoToday.HighFeelsLike;
				HiLoYest.HighFeelsLikeTime = HiLoToday.HighFeelsLikeTime;
				HiLoToday.HighFeelsLike = FeelsLike;
				HiLoToday.HighFeelsLikeTime = timestamp;

				HiLoYest.LowFeelsLike = HiLoToday.LowFeelsLike;
				HiLoYest.LowFeelsLikeTime = HiLoToday.LowFeelsLikeTime;
				HiLoToday.LowFeelsLike = FeelsLike;
				HiLoToday.LowFeelsLikeTime = timestamp;

				// Humidex
				HiLoYest.HighHumidex = HiLoToday.HighHumidex;
				HiLoYest.HighHumidexTime = HiLoToday.HighHumidexTime;
				HiLoToday.HighHumidex = Humidex;
				HiLoToday.HighHumidexTime = timestamp;

				// Lightning
				LightningStrikesToday = 0;

				// Save the current values in case of program restart
				WriteTodayFile(timestamp, true);
				WriteYesterdayFile(timestamp);

				if (cumulus.NOAAconf.Create)
				{
					try
					{
						NOAA noaa = new NOAA(cumulus, this);
						var utf8WithoutBom = new UTF8Encoding(false);
						var encoding = cumulus.NOAAconf.UseUtf8 ? utf8WithoutBom : Encoding.GetEncoding("iso-8859-1");

						List<string> report;

						DateTime noaats = timestamp.AddDays(-1);

						// do monthly NOAA report
						var monthDate = new DateOnly(noaats.Year, noaats.Month, 1);
						Cumulus.LogMessage($"Creating NOAA monthly report for {monthDate.Year}/{monthDate.Month:D2}");
						report = noaa.CreateMonthlyReport(monthDate);
						cumulus.NOAAconf.LatestMonthReport = FormatDateTime(cumulus.NOAAconf.MonthFile, monthDate);
						string noaafile = cumulus.ReportPath + cumulus.NOAAconf.LatestMonthReport;
						Cumulus.LogMessage("Saving monthly report as " + noaafile);
						File.WriteAllLines(noaafile, report, encoding);

						// do yearly NOAA report
						var yearDate = new DateOnly(noaats.Year, 1, 1);
						Cumulus.LogMessage($"Creating NOAA yearly report for {yearDate.Year}");
						report = noaa.CreateYearlyReport(yearDate);
						cumulus.NOAAconf.LatestYearReport = FormatDateTime(cumulus.NOAAconf.YearFile, yearDate);
						noaafile = cumulus.ReportPath + cumulus.NOAAconf.LatestYearReport;
						Cumulus.LogMessage("Saving yearly report as " + noaafile);
						File.WriteAllLines(noaafile, report, encoding);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "Error creating NOAA reports");
					}
				}

				// Do we need to upload NOAA reports on next FTP?
				cumulus.NOAAconf.NeedFtp = cumulus.NOAAconf.AutoFtp;
				cumulus.NOAAconf.NeedCopy = cumulus.NOAAconf.AutoCopy;

				if (cumulus.NOAAconf.NeedFtp || cumulus.NOAAconf.NeedCopy)
				{
					Cumulus.LogMessage("NOAA reports will be uploaded at next web update");
				}

				// Do the End of day Extra files
				// This will set a flag to transfer on next FTP if required
				cumulus.DoExtraEndOfDayFiles().Wait();
				if (cumulus.EODfilesNeedFTP)
				{
					Cumulus.LogMessage("Extra files will be uploaded at next web update");
				}

				// Do the Daily graph data files
				Graphs.CreateEodGraphDataFiles();
				Cumulus.LogMessage("If required the daily graph data files will be uploaded at next web update");


				if (!string.IsNullOrEmpty(cumulus.DailyProgram))
				{
					Cumulus.LogMessage("Executing daily program: " + cumulus.DailyProgram + " params: " + cumulus.DailyParams);
					try
					{
						// Prepare the process to run
						ProcessStartInfo start = new ProcessStartInfo
						{
							// Enter in the command line arguments
							Arguments = cumulus.DailyParams,
							// Enter the executable to run, including the complete path
							FileName = cumulus.DailyProgram,
							// Don't show a console window
							CreateNoWindow = true
						};
						// Run the external process
						Process.Start(start);
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "Error executing external program");
					}
				}

				CurrentDay = timestamp.Day;
				CurrentMonth = timestamp.Month;
				CurrentYear = timestamp.Year;
				Cumulus.LogMessage("=== Day reset complete");
				Cumulus.LogMessage("Now recording data for day=" + CurrentDay + " month=" + CurrentMonth + " year=" + CurrentYear);
			}
			else
			{
				Cumulus.LogMessage("=== Day reset already done on day " + drday);
			}
		}

		private static async Task CopyMonthIniFile(DateTime ts)
		{
			string year = ts.Year.ToString();
			string month = ts.Month.ToString("D2");
			string savedFile = cumulus.Datapath + "month" + year + month + ".ini";
			Cumulus.LogMessage("Saving month.ini file as " + savedFile);
			try
			{
				await Utils.CopyFileAsync(cumulus.MonthIniFile, savedFile);
			}
			catch (Exception)
			{
				// ignore - probably just that it has already been copied
			}
		}

		private static async Task CopyYearIniFile(DateTime ts)
		{
			string year = ts.Year.ToString();
			string savedFile = cumulus.Datapath + "year" + year + ".ini";
			Cumulus.LogMessage("Saving year.ini file as " + savedFile);
			try
			{
				await Utils.CopyFileAsync(cumulus.YearIniFile, savedFile);
			}
			catch (Exception)
			{
				// ignore - probably just that it has already been copied
			}
		}

		private async Task DoDayfile(DateTime timestamp)
		{
			// Writes an entry to the daily extreme log file. Fields are comma-separated.
			// 0   Date in the form dd/mm/yy
			// 1  Highest wind gust
			// 2  Bearing of highest wind gust
			// 3  Time of highest wind gust
			// 4  Minimum temperature
			// 5  Time of minimum temperature
			// 6  Maximum temperature
			// 7  Time of maximum temperature
			// 8  Minimum sea level pressure
			// 9  Time of minimum pressure
			// 10  Maximum sea level pressure
			// 11  Time of maximum pressure
			// 12  Maximum rainfall rate
			// 13  Time of maximum rainfall rate
			// 14  Total rainfall for the day
			// 15  Average temperature for the day
			// 16  Total wind run
			// 17  Highest average wind speed
			// 18  Time of highest average wind speed
			// 19  Lowest humidity
			// 20  Time of lowest humidity
			// 21  Highest humidity
			// 22  Time of highest humidity
			// 23  Total evapotranspiration
			// 24  Total hours of sunshine
			// 25  High heat index
			// 26  Time of high heat index
			// 27  High apparent temperature
			// 28  Time of high apparent temperature
			// 29  Low apparent temperature
			// 30  Time of low apparent temperature
			// 31  High hourly rain
			// 32  Time of high hourly rain
			// 33  Low wind chill
			// 34  Time of low wind chill
			// 35  High dew point
			// 36  Time of high dew point
			// 37  Low dew point
			// 38  Time of low dew point
			// 39  Dominant wind bearing
			// 40  Heating degree days
			// 41  Cooling degree days
			// 42  High solar radiation
			// 43  Time of high solar radiation
			// 44  High UV Index
			// 45  Time of high UV Index
			// 46  High Feels like
			// 47  Time of high feels like
			// 48  Low feels like
			// 49  Time of low feels like
			// 50  High Humidex
			// 51  Time of high Humidex
			// 52  Chill hours
			// 53  Max Rain 24 hours
			// 54  Max Rain 24 hours Time

			double AvgTemp;
			if (tempsamplestoday > 0)
				AvgTemp = TempTotalToday / tempsamplestoday;
			else
				AvgTemp = 0;

			// save the value for yesterday
			YestAvgTemp = AvgTemp;

			//var sep = ",";
			//var sep2 = ",,";


			// Add a new record to the database
			var tim = timestamp.AddDays(-1);
			var newRec = new DayData()
			{
				Timestamp = tim.Date,
				HighGust = HiLoToday.HighGust,
				HighGustBearing = HiLoToday.HighGustBearing,
				HighGustTime = HiLoToday.HighGust.HasValue ? HiLoToday.HighGustTime : null,
				LowTemp = HiLoToday.LowTemp,
				LowTempTime = HiLoToday.LowTemp.HasValue ? HiLoToday.LowTempTime : null,
				HighTemp = HiLoToday.HighTemp,
				HighTempTime = HiLoToday.HighTemp.HasValue ? HiLoToday.HighTempTime : null,
				LowPress = HiLoToday.LowPress,
				LowPressTime = HiLoToday.LowPress.HasValue ? HiLoToday.LowPressTime : null,
				HighPress = HiLoToday.HighPress,
				HighPressTime = HiLoToday.HighPress.HasValue ? HiLoToday.HighPressTime : null,
				HighRainRate = HiLoToday.HighRainRate,
				HighRainRateTime = HiLoToday.HighRainRate.HasValue ? HiLoToday.HighRainRateTime : null,
				TotalRain = RainToday,
				AvgTemp = HiLoToday.LowTemp.HasValue ? AvgTemp : null,
				WindRun = HiLoToday.HighWind.HasValue ? WindRunToday : null,
				HighAvgWind = HiLoToday.HighWind,
				HighAvgWindTime = HiLoToday.HighWind.HasValue ? HiLoToday.HighWindTime : null,
				LowHumidity = HiLoToday.LowHumidity,
				LowHumidityTime = HiLoToday.LowHumidity.HasValue ? HiLoToday.LowHumidityTime : null,
				HighHumidity = HiLoToday.HighHumidity,
				HighHumidityTime = HiLoToday.HighHumidity.HasValue ? HiLoToday.HighHumidityTime : null,
				ET = HiLoToday.HighSolar.HasValue ? ET : null,
				SunShineHours = HiLoToday.HighSolar.HasValue ? (cumulus.RolloverHour == 0 ? SunshineHours : SunshineToMidnight) : null,
				HighHeatIndex = HiLoToday.HighHeatIndex,
				HighHeatIndexTime = HiLoToday.HighHeatIndex.HasValue ? HiLoToday.HighHeatIndexTime : null,
				HighAppTemp = HiLoToday.HighAppTemp,
				HighAppTempTime = HiLoToday.HighAppTemp.HasValue ? HiLoToday.HighAppTempTime : null,
				LowAppTemp = HiLoToday.LowAppTemp,
				LowAppTempTime = HiLoToday.LowAppTemp.HasValue ? HiLoToday.LowAppTempTime : null,
				HighHourlyRain = HiLoToday.HighHourlyRain,
				HighHourlyRainTime = HiLoToday.HighHourlyRainTime,
				LowWindChill = HiLoToday.LowWindChill,
				LowWindChillTime = HiLoToday.LowWindChill.HasValue ? HiLoToday.LowWindChillTime : null,
				HighDewPoint = HiLoToday.HighDewPoint,
				HighDewPointTime = HiLoToday.HighDewPoint.HasValue ? HiLoToday.HighDewPointTime : null,
				LowDewPoint = HiLoToday.LowDewPoint,
				LowDewPointTime = HiLoToday.LowDewPoint.HasValue ? HiLoToday.LowDewPointTime : null,
				DominantWindBearing = HiLoToday.HighWind.HasValue ? DominantWindBearing : null,
				HeatingDegreeDays = HiLoToday.LowTemp.HasValue ? HeatingDegreeDays : null,
				CoolingDegreeDays = HiLoToday.LowTemp.HasValue ? CoolingDegreeDays : null,
				HighSolar = HiLoToday.HighSolar,
				HighSolarTime = HiLoToday.HighSolar.HasValue ? HiLoToday.HighSolarTime : null,
				HighUv = HiLoToday.HighUv,
				HighUvTime = HiLoToday.HighUv.HasValue ? HiLoToday.HighUvTime : null,
				HighFeelsLike = HiLoToday.HighFeelsLike,
				HighFeelsLikeTime = HiLoToday.HighFeelsLike.HasValue ? HiLoToday.HighFeelsLikeTime : null,
				LowFeelsLike = HiLoToday.LowFeelsLike,
				LowFeelsLikeTime = HiLoToday.LowFeelsLike.HasValue ? HiLoToday.LowFeelsLikeTime : null,
				HighHumidex = HiLoToday.HighHumidex,
				HighHumidexTime = HiLoToday.HighHumidex.HasValue ? HiLoToday.HighHumidexTime : null,
				ChillHours = HiLoToday.LowTemp.HasValue ? ChillHours : null,
				HighRain24Hours = HiLoToday.HighRain24h,
				HighRain24HoursTime = HiLoToday.HighRain24h.HasValue ? HiLoToday.HighRain24hTime : null
			};

			_ = Database.InsertOrReplace(newRec);



			var csv = newRec.ToCSV(true);

			Cumulus.LogMessage("DailyData entry:");
			Cumulus.LogMessage(csv);

			if (cumulus.StationOptions.LogMainStation)
			{
				var success = false;
				var retries = Cumulus.LogFileRetries;
				do
				{
					try
					{
						Cumulus.LogMessage("dayfile-v4.txt opened for writing");

						if ((HiLoToday.HighTemp < -400) || (HiLoToday.LowTemp > 900))
						{
							Cumulus.LogMessage("***Error: Daily values are still at default at end of day");
							Cumulus.LogMessage("Data not logged to dayfile-v4.txt");
							return;
						}
						else
						{
							Cumulus.LogMessage("Writing entry to dayfile-v4.txt");

							using FileStream fs = new FileStream(cumulus.DayFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
							using StreamWriter file = new StreamWriter(fs);

							await file.WriteLineAsync(csv);
							file.Close();
							fs.Close();

							success = true;

							Cumulus.LogMessage($"Dayfile log entry for {newRec.Timestamp.ToString("dd/MM/yy", invDate)} written");
						}
					}
					catch (IOException ex)
					{
						if ((uint)ex.HResult == 0x80070020) // -2147024864
						{
							cumulus.LogExceptionMessage(ex, "Error dayfile-v4.txt is in use");
							retries--;
							Thread.Sleep(250);
						}
						else
							cumulus.LogExceptionMessage(ex, "Error writing to dayfile-v4.txt");
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "Error writing to dayfile-v4.txt");
					}
				} while (!success && retries >= 0);
			}


			if (cumulus.MySqlStuff.Settings.Dayfile.Enabled)
			{
				cumulus.MySqlStuff.DoDailyData(timestamp, AvgTemp);
			}
		}

		/// <summary>
		///  Calculate checksum of data received from serial port
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected static int checksum(List<int> data)
		{
			int sum = 0;

			for (int i = 0; i < data.Count - 1; i++)
			{
				sum += data[i];
			}

			return sum % 256;
		}

		protected static int BCDchartoint(int c)
		{
			return ((c / 16) * 10) + (c % 16);
		}

		/// <summary>
		///  Convert temp supplied in C to units in use
		/// </summary>
		/// <param name="value">Temp in C</param>
		/// <returns>Temp in configured units</returns>
		public static double? ConvertTempCToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			if (cumulus.Units.Temp == 1)
			{
				return MeteoLib.CToF(value.Value);
			}
			else
			{
				// C
				return value;
			}
		}

		/// <summary>
		///  Convert temp supplied in F to units in use
		/// </summary>
		/// <param name="value">Temp in F</param>
		/// <returns>Temp in configured units</returns>
		public static double? ConvertTempFToUser(double? value)
		{
			if (!value.HasValue)
				return value;

			if (cumulus.Units.Temp == 0)
			{
				return MeteoLib.FtoC(value.Value);
			}
			else
			{
				// F
				return value;
			}
		}

		/// <summary>
		///  Convert temp supplied in user units to C
		/// </summary>
		/// <param name="value">Temp in configured units</param>
		/// <returns>Temp in C</returns>
		public static double? ConvertUserTempToC(double? value)
		{
			if (!value.HasValue)
				return value;

			if (cumulus.Units.Temp == 1)
			{
				return MeteoLib.FtoC(value.Value);
			}
			else
			{
				// C
				return value;
			}
		}

		/// <summary>
		///  Convert temp supplied in user units to F
		/// </summary>
		/// <param name="value">Temp in configured units</param>
		/// <returns>Temp in F</returns>
		public static double? ConvertUserTempToF(double? value)
		{
			if (!value.HasValue)
				return value;

			if (cumulus.Units.Temp == 1)
			{
				return value;
			}
			else
			{
				// C
				return MeteoLib.CToF(value.Value);
			}
		}

		/// <summary>
		///  Converts wind supplied in m/s to user units
		/// </summary>
		/// <param name="value">Wind in m/s</param>
		/// <returns>Wind in configured units</returns>
		public static double ConvertWindMSToUser(double value)
		{
			return cumulus.Units.Wind switch
			{
				0 => value,
				1 => value * 2.23693629,
				2 => value * 3.6,
				3 => value * 1.94384449,
				_ => 0,
			};
		}

		/// <summary>
		///  Converts wind supplied in mph to user units
		/// </summary>
		/// <param name="value">Wind in mph</param>
		/// <returns>Wind in configured units</returns>
		public static double? ConvertWindMPHToUser(double? value)
		{
			if (value == null)
				return null;

			return cumulus.Units.Wind switch
			{
				0 => value * 0.44704,
				1 => value,
				2 => value * 1.60934,
				3 => value * 0.868976,
				_ => 0,
			};
		}

		/// <summary>
		/// Converts wind in user units to m/s
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual double? ConvertUserWindToMS(double? value)
		{
			if (value == null) return null;

			return cumulus.Units.Wind switch
			{
				0 => value,
				1 => value / 2.23693629,
				2 => value / 3.6F,
				3 => value / 1.94384449,
				_ => 0,
			};
		}

		/// <summary>
		/// Converts value in kilometres to distance unit based on users configured wind units
		/// </summary>
		/// <param name="val"></param>
		/// <returns>Wind in configured units</returns>
		public static double ConvertKmtoUserUnits(double val)
		{
			return cumulus.Units.Wind switch
			{
				// m/s
				0 or 2 => val,
				// mph
				1 => val * 0.621371,
				// knots
				3 => val * 0.539957,
				_ => val,
			};
		}

		/// <summary>
		///  Converts windrun supplied in user units to km
		/// </summary>
		/// <param name="value">Windrun in configured units</param>
		/// <returns>Wind in km</returns>
		public virtual double ConvertWindRunToKm(double value)
		{
			return cumulus.Units.Wind switch
			{
				// m/s
				0 or 2 => value,
				// mph
				1 => value / 0.621371192,
				// knots
				3 => value / 0.539956803,
				_ => 0,
			};
		}

		public static double? ConvertUserWindToKPH(double? wind) // input is in Units.Wind units, convert to km/h
		{
			if (wind == null)
				return null;

			return cumulus.Units.Wind switch
			{
				// m/s
				0 => wind * 3.6,
				// mph
				1 => wind * 1.609344,
				// kph
				2 => wind,
				// knots
				3 => wind * 1.852,
				_ => wind,
			};
		}

		/// <summary>
		/// Converts rain in mm to units in use
		/// </summary>
		/// <param name="value">Rain in mm</param>
		/// <returns>Rain in configured units</returns>
		public virtual double ConvertRainMMToUser(double value)
		{
			return cumulus.Units.Rain == 1 ? value * 0.0393700787 : value;
		}

		/// <summary>
		/// Converts rain in inches to units in use
		/// </summary>
		/// <param name="value">Rain in mm</param>
		/// <returns>Rain in configured units</returns>
		public virtual double ConvertRainINToUser(double value)
		{
			return cumulus.Units.Rain == 1 ? value : value * 25.4;
		}

		/// <summary>
		/// Converts rain in units in use to mm
		/// </summary>
		/// <param name="value">Rain in configured units</param>
		/// <returns>Rain in mm</returns>
		public virtual double? ConvertUserRainToMM(double? value)
		{
			if (value == null)
				return null;

			return cumulus.Units.Rain == 1 ? value / 0.0393700787 : value;
		}

		/// <summary>
		/// Convert pressure in mb to units in use
		/// </summary>
		/// <param name="value">pressure in mb</param>
		/// <returns>pressure in configured units</returns>
		public static double? ConvertPressMBToUser(double? value)
		{
			if (value == null)
				return null;

			return cumulus.Units.Press == 2 ? value * 0.0295333727 : value;
		}

		/// <summary>
		/// Convert pressure in inHg to units in use
		/// </summary>
		/// <param name="value">pressure in mb</param>
		/// <returns>pressure in configured units</returns>
		public static double? ConvertPressINHGToUser(double? value)
		{
			if (value == null)
				return null;

			return cumulus.Units.Press == 2 ? value : value * 33.8638866667;
		}

		/// <summary>
		/// Convert pressure in units in use to mb
		/// </summary>
		/// <param name="value">pressure in configured units</param>
		/// <returns>pressure in mb</returns>
		public static double? ConvertUserPressToMB(double? value)
		{
			if (value == null)
				return null;

			return cumulus.Units.Press == 2 ? value / 0.0295333727 : value;
		}

		/// <summary>
		/// Convert pressure in units in use to inHg
		/// </summary>
		/// <param name="value">pressure in configured units</param>
		/// <returns>pressure in mb</returns>
		public static double? ConvertUserPressToIN(double? value)
		{
			if (value == null)
				return null;

			return cumulus.Units.Press == 2 ? value : value * 0.0295333727;
		}

		public static string CompassPoint(int? bearing)
		{
			return (bearing ?? 0) == 0 ? "-" : cumulus.compassp[(((bearing.Value * 100) + 1125) % 36000) / 2250];
		}

		public void StartLoop()
		{
			try
			{
				t = new Thread(Start) { IsBackground = true };
				t.Start();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "An error occurred during the station start-up");
			}
		}

		public virtual void getAndProcessHistoryData()
		{
		}

		public virtual void startReadingHistoryData()
		{
		}

		public virtual void DoStartup()
		{
		}

		/// <summary>
		/// Calculates average bearing for last 10 minutes
		/// </summary>
		/// <returns></returns>
		public int CalcAverageBearing()
		{
			double totalwindX = Last10MinWindList.Sum(o => o.gustX);
			double totalwindY = Last10MinWindList.Sum(o => o.gustY);

			if (totalwindX == 0)
			{
				return 0;
			}

			int avgbear = calcavgbear(totalwindX, totalwindY);

			if (avgbear == 0)
			{
				avgbear = 360;
			}

			return avgbear;
		}

		private static int calcavgbear(double x, double y)
		{
			var avg = 90 - (int)(Trig.RadToDeg(Math.Atan2(y, x)));
			if (avg < 0)
			{
				avg = 360 + avg;
			}

			return avg;
		}

		public void AddRecentDataWithAq(DateTime timestamp, double? windAverage, double? recentMaxGust, double? windLatest, int? bearing, int? avgBearing, double? outsidetemp,
			double? windChill, double? dewpoint, double? heatIndex, int? humidity, double? pressure, double? rainToday, int? solarRad, double? uv, double rainCounter, double? feelslike, double? humidex,
			double? appTemp, double? insideTemp, int? insideHum, int? solarMax, double? rainrate)
		{
			double? pm2p5 = null;
			double? pm10 = null;
			// Check for Air Quality readings
			switch (cumulus.StationOptions.PrimaryAqSensor)
			{
				case (int)Cumulus.PrimaryAqSensor.AirLinkOutdoor:
					if (cumulus.airLinkDataOut != null)
					{
						pm2p5 = cumulus.airLinkDataOut.pm2p5;
						pm10 = cumulus.airLinkDataOut.pm10;
					}
					break;
				case (int)Cumulus.PrimaryAqSensor.AirLinkIndoor:
					if (cumulus.airLinkDataIn != null)
					{
						pm2p5 = cumulus.airLinkDataIn.pm2p5;
						pm10 = cumulus.airLinkDataIn.pm10;
					}
					break;
				case (int)Cumulus.PrimaryAqSensor.Ecowitt1:
					pm2p5 = AirQuality[1];
					break;
				case (int)Cumulus.PrimaryAqSensor.Ecowitt2:
					pm2p5 = AirQuality[2];
					break;
				case (int)Cumulus.PrimaryAqSensor.Ecowitt3:
					pm2p5 = AirQuality[3];
					break;
				case (int)Cumulus.PrimaryAqSensor.Ecowitt4:
					pm2p5 = AirQuality[4];
					break;
				case (int)Cumulus.PrimaryAqSensor.EcowittCO2:
					pm2p5 = CO2_pm2p5;
					pm10 = CO2_pm10;
					break;

				default: // Not enabled, use invalid values
					break;
			}

			AddRecentDataEntry(timestamp, windAverage, recentMaxGust, windLatest, bearing, avgBearing, outsidetemp, windChill, dewpoint, heatIndex, humidity, pressure, rainToday, solarRad, uv, rainCounter, feelslike, humidex, appTemp, insideTemp, insideHum, solarMax, rainrate, pm2p5, pm10);
		}


		public void UpdateRecentDataAqEntry(DateTime ts, double? pm2p5, double? pm10)
		{
			try
			{
				Database.Execute("update RecentData set Pm2p5=?, Pm10=? where Timestamp=?", pm2p5, pm10, ts.ToUniversalTime());
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "UpdateGraphDataAqEntry: Exception caught");
			}
		}


		/// <summary>
		/// Adds a new entry to the list of wind readings from the last 10 minutes
		/// </summary>
		/// <param name="ts"></param>
		public void AddLast10MinWindEntry(DateTime ts, double windgust, double windspeed, double Xvec, double Yvec)
		{
			Last10MinWind last10minwind = new Last10MinWind(ts, windgust, windspeed, Xvec, Yvec);
			Last10MinWindList.Add(last10minwind);
		}

		/*
		public double getStartOfDayRainCounter(DateTime timestamp)
		{
			// TODO:
			return -1;
		}
		*/


		/// <summary>
		/// Removes entries from Last10MinWindList older than ts - 10 minutes
		/// </summary>
		/// <param name="?"></param>
		/// <returns></returns>
		public void RemoveOld10MinWindData(DateTime ts)
		{
			DateTime tenminutesago = ts.AddMinutes(-10);

			if (Last10MinWindList.Count > 0)
			{
				// there are entries to consider
				while ((Last10MinWindList.Count > 0) && (Last10MinWindList.First().timestamp < tenminutesago))
				{
					// the oldest entry is older than 10 mins ago, delete it
					Last10MinWindList.RemoveAt(0);
				}
			}
		}

		public void DoTrendValues(DateTime ts, bool rollover = false)
		{
			List<RecentData> retVals;
			double trendval;
			var recTs = ts;

				// if this is the special case of rollover processing, we want the High today record to on the previous day at 23:59 or 08:59
				if (rollover)
				{
					recTs = recTs.AddMinutes(-1);
				}

			// Do 3 hour trends
			try
			{
				retVals = Database.Query<RecentData>("select OutsideTemp, Pressure from RecentData where Timestamp >=? order by Timestamp limit 1", ts.AddHours(-3).ToUniversalTime());

				if (retVals.Count != 1)
				{
					temptrendval = 0;
					presstrendval = 0;
				}
				else
				{
					// calculate and display the temp trend
					if (TempReadyToPlot && Temperature.HasValue && retVals[0].OutsideTemp.HasValue)
					{
						temptrendval = (Temperature.Value - retVals[0].OutsideTemp.Value) / 3.0F;
						cumulus.TempChangeAlarm.CheckAlarm(temptrendval);
					}

					// calculate and display the pressure trend
					if (PressReadyToPlot && Pressure.HasValue && retVals[0].Pressure.HasValue)
					{
						presstrendval = (Pressure.Value - retVals[0].Pressure.Value) / 3.0F;
						cumulus.PressChangeAlarm.CheckAlarm(presstrendval);
					}
				}
			}
			catch
			{
				temptrendval = 0;
				presstrendval = 0;
			}

			// Do 1 hour trends
			try
			{
				retVals = Database.Query<RecentData>("select OutsideTemp, raincounter from RecentData where Timestamp >=? order by Timestamp limit 1", ts.AddHours(-1).ToUniversalTime());

				if (retVals.Count != 1)
				{
					TempChangeLastHour = 0;
					RainLastHour = 0;
				}
				else
				{
					// Calculate Temperature change in the last hour
					if (Temperature.HasValue && retVals[0].OutsideTemp.HasValue)
					{
						TempChangeLastHour = Temperature.Value - retVals[0].OutsideTemp.Value;
					}

					// calculate and display rainfall in last hour
					if (retVals[0].raincounter.HasValue)
					{
						// normal case
						trendval = Raincounter - retVals[0].raincounter.Value;

						// Round value as some values may have been read from log file and already rounded
						trendval = Math.Round(trendval, cumulus.RainDPlaces);

						var tempRainLastHour = trendval * cumulus.Calib.Rain.Mult;

						if (ConvertUserRainToMM(tempRainLastHour) > cumulus.Spike.MaxHourlyRain)
						{
							// ignore
							cumulus.LogSpikeRemoval("Max hourly rainfall spike value exceed");
							lastSpikeRemoval = DateTime.Now;
							cumulus.SpikeAlarm.LastError = $"Max hourly rainfall greater than spike value - Value={tempRainLastHour:F1}";
							cumulus.SpikeAlarm.Triggered = true;
						}
						else
						{
							RainLastHour = tempRainLastHour;

							if (RainLastHour > AllTime.HourlyRain.Val)
								SetAlltime(AllTime.HourlyRain, RainLastHour, recTs);

							CheckMonthlyAlltime("HourlyRain", RainLastHour, true, recTs);

							if (RainLastHour > HiLoToday.HighHourlyRain)
							{
								HiLoToday.HighHourlyRain = RainLastHour;
								HiLoToday.HighHourlyRainTime = recTs;
								WriteTodayFile(ts, false);
							}

							if (RainLastHour > ThisMonth.HourlyRain.Val)
							{
								ThisMonth.HourlyRain.Val = RainLastHour;
								ThisMonth.HourlyRain.Ts = recTs;
								WriteMonthIniFile();
							}

							if (RainLastHour > ThisYear.HourlyRain.Val)
							{
								ThisYear.HourlyRain.Val = RainLastHour;
								ThisYear.HourlyRain.Ts = recTs;
								WriteYearIniFile();
							}
						}
					}

				}
			}
			catch
			{
				TempChangeLastHour = 0;
				RainLastHour = 0;
			}

			if (calculaterainrate)
			{
				// Station doesn't supply rain rate, calculate one based on rain in last 5 minutes
				try
				{
					DateTime fiveminutesago = ts.AddSeconds(-330);

					retVals = Database.Query<RecentData>("select raincounter from RecentData where Timestamp >= ? order by Timestamp limit 1", ts.AddMinutes(-5.5).ToUniversalTime());

					if (retVals.Count != 1 || !retVals[0].raincounter.HasValue || Raincounter < retVals[0].raincounter)
					{
						RainRate = 0;
					}
					else
					{
						var raindiff = Math.Round(Raincounter - retVals[0].raincounter.Value, cumulus.RainDPlaces);

						var timediffhours = 1.0 / 12.0;


						var tempRainRate = Math.Round((double)(raindiff / timediffhours) * cumulus.Calib.Rain.Mult, cumulus.RainDPlaces);

						if (tempRainRate < 0)
						{
							tempRainRate = 0;
						}

						if (ConvertUserRainToMM(tempRainRate) > cumulus.Spike.MaxRainRate)
						{
							// ignore
							cumulus.LogSpikeRemoval("Max rainfall rate spike value exceed");
							lastSpikeRemoval = DateTime.Now;
							cumulus.SpikeAlarm.LastError = $"Max rainfall rate greater than spike value - Value={tempRainRate:F1}";
							cumulus.SpikeAlarm.Triggered = true;

						}
						else
						{
							RainRate = tempRainRate;

							if (RainRate > AllTime.HighRainRate.Val)
								SetAlltime(AllTime.HighRainRate, RainRate.Value, ts);

							CheckMonthlyAlltime("HighRainRate", RainRate, true, ts);

							cumulus.HighRainRateAlarm.CheckAlarm(RainRate.Value);

							if (RainRate > (HiLoToday.HighRainRate ?? Cumulus.DefaultHiVal))
							{
								HiLoToday.HighRainRate = RainRate;
								HiLoToday.HighRainRateTime = recTs;
								WriteTodayFile(ts, false);
							}

							if (RainRate > ThisMonth.HighRainRate.Val)
							{
								ThisMonth.HighRainRate.Val = RainRate.Value;
								ThisMonth.HighRainRate.Ts = recTs;
								WriteMonthIniFile();
							}

							if (RainRate > ThisYear.HighRainRate.Val)
							{
								ThisYear.HighRainRate.Val = RainRate.Value;
								ThisYear.HighRainRate.Ts = recTs;
								WriteYearIniFile();
							}
						}
					}
				}
				catch
				{
					RainRate = 0;
				}

			}

			// calculate and display rainfall in last 24 hour
			try
			{
				retVals = Database.Query<RecentData>("select raincounter from RecentData where Timestamp >= ? order by Timestamp limit 1", ts.AddDays(-1).ToUniversalTime());

				if (retVals.Count != 1 || !retVals[0].raincounter.HasValue || Raincounter < retVals[0].raincounter)
				{
					RainLast24Hour = 0;
				}
				else
				{
					trendval = Math.Round(Raincounter - retVals[0].raincounter.Value, cumulus.RainDPlaces);

					// Round value as some values may have been read from log file and already rounded
					trendval = Math.Round(trendval, cumulus.RainDPlaces);

					if (trendval < 0)
					{
						trendval = 0;
					}

					RainLast24Hour = trendval * cumulus.Calib.Rain.Mult;

					if (RainLast24Hour > AllTime.HighRain24Hours.Val)
					{
						SetAlltime(AllTime.HighRain24Hours, RainLast24Hour, recTs);
					}

					CheckMonthlyAlltime("HighRain24Hours", RainLast24Hour, true, recTs);

					if (RainLast24Hour > (HiLoToday.HighRain24h ?? Cumulus.DefaultHiVal))
					{
						HiLoToday.HighRain24h = RainLast24Hour;
						HiLoToday.HighRain24hTime = recTs;
						WriteTodayFile(ts, false);
					}

					if (RainLast24Hour > ThisMonth.HighRain24Hours.Val)
					{
						ThisMonth.HighRain24Hours.Val = RainLast24Hour;
						ThisMonth.HighRain24Hours.Ts = recTs;
						WriteMonthIniFile();
					}

					if (RainLast24Hour > ThisYear.HighRain24Hours.Val)
					{
						ThisYear.HighRain24Hours.Val = RainLast24Hour;
						ThisYear.HighRain24Hours.Ts = recTs;
						WriteYearIniFile();
					}
				}
			}
			catch
			{
				RainLast24Hour = 0;
			}
		}

		public void CalculateDominantWindBearing(int? averageBearing, double? averageSpeed, int minutes)
		{
			if (averageBearing == null || averageSpeed == null)
				return;

			DominantWindBearingX += (minutes * averageSpeed.Value * Math.Sin(Trig.DegToRad(averageBearing.Value)));
			DominantWindBearingY += (minutes * averageSpeed.Value * Math.Cos(Trig.DegToRad(averageBearing.Value)));
			DominantWindBearingMinutes += minutes;

			if (DominantWindBearingX == 0)
			{
				DominantWindBearing = 0;
			}
			else
			{
				try
				{
					DominantWindBearing = calcavgbear(DominantWindBearingX, DominantWindBearingY);
					if (DominantWindBearing == 0)
					{
						DominantWindBearing = 360;
					}
				}
				catch
				{
					Cumulus.LogMessage("Error in dominant wind direction calculation");
				}
			}

			/*if (DominantWindBearingX < 0)
			{
				DominantWindBearing = 270 - DominantWindBearing;
			}
			else
			{
				DominantWindBearing = 90 - DominantWindBearing;
			}*/
		}

		public void DoDayResetIfNeeded()
		{
			int hourInc = cumulus.GetHourInc();
			var now = DateTime.Now;

			if (cumulus.LastUpdateTime.AddHours(hourInc).Date != now.AddHours(hourInc).Date)
			{
				Cumulus.LogMessage("Day reset required");
				DayReset(now);
			}

			if (cumulus.LastUpdateTime.Date != now.Date)
			{
				ResetMidnightRain(now);
				ResetSunshineHours(now);
				ResetMidnightTemperatures(now);
			}

			if (cumulus.LastUpdateTime.AddHours(hourInc).Date != now.AddHours(hourInc).Date)
			{
				// Reset the last update time to now so we do not rollover again if MX is restarted before any data comes in
				cumulus.LastUpdateTime = now;
				WriteTodayFile(cumulus.LastUpdateTime, false);
			}
		}

		public int DominantWindBearing { get; set; }

		public int DominantWindBearingMinutes { get; set; }

		public double DominantWindBearingY { get; set; }

		public double DominantWindBearingX { get; set; }

		public double YesterdayWindRun { get; set; }
		public double AnnualETTotal { get; set; }
		public double StartofdayET { get; set; }

		public int ConsecutiveRainDays { get; set; }
		public int ConsecutiveDryDays { get; set; }
		public DateTime FOSensorClockTime { get; set; }
		public DateTime FOStationClockTime { get; set; }
		public DateTime FOSolarClockTime { get; set; }
		public double YestAvgTemp { get; set; }
		public double? AltimeterPressure { get; set; } = null;
		public int YestDominantWindBearing { get; set; }
		public double RainLast24Hour { get; set; }
		public string ConBatText { get; set; }
		public string ConSupplyVoltageText { get; set; }
		public string TxBatText { get; set; }

		public double YestChillHours { get; set; }
		public double YestHeatingDegreeDays { get; set; }
		public double YestCoolingDegreeDays { get; set; }
		public double TempChangeLastHour { get; set; }
		public double? WetBulb { get; set; } = null;
		public int? CloudBase { get; set; } = null;
		public double StormRain { get; set; }
		public DateTime StartOfStorm { get; set; }
		public bool SensorContactLost { get; set; }
		public bool DataStopped { get; set; }
		public DateTime DataStoppedTime { get; set; }
		public bool IsRaining { get; set; }

		public void LoadLastHoursFromDataLogs(DateTime ts)
		{
			Cumulus.LogMessage("Loading last N hour data from data logs: " + ts);
			LoadRecentFromDataLogs(ts);
			LoadRecentAqFromDataLogs(ts);
			LoadLast3HourData(ts);
			LoadRecentWindRose();
		}

		private void LoadRecentFromDataLogs(DateTime ts)
		{
			// Recent data goes back a week
			var datefrom = ts.AddDays(-cumulus.RecentDataDays);
			var dateto = ts;
			var entrydate = datefrom;
			var filedate = datefrom;
			string logFile = cumulus.GetLogFileName(filedate);
			bool finished = false;
			int numadded = 0;

			var rowsToAdd = new List<RecentData>();

			Cumulus.LogMessage($"LoadRecent: Attempting to load {cumulus.RecentDataDays} days of entries to recent data list");

			// try and find the last entry in the database
			try
			{
				var start = Database.ExecuteScalar<DateTime>("select MAX(Timestamp) from RecentData");
				if (datefrom < start)
					datefrom = start;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "LoadRecent: Error querying database for latest record");
			}


			while (!finished)
			{
				if (File.Exists(logFile))
				{
					int linenum = 0;
					int errorCount = 0;
					rowsToAdd.Clear();

					try
					{
						var lines = File.ReadAllLines(logFile);

						foreach (var line in lines)
						{
							try
							{
								// process each record in the file
								linenum++;

								//var rec = ParseLogFileRec(line, false);
								var fields = line.Split(',');
								var rec = new IntervalData();
								rec.FromString(fields);

								if (rec.Timestamp >= datefrom && entrydate <= dateto)
								{
									rowsToAdd.Add(new RecentData()
									{
										Timestamp = rec.Timestamp,
										DewPoint = rec.DewPoint,
										HeatIndex = rec.HeatIndex,
										Humidity = rec.Humidity,
										OutsideTemp = rec.Temp,
										Pressure = rec.Pressure,
										RainToday = rec.RainToday,
										SolarRad = rec.SolarRad,
										UV = rec.UV,
										WindAvgDir = rec.WindAvgDir,
										WindGust = rec.WindGust10m,
										WindLatest = rec.WindLatest,
										WindChill = rec.WindChill,
										WindDir = rec.WindDir,
										WindSpeed = rec.WindAvg,
										raincounter = rec.RainCounter,
										FeelsLike = rec.FeelsLike,
										Humidex = rec.Humidex,
										AppTemp = rec.Apparent,
										IndoorTemp = rec.InsideTemp,
										IndoorHumidity = rec.InsideHumidity,
										SolarMax = rec.SolarMax,
										RainRate = rec.RainRate,
										Pm2p5 = null,
										Pm10 = null
									});
									++numadded;
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, $"LoadRecent: Error at line {linenum} of {logFile}");
								Cumulus.LogMessage("Please edit the file to correct the error");
								errorCount++;
								if (errorCount >= 10)
								{
									Cumulus.LogMessage($"LoadRecent: Too many errors reading {logFile} - aborting load of graph data");
								}
							}
						}
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"LoadRecent: Error at line {linenum} of {logFile}");
						Cumulus.LogMessage("Please edit the file to correct the error");
					}

					try
					{
						if (rowsToAdd.Count > 0)
						{
							//RecentDataDb.InsertAllOrIgnore(rowsToAdd);
							//await RecentDataDb.InsertAllAsync(rowsToAdd);
							Database.InsertAll(rowsToAdd, "OR REPLACE");
						}

					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, "LoadRecent: Error inserting recent data into database");
					}

				}

				if (entrydate >= dateto || filedate > dateto.AddMonths(1))
				{
					finished = true;
				}
				else
				{
					filedate = filedate.AddMonths(1);
					logFile = cumulus.GetLogFileName(filedate);
				}
			}
			Cumulus.LogMessage($"LoadRecent: Loaded {numadded} new entries to recent database");
		}

		private void LoadRecentAqFromDataLogs(DateTime ts)
		{
			var datefrom = ts.AddDays(-cumulus.RecentDataDays);
			var dateto = ts;
			var entrydate = datefrom;
			var filedate = datefrom;
			string logFile;
			bool finished = false;
			int updatedCount = 0;

			// try and find the first entry in the database that has a "blank" AQ entry (PM2.5 or PM10 = -1)
			try
			{
				var start = Database.ExecuteScalar<DateTime>("select Timestamp from RecentData where Pm2p5=-1 or Pm10=-1 order by Timestamp limit 1");
				if (start == DateTime.MinValue)
					return;

				if (datefrom < start)
					datefrom = start;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "LoadRecentAqFromDataLogs: Error querying database for oldest record without AQ data");
			}

			if (cumulus.StationOptions.PrimaryAqSensor < 0) return;

			Cumulus.LogMessage($"LoadRecentAqFromDataLogs: Attempting to load {cumulus.RecentDataDays} days of entries to Air Quality recent data");

			if (cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkOutdoor
				|| cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkIndoor)
			{
				logFile = cumulus.GetAirLinkLogFileName(filedate);
			}
			else if ((cumulus.StationOptions.PrimaryAqSensor >= (int)Cumulus.PrimaryAqSensor.Ecowitt1 && cumulus.StationOptions.PrimaryAqSensor <= (int)Cumulus.PrimaryAqSensor.Ecowitt4) ||
					cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.EcowittCO2) // Ecowitt
			{
				logFile = cumulus.GetExtraLogFileName(filedate);
			}
			else
			{
				Cumulus.LogMessage($"LoadRecentAqFromDataLogs: Error - The primary AQ sensor is not set to a valid value, currently={cumulus.StationOptions.PrimaryAqSensor}");
				return;
			}

			while (!finished)
			{
				if (File.Exists(logFile))
				{
					int linenum = 0;
					int errorCount = 0;

					try
					{
						//RecentDataDb.BeginTransaction();
						var lines = File.ReadAllLines(logFile);

						foreach (var line in lines)
						{
							try
							{
								// process each record in the file
								linenum++;
								var st = new List<string>(line.Split(','));
								entrydate = Utils.FromUnixTime(int.Parse(st[1]));

								if (entrydate >= datefrom && entrydate <= dateto)
								{
									// entry is from required period
									double pm2p5, pm10;
									if (cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkIndoor)
									{
										// AirLink Indoor
										pm2p5 = Convert.ToDouble(st[5], invNum);
										pm10 = Convert.ToDouble(st[10], invNum);
									}
									else if (cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkOutdoor)
									{
										// AirLink Outdoor
										pm2p5 = Convert.ToDouble(st[32], invNum);
										pm10 = Convert.ToDouble(st[37], invNum);
									}
									else if (cumulus.StationOptions.PrimaryAqSensor >= (int)Cumulus.PrimaryAqSensor.Ecowitt1 && cumulus.StationOptions.PrimaryAqSensor <= (int)Cumulus.PrimaryAqSensor.Ecowitt4)
									{
										// Ecowitt sensor 1-4 - fields 68 -> 71
										pm2p5 = Convert.ToDouble(st[67 + cumulus.StationOptions.PrimaryAqSensor], invNum);
										pm10 = -1;
									}
									else
									{
										// Ecowitt CO2 sensor
										pm2p5 = Convert.ToDouble(st[86], invNum);
										pm10 = Convert.ToDouble(st[88], invNum);
									}

									//UpdateGraphDataAqEntry(entrydate, pm2p5, pm10);
									UpdateRecentDataAqEntry(entrydate, pm2p5, pm10);
									updatedCount++;
								}
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, $"LoadRecentAqFromDataLogs: Error at line {linenum} of {logFile}");
								Cumulus.LogMessage("Please edit the file to correct the error");
								errorCount++;
								if (errorCount >= 20)
								{
									Cumulus.LogMessage($"LoadRecentAqFromDataLogs: Too many errors reading {logFile} - aborting load of graph data");
								}
							}
						}

						//RecentDataDb.Commit();
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"LoadRecentAqFromDataLogs: Error at line {linenum} of {logFile}");
						Cumulus.LogMessage("Please edit the file to correct the error");
						//RecentDataDb.Rollback();
					}
				}

				if (entrydate >= dateto || filedate > dateto.AddMonths(1))
				{
					finished = true;
				}
				else
				{
					filedate = filedate.AddMonths(1);
					if (cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkOutdoor
						|| cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.AirLinkIndoor) // AirLink
					{
						logFile = cumulus.GetAirLinkLogFileName(filedate);
					}
					else if ((cumulus.StationOptions.PrimaryAqSensor >= (int)Cumulus.PrimaryAqSensor.Ecowitt1
						&& cumulus.StationOptions.PrimaryAqSensor <= (int)Cumulus.PrimaryAqSensor.Ecowitt4)
						|| cumulus.StationOptions.PrimaryAqSensor == (int)Cumulus.PrimaryAqSensor.EcowittCO2) // Ecowitt
					{
						logFile = cumulus.GetExtraLogFileName(filedate);
					}
				}
			}
			Cumulus.LogMessage($"LoadRecentAqFromDataLogs: Loaded {updatedCount} new entries to recent database");
		}

		private void LoadRecentWindRose()
		{
			// We can now just query the recent data database as it has been populated from the logs
			var datefrom = DateTime.Now.AddHours(-24);

			var result = Database.Query<RecentData>("select WindGust, WindDir from RecentData where Timestamp >= ? and WindGust is not null and WindDir is not null order by Timestamp", datefrom.ToUniversalTime());

			foreach (var rec in result)
			{
				windspeeds[nextwindvalue] = rec.WindGust.Value;
				windbears[nextwindvalue] = rec.WindDir.Value;
				nextwindvalue = (nextwindvalue + 1) % MaxWindRecent;
				if (numwindvalues < maxwindvalues)
				{
					numwindvalues++;
				}
			}
		}

		private void LoadLast3HourData(DateTime ts)
		{
			var datefrom = ts.AddHours(-3);
			var dateto = ts;

			Cumulus.LogMessage($"LoadLast3Hour: Attempting to load 3 hour data list");

			var result = Database.Query<RecentData>("select * from RecentData where Timestamp >= ? and Timestamp <= ? and WindGust is not null and WindSpeed is not null and WindDir is not null order by Timestamp", datefrom.ToUniversalTime(), dateto.ToUniversalTime());

			foreach (var rec in result)
			{
				try
				{
					if (rec.WindGust.HasValue && rec.WindSpeed.HasValue)
					{
						WindRecent[nextwind].Gust = rec.WindGust.Value;
						WindRecent[nextwind].Speed = rec.WindSpeed.Value;
						WindRecent[nextwind].Timestamp = rec.Timestamp;
						nextwind = (nextwind + 1) % MaxWindRecent;
					}
					if (rec.WindGust.HasValue && rec.WindDir.HasValue)
					{
						WindVec[nextwindvec].X = rec.WindGust.Value * Math.Sin(Trig.DegToRad(rec.WindDir.Value));
						WindVec[nextwindvec].Y = rec.WindGust.Value * Math.Cos(Trig.DegToRad(rec.WindDir.Value));
						WindVec[nextwindvec].Timestamp = rec.Timestamp;
						WindVec[nextwindvec].Bearing = Bearing ?? 0; // savedBearing;
						nextwindvec = (nextwindvec + 1) % MaxWindRecent;
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "LoadLast3Hour: Error loading data from database");
				}
			}
			Cumulus.LogMessage($"LoadLast3Hour: Loaded {result.Count} entries to last 3 hour data list");
		}

		public string LoadDayFileToDb()
		{
			int addedEntries = 0;
			DateTime start;

			var rowsToAdd = new List<DayData>();

			Cumulus.LogMessage($"LoadDayFileToDb: Attempting to load the daily data");

			var watch = Stopwatch.StartNew();

			// try and find the first entry in the database that has a "blank" AQ entry (PM2.5 or PM10 = -1)
			try
			{
				start = Database.ExecuteScalar<DateTime>("select MAX(Timestamp) from DayData");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "LoadDayFileToDb: Error querying database for latest record");
				start = DateTime.MinValue;
			}


			if (File.Exists(cumulus.DayFileName))
			{
				int linenum = 0;
				int errorCount = 0;

				try
				{
					using var sr = new StreamReader(cumulus.DayFileName);
					var doMore = true;
					do
					{
						try
						{
							// process each record in the file

							linenum++;
							string Line = sr.ReadLine();

							var newRec = new DayData();
							var ok = newRec.ParseDayFileRecv4(Line);

							if (ok && newRec.Timestamp > start)
							{
								rowsToAdd.Add(newRec);
								addedEntries++;
							}
							else
							{
								doMore = false;
							}


						}
						catch (Exception ex)
						{
							cumulus.LogExceptionMessage(ex, $"LoadDayFileToDb: Error at line {linenum} of {cumulus.DayFileName}");
							Cumulus.LogMessage("Please edit the file to correct the error");
							errorCount++;
							if (errorCount >= 20)
							{
								Cumulus.LogMessage($"LoadDayFileToDb: Too many errors reading {cumulus.DayFileName} - aborting load of daily data");
							}
						}
					} while (!(sr.EndOfStream || errorCount >= 20) && doMore);
					sr.Close();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, $"LoadDayFileToDb: Error at line {linenum} of {cumulus.DayFileName}");
					Cumulus.LogMessage("Please edit the file to correct the error");
				}

				// Anything to add to the data base?
				try
				{
					Database.InsertAll(rowsToAdd);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "LoadDayFileToDb: Error inserting daily data into database");
				}


				watch.Stop();
				cumulus.LogDebugMessage($"LoadDayFileToDb: Dayfile load = {watch.ElapsedMilliseconds} ms");
				var msg = $"LoadDayFileToDb: Loaded {addedEntries} entries to the daily data table";
				Cumulus.LogMessage(msg);
				return msg;

			}
			else
			{
				var msg = "LoadDayFileToDb: No Dayfile found - No entries added to the daily data table";
				Cumulus.LogMessage(msg);
				return msg;
			}
		}


		public void LoadLogFilesToDb()
		{

			DateTime lastLogDate;

			Cumulus.LogMessage("LoadLogFilesToDb: Starting Process");

			try
			{
				// get the last date time from the database - if any
				lastLogDate = Database.ExecuteScalar<DateTime>("select max(Timestamp) from IntervalData");
				Cumulus.LogMessage($"LoadLogFilesToDb: Last data logged in database = {lastLogDate.ToString("yyyy-MM-dd HH:mm", invDate)}");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "LoadLogFilesToDb: Error querying the database for the last logged data time");
				return;
			}


			if (lastLogDate == DateTime.MinValue)
				lastLogDate = cumulus.RecordsBeganDate;

			// Check the last data time against the time now and see it is within a logging period window
			if (lastLogDate.AddMinutes(cumulus.logints[cumulus.DataLogInterval]) > cumulus.LastUpdateTime)
			{
				// the database is up to date - nothing to do here
				Cumulus.LogMessage("LoadLogFilesToDb: The database is up to date");
				return;
			}


			var finished = false;
			var dataToLoad = new List<IntervalData>();
			var fileDate = lastLogDate;
			var logFile = cumulus.GetLogFileName(fileDate);
			int totalInserted = 0;

			Console.WriteLine();

			while (!finished)
			{
				if (File.Exists(logFile))
				{

					cumulus.LogDebugMessage($"LoadLogFilesToDb: Processing log file - {logFile}");

					Console.Write($"\rLoading log file for {fileDate:yyyy-MM} to the database");

					var linenum = 0;
					try
					{
						var logfile = File.ReadAllLines(logFile);

						foreach (var line in logfile)
						{
							// process each record in the file
							linenum++;
							var rec = new IntervalData();
							rec.FromString(line.Split(','));
							if (rec.Timestamp >= lastLogDate)
								dataToLoad.Add(rec);
						}

						// load the data a month at a time into the database so we do not hold it all in memory
						// now load the data into the database
						if (dataToLoad.Count > 0)
						{
							try
							{
								cumulus.LogDebugMessage($"LoadLogFilesToDb: Loading {dataToLoad.Count} rows into the database");
								var inserted = Database.InsertAll(dataToLoad, "OR IGNORE");
								totalInserted += inserted;
								cumulus.LogDebugMessage($"LoadLogFilesToDb: Inserted {inserted} rows into the database");
							}
							catch (Exception ex)
							{
								cumulus.LogExceptionMessage(ex, "LoadLogFilesToDb: Error inserting the data into the database");
							}
						}

						// clear the db List
						dataToLoad.Clear();
					}
					catch (Exception ex)
					{
						cumulus.LogExceptionMessage(ex, $"LoadLogFilesToDb: Error at line {linenum} of {logFile}");
						Cumulus.LogMessage("Please edit the file to correct the error");
					}
				}
				else
				{
					cumulus.LogDebugMessage($"LoadLogFilesToDb: Log file  not found - {logFile}");
				}
				if (fileDate >= DateTime.Now)
				{
					finished = true;
					cumulus.LogDebugMessage("LoadLogFilesToDb: Finished processing the log files");
				}
				else
				{
					cumulus.LogDebugMessage($"LoadLogFilesToDb: Finished processing log file - {logFile}");
					fileDate = fileDate.AddMonths(1);
					logFile = cumulus.GetLogFileName(fileDate);
				}
			}
			Console.WriteLine($"\rCompleted loading the log files to the database. {totalInserted} rows added\n");
		}


		internal void UpdateStatusPanel(DateTime timestamp)
		{
			LastDataReadTimestamp = timestamp;
			_ = sendWebSocketData();
		}


		internal static void UpdateMQTT()
		{
			if (cumulus.MQTT.EnableDataUpdate)
			{
				MqttPublisher.UpdateMQTTfeed("DataUpdate");
			}
		}

		/// <summary>
		/// Returns a plus sign if the supplied number is greater than zero, otherwise empty string
		/// </summary>
		/// <param name="num">The number to be tested</param>
		/// <returns>Plus sign or empty</returns>
		/*
		private string PlusSign(double num)
		{
			return num > 0 ? "+" : "";
		}
		*/

		public void DoET(double value, DateTime timestamp)
		{
			// Value is annual total

			if (noET)
			{
				// Start of day ET value not yet set
				Cumulus.LogMessage("*** First ET reading. Set startofdayET to total: " + value);
				StartofdayET = value;
				noET = false;
			}

			//if ((value == 0) && (StartofdayET > 0))
			if (Math.Round(value, 3) < Math.Round(StartofdayET, 3)) // change b3046
			{
				// ET reset
				Cumulus.LogMessage(String.Format("*** ET Reset *** AnnualET: {0:0.000}, StartofdayET: {1:0.000}, StationET: {2:0.000}, CurrentET: {3:0.000}", AnnualETTotal, StartofdayET, value, ET));
				AnnualETTotal = value; // add b3046
									   // set the start of day figure so it reflects the ET
									   // so far today
				StartofdayET = AnnualETTotal - ET;
				WriteTodayFile(timestamp, false);
				Cumulus.LogMessage(String.Format("New ET values. AnnualET: {0:0.000}, StartofdayET: {1:0.000}, StationET: {2:0.000}, CurrentET: {3:0.000}", AnnualETTotal, StartofdayET, value, ET));
			}
			else
			{
				AnnualETTotal = value;
			}

			ET = AnnualETTotal - StartofdayET;

			HaveReadData = true;
		}

		public void DoSoilMoisture(int? value, int index)
		{
			if (index < SoilMoisture.Length)
			{
				SoilMoisture[index] = value;
				dataValuesUpdated.SoilMoisture[index] = value.HasValue;
			}
		}

		public void DoSoilTemp(double? value, int index)
		{
			if (index < SoilTemp.Length)
			{
				SoilTemp[index] = value;
				dataValuesUpdated.SoilTemp[index] = value.HasValue;
			}
		}

		public void DoAirQuality(double? value, int index)
		{
			if (index < AirQuality.Length)
			{
				AirQuality[index] = value;
				dataValuesUpdated.AirQuality[index] = value.HasValue;
			}

		}

		public void DoAirQualityAvg(double? value, int index)
		{
			if (index < AirQualityAvg.Length)
			{
				AirQualityAvg[index] = value;
				dataValuesUpdated.AirQualityAvg[index] = value.HasValue;
			}
		}

		public void DoLeakSensor(int value, int index)
		{
			switch (index)
			{
				case 1:
					LeakSensor1 = value;
					break;
				case 2:
					LeakSensor2 = value;
					break;
				case 3:
					LeakSensor3 = value;
					break;
				case 4:
					LeakSensor4 = value;
					break;
			}
		}

		public void DoLeafWetness(double? value, int index)
		{
			if (index < LeafWetness.Length)
			{
				LeafWetness[index] = value;
				dataValuesUpdated.LeafWetness[index] = value.HasValue;

				if (cumulus.StationOptions.LeafWetnessIsRainingIdx == index)
				{
					IsRaining = value >= cumulus.StationOptions.LeafWetnessIsRainingThrsh;
					cumulus.IsRainingAlarm.Triggered = IsRaining;
				}
			}
		}


		public void DoLeafTemp(double? value, int index)
		{
			if (index < LeafTemp.Length)
			{
				LeafTemp[index] = value;
				dataValuesUpdated.LeafTemp[index] = value.HasValue;
			}
		}

		public string BetelCast(double z_hpa, int z_month, string z_wind, int z_trend, bool z_north, double z_baro_top, double z_baro_bottom)
		{
			double z_range = z_baro_top - z_baro_bottom;
			double z_constant = (z_range / 22.0F);

			bool z_summer = (z_month >= 4 && z_month <= 9); // true if "Summer"

			if (z_north)
			{
				// North hemisphere
				if (z_wind == cumulus.compassp[0]) // N
				{
					z_hpa += 6F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[1]) // NNE
				{
					z_hpa += 5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[2]) // NE
				{
					//			z_hpa += 4 ;
					z_hpa += 5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[3]) // ENE
				{
					z_hpa += 2F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[4]) // E
				{
					z_hpa -= 0.5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[5]) // ESE
				{
					//			z_hpa -= 3 ;
					z_hpa -= 2F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[6]) // SE
				{
					z_hpa -= 5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[7]) // SSE
				{
					z_hpa -= 8.5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[8]) // S
				{
					//			z_hpa -= 11 ;
					z_hpa -= 12F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[9]) // SSW
				{
					z_hpa -= 10F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[10]) // SW
				{
					z_hpa -= 6F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[11]) // WSW
				{
					z_hpa -= 4.5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[12]) // W
				{
					z_hpa -= 3F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[13]) // WNW
				{
					z_hpa -= 0.5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[14]) // NW
				{
					z_hpa += 1.5F / 100 * z_range;
				}
				else if (z_wind == cumulus.compassp[15]) // NNW
				{
					z_hpa += 3F / 100F * z_range;
				}
				if (z_summer)
				{
					// if Summer
					if (z_trend == 1)
					{
						// rising
						z_hpa += 7F / 100F * z_range;
					}
					else if (z_trend == 2)
					{
						//	falling
						z_hpa -= 7F / 100F * z_range;
					}
				}
			}
			else
			{
				// must be South hemisphere
				if (z_wind == cumulus.compassp[8]) // S
				{
					z_hpa += 6F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[9]) // SSW
				{
					z_hpa += 5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[10]) // SW
				{
					//			z_hpa += 4 ;
					z_hpa += 5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[11]) // WSW
				{
					z_hpa += 2F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[12]) // W
				{
					z_hpa -= 0.5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[13]) // WNW
				{
					//			z_hpa -= 3 ;
					z_hpa -= 2F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[14]) // NW
				{
					z_hpa -= 5F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[15]) // NNW
				{
					z_hpa -= 8.5F / 100 * z_range;
				}
				else if (z_wind == cumulus.compassp[0]) // N
				{
					//			z_hpa -= 11 ;
					z_hpa -= 12F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[1]) // NNE
				{
					z_hpa -= 10F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[2]) // NE
				{
					z_hpa -= 6F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[3]) // ENE
				{
					z_hpa -= 4.5F / 100 * z_range; //
				}
				else if (z_wind == cumulus.compassp[4]) // E
				{
					z_hpa -= 3F / 100F * z_range;
				}
				else if (z_wind == cumulus.compassp[5]) // ESE
				{
					z_hpa -= 0.5F / 100 * z_range;
				}
				else if (z_wind == cumulus.compassp[6]) // SE
				{
					z_hpa += 1.5F / 100 * z_range;
				}
				else if (z_wind == cumulus.compassp[7]) // SSE
				{
					z_hpa += 3F / 100F * z_range;
				}
				if (!z_summer)
				{
					// if Winter
					if (z_trend == 1)
					{
						// rising
						z_hpa += 7F / 100F * z_range;
					}
					else if (z_trend == 2)
					{
						// falling
						z_hpa -= 7F / 100F * z_range;
					}
				}
			} // END North / South

			if (z_hpa == z_baro_top)
			{
				z_hpa = z_baro_top - 1;
			}

			int z_option = (int)Math.Floor((z_hpa - z_baro_bottom) / z_constant);

			StringBuilder z_output = new StringBuilder(100);
			if (z_option < 0)
			{
				z_option = 0;
				z_output.Append($"{cumulus.exceptional}, ");
			}
			if (z_option > 21)
			{
				z_option = 21;
				z_output.Append($"{cumulus.exceptional}, ");
			}

			if (z_trend == 1)
			{
				// rising
				Forecastnumber = cumulus.riseOptions[z_option] + 1;
				z_output.Append(cumulus.zForecast[cumulus.riseOptions[z_option]]);
			}
			else if (z_trend == 2)
			{
				// falling
				Forecastnumber = cumulus.fallOptions[z_option] + 1;
				z_output.Append(cumulus.zForecast[cumulus.fallOptions[z_option]]);
			}
			else
			{
				// must be "steady"
				Forecastnumber = cumulus.steadyOptions[z_option] + 1;
				z_output.Append(cumulus.zForecast[cumulus.steadyOptions[z_option]]);
			}
			return z_output.ToString();
		}

		public int Forecastnumber { get; set; }

		/// <summary>
		/// Takes speed in user units, returns Bft number
		/// </summary>
		/// <param name="windspeed"></param>
		/// <returns></returns>
		public int Beaufort(double? speed)
		{
			if (speed == null)
				return -1;

			double windspeedMS = ConvertUserWindToMS(speed).Value;
			if (windspeedMS < 0.3)
				return 0;
			else if (windspeedMS < 1.6)
				return 1;
			else if (windspeedMS < 3.4)
				return 2;
			else if (windspeedMS < 5.5)
				return 3;
			else if (windspeedMS < 8.0)
				return 4;
			else if (windspeedMS < 10.8)
				return 5;
			else if (windspeedMS < 13.9)
				return 6;
			else if (windspeedMS < 17.2)
				return 7;
			else if (windspeedMS < 20.8)
				return 8;
			else if (windspeedMS < 24.5)
				return 9;
			else if (windspeedMS < 28.5)
				return 10;
			else if (windspeedMS < 32.7)
				return 11;
			else return 12;
		}

		// This overridden in each station implementation
		public virtual void Stop()
		{
			Cumulus.LogMessage("Closing the database");
			//DatabaseAsync.CloseAsync().Wait();
			Database.Close();
			Cumulus.LogMessage("Database closed");
		}

		public void ReadAlltimeIniFile()
		{
			Cumulus.LogMessage(Path.GetFullPath(cumulus.AlltimeIniFile));
			IniFile ini = new IniFile(cumulus.AlltimeIniFile);

			AllTime.HighTemp.Val = ini.GetValue("Temperature", "hightempvalue", Cumulus.DefaultHiVal);
			AllTime.HighTemp.Ts = ini.GetValue("Temperature", "hightemptime", cumulus.defaultRecordTS);

			AllTime.LowTemp.Val = ini.GetValue("Temperature", "lowtempvalue", Cumulus.DefaultLoVal);
			AllTime.LowTemp.Ts = ini.GetValue("Temperature", "lowtemptime", cumulus.defaultRecordTS);

			AllTime.LowChill.Val = ini.GetValue("Temperature", "lowchillvalue", Cumulus.DefaultLoVal);
			AllTime.LowChill.Ts = ini.GetValue("Temperature", "lowchilltime", cumulus.defaultRecordTS);

			AllTime.HighMinTemp.Val = ini.GetValue("Temperature", "highmintempvalue", Cumulus.DefaultHiVal);
			AllTime.HighMinTemp.Ts = ini.GetValue("Temperature", "highmintemptime", cumulus.defaultRecordTS);

			AllTime.LowMaxTemp.Val = ini.GetValue("Temperature", "lowmaxtempvalue", Cumulus.DefaultLoVal);
			AllTime.LowMaxTemp.Ts = ini.GetValue("Temperature", "lowmaxtemptime", cumulus.defaultRecordTS);

			AllTime.HighAppTemp.Val = ini.GetValue("Temperature", "highapptempvalue", Cumulus.DefaultHiVal);
			AllTime.HighAppTemp.Ts = ini.GetValue("Temperature", "highapptemptime", cumulus.defaultRecordTS);

			AllTime.LowAppTemp.Val = ini.GetValue("Temperature", "lowapptempvalue", Cumulus.DefaultLoVal);
			AllTime.LowAppTemp.Ts = ini.GetValue("Temperature", "lowapptemptime", cumulus.defaultRecordTS);

			AllTime.HighFeelsLike.Val = ini.GetValue("Temperature", "highfeelslikevalue", Cumulus.DefaultHiVal);
			AllTime.HighFeelsLike.Ts = ini.GetValue("Temperature", "highfeelsliketime", cumulus.defaultRecordTS);

			AllTime.LowFeelsLike.Val = ini.GetValue("Temperature", "lowfeelslikevalue", Cumulus.DefaultLoVal);
			AllTime.LowFeelsLike.Ts = ini.GetValue("Temperature", "lowfeelsliketime", cumulus.defaultRecordTS);

			AllTime.HighHumidex.Val = ini.GetValue("Temperature", "highhumidexvalue", Cumulus.DefaultHiVal);
			AllTime.HighHumidex.Ts = ini.GetValue("Temperature", "highhumidextime", cumulus.defaultRecordTS);

			AllTime.HighHeatIndex.Val = ini.GetValue("Temperature", "highheatindexvalue", Cumulus.DefaultHiVal);
			AllTime.HighHeatIndex.Ts = ini.GetValue("Temperature", "highheatindextime", cumulus.defaultRecordTS);

			AllTime.HighDewPoint.Val = ini.GetValue("Temperature", "highdewpointvalue", Cumulus.DefaultHiVal);
			AllTime.HighDewPoint.Ts = ini.GetValue("Temperature", "highdewpointtime", cumulus.defaultRecordTS);

			AllTime.LowDewPoint.Val = ini.GetValue("Temperature", "lowdewpointvalue", Cumulus.DefaultLoVal);
			AllTime.LowDewPoint.Ts = ini.GetValue("Temperature", "lowdewpointtime", cumulus.defaultRecordTS);

			AllTime.HighDailyTempRange.Val = ini.GetValue("Temperature", "hightemprangevalue", Cumulus.DefaultHiVal);
			AllTime.HighDailyTempRange.Ts = ini.GetValue("Temperature", "hightemprangetime", cumulus.defaultRecordTS);

			AllTime.LowDailyTempRange.Val = ini.GetValue("Temperature", "lowtemprangevalue", Cumulus.DefaultLoVal);
			AllTime.LowDailyTempRange.Ts = ini.GetValue("Temperature", "lowtemprangetime", cumulus.defaultRecordTS);

			AllTime.HighWind.Val = ini.GetValue("Wind", "highwindvalue", Cumulus.DefaultHiVal);
			AllTime.HighWind.Ts = ini.GetValue("Wind", "highwindtime", cumulus.defaultRecordTS);

			AllTime.HighGust.Val = ini.GetValue("Wind", "highgustvalue", Cumulus.DefaultHiVal);
			AllTime.HighGust.Ts = ini.GetValue("Wind", "highgusttime", cumulus.defaultRecordTS);

			AllTime.HighWindRun.Val = ini.GetValue("Wind", "highdailywindrunvalue", Cumulus.DefaultHiVal);
			AllTime.HighWindRun.Ts = ini.GetValue("Wind", "highdailywindruntime", cumulus.defaultRecordTS);

			AllTime.HighRainRate.Val = ini.GetValue("Rain", "highrainratevalue", Cumulus.DefaultHiVal);
			AllTime.HighRainRate.Ts = ini.GetValue("Rain", "highrainratetime", cumulus.defaultRecordTS);

			AllTime.DailyRain.Val = ini.GetValue("Rain", "highdailyrainvalue", Cumulus.DefaultHiVal);
			AllTime.DailyRain.Ts = ini.GetValue("Rain", "highdailyraintime", cumulus.defaultRecordTS);

			AllTime.HourlyRain.Val = ini.GetValue("Rain", "highhourlyrainvalue", Cumulus.DefaultHiVal);
			AllTime.HourlyRain.Ts = ini.GetValue("Rain", "highhourlyraintime", cumulus.defaultRecordTS);

			AllTime.HighRain24Hours.Val = ini.GetValue("Rain", "high24hourrainvalue", Cumulus.DefaultHiVal);
			AllTime.HighRain24Hours.Ts = ini.GetValue("Rain", "high24hourraintime", cumulus.defaultRecordTS);

			AllTime.MonthlyRain.Val = ini.GetValue("Rain", "highmonthlyrainvalue", Cumulus.DefaultHiVal);
			AllTime.MonthlyRain.Ts = ini.GetValue("Rain", "highmonthlyraintime", cumulus.defaultRecordTS);

			AllTime.LongestDryPeriod.Val = ini.GetValue("Rain", "longestdryperiodvalue", 0);
			AllTime.LongestDryPeriod.Ts = ini.GetValue("Rain", "longestdryperiodtime", cumulus.defaultRecordTS);

			AllTime.LongestWetPeriod.Val = ini.GetValue("Rain", "longestwetperiodvalue", 0);
			AllTime.LongestWetPeriod.Ts = ini.GetValue("Rain", "longestwetperiodtime", cumulus.defaultRecordTS);

			AllTime.HighPress.Val = ini.GetValue("Pressure", "highpressurevalue", Cumulus.DefaultHiVal);
			AllTime.HighPress.Ts = ini.GetValue("Pressure", "highpressuretime", cumulus.defaultRecordTS);

			AllTime.LowPress.Val = ini.GetValue("Pressure", "lowpressurevalue", Cumulus.DefaultLoVal);
			AllTime.LowPress.Ts = ini.GetValue("Pressure", "lowpressuretime", cumulus.defaultRecordTS);

			AllTime.HighHumidity.Val = ini.GetValue("Humidity", "highhumidityvalue", Cumulus.DefaultHiVal);
			AllTime.HighHumidity.Ts = ini.GetValue("Humidity", "highhumiditytime", cumulus.defaultRecordTS);

			AllTime.LowHumidity.Val = ini.GetValue("Humidity", "lowhumidityvalue", Cumulus.DefaultLoVal);
			AllTime.LowHumidity.Ts = ini.GetValue("Humidity", "lowhumiditytime", cumulus.defaultRecordTS);

			Cumulus.LogMessage("Alltime.ini file read");
		}

		public void WriteAlltimeIniFile()
		{
			try
			{
				IniFile ini = new IniFile(cumulus.AlltimeIniFile);

				ini.SetValue("Temperature", "hightempvalue", AllTime.HighTemp.Val);
				ini.SetValue("Temperature", "hightemptime", AllTime.HighTemp.Ts);
				ini.SetValue("Temperature", "lowtempvalue", AllTime.LowTemp.Val);
				ini.SetValue("Temperature", "lowtemptime", AllTime.LowTemp.Ts);
				ini.SetValue("Temperature", "lowchillvalue", AllTime.LowChill.Val);
				ini.SetValue("Temperature", "lowchilltime", AllTime.LowChill.Ts);
				ini.SetValue("Temperature", "highmintempvalue", AllTime.HighMinTemp.Val);
				ini.SetValue("Temperature", "highmintemptime", AllTime.HighMinTemp.Ts);
				ini.SetValue("Temperature", "lowmaxtempvalue", AllTime.LowMaxTemp.Val);
				ini.SetValue("Temperature", "lowmaxtemptime", AllTime.LowMaxTemp.Ts);
				ini.SetValue("Temperature", "highapptempvalue", AllTime.HighAppTemp.Val);
				ini.SetValue("Temperature", "highapptemptime", AllTime.HighAppTemp.Ts);
				ini.SetValue("Temperature", "lowapptempvalue", AllTime.LowAppTemp.Val);
				ini.SetValue("Temperature", "lowapptemptime", AllTime.LowAppTemp.Ts);
				ini.SetValue("Temperature", "highfeelslikevalue", AllTime.HighFeelsLike.Val);
				ini.SetValue("Temperature", "highfeelsliketime", AllTime.HighFeelsLike.Ts);
				ini.SetValue("Temperature", "lowfeelslikevalue", AllTime.LowFeelsLike.Val);
				ini.SetValue("Temperature", "lowfeelsliketime", AllTime.LowFeelsLike.Ts);
				ini.SetValue("Temperature", "highhumidexvalue", AllTime.HighHumidex.Val);
				ini.SetValue("Temperature", "highhumidextime", AllTime.HighHumidex.Ts);
				ini.SetValue("Temperature", "highheatindexvalue", AllTime.HighHeatIndex.Val);
				ini.SetValue("Temperature", "highheatindextime", AllTime.HighHeatIndex.Ts);
				ini.SetValue("Temperature", "highdewpointvalue", AllTime.HighDewPoint.Val);
				ini.SetValue("Temperature", "highdewpointtime", AllTime.HighDewPoint.Ts);
				ini.SetValue("Temperature", "lowdewpointvalue", AllTime.LowDewPoint.Val);
				ini.SetValue("Temperature", "lowdewpointtime", AllTime.LowDewPoint.Ts);
				ini.SetValue("Temperature", "hightemprangevalue", AllTime.HighDailyTempRange.Val);
				ini.SetValue("Temperature", "hightemprangetime", AllTime.HighDailyTempRange.Ts);
				ini.SetValue("Temperature", "lowtemprangevalue", AllTime.LowDailyTempRange.Val);
				ini.SetValue("Temperature", "lowtemprangetime", AllTime.LowDailyTempRange.Ts);
				ini.SetValue("Wind", "highwindvalue", AllTime.HighWind.Val);
				ini.SetValue("Wind", "highwindtime", AllTime.HighWind.Ts);
				ini.SetValue("Wind", "highgustvalue", AllTime.HighGust.Val);
				ini.SetValue("Wind", "highgusttime", AllTime.HighGust.Ts);
				ini.SetValue("Wind", "highdailywindrunvalue", AllTime.HighWindRun.Val);
				ini.SetValue("Wind", "highdailywindruntime", AllTime.HighWindRun.Ts);
				ini.SetValue("Rain", "highrainratevalue", AllTime.HighRainRate.Val);
				ini.SetValue("Rain", "highrainratetime", AllTime.HighRainRate.Ts);
				ini.SetValue("Rain", "highdailyrainvalue", AllTime.DailyRain.Val);
				ini.SetValue("Rain", "highdailyraintime", AllTime.DailyRain.Ts);
				ini.SetValue("Rain", "highhourlyrainvalue", AllTime.HourlyRain.Val);
				ini.SetValue("Rain", "highhourlyraintime", AllTime.HourlyRain.Ts);
				ini.SetValue("Rain", "high24hourrainvalue", AllTime.HighRain24Hours.Val);
				ini.SetValue("Rain", "high24hourraintime", AllTime.HighRain24Hours.Ts);
				ini.SetValue("Rain", "highmonthlyrainvalue", AllTime.MonthlyRain.Val);
				ini.SetValue("Rain", "highmonthlyraintime", AllTime.MonthlyRain.Ts);
				ini.SetValue("Rain", "longestdryperiodvalue", AllTime.LongestDryPeriod.Val);
				ini.SetValue("Rain", "longestdryperiodtime", AllTime.LongestDryPeriod.Ts);
				ini.SetValue("Rain", "longestwetperiodvalue", AllTime.LongestWetPeriod.Val);
				ini.SetValue("Rain", "longestwetperiodtime", AllTime.LongestWetPeriod.Ts);
				ini.SetValue("Pressure", "highpressurevalue", AllTime.HighPress.Val);
				ini.SetValue("Pressure", "highpressuretime", AllTime.HighPress.Ts);
				ini.SetValue("Pressure", "lowpressurevalue", AllTime.LowPress.Val);
				ini.SetValue("Pressure", "lowpressuretime", AllTime.LowPress.Ts);
				ini.SetValue("Humidity", "highhumidityvalue", AllTime.HighHumidity.Val);
				ini.SetValue("Humidity", "highhumiditytime", AllTime.HighHumidity.Ts);
				ini.SetValue("Humidity", "lowhumidityvalue", AllTime.LowHumidity.Val);
				ini.SetValue("Humidity", "lowhumiditytime", AllTime.LowHumidity.Ts);

				ini.Flush();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error writing alltime.ini file");
			}
		}

		public void ReadMonthlyAlltimeIniFile()
		{
			IniFile ini = new IniFile(cumulus.MonthlyAlltimeIniFile);
			for (int month = 1; month <= 12; month++)
			{
				string monthstr = month.ToString("D2");

				MonthlyRecs[month].HighTemp.Val = ini.GetValue("Temperature" + monthstr, "hightempvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighTemp.Ts = ini.GetValue("Temperature" + monthstr, "hightemptime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowTemp.Val = ini.GetValue("Temperature" + monthstr, "lowtempvalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowTemp.Ts = ini.GetValue("Temperature" + monthstr, "lowtemptime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowChill.Val = ini.GetValue("Temperature" + monthstr, "lowchillvalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowChill.Ts = ini.GetValue("Temperature" + monthstr, "lowchilltime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighMinTemp.Val = ini.GetValue("Temperature" + monthstr, "highmintempvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighMinTemp.Ts = ini.GetValue("Temperature" + monthstr, "highmintemptime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowMaxTemp.Val = ini.GetValue("Temperature" + monthstr, "lowmaxtempvalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowMaxTemp.Ts = ini.GetValue("Temperature" + monthstr, "lowmaxtemptime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighAppTemp.Val = ini.GetValue("Temperature" + monthstr, "highapptempvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighAppTemp.Ts = ini.GetValue("Temperature" + monthstr, "highapptemptime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowAppTemp.Val = ini.GetValue("Temperature" + monthstr, "lowapptempvalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowAppTemp.Ts = ini.GetValue("Temperature" + monthstr, "lowapptemptime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighFeelsLike.Val = ini.GetValue("Temperature" + monthstr, "highfeelslikevalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighFeelsLike.Ts = ini.GetValue("Temperature" + monthstr, "highfeelsliketime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowFeelsLike.Val = ini.GetValue("Temperature" + monthstr, "lowfeelslikevalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowFeelsLike.Ts = ini.GetValue("Temperature" + monthstr, "lowfeelsliketime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighHumidex.Val = ini.GetValue("Temperature" + monthstr, "highhumidexvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighHumidex.Ts = ini.GetValue("Temperature" + monthstr, "highhumidextime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighHeatIndex.Val = ini.GetValue("Temperature" + monthstr, "highheatindexvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighHeatIndex.Ts = ini.GetValue("Temperature" + monthstr, "highheatindextime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighDewPoint.Val = ini.GetValue("Temperature" + monthstr, "highdewpointvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighDewPoint.Ts = ini.GetValue("Temperature" + monthstr, "highdewpointtime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowDewPoint.Val = ini.GetValue("Temperature" + monthstr, "lowdewpointvalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowDewPoint.Ts = ini.GetValue("Temperature" + monthstr, "lowdewpointtime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighDailyTempRange.Val = ini.GetValue("Temperature" + monthstr, "hightemprangevalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighDailyTempRange.Ts = ini.GetValue("Temperature" + monthstr, "hightemprangetime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowDailyTempRange.Val = ini.GetValue("Temperature" + monthstr, "lowtemprangevalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowDailyTempRange.Ts = ini.GetValue("Temperature" + monthstr, "lowtemprangetime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighWind.Val = ini.GetValue("Wind" + monthstr, "highwindvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighWind.Ts = ini.GetValue("Wind" + monthstr, "highwindtime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighGust.Val = ini.GetValue("Wind" + monthstr, "highgustvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighGust.Ts = ini.GetValue("Wind" + monthstr, "highgusttime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighWindRun.Val = ini.GetValue("Wind" + monthstr, "highdailywindrunvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighWindRun.Ts = ini.GetValue("Wind" + monthstr, "highdailywindruntime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighRainRate.Val = ini.GetValue("Rain" + monthstr, "highrainratevalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighRainRate.Ts = ini.GetValue("Rain" + monthstr, "highrainratetime", cumulus.defaultRecordTS);

				MonthlyRecs[month].DailyRain.Val = ini.GetValue("Rain" + monthstr, "highdailyrainvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].DailyRain.Ts = ini.GetValue("Rain" + monthstr, "highdailyraintime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HourlyRain.Val = ini.GetValue("Rain" + monthstr, "highhourlyrainvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HourlyRain.Ts = ini.GetValue("Rain" + monthstr, "highhourlyraintime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighRain24Hours.Val = ini.GetValue("Rain" + monthstr, "high24hourrainvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighRain24Hours.Ts = ini.GetValue("Rain" + monthstr, "high24hourraintime", cumulus.defaultRecordTS);

				MonthlyRecs[month].MonthlyRain.Val = ini.GetValue("Rain" + monthstr, "highmonthlyrainvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].MonthlyRain.Ts = ini.GetValue("Rain" + monthstr, "highmonthlyraintime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LongestDryPeriod.Val = ini.GetValue("Rain" + monthstr, "longestdryperiodvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].LongestDryPeriod.Ts = ini.GetValue("Rain" + monthstr, "longestdryperiodtime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LongestWetPeriod.Val = ini.GetValue("Rain" + monthstr, "longestwetperiodvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].LongestWetPeriod.Ts = ini.GetValue("Rain" + monthstr, "longestwetperiodtime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighPress.Val = ini.GetValue("Pressure" + monthstr, "highpressurevalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighPress.Ts = ini.GetValue("Pressure" + monthstr, "highpressuretime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowPress.Val = ini.GetValue("Pressure" + monthstr, "lowpressurevalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowPress.Ts = ini.GetValue("Pressure" + monthstr, "lowpressuretime", cumulus.defaultRecordTS);

				MonthlyRecs[month].HighHumidity.Val = ini.GetValue("Humidity" + monthstr, "highhumidityvalue", Cumulus.DefaultHiVal);
				MonthlyRecs[month].HighHumidity.Ts = ini.GetValue("Humidity" + monthstr, "highhumiditytime", cumulus.defaultRecordTS);

				MonthlyRecs[month].LowHumidity.Val = ini.GetValue("Humidity" + monthstr, "lowhumidityvalue", Cumulus.DefaultLoVal);
				MonthlyRecs[month].LowHumidity.Ts = ini.GetValue("Humidity" + monthstr, "lowhumiditytime", cumulus.defaultRecordTS);
			}

			Cumulus.LogMessage("MonthlyAlltime.ini file read");
		}

		public void WriteMonthlyAlltimeIniFile()
		{
			try
			{
				IniFile ini = new IniFile(cumulus.MonthlyAlltimeIniFile);
				for (int month = 1; month <= 12; month++)
				{
					string monthstr = month.ToString("D2");

					ini.SetValue("Temperature" + monthstr, "hightempvalue", MonthlyRecs[month].HighTemp.Val);
					ini.SetValue("Temperature" + monthstr, "hightemptime", MonthlyRecs[month].HighTemp.Ts);
					ini.SetValue("Temperature" + monthstr, "lowtempvalue", MonthlyRecs[month].LowTemp.Val);
					ini.SetValue("Temperature" + monthstr, "lowtemptime", MonthlyRecs[month].LowTemp.Ts);
					ini.SetValue("Temperature" + monthstr, "lowchillvalue", MonthlyRecs[month].LowChill.Val);
					ini.SetValue("Temperature" + monthstr, "lowchilltime", MonthlyRecs[month].LowChill.Ts);
					ini.SetValue("Temperature" + monthstr, "highmintempvalue", MonthlyRecs[month].HighMinTemp.Val);
					ini.SetValue("Temperature" + monthstr, "highmintemptime", MonthlyRecs[month].HighMinTemp.Ts);
					ini.SetValue("Temperature" + monthstr, "lowmaxtempvalue", MonthlyRecs[month].LowMaxTemp.Val);
					ini.SetValue("Temperature" + monthstr, "lowmaxtemptime", MonthlyRecs[month].LowMaxTemp.Ts);
					ini.SetValue("Temperature" + monthstr, "highapptempvalue", MonthlyRecs[month].HighAppTemp.Val);
					ini.SetValue("Temperature" + monthstr, "highapptemptime", MonthlyRecs[month].HighAppTemp.Ts);
					ini.SetValue("Temperature" + monthstr, "lowapptempvalue", MonthlyRecs[month].LowAppTemp.Val);
					ini.SetValue("Temperature" + monthstr, "lowapptemptime", MonthlyRecs[month].LowAppTemp.Ts);
					ini.SetValue("Temperature" + monthstr, "highfeelslikevalue", MonthlyRecs[month].HighFeelsLike.Val);
					ini.SetValue("Temperature" + monthstr, "highfeelsliketime", MonthlyRecs[month].HighFeelsLike.Ts);
					ini.SetValue("Temperature" + monthstr, "lowfeelslikevalue", MonthlyRecs[month].LowFeelsLike.Val);
					ini.SetValue("Temperature" + monthstr, "lowfeelsliketime", MonthlyRecs[month].LowFeelsLike.Ts);
					ini.SetValue("Temperature" + monthstr, "highhumidexvalue", MonthlyRecs[month].HighHumidex.Val);
					ini.SetValue("Temperature" + monthstr, "highhumidextime", MonthlyRecs[month].HighHumidex.Ts);
					ini.SetValue("Temperature" + monthstr, "highheatindexvalue", MonthlyRecs[month].HighHeatIndex.Val);
					ini.SetValue("Temperature" + monthstr, "highheatindextime", MonthlyRecs[month].HighHeatIndex.Ts);
					ini.SetValue("Temperature" + monthstr, "highdewpointvalue", MonthlyRecs[month].HighDewPoint.Val);
					ini.SetValue("Temperature" + monthstr, "highdewpointtime", MonthlyRecs[month].HighDewPoint.Ts);
					ini.SetValue("Temperature" + monthstr, "lowdewpointvalue", MonthlyRecs[month].LowDewPoint.Val);
					ini.SetValue("Temperature" + monthstr, "lowdewpointtime", MonthlyRecs[month].LowDewPoint.Ts);
					ini.SetValue("Temperature" + monthstr, "hightemprangevalue", MonthlyRecs[month].HighDailyTempRange.Val);
					ini.SetValue("Temperature" + monthstr, "hightemprangetime", MonthlyRecs[month].HighDailyTempRange.Ts);
					ini.SetValue("Temperature" + monthstr, "lowtemprangevalue", MonthlyRecs[month].LowDailyTempRange.Val);
					ini.SetValue("Temperature" + monthstr, "lowtemprangetime", MonthlyRecs[month].LowDailyTempRange.Ts);
					ini.SetValue("Wind" + monthstr, "highwindvalue", MonthlyRecs[month].HighWind.Val);
					ini.SetValue("Wind" + monthstr, "highwindtime", MonthlyRecs[month].HighWind.Ts);
					ini.SetValue("Wind" + monthstr, "highgustvalue", MonthlyRecs[month].HighGust.Val);
					ini.SetValue("Wind" + monthstr, "highgusttime", MonthlyRecs[month].HighGust.Ts);
					ini.SetValue("Wind" + monthstr, "highdailywindrunvalue", MonthlyRecs[month].HighWindRun.Val);
					ini.SetValue("Wind" + monthstr, "highdailywindruntime", MonthlyRecs[month].HighWindRun.Ts);
					ini.SetValue("Rain" + monthstr, "highrainratevalue", MonthlyRecs[month].HighRainRate.Val);
					ini.SetValue("Rain" + monthstr, "highrainratetime", MonthlyRecs[month].HighRainRate.Ts);
					ini.SetValue("Rain" + monthstr, "highdailyrainvalue", MonthlyRecs[month].DailyRain.Val);
					ini.SetValue("Rain" + monthstr, "highdailyraintime", MonthlyRecs[month].DailyRain.Ts);
					ini.SetValue("Rain" + monthstr, "highhourlyrainvalue", MonthlyRecs[month].HourlyRain.Val);
					ini.SetValue("Rain" + monthstr, "highhourlyraintime", MonthlyRecs[month].HourlyRain.Ts);
					ini.SetValue("Rain" + monthstr, "high24hourrainvalue", MonthlyRecs[month].HighRain24Hours.Val);
					ini.SetValue("Rain" + monthstr, "high24hourraintime", MonthlyRecs[month].HighRain24Hours.Ts);
					ini.SetValue("Rain" + monthstr, "highmonthlyrainvalue", MonthlyRecs[month].MonthlyRain.Val);
					ini.SetValue("Rain" + monthstr, "highmonthlyraintime", MonthlyRecs[month].MonthlyRain.Ts);
					ini.SetValue("Rain" + monthstr, "longestdryperiodvalue", MonthlyRecs[month].LongestDryPeriod.Val);
					ini.SetValue("Rain" + monthstr, "longestdryperiodtime", MonthlyRecs[month].LongestDryPeriod.Ts);
					ini.SetValue("Rain" + monthstr, "longestwetperiodvalue", MonthlyRecs[month].LongestWetPeriod.Val);
					ini.SetValue("Rain" + monthstr, "longestwetperiodtime", MonthlyRecs[month].LongestWetPeriod.Ts);
					ini.SetValue("Pressure" + monthstr, "highpressurevalue", MonthlyRecs[month].HighPress.Val);
					ini.SetValue("Pressure" + monthstr, "highpressuretime", MonthlyRecs[month].HighPress.Ts);
					ini.SetValue("Pressure" + monthstr, "lowpressurevalue", MonthlyRecs[month].LowPress.Val);
					ini.SetValue("Pressure" + monthstr, "lowpressuretime", MonthlyRecs[month].LowPress.Ts);
					ini.SetValue("Humidity" + monthstr, "highhumidityvalue", MonthlyRecs[month].HighHumidity.Val);
					ini.SetValue("Humidity" + monthstr, "highhumiditytime", MonthlyRecs[month].HighHumidity.Ts);
					ini.SetValue("Humidity" + monthstr, "lowhumidityvalue", MonthlyRecs[month].LowHumidity.Val);
					ini.SetValue("Humidity" + monthstr, "lowhumiditytime", MonthlyRecs[month].LowHumidity.Ts);
				}
				ini.Flush();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Error writing MonthlyAlltime.ini file");
			}
		}

		public void SetDefaultMonthlyHighsAndLows()
		{
			// this Month highs and lows
			ThisMonth.HighGust.Val = Cumulus.DefaultHiVal;
			ThisMonth.HighWind.Val = Cumulus.DefaultHiVal;
			ThisMonth.HighTemp.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowTemp.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighAppTemp.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowAppTemp.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighFeelsLike.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowFeelsLike.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighHumidex.Val = Cumulus.DefaultHiVal;
			ThisMonth.HighDewPoint.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowDewPoint.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighPress.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowPress.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighRainRate.Val = Cumulus.DefaultHiVal;
			ThisMonth.HourlyRain.Val = Cumulus.DefaultHiVal;
			ThisMonth.HighRain24Hours.Val = Cumulus.DefaultHiVal;
			ThisMonth.DailyRain.Val = Cumulus.DefaultHiVal;
			ThisMonth.HighHumidity.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowHumidity.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighHeatIndex.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowChill.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighMinTemp.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowMaxTemp.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighWindRun.Val = Cumulus.DefaultHiVal;
			ThisMonth.LowDailyTempRange.Val = Cumulus.DefaultLoVal;
			ThisMonth.HighDailyTempRange.Val = Cumulus.DefaultHiVal;

			// this Month highs and lows - timestamps
			ThisMonth.HighGust.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighWind.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighTemp.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowTemp.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighAppTemp.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowAppTemp.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighFeelsLike.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowFeelsLike.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighHumidex.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighDewPoint.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowDewPoint.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighPress.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowPress.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighRainRate.Ts = cumulus.defaultRecordTS;
			ThisMonth.HourlyRain.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighRain24Hours.Ts = cumulus.defaultRecordTS;
			ThisMonth.DailyRain.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighHumidity.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowHumidity.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighHeatIndex.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowChill.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighMinTemp.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowMaxTemp.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighWindRun.Ts = cumulus.defaultRecordTS;
			ThisMonth.LowDailyTempRange.Ts = cumulus.defaultRecordTS;
			ThisMonth.HighDailyTempRange.Ts = cumulus.defaultRecordTS;
		}

		public void ReadMonthIniFile()
		{
			//DateTime timestamp;

			SetDefaultMonthlyHighsAndLows();

			if (File.Exists(cumulus.MonthIniFile))
			{
				//int hourInc = cumulus.GetHourInc();

				IniFile ini = new IniFile(cumulus.MonthIniFile);

				// Date
				//timestamp = ini.GetValue("General", "Date", cumulus.defaultRecordTS);

				ThisMonth.HighWind.Val = ini.GetValue("Wind", "Speed", Cumulus.DefaultHiVal);
				ThisMonth.HighWind.Ts = ini.GetValue("Wind", "SpTime", cumulus.defaultRecordTS);
				ThisMonth.HighGust.Val = ini.GetValue("Wind", "Gust", Cumulus.DefaultHiVal);
				ThisMonth.HighGust.Ts = ini.GetValue("Wind", "Time", cumulus.defaultRecordTS);
				ThisMonth.HighWindRun.Val = ini.GetValue("Wind", "Windrun", Cumulus.DefaultHiVal);
				ThisMonth.HighWindRun.Ts = ini.GetValue("Wind", "WindrunTime", cumulus.defaultRecordTS);
				// Temperature
				ThisMonth.LowTemp.Val = ini.GetValue("Temp", "Low", Cumulus.DefaultLoVal);
				ThisMonth.LowTemp.Ts = ini.GetValue("Temp", "LTime", cumulus.defaultRecordTS);
				ThisMonth.HighTemp.Val = ini.GetValue("Temp", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighTemp.Ts = ini.GetValue("Temp", "HTime", cumulus.defaultRecordTS);
				ThisMonth.LowMaxTemp.Val = ini.GetValue("Temp", "LowMax", Cumulus.DefaultLoVal);
				ThisMonth.LowMaxTemp.Ts = ini.GetValue("Temp", "LMTime", cumulus.defaultRecordTS);
				ThisMonth.HighMinTemp.Val = ini.GetValue("Temp", "HighMin", Cumulus.DefaultHiVal);
				ThisMonth.HighMinTemp.Ts = ini.GetValue("Temp", "HMTime", cumulus.defaultRecordTS);
				ThisMonth.LowDailyTempRange.Val = ini.GetValue("Temp", "LowRange", Cumulus.DefaultLoVal);
				ThisMonth.LowDailyTempRange.Ts = ini.GetValue("Temp", "LowRangeTime", cumulus.defaultRecordTS);
				ThisMonth.HighDailyTempRange.Val = ini.GetValue("Temp", "HighRange", Cumulus.DefaultHiVal);
				ThisMonth.HighDailyTempRange.Ts = ini.GetValue("Temp", "HighRangeTime", cumulus.defaultRecordTS);
				// Pressure
				ThisMonth.LowPress.Val = ini.GetValue("Pressure", "Low", Cumulus.DefaultLoVal);
				ThisMonth.LowPress.Ts = ini.GetValue("Pressure", "LTime", cumulus.defaultRecordTS);
				ThisMonth.HighPress.Val = ini.GetValue("Pressure", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighPress.Ts = ini.GetValue("Pressure", "HTime", cumulus.defaultRecordTS);
				// rain rate
				ThisMonth.HighRainRate.Val = ini.GetValue("Rain", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighRainRate.Ts = ini.GetValue("Rain", "HTime", cumulus.defaultRecordTS);
				ThisMonth.HourlyRain.Val = ini.GetValue("Rain", "HourlyHigh", Cumulus.DefaultHiVal);
				ThisMonth.HourlyRain.Ts = ini.GetValue("Rain", "HHourlyTime", cumulus.defaultRecordTS);
				ThisMonth.HighRain24Hours.Val = ini.GetValue("Rain", "24Hour", Cumulus.DefaultHiVal);
				ThisMonth.HighRain24Hours.Ts = ini.GetValue("Rain", "24HourTime", cumulus.defaultRecordTS);
				ThisMonth.DailyRain.Val = ini.GetValue("Rain", "DailyHigh", Cumulus.DefaultHiVal);
				ThisMonth.DailyRain.Ts = ini.GetValue("Rain", "HDailyTime", cumulus.defaultRecordTS);
				ThisMonth.LongestDryPeriod.Val = ini.GetValue("Rain", "LongestDryPeriod", Cumulus.DefaultHiVal);
				ThisMonth.LongestDryPeriod.Ts = ini.GetValue("Rain", "LongestDryPeriodTime", cumulus.defaultRecordTS);
				ThisMonth.LongestWetPeriod.Val = ini.GetValue("Rain", "LongestWetPeriod", Cumulus.DefaultHiVal);
				ThisMonth.LongestWetPeriod.Ts = ini.GetValue("Rain", "LongestWetPeriodTime", cumulus.defaultRecordTS);
				// humidity
				ThisMonth.LowHumidity.Val = ini.GetValue("Humidity", "Low", Cumulus.DefaultLoVal);
				ThisMonth.LowHumidity.Ts = ini.GetValue("Humidity", "LTime", cumulus.defaultRecordTS);
				ThisMonth.HighHumidity.Val = ini.GetValue("Humidity", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighHumidity.Ts = ini.GetValue("Humidity", "HTime", cumulus.defaultRecordTS);
				// heat index
				ThisMonth.HighHeatIndex.Val = ini.GetValue("HeatIndex", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighHeatIndex.Ts = ini.GetValue("HeatIndex", "HTime", cumulus.defaultRecordTS);
				// App temp
				ThisMonth.LowAppTemp.Val = ini.GetValue("AppTemp", "Low", Cumulus.DefaultLoVal);
				ThisMonth.LowAppTemp.Ts = ini.GetValue("AppTemp", "LTime", cumulus.defaultRecordTS);
				ThisMonth.HighAppTemp.Val = ini.GetValue("AppTemp", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighAppTemp.Ts = ini.GetValue("AppTemp", "HTime", cumulus.defaultRecordTS);
				// Dewpoint
				ThisMonth.LowDewPoint.Val = ini.GetValue("Dewpoint", "Low", Cumulus.DefaultLoVal);
				ThisMonth.LowDewPoint.Ts = ini.GetValue("Dewpoint", "LTime", cumulus.defaultRecordTS);
				ThisMonth.HighDewPoint.Val = ini.GetValue("Dewpoint", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighDewPoint.Ts = ini.GetValue("Dewpoint", "HTime", cumulus.defaultRecordTS);
				// wind chill
				ThisMonth.LowChill.Val = ini.GetValue("WindChill", "Low", Cumulus.DefaultLoVal);
				ThisMonth.LowChill.Ts = ini.GetValue("WindChill", "LTime", cumulus.defaultRecordTS);
				// Feels like temp
				ThisMonth.LowFeelsLike.Val = ini.GetValue("FeelsLike", "Low", Cumulus.DefaultLoVal);
				ThisMonth.LowFeelsLike.Ts = ini.GetValue("FeelsLike", "LTime", cumulus.defaultRecordTS);
				ThisMonth.HighFeelsLike.Val = ini.GetValue("FeelsLike", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighFeelsLike.Ts = ini.GetValue("FeelsLike", "HTime", cumulus.defaultRecordTS);
				// Humidex
				ThisMonth.HighHumidex.Val = ini.GetValue("Humidex", "High", Cumulus.DefaultHiVal);
				ThisMonth.HighHumidex.Ts = ini.GetValue("Humidex", "HTime", cumulus.defaultRecordTS);

				Cumulus.LogMessage("Month.ini file read");
			}
		}

		public void WriteMonthIniFile()
		{
			cumulus.LogDebugMessage("Writing to Month.ini file");
			lock (monthIniThreadLock)
			{
				try
				{
					int hourInc = cumulus.GetHourInc();

					IniFile ini = new IniFile(cumulus.MonthIniFile);
					// Date
					ini.SetValue("General", "Date", DateTime.Now.AddHours(hourInc));
					// Wind
					ini.SetValue("Wind", "Speed", ThisMonth.HighWind.Val);
					ini.SetValue("Wind", "SpTime", ThisMonth.HighWind.Ts);
					ini.SetValue("Wind", "Gust", ThisMonth.HighGust.Val);
					ini.SetValue("Wind", "Time", ThisMonth.HighGust.Ts);
					ini.SetValue("Wind", "Windrun", ThisMonth.HighWindRun.Val);
					ini.SetValue("Wind", "WindrunTime", ThisMonth.HighWindRun.Ts);
					// Temperature
					ini.SetValue("Temp", "Low", ThisMonth.LowTemp.Val);
					ini.SetValue("Temp", "LTime", ThisMonth.LowTemp.Ts);
					ini.SetValue("Temp", "High", ThisMonth.HighTemp.Val);
					ini.SetValue("Temp", "HTime", ThisMonth.HighTemp.Ts);
					ini.SetValue("Temp", "LowMax", ThisMonth.LowMaxTemp.Val);
					ini.SetValue("Temp", "LMTime", ThisMonth.LowMaxTemp.Ts);
					ini.SetValue("Temp", "HighMin", ThisMonth.HighMinTemp.Val);
					ini.SetValue("Temp", "HMTime", ThisMonth.HighMinTemp.Ts);
					ini.SetValue("Temp", "LowRange", ThisMonth.LowDailyTempRange.Val);
					ini.SetValue("Temp", "LowRangeTime", ThisMonth.LowDailyTempRange.Ts);
					ini.SetValue("Temp", "HighRange", ThisMonth.HighDailyTempRange.Val);
					ini.SetValue("Temp", "HighRangeTime", ThisMonth.HighDailyTempRange.Ts);
					// Pressure
					ini.SetValue("Pressure", "Low", ThisMonth.LowPress.Val);
					ini.SetValue("Pressure", "LTime", ThisMonth.LowPress.Ts);
					ini.SetValue("Pressure", "High", ThisMonth.HighPress.Val);
					ini.SetValue("Pressure", "HTime", ThisMonth.HighPress.Ts);
					// rain
					ini.SetValue("Rain", "High", ThisMonth.HighRainRate.Val);
					ini.SetValue("Rain", "HTime", ThisMonth.HighRainRate.Ts);
					ini.SetValue("Rain", "HourlyHigh", ThisMonth.HourlyRain.Val);
					ini.SetValue("Rain", "HHourlyTime", ThisMonth.HourlyRain.Ts);
					ini.SetValue("Rain", "DailyHigh", ThisMonth.DailyRain.Val);
					ini.SetValue("Rain", "HDailyTime", ThisMonth.DailyRain.Ts);
					ini.SetValue("Rain", "24Hour", ThisMonth.HighRain24Hours.Val);
					ini.SetValue("Rain", "24HourTime", ThisMonth.HighRain24Hours.Ts);
					ini.SetValue("Rain", "LongestDryPeriod", ThisMonth.LongestDryPeriod.Val);
					ini.SetValue("Rain", "LongestDryPeriodTime", ThisMonth.LongestDryPeriod.Ts);
					ini.SetValue("Rain", "LongestWetPeriod", ThisMonth.LongestWetPeriod.Val);
					ini.SetValue("Rain", "LongestWetPeriodTime", ThisMonth.LongestWetPeriod.Ts);
					// humidity
					ini.SetValue("Humidity", "Low", ThisMonth.LowHumidity.Val);
					ini.SetValue("Humidity", "LTime", ThisMonth.LowHumidity.Ts);
					ini.SetValue("Humidity", "High", ThisMonth.HighHumidity.Val);
					ini.SetValue("Humidity", "HTime", ThisMonth.HighHumidity.Ts);
					// heat index
					ini.SetValue("HeatIndex", "High", ThisMonth.HighHeatIndex.Val);
					ini.SetValue("HeatIndex", "HTime", ThisMonth.HighHeatIndex.Ts);
					// App temp
					ini.SetValue("AppTemp", "Low", ThisMonth.LowAppTemp.Val);
					ini.SetValue("AppTemp", "LTime", ThisMonth.LowAppTemp.Ts);
					ini.SetValue("AppTemp", "High", ThisMonth.HighAppTemp.Val);
					ini.SetValue("AppTemp", "HTime", ThisMonth.HighAppTemp.Ts);
					// Dewpoint
					ini.SetValue("Dewpoint", "Low", ThisMonth.LowDewPoint.Val);
					ini.SetValue("Dewpoint", "LTime", ThisMonth.LowDewPoint.Ts);
					ini.SetValue("Dewpoint", "High", ThisMonth.HighDewPoint.Val);
					ini.SetValue("Dewpoint", "HTime", ThisMonth.HighDewPoint.Ts);
					// wind chill
					ini.SetValue("WindChill", "Low", ThisMonth.LowChill.Val);
					ini.SetValue("WindChill", "LTime", ThisMonth.LowChill.Ts);
					// feels like
					ini.SetValue("FeelsLike", "Low", ThisMonth.LowFeelsLike.Val);
					ini.SetValue("FeelsLike", "LTime", ThisMonth.LowFeelsLike.Ts);
					ini.SetValue("FeelsLike", "High", ThisMonth.HighFeelsLike.Val);
					ini.SetValue("FeelsLike", "HTime", ThisMonth.HighFeelsLike.Ts);
					// Humidex
					ini.SetValue("Humidex", "High", ThisMonth.HighHumidex.Val);
					ini.SetValue("Humidex", "HTime", ThisMonth.HighHumidex.Ts);

					ini.Flush();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Error writing month.ini file");
				}
			}
			cumulus.LogDebugMessage("End writing to Month.ini file");
		}

		public void ReadYearIniFile()
		{
			//DateTime timestamp;

			SetDefaultYearlyHighsAndLows();

			if (File.Exists(cumulus.YearIniFile))
			{
				//int hourInc = cumulus.GetHourInc();

				IniFile ini = new IniFile(cumulus.YearIniFile);

				// Date
				//timestamp = ini.GetValue("General", "Date", cumulus.defaultRecordTS);

				ThisYear.HighWind.Val = ini.GetValue("Wind", "Speed", Cumulus.DefaultHiVal);
				ThisYear.HighWind.Ts = ini.GetValue("Wind", "SpTime", cumulus.defaultRecordTS);
				ThisYear.HighGust.Val = ini.GetValue("Wind", "Gust", Cumulus.DefaultHiVal);
				ThisYear.HighGust.Ts = ini.GetValue("Wind", "Time", cumulus.defaultRecordTS);
				ThisYear.HighWindRun.Val = ini.GetValue("Wind", "Windrun", Cumulus.DefaultHiVal);
				ThisYear.HighWindRun.Ts = ini.GetValue("Wind", "WindrunTime", cumulus.defaultRecordTS);
				// Temperature
				ThisYear.LowTemp.Val = ini.GetValue("Temp", "Low", Cumulus.DefaultLoVal);
				ThisYear.LowTemp.Ts = ini.GetValue("Temp", "LTime", cumulus.defaultRecordTS);
				ThisYear.HighTemp.Val = ini.GetValue("Temp", "High", Cumulus.DefaultHiVal);
				ThisYear.HighTemp.Ts = ini.GetValue("Temp", "HTime", cumulus.defaultRecordTS);
				ThisYear.LowMaxTemp.Val = ini.GetValue("Temp", "LowMax", Cumulus.DefaultLoVal);
				ThisYear.LowMaxTemp.Ts = ini.GetValue("Temp", "LMTime", cumulus.defaultRecordTS);
				ThisYear.HighMinTemp.Val = ini.GetValue("Temp", "HighMin", Cumulus.DefaultHiVal);
				ThisYear.HighMinTemp.Ts = ini.GetValue("Temp", "HMTime", cumulus.defaultRecordTS);
				ThisYear.LowDailyTempRange.Val = ini.GetValue("Temp", "LowRange", Cumulus.DefaultLoVal);
				ThisYear.LowDailyTempRange.Ts = ini.GetValue("Temp", "LowRangeTime", cumulus.defaultRecordTS);
				ThisYear.HighDailyTempRange.Val = ini.GetValue("Temp", "HighRange", Cumulus.DefaultHiVal);
				ThisYear.HighDailyTempRange.Ts = ini.GetValue("Temp", "HighRangeTime", cumulus.defaultRecordTS);
				// Pressure
				ThisYear.LowPress.Val = ini.GetValue("Pressure", "Low", Cumulus.DefaultLoVal);
				ThisYear.LowPress.Ts = ini.GetValue("Pressure", "LTime", cumulus.defaultRecordTS);
				ThisYear.HighPress.Val = ini.GetValue("Pressure", "High", Cumulus.DefaultHiVal);
				ThisYear.HighPress.Ts = ini.GetValue("Pressure", "HTime", cumulus.defaultRecordTS);
				// rain
				ThisYear.HighRainRate.Val = ini.GetValue("Rain", "High", Cumulus.DefaultHiVal);
				ThisYear.HighRainRate.Ts = ini.GetValue("Rain", "HTime", cumulus.defaultRecordTS);
				ThisYear.HourlyRain.Val = ini.GetValue("Rain", "HourlyHigh", Cumulus.DefaultHiVal);
				ThisYear.HourlyRain.Ts = ini.GetValue("Rain", "HHourlyTime", cumulus.defaultRecordTS);
				ThisYear.DailyRain.Val = ini.GetValue("Rain", "DailyHigh", Cumulus.DefaultHiVal);
				ThisYear.DailyRain.Ts = ini.GetValue("Rain", "HDailyTime", cumulus.defaultRecordTS);
				ThisYear.HighRain24Hours.Val = ini.GetValue("Rain", "24Hour", Cumulus.DefaultHiVal);
				ThisYear.HighRain24Hours.Ts = ini.GetValue("Rain", "24HourTime", cumulus.defaultRecordTS);
				ThisYear.MonthlyRain.Val = ini.GetValue("Rain", "MonthlyHigh", Cumulus.DefaultHiVal);
				ThisYear.MonthlyRain.Ts = ini.GetValue("Rain", "HMonthlyTime", cumulus.defaultRecordTS);
				ThisYear.LongestDryPeriod.Val = ini.GetValue("Rain", "LongestDryPeriod", Cumulus.DefaultHiVal);
				ThisYear.LongestDryPeriod.Ts = ini.GetValue("Rain", "LongestDryPeriodTime", cumulus.defaultRecordTS);
				ThisYear.LongestWetPeriod.Val = ini.GetValue("Rain", "LongestWetPeriod", Cumulus.DefaultHiVal);
				ThisYear.LongestWetPeriod.Ts = ini.GetValue("Rain", "LongestWetPeriodTime", cumulus.defaultRecordTS);
				// humidity
				ThisYear.LowHumidity.Val = ini.GetValue("Humidity", "Low", Cumulus.DefaultLoVal);
				ThisYear.LowHumidity.Ts = ini.GetValue("Humidity", "LTime", cumulus.defaultRecordTS);
				ThisYear.HighHumidity.Val = ini.GetValue("Humidity", "High", Cumulus.DefaultHiVal);
				ThisYear.HighHumidity.Ts = ini.GetValue("Humidity", "HTime", cumulus.defaultRecordTS);
				// heat index
				ThisYear.HighHeatIndex.Val = ini.GetValue("HeatIndex", "High", Cumulus.DefaultHiVal);
				ThisYear.HighHeatIndex.Ts = ini.GetValue("HeatIndex", "HTime", cumulus.defaultRecordTS);
				// App temp
				ThisYear.LowAppTemp.Val = ini.GetValue("AppTemp", "Low", Cumulus.DefaultLoVal);
				ThisYear.LowAppTemp.Ts = ini.GetValue("AppTemp", "LTime", cumulus.defaultRecordTS);
				ThisYear.HighAppTemp.Val = ini.GetValue("AppTemp", "High", Cumulus.DefaultHiVal);
				ThisYear.HighAppTemp.Ts = ini.GetValue("AppTemp", "HTime", cumulus.defaultRecordTS);
				// Dewpoint
				ThisYear.LowDewPoint.Val = ini.GetValue("Dewpoint", "Low", Cumulus.DefaultLoVal);
				ThisYear.LowDewPoint.Ts = ini.GetValue("Dewpoint", "LTime", cumulus.defaultRecordTS);
				ThisYear.HighDewPoint.Val = ini.GetValue("Dewpoint", "High", Cumulus.DefaultHiVal);
				ThisYear.HighDewPoint.Ts = ini.GetValue("Dewpoint", "HTime", cumulus.defaultRecordTS);
				// wind chill
				ThisYear.LowChill.Val = ini.GetValue("WindChill", "Low", Cumulus.DefaultLoVal);
				ThisYear.LowChill.Ts = ini.GetValue("WindChill", "LTime", cumulus.defaultRecordTS);
				// Feels like
				ThisYear.LowFeelsLike.Val = ini.GetValue("FeelsLike", "Low", Cumulus.DefaultLoVal);
				ThisYear.LowFeelsLike.Ts = ini.GetValue("FeelsLike", "LTime", cumulus.defaultRecordTS);
				ThisYear.HighFeelsLike.Val = ini.GetValue("FeelsLike", "High", Cumulus.DefaultHiVal);
				ThisYear.HighFeelsLike.Ts = ini.GetValue("FeelsLike", "HTime", cumulus.defaultRecordTS);
				// Humidex
				ThisYear.HighHumidex.Val = ini.GetValue("Humidex", "High", Cumulus.DefaultHiVal);
				ThisYear.HighHumidex.Ts = ini.GetValue("Humidex", "HTime", cumulus.defaultRecordTS);

				Cumulus.LogMessage("Year.ini file read");
			}
		}

		public void WriteYearIniFile()
		{
			lock (yearIniThreadLock)
			{
				try
				{
					int hourInc = cumulus.GetHourInc();

					IniFile ini = new IniFile(cumulus.YearIniFile);
					// Date
					ini.SetValue("General", "Date", DateTime.Now.AddHours(hourInc));
					// Wind
					ini.SetValue("Wind", "Speed", ThisYear.HighWind.Val);
					ini.SetValue("Wind", "SpTime", ThisYear.HighWind.Ts);
					ini.SetValue("Wind", "Gust", ThisYear.HighGust.Val);
					ini.SetValue("Wind", "Time", ThisYear.HighGust.Ts);
					ini.SetValue("Wind", "Windrun", ThisYear.HighWindRun.Val);
					ini.SetValue("Wind", "WindrunTime", ThisYear.HighWindRun.Ts);
					// Temperature
					ini.SetValue("Temp", "Low", ThisYear.LowTemp.Val);
					ini.SetValue("Temp", "LTime", ThisYear.LowTemp.Ts);
					ini.SetValue("Temp", "High", ThisYear.HighTemp.Val);
					ini.SetValue("Temp", "HTime", ThisYear.HighTemp.Ts);
					ini.SetValue("Temp", "LowMax", ThisYear.LowMaxTemp.Val);
					ini.SetValue("Temp", "LMTime", ThisYear.LowMaxTemp.Ts);
					ini.SetValue("Temp", "HighMin", ThisYear.HighMinTemp.Val);
					ini.SetValue("Temp", "HMTime", ThisYear.HighMinTemp.Ts);
					ini.SetValue("Temp", "LowRange", ThisYear.LowDailyTempRange.Val);
					ini.SetValue("Temp", "LowRangeTime", ThisYear.LowDailyTempRange.Ts);
					ini.SetValue("Temp", "HighRange", ThisYear.HighDailyTempRange.Val);
					ini.SetValue("Temp", "HighRangeTime", ThisYear.HighDailyTempRange.Ts);
					// Pressure
					ini.SetValue("Pressure", "Low", ThisYear.LowPress.Val);
					ini.SetValue("Pressure", "LTime", ThisYear.LowPress.Ts);
					ini.SetValue("Pressure", "High", ThisYear.HighPress.Val);
					ini.SetValue("Pressure", "HTime", ThisYear.HighPress.Ts);
					// rain
					ini.SetValue("Rain", "High", ThisYear.HighRainRate.Val);
					ini.SetValue("Rain", "HTime", ThisYear.HighRainRate.Ts);
					ini.SetValue("Rain", "HourlyHigh", ThisYear.HourlyRain.Val);
					ini.SetValue("Rain", "HHourlyTime", ThisYear.HourlyRain.Ts);
					ini.SetValue("Rain", "DailyHigh", ThisYear.DailyRain.Val);
					ini.SetValue("Rain", "HDailyTime", ThisYear.DailyRain.Ts);
					ini.SetValue("Rain", "24Hour", ThisYear.HighRain24Hours.Val);
					ini.SetValue("Rain", "24HourTime", ThisYear.HighRain24Hours.Ts);
					ini.SetValue("Rain", "MonthlyHigh", ThisYear.MonthlyRain.Val);
					ini.SetValue("Rain", "HMonthlyTime", ThisYear.MonthlyRain.Ts);
					ini.SetValue("Rain", "LongestDryPeriod", ThisYear.LongestDryPeriod.Val);
					ini.SetValue("Rain", "LongestDryPeriodTime", ThisYear.LongestDryPeriod.Ts);
					ini.SetValue("Rain", "LongestWetPeriod", ThisYear.LongestWetPeriod.Val);
					ini.SetValue("Rain", "LongestWetPeriodTime", ThisYear.LongestWetPeriod.Ts);
					// humidity
					ini.SetValue("Humidity", "Low", ThisYear.LowHumidity.Val);
					ini.SetValue("Humidity", "LTime", ThisYear.LowHumidity.Ts);
					ini.SetValue("Humidity", "High", ThisYear.HighHumidity.Val);
					ini.SetValue("Humidity", "HTime", ThisYear.HighHumidity.Ts);
					// heat index
					ini.SetValue("HeatIndex", "High", ThisYear.HighHeatIndex.Val);
					ini.SetValue("HeatIndex", "HTime", ThisYear.HighHeatIndex.Ts);
					// App temp
					ini.SetValue("AppTemp", "Low", ThisYear.LowAppTemp.Val);
					ini.SetValue("AppTemp", "LTime", ThisYear.LowAppTemp.Ts);
					ini.SetValue("AppTemp", "High", ThisYear.HighAppTemp.Val);
					ini.SetValue("AppTemp", "HTime", ThisYear.HighAppTemp.Ts);
					// Dewpoint
					ini.SetValue("Dewpoint", "Low", ThisYear.LowDewPoint.Val);
					ini.SetValue("Dewpoint", "LTime", ThisYear.LowDewPoint.Ts);
					ini.SetValue("Dewpoint", "High", ThisYear.HighDewPoint.Val);
					ini.SetValue("Dewpoint", "HTime", ThisYear.HighDewPoint.Ts);
					// wind chill
					ini.SetValue("WindChill", "Low", ThisYear.LowChill.Val);
					ini.SetValue("WindChill", "LTime", ThisYear.LowChill.Ts);
					// Feels like
					ini.SetValue("FeelsLike", "Low", ThisYear.LowFeelsLike.Val);
					ini.SetValue("FeelsLike", "LTime", ThisYear.LowFeelsLike.Ts);
					ini.SetValue("FeelsLike", "High", ThisYear.HighFeelsLike.Val);
					ini.SetValue("FeelsLike", "HTime", ThisYear.HighFeelsLike.Ts);
					// Humidex
					ini.SetValue("Humidex", "High", ThisYear.HighHumidex.Val);
					ini.SetValue("Humidex", "HTime", ThisYear.HighHumidex.Ts);

					ini.Flush();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "Error writing year.ini file");
				}
			}
		}

		public void SetDefaultYearlyHighsAndLows()
		{
			// this Year highs and lows
			ThisYear.HighGust.Val = Cumulus.DefaultHiVal;
			ThisYear.HighWind.Val = Cumulus.DefaultHiVal;
			ThisYear.HighTemp.Val = Cumulus.DefaultHiVal;
			ThisYear.LowTemp.Val = Cumulus.DefaultLoVal;
			ThisYear.HighAppTemp.Val = Cumulus.DefaultHiVal;
			ThisYear.LowAppTemp.Val = Cumulus.DefaultLoVal;
			ThisYear.HighFeelsLike.Val = Cumulus.DefaultHiVal;
			ThisYear.LowFeelsLike.Val = Cumulus.DefaultLoVal;
			ThisYear.HighHumidex.Val = Cumulus.DefaultHiVal;
			ThisYear.HighDewPoint.Val = Cumulus.DefaultHiVal;
			ThisYear.LowDewPoint.Val = Cumulus.DefaultLoVal;
			ThisYear.HighPress.Val = Cumulus.DefaultHiVal;
			ThisYear.LowPress.Val = Cumulus.DefaultLoVal;
			ThisYear.HighRainRate.Val = Cumulus.DefaultHiVal;
			ThisYear.HourlyRain.Val = Cumulus.DefaultHiVal;
			ThisYear.HighRain24Hours.Val = Cumulus.DefaultHiVal;
			ThisYear.DailyRain.Val = Cumulus.DefaultHiVal;
			ThisYear.MonthlyRain.Val = Cumulus.DefaultHiVal;
			ThisYear.HighHumidity.Val = Cumulus.DefaultHiVal;
			ThisYear.LowHumidity.Val = Cumulus.DefaultLoVal;
			ThisYear.HighHeatIndex.Val = Cumulus.DefaultHiVal;
			ThisYear.LowChill.Val = Cumulus.DefaultLoVal;
			ThisYear.HighMinTemp.Val = Cumulus.DefaultHiVal;
			ThisYear.LowMaxTemp.Val = Cumulus.DefaultLoVal;
			ThisYear.HighWindRun.Val = Cumulus.DefaultHiVal;
			ThisYear.LowDailyTempRange.Val = Cumulus.DefaultLoVal;
			ThisYear.HighDailyTempRange.Val = Cumulus.DefaultHiVal;

			// this Year highs and lows - timestamps
			ThisYear.HighGust.Ts = cumulus.defaultRecordTS;
			ThisYear.HighWind.Ts = cumulus.defaultRecordTS;
			ThisYear.HighTemp.Ts = cumulus.defaultRecordTS;
			ThisYear.LowTemp.Ts = cumulus.defaultRecordTS;
			ThisYear.HighAppTemp.Ts = cumulus.defaultRecordTS;
			ThisYear.LowAppTemp.Ts = cumulus.defaultRecordTS;
			ThisYear.HighFeelsLike.Ts = cumulus.defaultRecordTS;
			ThisYear.LowFeelsLike.Ts = cumulus.defaultRecordTS;
			ThisYear.HighHumidex.Ts = cumulus.defaultRecordTS;
			ThisYear.HighDewPoint.Ts = cumulus.defaultRecordTS;
			ThisYear.LowDewPoint.Ts = cumulus.defaultRecordTS;
			ThisYear.HighPress.Ts = cumulus.defaultRecordTS;
			ThisYear.LowPress.Ts = cumulus.defaultRecordTS;
			ThisYear.HighRainRate.Ts = cumulus.defaultRecordTS;
			ThisYear.HourlyRain.Ts = cumulus.defaultRecordTS;
			ThisYear.HighRain24Hours.Ts = cumulus.defaultRecordTS;
			ThisYear.DailyRain.Ts = cumulus.defaultRecordTS;
			ThisYear.MonthlyRain.Ts = cumulus.defaultRecordTS;
			ThisYear.HighHumidity.Ts = cumulus.defaultRecordTS;
			ThisYear.LowHumidity.Ts = cumulus.defaultRecordTS;
			ThisYear.HighHeatIndex.Ts = cumulus.defaultRecordTS;
			ThisYear.LowChill.Ts = cumulus.defaultRecordTS;
			ThisYear.HighMinTemp.Ts = cumulus.defaultRecordTS;
			ThisYear.LowMaxTemp.Ts = cumulus.defaultRecordTS;
			ThisYear.DailyRain.Ts = cumulus.defaultRecordTS;
			ThisYear.LowDailyTempRange.Ts = cumulus.defaultRecordTS;
			ThisYear.HighDailyTempRange.Ts = cumulus.defaultRecordTS;
		}

		public string PressINstr(double pressure)
		{
			return ConvertUserPressToIN(pressure).Value.ToString("F3", invNum);
		}

		public string PressPAstr(double pressure)
		{
			// return value to 0.1 hPa
			return (ConvertUserPressToMB(pressure).Value / 100).ToString("F4", invNum);
		}

		public string WindMPHStr(double wind)
		{
			var windMPH = ConvertUserWindToMPH(wind).Value;
			if (cumulus.StationOptions.RoundWindSpeed)
				windMPH = Math.Round(windMPH);

			return windMPH.ToString("F1", invNum);
		}

		public string WindMSStr(double wind)
		{
			var windMS = ConvertUserWindToMS(wind).Value;
			if (cumulus.StationOptions.RoundWindSpeed)
				windMS = Math.Round(windMS);

			return windMS.ToString("F1", invNum);
		}

		/// <summary>
		/// Convert rain in user units to inches for WU etc
		/// </summary>
		/// <param name="rain"></param>
		/// <returns></returns>
		public string RainINstr(double rain)
		{
			return ConvertUserRainToIn(rain).ToString("F2", invNum);
		}

		/// <summary>
		/// Convert rain in user units to mm for APIs etc
		/// </summary>
		/// <param name="rain"></param>
		/// <returns></returns>
		public string RainMMstr(double rain)
		{
			return ConvertUserRainToMM(rain).Value.ToString("F2", invNum);
		}

		/// <summary>
		/// Convert temp in user units to F for WU etc
		/// </summary>
		/// <param name="temp"></param>
		/// <returns></returns>
		public string TempFstr(double temp)
		{
			return ConvertUserTempToF(temp).Value.ToString("F1", invNum);
		}

		/// <summary>
		/// Convert temp in user units to C for APIs etc
		/// </summary>
		/// <param name="temp"></param>
		/// <returns></returns>
		public string TempCstr(double temp)
		{
			return ConvertUserTempToC(temp).Value.ToString("F1", invNum);
		}

		public static double ConvertUserRainToIN(double rain)
		{
			if (cumulus.Units.Rain == 0)
			{
				return rain * 0.0393700787;
			}
			else
			{
				return rain;
			}
		}

		public string GetExtraTemp()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			for (int sensor = 1; sensor < 11; sensor++)
			{
				json.Append("[\"");
				json.Append(cumulus.ExtraTempCaptions[sensor]);
				json.Append("\",\"");
				if (ExtraTemp[sensor].HasValue)
					json.Append(ExtraTemp[sensor].Value.ToString(cumulus.TempFormat));
				else
					json.Append('-');
				json.Append("\",\"&deg;");
				json.Append(cumulus.Units.TempText[1]);
				json.Append("\"],");
			}
			json.Length--;
			json.Append("]}");
			return json.ToString();
		}

		public string GetUserTemp()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			for (int sensor = 1; sensor < 9; sensor++)
			{
				json.Append("[\"");
				json.Append(cumulus.UserTempCaptions[sensor]);
				json.Append("\",\"");
				if (UserTemp[sensor].HasValue)
					json.Append(UserTemp[sensor].Value.ToString(cumulus.TempFormat));
				else
					json.Append('-');
				json.Append("\",\"&deg;");
				json.Append(cumulus.Units.TempText[1]);
				json.Append("\"]");

				if (sensor < 8)
				{
					json.Append(',');
				}
			}

			json.Append("]}");
			return json.ToString();
		}

		public string GetExtraHum()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			for (int sensor = 1; sensor < 11; sensor++)
			{
				json.Append("[\"");
				json.Append(cumulus.ExtraHumCaptions[sensor]);
				json.Append("\",\"");
				if (ExtraHum[sensor].HasValue)
					json.Append(ExtraHum[sensor].Value.ToString(cumulus.HumFormat));
				else
					json.Append('-');
				json.Append("\",\"%\"]");

				if (sensor < 10)
				{
					json.Append(',');
				}
			}

			json.Append("]}");
			return json.ToString();
		}

		public string GetExtraDew()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			for (int sensor = 1; sensor < 11; sensor++)
			{
				json.Append("[\"");
				json.Append(cumulus.ExtraDPCaptions[sensor]);
				json.Append("\",\"");
				if (ExtraDewPoint[sensor].HasValue)
					json.Append(ExtraDewPoint[sensor].Value.ToString(cumulus.TempFormat));
				else
					json.Append('-');
				json.Append("\",\"&deg;");
				json.Append(cumulus.Units.TempText[1]);
				json.Append("\"]");

				if (sensor < 10)
				{
					json.Append(',');
				}
			}

			json.Append("]}");
			return json.ToString();
		}

		public string GetSoilTemp()
		{
			var json = new StringBuilder("{\"data\":[", 2048);

			for (int i = 1; i <= 16; i++)
			{
				json.Append("[\"");
				json.Append(cumulus.SoilTempCaptions[i]);
				json.Append("\",\"");
				if (SoilTemp[i].HasValue)
					json.Append(SoilTemp[i].Value.ToString(cumulus.TempFormat));
				else
					json.Append('-');
				json.Append("\",\"&deg;");
				json.Append(cumulus.Units.TempText[1]);
				json.Append("\"],");
			}
			json.Length--;
			json.Append("]}");
			return json.ToString();
		}

		public string GetSoilMoisture()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			for (var i = 1; i < SoilMoisture.Length; i++)
			{
				json.Append($"[\"{cumulus.SoilMoistureCaptions[i]}\",\"");
				if (SoilMoisture[i].HasValue)
					json.Append(SoilMoisture[i].Value.ToString("F0"));
				else
					json.Append('-');
				json.Append($"\",\"{cumulus.Units.SoilMoistureUnitText}\"],");
			}
			json.Length--;
			json.Append("]}");
			return json.ToString();
		}

		public string GetAirQuality()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			json.Append($"[\"{cumulus.AirQualityCaptions[1]}\",\"{(AirQuality[1].HasValue ? AirQuality[1].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.AirQualityCaptions[2]}\",\"{(AirQuality[2].HasValue ? AirQuality[2].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.AirQualityCaptions[3]}\",\"{(AirQuality[3].HasValue ? AirQuality[3].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.AirQualityCaptions[4]}\",\"{(AirQuality[4].HasValue ? AirQuality[4].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.AirQualityAvgCaptions[1]}\",\"{(AirQualityAvg[1].HasValue ? AirQualityAvg[1].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.AirQualityAvgCaptions[2]}\",\"{(AirQualityAvg[2].HasValue ? AirQualityAvg[2].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.AirQualityAvgCaptions[3]}\",\"{(AirQualityAvg[3].HasValue ? AirQualityAvg[3].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.AirQualityAvgCaptions[4]}\",\"{(AirQualityAvg[4].HasValue ? AirQualityAvg[4].Value : "-"):F1}\",\"{cumulus.Units.AirQualityUnitText}\"]");
			json.Append("]}");
			return json.ToString();
		}

		public string GetCO2sensor()
		{
			var json = new StringBuilder("{\"data\":[", 1024);

			json.Append($"[\"{cumulus.CO2_CurrentCaption}\",\"{(CO2.HasValue ? CO2 : "-")}\",\"{cumulus.Units.CO2UnitText}\"],");
			json.Append($"[\"{cumulus.CO2_24HourCaption}\",\"{(CO2_24h.HasValue ? CO2_24h : "-")}\",\"{cumulus.Units.CO2UnitText}\"],");
			json.Append($"[\"{cumulus.CO2_pm2p5Caption}\",\"{(CO2_pm2p5.HasValue ? CO2_pm2p5.Value.ToString("F1") : "-")}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.CO2_pm2p5_24hrCaption}\",\"{(CO2_pm2p5_24h.HasValue ? CO2_pm2p5_24h.Value.ToString("F1") : "-")}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.CO2_pm10Caption}\",\"{(CO2_pm10.HasValue ? CO2_pm10.Value.ToString("F1") : "-")}\",\"{cumulus.Units.AirQualityUnitText}\"],");
			json.Append($"[\"{cumulus.CO2_pm10_24hrCaption}\",\"{(CO2_pm10_24h.HasValue ? CO2_pm10_24h.Value.ToString("F1") : "-")}\",\"{cumulus.Units.AirQualityUnitText}\"]");
			json.Append("]}");
			return json.ToString();
		}

		public string GetLightning()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append($"[\"Distance to last strike\",\"{(LightningDistance == -1 ? "-" : LightningDistance.ToString(cumulus.WindRunFormat))}\",\"{cumulus.Units.WindRunText}\"],");
			json.Append($"[\"Time of last strike\",\"{(DateTime.Equals(LightningTime, DateTime.MinValue) ? "-" : LightningTime.ToString("g"))}\",\"\"],");
			json.Append($"[\"Number of strikes today\",\"{LightningStrikesToday}\",\"\"]");
			json.Append("]}");
			return json.ToString();
		}

		public string GetLeaf()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append($"[\"{cumulus.LeafTempCaptions[1]}\",\"{(LeafTemp[1].HasValue ? LeafTemp[1].Value.ToString(cumulus.TempFormat) : "-")}\",\"&deg;{cumulus.Units.TempText[1]}\"],");
			json.Append($"[\"{cumulus.LeafTempCaptions[2]}\",\"{(LeafTemp[2].HasValue ? LeafTemp[2].Value.ToString(cumulus.TempFormat) : "-")}\",\"&deg;{cumulus.Units.TempText[1]}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[1]}\",\"{(LeafWetness[1].HasValue ? LeafWetness[1].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[2]}\",\"{(LeafWetness[2].HasValue ? LeafWetness[2].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"]");
			json.Append("]}");
			return json.ToString();
		}

		public string GetLeaf4()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append($"[\"{cumulus.LeafTempCaptions[1]}\",\"{(LeafTemp[1].HasValue ? LeafTemp[1].Value.ToString(cumulus.TempFormat) : "-")}\",\"&deg;{cumulus.Units.TempText[1]}\"],");
			json.Append($"[\"{cumulus.LeafTempCaptions[2]}\",\"{(LeafTemp[2].HasValue ? LeafTemp[2].Value.ToString(cumulus.TempFormat) : "-")}\",\"&deg;{cumulus.Units.TempText[1]}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[1]}\",\"{(LeafWetness[1].HasValue ? LeafWetness[1].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[2]}\",\"{(LeafWetness[2].HasValue ? LeafWetness[2].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[3]}\",\"{(LeafWetness[3].HasValue ? LeafWetness[3].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[4]}\",\"{(LeafWetness[4].HasValue ? LeafWetness[4].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"]");
			json.Append("]}");
			return json.ToString();
		}

		public string GetLeaf8()
		{
			var json = new StringBuilder("{\"data\":[", 256);

			json.Append($"[\"{cumulus.LeafTempCaptions[1]}\",\"{(LeafTemp[1].HasValue ? LeafTemp[1].Value.ToString(cumulus.TempFormat) : "-")}\",\"&deg;{cumulus.Units.TempText[1]}\"],");
			json.Append($"[\"{cumulus.LeafTempCaptions[2]}\",\"{(LeafTemp[2].HasValue ? LeafTemp[2].Value.ToString(cumulus.TempFormat) : "-")}\",\"&deg;{cumulus.Units.TempText[1]}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[1]}\",\"{(LeafWetness[1].HasValue ? LeafWetness[1].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[2]}\",\"{(LeafWetness[2].HasValue ? LeafWetness[2].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[3]}\",\"{(LeafWetness[3].HasValue ? LeafWetness[3].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[4]}\",\"{(LeafWetness[4].HasValue ? LeafWetness[4].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[5]}\",\"{(LeafWetness[5].HasValue ? LeafWetness[5].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[6]}\",\"{(LeafWetness[6].HasValue ? LeafWetness[6].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[7]}\",\"{(LeafWetness[7].HasValue ? LeafWetness[7].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"],");
			json.Append($"[\"{cumulus.LeafWetnessCaptions[8]}\",\"{(LeafWetness[8].HasValue ? LeafWetness[8].Value.ToString(cumulus.LeafWetFormat) : "-")}\",\"{cumulus.Units.LeafWetnessUnitText}\"]");
			json.Append("]}");
			return json.ToString();
		}


		public static string GetAirLinkCountsOut()
		{
			var json = new StringBuilder("{\"data\":[", 256);
			if (cumulus.airLinkOut != null)
			{
				json.Append($"[\"1 μm\",\"{cumulus.airLinkDataOut.pm1:F1}\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append($"[\"2.5 μm\",\"{cumulus.airLinkDataOut.pm2p5:F1}\",\"{cumulus.airLinkDataOut.pm2p5_1hr:F1}\",\"{cumulus.airLinkDataOut.pm2p5_3hr:F1}\",\"{cumulus.airLinkDataOut.pm2p5_24hr:F1}\",\"{cumulus.airLinkDataOut.pm2p5_nowcast:F1}\"],");
				json.Append($"[\"10 μm\",\"{cumulus.airLinkDataOut.pm10:F1}\",\"{cumulus.airLinkDataOut.pm10_1hr:F1}\",\"{cumulus.airLinkDataOut.pm10_3hr:F1}\",\"{cumulus.airLinkDataOut.pm10_24hr:F1}\",\"{cumulus.airLinkDataOut.pm10_nowcast:F1}\"]");
			}
			else
			{
				json.Append("[\"1 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append("[\"2.5 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append("[\"10 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"]");
			}
			json.Append("]}");
			return json.ToString();
		}

		public static string GetAirLinkAqiOut()
		{
			var json = new StringBuilder("{\"data\":[", 256);
			if (cumulus.airLinkOut != null)
			{
				json.Append($"[\"2.5 μm\",\"{cumulus.airLinkDataOut.aqiPm2p5:F1}\",\"{cumulus.airLinkDataOut.aqiPm2p5_1hr:F1}\",\"{cumulus.airLinkDataOut.aqiPm2p5_3hr:F1}\",\"{cumulus.airLinkDataOut.aqiPm2p5_24hr:F1}\",\"{cumulus.airLinkDataOut.aqiPm2p5_nowcast:F1}\"],");
				json.Append($"[\"10 μm\",\"{cumulus.airLinkDataOut.aqiPm10:F1}\",\"{cumulus.airLinkDataOut.aqiPm10_1hr:F1}\",\"{cumulus.airLinkDataOut.aqiPm10_3hr:F1}\",\"{cumulus.airLinkDataOut.aqiPm10_24hr:F1}\",\"{cumulus.airLinkDataOut.aqiPm10_nowcast:F1}\"]");
			}
			else
			{
				json.Append("[\"2.5 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append("[\"10 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"]");
			}
			json.Append("]}");
			return json.ToString();
		}

		public static string GetAirLinkPctOut()
		{
			var json = new StringBuilder("{\"data\":[", 256);
			if (cumulus.airLinkOut != null)
			{
				json.Append($"[\"All sizes\",\"--\",\"{cumulus.airLinkDataOut.pct_1hr}%\",\"{cumulus.airLinkDataOut.pct_3hr}%\",\"{cumulus.airLinkDataOut.pct_24hr}%\",\"{cumulus.airLinkDataOut.pct_nowcast}%\"]");
			}
			else
			{
				json.Append("[\"All sizes\",\"--\",\"--\",\"--\",\"--\",\"--\"]");
			}
			json.Append("]}");
			return json.ToString();
		}

		public static string GetAirLinkCountsIn()
		{
			var json = new StringBuilder("{\"data\":[", 256);
			if (cumulus.airLinkIn != null)
			{
				json.Append($"[\"1 μm\",\"{cumulus.airLinkDataIn.pm1:F1}\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append($"[\"2.5 μm\",\"{cumulus.airLinkDataIn.pm2p5:F1}\",\"{cumulus.airLinkDataIn.pm2p5_1hr:F1}\",\"{cumulus.airLinkDataIn.pm2p5_3hr:F1}\",\"{cumulus.airLinkDataIn.pm2p5_24hr:F1}\",\"{cumulus.airLinkDataIn.pm2p5_nowcast:F1}\"],");
				json.Append($"[\"10 μm\",\"{cumulus.airLinkDataIn.pm10:F1}\",\"{cumulus.airLinkDataIn.pm10_1hr:F1}\",\"{cumulus.airLinkDataIn.pm10_3hr:F1}\",\"{cumulus.airLinkDataIn.pm10_24hr:F1}\",\"{cumulus.airLinkDataIn.pm10_nowcast:F1}\"]");
			}
			else
			{
				json.Append("[\"1 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append("[\"2.5 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append("[\"10 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"]");
			}
			json.Append("]}");
			return json.ToString();
		}

		public static string GetAirLinkAqiIn()
		{
			var json = new StringBuilder("{\"data\":[", 256);
			if (cumulus.airLinkIn != null)
			{
				json.Append($"[\"2.5 μm\",\"{cumulus.airLinkDataIn.aqiPm2p5:F1}\",\"{cumulus.airLinkDataIn.aqiPm2p5_1hr:F1}\",\"{cumulus.airLinkDataIn.aqiPm2p5_3hr:F1}\",\"{cumulus.airLinkDataIn.aqiPm2p5_24hr:F1}\",\"{cumulus.airLinkDataIn.aqiPm2p5_nowcast:F1}\"],");
				json.Append($"[\"10 μm\",\"{cumulus.airLinkDataIn.aqiPm10:F1}\",\"{cumulus.airLinkDataIn.aqiPm10_1hr:F1}\",\"{cumulus.airLinkDataIn.aqiPm10_3hr:F1}\",\"{cumulus.airLinkDataIn.aqiPm10_24hr:F1}\",\"{cumulus.airLinkDataIn.aqiPm10_nowcast:F1}\"]");
			}
			else
			{
				json.Append("[\"2.5 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"],");
				json.Append("[\"10 μm\",\"--\",\"--\",\"--\",\"--\",\"--\"]");
			}
			json.Append("]}");
			return json.ToString();
		}

		public static string GetAirLinkPctIn()
		{
			var json = new StringBuilder("{\"data\":[", 256);
			if (cumulus.airLinkIn != null)
			{
				json.Append($"[\"All sizes\",\"--\",\"{cumulus.airLinkDataIn.pct_1hr}%\",\"{cumulus.airLinkDataIn.pct_3hr}%\",\"{cumulus.airLinkDataIn.pct_24hr}%\",\"{cumulus.airLinkDataIn.pct_nowcast}%\"]");
			}
			else
			{
				json.Append("[\"All sizes\",\"--\",\"--\",\"--\",\"--\",\"--\"]");
			}
			json.Append("]}");
			return json.ToString();
		}

		/*
		private string extrajsonformat(int item, double value, DateTime timestamp, string unit, string valueformat, string dateformat)
		{
			return "[\"" + alltimedescs[item] + "\",\"" + value.ToString(valueformat) + " " + unit + "\",\"" + timestamp.ToString(dateformat) + "\"]";
		}
		*/

		// The Today/Yesterday data is in the form:
		// Name, today value + units, today time, yesterday value + units, yesterday time
		// It's used to automatically populate a DataTables table in the browser interface
		public string GetTodayYestTemp()
		{
			var nullVal = "-";
			var nullTime = "--:--";
			var json = new StringBuilder("{\"data\":[", 2048);
			var sepStr = "\",\"";
			var closeStr = "\"],";
			var tempUnitStr = "&nbsp;&deg;" + cumulus.Units.TempText[1].ToString() + sepStr;

			json.Append("[\"High Temperature\",\"");
			json.Append(HiLoToday.HighTemp.HasValue ? HiLoToday.HighTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.HighTemp.HasValue ? HiLoToday.HighTempTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighTemp.HasValue ? HiLoYest.HighTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.HighTemp.HasValue ? HiLoYest.HighTempTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"Low Temperature\",\"");
			json.Append(HiLoToday.LowTemp.HasValue ? HiLoToday.LowTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.LowTemp.HasValue ? HiLoToday.LowTempTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.LowTemp.HasValue ? HiLoYest.LowTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.LowTemp.HasValue ? HiLoYest.LowTempTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"Temperature Range\",\"");
			json.Append(HiLoToday.TempRange.HasValue ? HiLoToday.TempRange.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append("&nbsp;\",\"");
			json.Append(HiLoYest.TempRange.HasValue ? HiLoYest.TempRange.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append("&nbsp;\"],");

			json.Append("[\"High Apparent Temperature\",\"");
			json.Append(HiLoToday.HighAppTemp.HasValue ? HiLoToday.HighAppTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.HighAppTemp.HasValue ? HiLoToday.HighAppTempTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighAppTemp.HasValue ? HiLoYest.HighAppTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.HighAppTemp.HasValue ? HiLoYest.HighAppTempTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"Low Apparent Temperature\",\"");
			json.Append(HiLoToday.LowAppTemp.HasValue ? HiLoToday.LowAppTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.LowAppTemp.HasValue ? HiLoToday.LowAppTempTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.LowAppTemp.HasValue ? HiLoYest.LowAppTemp.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.LowAppTemp.HasValue ? HiLoYest.LowAppTempTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"High Feels Like\",\"");
			json.Append(HiLoToday.HighFeelsLike.HasValue ? HiLoToday.HighFeelsLike.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.HighFeelsLike.HasValue ? HiLoToday.HighFeelsLikeTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighFeelsLike.HasValue ? HiLoYest.HighFeelsLike.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.HighFeelsLike.HasValue ? HiLoYest.HighFeelsLikeTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"Low Feels Like\",\"");
			json.Append(HiLoToday.LowFeelsLike.HasValue ? HiLoToday.LowFeelsLike.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.LowFeelsLike.HasValue ? HiLoToday.LowFeelsLikeTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.LowFeelsLike.HasValue ? HiLoYest.LowFeelsLike.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.LowFeelsLike.HasValue ? HiLoYest.LowFeelsLikeTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"High Humidex\",\"");
			json.Append(HiLoToday.HighHumidex.HasValue ? HiLoToday.HighHumidex.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append("\",\"");
			json.Append(HiLoToday.HighHumidex.HasValue ? HiLoToday.HighHumidexTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighHumidex.HasValue ? HiLoYest.HighHumidex.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append("\",\"");
			json.Append(HiLoYest.HighHumidex.HasValue ? HiLoYest.HighHumidexTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);
			json.Append("[\"High Dew Point\",\"");
			json.Append(HiLoToday.HighDewPoint.HasValue ? HiLoToday.HighDewPoint.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.HighDewPoint.HasValue ? HiLoToday.HighDewPointTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighDewPoint.HasValue ? HiLoYest.HighDewPoint.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.HighDewPoint.HasValue ? HiLoYest.HighDewPointTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"Low Dew Point\",\"");
			json.Append(HiLoToday.LowDewPoint.HasValue ? HiLoToday.LowDewPoint.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.LowDewPoint.HasValue ? HiLoToday.LowDewPointTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.LowDewPoint.HasValue ? HiLoYest.LowDewPoint.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.LowDewPoint.HasValue ? HiLoYest.LowDewPointTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"Low Wind Chill\",\"");
			json.Append(HiLoToday.LowWindChill.HasValue ? HiLoToday.LowWindChill.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.LowWindChill.HasValue ? HiLoToday.LowWindChillTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.LowWindChill.HasValue ? HiLoYest.LowWindChill.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.LowWindChill.HasValue ? HiLoYest.LowWindChillTime.ToShortTimeString() : nullTime);
			json.Append(closeStr);

			json.Append("[\"High Heat Index\",\"");
			json.Append(HiLoToday.HighHeatIndex.HasValue ? HiLoToday.HighHeatIndex.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoToday.HighHeatIndex.HasValue ? HiLoToday.HighHeatIndexTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighHeatIndex.HasValue ? HiLoYest.HighHeatIndex.Value.ToString(cumulus.TempFormat) : nullVal);
			json.Append(tempUnitStr);
			json.Append(HiLoYest.HighHeatIndex.HasValue ? HiLoYest.HighHeatIndexTime.ToShortTimeString() : nullTime);
			json.Append("\"]");

			json.Append("]}");
			return json.ToString();
		}

		public string GetTodayYestHum()
		{
			var nullVal = "-";
			var nullTime = "--:--";

			var json = new StringBuilder("{\"data\":[", 512);
			var sepStr = "\",\"";
			var unitStr = "&nbsp;%" + sepStr;

			json.Append("[\"High Humidity\",\"");
			json.Append(HiLoToday.HighHumidity.HasValue ? HiLoToday.HighHumidity.Value.ToString(cumulus.HumFormat) : nullVal);
			json.Append(unitStr);
			json.Append(HiLoToday.HighHumidity.HasValue ? HiLoToday.HighHumidityTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighHumidity.HasValue ? HiLoYest.HighHumidity.Value.ToString(cumulus.HumFormat) : nullVal);
			json.Append(unitStr);
			json.Append(HiLoYest.HighHumidity.HasValue ? HiLoYest.HighHumidityTime.ToShortTimeString() : nullTime);
			json.Append("\"],");

			json.Append("[\"Low Humidity\",\"");
			json.Append(HiLoToday.LowHumidity.HasValue ? HiLoToday.LowHumidity.Value.ToString(cumulus.HumFormat) : nullVal);
			json.Append(unitStr);
			json.Append(HiLoToday.LowHumidity.HasValue ? HiLoToday.LowHumidityTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.LowHumidity.HasValue ? HiLoYest.LowHumidity.Value.ToString(cumulus.HumFormat) : nullVal);
			json.Append(unitStr);
			json.Append(HiLoYest.LowHumidity.HasValue ? HiLoYest.LowHumidityTime.ToShortTimeString() : nullTime);
			json.Append("\"]");

			json.Append("]}");
			return json.ToString();
		}

		public string GetTodayYestRain()
		{
			var nullVal = "-";
			var nullTime = "--:--";

			var json = new StringBuilder("{\"data\":[", 512);
			var sepStr = "\",\"";
			var unitStr = "&nbsp;" + cumulus.Units.RainText;

			json.Append("[\"Total Rain\",\"");
			json.Append(RainToday.HasValue ? RainToday.Value.ToString(cumulus.RainFormat) : nullVal);
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append(sepStr);
			json.Append(RainYesterday.HasValue ? RainYesterday.Value.ToString(cumulus.RainFormat) : nullVal);
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append("\"],");

			json.Append("[\"High Rain Rate\",\"");
			json.Append(HiLoToday.HighRainRate.HasValue ? HiLoToday.HighRainRate.Value.ToString(cumulus.RainFormat) : nullVal);
			json.Append(unitStr + "/hr");
			json.Append(sepStr);
			json.Append(HiLoToday.HighRainRate.HasValue ? HiLoToday.HighRainRateTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighRainRate.HasValue ? HiLoYest.HighRainRate.Value.ToString(cumulus.RainFormat) : nullVal);
			json.Append(unitStr + "/hr");
			json.Append(sepStr);
			json.Append(HiLoYest.HighRainRate.HasValue ? HiLoYest.HighRainRateTime.ToShortTimeString() : nullTime);
			json.Append("\"],");

			json.Append("[\"High Hourly Rain\",\"");
			json.Append(HiLoToday.HighHourlyRain.ToString(cumulus.RainFormat));
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append(HiLoToday.HighHourlyRainTime.ToShortTimeString());
			json.Append(sepStr);
			json.Append(HiLoYest.HighHourlyRain.ToString(cumulus.RainFormat));
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append(HiLoYest.HighHourlyRainTime.ToShortTimeString());
			json.Append("\"]");

			json.Append("]}");
			return json.ToString();
		}

		public string GetTodayYestWind()
		{
			var nullVal = "-";
			var nullTime = "--:--";

			var json = new StringBuilder("{\"data\":[", 512);
			var sepStr = "\",\"";

			json.Append("[\"Highest Gust\",\"");
			json.Append(HiLoToday.HighGust.HasValue ? HiLoToday.HighGust.Value.ToString(cumulus.WindFormat) : nullVal);
			json.Append("&nbsp;" + cumulus.Units.WindText);
			json.Append(sepStr);
			json.Append(HiLoToday.HighGust.HasValue ? HiLoToday.HighGustTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighGust.HasValue ? HiLoYest.HighGust.Value.ToString(cumulus.WindFormat) : nullVal);
			json.Append("&nbsp;" + cumulus.Units.WindText);
			json.Append(sepStr);
			json.Append(HiLoYest.HighGust.HasValue ? HiLoYest.HighGustTime.ToShortTimeString() : nullTime);
			json.Append("\"],");

			json.Append("[\"Highest Speed\",\"");
			json.Append(HiLoToday.HighWind.HasValue ? HiLoToday.HighWind.Value.ToString(cumulus.WindAvgFormat) : nullVal);
			json.Append("&nbsp;" + cumulus.Units.WindText);
			json.Append(sepStr);
			json.Append(HiLoToday.HighWind.HasValue ? HiLoToday.HighWindTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighWind.HasValue ? HiLoYest.HighWind.Value.ToString(cumulus.WindAvgFormat) : nullVal);
			json.Append("&nbsp;" + cumulus.Units.WindText);
			json.Append(sepStr);
			json.Append(HiLoYest.HighWind.HasValue ? HiLoYest.HighWindTime.ToShortTimeString() : nullTime);
			json.Append("\"],");

			json.Append("[\"Wind Run\",\"");
			json.Append(WindRunToday.ToString(cumulus.WindRunFormat));
			json.Append("&nbsp;" + cumulus.Units.WindRunText);
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append(sepStr);
			json.Append(YesterdayWindRun.ToString(cumulus.WindRunFormat));
			json.Append("&nbsp;" + cumulus.Units.WindRunText);
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append("\"],");

			json.Append("[\"Dominant Direction\",\"");
			json.Append(DominantWindBearing.ToString("F0"));
			json.Append("&nbsp;&deg;&nbsp;" + CompassPoint(DominantWindBearing));
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append(sepStr);
			json.Append(YestDominantWindBearing.ToString("F0"));
			json.Append("&nbsp;&deg;&nbsp;" + CompassPoint(YestDominantWindBearing));
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append("\"]");

			json.Append("]}");
			return json.ToString();
		}

		public string GetTodayYestPressure()
		{
			var nullVal = "-";
			var nullTime = "--:--";

			var json = new StringBuilder("{\"data\":[", 512);
			var sepStr = "\",\"";
			var unitStr = "&nbsp;" + cumulus.Units.PressText;

			json.Append("[\"High Pressure\",\"");
			json.Append(HiLoToday.HighPress.HasValue ? HiLoToday.HighPress.Value.ToString(cumulus.PressFormat) : nullVal);
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append(HiLoToday.HighPress.HasValue ? HiLoToday.HighPressTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighPress.HasValue ? HiLoYest.HighPress.Value.ToString(cumulus.PressFormat) : nullVal);
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append(HiLoYest.HighPress.HasValue ? HiLoYest.HighPressTime.ToShortTimeString() : nullTime);
			json.Append("\"],");

			json.Append("[\"Low Pressure\",\"");
			json.Append(HiLoToday.LowPress.HasValue ? HiLoToday.LowPress.Value.ToString(cumulus.PressFormat) : nullVal);
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append(HiLoToday.LowPress.HasValue ? HiLoToday.LowPressTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.LowPress.HasValue ? HiLoYest.LowPress.Value.ToString(cumulus.PressFormat) : nullVal);
			json.Append(unitStr);
			json.Append(sepStr);
			json.Append(HiLoYest.LowPress.HasValue ? HiLoYest.LowPressTime.ToShortTimeString() : nullTime);
			json.Append("\"]");

			json.Append("]}");
			return json.ToString();
		}

		public string GetTodayYestSolar()
		{
			var nullVal = "-";
			var nullTime = "--:--";

			var json = new StringBuilder("{\"data\":[", 512);
			var sepStr = "\",\"";

			json.Append("[\"High Solar Radiation\",\"");
			json.Append(HiLoToday.HighSolar.HasValue ? HiLoToday.HighSolar.Value : nullVal);
			json.Append("&nbsp;W/m2");
			json.Append(sepStr);
			json.Append(HiLoToday.HighSolar.HasValue ? HiLoToday.HighSolarTime.ToShortTimeString() : nullTime);
			json.Append(sepStr);
			json.Append(HiLoYest.HighSolar.HasValue ? HiLoYest.HighSolar.Value : nullVal);
			json.Append("&nbsp;W/m2");
			json.Append(sepStr);
			json.Append(HiLoYest.HighSolar.HasValue ? HiLoYest.HighSolarTime.ToShortTimeString() : nullTime);
			json.Append("\"],");

			json.Append("[\"Hours of Sunshine\",\"");
			json.Append(SunshineHours.ToString(cumulus.SunFormat));
			json.Append("&nbsp;hrs");
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append(sepStr);
			json.Append(YestSunshineHours.ToString(cumulus.SunFormat));
			json.Append("&nbsp;hrs");
			json.Append(sepStr);
			json.Append("&nbsp;");
			json.Append("\"]");

			json.Append("]}");
			return json.ToString();
		}

		public string GetCachedSqlCommands(string draw, int start, int length, string search)
		{
			try
			{
				var filtered = 0;
				var thisDraw = 0;

				var json = new StringBuilder(350 * cumulus.MySqlStuff.FailedList.Count);

				json.Append("{\"data\":[");

				//var lines = File.ReadLines(cumulus.DayFile).Skip(start).Take(length);

				foreach (var rec in cumulus.MySqlStuff.FailedList)
				{
					// if we have a search string and no match, skip to next line
					if (!string.IsNullOrEmpty(search) && !rec.statement.Contains(search))
					{
						continue;
					}

					// this line either matches the search
					filtered++;

					// skip records until we get to the start entry
					if (filtered <= start)
					{
						continue;
					}

					// only send the number requested
					if (thisDraw < length)
					{
						// track the number of lines we have to return so far
						thisDraw++;

						json.Append($"[{rec.key},\"{rec.statement}\"],");
					}
					else if (string.IsNullOrEmpty(search))
					{
						// no search so we can bail out as we already know the total number of records
						break;
					}
				}

				// trim last ","
				if (thisDraw > 0)
					json.Length--;
				json.Append("],\"recordsTotal\":");
				json.Append(cumulus.MySqlStuff.FailedList.Count);
				json.Append(",\"draw\":");
				json.Append(draw);
				json.Append(",\"recordsFiltered\":");
				json.Append(string.IsNullOrEmpty(search) ? cumulus.MySqlStuff.FailedList.Count : filtered);
				json.Append('}');

				return json.ToString();

			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "GetCachedSqlCommands: Error occurred");
			}

			return "";
		}

		public static string GetUnits()
		{
			var json = new StringBuilder("{", 200);

			json.Append($"\"temp\":\"{cumulus.Units.TempText[1]}\",");
			json.Append($"\"wind\":\"{cumulus.Units.WindText}\",");
			json.Append($"\"windrun\":\"{cumulus.Units.WindRunText}\",");
			json.Append($"\"rain\":\"{cumulus.Units.RainText}\",");
			json.Append($"\"press\":\"{cumulus.Units.PressText}\",");
			json.Append($"\"soilmoisture\":\"{cumulus.Units.SoilMoistureUnitText}\",");
			json.Append($"\"co2\":\"{cumulus.Units.CO2UnitText}\",");
			json.Append($"\"leafwet\":\"{cumulus.Units.LeafWetnessUnitText}\",");
			json.Append($"\"aq\":\"{cumulus.Units.AirQualityUnitText}\"");
			json.Append('}');
			return json.ToString();
		}


		internal string GetCurrentData()
		{
			// no need to use multiplier as rose is all relative
			StringBuilder windRoseData = new StringBuilder(windcounts[0].ToString(cumulus.WindFormat, invNum), 4096);
			lock (windRoseData)
			{
				for (var i = 1; i < cumulus.NumWindRosePoints; i++)
				{
					windRoseData.Append(',');
					windRoseData.Append(windcounts[i].ToString(cumulus.WindFormat, invNum));
				}
			}
			string stormRainStart = StartOfStorm == DateTime.MinValue ? "-----" : StartOfStorm.ToString("d");

			var data = new WebSocketData(cumulus, Temperature, Humidity, TempTotalToday / tempsamplestoday, IndoorTemp, Dewpoint, WindChill, IndoorHum,
				Pressure, WindLatest, WindAverage, RecentMaxGust, WindRunToday, Bearing, AvgBearing, RainToday, RainYesterday, RainMonth, RainYear, RainRate,
				RainLastHour, HeatIndex, Humidex, ApparentTemp, temptrendval, presstrendval, HiLoToday.HighGust, HiLoToday.HighGustTime.ToString("HH:mm"), HiLoToday.HighWind,
				HiLoToday.HighGustBearing, cumulus.Units.WindText, BearingRangeFrom10, BearingRangeTo10, windRoseData.ToString(), HiLoToday.HighTemp, HiLoToday.LowTemp,
				HiLoToday.HighTempTime.ToString("HH:mm"), HiLoToday.LowTempTime.ToString("HH:mm"), HiLoToday.HighPress, HiLoToday.LowPress, HiLoToday.HighPressTime.ToString("HH:mm"),
				HiLoToday.LowPressTime.ToString("HH:mm"), HiLoToday.HighRainRate, HiLoToday.HighRainRateTime.ToString("HH:mm"), HiLoToday.HighHumidity, HiLoToday.LowHumidity,
				HiLoToday.HighHumidityTime.ToString("HH:mm"), HiLoToday.LowHumidityTime.ToString("HH:mm"), cumulus.Units.PressText, cumulus.Units.TempText, cumulus.Units.RainText,
				HiLoToday.HighDewPoint, HiLoToday.LowDewPoint, HiLoToday.HighDewPointTime.ToString("HH:mm"), HiLoToday.LowDewPointTime.ToString("HH:mm"), HiLoToday.LowWindChill,
				HiLoToday.LowWindChillTime.ToString("HH:mm"), SolarRad, HiLoToday.HighSolar, HiLoToday.HighSolarTime.ToString("HH:mm"), UV, HiLoToday.HighUv,
				HiLoToday.HighUvTime.ToString("HH:mm"), forecaststr, getTimeString(cumulus.SunRiseTime), getTimeString(cumulus.SunSetTime),
				getTimeString(cumulus.MoonRiseTime), getTimeString(cumulus.MoonSetTime), HiLoToday.HighHeatIndex, HiLoToday.HighHeatIndexTime.ToString("HH:mm"), HiLoToday.HighAppTemp,
				HiLoToday.LowAppTemp, HiLoToday.HighAppTempTime.ToString("HH:mm"), HiLoToday.LowAppTempTime.ToString("HH:mm"), CurrentSolarMax,
				AllTime.HighPress.Val, AllTime.LowPress.Val, SunshineHours, CompassPoint(DominantWindBearing), LastRainTip,
				HiLoToday.HighHourlyRain, HiLoToday.HighHourlyRainTime.ToString("HH:mm"), "F" + cumulus.Beaufort(HiLoToday.HighWind ?? 0), "F" + cumulus.Beaufort(WindAverage ?? 0),
				cumulus.BeaufortDesc(WindAverage ?? 0), LastDataReadTimestamp.ToString("HH:mm:ss"), DataStopped, StormRain, stormRainStart, CloudBase, cumulus.CloudBaseInFeet ? "ft" : "m", RainLast24Hour,
				cumulus.LowTempAlarm.Triggered, cumulus.HighTempAlarm.Triggered, cumulus.TempChangeAlarm.UpTriggered, cumulus.TempChangeAlarm.DownTriggered, cumulus.HighRainTodayAlarm.Triggered, cumulus.HighRainRateAlarm.Triggered,
				cumulus.LowPressAlarm.Triggered, cumulus.HighPressAlarm.Triggered, cumulus.PressChangeAlarm.UpTriggered, cumulus.PressChangeAlarm.DownTriggered, cumulus.HighGustAlarm.Triggered, cumulus.HighWindAlarm.Triggered,
				cumulus.SensorAlarm.Triggered, cumulus.BatteryLowAlarm.Triggered, cumulus.SpikeAlarm.Triggered, cumulus.UpgradeAlarm.Triggered, cumulus.HttpUploadAlarm.Triggered, cumulus.MySqlUploadAlarm.Triggered, cumulus.IsRainingAlarm.Triggered,
				FeelsLike, HiLoToday.HighFeelsLike, HiLoToday.HighFeelsLikeTime.ToString("HH:mm:ss"), HiLoToday.LowFeelsLike, HiLoToday.LowFeelsLikeTime.ToString("HH:mm:ss"),
				HiLoToday.HighHumidex, HiLoToday.HighHumidexTime.ToString("HH:mm:ss"));

			try
			{
				using MemoryStream stream = new MemoryStream();
				DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(WebSocketData));
				DataContractJsonSerializerSettings s = new DataContractJsonSerializerSettings();
				ds.WriteObject(stream, data);
				string jsonString = Encoding.UTF8.GetString(stream.ToArray());
				stream.Close();
				return jsonString;
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "GetCurentData: Error");
				return "";
			}

		}

		// Returns true if the gust value exceeds current RecentMaxGust, false if it fails
		public bool CheckHighGust(double? gust, int? gustdir, DateTime timestamp)
		{
			if (gust is double gustVal)
			{
				// Spike check is in m/s
				var windGustMS = ConvertUserWindToMS(gustVal).Value;
				if (((previousGust != 999) && (Math.Abs(windGustMS - previousGust) > cumulus.Spike.GustDiff)) || windGustMS >= cumulus.Limit.WindHigh)
				{
					cumulus.LogSpikeRemoval("Wind Gust difference greater than specified; reading ignored");
					cumulus.LogSpikeRemoval($"Gust: NewVal={windGustMS:F1} OldVal={previousGust:F1} SpikeGustDiff={cumulus.Spike.GustDiff:F1} HighLimit={cumulus.Limit.WindHigh:F1}");
					lastSpikeRemoval = DateTime.Now;
					cumulus.SpikeAlarm.LastError = $"Wind Gust difference greater than spike value - NewVal={windGustMS:F1}, OldVal={previousGust:F1}";
					cumulus.SpikeAlarm.Triggered = true;
					return false;
				}

				if (gustVal > (RecentMaxGust ?? 0))
				{
					if (gustVal > HiLoToday.HighGust)
					{
						HiLoToday.HighGust = gustVal;
						HiLoToday.HighGustTime = timestamp;
						HiLoToday.HighGustBearing = gustdir;
						WriteTodayFile(timestamp, false);
					}
					if (gustVal > ThisMonth.HighGust.Val)
					{
						ThisMonth.HighGust.Val = gustVal;
						ThisMonth.HighGust.Ts = timestamp;
						WriteMonthIniFile();
					}
					if (gustVal > ThisYear.HighGust.Val)
					{
						ThisYear.HighGust.Val = gustVal;
						ThisYear.HighGust.Ts = timestamp;
						WriteYearIniFile();
					}
					// All time high gust?
					if (gustVal > AllTime.HighGust.Val)
					{
						SetAlltime(AllTime.HighGust, gustVal, timestamp);
					}

					// check for monthly all time records (and set)
					CheckMonthlyAlltime("HighGust", gustVal, true, timestamp);

					cumulus.HighGustAlarm.CheckAlarm(gustVal);
				}
				return true;
			}
			return false;
		}


		internal static void LogRawStationData(string msg, bool xmt)
		{
			if (cumulus.ProgramOptions.LogRawStationData && cumulus.RawDataStation != null)
			{
				cumulus.RawDataStation.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + (xmt ? "> " : "< ") + msg);
			}
		}

		internal static void LogRawExtraData(string msg, bool xmt)
		{
			if (cumulus.ProgramOptions.LogRawExtraData && cumulus.RawDataExtraLog != null)
			{
				cumulus.RawDataExtraLog.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + (xmt ? "> " : "< ") + msg);
			}
		}

		public class CommTimer : IDisposable
		{
			public Timer tmrComm = new Timer();
			public bool timedout = false;
			public CommTimer()
			{
				timedout = false;
				tmrComm.AutoReset = false;
				tmrComm.Enabled = false;
				tmrComm.Interval = 1000; //default to 1 second
				tmrComm.Elapsed += new ElapsedEventHandler(OnTimedCommEvent);
			}

			public void OnTimedCommEvent(object source, ElapsedEventArgs e)
			{
				timedout = true;
				tmrComm.Stop();
			}

			public void Start(double timeoutperiod)
			{
				tmrComm.Interval = timeoutperiod;             //time to time out in milliseconds
				tmrComm.Stop();
				timedout = false;
				tmrComm.Start();
			}

			public void Stop()
			{
				tmrComm.Stop();
				timedout = true;
			}

			public void Dispose()
			{
				tmrComm.Close();
				tmrComm.Dispose();
				GC.SuppressFinalize(this);
			}
		}

		internal class DbDateTimeDouble
		{
			[PrimaryKey]
			public DateTime? Timestamp { get; set; }
			public double? var { get; set; }
		}

		internal class DbLongDouble
		{
			[PrimaryKey]
			public long? Timestamp { get; set; }
			public double? var { get; set; }
		}
	}


	public class Last10MinWind
	{
		public DateTime timestamp;
		public double gust;
		public double speed;
		public double gustX;
		public double gustY;

		public Last10MinWind(DateTime ts, double windgust, double windspeed, double Xgust, double Ygust)
		{
			timestamp = ts;
			gust = windgust;
			speed = windspeed;
			gustX = Xgust;
			gustY = Ygust;
		}
	}

	public class RecentDailyData
	{
		public DateTime timestamp;
		public double rain;
		public double sunhours;
		public double mintemp;
		public double maxtemp;
		public double avgtemp;

		public RecentDailyData(DateTime ts, double dailyrain, double sunhrs, double mint, double maxt, double avgt)
		{
			timestamp = ts;
			rain = dailyrain;
			sunhours = sunhrs;
			mintemp = mint;
			maxtemp = maxt;
			avgtemp = avgt;
		}
	}


	class EtData
	{
		public double? avgTemp { get; set; }
		public int? avgHum { get; set; }
		public double? avgSol { get; set; }
		public double? avgSolMax { get; set; }
		public double? avgWind { get; set; }
		public double? avgPress { get; set; }
	}

	public class SqlCache
	{
		[AutoIncrement, PrimaryKey]
		public long? key { get; set; }
		public string statement { get; set; }
	}

	public class AllTimeRecords
	{
		// Add an indexer so we can reference properties with a string
		public AllTimeRec this[string propertyName]
		{
			get
			{
				// probably faster without reflection:
				// like:  return Properties.Settings.Default.PropertyValues[propertyName]
				// instead of the following
				Type myType = typeof(AllTimeRecords);
				PropertyInfo myPropInfo = myType.GetProperty(propertyName);
				return (AllTimeRec) myPropInfo.GetValue(this, null);
			}
			set
			{
				Type myType = typeof(AllTimeRecords);
				PropertyInfo myPropInfo = myType.GetProperty(propertyName);
				myPropInfo.SetValue(this, value, null);
			}
		}

		public AllTimeRec HighTemp { get; set; } = new AllTimeRec(0);
		public AllTimeRec LowTemp { get; set; } = new AllTimeRec(1);
		public AllTimeRec HighGust { get; set; } = new AllTimeRec(2);
		public AllTimeRec HighWind { get; set; } = new AllTimeRec(3);
		public AllTimeRec LowChill { get; set; } = new AllTimeRec(4);
		public AllTimeRec HighRainRate { get; set; } = new AllTimeRec(5);
		public AllTimeRec DailyRain { get; set; } = new AllTimeRec(6);
		public AllTimeRec HourlyRain { get; set; } = new AllTimeRec(7);
		public AllTimeRec LowPress { get; set; } = new AllTimeRec(8);
		public AllTimeRec HighPress { get; set; } = new AllTimeRec(9);
		public AllTimeRec MonthlyRain { get; set; } = new AllTimeRec(10);
		public AllTimeRec HighMinTemp { get; set; } = new AllTimeRec(11);
		public AllTimeRec LowMaxTemp { get; set; } = new AllTimeRec(12);
		public AllTimeRec HighHumidity { get; set; } = new AllTimeRec(13);
		public AllTimeRec LowHumidity { get; set; } = new AllTimeRec(14);
		public AllTimeRec HighAppTemp { get; set; } = new AllTimeRec(15);
		public AllTimeRec LowAppTemp { get; set; } = new AllTimeRec(16);
		public AllTimeRec HighHeatIndex { get; set; } = new AllTimeRec(17);
		public AllTimeRec HighDewPoint { get; set; } = new AllTimeRec(18);
		public AllTimeRec LowDewPoint{ get; set; } = new AllTimeRec(19);
		public AllTimeRec HighWindRun { get; set; } = new AllTimeRec(20);
		public AllTimeRec LongestDryPeriod { get; set; } = new AllTimeRec(21);
		public AllTimeRec LongestWetPeriod { get; set; } = new AllTimeRec(22);
		public AllTimeRec HighDailyTempRange { get; set; } = new AllTimeRec(23);
		public AllTimeRec LowDailyTempRange { get; set; } = new AllTimeRec(24);
		public AllTimeRec HighFeelsLike { get; set; } = new AllTimeRec(25);
		public AllTimeRec LowFeelsLike { get; set; } = new AllTimeRec(26);
		public AllTimeRec HighHumidex { get; set; } = new AllTimeRec(27);
		public AllTimeRec HighRain24Hours { get; set; } = new AllTimeRec(28);
	}

	public class AllTimeRec
	{
		private static readonly string[] alltimedescs = new[]
		{
			"High temperature", "Low temperature", "High gust", "High wind speed", "Low wind chill", "High rain rate", "High daily rain",
			"High hourly rain", "Low pressure", "High pressure", "Highest monthly rainfall", "Highest minimum temp", "Lowest maximum temp",
			"High humidity", "Low humidity", "High apparent temp", "Low apparent temp", "High heat index", "High dew point", "Low dew point",
			"High daily windrun", "Longest dry period", "Longest wet period", "High daily temp range", "Low daily temp range",
			"High feels like", "Low feels like", "High Humidex", "High 24 hour rain"
		};
		private readonly int idx;

		public AllTimeRec(int index)
		{
			idx = index;
		}
		public double Val { get; set; }
		public DateTime Ts { get; set; }
		public string Desc
		{
			get
			{
				return alltimedescs[idx];
			}
		}
		public string GetValString(string format = "")
		{
			if (Val == -9999.0 || Val == 9999.0)
				return "-";
			else
				return Val.ToString(format);
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
