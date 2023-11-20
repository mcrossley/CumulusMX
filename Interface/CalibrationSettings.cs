﻿using System;
using System.Globalization;
using System.IO;
using System.Net;
using EmbedIO;
using ServiceStack;

namespace CumulusMX
{
	public class CalibrationSettings
	{
		private readonly Cumulus cumulus;

		public CalibrationSettings(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		//public string UpdateCalibrationConfig(HttpListenerContext context)
		public string UpdateConfig(IHttpContext context)
		{
			var json = "";
			DataJson settings;
			var invC = new CultureInfo("");

			try
			{
				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				json = WebUtility.UrlDecode(data[5..]);

				// de-serialize it to the settings structure
				settings = json.FromJson<DataJson>();
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing Calibration Settings JSON";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Calibration Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}

			try
			{
				// process the settings
				Cumulus.LogMessage("Updating calibration settings");

				// offsets
				cumulus.Calib.Press.Offset = Convert.ToDouble(settings.pressure.offset, invC);
				cumulus.Calib.Temp.Offset = Convert.ToDouble(settings.temp.offset, invC);
				cumulus.Calib.InTemp.Offset = Convert.ToDouble(settings.tempin.offset, invC);
				cumulus.Calib.Hum.Offset = settings.hum.offset;
				cumulus.Calib.InHum.Offset = Convert.ToDouble(settings.humin.offset, invC);
				cumulus.Calib.WindDir.Offset = settings.winddir.offset;
				cumulus.Calib.Solar.Offset = Convert.ToDouble(settings.solar.offset);
				cumulus.Calib.UV.Offset = Convert.ToDouble(settings.uv.offset, invC);
				cumulus.Calib.WetBulb.Offset = Convert.ToDouble(settings.wetbulb.offset, invC);

				// multipliers
				cumulus.Calib.Press.Mult = Convert.ToDouble(settings.pressure.multiplier, invC);
				cumulus.Calib.WindSpeed.Mult = Convert.ToDouble(settings.windspd.multiplier, invC);
				cumulus.Calib.WindGust.Mult = Convert.ToDouble(settings.gust.multiplier, invC);
				cumulus.Calib.Temp.Mult = Convert.ToDouble(settings.temp.multiplier, invC);
				cumulus.Calib.InTemp.Mult = Convert.ToDouble(settings.tempin.multiplier, invC);
				cumulus.Calib.Hum.Mult = Convert.ToDouble(settings.hum.multiplier, invC);
				cumulus.Calib.InHum.Mult = Convert.ToDouble(settings.humin.multiplier, invC);
				cumulus.Calib.Rain.Mult = Convert.ToDouble(settings.rain.multiplier, invC);
				cumulus.Calib.Solar.Mult = Convert.ToDouble(settings.solar.multiplier, invC);
				cumulus.Calib.UV.Mult = Convert.ToDouble(settings.uv.multiplier, invC);
				cumulus.Calib.WetBulb.Mult = Convert.ToDouble(settings.wetbulb.multiplier, invC);

				//multipliers2
				cumulus.Calib.Press.Mult2 = Convert.ToDouble(settings.pressure.multiplier2, invC);
				cumulus.Calib.WindSpeed.Mult2 = Convert.ToDouble(settings.windspd.multiplier2, invC);
				cumulus.Calib.WindGust.Mult2 = Convert.ToDouble(settings.gust.multiplier2, invC);
				cumulus.Calib.Temp.Mult2 = Convert.ToDouble(settings.temp.multiplier2, invC);
				cumulus.Calib.InTemp.Mult2 = Convert.ToDouble(settings.tempin.multiplier2, invC);
				cumulus.Calib.Hum.Mult2 = Convert.ToDouble(settings.hum.multiplier2, invC);
				cumulus.Calib.InHum.Mult2 = Convert.ToDouble(settings.humin.multiplier2, invC);
				cumulus.Calib.Solar.Mult2 = Convert.ToDouble(settings.solar.multiplier2, invC);
				cumulus.Calib.UV.Mult2 = Convert.ToDouble(settings.uv.multiplier2, invC);
				cumulus.Calib.WetBulb.Mult2 = Convert.ToDouble(settings.wetbulb.multiplier2, invC);

				// spike removal
				cumulus.Spike.TempDiff = Convert.ToDouble(settings.temp.spike, invC);
				cumulus.Spike.HumidityDiff = Convert.ToDouble(settings.hum.spike, invC);
				cumulus.Spike.WindDiff = Convert.ToDouble(settings.windspd.spike, invC);
				cumulus.Spike.GustDiff = Convert.ToDouble(settings.gust.spike, invC);
				cumulus.Spike.MaxHourlyRain = Convert.ToDouble(settings.rain.spikehour, invC);
				cumulus.Spike.MaxRainRate = Convert.ToDouble(settings.rain.spikerate, invC);
				cumulus.Spike.PressDiff = Convert.ToDouble(settings.pressure.spike, invC);
				cumulus.Spike.InTempDiff = Convert.ToDouble(settings.tempin.spike, invC);
				cumulus.Spike.InHumDiff = Convert.ToDouble(settings.humin.spike, invC);

				// limits
				cumulus.Limit.TempHigh = Convert.ToDouble(settings.temp.limitmax, invC);
				cumulus.Limit.TempLow = Convert.ToDouble(settings.temp.limitmin, invC);
				cumulus.Limit.DewHigh = Convert.ToDouble(settings.dewpt.limitmax, invC);
				cumulus.Limit.PressHigh = Convert.ToDouble(settings.pressure.limitmax, invC);
				cumulus.Limit.PressLow = Convert.ToDouble(settings.pressure.limitmin, invC);
				cumulus.Limit.WindHigh = Convert.ToDouble(settings.gust.limitmax, invC);

				// Save the settings
				cumulus.WriteIniFile();

				// Clear the spike alarm
				cumulus.SpikeAlarm.Triggered = false;

				// Log the new values
				Cumulus.LogMessage("Setting new calibration values...");
				cumulus.LogOffsetsMultipliers();

				context.Response.StatusCode = 200;
			}
			catch (Exception ex)
			{
				var msg = "Error setting Calibration settings";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Calibration Data: " + json);
				context.Response.StatusCode = 500;
				return ex.Message;
			}
			return "success";
		}

		public string GetAlpacaFormData()
		{
			var pressure = new Calibrations()
			{
				offset = cumulus.Calib.Press.Offset,
				multiplier = cumulus.Calib.Press.Mult,
				multiplier2 = cumulus.Calib.Press.Mult2,
				spike = cumulus.Spike.PressDiff,
				limitmax = cumulus.Limit.PressHigh,
				limitmin = cumulus.Limit.PressLow
			};

			var temp = new Calibrations()
			{
				offset = cumulus.Calib.Temp.Offset,
				multiplier = cumulus.Calib.Temp.Mult,
				multiplier2 = cumulus.Calib.Temp.Mult2,
				spike = cumulus.Spike.TempDiff,
				limitmax = cumulus.Limit.TempHigh,
				limitmin = cumulus.Limit.TempLow,
			};

			var tempin = new Calibrations()
			{
				offset = cumulus.Calib.InTemp.Offset,
				multiplier = cumulus.Calib.InTemp.Mult,
				multiplier2 = cumulus.Calib.InTemp.Mult2,
				spike = cumulus.Spike.InTempDiff
			};

			var hum = new Calibrations()
			{
				offset = (int)cumulus.Calib.Hum.Offset,
				multiplier = cumulus.Calib.Hum.Mult,
				multiplier2 = cumulus.Calib.Hum.Mult2,
				spike = cumulus.Spike.HumidityDiff
			};

			var humin = new Calibrations()
			{
				offset = (int)cumulus.Calib.InHum.Offset,
				multiplier = cumulus.Calib.InHum.Mult,
				multiplier2 = cumulus.Calib.InHum.Mult2,
				spike = cumulus.Spike.InHumDiff
			};


			var windspd = new Calibrations()
			{
				multiplier = cumulus.Calib.WindSpeed.Mult,
				multiplier2 = cumulus.Calib.WindSpeed.Mult2,
				spike = cumulus.Spike.WindDiff
			};

			var gust = new Calibrations()
			{
				multiplier = cumulus.Calib.WindGust.Mult,
				multiplier2 = cumulus.Calib.WindGust.Mult2,
				spike = cumulus.Spike.GustDiff,
				limitmax = cumulus.Limit.WindHigh
			};

			var winddir = new Calibrations()
			{
				offset = (int)cumulus.Calib.WindDir.Offset
			};

			var rain = new Calibrations()
			{
				multiplier = cumulus.Calib.Rain.Mult,
				spikehour = cumulus.Spike.MaxHourlyRain,
				spikerate = cumulus.Spike.MaxRainRate
			};

			var solar = new Calibrations()
			{
				offset = cumulus.Calib.Solar.Offset,
				multiplier = cumulus.Calib.Solar.Mult,
				multiplier2 = cumulus.Calib.Solar.Mult2
			};

			var uv = new Calibrations()
			{
				offset = cumulus.Calib.UV.Offset,
				multiplier = cumulus.Calib.UV.Mult,
				multiplier2 = cumulus.Calib.UV.Mult2
			};

			var wetbulb = new Calibrations()
			{
				offset = cumulus.Calib.WetBulb.Offset,
				multiplier = cumulus.Calib.WetBulb.Mult,
				multiplier2 = cumulus.Calib.WetBulb.Mult2
			};

			var dewpt = new Calibrations()
			{
				limitmax = cumulus.Limit.DewHigh
			};

			var data = new DataJson()
			{
				accessible = cumulus.ProgramOptions.EnableAccessibility,
				pressure = pressure,
				temp = temp,
				tempin = tempin,
				hum = hum,
				humin = humin,
				windspd = windspd,
				gust = gust,
				winddir = winddir,
				rain = rain,
				solar = solar,
				uv = uv,
				wetbulb = wetbulb,
				dewpt = dewpt
			};

			return data.ToJson();
		}


		private class DataJson
		{
			public bool accessible { get; set; }
			public Calibrations pressure { get; set; }
			public Calibrations temp { get; set; }
			public Calibrations tempin { get; set; }
			public Calibrations hum { get; set; }
			public Calibrations humin { get; set; }
			public Calibrations windspd { get; set; }
			public Calibrations gust { get; set; }
			public Calibrations winddir { get; set; }
			public Calibrations rain { get; set; }
			public Calibrations solar { get; set; }
			public Calibrations uv { get; set; }
			public Calibrations wetbulb { get; set; }
			public Calibrations dewpt { get; set; }
		}


		private class Calibrations
		{
			public double offset { get; set; }
			public double multiplier { get; set; }
			public double multiplier2 { get; set; }
			public double spike { get; set; }
			public double spikerate { get; set; }
			public double spikehour { get; set; }
			public double limitmin { get; set; }
			public double limitmax { get; set; }
		}
	}
}
