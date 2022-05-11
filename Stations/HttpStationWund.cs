using System;
using System.Globalization;
using EmbedIO;

namespace CumulusMX
{
	class HttpStationWund : WeatherStation
	{
		private bool starting = true;
		private bool stopping = false;
		private double previousRainCount = -1;
		private double rainCount = 0;
		private readonly NumberFormatInfo invNum = CultureInfo.InvariantCulture.NumberFormat;

		public HttpStationWund(Cumulus cumulus) : base(cumulus)
		{
			Cumulus.LogMessage("Starting HTTP Station (Wunderground)");

			cumulus.StationOptions.CalculatedWC = true;
			cumulus.Units.AirQualityUnitText = "µg/m³";
			cumulus.Units.SoilMoistureUnitText = "%";
			cumulus.Units.LeafWetnessUnitText = "%";

			// Wunderground does not send the rain rate, so we will calculate it
			calculaterainrate = true;

		}

		public override void DoStartup()
		{
			Start();
			starting = false;
		}

		public override void Start()
		{
			DoDayResetIfNeeded();
			timerStartNeeded = true;
		}

		public override void Stop()
		{
			stopping = true;
			StopMinuteTimer();

			// Call the common code in the base class
			base.Stop();
		}

		public string ProcessData(IHttpContext context)
		{
			/*
			GET Parameters - all fields are URL escaped
			===========================================
			ID					- ignored
			PASSWORD			- ignored
			weather				- ignored
			clouds				- ignored
			visibility			- ignored
			action				- ignored
			softwaretype		- ignored
			realtime			- ignored
			rtfreq				- ignored

			ID=ISAARB3&PASSWORD=key&tempf=81.5&humidity=43&dewptf=56.8&windchillf=81.5&winddir=329&windspeedmph=0.00&windgustmph=5.82&rainin=0.000&dailyrainin=0.000&weeklyrainin=0.000&monthlyrainin=4.118&yearlyrainin=29.055&solarradiation=253.20&UV=1&indoortempf=80.6&indoorhumidity=50&baromin=29.943&AqPM2.5=10.0&soilmoisture=51&soilmoisture2=65&soilmoisture3=72&soilmoisture4=36&soilmoisture5=48&lowbatt=0&dateutc=now&softwaretype=GW1000A_V1.6.8&action=updateraw&realtime=1&rtfreq=5

			 */

			DateTime recDate;

			if (starting || stopping)
			{
				context.Response.StatusCode = 200;
				return "success";
			}

			try
			{
				// dateutc = "YYYY-MM-DD HH:mm:SS" or "now"

				cumulus.LogDebugMessage($"ProcessData: Processing query - {context.Request.RawUrl}");

				var data = context.Request.QueryString;

				// We will ignore the dateutc field, this is "live" data so just use "now" to avoid any clock issues
				recDate = DateTime.Now;

				// === Wind ===
				try
				{
					// winddir - [0 - 360 instantaneous wind direction]
					// windspeedmph - [mph instantaneous wind speed]
					// windgustmph - [mph current wind gust, using software specific time period]
					// windgustdir - [0 - 360 using software specific time period]
					// - values below are not always provided
					// windspdmph_avg2m - [mph 2 minute average wind speed mph]
					// winddir_avg2m - [0 - 360 2 minute average wind direction]
					// windgustmph_10m - [mph past 10 minutes wind gust mph]
					// windgustdir_10m - [0 - 360 past 10 minutes wind gust direction]

					var gust = data["windgustmph"];
					var dir = data["winddir"];
					var avg = data["windspeedmph"];

					if (gust == null || dir == null || avg == null ||
						 gust == "-9999" || dir == "-9999" || avg == "-9999")
					{
						Cumulus.LogMessage($"ProcessData: Error, missing wind data");
					}

					var gustVal = gust == null || gust == "-9999" ? null : ConvertWindMPHToUser(Convert.ToDouble(gust, invNum));
					int? dirVal = dir == null || dir == "-9999" ? null : Convert.ToInt32(dir, invNum);
					var avgVal = avg == null || avg == "-9999" ? null : ConvertWindMPHToUser(Convert.ToDouble(avg, invNum));
					DoWind(gustVal, dirVal, avgVal, recDate);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Wind data");
					context.Response.StatusCode = 500;
					return "Failed: Error in wind data - " + ex.Message;
				}


				// === Humidity ===
				try
				{
					// humidity - [% outdoor humidity 0 - 100 %]
					// indoorhumidity - [% indoor humidity 0 - 100]

					var humIn = data["indoorhumidity"];
					var humOut = data["humidity"];

					int? val = humIn == null || humIn == "-9999" ? null : Convert.ToInt32(humIn, invNum);
					DoIndoorHumidity(val);
					if (val == null)
						Cumulus.LogMessage($"ProcessData: Error, missing indoor humidity");

					val = humOut == null || humOut == "-9999" ? null : Convert.ToInt32(humOut, invNum);
					DoHumidity(val, recDate);
					if (val == null)
						Cumulus.LogMessage($"ProcessData: Error, missing outdoor humidity");
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Humidity data");
					context.Response.StatusCode = 500;
					return "Failed: Error in humidity data - " + ex.Message;
				}


				// === Pressure ===
				try
				{
					// baromin - [barometric pressure inches]

					var press = data["baromin"];
					double? val = press == null || press == "-9999" ? null : ConvertPressINHGToUser(Convert.ToDouble(press, invNum));
					DoPressure(val, recDate);
					if (val == null)
						Cumulus.LogMessage($"ProcessData: Error, missing baro pressure");
					else
						UpdatePressureTrendString();
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Pressure data");
					context.Response.StatusCode = 500;
					return "Failed: Error in baro pressure data - " + ex.Message;
				}


				// === Indoor temp ===
				try
				{
					// indoortempf - [F indoor temperature F]

					var temp = data["indoortempf"];
					double? val = temp == null || temp == "-9999" ? null : ConvertTempFToUser(Convert.ToDouble(temp, invNum));
					DoIndoorTemp(val);
					if (val == null)
						Cumulus.LogMessage($"ProcessData: Error, missing indoor temp");
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Indoor temp data");
					context.Response.StatusCode = 500;
					return "Failed: Error in indoor temp data - " + ex.Message;
				}


				// === Outdoor temp ===
				try
				{
					// tempf - [F outdoor temperature]

					var temp = data["tempf"];
					double? val = temp == null || temp == "-9999" ? null : ConvertTempFToUser(Convert.ToDouble(temp, invNum));
					DoTemperature(val, recDate);
					if (val == null)
						Cumulus.LogMessage($"ProcessData: Error, missing outdoor temp");
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Outdoor temp data");
					context.Response.StatusCode = 500;
					return "Failed: Error in outdoor temp data - " + ex.Message;
				}


				// === Rain ===
				try
				{
					// rainin - [rain inches over the past hour)] --the accumulated rainfall in the past 60 min
					// dailyrainin - [rain inches so far today in local time]

					var rain = data["dailyrainin"];

					if (rain == null || rain == "-9999")
					{
						Cumulus.LogMessage($"ProcessData: Error, missing rainfall");
					}
					else
					{
						var rainVal = ConvertRainINToUser(Convert.ToDouble(rain, invNum));

						if (rainVal < previousRainCount)
						{
							// rain counter has reset
							rainCount += previousRainCount;
							previousRainCount = rainVal;
						}

						DoRain(rainCount + rainVal, 0, recDate);

					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Rain data");
					context.Response.StatusCode = 500;
					return "Failed: Error in rainfall data - " + ex.Message;
				}


				// === Dewpoint ===
				try
				{
					// dewptf - [F outdoor dewpoint F]

					var dewpnt = data["dewptf"];
					double? dpVal = dewpnt == null || dewpnt == "-9999" ? null : ConvertTempFToUser(Convert.ToDouble(dewpnt, invNum));
					DoDewpoint(dpVal, recDate);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Dew point data");
					context.Response.StatusCode = 500;
					return "Failed: Error in dew point data - " + ex.Message;
				}


				// === Wind Chill ===
				// - no w/c in wunderground data, so it must be set to CMX calculated
				DoWindChill(null, recDate);
				if (data["windspeedmph"] != null && data["tempf"] != null && data["windspeedmph"] != "-9999" && data["tempf"] != "-9999")
				{
					// === Apparent/Feels Like ===
					if (data["humidity"] != null && data["humidity"] != "-9999")
					{
						DoApparentTemp(recDate);
						DoFeelsLike(recDate);
					}
					else
					{
						Cumulus.LogMessage("ProcessData: Insufficient data to calculate Apparent/Feels like Temps");
					}
				}


				// === Humidex ===
				// - CMX calculated
				if (data["tempf"] != null && data["humidity"] != null && data["tempf"] != "-9999" && data["humidity"] != "-9999")
				{
					DoHumidex(recDate);
					DoCloudBaseHeatIndex(recDate);
				}
				else
				{
					Cumulus.LogMessage("ProcessData: Insufficient data to calculate Humidex");
				}

				DoForecast(string.Empty, false);


				// === Extra Temperature ===
				try
				{
					// temp[2-4]f

					for (var i = 2; i < 5; i++)
					{
						var str = data["temp" + i + "f"];
						double? temp = str != null && str != "-9999" ? Convert.ToDouble(str, invNum) : null;
						DoExtraTemp(ConvertTempFToUser(temp), i - 1);
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in extra temperature data");
				}


				// === Solar ===
				try
				{
					// solarradiation - [W/m^2]

					var str = data["solarradiation"];
					int? sol = str != null && str != "-9999" ? (int)Convert.ToDouble(str, invNum) : null;
					DoSolarRad(sol, recDate);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in solar data");
				}


				// === UV ===
				try
				{
					// UV - [index]

					var str = data["UV"];
					double? uv = str != null && str != "-9999" ? Convert.ToDouble(str, invNum) : null;
					DoUV(uv, recDate);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in UV data");
					DoUV(null, recDate);
				}


				// === Soil Temp ===
				try
				{
					// soiltempf - [F soil temperature]
					// soiltemp[2-4]f

					var str = data["soiltempf"];
					double? temp = str != null && str != "-9999" ? Convert.ToDouble(str, invNum) : null;
					DoSoilTemp(ConvertTempFToUser(temp), 1);

					for (var i = 2; i <= 4; i++)
					{
						str = data["soiltemp" + i + "f"];
						temp = str != null && str != "-9999" ? Convert.ToDouble(str, invNum) : null;
						DoSoilTemp(ConvertTempFToUser(temp), i);
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Soil temp data");
				}


				// === Soil Moisture ===
				try
				{
					// soilmoisture - [%]
					// soilmoisture[2-4]

					var str1 = data["soilmoisture"];
					int? moist = str1 != null && str1 != "-9999" ? (int)Convert.ToDouble(str1, invNum) : null;
					DoSoilMoisture(moist, 1);

					for (var i = 2; i <= 4; i++)
					{
						var str = data["soilmoisture" + i];
						moist = str != null && str != "-9999" ? (int)Convert.ToDouble(str, invNum) : null;
						DoSoilMoisture(moist, i);
					}
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Soil moisture data");
				}


				// === Leaf Wetness ===
				try
				{
					// leafwetness - [%]
					// leafwetness2

					var str = data["leafwetness"];
					int? wet = str != null && str != "-9999" ? (int)Convert.ToDouble(str, invNum) : null;
					DoLeafWetness(wet, 1);

					str = data["leafwetness2"];
					wet = str != null && str != "-9999" ? (int)Convert.ToDouble(str, invNum) : null;
					DoLeafWetness(wet, 2);
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Leaf wetness data");
				}


				// === Air Quality ===
				try
				{
					// AqPM2.5 - PM2.5 mass - UG / M3
					// AqPM10 - PM10 mass - PM10 mass

					var str2 = data["AqPM2.5"];
					CO2_pm2p5 = str2 != null && str2 != "-9999" ? Convert.ToDouble(str2, invNum) : null;

					var str10 = data["AqPM10"];
					CO2_pm10 = str10 != null && str10 != "-9999" ? Convert.ToDouble(str10, invNum) : null;
				}
				catch (Exception ex)
				{
					cumulus.LogExceptionMessage(ex, "ProcessData: Error in Air Quality data");
				}

				UpdateStatusPanel(recDate);
				UpdateMQTT();
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "ProcessData: Error");
				context.Response.StatusCode = 500;
				return "Failed: General error - " + ex.Message;
			}

			cumulus.LogDebugMessage($"ProcessData: Complete");

			context.Response.StatusCode = 200;
			return "success";
		}
	}
}
