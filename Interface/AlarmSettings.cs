﻿using System;
using System.IO;
using System.Net;

using EmbedIO;

using ServiceStack;

namespace CumulusMX
{
	public class AlarmSettings
	{
		private readonly Cumulus cumulus;

		public AlarmSettings(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		public string GetSettings()
		{
			var alarmUnits = new UnitsJson()
			{
				tempUnits = cumulus.Units.TempText,
				pressUnits = cumulus.Units.PressText,
				rainUnits = cumulus.Units.RainText,
				windUnits = cumulus.Units.WindText
			};

			var data = new DataJson()
			{
				tempBelow = new ValuesJson()
				{
					Enabled = cumulus.LowTempAlarm.Enabled,
					Val = cumulus.LowTempAlarm.Value,
					SoundEnabled = cumulus.LowTempAlarm.Sound,
					Sound = cumulus.LowTempAlarm.SoundFile,
					Notify = cumulus.LowTempAlarm.Notify,
					Email = cumulus.LowTempAlarm.Email,
					Latches = cumulus.LowTempAlarm.Latch,
					LatchHrs = cumulus.LowTempAlarm.LatchHours,
					Action = cumulus.LowTempAlarm.Action,
					ActionParams = cumulus.LowTempAlarm.ActionParams
				},
				tempAbove = new ValuesJson()
				{
					Enabled = cumulus.HighTempAlarm.Enabled,
					Val = cumulus.HighTempAlarm.Value,
					SoundEnabled = cumulus.HighTempAlarm.Sound,
					Sound = cumulus.HighTempAlarm.SoundFile,
					Notify = cumulus.HighTempAlarm.Notify,
					Email = cumulus.HighTempAlarm.Email,
					Latches = cumulus.HighTempAlarm.Latch,
					LatchHrs = cumulus.HighTempAlarm.LatchHours,
					Action = cumulus.HighTempAlarm.Action,
					ActionParams = cumulus.HighTempAlarm.ActionParams
				},
				tempChange = new ValuesJson()
				{
					Enabled = cumulus.TempChangeAlarm.Enabled,
					Val = cumulus.TempChangeAlarm.Value,
					SoundEnabled = cumulus.TempChangeAlarm.Sound,
					Sound = cumulus.TempChangeAlarm.SoundFile,
					Notify = cumulus.TempChangeAlarm.Notify,
					Email = cumulus.TempChangeAlarm.Email,
					Latches = cumulus.TempChangeAlarm.Latch,
					LatchHrs = cumulus.TempChangeAlarm.LatchHours,
					Action = cumulus.TempChangeAlarm.Action,
					ActionParams = cumulus.TempChangeAlarm.ActionParams
				},
				pressBelow = new ValuesJson()
				{
					Enabled = cumulus.LowPressAlarm.Enabled,
					Val = cumulus.LowPressAlarm.Value,
					SoundEnabled = cumulus.LowPressAlarm.Sound,
					Sound = cumulus.LowPressAlarm.SoundFile,
					Notify = cumulus.LowPressAlarm.Notify,
					Email = cumulus.LowPressAlarm.Email,
					Latches = cumulus.LowPressAlarm.Latch,
					LatchHrs = cumulus.LowPressAlarm.LatchHours,
					Action = cumulus.LowPressAlarm.Action,
					ActionParams = cumulus.LowPressAlarm.ActionParams
				},
				pressAbove = new ValuesJson()
				{
					Enabled = cumulus.HighPressAlarm.Enabled,
					Val = cumulus.HighPressAlarm.Value,
					SoundEnabled = cumulus.HighPressAlarm.Sound,
					Sound = cumulus.HighPressAlarm.SoundFile,
					Notify = cumulus.HighPressAlarm.Notify,
					Email = cumulus.HighPressAlarm.Email,
					Latches = cumulus.HighPressAlarm.Latch,
					LatchHrs = cumulus.HighPressAlarm.LatchHours,
					Action = cumulus.HighPressAlarm.Action,
					ActionParams = cumulus.HighPressAlarm.ActionParams
				},
				pressChange = new ValuesJson()
				{
					Enabled = cumulus.PressChangeAlarm.Enabled,
					Val = cumulus.PressChangeAlarm.Value,
					SoundEnabled = cumulus.PressChangeAlarm.Sound,
					Sound = cumulus.PressChangeAlarm.SoundFile,
					Notify = cumulus.PressChangeAlarm.Notify,
					Email = cumulus.PressChangeAlarm.Email,
					Latches = cumulus.PressChangeAlarm.Latch,
					LatchHrs = cumulus.PressChangeAlarm.LatchHours,
					Action = cumulus.PressChangeAlarm.Action,
					ActionParams = cumulus.PressChangeAlarm.ActionParams
				},
				rainAbove = new ValuesJson()
				{
					Enabled = cumulus.HighRainTodayAlarm.Enabled,
					Val = cumulus.HighRainTodayAlarm.Value,
					SoundEnabled = cumulus.HighRainTodayAlarm.Sound,
					Sound = cumulus.HighRainTodayAlarm.SoundFile,
					Notify = cumulus.HighRainTodayAlarm.Notify,
					Email = cumulus.HighRainTodayAlarm.Email,
					Latches = cumulus.HighRainTodayAlarm.Latch,
					LatchHrs = cumulus.HighRainTodayAlarm.LatchHours,
					Action = cumulus.HighRainTodayAlarm.Action,
					ActionParams = cumulus.HighRainTodayAlarm.ActionParams
				},
				rainRateAbove = new ValuesJson()
				{
					Enabled = cumulus.HighRainRateAlarm.Enabled,
					Val = cumulus.HighRainRateAlarm.Value,
					SoundEnabled = cumulus.HighRainRateAlarm.Sound,
					Sound = cumulus.HighRainRateAlarm.SoundFile,
					Notify = cumulus.HighRainRateAlarm.Notify,
					Email = cumulus.HighRainRateAlarm.Email,
					Latches = cumulus.HighRainRateAlarm.Latch,
					LatchHrs = cumulus.HighRainRateAlarm.LatchHours,
					Action = cumulus.HighRainRateAlarm.Action,
					ActionParams = cumulus.HighRainRateAlarm.ActionParams
				},
				isRaining = new ValuesJson()
				{
					Enabled = cumulus.IsRainingAlarm.Enabled,
					SoundEnabled = cumulus.IsRainingAlarm.Sound,
					Sound = cumulus.IsRainingAlarm.SoundFile,
					Notify = cumulus.IsRainingAlarm.Notify,
					Email = cumulus.IsRainingAlarm.Email,
					Latches = cumulus.IsRainingAlarm.Latch,
					LatchHrs = cumulus.IsRainingAlarm.LatchHours,
					Threshold = cumulus.IsRainingAlarm.TriggerThreshold,
					Action = cumulus.IsRainingAlarm.Action,
					ActionParams = cumulus.IsRainingAlarm.ActionParams
				},
				gustAbove = new ValuesJson()
				{
					Enabled = cumulus.HighGustAlarm.Enabled,
					Val = cumulus.HighGustAlarm.Value,
					SoundEnabled = cumulus.HighGustAlarm.Sound,
					Sound = cumulus.HighGustAlarm.SoundFile,
					Notify = cumulus.HighGustAlarm.Notify,
					Email = cumulus.HighGustAlarm.Email,
					Latches = cumulus.HighGustAlarm.Latch,
					LatchHrs = cumulus.HighGustAlarm.LatchHours,
					Action = cumulus.HighGustAlarm.Action,
					ActionParams = cumulus.HighGustAlarm.ActionParams
				},
				windAbove = new ValuesJson()
				{
					Enabled = cumulus.HighWindAlarm.Enabled,
					Val = cumulus.HighWindAlarm.Value,
					SoundEnabled = cumulus.HighWindAlarm.Sound,
					Sound = cumulus.HighWindAlarm.SoundFile,
					Notify = cumulus.HighWindAlarm.Notify,
					Email = cumulus.HighWindAlarm.Email,
					Latches = cumulus.HighWindAlarm.Latch,
					LatchHrs = cumulus.HighWindAlarm.LatchHours,
					Action = cumulus.HighWindAlarm.Action,
					ActionParams = cumulus.HighWindAlarm.ActionParams
				},
				contactLost = new ValuesJson()
				{
					Enabled = cumulus.SensorAlarm.Enabled,
					SoundEnabled = cumulus.SensorAlarm.Sound,
					Sound = cumulus.SensorAlarm.SoundFile,
					Notify = cumulus.SensorAlarm.Notify,
					Email = cumulus.SensorAlarm.Email,
					Latches = cumulus.SensorAlarm.Latch,
					LatchHrs = cumulus.SensorAlarm.LatchHours,
					Threshold = cumulus.SensorAlarm.TriggerThreshold,
					Action = cumulus.SensorAlarm.Action,
					ActionParams = cumulus.SensorAlarm.ActionParams
				},
				newRecord = new ValuesJson()
				{
					Enabled = cumulus.NewRecordAlarm.Enabled,
					SoundEnabled = cumulus.NewRecordAlarm.Sound,
					Sound = cumulus.NewRecordAlarm.SoundFile,
					Notify = cumulus.NewRecordAlarm.Notify,
					Email = cumulus.NewRecordAlarm.Email,
					Latches = cumulus.NewRecordAlarm.Latch,
					LatchHrs = cumulus.NewRecordAlarm.LatchHours,
					Threshold = cumulus.NewRecordAlarm.TriggerThreshold,
					Action = cumulus.NewRecordAlarm.Action,
					ActionParams = cumulus.NewRecordAlarm.ActionParams
				},
				dataStopped = new ValuesJson()
				{
					Enabled = cumulus.DataStoppedAlarm.Enabled,
					SoundEnabled = cumulus.DataStoppedAlarm.Sound,
					Sound = cumulus.DataStoppedAlarm.SoundFile,
					Notify = cumulus.DataStoppedAlarm.Notify,
					Email = cumulus.DataStoppedAlarm.Email,
					Latches = cumulus.DataStoppedAlarm.Latch,
					LatchHrs = cumulus.DataStoppedAlarm.LatchHours,
					Threshold = cumulus.DataStoppedAlarm.TriggerThreshold,
					Action = cumulus.DataStoppedAlarm.Action,
					ActionParams = cumulus.DataStoppedAlarm.ActionParams
				},
				batteryLow = new ValuesJson()
				{
					Enabled = cumulus.BatteryLowAlarm.Enabled,
					SoundEnabled = cumulus.BatteryLowAlarm.Sound,
					Sound = cumulus.BatteryLowAlarm.SoundFile,
					Notify = cumulus.BatteryLowAlarm.Notify,
					Email = cumulus.BatteryLowAlarm.Email,
					Latches = cumulus.BatteryLowAlarm.Latch,
					LatchHrs = cumulus.BatteryLowAlarm.LatchHours,
					Threshold = cumulus.BatteryLowAlarm.TriggerThreshold,
					Action = cumulus.BatteryLowAlarm.Action,
					ActionParams = cumulus.BatteryLowAlarm.ActionParams
				},
				spike = new ValuesJson()
				{
					Enabled = cumulus.SpikeAlarm.Enabled,
					SoundEnabled = cumulus.SpikeAlarm.Sound,
					Sound = cumulus.SpikeAlarm.SoundFile,
					Notify = cumulus.SpikeAlarm.Notify,
					Email = cumulus.SpikeAlarm.Email,
					Latches = cumulus.SpikeAlarm.Latch,
					LatchHrs = cumulus.SpikeAlarm.LatchHours,
					Threshold = cumulus.SpikeAlarm.TriggerThreshold,
					Action = cumulus.SpikeAlarm.Action,
					ActionParams = cumulus.SpikeAlarm.ActionParams
				},
				upgrade = new ValuesJson()
				{
					Enabled = cumulus.UpgradeAlarm.Enabled,
					SoundEnabled = cumulus.UpgradeAlarm.Sound,
					Sound = cumulus.UpgradeAlarm.SoundFile,
					Notify = cumulus.UpgradeAlarm.Notify,
					Email = cumulus.UpgradeAlarm.Email,
					Latches = cumulus.UpgradeAlarm.Latch,
					LatchHrs = cumulus.UpgradeAlarm.LatchHours,
					Action = cumulus.UpgradeAlarm.Action,
					ActionParams = cumulus.UpgradeAlarm.ActionParams
				},
				thirdParty = new ValuesJson()
				{
					Enabled = cumulus.ThirdPartyUploadAlarm.Enabled,
					SoundEnabled = cumulus.ThirdPartyUploadAlarm.Sound,
					Sound = cumulus.ThirdPartyUploadAlarm.SoundFile,
					Notify = cumulus.ThirdPartyUploadAlarm.Notify,
					Email = cumulus.ThirdPartyUploadAlarm.Email,
					Latches = cumulus.ThirdPartyUploadAlarm.Latch,
					LatchHrs = cumulus.ThirdPartyUploadAlarm.LatchHours,
					Threshold = cumulus.ThirdPartyUploadAlarm.TriggerThreshold,
					Action = cumulus.ThirdPartyUploadAlarm.Action,
					ActionParams = cumulus.ThirdPartyUploadAlarm.ActionParams
				},
				mySqlUpload = new ValuesJson()
				{
					Enabled = cumulus.MySqlUploadAlarm.Enabled,
					SoundEnabled = cumulus.MySqlUploadAlarm.Sound,
					Sound = cumulus.MySqlUploadAlarm.SoundFile,
					Notify = cumulus.MySqlUploadAlarm.Notify,
					Email = cumulus.MySqlUploadAlarm.Email,
					Latches = cumulus.MySqlUploadAlarm.Latch,
					LatchHrs = cumulus.MySqlUploadAlarm.LatchHours,
					Threshold = cumulus.MySqlUploadAlarm.TriggerThreshold,
					Action = cumulus.MySqlUploadAlarm.Action,
					ActionParams = cumulus.MySqlUploadAlarm.ActionParams
				},
				ftpUpload = new ValuesJson()
				{
					Enabled = cumulus.FtpAlarm.Enabled,
					SoundEnabled = cumulus.FtpAlarm.Sound,
					Sound = cumulus.FtpAlarm.SoundFile,
					Notify = cumulus.FtpAlarm.Notify,
					Email = cumulus.FtpAlarm.Email,
					Latches = cumulus.FtpAlarm.Latch,
					LatchHrs = cumulus.FtpAlarm.LatchHours,
					Threshold = cumulus.FtpAlarm.TriggerThreshold,
					Action = cumulus.FtpAlarm.Action,
					ActionParams = cumulus.FtpAlarm.ActionParams
				}
			};

			var email = new EmailJson()
			{
				fromEmail = cumulus.AlarmFromEmail,
				destEmail = cumulus.AlarmDestEmail.Join(";"),
				useHtml = cumulus.AlarmEmailHtml,
				useBcc = cumulus.AlarmEmailUseBcc
			};

			var retObject = new SettingsJson()
			{
				data = data,
				units = alarmUnits,
				email = email
			};

			return retObject.ToJson();
		}

		public string GetAlarmInfo()
		{
			//var InvC = new CultureInfo("");

			var data = new InfoDataJson()
			{
				tempBelow = new InfoJson()
				{
					Enabled = cumulus.LowTempAlarm.Enabled,
					SoundEnabled = cumulus.LowTempAlarm.Sound,
					Sound = cumulus.LowTempAlarm.SoundFile,
					Notify = cumulus.LowTempAlarm.Notify
				},
				tempAbove = new InfoJson()
				{
					Enabled = cumulus.HighTempAlarm.Enabled,
					SoundEnabled = cumulus.HighTempAlarm.Sound,
					Sound = cumulus.HighTempAlarm.SoundFile,
					Notify = cumulus.HighTempAlarm.Notify
				},
				tempChange = new InfoJson()
				{
					Enabled = cumulus.TempChangeAlarm.Enabled,
					SoundEnabled = cumulus.TempChangeAlarm.Sound,
					Sound = cumulus.TempChangeAlarm.SoundFile,
					Notify = cumulus.TempChangeAlarm.Notify
				},
				pressBelow = new InfoJson()
				{
					Enabled = cumulus.LowPressAlarm.Enabled,
					SoundEnabled = cumulus.LowPressAlarm.Sound,
					Sound = cumulus.LowPressAlarm.SoundFile,
					Notify = cumulus.LowPressAlarm.Notify
				},
				pressAbove = new InfoJson()
				{
					Enabled = cumulus.HighPressAlarm.Enabled,
					SoundEnabled = cumulus.HighPressAlarm.Sound,
					Sound = cumulus.HighPressAlarm.SoundFile,
					Notify = cumulus.HighPressAlarm.Notify
				},
				pressChange = new InfoJson()
				{
					Enabled = cumulus.PressChangeAlarm.Enabled,
					SoundEnabled = cumulus.PressChangeAlarm.Sound,
					Sound = cumulus.PressChangeAlarm.SoundFile,
					Notify = cumulus.PressChangeAlarm.Notify
				},
				rainAbove = new InfoJson()
				{
					Enabled = cumulus.HighRainTodayAlarm.Enabled,
					SoundEnabled = cumulus.HighRainTodayAlarm.Sound,
					Sound = cumulus.HighRainTodayAlarm.SoundFile,
					Notify = cumulus.HighRainTodayAlarm.Notify
				},
				rainRateAbove = new InfoJson()
				{
					Enabled = cumulus.HighRainRateAlarm.Enabled,
					SoundEnabled = cumulus.HighRainRateAlarm.Sound,
					Sound = cumulus.HighRainRateAlarm.SoundFile,
					Notify = cumulus.HighRainRateAlarm.Notify
				},
				isRaining = new InfoJson()
				{
					Enabled = cumulus.IsRainingAlarm.Enabled,
					SoundEnabled = cumulus.IsRainingAlarm.Sound,
					Sound = cumulus.IsRainingAlarm.SoundFile,
					Notify = cumulus.IsRainingAlarm.Notify
				},
				gustAbove = new InfoJson()
				{
					Enabled = cumulus.HighGustAlarm.Enabled,
					SoundEnabled = cumulus.HighGustAlarm.Sound,
					Sound = cumulus.HighGustAlarm.SoundFile,
					Notify = cumulus.HighGustAlarm.Notify
				},
				windAbove = new InfoJson()
				{
					Enabled = cumulus.HighWindAlarm.Enabled,
					SoundEnabled = cumulus.HighWindAlarm.Sound,
					Sound = cumulus.HighWindAlarm.SoundFile,
					Notify = cumulus.HighWindAlarm.Notify
				},
				contactLost = new InfoJson()
				{
					Enabled = cumulus.SensorAlarm.Enabled,
					SoundEnabled = cumulus.SensorAlarm.Sound,
					Sound = cumulus.SensorAlarm.SoundFile,
					Notify = cumulus.SensorAlarm.Notify
				},
				newRecord = new InfoJson()
				{
					Enabled = cumulus.NewRecordAlarm.Enabled,
					SoundEnabled = cumulus.NewRecordAlarm.Sound,
					Sound = cumulus.NewRecordAlarm.SoundFile,
					Notify = cumulus.NewRecordAlarm.Notify
				},
				dataStopped = new InfoJson()
				{
					Enabled = cumulus.DataStoppedAlarm.Enabled,
					SoundEnabled = cumulus.DataStoppedAlarm.Sound,
					Sound = cumulus.DataStoppedAlarm.SoundFile,
					Notify = cumulus.DataStoppedAlarm.Notify
				},
				batteryLow = new InfoJson()
				{
					Enabled = cumulus.BatteryLowAlarm.Enabled,
					SoundEnabled = cumulus.BatteryLowAlarm.Sound,
					Sound = cumulus.BatteryLowAlarm.SoundFile,
					Notify = cumulus.BatteryLowAlarm.Notify
				},
				spike = new InfoJson()
				{
					Enabled = cumulus.SpikeAlarm.Enabled,
					SoundEnabled = cumulus.SpikeAlarm.Sound,
					Sound = cumulus.SpikeAlarm.SoundFile,
					Notify = cumulus.SpikeAlarm.Notify
				},
				upgrade = new InfoJson()
				{
					Enabled = cumulus.UpgradeAlarm.Enabled,
					SoundEnabled = cumulus.UpgradeAlarm.Sound,
					Sound = cumulus.UpgradeAlarm.SoundFile,
					Notify = cumulus.UpgradeAlarm.Notify
				},
				httpUpload = new InfoJson()
				{
					Enabled = cumulus.ThirdPartyUploadAlarm.Enabled,
					SoundEnabled = cumulus.ThirdPartyUploadAlarm.Sound,
					Sound = cumulus.ThirdPartyUploadAlarm.SoundFile,
					Notify = cumulus.ThirdPartyUploadAlarm.Notify
				},
				mySqlUpload = new InfoJson()
				{
					Enabled = cumulus.MySqlUploadAlarm.Enabled,
					SoundEnabled = cumulus.MySqlUploadAlarm.Sound,
					Sound = cumulus.MySqlUploadAlarm.SoundFile,
					Notify = cumulus.MySqlUploadAlarm.Notify
				},
				ftpUpload = new InfoJson()
				{
					Enabled = cumulus.FtpAlarm.Enabled,
					SoundEnabled = cumulus.FtpAlarm.Sound,
					Sound = cumulus.FtpAlarm.SoundFile,
					Notify = cumulus.FtpAlarm.Notify
				}
			};

			return data.ToJson();
		}


		public string UpdateSettings(IHttpContext context)
		{
			var json = "";
			SettingsJson result;
			DataJson settings;

			try
			{
				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				json = WebUtility.UrlDecode(data);

				// de-serialize it to the settings structure
				//var settings = JsonConvert.DeserializeObject<JsonAlarmSettingsData>(json);
				//var settings = JsonSerializer.DeserializeFromString<JsonAlarmSettingsData>(json);

				result = json.FromJson<SettingsJson>();
				settings = result.data;
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing Alarm Settings JSON";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Alarm Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}

			try
			{
				// process the settings
				cumulus.LogMessage("Updating Alarm settings");

				var emailRequired = false;

				cumulus.LowTempAlarm.Enabled = settings.tempBelow.Enabled;
				cumulus.LowTempAlarm.Value = settings.tempBelow.Val;
				cumulus.LowTempAlarm.Sound = settings.tempBelow.SoundEnabled;
				cumulus.LowTempAlarm.SoundFile = settings.tempBelow.Sound.Trim();
				cumulus.LowTempAlarm.Notify = settings.tempBelow.Notify;
				cumulus.LowTempAlarm.Email = settings.tempBelow.Email;
				cumulus.LowTempAlarm.Latch = settings.tempBelow.Latches;
				cumulus.LowTempAlarm.LatchHours = settings.tempBelow.LatchHrs;
				emailRequired = cumulus.LowTempAlarm.Email && cumulus.LowTempAlarm.Enabled;
				cumulus.LowTempAlarm.Action = settings.tempBelow.Action.Trim();
				cumulus.LowTempAlarm.ActionParams = settings.tempBelow.ActionParams.Trim();

				cumulus.HighTempAlarm.Enabled = settings.tempAbove.Enabled;
				cumulus.HighTempAlarm.Value = settings.tempAbove.Val;
				cumulus.HighTempAlarm.Sound = settings.tempAbove.SoundEnabled;
				cumulus.HighTempAlarm.SoundFile = settings.tempAbove.Sound.Trim();
				cumulus.HighTempAlarm.Notify = settings.tempAbove.Notify;
				cumulus.HighTempAlarm.Email = settings.tempAbove.Email;
				cumulus.HighTempAlarm.Latch = settings.tempAbove.Latches;
				cumulus.HighTempAlarm.LatchHours = settings.tempAbove.LatchHrs;
				emailRequired = emailRequired || (cumulus.HighTempAlarm.Email && cumulus.HighTempAlarm.Enabled);
				cumulus.HighTempAlarm.Action = settings.tempAbove.Action.Trim();
				cumulus.HighTempAlarm.ActionParams = settings.tempAbove.ActionParams.Trim();

				cumulus.TempChangeAlarm.Enabled = settings.tempChange.Enabled;
				cumulus.TempChangeAlarm.Value = settings.tempChange.Val;
				cumulus.TempChangeAlarm.Sound = settings.tempChange.SoundEnabled;
				cumulus.TempChangeAlarm.SoundFile = settings.tempChange.Sound.Trim();
				cumulus.TempChangeAlarm.Notify = settings.tempChange.Notify;
				cumulus.TempChangeAlarm.Email = settings.tempChange.Email;
				cumulus.TempChangeAlarm.Latch = settings.tempChange.Latches;
				cumulus.TempChangeAlarm.LatchHours = settings.tempChange.LatchHrs;
				emailRequired = emailRequired || (cumulus.TempChangeAlarm.Email && cumulus.TempChangeAlarm.Enabled);
				cumulus.TempChangeAlarm.Action = settings.tempChange.Action.Trim();
				cumulus.TempChangeAlarm.ActionParams = settings.tempChange.ActionParams.Trim();

				cumulus.LowPressAlarm.Enabled = settings.pressBelow.Enabled;
				cumulus.LowPressAlarm.Value = settings.pressBelow.Val;
				cumulus.LowPressAlarm.Sound = settings.pressBelow.SoundEnabled;
				cumulus.LowPressAlarm.SoundFile = settings.pressBelow.Sound.Trim();
				cumulus.LowPressAlarm.Notify = settings.pressBelow.Notify;
				cumulus.LowPressAlarm.Email = settings.pressBelow.Email;
				cumulus.LowPressAlarm.Latch = settings.pressBelow.Latches;
				cumulus.LowPressAlarm.LatchHours = settings.pressBelow.LatchHrs;
				emailRequired = emailRequired || (cumulus.LowPressAlarm.Email && cumulus.LowPressAlarm.Enabled);
				cumulus.LowPressAlarm.Action = settings.pressBelow.Action.Trim();
				cumulus.LowPressAlarm.ActionParams = settings.pressBelow.ActionParams.Trim();

				cumulus.HighPressAlarm.Enabled = settings.pressAbove.Enabled;
				cumulus.HighPressAlarm.Value = settings.pressAbove.Val;
				cumulus.HighPressAlarm.Sound = settings.pressAbove.SoundEnabled;
				cumulus.HighPressAlarm.SoundFile = settings.pressAbove.Sound.Trim();
				cumulus.HighPressAlarm.Notify = settings.pressAbove.Notify;
				cumulus.HighPressAlarm.Email = settings.pressAbove.Email;
				cumulus.HighPressAlarm.Latch = settings.pressAbove.Latches;
				cumulus.HighPressAlarm.LatchHours = settings.pressAbove.LatchHrs;
				emailRequired = emailRequired || (cumulus.HighPressAlarm.Email && cumulus.HighPressAlarm.Enabled);
				cumulus.HighPressAlarm.Action = settings.pressAbove.Action.Trim();
				cumulus.HighPressAlarm.ActionParams = settings.pressAbove.ActionParams.Trim();

				cumulus.PressChangeAlarm.Enabled = settings.pressChange.Enabled;
				cumulus.PressChangeAlarm.Value = settings.pressChange.Val;
				cumulus.PressChangeAlarm.Sound = settings.pressChange.SoundEnabled;
				cumulus.PressChangeAlarm.SoundFile = settings.pressChange.Sound.Trim();
				cumulus.PressChangeAlarm.Notify = settings.pressChange.Notify;
				cumulus.PressChangeAlarm.Email = settings.pressChange.Email;
				cumulus.PressChangeAlarm.Latch = settings.pressChange.Latches;
				cumulus.PressChangeAlarm.LatchHours = settings.pressChange.LatchHrs;
				emailRequired = emailRequired || (cumulus.PressChangeAlarm.Email && cumulus.PressChangeAlarm.Enabled);
				cumulus.PressChangeAlarm.Action = settings.pressChange.Action.Trim();
				cumulus.PressChangeAlarm.ActionParams = settings.pressChange.ActionParams.Trim();

				cumulus.HighRainTodayAlarm.Enabled = settings.rainAbove.Enabled;
				cumulus.HighRainTodayAlarm.Value = settings.rainAbove.Val;
				cumulus.HighRainTodayAlarm.Sound = settings.rainAbove.SoundEnabled;
				cumulus.HighRainTodayAlarm.SoundFile = settings.rainAbove.Sound.Trim();
				cumulus.HighRainTodayAlarm.Notify = settings.rainAbove.Notify;
				cumulus.HighRainTodayAlarm.Email = settings.rainAbove.Email;
				cumulus.HighRainTodayAlarm.Latch = settings.rainAbove.Latches;
				cumulus.HighRainTodayAlarm.LatchHours = settings.rainAbove.LatchHrs;
				emailRequired = emailRequired || (cumulus.HighRainTodayAlarm.Email && cumulus.HighRainTodayAlarm.Enabled);
				cumulus.HighRainTodayAlarm.Action = settings.rainAbove.Action.Trim();
				cumulus.HighRainTodayAlarm.ActionParams = settings.rainAbove.ActionParams.Trim();

				cumulus.HighRainRateAlarm.Enabled = settings.rainRateAbove.Enabled;
				cumulus.HighRainRateAlarm.Value = settings.rainRateAbove.Val;
				cumulus.HighRainRateAlarm.Sound = settings.rainRateAbove.SoundEnabled;
				cumulus.HighRainRateAlarm.SoundFile = settings.rainRateAbove.Sound.Trim();
				cumulus.HighRainRateAlarm.Notify = settings.rainRateAbove.Notify;
				cumulus.HighRainRateAlarm.Email = settings.rainRateAbove.Email;
				cumulus.HighRainRateAlarm.Latch = settings.rainRateAbove.Latches;
				cumulus.HighRainRateAlarm.LatchHours = settings.rainRateAbove.LatchHrs;
				emailRequired = emailRequired || (cumulus.HighRainRateAlarm.Email && cumulus.HighRainRateAlarm.Enabled);
				cumulus.HighRainRateAlarm.Action = settings.rainRateAbove.Action.Trim();
				cumulus.HighRainRateAlarm.ActionParams = settings.rainRateAbove.ActionParams.Trim();

				cumulus.IsRainingAlarm.Enabled = settings.isRaining.Enabled;
				cumulus.IsRainingAlarm.Sound = settings.isRaining.SoundEnabled;
				cumulus.IsRainingAlarm.SoundFile = settings.isRaining.Sound.Trim();
				cumulus.IsRainingAlarm.Notify = settings.isRaining.Notify;
				cumulus.IsRainingAlarm.Email = settings.isRaining.Email;
				cumulus.IsRainingAlarm.Latch = settings.isRaining.Latches;
				cumulus.IsRainingAlarm.LatchHours = settings.isRaining.LatchHrs;
				emailRequired = emailRequired || (cumulus.IsRainingAlarm.Email && cumulus.IsRainingAlarm.Enabled);
				cumulus.IsRainingAlarm.Action = settings.isRaining.Action.Trim();
				cumulus.IsRainingAlarm.ActionParams = settings.isRaining.ActionParams.Trim();

				cumulus.HighGustAlarm.Enabled = settings.gustAbove.Enabled;
				cumulus.HighGustAlarm.Value = settings.gustAbove.Val;
				cumulus.HighGustAlarm.Sound = settings.gustAbove.SoundEnabled;
				cumulus.HighGustAlarm.SoundFile = settings.gustAbove.Sound.Trim();
				cumulus.HighGustAlarm.Notify = settings.gustAbove.Notify;
				cumulus.HighGustAlarm.Email = settings.gustAbove.Email;
				cumulus.HighGustAlarm.Latch = settings.gustAbove.Latches;
				cumulus.HighGustAlarm.LatchHours = settings.gustAbove.LatchHrs;
				emailRequired = emailRequired || (cumulus.HighGustAlarm.Email && cumulus.HighGustAlarm.Enabled);
				cumulus.HighGustAlarm.Action = settings.gustAbove.Action.Trim();
				cumulus.HighGustAlarm.ActionParams = settings.gustAbove.ActionParams.Trim();

				cumulus.HighWindAlarm.Enabled = settings.windAbove.Enabled;
				cumulus.HighWindAlarm.Value = settings.windAbove.Val;
				cumulus.HighWindAlarm.Sound = settings.windAbove.SoundEnabled;
				cumulus.HighWindAlarm.SoundFile = settings.windAbove.Sound.Trim();
				cumulus.HighWindAlarm.Notify = settings.windAbove.Notify;
				cumulus.HighWindAlarm.Email = settings.windAbove.Email;
				cumulus.HighWindAlarm.Latch = settings.windAbove.Latches;
				cumulus.HighWindAlarm.LatchHours = settings.windAbove.LatchHrs;
				emailRequired = emailRequired || (cumulus.HighWindAlarm.Email && cumulus.HighWindAlarm.Enabled);
				cumulus.HighWindAlarm.Action = settings.windAbove.Action.Trim();
				cumulus.HighWindAlarm.ActionParams = settings.windAbove.ActionParams.Trim();

				cumulus.NewRecordAlarm.Enabled = settings.newRecord.Enabled;
				cumulus.NewRecordAlarm.Sound = settings.newRecord.SoundEnabled;
				cumulus.NewRecordAlarm.SoundFile = settings.newRecord.Sound.Trim();
				cumulus.NewRecordAlarm.Notify = settings.newRecord.Notify;
				cumulus.NewRecordAlarm.Email = settings.newRecord.Email;
				cumulus.NewRecordAlarm.Latch = settings.newRecord.Latches;
				cumulus.NewRecordAlarm.LatchHours = settings.newRecord.LatchHrs;
				cumulus.NewRecordAlarm.TriggerThreshold = settings.newRecord.Threshold;
				emailRequired = emailRequired || (cumulus.NewRecordAlarm.Email && cumulus.NewRecordAlarm.Enabled);
				cumulus.NewRecordAlarm.Action = settings.newRecord.Action.Trim();
				cumulus.NewRecordAlarm.ActionParams = settings.newRecord.ActionParams.Trim();

				cumulus.SensorAlarm.Enabled = settings.contactLost.Enabled;
				cumulus.SensorAlarm.Sound = settings.contactLost.SoundEnabled;
				cumulus.SensorAlarm.SoundFile = settings.contactLost.Sound.Trim();
				cumulus.SensorAlarm.Notify = settings.contactLost.Notify;
				cumulus.SensorAlarm.Email = settings.contactLost.Email;
				cumulus.SensorAlarm.Latch = settings.contactLost.Latches;
				cumulus.SensorAlarm.LatchHours = settings.contactLost.LatchHrs;
				cumulus.SensorAlarm.TriggerThreshold = settings.contactLost.Threshold;
				emailRequired = emailRequired || (cumulus.SensorAlarm.Email && cumulus.SensorAlarm.Enabled);
				cumulus.SensorAlarm.Action = settings.contactLost.Action.Trim();
				cumulus.SensorAlarm.ActionParams = settings.contactLost.ActionParams.Trim();

				cumulus.DataStoppedAlarm.Enabled = settings.dataStopped.Enabled;
				cumulus.DataStoppedAlarm.Sound = settings.dataStopped.SoundEnabled;
				cumulus.DataStoppedAlarm.SoundFile = settings.dataStopped.Sound.Trim();
				cumulus.DataStoppedAlarm.Notify = settings.dataStopped.Notify;
				cumulus.DataStoppedAlarm.Email = settings.dataStopped.Email;
				cumulus.DataStoppedAlarm.Latch = settings.dataStopped.Latches;
				cumulus.DataStoppedAlarm.LatchHours = settings.dataStopped.LatchHrs;
				cumulus.DataStoppedAlarm.TriggerThreshold = settings.dataStopped.Threshold;
				emailRequired = emailRequired || (cumulus.DataStoppedAlarm.Email && cumulus.DataStoppedAlarm.Enabled);
				cumulus.DataStoppedAlarm.Action = settings.dataStopped.Action.Trim();
				cumulus.DataStoppedAlarm.ActionParams = settings.dataStopped.ActionParams.Trim();

				cumulus.BatteryLowAlarm.Enabled = settings.batteryLow.Enabled;
				cumulus.BatteryLowAlarm.Sound = settings.batteryLow.SoundEnabled;
				cumulus.BatteryLowAlarm.SoundFile = settings.batteryLow.Sound.Trim();
				cumulus.BatteryLowAlarm.Notify = settings.batteryLow.Notify;
				cumulus.BatteryLowAlarm.Email = settings.batteryLow.Email;
				cumulus.BatteryLowAlarm.Latch = settings.batteryLow.Latches;
				cumulus.BatteryLowAlarm.LatchHours = settings.batteryLow.LatchHrs;
				cumulus.BatteryLowAlarm.TriggerThreshold = settings.batteryLow.Threshold;
				emailRequired = emailRequired || (cumulus.BatteryLowAlarm.Email && cumulus.BatteryLowAlarm.Enabled);
				cumulus.BatteryLowAlarm.Action = settings.batteryLow.Action.Trim();
				cumulus.BatteryLowAlarm.ActionParams = settings.batteryLow.ActionParams.Trim();

				cumulus.SpikeAlarm.Enabled = settings.spike.Enabled;
				cumulus.SpikeAlarm.Sound = settings.spike.SoundEnabled;
				cumulus.SpikeAlarm.SoundFile = settings.spike.Sound.Trim();
				cumulus.SpikeAlarm.Notify = settings.spike.Notify;
				cumulus.SpikeAlarm.Email = settings.spike.Email;
				cumulus.SpikeAlarm.Latch = settings.spike.Latches;
				cumulus.SpikeAlarm.LatchHours = settings.spike.LatchHrs;
				cumulus.SpikeAlarm.TriggerThreshold = settings.spike.Threshold;
				emailRequired = emailRequired || (cumulus.SpikeAlarm.Email && cumulus.SpikeAlarm.Enabled);
				cumulus.SpikeAlarm.Action = settings.spike.Action.Trim();
				cumulus.SpikeAlarm.ActionParams = settings.spike.ActionParams.Trim();

				cumulus.UpgradeAlarm.Enabled = settings.upgrade.Enabled;
				cumulus.UpgradeAlarm.Sound = settings.upgrade.SoundEnabled;
				cumulus.UpgradeAlarm.SoundFile = settings.upgrade.Sound.Trim();
				cumulus.UpgradeAlarm.Notify = settings.upgrade.Notify;
				cumulus.UpgradeAlarm.Email = settings.upgrade.Email;
				cumulus.UpgradeAlarm.Latch = settings.upgrade.Latches;
				cumulus.UpgradeAlarm.LatchHours = settings.upgrade.LatchHrs;
				emailRequired = emailRequired || (cumulus.UpgradeAlarm.Email && cumulus.UpgradeAlarm.Enabled);
				cumulus.UpgradeAlarm.Action = settings.upgrade.Action.Trim();
				cumulus.UpgradeAlarm.ActionParams = settings.upgrade.ActionParams.Trim();

				cumulus.ThirdPartyUploadAlarm.Enabled = settings.thirdParty.Enabled;
				cumulus.ThirdPartyUploadAlarm.Sound = settings.thirdParty.SoundEnabled;
				cumulus.ThirdPartyUploadAlarm.SoundFile = settings.thirdParty.Sound.Trim();
				cumulus.ThirdPartyUploadAlarm.Notify = settings.thirdParty.Notify;
				cumulus.ThirdPartyUploadAlarm.Email = settings.thirdParty.Email;
				cumulus.ThirdPartyUploadAlarm.Latch = settings.thirdParty.Latches;
				cumulus.ThirdPartyUploadAlarm.LatchHours = settings.thirdParty.LatchHrs;
				cumulus.ThirdPartyUploadAlarm.TriggerThreshold = settings.thirdParty.Threshold;
				emailRequired = emailRequired || (cumulus.ThirdPartyUploadAlarm.Email && cumulus.ThirdPartyUploadAlarm.Enabled);
				cumulus.ThirdPartyUploadAlarm.Action = settings.thirdParty.Action.Trim();
				cumulus.ThirdPartyUploadAlarm.ActionParams = settings.thirdParty.ActionParams.Trim();

				cumulus.MySqlUploadAlarm.Enabled = settings.mySqlUpload.Enabled;
				cumulus.MySqlUploadAlarm.Sound = settings.mySqlUpload.SoundEnabled;
				cumulus.MySqlUploadAlarm.SoundFile = settings.mySqlUpload.Sound.Trim();
				cumulus.MySqlUploadAlarm.Notify = settings.mySqlUpload.Notify;
				cumulus.MySqlUploadAlarm.Email = settings.mySqlUpload.Email;
				cumulus.MySqlUploadAlarm.Latch = settings.mySqlUpload.Latches;
				cumulus.MySqlUploadAlarm.LatchHours = settings.mySqlUpload.LatchHrs;
				cumulus.MySqlUploadAlarm.TriggerThreshold = settings.mySqlUpload.Threshold;
				emailRequired = emailRequired || (cumulus.MySqlUploadAlarm.Email && cumulus.MySqlUploadAlarm.Enabled);
				cumulus.MySqlUploadAlarm.Action = settings.mySqlUpload.Action.Trim();
				cumulus.MySqlUploadAlarm.ActionParams = settings.mySqlUpload.ActionParams.Trim();

				cumulus.FtpAlarm.Enabled = settings.ftpUpload.Enabled;
				cumulus.FtpAlarm.Sound = settings.ftpUpload.SoundEnabled;
				cumulus.FtpAlarm.SoundFile = settings.ftpUpload.Sound.Trim();
				cumulus.FtpAlarm.Notify = settings.ftpUpload.Notify;
				cumulus.FtpAlarm.Email = settings.ftpUpload.Email;
				cumulus.FtpAlarm.Latch = settings.ftpUpload.Latches;
				cumulus.FtpAlarm.LatchHours = settings.ftpUpload.LatchHrs;
				cumulus.FtpAlarm.TriggerThreshold = settings.ftpUpload.Threshold;
				emailRequired = emailRequired || (cumulus.FtpAlarm.Email && cumulus.FtpAlarm.Enabled);
				cumulus.FtpAlarm.Action = settings.ftpUpload.Action.Trim();
				cumulus.FtpAlarm.ActionParams = settings.ftpUpload.ActionParams.Trim();

				// validate the from email
				if (emailRequired && !EmailSender.CheckEmailAddress(result.email.fromEmail.Trim()))
				{
					var msg = "ERROR: Alarm email option enabled and an invalid Alarm from email address entered";
					cumulus.LogWarningMessage(msg);
					context.Response.StatusCode = 500;
					return msg;
				}
				else
				{
					cumulus.AlarmFromEmail = result.email.fromEmail.Trim();
				}

				// validate the destination email(s)
				var emails = result.email.destEmail.Trim().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < emails.Length; i++)
				{
					emails[i] = emails[i].Trim();
					if (!EmailSender.CheckEmailAddress(emails[i]))
					{
						var msg = "ERROR: Invalid Alarm destination email address entered - " + emails[i];
						cumulus.LogWarningMessage(msg);
						context.Response.StatusCode = 500;
						return msg;
					}
				}
				cumulus.AlarmDestEmail = emails;
				cumulus.AlarmEmailHtml = result.email.useHtml;
				cumulus.AlarmEmailUseBcc = result.email.useBcc;

				// Save the settings
				cumulus.WriteIniFile();

				context.Response.StatusCode = 200;
			}
			catch (Exception ex)
			{
				var msg = "Error processing Alarm settings";
				cumulus.LogExceptionMessage(ex, msg);
				cumulus.LogDebugMessage("Alarm Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}
			return "success";
		}

		public string TestEmail(IHttpContext context)
		{
			try
			{

				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				var json = WebUtility.UrlDecode(data);

				var result = json.FromJson<EmailJson>();
				// process the settings
				cumulus.LogMessage("Sending test email...");

				// validate the from email
				if (!EmailSender.CheckEmailAddress(result.fromEmail.Trim()))
				{
					var msg = "ERROR: Invalid Alarm from email address entered";
					cumulus.LogMessage(msg);
					context.Response.StatusCode = 500;
					return msg;
				}
				var from = result.fromEmail.Trim();

				// validate the destination email(s)
				var dest = result.destEmail.Trim().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < dest.Length; i++)
				{
					dest[i] = dest[i].Trim();
					if (!EmailSender.CheckEmailAddress(dest[i]))
					{
						var msg = "ERROR: Invalid Alarm destination email address entered";
						cumulus.LogMessage(msg);
						context.Response.StatusCode = 500;
						return msg;
					}
				}

				var ret = cumulus.emailer.SendTestEmail(dest, from, "Cumulus MX Test Email", "A test email from Cumulus MX.", result.useHtml);

				if (ret == "OK")
				{
					cumulus.LogMessage("Test email sent without error");
				}
				else
				{
					context.Response.StatusCode = 500;
					return ret;
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "Test Email");
				context.Response.StatusCode = 500;
				return ex.Message;
			}

			return "success";
		}

		private class DataJson
		{
			public ValuesJson tempBelow { get; set; }
			public ValuesJson tempAbove { get; set; }
			public ValuesJson tempChange { get; set; }
			public ValuesJson pressBelow { get; set; }
			public ValuesJson pressAbove { get; set; }
			public ValuesJson pressChange { get; set; }
			public ValuesJson rainAbove { get; set; }
			public ValuesJson rainRateAbove { get; set; }
			public ValuesJson gustAbove { get; set; }
			public ValuesJson windAbove { get; set; }
			public ValuesJson contactLost { get; set; }
			public ValuesJson dataStopped { get; set; }
			public ValuesJson batteryLow { get; set; }
			public ValuesJson spike { get; set; }
			public ValuesJson upgrade { get; set; }
			public ValuesJson thirdParty { get; set; }
			public ValuesJson mySqlUpload { get; set; }
			public ValuesJson isRaining { get; set; }
			public ValuesJson ftpUpload { get; set; }
			public ValuesJson newRecord { get; set; }
		}

		private class ValuesJson
		{
			public bool Enabled { get; set; }
			public double Val { get; set; }
			public bool SoundEnabled { get; set; }
			public string Sound { get; set; }
			public bool Notify { get; set; }
			public bool Email { get; set; }
			public bool Latches { get; set; }
			public double LatchHrs { get; set; }
			public int Threshold { get; set; }
			public string Action { get; set; }
			public string ActionParams { get; set; }
		}

		private class InfoDataJson
		{
			public InfoJson tempBelow { get; set; }
			public InfoJson tempAbove { get; set; }
			public InfoJson tempChange { get; set; }
			public InfoJson pressBelow { get; set; }
			public InfoJson pressAbove { get; set; }
			public InfoJson pressChange { get; set; }
			public InfoJson rainAbove { get; set; }
			public InfoJson rainRateAbove { get; set; }
			public InfoJson gustAbove { get; set; }
			public InfoJson windAbove { get; set; }
			public InfoJson newRecord { get; set; }
			public InfoJson contactLost { get; set; }
			public InfoJson dataStopped { get; set; }
			public InfoJson batteryLow { get; set; }
			public InfoJson spike { get; set; }
			public InfoJson upgrade { get; set; }
			public InfoJson httpUpload { get; set; }
			public InfoJson mySqlUpload { get; set; }
			public InfoJson isRaining { get; set; }
			public InfoJson ftpUpload { get; set; }
		}

		private class InfoJson
		{
			public bool Enabled { get; set; }
			public bool SoundEnabled { get; set; }
			public string Sound { get; set; }
			public bool Notify { get; set; }
		}

		private class EmailJson
		{
			public string fromEmail { get; set; }
			public string destEmail { get; set; }
			public bool useHtml { get; set; }
			public bool useBcc { get; set; }
		}

		private class UnitsJson
		{
			public string tempUnits { get; set; }
			public string pressUnits { get; set; }
			public string rainUnits { get; set; }
			public string windUnits { get; set; }
		}

		private class SettingsJson
		{
			public DataJson data { get; set; }
			public UnitsJson units { get; set; }
			public EmailJson email { get; set; }
		}
	}
}
