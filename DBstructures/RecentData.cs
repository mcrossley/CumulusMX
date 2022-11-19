using System;
using ServiceStack.Text;
using SQLite;

namespace CumulusMX
{
	internal class RecentData
	{
		private DateTime _time;
		private long _timestamp;

		[Ignore]
		public DateTime Time
		{
			get { return _time; }
			set
			{
				if (value.Kind == DateTimeKind.Unspecified)
					_time = DateTime.SpecifyKind(value, DateTimeKind.Local);
				else
					_time = value;
				Timestamp = value.ToUnixTime();
			}
		}

		[PrimaryKey]
		public long Timestamp
		{
			get { return _timestamp; }
			set
			{
				_timestamp = value;
				_time = value.FromUnixTime().ToLocalTime();
			}
		}

		public double? WindSpeed { get; set; }
		public double? WindGust { get; set; }
		public double? WindLatest { get; set; }
		public int? WindDir { get; set; }
		public int? WindAvgDir { get; set; }
		public double? OutsideTemp { get; set; }
		public double? WindChill { get; set; }
		public double? DewPoint { get; set; }
		public double? HeatIndex { get; set; }
		public int? Humidity { get; set; }
		public double? Pressure { get; set; }
		public double? RainToday { get; set; }
		public int? SolarRad { get; set; }
		public double? UV { get; set; }
		public double? raincounter { get; set; }
		public double? FeelsLike { get; set; }
		public double? Humidex { get; set; }
		public double? AppTemp { get; set; }
		public double? IndoorTemp { get; set; }
		public int? IndoorHumidity { get; set; }
		public int? SolarMax { get; set; }
		public double? Pm2p5 { get; set; }
		public double? Pm10 { get; set; }
		public double? RainRate { get; set; }
	}
}
