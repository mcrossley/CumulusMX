﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;

using EmbedIO;

using ServiceStack;


namespace CumulusMX
{
	public class MqttSettings
	{
		private readonly Cumulus cumulus;

		public MqttSettings(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		public string UpdateConfig(IHttpContext context)
		{
			var errorMsg = "";
			var json = "";
			MqttConfig settings;

			context.Response.StatusCode = 200;

			try
			{
				var data = new StreamReader(context.Request.InputStream).ReadToEnd();

				// Start at char 5 to skip the "json:" prefix
				json = WebUtility.UrlDecode(data.Substring(5));

				// de-serialize it to the settings structure
				settings = json.FromJson<MqttConfig>();
			}
			catch (Exception ex)
			{
				var msg = "Error de-serializing MQTT Settings JSON: " + ex.Message;
				cumulus.LogErrorMessage(msg);
				cumulus.LogDebugMessage("MQTT Data: " + json);
				context.Response.StatusCode = 500;
				return msg;
			}


			// process the settings
			try
			{
				cumulus.LogMessage("Updating internet settings");

				// MQTT
				try
				{
					cumulus.MQTT.Server = settings.server ?? string.Empty;
					cumulus.MQTT.Port = settings.port;
					cumulus.MQTT.UseTLS = settings.useTls;
					cumulus.MQTT.Username = settings.username ?? string.Empty;
					cumulus.MQTT.Password = settings.password ?? string.Empty;
					cumulus.MQTT.EnableDataUpdate = settings.dataUpdate.enabled;
					if (cumulus.MQTT.EnableDataUpdate)
					{
						cumulus.MQTT.UpdateTemplate = settings.dataUpdate.template ?? string.Empty;
					}

					cumulus.MQTT.EnableInterval = settings.interval.enabled;
					if (cumulus.MQTT.EnableInterval)
					{
						cumulus.MQTT.IntervalTemplate = settings.interval.template ?? string.Empty;
					}
				}
				catch (Exception ex)
				{
					var msg = "Error processing MQTT settings: " + ex.Message;
					cumulus.LogErrorMessage(msg);
					errorMsg += msg + "\n\n";
					context.Response.StatusCode = 500;
				}

				// Save the settings
				cumulus.WriteIniFile();

				// Setup MQTT
				if (cumulus.MQTT.EnableDataUpdate || cumulus.MQTT.EnableInterval)
				{
					if (!MqttPublisher.Configured)
					{
						MqttPublisher.Setup(cumulus);
					}
					else
					{
						MqttPublisher.ReadTemplateFiles();
					}
				}
			}
			catch (Exception ex)
			{
				var msg = "Error processing MQTT settings: " + ex.Message;
				cumulus.LogErrorMessage(msg);
				cumulus.LogDebugMessage("MQTT data: " + json);
				errorMsg += msg;
				context.Response.StatusCode = 500;
			}

			return context.Response.StatusCode == 200 ? "success" : errorMsg;
		}

		public string GetAlpacaFormData()
		{
			// Build the settings data, convert to JSON, and return it

			var mqttUpdate = new MqttData()
			{
				enabled = cumulus.MQTT.EnableDataUpdate,
				template = cumulus.MQTT.UpdateTemplate
			};

			var mqttInterval = new MqttData()
			{
				enabled = cumulus.MQTT.EnableInterval,
				template = cumulus.MQTT.IntervalTemplate
			};

			var mqttsettings = new MqttConfig()
			{
				server = cumulus.MQTT.Server,
				port = cumulus.MQTT.Port,
				useTls = cumulus.MQTT.UseTLS,
				username = cumulus.MQTT.Username,
				password = cumulus.MQTT.Password,
				dataUpdate = mqttUpdate,
				interval = mqttInterval
			};

			return mqttsettings.ToJson();
		}

		private class MqttConfig
		{
			public string server { get; set; }
			public int port { get; set; }
			public bool useTls { get; set; }
			public string username { get; set; }
			public string password { get; set; }
			public MqttData dataUpdate { get; set; }
			public MqttData interval { get; set; }
		}

		private class MqttData
		{
			public bool enabled { get; set; }
			public string template { get; set; }
		}
	}
}
