﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using EmbedIO;

namespace CumulusMX
{
	public class ApiTagProcessor
	{
		private readonly Cumulus cumulus;
		private readonly object lockObject = new object();

		internal ApiTagProcessor(Cumulus cumulus)
		{
			this.cumulus = cumulus;
		}

		// Output the processed response as a JSON string
		public string ProcessJson(IHttpRequest request)
		{
			lock (lockObject)
			{
				var rc = false;
				var query = request.Url.Query;

				cumulus.LogDebugMessage("API tag: Processing API JSON tag request");
				cumulus.LogDataMessage($"API tag: Source = {request.RemoteEndPoint} Input string = {query}");

				var output = new StringBuilder("{", query.Length * 2);

				try
				{
					// remove leading "?" and split on "&"
					var input = new List<string>(query[1..].Split('&'));
					var parms = new Dictionary<string, string>();
					if (input[0] == "rc")
					{
						input.RemoveAt(0);
						rc = true;
					}

					foreach (var tag in input)
					{
						if (rc)
						{
							parms.Add("webtag", tag);
							parms.Add("rc", "y");
						}
						var val = cumulus.WebTags.GetWebTagText(tag, parms);
						output.Append($"\"{tag}\":\"{val}\",");
						if (rc)
						{
							parms.Clear();
						}
					}
					if (output.Length > 1)
					{
						// remove trailing ","
						output.Remove(output.Length - 1, 1);
					}
					output.Append('}');

#if DEBUG
					cumulus.LogDataMessage("API tag: Output string = " + output);
#endif
				}
				catch (Exception ex)
				{
					Program.cumulus.LogExceptionMessage(ex, "API ProcessJson: Error");
					output.Append($"\"ERROR\":\"{ex.Message}\"}}");
				}

				return output.ToString();
			}
		}

		// Just return the processed text as-is
		public string ProcessText(IHttpRequest request)
		{
			cumulus.LogDebugMessage("API tag: Processing API Text tag request");

			try
			{
				var data = new StreamReader(request.InputStream).ReadToEnd();

				cumulus.LogDataMessage($"API tag: Source = {request.RemoteEndPoint} Input string = {data}");

				var tokenParser = new TokenParser(cumulus.TokenParserOnToken)
				{
					Encoding = new UTF8Encoding(false),
					InputText = data
				};
				var output = tokenParser.ToStringFromString();

				cumulus.LogDataMessage("API tag: Output string = " + output);

				return output;
			}
			catch (Exception ex)
			{
				Program.cumulus.LogExceptionMessage(ex, "API ProcessText: Error");
				return $"{{\"ERROR\":\"{ex.Message}\"}}";
			}
		}
	}
}
