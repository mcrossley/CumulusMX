using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CumulusMX.ThirdParty
{
	internal class WebUploadWund : WebUploadServiceBase
	{
		public bool RapidFireEnabled;
		public bool SendAverage;
		public bool SendSoilTemp1;
		public bool SendSoilTemp2;
		public bool SendSoilTemp3;
		public bool SendSoilTemp4;
		public bool SendSoilMoisture1;
		public bool SendSoilMoisture2;
		public bool SendSoilMoisture3;
		public bool SendSoilMoisture4;
		public bool SendLeafWetness1;
		public bool SendLeafWetness2;
		public int ErrorFlagCount;

		public WebUploadWund(Cumulus cumulus, string name) : base (cumulus, name)
		{
			IntTimer.Elapsed += TimerTick;
		}


		internal override async Task DoUpdate(DateTime timestamp)
		{
			if (Updating || station.DataStopped)
			{
				// No data coming in, do not do anything
				return;
			}

			Updating = true;

			string pwstring;
			string URL = GetURL(out pwstring, timestamp);

			string starredpwstring = "&PASSWORD=" + new string('*', PW.Length);

			string logUrl = URL.Replace(pwstring, starredpwstring);
			if (!RapidFireEnabled)
			{
				cumulus.LogDebugMessage("Wunderground: URL = " + logUrl);
			}

			try
			{
				HttpResponseMessage response = await httpClient.GetAsync(URL);
				var responseBodyAsText = await response.Content.ReadAsStringAsync();
				if (response.StatusCode != HttpStatusCode.OK)
				{
					// Flag the error immediately if no rapid fire
					// Flag error after every 12 rapid fire failures (1 minute)
					ErrorFlagCount++;
					if (!RapidFireEnabled || ErrorFlagCount >= 12)
					{
						Cumulus.LogMessage("Wunderground: Response = " + response.StatusCode + ": " + responseBodyAsText);
						cumulus.HttpUploadAlarm.LastError = "Wunderground: HTTP response - " + response.StatusCode;
						cumulus.HttpUploadAlarm.Triggered = true;
						ErrorFlagCount = 0;
					}
				}
				else
				{
					cumulus.HttpUploadAlarm.Triggered = false;
					ErrorFlagCount = 0;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Wunderground: ERROR");
				cumulus.HttpUploadAlarm.LastError = "Wunderground: " + ex.Message;
				cumulus.HttpUploadAlarm.Triggered = true;
			}
			finally
			{
				Updating = false;
			}
		}


		internal override string GetURL(out string pwstring, DateTime timestamp)
		{
			// API documentation: https://support.weather.com/s/article/PWS-Upload-Protocol?language=en_US

			var invC = new CultureInfo("");

			string dateUTC = timestamp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'+'HH'%3A'mm'%3A'ss");
			StringBuilder URL = new StringBuilder(1024);
			if (RapidFireEnabled && !CatchingUp)
			{
				URL.Append("http://rtupdate.wunderground.com/weatherstation/updateweatherstation.php?ID=");
			}
			else
			{
				URL.Append("http://weatherstation.wunderground.com/weatherstation/updateweatherstation.php?ID=");
			}

			pwstring = PW;
			URL.Append(ID);
			URL.Append($"&PASSWORD={PW}");
			URL.Append($"&dateutc={dateUTC}");
			StringBuilder Data = new StringBuilder(1024);
			if (SendAverage && station.WindAverage.HasValue)
			{
				// send average speed and bearing
				Data.Append($"&winddir={station.AvgBearing}&windspeedmph={station.WindMPHStr(station.WindAverage.Value)}");
			}
			else if (station.WindLatest.HasValue)
			{
				// send "instantaneous" speed (i.e. latest) and bearing
				Data.Append($"&winddir={station.Bearing}&windspeedmph={station.WindMPHStr(station.WindLatest.Value)}");
			}
			if (station.RecentMaxGust.HasValue)
				Data.Append($"&windgustmph={station.WindMPHStr(station.RecentMaxGust.Value)}");
			// may not strictly be a 2 min average!
			if (station.WindAverage.HasValue)
			{
				Data.Append($"&windspdmph_avg2m={station.WindMPHStr(station.WindAverage.Value)}");
				Data.Append($"&winddir_avg2m={station.AvgBearing}");
			}
			if (station.Humidity.HasValue)
				Data.Append($"&humidity={station.Humidity.Value}");
			if (station.Temperature.HasValue)
				Data.Append($"&tempf={station.TempFstr(station.Temperature.Value)}");
			Data.Append($"&rainin={station.RainINstr(station.RainLastHour)}");
			Data.Append("&dailyrainin=");
			if (cumulus.RolloverHour == 0)
			{
				// use today"s rain
				Data.Append(station.RainINstr(station.RainToday ?? 0));
			}
			else
			{
				Data.Append(station.RainINstr(station.RainSinceMidnight));
			}
			if (station.Pressure.HasValue)
				Data.Append($"&baromin={station.PressINstr(station.Pressure.Value)}");
			if (station.Dewpoint.HasValue)
				Data.Append($"&dewptf={station.TempFstr(station.Dewpoint.Value)}");
			if (SendUV && station.UV.HasValue)
				Data.Append($"&UV={station.UV.Value.ToString(cumulus.UVFormat, invC)}");
			if (SendSolar)
				Data.Append($"&solarradiation={station.SolarRad:F0}");
			if (SendIndoor)
			{
				if (station.IndoorTemp.HasValue)
					Data.Append($"&indoortempf={station.TempFstr(station.IndoorTemp.Value)}");
				if (station.IndoorHum.HasValue)
					Data.Append($"&indoorhumidity={station.IndoorHum.Value}");
			}
			// Davis soil and leaf sensors
			if (SendSoilTemp1 && station.SoilTemp[1].HasValue)
				Data.Append($"&soiltempf={station.TempFstr(station.SoilTemp[1].Value)}");
			if (SendSoilTemp2 && station.SoilTemp[2].HasValue)
				Data.Append($"&soiltempf2={station.TempFstr(station.SoilTemp[2].Value)}");
			if (SendSoilTemp3 && station.SoilTemp[3].HasValue)
				Data.Append($"&soiltempf3={station.TempFstr(station.SoilTemp[3].Value)}");
			if (SendSoilTemp4 && station.SoilTemp[4].HasValue)
				Data.Append($"&soiltempf4={station.TempFstr(station.SoilTemp[4].Value)}");

			if (SendSoilMoisture1 && station.SoilMoisture[1].HasValue)
				Data.Append($"&soilmoisture={station.SoilMoisture[1].Value}");
			if (SendSoilMoisture2 && station.SoilMoisture[2].HasValue)
				Data.Append($"&soilmoisture2={station.SoilMoisture[2].Value}");
			if (SendSoilMoisture3 && station.SoilMoisture[3].HasValue)
				Data.Append($"&soilmoisture3={station.SoilMoisture[3].Value}");
			if (SendSoilMoisture4 && station.SoilMoisture[4].HasValue)
				Data.Append($"&soilmoisture4={station.SoilMoisture[4].Value}");

			if (SendLeafWetness1 && station.LeafWetness[1].HasValue)
				Data.Append($"&leafwetness={station.LeafWetness[1].Value}");
			if (SendLeafWetness2 && station.LeafWetness[2].HasValue)
				Data.Append($"&leafwetness2={station.LeafWetness[2].Value}");

			if (SendAirQuality && cumulus.StationOptions.PrimaryAqSensor > (int)Cumulus.PrimaryAqSensor.Undefined)
			{
				switch (cumulus.StationOptions.PrimaryAqSensor)
				{
					case (int)Cumulus.PrimaryAqSensor.AirLinkOutdoor:
						if (cumulus.airLinkDataOut != null)
						{
							Data.Append($"&AqPM2.5={cumulus.airLinkDataOut.pm2p5:F1}&AqPM10={cumulus.airLinkDataOut.pm10.ToString("F1", invC)}");
						}
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt1:
						if (station.AirQuality[1].HasValue)
							Data.Append($"&AqPM2.5={station.AirQuality[1].Value.ToString("F1", invC)}");
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt2:
						if (station.AirQuality[2].HasValue)
							Data.Append($"&AqPM2.5={station.AirQuality[2].Value.ToString("F1", invC)}");
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt3:
						if (station.AirQuality[3].HasValue)
							Data.Append($"&AqPM2.5={station.AirQuality[3].Value.ToString("F1", invC)}");
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt4:
						if (station.AirQuality[4].HasValue)
							Data.Append($"&AqPM2.5={station.AirQuality[4].Value.ToString("F1", invC)}");
						break;
				}
			}

			Data.Append($"&softwaretype=Cumulus%20v{cumulus.Version}");
			Data.Append("&action=updateraw");
			if (cumulus.Wund.RapidFireEnabled && !CatchingUp)
				Data.Append("&realtime=1&rtfreq=5");

			Data.Replace(",", ".");
			URL.Append(Data);

			return URL.ToString();
		}

		private void TimerTick(object sender, ElapsedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(ID))
				_ = DoUpdate(DateTime.Now);
		}


	}
}
