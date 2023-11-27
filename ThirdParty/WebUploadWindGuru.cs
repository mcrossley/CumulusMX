using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Text;

namespace CumulusMX.ThirdParty
{
	internal class WebUploadWindGuru : WebUploadServiceBase
	{
		public bool SendRain;

		public WebUploadWindGuru(Cumulus cumulus, string name) : base (cumulus, name)
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

			string apistring;
			string url = GetURL(out apistring, timestamp);
			string logUrl = url.Replace(apistring, "<<StationUID>>");

			cumulus.LogDebugMessage("WindGuru: URL = " + logUrl);

			try
			{
				using var response = await Cumulus.MyHttpClient.GetAsync(url);
				var responseBodyAsText = await response.Content.ReadAsStringAsync();
				cumulus.LogDebugMessage("WindGuru: " + response.StatusCode + ": " + responseBodyAsText);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					cumulus.LogMessage("WindGuru: ERROR - " + response.StatusCode + ": " + responseBodyAsText);
					cumulus.ThirdPartyUploadAlarm.LastMessage = "WindGuru: HTTP response - " + response.StatusCode;
					cumulus.ThirdPartyUploadAlarm.Triggered = true;
				}
				else
				{
					cumulus.ThirdPartyUploadAlarm.Triggered = false;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "WindGuru: ERROR");
				cumulus.ThirdPartyUploadAlarm.LastMessage = "WindGuru: " + ex.Message;
				cumulus.ThirdPartyUploadAlarm.Triggered = true;
			}
			finally
			{
				Updating = false;
			}
		}

		internal override string GetURL(out string uidstring, DateTime timestamp)
		{
			var InvC = new CultureInfo("");

			string salt = timestamp.ToUnixTime().ToString();
			string hash = Utils.GetMd5String(salt + cumulus.WindGuru.ID + cumulus.WindGuru.PW);

			uidstring = cumulus.WindGuru.ID;

			int numvalues = 0;
			double totalwind = 0;
			double maxwind = 0;
			double minwind = 999;
			for (int i = 0; i < WeatherStation.MaxWindRecent; i++)
			{
				if (station.RecentWind[i].Timestamp >= DateTime.Now.AddMinutes(-cumulus.WindGuru.Interval))
				{
					numvalues++;
					totalwind += station.RecentWind[i].Gust;

					if (station.RecentWind[i].Gust > maxwind)
					{
						maxwind = station.RecentWind[i].Gust;
					}

					if (station.RecentWind[i].Gust < minwind)
					{
						minwind = station.RecentWind[i].Gust;
					}
				}
			}
			// average the values
			double avgwind = totalwind / numvalues;

			StringBuilder URL = new StringBuilder("http://www.windguru.cz/upload/api.php?", 1024);

			URL.Append("uid=" + HttpUtility.UrlEncode(cumulus.WindGuru.ID));
			URL.Append("&salt=" + salt);
			URL.Append("&hash=" + hash);
			URL.Append("&interval=" + cumulus.WindGuru.Interval * 60);
			URL.Append("&wind_avg=" + WeatherStation.ConvertUserWindToKnots(avgwind).Value.ToString("F1", InvC));
			URL.Append("&wind_max=" + WeatherStation.ConvertUserWindToKnots(maxwind).Value.ToString("F1", InvC));
			URL.Append("&wind_min=" + WeatherStation.ConvertUserWindToKnots(minwind).Value.ToString("F1", InvC));
			URL.Append("&wind_direction=" + station.AvgBearing);
			if (station.Temperature.HasValue)
				URL.Append("&temperature=" + WeatherStation.ConvertUserTempToC(station.Temperature).Value.ToString("F1", InvC));
			if (station.Humidity.HasValue)
				URL.Append("&rh=" + station.Humidity.Value);
			if (station.Pressure.HasValue)
				URL.Append("&mslp=" + WeatherStation.ConvertUserPressureToHPa(station.Pressure).Value.ToString("F1", InvC));
			if (cumulus.WindGuru.SendRain)
			{
				URL.Append("&precip=" + WeatherStation.ConvertUserRainToMM(station.RainLastHour).Value.ToString("F1", InvC));
				URL.Append("&precip_interval=3600");
			}

			return URL.ToString();
		}


	}
}
