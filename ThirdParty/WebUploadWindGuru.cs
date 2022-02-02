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
				HttpResponseMessage response = await httpClient.GetAsync(url);
				var responseBodyAsText = await response.Content.ReadAsStringAsync();
				cumulus.LogDebugMessage("WindGuru: " + response.StatusCode + ": " + responseBodyAsText);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					Cumulus.LogMessage("WindGuru: ERROR - " + response.StatusCode + ": " + responseBodyAsText);
					cumulus.HttpUploadAlarm.LastError = "WindGuru: HTTP response - " + response.StatusCode;
					cumulus.HttpUploadAlarm.Triggered = true;
				}
				else
				{
					cumulus.HttpUploadAlarm.Triggered = false;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "WindGuru: ERROR");
				cumulus.HttpUploadAlarm.LastError = "WindGuru: " + ex.Message;
				cumulus.HttpUploadAlarm.Triggered = true;
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
				if (station.WindRecent[i].Timestamp >= DateTime.Now.AddMinutes(-cumulus.WindGuru.Interval))
				{
					numvalues++;
					totalwind += station.WindRecent[i].Gust;

					if (station.WindRecent[i].Gust > maxwind)
					{
						maxwind = station.WindRecent[i].Gust;
					}

					if (station.WindRecent[i].Gust < minwind)
					{
						minwind = station.WindRecent[i].Gust;
					}
				}
			}
			// average the values
			double avgwind = totalwind / numvalues * cumulus.Calib.WindSpeed.Mult;

			maxwind *= cumulus.Calib.WindGust.Mult;
			minwind *= cumulus.Calib.WindGust.Mult;


			StringBuilder URL = new StringBuilder("http://www.windguru.cz/upload/api.php?", 1024);

			URL.Append("uid=" + HttpUtility.UrlEncode(cumulus.WindGuru.ID));
			URL.Append("&salt=" + salt);
			URL.Append("&hash=" + hash);
			URL.Append("&interval=" + cumulus.WindGuru.Interval * 60);
			URL.Append("&wind_avg=" + station.ConvertUserWindToKnots(avgwind).ToString("F1", InvC));
			URL.Append("&wind_max=" + station.ConvertUserWindToKnots(maxwind).ToString("F1", InvC));
			URL.Append("&wind_min=" + station.ConvertUserWindToKnots(minwind).ToString("F1", InvC));
			URL.Append("&wind_direction=" + station.AvgBearing);
			if (station.Temperature.HasValue)
				URL.Append("&temperature=" + station.ConvertUserTempToC(station.Temperature).Value.ToString("F1", InvC));
			if (station.Humidity.HasValue)
				URL.Append("&rh=" + station.Humidity.Value);
			if (station.Pressure.HasValue)
				URL.Append("&mslp=" + station.ConvertUserPressureToHPa(station.Pressure).Value.ToString("F1", InvC));
			if (cumulus.WindGuru.SendRain)
			{
				URL.Append("&precip=" + station.ConvertUserRainToMM(station.RainLastHour).Value.ToString("F1", InvC));
				URL.Append("&precip_interval=3600");
			}

			return URL.ToString();
		}


	}
}
