﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.Files;
using FluentFTP;
using FluentFTP.Helpers;
using Renci.SshNet;
using SQLite;
using NReco.Logging.File;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

//using MQTTnet;

namespace CumulusMX
{
	public class Cumulus
	{
		/////////////////////////////////
		/// Now derived from app properties
		public string Version;
		public string Build;
		/////////////////////////////////



		public static readonly SemaphoreSlim syncInit = new SemaphoreSlim(1);

		/*
		public enum VPRainGaugeTypes
		{
			MM = 0,
			IN = 1
		}
		*/

		/*
		public enum VPConnTypes
		{
			Serial = 0,
			TCPIP = 1
		}
		*/

		public enum PressUnits
		{
			MB,
			HPA,
			IN
		}

		public enum WindUnits
		{
			MS,
			MPH,
			KPH,
			KNOTS
		}

		public enum TempUnits
		{
			C,
			F
		}

		public enum RainUnits
		{
			MM,
			IN
		}

		/*
		public enum SolarCalcTypes
		{
			RyanStolzenbach = 0,
			Bras = 1
		}
		*/

		public enum FtpProtocols
		{
			FTP = 0,
			FTPS = 1,
			SFTP = 2
		}

		public enum PrimaryAqSensor
		{
			Undefined = -1,
			AirLinkOutdoor = 0,
			Ecowitt1 = 1,
			Ecowitt2 = 2,
			Ecowitt3 = 3,
			Ecowitt4 = 4,
			AirLinkIndoor = 5,
			EcowittCO2 = 6
		}

		private readonly string[] sshAuthenticationVals = { "password", "psk", "password_psk" };

		/*
		public struct Dataunits
		{
			public Units.Presss Units.Press;
			public Units.Winds Units.Wind;
			public tempunits tempunit;
			public rainunits rainunit;
		}
		*/

		/*
		public struct CurrentData
		{
			public double OutdoorTemperature;
			public double AvgTempToday;
			public double IndoorTemperature;
			public double OutdoorDewpoint;
			public double WindChill;
			public int IndoorHumidity;
			public int OutdoorHumidity;
			public double Pressure;
			public double WindLatest;
			public double WindAverage;
			public double Recentmaxgust;
			public double WindRunToday;
			public int Bearing;
			public int Avgbearing;
			public double RainToday;
			public double RainYesterday;
			public double RainMonth;
			public double RainYear;
			public double RainRate;
			public double RainLastHour;
			public double HeatIndex;
			public double Humidex;
			public double AppTemp;
			public double FeelsLike;
			public double TempTrend;
			public double PressTrend;
		}
		*/

		/*
		public struct HighLowData
		{
			public double TodayLow;
			public DateTime TodayLowDT;
			public double TodayHigh;
			public DateTime TodayHighDT;
			public double YesterdayLow;
			public DateTime YesterdayLowDT;
			public double YesterdayHigh;
			public DateTime YesterdayHighDT;
			public double MonthLow;
			public DateTime MonthLowDT;
			public double MonthHigh;
			public DateTime MonthHighDT;
			public double YearLow;
			public DateTime YearLowDT;
			public double YearHigh;
			public DateTime YearHighDT;
		}
		*/

		public struct TExtraFiles
		{
			public string local;
			public string remote;
			public bool process;
			public bool binary;
			public bool realtime;
			public bool endofday;
			public bool FTP;
			public bool UTF8;
		}

		//public Dataunits Units;

		public const double DefaultHiVal = -9999.0;
		public const double DefaultLoVal = 9999.0;

		public const int DayfileFields = 55;

		public const int LogFileRetries = 3;

		private WeatherStation station;

		internal DavisAirLink airLinkIn;
		public int airLinkInLsid;
		public string AirLinkInHostName;
		internal DavisAirLink airLinkOut;
		public int airLinkOutLsid;
		public string AirLinkOutHostName;

		internal HttpStationEcowitt ecowittExtra;
		internal HttpStationAmbient ambientExtra;

		public DateTime LastUpdateTime;

		public PerformanceCounter UpTime;

		private WebTags webtags;
		private TokenParser tokenParser;
		private TokenParser realtimeTokenParser;
		private readonly TokenParser customLogIntvlTokenParser = new TokenParser();
		private readonly TokenParser customLogDailyTokenParser = new TokenParser();


		private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;
		private readonly DateTimeFormatInfo invDate = CultureInfo.InvariantCulture.DateTimeFormat;

		public bool NormalRunning = false;
		private static LoggerFactory loggerFactory = new LoggerFactory();
		private ILogger FtpLoggerRT;
		private ILogger FtpLoggerIN;
		private ILogger FtpLoggerMX;

		public volatile int WebUpdating;
		public volatile bool SqlCatchingUp;

		public double WindRoseAngle { get; set; }

		public int NumWindRosePoints { get; set; }

		//public int[] WUnitFact = new[] { 1000, 2237, 3600, 1944 };
		//public int[] TUnitFact = new[] { 1000, 1800 };
		//public int[] TUnitAdd = new[] { 0, 32 };
		//public int[] PUnitFact = new[] { 1000, 1000, 2953 };
		//public int[] PressFact = new[] { 1, 1, 100 };
		//public int[] RUnitFact = new[] { 1000, 39 };

		public int[] logints = new[] { 1, 5, 10, 15, 20, 30 };

		//public int UnitMult = 1000;

		public int GraphDays = 31;

		public string NewMoon = "New Moon",
			WaxingCrescent = "Waxing Crescent",
			FirstQuarter = "First Quarter",
			WaxingGibbous = "Waxing Gibbous",
			FullMoon = "Full Moon",
			WaningGibbous = "Waning Gibbous",
			LastQuarter = "Last Quarter",
			WaningCrescent = "Waning Crescent";

		public string Calm = "Calm",
			Lightair = "Light air",
			Lightbreeze = "Light breeze",
			Gentlebreeze = "Gentle breeze",
			Moderatebreeze = "Moderate breeze",
			Freshbreeze = "Fresh breeze",
			Strongbreeze = "Strong breeze",
			Neargale = "Near gale",
			Gale = "Gale",
			Stronggale = "Strong gale",
			Storm = "Storm",
			Violentstorm = "Violent storm",
			Hurricane = "Hurricane";

		public string Risingveryrapidly = "Rising very rapidly",
			Risingquickly = "Rising quickly",
			Rising = "Rising",
			Risingslowly = "Rising slowly",
			Steady = "Steady",
			Fallingslowly = "Falling slowly",
			Falling = "Falling",
			Fallingquickly = "Falling quickly",
			Fallingveryrapidly = "Falling very rapidly";

		public string[] compassp = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };

		public string[] zForecast =
		{
			"Settled fine", "Fine weather", "Becoming fine", "Fine, becoming less settled", "Fine, possible showers", "Fairly fine, improving",
			"Fairly fine, possible showers early", "Fairly fine, showery later", "Showery early, improving", "Changeable, mending",
			"Fairly fine, showers likely", "Rather unsettled clearing later", "Unsettled, probably improving", "Showery, bright intervals",
			"Showery, becoming less settled", "Changeable, some rain", "Unsettled, short fine intervals", "Unsettled, rain later", "Unsettled, some rain",
			"Mostly very unsettled", "Occasional rain, worsening", "Rain at times, very unsettled", "Rain at frequent intervals", "Rain, very unsettled",
			"Stormy, may improve", "Stormy, much rain"
		};

		public string[] DavisForecast1 =
		{
			"FORECAST REQUIRES 3 HRS. OF RECENT DATA",
			"Mostly cloudy with little temperature change.",
			"Mostly cloudy and cooler.",
			"Clearing, cooler and windy.",
			"Clearing and cooler.",
			"Increasing clouds and cooler.",
			"Increasing clouds with little temperature change.",
			"Increasing clouds and warmer.",
			"Mostly clear for 12 to 24 hours with little temperature change.",
			"Mostly clear for 6 to 12 hours with little temperature change.",
			"Mostly clear and warmer. ", "Mostly clear for 12 to 24 hours and cooler.",
			"Mostly clear for 12 hours with little temperature change.",
			"Mostly clear with little temperature change.",
			"Mostly clear and cooler.",
			"Partially cloudy, Rain and/or snow possible or continuing.",
			"Partially cloudy, Snow possible or continuing.",
			"Partially cloudy, Rain possible or continuing.",
			"Mostly cloudy, Rain and/or snow possible or continuing.",
			"Mostly cloudy, Snow possible or continuing.",
			"Mostly cloudy, Rain possible or continuing.",
			"Mostly cloudy. ", "Partially cloudy.",
			"Mostly clear.",
			"Partly cloudy with little temperature change.",
			"Partly cloudy and cooler.",
			"Unknown forecast rule."
		};

		public string[] DavisForecast2 =
		{
			"",
			"Precipitation possible within 48 hours.",
			"Precipitation possible within 24 to 48 hours.",
			"Precipitation possible within 24 hours.",
			"Precipitation possible within 12 to 24 hours.",
			"Precipitation possible within 12 hours, possibly heavy at times.",
			"Precipitation possible within 12 hours.",
			"Precipitation possible within 6 to 12 hours. ",
			"Precipitation possible within 6 to 12 hours, possibly heavy at times.",
			"Precipitation possible and windy within 6 hours.",
			"Precipitation possible within 6 hours.",
			"Precipitation ending in 12 to 24 hours.",
			"Precipitation possibly heavy at times and ending within 12 hours.",
			"Precipitation ending within 12 hours.",
			"Precipitation ending within 6 hours.",
			"Precipitation likely, possibly heavy at times.",
			"Precipitation likely.",
			"Precipitation continuing, possibly heavy at times.",
			"Precipitation continuing."
		};

		public string[] DavisForecast3 =
		{
			"",
			"Windy with possible wind shift to the W, SW, or S.",
			"Possible wind shift to the W, SW, or S.",
			"Windy with possible wind shift to the W, NW, or N.",
			"Possible wind shift to the W, NW, or N.",
			"Windy.",
			"Increasing winds."
		};

		public int[,] DavisForecastLookup =
		{
			{14, 0, 0}, {13, 0, 0}, {12, 0, 0}, {11, 0, 0}, {13, 0, 0}, {25, 0, 0}, {24, 0, 0}, {24, 0, 0}, {10, 0, 0}, {24, 0, 0}, {24, 0, 0},
			{13, 0, 0}, {7, 2, 0}, {24, 0, 0}, {13, 0, 0}, {6, 3, 0}, {13, 0, 0}, {24, 0, 0}, {13, 0, 0}, {6, 6, 0}, {13, 0, 0}, {24, 0, 0},
			{13, 0, 0}, {7, 3, 0}, {10, 0, 6}, {24, 0, 0}, {13, 0, 0}, {7, 6, 6}, {10, 0, 6}, {7, 0, 0}, {24, 0, 0}, {13, 0, 0}, {7, 6, 6},
			{10, 0, 6}, {7, 0, 0}, {24, 0, 0}, {13, 0, 0}, {7, 6, 6}, {24, 0, 0}, {13, 0, 0}, {10, 1, 0}, {10, 0, 0}, {24, 0, 0}, {13, 0, 0},
			{6, 2, 0}, {6, 0, 0}, {24, 0, 0}, {13, 0, 0}, {7, 4, 0}, {24, 0, 0}, {13, 0, 0}, {7, 4, 5}, {24, 0, 0}, {13, 0, 0}, {7, 4, 5},
			{24, 0, 0}, {13, 0, 0}, {7, 7, 0}, {24, 0, 0}, {13, 0, 0}, {7, 7, 5}, {24, 0, 0}, {13, 0, 0}, {7, 4, 5}, {24, 0, 0}, {13, 0, 0},
			{7, 6, 0}, {24, 0, 0}, {13, 0, 0}, {7, 16, 0}, {4, 14, 0}, {24, 0, 0}, {4, 14, 0}, {13, 0, 0}, {4, 14, 0}, {25, 0, 0}, {24, 0, 0},
			{14, 0, 0}, {4, 14, 0}, {13, 0, 0}, {4, 14, 0}, {14, 0, 0}, {24, 0, 0}, {13, 0, 0}, {6, 3, 0}, {2, 18, 0}, {24, 0, 0}, {13, 0, 0},
			{2, 16, 0}, {1, 18, 0}, {1, 16, 0}, {24, 0, 0}, {13, 0, 0}, {5, 9, 0}, {6, 9, 0}, {2, 18, 6}, {24, 0, 0}, {13, 0, 0}, {2, 16, 6},
			{1, 18, 6}, {1, 16, 6}, {24, 0, 0}, {13, 0, 0}, {5, 4, 4}, {6, 4, 4}, {24, 0, 0}, {13, 0, 0}, {5, 10, 4}, {6, 10, 4}, {2, 13, 4},
			{2, 0, 4}, {1, 13, 4}, {1, 0, 4}, {2, 13, 4}, {24, 0, 0}, {13, 0, 0}, {2, 3, 4}, {1, 13, 4}, {1, 3, 4}, {3, 14, 0}, {3, 0, 0},
			{2, 14, 3}, {2, 0, 3}, {3, 0, 0}, {24, 0, 0}, {13, 0, 0}, {1, 6, 5}, {24, 0, 0}, {13, 0, 0}, {5, 5, 5}, {2, 14, 5}, {24, 0, 0},
			{13, 0, 0}, {2, 6, 5}, {2, 11, 0}, {2, 0, 0}, {2, 17, 5}, {24, 0, 0}, {13, 0, 0}, {2, 7, 5}, {1, 17, 5}, {24, 0, 0}, {13, 0, 0},
			{1, 7, 5}, {24, 0, 0}, {13, 0, 0}, {6, 5, 5}, {2, 0, 5}, {2, 17, 5}, {24, 0, 0}, {13, 0, 0}, {2, 15, 5}, {1, 17, 5}, {1, 15, 5},
			{24, 0, 0}, {13, 0, 0}, {5, 10, 5}, {6, 10, 5}, {5, 18, 3}, {24, 0, 0}, {13, 0, 0}, {2, 16, 3}, {1, 18, 3}, {1, 16, 3}, {5, 10, 3},
			{24, 0, 0}, {13, 0, 0}, {5, 10, 4}, {6, 10, 3}, {6, 10, 4}, {24, 0, 0}, {13, 0, 0}, {5, 10, 3}, {6, 10, 3}, {24, 0, 0}, {13, 0, 0},
			{5, 4, 3}, {6, 4, 3}, {2, 12, 3}, {24, 0, 0}, {13, 0, 0}, {2, 8, 3}, {1, 13, 3}, {1, 8, 3}, {2, 18, 0}, {24, 0, 0}, {13, 0, 0},
			{2, 16, 3}, {1, 18, 0}, {1, 16, 0}, {24, 0, 0}, {13, 0, 0}, {2, 5, 5}, {0, 0, 0}, {26, 0, 0}, {26, 0, 0}, {26, 0, 0}, {26, 0, 0}, {26, 0, 0}, {26, 0, 0}, {26, 0, 0}
		};

		// equivalents of Zambretti "dial window" letters A - Z
		public int[] riseOptions = { 25, 25, 25, 24, 24, 19, 16, 12, 11, 9, 8, 6, 5, 2, 1, 1, 0, 0, 0, 0, 0, 0 };
		public int[] steadyOptions = { 25, 25, 25, 25, 25, 25, 23, 23, 22, 18, 15, 13, 10, 4, 1, 1, 0, 0, 0, 0, 0, 0 };
		public int[] fallOptions = { 25, 25, 25, 25, 25, 25, 25, 25, 23, 23, 21, 20, 17, 14, 7, 3, 1, 1, 1, 0, 0, 0 };

		internal int[] FactorsOf60 = { 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60 };

		public TimeSpan AvgSpeedTime { get; set; }

		public TimeSpan PeakGustTime { get; set; }
		public TimeSpan AvgBearingTime { get; set; }

		public bool UTF8encode { get; set; }

		internal int TempDPlaces = 1;
		public string TempFormat;

		internal int WindDPlaces = 1;
		internal int WindAvgDPlaces = 1;
		public string WindFormat;
		public string WindAvgFormat;

		internal int HumDPlaces = 0;
		public string HumFormat;

		internal int AirQualityDPlaces = 1;
		public string AirQualityFormat;

		public int WindRunDPlaces = 1;
		public string WindRunFormat;

		public int RainDPlaces = 1;
		public string RainFormat;

		internal int PressDPlaces = 1;
		public string PressFormat;

		internal int SunshineDPlaces = 1;
		public string SunFormat;

		internal int UVDPlaces = 1;
		public string UVFormat;

		public string ETFormat;

		internal int LeafWetDPlaces = 0;
		public string LeafWetFormat = "F0";

		public string ComportName;
		public string DefaultComportName;

		//public string IPaddress;

		//public int TCPport;

		//public VPConnTypes VPconntype;

		public string Platform;

		public string dbfile;
		public SQLiteConnection LogDB;

		public string diaryfile;
		public SQLiteConnection DiaryDB;

		public string Datapath;

		public string ListSeparator;
		public char DirectorySeparator;

		public int RolloverHour;
		public bool Use10amInSummer;

		public decimal Latitude;
		public decimal Longitude;
		public double Altitude;

		internal int wsPort;
		internal bool DebuggingEnabled;

		public SerialPort cmprtRG11;
		public SerialPort cmprt2RG11;

		private const int DefaultWebUpdateInterval = 15;

		public int RecordSetTimeoutHrs = 24;

		private const int VP2SERIALCONNECTION = 0;
		//private const int VP2USBCONNECTION = 1;
		//private const int VP2TCPIPCONNECTION = 2;

		public string AlltimeIniFile;
		public string Alltimelogfile;
		public string MonthlyAlltimeIniFile;
		public string MonthlyAlltimeLogFile;
		//private string logFilePath;
		public string DayFileName;
		public string YesterdayFile;
		public string TodayIniFile;
		public string MonthIniFile;
		public string YearIniFile;
		//private readonly string stringsFile;
		private string backupPath;
		//private readonly string ExternaldataFile;
		public string WebTagFile;

		public bool SynchronisedWebUpdate;

		private List<string> WundList = new List<string>();
		private List<string> WindyList = new List<string>();
		private List<string> PWSList = new List<string>();
		private List<string> WOWList = new List<string>();
		private List<string> OWMList = new List<string>();

		internal string rawStationDataLogFile = "MXdiags/stationdata.log";
		internal string rawExtraDataLogFile = "MXdiags/extradata.log";

		internal DataLogger RawDataStation;
		internal DataLogger RawDataExtraLog
			;


		// Calibration settings
		/// <summary>
		/// User value calibration settings
		/// </summary>
		public Calibrations Calib = new Calibrations();

		/// <summary>
		/// User extreme limit settings
		/// </summary>
		public Limits Limit = new Limits();

		/// <summary>
		/// User spike limit settings
		/// </summary>
		public Spikes Spike = new Spikes();

		public ProgramOptionsClass ProgramOptions = new ProgramOptionsClass();

		public StationOptions StationOptions = new StationOptions();
		public FtpOptionsClass FtpOptions = new FtpOptionsClass();

		public StationUnits Units = new StationUnits();

		public DavisOptions DavisOptions = new DavisOptions();
		public FineOffsetOptions FineOffsetOptions = new FineOffsetOptions();
		public ImetOptions ImetOptions = new ImetOptions();
		public EasyWeatherOptions EwOptions = new EasyWeatherOptions();
		public WeatherFlowOptions WeatherFlowOptions = new WeatherFlowOptions();

		public GraphOptions GraphOptions = new GraphOptions();

		public SelectaChartOptions SelectaChartOptions = new SelectaChartOptions();

		public DisplayOptions DisplayOptions = new DisplayOptions();

		public ExtraDataLogOptions ExtraDataLogging = new ExtraDataLogOptions();

		public EmailSender emailer;
		public EmailSender.SmtpOptions SmtpOptions = new EmailSender.SmtpOptions();

		public SolarOptions SolarOptions = new SolarOptions();

		public string AlarmEmailPreamble;
		public string AlarmEmailSubject;
		public string AlarmFromEmail;
		public string[] AlarmDestEmail;
		public bool AlarmEmailHtml;

		public bool RealtimeIntervalEnabled; // The timer is to be started
		private int realtimeFTPRetries; // Count of failed realtime FTP attempts

		// Wunderground object
		internal ThirdParty.WebUploadWund Wund;

		// Windy.com object
		internal ThirdParty.WebUploadWindy Windy;

		// Wind Guru object
		internal ThirdParty.WebUploadWindGuru WindGuru;

		// PWS Weather object
		internal ThirdParty.WebUploadServiceBase PWS;

		// WOW object
		internal ThirdParty.WebUploadWow WOW;

		// APRS object
		internal ThirdParty.WebUploadAprs APRS;

		// Awekas object
		internal ThirdParty.WebUploadAwekas AWEKAS;

		// WeatherCloud object
		internal ThirdParty.WebUploadWCloud WCloud;

		// OpenWeatherMap object
		internal ThirdParty.WebUploadOWM OpenWeatherMap;


		// MQTT settings
		public struct MqttSettings
		{
			public string Server;
			public int Port;
			public int IpVersion;
			public bool UseTLS;
			public string Username;
			public string Password;
			public bool EnableDataUpdate;
			public string UpdateTemplate;
			public bool EnableInterval;
			public int IntervalTime;
			public string IntervalTemplate;
		}
		public MqttSettings MQTT;

		// NOAA report settings
		public NOAAconfig NOAAconf = new NOAAconfig();

		// Growing Degree Days
		public double GrowingBase1;
		public double GrowingBase2;
		public int GrowingYearStarts;
		public bool GrowingCap30C;

		public int TempSumYearStarts;
		public double TempSumBase1;
		public double TempSumBase2;

		public bool EODfilesNeedFTP;

		public bool IsOSX = false;
		public double CPUtemp = -999;

		// Alarms
		public Alarm DataStoppedAlarm = new Alarm("Data Stopped", AlarmTypes.Trigger);
		public Alarm BatteryLowAlarm = new Alarm("Battery Low", AlarmTypes.Trigger);
		public Alarm SensorAlarm = new Alarm("Sensor Data Stopped", AlarmTypes.Trigger);
		public Alarm SpikeAlarm = new Alarm("Data Spike",AlarmTypes.Trigger);
		public Alarm HighWindAlarm = new Alarm("High Wind", AlarmTypes.Above);
		public Alarm HighGustAlarm = new Alarm("High Gust", AlarmTypes.Above);
		public Alarm HighRainRateAlarm = new Alarm("High Rainfall Rate", AlarmTypes.Above);
		public Alarm HighRainTodayAlarm = new Alarm("Total Rainfall Today", AlarmTypes.Above);
		public AlarmChange PressChangeAlarm = new AlarmChange("Pressure Change");
		public Alarm HighPressAlarm = new Alarm("High Pressure", AlarmTypes.Above);
		public Alarm LowPressAlarm = new Alarm("Low Pressure", AlarmTypes.Below);
		public AlarmChange TempChangeAlarm = new AlarmChange("Temperature Change");
		public Alarm HighTempAlarm = new Alarm("High Temperature", AlarmTypes.Above);
		public Alarm LowTempAlarm = new Alarm("Low Temperature", AlarmTypes.Below);
		public Alarm UpgradeAlarm = new Alarm("Upgrade Available", AlarmTypes.Trigger);
		public Alarm HttpUploadAlarm = new Alarm("HTTP Uploads", AlarmTypes.Trigger);
		public Alarm MySqlUploadAlarm = new Alarm("MySQL Uploads", AlarmTypes.Trigger);
		public Alarm IsRainingAlarm = new Alarm("IsRaining", AlarmTypes.Trigger);


		private const double DEFAULTFCLOWPRESS = 950.0;
		private const double DEFAULTFCHIGHPRESS = 1050.0;

		private const string ForumDefault = "https://cumulus.hosiene.co.uk/";

		private const string WebcamDefault = "";

		private const string DefaultSoundFile = "alarm.mp3";
		private const string DefaultSoundFileOld = "alert.wav";

		public int RecentDataDays = 7;

		public int RealtimeInterval;

		public string ForecastNotAvailable = "Not available";

		//public WebServer httpServer;
		public MxWebSocket WebSock;


		// Custom HTTP - seconds
		private static readonly HttpClientHandler customHttpSecondsHandler = new HttpClientHandler();
		private readonly HttpClient customHttpSecondsClient = new HttpClient(customHttpSecondsHandler);
		private bool updatingCustomHttpSeconds;
		private readonly TokenParser customHttpSecondsTokenParser = new TokenParser();
		internal Timer CustomHttpSecondsTimer;
		internal bool CustomHttpSecondsEnabled;
		internal string[] CustomHttpSecondsStrings = new string[10];
		internal int CustomHttpSecondsInterval;

		// Custom HTTP - minutes
		private static readonly HttpClientHandler customHttpMinutesHandler = new HttpClientHandler();
		private readonly HttpClient customHttpMinutesClient = new HttpClient(customHttpMinutesHandler);
		private bool updatingCustomHttpMinutes;
		private readonly TokenParser customHttpMinutesTokenParser = new TokenParser();
		internal bool CustomHttpMinutesEnabled;
		internal string[] CustomHttpMinutesStrings = new string[10];
		internal int CustomHttpMinutesInterval;
		internal int CustomHttpMinutesIntervalIndex;

		// Custom HTTP - roll-over
		private static readonly HttpClientHandler customHttpRolloverHandler = new HttpClientHandler();
		private readonly HttpClient customHttpRolloverClient = new HttpClient(customHttpRolloverHandler);
		private bool updatingCustomHttpRollover;
		private readonly TokenParser customHttpRolloverTokenParser = new TokenParser();
		internal bool CustomHttpRolloverEnabled;
		internal string[] CustomHttpRolloverStrings = new string[10];

		public Thread ftpThread;
		public Thread MySqlCatchupThread;

		public string xapHeartbeat;
		public string xapsource;

		public string LatestBuild = "n/a";

		internal MySqlHander MySqlStuff;

		public AirLinkData airLinkDataIn;
		public AirLinkData airLinkDataOut;

		public CustomLogSettings[] CustomIntvlLogSettings = new CustomLogSettings[10];
		public CustomLogSettings[] CustomDailyLogSettings = new CustomLogSettings[10];

		public string[] StationDesc =
		{
			"Davis Vantage Pro",			// 0
			"Davis Vantage Pro2",			// 1
			"Oregon Scientific WMR-928",	// 2
			"Oregon Scientific WM-918",		// 3
			"EasyWeather",					// 4
			"Fine Offset",					// 5
			"LaCrosse WS2300",				// 6
			"Fine Offset with Solar",		// 7
			"Oregon Scientific WMR100",		// 8
			"Oregon Scientific WMR200",		// 9
			"Instromet",					// 10
			"Davis WLL",					// 11
			"GW1000",						// 12
			"HTTP WUnderground",			// 13
			"HTTP Ecowitt",					// 14
			"HTTP Ambient",					// 15
			"WeatherFlow Tempest",			// 16
			"Simulator"						// 17
		};

		public string[] APRSstationtype = { "DsVP", "DsVP", "WMR928", "WM918", "EW", "FO", "WS2300", "FOs", "WMR100", "WMR200", "IMET", "DsVP", "Ecow", "Unkn", "Ecow", "Ambt", "Tmpt", "Simul" };

		public string loggingfile;

		public Cumulus()
		{
			// Set up the diagnostic tracing
			loggingfile = RemoveOldDiagsFiles("MXdiags" + Path.DirectorySeparatorChar);

			Program.svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Creating main MX log file - " + loggingfile);
			Program.svcTextListener.Flush();

			// Get the build configuration
			var assemblyConfigurationAttribute = typeof(Cumulus).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
			var buildConfigurationName = assemblyConfigurationAttribute?.Configuration;

			// on Linux the default listener writes everything to the syslog :(
			// remove the default listener on Release code as well
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || buildConfigurationName == "Release")
			{
				Trace.Listeners.Remove("Default");
			}

			TextWriterTraceListener myTextListener = new TextWriterTraceListener(loggingfile, "MXlog");
			Trace.Listeners.Add(myTextListener);
			Trace.AutoFlush = true;
		}


		public void Initialise(int HTTPport, bool DebugEnabled, string startParms)
		{
			var fullVer = Assembly.GetExecutingAssembly().GetName().Version;
			Version = $"{fullVer.Major}.{fullVer.Minor}.{fullVer.Build}";
			Build = Assembly.GetExecutingAssembly().GetName().Version.Revision.ToString();

			DirectorySeparator = Path.DirectorySeparatorChar;

			AppDir = Directory.GetCurrentDirectory() + DirectorySeparator;
			WebTagFile = AppDir + "WebTags.txt";

			//b3045>, use same port for WS...  WS port = HTTPS port
			//wsPort = WSport;
			wsPort = HTTPport;

			DebuggingEnabled = DebugEnabled;

			SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());



			LogMessage(" ========================== Cumulus MX starting ==========================");

			LogMessage("Command line: " + Environment.CommandLine + " " + startParms);

			LogMessage($"Cumulus MX v.{Version} build {Build} - running 64 bit: {Environment.Is64BitProcess}");
			LogConsoleMessage($"Cumulus MX v.{Version} build {Build} - running 64 bit: {Environment.Is64BitProcess}");
			LogConsoleMessage("Working Dir: " + AppDir);

			IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

			Platform = IsOSX ? "Mac OS X" : Environment.OSVersion.Platform.ToString();

			// Set the default comport name depending on platform
			DefaultComportName = Platform[..3] == "Win" ? "COM1" : "/dev/ttyUSB0";

			LogMessage("Platform: " + Platform);

			LogMessage($"OS version: {Environment.OSVersion}, 64bit OS: {Environment.Is64BitOperatingSystem}");

			LogMessage("Running as a Service: " + Program.service);

			LogMessage("Running Elevated: " + SelfInstaller.IsElevated());

			LogMessage($"Current culture: {CultureInfo.CurrentCulture.DisplayName} [{CultureInfo.CurrentCulture.Name}]");


			// determine system uptime based on OS
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				try
				{
					// Windows enable the performance counter method
					UpTime = new PerformanceCounter("System", "System Up Time");
				}
				catch (Exception e)
				{
					LogMessage("Error: Unable to access the System Up Time performance counter. System up time will not be available");
					LogDebugMessage($"Error: {e}");
				}
			}

			// Check if all the folders required by CMX exist, if not create them
			CreateRequiredFolders();

			Datapath = "datav4" + DirectorySeparator;
			backupPath = "backup" + DirectorySeparator;
			ReportPath = "Reports" + DirectorySeparator;
			var WebPath = "web" + DirectorySeparator;

			dbfile = Datapath + "cumulusmx-v4.db";
			diaryfile = Datapath + "diary.db";

			//AlltimeFile = Datapath + "alltime.rec";
			AlltimeIniFile = Datapath + "alltime.ini";
			Alltimelogfile = Datapath + "alltimelog.txt";
			MonthlyAlltimeIniFile = Datapath + "monthlyalltime.ini";
			MonthlyAlltimeLogFile = Datapath + "monthlyalltimelog.txt";
			//logFilePath = Datapath;
			DayFileName = Datapath + "dayfile-v4.txt";
			YesterdayFile = Datapath + "yesterday.ini";
			TodayIniFile = Datapath + "today.ini";
			MonthIniFile = Datapath + "month.ini";
			YearIniFile = Datapath + "year.ini";

			//stringsFile = "strings.ini";

			// initialise the third party uploads
			Wund = new ThirdParty.WebUploadWund(this, "WUnderground");
			Windy = new ThirdParty.WebUploadWindy(this, "Windy");
			WindGuru = new ThirdParty.WebUploadWindGuru(this, "WindGuru");
			PWS = new ThirdParty.WebUploadPWS(this, "PWS");
			WOW = new ThirdParty.WebUploadWow(this, "WOW");
			APRS = new ThirdParty.WebUploadAprs(this, "APRS");
			AWEKAS = new ThirdParty.WebUploadAwekas(this, "AWEKAS");
			WCloud = new ThirdParty.WebUploadWCloud(this, "WCloud");
			OpenWeatherMap = new ThirdParty.WebUploadOWM(this, "OpenWeatherMap");

			// Set the default upload intervals for third party uploads
			Wund.DefaultInterval = 15;
			Windy.DefaultInterval = 15;
			WindGuru.DefaultInterval = 1;
			PWS.DefaultInterval = 15;
			APRS.DefaultInterval = 9;
			AWEKAS.DefaultInterval = 15 * 60;
			WCloud.DefaultInterval = 10;
			OpenWeatherMap.DefaultInterval = 15;

			MySqlStuff = new MySqlHander(this);

			StdWebFiles = new FileGenerationFtpOptions[2];
			StdWebFiles[0] = new FileGenerationFtpOptions()
			{
				TemplateFileName = WebPath + "websitedataT.json",
				LocalPath = WebPath,
				LocalFileName = "websitedata.json",
				RemoteFileName = "websitedata.json"
			};
			StdWebFiles[1] = new FileGenerationFtpOptions()
			{
				LocalPath = "",
				LocalFileName = "wxnow.txt",
				RemoteFileName = "wxnow.txt"
			};

			RealtimeFiles = new FileGenerationFtpOptions[2];
			RealtimeFiles[0] = new FileGenerationFtpOptions()
			{
				LocalFileName = "realtime.txt",
				RemoteFileName = "realtime.txt"
			};
			RealtimeFiles[1] = new FileGenerationFtpOptions()
			{
				TemplateFileName = WebPath + "realtimegaugesT.txt",
				LocalPath = WebPath,
				LocalFileName = "realtimegauges.txt",
				RemoteFileName = "realtimegauges.txt"
			};

			GraphDataFiles = new FileGenerationFtpOptions[13];
			GraphDataFiles[0] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "graphconfig.json",
				RemoteFileName = "graphconfig.json"
			};
			GraphDataFiles[1] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "availabledata.json",
				RemoteFileName = "availabledata.json"
			};
			GraphDataFiles[2] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "tempdata.json",
				RemoteFileName = "tempdata.json"
			};
			GraphDataFiles[3] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "pressdata.json",
				RemoteFileName = "pressdata.json"
			};
			GraphDataFiles[4] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "winddata.json",
				RemoteFileName = "winddata.json"
			};
			GraphDataFiles[5] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "wdirdata.json",
				RemoteFileName = "wdirdata.json"
			};
			GraphDataFiles[6] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "humdata.json",
				RemoteFileName = "humdata.json"
			};
			GraphDataFiles[7] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "raindata.json",
				RemoteFileName = "raindata.json"
			};
			GraphDataFiles[8] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "dailyrain.json",
				RemoteFileName = "dailyrain.json"
			};
			GraphDataFiles[9] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "dailytemp.json",
				RemoteFileName = "dailytemp.json"
			};
			GraphDataFiles[10] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "solardata.json",
				RemoteFileName = "solardata.json"
			};
			GraphDataFiles[11] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "sunhours.json",
				RemoteFileName = "sunhours.json"
			};
			GraphDataFiles[12] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "airquality.json",
				RemoteFileName = "airquality.json"
			};

			GraphDataEodFiles = new FileGenerationFtpOptions[8];
			GraphDataEodFiles[0] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alldailytempdata.json",
				RemoteFileName = "alldailytempdata.json"
			};
			GraphDataEodFiles[1] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alldailypressdata.json",
				RemoteFileName = "alldailypressdata.json"
			};
			GraphDataEodFiles[2] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alldailywinddata.json",
				RemoteFileName = "alldailywinddata.json"
			};
			GraphDataEodFiles[3] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alldailyhumdata.json",
				RemoteFileName = "alldailyhumdata.json"
			};
			GraphDataEodFiles[4] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alldailyraindata.json",
				RemoteFileName = "alldailyraindata.json"
			};
			GraphDataEodFiles[5] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alldailysolardata.json",
				RemoteFileName = "alldailysolardata.json"
			};
			GraphDataEodFiles[6] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alldailydegdaydata.json",
				RemoteFileName = "alldailydegdaydata.json"
			};
			GraphDataEodFiles[7] = new FileGenerationFtpOptions()
			{
				LocalPath = WebPath,
				LocalFileName = "alltempsumdata.json",
				RemoteFileName = "alltempsumdata.json"
			};

			ProgramOptions.Culture = new CultureConfig();

			for (var i = 0; i < 10; i++)
			{
				CustomIntvlLogSettings[i] = new CustomLogSettings();
				CustomDailyLogSettings[i] = new CustomLogSettings();
			}

			// initialise the alarms
			DataStoppedAlarm.cumulus = this;
			BatteryLowAlarm.cumulus = this;
			SensorAlarm.cumulus = this;
			SpikeAlarm.cumulus = this;
			HighWindAlarm.cumulus = this;
			HighWindAlarm.Units = Units.WindText;
			HighGustAlarm.cumulus = this;
			HighGustAlarm.Units = Units.WindText;
			HighRainRateAlarm.cumulus = this;
			HighRainRateAlarm.Units = Units.RainTrendText;
			HighRainTodayAlarm.cumulus = this;
			HighRainTodayAlarm.Units = Units.RainText;
			PressChangeAlarm.cumulus = this;
			PressChangeAlarm.Units = Units.PressTrendText;
			HighPressAlarm.cumulus = this;
			HighPressAlarm.Units = Units.PressText;
			LowPressAlarm.cumulus = this;
			LowPressAlarm.Units = Units.PressText;
			TempChangeAlarm.cumulus = this;
			TempChangeAlarm.Units = Units.TempTrendText;
			HighTempAlarm.cumulus = this;
			HighTempAlarm.Units = Units.TempText;
			LowTempAlarm.cumulus = this;
			LowTempAlarm.Units = Units.TempText;
			UpgradeAlarm.cumulus = this;
			HttpUploadAlarm.cumulus = this;
			MySqlUploadAlarm.cumulus = this;
			IsRainingAlarm.cumulus = this;

			// Read the configuration file
			ReadIniFile();

			ListSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

			DecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

			LogMessage("Directory separator=[" + DirectorySeparator + "] Decimal separator=[" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "] List separator=[" + CultureInfo.CurrentCulture.TextInfo.ListSeparator + "]");
			LogMessage("Date separator=[" + CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator + "] Time separator=[" + CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator + "]");

			TimeZoneInfo localZone = TimeZoneInfo.Local;
			DateTime now = DateTime.Now;

			LogMessage("Standard time zone name:   " + localZone.StandardName);
			LogMessage("Daylight saving time name: " + localZone.DaylightName);
			LogMessage("Daylight saving time? " + localZone.IsDaylightSavingTime(now));

			// Take a backup of all the data before we start proper
			BackupData(false, DateTime.Now);


			// Do we prevent more than one copy of CumulusMX running?
			try
			{
				if (!Program.appMutex.WaitOne(0, false))
				{
					if (ProgramOptions.WarnMultiple)
					{
						LogConsoleMessage("Cumulus is already running - terminating", ConsoleColor.Red);
						LogConsoleMessage("Program exit");
						LogMessage("Stop second instance: Cumulus is already running and 'Stop second instance' is enabled - terminating");
						LogMessage("Stop second instance: Program exit");
						Program.exitSystem = true;
						return;
					}
					else
					{
						LogConsoleMessage("Cumulus is already running - but 'Stop second instance' is disabled", ConsoleColor.Yellow);
						LogMessage("Stop second instance: Cumulus is already running but 'Stop second instance' is disabled - continuing");
					}
				}
				else
				{
					LogMessage("Stop second instance: No other running instances of Cumulus found");
				}
			}
			catch (AbandonedMutexException)
			{
				LogMessage("Stop second instance: Abandoned Mutex Error!");
				LogMessage("Stop second instance: Was a previous copy of Cumulus terminated from task manager, or otherwise forcibly stopped?");
				LogMessage("Stop second instance: Continuing this instance of Cumulus");
			}
			catch (Exception ex)
			{
				LogMessage("Stop second instance: Mutex Error! - " + ex);
				if (ProgramOptions.WarnMultiple)
				{
					LogMessage("Stop second instance: Terminating this instance of Cumulus");
					Program.exitSystem = true;
					return;
				}
				else
				{
					LogMessage("Stop second instance: 'Stop second instance' is disabled - continuing this instance of Cumulus");
				}
			}

			// Do we delay the start of Cumulus MX for a fixed period?
			if (ProgramOptions.StartupDelaySecs > 0)
			{
				// Check uptime
				double ts = -1;
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && UpTime != null)
				{
					// Windows enable the performance counter method
					UpTime = new PerformanceCounter("System", "System Up Time");
					UpTime.NextValue();
					ts = UpTime.NextValue();
				}
				else if (File.Exists(@"/proc/uptime"))
				{
					var text = File.ReadAllText(@"/proc/uptime");
					var strTime = text.Split(' ')[0];
					LogDebugMessage("Found /proc/uptime string: " + strTime);
					_ = double.TryParse(strTime, out ts);
				}

				// Only delay if the delay uptime is undefined (0), or the current uptime is less than the user specified max uptime to apply the delay
				LogMessage($"System uptime = {ts:F0} secs");
				if (ProgramOptions.StartupDelayMaxUptime == 0 || (ts > -1 && ProgramOptions.StartupDelayMaxUptime > ts))
				{
					var msg1 = $"Delaying start for {ProgramOptions.StartupDelaySecs} seconds";
					var msg2 = $"Start-up delay complete, continuing...";
					LogConsoleMessage(msg1);
					LogMessage(msg1);
					Thread.Sleep(ProgramOptions.StartupDelaySecs * 1000);
					LogConsoleMessage(msg2);
					LogMessage(msg2);
				}
				else
				{
					LogMessage("No start-up delay, max uptime exceeded, or no uptime available");
				}
			}
			else
			{
				LogMessage("No start-up delay - disabled");
			}

			// Do we wait for a ping response from a remote host before starting?
			if (!string.IsNullOrWhiteSpace(ProgramOptions.StartupPingHost))
			{
				var msg0 = $"Sending PING to {ProgramOptions.StartupPingHost}";
				var msg1 = $"Waiting for PING reply from {ProgramOptions.StartupPingHost}";
				var msg2 = $"Received PING response from {ProgramOptions.StartupPingHost}, continuing...";
				var msg3 = $"No PING response received in {ProgramOptions.StartupPingEscapeTime} minutes, continuing anyway";
				var escapeTime = DateTime.Now.AddMinutes(ProgramOptions.StartupPingEscapeTime);
				var attempt = 1;
				var pingSuccess = false;
				// This is the timeout for "hung" attempts, we will double this at every failure so we do not create too many hung resources
				var pingTimeoutSecs = 10;

				do
				{
					var pingTimeoutDT = DateTime.Now.AddSeconds(pingTimeoutSecs);
					var pingTokenSource = new CancellationTokenSource();
					var pingCancelToken = pingTokenSource.Token;

					LogDebugMessage($"Starting PING #{attempt} task with time-out of {pingTimeoutSecs} seconds");

					var pingTask = Task.Run(() =>
					{
						var cnt = attempt;

						using var ping = new Ping();
						try
						{
							LogMessage($"Sending PING #{cnt} to {ProgramOptions.StartupPingHost}");

							// set the actual ping timeout 5 secs less than the task timeout
							var reply = ping.Send(ProgramOptions.StartupPingHost, (pingTimeoutSecs - 5) * 1000);

							// were we hung on the network and now cancelled? if so just exit silently
							if (pingCancelToken.IsCancellationRequested)
							{
								LogDebugMessage($"Cancelled PING #{cnt} task exiting");
							}
							else
							{
								var msg = $"Received PING #{cnt} response from {ProgramOptions.StartupPingHost}, status: {reply.Status}";

								LogMessage(msg);
								LogConsoleMessage(msg);

								if (reply.Status == IPStatus.Success)
								{
									pingSuccess = true;
								}
							}
						}
						catch (Exception e)
						{
							LogMessage($"PING #{cnt} to {ProgramOptions.StartupPingHost} failed with error: {e.InnerException.Message}");
						}
					}, pingCancelToken);

					// wait for the ping to return
					do
					{
						Thread.Sleep(100);
					} while (pingTask.Status == TaskStatus.Running && DateTime.Now < pingTimeoutDT);

					LogDebugMessage($"PING #{attempt} task status: {pingTask.Status}");

					// did we timeout waiting for the task to end?
					if (DateTime.Now >= pingTimeoutDT)
					{
						// yep, so attempt to cancel the task
						LogMessage($"Nothing returned from PING #{attempt}, attempting the cancel the task");
						pingTokenSource.Cancel();
						// and double the timeout for next attempt
						pingTimeoutSecs *= 2;
					}

					if (!pingSuccess)
					{
						// no response wait 10 seconds before trying again
						LogDebugMessage("Waiting 10 seconds before retry...");
						Thread.Sleep(10000);
						attempt++;
						// Force a DNS refresh if not an IPv4 address
						if (!Utils.ValidateIPv4(ProgramOptions.StartupPingHost))
						{
							// catch and ignore IPv6 and invalid host name for now
							try
							{
								Dns.GetHostEntry(ProgramOptions.StartupPingHost);
							}
							catch (Exception ex)
							{
								LogMessage($"PING #{attempt}: Error with DNS refresh - {ex.Message}");
							}
						}
					}
				} while (!pingSuccess && DateTime.Now < escapeTime);

				if (DateTime.Now >= escapeTime)
				{
					LogConsoleMessage(msg3, ConsoleColor.Yellow);
					LogMessage(msg3);
				}
				else
				{
					LogConsoleMessage(msg2);
					LogMessage(msg2);
				}
			}
			else
			{
				LogMessage("No start-up PING");
			}

			// do we have a start-up task to run?
			if (!string.IsNullOrEmpty(ProgramOptions.StartupTask))
			{
				LogMessage($"Running start-up task: {ProgramOptions.StartupTask}, arguments: {ProgramOptions.StartupTaskParams}, wait: {ProgramOptions.StartupTaskWait}");
				try
				{
					Utils.RunExternalTask(ProgramOptions.StartupTask, ProgramOptions.StartupTaskParams, ProgramOptions.StartupTaskWait);
				}
				catch (Exception ex)
				{
					LogMessage($"Error running start-up task: {ex.Message}");
				}
			}

			SetupFtpLogging();

			GC.Collect();

			LogMessage("Data path = " + Datapath);

			AppDomain.CurrentDomain.SetData("DataDirectory", Datapath);

			// Open database (create file if it doesn't exist)
			SQLiteOpenFlags flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite;

			// Open diary database (create file if it doesn't exist)
			//DiaryDB = new SQLiteConnection(diaryfile, flags, true);  // We should be using this - storing datetime as ticks, but historically string storage has been used, so we are stuck with it?
			DiaryDB = new SQLiteConnection(diaryfile, flags, false);
			DiaryDB.CreateTable<DiaryDataEditor.DiaryData>();

			try
			{
				// clean-up the diary db, change any entries to use date+time to just use date
				// first see if there will be any days with more than one record
				var duplicates = DiaryDB.Query<DiaryDataEditor.DiaryData>("SELECT * FROM DiaryData WHERE rowid < (SELECT max(rowid) FROM DiaryData d2 WHERE date(DiaryData.Timestamp) = date(d2.Timestamp))");
				if (duplicates.Count > 0)
				{
					LogConsoleMessage($"WARNING: Duplicate entries ({duplicates.Count}) found in your Weather Diary database - please see log file for details");
					LogMessage($"Duplicate entries ({duplicates.Count}) found in the Weather Diary database. The following entries will be removed...");

					foreach (var rec in duplicates)
					{
						LogMessage($"  Date: {rec.Timestamp.Date}, Falling: {rec.snowFalling}, Lying: {rec.snowLying}, Depth: {rec.snowDepth}, Entry: '{rec.entry}'");
					}

					// Remove the duplicates, leave the latest, remove the oldest
					var deleted = DiaryDB.Execute("DELETE FROM DiaryData WHERE rowid < (SELECT max(rowid) FROM DiaryData d2 WHERE date(DiaryData.Timestamp) = date(d2.Timestamp))");
					if (deleted > 0)
					{
						LogMessage($"{deleted} duplicate records deleted from the weather diary database");
					}
				}
				// Now reset the now unique-by-day records to have a time of 00:00:00
				DiaryDB.Execute("UPDATE DiaryData SET Timestamp = datetime(date(TimeStamp)) WHERE time(Timestamp) <> '00:00:00'");
			}
			catch (Exception ex)
			{
				LogErrorMessage("Error cleaning up the Diary DB, exception = " + ex.Message);
			}

			LogMessage("Debug logging :" + (ProgramOptions.DebugLogging ? "enabled" : "disabled"));
			LogMessage("Data logging  :" + (ProgramOptions.DataLogging ? "enabled" : "disabled"));
			LogMessage("FTP logging   :" + (FtpOptions.Logging ? "enabled" : "disabled"));
			LogMessage("Email logging :" + (SmtpOptions.Logging ? "enabled" : "disabled"));
			LogMessage("Spike logging :" + (ErrorLogSpikeRemoval ? "enabled" : "disabled"));
			LogMessage("Logging interval = " + logints[DataLogInterval] + " mins");
			LogMessage("Real time interval = " + RealtimeInterval / 1000 + " secs");
			LogMessage("NoSensorCheck = " + (StationOptions.NoSensorCheck ? "1" : "0"));

			TempFormat = "F" + TempDPlaces;
			WindFormat = "F" + WindDPlaces;
			WindAvgFormat = "F" + WindAvgDPlaces;
			RainFormat = "F" + RainDPlaces;
			PressFormat = "F" + PressDPlaces;
			HumFormat = "F" + HumDPlaces;
			UVFormat = "F" + UVDPlaces;
			SunFormat = "F" + SunshineDPlaces;
			ETFormat = "F" + (RainDPlaces + 1);
			WindRunFormat = "F" + WindRunDPlaces;
			TempTrendFormat = "+0.0;-0.0;0";
			AirQualityFormat = "F" + AirQualityDPlaces;

			ReadStringsFile();

			SetUpHttpProxy();

			CustomHttpSecondsTimer = new Timer { Interval = CustomHttpSecondsInterval * 1000 };
			CustomHttpSecondsTimer.Elapsed += CustomHttpSecondsTimerTick;
			CustomHttpSecondsTimer.AutoReset = true;

			customHttpSecondsTokenParser.OnToken += TokenParserOnToken;
			customHttpMinutesTokenParser.OnToken += TokenParserOnToken;
			customHttpRolloverTokenParser.OnToken += TokenParserOnToken;

			customLogIntvlTokenParser.OnToken += TokenParserOnToken;
			customLogDailyTokenParser.OnToken += TokenParserOnToken;

			if (SmtpOptions.Enabled)
			{
				emailer = new EmailSender(this);
			}

			DoSunriseAndSunset();
			DoMoonPhase();
			MoonAge = MoonriseMoonset.MoonAge();
			DoMoonImage();

			LogMessage("Station type: " + (StationType == -1 ? "Undefined" : StationDesc[StationType]));

			SetupUnitText();

			LogMessage($"WindUnit={Units.WindText} RainUnit={Units.RainText} TempUnit={Units.TempText} PressureUnit={Units.PressText}");
			LogMessage($"YTDRain={YTDrain:F3} Year={YTDrainyear}");
			LogMessage($"RainDayThreshold={RainDayThreshold:F3}");
			LogMessage($"Roll over hour={RolloverHour}");

			LogOffsetsMultipliers();

			LogPrimaryAqSensor();

			// Set the alarm units
			HighWindAlarm.Units = Units.WindText;
			HighGustAlarm.Units = Units.WindText;
			HighRainRateAlarm.Units = Units.RainTrendText;
			HighRainTodayAlarm.Units = Units.RainText;
			PressChangeAlarm.Units = Units.PressTrendText;
			HighPressAlarm.Units = Units.PressText;
			LowPressAlarm.Units = Units.PressText;
			TempChangeAlarm.Units = Units.TempTrendText;
			HighTempAlarm.Units = Units.TempText;
			LowTempAlarm.Units = Units.TempText;

			_ = GetLatestVersion(); // do not wait for this

			LogMessage("Cumulus Starting");

			// switch off logging from Unosquare.Swan which underlies embedIO
			Swan.Logging.Logger.NoLogging();


			var assemblyPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
			var htmlRootPath = Path.Combine(assemblyPath, "interface");

			LogMessage("HTML root path = " + htmlRootPath);

			WebSock = new MxWebSocket("/ws/", this);

			WebServer httpServer = new WebServer(o => o
					.WithUrlPrefix($"http://*:{HTTPport}/")
					.WithMode(HttpListenerMode.EmbedIO)
				)
				.WithWebApi("/api/", m => m
					.WithController<Api.EditController>()
					.WithController<Api.DataController>()
					.WithController<Api.TagController>()
					.WithController<Api.GraphDataController>()
					.WithController<Api.RecordsController>()
					.WithController<Api.TodayYestDataController>()
					.WithController<Api.ExtraDataController>()
					.WithController<Api.SettingsController>()
					.WithController<Api.ReportsController>()
					.WithController<Api.UtilsController>()
				)
				.WithWebApi("/station", m => m
					.WithController<HttpStations.HttpStation>()
				)
				.WithModule(WebSock)
				.WithStaticFolder("/", htmlRootPath, true, m => m
					.WithoutContentCaching()
				);

			// Set up the API web server
			// Some APi functions require the station, so set them after station initialisation
			Api.programSettings = new ProgramSettings(this);
			Api.stationSettings = new StationSettings(this);
			Api.internetSettings = new InternetSettings(this);
			Api.dataLoggingSettings = new DataLoggingSettings(this);
			Api.thirdpartySettings = new ThirdPartySettings(this);
			Api.extraSensorSettings = new ExtraSensorSettings(this);
			Api.calibrationSettings = new CalibrationSettings(this);
			Api.noaaSettings = new NOAASettings(this);
			Api.alarmSettings = new AlarmSettings(this);
			Api.mySqlSettings = new MysqlSettings(this);
			Api.customLogs = new CustomLogsSettings(this);
			Api.dataEditor = new DataEditor(this);
			Api.logfileEditor = new DataEditors(this);
			Api.tagProcessor = new ApiTagProcessor(this);
			Api.wizard = new Wizard(this);

			// Don't wait for the web server
			_ = httpServer.RunAsync();

			// get the local v4 IP addresses
			Console.WriteLine();
			var ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
			LogMessage($"Cumulus running at: http://localhost:{HTTPport}/");
			Console.Write("Cumulus running at: ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"http://localhost:{HTTPport}/");
			Console.ResetColor();
			foreach (var ip in ips)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					LogConsoleMessage($"                    http://{ip}:{HTTPport}/", ConsoleColor.Yellow);
				}

			}

			Console.WriteLine();
			if (File.Exists("Cumulus.ini"))
			{
				LogConsoleMessage("  Open the admin interface by entering one of the above URLs into a web browser.", ConsoleColor.Cyan);
			}
			else
			{
				LogConsoleMessage("  Leave this window open, then...", ConsoleColor.Cyan);
				LogConsoleMessage("  Run the First Time Configuration Wizard by entering one of the URLs above plus \"wizard.html\" into your browser", ConsoleColor.Cyan);
				LogConsoleMessage($"  e.g. http://localhost:{HTTPport}/wizard.html", ConsoleColor.Cyan);
			}
			Console.WriteLine();

			//LogDebugMessage("Lock: Cumulus waiting for the lock");
			syncInit.Wait();
			//LogDebugMessage("Lock: Cumulus has lock");

			LogMessage("Opening station");

			switch (StationType)
			{
				case StationTypes.FineOffset:
				case StationTypes.FineOffsetSolar:
					Manufacturer = EW;
					station = new FOStation(this);
					break;
				case StationTypes.VantagePro:
				case StationTypes.VantagePro2:
					Manufacturer = DAVIS;
					station = new DavisStation(this);
					break;
				case StationTypes.WMR928:
					Manufacturer = OREGON;
					station = new WMR928Station(this);
					break;
				case StationTypes.WM918:
					Manufacturer = OREGON;
					station = new WM918Station(this);
					break;
				case StationTypes.WS2300:
					Manufacturer = LACROSSE;
					station = new WS2300Station(this);
					break;
				case StationTypes.WMR200:
					Manufacturer = OREGONUSB;
					station = new WMR200Station(this);
					break;
				case StationTypes.Instromet:
					Manufacturer = INSTROMET;
					station = new ImetStation(this);
					break;
				case StationTypes.WMR100:
					Manufacturer = OREGONUSB;
					station = new WMR100Station(this);
					break;
				case StationTypes.EasyWeather:
					Manufacturer = EW;
					station = new EasyWeather(this);
					station.LoadLastHoursFromDataLogs(DateTime.Now);
					break;
				case StationTypes.WLL:
					Manufacturer = DAVIS;
					station = new DavisWllStation(this);
					break;
				case StationTypes.GW1000:
					Manufacturer = ECOWITT;
					station = new GW1000Station(this);
					break;
				case StationTypes.Tempest:
					Manufacturer = WEATHERFLOW;
					station = new TempestStation(this);
					break;
				case StationTypes.HttpWund:
					Manufacturer = HTTPSTATION;
					station = new HttpStationWund(this);
					break;
				case StationTypes.HttpEcowitt:
					Manufacturer = ECOWITT;
					station = new HttpStationEcowitt(this);
					break;
				case StationTypes.HttpAmbient:
					Manufacturer = AMBIENT;
					station = new HttpStationAmbient(this);
					break;
				case StationTypes.Simulator:
					Manufacturer = SIMULATOR;
					station = new Simulator(this);
					break;
				default:
					LogConsoleMessage("Station type not set", ConsoleColor.Red);
					LogMessage("Station type not set");
					break;
			}

			if (station != null)
			{
				Api.Station = station;
				Api.stationSettings.SetStation(station);
				Api.dataEditor.SetStation(station);
				Api.logfileEditor.SetStation(station);
				Api.RecordsJson = new RecordsData(this, station);

				if (StationType == StationTypes.HttpWund)
				{
					HttpStations.stationWund = (HttpStationWund)station;
				}
				else if (StationType == StationTypes.HttpEcowitt)
				{
					HttpStations.stationEcowitt = (HttpStationEcowitt)station;
				}
				else if (StationType == StationTypes.HttpAmbient)
				{
					HttpStations.stationAmbient = (HttpStationAmbient)station;
				}

				LogMessage("Creating extra sensors");
				if (AirLinkInEnabled)
				{
					airLinkDataIn = new AirLinkData();
					airLinkIn = new DavisAirLink(this, true, station);
				}
				if (AirLinkOutEnabled)
				{
					airLinkDataOut = new AirLinkData();
					airLinkOut = new DavisAirLink(this, false, station);
				}
				if (EcowittSettings.ExtraEnabled)
				{
					ecowittExtra = new HttpStationEcowitt(this, station);
					HttpStations.stationEcowittExtra = ecowittExtra;
				}
				if (AmbientExtraEnabled)
				{
					ambientExtra = new HttpStationAmbient(this, station);
					HttpStations.stationAmbientExtra = ambientExtra;
				}

				// set the third party upload station
				Wund.station = station;
				Windy.station = station;
				WindGuru.station = station;
				PWS.station = station;
				WOW.station = station;
				APRS.station = station;
				AWEKAS.station = station;
				WCloud.station = station;
				OpenWeatherMap.station = station;

				MySqlStuff.InitialConfig(station);


				webtags = new WebTags(this, station);
				webtags.InitialiseWebtags();

				Api.tagProcessor.SetWebTags(webtags);

				tokenParser = new TokenParser();
				tokenParser.OnToken += TokenParserOnToken;

				realtimeTokenParser = new TokenParser();
				realtimeTokenParser.OnToken += TokenParserOnToken;

				RealtimeTimer.Interval = RealtimeInterval;
				RealtimeTimer.Elapsed += RealtimeTimerTick;
				RealtimeTimer.AutoReset = true;

				SetFtpLogging(FtpOptions.Logging);

				WebTimer.Elapsed += WebTimerTick;

				xapsource = "sanday.cumulus." + Environment.MachineName;

				xapHeartbeat = "xap-hbeat\n{\nv=12\nhop=1\nuid=FF" + xapUID + "00\nclass=xap-hbeat.alive\nsource=" + xapsource + "\ninterval=60\n}";

				if (xapEnabled)
				{
					Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, xapPort);

					byte[] data = Encoding.ASCII.GetBytes(xapHeartbeat);

					sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
					sock.SendTo(data, iep1);
					sock.Close();
				}

				if (MQTT.EnableDataUpdate || MQTT.EnableInterval)
				{
					MqttPublisher.Setup(this);

					if (MQTT.EnableInterval)
					{
						MQTTTimer.Elapsed += MQTTTimerTick;
					}
				}

				InitialiseRG11();

				// do any history catch-up or other work required before starting for real
				station.DoStartup();

				if (station.timerStartNeeded)
				{
					StartTimersAndSensors();
				}

				if ((StationType == StationTypes.WMR100) || (StationType == StationTypes.EasyWeather) || (Manufacturer == OREGON))
				{
					station.StartLoop();
				}

				// let the web socket know about the station
				WebSock.SetStation = station;

				// If enabled generate the daily graph data files, and upload at first opportunity
				LogDebugMessage("Generating the daily graph data files");
				station.Graphs.CreateEodGraphDataFiles();
			}

			//LogDebugMessage("Lock: Cumulus releasing the lock");
			syncInit.Release();
		}

		internal void SetUpHttpProxy()
		{
			if (!string.IsNullOrEmpty(HTTPProxyName))
			{
				var proxy = new WebProxy(HTTPProxyName, HTTPProxyPort);

				Wund.httpHandler.Proxy = proxy;
				Wund.httpHandler.UseProxy = true;

				PWS.httpHandler.Proxy = proxy;
				PWS.httpHandler.UseProxy = true;

				WOW.httpHandler.Proxy = proxy;
				WOW.httpHandler.UseProxy = true;

				AWEKAS.httpHandler.Proxy = proxy;
				AWEKAS.httpHandler.UseProxy = true;

				Windy.httpHandler.Proxy = proxy;
				Windy.httpHandler.UseProxy = true;

				WCloud.httpHandler.Proxy = proxy;
				WCloud.httpHandler.UseProxy = true;

				customHttpSecondsHandler.Proxy = proxy;
				customHttpSecondsHandler.UseProxy = true;

				customHttpMinutesHandler.Proxy = proxy;
				customHttpMinutesHandler.UseProxy = true;

				customHttpRolloverHandler.Proxy = proxy;
				customHttpRolloverHandler.UseProxy = true;

				if (!string.IsNullOrEmpty(HTTPProxyUser))
				{
					var creds = new NetworkCredential(HTTPProxyUser, HTTPProxyPassword);

					Wund.httpHandler.Credentials = creds;
					PWS.httpHandler.Credentials = creds;
					WOW.httpHandler.Credentials = creds;
					AWEKAS.httpHandler.Credentials = creds;
					Windy.httpHandler.Credentials = creds;
					WCloud.httpHandler.Credentials = creds;
					customHttpSecondsHandler.Credentials = creds;
					customHttpMinutesHandler.Credentials = creds;
					customHttpRolloverHandler.Credentials = creds;
				}
			}
		}

		private void CustomHttpSecondsTimerTick(object sender, ElapsedEventArgs e)
		{
			if (!station.DataStopped)
				_ = CustomHttpSecondsUpdate();
		}


		internal void SetupUnitText()
		{
			switch (Units.Temp)
			{
				case 0:
					Units.TempText = "°C";
					Units.TempTrendText = "°C/hr";
					break;
				case 1:
					Units.TempText = "°F";
					Units.TempTrendText = "°F/hr";
					break;
			}

			switch (Units.Rain)
			{
				case 0:
					Units.RainText = "mm";
					Units.RainTrendText = "mm/hr";
					break;
				case 1:
					Units.RainText = "in";
					Units.RainTrendText = "in/hr";
					break;
			}

			switch (Units.Press)
			{
				case 0:
					Units.PressText = "mb";
					Units.PressTrendText = "mb/hr";
					break;
				case 1:
					Units.PressText = "hPa";
					Units.PressTrendText = "hPa/hr";
					break;
				case 2:
					Units.PressText = "in";
					Units.PressTrendText = "in/hr";
					break;
			}

			switch (Units.Wind)
			{
				case 0:
					Units.WindText = "m/s";
					Units.WindRunText = "km";
					break;
				case 1:
					Units.WindText = "mph";
					Units.WindRunText = "miles";
					break;
				case 2:
					Units.WindText = "km/h";
					Units.WindRunText = "km";
					break;
				case 3:
					Units.WindText = "kts";
					Units.WindRunText = "nm";
					break;
			}
		}

		// If the temperature units are changed, reset NOAA thresholds to defaults
		internal void ChangeTempUnits()
		{
			SetupUnitText();

			NOAAconf.HeatThreshold = Units.Temp == 0 ? 18.3 : 65;
			NOAAconf.CoolThreshold = Units.Temp == 0 ? 18.3 : 65;
			NOAAconf.MaxTempComp1 = Units.Temp == 0 ? 27 : 80;
			NOAAconf.MaxTempComp2 = Units.Temp == 0 ? 0 : 32;
			NOAAconf.MinTempComp1 = Units.Temp == 0 ? 0 : 32;
			NOAAconf.MinTempComp2 = Units.Temp == 0 ? -18 : 0;

			ChillHourThreshold = Units.Temp == 0 ? 7 : 45;

			GrowingBase1 = Units.Temp == 0 ? 5.0 : 40.0;
			GrowingBase2 = Units.Temp == 0 ? 10.0 : 50.0;

			TempChangeAlarm.Units = Units.TempTrendText;
			HighTempAlarm.Units = Units.TempText;
			LowTempAlarm.Units = Units.TempText;
		}

		internal void ChangeRainUnits()
		{
			SetupUnitText();

			NOAAconf.RainComp1 = Units.Rain == 0 ? 0.2 : 0.01;
			NOAAconf.RainComp2 = Units.Rain == 0 ? 2 : 0.1;
			NOAAconf.RainComp3 = Units.Rain == 0 ? 20 : 1;

			HighRainRateAlarm.Units = Units.RainTrendText;
			HighRainTodayAlarm.Units = Units.RainText;
		}

		internal void ChangePressureUnits()
		{
			SetupUnitText();

			FCPressureThreshold = Units.Press == 2 ? 0.00295333727 : 0.1;

			PressChangeAlarm.Units = Units.PressTrendText;
			HighPressAlarm.Units = Units.PressText;
			LowPressAlarm.Units = Units.PressText;
		}

		internal void ChangeWindUnits()
		{
			SetupUnitText();

			HighWindAlarm.Units = Units.WindText;
			HighGustAlarm.Units = Units.WindText;
		}

		public void SetFtpLogging(bool isSet)
		{
			if (RealtimeFTP == null || RealtimeFTP.IsDisposed)
				return;

			if (isSet)
			{
				RealtimeFTP.Logger = FtpLoggerRT;
			}
			else
			{
				RealtimeFTP.Logger = null;
			}
		}


		/*
		private string LocalIPAddress()
		{
			IPHostEntry host;
			string localIP = "";
			host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					localIP = ip.ToString();
					break;
				}
			}
			return localIP;
		}
		*/

		/*
		private void OnDisconnect(UserContext context)
		{
			LogDebugMessage("Disconnect From : " + context.ClientAddress.ToString());

			foreach (var conn in WSconnections.ToList())
			{
				if (context.ClientAddress.ToString().Equals(conn.ClientAddress.ToString()))
				{
					WSconnections.Remove(conn);
				}
			}
		}

		private void OnConnected(UserContext context)
		{
			LogDebugMessage("Connected From : " + context.ClientAddress.ToString());
		}

		private void OnConnect(UserContext context)
		{
			LogDebugMessage("OnConnect From : " + context.ClientAddress.ToString());
			WSconnections.Add(context);
		}

		internal List<UserContext> WSconnections = new List<UserContext>();

		private void OnSend(UserContext context)
		{
			LogDebugMessage("OnSend From : " + context.ClientAddress.ToString());
		}

		private void OnReceive(UserContext context)
		{
			LogDebugMessage("WS receive : " + context.DataFrame.ToString());
		}
*/
		private void InitialiseRG11()
		{
			if (RG11Enabled && RG11Port.Length > 0)
			{
				cmprtRG11 = new SerialPort(RG11Port, 9600, Parity.None, 8, StopBits.One) { Handshake = Handshake.None, RtsEnable = true, DtrEnable = true };

				cmprtRG11.PinChanged += RG11StateChange;
			}

			if (RG11Enabled2 && RG11Port2.Length > 0 && (!RG11Port2.Equals(RG11Port)))
			{
				// a second RG11 is in use, using a different com port
				cmprt2RG11 = new SerialPort(RG11Port2, 9600, Parity.None, 8, StopBits.One) { Handshake = Handshake.None, RtsEnable = true, DtrEnable = true };

				cmprt2RG11.PinChanged += RG11StateChange;
			}
		}

		private void RG11StateChange(object sender, SerialPinChangedEventArgs e)
		{
			bool isDSR = e.EventType == SerialPinChange.DsrChanged;
			bool isCTS = e.EventType == SerialPinChange.CtsChanged;

			// Is this a trigger that the first RG11 is configured for?
			bool isDevice1 = (((SerialPort)sender).PortName == RG11Port) && ((isDSR && RG11DTRmode) || (isCTS && !RG11DTRmode));
			// Is this a trigger that the second RG11 is configured for?
			bool isDevice2 = (((SerialPort)sender).PortName == RG11Port2) && ((isDSR && RG11DTRmode2) || (isCTS && !RG11DTRmode2));

			// is the pin on or off?
			bool isOn = (isDSR && ((SerialPort)sender).DsrHolding) || (isCTS && ((SerialPort)sender).CtsHolding);

			if (isDevice1)
			{
				if (RG11TBRmode)
				{
					if (isOn)
					{
						// relay closed, record a 'tip'
						station.RG11RainToday += RG11tipsize;
					}
				}
				else
				{
					station.IsRaining = isOn;
					IsRainingAlarm.Triggered = isOn;
				}
			}
			else if (isDevice2)
			{
				if (RG11TBRmode2)
				{
					if (isOn)
					{
						// relay closed, record a 'tip'
						station.RG11RainToday += RG11tipsize;
					}
				}
				else
				{
					station.IsRaining = isOn;
					IsRainingAlarm.Triggered = isOn;
				}
			}
		}


		private void WebTimerTick(object sender, ElapsedEventArgs e)
		{
			if (station.DataStopped)
			{
				// No data coming in, do not do anything else
				return;
			}

			var now = DateTime.Now;

			if (WebUpdating == 1)
			{
				LogMessage("Warning, previous web update is still in progress, first chance, skipping this interval");
				WebUpdating++;
			}
			else if (WebUpdating >= 2)
			{
				LogMessage("Warning, previous web update is still in progress, second chance, aborting connection");
				if (ftpThread.ThreadState == System.Threading.ThreadState.Running)
					ftpThread.Interrupt();
				LogMessage("Trying new web update");
				WebUpdating = 1;
				ftpThread = new Thread(() => DoHTMLFiles(now)) { IsBackground = true };
				ftpThread.Start();
			}
			else
			{
				WebUpdating = 1;
				ftpThread = new Thread(() => DoHTMLFiles(now)) { IsBackground = true };
				ftpThread.Start();
			}
		}


		public void MQTTTimerTick(object sender, ElapsedEventArgs e)
		{
			if (!station.DataStopped)
				MqttPublisher.UpdateMQTTfeed("Interval");
		}



		internal void RealtimeTimerTick(object sender, ElapsedEventArgs elapsedEventArgs)
		{
			var cycle = RealtimeCycleCounter++;
			var reconnecting = false;

			if (station.DataStopped)
			{
				// No data coming in, do not do anything
				return;
			}

			if ((!station.PressReadyToPlot || !station.TempReadyToPlot || !station.WindReadyToPlot) && !StationOptions.NoSensorCheck)
			{
				// not all the data is ready and NoSensorCheck is not enabled
				return;
			}

			LogDebugMessage($"Realtime[{cycle}]: Start cycle");
			try
			{
				// Process any files
				if (RealtimeCopyInProgress || RealtimeFtpInProgress)
				{
					LogMessage($"Realtime[{cycle}]: Warning, a previous cycle is still processing local files. Skipping this interval.");
				}
				else
				{
					RealtimeCopyInProgress = true;
					CreateRealtimeFile(cycle).Wait();
					CreateRealtimeHTMLfiles(cycle).Wait();
					RealtimeCopyInProgress = false;

					if (FtpOptions.LocalCopyEnabled)
					{
						_ = RealtimeLocalCopy(cycle); // let this run in background
					}

					if (FtpOptions.RealtimeEnabled && FtpOptions.Enabled && !RealtimeFtpReconnecting)
					{
						// Is a previous cycle still running?
						if (RealtimeFtpInProgress)
						{
							LogMessage($"Realtime[{cycle}]: Warning, a previous cycle is still trying to connect to FTP server, skip count = {++realtimeFTPRetries}");
							// real time interval is in ms, if a session has been uploading for 3 minutes - abort it and reconnect
							if (realtimeFTPRetries * RealtimeInterval / 1000 > 3 * 60)
							{
								LogMessage($"Realtime[{cycle}]: Realtime has been in progress for more than 3 minutes, attempting to reconnect.");
								//RealtimeFTPConnectionTest(cycle);
								_ = RealtimeFTPReconnect(); // let this run in background
							}
							else
							{
								LogMessage($"Realtime[{cycle}]: No FTP attempted this cycle");
							}
						}
						else
						{
							// This only happens if the user enables realtime FTP after starting Cumulus
							if (FtpOptions.FtpMode == FtpProtocols.SFTP && (RealtimeSSH == null || !RealtimeSSH.ConnectionInfo.IsAuthenticated))
							{
								_ = RealtimeFTPReconnect(); // let this run in background
								reconnecting = true;
							}
							if (FtpOptions.FtpMode != FtpProtocols.SFTP && !RealtimeFTP.IsConnected)
							{
								_ = RealtimeFTPReconnect(); // let this run in background
								reconnecting = true;
							}

							if (!reconnecting)
							{
								// Finally we can do some FTP!
								RealtimeFtpInProgress = true;

								try
								{
									RealtimeFTPUpload(cycle);
									realtimeFTPRetries = 0;
								}
								catch (Exception)
								{
									LogMessage($"Realtime[{cycle}]: Error during realtime FTP update that requires reconnection");
									_ = RealtimeFTPReconnect(); // let this run in background
								}
								RealtimeFtpInProgress = false;
							}
						}
					}

					if (!string.IsNullOrEmpty(RealtimeProgram))
					{
						try
						{
							realtimeTokenParser.InputText = RealtimeParams;
							var args = realtimeTokenParser.ToStringFromString();
							LogDebugMessage($"Realtime[{cycle}]: Execute realtime program - {RealtimeProgram}, with parameters - {args}");
							Utils.RunExternalTask(RealtimeProgram, args, false);
						}
						catch (Exception ex)
						{
							LogDebugMessage($"Realtime[{cycle}]: Error in realtime program - {RealtimeProgram}. Error: {ex.Message}");
						}
					}
				}

				MySqlStuff.DoRealtimeData(cycle, true);
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, $"Realtime[{cycle}]: Error during update");
				if (FtpOptions.RealtimeEnabled && FtpOptions.Enabled)
				{
					_ = RealtimeFTPReconnect(); // let this run in background
					RealtimeFtpInProgress = false;
				}
			}
			LogDebugMessage($"Realtime[{cycle}]: End cycle");
		}

		private async Task RealtimeFTPReconnect()
		{
			RealtimeFtpReconnecting = true;
			await Task.Run(() =>
			{
				bool connected;
				bool reinit;

				do
				{
					connected = false;
					reinit = false;


					// Try to disconnect cleanly first
					//TODO: Just bypassing this for now for FTP, if it works refactor to remove redundant code
					if (FtpOptions.FtpMode == FtpProtocols.SFTP)
					{
						try
						{
							LogMessage("RealtimeReconnect: Realtime ftp attempting disconnect");
							if (FtpOptions.FtpMode == FtpProtocols.SFTP && RealtimeSSH != null)
							{
								RealtimeSSH.Disconnect();
							}
							if (FtpOptions.FtpMode != FtpProtocols.SFTP && RealtimeFTP != null)
							{
								RealtimeFTP.Config.DisconnectWithQuit = false;
								RealtimeFTP.Disconnect();
							}
							LogMessage("RealtimeReconnect: Realtime ftp disconnected");
						}
						catch (ObjectDisposedException)
						{
							LogDebugMessage($"RealtimeReconnect: Error, connection is disposed");
						}
						catch (Exception ex)
						{
							LogDebugMessage($"RealtimeReconnect: Error disconnecting from server - {ex.Message}");
						}
						finally
						{
							if (FtpOptions.FtpMode != FtpProtocols.SFTP && RealtimeFTP != null)
								RealtimeFTP.Config.DisconnectWithQuit = false;
						}


						// Attempt a simple reconnect
						try
						{
							LogMessage("RealtimeReconnect: Realtime ftp attempting to reconnect");
							if (FtpOptions.FtpMode == FtpProtocols.SFTP)
							{
								RealtimeSSH.Connect();
								connected = RealtimeSSH.ConnectionInfo.IsAuthenticated;
							}
							else
							{
								if (FtpOptions.AutoDetect)
								{
									RealtimeFTP.AutoConnect();
								}
								else
								{
									RealtimeFTP.Connect();
								}

								connected = RealtimeFTP.IsConnected;
							}
							LogMessage("RealtimeReconnect: Reconnected with server (we think)");
						}
						catch (ObjectDisposedException)
						{
							reinit = true;
							LogDebugMessage($"RealtimeReconnect: Error, connection is disposed");
						}
						catch (Exception ex)
						{
							reinit = true;
							LogDebugMessage($"RealtimeReconnect: Error reconnecting ftp server - {ex.Message}");
							if (ex.InnerException != null)
								LogDebugMessage($"RealtimeReconnect: Base exception - {ex.GetBaseException().Message}");
						}
					}
					else
					{
						reinit = true;
					}


					// Simple reconnect failed - start again and reinitialise the connections
					// RealtimeXXXLogin() has its own error handling
					if (reinit)
					{
						LogMessage("RealtimeReconnect: Realtime ftp attempting to reinitialise the connection");
						if (FtpOptions.FtpMode == FtpProtocols.SFTP)
						{
							RealtimeSSHLogin();
							connected = RealtimeSSH.ConnectionInfo.IsAuthenticated;
						}
						else
						{
							RealtimeFTPLogin();
							connected = RealtimeFTP.IsConnected;
						}
						if (connected)
						{
							LogMessage("RealtimeReconnect: Realtime ftp connection reinitialised");
						}
						else
						{
							LogMessage("RealtimeReconnect: Realtime ftp connection failed to connect after reinitialisation");
						}
					}


					// We *think* we are connected, now try and do something!
					if (connected)
					{
						try
						{
							string pwd;
							LogMessage("RealtimeReconnect: Realtime ftp testing the connection");
							if (FtpOptions.FtpMode == FtpProtocols.SFTP)
							{
								pwd = RealtimeSSH.WorkingDirectory;
								// Double check
								if (!RealtimeSSH.IsConnected)
								{
									connected = false;
								}
							}
							else
							{
								pwd = RealtimeFTP.GetWorkingDirectory();
								// Double check
								if (!RealtimeFTP.IsConnected)
								{
									connected = false;
								}
							}
							if (pwd.Length == 0)
							{
								connected = false;
								LogMessage("RealtimeReconnect: Realtime ftp connection test failed to get Present Working Directory");
							}
							else
							{
								LogMessage($"RealtimeReconnect: Realtime ftp connection test found Present Working Directory OK - [{pwd}]");
							}
						}
						catch (Exception ex)
						{
							LogDebugMessage($"RealtimeReconnect: Realtime ftp connection test Failed - {ex.Message}");
							if (ex.InnerException != null)
								LogDebugMessage($"RealtimeReconnect: Base exception - {ex.GetBaseException().Message}");


							connected = false;
						}
					}


					if (!connected)
					{
						LogMessage("RealtimeReconnect: Sleeping for 20 seconds before trying again...");
						Thread.Sleep(20 * 1000);
					}
				} while (!connected);


				// OK we are reconnected, let the FTP recommence
				RealtimeFtpReconnecting = false;
				RealtimeFtpInProgress = false;
				realtimeFTPRetries = 0;
				RealtimeCopyInProgress = false;
				LogMessage("RealtimeReconnect: Realtime FTP now connected to server (tested)");
				LogMessage("RealtimeReconnect: Realtime FTP operations will be restarted");
			});
		}


		private async Task RealtimeLocalCopy(byte cycle)
		{
			var dstPath = "";
			var folderSep1 = Path.DirectorySeparatorChar.ToString();
			var folderSep2 = Path.AltDirectorySeparatorChar.ToString();


			if (FtpOptions.LocalCopyFolder.Length > 0)
			{
				dstPath = (FtpOptions.Directory.EndsWith(folderSep1) || FtpOptions.Directory.EndsWith(folderSep2) ? FtpOptions.LocalCopyFolder : FtpOptions.LocalCopyFolder + folderSep1);
			}

			for (var i = 0; i < RealtimeFiles.Length; i++)
			{
				if (RealtimeFiles[i].Create && RealtimeFiles[i].Copy)
				{
					var dstFile = dstPath + RealtimeFiles[i].RemoteFileName;
					var srcFile = RealtimeFiles[i].LocalPath + RealtimeFiles[i].LocalFileName;

					try
					{
						LogDebugMessage($"RealtimeLocalCopy[{cycle}]: Copying - {RealtimeFiles[i].LocalFileName}");
						await Utils.CopyFileAsync(srcFile, dstFile);
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"RealtimeLocalCopy[{cycle}]: Error copying [{srcFile}] to [{dstFile}. Error");
					}
				}
			}
		}


		private void RealtimeFTPUpload(byte cycle)
		{
			var remotePath = "";
			bool doMore;

			if (FtpOptions.Directory.Length > 0)
			{
				remotePath = (FtpOptions.Directory.EndsWith("/") ? FtpOptions.Directory : FtpOptions.Directory + "/");
			}

			for (var i = 0; i < RealtimeFiles.Length; i++)
			{
				if (RealtimeFiles[i].Create && RealtimeFiles[i].FTP)
				{
					var remoteFile = remotePath + RealtimeFiles[i].RemoteFileName;
					var localFile = RealtimeFiles[i].LocalPath + RealtimeFiles[i].LocalFileName;

					LogFtpDebugMessage($"Realtime[{cycle}]: Uploading - {RealtimeFiles[i].LocalFileName}");
					if (FtpOptions.FtpMode == FtpProtocols.SFTP)
					{
						doMore = UploadFile(RealtimeSSH, localFile, remoteFile, cycle);
					}
					else
					{
						doMore = UploadFile(RealtimeFTP, localFile, remoteFile, cycle);
					}

					if (!doMore)
					{
						LogFtpMessage($"Realtime[{cycle}]: Aborting this upload");
						throw new FtpException("Connection failed.");
					}
				}
			}

			// Extra files
			for (int i = 0; i < numextrafiles; i++)
			{
				var uploadfile = ExtraFiles[i].local;
				var remotefile = ExtraFiles[i].remote;

				if ((uploadfile.Length > 0) && (remotefile.Length > 0) && ExtraFiles[i].realtime && ExtraFiles[i].FTP)
				{
					uploadfile = GetUploadFilename(uploadfile, DateTime.Now);

					if (File.Exists(uploadfile))
					{
						remotefile = GetRemoteFileName(remotefile, DateTime.Now);

						// all checks OK, file needs to be uploaded
						if (ExtraFiles[i].process)
						{
							// we've already processed the file
							uploadfile += "tmp";
						}
						LogFtpDebugMessage($"Realtime[{cycle}]: Uploading extra web file[{i}] {uploadfile} to {remotefile}");
						if (FtpOptions.FtpMode == FtpProtocols.SFTP)
						{
							doMore = UploadFile(RealtimeSSH, uploadfile, remotefile, cycle);
						}
						else
						{
							doMore = UploadFile(RealtimeFTP, uploadfile, remotefile, cycle);
						}

						if (!doMore)
						{
							LogMessage($"Realtime[{cycle}]: Aborting this upload");
							throw new FtpException("Connection failed.");
						}
					}
					else
					{
						LogMessage($"Realtime[{cycle}]: Warning, extra web file[{i}] not found! - {uploadfile}");
					}
				}
			}
		}

		private async Task CreateRealtimeHTMLfiles(int cycle)
		{
			// Process realtime files
			for (var i = 0; i < RealtimeFiles.Length; i++)
			{
				if (RealtimeFiles[i].Create && !string.IsNullOrWhiteSpace(RealtimeFiles[i].TemplateFileName))
				{
					var destFile = RealtimeFiles[i].LocalPath + RealtimeFiles[i].LocalFileName;
					try
					{
						LogDebugMessage($"Realtime[{cycle}]: Processing realtime file - {RealtimeFiles[i].LocalFileName}");
						await ProcessTemplateFile(RealtimeFiles[i].TemplateFileName, destFile, realtimeTokenParser, true);
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"Realtime[{cycle}]: Error processing file [{RealtimeFiles[i].LocalFileName}] to [{destFile}]. Error");
					}
				}
			}

			for (int i = 0; i < numextrafiles; i++)
			{
				if (ExtraFiles[i].realtime)
				{
					var uploadfile = ExtraFiles[i].local;
					var remotefile = ExtraFiles[i].remote;

					if ((uploadfile.Length > 0) && (remotefile.Length > 0))
					{
						uploadfile = GetUploadFilename(uploadfile, DateTime.Now);

						if (File.Exists(uploadfile))
						{
							remotefile = GetRemoteFileName(remotefile, DateTime.Now);

							if (ExtraFiles[i].process)
							{
								// process the file
								try
								{
									LogDebugMessage($"Realtime[{cycle}]: Processing extra file[{i}] - {uploadfile}");
									ProcessTemplateFile(uploadfile, uploadfile + "tmp", realtimeTokenParser, false).Wait();
								}
								catch (Exception ex)
								{
									LogExceptionMessage(ex, $"Realtime[{cycle}]: Error processing extra file [{uploadfile}] to [{uploadfile}]. Error");
									continue;
								}
								uploadfile += "tmp";
							}

							if (!ExtraFiles[i].FTP)
							{
								// just copy the file
								try
								{
									LogDebugMessage($"Realtime[{cycle}]: Copying extra file[{i}] {uploadfile} to {remotefile}");
									await Utils.CopyFileAsync(uploadfile, remotefile);
								}
								catch (Exception ex)
								{
									LogExceptionMessage(ex, $"Realtime[{cycle}]: Error copying extra realtime file[{i}] - {uploadfile}");
								}
							}
						}
						else
						{
							LogMessage($"Realtime[{cycle}]: Extra realtime web file[{i}] not found - {uploadfile}");
						}

					}
				}
			}
		}

		public void TokenParserOnToken(string strToken, ref string strReplacement)
		{
			var tagParams = new Dictionary<string, string>();
			var paramList = ParseParams(strToken);
			var webTag = paramList[0];

			tagParams.Add("webtag", webTag);
			for (int i = 1; i < paramList.Count; i += 2)
			{
				// odd numbered entries are keys
				string key = paramList[i];
				// even numbered entries are values
				string value = paramList[i + 1];
				tagParams.Add(key, value);
			}

			strReplacement = webtags.GetWebTagText(webTag, tagParams);
		}

		private static List<string> ParseParams(string line)
		{
			var insideQuotes = false;
			var start = -1;

			var parts = new List<string>();

			for (var i = 0; i < line.Length; i++)
			{
				if (char.IsWhiteSpace(line[i]))
				{
					if (!insideQuotes && start != -1)
					{
						parts.Add(line[start..i]);
						start = -1;
					}
				}
				else if (line[i] == '"')
				{
					if (start != -1)
					{
						parts.Add(line[start..i]);
						start = -1;
					}
					insideQuotes = !insideQuotes;
				}
				else if (line[i] == '=')
				{
					if (!insideQuotes)
					{
						if (start != -1)
						{
							parts.Add(line[start..i]);
							start = -1;
						}
					}
				}
				else
				{
					if (start == -1)
						start = i;
				}
			}

			if (start != -1)
				parts.Add(line[start..]);

			return parts;
		}

		public string DecimalSeparator { get; set; }

		internal void DoMoonPhase()
		{
			DateTime now = DateTime.Now;
			double[] moonriseset = MoonriseMoonset.MoonRise(now.Year, now.Month, now.Day, TimeZoneInfo.Local.GetUtcOffset(now).TotalHours, Latitude, Longitude);
			MoonRiseTime = TimeSpan.FromHours(moonriseset[0]);
			MoonSetTime = TimeSpan.FromHours(moonriseset[1]);

			DateTime utcNow = DateTime.UtcNow;
			MoonPhaseAngle = MoonriseMoonset.MoonPhase(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour);
			MoonPercent = (100.0 * (1.0 + Math.Cos(MoonPhaseAngle * Math.PI / 180)) / 2.0);

			// If between full moon and new moon, angle is between 180 and 360, make percent negative to indicate waning
			if (MoonPhaseAngle > 180)
			{
				MoonPercent = -MoonPercent;
			}
			/*
			// New   = -0.4 -> 0.4
			// 1st Q = 45 -> 55
			// Full  = 99.6 -> -99.6
			// 3rd Q = -45 -> -55
			if ((MoonPercent > 0.4) && (MoonPercent < 45))
				MoonPhaseString = WaxingCrescent;
			else if ((MoonPercent >= 45) && (MoonPercent <= 55))
				MoonPhaseString = FirstQuarter;
			else if ((MoonPercent > 55) && (MoonPercent < 99.6))
				MoonPhaseString = WaxingGibbous;
			else if ((MoonPercent >= 99.6) || (MoonPercent <= -99.6))
				MoonPhaseString = FullMoon;
			else if ((MoonPercent < -55) && (MoonPercent > -99.6))
				MoonPhaseString = WaningGibbous;
			else if ((MoonPercent <= -45) && (MoonPercent >= -55))
				MoonPhaseString = LastQuarter;
			else if ((MoonPercent > -45) && (MoonPercent < -0.4))
				MoonPhaseString = WaningCrescent;
			else
				MoonPhaseString = NewMoon;
			*/

			// Use Phase Angle to determine string - it's linear unlike Illuminated Percentage
			// New  = 186 - 180 - 174
			// 1st  =  96 -  90 -  84
			// Full =   6 -   0 - 354
			// 3rd  = 276 - 270 - 264
			if (MoonPhaseAngle < 174 && MoonPhaseAngle > 96)
				MoonPhaseString = WaxingCrescent;
			else if (MoonPhaseAngle <= 96 && MoonPhaseAngle >= 84)
				MoonPhaseString = FirstQuarter;
			else if (MoonPhaseAngle < 84 && MoonPhaseAngle > 6)
				MoonPhaseString = WaxingGibbous;
			else if (MoonPhaseAngle <= 6 || MoonPhaseAngle >= 354)
				MoonPhaseString = FullMoon;
			else if (MoonPhaseAngle < 354 && MoonPhaseAngle > 276)
				MoonPhaseString = WaningGibbous;
			else if (MoonPhaseAngle <= 276 && MoonPhaseAngle >= 264)
				MoonPhaseString = LastQuarter;
			else if (MoonPhaseAngle < 264 && MoonPhaseAngle > 186)
				MoonPhaseString = WaningCrescent;
			else
				MoonPhaseString = NewMoon;
		}

		internal void DoMoonImage()
		{
			if (MoonImage.Enabled)
			{
				LogDebugMessage("Generating new Moon image");
				var ret = MoonriseMoonset.CreateMoonImage(MoonPhaseAngle, (double) Latitude, MoonImage.Size, MoonImage.Transparent);

				if (ret)
				{
					// set a flag to show file is ready for FTP
					MoonImage.ReadyToFtp = true;
					MoonImage.ReadyToCopy = true;
				}
			}
		}


		/*
		private string GetMoonStage(double fAge)
		{
			string sStage;

			if (fAge < 1.84566)
			{
				sStage = NewMoon;
			}
			else if (fAge < 5.53699)
			{
				sStage = WaxingCrescent;
			}
			else if (fAge < 9.22831)
			{
				sStage = FirstQuarter;
			}
			else if (fAge < 12.91963)
			{
				sStage = WaxingGibbous;
			}
			else if (fAge < 16.61096)
			{
				sStage = FullMoon;
			}
			else if (fAge < 20.30228)
			{
				sStage = WaningGibbous;
			}
			else if (fAge < 23.9931)
			{
				sStage = LastQuarter;
			}
			else if (fAge < 27.68493)
			{
				sStage = WaningCrescent;
			}
			else
			{
				sStage = NewMoon;
			}

			return sStage;
		}
		*/

		public double MoonAge { get; set; }

		public string MoonPhaseString { get; set; }

		public double MoonPhaseAngle { get; set; }

		public double MoonPercent { get; set; }

		public TimeSpan MoonSetTime { get; set; }

		public TimeSpan MoonRiseTime { get; set; }

		public MoonImageOptionsClass MoonImage = new MoonImageOptionsClass();

		private void GetSunriseSunset(DateTime time, out DateTime sunrise, out DateTime sunset, out bool alwaysUp, out bool alwaysDown)
		{
			string rise = SunriseSunset.SunRise(time, TimeZoneInfo.Local.GetUtcOffset(time).TotalHours, (double) Longitude, (double) Latitude);
			string set = SunriseSunset.SunSet(time, TimeZoneInfo.Local.GetUtcOffset(time).TotalHours, (double) Longitude, (double) Latitude);

			if (rise.Equals("Always Down") || set.Equals("Always Down"))
			{
				alwaysDown = true;
				alwaysUp = false;
				sunrise = DateTime.MinValue;
				sunset = DateTime.MinValue;
			}
			else if (rise.Equals("Always Up") || set.Equals("Always Up"))
			{
				alwaysDown = false;
				alwaysUp = true;
				sunrise = DateTime.MinValue;
				sunset = DateTime.MinValue;
			}
			else
			{
				alwaysDown = false;
				alwaysUp = false;
				try
				{
					int h = Convert.ToInt32(rise[..2]);
					int m = Convert.ToInt32(rise.Substring(2, 2));
					int s = Convert.ToInt32(rise.Substring(4, 2));
					sunrise = DateTime.Now.Date.Add(new TimeSpan(h, m, s));
				}
				catch (Exception)
				{
					sunrise = DateTime.MinValue;
				}

				try
				{
					int h = Convert.ToInt32(set[..2]);
					int m = Convert.ToInt32(set.Substring(2, 2));
					int s = Convert.ToInt32(set.Substring(4, 2));
					sunset = DateTime.Now.Date.Add(new TimeSpan(h, m, s));
				}
				catch (Exception)
				{
					sunset = DateTime.MinValue;
				}
			}
		}

		/*
		private DateTime getSunriseTime(DateTime time)
		{
			string rise = SunriseSunset.sunrise(time, TimeZone.CurrentTimeZone.GetUtcOffset(time).TotalHours, Longitude, Latitude);
			//LogMessage("Sunrise: " + rise);
			int h = Convert.ToInt32(rise.Substring(0, 2));
			int m = Convert.ToInt32(rise.Substring(2, 2));
			int s = Convert.ToInt32(rise.Substring(4, 2));
			return DateTime.Now.Date.Add(new TimeSpan(h, m, s));
		}
		*/

		/*
		private DateTime getSunsetTime(DateTime time)
		{
			string rise = SunriseSunset.sunset(time, TimeZone.CurrentTimeZone.GetUtcOffset(time).TotalHours, Longitude, Latitude);
			//LogMessage("Sunrise: " + rise);
			int h = Convert.ToInt32(rise.Substring(0, 2));
			int m = Convert.ToInt32(rise.Substring(2, 2));
			int s = Convert.ToInt32(rise.Substring(4, 2));
			return DateTime.Now.Date.Add(new TimeSpan(h, m, s));
		}
		*/

		private void GetDawnDusk(DateTime time, out DateTime dawn, out DateTime dusk, out bool alwaysUp, out bool alwaysDown)
		{
			string dawnStr = SunriseSunset.CivilTwilightEnds(time, TimeZoneInfo.Local.GetUtcOffset(time).TotalHours, (double) Longitude, (double) Latitude);
			string duskStr = SunriseSunset.CivilTwilightStarts(time, TimeZoneInfo.Local.GetUtcOffset(time).TotalHours, (double) Longitude, (double) Latitude);

			if (dawnStr.Equals("Always Down") || duskStr.Equals("Always Down"))
			{
				alwaysDown = true;
				alwaysUp = false;
				dawn = DateTime.MinValue;
				dusk = DateTime.MinValue;
			}
			else if (dawnStr.Equals("Always Up") || duskStr.Equals("Always Up"))
			{
				alwaysDown = false;
				alwaysUp = true;
				dawn = DateTime.MinValue;
				dusk = DateTime.MinValue;
			}
			else
			{
				alwaysDown = false;
				alwaysUp = false;
				try
				{
					int h = Convert.ToInt32(dawnStr[..2]);
					int m = Convert.ToInt32(dawnStr.Substring(2, 2));
					int s = Convert.ToInt32(dawnStr.Substring(4, 2));
					dawn = DateTime.Now.Date.Add(new TimeSpan(h, m, s));
				}
				catch (Exception)
				{
					dawn = DateTime.MinValue;
				}

				try
				{
					int h = Convert.ToInt32(duskStr[..2]);
					int m = Convert.ToInt32(duskStr.Substring(2, 2));
					int s = Convert.ToInt32(duskStr.Substring(4, 2));
					dusk = DateTime.Now.Date.Add(new TimeSpan(h, m, s));
				}
				catch (Exception)
				{
					dusk = DateTime.MinValue;
				}
			}
		}

		/*
		private DateTime getDawnTime(DateTime time)
		{
			string rise = SunriseSunset.CivilTwilightEnds(time, TimeZone.CurrentTimeZone.GetUtcOffset(time).TotalHours, Longitude, Latitude);
			//LogMessage("Sunrise: " + rise);
			try
			{
				int h = Convert.ToInt32(rise.Substring(0, 2));
				int m = Convert.ToInt32(rise.Substring(2, 2));
				int s = Convert.ToInt32(rise.Substring(4, 2));
				return DateTime.Now.Date.Add(new TimeSpan(h, m, s));
			}
			catch (Exception)
			{
				return DateTime.Now.Date;
			}
		}
		*/

		/*
		private DateTime getDuskTime(DateTime time)
		{
			string rise = SunriseSunset.CivilTwilightStarts(time, TimeZone.CurrentTimeZone.GetUtcOffset(time).TotalHours, Longitude, Latitude);
			//LogMessage("Sunrise: " + rise);
			try
			{
				int h = Convert.ToInt32(rise.Substring(0, 2));
				int m = Convert.ToInt32(rise.Substring(2, 2));
				int s = Convert.ToInt32(rise.Substring(4, 2));
				return DateTime.Now.Date.Add(new TimeSpan(h, m, s));
			}
			catch (Exception)
			{
				return DateTime.Now.Date;
			}
		}
		*/

		internal void DoSunriseAndSunset()
		{
			LogMessage("Calculating sunrise and sunset times");
			DateTime now = DateTime.Now;
			DateTime tomorrow = now.AddDays(1);
			GetSunriseSunset(now, out SunRiseTime, out SunSetTime, out SunAlwaysUp, out SunAlwaysDown);

			if (SunAlwaysUp)
			{
				LogMessage("Sun always up");
				DayLength = new TimeSpan(24, 0, 0);
			}
			else if (SunAlwaysDown)
			{
				LogMessage("Sun always down");
				DayLength = new TimeSpan(0, 0, 0);
			}
			else
			{
				LogMessage("Sunrise: " + SunRiseTime.ToString("HH:mm:ss", invDate));
				LogMessage("Sunset : " + SunSetTime.ToString("HH:mm:ss", invDate));
				if (SunRiseTime == DateTime.MinValue)
				{
					DayLength = SunSetTime - DateTime.Now.Date;
				}
				else if (SunSetTime == DateTime.MinValue)
				{
					DayLength = DateTime.Now.Date.AddDays(1) - SunRiseTime;
				}
				else if (SunSetTime > SunRiseTime)
				{
					DayLength = SunSetTime - SunRiseTime;
				}
				else
				{
					DayLength = new TimeSpan(24, 0, 0) - (SunRiseTime - SunSetTime);
				}
			}

			DateTime tomorrowSunRiseTime;
			DateTime tomorrowSunSetTime;
			TimeSpan tomorrowDayLength;
			bool tomorrowSunAlwaysUp;
			bool tomorrowSunAlwaysDown;

			GetSunriseSunset(tomorrow, out tomorrowSunRiseTime, out tomorrowSunSetTime, out tomorrowSunAlwaysUp, out tomorrowSunAlwaysDown);

			if (tomorrowSunAlwaysUp)
			{
				LogMessage("Tomorrow sun always up");
				tomorrowDayLength = new TimeSpan(24, 0, 0);
			}
			else if (tomorrowSunAlwaysDown)
			{
				LogMessage("Tomorrow sun always down");
				tomorrowDayLength = new TimeSpan(0, 0, 0);
			}
			else
			{
				LogMessage("Tomorrow sunrise: " + tomorrowSunRiseTime.ToString("HH:mm:ss", invDate));
				LogMessage("Tomorrow sunset : " + tomorrowSunSetTime.ToString("HH:mm:ss", invDate));
				tomorrowDayLength = tomorrowSunSetTime - tomorrowSunRiseTime;
			}

			int tomorrowdiff = Convert.ToInt32(tomorrowDayLength.TotalSeconds - DayLength.TotalSeconds);
			LogDebugMessage("Tomorrow length diff: " + tomorrowdiff);

			bool tomorrowminus;

			if (tomorrowdiff < 0)
			{
				tomorrowminus = true;
				tomorrowdiff = -tomorrowdiff;
			}
			else
			{
				tomorrowminus = false;
			}

			int tomorrowmins = tomorrowdiff / 60;
			int tomorrowsecs = tomorrowdiff % 60;

			if (tomorrowminus)
			{
				try
				{
					TomorrowDayLengthText = string.Format(thereWillBeMinSLessDaylightTomorrow, tomorrowmins, tomorrowsecs);
				}
				catch (Exception)
				{
					TomorrowDayLengthText = "Error in LessDaylightTomorrow format string";
				}
			}
			else
			{
				try
				{
					TomorrowDayLengthText = string.Format(thereWillBeMinSMoreDaylightTomorrow, tomorrowmins, tomorrowsecs);
				}
				catch (Exception)
				{
					TomorrowDayLengthText = "Error in MoreDaylightTomorrow format string";
				}
			}

			GetDawnDusk(now, out Dawn, out Dusk, out TwilightAlways, out TwilightNever);

			if (TwilightAlways)
			{
				DaylightLength = new TimeSpan(24, 0, 0);
			}
			else if (TwilightNever)
			{
				DaylightLength = new TimeSpan(0, 0, 0);
			}
			else
			{
				if (Dawn == DateTime.MinValue)
				{
					DaylightLength = Dusk - DateTime.Now.Date;
				}
				else if (Dusk == DateTime.MinValue)
				{
					DaylightLength = DateTime.Now.Date.AddDays(1) - Dawn;
				}
				else if (Dusk > Dawn)
				{
					DaylightLength = Dusk - Dawn;
				}
				else
				{
					DaylightLength = new TimeSpan(24, 0, 0) - (Dawn - Dusk);
				}
			}
		}

		public DateTime SunSetTime;

		public DateTime SunRiseTime;

		internal bool SunAlwaysUp;
		internal bool SunAlwaysDown;

		internal bool TwilightAlways;
		internal bool TwilightNever;

		public string TomorrowDayLengthText { get; set; }

		public bool IsDaylight()
		{
			if (TwilightAlways)
			{
				return true;
			}
			if (TwilightNever)
			{
				return false;
			}
			if (Dusk > Dawn)
			{
				// 'Normal' case where sun sets before midnight
				return (DateTime.Now >= Dawn) && (DateTime.Now <= Dusk);
			}
			else
			{
				return !((DateTime.Now >= Dusk) && (DateTime.Now <= Dawn));
			}
		}

		public bool IsSunUp()
		{
			if (SunAlwaysUp)
			{
				return true;
			}
			if (SunAlwaysDown)
			{
				return false;
			}
			if (SunSetTime > SunRiseTime)
			{
				// 'Normal' case where sun sets before midnight
				return (DateTime.Now >= SunRiseTime) && (DateTime.Now <= SunSetTime);
			}
			else
			{
				return !((DateTime.Now >= SunSetTime) && (DateTime.Now <= SunRiseTime));
			}
		}

		private static string RemoveOldDiagsFiles(string directory)
		{
			const int maxEntries = 12;

			List<string> fileEntries = new List<string>(Directory.GetFiles(directory).Where(f => System.Text.RegularExpressions.Regex.Match(f, @"[\\/]+\d{8}-\d{6}\.txt").Success));

			fileEntries.Sort();

			while (fileEntries.Count >= maxEntries)
			{
				File.Delete(fileEntries.First());
				fileEntries.RemoveAt(0);
			}

			return $"{directory}{DateTime.Now:yyyyMMdd-HHmmss}.txt";
		}

		public void RotateLogFiles()
		{
			// cycle the MXdiags log file?
			var logfileSize = new FileInfo(loggingfile).Length;
			// if > 20 MB
			if (logfileSize > 20971520)
			{
				var oldfile = loggingfile;
				loggingfile = RemoveOldDiagsFiles("MXdiags" + DirectorySeparator);
				LogMessage("Rotating log file, new log file will be: " + loggingfile.Split(DirectorySeparator).Last());
				TextWriterTraceListener myTextListener = new TextWriterTraceListener(loggingfile, "MXlog");
				Trace.Listeners.Remove("MXlog");
				Trace.Listeners.Add(myTextListener);
				LogMessage("Rotated log file, old log file was: " + oldfile.Split(DirectorySeparator).Last());
			}
		}

		private void ReadIniFile()
		{
			var DavisBaudRates = new List<int> { 1200, 2400, 4800, 9600, 14400, 19200 };
			ImetOptions.BaudRates = new List<int> { 19200, 115200 };
			var rewriteRequired = false; // Do we need to re-save the ini file after migration processing or resetting options?

			LogMessage("Reading Cumulus.ini file");
			//DateTimeToString(LongDate, "ddddd", Now);

			IniFile ini = new IniFile("Cumulus.ini");

			// check for Cumulus 1 [FTP Site] and correct it
			if (ini.GetValue("FTP Site", "Port", -999) != -999)
			{
				if (File.Exists("Cumulus.ini"))
				{
					var contents = File.ReadAllText("Cumulus.ini");
					contents = contents.Replace("[FTP Site]", "[FTP site]");
					File.WriteAllText("Cumulus.ini", contents);
					ini.Refresh();
				}
			}

			ProgramOptions.EnableAccessibility = ini.GetValue("Program", "EnableAccessibility", false);

			ProgramOptions.StartupPingHost = ini.GetValue("Program", "StartupPingHost", "");
			ProgramOptions.StartupPingEscapeTime = ini.GetValue("Program", "StartupPingEscapeTime", 999);

			ProgramOptions.StartupDelaySecs = ini.GetValue("Program", "StartupDelaySecs", 0);
			ProgramOptions.StartupDelayMaxUptime = ini.GetValue("Program", "StartupDelayMaxUptime", 300);

			ProgramOptions.StartupTask = ini.GetValue("Program", "StartupTask", "");
			ProgramOptions.StartupTaskParams = ini.GetValue("Program", "StartupTaskParams", "");
			ProgramOptions.StartupTaskWait = ini.GetValue("Program", "StartupTaskWait", false);

			ProgramOptions.ShutdownTask = ini.GetValue("Program", "ShutdownTask", "");
			ProgramOptions.ShutdownTaskParams = ini.GetValue("Program", "ShutdownTaskParams", "");

			ProgramOptions.DataStoppedExit = ini.GetValue("Program", "DataStoppedExit", false);
			ProgramOptions.DataStoppedMins = ini.GetValue("Program", "DataStoppedMins", 10);
			ProgramOptions.Culture.RemoveSpaceFromDateSeparator = ini.GetValue("Culture", "RemoveSpaceFromDateSeparator", false);
			// if the culture names match, then we apply the new date separator if change is enabled and it contains a space
			if (ProgramOptions.Culture.RemoveSpaceFromDateSeparator && CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator.Contains(' '))
			{
				// get the existing culture
				var newCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
				// change the date separator
				newCulture.DateTimeFormat.DateSeparator = CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator.Replace(" ", "");
				// set current thread culture
				Thread.CurrentThread.CurrentCulture = newCulture;
				// set the default culture for other threads
				CultureInfo.DefaultThreadCurrentCulture = newCulture;
			}

			ProgramOptions.EncryptedCreds = ini.GetValue("Program", "EncryptedCreds", false);

			ProgramOptions.UpdateDayfile = ini.GetValue("Program", "UpdateDayfile", true);
			ProgramOptions.UpdateLogfile = ini.GetValue("Program", "UpdateLogfile", true);
			ProgramOptions.DisplayPasswords = ini.GetValue("Program", "DisplayPasswords", false);

			ProgramOptions.WarnMultiple = ini.GetValue("Station", "WarnMultiple", true);
			ProgramOptions.ListWebTags = ini.GetValue("Station", "ListWebTags", false);
			SmtpOptions.Logging = ini.GetValue("SMTP", "Logging", false);
			if (DebuggingEnabled)
			{
				ProgramOptions.DebugLogging = true;
				ProgramOptions.DataLogging = true;
			}
			else
			{
				ProgramOptions.DebugLogging = ini.GetValue("Station", "Logging", false);
				ProgramOptions.DataLogging = ini.GetValue("Station", "DataLogging", false);
			}
			ProgramOptions.LogRawStationData = ini.GetValue("Station", "LogRawStationData", false);
			ProgramOptions.LogRawExtraData = ini.GetValue("Station", "LogRawExtraData", false);

			ComportName = ini.GetValue("Station", "ComportName", DefaultComportName);

			StationType = ini.GetValue("Station", "Type", -1);
			StationModel = ini.GetValue("Station", "Model", "");

			FineOffsetStation = (StationType == StationTypes.FineOffset || StationType == StationTypes.FineOffsetSolar);
			DavisStation = (StationType == StationTypes.VantagePro || StationType == StationTypes.VantagePro2);

			// Davis Options
			DavisOptions.UseLoop2 = ini.GetValue("Station", "UseDavisLoop2", true);
			DavisOptions.ReadReceptionStats = ini.GetValue("Station", "DavisReadReceptionStats", true);
			DavisOptions.SetLoggerInterval = ini.GetValue("Station", "DavisSetLoggerInterval", false);
			DavisOptions.InitWaitTime = ini.GetValue("Station", "DavisInitWaitTime", 2000);
			DavisOptions.IPResponseTime = ini.GetValue("Station", "DavisIPResponseTime", 500);
			//StationOptions.DavisReadTimeout = ini.GetValue("Station", "DavisReadTimeout", 1000); // Not currently used
			DavisOptions.IncrementPressureDP = ini.GetValue("Station", "DavisIncrementPressureDP", false);
			if (StationType == StationTypes.VantagePro && DavisOptions.UseLoop2 == true)
			{
				DavisOptions.UseLoop2 = false;
				rewriteRequired = true;
			}
			DavisOptions.BaudRate = ini.GetValue("Station", "DavisBaudRate", 19200);
			// Check we have a valid value
			if (!DavisBaudRates.Contains(DavisOptions.BaudRate))
			{
				// nope, that isn't allowed, set the default
				LogMessage("Error, the value for DavisBaudRate in the ini file " + DavisOptions.BaudRate + " is not valid, using default 19200.");
				DavisOptions.BaudRate = 19200;
				rewriteRequired = true;
			}
			DavisOptions.ForceVPBarUpdate = ini.GetValue("Station", "ForceVPBarUpdate", false);
			//DavisUseDLLBarCalData = ini.GetValue("Station", "DavisUseDLLBarCalData", false);
			//DavisCalcAltPress = ini.GetValue("Station", "DavisCalcAltPress", true);
			//DavisConsoleHighGust = ini.GetValue("Station", "DavisConsoleHighGust", false);
			DavisOptions.RainGaugeType = ini.GetValue("Station", "VPrainGaugeType", -1);
			if (DavisOptions.RainGaugeType > 3)
			{
				DavisOptions.RainGaugeType = -1;
				rewriteRequired = true;
			}
			DavisOptions.ConnectionType = ini.GetValue("Station", "VP2ConnectionType", VP2SERIALCONNECTION);
			DavisOptions.TCPPort = ini.GetValue("Station", "VP2TCPPort", 22222);
			DavisOptions.IPAddr = ini.GetValue("Station", "VP2IPAddr", "0.0.0.0");

			WeatherFlowOptions.WFDeviceId = ini.GetValue("Station", "WeatherFlowDeviceId", 0);
			WeatherFlowOptions.WFTcpPort = ini.GetValue("Station", "WeatherFlowTcpPort", 50222);
			WeatherFlowOptions.WFToken = ini.GetValue("Station", "WeatherFlowToken", "api token");
			WeatherFlowOptions.WFDaysHist = ini.GetValue("Station", "WeatherFlowDaysHist", 0);

			//VPClosedownTime = ini.GetValue("Station", "VPClosedownTime", 99999999);
			//VP2SleepInterval = ini.GetValue("Station", "VP2SleepInterval", 0);
			DavisOptions.PeriodicDisconnectInterval = ini.GetValue("Station", "VP2PeriodicDisconnectInterval", 0);

			Latitude = ini.GetValue("Station", "Latitude", (decimal) 0.0);
			if (Latitude > 90 || Latitude < -90)
			{
				Latitude = 0;
				LogMessage($"Error, invalid latitude value in Cumulus.ini [{Latitude}], defaulting to zero.");
				rewriteRequired = true;
			}
			Longitude = ini.GetValue("Station", "Longitude", (decimal) 0.0);
			if (Longitude > 180 || Longitude < -180)
			{
				Longitude = 0;
				LogMessage($"Error, invalid longitude value in Cumulus.ini [{Longitude}], defaulting to zero.");
				rewriteRequired = true;
			}

			LatTxt = ini.GetValue("Station", "LatTxt", "");
			LatTxt = LatTxt.Replace(" ", "&nbsp;");
			LatTxt = LatTxt.Replace("°", "&#39;");
			LonTxt = ini.GetValue("Station", "LonTxt", "");
			LonTxt = LonTxt.Replace(" ", "&nbsp;");
			LonTxt = LonTxt.Replace("°", "&#39;");

			Altitude = ini.GetValue("Station", "Altitude", 0.0);
			AltitudeInFeet = ini.GetValue("Station", "AltitudeInFeet", false);
			StationOptions.AnemometerHeightM = ini.GetValue("Station", "AnemometerHeightM", 3);
			StationOptions.TimeZone = ini.GetValue("Station", "TimeZone", "");

			StationOptions.Humidity98Fix = ini.GetValue("Station", "Humidity98Fix", false);
			StationOptions.CalcWind10MinAve = ini.GetValue("Station", "Wind10MinAverage", false);
			StationOptions.UseSpeedForAvgCalc = ini.GetValue("Station", "UseSpeedForAvgCalc", false);
			StationOptions.UseSpeedForLatest = ini.GetValue("Station", "UseSpeedForLatest", false);
			StationOptions.UseRainForIsRaining = ini.GetValue("Station", "UseRainForIsRaining", false);
			StationOptions.LeafWetnessIsRainingIdx = ini.GetValue("Station", "LeafWetnessIsRainingIdx", -1);
			StationOptions.LeafWetnessIsRainingThrsh = ini.GetValue("Station", "LeafWetnessIsRainingVal", 0.0);

			StationOptions.AvgBearingMinutes = ini.GetValue("Station", "AvgBearingMinutes", 10);
			if (StationOptions.AvgBearingMinutes > 120)
			{
				StationOptions.AvgBearingMinutes = 120;
				rewriteRequired = true;
			}
			if (StationOptions.AvgBearingMinutes == 0)
			{
				StationOptions.AvgBearingMinutes = 1;
				rewriteRequired = true;
			}

			AvgBearingTime = new TimeSpan(StationOptions.AvgBearingMinutes / 60, StationOptions.AvgBearingMinutes % 60, 0);

			StationOptions.AvgSpeedMinutes = ini.GetValue("Station", "AvgSpeedMinutes", 10);
			if (StationOptions.AvgSpeedMinutes > 120)
			{
				StationOptions.AvgSpeedMinutes = 120;
				rewriteRequired = true;
			}
			if (StationOptions.AvgSpeedMinutes == 0)
			{
				StationOptions.AvgSpeedMinutes = 1;
				rewriteRequired = true;
			}

			AvgSpeedTime = new TimeSpan(StationOptions.AvgSpeedMinutes / 60, StationOptions.AvgSpeedMinutes % 60, 0);

			LogMessage("AvgSpdMins=" + StationOptions.AvgSpeedMinutes + " AvgSpdTime=" + AvgSpeedTime.ToString());

			StationOptions.PeakGustMinutes = ini.GetValue("Station", "PeakGustMinutes", 10);
			if (StationOptions.PeakGustMinutes > 120)
			{
				StationOptions.PeakGustMinutes = 120;
				rewriteRequired = true;
			}

			if (StationOptions.PeakGustMinutes == 0)
			{
				StationOptions.PeakGustMinutes = 1;
				rewriteRequired = true;
			}

			PeakGustTime = new TimeSpan(StationOptions.PeakGustMinutes / 60, StationOptions.PeakGustMinutes % 60, 0);

			StationOptions.NoSensorCheck = ini.GetValue("Station", "NoSensorCheck", false);

			StationOptions.CalculatedDP = ini.GetValue("Station", "CalculatedDP", false);
			StationOptions.CalculatedWC = ini.GetValue("Station", "CalculatedWC", false);
			StationOptions.CalculatedET = ini.GetValue("Station", "CalculatedET", false);
			RolloverHour = ini.GetValue("Station", "RolloverHour", 0);
			Use10amInSummer = ini.GetValue("Station", "Use10amInSummer", true);
			//ConfirmClose = ini.GetValue("Station", "ConfirmClose", false);
			//CloseOnSuspend = ini.GetValue("Station", "CloseOnSuspend", false);
			//RestartIfUnplugged = ini.GetValue("Station", "RestartIfUnplugged", false);
			//RestartIfDataStops = ini.GetValue("Station", "RestartIfDataStops", false);
			StationOptions.SyncTime = ini.GetValue("Station", "SyncDavisClock", false);
			StationOptions.ClockSettingHour = ini.GetValue("Station", "ClockSettingHour", 4);
			StationOptions.WS2300IgnoreStationClock = ini.GetValue("Station", "WS2300IgnoreStationClock", false);
			//WS2300Sync = ini.GetValue("Station", "WS2300Sync", false);
			StationOptions.LogExtraSensors = ini.GetValue("Station", "LogExtraSensors", false);
			StationOptions.LogMainStation = ini.GetValue("Station", "LogMainSttaion", true);
			ReportDataStoppedErrors = ini.GetValue("Station", "ReportDataStoppedErrors", true);
			ReportLostSensorContact = ini.GetValue("Station", "ReportLostSensorContact", true);
			//NoFlashWetDryDayRecords = ini.GetValue("Station", "NoFlashWetDryDayRecords", false);
			ErrorLogSpikeRemoval = ini.GetValue("Station", "ErrorLogSpikeRemoval", true);
			DataLogInterval = ini.GetValue("Station", "DataLogInterval", 2);
			// this is now an index
			if (DataLogInterval > 5)
			{
				DataLogInterval = 2;
				rewriteRequired = true;
			}

			FineOffsetOptions.SyncReads = ini.GetValue("Station", "SyncFOReads", true);
			FineOffsetOptions.ReadAvoidPeriod = ini.GetValue("Station", "FOReadAvoidPeriod", 3);
			FineOffsetOptions.ReadTime = ini.GetValue("Station", "FineOffsetReadTime", 150);
			FineOffsetOptions.SetLoggerInterval = ini.GetValue("Station", "FineOffsetSetLoggerInterval", false);
			FineOffsetOptions.VendorID = ini.GetValue("Station", "VendorID", -1);
			FineOffsetOptions.ProductID = ini.GetValue("Station", "ProductID", -1);


			Units.Wind = ini.GetValue("Station", "WindUnit", 2);
			Units.Press = ini.GetValue("Station", "PressureUnit", 1);

			Units.Rain = ini.GetValue("Station", "RainUnit", 0);
			Units.Temp = ini.GetValue("Station", "TempUnit", 0);

			StationOptions.RoundWindSpeed = ini.GetValue("Station", "RoundWindSpeed", false);
			StationOptions.PrimaryAqSensor = ini.GetValue("Station", "PrimaryAqSensor", -1);


			// Unit decimals
			RainDPlaces = RainDPlaceDefaults[Units.Rain];
			TempDPlaces = TempDPlaceDefaults[Units.Temp];
			PressDPlaces = PressDPlaceDefaults[Units.Press];
			WindDPlaces = StationOptions.RoundWindSpeed ? 0 : WindDPlaceDefaults[Units.Wind];
			WindAvgDPlaces = WindDPlaces;
			AirQualityDPlaces = 1;

			// Unit decimal overrides
			WindDPlaces = ini.GetValue("Station", "WindSpeedDecimals", WindDPlaces);
			WindAvgDPlaces = ini.GetValue("Station", "WindSpeedAvgDecimals", WindAvgDPlaces);
			WindRunDPlaces = ini.GetValue("Station", "WindRunDecimals", WindRunDPlaces);
			SunshineDPlaces = ini.GetValue("Station", "SunshineHrsDecimals", 1);
			PressDPlaces = ini.GetValue("Station", "PressDecimals", PressDPlaces);
			RainDPlaces = ini.GetValue("Station", "RainDecimals", RainDPlaces);
			TempDPlaces = ini.GetValue("Station", "TempDecimals", TempDPlaces);
			UVDPlaces = ini.GetValue("Station", "UVDecimals", UVDPlaces);
			AirQualityDPlaces = ini.GetValue("Station", "AirQualityDecimals", AirQualityDPlaces);

			if (StationType == StationTypes.VantagePro || StationType == StationTypes.VantagePro2)
			{
				// Use one more DP for Davis stations
				if (DavisOptions.IncrementPressureDP)
				{
					++PressDPlaces;
				}
			}


			LocationName = ini.GetValue("Station", "LocName", "");
			LocationDesc = ini.GetValue("Station", "LocDesc", "");

			YTDrain = ini.GetValue("Station", "YTDrain", 0.0);
			YTDrainyear = ini.GetValue("Station", "YTDrainyear", 0);

			EwOptions.Interval = ini.GetValue("Station", "EWInterval", 1.0);
			EwOptions.Filename = ini.GetValue("Station", "EWFile", "");
			//EWallowFF = ini.GetValue("Station", "EWFF", false);
			//EWdisablecheckinit = ini.GetValue("Station", "EWdisablecheckinit", false);
			//EWduplicatecheck = ini.GetValue("Station", "EWduplicatecheck", true);
			EwOptions.MinPressMB = ini.GetValue("Station", "EWminpressureMB", 900);
			EwOptions.MaxPressMB = ini.GetValue("Station", "EWmaxpressureMB", 1200);
			EwOptions.MaxRainTipDiff = ini.GetValue("Station", "EWMaxRainTipDiff", 30);
			EwOptions.PressOffset = ini.GetValue("Station", "EWpressureoffset", 9999.0);

			Spike.TempDiff = ini.GetValue("Station", "EWtempdiff", 999.0);
			Spike.PressDiff = ini.GetValue("Station", "EWpressurediff", 999.0);
			Spike.HumidityDiff = ini.GetValue("Station", "EWhumiditydiff", 999.0);
			Spike.GustDiff = ini.GetValue("Station", "EWgustdiff", 999.0);
			Spike.WindDiff = ini.GetValue("Station", "EWwinddiff", 999.0);
			Spike.MaxRainRate = ini.GetValue("Station", "EWmaxRainRate", 999.0);
			Spike.MaxHourlyRain = ini.GetValue("Station", "EWmaxHourlyRain", 999.0);
			Spike.InTempDiff = ini.GetValue("Station", "EWinTempdiff", 999.0);
			Spike.InHumDiff = ini.GetValue("Station", "EWinHumiditydiff", 999.0);

			LCMaxWind = ini.GetValue("Station", "LCMaxWind", 9999);

			RecordsBeganStr = ini.GetValue("Station", "StartDate", DateTime.Now.ToString("dd/MM/yyyy", invDate));
			RecordsBeganDate = DateTime.Now;
			// Try to convert old style localised entries to fixed format dd/MM/yyyy
			if (!DateTime.TryParseExact(RecordsBeganStr, "dd/MM/yyyy", invDate, DateTimeStyles.None, out RecordsBeganDate))
			{
				if (!DateTime.TryParseExact(RecordsBeganStr, "D", CultureInfo.CurrentCulture, DateTimeStyles.None, out RecordsBeganDate))
				{
					var msg = "Failed to parse the 'StartDate' in cumulus.ini";
					LogConsoleMessage(msg);
					LogMessage(msg);
					msg = $"Please manually update it from it's present format '{RecordsBeganStr}' to 'dd/mm/yyyy'";
					LogConsoleMessage(msg);
					LogMessage(msg);
				}
				else
					rewriteRequired = true;
			}
			RecordsBeganStr = RecordsBeganDate.ToString("dd/MM/yyyy", invDate);

			LogMessage("Cumulus start date: " + RecordsBeganStr);

			ImetOptions.WaitTime = ini.GetValue("Station", "ImetWaitTime", 500);
			ImetOptions.ReadDelay = ini.GetValue("Station", "ImetReadDelay", 500);
			ImetOptions.UpdateLogPointer = ini.GetValue("Station", "ImetUpdateLogPointer", true);
			ImetOptions.BaudRate = ini.GetValue("Station", "ImetBaudRate", 19200);
			// Check we have a valid value
			if (!ImetOptions.BaudRates.Contains(ImetOptions.BaudRate))
			{
				// nope, that isn't allowed, set the default
				LogMessage("Error, the value for ImetOptions.ImetBaudRate in the ini file " + ImetOptions.BaudRate + " is not valid, using default 19200.");
				ImetOptions.BaudRate = 19200;
				rewriteRequired = true;
			}

			UseDataLogger = ini.GetValue("Station", "UseDataLogger", true);
			UseCumulusForecast = ini.GetValue("Station", "UseCumulusForecast", false);
			HourlyForecast = ini.GetValue("Station", "HourlyForecast", false);
			StationOptions.UseCumulusPresstrendstr = ini.GetValue("Station", "UseCumulusPresstrendstr", false);
			//UseWindChillCutoff = ini.GetValue("Station", "UseWindChillCutoff", false);
			RecordSetTimeoutHrs = ini.GetValue("Station", "RecordSetTimeoutHrs", 24);

			SnowDepthHour = ini.GetValue("Station", "SnowDepthHour", 0);

			StationOptions.UseZeroBearing = ini.GetValue("Station", "UseZeroBearing", false);

			RainDayThreshold = ini.GetValue("Station", "RainDayThreshold", -1.0);

			FCpressinMB = ini.GetValue("Station", "FCpressinMB", true);
			FClowpress = ini.GetValue("Station", "FClowpress", DEFAULTFCLOWPRESS);
			FChighpress = ini.GetValue("Station", "FChighpress", DEFAULTFCHIGHPRESS);
			FCPressureThreshold = ini.GetValue("Station", "FCPressureThreshold", -1.0);

			RainSeasonStart = ini.GetValue("Station", "RainSeasonStart", 1);
			if (RainSeasonStart < 1 || RainSeasonStart > 12)
			{
				RainSeasonStart = 1;
				rewriteRequired = true;
			}
			ChillHourSeasonStart = ini.GetValue("Station", "ChillHourSeasonStart", Latitude >= 0 ? 10 : 4);
			if (ChillHourSeasonStart < 1 || ChillHourSeasonStart > 12)
			{
				ChillHourSeasonStart = 1;
				rewriteRequired = true;
			}
			ChillHourThreshold = ini.GetValue("Station", "ChillHourThreshold", -999.0);
			if (ChillHourThreshold < -998)
			{
				ChillHourThreshold = Units.Temp == 0 ? 7 : 45;
				rewriteRequired = true;
			}

			RG11Enabled = ini.GetValue("Station", "RG11Enabled", false);
			RG11Port = ini.GetValue("Station", "RG11portName", DefaultComportName);
			RG11TBRmode = ini.GetValue("Station", "RG11TBRmode", false);
			RG11tipsize = ini.GetValue("Station", "RG11tipsize", 0.0);
			RG11IgnoreFirst = ini.GetValue("Station", "RG11IgnoreFirst", false);
			RG11DTRmode = ini.GetValue("Station", "RG11DTRmode", true);

			RG11Enabled2 = ini.GetValue("Station", "RG11Enabled2", false);
			RG11Port2 = ini.GetValue("Station", "RG11port2Name", DefaultComportName);
			RG11TBRmode2 = ini.GetValue("Station", "RG11TBRmode2", false);
			RG11tipsize2 = ini.GetValue("Station", "RG11tipsize2", 0.0);
			RG11IgnoreFirst2 = ini.GetValue("Station", "RG11IgnoreFirst2", false);
			RG11DTRmode2 = ini.GetValue("Station", "RG11DTRmode2", true);

			if (FCPressureThreshold < 0)
			{
				FCPressureThreshold = Units.Press == 2 ? 0.00295333727 : 0.1;
			}

			//special_logging = ini.GetValue("Station", "SpecialLog", false);
			//solar_logging = ini.GetValue("Station", "SolarLog", false);


			//RTdisconnectcount = ini.GetValue("Station", "RTdisconnectcount", 0);

			WMR928TempChannel = ini.GetValue("Station", "WMR928TempChannel", 0);
			WMR200TempChannel = ini.GetValue("Station", "WMR200TempChannel", 1);

			// WeatherLink Live device settings
			WllApiKey = ini.GetValue("WLL", "WLv2ApiKey", "");
			WllApiSecret = ini.GetValue("WLL", "WLv2ApiSecret", "");
			WllStationId = ini.GetValue("WLL", "WLStationId", -1);
			//if (WllStationId == "-1") WllStationId = "";
			WLLAutoUpdateIpAddress = ini.GetValue("WLL", "AutoUpdateIpAddress", true);
			WllBroadcastDuration = ini.GetValue("WLL", "BroadcastDuration", WllBroadcastDuration);
			WllBroadcastPort = ini.GetValue("WLL", "BroadcastPort", WllBroadcastPort);
			WllPrimaryRain = ini.GetValue("WLL", "PrimaryRainTxId", 1);
			WllPrimaryTempHum = ini.GetValue("WLL", "PrimaryTempHumTxId", 1);
			WllPrimaryWind = ini.GetValue("WLL", "PrimaryWindTxId", 1);
			WllPrimaryRain = ini.GetValue("WLL", "PrimaryRainTxId", 1);
			WllPrimarySolar = ini.GetValue("WLL", "PrimarySolarTxId", 0);
			WllPrimaryUV = ini.GetValue("WLL", "PrimaryUvTxId", 0);
			WllExtraSoilTempTx1 = ini.GetValue("WLL", "ExtraSoilTempTxId1", 0);
			WllExtraSoilTempIdx1 = ini.GetValue("WLL", "ExtraSoilTempIdx1", 1);
			WllExtraSoilTempTx2 = ini.GetValue("WLL", "ExtraSoilTempTxId2", 0);
			WllExtraSoilTempIdx2 = ini.GetValue("WLL", "ExtraSoilTempIdx2", 2);
			WllExtraSoilTempTx3 = ini.GetValue("WLL", "ExtraSoilTempTxId3", 0);
			WllExtraSoilTempIdx3 = ini.GetValue("WLL", "ExtraSoilTempIdx3", 3);
			WllExtraSoilTempTx4 = ini.GetValue("WLL", "ExtraSoilTempTxId4", 0);
			WllExtraSoilTempIdx4 = ini.GetValue("WLL", "ExtraSoilTempIdx4", 4);
			WllExtraSoilMoistureTx1 = ini.GetValue("WLL", "ExtraSoilMoistureTxId1", 0);
			WllExtraSoilMoistureIdx1 = ini.GetValue("WLL", "ExtraSoilMoistureIdx1", 1);
			WllExtraSoilMoistureTx2 = ini.GetValue("WLL", "ExtraSoilMoistureTxId2", 0);
			WllExtraSoilMoistureIdx2 = ini.GetValue("WLL", "ExtraSoilMoistureIdx2", 2);
			WllExtraSoilMoistureTx3 = ini.GetValue("WLL", "ExtraSoilMoistureTxId3", 0);
			WllExtraSoilMoistureIdx3 = ini.GetValue("WLL", "ExtraSoilMoistureIdx3", 3);
			WllExtraSoilMoistureTx4 = ini.GetValue("WLL", "ExtraSoilMoistureTxId4", 0);
			WllExtraSoilMoistureIdx4 = ini.GetValue("WLL", "ExtraSoilMoistureIdx4", 4);
			WllExtraLeafTx1 = ini.GetValue("WLL", "ExtraLeafTxId1", 0);
			WllExtraLeafIdx1 = ini.GetValue("WLL", "ExtraLeafIdx1", 1);
			WllExtraLeafTx2 = ini.GetValue("WLL", "ExtraLeafTxId2", 0);
			WllExtraLeafIdx2 = ini.GetValue("WLL", "ExtraLeafIdx2", 2);
			for (int i = 1; i <=8; i++)
			{
				WllExtraTempTx[i - 1] = ini.GetValue("WLL", "ExtraTempTxId" + i, 0);
				WllExtraHumTx[i - 1] = ini.GetValue("WLL", "ExtraHumOnTxId" + i, false);
			}

			// GW1000 settings
			Gw1000IpAddress = ini.GetValue("GW1000", "IPAddress", "0.0.0.0");
			Gw1000MacAddress = ini.GetValue("GW1000", "MACAddress", "");
			Gw1000AutoUpdateIpAddress = ini.GetValue("GW1000", "AutoUpdateIpAddress", true);
			Gw1000PrimaryTHSensor = ini.GetValue("GW1000", "PrimaryTHSensor", 0);  // 0=default, 1-8=extra t/h sensor number
			Gw1000PrimaryRainSensor = ini.GetValue("GW1000", "PrimaryRainSensor", 0); //0=main station (tipping bucket) 1=piezo
			EcowittSettings.ExtraEnabled = ini.GetValue("GW1000", "ExtraSensorDataEnabled", false);
			EcowittSettings.ExtraUseSolar = ini.GetValue("GW1000", "ExtraSensorUseSolar", true);
			EcowittSettings.ExtraUseUv = ini.GetValue("GW1000", "ExtraSensorUseUv", true);
			EcowittSettings.ExtraUseTempHum = ini.GetValue("GW1000", "ExtraSensorUseTempHum", true);
			EcowittSettings.ExtraUseSoilTemp = ini.GetValue("GW1000", "ExtraSensorUseSoilTemp", true);
			EcowittSettings.ExtraUseSoilMoist = ini.GetValue("GW1000", "ExtraSensorUseSoilMoist", true);
			EcowittSettings.ExtraUseLeafWet = ini.GetValue("GW1000", "ExtraSensorUseLeafWet", true);
			EcowittSettings.ExtraUseUserTemp = ini.GetValue("GW1000", "ExtraSensorUseUserTemp", true);
			EcowittSettings.ExtraUseAQI = ini.GetValue("GW1000", "ExtraSensorUseAQI", true);
			EcowittSettings.ExtraUseCo2= ini.GetValue("GW1000", "ExtraSensorUseCo2", true);
			EcowittSettings.ExtraUseLightning = ini.GetValue("GW1000", "ExtraSensorUseLightning", true);
			EcowittSettings.ExtraUseLeak = ini.GetValue("GW1000", "ExtraSensorUseLeak", true);
			EcowittSettings.SetCustomServer = ini.GetValue("GW1000", "SetCustomServer", false);
			EcowittSettings.GatewayAddr = ini.GetValue("GW1000", "EcowittGwAddr", "0.0.0.0");
			var localIp = Utils.GetIpWithDefaultGateway();
			EcowittSettings.LocalAddr = ini.GetValue("GW1000", "EcowittLocalAddr", localIp.ToString());
			EcowittSettings.CustomInterval = ini.GetValue("GW1000", "EcowittCustomInterval", 16);
			//
			EcowittSettings.ExtraSetCustomServer = ini.GetValue("GW1000", "ExtraSetCustomServer", false);
			EcowittSettings.ExtraGatewayAddr = ini.GetValue("GW1000", "EcowittExtraGwAddr", "0.0.0.0");
			EcowittSettings.ExtraLocalAddr = ini.GetValue("GW1000", "EcowittExtraLocalAddr", localIp.ToString());
			EcowittSettings.ExtraCustomInterval = ini.GetValue("GW1000", "EcowittExtraCustomInterval", 16);
			// api
			EcowittSettings.AppKey = ini.GetValue("GW1000", "EcowittAppKey", "");
			EcowittSettings.UserApiKey = ini.GetValue("GW1000", "EcowittUserKey", "");
			EcowittSettings.MacAddress = ini.GetValue("GW1000", "EcowittMacAddress", "");
			if (string.IsNullOrEmpty(EcowittSettings.MacAddress) && !string.IsNullOrEmpty(Gw1000MacAddress))
			{
				EcowittSettings.MacAddress = Gw1000MacAddress;
			}
			// WN34 sensor mapping
			for (int i = 1; i <= 8; i++)
			{
				EcowittSettings.MapWN34[i] = ini.GetValue("GW1000", "WN34MapChan" + i, 0);
			}


			// Ambient settings
			AmbientExtraEnabled = ini.GetValue("Ambient", "ExtraSensorDataEnabled", false);
			AmbientExtraUseSolar = ini.GetValue("Ambient", "ExtraSensorUseSolar", true);
			AmbientExtraUseUv = ini.GetValue("Ambient", "ExtraSensorUseUv", true);
			AmbientExtraUseTempHum = ini.GetValue("Ambient", "ExtraSensorUseTempHum", true);
			AmbientExtraUseSoilTemp = ini.GetValue("Ambient", "ExtraSensorUseSoilTemp", true);
			AmbientExtraUseSoilMoist = ini.GetValue("Ambient", "ExtraSensorUseSoilMoist", true);
			//AmbientExtraUseLeafWet = ini.GetValue("Ambient", "ExtraSensorUseLeafWet", true);
			AmbientExtraUseAQI = ini.GetValue("Ambient", "ExtraSensorUseAQI", true);
			AmbientExtraUseCo2 = ini.GetValue("Ambient", "ExtraSensorUseCo2", true);
			AmbientExtraUseLightning = ini.GetValue("Ambient", "ExtraSensorUseLightning", true);
			AmbientExtraUseLeak = ini.GetValue("Ambient", "ExtraSensorUseLeak", true);

			// AirLink settings
			// We have to convert previous per AL IsNode config to global
			// So check if the global value exists
			if (ini.ValueExists("AirLink", "IsWllNode"))
			{
				AirLinkIsNode = ini.GetValue("AirLink", "IsWllNode", false);
			}
			else
			{
				AirLinkIsNode = ini.GetValue("AirLink", "In-IsNode", false) || ini.GetValue("AirLink", "Out-IsNode", false);
				rewriteRequired = true;
			}
			AirLinkApiKey = ini.GetValue("AirLink", "WLv2ApiKey", "");
			AirLinkApiSecret = ini.GetValue("AirLink", "WLv2ApiSecret", "");
			AirLinkAutoUpdateIpAddress = ini.GetValue("AirLink", "AutoUpdateIpAddress", true);
			AirLinkInEnabled = ini.GetValue("AirLink", "In-Enabled", false);
			AirLinkInIPAddr = ini.GetValue("AirLink", "In-IPAddress", "0.0.0.0");
			AirLinkInStationId = ini.GetValue("AirLink", "In-WLStationId", -1);
			if (AirLinkInStationId == -1 && AirLinkIsNode)
			{
				AirLinkInStationId = WllStationId;
				rewriteRequired = true;
			}
			AirLinkInHostName = ini.GetValue("AirLink", "In-Hostname", "");

			AirLinkOutEnabled = ini.GetValue("AirLink", "Out-Enabled", false);
			AirLinkOutIPAddr = ini.GetValue("AirLink", "Out-IPAddress", "0.0.0.0");
			AirLinkOutStationId = ini.GetValue("AirLink", "Out-WLStationId", -1);
			if (AirLinkOutStationId == -1 && AirLinkIsNode)
			{
				AirLinkOutStationId = WllStationId;
				rewriteRequired = true;
			}
			AirLinkOutHostName = ini.GetValue("AirLink", "Out-Hostname", "");

			airQualityIndex = ini.GetValue("AirLink", "AQIformula", 0);

			FtpOptions.Enabled = ini.GetValue("FTP site", "Enabled", true);
			FtpOptions.Hostname = ini.GetValue("FTP site", "Host", "");
			FtpOptions.Port = ini.GetValue("FTP site", "Port", 21);
			FtpOptions.Username = ini.GetValue("FTP site", "Username", "");
			FtpOptions.Password = ini.GetValue("FTP site", "Password", "");
			FtpOptions.Directory = ini.GetValue("FTP site", "Directory", "");
			if (FtpOptions.Hostname == "" && FtpOptions.Enabled)
			{
				FtpOptions.Enabled = false;
				rewriteRequired = true;
			}

			FtpOptions.AutoDetect = ini.GetValue("FTP site", "ConnectionAutoDetect", false);
			FtpOptions.IgnoreCertErrors = ini.GetValue("FTP site", "IgnoreCertErrors", false);
			FtpOptions.ActiveMode = ini.GetValue("FTP site", "ActiveFTP", false);
			FtpOptions.FtpMode = (FtpProtocols)ini.GetValue("FTP site", "Sslftp", 0);
			// BUILD 3092 - added alternate SFTP authentication options
			FtpOptions.SshAuthen = ini.GetValue("FTP site", "SshFtpAuthentication", "password");
			if (!sshAuthenticationVals.Contains(FtpOptions.SshAuthen))
			{
				FtpOptions.SshAuthen = "password";
				LogMessage($"Error, invalid SshFtpAuthentication value in Cumulus.ini [{FtpOptions.SshAuthen}], defaulting to Password.");
				rewriteRequired = true;
			}
			FtpOptions.SshPskFile = ini.GetValue("FTP site", "SshFtpPskFile", "");
			if ((FtpOptions.SshAuthen == "psk" || FtpOptions.SshAuthen == "password_psk") && (string.IsNullOrEmpty(FtpOptions.SshPskFile) || !File.Exists(FtpOptions.SshPskFile)))
			{
				LogMessage($"Error, file name specified by SshFtpPskFile value in Cumulus.ini does not exist [{FtpOptions.SshPskFile}].");
				rewriteRequired = true;
			}
			FtpOptions.DisableEPSV = ini.GetValue("FTP site", "DisableEPSV", false);
			FtpOptions.DisableExplicit = ini.GetValue("FTP site", "DisableFtpsExplicit", false);
			FtpOptions.Logging = ini.GetValue("FTP site", "FTPlogging", false);
			FtpOptions.LoggingLevel = ini.GetValue("FTP site", "FTPloggingLevel", 2);
			RealtimeIntervalEnabled = ini.GetValue("FTP site", "EnableRealtime", false);
			FtpOptions.RealtimeEnabled = ini.GetValue("FTP site", "RealtimeFTPEnabled", false);

			// Local Copy Options
			FtpOptions.LocalCopyEnabled = ini.GetValue("FTP site", "EnableLocalCopy", false);
			//FtpOptions.LocalCopyRealtimeEnabled = ini.GetValue("FTP site", "EnableRealtimeLocalCopy", false);
			FtpOptions.LocalCopyFolder = ini.GetValue("FTP site", "LocalCopyFolder", "");
			var sep1 = Path.DirectorySeparatorChar.ToString();
			var sep2 = Path.AltDirectorySeparatorChar.ToString();
			if (FtpOptions.LocalCopyFolder.Length > 1 &&
				!(FtpOptions.LocalCopyFolder.EndsWith(sep1) || FtpOptions.LocalCopyFolder.EndsWith(sep2))
				)
			{
				FtpOptions.LocalCopyFolder += sep1;
				rewriteRequired = true;
			}

			MoonImage.Ftp = ini.GetValue("FTP site", "IncludeMoonImage", false);
			MoonImage.Copy = ini.GetValue("FTP site", "CopyMoonImage", false);


			RealtimeFiles[0].Create = ini.GetValue("FTP site", "RealtimeTxtCreate", false);
			RealtimeFiles[0].FTP = RealtimeFiles[0].Create && ini.GetValue("FTP site", "RealtimeTxtFTP", false);
			RealtimeFiles[0].Copy = RealtimeFiles[0].Create && ini.GetValue("FTP site", "RealtimeTxtCopy", false);
			RealtimeFiles[1].Create = ini.GetValue("FTP site", "RealtimeGaugesTxtCreate", false);
			RealtimeFiles[1].FTP = RealtimeFiles[1].Create && ini.GetValue("FTP site", "RealtimeGaugesTxtFTP", false);
			RealtimeFiles[1].Copy = RealtimeFiles[1].Create && ini.GetValue("FTP site", "RealtimeGaugesTxtCopy", false);

			RealtimeInterval = ini.GetValue("FTP site", "RealtimeInterval", 30000);
			if (RealtimeInterval < 1)
			{
				RealtimeInterval = 1;
				rewriteRequired = true;
			}
			//RealtimeTimer.Change(0,RealtimeInterval);

			WebAutoUpdate = ini.GetValue("FTP site", "AutoUpdate", false);  // Deprecated, to be remove at some future date
			// Have to allow for upgrade, set interval enabled to old WebAutoUpdate
			WebIntervalEnabled = ini.GetValue("FTP site", "IntervalEnabled", WebAutoUpdate);
			FtpOptions.IntervalEnabled = ini.GetValue("FTP site", "IntervalFtpEnabled", WebAutoUpdate); ;

			UpdateInterval = ini.GetValue("FTP site", "UpdateInterval", DefaultWebUpdateInterval);
			if (UpdateInterval<1)
			{
				UpdateInterval = 1;
				rewriteRequired = true;
			}
			SynchronisedWebUpdate = (60 % UpdateInterval == 0);

			var IncludeStandardFiles = false;
			if (ini.ValueExists("FTP site", "IncludeSTD"))
			{
				IncludeStandardFiles = ini.GetValue("FTP site", "IncludeSTD", false);
			}
			for (var i = 0; i < StdWebFiles.Length; i++)
			{
				var keyNameCreate = "Create-" + StdWebFiles[i].LocalFileName.Split('.')[0];
				var keyNameFTP = "Ftp-" + StdWebFiles[i].LocalFileName.Split('.')[0];
				var keyNameCopy = "Copy-" + StdWebFiles[i].LocalFileName.Split('.')[0];
				StdWebFiles[i].Create = ini.GetValue("FTP site", keyNameCreate, IncludeStandardFiles);
				StdWebFiles[i].FTP = ini.GetValue("FTP site", keyNameFTP, IncludeStandardFiles);
				StdWebFiles[i].Copy = ini.GetValue("FTP site", keyNameCopy, IncludeStandardFiles);
			}

			var IncludeGraphDataFiles = false;
			if (ini.ValueExists("FTP site", "IncludeGraphDataFiles"))
			{
				IncludeGraphDataFiles = ini.GetValue("FTP site", "IncludeGraphDataFiles", true);
			}
			for (var i = 0; i < GraphDataFiles.Length; i++)
			{
				var keyNameCreate = "Create-" + GraphDataFiles[i].LocalFileName.Split('.')[0];
				var keyNameFTP = "Ftp-" + GraphDataFiles[i].LocalFileName.Split('.')[0];
				var keyNameCopy = "Copy-" + GraphDataFiles[i].LocalFileName.Split('.')[0];
				GraphDataFiles[i].Create = ini.GetValue("FTP site", keyNameCreate, IncludeGraphDataFiles);
				GraphDataFiles[i].FTP = ini.GetValue("FTP site", keyNameFTP, IncludeGraphDataFiles);
				GraphDataFiles[i].Copy = ini.GetValue("FTP site", keyNameCopy, IncludeGraphDataFiles);
			}
			for (var i = 0; i < GraphDataEodFiles.Length; i++)
			{
				var keyNameCreate = "Create-" + GraphDataEodFiles[i].LocalFileName.Split('.')[0];
				var keyNameFTP = "Ftp-" + GraphDataEodFiles[i].LocalFileName.Split('.')[0];
				var keyNameCopy = "Copy-" + GraphDataEodFiles[i].LocalFileName.Split('.')[0];
				GraphDataEodFiles[i].Create = ini.GetValue("FTP site", keyNameCreate, IncludeGraphDataFiles);
				GraphDataEodFiles[i].FTP = ini.GetValue("FTP site", keyNameFTP, IncludeGraphDataFiles);
				GraphDataEodFiles[i].Copy = ini.GetValue("FTP site", keyNameCopy, IncludeGraphDataFiles);
			}

			FTPRename = ini.GetValue("FTP site", "FTPRename", true);
			UTF8encode = ini.GetValue("FTP site", "UTF8encode", true);
			DeleteBeforeUpload = ini.GetValue("FTP site", "DeleteBeforeUpload", false);

			//MaxFTPconnectRetries = ini.GetValue("FTP site", "MaxFTPconnectRetries", 3);

			for (int i = 0; i < numextrafiles; i++)
			{
				ExtraFiles[i].local = ini.GetValue("FTP site", "ExtraLocal" + i, "");
				ExtraFiles[i].remote = ini.GetValue("FTP site", "ExtraRemote" + i, "");
				ExtraFiles[i].process = ini.GetValue("FTP site", "ExtraProcess" + i, false);
				ExtraFiles[i].binary = ini.GetValue("FTP site", "ExtraBinary" + i, false);
				ExtraFiles[i].realtime = ini.GetValue("FTP site", "ExtraRealtime" + i, false);
				ExtraFiles[i].FTP = ini.GetValue("FTP site", "ExtraFTP" + i, true);
				ExtraFiles[i].UTF8 = ini.GetValue("FTP site", "ExtraUTF" + i, false);
				ExtraFiles[i].endofday = ini.GetValue("FTP site", "ExtraEOD" + i, false);
			}

			ExternalProgram = ini.GetValue("FTP site", "ExternalProgram", "");
			RealtimeProgram = ini.GetValue("FTP site", "RealtimeProgram", "");
			DailyProgram = ini.GetValue("FTP site", "DailyProgram", "");
			ExternalParams = ini.GetValue("FTP site", "ExternalParams", "");
			RealtimeParams = ini.GetValue("FTP site", "RealtimeParams", "");
			DailyParams = ini.GetValue("FTP site", "DailyParams", "");

			ForumURL = ini.GetValue("Web Site", "ForumURL", ForumDefault);
			WebcamURL = ini.GetValue("Web Site", "WebcamURL", WebcamDefault);

			CloudBaseInFeet = ini.GetValue("Station", "CloudBaseInFeet", true);

			GraphDays = ini.GetValue("Graphs", "ChartMaxDays", 31);
			GraphHours = ini.GetValue("Graphs", "GraphHours", 72);
			RecentDataDays = (int)Math.Ceiling(Math.Max(7, GraphHours / 24.0));
			MoonImage.Enabled = ini.GetValue("Graphs", "MoonImageEnabled", false);
			MoonImage.Size = ini.GetValue("Graphs", "MoonImageSize", 100);
			if (MoonImage.Size < 10)
			{
				MoonImage.Size = 10;
				rewriteRequired = true;
			}
			MoonImage.Transparent = ini.GetValue("Graphs", "MoonImageShadeTransparent", false);
			MoonImage.FtpDest = ini.GetValue("Graphs", "MoonImageFtpDest", "images/moon.png");
			MoonImage.CopyDest = ini.GetValue("Graphs", "MoonImageCopyDest", FtpOptions.LocalCopyFolder + "images" + sep1 + "moon.png");
			GraphOptions.TempVisible = ini.GetValue("Graphs", "TempVisible", true);
			GraphOptions.InTempVisible = ini.GetValue("Graphs", "InTempVisible", true);
			GraphOptions.HIVisible = ini.GetValue("Graphs", "HIVisible", true);
			GraphOptions.DPVisible = ini.GetValue("Graphs", "DPVisible", true);
			GraphOptions.WCVisible = ini.GetValue("Graphs", "WCVisible", true);
			GraphOptions.AppTempVisible = ini.GetValue("Graphs", "AppTempVisible", true);
			GraphOptions.FeelsLikeVisible = ini.GetValue("Graphs", "FeelsLikeVisible", true);
			GraphOptions.HumidexVisible = ini.GetValue("Graphs", "HumidexVisible", true);
			GraphOptions.InHumVisible = ini.GetValue("Graphs", "InHumVisible", true);
			GraphOptions.OutHumVisible = ini.GetValue("Graphs", "OutHumVisible", true);
			GraphOptions.UVVisible = ini.GetValue("Graphs", "UVVisible", true);
			GraphOptions.SolarVisible = ini.GetValue("Graphs", "SolarVisible", true);
			GraphOptions.SunshineVisible = ini.GetValue("Graphs", "SunshineVisible", true);
			GraphOptions.DailyAvgTempVisible = ini.GetValue("Graphs", "DailyAvgTempVisible", true);
			GraphOptions.DailyMaxTempVisible = ini.GetValue("Graphs", "DailyMaxTempVisible", true);
			GraphOptions.DailyMinTempVisible = ini.GetValue("Graphs", "DailyMinTempVisible", true);
			GraphOptions.GrowingDegreeDaysVisible1 = ini.GetValue("Graphs", "GrowingDegreeDaysVisible1", true);
			GraphOptions.GrowingDegreeDaysVisible2 = ini.GetValue("Graphs", "GrowingDegreeDaysVisible2", true);
			GraphOptions.TempSumVisible0 = ini.GetValue("Graphs", "TempSumVisible0", true);
			GraphOptions.TempSumVisible1 = ini.GetValue("Graphs", "TempSumVisible1", true);
			GraphOptions.TempSumVisible2 = ini.GetValue("Graphs", "TempSumVisible2", true);
			GraphOptions.ExtraTempVisible = ini.GetValue("Graphs", "ExtraTempVisible", new bool[10]);
			GraphOptions.ExtraHumVisible = ini.GetValue("Graphs", "ExtraHumVisible", new bool[10]);
			GraphOptions.SoilTempVisible = ini.GetValue("Graphs", "SoilTempVisible", new bool[16]);
			GraphOptions.SoilMoistVisible = ini.GetValue("Graphs", "SoilMoistVisible", new bool[16]);
			GraphOptions.UserTempVisible = ini.GetValue("Graphs", "UserTempVisible", new bool[8]);


			Wund.ID = ini.GetValue("Wunderground", "ID", "");
			Wund.PW = ini.GetValue("Wunderground", "Password", "");
			Wund.Enabled = ini.GetValue("Wunderground", "Enabled", false);
			Wund.RapidFireEnabled = ini.GetValue("Wunderground", "RapidFire", false);
			Wund.Interval = ini.GetValue("Wunderground", "Interval", Wund.DefaultInterval);
			//WundHTTPLogging = ini.GetValue("Wunderground", "Logging", false);
			Wund.SendUV = ini.GetValue("Wunderground", "SendUV", false);
			Wund.SendSolar = ini.GetValue("Wunderground", "SendSR", false);
			Wund.SendIndoor = ini.GetValue("Wunderground", "SendIndoor", false);
			Wund.SendSoilTemp1 = ini.GetValue("Wunderground", "SendSoilTemp1", false);
			Wund.SendSoilTemp2 = ini.GetValue("Wunderground", "SendSoilTemp2", false);
			Wund.SendSoilTemp3 = ini.GetValue("Wunderground", "SendSoilTemp3", false);
			Wund.SendSoilTemp4 = ini.GetValue("Wunderground", "SendSoilTemp4", false);
			Wund.SendSoilMoisture1 = ini.GetValue("Wunderground", "SendSoilMoisture1", false);
			Wund.SendSoilMoisture2 = ini.GetValue("Wunderground", "SendSoilMoisture2", false);
			Wund.SendSoilMoisture3 = ini.GetValue("Wunderground", "SendSoilMoisture3", false);
			Wund.SendSoilMoisture4 = ini.GetValue("Wunderground", "SendSoilMoisture4", false);
			Wund.SendLeafWetness1 = ini.GetValue("Wunderground", "SendLeafWetness1", false);
			Wund.SendLeafWetness2 = ini.GetValue("Wunderground", "SendLeafWetness2", false);
			Wund.SendAirQuality = ini.GetValue("Wunderground", "SendAirQuality", false);
			Wund.SendAverage = ini.GetValue("Wunderground", "SendAverage", false);
			Wund.CatchUp = ini.GetValue("Wunderground", "CatchUp", true);

			Wund.SynchronisedUpdate = !Wund.RapidFireEnabled;

			Windy.ApiKey = ini.GetValue("Windy", "APIkey", "");
			Windy.StationIdx = ini.GetValue("Windy", "StationIdx", 0);
			Windy.Enabled = ini.GetValue("Windy", "Enabled", false);
			Windy.Interval = ini.GetValue("Windy", "Interval", Windy.DefaultInterval);
			if (Windy.Interval < 5)
			{
				Windy.Interval = 5;
				rewriteRequired = true;
			}
			//WindyHTTPLogging = ini.GetValue("Windy", "Logging", false);
			Windy.SendUV = ini.GetValue("Windy", "SendUV", false);
			Windy.SendSolar = ini.GetValue("Windy", "SendSolar", false);
			Windy.CatchUp = ini.GetValue("Windy", "CatchUp", false);

			AWEKAS.ID = ini.GetValue("Awekas", "User", "");
			AWEKAS.PW = ini.GetValue("Awekas", "Password", "");
			AWEKAS.Enabled = ini.GetValue("Awekas", "Enabled", false);
			AWEKAS.Interval = ini.GetValue("Awekas", "Interval", AWEKAS.DefaultInterval);
			if (AWEKAS.Interval < 15)
			{
				AWEKAS.Interval = 15;
				rewriteRequired = true;
			}
			AWEKAS.Lang = ini.GetValue("Awekas", "Language", "en");
			AWEKAS.OriginalInterval = AWEKAS.Interval;
			AWEKAS.SendUV = ini.GetValue("Awekas", "SendUV", false);
			AWEKAS.SendSolar = ini.GetValue("Awekas", "SendSR", false);
			AWEKAS.SendSoilTemp = ini.GetValue("Awekas", "SendSoilTemp", false);
			AWEKAS.SendIndoor = ini.GetValue("Awekas", "SendIndoor", false);
			AWEKAS.SendSoilMoisture = ini.GetValue("Awekas", "SendSoilMoisture", false);
			AWEKAS.SendLeafWetness = ini.GetValue("Awekas", "SendLeafWetness", false);
			AWEKAS.SendAirQuality = ini.GetValue("Awekas", "SendAirQuality", false);

			AWEKAS.SynchronisedUpdate = (AWEKAS.Interval % 60 == 0);

			WindGuru.ID = ini.GetValue("WindGuru", "StationUID", "");
			WindGuru.PW = ini.GetValue("WindGuru", "Password", "");
			WindGuru.Enabled = ini.GetValue("WindGuru", "Enabled", false);
			WindGuru.Interval = ini.GetValue("WindGuru", "Interval", WindGuru.DefaultInterval);
			if (WindGuru.Interval < 1)
			{
				WindGuru.Interval = 1;
				rewriteRequired = true;
			}
			WindGuru.SendRain = ini.GetValue("WindGuru", "SendRain", false);

			WCloud.ID = ini.GetValue("WeatherCloud", "Wid", "");
			WCloud.PW = ini.GetValue("WeatherCloud", "Key", "");
			WCloud.Enabled = ini.GetValue("WeatherCloud", "Enabled", false);
			WCloud.Interval = ini.GetValue("WeatherCloud", "Interval", WCloud.DefaultInterval);
			WCloud.SendUV = ini.GetValue("WeatherCloud", "SendUV", false);
			WCloud.SendSolar = ini.GetValue("WeatherCloud", "SendSR", false);
			WCloud.SendAirQuality = ini.GetValue("WeatherCloud", "SendAirQuality", false);
			WCloud.SendSoilMoisture = ini.GetValue("WeatherCloud", "SendSoilMoisture", false);
			WCloud.SoilMoistureSensor= ini.GetValue("WeatherCloud", "SoilMoistureSensor", 1);
			WCloud.SendLeafWetness = ini.GetValue("WeatherCloud", "SendLeafWetness", false);
			WCloud.LeafWetnessSensor = ini.GetValue("WeatherCloud", "LeafWetnessSensor", 1);

			//Twitter.ID = ini.GetValue("Twitter", "User", "");
			//Twitter.PW = ini.GetValue("Twitter", "Password", "");
			//Twitter.Enabled = ini.GetValue("Twitter", "Enabled", false);
			//Twitter.Interval = ini.GetValue("Twitter", "Interval", 60);
			//if (Twitter.Interval < 1)
			//{
			//	Twitter.Interval = 1;
			//	rewriteRequired = true;
			//}
			//Twitter.OauthToken = ini.GetValue("Twitter", "OauthToken", "unknown");
			//Twitter.OauthTokenSecret = ini.GetValue("Twitter", "OauthTokenSecret", "unknown");
			//Twitter.SendLocation = ini.GetValue("Twitter", "SendLocation", true);

			//if HTTPLogging then
			//  MainForm.WUHTTP.IcsLogger = MainForm.HTTPlogger;

			PWS.ID = ini.GetValue("PWSweather", "ID", "");
			PWS.PW = ini.GetValue("PWSweather", "Password", "");
			PWS.Enabled = ini.GetValue("PWSweather", "Enabled", false);
			PWS.Interval = ini.GetValue("PWSweather", "Interval", PWS.DefaultInterval);
			if (PWS.Interval < 1)
			{
				PWS.Interval = 1;
				rewriteRequired = true;
			}
			PWS.SendUV = ini.GetValue("PWSweather", "SendUV", false);
			PWS.SendSolar = ini.GetValue("PWSweather", "SendSR", false);
			PWS.CatchUp = ini.GetValue("PWSweather", "CatchUp", true);

			WOW.ID = ini.GetValue("WOW", "ID", "");
			WOW.PW = ini.GetValue("WOW", "Password", "");
			WOW.Enabled = ini.GetValue("WOW", "Enabled", false);
			WOW.Interval = ini.GetValue("WOW", "Interval", WOW.DefaultInterval);
			if (WOW.Interval < 1)
			{
				WOW.Interval = 1;
				rewriteRequired = true;
			}
			WOW.SendUV = ini.GetValue("WOW", "SendUV", false);
			WOW.SendSolar = ini.GetValue("WOW", "SendSR", false);
			WOW.SendSoilTemp = ini.GetValue("WOW", "SendSoilTemp", false);
			WOW.SoilTempSensor = ini.GetValue("WOW", "SoilTempSensor", 1);
			WOW.CatchUp = ini.GetValue("WOW", "CatchUp", false);

			APRS.ID = ini.GetValue("APRS", "ID", "");
			APRS.PW = ini.GetValue("APRS", "pass", "");
			APRS.Server = ini.GetValue("APRS", "server", "cwop.aprs.net");
			APRS.Port = ini.GetValue("APRS", "port", 14580);
			APRS.Enabled = ini.GetValue("APRS", "Enabled", false);
			APRS.Interval = ini.GetValue("APRS", "Interval", APRS.DefaultInterval);
			if (APRS.Interval < 1)
			{
				APRS.Interval = 1;
				rewriteRequired = true;
			}
			APRS.HumidityCutoff = ini.GetValue("APRS", "APRSHumidityCutoff", false);
			APRS.SendSolar = ini.GetValue("APRS", "SendSR", false);

			OpenWeatherMap.Enabled = ini.GetValue("OpenWeatherMap", "Enabled", false);
			OpenWeatherMap.CatchUp = ini.GetValue("OpenWeatherMap", "CatchUp", true);
			OpenWeatherMap.PW = ini.GetValue("OpenWeatherMap", "APIkey", "");
			OpenWeatherMap.ID = ini.GetValue("OpenWeatherMap", "StationId", "");
			OpenWeatherMap.Interval = ini.GetValue("OpenWeatherMap", "Interval", OpenWeatherMap.DefaultInterval);

			MQTT.Server = ini.GetValue("MQTT", "Server", "");
			MQTT.Port = ini.GetValue("MQTT", "Port", 1883);
			MQTT.IpVersion = ini.GetValue("MQTT", "IPversion", 0); // 0 = unspecified, 4 = force IPv4, 6 = force IPv6
			if (MQTT.IpVersion != 0 && MQTT.IpVersion != 4 && MQTT.IpVersion != 6)
			{
				MQTT.IpVersion = 0;
				rewriteRequired = true;
			}
			MQTT.UseTLS = ini.GetValue("MQTT", "UseTLS", false);
			MQTT.Username = ini.GetValue("MQTT", "Username", "");
			MQTT.Password = ini.GetValue("MQTT", "Password", "");
			MQTT.EnableDataUpdate = ini.GetValue("MQTT", "EnableDataUpdate", false);
			MQTT.UpdateTemplate = ini.GetValue("MQTT", "UpdateTemplate", "DataUpdateTemplate.txt");
			MQTT.EnableInterval = ini.GetValue("MQTT", "EnableInterval", false);
			MQTT.IntervalTime = ini.GetValue("MQTT", "IntervalTime", 600); // default to 10 minutes
			MQTT.IntervalTemplate = ini.GetValue("MQTT", "IntervalTemplate", "IntervalTemplate.txt");

			LowTempAlarm.Value = ini.GetValue("Alarms", "alarmlowtemp", 0.0);
			LowTempAlarm.Enabled = ini.GetValue("Alarms", "LowTempAlarmSet", false);
			LowTempAlarm.Sound = ini.GetValue("Alarms", "LowTempAlarmSound", false);
			LowTempAlarm.SoundFile = ini.GetValue("Alarms", "LowTempAlarmSoundFile", DefaultSoundFile);
			if (LowTempAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				LowTempAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			LowTempAlarm.Notify = ini.GetValue("Alarms", "LowTempAlarmNotify", false);
			LowTempAlarm.Email = ini.GetValue("Alarms", "LowTempAlarmEmail", false);
			LowTempAlarm.Latch = ini.GetValue("Alarms", "LowTempAlarmLatch", false);
			LowTempAlarm.LatchHours = ini.GetValue("Alarms", "LowTempAlarmLatchHours", 24);
			LowTempAlarm.Action = ini.GetValue("Alarms", "LowTempAlarmAction", "");
			LowTempAlarm.ActionParams = ini.GetValue("Alarms", "LowTempAlarmActionParams", "");

			HighTempAlarm.Value = ini.GetValue("Alarms", "alarmhightemp", 0.0);
			HighTempAlarm.Enabled = ini.GetValue("Alarms", "HighTempAlarmSet", false);
			HighTempAlarm.Sound = ini.GetValue("Alarms", "HighTempAlarmSound", false);
			HighTempAlarm.SoundFile = ini.GetValue("Alarms", "HighTempAlarmSoundFile", DefaultSoundFile);
			if (HighTempAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				HighTempAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			HighTempAlarm.Notify = ini.GetValue("Alarms", "HighTempAlarmNotify", false);
			HighTempAlarm.Email = ini.GetValue("Alarms", "HighTempAlarmEmail", false);
			HighTempAlarm.Latch = ini.GetValue("Alarms", "HighTempAlarmLatch", false);
			HighTempAlarm.LatchHours = ini.GetValue("Alarms", "HighTempAlarmLatchHours", 24);
			HighTempAlarm.Action = ini.GetValue("Alarms", "HighTempAlarmAction", "");
			HighTempAlarm.ActionParams = ini.GetValue("Alarms", "HighTempAlarmActionParams", "");

			TempChangeAlarm.Value = ini.GetValue("Alarms", "alarmtempchange", 0.0);
			TempChangeAlarm.Enabled = ini.GetValue("Alarms", "TempChangeAlarmSet", false);
			TempChangeAlarm.Sound = ini.GetValue("Alarms", "TempChangeAlarmSound", false);
			TempChangeAlarm.SoundFile = ini.GetValue("Alarms", "TempChangeAlarmSoundFile", DefaultSoundFile);
			if (TempChangeAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				TempChangeAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			TempChangeAlarm.Notify = ini.GetValue("Alarms", "TempChangeAlarmNotify", false);
			TempChangeAlarm.Email = ini.GetValue("Alarms", "TempChangeAlarmEmail", false);
			TempChangeAlarm.Latch = ini.GetValue("Alarms", "TempChangeAlarmLatch", false);
			TempChangeAlarm.LatchHours = ini.GetValue("Alarms", "TempChangeAlarmLatchHours", 24);
			TempChangeAlarm.Action = ini.GetValue("Alarms", "TempChangeAlarmAction", "");
			TempChangeAlarm.ActionParams = ini.GetValue("Alarms", "TempChangeAlarmActionParams", "");

			LowPressAlarm.Value = ini.GetValue("Alarms", "alarmlowpress", 0.0);
			LowPressAlarm.Enabled = ini.GetValue("Alarms", "LowPressAlarmSet", false);
			LowPressAlarm.Sound = ini.GetValue("Alarms", "LowPressAlarmSound", false);
			LowPressAlarm.SoundFile = ini.GetValue("Alarms", "LowPressAlarmSoundFile", DefaultSoundFile);
			if (LowPressAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				LowPressAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			LowPressAlarm.Notify = ini.GetValue("Alarms", "LowPressAlarmNotify", false);
			LowPressAlarm.Email = ini.GetValue("Alarms", "LowPressAlarmEmail", false);
			LowPressAlarm.Latch = ini.GetValue("Alarms", "LowPressAlarmLatch", false);
			LowPressAlarm.LatchHours = ini.GetValue("Alarms", "LowPressAlarmLatchHours", 24);
			LowPressAlarm.Action = ini.GetValue("Alarms", "LowPressAlarmAction", "");
			LowPressAlarm.ActionParams = ini.GetValue("Alarms", "LowPressAlarmActionParams", "");

			HighPressAlarm.Value = ini.GetValue("Alarms", "alarmhighpress", 0.0);
			HighPressAlarm.Enabled = ini.GetValue("Alarms", "HighPressAlarmSet", false);
			HighPressAlarm.Sound = ini.GetValue("Alarms", "HighPressAlarmSound", false);
			HighPressAlarm.SoundFile = ini.GetValue("Alarms", "HighPressAlarmSoundFile", DefaultSoundFile);
			if (HighPressAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				HighPressAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			HighPressAlarm.Notify = ini.GetValue("Alarms", "HighPressAlarmNotify", false);
			HighPressAlarm.Email = ini.GetValue("Alarms", "HighPressAlarmEmail", false);
			HighPressAlarm.Latch = ini.GetValue("Alarms", "HighPressAlarmLatch", false);
			HighPressAlarm.LatchHours = ini.GetValue("Alarms", "HighPressAlarmLatchHours", 24);
			HighPressAlarm.Action = ini.GetValue("Alarms", "HighPressAlarmAction", "");
			HighPressAlarm.ActionParams = ini.GetValue("Alarms", "HighPressAlarmActionParams", "");

			PressChangeAlarm.Value = ini.GetValue("Alarms", "alarmpresschange", 0.0);
			PressChangeAlarm.Enabled = ini.GetValue("Alarms", "PressChangeAlarmSet", false);
			PressChangeAlarm.Sound = ini.GetValue("Alarms", "PressChangeAlarmSound", false);
			PressChangeAlarm.SoundFile = ini.GetValue("Alarms", "PressChangeAlarmSoundFile", DefaultSoundFile);
			if (PressChangeAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				PressChangeAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			PressChangeAlarm.Notify = ini.GetValue("Alarms", "PressChangeAlarmNotify", false);
			PressChangeAlarm.Email = ini.GetValue("Alarms", "PressChangeAlarmEmail", false);
			PressChangeAlarm.Latch = ini.GetValue("Alarms", "PressChangeAlarmLatch", false);
			PressChangeAlarm.LatchHours = ini.GetValue("Alarms", "PressChangeAlarmLatchHours", 24);
			PressChangeAlarm.Action = ini.GetValue("Alarms", "PressChangeAlarmAction", "");
			PressChangeAlarm.ActionParams = ini.GetValue("Alarms", "PressChangeAlarmActionParams", "");

			HighRainTodayAlarm.Value = ini.GetValue("Alarms", "alarmhighraintoday", 0.0);
			HighRainTodayAlarm.Enabled = ini.GetValue("Alarms", "HighRainTodayAlarmSet", false);
			HighRainTodayAlarm.Sound = ini.GetValue("Alarms", "HighRainTodayAlarmSound", false);
			HighRainTodayAlarm.SoundFile = ini.GetValue("Alarms", "HighRainTodayAlarmSoundFile", DefaultSoundFile);
			if (HighRainTodayAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				HighRainTodayAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			HighRainTodayAlarm.Notify = ini.GetValue("Alarms", "HighRainTodayAlarmNotify", false);
			HighRainTodayAlarm.Email = ini.GetValue("Alarms", "HighRainTodayAlarmEmail", false);
			HighRainTodayAlarm.Latch = ini.GetValue("Alarms", "HighRainTodayAlarmLatch", false);
			HighRainTodayAlarm.LatchHours = ini.GetValue("Alarms", "HighRainTodayAlarmLatchHours", 24);
			HighRainTodayAlarm.Action = ini.GetValue("Alarms", "HighRainTodayAlarmAction", "");
			HighRainTodayAlarm.ActionParams = ini.GetValue("Alarms", "HighRainTodayAlarmActionParams", "");

			HighRainRateAlarm.Value = ini.GetValue("Alarms", "alarmhighrainrate", 0.0);
			HighRainRateAlarm.Enabled = ini.GetValue("Alarms", "HighRainRateAlarmSet", false);
			HighRainRateAlarm.Sound = ini.GetValue("Alarms", "HighRainRateAlarmSound", false);
			HighRainRateAlarm.SoundFile = ini.GetValue("Alarms", "HighRainRateAlarmSoundFile", DefaultSoundFile);
			if (HighRainRateAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				HighRainRateAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			HighRainRateAlarm.Notify = ini.GetValue("Alarms", "HighRainRateAlarmNotify", false);
			HighRainRateAlarm.Email = ini.GetValue("Alarms", "HighRainRateAlarmEmail", false);
			HighRainRateAlarm.Latch = ini.GetValue("Alarms", "HighRainRateAlarmLatch", false);
			HighRainRateAlarm.LatchHours = ini.GetValue("Alarms", "HighRainRateAlarmLatchHours", 24);
			HighRainRateAlarm.Action = ini.GetValue("Alarms", "HighRainRateAlarmAction", "");
			HighRainRateAlarm.ActionParams = ini.GetValue("Alarms", "HighRainRateAlarmActionParams", "");

			IsRainingAlarm.Enabled = ini.GetValue("Alarms", "IsRainingAlarmSet", false);
			IsRainingAlarm.Sound = ini.GetValue("Alarms", "IsRainingAlarmSound", false);
			IsRainingAlarm.SoundFile = ini.GetValue("Alarms", "IsRainingAlarmSoundFile", DefaultSoundFile);
			IsRainingAlarm.Notify = ini.GetValue("Alarms", "IsRainingAlarmNotify", false);
			IsRainingAlarm.Email = ini.GetValue("Alarms", "IsRainingAlarmEmail", false);
			IsRainingAlarm.Latch = ini.GetValue("Alarms", "IsRainingAlarmLatch", false);
			IsRainingAlarm.LatchHours = ini.GetValue("Alarms", "IsRainingAlarmLatchHours", 1);
			IsRainingAlarm.Action = ini.GetValue("Alarms", "IsRainingAlarmAction", "");
			IsRainingAlarm.ActionParams = ini.GetValue("Alarms", "IsRainingAlarmActionParams", "");

			HighGustAlarm.Value = ini.GetValue("Alarms", "alarmhighgust", 0.0);
			HighGustAlarm.Enabled = ini.GetValue("Alarms", "HighGustAlarmSet", false);
			HighGustAlarm.Sound = ini.GetValue("Alarms", "HighGustAlarmSound", false);
			HighGustAlarm.SoundFile = ini.GetValue("Alarms", "HighGustAlarmSoundFile", DefaultSoundFile);
			if (HighGustAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				HighGustAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			HighGustAlarm.Notify = ini.GetValue("Alarms", "HighGustAlarmNotify", false);
			HighGustAlarm.Email = ini.GetValue("Alarms", "HighGustAlarmEmail", false);
			HighGustAlarm.Latch = ini.GetValue("Alarms", "HighGustAlarmLatch", false);
			HighGustAlarm.LatchHours = ini.GetValue("Alarms", "HighGustAlarmLatchHours", 24);
			HighGustAlarm.Action = ini.GetValue("Alarms", "HighGustAlarmAction", "");
			HighGustAlarm.ActionParams = ini.GetValue("Alarms", "HighGustAlarmActionParams", "");

			HighWindAlarm.Value = ini.GetValue("Alarms", "alarmhighwind", 0.0);
			HighWindAlarm.Enabled = ini.GetValue("Alarms", "HighWindAlarmSet", false);
			HighWindAlarm.Sound = ini.GetValue("Alarms", "HighWindAlarmSound", false);
			HighWindAlarm.SoundFile = ini.GetValue("Alarms", "HighWindAlarmSoundFile", DefaultSoundFile);
			if (HighWindAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				HighWindAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			HighWindAlarm.Notify = ini.GetValue("Alarms", "HighWindAlarmNotify", false);
			HighWindAlarm.Email = ini.GetValue("Alarms", "HighWindAlarmEmail", false);
			HighWindAlarm.Latch = ini.GetValue("Alarms", "HighWindAlarmLatch", false);
			HighWindAlarm.LatchHours = ini.GetValue("Alarms", "HighWindAlarmLatchHours", 24);
			HighWindAlarm.Action = ini.GetValue("Alarms", "HighWindAlarmAction", "");
			HighWindAlarm.ActionParams = ini.GetValue("Alarms", "HighWindAlarmActionParams", "");

			SensorAlarm.Enabled = ini.GetValue("Alarms", "SensorAlarmSet", true);
			SensorAlarm.Sound = ini.GetValue("Alarms", "SensorAlarmSound", false);
			SensorAlarm.SoundFile = ini.GetValue("Alarms", "SensorAlarmSoundFile", DefaultSoundFile);
			if (SensorAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				SensorAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			SensorAlarm.Notify = ini.GetValue("Alarms", "SensorAlarmNotify", true);
			SensorAlarm.Email = ini.GetValue("Alarms", "SensorAlarmEmail", false);
			SensorAlarm.Latch = ini.GetValue("Alarms", "SensorAlarmLatch", true);
			SensorAlarm.LatchHours = ini.GetValue("Alarms", "SensorAlarmLatchHours", 1);
			SensorAlarm.TriggerThreshold = ini.GetValue("Alarms", "SensorAlarmTriggerCount", 2);
			SensorAlarm.Action = ini.GetValue("Alarms", "SensorAlarmAction", "");
			SensorAlarm.ActionParams = ini.GetValue("Alarms", "SensorAlarmActionParams", "");

			DataStoppedAlarm.Enabled = ini.GetValue("Alarms", "DataStoppedAlarmSet", true);
			DataStoppedAlarm.Sound = ini.GetValue("Alarms", "DataStoppedAlarmSound", false);
			DataStoppedAlarm.SoundFile = ini.GetValue("Alarms", "DataStoppedAlarmSoundFile", DefaultSoundFile);
			if (DataStoppedAlarm.SoundFile.Contains(DefaultSoundFileOld))
			{
				SensorAlarm.SoundFile = DefaultSoundFile;
				rewriteRequired = true;
			}
			DataStoppedAlarm.Notify = ini.GetValue("Alarms", "DataStoppedAlarmNotify", true);
			DataStoppedAlarm.Email = ini.GetValue("Alarms", "DataStoppedAlarmEmail", false);
			DataStoppedAlarm.Latch = ini.GetValue("Alarms", "DataStoppedAlarmLatch", true);
			DataStoppedAlarm.LatchHours = ini.GetValue("Alarms", "DataStoppedAlarmLatchHours", 1);
			DataStoppedAlarm.TriggerThreshold = ini.GetValue("Alarms", "DataStoppedAlarmTriggerCount", 2);
			DataStoppedAlarm.Action = ini.GetValue("Alarms", "DataStoppedAlarmAction", "");
			DataStoppedAlarm.ActionParams = ini.GetValue("Alarms", "DataStoppedAlarmActionParams", "");

			// Alarms below here were created after the change in default sound file, so no check required
			BatteryLowAlarm.Enabled = ini.GetValue("Alarms", "BatteryLowAlarmSet", false);
			BatteryLowAlarm.Sound = ini.GetValue("Alarms", "BatteryLowAlarmSound", false);
			BatteryLowAlarm.SoundFile = ini.GetValue("Alarms", "BatteryLowAlarmSoundFile", DefaultSoundFile);
			BatteryLowAlarm.Notify = ini.GetValue("Alarms", "BatteryLowAlarmNotify", false);
			BatteryLowAlarm.Email = ini.GetValue("Alarms", "BatteryLowAlarmEmail", false);
			BatteryLowAlarm.Latch = ini.GetValue("Alarms", "BatteryLowAlarmLatch", false);
			BatteryLowAlarm.LatchHours = ini.GetValue("Alarms", "BatteryLowAlarmLatchHours", 24);
			BatteryLowAlarm.TriggerThreshold = ini.GetValue("Alarms", "BatteryLowAlarmTriggerCount", 1);
			BatteryLowAlarm.Action = ini.GetValue("Alarms", "BatteryLowAlarmAction", "");
			BatteryLowAlarm.ActionParams = ini.GetValue("Alarms", "BatteryLowAlarmActionParams", "");

			SpikeAlarm.Enabled = ini.GetValue("Alarms", "DataSpikeAlarmSet", false);
			SpikeAlarm.Sound = ini.GetValue("Alarms", "DataSpikeAlarmSound", false);
			SpikeAlarm.SoundFile = ini.GetValue("Alarms", "DataSpikeAlarmSoundFile", DefaultSoundFile);
			SpikeAlarm.Notify = ini.GetValue("Alarms", "DataSpikeAlarmNotify", true);
			SpikeAlarm.Email = ini.GetValue("Alarms", "DataSpikeAlarmEmail", true);
			SpikeAlarm.Latch = ini.GetValue("Alarms", "DataSpikeAlarmLatch", true);
			SpikeAlarm.LatchHours = ini.GetValue("Alarms", "DataSpikeAlarmLatchHours", 24);
			SpikeAlarm.TriggerThreshold = ini.GetValue("Alarms", "DataSpikeAlarmTriggerCount", 1);
			SpikeAlarm.Action = ini.GetValue("Alarms", "DataSpikeAlarmAction", "");
			SpikeAlarm.ActionParams = ini.GetValue("Alarms", "DataSpikeAlarmActionParams", "");

			UpgradeAlarm.Enabled = ini.GetValue("Alarms", "UpgradeAlarmSet", true);
			UpgradeAlarm.Sound = ini.GetValue("Alarms", "UpgradeAlarmSound", false);
			UpgradeAlarm.SoundFile = ini.GetValue("Alarms", "UpgradeAlarmSoundFile", DefaultSoundFile);
			UpgradeAlarm.Notify = ini.GetValue("Alarms", "UpgradeAlarmNotify", true);
			UpgradeAlarm.Email = ini.GetValue("Alarms", "UpgradeAlarmEmail", false);
			UpgradeAlarm.Latch = ini.GetValue("Alarms", "UpgradeAlarmLatch", false);
			UpgradeAlarm.LatchHours = ini.GetValue("Alarms", "UpgradeAlarmLatchHours", 24);
			UpgradeAlarm.Action = ini.GetValue("Alarms", "UpgradeAlarmAction", "");
			UpgradeAlarm.ActionParams = ini.GetValue("Alarms", "UpgradeAlarmActionParams", "");

			HttpUploadAlarm.Enabled = ini.GetValue("Alarms", "HttpUploadAlarmSet", false);
			HttpUploadAlarm.Sound = ini.GetValue("Alarms", "HttpUploadAlarmSound", false);
			HttpUploadAlarm.SoundFile = ini.GetValue("Alarms", "HttpUploadAlarmSoundFile", DefaultSoundFile);
			HttpUploadAlarm.Notify = ini.GetValue("Alarms", "HttpUploadAlarmNotify", false);
			HttpUploadAlarm.Email = ini.GetValue("Alarms", "HttpUploadAlarmEmail", false);
			HttpUploadAlarm.Latch = ini.GetValue("Alarms", "HttpUploadAlarmLatch", false);
			HttpUploadAlarm.LatchHours = ini.GetValue("Alarms", "HttpUploadAlarmLatchHours", 24);
			HttpUploadAlarm.TriggerThreshold = ini.GetValue("Alarms", "HttpUploadAlarmTriggerCount", 1);
			HttpUploadAlarm.Action = ini.GetValue("Alarms", "HttpUploadAlarmAction", "");
			HttpUploadAlarm.ActionParams = ini.GetValue("Alarms", "HttpUploadAlarmActionParams", "");

			MySqlUploadAlarm.Enabled = ini.GetValue("Alarms", "MySqlUploadAlarmSet", false);
			MySqlUploadAlarm.Sound = ini.GetValue("Alarms", "MySqlUploadAlarmSound", false);
			MySqlUploadAlarm.SoundFile = ini.GetValue("Alarms", "MySqlUploadAlarmSoundFile", DefaultSoundFile);
			MySqlUploadAlarm.Notify = ini.GetValue("Alarms", "MySqlUploadAlarmNotify", false);
			MySqlUploadAlarm.Email = ini.GetValue("Alarms", "MySqlUploadAlarmEmail", false);
			MySqlUploadAlarm.Latch = ini.GetValue("Alarms", "MySqlUploadAlarmLatch", false);
			MySqlUploadAlarm.LatchHours = ini.GetValue("Alarms", "MySqlUploadAlarmLatchHours", 24);
			MySqlUploadAlarm.TriggerThreshold = ini.GetValue("Alarms", "MySqlUploadAlarmTriggerCount", 1);
			MySqlUploadAlarm.Action = ini.GetValue("Alarms", "MySqlUploadAlarmAction", "");
			MySqlUploadAlarm.ActionParams = ini.GetValue("Alarms", "MySqlUploadAlarmActionParams", "");

			AlarmFromEmail = ini.GetValue("Alarms", "FromEmail", "");
			AlarmDestEmail = ini.GetValue("Alarms", "DestEmail", "").Split(';');
			AlarmEmailHtml = ini.GetValue("Alarms", "UseHTML", false);

			Calib.Press.Offset = ini.GetValue("Offsets", "PressOffset", 0.0);
			Calib.Temp.Offset = ini.GetValue("Offsets", "TempOffset", 0.0);
			Calib.Hum.Offset = ini.GetValue("Offsets", "HumOffset", 0);
			Calib.WindDir.Offset = ini.GetValue("Offsets", "WindDirOffset", 0);
			Calib.Solar.Offset = ini.GetValue("Offsets", "SolarOffset", 0.0);
			Calib.UV.Offset = ini.GetValue("Offsets", "UVOffset", 0.0);
			Calib.WetBulb.Offset = ini.GetValue("Offsets", "WetBulbOffset", 0.0);
			Calib.InTemp.Offset = ini.GetValue("Offsets", "InTempOffset", 0.0);
			Calib.InHum.Offset = ini.GetValue("Offsets", "InHumOffset", 0);

			Calib.Press.Mult = ini.GetValue("Offsets", "PressMult", 1.0);
			Calib.WindSpeed.Mult = ini.GetValue("Offsets", "WindSpeedMult", 1.0);
			Calib.WindGust.Mult = ini.GetValue("Offsets", "WindGustMult", 1.0);
			Calib.Temp.Mult = ini.GetValue("Offsets", "TempMult", 1.0);
			Calib.Temp.Mult2 = ini.GetValue("Offsets", "TempMult2", 0.0);
			Calib.Hum.Mult = ini.GetValue("Offsets", "HumMult", 1.0);
			Calib.Hum.Mult2 = ini.GetValue("Offsets", "HumMult2", 0.0);
			Calib.Rain.Mult = ini.GetValue("Offsets", "RainMult", 1.0);
			Calib.Solar.Mult = ini.GetValue("Offsets", "SolarMult", 1.0);
			Calib.UV.Mult = ini.GetValue("Offsets", "UVMult", 1.0);
			Calib.WetBulb.Mult = ini.GetValue("Offsets", "WetBulbMult", 1.0);
			Calib.InTemp.Mult = ini.GetValue("Offsets", "InTempMult", 1.0);
			Calib.InHum.Mult = ini.GetValue("Offsets", "InHumMult", 1.0);

			Limit.TempHigh = ini.GetValue("Limits", "TempHighC", 60.0);
			Limit.TempLow = ini.GetValue("Limits", "TempLowC", -60.0);
			Limit.DewHigh = ini.GetValue("Limits", "DewHighC", 40.0);
			Limit.PressHigh = ini.GetValue("Limits", "PressHighMB", 1090.0);
			Limit.PressLow = ini.GetValue("Limits", "PressLowMB", 870.0);
			Limit.WindHigh = ini.GetValue("Limits", "WindHighMS", 90.0);

			xapEnabled = ini.GetValue("xAP", "Enabled", false);
			xapUID = ini.GetValue("xAP", "UID", "4375");
			xapPort = ini.GetValue("xAP", "Port", 3639);

			SolarOptions.SunThreshold = ini.GetValue("Solar", "SunThreshold", 75);
			SolarOptions.SolarMinimum = ini.GetValue("Solar", "SolarMinimum", 30);
			SolarOptions.LuxToWM2 = ini.GetValue("Solar", "LuxToWM2", 0.0079);
			SolarOptions.UseBlakeLarsen = ini.GetValue("Solar", "UseBlakeLarsen", false);
			SolarOptions.SolarCalc = ini.GetValue("Solar", "SolarCalc", 0);

			// Migrate old single solar factors to the new dual scheme
			if (ini.ValueExists("Solar", "RStransfactor"))
			{
				SolarOptions.RStransfactorJun = ini.GetValue("Solar", "RStransfactor", 0.8);
				SolarOptions.RStransfactorDec = SolarOptions.RStransfactorJun;
				rewriteRequired = true;
			}
			else
			{
				SolarOptions.RStransfactorJun = ini.GetValue("Solar", "RStransfactorJun", 0.8);
				SolarOptions.RStransfactorDec = ini.GetValue("Solar", "RStransfactorDec", 0.8);
			}
			if (ini.ValueExists("Solar", "BrasTurbidity"))
			{
				SolarOptions.BrasTurbidityJun = ini.GetValue("Solar", "BrasTurbidity", 2.0);
				SolarOptions.BrasTurbidityDec = SolarOptions.BrasTurbidityJun;
				rewriteRequired = true;
			}
			else
			{
				SolarOptions.BrasTurbidityJun = ini.GetValue("Solar", "BrasTurbidityJun", 2.0);
				SolarOptions.BrasTurbidityDec = ini.GetValue("Solar", "BrasTurbidityDec", 2.0);
			}

			NOAAconf.Name = ini.GetValue("NOAA", "Name", " ");
			NOAAconf.City = ini.GetValue("NOAA", "City", " ");
			NOAAconf.State = ini.GetValue("NOAA", "State", " ");
			NOAAconf.Use12hour = ini.GetValue("NOAA", "12hourformat", false);
			NOAAconf.HeatThreshold = ini.GetValue("NOAA", "HeatingThreshold", -1000.0);
			if (NOAAconf.HeatThreshold < -99 || NOAAconf.HeatThreshold > 150)
			{
				NOAAconf.HeatThreshold = Units.Temp == 0 ? 18.3 : 65;
				rewriteRequired = true;
			}
			NOAAconf.CoolThreshold = ini.GetValue("NOAA", "CoolingThreshold", -1000.0);
			if (NOAAconf.CoolThreshold < -99 || NOAAconf.CoolThreshold > 150)
			{
				NOAAconf.CoolThreshold = Units.Temp == 0 ? 18.3 : 65;
				rewriteRequired = true;
			}
			NOAAconf.MaxTempComp1 = ini.GetValue("NOAA", "MaxTempComp1", -1000.0);
			if (NOAAconf.MaxTempComp1 < -99 || NOAAconf.MaxTempComp1 > 150)
			{
				NOAAconf.MaxTempComp1 = Units.Temp == 0 ? 27 : 80;
				rewriteRequired = true;
			}
			NOAAconf.MaxTempComp2 = ini.GetValue("NOAA", "MaxTempComp2", -1000.0);
			if (NOAAconf.MaxTempComp2 < -99 || NOAAconf.MaxTempComp2 > 99)
			{
				NOAAconf.MaxTempComp2 = Units.Temp == 0 ? 0 : 32;
				rewriteRequired = true;
			}
			NOAAconf.MinTempComp1 = ini.GetValue("NOAA", "MinTempComp1", -1000.0);
			if (NOAAconf.MinTempComp1 < -99 || NOAAconf.MinTempComp1 > 99)
			{
				NOAAconf.MinTempComp1 = Units.Temp == 0 ? 0 : 32;
				rewriteRequired = true;
			}
			NOAAconf.MinTempComp2 = ini.GetValue("NOAA", "MinTempComp2", -1000.0);
			if (NOAAconf.MinTempComp2 < -99 || NOAAconf.MinTempComp2 > 99)
			{
				NOAAconf.MinTempComp2 = Units.Temp == 0 ? -18 : 0;
				rewriteRequired = true;
			}
			NOAAconf.RainComp1 = ini.GetValue("NOAA", "RainComp1", -1000.0);
			if (NOAAconf.RainComp1 < 0 || NOAAconf.RainComp1 > 99)
			{
				NOAAconf.RainComp1 = Units.Rain == 0 ? 0.2 : 0.01;
				rewriteRequired = true;
			}
			NOAAconf.RainComp2 = ini.GetValue("NOAA", "RainComp2", -1000.0);
			if (NOAAconf.RainComp2 < 0 || NOAAconf.RainComp2 > 99)
			{
				NOAAconf.RainComp2 = Units.Rain == 0 ? 2 : 0.1;
				rewriteRequired = true;
			}
			NOAAconf.RainComp3 = ini.GetValue("NOAA", "RainComp3", -1000.0);
			if (NOAAconf.RainComp3 < 0 || NOAAconf.RainComp3 > 99)
			{
				NOAAconf.RainComp3 = Units.Rain == 0 ? 20 : 1;
				rewriteRequired = true;
			}

			NOAAconf.Create = ini.GetValue("NOAA", "AutoSave", false);
			NOAAconf.AutoFtp = ini.GetValue("NOAA", "AutoFTP", false);
			NOAAconf.FtpFolder = ini.GetValue("NOAA", "FTPDirectory", "");
			NOAAconf.AutoCopy = ini.GetValue("NOAA", "AutoCopy", false);
			NOAAconf.CopyFolder = ini.GetValue("NOAA", "CopyDirectory", "");
			NOAAconf.MonthFile = ini.GetValue("NOAA", "MonthFileFormat", "'NOAAMO'MMyy'.txt'");
			// Check for Cumulus 1 default format - and update
			if (NOAAconf.MonthFile == "'NOAAMO'mmyy'.txt'" || NOAAconf.MonthFile == "\"NOAAMO\"mmyy\".txt\"")
			{
				NOAAconf.MonthFile = "'NOAAMO'MMyy'.txt'";
				rewriteRequired = true;
			}
			NOAAconf.YearFile = ini.GetValue("NOAA", "YearFileFormat", "'NOAAYR'yyyy'.txt'");
			NOAAconf.UseUtf8 = ini.GetValue("NOAA", "NOAAUseUTF8", true);
			NOAAconf.UseDotDecimal = ini.GetValue("NOAA", "UseDotDecimal", false);
			NOAAconf.UseMinMaxAvg = ini.GetValue("NOAA", "UseMinMaxAvg", false);

			NOAAconf.TempNorms[1] = ini.GetValue("NOAA", "NOAATempNormJan", -1000.0);
			NOAAconf.TempNorms[2] = ini.GetValue("NOAA", "NOAATempNormFeb", -1000.0);
			NOAAconf.TempNorms[3] = ini.GetValue("NOAA", "NOAATempNormMar", -1000.0);
			NOAAconf.TempNorms[4] = ini.GetValue("NOAA", "NOAATempNormApr", -1000.0);
			NOAAconf.TempNorms[5] = ini.GetValue("NOAA", "NOAATempNormMay", -1000.0);
			NOAAconf.TempNorms[6] = ini.GetValue("NOAA", "NOAATempNormJun", -1000.0);
			NOAAconf.TempNorms[7] = ini.GetValue("NOAA", "NOAATempNormJul", -1000.0);
			NOAAconf.TempNorms[8] = ini.GetValue("NOAA", "NOAATempNormAug", -1000.0);
			NOAAconf.TempNorms[9] = ini.GetValue("NOAA", "NOAATempNormSep", -1000.0);
			NOAAconf.TempNorms[10] = ini.GetValue("NOAA", "NOAATempNormOct", -1000.0);
			NOAAconf.TempNorms[11] = ini.GetValue("NOAA", "NOAATempNormNov", -1000.0);
			NOAAconf.TempNorms[12] = ini.GetValue("NOAA", "NOAATempNormDec", -1000.0);

			NOAAconf.RainNorms[1] = ini.GetValue("NOAA", "NOAARainNormJan", -1000.0);
			NOAAconf.RainNorms[2] = ini.GetValue("NOAA", "NOAARainNormFeb", -1000.0);
			NOAAconf.RainNorms[3] = ini.GetValue("NOAA", "NOAARainNormMar", -1000.0);
			NOAAconf.RainNorms[4] = ini.GetValue("NOAA", "NOAARainNormApr", -1000.0);
			NOAAconf.RainNorms[5] = ini.GetValue("NOAA", "NOAARainNormMay", -1000.0);
			NOAAconf.RainNorms[6] = ini.GetValue("NOAA", "NOAARainNormJun", -1000.0);
			NOAAconf.RainNorms[7] = ini.GetValue("NOAA", "NOAARainNormJul", -1000.0);
			NOAAconf.RainNorms[8] = ini.GetValue("NOAA", "NOAARainNormAug", -1000.0);
			NOAAconf.RainNorms[9] = ini.GetValue("NOAA", "NOAARainNormSep", -1000.0);
			NOAAconf.RainNorms[10] = ini.GetValue("NOAA", "NOAARainNormOct", -1000.0);
			NOAAconf.RainNorms[11] = ini.GetValue("NOAA", "NOAARainNormNov", -1000.0);
			NOAAconf.RainNorms[12] = ini.GetValue("NOAA", "NOAARainNormDec", -1000.0);

			HTTPProxyName = ini.GetValue("Proxies", "HTTPProxyName", "");
			HTTPProxyPort = ini.GetValue("Proxies", "HTTPProxyPort", 0);
			HTTPProxyUser = ini.GetValue("Proxies", "HTTPProxyUser", "");
			HTTPProxyPassword = ini.GetValue("Proxies", "HTTPProxyPassword", "");

			NumWindRosePoints = ini.GetValue("Display", "NumWindRosePoints", 16);
			WindRoseAngle = 360.0 / NumWindRosePoints;
			DisplayOptions.UseApparent = ini.GetValue("Display", "UseApparent", false);
			DisplayOptions.ShowSolar = ini.GetValue("Display", "DisplaySolarData", false);
			DisplayOptions.ShowUV = ini.GetValue("Display", "DisplayUvData", false);

			// MySQL - common
			MySqlStuff.ConnSettings.Server = ini.GetValue("MySQL", "Host", "127.0.0.1");
			MySqlStuff.ConnSettings.Port = (uint)ini.GetValue("MySQL", "Port", 3306);
			MySqlStuff.ConnSettings.UserID = ini.GetValue("MySQL", "User", "");
			MySqlStuff.ConnSettings.Password = ini.GetValue("MySQL", "Pass", "");
			MySqlStuff.ConnSettings.Database = ini.GetValue("MySQL", "Database", "database");
			MySqlStuff.Settings.UpdateOnEdit = ini.GetValue("MySQL", "UpdateOnEdit", true);
			MySqlStuff.Settings.BufferOnfailure = ini.GetValue("MySQL", "BufferOnFailure", false);

			if (string.IsNullOrEmpty(MySqlStuff.ConnSettings.Server) || string.IsNullOrEmpty(MySqlStuff.ConnSettings.UserID) || string.IsNullOrEmpty(MySqlStuff.ConnSettings.Password))
				MySqlStuff.Settings.UpdateOnEdit = false;

			// MySQL - monthly log file
			MySqlStuff.Settings.Monthly.Enabled = ini.GetValue("MySQL", "MonthlyMySqlEnabled", false);
			MySqlStuff.Settings.Monthly.TableName = ini.GetValue("MySQL", "MonthlyTable", "Monthly");
			// MySQL - real-time
			MySqlStuff.Settings.Realtime.Enabled = ini.GetValue("MySQL", "RealtimeMySqlEnabled", false);
			MySqlStuff.Settings.Realtime.TableName = ini.GetValue("MySQL", "RealtimeTable", "Realtime");
			MySqlStuff.Settings.RealtimeRetention = ini.GetValue("MySQL", "RealtimeRetention", "");
			MySqlStuff.Settings.RealtimeLimit1Minute = ini.GetValue("MySQL", "RealtimeMySql1MinLimit", false) && RealtimeInterval < 60000; // do not enable if real time interval is greater than 1 minute
																																				// MySQL - dayfile
			MySqlStuff.Settings.Dayfile.Enabled = ini.GetValue("MySQL", "DayfileMySqlEnabled", false);
			MySqlStuff.Settings.Dayfile.TableName = ini.GetValue("MySQL", "DayfileTable", "Dayfile");
			// MySQL - custom seconds
			MySqlStuff.Settings.CustomSecs.Commands[0] = ini.GetValue("MySQL", "CustomMySqlSecondsCommandString", "");
			for (var i = 1; i < 10; i++)
			{
				if (ini.ValueExists("MySQL", "CustomMySqlSecondsCommandString" + i))
					MySqlStuff.Settings.CustomSecs.Commands[i] = ini.GetValue("MySQL", "CustomMySqlSecondsCommandString" + i, "");
			}
			MySqlStuff.Settings.CustomSecs.Enabled = ini.GetValue("MySQL", "CustomMySqlSecondsEnabled", false);
			MySqlStuff.Settings.CustomSecs.Interval = ini.GetValue("MySQL", "CustomMySqlSecondsInterval", 10);
			if (MySqlStuff.Settings.CustomSecs.Interval < 1) { MySqlStuff.Settings.CustomSecs.Interval = 1; }
			// MySQL - custom minutes
			MySqlStuff.Settings.CustomMins.Commands[0] = ini.GetValue("MySQL", "CustomMySqlMinutesCommandString", "");
			for (var i = 1; i < 10; i++)
			{
				if (ini.ValueExists("MySQL", "CustomMySqlMinutesCommandString" + i))
					MySqlStuff.Settings.CustomMins.Commands[i] = ini.GetValue("MySQL", "CustomMySqlMinutesCommandString" + i, "");
			}
			MySqlStuff.Settings.CustomMins.Enabled = ini.GetValue("MySQL", "CustomMySqlMinutesEnabled", false);
			MySqlStuff.CustomMinutesIntervalIndex = ini.GetValue("MySQL", "CustomMySqlMinutesIntervalIndex", -1);
			if (MySqlStuff.CustomMinutesIntervalIndex >= 0 && MySqlStuff.CustomMinutesIntervalIndex < FactorsOf60.Length)
			{
				MySqlStuff.Settings.CustomMins.Interval = FactorsOf60[MySqlStuff.CustomMinutesIntervalIndex];
			}
			else
			{
				MySqlStuff.Settings.CustomMins.Interval = 10;
				MySqlStuff.CustomMinutesIntervalIndex = 6;
				rewriteRequired = true;
			}
			// MySQL - custom roll-over
			MySqlStuff.Settings.CustomRollover.Commands[0] = ini.GetValue("MySQL", "CustomMySqlRolloverCommandString", "");
			for (var i = 1; i < 10; i++)
			{
				if (ini.ValueExists("MySQL", "CustomMySqlRolloverCommandString" + i))
					MySqlStuff.Settings.CustomRollover.Commands[i] = ini.GetValue("MySQL", "CustomMySqlRolloverCommandString" + i, "");
			}
			MySqlStuff.Settings.CustomRollover.Enabled = ini.GetValue("MySQL", "CustomMySqlRolloverEnabled", false);

			// Custom HTTP - seconds
			CustomHttpSecondsStrings[0] = ini.GetValue("HTTP", "CustomHttpSecondsString", "");
			for (var i = 1; i < 10; i++)
			{
				if (ini.ValueExists("HTTP", "CustomHttpSecondsString" + i))
					CustomHttpSecondsStrings[i] = ini.GetValue("HTTP", "CustomHttpSecondsString" + i, "");
			}
			CustomHttpSecondsEnabled = ini.GetValue("HTTP", "CustomHttpSecondsEnabled", false);
			CustomHttpSecondsInterval = ini.GetValue("HTTP", "CustomHttpSecondsInterval", 10);
			if (CustomHttpSecondsInterval < 1) { CustomHttpSecondsInterval = 1; }
			// Custom HTTP - minutes
			CustomHttpMinutesStrings[0] = ini.GetValue("HTTP", "CustomHttpMinutesString", "");
			for (var i = 1; i < 10; i++)
			{
				if (ini.ValueExists("HTTP", "CustomHttpMinutesString" + i))
					CustomHttpMinutesStrings[i] = ini.GetValue("HTTP", "CustomHttpMinutesString" + i, "");
			}
			CustomHttpMinutesEnabled = ini.GetValue("HTTP", "CustomHttpMinutesEnabled", false);
			CustomHttpMinutesIntervalIndex = ini.GetValue("HTTP", "CustomHttpMinutesIntervalIndex", -1);
			if (CustomHttpMinutesIntervalIndex >= 0 && CustomHttpMinutesIntervalIndex < FactorsOf60.Length)
			{
				CustomHttpMinutesInterval = FactorsOf60[CustomHttpMinutesIntervalIndex];
			}
			else
			{
				CustomHttpMinutesInterval = 10;
				CustomHttpMinutesIntervalIndex = 6;
				rewriteRequired = true;
			}
			// Http - custom roll-over
			CustomHttpRolloverStrings[0] = ini.GetValue("HTTP", "CustomHttpRolloverString", "");
			for (var i = 1; i < 10; i++)
			{
				if (ini.ValueExists("HTTP", "CustomHttpRolloverString" + i))
					CustomHttpRolloverStrings[i] = ini.GetValue("HTTP", "CustomHttpRolloverString" + i, "");
			}
			CustomHttpRolloverEnabled = ini.GetValue("HTTP", "CustomHttpRolloverEnabled", false);

			// Select-a-Chart settings
			for (int i = 0; i < SelectaChartOptions.series.Length; i++)
			{
				SelectaChartOptions.series[i] = ini.GetValue("Select-a-Chart", "Series" + i, "0");
				SelectaChartOptions.colours[i] = ini.GetValue("Select-a-Chart", "Colour" + i, "");
			}

			// Email settings
			SmtpOptions.Enabled = ini.GetValue("SMTP", "Enabled", false);
			SmtpOptions.Server = ini.GetValue("SMTP", "ServerName", "");
			SmtpOptions.Port = ini.GetValue("SMTP", "Port", 587);
			SmtpOptions.SslOption = ini.GetValue("SMTP", "SSLOption", 1);
			SmtpOptions.RequiresAuthentication = ini.GetValue("SMTP", "RequiresAuthentication", false);
			SmtpOptions.User = ini.GetValue("SMTP", "User", "");
			SmtpOptions.Password = ini.GetValue("SMTP", "Password", "");
			SmtpOptions.IgnoreCertErrors = ini.GetValue("SMTP", "IgnoreCertErrors", false);

			// Growing Degree Days
			GrowingBase1 = ini.GetValue("GrowingDD", "BaseTemperature1", (Units.Temp == 0 ? 5.0 : 40.0));
			GrowingBase2 = ini.GetValue("GrowingDD", "BaseTemperature2", (Units.Temp == 0 ? 10.0 : 50.0));
			GrowingYearStarts = ini.GetValue("GrowingDD", "YearStarts", (Latitude >= 0 ? 1 : 7));
			GrowingCap30C = ini.GetValue("GrowingDD", "Cap30C", true);

			// Temperature Sum
			TempSumYearStarts = ini.GetValue("TempSum", "TempSumYearStart", (Latitude >= 0 ? 1 : 7));
			if (TempSumYearStarts < 1 || TempSumYearStarts > 12)
			{
				TempSumYearStarts = 1;
				rewriteRequired = true;
			}
			TempSumBase1 = ini.GetValue("TempSum", "BaseTemperature1", GrowingBase1);
			TempSumBase2 = ini.GetValue("TempSum", "BaseTemperature2", GrowingBase2);

			// Additional sensor logging - default to the orginal LogExtraSensors setting
			ExtraDataLogging.Temperature = ini.GetValue("ExtraDataLogging", "Temperature", StationOptions.LogExtraSensors);
			ExtraDataLogging.Humidity = ini.GetValue("ExtraDataLogging", "Humidity", StationOptions.LogExtraSensors);
			ExtraDataLogging.Dewpoint = ini.GetValue("ExtraDataLogging", "Dewpoint", StationOptions.LogExtraSensors);
			ExtraDataLogging.UserTemp = ini.GetValue("ExtraDataLogging", "UserTemp", StationOptions.LogExtraSensors);
			ExtraDataLogging.SoilTemp = ini.GetValue("ExtraDataLogging", "SoilTemp", StationOptions.LogExtraSensors);
			ExtraDataLogging.SoilMoisture = ini.GetValue("ExtraDataLogging", "SoilMoisture", StationOptions.LogExtraSensors);
			ExtraDataLogging.LeafTemp = ini.GetValue("ExtraDataLogging", "LeafTemp", StationOptions.LogExtraSensors);
			ExtraDataLogging.LeafWetness = ini.GetValue("ExtraDataLogging", "LeafWetness", StationOptions.LogExtraSensors);
			ExtraDataLogging.AirQual = ini.GetValue("ExtraDataLogging", "AirQual", StationOptions.LogExtraSensors);
			ExtraDataLogging.CO2 = ini.GetValue("ExtraDataLogging", "CO2", StationOptions.LogExtraSensors);

			// Custom Log Settings
			for (var i = 0; i < 10; i++)
			{
				if (ini.ValueExists("CustomLogs", "DailyFilename" + i))
					CustomDailyLogSettings[i].FileName = ini.GetValue("CustomLogs", "DailyFilename" + i, "");

				if (ini.ValueExists("CustomLogs", "DailyContent" + i))
					CustomDailyLogSettings[i].ContentString = ini.GetValue("CustomLogs", "DailyContent" + i, "").Replace("\n", "").Replace("\r", "");

				if (string.IsNullOrEmpty(CustomDailyLogSettings[i].FileName) || string.IsNullOrEmpty(CustomDailyLogSettings[i].ContentString))
					CustomDailyLogSettings[i].Enabled = false;
				else
					CustomDailyLogSettings[i].Enabled = ini.GetValue("CustomLogs", "DailyEnabled" + i, false);



				if (ini.ValueExists("CustomLogs", "IntervalFilename" + i))
					CustomIntvlLogSettings[i].FileName = ini.GetValue("CustomLogs", "IntervalFilename" + i, "");

				if (ini.ValueExists("CustomLogs", "IntervalContent" + i))
					CustomIntvlLogSettings[i].ContentString = ini.GetValue("CustomLogs", "IntervalContent" + i, "").Replace("\n", "").Replace("\r", "");

				if (string.IsNullOrEmpty(CustomIntvlLogSettings[i].FileName) || string.IsNullOrEmpty(CustomIntvlLogSettings[i].ContentString))
					CustomIntvlLogSettings[i].Enabled = false;
				else
					CustomIntvlLogSettings[i].Enabled = ini.GetValue("CustomLogs", "IntervalEnabled" + i, false);

				if (ini.ValueExists("CustomLogs", "IntervalIdx" + i))
				{
					CustomIntvlLogSettings[i].IntervalIdx = ini.GetValue("CustomLogs", "IntervalIdx" + i, DataLogInterval);

					if (CustomIntvlLogSettings[i].IntervalIdx >= 0 && CustomIntvlLogSettings[i].IntervalIdx < FactorsOf60.Length)
					{
						CustomIntvlLogSettings[i].Interval = FactorsOf60[CustomIntvlLogSettings[i].IntervalIdx];
					}
					else
					{
						CustomIntvlLogSettings[i].Interval = FactorsOf60[DataLogInterval];
						CustomIntvlLogSettings[i].IntervalIdx = DataLogInterval;
						rewriteRequired = true;
					}
				}
				else
				{
					CustomIntvlLogSettings[i].Interval = FactorsOf60[DataLogInterval];
					CustomIntvlLogSettings[i].IntervalIdx = DataLogInterval;
				}
			}

			// do we need to decrypt creds?
			if (ProgramOptions.EncryptedCreds)
			{
				WllApiKey = Crypto.DecryptString(WllApiKey, Program.InstanceId, "WllApiKey");
				WllApiSecret = Crypto.DecryptString(WllApiSecret, Program.InstanceId, "WllApiSecret");
				AirLinkApiKey = Crypto.DecryptString(AirLinkApiKey, Program.InstanceId, "AirLinkApiKey");
				AirLinkApiSecret = Crypto.DecryptString(AirLinkApiSecret, Program.InstanceId, "AirLinkApiSecret");
				FtpOptions.Username = Crypto.DecryptString(FtpOptions.Username, Program.InstanceId, "FtpOptions.Username");
				FtpOptions.Password = Crypto.DecryptString(FtpOptions.Password, Program.InstanceId, "FtpOptions.Password");
				Wund.PW = Crypto.DecryptString(Wund.PW, Program.InstanceId, "Wund.PW");
				Windy.ApiKey = Crypto.DecryptString(Windy.ApiKey, Program.InstanceId, "Windy.ApiKey");
				AWEKAS.PW = Crypto.DecryptString(AWEKAS.PW, Program.InstanceId, "AWEKAS.PW");
				WindGuru.PW = Crypto.DecryptString(WindGuru.PW, Program.InstanceId, "WindGuru.PW");
				WCloud.PW = Crypto.DecryptString(WCloud.PW, Program.InstanceId, "WCloud.PW");
				PWS.PW = Crypto.DecryptString(PWS.PW, Program.InstanceId, "PWS.PW");
				WOW.PW = Crypto.DecryptString(WOW.PW, Program.InstanceId, "WOW.PW");
				APRS.PW = Crypto.DecryptString(APRS.PW, Program.InstanceId, "APRS.PW");
				OpenWeatherMap.PW = Crypto.DecryptString(OpenWeatherMap.PW, Program.InstanceId, "OpenWeatherMap.PW");
				MQTT.Username = Crypto.DecryptString(MQTT.Username, Program.InstanceId, "MQTT.Username");
				MQTT.Password = Crypto.DecryptString(MQTT.Password, Program.InstanceId, "MQTT.Password");
				MySqlStuff.ConnSettings.UserID = Crypto.DecryptString(MySqlStuff.ConnSettings.UserID, Program.InstanceId, "MySql UserID");
				MySqlStuff.ConnSettings.Password = Crypto.DecryptString(MySqlStuff.ConnSettings.Password, Program.InstanceId, "MySql Password");
				SmtpOptions.User = Crypto.DecryptString(SmtpOptions.User, Program.InstanceId, "SmtpOptions.User");
				SmtpOptions.Password = Crypto.DecryptString(SmtpOptions.Password, Program.InstanceId, "SmtpOptions.Password");
				HTTPProxyUser = Crypto.DecryptString(HTTPProxyUser, Program.InstanceId, "HTTPProxyUser");
				HTTPProxyPassword = Crypto.DecryptString(HTTPProxyPassword, Program.InstanceId, "HTTPProxyPassword");
				EcowittSettings.AppKey = Crypto.DecryptString(EcowittSettings.AppKey, Program.InstanceId, "EcowittSettings.AppKey");
				EcowittSettings.UserApiKey = Crypto.DecryptString(EcowittSettings.UserApiKey, Program.InstanceId, "EcowittSettings.UserApiKey");
			}
			else
			{
				rewriteRequired = true;
			}


			LogMessage("Reading Cumulus.ini file completed");

			if (rewriteRequired && File.Exists("Cumulus.ini"))
			{
				LogMessage("Some values in Cumulus.ini had invalid values, or new required entries have been created.");
				LogMessage("Recreating Cumulus.ini to reflect the new configuration.");
				LogDebugMessage("Deleting existing Cumulus.ini");
				File.Delete("Cumulus.ini");
				WriteIniFile();
			}
		}

		internal void WriteIniFile()
		{
			LogMessage("Writing Cumulus.ini file");

			IniFile ini = new IniFile("Cumulus.ini");

			ini.SetValue("Program", "EnableAccessibility", ProgramOptions.EnableAccessibility);

			ini.SetValue("Program", "StartupPingHost", ProgramOptions.StartupPingHost);
			ini.SetValue("Program", "StartupPingEscapeTime", ProgramOptions.StartupPingEscapeTime);

			ini.SetValue("Program", "StartupDelaySecs", ProgramOptions.StartupDelaySecs);
			ini.SetValue("Program", "StartupDelayMaxUptime", ProgramOptions.StartupDelayMaxUptime);

			ini.SetValue("Program", "StartupTask", ProgramOptions.StartupTask);
			ini.SetValue("Program", "StartupTaskParams", ProgramOptions.StartupTaskParams);
			ini.SetValue("Program", "StartupTaskWait", ProgramOptions.StartupTaskWait);

			ini.SetValue("Program", "ShutdownTask", ProgramOptions.ShutdownTask);
			ini.SetValue("Program", "ShutdownTaskParams", ProgramOptions.ShutdownTaskParams);

			ini.SetValue("Program", "DataStoppedExit", ProgramOptions.DataStoppedExit);
			ini.SetValue("Program", "DataStoppedMins", ProgramOptions.DataStoppedMins);

			ini.SetValue("Program", "UpdateDayfile", ProgramOptions.UpdateDayfile);
			ini.SetValue("Program", "UpdateLogfile", ProgramOptions.UpdateLogfile);
			ini.SetValue("Program", "DisplayPasswords", ProgramOptions.DisplayPasswords);

			ini.SetValue("Culture", "RemoveSpaceFromDateSeparator", ProgramOptions.Culture.RemoveSpaceFromDateSeparator);

			ini.SetValue("Station", "WarnMultiple", ProgramOptions.WarnMultiple);
			ini.SetValue("Station", "ListWebTags", ProgramOptions.ListWebTags);

			ini.SetValue("Program", "EncryptedCreds", true);

			ini.SetValue("Station", "Type", StationType);
			ini.SetValue("Station", "Model", StationModel);
			ini.SetValue("Station", "ComportName", ComportName);
			ini.SetValue("Station", "Latitude", Latitude);
			ini.SetValue("Station", "Longitude", Longitude);
			ini.SetValue("Station", "LatTxt", LatTxt);
			ini.SetValue("Station", "LonTxt", LonTxt);
			ini.SetValue("Station", "Altitude", Altitude);
			ini.SetValue("Station", "AltitudeInFeet", AltitudeInFeet);
			ini.SetValue("Station", "AnemometerHeightM", StationOptions.AnemometerHeightM);
			ini.SetValue("Station", "TimeZone", StationOptions.TimeZone);

			ini.SetValue("Station", "Humidity98Fix", StationOptions.Humidity98Fix);
			ini.SetValue("Station", "Wind10MinAverage", StationOptions.CalcWind10MinAve);
			ini.SetValue("Station", "UseSpeedForAvgCalc", StationOptions.UseSpeedForAvgCalc);
			ini.SetValue("Station", "AvgBearingMinutes", StationOptions.AvgBearingMinutes);
			ini.SetValue("Station", "AvgSpeedMinutes", StationOptions.AvgSpeedMinutes);
			ini.SetValue("Station", "PeakGustMinutes", StationOptions.PeakGustMinutes);
			ini.SetValue("Station", "LCMaxWind", LCMaxWind);
			ini.SetValue("Station", "RecordSetTimeoutHrs", RecordSetTimeoutHrs);
			ini.SetValue("Station", "SnowDepthHour", SnowDepthHour);
			ini.SetValue("Station", "UseRainForIsRaining", StationOptions.UseRainForIsRaining);
			ini.SetValue("Station", "LeafWetnessIsRainingIdx", StationOptions.LeafWetnessIsRainingIdx);
			ini.SetValue("Station", "LeafWetnessIsRainingVal", StationOptions.LeafWetnessIsRainingThrsh);

			ini.SetValue("Station", "Logging", ProgramOptions.DebugLogging);
			ini.SetValue("Station", "DataLogging", ProgramOptions.DataLogging);
			ini.SetValue("Station", "LogRawStationData", ProgramOptions.LogRawStationData);
			ini.SetValue("Station", "LogRawExtraData", ProgramOptions.LogRawExtraData);

			ini.SetValue("Station", "DavisReadReceptionStats", DavisOptions.ReadReceptionStats);
			ini.SetValue("Station", "DavisSetLoggerInterval", DavisOptions.SetLoggerInterval);
			ini.SetValue("Station", "UseDavisLoop2", DavisOptions.UseLoop2);
			ini.SetValue("Station", "DavisInitWaitTime", DavisOptions.InitWaitTime);
			ini.SetValue("Station", "DavisIPResponseTime", DavisOptions.IPResponseTime);
			ini.SetValue("Station", "DavisBaudRate", DavisOptions.BaudRate);
			ini.SetValue("Station", "VPrainGaugeType", DavisOptions.RainGaugeType);
			ini.SetValue("Station", "VP2ConnectionType", DavisOptions.ConnectionType);
			ini.SetValue("Station", "VP2TCPPort", DavisOptions.TCPPort);
			ini.SetValue("Station", "VP2IPAddr", DavisOptions.IPAddr);
			ini.SetValue("Station", "VP2PeriodicDisconnectInterval", DavisOptions.PeriodicDisconnectInterval);
			ini.SetValue("Station", "ForceVPBarUpdate", DavisOptions.ForceVPBarUpdate);

			ini.SetValue("Station", "NoSensorCheck", StationOptions.NoSensorCheck);
			ini.SetValue("Station", "CalculatedDP", StationOptions.CalculatedDP);
			ini.SetValue("Station", "CalculatedWC", StationOptions.CalculatedWC);
			ini.SetValue("Station", "CalculatedET", StationOptions.CalculatedET);

			ini.SetValue("Station", "RolloverHour", RolloverHour);
			ini.SetValue("Station", "Use10amInSummer", Use10amInSummer);
			//ini.SetValue("Station", "ConfirmClose", ConfirmClose);
			//ini.SetValue("Station", "CloseOnSuspend", CloseOnSuspend);
			//ini.SetValue("Station", "RestartIfUnplugged", RestartIfUnplugged);
			//ini.SetValue("Station", "RestartIfDataStops", RestartIfDataStops);
			ini.SetValue("Station", "SyncDavisClock", StationOptions.SyncTime);
			ini.SetValue("Station", "ClockSettingHour", StationOptions.ClockSettingHour);
			ini.SetValue("Station", "WS2300IgnoreStationClock", StationOptions.WS2300IgnoreStationClock);
			ini.SetValue("Station", "LogExtraSensors", StationOptions.LogExtraSensors);
			ini.SetValue("Station", "DataLogInterval", DataLogInterval);

			ini.SetValue("Station", "SyncFOReads", FineOffsetOptions.SyncReads);
			ini.SetValue("Station", "FOReadAvoidPeriod", FineOffsetOptions.ReadAvoidPeriod);
			ini.SetValue("Station", "FineOffsetReadTime", FineOffsetOptions.ReadTime);
			ini.SetValue("Station", "FineOffsetSetLoggerInterval", FineOffsetOptions.SetLoggerInterval);
			ini.SetValue("Station", "VendorID", FineOffsetOptions.VendorID);
			ini.SetValue("Station", "ProductID", FineOffsetOptions.ProductID);


			ini.SetValue("Station", "WindUnit", Units.Wind);
			ini.SetValue("Station", "PressureUnit", Units.Press);
			ini.SetValue("Station", "RainUnit", Units.Rain);
			ini.SetValue("Station", "TempUnit", Units.Temp);

			ini.SetValue("Station", "WindSpeedDecimals", WindDPlaces);
			ini.SetValue("Station", "WindSpeedAvgDecimals", WindAvgDPlaces);
			ini.SetValue("Station", "WindRunDecimals", WindRunDPlaces);
			ini.SetValue("Station", "SunshineHrsDecimals", SunshineDPlaces);
			ini.SetValue("Station", "PressDecimals", PressDPlaces);
			ini.SetValue("Station", "RainDecimals", RainDPlaces);
			ini.SetValue("Station", "TempDecimals", TempDPlaces);
			ini.SetValue("Station", "UVDecimals", UVDPlaces);
			ini.SetValue("Station", "AirQualityDecimals", AirQualityDPlaces);


			ini.SetValue("Station", "LocName", LocationName);
			ini.SetValue("Station", "LocDesc", LocationDesc);
			ini.SetValue("Station", "StartDate", RecordsBeganStr);
			ini.SetValue("Station", "YTDrain", YTDrain);
			ini.SetValue("Station", "YTDrainyear", YTDrainyear);
			ini.SetValue("Station", "UseDataLogger", UseDataLogger);
			ini.SetValue("Station", "UseCumulusForecast", UseCumulusForecast);
			ini.SetValue("Station", "HourlyForecast", HourlyForecast);
			ini.SetValue("Station", "UseCumulusPresstrendstr", StationOptions.UseCumulusPresstrendstr);
			ini.SetValue("Station", "FCpressinMB", FCpressinMB);
			ini.SetValue("Station", "FClowpress", FClowpress);
			ini.SetValue("Station", "FChighpress", FChighpress);
			//ini.SetValue("Station", "FCPressureThreshold", FCPressureThreshold);
			ini.SetValue("Station", "UseZeroBearing", StationOptions.UseZeroBearing);
			ini.SetValue("Station", "RoundWindSpeed", StationOptions.RoundWindSpeed);
			ini.SetValue("Station", "PrimaryAqSensor", StationOptions.PrimaryAqSensor);

			ini.SetValue("Station", "EWInterval", EwOptions.Interval);
			ini.SetValue("Station", "EWFile", EwOptions.Filename);
			ini.SetValue("Station", "EWminpressureMB", EwOptions.MinPressMB);
			ini.SetValue("Station", "EWmaxpressureMB", EwOptions.MaxPressMB);
			ini.SetValue("Station", "EWMaxRainTipDiff", EwOptions.MaxRainTipDiff);
			ini.SetValue("Station", "EWpressureoffset", EwOptions.PressOffset);

			ini.SetValue("Station", "EWtempdiff", Spike.TempDiff);
			ini.SetValue("Station", "EWpressurediff", Spike.PressDiff);
			ini.SetValue("Station", "EWhumiditydiff", Spike.HumidityDiff);
			ini.SetValue("Station", "EWgustdiff", Spike.GustDiff);
			ini.SetValue("Station", "EWwinddiff", Spike.WindDiff);
			ini.SetValue("Station", "EWmaxHourlyRain", Spike.MaxHourlyRain);
			ini.SetValue("Station", "EWmaxRainRate", Spike.MaxRainRate);
			ini.SetValue("Station", "EWinTempdiff", Spike.InTempDiff);
			ini.SetValue("Station", "EWinHumiditydiff", Spike.InHumDiff);

			ini.SetValue("Station", "RainSeasonStart", RainSeasonStart);
			ini.SetValue("Station", "RainDayThreshold", RainDayThreshold);

			ini.SetValue("Station", "ChillHourSeasonStart", ChillHourSeasonStart);
			ini.SetValue("Station", "ChillHourThreshold", ChillHourThreshold);

			ini.SetValue("Station", "ErrorLogSpikeRemoval", ErrorLogSpikeRemoval);

			ini.SetValue("Station", "ImetBaudRate", ImetOptions.BaudRate);
			ini.SetValue("Station", "ImetWaitTime", ImetOptions.WaitTime);
			ini.SetValue("Station", "ImetReadDelay", ImetOptions.ReadDelay);
			ini.SetValue("Station", "ImetUpdateLogPointer", ImetOptions.UpdateLogPointer);

			ini.SetValue("Station", "RG11Enabled", RG11Enabled);
			ini.SetValue("Station", "RG11portName", RG11Port);
			ini.SetValue("Station", "RG11TBRmode", RG11TBRmode);
			ini.SetValue("Station", "RG11tipsize", RG11tipsize);
			ini.SetValue("Station", "RG11IgnoreFirst", RG11IgnoreFirst);
			ini.SetValue("Station", "RG11DTRmode", RG11DTRmode);

			ini.SetValue("Station", "RG11Enabled2", RG11Enabled2);
			ini.SetValue("Station", "RG11portName2", RG11Port2);
			ini.SetValue("Station", "RG11TBRmode2", RG11TBRmode2);
			ini.SetValue("Station", "RG11tipsize2", RG11tipsize2);
			ini.SetValue("Station", "RG11IgnoreFirst2", RG11IgnoreFirst2);
			ini.SetValue("Station", "RG11DTRmode2", RG11DTRmode2);

			// WeatherFlow Options
			ini.SetValue("Station", "WeatherFlowDeviceId", WeatherFlowOptions.WFDeviceId);
			ini.SetValue("Station", "WeatherFlowTcpPort", WeatherFlowOptions.WFTcpPort);
			ini.SetValue("Station", "WeatherFlowToken", WeatherFlowOptions.WFToken);
			ini.SetValue("Station", "WeatherFlowDaysHist", WeatherFlowOptions.WFDaysHist);


			// WeatherLink Live device settings
			ini.SetValue("WLL", "AutoUpdateIpAddress", WLLAutoUpdateIpAddress);
			ini.SetValue("WLL", "WLv2ApiKey", Crypto.EncryptString(WllApiKey, Program.InstanceId, "WllApiKey"));
			ini.SetValue("WLL", "WLv2ApiSecret", Crypto.EncryptString(WllApiSecret, Program.InstanceId, "WllApiSecret"));
			ini.SetValue("WLL", "WLStationId", WllStationId);
			ini.SetValue("WLL", "PrimaryRainTxId", WllPrimaryRain);
			ini.SetValue("WLL", "PrimaryTempHumTxId", WllPrimaryTempHum);
			ini.SetValue("WLL", "PrimaryWindTxId", WllPrimaryWind);
			ini.SetValue("WLL", "PrimaryRainTxId", WllPrimaryRain);
			ini.SetValue("WLL", "PrimarySolarTxId", WllPrimarySolar);
			ini.SetValue("WLL", "PrimaryUvTxId", WllPrimaryUV);
			ini.SetValue("WLL", "ExtraSoilTempTxId1", WllExtraSoilTempTx1);
			ini.SetValue("WLL", "ExtraSoilTempIdx1", WllExtraSoilTempIdx1);
			ini.SetValue("WLL", "ExtraSoilTempTxId2", WllExtraSoilTempTx2);
			ini.SetValue("WLL", "ExtraSoilTempIdx2", WllExtraSoilTempIdx2);
			ini.SetValue("WLL", "ExtraSoilTempTxId3", WllExtraSoilTempTx3);
			ini.SetValue("WLL", "ExtraSoilTempIdx3", WllExtraSoilTempIdx3);
			ini.SetValue("WLL", "ExtraSoilTempTxId4", WllExtraSoilTempTx4);
			ini.SetValue("WLL", "ExtraSoilTempIdx4", WllExtraSoilTempIdx4);
			ini.SetValue("WLL", "ExtraSoilMoistureTxId1", WllExtraSoilMoistureTx1);
			ini.SetValue("WLL", "ExtraSoilMoistureIdx1", WllExtraSoilMoistureIdx1);
			ini.SetValue("WLL", "ExtraSoilMoistureTxId2", WllExtraSoilMoistureTx2);
			ini.SetValue("WLL", "ExtraSoilMoistureIdx2", WllExtraSoilMoistureIdx2);
			ini.SetValue("WLL", "ExtraSoilMoistureTxId3", WllExtraSoilMoistureTx3);
			ini.SetValue("WLL", "ExtraSoilMoistureIdx3", WllExtraSoilMoistureIdx3);
			ini.SetValue("WLL", "ExtraSoilMoistureTxId4", WllExtraSoilMoistureTx4);
			ini.SetValue("WLL", "ExtraSoilMoistureIdx4", WllExtraSoilMoistureIdx4);
			ini.SetValue("WLL", "ExtraLeafTxId1", WllExtraLeafTx1);
			ini.SetValue("WLL", "ExtraLeafIdx1", WllExtraLeafIdx1);
			ini.SetValue("WLL", "ExtraLeafTxId2", WllExtraLeafTx2);
			ini.SetValue("WLL", "ExtraLeafIdx2", WllExtraLeafIdx2);
			for (int i = 1; i <= 8; i++)
			{
				ini.SetValue("WLL", "ExtraTempTxId" + i, WllExtraTempTx[i - 1]);
				ini.SetValue("WLL", "ExtraHumOnTxId" + i, WllExtraHumTx[i - 1]);
			}

			// GW1000 settings
			ini.SetValue("GW1000", "IPAddress", Gw1000IpAddress);
			ini.SetValue("GW1000", "MACAddress", Gw1000MacAddress);
			ini.SetValue("GW1000", "AutoUpdateIpAddress", Gw1000AutoUpdateIpAddress);
			ini.SetValue("GW1000", "PrimaryTHSensor", Gw1000PrimaryTHSensor);
			ini.SetValue("GW1000", "PrimaryRainSensor", Gw1000PrimaryRainSensor);
			ini.SetValue("GW1000", "ExtraSensorDataEnabled", EcowittSettings.ExtraEnabled);
			ini.SetValue("GW1000", "ExtraSensorUseSolar", EcowittSettings.ExtraUseSolar);
			ini.SetValue("GW1000", "ExtraSensorUseUv", EcowittSettings.ExtraUseUv);
			ini.SetValue("GW1000", "ExtraSensorUseTempHum", EcowittSettings.ExtraUseTempHum);
			ini.SetValue("GW1000", "ExtraSensorUseSoilTemp", EcowittSettings.ExtraUseSoilTemp);
			ini.SetValue("GW1000", "ExtraSensorUseSoilMoist", EcowittSettings.ExtraUseSoilMoist);
			ini.SetValue("GW1000", "ExtraSensorUseLeafWet", EcowittSettings.ExtraUseLeafWet);
			ini.SetValue("GW1000", "ExtraSensorUseUserTemp", EcowittSettings.ExtraUseUserTemp);
			ini.SetValue("GW1000", "ExtraSensorUseAQI", EcowittSettings.ExtraUseAQI);
			ini.SetValue("GW1000", "ExtraSensorUseCo2", EcowittSettings.ExtraUseCo2);
			ini.SetValue("GW1000", "ExtraSensorUseLightning", EcowittSettings.ExtraUseLightning);
			ini.SetValue("GW1000", "ExtraSensorUseLeak", EcowittSettings.ExtraUseLeak);
			ini.SetValue("GW1000", "SetCustomServer", EcowittSettings.SetCustomServer);
			ini.SetValue("GW1000", "EcowittGwAddr", EcowittSettings.GatewayAddr);
			ini.SetValue("GW1000", "EcowittLocalAddr", EcowittSettings.LocalAddr);
			ini.SetValue("GW1000", "EcowittCustomInterval", EcowittSettings.CustomInterval);
			ini.SetValue("GW1000", "ExtraSetCustomServer", EcowittSettings.ExtraSetCustomServer);
			ini.SetValue("GW1000", "EcowittExtraGwAddr", EcowittSettings.ExtraGatewayAddr);
			ini.SetValue("GW1000", "EcowittExtraLocalAddr", EcowittSettings.ExtraLocalAddr);
			ini.SetValue("GW1000", "EcowittExtraCustomInterval", EcowittSettings.ExtraCustomInterval);
			// api
			ini.SetValue("GW1000", "EcowittAppKey", Crypto.EncryptString(EcowittSettings.AppKey, Program.InstanceId, "EcowittSettings.AppKey"));
			ini.SetValue("GW1000", "EcowittUserKey", Crypto.EncryptString(EcowittSettings.UserApiKey, Program.InstanceId, "EcowittSettings.UserApiKey"));
			ini.SetValue("GW1000", "EcowittMacAddress", EcowittSettings.MacAddress);
			// WN34 sensor mapping
			for (int i = 1; i <= 8; i++)
			{
				ini.SetValue("GW1000", "WN34MapChan" + i, EcowittSettings.MapWN34[i]);
			}

			// Ambient settings
			ini.SetValue("Ambient", "ExtraSensorDataEnabled", AmbientExtraEnabled);
			ini.SetValue("Ambient", "ExtraSensorUseSolar", AmbientExtraUseSolar);
			ini.SetValue("Ambient", "ExtraSensorUseUv", AmbientExtraUseUv);
			ini.SetValue("Ambient", "ExtraSensorUseTempHum", AmbientExtraUseTempHum);
			ini.SetValue("Ambient", "ExtraSensorUseSoilTemp", AmbientExtraUseSoilTemp);
			ini.SetValue("Ambient", "ExtraSensorUseSoilMoist", AmbientExtraUseSoilMoist);
			//ini.SetValue("Ambient", "ExtraSensorUseLeafWet", AmbientExtraUseLeafWet);
			ini.SetValue("Ambient", "ExtraSensorUseAQI", AmbientExtraUseAQI);
			ini.SetValue("Ambient", "ExtraSensorUseCo2", AmbientExtraUseCo2);
			ini.SetValue("Ambient", "ExtraSensorUseLightning", AmbientExtraUseLightning);
			ini.SetValue("Ambient", "ExtraSensorUseLeak", AmbientExtraUseLeak);


			// AirLink settings
			ini.SetValue("AirLink", "IsWllNode", AirLinkIsNode);
			ini.SetValue("AirLink", "WLv2ApiKey", Crypto.EncryptString(AirLinkApiKey, Program.InstanceId, "AirLinkApiKey"));
			ini.SetValue("AirLink", "WLv2ApiSecret", Crypto.EncryptString(AirLinkApiSecret, Program.InstanceId, "AirLinkApiSecret"));
			ini.SetValue("AirLink", "AutoUpdateIpAddress", AirLinkAutoUpdateIpAddress);
			ini.SetValue("AirLink", "In-Enabled", AirLinkInEnabled);
			ini.SetValue("AirLink", "In-IPAddress", AirLinkInIPAddr);
			ini.SetValue("AirLink", "In-WLStationId", AirLinkInStationId);
			ini.SetValue("AirLink", "In-Hostname", AirLinkInHostName);

			ini.SetValue("AirLink", "Out-Enabled", AirLinkOutEnabled);
			ini.SetValue("AirLink", "Out-IPAddress", AirLinkOutIPAddr);
			ini.SetValue("AirLink", "Out-WLStationId", AirLinkOutStationId);
			ini.SetValue("AirLink", "Out-Hostname", AirLinkOutHostName);
			ini.SetValue("AirLink", "AQIformula", airQualityIndex);

			ini.SetValue("Web Site", "ForumURL", ForumURL);
			ini.SetValue("Web Site", "WebcamURL", WebcamURL);

			ini.SetValue("FTP site", "Enabled", FtpOptions.Enabled);
			ini.SetValue("FTP site", "Host", FtpOptions.Hostname);
			ini.SetValue("FTP site", "Port", FtpOptions.Port);
			ini.SetValue("FTP site", "Username", Crypto.EncryptString(FtpOptions.Username, Program.InstanceId, "FtpOptions.Username"));
			ini.SetValue("FTP site", "Password", Crypto.EncryptString(FtpOptions.Password, Program.InstanceId, "FtpOptions.Password"));
			ini.SetValue("FTP site", "Directory", FtpOptions.Directory);

			//ini.SetValue("FTP site", "AutoUpdate", WebAutoUpdate);  // Deprecated - now read-only
			ini.SetValue("FTP site", "Sslftp", (int)FtpOptions.FtpMode);
			// BUILD 3092 - added alternate SFTP authentication options
			ini.SetValue("FTP site", "SshFtpAuthentication", FtpOptions.SshAuthen);
			ini.SetValue("FTP site", "SshFtpPskFile", FtpOptions.SshPskFile);
			ini.SetValue("FTP site", "ConnectionAutoDetect", FtpOptions.AutoDetect);
			ini.SetValue("FTP site", "IgnoreCertErrors", FtpOptions.IgnoreCertErrors);

			ini.SetValue("FTP site", "FTPlogging", FtpOptions.Logging);
			ini.SetValue("FTP site", "FTPloggingLevel", FtpOptions.LoggingLevel);
			ini.SetValue("FTP site", "UTF8encode", UTF8encode);
			ini.SetValue("FTP site", "EnableRealtime", RealtimeIntervalEnabled);
			ini.SetValue("FTP site", "RealtimeInterval", RealtimeInterval);
			ini.SetValue("FTP site", "RealtimeFTPEnabled", FtpOptions.RealtimeEnabled);
			ini.SetValue("FTP site", "RealtimeTxtCreate", RealtimeFiles[0].Create);
			ini.SetValue("FTP site", "RealtimeTxtFTP", RealtimeFiles[0].FTP);
			ini.SetValue("FTP site", "RealtimeTxtCopy", RealtimeFiles[0].Copy);

			ini.SetValue("FTP site", "RealtimeGaugesTxtCreate", RealtimeFiles[1].Create);
			ini.SetValue("FTP site", "RealtimeGaugesTxtFTP", RealtimeFiles[1].FTP);
			ini.SetValue("FTP site", "RealtimeGaugesTxtCopy", RealtimeFiles[1].Copy);

			ini.SetValue("FTP site", "IntervalEnabled", WebIntervalEnabled);
			ini.SetValue("FTP site", "IntervalFtpEnabled", FtpOptions.IntervalEnabled);

			ini.SetValue("FTP site", "UpdateInterval", UpdateInterval);
			for (var i = 0; i < StdWebFiles.Length; i++)
			{
				var keyNameCreate = "Create-" + StdWebFiles[i].LocalFileName.Split('.')[0];
				var keyNameFTP = "Ftp-" + StdWebFiles[i].LocalFileName.Split('.')[0];
				var keyNameCopy = "Copy-" + StdWebFiles[i].LocalFileName.Split('.')[0];
				ini.SetValue("FTP site", keyNameCreate, StdWebFiles[i].Create);
				ini.SetValue("FTP site", keyNameFTP, StdWebFiles[i].FTP);
				ini.SetValue("FTP site", keyNameCopy, StdWebFiles[i].Copy);
			}

			for (var i = 0; i < GraphDataFiles.Length; i++)
			{
				var keyNameCreate = "Create-" + GraphDataFiles[i].LocalFileName.Split('.')[0];
				var keyNameFTP = "Ftp-" + GraphDataFiles[i].LocalFileName.Split('.')[0];
				var keyNameCopy = "Copy-" + GraphDataFiles[i].LocalFileName.Split('.')[0];
				ini.SetValue("FTP site", keyNameCreate, GraphDataFiles[i].Create);
				ini.SetValue("FTP site", keyNameFTP, GraphDataFiles[i].FTP);
				ini.SetValue("FTP site", keyNameCopy, GraphDataFiles[i].Copy);
			}

			for (var i = 0; i < GraphDataEodFiles.Length; i++)
			{
				var keyNameCreate = "Create-" + GraphDataEodFiles[i].LocalFileName.Split('.')[0];
				var keyNameFTP = "Ftp-" + GraphDataEodFiles[i].LocalFileName.Split('.')[0];
				var keyNameCopy = "Copy-" + GraphDataEodFiles[i].LocalFileName.Split('.')[0];
				ini.SetValue("FTP site", keyNameCreate, GraphDataEodFiles[i].Create);
				ini.SetValue("FTP site", keyNameFTP, GraphDataEodFiles[i].FTP);
				ini.SetValue("FTP site", keyNameCopy, GraphDataEodFiles[i].Copy);
			}

			ini.SetValue("FTP site", "IncludeMoonImage", MoonImage.Ftp);
			ini.SetValue("FTP site", "CopyMoonImage", MoonImage.Copy);
			ini.SetValue("FTP site", "FTPRename", FTPRename);
			ini.SetValue("FTP site", "DeleteBeforeUpload", DeleteBeforeUpload);
			ini.SetValue("FTP site", "ActiveFTP", FtpOptions.ActiveMode);
			ini.SetValue("FTP site", "DisableEPSV", FtpOptions.DisableEPSV);
			ini.SetValue("FTP site", "DisableFtpsExplicit", FtpOptions.DisableExplicit);


			for (int i = 0; i < numextrafiles; i++)
			{
				if (string.IsNullOrEmpty(ExtraFiles[i].local) && string.IsNullOrEmpty(ExtraFiles[i].remote))
				{
					ini.DeleteValue("FTP site", "ExtraLocal" + i);
					ini.DeleteValue("FTP site", "ExtraRemote" + i);
					ini.DeleteValue("FTP site", "ExtraProcess" + i);
					ini.DeleteValue("FTP site", "ExtraBinary" + i);
					ini.DeleteValue("FTP site", "ExtraRealtime" + i);
					ini.DeleteValue("FTP site", "ExtraFTP" + i);
					ini.DeleteValue("FTP site", "ExtraUTF" + i);
					ini.DeleteValue("FTP site", "ExtraEOD" + i);

				}
				else
				{
					ini.SetValue("FTP site", "ExtraLocal" + i, ExtraFiles[i].local);
					ini.SetValue("FTP site", "ExtraRemote" + i, ExtraFiles[i].remote);
					ini.SetValue("FTP site", "ExtraProcess" + i, ExtraFiles[i].process);
					ini.SetValue("FTP site", "ExtraBinary" + i, ExtraFiles[i].binary);
					ini.SetValue("FTP site", "ExtraRealtime" + i, ExtraFiles[i].realtime);
					ini.SetValue("FTP site", "ExtraFTP" + i, ExtraFiles[i].FTP);
					ini.SetValue("FTP site", "ExtraUTF" + i, ExtraFiles[i].UTF8);
					ini.SetValue("FTP site", "ExtraEOD" + i, ExtraFiles[i].endofday);
				}
			}

			ini.SetValue("FTP site", "ExternalProgram", ExternalProgram);
			ini.SetValue("FTP site", "RealtimeProgram", RealtimeProgram);
			ini.SetValue("FTP site", "DailyProgram", DailyProgram);
			ini.SetValue("FTP site", "ExternalParams", ExternalParams);
			ini.SetValue("FTP site", "RealtimeParams", RealtimeParams);
			ini.SetValue("FTP site", "DailyParams", DailyParams);

			// Local Copy Options
			ini.SetValue("FTP site", "EnableLocalCopy", FtpOptions.LocalCopyEnabled);
			ini.SetValue("FTP site", "LocalCopyFolder", FtpOptions.LocalCopyFolder);


			ini.SetValue("Station", "CloudBaseInFeet", CloudBaseInFeet);

			ini.SetValue("Wunderground", "ID", Wund.ID);
			ini.SetValue("Wunderground", "Password", Crypto.EncryptString(Wund.PW, Program.InstanceId, "Wund.PW"));
			ini.SetValue("Wunderground", "Enabled", Wund.Enabled);
			ini.SetValue("Wunderground", "RapidFire", Wund.RapidFireEnabled);
			ini.SetValue("Wunderground", "Interval", Wund.Interval);
			ini.SetValue("Wunderground", "SendUV", Wund.SendUV);
			ini.SetValue("Wunderground", "SendSR", Wund.SendSolar);
			ini.SetValue("Wunderground", "SendIndoor", Wund.SendIndoor);
			ini.SetValue("Wunderground", "SendAverage", Wund.SendAverage);
			ini.SetValue("Wunderground", "CatchUp", Wund.CatchUp);
			ini.SetValue("Wunderground", "SendSoilTemp1", Wund.SendSoilTemp1);
			ini.SetValue("Wunderground", "SendSoilTemp2", Wund.SendSoilTemp2);
			ini.SetValue("Wunderground", "SendSoilTemp3", Wund.SendSoilTemp3);
			ini.SetValue("Wunderground", "SendSoilTemp4", Wund.SendSoilTemp4);
			ini.SetValue("Wunderground", "SendSoilMoisture1", Wund.SendSoilMoisture1);
			ini.SetValue("Wunderground", "SendSoilMoisture2", Wund.SendSoilMoisture2);
			ini.SetValue("Wunderground", "SendSoilMoisture3", Wund.SendSoilMoisture3);
			ini.SetValue("Wunderground", "SendSoilMoisture4", Wund.SendSoilMoisture4);
			ini.SetValue("Wunderground", "SendLeafWetness1", Wund.SendLeafWetness1);
			ini.SetValue("Wunderground", "SendLeafWetness2", Wund.SendLeafWetness2);
			ini.SetValue("Wunderground", "SendAirQuality", Wund.SendAirQuality);

			ini.SetValue("Windy", "APIkey", Crypto.EncryptString(Windy.ApiKey, Program.InstanceId, "Windy.ApiKey"));
			ini.SetValue("Windy", "StationIdx", Windy.StationIdx);
			ini.SetValue("Windy", "Enabled", Windy.Enabled);
			ini.SetValue("Windy", "Interval", Windy.Interval);
			ini.SetValue("Windy", "SendUV", Windy.SendUV);
			ini.SetValue("Windy", "CatchUp", Windy.CatchUp);

			ini.SetValue("Awekas", "User", AWEKAS.ID);
			ini.SetValue("Awekas", "Password", Crypto.EncryptString(AWEKAS.PW, Program.InstanceId, "AWEKAS.PW"));
			ini.SetValue("Awekas", "Language", AWEKAS.Lang);
			ini.SetValue("Awekas", "Enabled", AWEKAS.Enabled);
			ini.SetValue("Awekas", "Interval", AWEKAS.Interval);
			ini.SetValue("Awekas", "SendUV", AWEKAS.SendUV);
			ini.SetValue("Awekas", "SendSR", AWEKAS.SendSolar);
			ini.SetValue("Awekas", "SendSoilTemp", AWEKAS.SendSoilTemp);
			ini.SetValue("Awekas", "SendIndoor", AWEKAS.SendIndoor);
			ini.SetValue("Awekas", "SendSoilMoisture", AWEKAS.SendSoilMoisture);
			ini.SetValue("Awekas", "SendLeafWetness", AWEKAS.SendLeafWetness);
			ini.SetValue("Awekas", "SendAirQuality", AWEKAS.SendAirQuality);

			ini.SetValue("WeatherCloud", "Wid", WCloud.ID);
			ini.SetValue("WeatherCloud", "Key", Crypto.EncryptString(WCloud.PW, Program.InstanceId, "WCloud.PW"));
			ini.SetValue("WeatherCloud", "Enabled", WCloud.Enabled);
			ini.SetValue("WeatherCloud", "Interval", WCloud.Interval);
			ini.SetValue("WeatherCloud", "SendUV", WCloud.SendUV);
			ini.SetValue("WeatherCloud", "SendSR", WCloud.SendSolar);
			ini.SetValue("WeatherCloud", "SendAQI", WCloud.SendAirQuality);
			ini.SetValue("WeatherCloud", "SendSoilMoisture", WCloud.SendSoilMoisture);
			ini.SetValue("WeatherCloud", "SoilMoistureSensor", WCloud.SoilMoistureSensor);
			ini.SetValue("WeatherCloud", "SendLeafWetness", WCloud.SendLeafWetness);
			ini.SetValue("WeatherCloud", "LeafWetnessSensor", WCloud.LeafWetnessSensor);

			//ini.SetValue("Twitter", "User", Twitter.ID);
			//ini.SetValue("Twitter", "Password", Crypto.EncryptString(Twitter.PW));
			//ini.SetValue("Twitter", "Enabled", Twitter.Enabled);
			//ini.SetValue("Twitter", "Interval", Twitter.Interval);
			//ini.SetValue("Twitter", "OauthToken", Twitter.OauthToken);
			//ini.SetValue("Twitter", "OauthTokenSecret", Crypto.EncryptString(Twitter.OauthTokenSecret));
			//ini.SetValue("Twitter", "TwitterSendLocation", Twitter.SendLocation);

			ini.SetValue("PWSweather", "ID", PWS.ID);
			ini.SetValue("PWSweather", "Password", Crypto.EncryptString(PWS.PW, Program.InstanceId, "PWS.PW"));
			ini.SetValue("PWSweather", "Enabled", PWS.Enabled);
			ini.SetValue("PWSweather", "Interval", PWS.Interval);
			ini.SetValue("PWSweather", "SendUV", PWS.SendUV);
			ini.SetValue("PWSweather", "SendSR", PWS.SendSolar);
			ini.SetValue("PWSweather", "CatchUp", PWS.CatchUp);

			ini.SetValue("WOW", "ID", WOW.ID);
			ini.SetValue("WOW", "Password", Crypto.EncryptString(WOW.PW, Program.InstanceId, "WOW.PW"));
			ini.SetValue("WOW", "Enabled", WOW.Enabled);
			ini.SetValue("WOW", "Interval", WOW.Interval);
			ini.SetValue("WOW", "SendUV", WOW.SendUV);
			ini.SetValue("WOW", "SendSR", WOW.SendSolar);
			ini.SetValue("WOW", "SendSoilTemp", WOW.SendSoilTemp);
			ini.SetValue("WOW", "SoilTempSensor", WOW.SoilTempSensor);
			ini.SetValue("WOW", "CatchUp", WOW.CatchUp);

			ini.SetValue("APRS", "ID", APRS.ID);
			ini.SetValue("APRS", "pass", Crypto.EncryptString(APRS.PW, Program.InstanceId, "APRS.PW"));
			ini.SetValue("APRS", "server", APRS.Server);
			ini.SetValue("APRS", "port", APRS.Port);
			ini.SetValue("APRS", "Enabled", APRS.Enabled);
			ini.SetValue("APRS", "Interval", APRS.Interval);
			ini.SetValue("APRS", "SendSR", APRS.SendSolar);
			ini.SetValue("APRS", "APRSHumidityCutoff", APRS.HumidityCutoff);

			ini.SetValue("OpenWeatherMap", "Enabled", OpenWeatherMap.Enabled);
			ini.SetValue("OpenWeatherMap", "CatchUp", OpenWeatherMap.CatchUp);
			ini.SetValue("OpenWeatherMap", "APIkey", Crypto.EncryptString(OpenWeatherMap.PW, Program.InstanceId, "OpenWeatherMap.PW"));
			ini.SetValue("OpenWeatherMap", "StationId", OpenWeatherMap.ID);
			ini.SetValue("OpenWeatherMap", "Interval", OpenWeatherMap.Interval);

			ini.SetValue("WindGuru", "Enabled", WindGuru.Enabled);
			ini.SetValue("WindGuru", "StationUID", WindGuru.ID);
			ini.SetValue("WindGuru", "Password", Crypto.EncryptString(WindGuru.PW, Program.InstanceId, "WindGuru.PW"));
			ini.SetValue("WindGuru", "Interval", WindGuru.Interval);
			ini.SetValue("WindGuru", "SendRain", WindGuru.SendRain);

			ini.SetValue("MQTT", "Server", MQTT.Server);
			ini.SetValue("MQTT", "Port", MQTT.Port);
			ini.SetValue("MQTT", "UseTLS", MQTT.UseTLS);
			ini.SetValue("MQTT", "Username", Crypto.EncryptString(MQTT.Username, Program.InstanceId, "MQTT.Username,"));
			ini.SetValue("MQTT", "Password", Crypto.EncryptString(MQTT.Password, Program.InstanceId, "MQTT.Password"));
			ini.SetValue("MQTT", "EnableDataUpdate", MQTT.EnableDataUpdate);
			ini.SetValue("MQTT", "UpdateTemplate", MQTT.UpdateTemplate);
			ini.SetValue("MQTT", "EnableInterval", MQTT.EnableInterval);
			ini.SetValue("MQTT", "IntervalTime", MQTT.IntervalTime);
			ini.SetValue("MQTT", "IntervalTemplate", MQTT.IntervalTemplate);

			ini.SetValue("Alarms", "alarmlowtemp", LowTempAlarm.Value);
			ini.SetValue("Alarms", "LowTempAlarmSet", LowTempAlarm.Enabled);
			ini.SetValue("Alarms", "LowTempAlarmSound", LowTempAlarm.Sound);
			ini.SetValue("Alarms", "LowTempAlarmSoundFile", LowTempAlarm.SoundFile);
			ini.SetValue("Alarms", "LowTempAlarmNotify", LowTempAlarm.Notify);
			ini.SetValue("Alarms", "LowTempAlarmEmail", LowTempAlarm.Email);
			ini.SetValue("Alarms", "LowTempAlarmLatch", LowTempAlarm.Latch);
			ini.SetValue("Alarms", "LowTempAlarmLatchHours", LowTempAlarm.LatchHours);
			ini.SetValue("Alarms", "LowTempAlarmAction", LowTempAlarm.Action);
			ini.SetValue("Alarms", "LowTempAlarmActionParams", LowTempAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmhightemp", HighTempAlarm.Value);
			ini.SetValue("Alarms", "HighTempAlarmSet", HighTempAlarm.Enabled);
			ini.SetValue("Alarms", "HighTempAlarmSound", HighTempAlarm.Sound);
			ini.SetValue("Alarms", "HighTempAlarmSoundFile", HighTempAlarm.SoundFile);
			ini.SetValue("Alarms", "HighTempAlarmNotify", HighTempAlarm.Notify);
			ini.SetValue("Alarms", "HighTempAlarmEmail", HighTempAlarm.Email);
			ini.SetValue("Alarms", "HighTempAlarmLatch", HighTempAlarm.Latch);
			ini.SetValue("Alarms", "HighTempAlarmLatchHours", HighTempAlarm.LatchHours);
			ini.SetValue("Alarms", "HighTempAlarmAction", HighTempAlarm.Action);
			ini.SetValue("Alarms", "HighTempAlarmActionParams", HighTempAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmtempchange", TempChangeAlarm.Value);
			ini.SetValue("Alarms", "TempChangeAlarmSet", TempChangeAlarm.Enabled);
			ini.SetValue("Alarms", "TempChangeAlarmSound", TempChangeAlarm.Sound);
			ini.SetValue("Alarms", "TempChangeAlarmSoundFile", TempChangeAlarm.SoundFile);
			ini.SetValue("Alarms", "TempChangeAlarmNotify", TempChangeAlarm.Notify);
			ini.SetValue("Alarms", "TempChangeAlarmEmail", TempChangeAlarm.Email);
			ini.SetValue("Alarms", "TempChangeAlarmLatch", TempChangeAlarm.Latch);
			ini.SetValue("Alarms", "TempChangeAlarmLatchHours", TempChangeAlarm.LatchHours);
			ini.SetValue("Alarms", "TempChangeAlarmAction", TempChangeAlarm.Action);
			ini.SetValue("Alarms", "TempChangeAlarmActionParams", TempChangeAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmlowpress", LowPressAlarm.Value);
			ini.SetValue("Alarms", "LowPressAlarmSet", LowPressAlarm.Enabled);
			ini.SetValue("Alarms", "LowPressAlarmSound", LowPressAlarm.Sound);
			ini.SetValue("Alarms", "LowPressAlarmSoundFile", LowPressAlarm.SoundFile);
			ini.SetValue("Alarms", "LowPressAlarmNotify", LowPressAlarm.Notify);
			ini.SetValue("Alarms", "LowPressAlarmEmail", LowPressAlarm.Email);
			ini.SetValue("Alarms", "LowPressAlarmLatch", LowPressAlarm.Latch);
			ini.SetValue("Alarms", "LowPressAlarmLatchHours", LowPressAlarm.LatchHours);
			ini.SetValue("Alarms", "LowPressAlarmAction", LowPressAlarm.Action);
			ini.SetValue("Alarms", "LowPressAlarmActionParams", LowPressAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmhighpress", HighPressAlarm.Value);
			ini.SetValue("Alarms", "HighPressAlarmSet", HighPressAlarm.Enabled);
			ini.SetValue("Alarms", "HighPressAlarmSound", HighPressAlarm.Sound);
			ini.SetValue("Alarms", "HighPressAlarmSoundFile", HighPressAlarm.SoundFile);
			ini.SetValue("Alarms", "HighPressAlarmNotify", HighPressAlarm.Notify);
			ini.SetValue("Alarms", "HighPressAlarmEmail", HighPressAlarm.Email);
			ini.SetValue("Alarms", "HighPressAlarmLatch", HighPressAlarm.Latch);
			ini.SetValue("Alarms", "HighPressAlarmLatchHours", HighPressAlarm.LatchHours);
			ini.SetValue("Alarms", "HighPressAlarmAction", HighPressAlarm.Action);
			ini.SetValue("Alarms", "HighPressAlarmActionParams", HighPressAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmpresschange", PressChangeAlarm.Value);
			ini.SetValue("Alarms", "PressChangeAlarmSet", PressChangeAlarm.Enabled);
			ini.SetValue("Alarms", "PressChangeAlarmSound", PressChangeAlarm.Sound);
			ini.SetValue("Alarms", "PressChangeAlarmSoundFile", PressChangeAlarm.SoundFile);
			ini.SetValue("Alarms", "PressChangeAlarmNotify", PressChangeAlarm.Notify);
			ini.SetValue("Alarms", "PressChangeAlarmEmail", PressChangeAlarm.Email);
			ini.SetValue("Alarms", "PressChangeAlarmLatch", PressChangeAlarm.Latch);
			ini.SetValue("Alarms", "PressChangeAlarmLatchHours", PressChangeAlarm.LatchHours);
			ini.SetValue("Alarms", "PressChangeAlarmAction", PressChangeAlarm.Action);
			ini.SetValue("Alarms", "PressChangeAlarmActionParams", PressChangeAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmhighraintoday", HighRainTodayAlarm.Value);
			ini.SetValue("Alarms", "HighRainTodayAlarmSet", HighRainTodayAlarm.Enabled);
			ini.SetValue("Alarms", "HighRainTodayAlarmSound", HighRainTodayAlarm.Sound);
			ini.SetValue("Alarms", "HighRainTodayAlarmSoundFile", HighRainTodayAlarm.SoundFile);
			ini.SetValue("Alarms", "HighRainTodayAlarmNotify", HighRainTodayAlarm.Notify);
			ini.SetValue("Alarms", "HighRainTodayAlarmEmail", HighRainTodayAlarm.Email);
			ini.SetValue("Alarms", "HighRainTodayAlarmLatch", HighRainTodayAlarm.Latch);
			ini.SetValue("Alarms", "HighRainTodayAlarmLatchHours", HighRainTodayAlarm.LatchHours);
			ini.SetValue("Alarms", "HighRainTodayAlarmAction", HighRainTodayAlarm.Action);
			ini.SetValue("Alarms", "HighRainTodayAlarmActionParams", HighRainTodayAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmhighrainrate", HighRainRateAlarm.Value);
			ini.SetValue("Alarms", "HighRainRateAlarmSet", HighRainRateAlarm.Enabled);
			ini.SetValue("Alarms", "HighRainRateAlarmSound", HighRainRateAlarm.Sound);
			ini.SetValue("Alarms", "HighRainRateAlarmSoundFile", HighRainRateAlarm.SoundFile);
			ini.SetValue("Alarms", "HighRainRateAlarmNotify", HighRainRateAlarm.Notify);
			ini.SetValue("Alarms", "HighRainRateAlarmEmail", HighRainRateAlarm.Email);
			ini.SetValue("Alarms", "HighRainRateAlarmLatch", HighRainRateAlarm.Latch);
			ini.SetValue("Alarms", "HighRainRateAlarmLatchHours", HighRainRateAlarm.LatchHours);
			ini.SetValue("Alarms", "HighRainRateAlarmAction", HighRainRateAlarm.Action);
			ini.SetValue("Alarms", "HighRainRateAlarmActionParams", HighRainRateAlarm.ActionParams);

			ini.SetValue("Alarms", "IsRainingAlarmSet", IsRainingAlarm.Enabled);
			ini.SetValue("Alarms", "IsRainingAlarmSound", IsRainingAlarm.Sound);
			ini.SetValue("Alarms", "IsRainingAlarmSoundFile", IsRainingAlarm.SoundFile);
			ini.SetValue("Alarms", "IsRainingAlarmNotify", IsRainingAlarm.Notify);
			ini.SetValue("Alarms", "IsRainingAlarmEmail", IsRainingAlarm.Email);
			ini.SetValue("Alarms", "IsRainingAlarmLatch", IsRainingAlarm.Latch);
			ini.SetValue("Alarms", "IsRainingAlarmLatchHours", IsRainingAlarm.LatchHours);
			ini.SetValue("Alarms", "IsRainingAlarmTriggerCount", IsRainingAlarm.TriggerThreshold);
			ini.SetValue("Alarms", "IsRainingAlarmAction", IsRainingAlarm.Action);
			ini.SetValue("Alarms", "IsRainingAlarmActionParams", IsRainingAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmhighgust", HighGustAlarm.Value);
			ini.SetValue("Alarms", "HighGustAlarmSet", HighGustAlarm.Enabled);
			ini.SetValue("Alarms", "HighGustAlarmSound", HighGustAlarm.Sound);
			ini.SetValue("Alarms", "HighGustAlarmSoundFile", HighGustAlarm.SoundFile);
			ini.SetValue("Alarms", "HighGustAlarmNotify", HighGustAlarm.Notify);
			ini.SetValue("Alarms", "HighGustAlarmEmail", HighGustAlarm.Email);
			ini.SetValue("Alarms", "HighGustAlarmLatch", HighGustAlarm.Latch);
			ini.SetValue("Alarms", "HighGustAlarmLatchHours", HighGustAlarm.LatchHours);
			ini.SetValue("Alarms", "HighGustAlarmAction", HighGustAlarm.Action);
			ini.SetValue("Alarms", "HighGustAlarmActionParams", HighGustAlarm.ActionParams);

			ini.SetValue("Alarms", "alarmhighwind", HighWindAlarm.Value);
			ini.SetValue("Alarms", "HighWindAlarmSet", HighWindAlarm.Enabled);
			ini.SetValue("Alarms", "HighWindAlarmSound", HighWindAlarm.Sound);
			ini.SetValue("Alarms", "HighWindAlarmSoundFile", HighWindAlarm.SoundFile);
			ini.SetValue("Alarms", "HighWindAlarmNotify", HighWindAlarm.Notify);
			ini.SetValue("Alarms", "HighWindAlarmEmail", HighWindAlarm.Email);
			ini.SetValue("Alarms", "HighWindAlarmLatch", HighWindAlarm.Latch);
			ini.SetValue("Alarms", "HighWindAlarmLatchHours", HighWindAlarm.LatchHours);
			ini.SetValue("Alarms", "HighWindAlarmAction", HighWindAlarm.Action);
			ini.SetValue("Alarms", "HighWindAlarmActionParams", HighWindAlarm.ActionParams);

			ini.SetValue("Alarms", "SensorAlarmSet", SensorAlarm.Enabled);
			ini.SetValue("Alarms", "SensorAlarmSound", SensorAlarm.Sound);
			ini.SetValue("Alarms", "SensorAlarmSoundFile", SensorAlarm.SoundFile);
			ini.SetValue("Alarms", "SensorAlarmNotify", SensorAlarm.Notify);
			ini.SetValue("Alarms", "SensorAlarmEmail", SensorAlarm.Email);
			ini.SetValue("Alarms", "SensorAlarmLatch", SensorAlarm.Latch);
			ini.SetValue("Alarms", "SensorAlarmLatchHours", SensorAlarm.LatchHours);
			ini.SetValue("Alarms", "SensorAlarmTriggerCount", SensorAlarm.TriggerThreshold);
			ini.SetValue("Alarms", "SensorAlarmAction", SensorAlarm.Action);
			ini.SetValue("Alarms", "SensorAlarmActionParams", SensorAlarm.ActionParams);

			ini.SetValue("Alarms", "DataStoppedAlarmSet", DataStoppedAlarm.Enabled);
			ini.SetValue("Alarms", "DataStoppedAlarmSound", DataStoppedAlarm.Sound);
			ini.SetValue("Alarms", "DataStoppedAlarmSoundFile", DataStoppedAlarm.SoundFile);
			ini.SetValue("Alarms", "DataStoppedAlarmNotify", DataStoppedAlarm.Notify);
			ini.SetValue("Alarms", "DataStoppedAlarmEmail", DataStoppedAlarm.Email);
			ini.SetValue("Alarms", "DataStoppedAlarmLatch", DataStoppedAlarm.Latch);
			ini.SetValue("Alarms", "DataStoppedAlarmLatchHours", DataStoppedAlarm.LatchHours);
			ini.SetValue("Alarms", "DataStoppedAlarmTriggerCount", DataStoppedAlarm.TriggerThreshold);
			ini.SetValue("Alarms", "DataStoppedAlarmAction", DataStoppedAlarm.Action);
			ini.SetValue("Alarms", "DataStoppedAlarmActionParams", DataStoppedAlarm.ActionParams);

			ini.SetValue("Alarms", "BatteryLowAlarmSet", BatteryLowAlarm.Enabled);
			ini.SetValue("Alarms", "BatteryLowAlarmSound", BatteryLowAlarm.Sound);
			ini.SetValue("Alarms", "BatteryLowAlarmSoundFile", BatteryLowAlarm.SoundFile);
			ini.SetValue("Alarms", "BatteryLowAlarmNotify", BatteryLowAlarm.Notify);
			ini.SetValue("Alarms", "BatteryLowAlarmEmail", BatteryLowAlarm.Email);
			ini.SetValue("Alarms", "BatteryLowAlarmLatch", BatteryLowAlarm.Latch);
			ini.SetValue("Alarms", "BatteryLowAlarmLatchHours", BatteryLowAlarm.LatchHours);
			ini.SetValue("Alarms", "BatteryLowAlarmTriggerCount", BatteryLowAlarm.TriggerThreshold);
			ini.SetValue("Alarms", "BatteryLowAlarmAction", BatteryLowAlarm.Action);
			ini.SetValue("Alarms", "BatteryLowAlarmActionParams", BatteryLowAlarm.ActionParams);

			ini.SetValue("Alarms", "DataSpikeAlarmSet", SpikeAlarm.Enabled);
			ini.SetValue("Alarms", "DataSpikeAlarmSound", SpikeAlarm.Sound);
			ini.SetValue("Alarms", "DataSpikeAlarmSoundFile", SpikeAlarm.SoundFile);
			ini.SetValue("Alarms", "DataSpikeAlarmNotify", SpikeAlarm.Notify);
			ini.SetValue("Alarms", "DataSpikeAlarmEmail", SpikeAlarm.Email);
			ini.SetValue("Alarms", "DataSpikeAlarmLatch", SpikeAlarm.Latch);
			ini.SetValue("Alarms", "DataSpikeAlarmLatchHours", SpikeAlarm.LatchHours);
			ini.SetValue("Alarms", "DataSpikeAlarmTriggerCount", SpikeAlarm.TriggerThreshold);
			ini.SetValue("Alarms", "DataSpikeAlarmAction", SpikeAlarm.Action);
			ini.SetValue("Alarms", "DataSpikeAlarmActionParams", SpikeAlarm.ActionParams);

			ini.SetValue("Alarms", "UpgradeAlarmSet", UpgradeAlarm.Enabled);
			ini.SetValue("Alarms", "UpgradeAlarmSound", UpgradeAlarm.Sound);
			ini.SetValue("Alarms", "UpgradeAlarmSoundFile", UpgradeAlarm.SoundFile);
			ini.SetValue("Alarms", "UpgradeAlarmNotify", UpgradeAlarm.Notify);
			ini.SetValue("Alarms", "UpgradeAlarmEmail", UpgradeAlarm.Email);
			ini.SetValue("Alarms", "UpgradeAlarmLatch", UpgradeAlarm.Latch);
			ini.SetValue("Alarms", "UpgradeAlarmLatchHours", UpgradeAlarm.LatchHours);
			ini.SetValue("Alarms", "UpgradeAlarmAction", UpgradeAlarm.Action);
			ini.SetValue("Alarms", "UpgradeAlarmActionParams", UpgradeAlarm.ActionParams);

			ini.SetValue("Alarms", "HttpUploadAlarmSet", HttpUploadAlarm.Enabled);
			ini.SetValue("Alarms", "HttpUploadAlarmSound", HttpUploadAlarm.Sound);
			ini.SetValue("Alarms", "HttpUploadAlarmSoundFile", HttpUploadAlarm.SoundFile);
			ini.SetValue("Alarms", "HttpUploadAlarmNotify", HttpUploadAlarm.Notify);
			ini.SetValue("Alarms", "HttpUploadAlarmEmail", HttpUploadAlarm.Email);
			ini.SetValue("Alarms", "HttpUploadAlarmLatch", HttpUploadAlarm.Latch);
			ini.SetValue("Alarms", "HttpUploadAlarmLatchHours", HttpUploadAlarm.LatchHours);
			ini.SetValue("Alarms", "HttpUploadAlarmTriggerCount", HttpUploadAlarm.TriggerThreshold);
			ini.SetValue("Alarms", "HttpUploadAlarmAction", HttpUploadAlarm.Action);
			ini.SetValue("Alarms", "HttpUploadAlarmActionParams", HttpUploadAlarm.ActionParams);

			ini.SetValue("Alarms", "MySqlUploadAlarmSet", MySqlUploadAlarm.Enabled);
			ini.SetValue("Alarms", "MySqlUploadAlarmSound", MySqlUploadAlarm.Sound);
			ini.SetValue("Alarms", "MySqlUploadAlarmSoundFile", MySqlUploadAlarm.SoundFile);
			ini.SetValue("Alarms", "MySqlUploadAlarmNotify", MySqlUploadAlarm.Notify);
			ini.SetValue("Alarms", "MySqlUploadAlarmEmail", MySqlUploadAlarm.Email);
			ini.SetValue("Alarms", "MySqlUploadAlarmLatch", MySqlUploadAlarm.Latch);
			ini.SetValue("Alarms", "MySqlUploadAlarmLatchHours", MySqlUploadAlarm.LatchHours);
			ini.SetValue("Alarms", "MySqlUploadAlarmTriggerCount", MySqlUploadAlarm.TriggerThreshold);
			ini.SetValue("Alarms", "MySqlUploadAlarmAction", MySqlUploadAlarm.Action);
			ini.SetValue("Alarms", "MySqlUploadAlarmActionParams", MySqlUploadAlarm.ActionParams);

			ini.SetValue("Alarms", "FromEmail", AlarmFromEmail);
			ini.SetValue("Alarms", "DestEmail", AlarmDestEmail.Join(";"));
			ini.SetValue("Alarms", "UseHTML", AlarmEmailHtml);


			ini.SetValue("Offsets", "PressOffset", Calib.Press.Offset);
			ini.SetValue("Offsets", "TempOffset", Calib.Temp.Offset);
			ini.SetValue("Offsets", "HumOffset", Calib.Hum.Offset);
			ini.SetValue("Offsets", "WindDirOffset", Calib.WindDir.Offset);
			ini.SetValue("Offsets", "UVOffset", Calib.UV.Offset);
			ini.SetValue("Offsets", "SolarOffset", Calib.Solar.Offset);
			ini.SetValue("Offsets", "WetBulbOffset", Calib.WetBulb.Offset);
			//ini.SetValue("Offsets", "DavisCalcAltPressOffset", DavisCalcAltPressOffset);
			ini.SetValue("Offsets", "InTempOffset", Calib.InTemp.Offset);
			ini.SetValue("Offsets", "InHumOffset", Calib.InHum.Offset);

			ini.SetValue("Offsets", "PressMult", Calib.Press.Mult);
			ini.SetValue("Offsets", "WindSpeedMult", Calib.WindSpeed.Mult);
			ini.SetValue("Offsets", "WindGustMult", Calib.WindGust.Mult);
			ini.SetValue("Offsets", "TempMult", Calib.Temp.Mult);
			ini.SetValue("Offsets", "TempMult2", Calib.Temp.Mult2);
			ini.SetValue("Offsets", "HumMult", Calib.Hum.Mult);
			ini.SetValue("Offsets", "HumMult2", Calib.Hum.Mult2);
			ini.SetValue("Offsets", "RainMult", Calib.Rain.Mult);
			ini.SetValue("Offsets", "SolarMult", Calib.Solar.Mult);
			ini.SetValue("Offsets", "UVMult", Calib.UV.Mult);
			ini.SetValue("Offsets", "WetBulbMult", Calib.WetBulb.Mult);
			ini.SetValue("Offsets", "InTempMult", Calib.InTemp.Mult);
			ini.SetValue("Offsets", "InHumMult", Calib.InHum.Mult);

			ini.SetValue("Limits", "TempHighC", Limit.TempHigh);
			ini.SetValue("Limits", "TempLowC", Limit.TempLow);
			ini.SetValue("Limits", "DewHighC", Limit.DewHigh);
			ini.SetValue("Limits", "PressHighMB", Limit.PressHigh);
			ini.SetValue("Limits", "PressLowMB", Limit.PressLow);
			ini.SetValue("Limits", "WindHighMS", Limit.WindHigh);

			ini.SetValue("xAP", "Enabled", xapEnabled);
			ini.SetValue("xAP", "UID", xapUID);
			ini.SetValue("xAP", "Port", xapPort);

			ini.SetValue("Solar", "SunThreshold", SolarOptions.SunThreshold);
			ini.SetValue("Solar", "SolarMinimum", SolarOptions.SolarMinimum);
			ini.SetValue("Solar", "UseBlakeLarsen", SolarOptions.UseBlakeLarsen);
			ini.SetValue("Solar", "SolarCalc", SolarOptions.SolarCalc);
			ini.SetValue("Solar", "LuxToWM2", SolarOptions.LuxToWM2);
			ini.SetValue("Solar", "RStransfactorJun", SolarOptions.RStransfactorJun);
			ini.SetValue("Solar", "RStransfactorDec", SolarOptions.RStransfactorDec);
			ini.SetValue("Solar", "BrasTurbidityJun", SolarOptions.BrasTurbidityJun);
			ini.SetValue("Solar", "BrasTurbidityDec", SolarOptions.BrasTurbidityDec);

			ini.SetValue("NOAA", "Name", NOAAconf.Name);
			ini.SetValue("NOAA", "City", NOAAconf.City);
			ini.SetValue("NOAA", "State", NOAAconf.State);
			ini.SetValue("NOAA", "12hourformat", NOAAconf.Use12hour);
			ini.SetValue("NOAA", "HeatingThreshold", NOAAconf.HeatThreshold);
			ini.SetValue("NOAA", "CoolingThreshold", NOAAconf.CoolThreshold);
			ini.SetValue("NOAA", "MaxTempComp1", NOAAconf.MaxTempComp1);
			ini.SetValue("NOAA", "MaxTempComp2", NOAAconf.MaxTempComp2);
			ini.SetValue("NOAA", "MinTempComp1", NOAAconf.MinTempComp1);
			ini.SetValue("NOAA", "MinTempComp2", NOAAconf.MinTempComp2);
			ini.SetValue("NOAA", "RainComp1", NOAAconf.RainComp1);
			ini.SetValue("NOAA", "RainComp2", NOAAconf.RainComp2);
			ini.SetValue("NOAA", "RainComp3", NOAAconf.RainComp3);
			ini.SetValue("NOAA", "AutoSave", NOAAconf.Create);
			ini.SetValue("NOAA", "AutoFTP", NOAAconf.AutoFtp);
			ini.SetValue("NOAA", "FTPDirectory", NOAAconf.FtpFolder);
			ini.SetValue("NOAA", "AutoCopy", NOAAconf.AutoCopy);
			ini.SetValue("NOAA", "CopyDirectory", NOAAconf.CopyFolder);
			ini.SetValue("NOAA", "MonthFileFormat", NOAAconf.MonthFile);
			ini.SetValue("NOAA", "YearFileFormat", NOAAconf.YearFile);
			ini.SetValue("NOAA", "NOAAUseUTF8", NOAAconf.UseUtf8);
			ini.SetValue("NOAA", "UseDotDecimal", NOAAconf.UseDotDecimal);
			ini.SetValue("NOAA", "UseMinMaxAvg", NOAAconf.UseMinMaxAvg);

			ini.SetValue("NOAA", "NOAATempNormJan", NOAAconf.TempNorms[1]);
			ini.SetValue("NOAA", "NOAATempNormFeb", NOAAconf.TempNorms[2]);
			ini.SetValue("NOAA", "NOAATempNormMar", NOAAconf.TempNorms[3]);
			ini.SetValue("NOAA", "NOAATempNormApr", NOAAconf.TempNorms[4]);
			ini.SetValue("NOAA", "NOAATempNormMay", NOAAconf.TempNorms[5]);
			ini.SetValue("NOAA", "NOAATempNormJun", NOAAconf.TempNorms[6]);
			ini.SetValue("NOAA", "NOAATempNormJul", NOAAconf.TempNorms[7]);
			ini.SetValue("NOAA", "NOAATempNormAug", NOAAconf.TempNorms[8]);
			ini.SetValue("NOAA", "NOAATempNormSep", NOAAconf.TempNorms[9]);
			ini.SetValue("NOAA", "NOAATempNormOct", NOAAconf.TempNorms[10]);
			ini.SetValue("NOAA", "NOAATempNormNov", NOAAconf.TempNorms[11]);
			ini.SetValue("NOAA", "NOAATempNormDec", NOAAconf.TempNorms[12]);

			ini.SetValue("NOAA", "NOAARainNormJan", NOAAconf.RainNorms[1]);
			ini.SetValue("NOAA", "NOAARainNormFeb", NOAAconf.RainNorms[2]);
			ini.SetValue("NOAA", "NOAARainNormMar", NOAAconf.RainNorms[3]);
			ini.SetValue("NOAA", "NOAARainNormApr", NOAAconf.RainNorms[4]);
			ini.SetValue("NOAA", "NOAARainNormMay", NOAAconf.RainNorms[5]);
			ini.SetValue("NOAA", "NOAARainNormJun", NOAAconf.RainNorms[6]);
			ini.SetValue("NOAA", "NOAARainNormJul", NOAAconf.RainNorms[7]);
			ini.SetValue("NOAA", "NOAARainNormAug", NOAAconf.RainNorms[8]);
			ini.SetValue("NOAA", "NOAARainNormSep", NOAAconf.RainNorms[9]);
			ini.SetValue("NOAA", "NOAARainNormOct", NOAAconf.RainNorms[10]);
			ini.SetValue("NOAA", "NOAARainNormNov", NOAAconf.RainNorms[11]);
			ini.SetValue("NOAA", "NOAARainNormDec", NOAAconf.RainNorms[12]);


			ini.SetValue("Proxies", "HTTPProxyName", HTTPProxyName);
			ini.SetValue("Proxies", "HTTPProxyPort", HTTPProxyPort);
			ini.SetValue("Proxies", "HTTPProxyUser", Crypto.EncryptString(HTTPProxyUser, Program.InstanceId, "HTTPProxyUser"));
			ini.SetValue("Proxies", "HTTPProxyPassword", Crypto.EncryptString(HTTPProxyPassword, Program.InstanceId, "HTTPProxyPassword"));

			ini.SetValue("Display", "NumWindRosePoints", NumWindRosePoints);
			ini.SetValue("Display", "UseApparent", DisplayOptions.UseApparent);
			ini.SetValue("Display", "DisplaySolarData", DisplayOptions.ShowSolar);
			ini.SetValue("Display", "DisplayUvData", DisplayOptions.ShowUV);

			ini.SetValue("Graphs", "ChartMaxDays", GraphDays);
			ini.SetValue("Graphs", "GraphHours", GraphHours);
			ini.SetValue("Graphs", "MoonImageEnabled", MoonImage.Enabled);
			ini.SetValue("Graphs", "MoonImageSize", MoonImage.Size);
			ini.SetValue("Graphs", "MoonImageShadeTransparent", MoonImage.Transparent);
			ini.SetValue("Graphs", "MoonImageFtpDest", MoonImage.FtpDest);
			ini.SetValue("Graphs", "MoonImageCopyDest", MoonImage.CopyDest);
			ini.SetValue("Graphs", "TempVisible", GraphOptions.TempVisible);
			ini.SetValue("Graphs", "InTempVisible", GraphOptions.InTempVisible);
			ini.SetValue("Graphs", "HIVisible", GraphOptions.HIVisible);
			ini.SetValue("Graphs", "DPVisible", GraphOptions.DPVisible);
			ini.SetValue("Graphs", "WCVisible", GraphOptions.WCVisible);
			ini.SetValue("Graphs", "AppTempVisible", GraphOptions.AppTempVisible);
			ini.SetValue("Graphs", "FeelsLikeVisible", GraphOptions.FeelsLikeVisible);
			ini.SetValue("Graphs", "HumidexVisible", GraphOptions.HumidexVisible);
			ini.SetValue("Graphs", "InHumVisible", GraphOptions.InHumVisible);
			ini.SetValue("Graphs", "OutHumVisible", GraphOptions.OutHumVisible);
			ini.SetValue("Graphs", "UVVisible", GraphOptions.UVVisible);
			ini.SetValue("Graphs", "SolarVisible", GraphOptions.SolarVisible);
			ini.SetValue("Graphs", "SunshineVisible", GraphOptions.SunshineVisible);
			ini.SetValue("Graphs", "DailyAvgTempVisible", GraphOptions.DailyAvgTempVisible);
			ini.SetValue("Graphs", "DailyMaxTempVisible", GraphOptions.DailyMaxTempVisible);
			ini.SetValue("Graphs", "DailyMinTempVisible", GraphOptions.DailyMinTempVisible);
			ini.SetValue("Graphs", "GrowingDegreeDaysVisible1", GraphOptions.GrowingDegreeDaysVisible1);
			ini.SetValue("Graphs", "GrowingDegreeDaysVisible2", GraphOptions.GrowingDegreeDaysVisible2);
			ini.SetValue("Graphs", "TempSumVisible0", GraphOptions.TempSumVisible0);
			ini.SetValue("Graphs", "TempSumVisible1", GraphOptions.TempSumVisible1);
			ini.SetValue("Graphs", "TempSumVisible2", GraphOptions.TempSumVisible2);
			ini.SetValue("Graphs", "ExtraTempVisible", GraphOptions.ExtraTempVisible);
			ini.SetValue("Graphs", "ExtraHumVisible", GraphOptions.ExtraHumVisible);
			ini.SetValue("Graphs", "SoilTempVisible", GraphOptions.SoilTempVisible);
			ini.SetValue("Graphs", "SoilMoistVisible", GraphOptions.SoilMoistVisible);
			ini.SetValue("Graphs", "UserTempVisible", GraphOptions.UserTempVisible);


			ini.SetValue("MySQL", "Host", MySqlStuff.ConnSettings.Server);
			ini.SetValue("MySQL", "Port", (int) MySqlStuff.ConnSettings.Port);
			ini.SetValue("MySQL", "User", Crypto.EncryptString(MySqlStuff.ConnSettings.UserID, Program.InstanceId, "MySql UserID"));
			ini.SetValue("MySQL", "Pass", Crypto.EncryptString(MySqlStuff.ConnSettings.Password, Program.InstanceId, "MySql Password"));
			ini.SetValue("MySQL", "Database", MySqlStuff.ConnSettings.Database);
			ini.SetValue("MySQL", "MonthlyMySqlEnabled", MySqlStuff.Settings.Monthly.Enabled);
			ini.SetValue("MySQL", "RealtimeMySqlEnabled", MySqlStuff.Settings.Realtime.Enabled);
			ini.SetValue("MySQL", "RealtimeMySql1MinLimit", MySqlStuff.Settings.RealtimeLimit1Minute);
			ini.SetValue("MySQL", "DayfileMySqlEnabled", MySqlStuff.Settings.Dayfile.Enabled);
			ini.SetValue("MySQL", "UpdateOnEdit", MySqlStuff.Settings.UpdateOnEdit);
			ini.SetValue("MySQL", "BufferOnFailure", MySqlStuff.Settings.BufferOnfailure);


			ini.SetValue("MySQL", "MonthlyTable", MySqlStuff.Settings.Monthly.TableName);
			ini.SetValue("MySQL", "DayfileTable", MySqlStuff.Settings.Dayfile.TableName);
			ini.SetValue("MySQL", "RealtimeTable", MySqlStuff.Settings.Realtime.TableName);
			ini.SetValue("MySQL", "RealtimeRetention", MySqlStuff.Settings.RealtimeRetention);

			ini.SetValue("MySQL", "CustomMySqlSecondsEnabled", MySqlStuff.Settings.CustomSecs.Enabled);
			ini.SetValue("MySQL", "CustomMySqlMinutesEnabled", MySqlStuff.Settings.CustomMins.Enabled);
			ini.SetValue("MySQL", "CustomMySqlRolloverEnabled", MySqlStuff.Settings.CustomRollover.Enabled);

			ini.SetValue("MySQL", "CustomMySqlSecondsInterval", MySqlStuff.Settings.CustomSecs.Interval);
			ini.SetValue("MySQL", "CustomMySqlMinutesIntervalIndex", MySqlStuff.CustomMinutesIntervalIndex);

			ini.SetValue("MySQL", "CustomMySqlSecondsCommandString", MySqlStuff.Settings.CustomSecs.Commands[0]);
			ini.SetValue("MySQL", "CustomMySqlMinutesCommandString", MySqlStuff.Settings.CustomMins.Commands[0]);
			ini.SetValue("MySQL", "CustomMySqlRolloverCommandString", MySqlStuff.Settings.CustomRollover.Commands[0]);


			for (var i = 1; i < 10; i++)
			{
				if (string.IsNullOrEmpty(MySqlStuff.Settings.CustomSecs.Commands[i]))
					ini.DeleteValue("MySQL", "CustomMySqlSecondsCommandString" + i);
				else
					ini.SetValue("MySQL", "CustomMySqlSecondsCommandString" + i, MySqlStuff.Settings.CustomSecs.Commands[i]);

				if (string.IsNullOrEmpty(MySqlStuff.Settings.CustomMins.Commands[i]))
					ini.DeleteValue("MySQL", "CustomMySqlMinutesCommandString" + i);
				else
					ini.SetValue("MySQL", "CustomMySqlMinutesCommandString" + i, MySqlStuff.Settings.CustomMins.Commands[i]);

				if (string.IsNullOrEmpty(MySqlStuff.Settings.CustomRollover.Commands[i]))
					ini.DeleteValue("MySQL", "CustomMySqlRolloverCommandString" + i);
				else
					ini.SetValue("MySQL", "CustomMySqlRolloverCommandString" + i, MySqlStuff.Settings.CustomRollover.Commands[i]);
			}

			ini.SetValue("HTTP", "CustomHttpSecondsString", CustomHttpSecondsStrings[0]);
			ini.SetValue("HTTP", "CustomHttpMinutesString", CustomHttpMinutesStrings[0]);
			ini.SetValue("HTTP", "CustomHttpRolloverString", CustomHttpRolloverStrings[0]);

			for (var i = 1; i < 10; i++)
			{
				if (string.IsNullOrEmpty(CustomHttpSecondsStrings[i]))
					ini.DeleteValue("HTTP", "CustomHttpSecondsString" + i);
				else
					ini.SetValue("HTTP", "CustomHttpSecondsString" + i, CustomHttpSecondsStrings[i]);

				if (string.IsNullOrEmpty(CustomHttpMinutesStrings[i]))
					ini.DeleteValue("HTTP", "CustomHttpMinutesString" + i);
				else
					ini.SetValue("HTTP", "CustomHttpMinutesString" + i, CustomHttpMinutesStrings[i]);

				if (string.IsNullOrEmpty(CustomHttpRolloverStrings[i]))
					ini.DeleteValue("HTTP", "CustomHttpRolloverString" + i);
				else
					ini.SetValue("HTTP", "CustomHttpRolloverString" + i, CustomHttpRolloverStrings[i]);
			}

			ini.SetValue("HTTP", "CustomHttpSecondsEnabled", CustomHttpSecondsEnabled);
			ini.SetValue("HTTP", "CustomHttpMinutesEnabled", CustomHttpMinutesEnabled);
			ini.SetValue("HTTP", "CustomHttpRolloverEnabled", CustomHttpRolloverEnabled);

			ini.SetValue("HTTP", "CustomHttpSecondsInterval", CustomHttpSecondsInterval);
			ini.SetValue("HTTP", "CustomHttpMinutesIntervalIndex", CustomHttpMinutesIntervalIndex);

			for (int i = 0; i < SelectaChartOptions.series.Length; i++)
			{
				ini.SetValue("Select-a-Chart", "Series" + i, SelectaChartOptions.series[i]);
				ini.SetValue("Select-a-Chart", "Colour" + i, SelectaChartOptions.colours[i]);
			}

			// Email settings
			ini.SetValue("SMTP", "Enabled", SmtpOptions.Enabled);
			ini.SetValue("SMTP", "ServerName", SmtpOptions.Server);
			ini.SetValue("SMTP", "Port", SmtpOptions.Port);
			ini.SetValue("SMTP", "SSLOption", SmtpOptions.SslOption);
			ini.SetValue("SMTP", "RequiresAuthentication", SmtpOptions.RequiresAuthentication);
			ini.SetValue("SMTP", "User", Crypto.EncryptString(SmtpOptions.User, Program.InstanceId, "SmtpOptions.User"));
			ini.SetValue("SMTP", "Password", Crypto.EncryptString(SmtpOptions.Password, Program.InstanceId, "SmtpOptions.Password"));
			ini.SetValue("SMTP", "Logging", SmtpOptions.Logging);
			ini.SetValue("SMTP", "IgnoreCertErrors", SmtpOptions.IgnoreCertErrors);

			// Growing Degree Days
			ini.SetValue("GrowingDD", "BaseTemperature1", GrowingBase1);
			ini.SetValue("GrowingDD", "BaseTemperature2", GrowingBase2);
			ini.SetValue("GrowingDD", "YearStarts", GrowingYearStarts);
			ini.SetValue("GrowingDD", "Cap30C", GrowingCap30C);

			// Temperature Sum
			ini.SetValue("TempSum", "TempSumYearStart", TempSumYearStarts);
			ini.SetValue("TempSum", "BaseTemperature1", TempSumBase1);
			ini.SetValue("TempSum", "BaseTemperature2", TempSumBase2);

			// Additional sensor logging
			ini.SetValue("ExtraDataLogging", "Temperature", ExtraDataLogging.Temperature);
			ini.SetValue("ExtraDataLogging", "Humidity", ExtraDataLogging.Humidity);
			ini.SetValue("ExtraDataLogging", "Dewpoint", ExtraDataLogging.Dewpoint);
			ini.SetValue("ExtraDataLogging", "UserTemp", ExtraDataLogging.UserTemp);
			ini.SetValue("ExtraDataLogging", "SoilTemp", ExtraDataLogging.SoilTemp);
			ini.SetValue("ExtraDataLogging", "SoilMoisture", ExtraDataLogging.SoilMoisture);
			ini.SetValue("ExtraDataLogging", "LeafTemp", ExtraDataLogging.LeafTemp);
			ini.SetValue("ExtraDataLogging", "LeafWetness", ExtraDataLogging.LeafWetness);
			ini.SetValue("ExtraDataLogging", "AirQual", ExtraDataLogging.AirQual);
			ini.SetValue("ExtraDataLogging", "CO2", ExtraDataLogging.CO2);

			// Custom Daily Log Settings
			for (var i = 0; i < 10; i++)
			{
				if (string.IsNullOrEmpty(CustomDailyLogSettings[i].FileName) && string.IsNullOrEmpty(CustomDailyLogSettings[i].ContentString))
				{
					ini.DeleteValue("CustomLogs", "DailyEnabled" + i);
					ini.DeleteValue("CustomLogs", "DailyFilename" + i);
					ini.DeleteValue("CustomLogs", "DailyContent" + i);
				}
				else
				{
					ini.SetValue("CustomLogs", "DailyEnabled" + i, CustomDailyLogSettings[i].Enabled);
					ini.SetValue("CustomLogs", "DailyFilename" + i, CustomDailyLogSettings[i].FileName);
					ini.SetValue("CustomLogs", "DailyContent" + i, CustomDailyLogSettings[i].ContentString);
				}
			}

			// Custom Interval Log Settings
			for (var i = 0; i < 10; i++)
			{
				if (string.IsNullOrEmpty(CustomIntvlLogSettings[i].FileName) && string.IsNullOrEmpty(CustomIntvlLogSettings[i].ContentString))
				{
					ini.DeleteValue("CustomLogs", "IntervalEnabled" + i);
					ini.DeleteValue("CustomLogs", "IntervalFilename" + i);
					ini.DeleteValue("CustomLogs", "IntervalContent" + i);
					ini.DeleteValue("CustomLogs", "IntervalIdx" + i);
				}
				else
				{
					ini.SetValue("CustomLogs", "IntervalEnabled" + i, CustomIntvlLogSettings[i].Enabled);
					ini.SetValue("CustomLogs", "IntervalFilename" + i, CustomIntvlLogSettings[i].FileName);
					ini.SetValue("CustomLogs", "IntervalContent" + i, CustomIntvlLogSettings[i].ContentString);
					ini.SetValue("CustomLogs", "IntervalIdx" + i, CustomIntvlLogSettings[i].IntervalIdx);
				}
			}

			ini.Flush();

			LogMessage("Completed writing Cumulus.ini file");
		}

		private void ReadStringsFile()
		{
			IniFile ini = new IniFile("strings.ini");

			// forecast

			ForecastNotAvailable = ini.GetValue("Forecast", "notavailable", ForecastNotAvailable);

			exceptional = ini.GetValue("Forecast", "exceptional", exceptional);
			for (var i = 0; i <= 25; i++)
			{
				zForecast[i] = ini.GetValue("Forecast", "forecast" + (i + 1), zForecast[i]);
			}
			// moon phases
			NewMoon = ini.GetValue("MoonPhases", "Newmoon", NewMoon);
			WaxingCrescent = ini.GetValue("MoonPhases", "WaxingCrescent", WaxingCrescent);
			FirstQuarter = ini.GetValue("MoonPhases", "FirstQuarter", FirstQuarter);
			WaxingGibbous = ini.GetValue("MoonPhases", "WaxingGibbous", WaxingGibbous);
			FullMoon = ini.GetValue("MoonPhases", "Fullmoon", FullMoon);
			WaningGibbous = ini.GetValue("MoonPhases", "WaningGibbous", WaningGibbous);
			LastQuarter = ini.GetValue("MoonPhases", "LastQuarter", LastQuarter);
			WaningCrescent = ini.GetValue("MoonPhases", "WaningCrescent", WaningCrescent);
			// Beaufort
			Calm = ini.GetValue("Beaufort", "Calm", Calm);
			Lightair = ini.GetValue("Beaufort", "Lightair", Lightair);
			Lightbreeze = ini.GetValue("Beaufort", "Lightbreeze", Lightbreeze);
			Gentlebreeze = ini.GetValue("Beaufort", "Gentlebreeze", Gentlebreeze);
			Moderatebreeze = ini.GetValue("Beaufort", "Moderatebreeze", Moderatebreeze);
			Freshbreeze = ini.GetValue("Beaufort", "Freshbreeze", Freshbreeze);
			Strongbreeze = ini.GetValue("Beaufort", "Strongbreeze", Strongbreeze);
			Neargale = ini.GetValue("Beaufort", "Neargale", Neargale);
			Gale = ini.GetValue("Beaufort", "Gale", Gale);
			Stronggale = ini.GetValue("Beaufort", "Stronggale", Stronggale);
			Storm = ini.GetValue("Beaufort", "Storm", Storm);
			Violentstorm = ini.GetValue("Beaufort", "Violentstorm", Violentstorm);
			Hurricane = ini.GetValue("Beaufort", "Hurricane", Hurricane);
			// trends
			Risingveryrapidly = ini.GetValue("Trends", "Risingveryrapidly", Risingveryrapidly);
			Risingquickly = ini.GetValue("Trends", "Risingquickly", Risingquickly);
			Rising = ini.GetValue("Trends", "Rising", Rising);
			Risingslowly = ini.GetValue("Trends", "Risingslowly", Risingslowly);
			Steady = ini.GetValue("Trends", "Steady", Steady);
			Fallingslowly = ini.GetValue("Trends", "Fallingslowly", Fallingslowly);
			Falling = ini.GetValue("Trends", "Falling", Falling);
			Fallingquickly = ini.GetValue("Trends", "Fallingquickly", Fallingquickly);
			Fallingveryrapidly = ini.GetValue("Trends", "Fallingveryrapidly", Fallingveryrapidly);
			// compass points
			compassp[0] = ini.GetValue("Compass", "N", compassp[0]);
			compassp[1] = ini.GetValue("Compass", "NNE", compassp[1]);
			compassp[2] = ini.GetValue("Compass", "NE", compassp[2]);
			compassp[3] = ini.GetValue("Compass", "ENE", compassp[3]);
			compassp[4] = ini.GetValue("Compass", "E", compassp[4]);
			compassp[5] = ini.GetValue("Compass", "ESE", compassp[5]);
			compassp[6] = ini.GetValue("Compass", "SE", compassp[6]);
			compassp[7] = ini.GetValue("Compass", "SSE", compassp[7]);
			compassp[8] = ini.GetValue("Compass", "S", compassp[8]);
			compassp[9] = ini.GetValue("Compass", "SSW", compassp[9]);
			compassp[10] = ini.GetValue("Compass", "SW", compassp[10]);
			compassp[11] = ini.GetValue("Compass", "WSW", compassp[11]);
			compassp[12] = ini.GetValue("Compass", "W", compassp[12]);
			compassp[13] = ini.GetValue("Compass", "WNW", compassp[13]);
			compassp[14] = ini.GetValue("Compass", "NW", compassp[14]);
			compassp[15] = ini.GetValue("Compass", "NNW", compassp[15]);

			for (var i = 1; i <= 4; i++)
			{
				// leaf temp captions (for Extra Sensor Data screen)
				LeafTempCaptions[i] = ini.GetValue("LeafTempCaptions", "Sensor" + i, LeafTempCaptions[i]);

				// air quality captions (for Extra Sensor Data screen)
				AirQualityCaptions[i] = ini.GetValue("AirQualityCaptions", "Sensor" + i, AirQualityCaptions[i]);
				AirQualityAvgCaptions[i] = ini.GetValue("AirQualityCaptions", "SensorAvg", AirQualityAvgCaptions[1]);
			}

			for (var i = 1; i <= 8; i++)
			{
				// leaf wetness captions (for Extra Sensor Data screen)
				LeafWetnessCaptions[i] = ini.GetValue("LeafWetnessCaptions", "Sensor" + i, LeafWetnessCaptions[i]);

				// User temperature captions (for Extra Sensor Data screen)
				UserTempCaptions[i] = ini.GetValue("UserTempCaptions", "Sensor" + i, UserTempCaptions[i]);
			}

			for (var i = 1; i <= 10; i++)
			{
				// Extra temperature captions (for Extra Sensor Data screen)
				ExtraTempCaptions[i] = ini.GetValue("ExtraTempCaptions", "Sensor" + i, ExtraTempCaptions[i]);

				// Extra humidity captions (for Extra Sensor Data screen)
				ExtraHumCaptions[i] = ini.GetValue("ExtraHumCaptions", "Sensor" + i, ExtraHumCaptions[i]);

				// Extra dew point captions (for Extra Sensor Data screen)
				ExtraDPCaptions[i] = ini.GetValue("ExtraDPCaptions", "Sensor" + i, ExtraDPCaptions[i]);
			}

			for (var i = 1; i <= 16; i++)
			{
				// soil temp captions (for Extra Sensor Data screen)
				SoilTempCaptions[i] = ini.GetValue("SoilTempCaptions", "Sensor" + i, SoilTempCaptions[i]);

				// soil moisture captions (for Extra Sensor Data screen)
				SoilMoistureCaptions[i] = ini.GetValue("SoilMoistureCaptions", "Sensor" + i, SoilMoistureCaptions[i]);
			}

			// CO2 captions - Ecowitt WH45 sensor
			CO2_CurrentCaption = ini.GetValue("CO2Captions", "CO2-Current", CO2_CurrentCaption);
			CO2_24HourCaption = ini.GetValue("CO2Captions", "CO2-24hr", CO2_24HourCaption);
			CO2_pm2p5Caption = ini.GetValue("CO2Captions", "CO2-Pm2p5", CO2_pm2p5Caption);
			CO2_pm2p5_24hrCaption = ini.GetValue("CO2Captions", "CO2-Pm2p5-24hr", CO2_pm2p5_24hrCaption);
			CO2_pm10Caption = ini.GetValue("CO2Captions", "CO2-Pm10", CO2_pm10Caption);
			CO2_pm10_24hrCaption = ini.GetValue("CO2Captions", "CO2-Pm10-24hr", CO2_pm10_24hrCaption);

			// User temperature captions (for Extra Sensor Data screen)
			UserTempCaptions[1] = ini.GetValue("UserTempCaptions", "Sensor1", UserTempCaptions[1]);
			UserTempCaptions[2] = ini.GetValue("UserTempCaptions", "Sensor2", UserTempCaptions[2]);
			UserTempCaptions[3] = ini.GetValue("UserTempCaptions", "Sensor3", UserTempCaptions[3]);
			UserTempCaptions[4] = ini.GetValue("UserTempCaptions", "Sensor4", UserTempCaptions[4]);
			UserTempCaptions[5] = ini.GetValue("UserTempCaptions", "Sensor5", UserTempCaptions[5]);
			UserTempCaptions[6] = ini.GetValue("UserTempCaptions", "Sensor6", UserTempCaptions[6]);
			UserTempCaptions[7] = ini.GetValue("UserTempCaptions", "Sensor7", UserTempCaptions[7]);
			UserTempCaptions[8] = ini.GetValue("UserTempCaptions", "Sensor8", UserTempCaptions[8]);

			thereWillBeMinSLessDaylightTomorrow = ini.GetValue("Solar", "LessDaylightTomorrow", thereWillBeMinSLessDaylightTomorrow);
			thereWillBeMinSMoreDaylightTomorrow = ini.GetValue("Solar", "MoreDaylightTomorrow", thereWillBeMinSMoreDaylightTomorrow);

			// Davis forecast 1
			DavisForecast1[0] = ini.GetValue("DavisForecast1", "forecast1", DavisForecast1[0]);
			for (var i = 1; i <= 25; i++)
			{
				DavisForecast1[i] = ini.GetValue("DavisForecast1", "forecast" + (i + 1), DavisForecast1[i]) + " ";
			}
			DavisForecast1[26] = ini.GetValue("DavisForecast1", "forecast27", DavisForecast1[26]);

			// Davis forecast 2
			DavisForecast2[0] = ini.GetValue("DavisForecast2", "forecast1", DavisForecast2[0]);
			for (var i = 1; i <= 18; i++)
			{
				DavisForecast2[i] = ini.GetValue("DavisForecast2", "forecast" + (i + 1), DavisForecast2[i]) + " ";
			}

			// Davis forecast 3
			for (var i = 0; i <= 6; i++)
			{
				DavisForecast3[i] = ini.GetValue("DavisForecast3", "forecast" + (i + 1), DavisForecast3[i]);
			}

			// alarm emails
			AlarmEmailSubject = ini.GetValue("AlarmEmails", "subject", "Cumulus MX Alarm");
			AlarmEmailPreamble = ini.GetValue("AlarmEmails", "preamble", "A Cumulus MX alarm has been triggered.");
			HighGustAlarm.EmailMsg = ini.GetValue("AlarmEmails", "windGustAbove", "A wind gust above {0} {1} has occurred.");
			HighPressAlarm.EmailMsg = ini.GetValue("AlarmEmails", "pressureAbove", "The pressure has risen above {0} {1}.");
			HighTempAlarm.EmailMsg = ini.GetValue("AlarmEmails", "tempAbove", "The temperature has risen above {0} {1}.");
			LowPressAlarm.EmailMsg = ini.GetValue("AlarmEmails", "pressBelow", "The pressure has fallen below {0} {1}.");
			LowTempAlarm.EmailMsg = ini.GetValue("AlarmEmails", "tempBelow", "The temperature has fallen below {0} {1}.");
			PressChangeAlarm.EmailMsgDn = ini.GetValue("AlarmEmails", "pressDown", "The pressure has decreased by more than {0} {1}.");
			PressChangeAlarm.EmailMsgUp = ini.GetValue("AlarmEmails", "pressUp", "The pressure has increased by more than {0} {1}.");
			HighRainTodayAlarm.EmailMsg = ini.GetValue("AlarmEmails", "rainAbove", "The rainfall today has exceeded {0} {1}.");
			HighRainRateAlarm.EmailMsg = ini.GetValue("AlarmEmails", "rainRateAbove", "The rainfall rate has exceeded {0} {1}.");
			SensorAlarm.EmailMsg = ini.GetValue("AlarmEmails", "sensorLost", "Contact has been lost with a remote sensor,");
			TempChangeAlarm.EmailMsgDn = ini.GetValue("AlarmEmails", "tempDown", "The temperature decreased by more than {0} {1}.");
			TempChangeAlarm.EmailMsgUp = ini.GetValue("AlarmEmails", "tempUp", "The temperature has increased by more than {0} {1}.");
			HighWindAlarm.EmailMsg = ini.GetValue("AlarmEmails", "windAbove", "The average wind speed has exceeded {0} {1}.");
			DataStoppedAlarm.EmailMsg = ini.GetValue("AlarmEmails", "dataStopped", "Cumulus has stopped receiving data from your weather station.");
			BatteryLowAlarm.EmailMsg = ini.GetValue("AlarmEmails", "batteryLow", "A low battery condition has been detected.");
			SpikeAlarm.EmailMsg = ini.GetValue("AlarmEmails", "dataSpike", "A data spike from your weather station has been suppressed.");
			UpgradeAlarm.EmailMsg = ini.GetValue("AlarmEmails", "upgrade", "An upgrade to Cumulus MX is now available.");
			HttpUploadAlarm.EmailMsg = ini.GetValue("AlarmEmails", "httpStopped", "HTTP uploads are failing.");
			MySqlUploadAlarm.EmailMsg = ini.GetValue("AlarmEmails", "mySqlStopped", "MySQL uploads are failing.");
			IsRainingAlarm.EmailMsg = ini.GetValue("AlarmEmails", "isRaining", "It has started to rain.");
		}



		public int xapPort { get; set; }

		public string xapUID { get; set; }

		public bool xapEnabled { get; set; }

		public bool CloudBaseInFeet { get; set; }

		public string WebcamURL { get; set; }

		public string ForumURL { get; set; }

		public string DailyParams { get; set; }

		public string RealtimeParams { get; set; }

		public string ExternalParams { get; set; }

		public string DailyProgram { get; set; }

		public string RealtimeProgram { get; set; }

		public string ExternalProgram { get; set; }

		public TExtraFiles[] ExtraFiles = new TExtraFiles[numextrafiles];

		//public int MaxFTPconnectRetries { get; set; }

		public bool DeleteBeforeUpload { get; set; }

		public bool FTPRename { get; set; }

		public int UpdateInterval { get; set; }

		public Timer RealtimeTimer = new Timer();

		public bool WebIntervalEnabled { get; set; }

		public bool WebAutoUpdate { get; set; }

		public int WMR200TempChannel { get; set; }

		public int WMR928TempChannel { get; set; }

		public int RTdisconnectcount { get; set; }

		//public int VP2SleepInterval { get; set; }

		//public int VPClosedownTime { get; set; }
		public string AirLinkInIPAddr { get; set; }
		public string AirLinkOutIPAddr { get; set; }

		public bool AirLinkInEnabled { get; set; }
		public bool AirLinkOutEnabled { get; set; }

		public Stations.EcowittSettings EcowittSettings = new Stations.EcowittSettings();

		public bool AmbientExtraEnabled { get; set; }
		public bool AmbientExtraUseSolar { get; set; }
		public bool AmbientExtraUseUv { get; set; }
		public bool AmbientExtraUseTempHum { get; set; }
		public bool AmbientExtraUseSoilTemp { get; set; }
		public bool AmbientExtraUseSoilMoist { get; set; }
		//public bool AmbientExtraUseLeafWet { get; set; }
		public bool AmbientExtraUseAQI { get; set; }
		public bool AmbientExtraUseCo2 { get; set; }
		public bool AmbientExtraUseLightning { get; set; }
		public bool AmbientExtraUseLeak { get; set; }

		//public bool solar_logging { get; set; }

		//public bool special_logging { get; set; }

		public bool RG11DTRmode2 { get; set; }

		public bool RG11IgnoreFirst2 { get; set; }

		public double RG11tipsize2 { get; set; }

		public bool RG11TBRmode2 { get; set; }

		public string RG11Port2 { get; set; }

		public bool RG11DTRmode { get; set; }

		public bool RG11IgnoreFirst { get; set; }

		public double RG11tipsize { get; set; }

		public bool RG11TBRmode { get; set; }

		public string RG11Port { get; set; }

		public bool RG11Enabled { get; set; }
		public bool RG11Enabled2 { get; set; }

		public double ChillHourThreshold { get; set; }

		public int ChillHourSeasonStart { get; set; }

		public int RainSeasonStart { get; set; }

		public double FCPressureThreshold { get; set; }

		public double FChighpress { get; set; }

		public double FClowpress { get; set; }

		public bool FCpressinMB { get; set; }

		public double RainDayThreshold { get; set; }

		public int SnowDepthHour { get; set; }

		//public bool UseWindChillCutoff { get; set; }

		public bool HourlyForecast { get; set; }

		public bool UseCumulusForecast { get; set; }

		public bool UseDataLogger { get; set; }

		public bool DavisConsoleHighGust { get; set; }

		public bool DavisCalcAltPress { get; set; }

		public bool DavisUseDLLBarCalData { get; set; }

		public int LCMaxWind { get; set; }

		//public bool EWduplicatecheck { get; set; }

		public string RecordsBeganStr { get; set; }

		public DateTime RecordsBeganDate;

		//public bool EWdisablecheckinit { get; set; }

		//public bool EWallowFF { get; set; }

		public int YTDrainyear { get; set; }

		public double YTDrain { get; set; }

		public string LocationDesc { get; set; }

		public string LocationName { get; set; }

		public string HTTPProxyPassword { get; set; }

		public string HTTPProxyUser { get; set; }

		public int HTTPProxyPort { get; set; }

		public string HTTPProxyName { get; set; }

		public int[] WindDPlaceDefaults = { 1, 0, 0, 0 }; // m/s, mph, km/h, knots
		public int[] TempDPlaceDefaults = { 1, 1 };
		public int[] PressDPlaceDefaults = { 1, 1, 2 };
		public int[] RainDPlaceDefaults = { 1, 2 };
		public const int numextrafiles = 99;
		public const int numOfSelectaChartSeries = 6;

		//public bool WS2300Sync { get; set; }

		public bool ErrorLogSpikeRemoval { get; set; }

		//public bool NoFlashWetDryDayRecords { get; set; }

		public bool ReportLostSensorContact { get; set; }

		public bool ReportDataStoppedErrors { get; set; }

		//public bool RestartIfDataStops { get; set; }

		//public bool RestartIfUnplugged { get; set; }

		//public bool CloseOnSuspend { get; set; }

		//public bool ConfirmClose { get; set; }

		public int DataLogInterval { get; set; }

		public int UVdecimals { get; set; }

		public string LonTxt { get; set; }

		public string LatTxt { get; set; }

		public bool AltitudeInFeet { get; set; }

		public string StationModel { get; set; }

		public int StationType { get; set; }

		public string LatestImetReading { get; set; }

		public bool FineOffsetStation { get; set; }

		public bool DavisStation { get; set; }
		public string TempTrendFormat { get; set; }
		public string AppDir { get; set; }

		public int Manufacturer { get; set; }
		public int ImetLoggerInterval { get; set; }
		public TimeSpan DayLength { get; set; }
		public DateTime Dawn;
		public DateTime Dusk;
		public TimeSpan DaylightLength { get; set; }
		public int GraphHours { get; set; }

		// WeatherLink Live transmitter Ids and indexes
		public string WllApiKey;
		public string WllApiSecret;
		public int WllStationId;
		public int WllParentId;

		/// <value>Read-only setting, default 20 minutes (1200 sec)</value>
		public int WllBroadcastDuration = 1200;
		/// <value>Read-only setting, default 22222</value>
		public int WllBroadcastPort = 22222;
		public bool WLLAutoUpdateIpAddress = true;
		public int WllPrimaryWind = 1;
		public int WllPrimaryTempHum = 1;
		public int WllPrimaryRain = 1;
		public int WllPrimarySolar;
		public int WllPrimaryUV;

		public int WllExtraSoilTempTx1;
		public int WllExtraSoilTempIdx1 = 1;
		public int WllExtraSoilTempTx2;
		public int WllExtraSoilTempIdx2 = 2;
		public int WllExtraSoilTempTx3;
		public int WllExtraSoilTempIdx3 = 3;
		public int WllExtraSoilTempTx4;
		public int WllExtraSoilTempIdx4 = 4;

		public int WllExtraSoilMoistureTx1;
		public int WllExtraSoilMoistureIdx1 = 1;
		public int WllExtraSoilMoistureTx2;
		public int WllExtraSoilMoistureIdx2 = 2;
		public int WllExtraSoilMoistureTx3;
		public int WllExtraSoilMoistureIdx3 = 3;
		public int WllExtraSoilMoistureTx4;
		public int WllExtraSoilMoistureIdx4 = 4;

		public int WllExtraLeafTx1;
		public int WllExtraLeafIdx1 = 1;
		public int WllExtraLeafTx2;
		public int WllExtraLeafIdx2 = 2;

		public int[] WllExtraTempTx = { 0, 0, 0, 0, 0, 0, 0, 0 };

		public bool[] WllExtraHumTx = { false, false, false, false, false, false, false, false };

		// WeatherLink Live transmitter Ids and indexes
		public bool AirLinkIsNode;
		public string AirLinkApiKey;
		public string AirLinkApiSecret;
		public int AirLinkInStationId;
		public int AirLinkOutStationId;
		public bool AirLinkAutoUpdateIpAddress = true;

		public int airQualityIndex = -1;

		public string Gw1000IpAddress;
		public string Gw1000MacAddress;
		public bool Gw1000AutoUpdateIpAddress = true;
		public int Gw1000PrimaryTHSensor;
		public int Gw1000PrimaryRainSensor;

		public Timer WebTimer = new Timer();
		public Timer MQTTTimer = new Timer();
		//public Timer AirLinkTimer = new Timer();

		public int DAVIS = 0;
		public int OREGON = 1;
		public int EW = 2;
		public int LACROSSE = 3;
		public int OREGONUSB = 4;
		public int INSTROMET = 5;
		public int ECOWITT = 6;
		public int HTTPSTATION = 7;
		public int AMBIENT = 8;
		public int WEATHERFLOW = 9;
		public int SIMULATOR = 10;

		//public bool startingup = true;
		public string ReportPath;
		public string LatestError;
		public DateTime LatestErrorTS = DateTime.MinValue;
		//public DateTime defaultRecordTS = new DateTime(2000, 1, 1, 0, 0, 0);
		public DateTime defaultRecordTS = DateTime.MinValue;
		public string WxnowFile = "wxnow.txt";
		private readonly string RealtimeFile = "realtime.txt";
		//private readonly string TwitterTxtFile;
		private FtpClient RealtimeFTP;
		private SftpClient RealtimeSSH;
		private volatile bool RealtimeFtpInProgress;
		private volatile bool RealtimeCopyInProgress;
		private volatile bool RealtimeFtpReconnecting;
		private byte RealtimeCycleCounter;

		public FileGenerationFtpOptions[] StdWebFiles;
		public FileGenerationFtpOptions[] RealtimeFiles;
		public FileGenerationFtpOptions[] GraphDataFiles;
		public FileGenerationFtpOptions[] GraphDataEodFiles;


		public string exceptional = "Exceptional Weather";
//		private WebSocketServer wsServer;
		public string[] ExtraTempCaptions = { "", "Sensor 1", "Sensor 2", "Sensor 3", "Sensor 4", "Sensor 5", "Sensor 6", "Sensor 7", "Sensor 8", "Sensor 9", "Sensor 10" };
		public string[] ExtraHumCaptions = { "", "Sensor 1", "Sensor 2", "Sensor 3", "Sensor 4", "Sensor 5", "Sensor 6", "Sensor 7", "Sensor 8", "Sensor 9", "Sensor 10" };
		public string[] ExtraDPCaptions = { "", "Sensor 1", "Sensor 2", "Sensor 3", "Sensor 4", "Sensor 5", "Sensor 6", "Sensor 7", "Sensor 8", "Sensor 9", "Sensor 10" };
		public string[] SoilTempCaptions = { "", "Sensor 1", "Sensor 2", "Sensor 3", "Sensor 4", "Sensor 5", "Sensor 6", "Sensor 7", "Sensor 8", "Sensor 9", "Sensor 10", "Sensor 11", "Sensor 12", "Sensor 13", "Sensor 14", "Sensor 15", "Sensor 16" };
		public string[] SoilMoistureCaptions = { "", "Sensor 1", "Sensor 2", "Sensor 3", "Sensor 4", "Sensor 5", "Sensor 6", "Sensor 7", "Sensor 8", "Sensor 9", "Sensor 10", "Sensor 11", "Sensor 12", "Sensor 13", "Sensor 14", "Sensor 15", "Sensor 16" };
		public string[] AirQualityCaptions = { "", "Sensor 1", "Sensor 2", "Sensor 3", "Sensor 4" };
		public string[] AirQualityAvgCaptions = { "", "Sensor Avg 1", "Sensor Avg 2", "Sensor Avg 3", "Sensor Avg 4" };
		public string[] LeafTempCaptions = { "", "Temp 1", "Temp 2", "Temp 3", "Temp 4" };
		public string[] LeafWetnessCaptions = { "", "Wetness 1", "Wetness 2", "Wetness 3", "Wetness 4", "Wetness 5", "Wetness 6", "Wetness 7", "Wetness 8" };
		public string[] UserTempCaptions = { "", "Sensor 1", "Sensor 2", "Sensor 3", "Sensor 4", "Sensor 5", "Sensor 6", "Sensor 7", "Sensor 8" };
		private string thereWillBeMinSLessDaylightTomorrow = "There will be {0}min {1}s less daylight tomorrow";
		private string thereWillBeMinSMoreDaylightTomorrow = "There will be {0}min {1}s more daylight tomorrow";
		// WH45 CO2 sensor captions
		public string CO2_CurrentCaption = "CO&#8322 Current";
		public string CO2_24HourCaption = "CO&#8322 24h avg";
		public string CO2_pm2p5Caption = "PM 2.5";
		public string CO2_pm2p5_24hrCaption = "PM 2.5 24h avg";
		public string CO2_pm10Caption = "PM 10";
		public string CO2_pm10_24hrCaption = "PM 10 24h avg";

		/*
		public string Getversion()
		{
			return Version;
		}

		public void SetComport(string comport)
		{
			ComportName = comport;
		}

		public string GetComport()
		{
			return ComportName;
		}

		public void SetStationType(int type)
		{
			StationType = type;
		}

		public int GetStationType()
		{
			return StationType;
		}

		public void SetVPRainGaugeType(int type)
		{
			VPrainGaugeType = type;
		}

		public int GetVPRainGaugeType()
		{
			return VPrainGaugeType;
		}

		public void SetVPConnectionType(VPConnTypes type)
		{
			VPconntype = type;
		}

		public VPConnTypes GetVPConnectionType()
		{
			return VPconntype;
		}

		public void SetIPaddress(string address)
		{
			IPaddress = address;
		}

		public string GetIPaddress()
		{
			return IPaddress;
		}

		public void SetTCPport(int port)
		{
			TCPport = port;
		}

		public int GetTCPport()
		{
			return TCPport;
		}
		*/

		public string GetLogFileName(DateTime thedate)
		{
			// First determine the date for the log file.
			// If we're using 9am roll-over, the date should be 9 hours (10 in summer)
			// before 'Now'
			DateTime logfiledate = thedate.AddHours(GetHourInc(thedate));

			var datestring = logfiledate.ToString("yyyy-MM");

			return Datapath + datestring + "-log.txt";
		}

		public string GetExtraLogFileName(DateTime thedate)
		{
			// First determine the date for the log file.
			// If we're using 9am roll-over, the date should be 9 hours (10 in summer)
			// before 'Now'
			DateTime logfiledate = thedate.AddHours(GetHourInc(thedate));

			var datestring = logfiledate.ToString("yyyy-MM");

			return Datapath + "Extra-" + datestring + "-log.txt";
		}

		public string GetAirLinkLogFileName(DateTime thedate)
		{
			// First determine the date for the log file.
			// If we're using 9am roll-over, the date should be 9 hours (10 in summer)
			// before 'Now'
			DateTime logfiledate = thedate.AddHours(GetHourInc(thedate));

			var datestring = logfiledate.ToString("yyyy-MM");

			return Datapath + "AirLink-" + datestring + "-log.txt";
		}

		public string GetCustomIntvlLogFileName(int idx, DateTime thedate)
		{
			// First determine the date for the log file.
			// If we're using 9am roll-over, the date should be 9 hours (10 in summer)
			// before 'Now'
			DateTime logfiledate = thedate.AddHours(GetHourInc(thedate));

			var datestring = logfiledate.ToString("yyyyMM");
			datestring = datestring.Replace(".", "");

			return Datapath + CustomIntvlLogSettings[idx].FileName + "-" + datestring + ".txt";
		}

		public string GetCustomDailyLogFileName(int idx)
		{
			return Datapath + CustomDailyLogSettings[idx].FileName + ".txt";
		}

		public const int NumLogFileFields = 29;

		public async Task DoLogFile(DateTime timestamp, bool live)
		{
			// Writes an entry to the n-minute log file. Fields are comma-separated:
			// 0  Date in the form dd/mm/yy hh:mm
			// 1  Current unix time stamp
			// 2  Current temperature
			// 3  Current humidity
			// 4  Current dewpoint
			// 5  Current wind speed
			// 6  Recent (10-minute) high gust
			// 7  Average wind bearing
			// 8  Current rainfall rate
			// 9  Total rainfall today so far
			// 10  Current sea level pressure
			// 11  Total rainfall counter as held by the station
			// 12  Inside temperature
			// 13  Inside humidity
			// 14  Current gust (i.e. 'Latest')
			// 15  Wind chill
			// 16  Heat Index
			// 17  UV Index
			// 18  Solar Radiation
			// 19  Evapotranspiration
			// 20  Annual Evapotranspiration
			// 21  Apparent temperature
			// 22  Current theoretical max solar radiation
			// 23  Hours of sunshine so far today
			// 24  Current wind bearing
			// 25  RG-11 rain total
			// 26  Rain since midnight
			// 27  Feels like
			// 28  Humidex

			// first round the timestamp to whole minutes - database times are recorded in "ticks", so for exact matches we need round numbers
			timestamp = Utils.RoundToNearestMinuteProper(timestamp, 1, Utils.RoundingDirection.Down);

			// make sure solar max is calculated for those stations without a solar sensor
			LogMessage("DoLogFile: Writing log entry for " + timestamp);
			LogDebugMessage("DoLogFile: max gust: " + (station.RecentMaxGust.HasValue ? station.RecentMaxGust.Value.ToString(WindFormat) : "null"));
			station.CurrentSolarMax = AstroLib.SolarMax(timestamp, Longitude, Latitude, WeatherStation.AltitudeM(Altitude), out station.SolarElevation, SolarOptions);

			if (StationOptions.LogMainStation)
			{
				var newRec = new IntervalData()
				{
					StationTime = timestamp,
					Temp = station.Temperature,
					Humidity = station.Humidity,
					DewPoint = station.Dewpoint,
					WindAvg = station.WindAverage,
					WindGust10m = station.RecentMaxGust,
					WindAvgDir = station.AvgBearing,
					RainRate = station.RainRate,
					RainToday = station.RainToday,
					Pressure = station.Pressure,
					RainCounter = station.Raincounter,
					InsideTemp = station.IndoorTemp,
					InsideHumidity = station.IndoorHum,
					WindLatest = station.WindLatest,
					WindChill = station.WindChill,
					HeatIndex = station.HeatIndex,
					UV = station.UV,
					SolarRad = station.SolarRad,
					ET = station.ET,
					AnnualET = station.AnnualETTotal,
					Apparent = station.ApparentTemp,
					SolarMax = station.CurrentSolarMax,
					Sunshine = station.SunshineHours,
					WindDir = station.Bearing,
					RG11Rain = station.RG11RainToday,
					RainMidnight = station.RainSinceMidnight,
					FeelsLike = station.FeelsLike,
					Humidex = station.Humidex
				};

				_ = station.Database.InsertOrReplace(newRec);


				var filename = GetLogFileName(timestamp);

				var csv = newRec.ToCSV(true);

				var success = false;
				var retries = LogFileRetries;
				do
				{
					try
					{
						using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read))
						using (StreamWriter file = new StreamWriter(fs))
						{
							await file.WriteLineAsync(csv);
							file.Close();
							fs.Close();
						}

						success = true;

						LastUpdateTime = timestamp;
						LogMessage($"DoLogFile: log entry for {timestamp} written");
					}
					catch (IOException ex)
					{
						if ((uint)ex.HResult == 0x80070020) // -2147024864
						{
							LogMessage("DoLogFile: Error log file is in use: " + ex.Message);
							retries--;
							Thread.Sleep(250);
						}
						else
						{
							LogExceptionMessage(ex, $"DoLogFile: Error writing log entry for {timestamp}");
							retries = 0;
						}
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"DoLogFile: Error writing entry for {timestamp}");
						retries = 0;
					}
				} while (!success && retries >= 0);
			}

			station.WriteTodayFile(timestamp, true);

			if (MySqlStuff.Settings.Monthly.Enabled)
			{
				MySqlStuff.DoIntervalData(timestamp, live);
			}
		}


		public async Task DoCustomIntervalLogs(DateTime timestamp)
		{
			for (var i = 0; i < 10; i++)
			{
				if (CustomIntvlLogSettings[i].Enabled && timestamp.Minute % CustomIntvlLogSettings[i].Interval == 0)
				{
					await DoCustomIntervalLog(i, timestamp);
				}
			}
		}

		private async Task DoCustomIntervalLog(int idx, DateTime timestamp)
		{
			// Writes a custom log file
			//TODO: Add database store

			// create the filename
			var filename = GetCustomIntvlLogFileName(idx, timestamp);

			LogDebugMessage($"DoCustomIntervalLog: {CustomIntvlLogSettings[idx].FileName} - Writing log entry for {timestamp}");

			// create the line to be appended
			var sb = new StringBuilder(256);

			sb.Append(timestamp.ToString("dd/MM/yy") + ListSeparator);
			sb.Append(timestamp.ToString("HH:mm") + ListSeparator);

			// process the webtags in the content string
			customLogIntvlTokenParser.InputText = CustomIntvlLogSettings[idx].ContentString;
			sb.Append(customLogIntvlTokenParser.ToStringFromString());

			LogDataMessage("DoCustomIntervalLog: entry: " + sb);

			var success = false;
			var retries = LogFileRetries;
			do
			{
				try
				{
					using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read))
					using (StreamWriter file = new StreamWriter(fs))
					{
						await file.WriteLineAsync(sb);
						file.Close();
						fs.Close();

						success = true;

						LogDebugMessage($"DoCustomIntervalLog: {CustomIntvlLogSettings[idx].FileName} - Log entry for {timestamp} written");
					}
				}
				catch (Exception ex)
				{
					LogDebugMessage($"DoCustomIntervalLog: {CustomIntvlLogSettings[idx].FileName} - Error writing log entry for {timestamp} - {ex.Message}");
					retries--;
					await Task.Delay(250);
				}
			} while (!success && retries >= 0);
		}

		public void DoCustomDailyLogs(DateTime timestamp)
		{
			for (var i = 0; i < 10; i++)
			{
				if (CustomDailyLogSettings[i].Enabled)
				{
					DoCustomDailyLog(i, timestamp);
				}
			}
		}

		private async void DoCustomDailyLog(int idx, DateTime timestamp)
		{
			LogDebugMessage($"DoCustomDailyLog: {CustomDailyLogSettings[idx].FileName} - Writing log entry");

			// create the filename
			var filename = GetCustomDailyLogFileName(idx);

			string datestring = timestamp.AddDays(-1).ToString("dd/MM/yy");
			// NB this string is just for logging, the dayfile update code is further down
			var sb = new StringBuilder(300);
			sb.Append(datestring + ListSeparator);

			// process the webtags in the content string
			customLogDailyTokenParser.InputText = CustomDailyLogSettings[idx].ContentString;
			sb.Append(customLogDailyTokenParser.ToStringFromString());

			LogDataMessage("DoCustomDailyLog: entry: " + sb);

			var success = false;
			var retries = LogFileRetries;
			do
			{
				try
				{
					using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read))
					using (StreamWriter file = new StreamWriter(fs))
					{
						file.WriteLine(sb);
						file.Close();
						fs.Close();

						success = true;

						LogDebugMessage($"DoCustomDailyLog: {CustomDailyLogSettings[idx].FileName} - Log entry written");
					}
				}
				catch (Exception ex)
				{
					LogDebugMessage($"DoCustomDailyLog: {CustomDailyLogSettings[idx].FileName} - Error writing log entry - {ex.Message}");
					retries--;
					await Task.Delay(250);
				}
			} while (!success && retries >= 0);
		}


		public const int NumExtraLogFileFields = 92;

		public async Task DoExtraLogFile(DateTime timestamp)
		{
			// Writes an entry to the n-minute extralogfile. Fields are comma-separated:
			// 0  Date/time  in the form dd/mm/yy hh:mm
			// 1  Current Unix timestamp
			// 2-11  Temperature 1-10
			// 12-21 Humidity 1-10
			// 22-31 Dew point 1-10
			// 32-35 Soil temp 1-4
			// 36-39 Soil moisture 1-4
			// 40-41 Leaf temp 1-2
			// 42-43 Leaf wetness 1-2
			// 44-55 Soil temp 5-16
			// 56-67 Soil moisture 5-16
			// 68-71 Air quality 1-4
			// 72-75 Air quality avg 1-4
			// 76-83 User temperature 1-8
			// 84  CO2
			// 85  CO2 avg
			// 86  CO2 pm2.5
			// 87  CO2 pm2.5 avg
			// 88  CO2 pm10
			// 89  CO2 pm10 avg
			// 90  CO2 temp
			// 91  CO2 hum


			// first round the timestamp to whole minutes - database times are recorded in "ticks", so for exact matches we need round numbers
			timestamp = Utils.RoundToNearestMinuteProper(timestamp, 1, Utils.RoundingDirection.Down);

			if (ExtraDataLogging.Temperature)
			{
				var newRec = new ExtraTemp()
				{
					Time = timestamp,
					Temp1 = station.ExtraTemp[1],
					Temp2 = station.ExtraTemp[2],
					Temp3 = station.ExtraTemp[3],
					Temp4 = station.ExtraTemp[4],
					Temp5 = station.ExtraTemp[5],
					Temp6 = station.ExtraTemp[6],
					Temp7 = station.ExtraTemp[7],
					Temp8 = station.ExtraTemp[8],
					Temp9 = station.ExtraTemp[9],
					Temp10 = station.ExtraTemp[10]
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.Humidity)
			{
				var newRec = new ExtraHum()
				{
					Time = timestamp,
					Hum1 = station.ExtraHum[1],
					Hum2 = station.ExtraHum[2],
					Hum3 = station.ExtraHum[3],
					Hum4 = station.ExtraHum[4],
					Hum5 = station.ExtraHum[5],
					Hum6 = station.ExtraHum[6],
					Hum7 = station.ExtraHum[7],
					Hum8 = station.ExtraHum[8],
					Hum9 = station.ExtraHum[9],
					Hum10 = station.ExtraHum[10]
				};
				_ =  station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.Dewpoint)
			{
				var newRec = new ExtraDewPoint()
				{
					Time = timestamp,
					DewPoint1 = station.ExtraDewPoint[1],
					DewPoint2 = station.ExtraDewPoint[2],
					DewPoint3 = station.ExtraDewPoint[3],
					DewPoint4 = station.ExtraDewPoint[4],
					DewPoint5 = station.ExtraDewPoint[5],
					DewPoint6 = station.ExtraDewPoint[6],
					DewPoint7 = station.ExtraDewPoint[7],
					DewPoint8 = station.ExtraDewPoint[8],
					DewPoint9 = station.ExtraDewPoint[9],
					DewPoint10 = station.ExtraDewPoint[10]
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.UserTemp)
			{
				var newRec = new UserTemp()
				{
					Time = timestamp,
					Temp1 = station.UserTemp[1],
					Temp2 = station.UserTemp[2],
					Temp3 = station.UserTemp[3],
					Temp4 = station.UserTemp[4],
					Temp5 = station.UserTemp[5],
					Temp6 = station.UserTemp[6],
					Temp7 = station.UserTemp[7],
					Temp8 = station.UserTemp[8],
					Temp9 = station.UserTemp[9],
					Temp10 = station.UserTemp[10]
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.SoilTemp)
			{
				var newRec = new SoilTemp()
				{
					Time = timestamp,
					Temp1 = station.SoilTemp[1],
					Temp2 = station.SoilTemp[2],
					Temp3 = station.SoilTemp[3],
					Temp4 = station.SoilTemp[4],
					Temp5 = station.SoilTemp[5],
					Temp6 = station.SoilTemp[6],
					Temp7 = station.SoilTemp[7],
					Temp8 = station.SoilTemp[8],
					Temp9 = station.SoilTemp[9],
					Temp10 = station.SoilTemp[10],
					Temp11 = station.SoilTemp[11],
					Temp12 = station.SoilTemp[12],
					Temp13 = station.SoilTemp[13],
					Temp14 = station.SoilTemp[14],
					Temp15 = station.SoilTemp[15],
					Temp16 = station.SoilTemp[16]
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.SoilMoisture)
			{
				var newRec = new SoilMoist()
				{
					Time = timestamp,
					Moist1 = station.SoilMoisture[1],
					Moist2 = station.SoilMoisture[2],
					Moist3 = station.SoilMoisture[3],
					Moist4 = station.SoilMoisture[4],
					Moist5 = station.SoilMoisture[5],
					Moist6 = station.SoilMoisture[6],
					Moist7 = station.SoilMoisture[7],
					Moist8 = station.SoilMoisture[8],
					Moist9 = station.SoilMoisture[9],
					Moist10 = station.SoilMoisture[10],
					Moist11 = station.SoilMoisture[11],
					Moist12 = station.SoilMoisture[12],
					Moist13 = station.SoilMoisture[13],
					Moist14 = station.SoilMoisture[14],
					Moist15 = station.SoilMoisture[15],
					Moist16 = station.SoilMoisture[16]
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.LeafTemp)
			{
				var newRec = new LeafTemp()
				{
					Time = timestamp,
					Temp1 = station.LeafTemp[1],
					Temp2 = station.LeafTemp[2],
					Temp3 = station.LeafTemp[3],
					Temp4 = station.LeafTemp[4]
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.LeafWetness)
			{
				var newRec = new LeafWet()
				{
					Time = timestamp,
					Wet1 = station.LeafWetness[1],
					Wet2 = station.LeafWetness[2],
					Wet3 = station.LeafWetness[3],
					Wet4 = station.LeafWetness[4],
					Wet5 = station.LeafWetness[5],
					Wet6 = station.LeafWetness[6],
					Wet7 = station.LeafWetness[7],
					Wet8 = station.LeafWetness[8]
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.AirQual)
			{
				var newRec = new AirQuality()
				{
					Time = timestamp,
					Aq1 = station.AirQuality[1],
					Aq2 = station.AirQuality[2],
					Aq3 = station.AirQuality[3],
					Aq4 = station.AirQuality[4],
					AqAvg1 = station.AirQualityAvg[1],
					AqAvg2 = station.AirQualityAvg[2],
					AqAvg3 = station.AirQualityAvg[3],
					AqAvg4 = station.AirQualityAvg[4],
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (ExtraDataLogging.CO2)
			{
				var newRec = new CO2Data()
				{
					Time = timestamp,
					CO2now = station.CO2,
					CO2avg = station.CO2_24h,
					Pm2p5 = station.CO2_pm2p5,
					Pm2p5avg = station.CO2_pm2p5_24h,
					Pm10 = station.CO2_pm10,
					Pm10avg = station.CO2_pm10_24h,
					Temp = station.CO2_temperature,
					Hum = station.CO2_humidity,
				};
				_ = station.Database.InsertOrReplace(newRec);
			}

			if (StationOptions.LogExtraSensors)
			{
				LogDebugMessage($"DoExtraLogFile: Writing log entry for {timestamp}");

				var filename = GetExtraLogFileName(timestamp);
				var sep = ",";

				var sb = new StringBuilder(512);
				sb.Append(timestamp.ToString("dd/MM/yy HH:mm", invDate) + sep);         //0
				sb.Append(Utils.ToUnixTime(timestamp) + sep);                        //1

				for (int i = 1; i < station.ExtraTemp.Length; i++)
				{
					if (station.ExtraTemp[i].HasValue)
						sb.Append(station.ExtraTemp[i].Value.ToString(TempFormat, invNum));      //2-11
					sb.Append(sep);
				}
				for (int i = 1; i < station.ExtraHum.Length; i++)
				{
					if (station.ExtraHum[i].HasValue)
						sb.Append(station.ExtraHum[i].Value.ToString(HumFormat, invNum));        //12-21
					sb.Append(sep);
				}
				for (int i = 1; i < station.ExtraDewPoint.Length; i++)
				{
					if (station.ExtraDewPoint[i].HasValue)
						sb.Append(station.ExtraDewPoint[i].Value.ToString(TempFormat, invNum));  //22-31
					sb.Append(sep);
				}
				for (int i = 1; i <= 4; i++)
				{
					if (station.SoilTemp[i].HasValue)
						sb.Append(station.SoilTemp[i].Value.ToString(TempFormat, invNum));     //32-35
					sb.Append(sep);
				}
				for (int i = 1; i <= 4; i++)
				{
					if (station.SoilMoisture[i].HasValue)
						sb.Append(station.SoilMoisture[i]);   //36-39
					sb.Append(sep);
				}

				sb.Append((station.LeafTemp[1].HasValue ? station.LeafTemp[1].Value.ToString(TempFormat, invNum) : "") + sep);		//40
				sb.Append((station.LeafTemp[2].HasValue ? station.LeafTemp[2].Value.ToString(TempFormat, invNum) : "") + sep);		//41

				sb.Append((station.LeafWetness[1].HasValue ? station.LeafWetness[1].Value.ToString(LeafWetFormat) : "") + sep);		//42
				sb.Append((station.LeafWetness[2].HasValue ? station.LeafWetness[2].Value.ToString(LeafWetFormat) : "") + sep);		//43

				for (int i = 5; i <= 16; i++)
				{
					if (station.SoilTemp[i].HasValue)
						sb.Append(station.SoilTemp[i].Value.ToString(TempFormat, invNum));     //44-55
					sb.Append(sep);
				}
				for (int i = 5; i <= 16; i++)
				{
					if (station.SoilMoisture[i].HasValue)
						sb.Append(station.SoilMoisture[i]);      //56-67
					sb.Append(sep);
				}
				for (int i = 1; i <= 4; i++)
				{
					if (station.AirQuality[i].HasValue)
						sb.Append(station.AirQuality[i].Value.ToString("F1", invNum));     //68-71
					sb.Append(sep);
				}
				for (int i = 1; i <= 4; i++)
				{
					if (station.AirQualityAvg[i].HasValue)
						sb.Append(station.AirQualityAvg[i].Value.ToString("F1", invNum));  //72-75
					sb.Append(sep);
				}

				for (int i = 1; i < 9; i++)
				{
					if (station.UserTemp[i].HasValue)
						sb.Append(station.UserTemp[i].Value.ToString(TempFormat, invNum));   //76-83
					sb.Append(sep);
				}

				sb.Append((station.CO2.HasValue ? station.CO2.Value : "") + sep);                                       //84
				sb.Append((station.CO2_24h.HasValue ? station.CO2_24h.Value : "") + sep);                               //85
				sb.Append((station.CO2_pm2p5.HasValue ? station.CO2_pm2p5.Value.ToString("F1", invNum) : "") + sep);    //86
				sb.Append((station.CO2_pm2p5_24h.HasValue ? station.CO2_pm2p5_24h.Value.ToString("F1", invNum) : "") + sep);    //87
				sb.Append((station.CO2_pm10.HasValue ? station.CO2_pm10.Value.ToString("F1", invNum) : "") + sep);              //88
				sb.Append((station.CO2_pm10_24h.HasValue ? station.CO2_pm10_24h.Value.ToString("F1", invNum) : "") + sep);      //89
				sb.Append((station.CO2_temperature.HasValue ? station.CO2_temperature.Value.ToString(TempFormat, invNum) : "") + sep);   //90
				sb.Append(station.CO2_humidity.HasValue ? station.CO2_humidity : "");                                                    //91

				var success = false;
				var retries = LogFileRetries;
				do
				{
					try
					{
						using FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
						using StreamWriter file = new StreamWriter(fs);
						await file.WriteLineAsync(sb.ToString());
						file.Close();
						fs.Close();

						success = true;

						LogDebugMessage($"DoExtraLogFile: Log entry for {timestamp} written");
					}
					catch (IOException ex)
					{
						if ((uint)ex.HResult == 0x80070020) // -2147024864
						{
							LogExceptionMessage(ex, "DoExtraLogFile: Error log file is in use");
							retries--;
							Thread.Sleep(250);
						}
						else
						{
							LogExceptionMessage(ex, $"DoExtraLogFile: Error writing log entry for {timestamp}");
							retries = 0;
						}
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"DoExtraLogFile: Error writing log entry {timestamp}");
						retries = 0;
					}
				} while (!success && retries >= 0);
			}
		}

		public async Task DoAirLinkLogFile(DateTime timestamp)
		{
			// Writes an entry to the n-minute airlinklogfile. Fields are comma-separated:
			// 0  Date in the form dd/mm/yy hh:mm
			// 1  Current Unix time stamp
			// 2  Indoor Temperature
			// 3  Indoor Humidity
			// 4  Indoor PM 1
			// 5  Indoor PM 2.5
			// 6  Indoor PM 2.5 1-hour
			// 7  Indoor PM 2.5 3-hour
			// 8  Indoor PM 2.5 24-hour
			// 9  Indoor PM 2.5 nowcast
			// 10 Indoor PM 10
			// 11 Indoor PM 10 1-hour
			// 12 Indoor PM 10 3-hour
			// 13 Indoor PM 10 24-hour
			// 14 Indoor PM 10 nowcast
			// 15 Indoor Percent received 1-hour
			// 16 Indoor Percent received 3-hour
			// 17 Indoor Percent received nowcast
			// 18 Indoor Percent received 24-hour
			// 19 Indoor AQI PM2.5
			// 20 Indoor AQI PM2.5 1-hour
			// 21 Indoor AQI PM2.5 3-hour
			// 22 Indoor AQI PM2.5 24-hour
			// 23 Indoor AQI PM2.5 nowcast
			// 24 Indoor AQI PM10
			// 25 Indoor AQI PM10 1-hour
			// 26 Indoor AQI PM10 3-hour
			// 27 Indoor AQI PM10 24-hour
			// 28 Indoor AQI PM10 nowcast
			// 29 Outdoor Temperature
			// 30 Outdoor Humidity
			// 31 Outdoor PM 1
			// 32 Outdoor PM 2.5
			// 33 Outdoor PM 2.5 1-hour
			// 34 Outdoor PM 2.5 3-hour
			// 35 Outdoor PM 2.5 24-hour
			// 36 Outdoor PM 2.5 nowcast
			// 37 Outdoor PM 10
			// 38 Outdoor PM 10 1-hour
			// 39 Outdoor PM 10 3-hour
			// 40 Outdoor PM 10 24-hour
			// 41 Outdoor PM 10 nowcast
			// 42 Outdoor Percent received 1-hour
			// 43 Outdoor Percent received 3-hour
			// 44 Outdoor Percent received nowcast
			// 45 Outdoor Percent received 24-hour
			// 46 Outdoor AQI PM2.5
			// 47 Outdoor AQI PM2.5 1-hour
			// 48 Outdoor AQI PM2.5 3-hour
			// 49 Outdoor AQI PM2.5 24-hour
			// 50 Outdoor AQI PM2.5 nowcast
			// 51 Outdoor AQI PM10
			// 52 Outdoor AQI PM10 1-hour
			// 53 Outdoor AQI PM10 3-hour
			// 54 Outdoor AQI PM10 24-hour
			// 55 Outdoor AQI PM10 nowcast

			// first round the timestamp to whole minutes - database times are recorded in "ticks", so for exact matches we need round numbers
			timestamp = Utils.RoundToNearestMinuteProper(timestamp, 1, Utils.RoundingDirection.Down);

			var filename = GetAirLinkLogFileName(timestamp);
			var sep = ",";

			LogDebugMessage($"DoAirLinkLogFile: Writing log entry for {timestamp}");

			var sb = new StringBuilder(256);

			sb.Append(timestamp.ToString("dd/MM/yy HH:mm", invDate) + sep);
			sb.Append(Utils.ToUnixTime(timestamp) + sep);

			if (AirLinkInEnabled && airLinkDataIn != null)
			{
				sb.Append(airLinkDataIn.temperature.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.humidity + sep);
				sb.Append(airLinkDataIn.pm1.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm2p5.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm2p5_1hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm2p5_3hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm2p5_24hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm2p5_nowcast.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm10.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm10_1hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm10_3hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm10_24hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pm10_nowcast.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataIn.pct_1hr + sep);
				sb.Append(airLinkDataIn.pct_3hr + sep);
				sb.Append(airLinkDataIn.pct_24hr + sep);
				sb.Append(airLinkDataIn.pct_nowcast + sep);
				if (AirQualityDPlaces > 0)
				{
					sb.Append(airLinkDataIn.aqiPm2p5.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm2p5_1hr.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm2p5_3hr.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm2p5_24hr.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm2p5_nowcast.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm10.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm10_1hr.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm10_3hr.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm10_24hr.ToString(AirQualityFormat, invNum) + sep);
					sb.Append(airLinkDataIn.aqiPm10_nowcast.ToString(AirQualityFormat, invNum) + sep);
				}
				else // Zero decimals - truncate value rather than round
				{
					sb.Append((int)airLinkDataIn.aqiPm2p5 + sep);
					sb.Append((int)airLinkDataIn.aqiPm2p5_1hr + sep);
					sb.Append((int)airLinkDataIn.aqiPm2p5_3hr + sep);
					sb.Append((int)airLinkDataIn.aqiPm2p5_24hr + sep);
					sb.Append((int)airLinkDataIn.aqiPm2p5_nowcast + sep);
					sb.Append((int)airLinkDataIn.aqiPm10 + sep);
					sb.Append((int)airLinkDataIn.aqiPm10_1hr + sep);
					sb.Append((int)airLinkDataIn.aqiPm10_3hr + sep);
					sb.Append((int)airLinkDataIn.aqiPm10_24hr + sep);
					sb.Append((int)airLinkDataIn.aqiPm10_nowcast + sep);
				}
			}
			else
			{
				// write zero values - subtract 2 for firmware version, WiFi RSSI
				for (var i = 0; i < typeof(AirLinkData).GetProperties().Length - 2; i++)
				{
					sb.Append("0" + sep);
				}
			}

			if (AirLinkOutEnabled && airLinkDataOut != null)
			{
				sb.Append(airLinkDataOut.temperature.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.humidity + sep);
				sb.Append(airLinkDataOut.pm1.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm2p5.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm2p5_1hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm2p5_3hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm2p5_24hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm2p5_nowcast.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm10.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm10_1hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm10_3hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm10_24hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pm10_nowcast.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.pct_1hr + sep);
				sb.Append(airLinkDataOut.pct_3hr + sep);
				sb.Append(airLinkDataOut.pct_24hr + sep);
				sb.Append(airLinkDataOut.pct_nowcast + sep);
				sb.Append(airLinkDataOut.aqiPm2p5.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm2p5_1hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm2p5_3hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm2p5_24hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm2p5_nowcast.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm10.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm10_1hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm10_3hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm10_24hr.ToString("F1", invNum) + sep);
				sb.Append(airLinkDataOut.aqiPm10_nowcast.ToString("F1", invNum));
			}
			else
			{
				// write zero values - subtract 2 for firmware version, WiFi RSSI - subtract 1 for end field
				for (var i = 0; i < typeof(AirLinkData).GetProperties().Length - 3; i++)
				{
					sb.Append("0" + sep);
				}
				sb.Append('0');
			}

			var success = false;
			var retries = LogFileRetries;
			do
			{
				try
				{
					using FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
					using StreamWriter file = new StreamWriter(fs);
					await file.WriteLineAsync(sb);
					file.Close();
					fs.Close();

					success = true;

					LogMessage($"DoAirLinkLogFile: Log entry for {timestamp} written");
				}
				catch (IOException ex)
				{
					if ((uint)ex.HResult == 0x80070020) // -2147024864
					{
						LogExceptionMessage(ex, "DoAirLinkLogFile: Error log file is in use");
						retries--;
						Thread.Sleep(250);
					}
					else
					{
						LogExceptionMessage(ex, $"DoAirLinkLogFile: Error writing log entry for {timestamp}");
						retries = 0;
					}
				}
				catch (Exception ex)
				{
					LogExceptionMessage(ex, $"DoAirLinkLogFile: Error writing log entry for {timestamp}");
					retries = 0;
				}
			} while (!success && retries >= 0);
		}

		public void BackupData(bool daily, DateTime timestamp)
		{
			LogMessage($"Starting {(daily ? "daily" : "start-up")} backup");
			string dirpath = daily ? backupPath + "daily" + DirectorySeparator : backupPath;

			if (!Directory.Exists(dirpath))
			{
				LogMessage("BackupData: *** Error - backup folder does not exist - " + dirpath);
				CreateRequiredFolders();
				if (!Directory.Exists(dirpath))
				{
					return;
				}
			}
			else
			{
				string[] dirs = Directory.GetDirectories(dirpath);
				Array.Sort(dirs);
				var dirlist = new List<string>(dirs);

				while (dirlist.Count > 10)
				{
					try
					{
						if (Path.GetFileName(dirlist[0]) == "daily")
						{
							LogMessage("BackupData: *** Error - the backup folder has unexpected contents");
							break;
						}
						else
						{
							Directory.Delete(dirlist[0], true);
							dirlist.RemoveAt(0);
						}
					}
					catch (UnauthorizedAccessException)
					{
						LogErrorMessage("BackupData: Error, no permission to read/delete folder: " + dirlist[0]);
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"BackupData: Error while attempting to read/delete folder: {dirlist[0]}, error message");
					}

				}

				string foldername = daily ? timestamp.ToString("yyyyMMdd") : timestamp.ToString("yyyyMMddHHmmss");

				string folderpath = dirpath + foldername + DirectorySeparator;
				string datafolder = "datav4" + DirectorySeparator;

				var configbackup =  "Cumulus.ini";
				var uniquebackup = "UniqueId.txt";
				var alltimebackup = datafolder + Path.GetFileName(AlltimeIniFile);
				var monthlyAlltimebackup = datafolder + Path.GetFileName(MonthlyAlltimeIniFile);
				var daybackup = datafolder + Path.GetFileName(DayFileName);
				var yesterdaybackup = datafolder + Path.GetFileName(YesterdayFile);
				var todaybackup = datafolder + Path.GetFileName(TodayIniFile);
				var monthbackup = datafolder + Path.GetFileName(MonthIniFile);
				var yearbackup = datafolder + Path.GetFileName(YearIniFile);
				var diarybackup = datafolder + Path.GetFileName(diaryfile);
				var dbBackup = datafolder + Path.GetFileName(dbfile);

				var LogFile = GetLogFileName(timestamp);
				var logbackup = datafolder + Path.GetFileName(LogFile);

				var extraFile = GetExtraLogFileName(timestamp);
				var extraBackup = datafolder + Path.GetFileName(extraFile);

				var AirLinkFile = GetAirLinkLogFileName(timestamp);
				var AirLinkBackup = datafolder + Path.GetFileName(AirLinkFile);

				if (!Directory.Exists(folderpath))
				{
					try
					{
						Directory.CreateDirectory(folderpath);
						if (!Directory.Exists(datafolder))
							Directory.CreateDirectory(datafolder);
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, "Backup: Error creating folders");
					}


					// create a zip archive file for the backup
					using (FileStream zipFile = new FileStream(folderpath + DirectorySeparator + foldername + ".zip", FileMode.Create))
					{
						using ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Create);
						try
						{
							if (File.Exists(AlltimeIniFile))
								archive.CreateEntryFromFile(AlltimeIniFile, alltimebackup);
							if (File.Exists(MonthlyAlltimeIniFile))
								archive.CreateEntryFromFile(MonthlyAlltimeIniFile, monthlyAlltimebackup);
							if (File.Exists(DayFileName))
								archive.CreateEntryFromFile(DayFileName, daybackup);
							if (File.Exists(TodayIniFile))
								archive.CreateEntryFromFile(TodayIniFile, todaybackup);
							if (File.Exists(YesterdayFile))
								archive.CreateEntryFromFile(YesterdayFile, yesterdaybackup);
							if (File.Exists(LogFile))
								archive.CreateEntryFromFile(LogFile, logbackup);
							if (File.Exists(MonthIniFile))
								archive.CreateEntryFromFile(MonthIniFile, monthbackup);
							if (File.Exists(YearIniFile))
								archive.CreateEntryFromFile(YearIniFile, yearbackup);
							if (File.Exists("Cumulus.ini"))
								archive.CreateEntryFromFile("Cumulus.ini", configbackup);
							if (File.Exists("UniqueId.txt"))
								archive.CreateEntryFromFile("UniqueId.txt", uniquebackup);
						}
						catch (Exception ex)
						{
							LogExceptionMessage(ex, "Backup: Error backing up the data files");
						}

						if (daily)
						{
							// for daily backup the db is in use, so use an online backup
							try
							{
								var backUpDest = folderpath + "cumulusmx-v4.db";
								var zipLocation = datafolder + "cumulusmx-v4.db";
								LogDebugMessage("Making backup copy of the database");
								station.Database.Backup(backUpDest);
								LogDebugMessage("Completed backup copy of the database");

								LogDebugMessage("Archiving backup copy of the database");
								archive.CreateEntryFromFile(backUpDest, zipLocation);
								LogDebugMessage("Completed backup copy of the database");

								LogDebugMessage("Deleting backup copy of the database");
								File.Delete(backUpDest);

								backUpDest = folderpath + "diary.db";
								zipLocation = datafolder + "diary.db";
								LogDebugMessage("Making backup copy of the diary");
								DiaryDB.Backup(backUpDest);
								LogDebugMessage("Completed backup copy of the diary");

								LogDebugMessage("Archiving backup copy of the diary");
								archive.CreateEntryFromFile(backUpDest, zipLocation);
								LogDebugMessage("Completed backup copy of the diary");

								LogDebugMessage("Deleting backup copy of the diary");
								File.Delete(backUpDest);
							}
							catch (Exception ex)
							{
								LogExceptionMessage(ex, "Error making db backup");
							}
						}
						else
						{
							// start-up backup - the db is not yet in use, do a file copy including any recovery files
							try
							{
								LogDebugMessage("Archiving the database");
								archive.CreateEntryFromFile(dbfile, dbBackup);
								if (File.Exists(dbfile + "-journal"))
								{
									archive.CreateEntryFromFile(dbfile + "-journal", dbBackup + "-journal");
								}

								archive.CreateEntryFromFile(diaryfile, diarybackup);
								if (File.Exists(diaryfile + "-journal"))
								{
									archive.CreateEntryFromFile(diaryfile + "-journal", diarybackup + "-journal");
								}

								LogDebugMessage("Completed archive of the database");


								//CopyBackupFile(dbfile + "-shm", dbBackup + "-shm");
								//CopyBackupFile(dbfile + "-wal", dbBackup + "-wal");
							}
							catch (Exception ex)
							{
								LogExceptionMessage(ex, "Backup: Error backing up the database files");
							}
						}

						try
						{
							if (File.Exists(extraFile))
								archive.CreateEntryFromFile(extraFile, extraBackup);
							if (File.Exists(AirLinkFile))
								archive.CreateEntryFromFile(AirLinkFile, AirLinkBackup);

							// custom logs
							for (var i = 0; i < 10; i++)
							{
								if (CustomIntvlLogSettings[i].Enabled)
								{
									var filename = GetCustomIntvlLogFileName(i, timestamp);
									if (File.Exists(filename))
										archive.CreateEntryFromFile(filename, datafolder + Path.GetFileName(filename));
								}

								if (CustomDailyLogSettings[i].Enabled)
								{
									var filename = GetCustomDailyLogFileName(i);
									if (File.Exists(filename))
										archive.CreateEntryFromFile(filename, datafolder + Path.GetFileName(filename));
								}
							}

							// Do not do this extra backup between 00:00 & Roll-over hour on the first of the month
							// as the month has not yet rolled over - only applies for start-up backups
							if (timestamp.Day == 1 && timestamp.Hour >= RolloverHour)
							{
								var newTime = timestamp.AddDays(-1);
								// on the first of month, we also need to backup last months files as well
								var LogFile2 = GetLogFileName(newTime);
								var logbackup2 = datafolder + Path.GetFileName(LogFile2);

								var extraFile2 = GetExtraLogFileName(newTime);
								var extraBackup2 = datafolder + Path.GetFileName(extraFile2);

								var AirLinkFile2 = GetAirLinkLogFileName(timestamp.AddDays(-1));
								var AirLinkBackup2 = datafolder + Path.GetFileName(AirLinkFile2);

								if (File.Exists(LogFile2))
									archive.CreateEntryFromFile(LogFile2, logbackup2);
								if (File.Exists(extraFile2))
									archive.CreateEntryFromFile(extraFile2, extraBackup2);
								if (File.Exists(AirLinkFile2))
									archive.CreateEntryFromFile(AirLinkFile2, AirLinkBackup2);

								for (var i = 0; i < 10; i++)
								{
									if (CustomIntvlLogSettings[i].Enabled)
									{
										var filename = GetCustomIntvlLogFileName(i, newTime);
										if (File.Exists(filename))
											archive.CreateEntryFromFile(filename, datafolder + Path.GetFileName(filename));
									}
								}
							}
						}
						catch (Exception ex)
						{
							LogExceptionMessage(ex, "Backup: Error backing up extra log files");
						}
					}
					LogMessage("Created backup folder " + foldername);
				}
				else
				{
					LogMessage("Backup folder " + foldername + " already exists, skipping backup");
				}
			}
		}

		/*
		private async Task CopyBackupFile(string src, string dest)
		{
			try
			{
				if (File.Exists(src))
				{
					// don't wait for this to complete
					await Utils.CopyFileAsync(src, dest);
				}
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, $"BackupData: Error copying {src} to {dest}");
			}
		}
		*/

		/*
		/// <summary>
		/// Get a snapshot of the current data values
		/// </summary>
		/// <returns>Structure containing current values</returns>
		public CurrentData GetCurrentData()
		{
			CurrentData currentData = new CurrentData();

			if (station != null)
			{
				currentData.Avgbearing = station.AvgBearing;
				currentData.Bearing = station.Bearing;
				currentData.HeatIndex = station.HeatIndex;
				currentData.Humidex = station.Humidex;
				currentData.AppTemp = station.ApparentTemperature;
				currentData.FeelsLike = station.FeelsLike;
				currentData.IndoorHumidity = station.IndoorHumidity;
				currentData.IndoorTemperature = station.IndoorTemperature;
				currentData.OutdoorDewpoint = station.OutdoorDewpoint;
				currentData.Humidity = station.Humidity;
				currentData.OutdoorTemperature = station.OutdoorTemperature;
				currentData.AvgTempToday = station.TempTotalToday / station.tempsamplestoday;
				currentData.Pressure = station.Pressure;
				currentData.RainMonth = station.RainMonth;
				currentData.RainRate = station.RainRate;
				currentData.RainToday = station.RainToday;
				currentData.RainYesterday = station.RainYesterday;
				currentData.RainYear = station.RainYear;
				currentData.RainLastHour = station.RainLastHour;
				currentData.Recentmaxgust = station.RecentMaxGust;
				currentData.WindAverage = station.WindAverage;
				currentData.WindChill = station.WindChill;
				currentData.WindLatest = station.WindLatest;
				currentData.WindRunToday = station.WindRunToday;
				currentData.TempTrend = station.temptrendval;
				currentData.PressTrend = station.presstrendval;
			}

			return currentData;
		}
		*/

		/*public HighLowData GetHumidityHighLowData()
		{
			HighLowData data = new HighLowData();

			if (station != null)
			{
				data.TodayHigh = station.HighHumidityToday;
				data.TodayHighDT = station.HighHumidityTodayTime;

				data.TodayLow = station.LowHumidityToday;
				data.TodayLowDT = station.LowHumidityTodayTime;

				data.YesterdayHigh = station.Yesterdayhighouthumidity;
				data.YesterdayHighDT = station.Yesterdayhighouthumiditydt.ToLocalTime();

				data.YesterdayLow = station.Yesterdaylowouthumidity;
				data.YesterdayLowDT = station.Yesterdaylowouthumiditydt.ToLocalTime();

				data.MonthHigh = station.Monthhighouthumidity;
				data.MonthHighDT = station.Monthhighouthumiditydt.ToLocalTime();

				data.MonthLow = station.Monthlowouthumidity;
				data.MonthLowDT = station.Monthlowouthumiditydt.ToLocalTime();

				data.YearHigh = station.Yearhighouthumidity;
				data.YearHighDT = station.Yearhighouthumiditydt.ToLocalTime();

				data.YearLow = station.Yearlowouthumidity;
				data.YearLowDT = station.Yearlowouthumiditydt.ToLocalTime();
			}

			return data;
		}

		public HighLowData GetOuttempHighLowData()
		{
			HighLowData data = new HighLowData();

			if (station != null)
			{
				data.TodayHigh = station.Todayhighouttemp;
				data.TodayHighDT = station.Todayhighouttempdt.ToLocalTime();

				data.TodayLow = station.Todaylowouttemp;
				data.TodayLowDT = station.Todaylowouttempdt.ToLocalTime();

				data.YesterdayHigh = station.Yesterdayhighouttemp;
				data.YesterdayHighDT = station.Yesterdayhighouttempdt.ToLocalTime();

				data.YesterdayLow = station.Yesterdaylowouttemp;
				data.YesterdayLowDT = station.Yesterdaylowouttempdt.ToLocalTime();

				data.MonthHigh = station.Monthhighouttemp;
				data.MonthHighDT = station.Monthhighouttempdt.ToLocalTime();

				data.MonthLow = station.Monthlowouttemp;
				data.MonthLowDT = station.Monthlowouttempdt.ToLocalTime();

				data.YearHigh = station.Yearhighouttemp;
				data.YearHighDT = station.Yearhighouttempdt.ToLocalTime();

				data.YearLow = station.Yearlowouttemp;
				data.YearLowDT = station.Yearlowouttempdt.ToLocalTime();
			}

			return data;
		}

		public HighLowData GetPressureHighLowData()
		{
			HighLowData data = new HighLowData();

			if (station != null)
			{
				data.TodayHigh = station.Todayhighpressure;
				data.TodayHighDT = station.Todayhighpressuredt.ToLocalTime();

				data.TodayLow = station.Todaylowpressure;
				data.TodayLowDT = station.Todaylowpressuredt.ToLocalTime();

				data.YesterdayHigh = station.Yesterdayhighpressure;
				data.YesterdayHighDT = station.Yesterdayhighpressuredt.ToLocalTime();

				data.YesterdayLow = station.Yesterdaylowpressure;
				data.YesterdayLowDT = station.Yesterdaylowpressuredt.ToLocalTime();

				data.MonthHigh = station.Monthhighpressure;
				data.MonthHighDT = station.Monthhighpressuredt.ToLocalTime();

				data.MonthLow = station.Monthlowpressure;
				data.MonthLowDT = station.Monthlowpressuredt.ToLocalTime();

				data.YearHigh = station.Yearhighpressure;
				data.YearHighDT = station.Yearhighpressuredt.ToLocalTime();

				data.YearLow = station.Yearlowpressure;
				data.YearLowDT = station.Yearlowpressuredt.ToLocalTime();
			}

			return data;
		}

		public HighLowData GetRainRateHighLowData()
		{
			HighLowData data = new HighLowData();

			if (station != null)
			{
				data.TodayHigh = station.Todayhighrainrate;
				data.TodayHighDT = station.Todayhighrainratedt.ToLocalTime();

				data.YesterdayHigh = station.Yesterdayhighrainrate;
				data.YesterdayHighDT = station.Yesterdayhighrainratedt.ToLocalTime();

				data.MonthHigh = station.Monthhighrainrate;
				data.MonthHighDT = station.Monthhighrainratedt.ToLocalTime();

				data.YearHigh = station.Yearhighrainrate;
				data.YearHighDT = station.Yearhighrainratedt.ToLocalTime();
			}

			return data;
		}

		public HighLowData GetRainHourHighLowData()
		{
			HighLowData data = new HighLowData();

			if (station != null)
			{
				data.TodayHigh = station.Todayhighrainhour;
				data.TodayHighDT = station.Todayhighrainhourdt.ToLocalTime();

				data.MonthHigh = station.Monthhighrainhour;
				data.MonthHighDT = station.Monthhighrainhourdt.ToLocalTime();

				data.YearHigh = station.Yearhighrainhour;
				data.YearHighDT = station.Yearhighrainhourdt.ToLocalTime();
			}

			return data;
		}

		public HighLowData GetGustHighLowData()
		{
			HighLowData data = new HighLowData();

			if (station != null)
			{
				data.TodayHigh = station.Todayhighgust;
				data.TodayHighDT = station.Todayhighgustdt.ToLocalTime();

				data.YesterdayHigh = station.Yesterdayhighgust;
				data.YesterdayHighDT = station.Yesterdayhighgustdt.ToLocalTime();

				data.MonthHigh = station.ThisMonthRecs.HighGust.Val;
				data.MonthHighDT = station.ThisMonthRecs.HighGust.Ts.ToLocalTime();

				data.YearHigh = station.Yearhighgust;
				data.YearHighDT = station.Yearhighgustdt.ToLocalTime();
			}

			return data;
		}

		public HighLowData GetSpeedHighLowData()
		{
			HighLowData data = new HighLowData();

			if (station != null)
			{
				data.TodayHigh = station.Todayhighspeed;
				data.TodayHighDT = station.Todayhighspeeddt.ToLocalTime();

				data.YesterdayHigh = station.Yesterdayhighspeed;
				data.YesterdayHighDT = station.Yesterdayhighspeeddt.ToLocalTime();

				data.MonthHigh = station.Monthhighspeed;
				data.MonthHighDT = station.Monthhighspeeddt.ToLocalTime();

				data.YearHigh = station.Yearhighspeed;
				data.YearHighDT = station.Yearhighspeeddt.ToLocalTime();
			}

			return data;
		}*/

		/*
		public string GetForecast()
		{
			return station.Forecast;
		}

		public string GetCurrentActivity()
		{
			return CurrentActivity;
		}

		public bool GetImportDataSetting()
		{
			return ImportData;
		}

		public void SetImportDataSetting(bool setting)
		{
			ImportData = setting;
		}

		public bool GetLogExtraDataSetting()
		{
			return LogExtraData;
		}

		public void SetLogExtraDataSetting(bool setting)
		{
			LogExtraData = setting;
		}

		public string GetCumulusIniPath()
		{
			return CumulusIniPath;
		}

		public void SetCumulusIniPath(string inipath)
		{
			CumulusIniPath = inipath;
		}

		public int GetLogInterval()
		{
			return LogInterval;
		}

		public void SetLogInterval(int interval)
		{
			LogInterval = interval;
		}
		*/

		public int GetHourInc(DateTime timestamp)
		{
			if (RolloverHour == 0)
			{
				return 0;
			}
			else
			{
				try
				{
					if (Use10amInSummer && TimeZoneInfo.Local.IsDaylightSavingTime(timestamp))
					{
						// Locale is currently on Daylight time
						return -10;
					}

					else
					{
						// Locale is currently on Standard time or unknown
						return -9;
					}
				}
				catch (Exception)
				{
					return -9;
				}
			}
		}

		public int GetHourInc()
		{
			return GetHourInc(DateTime.Now);
		}

		/*
		private bool IsDaylightSavings()
		{
			return TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now);
		}
		*/

		public string Beaufort(double? Bspeed) // Takes speed in current unit, returns Bft number as text
		{
			return station.Beaufort(Bspeed).ToString() ;
		}

		public string BeaufortDesc(double? Bspeed)
		{
			// Takes speed in current units, returns Bft description

			// Convert to Force
			var force = station.Beaufort(Bspeed);
			return force switch
			{
				0 => Calm,
				1 => Lightair,
				2 => Lightbreeze,
				3 => Gentlebreeze,
				4 => Moderatebreeze,
				5 => Freshbreeze,
				6 => Strongbreeze,
				7 => Neargale,
				8 => Gale,
				9 => Stronggale,
				10 => Storm,
				11 => Violentstorm,
				12 => Hurricane,
				_ => "UNKNOWN",
			};
		}

		public void LogErrorMessage(string message)
		{
			LatestError = message;
			LatestErrorTS = DateTime.Now;
			LogMessage(message);
		}

		public void LogSpikeRemoval(string message)
		{
			if (ErrorLogSpikeRemoval)
			{
				LogErrorMessage("Spike removal: " + message);
			}
		}

		public void Stop()
		{
			LogMessage("Cumulus closing");

			// Stop the timers
			LogMessage("Stopping timers");
			try { RealtimeTimer.Stop(); } catch { }
			try { Wund.IntTimer.Stop(); } catch { }
			try { WebTimer.Stop(); } catch { }
			try { AWEKAS.IntTimer.Stop(); } catch { }
			try { MQTTTimer.Stop(); } catch { }
			//AirLinkTimer.Stop();
			try { CustomHttpSecondsTimer.Stop(); } catch { }
			try { MySqlStuff.CustomSecondsTimer.Stop(); } catch { }
			try { MQTTTimer.Stop(); } catch { }

			try
			{
				LogMessage("Stopping extra sensors...");
				// If we have a Outdoor AirLink sensor, and it is linked to this WLL then stop it now
				airLinkOut?.Stop();
				// If we have a Indoor AirLink sensor, and it is linked to this WLL then stop it now
				airLinkIn?.Stop();
				// If we have a Ecowitt Extra Sensors, stop it
				ecowittExtra?.Stop();
				// If we have a Ambient Extra Sensors, stop it
				ambientExtra?.Stop();

				LogMessage("Extra sensors stopped");
			}
			catch { }

			if (station != null)
			{
				LogMessage("Stopping station...");
				try
				{
					station.Stop();
					LogMessage("Station stopped");

					if (station.HaveReadData)
					{
						LogMessage("Writing today.ini file");
						station.WriteTodayFile(DateTime.Now, false);
						LogMessage("Completed writing today.ini file");
					}
					else
					{
						LogMessage("No data read this session, today.ini not written");
					}
				}
				catch { }
			}

			// do we have a shutdown task to run?
			if (!string.IsNullOrEmpty(ProgramOptions.ShutdownTask))
			{
				try
				{
					tokenParser.InputText = ProgramOptions.ShutdownTaskParams;
					var args = tokenParser.ToStringFromString();
					LogMessage($"Running shutdown task: {ProgramOptions.ShutdownTask}, arguments: {args}");
					Utils.RunExternalTask(ProgramOptions.ShutdownTask, args, false);
				}
				catch (Exception ex)
				{
					LogMessage($"Error running shutdown task: {ex.Message}");
				}
			}

			LogMessage("Station shutdown complete");
		}

		public void DoHTMLFiles(DateTime ts)
		{
			try
			{
				if (!RealtimeIntervalEnabled)
				{
					CreateRealtimeFile(999).Wait();
					MySqlStuff.DoRealtimeData(999, true);
				}

				LogDebugMessage("Creating standard web files");
				for (var i = 0; i < StdWebFiles.Length; i++)
				{
					if (StdWebFiles[i].Create && !string.IsNullOrWhiteSpace(StdWebFiles[i].TemplateFileName))
					{
						var destFile = StdWebFiles[i].LocalPath + StdWebFiles[i].LocalFileName;
						ProcessTemplateFile(StdWebFiles[i].TemplateFileName, destFile, tokenParser, true).Wait();
					}
				}
				LogDebugMessage("Done creating standard Data file");

				LogDebugMessage("Creating graph data files");
				station.Graphs.CreateGraphDataFiles(ts).Wait();
				LogDebugMessage("Done creating graph data files");

				//LogDebugMessage("Creating extra files");
				// handle any extra files
				for (int i = 0; i < numextrafiles; i++)
				{
					if (!ExtraFiles[i].realtime && !ExtraFiles[i].endofday)
					{
						var uploadfile = ExtraFiles[i].local;
						var remotefile = ExtraFiles[i].remote;

						if ((uploadfile.Length > 0) && (remotefile.Length > 0))
						{
							uploadfile = GetUploadFilename(uploadfile, DateTime.Now);

							if (File.Exists(uploadfile))
							{
								remotefile = GetRemoteFileName(remotefile, DateTime.Now);

								if (ExtraFiles[i].process)
								{
									LogDebugMessage($"Interval: Processing extra file[{i}] - {uploadfile}");
									// process the file
									ProcessTemplateFile(uploadfile, uploadfile + "tmp", tokenParser, false).Wait();
									uploadfile += "tmp";
								}

								if (!ExtraFiles[i].FTP)
								{
									// just copy the file
									LogDebugMessage($"Interval: Copying extra file[{i}] {uploadfile} to {remotefile}");
									try
									{
										Utils.CopyFileSync(uploadfile, remotefile);
									}
									catch (Exception ex)
									{
										LogExceptionMessage(ex, $"Interval: Error copying extra file[{i}] ");
									}
									//LogDebugMessage("Finished copying extra file " + uploadfile);
								}
							}
							else
							{
								LogMessage($"Interval: Warning, extra web file[{i}] not found - {uploadfile}");
							}
						}
					}
				}

				if (!string.IsNullOrEmpty(ExternalProgram))
				{
					try
					{
						tokenParser.InputText = ExternalParams;
						var args = tokenParser.ToStringFromString();
						LogDebugMessage("Interval: Executing program " + ExternalProgram + " " + args);
						Utils.RunExternalTask(ExternalProgram, args, false);
						LogDebugMessage("Interval: External program started");
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, "Interval: Error starting external program");
					}
				}

				//LogDebugMessage("Done creating extra files");

				_ = DoLocalCopy();

				DoFTPLogin();
			}
			finally
			{
				WebUpdating = 0;
			}
		}

		public async Task DoLocalCopy()
		{
			var remotePath = FtpOptions.LocalCopyFolder;

			try
			{
				var folderSep2 = Path.AltDirectorySeparatorChar;

				if (!FtpOptions.LocalCopyEnabled)
					return;

				if (FtpOptions.LocalCopyFolder.Length > 0)
				{
					remotePath = (FtpOptions.LocalCopyFolder.EndsWith(DirectorySeparator.ToString()) || FtpOptions.LocalCopyFolder.EndsWith(folderSep2.ToString())) ? FtpOptions.LocalCopyFolder : FtpOptions.LocalCopyFolder + DirectorySeparator;
				}
				else
				{
					return;
				}
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, "LocalCopy: Error with paths");
			}

			var srcfile = "";
			string dstfile;

			if (NOAAconf.NeedCopy)
			{
				try
				{
					// upload NOAA reports
					LogDebugMessage("LocalCopy: Copying NOAA reports");

					try
					{
						var dstPath = string.IsNullOrEmpty(NOAAconf.CopyFolder) ? remotePath : NOAAconf.CopyFolder;
						srcfile = ReportPath + NOAAconf.LatestMonthReport;
						dstfile = dstPath + DirectorySeparator + NOAAconf.LatestMonthReport;
						await Utils.CopyFileAsync(srcfile, dstfile);

						srcfile = ReportPath + NOAAconf.LatestYearReport;
						dstfile = dstPath + DirectorySeparator + NOAAconf.LatestYearReport;
						await Utils.CopyFileAsync(srcfile, dstfile);

						NOAAconf.NeedCopy = false;

						LogDebugMessage("LocalCopy: Done copying NOAA reports");
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, "LocalCopy: Error copy NOAA reports");
					}
				}
				catch (Exception ex)
				{
					LogExceptionMessage(ex, $"LocalCopy: Error copying file {srcfile}");
				}
			}

			// standard files
			LogDebugMessage("LocalCopy: Copying standard web files");
			for (var i = 0; i < StdWebFiles.Length; i++)
			{
				if (StdWebFiles[i].Copy && StdWebFiles[i].CopyRequired)
				{
					try
					{
						srcfile = StdWebFiles[i].LocalPath + StdWebFiles[i].LocalFileName;
						dstfile = remotePath + StdWebFiles[i].RemoteFileName;
						await Utils.CopyFileAsync(srcfile, dstfile);
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"LocalCopy: Error copying standard data file [{StdWebFiles[i].LocalFileName}]");
					}
				}
			}
			LogDebugMessage("LocalCopy: Done copying standard web files");

			LogDebugMessage("LocalCopy: Copying graph data files");
			for (int i = 0; i < GraphDataFiles.Length; i++)
			{
				if (GraphDataFiles[i].Copy && GraphDataFiles[i].CopyRequired)
				{
					try
					{
						srcfile = GraphDataFiles[i].LocalPath + GraphDataFiles[i].LocalFileName;
						dstfile = remotePath + GraphDataFiles[i].RemoteFileName;

						await Utils.CopyFileAsync(srcfile, dstfile);
						// The config files only need uploading once per change
						if (GraphDataFiles[i].LocalFileName == "availabledata.json" ||
							GraphDataFiles[i].LocalFileName == "graphconfig.json")
						{
							GraphDataFiles[i].CopyRequired = false;
						}
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"LocalCopy: Error copying graph data file [{srcfile}]");
					}
				}
			}
			LogDebugMessage("LocalCopy: Done copying graph data files");

			LogDebugMessage("LocalCopy: Copying daily graph data files");
			for (int i = 0; i < GraphDataEodFiles.Length; i++)
			{
				if (GraphDataEodFiles[i].Copy && GraphDataEodFiles[i].CopyRequired)
				{
					try
					{
						srcfile = GraphDataEodFiles[i].LocalPath + GraphDataEodFiles[i].LocalFileName;
						dstfile = remotePath + GraphDataEodFiles[i].RemoteFileName;

						await Utils.CopyFileAsync(srcfile, dstfile);
						// Uploaded OK, reset the upload required flag
						GraphDataEodFiles[i].CopyRequired = false;
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"LocalCopy: Error copying daily graph data file [{srcfile}]");
					}
				}
			}
			LogDebugMessage("LocalCopy: Done copying daily graph data files");

			if (MoonImage.Copy && MoonImage.ReadyToCopy)
			{
				try
				{
					LogDebugMessage("LocalCopy: Copying Moon image file");
					await Utils.CopyFileAsync("web" + DirectorySeparator + "moon.png", MoonImage.CopyDest);

					LogDebugMessage("LocalCopy: Done copying Moon image file");
					// clear the image ready for FTP flag, only upload once an hour
					MoonImage.ReadyToCopy = false;
				}
				catch (Exception ex)
				{
					LogExceptionMessage(ex, "LocalCopy: Error copying moon image");
				}
			}

			LogDebugMessage("LocalCopy: Copy process complete");
		}


		public void DoFTPLogin()
		{
			var remotePath = "";

			if (!FtpOptions.Enabled || !FtpOptions.IntervalEnabled)
				return;

			if (FtpOptions.Directory.Length > 0)
			{
				remotePath = (FtpOptions.Directory.EndsWith("/") ? FtpOptions.Directory : FtpOptions.Directory + "/");
			}

			if (FtpOptions.FtpMode == FtpProtocols.SFTP)
			{
				ConnectionInfo connectionInfo;
				try
				{
					if (string.IsNullOrEmpty(FtpOptions.Username))
					{
						LogMessage("SFTP[Int]: Error, your username is blank!");
						return;
					}

					if (FtpOptions.SshAuthen == "password")
					{
						if (string.IsNullOrEmpty(FtpOptions.Password))
						{
							LogMessage("SFTP[Int]: Error, your password is blank!");
							return;
						}

						connectionInfo = new ConnectionInfo(FtpOptions.Hostname, FtpOptions.Port, FtpOptions.Username, new PasswordAuthenticationMethod(FtpOptions.Username, FtpOptions.Password));
						LogFtpDebugMessage("SFTP[Int]: Connecting using password authentication");
					}
					else if (FtpOptions.SshAuthen == "psk")
					{
						if (File.Exists(FtpOptions.SshPskFile))
						{
							PrivateKeyFile pskFile = new PrivateKeyFile(FtpOptions.SshPskFile);
							connectionInfo = new ConnectionInfo(FtpOptions.Hostname, FtpOptions.Port, FtpOptions.Username, new PrivateKeyAuthenticationMethod(FtpOptions.Username, pskFile));
							LogFtpDebugMessage("SFTP[Int]: Connecting using PSK authentication");
						}
						else
						{
							LogMessage($"SFTP[Int]: Error: Could not find your PSK key file: {FtpOptions.SshPskFile}");
							return;
						}
					}
					else if (FtpOptions.SshAuthen == "password_psk")
					{
						if (string.IsNullOrEmpty(FtpOptions.Password))
						{
							LogMessage("SFTP[Int]: Error, your password is blank!");
							return;
						}

						if (File.Exists(FtpOptions.SshPskFile))
						{
							PrivateKeyFile pskFile = new PrivateKeyFile(FtpOptions.SshPskFile);
							connectionInfo = new ConnectionInfo(FtpOptions.Hostname, FtpOptions.Port, FtpOptions.Username, new PasswordAuthenticationMethod(FtpOptions.Username, FtpOptions.Password), new PrivateKeyAuthenticationMethod(FtpOptions.Username, pskFile));
							LogFtpDebugMessage("SFTP[Int]: Connecting using password or PSK authentication");
						}
						else
						{
							LogMessage($"SFTP[Int]: Error: Could not find your PSK key file: {FtpOptions.SshPskFile}");
							return;
						}
					}
					else
					{
						LogFtpMessage($"SFTP[Int]: Invalid SshftpAuthentication specified [{FtpOptions.SshAuthen}]");
						return;
					}
				}
				catch (Exception ex)
				{
					LogExceptionMessage(ex, "SFTP[Int]: Error creating SFTP connection object", true);
					return;
				}

				try
				{
					if (string.IsNullOrEmpty(FtpOptions.Hostname))
					{
						LogMessage("SFTP[Int]: Error, your server name is blank!");
						return;
					}

					using SftpClient conn = new SftpClient(connectionInfo);
					try
					{
						LogFtpDebugMessage($"SFTP[Int]: CumulusMX Connecting to {FtpOptions.Hostname} on port {FtpOptions.Port}");
						conn.Connect();
						if (ServicePointManager.DnsRefreshTimeout == 0)
						{
							ServicePointManager.DnsRefreshTimeout = 120000; // two minutes default
						}
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, "SFTP[Int]: Error connecting SFTP", true);
						if ((uint)ex.HResult == 0x80004005) // Could not resolve host
						{
							// Disable the DNS cache for the next query
							ServicePointManager.DnsRefreshTimeout = 0;
						}
						return;
					}

					if (conn.IsConnected)
					{
						if (NOAAconf.NeedFtp)
						{
							try
							{
								// upload NOAA reports
								LogFtpDebugMessage("SFTP[Int]: Uploading NOAA reports");

								var uploadfile = ReportPath + NOAAconf.LatestMonthReport;
								var remotefile = NOAAconf.FtpFolder + '/' + NOAAconf.LatestMonthReport;

								UploadFile(conn, uploadfile, remotefile, -1);

								uploadfile = ReportPath + NOAAconf.LatestYearReport;
								remotefile = NOAAconf.FtpFolder + '/' + NOAAconf.LatestYearReport;

								UploadFile(conn, uploadfile, remotefile, -1);

								LogFtpDebugMessage("SFTP[Int]: Done uploading NOAA reports");
							}
							catch (Exception e)
							{
								LogExceptionMessage(e, "SFTP[Int]: Error uploading file", true);
							}
							NOAAconf.NeedFtp = false;
						}

						// Extra files
						for (int i = 0; i < numextrafiles; i++)
						{
							var uploadfile = ExtraFiles[i].local;
							var remotefile = ExtraFiles[i].remote;

							if ((uploadfile.Length > 0) &&
								(remotefile.Length > 0) &&
								!ExtraFiles[i].realtime &&
								(!ExtraFiles[i].endofday || EODfilesNeedFTP == ExtraFiles[i].endofday) && // Either, it's not flagged as an EOD file, OR: It is flagged as EOD and EOD FTP is required
								ExtraFiles[i].FTP)
							{
								// For EOD files, we want the previous days log files since it is now just past the day roll-over time. Makes a difference on month roll-over
								var logDay = ExtraFiles[i].endofday ? DateTime.Now.AddDays(-1) : DateTime.Now;

								uploadfile = GetUploadFilename(uploadfile, logDay);

								if (File.Exists(uploadfile))
								{
									LogFtpDebugMessage("FTP[Int]: Uploading Extra file: " + uploadfile);

									remotefile = GetRemoteFileName(remotefile, logDay);

									// all checks OK, file needs to be uploaded
									if (ExtraFiles[i].process)
									{
										// we've already processed the file
										uploadfile += "tmp";
									}

									try
									{
										UploadFile(conn, uploadfile, remotefile, -1);
									}
									catch (Exception e)
									{
										LogExceptionMessage(e, $"SFTP[Int]: Error uploading Extra web file #{i} [{uploadfile}]", true);
									}
								}
								else
								{
									LogFtpMessage($"SFTP[Int]: Extra web file #{i} [{uploadfile}] not found!");
								}
							}
						}
						if (EODfilesNeedFTP)
						{
							EODfilesNeedFTP = false;
						}

						// standard files
						for (var i = 0; i < StdWebFiles.Length; i++)
						{
							if (StdWebFiles[i].FTP && StdWebFiles[i].FtpRequired)
							{
								try
								{
									var localFile = StdWebFiles[i].LocalPath + StdWebFiles[i].LocalFileName;
									var remotefile = remotePath + StdWebFiles[i].RemoteFileName;
									LogFtpDebugMessage("FTP[Int]: Uploading standard Data file: " + localFile);

									UploadFile(conn, localFile, remotefile, -1);
								}
								catch (Exception e)
								{
									LogFtpMessage($"SFTP[Int]: Error uploading standard data file [{StdWebFiles[i].LocalFileName}]");
									LogFtpMessage($"SFTP[Int]: Error = {e}");
								}
							}
						}

						for (int i = 0; i < GraphDataFiles.Length; i++)
						{
							if (GraphDataFiles[i].FTP && GraphDataFiles[i].FtpRequired)
							{
								var uploadfile = GraphDataFiles[i].LocalPath + GraphDataFiles[i].LocalFileName;
								var remotefile = remotePath + GraphDataFiles[i].RemoteFileName;

								try
								{
									LogFtpDebugMessage("FTP[Int]: Uploading graph data file: " + uploadfile);

									UploadFile(conn, uploadfile, remotefile, -1);
									// The config files only need uploading once per change
									if (GraphDataFiles[i].LocalFileName == "availabledata.json" ||
										GraphDataFiles[i].LocalFileName == "graphconfig.json")
									{
										GraphDataFiles[i].FtpRequired = false;
									}
								}
								catch (Exception e)
								{
									LogFtpMessage($"SFTP[Int]: Error uploading graph data file [{uploadfile}]");
									LogFtpMessage($"SFTP[Int]: Error = {e}");
								}
							}
						}

						for (int i = 0; i < GraphDataEodFiles.Length; i++)
						{
							if (GraphDataEodFiles[i].FTP && GraphDataEodFiles[i].FtpRequired)
							{
								var uploadfile = GraphDataEodFiles[i].LocalPath + GraphDataEodFiles[i].LocalFileName;
								var remotefile = remotePath + GraphDataEodFiles[i].RemoteFileName;
								try
								{
									LogFtpMessage("FTP[Int]: Uploading daily graph data file: " + uploadfile);

									UploadFile(conn, uploadfile, remotefile, -1);
									// Uploaded OK, reset the upload required flag
									GraphDataEodFiles[i].FtpRequired = false;
								}
								catch (Exception e)
								{
									LogFtpMessage($"SFTP[Int]: Error uploading daily graph data file [{uploadfile}]");
									LogFtpMessage($"SFTP[Int]: Error = {e}");
								}
							}
						}

						if (MoonImage.Ftp && MoonImage.ReadyToFtp)
						{
							try
							{
								LogFtpMessage("SFTP[Int]: Uploading Moon image file");
								UploadFile(conn, "web" + DirectorySeparator + "moon.png", remotePath + MoonImage.FtpDest, -1);
								LogFtpMessage("SFTP[Int]: Done uploading Moon image file");
								// clear the image ready for FTP flag, only upload once an hour
								MoonImage.ReadyToFtp = false;
							}
							catch (Exception e)
							{
								LogExceptionMessage(e, $"SFTP[Int]: Error uploading moon image", true);
							}
						}
					}
					try
					{
						// do not error on disconnect
						conn.Disconnect();
					}
					catch { }
				}
				catch (Exception ex)
				{
					LogFtpMessage($"SFTP[Int]: Error using SFTP connection - {ex.Message}");
				}
				LogFtpDebugMessage("SFTP[Int]: Process complete");
			}
			else
			{
				using (FtpClient conn = new FtpClient())
				{
					if (FtpOptions.Logging)
					{
						conn.Logger = FtpLoggerIN;
					}
					LogFtpDebugMessage($"FTP[Int]: CumulusMX Connecting to " + FtpOptions.Hostname);
					conn.Host = FtpOptions.Hostname;
					conn.Port = FtpOptions.Port;
					conn.Credentials = new NetworkCredential(FtpOptions.Username, FtpOptions.Password);

					if (!FtpOptions.AutoDetect)
					{

						if (FtpOptions.FtpMode == FtpProtocols.FTPS)
						{
							// Explicit = Current protocol - connects using FTP and switches to TLS
							// Implicit = Old depreciated protocol - connects using TLS
							conn.Config.EncryptionMode = FtpOptions.DisableExplicit ? FtpEncryptionMode.Implicit : FtpEncryptionMode.Explicit;
							conn.Config.DataConnectionEncryption = true;
							// b3045 - switch from System.Net.Ftp.Client to FluentFTP allows us to specify protocols
							// b3155 - switch to default again - this will use the highest version available in the OS
							//conn.SslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12;
						}

						if (FtpOptions.ActiveMode)
						{
							conn.Config.DataConnectionType = FtpDataConnectionType.PORT;
						}
						else if (FtpOptions.DisableEPSV)
						{
							conn.Config.DataConnectionType = FtpDataConnectionType.PASV;
						}
					}

					if (FtpOptions.FtpMode == FtpProtocols.FTPS)
					{
						conn.Config.ValidateAnyCertificate = FtpOptions.IgnoreCertErrors;
					}

					try
					{
						if (FtpOptions.AutoDetect)
						{
							conn.AutoConnect();
						}
						else
						{
							conn.Connect();
						}

						if (ServicePointManager.DnsRefreshTimeout == 0)
						{
							ServicePointManager.DnsRefreshTimeout = 120000; // two minutes default
						}
					}
					catch (Exception ex)
					{
						LogFtpMessage("FTP[Int]: Error connecting ftp - " + ex.Message);

						if (null != ex.InnerException)
						{
							LogMessage($"FTP[Int]: Error connecting ftp - {ex.GetBaseException().Message}");
						}

						if ((uint)ex.HResult == 0x80004005) // Could not resolve host
						{
							// Disable the DNS cache for the next query
							ServicePointManager.DnsRefreshTimeout = 0;
						}
						return;
					}

					//conn.Config.EnableThreadSafeDataConnections = false; // use same connection for all transfers


					if (conn.IsConnected)
					{
						if (NOAAconf.NeedFtp)
						{
							try
							{
								// upload NOAA reports
								LogFtpDebugMessage("FTP[Int]: Uploading NOAA reports");

								var uploadfile = ReportPath + NOAAconf.LatestMonthReport;
								var remotefile = NOAAconf.FtpFolder + '/' + NOAAconf.LatestMonthReport;

								UploadFile(conn, uploadfile, remotefile);

								uploadfile = ReportPath + NOAAconf.LatestYearReport;
								remotefile = NOAAconf.FtpFolder + '/' + NOAAconf.LatestYearReport;

								UploadFile(conn, uploadfile, remotefile);
								LogFtpDebugMessage("FTP[Int]: Upload of NOAA reports complete");
							}
							catch (Exception e)
							{
								LogExceptionMessage(e, "FTP[Int]: FTP[Int]: Error uploading NOAA files", true);
							}
							NOAAconf.NeedFtp = false;
						}

						// Extra files
						for (int i = 0; i < numextrafiles; i++)
						{
							var uploadfile = ExtraFiles[i].local;
							var remotefile = ExtraFiles[i].remote;

							if ((uploadfile.Length > 0) &&
								(remotefile.Length > 0) &&
								!ExtraFiles[i].realtime &&
								(EODfilesNeedFTP || (EODfilesNeedFTP == ExtraFiles[i].endofday)) &&
								ExtraFiles[i].FTP)
							{
								// For EOD files, we want the previous days log files since it is now just past the day roll-over time. Makes a difference on month roll-over
								var logDay = ExtraFiles[i].endofday ? DateTime.Now.AddDays(-1) : DateTime.Now;

								uploadfile = GetUploadFilename(uploadfile, logDay);

								if (File.Exists(uploadfile))
								{
									remotefile = GetRemoteFileName(remotefile, logDay);

									LogFtpDebugMessage("FTP[Int]: Uploading Extra files");

									// all checks OK, file needs to be uploaded
									if (ExtraFiles[i].process)
									{
										// we've already processed the file
										uploadfile += "tmp";
									}

									try
									{
										UploadFile(conn, uploadfile, remotefile);
									}
									catch (Exception e)
									{
										LogExceptionMessage(e, $"FTP[Int]: Error uploading file {uploadfile}", true);
									}
								}
								else
								{
									LogFtpMessage("FTP[Int]: Extra web file #" + i + " [" + uploadfile + "] not found!");
								}
							}
						}
						if (EODfilesNeedFTP)
						{
							EODfilesNeedFTP = false;
						}


						// standard files
						for (int i = 0; i < StdWebFiles.Length; i++)
						{
							if (StdWebFiles[i].FTP && StdWebFiles[i].FtpRequired)
							{
								try
								{
									var localfile = StdWebFiles[i].LocalPath + StdWebFiles[i].LocalFileName;
									LogFtpDebugMessage("FTP[Int]: Uploading standard Data file: " + localfile);

									UploadFile(conn, localfile, remotePath + StdWebFiles[i].RemoteFileName);
								}
								catch (Exception e)
								{
									LogFtpMessage($"FTP[Int]: Error uploading file {StdWebFiles[i].LocalFileName}: {e}");
								}
							}
						}

						for (int i = 0; i < GraphDataFiles.Length; i++)
						{
							if (GraphDataFiles[i].FTP && GraphDataFiles[i].FtpRequired)
							{
								try
								{
									var localfile = GraphDataFiles[i].LocalPath + GraphDataFiles[i].LocalFileName;
									var remotefile = remotePath + GraphDataFiles[i].RemoteFileName;
									LogFtpDebugMessage("FTP[Int]: Uploading graph data file: " + localfile);

									UploadFile(conn, localfile, remotefile);
								}
								catch (Exception e)
								{
									LogFtpMessage($"FTP[Int]: Error uploading graph data file [{GraphDataFiles[i].LocalFileName}]");
									LogFtpMessage($"FTP[Int]: Error = {e}");
								}
							}
						}

						for (int i = 0; i < GraphDataEodFiles.Length; i++)
						{
							if (GraphDataEodFiles[i].FTP && GraphDataEodFiles[i].FtpRequired)
							{
								var localfile = GraphDataEodFiles[i].LocalPath + GraphDataEodFiles[i].LocalFileName;
								var remotefile = remotePath + GraphDataEodFiles[i].RemoteFileName;
								try
								{
									LogFtpMessage("FTP[Int]: Uploading daily graph data file: " + localfile);

									UploadFile(conn, localfile, remotefile, -1);
									// Uploaded OK, reset the upload required flag
									GraphDataEodFiles[i].FtpRequired = false;
								}
								catch (Exception e)
								{
									LogFtpMessage($"SFTP[Int]: Error uploading daily graph data file [{GraphDataEodFiles[i].LocalFileName}]");
									LogFtpMessage($"SFTP[Int]: Error = {e}");
								}
							}
						}

						if (MoonImage.Ftp && MoonImage.ReadyToFtp)
						{
							try
							{
								LogFtpDebugMessage("FTP[Int]: Uploading Moon image file");
								UploadFile(conn, "web" + DirectorySeparator + "moon.png", remotePath + MoonImage.FtpDest);
								// clear the image ready for FTP flag, only upload once an hour
								MoonImage.ReadyToFtp = false;
							}
							catch (Exception e)
							{
								LogExceptionMessage(e, "FTP[Int]: Error uploading moon image", true);
							}
						}
					}

					// b3045 - dispose of connection
					conn.Disconnect();
					LogFtpDebugMessage("FTP[Int]: Disconnected from " + FtpOptions.Hostname);
				}
				LogFtpMessage("FTP[Int]: Process complete");
			}
		}

		// Return True if the connection still exists
		// Return False if the connection is disposed, null, or not connected
		private bool UploadFile(FtpClient conn, string localfile, string remotefile, int cycle = -1)
		{
			string remotefiletmp = FTPRename ? remotefile + "tmp" : remotefile;
			string cycleStr = cycle >= 0 ? cycle.ToString() : "Int";

			if (FtpOptions.Logging)
			{
				FtpLoggerMX.LogInformation("");
			}
			try
			{
				if (!File.Exists(localfile))
				{
					LogMessage($"FTP[{cycleStr}]: Error! Local file not found, aborting upload: {localfile}");
					return true;
				}

				if (FTPRename)
				{
					// delete the existing tmp file
					try
					{
						if (conn.FileExists(remotefiletmp))
						{
							conn.DeleteFile(remotefiletmp);
						}
					}
					catch
					{
						// continue on error
					}
				}

				if (DeleteBeforeUpload)
				{
					// delete the existing file
					try
					{
						LogFtpDebugMessage($"FTP[{cycleStr}]: Deleting {remotefile}");
						if (conn.FileExists(remotefile))
						{
							conn.DeleteFile(remotefile);
						}
						else
						{
							LogFtpDebugMessage($"FTP[{cycleStr}]: Cannot delete remote file {remotefile} as it does not exist");
						}
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"FTP[{cycleStr}]: Error deleting {remotefile}", true);
						if (ex.InnerException != null)
						{
							LogFtpMessage($"FTP[{cycleStr}]: Inner Exception: {ex.GetBaseException().Message}");
						}
						// continue on error
					}
				}

				LogFtpDebugMessage($"FTP[{cycleStr}]: Uploading {localfile} to {remotefiletmp}");

				var status = conn.UploadFile(localfile, remotefiletmp);

				if (status.IsFailure())
				{
					LogMessage($"FTP[{cycleStr}]: Upload of {localfile} to {remotefile} failed");
				}
				else if (FTPRename)
				{
					// rename the file
					LogFtpDebugMessage($"FTP[{cycleStr}]: Renaming {remotefiletmp} to {remotefile}");

					try
					{
						conn.Rename(remotefiletmp, remotefile);
						LogFtpDebugMessage($"FTP[{cycleStr}]: Renamed {remotefiletmp}");
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"FTP[{cycleStr}]: Error renaming {remotefiletmp} to {remotefile}", true);
						if (ex.InnerException != null)
						{
							LogFtpMessage($"FTP[{cycleStr}]: Inner Exception: {ex.GetBaseException().Message}");
						}
						return conn.IsConnected;
					}
				}
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, $"FTP[{cycleStr}]: Error uploading {localfile} to {remotefile}", true);
				if (ex.InnerException != null)
				{
					LogFtpMessage($"FTP[{cycleStr}]: Inner Exception: {ex.GetBaseException().Message}");
				}
			}

			return conn.IsConnected;
		}

		// Return True if the connection still exists
		// Return False if the connection is disposed, null, or not connected
		private bool UploadFile(SftpClient conn, string localfile, string remotefile, int cycle)
		{
			string remotefilename = FTPRename ? remotefile + "tmp" : remotefile;
			string cycleStr = cycle >= 0 ? cycle.ToString() : "Int";

			if (!File.Exists(localfile))
			{
				LogMessage($"SFTP[{cycleStr}]: Error! Local file not found, aborting upload: {localfile}");
				return true;
			}

			try
			{
				if (conn == null || !conn.IsConnected)
				{
					LogFtpMessage($"SFTP[{cycleStr}]: The SFTP object is null or not connected - skipping upload of {localfile}");
					return false;
				}
			}
			catch (ObjectDisposedException)
			{
				LogFtpMessage($"SFTP[{cycleStr}]: The SFTP object is disposed - skipping upload of {localfile}");
				return false;
			}

			try
			{
				// No delete before upload required for SFTP as we use the overwrite flag

				using (Stream istream = new FileStream(localfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					try
					{
						LogFtpDebugMessage($"SFTP[{cycleStr}]: Uploading {localfile} to {remotefilename}");

						conn.OperationTimeout = TimeSpan.FromSeconds(15);
						conn.UploadFile(istream, remotefilename, true);
						istream.Close();

						LogFtpDebugMessage($"SFTP[{cycleStr}]: Uploaded {localfile}");
					}
					catch (ObjectDisposedException)
					{
						LogFtpMessage($"SFTP[{cycleStr}]: The SFTP object is disposed");
						return false;
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"SFTP[{cycleStr}]: Error uploading {localfile} to {remotefilename}", true);

						if (ex.Message.Contains("Permission denied")) // Non-fatal //TODO: Check error code rather than text
							return true;

						// Lets start again anyway! Too hard to tell if the error is recoverable
							conn.Dispose();
						return false;
					}
				}

				if (FTPRename)
				{
					// rename the file
					try
					{
						LogFtpDebugMessage($"SFTP[{cycleStr}]: Renaming {remotefilename} to {remotefile}");
						conn.RenameFile(remotefilename, remotefile, true);
						LogFtpDebugMessage($"SFTP[{cycleStr}]: Renamed {remotefilename}");
					}
					catch (ObjectDisposedException)
					{
						LogFtpMessage($"SFTP[{cycleStr}]: The SFTP object is disposed");
						return false;
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, $"SFTP[{cycleStr}]: Error renaming {remotefilename} to {remotefile}", true);

						return true;
					}
				}
				LogFtpDebugMessage($"SFTP[{cycleStr}]: Completed uploading {localfile} to {remotefile}");
			}
			catch (ObjectDisposedException)
			{
				LogFtpMessage($"SFTP[{cycleStr}]: The SFTP object is disposed");
				return false;
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, $"SFTP[{cycleStr}]: Error uploading {localfile} to {remotefile}", true);
			}
			return true;
		}

		public static void LogMessage(string message)
		{
			Trace.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message);
		}

		public void LogDebugMessage(string message)
		{
			if (ProgramOptions.DebugLogging || ProgramOptions.DataLogging)
			{
				LogMessage(message);
			}
		}

		public void LogDataMessage(string message)
		{
			if (ProgramOptions.DataLogging)
			{
				LogMessage(message);
			}
		}

		public void LogFtpMessage(string message)
		{
			LogMessage(message);
			if (FtpOptions.Logging)
			{
				FtpLoggerMX.LogInformation("CMX: " + message);
			}
		}

		public void LogFtpDebugMessage(string message)
		{
			if (FtpOptions.Logging)
			{
				LogDebugMessage(message);
				FtpLoggerMX.LogInformation("CMX: " + message);
			}
		}

		public static void LogConsoleMessage(string message, ConsoleColor colour = ConsoleColor.White, bool LogDateTime = false)
		{
			if (!Program.service)
			{
				if (LogDateTime)
				{
					message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + message;
				}

				Console.ForegroundColor = colour;
				Console.WriteLine(message);
				Console.ResetColor();
			}

			Program.svcTextListener.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message);
			Program.svcTextListener.Flush();
		}


		public void LogExceptionMessage(Exception ex, string preamble, bool ftpLog=false)
		{
			/*
			LogMessage($"{preamble} - {ex.Message}");
			if (ftpLog && FtpOptions.Logging)
				FtpTrace.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + $"{preamble} - {ex.Message}");

			LogDebugMessage("Exception = " + ex);

			var baseEx = ex.GetBaseException();

			if (baseEx != null && baseEx.Message != ex.Message)
			{
				LogMessage("Base Exception - " + baseEx.Message);
				if (ftpLog && FtpOptions.Logging)
					FtpTrace.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Base Exception - " + baseEx.Message);

				LogDebugMessage("Base Exception = " + baseEx);
			}
			*/

			LogMessage(preamble);

			if (ftpLog && FtpOptions.Logging)
			{
				FtpLoggerMX.LogCritical($"{preamble} - {ex.Message}");
			}

			LogMessage(Utils.ExceptionToString(ex));
		}


		public void RollOverDataLogs()
		{
			try
			{
				if (ProgramOptions.LogRawStationData && (RawDataStation == null || RawDataStation.disposed))
				{
					if (File.Exists(rawStationDataLogFile))
					{
						if (File.Exists(rawStationDataLogFile + ".2"))
							File.Move(rawStationDataLogFile + ".2", rawStationDataLogFile + ".3", true);

						if (File.Exists(rawStationDataLogFile + ".1"))
							File.Move(rawStationDataLogFile + ".1", rawStationDataLogFile + ".2", true);

						File.Move(rawStationDataLogFile, rawStationDataLogFile + ".1", true);
					}

					RawDataStation = new DataLogger(rawStationDataLogFile);
				}

				if (ProgramOptions.LogRawExtraData && (RawDataExtraLog == null || RawDataExtraLog.disposed))
				{
					if (File.Exists(rawExtraDataLogFile))
					{
						if (File.Exists(rawExtraDataLogFile + ".2"))
							File.Move(rawExtraDataLogFile + ".2", rawExtraDataLogFile + ".3", true);

						if (File.Exists(rawExtraDataLogFile + ".1"))
							File.Move(rawExtraDataLogFile + ".1", rawExtraDataLogFile + ".2", true);

						File.Move(rawExtraDataLogFile, rawExtraDataLogFile + ".1", true);
					}

					RawDataExtraLog = new DataLogger(rawExtraDataLogFile);
				}
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, "RollOverDataLogs");
			}
		}


		/*
		public string ReplaceCommas(string AStr)
		{
			return AStr.Replace(',', '.');
		}
		*/

		private async Task CreateRealtimeFile(int cycle)
		{
			/*
			Example: 18/10/08 16:03:45 8.4 84 5.8 24.2 33.0 261 0.0 1.0 999.7 W 6 mph C mb mm 146.6 +0.1 85.2 588.4 11.6 20.3 57 3.6 -0.7 10.9 12:00 7.8 14:41 37.4 14:38 44.0 14:28 999.8 16:01 998.4 12:06 1.8.2 448 36.0 10.3 10.5 0 9.3

			Field  Example    Description
			1      18/10/08   date (always dd/mm/yy)
			2      16:03:45   time (always hh:mm:ss)
			3      8.4        outside temperature
			4      84         relative humidity
			5      5.8        dewpoint
			6      24.2       wind speed (average)
			7      33.0       latest wind speed
			8      261        wind bearing
			9      0.0        current rain rate
			10     1.0        rain today
			11     999.7      barometer
			12     W          wind direction
			13     6          wind speed (Beaufort)
			14     mph        wind units
			15     C          temperature units
			16     mb         pressure units
			17     mm         rain units
			18     146.6      wind run (today)
			19     +0.1       pressure trend value
			20     85.2       monthly rain
			21     588.4      yearly rain
			22     11.6       yesterday's rainfall
			23     20.3       inside temperature
			24     57         inside humidity
			25     3.6        wind chill
			26     -0.7       temperature trend value
			27     10.9       today's high temp
			28     12:00      time of today's high temp (hh:mm)
			29     7.8        today's low temp
			30     14:41      time of today's low temp (hh:mm)
			31     37.4       today's high wind speed (average)
			32     14:38      time of today's high wind speed (average) (hh:mm)
			33     44.0       today's high wind gust
			34     14:28      time of today's high wind gust (hh:mm)
			35     999.8      today's high pressure
			36     16:01      time of today's high pressure (hh:mm)
			37     998.4      today's low pressure
			38     12:06      time of today's low pressure (hh:mm)
			39     1.8.2      Cumulus version
			40     448        Cumulus build
			41     36.0       10-minute high gust
			42     10.3       heat index
			43     10.5       humidex
			44                UV
			45                ET
			46                Solar radiation
			47     234        Average Bearing (degrees)
			48     2.5        Rain last hour
			49     5          Forecast number
			50     1          Is daylight? (1 = yes)
			51     0          Sensor contact lost (1 = yes)
			52     NNW        wind direction (average)
			53     2040       Cloud base
			54     ft         Cloud base units
			55     12.3       Apparent Temp
			56     11.4       Sunshine hours today
			57     420        Current theoretical max solar radiation
			58     1          Is sunny?
			59     8.4        Feels Like temperature
		  */

			// Does the user want to create the realtime.txt file?
			if (!RealtimeFiles[0].Create)
			{
				return;
			}

			var filename = AppDir + RealtimeFile;
			DateTime timestamp = DateTime.Now;

			try
			{
				LogDebugMessage($"Realtime[{cycle}]: Creating realtime.txt");
				var sb = new StringBuilder();
				sb.Append(timestamp.ToString("dd/MM/yy HH:mm:ss ", invDate));                   // 1, 2
				sb.Append((station.Temperature ?? 0).ToString(TempFormat, invNum) + ' ');       // 3
				sb.Append((station.Humidity ?? 0).ToString() + ' ');                            // 4
				sb.Append((station.Dewpoint ?? 0).ToString(TempFormat, invNum) + ' ');          // 5
				sb.Append((station.WindAverage ?? 0).ToString(WindAvgFormat, invNum) + ' ');    // 6
				sb.Append((station.WindLatest ?? 0).ToString(WindFormat, invNum) + ' ');        // 7
				sb.Append(station.Bearing.ToString() + ' ');                                    // 8
				sb.Append((station.RainRate ?? 0).ToString(RainFormat, invNum) + ' ');          // 9
				sb.Append((station.RainToday ?? 0).ToString(RainFormat, invNum) + ' ');         // 10
				sb.Append((station.Pressure ?? 0).ToString(PressFormat, invNum) + ' ');         // 11
				sb.Append(WeatherStation.CompassPoint(station.Bearing) + ' ');                         // 12
				sb.Append(Beaufort(station.WindAverage ?? 0) + ' ');                            // 13
				sb.Append(Units.WindText + ' ');                                                // 14
				sb.Append(Units.TempText[1].ToString() + ' ');                                  // 15
				sb.Append(Units.PressText + ' ');                                               // 16
				sb.Append(Units.RainText + ' ');                                                // 17
				sb.Append(station.WindRunToday.ToString(WindRunFormat, invNum) + ' ');          // 18
				if (station.presstrendval > 0)
					sb.Append('+' + station.presstrendval.ToString(PressFormat, invNum) + ' '); // 19
				else
					sb.Append(station.presstrendval.ToString(PressFormat, invNum) + ' ');
				sb.Append(station.RainMonth.ToString(RainFormat, invNum) + ' ');                // 20
				sb.Append(station.RainYear.ToString(RainFormat, invNum) + ' ');                 // 21
				sb.Append((station.RainYesterday ?? 0).ToString(RainFormat, invNum) + ' ');     // 22
				sb.Append((station.IndoorTemp ?? 0).ToString(TempFormat, invNum) + ' ');        // 23
				sb.Append((station.IndoorHum ?? 0).ToString() + ' ');                           // 24
				sb.Append((station.WindChill ?? 0).ToString(TempFormat, invNum) + ' ');         // 25
				sb.Append(station.temptrendval.ToString(TempTrendFormat, invNum) + ' ');        // 26
				sb.Append((station.HiLoToday.HighTemp ?? 0).ToString(TempFormat, invNum) + ' ');	// 27
				sb.Append(station.HiLoToday.HighTempTime.ToString("HH:mm ", invDate));          // 28
				sb.Append((station.HiLoToday.LowTemp ?? 0).ToString(TempFormat, invNum) + ' ');	// 29
				sb.Append(station.HiLoToday.LowTempTime.ToString("HH:mm ", invDate));           // 30
				sb.Append((station.HiLoToday.HighWind ?? 0).ToString(WindAvgFormat, invNum) + ' ');    // 31
				sb.Append(station.HiLoToday.HighWindTime.ToString("HH:mm ", invDate));          // 32
				sb.Append((station.HiLoToday.HighGust ?? 0).ToString(WindFormat, invNum) + ' ');// 33
				sb.Append(station.HiLoToday.HighGustTime.ToString("HH:mm ", invDate));          // 34
				sb.Append((station.HiLoToday.HighPress ?? 0).ToString(PressFormat, invNum) + ' ');     // 35
				sb.Append(station.HiLoToday.HighPressTime.ToString("HH:mm ", invDate));         // 36
				sb.Append((station.HiLoToday.LowPress ?? 0).ToString(PressFormat, invNum) + ' ');      // 37
				sb.Append(station.HiLoToday.LowPressTime.ToString("HH:mm ", invDate));          // 38
				sb.Append(Version + ' ');                                                       // 39
				sb.Append(Build + ' ');                                                         // 40
				sb.Append((station.RecentMaxGust ?? 0).ToString(WindFormat, invNum) + ' ');     // 41
				sb.Append((station.HeatIndex ?? 0).ToString(TempFormat, invNum) + ' ');         // 42
				sb.Append((station.Humidex ?? 0).ToString(TempFormat, invNum) + ' ');           // 43
				sb.Append((station.UV ?? 0).ToString(UVFormat, invNum) + ' ');                  // 44
				sb.Append(station.ET.ToString(ETFormat, invNum) + ' ');                         // 45
				sb.Append((station.SolarRad ?? 0).ToString() + ' ');                            // 46
				sb.Append(station.AvgBearing.ToString() + ' ');                                 // 47
				sb.Append(station.RainLastHour.ToString(RainFormat, invNum) + ' ');             // 48
				sb.Append(station.Forecastnumber.ToString() + ' ');                             // 49
				sb.Append(IsDaylight() ? "1 " : "0 ");                                          // 50
				sb.Append(station.SensorContactLost ? "1 " : "0 ");                             // 51
				sb.Append(WeatherStation.CompassPoint(station.AvgBearing) + ' ');                      // 52
				sb.Append((station.CloudBase ?? 0).ToString() + ' ');                           // 53
				sb.Append(CloudBaseInFeet ? "ft " : "m ");                                      // 54
				sb.Append((station.ApparentTemp ?? 0).ToString(TempFormat, invNum) + ' ');      // 55
				sb.Append(station.SunshineHours.ToString(SunFormat, invNum) + ' ');             // 56
				sb.Append((station.CurrentSolarMax ?? 0).ToString() + ' ');						// 57
				sb.Append(station.IsSunny ? "1 " : "0 ");                                       // 58
				sb.Append((station.FeelsLike ?? 0).ToString(TempFormat, invNum));               // 59

				using StreamWriter file = new StreamWriter(filename, false);
				await file.WriteLineAsync(sb.ToString());
				file.Close();
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, "Error encountered during Realtime file update.");
			}
		}


		private async Task ProcessTemplateFile(string template, string outputfile, TokenParser parser, bool useAppDir)
		{
			string templatefile = template;

			if (useAppDir)
			{
				templatefile = AppDir + template;
			}

			if (File.Exists(templatefile))
			{
				var utf8WithoutBom = new UTF8Encoding(false);
				var encoding = UTF8encode ? utf8WithoutBom : Encoding.GetEncoding("iso-8859-1");
				parser.Encoding = encoding;
				parser.SourceFile = template;
				var output = parser.ToString();

				try
				{
					using StreamWriter file = new StreamWriter(outputfile, false, encoding);
					await file.WriteAsync(output);
					file.Close();
				}
				catch (Exception e)
				{
					LogMessage($"ProcessTemplateFile: Error writing to file '{outputfile}', error was - {e}");
				}
			}
			else
			{
				LogMessage($"ProcessTemplateFile: Error, template file not found - {templatefile}");
			}
		}

		public void StartTimersAndSensors()
		{
			LogMessage("Start Extra Sensors");
			airLinkOut?.Start();
			airLinkIn?.Start();
			ecowittExtra?.Start();
			ambientExtra?.Start();

			LogMessage("Start Timers");
			// start the general one-minute timer
			LogMessage("Starting 1-minute timer");
			station.StartMinuteTimer();
			LogMessage($"Data logging interval = {DataLogInterval} ({logints[DataLogInterval]} mins)");


			if (RealtimeIntervalEnabled)
			{
				if (FtpOptions.RealtimeEnabled)
				{
					LogConsoleMessage("Connecting real time FTP");
					if (FtpOptions.FtpMode == FtpProtocols.SFTP)
					{
						RealtimeSSHLogin();
					}
					else
					{
						RealtimeFTPLogin();
					}
				}

				LogMessage("Starting Realtime timer, interval = " + RealtimeInterval / 1000 + " seconds");
			}
			else
			{
				LogMessage("Realtime not enabled");
			}

			RealtimeTimer.Enabled = RealtimeIntervalEnabled;

			MySqlStuff.CustomSecondsTimer.Enabled = MySqlStuff.Settings.CustomSecs.Enabled;

			CustomHttpSecondsTimer.Enabled = CustomHttpSecondsEnabled;

			if (Wund.RapidFireEnabled)
			{
				Wund.IntTimer.Interval = 5000; // 5 seconds in rapid-fire mode
			}
			else
			{
				Wund.IntTimer.Interval = Wund.Interval * 60 * 1000; // mins to millisecs
			}


			AWEKAS.IntTimer.Interval = AWEKAS.Interval * 1000;
			AWEKAS.IntTimer.Enabled = AWEKAS.Enabled && !AWEKAS.SynchronisedUpdate;


			MQTTTimer.Interval = MQTT.IntervalTime * 1000; // secs to millisecs
			if (MQTT.EnableInterval)
			{
				MQTTTimer.Enabled = true;
			}


			Wund.CatchUpIfRequired();

			Windy.CatchUpIfRequired();

			PWS.CatchUpIfRequired();

			WOW.CatchUpIfRequired();

			OpenWeatherMap.CatchUpIfRequired();


			if (MySqlStuff.CatchUpList.IsEmpty)
			{
				// No archived entries to upload
				LogDebugMessage("MySqlList is Empty");
			}
			else
			{
				// start the archive upload thread
				LogMessage($"Starting MySQL catchup thread. Found {MySqlStuff.CatchUpList.Count} commands to execute");
				_ = MySqlStuff.CommandAsync(MySqlStuff.CatchUpList, "MySQL Archive", true);
			}

			WebTimer.Interval = UpdateInterval * 60 * 1000; // mins to millisecs
			WebTimer.Enabled = WebIntervalEnabled && !SynchronisedWebUpdate;


			OpenWeatherMap.EnableOpenWeatherMap();

			NormalRunning = true;
			LogMessage("Normal running");
			LogConsoleMessage("Normal running", ConsoleColor.Green);
		}


		public async Task DoExtraEndOfDayFiles()
		{
			int i;

			// handle any extra files that only require EOD processing
			for (i = 0; i < numextrafiles; i++)
			{
				if (ExtraFiles[i].endofday)
				{
					var uploadfile = ExtraFiles[i].local;
					var remotefile = ExtraFiles[i].remote;

					if ((uploadfile.Length > 0) && (remotefile.Length > 0))
					{
						// For EOD files, we want the previous days log files since it is now just past the day roll-over time. Makes a difference on month roll-over
						var logDay = DateTime.Now.AddDays(-1);

						uploadfile = GetUploadFilename(uploadfile, logDay);

						if (File.Exists(uploadfile))
						{
							remotefile = GetRemoteFileName(remotefile, logDay);

							if (ExtraFiles[i].process)
							{
								LogDebugMessage("EOD: Processing extra file " + uploadfile);
								// process the file
								var utf8WithoutBom = new UTF8Encoding(false);
								var encoding = UTF8encode ? utf8WithoutBom : Encoding.GetEncoding("iso-8859-1");
								tokenParser.Encoding = encoding;
								tokenParser.SourceFile = uploadfile;
								var output = tokenParser.ToString();
								uploadfile += "tmp";
								try
								{
									using StreamWriter file = new StreamWriter(uploadfile, false, encoding);
									await file.WriteAsync(output);
									file.Close();
								}
								catch (Exception ex)
								{
									LogExceptionMessage(ex, "EOD: Error writing file " + uploadfile);
								}
								//LogDebugMessage("Finished processing extra file " + uploadfile);
							}

							if (ExtraFiles[i].FTP)
							{
								// FTP the file at the next interval
								EODfilesNeedFTP = true;
							}
							else
							{
								// just copy the file
								LogDebugMessage($"EOD: Copying extra file {uploadfile} to {remotefile}");
								try
								{
									await Utils.CopyFileAsync(uploadfile, remotefile);
								}
								catch (Exception ex)
								{
									LogExceptionMessage(ex, "EOD: Error copying extra file: ");
								}
								//LogDebugMessage("Finished copying extra file " + uploadfile);
							}
						}
					}
				}
			}
		}

		public void RealtimeFTPDisconnect()
		{
			try
			{
				if (FtpOptions.FtpMode == FtpProtocols.SFTP && RealtimeSSH != null)
				{
					RealtimeSSH.Disconnect();
				}
				else if (RealtimeFTP != null)
				{
					RealtimeFTP.Disconnect();
					RealtimeFTP.Dispose();
				}
				LogDebugMessage("Disconnected Realtime FTP session");
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, "RealtimeFTPDisconnect: Error disconnecting connection (can be ignored?)");
			}
		}

		private void RealtimeFTPLogin()
		{
			// dispose of the previous FTP client
			if (RealtimeFTP != null && !RealtimeFTP.IsDisposed)
			{
				RealtimeFTP.Dispose();
			}

			RealtimeFTP = new FtpClient();

			//RealtimeTimer.Enabled = false;
			RealtimeFTP.Host = FtpOptions.Hostname;
			RealtimeFTP.Port = FtpOptions.Port;
			RealtimeFTP.Credentials = new NetworkCredential(FtpOptions.Username, FtpOptions.Password);
			RealtimeFTP.Config.SocketPollInterval = 20000; // increase beyond the timeout values
			RealtimeFTP.Config.LogPassword = false;

			SetFtpLogging(FtpOptions.Logging);

			if (!FtpOptions.AutoDetect)
			{
				if (FtpOptions.FtpMode == FtpProtocols.FTPS)
				{
					RealtimeFTP.Config.EncryptionMode = FtpOptions.DisableExplicit ? FtpEncryptionMode.Implicit : FtpEncryptionMode.Explicit;
					RealtimeFTP.Config.DataConnectionEncryption = true;
					// b3045 - switch from System.Net.Ftp.Client to FluentFTP allows us to specify protocols
					// b3155 - switch to default again - this will use the highest version available in the OS
					//RealtimeFTP.SslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12;
					LogDebugMessage($"RealtimeFTPLogin: Using FTPS protocol");
				}

				if (FtpOptions.ActiveMode)
				{
					RealtimeFTP.Config.DataConnectionType = FtpDataConnectionType.PORT;
					LogDebugMessage("RealtimeFTPLogin: Using Active FTP mode");
				}
				else if (FtpOptions.DisableEPSV)
				{
					RealtimeFTP.Config.DataConnectionType = FtpDataConnectionType.PASV;
					LogDebugMessage("RealtimeFTPLogin: Disabling EPSV mode");
				}
				else
				{
					RealtimeFTP.Config.DataConnectionType = FtpDataConnectionType.EPSV;
				}
			}

			if (FtpOptions.FtpMode == FtpProtocols.FTPS)
			{
				RealtimeFTP.Config.ValidateAnyCertificate = FtpOptions.IgnoreCertErrors;
			}

			if (FtpOptions.Enabled)
			{
				LogMessage($"RealtimeFTPLogin: Attempting realtime FTP connect to host {FtpOptions.Hostname} on port {FtpOptions.Port}");
				try
				{
					if (FtpOptions.AutoDetect)
					{
						RealtimeFTP.AutoConnect();
					}
					else
					{
						RealtimeFTP.Connect();
					}

					LogMessage("RealtimeFTPLogin: Realtime FTP connected");
					RealtimeFTP.Config.SocketKeepAlive = true;
				}
				catch (Exception ex)
				{
					LogExceptionMessage(ex, $"RealtimeFTPLogin: Error connecting ftp");
					RealtimeFTP.Disconnect();
				}
			}
			//RealtimeTimer.Enabled = true;
		}

		private void RealtimeSSHLogin()
		{
			if (FtpOptions.Enabled)
			{
				LogMessage($"RealtimeSSHLogin: Attempting realtime SFTP connect to host {FtpOptions.Hostname} on port {FtpOptions.Port}");
				try
				{
					// BUILD 3092 - added alternate SFTP authentication options
					ConnectionInfo connectionInfo;
					PrivateKeyFile pskFile;
					if (FtpOptions.SshAuthen == "password")
					{
						connectionInfo = new ConnectionInfo(FtpOptions.Hostname, FtpOptions.Port, FtpOptions.Username, new PasswordAuthenticationMethod(FtpOptions.Username, FtpOptions.Password));
						LogDebugMessage("RealtimeSSHLogin: Connecting using password authentication");
					}
					else if (FtpOptions.SshAuthen == "psk")
					{
						if (File.Exists(FtpOptions.SshPskFile))
						{
							pskFile = new PrivateKeyFile(FtpOptions.SshPskFile);
							connectionInfo = new ConnectionInfo(FtpOptions.Hostname, FtpOptions.Port, FtpOptions.Username, new PrivateKeyAuthenticationMethod(FtpOptions.Username, pskFile));
							LogDebugMessage("RealtimeSSHLogin: Connecting using PSK authentication");
						}
						else
						{
							LogMessage($"RealtimeSSHLogin: Error: Could not find your PSK key file: {FtpOptions.SshPskFile}");
							return;
						}
					}
					else if (FtpOptions.SshAuthen == "password_psk")
					{
						if (File.Exists(FtpOptions.SshPskFile))
						{
							pskFile = new PrivateKeyFile(FtpOptions.SshPskFile);
							connectionInfo = new ConnectionInfo(FtpOptions.Hostname, FtpOptions.Port, FtpOptions.Username, new PasswordAuthenticationMethod(FtpOptions.Username, FtpOptions.Password), new PrivateKeyAuthenticationMethod(FtpOptions.Username, pskFile));
							LogDebugMessage("RealtimeSSHLogin: Connecting using password or PSK authentication");
						}
						else
						{
							LogMessage($"RealtimeSSHLogin: Error: Could not find your PSK key file: {FtpOptions.SshPskFile}");
							return;
						}
					}
					else
					{
						LogMessage($"RealtimeSSHLogin: Invalid SshftpAuthentication specified [{FtpOptions.SshAuthen}]");
						return;
					}

					RealtimeSSH = new SftpClient(connectionInfo);

					//if (RealtimeSSH != null) RealtimeSSH.Dispose();
					//RealtimeSSH = new SftpClient(ftp_host, ftp_port, ftp_user, ftp_password);

					RealtimeSSH.Connect();
					RealtimeSSH.ConnectionInfo.Timeout = TimeSpan.FromSeconds(15);  // 15 seconds to match FTP default timeout
					RealtimeSSH.KeepAliveInterval = new TimeSpan(0, 0, 31);         // 31 second keep-alive
					LogMessage("RealtimeSSHLogin: Realtime SFTP connected");
				}
				catch (Exception ex)
				{
					LogExceptionMessage(ex, "RealtimeSSHLogin: Error connecting SFTP");
				}
			}
		}


		public async Task GetLatestVersion()
		{
			var http = new HttpClient();
			// Let this default to highest available version in the OS
			//ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
			try
			{
				var retVal = await http.GetAsync("https://github.com/cumulusmx/CumulusMX/releases/latest");
				var latestUri = retVal.RequestMessage.RequestUri.AbsolutePath;
				LatestBuild = new string(latestUri.Split('/').Last().Where(char.IsDigit).ToArray());
				if (int.Parse(Build) < int.Parse(LatestBuild))
				{
					var msg = $"You are not running the latest version of Cumulus MX, build {LatestBuild} is available.";
					LogConsoleMessage(msg, ConsoleColor.Cyan);
					LogMessage(msg);
					UpgradeAlarm.LastError = $"Build {LatestBuild} is available";
					UpgradeAlarm.Triggered = true;
				}
				else if (int.Parse(Build) == int.Parse(LatestBuild))
				{
					LogMessage("This Cumulus MX instance is running the latest version");
					UpgradeAlarm.Triggered = false;
				}
				else if (int.Parse(Build) > int.Parse(LatestBuild))
				{
					LogMessage($"This Cumulus MX instance appears to be running a beta/test version. This build = {Build}, latest released build = {LatestBuild}");
				}
				else
				{
					LogMessage($"Could not determine if you are running the latest Cumulus MX build or not. This build = {Build}, latest build = {LatestBuild}");
				}
			}
			catch (Exception ex)
			{
				LogExceptionMessage(ex, "Failed to get the latest build version from GitHub");
			}
		}

		public async Task CustomHttpSecondsUpdate()
		{
			if (!updatingCustomHttpSeconds)
			{
				updatingCustomHttpSeconds = true;

				for (var i = 0; i < 10; i++)
				{
					try
					{
						if (!string.IsNullOrEmpty(CustomHttpSecondsStrings[i]))
						{
							customHttpSecondsTokenParser.InputText = CustomHttpSecondsStrings[i];
							var processedString = customHttpSecondsTokenParser.ToStringFromString();
							LogDebugMessage($"CustomHttpSeconds[{i}]: Querying - {processedString}");
							var response = await customHttpSecondsClient.GetAsync(processedString);
							response.EnsureSuccessStatusCode();
							var responseBodyAsText = await response.Content.ReadAsStringAsync();
							LogDebugMessage($"CustomHttpSeconds[{i}]: Response - {response.StatusCode}");
							LogDataMessage($"CustomHttpSeconds[{i}]: Response Text - {responseBodyAsText}");
						}
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, "CustomHttpSeconds");
					}
				}

				updatingCustomHttpSeconds = false;
			}
			else
			{
				LogDebugMessage("CustomHttpSeconds: Query already in progress, skipping this attempt");
			}
		}

		public async Task CustomHttpMinutesUpdate()
		{
			if (!updatingCustomHttpMinutes)
			{
				updatingCustomHttpMinutes = true;

				for (var i = 0; i < 10; i++)
				{
					try
					{
						customHttpMinutesTokenParser.InputText = CustomHttpMinutesStrings[i];
						var processedString = customHttpMinutesTokenParser.ToStringFromString();
						LogDebugMessage($"CustomHttpMinutes[{i}]: Querying - {processedString}");
						var response = await customHttpMinutesClient.GetAsync(processedString);
						var responseBodyAsText = await response.Content.ReadAsStringAsync();
						LogDebugMessage($"CustomHttpMinutes[{i}]: Response code - {response.StatusCode}");
						LogDataMessage($"CustomHttpMinutes[{i}]: Response text - {responseBodyAsText}");
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, "CustomHttpMinutes");
					}
				}

				updatingCustomHttpMinutes = false;
			}
		}

		public async Task CustomHttpRolloverUpdate()
		{
			if (!updatingCustomHttpRollover)
			{
				updatingCustomHttpRollover = true;

				for (var i = 0; i < 10; i++)
				{
					try
					{
						customHttpRolloverTokenParser.InputText = CustomHttpRolloverStrings[i];
						var processedString = customHttpRolloverTokenParser.ToStringFromString();
						LogDebugMessage($"CustomHttpRollover[{i}]: Querying - {processedString}");
						var response = await customHttpRolloverClient.GetAsync(processedString);
						var responseBodyAsText = await response.Content.ReadAsStringAsync();
						LogDebugMessage($"CustomHttpRollover[{i}]: Response code - {response.StatusCode}");
						LogDataMessage($"CustomHttpRollover[{i}]: Response text - {responseBodyAsText}");
					}
					catch (Exception ex)
					{
						LogExceptionMessage(ex, "CustomHttpRollover");
					}
				}

				updatingCustomHttpRollover = false;
			}
		}

		public void AddToWebServiceLists(DateTime timestamp)
		{
			Wund.AddToList(timestamp);
			Windy.AddToList(timestamp);
			PWS.AddToList(timestamp);
			WOW.AddToList(timestamp);
			OpenWeatherMap.AddToList(timestamp);
		}


		private string GetUploadFilename(string input, DateTime dat)
		{
			if (input == "<currentlogfile>")
			{
				return GetLogFileName(dat);
			}
			else if (input == "<currentextralogfile>")
			{
				return GetExtraLogFileName(dat);
			}
			else if (input == "<airlinklogfile>")
			{
				return GetAirLinkLogFileName(dat);
			}
			else if (input == "<noaayearfile>")
			{
				NOAAReports noaa = new NOAAReports(this, station);
				return noaa.GetLastNoaaYearReportFilename(dat, true);
			}
			else if (input == "<noaamonthfile>")
			{
				NOAAReports noaa = new NOAAReports(this, station);
				return noaa.GetLastNoaaMonthReportFilename(dat, true);
			}

			return input;
		}

		private string GetRemoteFileName(string input, DateTime dat)
		{
			if (input.Contains("<currentlogfile>"))
			{
				return input.Replace("<currentlogfile>", Path.GetFileName(GetLogFileName(dat)));
			}
			else if (input.Contains("<currentextralogfile>"))
			{
				return input.Replace("<currentextralogfile>", Path.GetFileName(GetExtraLogFileName(dat)));
			}
			else if (input.Contains("<airlinklogfile>"))
			{
				return input.Replace("<airlinklogfile>", Path.GetFileName(GetAirLinkLogFileName(dat)));
			}
			else if (input.Contains("<noaayearfile>"))
			{
				NOAAReports noaa = new NOAAReports(this, station);
				return input.Replace("<noaayearfile>", Path.GetFileName(noaa.GetLastNoaaYearReportFilename(dat, false)));
			}
			else if (input.Contains("<noaamonthfile>"))
			{
				NOAAReports noaa = new NOAAReports(this, station);
				return input.Replace("<noaamonthfile>", Path.GetFileName(noaa.GetLastNoaaMonthReportFilename(dat, false)));
			}

			return input;
		}

		public void LogOffsetsMultipliers()
		{
			LogMessage("Offsets and Multipliers:");
			LogMessage($"PO={Calib.Press.Offset:F3} TO={Calib.Temp.Offset:F3} HO={Calib.Hum.Offset} WDO={Calib.WindDir.Offset} SO={Calib.Solar.Offset:F3} UVO={Calib.UV.Offset:F3} ITO={Calib.InTemp.Offset} IHO={Calib.InHum.Offset}");
			LogMessage($"PM={Calib.Press.Mult:F3} WSM={Calib.WindSpeed.Mult:F3} WGM={Calib.WindGust.Mult:F3} TM={Calib.Temp.Mult:F3} TM2={Calib.Temp.Mult2:F3} " +
						$"HM={Calib.Hum.Mult:F3} HM2={Calib.Hum.Mult2:F3} RM={Calib.Rain.Mult:F3} SM={Calib.Solar.Mult:F3} UVM={Calib.UV.Mult:F3} ITM={Calib.InTemp.Mult} IHM={Calib.InHum.Mult}");
			LogMessage("Spike removal:");
			LogMessage($"TD={Spike.TempDiff:F3} GD={Spike.GustDiff:F3} WD={Spike.WindDiff:F3} HD={Spike.HumidityDiff:F3} PD={Spike.PressDiff:F3} MR={Spike.MaxRainRate:F3} MH={Spike.MaxHourlyRain:F3} ITD={Spike.InTempDiff:F3} IHD={Spike.InHumDiff:F3}");
			LogMessage("Limits:");
			LogMessage($"TH={Limit.TempHigh.ToString(TempFormat)} TL={Limit.TempLow.ToString(TempFormat)} DH={Limit.DewHigh.ToString(TempFormat)} PH={Limit.PressHigh.ToString(PressFormat)} PL={Limit.PressLow.ToString(PressFormat)} GH={Limit.WindHigh:F3}");
		}

		private void LogPrimaryAqSensor()
		{
			switch (StationOptions.PrimaryAqSensor)
			{
				case (int)PrimaryAqSensor.Undefined:
					LogMessage("Primary AQ Sensor = Undefined");
					break;
				case (int)PrimaryAqSensor.Ecowitt1:
				case (int)PrimaryAqSensor.Ecowitt2:
				case (int)PrimaryAqSensor.Ecowitt3:
				case (int)PrimaryAqSensor.Ecowitt4:
					LogMessage("Primary AQ Sensor = Ecowitt" + StationOptions.PrimaryAqSensor);
					break;
				case (int)PrimaryAqSensor.EcowittCO2:
					LogMessage("Primary AQ Sensor = Ecowitt CO2");
					break;
				case (int)PrimaryAqSensor.AirLinkIndoor:
					LogMessage("Primary AQ Sensor = AirLink Indoor");
					break;
				case (int)PrimaryAqSensor.AirLinkOutdoor:
					LogMessage("Primary AQ Sensor = AirLink Outdoor");
					break;
			}
		}

		private void CreateRequiredFolders()
		{
			// The required folders are: /backup, /data, /Reports
			var folders = new string[4] { "backup", "backup/daily", "datav4", "Reports"};

			LogMessage("Checking required folders");

			foreach (var folder in folders)
			{
				try
				{
					if (!Directory.Exists(folder))
					{
						LogMessage("Creating required folder: /" + folder);
						Directory.CreateDirectory(folder);
					}
				}
				catch (UnauthorizedAccessException)
				{
					var msg = "Error, no permission to read/create folder: " + folder;
					LogConsoleMessage(msg, ConsoleColor.Red);
					LogErrorMessage(msg);
				}
				catch (Exception ex)
				{
					var msg = $"Error while attempting to read/create folder: {folder}, error message: {ex.Message}";
					LogConsoleMessage(msg, ConsoleColor.Red);
					LogExceptionMessage(ex, $"Error while attempting to read/create folder: {folder}");
				}
			}
		}

		public void SetupFtpLogging()
		{
			if (loggerFactory != null)
				loggerFactory.Dispose();

			loggerFactory = new LoggerFactory();
			var fileLoggerOptions = new FileLoggerOptions()
			{
				Append = true,
				FileSizeLimitBytes = 5242880,
				MaxRollingFiles = 3,
				MinLevel = (LogLevel)FtpOptions.LoggingLevel,
				FormatLogEntry = (msg) =>
				{
					var logBuilder = new StringBuilder();
					if (!string.IsNullOrEmpty(msg.Message))
					{
						var loglevel = "";
						switch (msg.LogLevel)
						{
							case LogLevel.Trace:
								loglevel = "TRCE";
								break;
							case LogLevel.Debug:
								loglevel = "DBUG";
								break;
							case LogLevel.Information:
								loglevel = "INFO";
								break;
							case LogLevel.Warning:
								loglevel = "WARN";
								break;
							case LogLevel.Error:
								loglevel = "FAIL";
								break;
							case LogLevel.Critical:
								loglevel = "CRIT";
								break;
						}
						DateTime timeStamp = DateTime.Now;
						logBuilder.Append(timeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
						logBuilder.Append('\t');
						logBuilder.Append(loglevel);
						logBuilder.Append("\t[");
						logBuilder.Append(msg.LogName);
						logBuilder.Append("]");
						//logBuilder.Append("\t[");
						//logBuilder.Append(ms.EventId.Id);
						//logBuilder.Append("]\t");
						logBuilder.Append("\t");
						logBuilder.Append(msg.Message);
					}
					return logBuilder.ToString();
				}
			};
			var fileLogger = new FileLoggerProvider("MXdiags" + Path.DirectorySeparatorChar + "ftp.log", fileLoggerOptions);
			loggerFactory.AddProvider(fileLogger);
			FtpLoggerRT = loggerFactory.CreateLogger("R-T");
			FtpLoggerIN = loggerFactory.CreateLogger("INT");
			FtpLoggerMX = loggerFactory.CreateLogger("CMX");
		}
	}

	/*
	internal class Raintotaldata
	{
		public DateTime timestamp;
		public double raintotal;

		public Raintotaldata(DateTime ts, double rain)
		{
			timestamp = ts;
			raintotal = rain;
		}
	}
	*/

	public static class StationTypes
	{
		public const int Undefined = -1;
		public const int VantagePro = 0;
		public const int VantagePro2 = 1;
		public const int WMR928 = 2;
		public const int WM918 = 3;
		public const int EasyWeather = 4;
		public const int FineOffset = 5;
		public const int WS2300 = 6;
		public const int FineOffsetSolar = 7;
		public const int WMR100 = 8;
		public const int WMR200 = 9;
		public const int Instromet = 10;
		public const int WLL = 11;
		public const int GW1000 = 12;
		public const int HttpWund = 13;
		public const int HttpEcowitt = 14;
		public const int HttpAmbient = 15;
		public const int Tempest = 16;
		public const int Simulator = 17;
	}

	/*
	public static class AirQualityIndex
	{
		public const int US_EPA = 0;
		public const int UK_COMEAP = 1;
		public const int EU_AQI = 2;
		public const int CANADA_AQHI = 3;
		public const int EU_CAQI = 4;
	}
	*/

	/*
	public static class DoubleExtensions
	{
		public static string ToUKString(this double value)
		{
			return value.ToString(CultureInfo.GetCultureInfo("en-GB"));
		}
	}
	*/

	public class ProgramOptionsClass
	{
		public bool EnableAccessibility { get; set; }
		public string StartupPingHost { get; set; }
		public int StartupPingEscapeTime { get; set; }
		public int StartupDelaySecs { get; set; }
		public int StartupDelayMaxUptime { get; set; }
		public string StartupTask { get; set; }
		public string StartupTaskParams { get; set; }
		public bool StartupTaskWait { get; set; }
		public string ShutdownTask { get; set; }
		public string ShutdownTaskParams { get; set; }
		public bool DebugLogging { get; set; }
		public bool DataLogging { get; set; }
		public bool WarnMultiple { get; set; }
		public bool ListWebTags { get; set; }
		public CultureConfig Culture { get; set; }
		public bool DataStoppedExit { get; set; }
		public int DataStoppedMins { get; set; }
		public bool EncryptedCreds { get; set; }
		public bool UpdateDayfile { get; set; }
		public bool UpdateLogfile { get; set; }
		public bool LogRawStationData { get; set; }
		public bool LogRawExtraData { get; set; }
		public bool DisplayPasswords { get; set; }
	}

	public class CultureConfig
	{
		public bool RemoveSpaceFromDateSeparator { get; set; }
	}

	public class StationUnits
	{
		/// <value> 0=m/s, 1=mph, 2=km/h, 3=knots</value>
		public int Wind { get; set; }
		/// <value> 0=mb, 1=hPa, 2=inHg </value>
		public int Press { get; set; }
		/// <value> 0=mm, 1=in </value>
		public int Rain { get; set; }
		/// <value> 0=C, 1=F </value>
		public int Temp { get; set; }

		public string WindText { get; set; }
		public string PressText { get; set; }
		public string RainText { get; set; }
		public string TempText { get; set; }

		public string TempTrendText { get; set; }
		public string RainTrendText { get; set; }
		public string PressTrendText { get; set; }
		public string WindRunText { get; set; }
		public string AirQualityUnitText { get; set; }
		public string SoilMoistureUnitText { get; set; }
		public string CO2UnitText { get; set; }
		public string LeafWetnessUnitText { get; set; }

		public StationUnits()
		{
			AirQualityUnitText = "µg/m³";
			SoilMoistureUnitText = "cb";
			CO2UnitText = "ppm";
			LeafWetnessUnitText = "";  // Davis is unitless, Ecowitt uses %
		}
	}

	public class StationOptions
	{
		public bool UseZeroBearing { get; set; }
		public bool CalcWind10MinAve { get; set; }
		public bool UseSpeedForAvgCalc { get; set; }
		public bool UseSpeedForLatest { get; set; }
		public bool Humidity98Fix { get; set; }
		public bool CalculatedDP { get; set; }
		public bool CalculatedWC { get; set; }
		public bool CalculatedET { get; set; }
		public bool SyncTime { get; set; }
		public int ClockSettingHour { get; set; }
		public bool UseCumulusPresstrendstr { get; set; }
		public bool LogMainStation { get; set; }
		public bool LogExtraSensors { get; set; }
		public bool WS2300IgnoreStationClock { get; set; }
		public bool RoundWindSpeed { get; set; }
		public int PrimaryAqSensor { get; set; }
		public bool NoSensorCheck { get; set; }
		public int AvgBearingMinutes { get; set; }
		public int AvgSpeedMinutes { get; set; }
		public int PeakGustMinutes { get; set; }
		public double AnemometerHeightM { get; set; }
		public bool UseRainForIsRaining { get; set; }
		public int LeafWetnessIsRainingIdx { get; set; }
		public double LeafWetnessIsRainingThrsh { get; set; }
		public string TimeZone { get; set; }
	}

	public class FtpOptionsClass
	{
		public bool Enabled { get; set; }
		public string Hostname { get; set; }
		public int Port { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Directory { get; set; }
		public bool IntervalEnabled { get; set; }
		public bool RealtimeEnabled { get; set; }
		/// <value>0=FTP, 1=FTPS, 3=SFTP</value>
		public Cumulus.FtpProtocols FtpMode { get; set; }
		/// <value>Valid options: password, psk, password_psk</value>
		public bool AutoDetect { get; set; }
		public string SshAuthen { get; set; }
		public string SshPskFile { get; set; }
		public bool Logging { get; set; }
		public int LoggingLevel { get; set; }
		public bool Utf8Encode { get; set; }
		public bool ActiveMode { get; set; }
		public bool DisableEPSV { get; set; }
		public bool DisableExplicit { get; set; }
		public bool IgnoreCertErrors { get; set; }

		public bool LocalCopyEnabled { get; set; }
		public string LocalCopyFolder { get; set; }
	}

	public class FileGenerationFtpOptions
	{
		public string TemplateFileName { get; set; }
		public string LocalFileName { get; set; }
		public string LocalPath { get; set; }
		public string RemoteFileName { get; set; }
		public bool Create { get; set; }
		public bool FTP { get; set; }
		public bool Copy { get; set; }
		public bool FtpRequired { get; set; }
		public bool CopyRequired { get; set; }
		public bool CreateRequired { get; set; }
		public FileGenerationFtpOptions()
		{
			CreateRequired = true;
			FtpRequired = true;
			CopyRequired = true;
		}
	}

	public class MoonImageOptionsClass
	{
		public bool Enabled { get; set; }
		public int Size { get; set; }
		public bool Transparent { get; set; }
		public bool Ftp { get; set; }
		public string FtpDest { get; set; }
		public bool Copy { get; set; }
		public string CopyDest { get; set; }
		public bool ReadyToFtp { get; set; }
		public bool ReadyToCopy { get; set; }
	}

	public class DavisOptions
	{
		public bool ForceVPBarUpdate { get; set; }
		public bool ReadReceptionStats { get; set; }
		public bool SetLoggerInterval { get; set; }
		public bool UseLoop2 { get; set; }
		public int InitWaitTime { get; set; }
		public int IPResponseTime { get; set; }
		public int ReadTimeout { get; set; }
		public bool IncrementPressureDP { get; set; }
		public int BaudRate { get; set; }
		public int RainGaugeType { get; set; }
		public int ConnectionType { get; set; }
		public int TCPPort { get; set; }
		public string IPAddr { get; set; }
		public int PeriodicDisconnectInterval { get; set; }
	}

	public class WeatherFlowOptions
	{
		public int WFDeviceId { get; set; }
		public int WFTcpPort { get; set; }
		public string WFToken { get; set; }
		public int WFDaysHist { get; set; }

	}

	public class FineOffsetOptions
	{
		public bool SyncReads { get; set; }
		public int ReadAvoidPeriod { get; set; }
		public int ReadTime { get; set; }
		public bool SetLoggerInterval { get; set; }
		public int VendorID { get; set; }
		public int ProductID { get; set; }
}

	public class ImetOptions
	{
		public List<int> BaudRates { get; set; }
		public int BaudRate { get; set; }
		/// <value>Delay to wait for a reply to a command (ms)</value>
		public int WaitTime { get; set; }
		/// <value>Delay between sending read live data commands (ms)</value>
		public int ReadDelay { get; set; }
		/// <value>Keep the logger pointer pointing at last data read</value>
		public bool UpdateLogPointer { get; set; }
	}

	public class EasyWeatherOptions
	{
		public double Interval { get; set; }
		public string Filename { get; set; }
		public int MinPressMB { get; set; }
		public int MaxPressMB { get; set; }
		public int MaxRainTipDiff { get; set; }
		public double PressOffset { get; set; }
	}

	public class SolarOptions
	{
		public int SunThreshold { get; set; }
		public int SolarMinimum { get; set; }
		public double LuxToWM2 { get; set; }
		public bool UseBlakeLarsen { get; set; }
		public int SolarCalc { get; set; }
		public double RStransfactorJun { get; set; }
		public double RStransfactorDec { get; set; }
		public double BrasTurbidityJun { get; set; }
		public double BrasTurbidityDec { get; set; }
	}

	public class GraphOptions
	{
		public bool TempVisible { get; set; }
		public bool InTempVisible { get; set; }
		public bool HIVisible { get; set; }
		public bool DPVisible { get; set; }
		public bool WCVisible { get; set; }
		public bool AppTempVisible { get; set; }
		public bool FeelsLikeVisible { get; set; }
		public bool HumidexVisible { get; set; }
		public bool InHumVisible { get; set; }
		public bool OutHumVisible { get; set; }
		public bool UVVisible { get; set; }
		public bool SolarVisible { get; set; }
		public bool SunshineVisible { get; set; }
		public bool DailyMaxTempVisible { get; set; }
		public bool DailyAvgTempVisible { get; set; }
		public bool DailyMinTempVisible { get; set; }
		public bool GrowingDegreeDaysVisible1 { get; set; }
		public bool GrowingDegreeDaysVisible2 { get; set; }
		public bool TempSumVisible0 { get; set; }
		public bool TempSumVisible1 { get; set; }
		public bool TempSumVisible2 { get; set; }
		public bool[] ExtraTempVisible = new bool[10];
		public bool[] ExtraHumVisible = new bool[10];
		public bool[] SoilTempVisible = new bool[16];
		public bool[] SoilMoistVisible = new bool[16];
		public bool[] UserTempVisible = new bool[8];
	}

	public class SelectaChartOptions
	{
		public string[] series { get; set; }
		public string[] colours { get; set; }

		public SelectaChartOptions()
		{
			series = new string[6];
			colours = new string[6];
		}
	}





	public class DisplayOptions
	{
		public bool UseApparent { get; set; }
		public bool ShowSolar { get; set; }
		public bool ShowUV { get; set; }
	}

	public class ExtraDataLogOptions
	{
		public bool Temperature { get; set; }
		public bool Humidity { get; set; }
		public bool Dewpoint { get; set; }
		public bool UserTemp { get; set; }
		public bool SoilTemp { get; set; }
		public bool SoilMoisture { get; set; }
		public bool LeafTemp { get; set; }
		public bool LeafWetness { get; set; }
		public bool AirQual { get; set; }
		public bool CO2 { get; set; }
	}

	public class AlarmEmails
	{
		public string Preamble { get; set; }
		public string HighGust { get; set; }
		public string HighWind { get; set; }
		public string HighTemp { get; set; }
		public string LowTemp { get; set; }
		public string TempDown { get; set; }
		public string TempUp { get; set; }
		public string HighPress { get; set; }
		public string LowPress { get; set; }
		public string PressDown { get; set; }
		public string PressUp { get; set; }
		public string Rain { get; set; }
		public string RainRate { get; set; }
		public string SensorLost { get; set; }
		public string DataStopped { get; set; }
		public string BatteryLow { get; set; }
		public string DataSpike { get; set; }
		public string Upgrade { get; set; }
	}

	public class MqttTemplate
	{
		public List<MqttTemplateMember> topics { get; set; }
	}

	public class MqttTemplateMember
	{
		public string topic { get; set; }
		public string data { get; set; }
		public bool retain { get; set; }
	}

	public class MySqlGeneralSettings
	{
		public bool UpdateOnEdit { get; set; }
		public bool BufferOnfailure { get; set; }
		public string RealtimeRetention { get; set; }
		public bool RealtimeLimit1Minute { get; set; }
		public MySqlTableSettings Realtime { get; set; }
		public MySqlTableSettings Monthly { get; set; }
		public MySqlTableSettings Dayfile { get; set; }
		public MySqlTableSettings CustomSecs { get; set; }
		public MySqlTableSettings CustomMins { get; set; }
		public MySqlTableSettings CustomRollover { get; set; }

		public MySqlGeneralSettings()
		{
			Realtime = new MySqlTableSettings();
			Monthly = new MySqlTableSettings();
			Dayfile = new MySqlTableSettings();
			CustomSecs = new MySqlTableSettings();
			CustomMins = new MySqlTableSettings();
			CustomRollover = new MySqlTableSettings();

			CustomSecs.Commands = new string[10];
			CustomMins.Commands = new string[10];
			CustomRollover.Commands = new string[10];
		}
	}

	public class CustomLogSettings
	{
		public bool Enabled { get; set; }
		public string FileName { get; set; }
		public string ContentString { get; set; }
		public int Interval { get; set; }
		public int IntervalIdx { get; set; }
	}

	public class MySqlTableSettings
	{
		public bool Enabled { get; set; }
		public string TableName { get; set; }
		public string[] Commands { get; set; }
		public int Interval { get; set; }
	}
}
