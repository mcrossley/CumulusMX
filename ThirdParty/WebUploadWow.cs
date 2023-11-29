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
	internal class WebUploadWow : WebUploadServiceBase
	{

		internal WebUploadWow(Cumulus cumulus, string name) : base(cumulus, name)
		{
		}


		internal override async Task DoUpdate(DateTime timestamp)
		{
			if (!Updating)
			{
				Updating = true;

				string pwstring;
				string URL = GetURL(out pwstring, timestamp);

				string starredpwstring = "&siteAuthenticationKey=" + new string('*', PW.Length);

				string LogURL = URL.Replace(pwstring, starredpwstring);
				cumulus.LogDebugMessage("WOW URL = " + LogURL);

				try
				{
					using var response = await Cumulus.MyHttpClient.GetAsync(URL);
					var responseBodyAsText = await response.Content.ReadAsStringAsync();
					if (response.StatusCode != HttpStatusCode.OK)
					{
						cumulus.LogMessage($"WOW Response: ERROR - Response code = {response.StatusCode}, body = {responseBodyAsText}");
						cumulus.ThirdPartyAlarm.LastMessage = $"WOW: HTTP response - Response code = {response.StatusCode}, body = {responseBodyAsText}";
						cumulus.ThirdPartyAlarm.Triggered = true;
					}
					else
					{
						cumulus.LogDebugMessage("WOW Response: " + response.StatusCode + ": " + responseBodyAsText);
						cumulus.ThirdPartyAlarm.Triggered = false;
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "WOW update error");
					cumulus.ThirdPartyAlarm.LastMessage = "WOW: " + ex.Message;
					cumulus.ThirdPartyAlarm.Triggered = true;
				}
				finally
				{
					Updating = false;
				}
			}
		}

		internal override string GetURL(out string pwstring, DateTime timestamp)
		{
			string dateUTC = timestamp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'+'HH'%3A'mm'%3A'ss");
			StringBuilder URL = new StringBuilder("http://wow.metoffice.gov.uk/automaticreading?siteid=", 1024);

			pwstring = PW;
			URL.Append(ID);
			URL.Append("&siteAuthenticationKey=" + PW);
			URL.Append("&dateutc=" + dateUTC);

			StringBuilder Data = new StringBuilder(1024);

			// send average speed and bearing
			Data.Append("&winddir=" + station.AvgBearing);
			if (station.WindAverage.HasValue)
				Data.Append("&windspeedmph=" + station.WindMPHStr(station.WindAverage.Value));
			if (station.RecentMaxGust.HasValue)
				Data.Append("&windgustmph=" + station.WindMPHStr(station.RecentMaxGust.Value));
			if (station.Humidity.HasValue)
				Data.Append("&humidity=" + station.Humidity.Value);
			if (station.Temperature.HasValue)
				Data.Append("&tempf=" + station.TempFstr(station.Temperature.Value));
			Data.Append("&rainin=" + station.RainINstr(station.RainLastHour));
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
				Data.Append("&baromin=" + station.PressINstr(station.Pressure.Value));
			if (station.Dewpoint.HasValue)
				Data.Append("&dewptf=" + station.TempFstr(station.Dewpoint.Value));
			if (SendUV && station.UV.HasValue)
				Data.Append("&UV=" + station.UV.Value.ToString(cumulus.UVFormat, CultureInfo.InvariantCulture.NumberFormat));
			if (SendSolar && station.SolarRad.HasValue)
				Data.Append("&solarradiation=" + station.SolarRad.Value);
			if (SendSoilTemp && station.SoilTemp[SoilTempSensor].HasValue)
				Data.Append($"&soiltempf=" + station.TempFstr(station.SoilTemp[SoilTempSensor].Value));

			Data.Append("&softwaretype=Cumulus%20v" + cumulus.Version);
			Data.Append("&action=updateraw");

			Data.Replace(",", ".");
			URL.Append(Data);

			return URL.ToString();
		}
	}
}
