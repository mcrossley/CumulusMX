using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using ServiceStack;

namespace CumulusMX
{
	public static class MqttPublisher
	{
		private static Cumulus cumulus;
		private static MqttClient mqttClient;
		private static bool configured;

		public static bool Configured { get => configured; set => configured = value; }

		public static void Setup(Cumulus cumulus)
		{
			MqttPublisher.cumulus = cumulus;

			var mqttFactory = new MqttFactory();

			mqttClient = (MqttClient)mqttFactory.CreateMqttClient();

			var clientId = Guid.NewGuid().ToString();

			var mqttTcpOptions = new MqttClientTcpOptions
			{
				Server = cumulus.MQTT.Server,
				Port = cumulus.MQTT.Port,
				TlsOptions = new MqttClientTlsOptions { UseTls = cumulus.MQTT.UseTLS },
				AddressFamily = cumulus.MQTT.IpVersion switch
				{
					4 => System.Net.Sockets.AddressFamily.InterNetwork,
					6 => System.Net.Sockets.AddressFamily.InterNetworkV6,
					_ => System.Net.Sockets.AddressFamily.Unspecified,
				}
			};
			var mqttOptions = new MqttClientOptions
			{
				ChannelOptions = mqttTcpOptions,
				ClientId = clientId,
				Credentials = string.IsNullOrEmpty(cumulus.MQTT.Password)
					? null
					: new MqttClientCredentials(cumulus.MQTT.Username, System.Text.Encoding.UTF8.GetBytes(cumulus.MQTT.Password)),
				CleanSession = true
			};

			_ = Connect(mqttOptions); // let this run in background

			mqttClient.DisconnectedAsync += (async e =>
			{
				Cumulus.LogMessage("Error: MQTT disconnected from the server");
				await Task.Delay(TimeSpan.FromSeconds(5));

				cumulus.LogDebugMessage("MQTT attempting to reconnect with server");
				try
				{
					Connect(mqttOptions).Wait();
					cumulus.LogDebugMessage("MQTT reconnected OK");
				}
				catch
				{
					Cumulus.LogMessage("Error: MQTT reconnection to server failed");
				}
			});

			configured = true;
		}


		private static async Task SendMessageAsync(string topic, string message, bool retain)
		{
			cumulus.LogDataMessage($"MQTT: publishing to topic '{topic}', message '{message}'");
			if (mqttClient.IsConnected)
			{
				var mqttMsg = new MqttApplicationMessageBuilder()
					.WithTopic(topic)
					.WithPayload(message)
					.WithRetainFlag(retain)
					.Build();

				await mqttClient.PublishAsync(mqttMsg, CancellationToken.None);
			}
			else
			{
				Cumulus.LogMessage("MQTT: Error - Not connected to MQTT server - message not sent");
			}
		}

		private static async Task Connect(MqttClientOptions options)
		{
			try
			{
				await mqttClient.ConnectAsync(options, CancellationToken.None);
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, "MQTT Error: failed to connect to the host");
			}
		}


		public static void UpdateMQTTfeed(string feedType)
		{
			var template = "mqtt/";

			if (feedType == "Interval")
			{
				template += cumulus.MQTT.IntervalTemplate;
			}
			else
			{
				template += cumulus.MQTT.UpdateTemplate;
			}

			if (!File.Exists(template))
				return;

			// use template file
			cumulus.LogDebugMessage($"MQTT: Using template - {template}");

			// read the file
			var templateText = File.ReadAllText(template);
			var templateObj = templateText.FromJson<MqttTemplate>();

			// process each of the topics in turn
			try
			{
				foreach (var feed in templateObj.topics)
				{
					var mqttTokenParser = new TokenParser { Encoding = new System.Text.UTF8Encoding(false) };
					mqttTokenParser.OnToken += cumulus.TokenParserOnToken;
					mqttTokenParser.InputText = feed.data;
					string message = mqttTokenParser.ToStringFromString();

					// send the message
					_ = SendMessageAsync(feed.topic, message, feed.retain);
				}
			}
			catch (Exception ex)
			{
				cumulus.LogExceptionMessage(ex, $"UpdateMQTTfeed: Error process the template file [{template}], Error");
			}
		}
	}
}
