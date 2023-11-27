using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CumulusMX.ThirdParty
{
	internal class WebUploadWCloud : WebUploadServiceBase
	{
		public bool SendLeafWetness;
		public int LeafWetnessSensor;

		public WebUploadWCloud(Cumulus cumulus, string name) : base(cumulus, name)
		{
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
			string url = GetURL(out pwstring, timestamp);

			string starredpwstring = "<key>";

			string logUrl = url.Replace(pwstring, starredpwstring);

			cumulus.LogDebugMessage("WeatherCloud: URL = " + logUrl);

			try
			{
				using var response = await Cumulus.MyHttpClient.GetAsync(url);
				var responseBodyAsText = await response.Content.ReadAsStringAsync();
				var msg = "";
				switch ((int)response.StatusCode)
				{
					case 200:
						msg = "Success";
						cumulus.ThirdPartyUploadAlarm.Triggered = false;
						break;
					case 400:
						msg = "Bad request";
						cumulus.ThirdPartyUploadAlarm.LastMessage = "WeatherCloud: " + msg;
						cumulus.ThirdPartyUploadAlarm.Triggered = true;
						break;
					case 401:
						msg = "Incorrect WID or Key";
						cumulus.ThirdPartyUploadAlarm.LastMessage = "WeatherCloud: " + msg;
						cumulus.ThirdPartyUploadAlarm.Triggered = true;
						break;
					case 429:
						msg = "Too many requests";
						cumulus.ThirdPartyUploadAlarm.LastMessage = "WeatherCloud: " + msg;
						cumulus.ThirdPartyUploadAlarm.Triggered = true;
						break;
					case 500:
						msg = "Server error";
						cumulus.ThirdPartyUploadAlarm.LastMessage = "WeatherCloud: " + msg;
						cumulus.ThirdPartyUploadAlarm.Triggered = true;
						break;
					default:
						msg = "Unknown error";
						cumulus.ThirdPartyUploadAlarm.LastMessage = "WeatherCloud: " + msg;
						cumulus.ThirdPartyUploadAlarm.Triggered = true;
						break;
				}
				if ((int)response.StatusCode == 200)
					cumulus.LogDebugMessage($"WeatherCloud: Response = {msg} ({response.StatusCode}): {responseBodyAsText}");
				else
					cumulus.LogMessage($"WeatherCloud: ERROR - Response = {msg} ({response.StatusCode}): {responseBodyAsText}");
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "WeatherCloud: ERROR");
				cumulus.ThirdPartyUploadAlarm.LastMessage = "WeatherCloud: " + ex.Message;
				cumulus.ThirdPartyUploadAlarm.Triggered = true;
			}
			finally
			{
				Updating = false;
			}
		}

		internal override string GetURL(out string pwstring, DateTime timestamp)
		{
			pwstring = PW;
			StringBuilder sb = new StringBuilder($"https://api.weathercloud.net/v01/set?wid={ID}&key={PW}");

			//Temperature
			if (station.IndoorTemp.HasValue)
				sb.Append("&tempin=" + (int) Math.Round(WeatherStation.ConvertUserTempToC(station.IndoorTemp).Value * 10));
			if (station.Temperature.HasValue)
				sb.Append("&temp=" + (int) Math.Round(WeatherStation.ConvertUserTempToC(station.Temperature).Value * 10));
			if (station.WindChill.HasValue)
				sb.Append("&chill=" + (int )Math.Round(WeatherStation.ConvertUserTempToC(station.WindChill).Value * 10));
			if (station.Dewpoint.HasValue)
				sb.Append("&dew=" + (int) Math.Round(WeatherStation.ConvertUserTempToC(station.Dewpoint).Value * 10));
			if (station.HeatIndex.HasValue)
				sb.Append("&heat=" + (int) Math.Round(WeatherStation.ConvertUserTempToC(station.HeatIndex).Value * 10));

			// Humidity
			if (station.IndoorHum.HasValue)
				sb.Append("&humin=" + station.IndoorHum.Value);
			if (station.Humidity.HasValue)
				sb.Append("&hum=" + station.Humidity.Value);

			// Wind
			if (station.WindLatest.HasValue)
				sb.Append("&wspd=" + (int) Math.Round(WeatherStation.ConvertUserWindToMS(station.WindLatest).Value * 10));
			if (station.RecentMaxGust.HasValue)
				sb.Append("&wspdhi=" + (int) Math.Round(WeatherStation.ConvertUserWindToMS(station.RecentMaxGust).Value * 10));
			sb.Append("&wspdavg=" + (int) Math.Round(WeatherStation.ConvertUserWindToMS(station.WindAverage).Value * 10));

			// Wind Direction
			sb.Append("&wdir=" + station.Bearing);
			sb.Append("&wdiravg=" + station.AvgBearing);

			// Pressure
			if (station.Pressure.HasValue)
				sb.Append("&bar=" + (int) Math.Round(WeatherStation.ConvertUserPressToMB(station.Pressure).Value * 10));

			// rain
			if (station.RainToday.HasValue)
				sb.Append("&rain=" + (int) Math.Round(WeatherStation.ConvertUserRainToMM(station.RainToday).Value * 10));
			if (station.RainRate.HasValue)
				sb.Append("&rainrate=" + (int) Math.Round(WeatherStation.ConvertUserRainToMM(station.RainRate).Value * 10));

			// ET
			if (SendSolar && cumulus.Manufacturer == cumulus.DAVIS)
			{
				sb.Append("&et=" + (int) Math.Round(WeatherStation.ConvertUserRainToMM(station.ET).Value * 10));
			}

			// solar
			if (SendSolar && station.SolarRad.HasValue)
			{
				sb.Append("&solarrad=" + station.SolarRad.Value * 10);
			}

			// uv
			if (SendUV && station.UV.HasValue)
			{
				sb.Append("&uvi=" + (int) Math.Round(station.UV.Value * 10));
			}

			// aq
			if (SendAirQuality)
			{
				switch (cumulus.StationOptions.PrimaryAqSensor)
				{
					case (int)Cumulus.PrimaryAqSensor.AirLinkOutdoor:
						if (cumulus.airLinkDataOut != null)
						{
							sb.Append($"&pm25={cumulus.airLinkDataOut.pm2p5:F0}");
							sb.Append($"&pm10={cumulus.airLinkDataOut.pm10:F0}");
							sb.Append($"&aqi={AirQualityIndices.US_EPApm2p5(cumulus.airLinkDataOut.pm2p5_24hr)}");
						}
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt1:
						if (station.AirQuality[1].HasValue)
							sb.Append($"&pm25={station.AirQuality[1]:F0}");
						if (station.AirQualityAvg[1].HasValue)
							sb.Append($"&aqi={AirQualityIndices.US_EPApm2p5(station.AirQualityAvg[1].Value)}");
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt2:
						if (station.AirQuality[2].HasValue)
							sb.Append($"&pm25={station.AirQuality[2]:F0}");
						if (station.AirQualityAvg[2].HasValue)
							sb.Append($"&aqi={AirQualityIndices.US_EPApm2p5(station.AirQualityAvg[2].Value)}");
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt3:
						if (station.AirQuality[3].HasValue)
							sb.Append($"&pm25={station.AirQuality[3]:F0}");
						if (station.AirQualityAvg[3].HasValue)
							sb.Append($"&aqi={AirQualityIndices.US_EPApm2p5(station.AirQualityAvg[3].Value)}");
						break;
					case (int)Cumulus.PrimaryAqSensor.Ecowitt4:
						if (station.AirQuality[4].HasValue)
							sb.Append($"&pm25={station.AirQuality[4]:F0}");
						if (station.AirQualityAvg[4].HasValue)
							sb.Append($"&aqi={AirQualityIndices.US_EPApm2p5(station.AirQualityAvg[4].Value)}");
						break;
					case (int)Cumulus.PrimaryAqSensor.EcowittCO2:
						if (station.CO2_pm2p5.HasValue)
							sb.Append($"&pm25={station.CO2_pm2p5.Value:F0}");
						if (station.CO2_pm10.HasValue)
							sb.Append($"&pm10={station.CO2_pm10.Value:F0}");
						if (station.CO2_pm2p5_24h.HasValue)
							sb.Append($"&aqi={AirQualityIndices.US_EPApm2p5(station.CO2_pm2p5_24h.Value)}");
						break;
				}
			}

			// soil moisture
			if (SendSoilMoisture)
			{
				// Weathercloud wants soil moisture in centibar. Davis supplies this, but Ecowitt provide a percentage
				int moist = station.SoilMoisture[SoilMoistureSensor] ?? 0;


				if (cumulus.Manufacturer == cumulus.EW && station.SoilMoisture[1].HasValue)
				{
					// very! approximate conversion from percentage to cb
					moist = (100 - station.SoilMoisture[1].Value) * 2;
				}

				sb.Append($"&soilmoist={moist}");
			}

			// leaf wetness
			if (SendLeafWetness)
			{
				// Weathercloud wants soil moisture in centibar. Davis supplies this, but Ecowitt provide a percentage
				var wet = station.LeafWetness[cumulus.WCloud.LeafWetnessSensor] ?? 0;
				sb.Append($"&leafwet={wet.ToString(cumulus.LeafWetFormat)}");
			}

			// time - UTCHmm"));

			// date - UTC
			sb.Append("&date=" + timestamp.ToUniversalTime().ToString("yyyyMMdd"));

			// software identification
			//sb.Append("&type=291&ver=" + cumulus.Version);
			sb.Append($"&software=Cumulus_MX_v{cumulus.Version}&softwareid=142787ebe716");

			return sb.ToString();
		}
	}
}
