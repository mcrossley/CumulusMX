using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;


namespace CumulusMX
{
	public static class HttpStations
	{
		internal static Stations.HttpStationWund stationWund { private get; set; }
		internal static Stations.HttpStationEcowitt stationEcowitt { private get; set; }
		internal static Stations.HttpStationEcowitt stationEcowittExtra { private get; set; }
		internal static Stations.HttpStationAmbient stationAmbient { private get; set; }
		internal static Stations.HttpStationAmbient stationAmbientExtra { private get; set; }


		// HTTP Station
		public class HttpStation : WebApiController
		{
			[Route(HttpVerbs.Post, "/{req}")]
			public async Task PostStation(string req)
			{
				try
				{
					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "ecowitt":
							if (stationEcowitt != null)
							{
								await writer.WriteAsync(stationEcowitt.ProcessData(HttpContext, true));
							}
							else
							{
								Response.StatusCode = 500;
								await writer.WriteAsync("HTTP Station (Ecowitt) is not running");
							}
							break;
						case "ecowittextra":
							if (stationEcowittExtra != null)
							{
								await writer.WriteAsync(stationEcowittExtra.ProcessData(HttpContext, false));
							}
							else
							{
								Response.StatusCode = 500;
								await writer.WriteAsync("HTTP Station (Ecowitt) is not running");
							}
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "PostTags: Error");
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}

			[Route(HttpVerbs.Get, "/{req}")]
			public async Task GetStation(string req)
			{
				try
				{
					Response.ContentType = "text/plain";

					using var writer = HttpContext.OpenResponseText();
					switch (req)
					{
						case "wunderground":
							if (stationWund != null)
							{
								await writer.WriteAsync(stationWund.ProcessData(HttpContext));
							}
							else
							{
								Response.StatusCode = 500;
								await writer.WriteAsync("HTTP Station (Wunderground) is not running");
							}
							break;
						case "ambient":
							if (stationAmbient != null)
							{
								await writer.WriteAsync(stationAmbient.ProcessData(HttpContext, true));
							}
							else
							{
								Response.StatusCode = 500;
								await writer.WriteAsync("HTTP Station (Ambient) is not running");
							}
							break;
						case "ambientextra":
							if (stationAmbientExtra != null)
							{
								await writer.WriteAsync(stationAmbientExtra.ProcessData(HttpContext, false));
							}
							else
							{
								Response.StatusCode = 500;
								await writer.WriteAsync("HTTP Station (Ambient) is not running");
							}
							break;
						default:
							throw new KeyNotFoundException("Key Not Found: " + req);
					}
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "GetStation: Error");
					using var writer = HttpContext.OpenResponseText();
					await writer.WriteAsync($"{{\"Title\":\"Unexpected Error\",\"ErrorCode\":\"{ex.GetType().Name}\",\"Description\":\"{ex.Message}\"}}");
					Response.StatusCode = 500;
				}
			}
		}
	}
}
