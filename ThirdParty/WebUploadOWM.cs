using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace CumulusMX.ThirdParty
{
	internal class WebUploadOWM : WebUploadServiceBase
	{

		internal WebUploadOWM(Cumulus cumulus, string name) : base(cumulus, name)
		{ }


		/// <summary>
		/// Process the list of OpenWeatherMap updates created at start-up from logger entries
		/// </summary>
		internal override async Task DoCatchUp()
		{
			Updating = true;

			string url = "http://api.openweathermap.org/data/3.0/measurements?appid=" + PW;
			string logUrl = url.Replace(PW, "<key>");

			for (int i = 0; i < CatchupList.Count; i++)
			{
				Cumulus.LogMessage("Uploading OpenWeatherMap archive #" + (i + 1));
				cumulus.LogDebugMessage("OpenWeatherMap: URL = " + logUrl);
				cumulus.LogDataMessage("OpenWeatherMap: Body = " + CatchupList[i]);

				try
				{
					var data = new StringContent(CatchupList[i], Encoding.UTF8, "application/json");
					HttpResponseMessage response = await httpClient.PostAsync(url, data);
					var responseBodyAsText = await response.Content.ReadAsStringAsync();
					var status = response.StatusCode == HttpStatusCode.NoContent ? "OK" : "Error";  // Returns a 204 response for OK!
					cumulus.LogDebugMessage($"OpenWeatherMap: Response code = {status} - {response.StatusCode}");

					if (response.StatusCode != HttpStatusCode.NoContent)
						cumulus.LogDataMessage($"OpenWeatherMap: Response data = {responseBodyAsText}");
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "OpenWeatherMap: Update error");
				}
			}

			Cumulus.LogMessage("End of OpenWeatherMap archive upload");
			CatchupList.Clear();
			CatchingUp = false;
			Updating = false;
		}


		internal override async Task DoUpdate(DateTime timestamp)
		{
			if (Updating || station.DataStopped)
			{
				// No data coming in, do not do anything
				return;
			}

			Updating = true;

			string url = "http://api.openweathermap.org/data/3.0/measurements?appid=" + PW;
			string logUrl = url.Replace(PW, "<key>");

			string jsonData = GetURL(out _, timestamp);

			cumulus.LogDebugMessage("OpenWeatherMap: URL = " + logUrl);
			cumulus.LogDataMessage("OpenWeatherMap: Body = " + jsonData);

			try
			{
				var data = new StringContent(jsonData, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await httpClient.PostAsync(url, data);
				var responseBodyAsText = await response.Content.ReadAsStringAsync();
				var status = response.StatusCode == HttpStatusCode.NoContent ? "OK" : "Error";  // Returns a 204 response for OK!
				cumulus.LogDebugMessage($"OpenWeatherMap: Response code = {status} - {response.StatusCode}");
				if (response.StatusCode != HttpStatusCode.NoContent)
				{
					Cumulus.LogMessage($"OpenWeatherMap: ERROR - Response code = {response.StatusCode}, Response data = {responseBodyAsText}");
					cumulus.ThirdPartyUploadAlarm.LastError = $"OpenWeatherMap: HTTP response - {response.StatusCode}, Response data = {responseBodyAsText}";
					cumulus.ThirdPartyUploadAlarm.Triggered = true;
				}
				else
				{
					cumulus.ThirdPartyUploadAlarm.Triggered = false;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "OpenWeatherMap: ERROR");
				cumulus.ThirdPartyUploadAlarm.LastError = "OpenWeatherMap: " + ex.Message;
				cumulus.ThirdPartyUploadAlarm.Triggered = true;
			}
			finally
			{
				Updating = false;
			}
		}


		// Documentation on the API can be found here...
		// https://stations.windguru.cz/upload_api.php
		//
		// GetURL actually returns the request body for OpenWeatherMap
		internal override string GetURL(out string pwstring, DateTime timestamp)
		{
			pwstring = null;

			StringBuilder sb = new StringBuilder($"[{{\"station_id\":\"{ID}\",");
			var invC = new CultureInfo("");

			sb.Append($"\"dt\":{Utils.ToUnixTime(timestamp)},");
			if (station.Temperature.HasValue)
				sb.Append($"\"temperature\":{Math.Round(WeatherStation.ConvertUserTempToC(station.Temperature).Value, 1).ToString(invC)},");
			sb.Append($"\"wind_deg\":{station.AvgBearing},");
			sb.Append($"\"wind_speed\":{Math.Round(WeatherStation.ConvertUserWindToMS(station.WindAverage).Value, 1).ToString(invC)},");
			if (station.RecentMaxGust.HasValue)
				sb.Append($"\"wind_gust\":{Math.Round(WeatherStation.ConvertUserWindToMS(station.RecentMaxGust).Value, 1).ToString(invC)},");
			if (station.Pressure.HasValue)
				sb.Append($"\"pressure\":{Math.Round(WeatherStation.ConvertUserPressureToHPa(station.Pressure).Value, 1).ToString(invC)},");
			if (station.Humidity.HasValue)
				sb.Append($"\"humidity\":{station.Humidity.Value},");
			sb.Append($"\"rain_1h\":{Math.Round(WeatherStation.ConvertUserRainToMM(station.RainLastHour).Value, 1).ToString(invC)},");
			sb.Append($"\"rain_24h\":{Math.Round(WeatherStation.ConvertUserRainToMM(station.RainLast24Hour).Value, 1).ToString(invC)}");
			sb.Append("}]");

			return sb.ToString();
		}


		// Find all stations associated with the users API key
		private OpenWeatherMapStation[] GetOpenWeatherMapStations()
		{
			OpenWeatherMapStation[] retVal = Array.Empty<OpenWeatherMapStation>();
			string url = "http://api.openweathermap.org/data/3.0/stations?appid=" + PW;
			try
			{
				using var client = new HttpClient();
				HttpResponseMessage response = client.GetAsync(url).Result;
				var responseBodyAsText = response.Content.ReadAsStringAsync().Result;
				cumulus.LogDataMessage("OpenWeatherMap: Get Stations Response: " + response.StatusCode + ": " + responseBodyAsText);

				if (responseBodyAsText.Length > 10)
				{
					var respJson = JsonSerializer.DeserializeFromString<OpenWeatherMapStation[]>(responseBodyAsText);
					retVal = respJson;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "OpenWeatherMap: Get Stations ERROR");
			}

			return retVal;
		}

		// Create a new OpenWeatherMap station
		private void CreateOpenWeatherMapStation()
		{
			var invC = new CultureInfo("");

			string url = "http://api.openweathermap.org/data/3.0/stations?appid=" + PW;
			try
			{
				var datestr = DateTime.Now.ToUniversalTime().ToString("yyMMddHHmm");
				StringBuilder sb = new StringBuilder($"{{\"external_id\":\"CMX-{datestr}\",");
				sb.Append($"\"name\":\"{cumulus.LocationName}\",");
				sb.Append($"\"latitude\":{cumulus.Latitude.ToString(invC)},");
				sb.Append($"\"longitude\":{cumulus.Longitude.ToString(invC)},");
				sb.Append($"\"altitude\":{(int)WeatherStation.AltitudeM(cumulus.Altitude)}}}");

				Cumulus.LogMessage($"OpenWeatherMap: Creating new station");
				Cumulus.LogMessage($"OpenWeatherMap: - {sb}");


				var data = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");

				HttpResponseMessage response = httpClient.PostAsync(url, data).Result;
				var responseBodyAsText = response.Content.ReadAsStringAsync().Result;
				var status = response.StatusCode == HttpStatusCode.Created ? "OK" : "Error";  // Returns a 201 response for OK
				cumulus.LogDebugMessage($"OpenWeatherMap: Create station response code = {status} - {response.StatusCode}");
				cumulus.LogDataMessage($"OpenWeatherMap: Create station response data = {responseBodyAsText}");

				if (response.StatusCode == HttpStatusCode.Created)
				{
					// It worked, save the result
					var respJson = JsonSerializer.DeserializeFromString<OpenWeatherMapNewStation>(responseBodyAsText);

					Cumulus.LogMessage($"OpenWeatherMap: Created new station, id = {respJson.ID}, name = {respJson.name}");
					ID = respJson.ID;
					cumulus.WriteIniFile();
				}
				else
				{
					Cumulus.LogMessage($"OpenWeatherMap: Failed to create new station. Error - {response.StatusCode}, text - {responseBodyAsText}");
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "OpenWeatherMap: Create station ERROR");
			}
		}

		internal void EnableOpenWeatherMap()
		{
			if (Enabled && string.IsNullOrWhiteSpace(ID))
			{
				// oh, oh! OpenWeatherMap is enabled, but we do not have a station id
				// first check if one already exists
				var stations = GetOpenWeatherMapStations();

				if (stations.Length == 0)
				{
					// No stations defined, we will create one
					Cumulus.LogMessage($"OpenWeatherMap: No station defined, attempting to create one");
					CreateOpenWeatherMapStation();
				}
				else if (stations.Length == 1)
				{
					// We have one station defined, lets use it!
					Cumulus.LogMessage($"OpenWeatherMap: No station defined, but found one associated with this API key, using this station - {stations[0].id} : {stations[0].name}");
					ID = stations[0].id;
					// save the setting
					cumulus.WriteIniFile();
				}
				else
				{
					// multiple stations defined, the user must select which one to use
					var msg = $"Multiple OpenWeatherMap stations found, please select the correct station id and enter it into your configuration";
					Cumulus.LogConsoleMessage(msg);
					Cumulus.LogMessage("OpenWeatherMap: " + msg);
					foreach (var station in stations)
					{
						msg = $"  Station Id = {station.id}, Name = {station.name}";
						Cumulus.LogConsoleMessage(msg);
						Cumulus.LogMessage("OpenWeatherMap: " + msg);
					}
				}
			}
		}

		private class OpenWeatherMapStation
		{
			public string id { get; set; }
			public string created_at { get; set; }
			public string updated_at { get; set; }
			public string external_id { get; set; }
			public string name { get; set; }
			public double longitude { get; set; }
			public double latitude { get; set; }
			public int altitude { get; set; }
			public int rank { get; set; }
		}

		private class OpenWeatherMapNewStation
		{
			public string ID { get; set; }
			public string created_at { get; set; }
			public string updated_at { get; set; }
			public string user_id { get; set; }
			public string external_id { get; set; }
			public string name { get; set; }
			public double longitude { get; set; }
			public double latitude { get; set; }
			public int altitude { get; set; }
			public int source_type { get; set; }
		}

	}
}
