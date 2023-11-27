using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CumulusMX.ThirdParty
{
	internal class WebUploadWindy : WebUploadServiceBase
	{
		public string ApiKey;
		public int StationIdx;

		public WebUploadWindy(Cumulus cumulus, string name) : base(cumulus, name)
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
			string logUrl = url.Replace(apistring, "<<API_KEY>>");

			cumulus.LogDebugMessage("Windy: URL = " + logUrl);

			try
			{
				using var response = await Cumulus.MyHttpClient.GetAsync(url);
				var responseBodyAsText = await response.Content.ReadAsStringAsync();
				cumulus.LogDebugMessage("Windy: Response = " + response.StatusCode + ": " + responseBodyAsText);
				if (response.StatusCode != HttpStatusCode.OK)
				{
					cumulus.LogMessage("Windy: ERROR - Response = " + response.StatusCode + ": " + responseBodyAsText);
					cumulus.ThirdPartyUploadAlarm.LastMessage = "Windy: HTTP response - " + response.StatusCode;
					cumulus.ThirdPartyUploadAlarm.Triggered = true;
				}
				else
				{
					cumulus.ThirdPartyUploadAlarm.Triggered = false;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Windy: ERROR");
				cumulus.ThirdPartyUploadAlarm.LastMessage = "Windy: " + ex.Message;
				cumulus.ThirdPartyUploadAlarm.Triggered = true;
			}
			finally
			{
				Updating = false;
			}
		}


		// Documentation on the API can be found here...
		// https://community.windy.com/topic/8168/report-your-weather-station-data-to-windy
		//
		internal override string GetURL(out string apistring, DateTime timestamp)
		{
			string dateUTC = timestamp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'+'HH':'mm':'ss");
			StringBuilder URL = new StringBuilder("https://stations.windy.com/pws/update/", 1024);

			apistring = ApiKey;

			URL.Append(ApiKey);
			URL.Append("?station=" + StationIdx);
			URL.Append("&dateutc=" + dateUTC);

			StringBuilder Data = new StringBuilder(1024);
			Data.Append("&winddir=" + station.AvgBearing);
			if (station.WindAverage.HasValue)
				Data.Append("&wind=" + station.WindMSStr(station.WindAverage.Value));
			if (station.RecentMaxGust.HasValue)
				Data.Append("&gust=" + station.WindMSStr(station.RecentMaxGust.Value));
			if (station.Temperature.HasValue)
				Data.Append("&temp=" + station.TempCstr(station.Temperature.Value));
			Data.Append("&precip=" + station.RainMMstr(station.RainLastHour));
			if (station.Pressure.HasValue)
				Data.Append("&pressure=" + station.PressPAstr(station.Pressure.Value));
			if (station.Dewpoint.HasValue)
				Data.Append("&dewpoint=" + station.TempCstr(station.Dewpoint.Value));
			if (station.Humidity.HasValue)
				Data.Append("&humidity=" + station.Humidity.Value);

			if (SendUV && station.UV.HasValue)
				Data.Append("&uv=" + station.UV.Value.ToString(cumulus.UVFormat, CultureInfo.InvariantCulture));
			if (SendSolar && station.SolarRad.HasValue)
				Data.Append("&solarradiation=" + station.SolarRad.Value.ToString("F0"));

			URL.Append(Data);

			return URL.ToString();
		}
	}
}
