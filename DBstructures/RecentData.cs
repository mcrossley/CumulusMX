using System;
using SQLite;

namespace CumulusMX
{
	internal class RecentData
	{
		[PrimaryKey]
		public DateTime Timestamp { get; set; }

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
