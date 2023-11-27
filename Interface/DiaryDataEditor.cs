using System;
using System.Globalization;
using System.IO;
using System.Text;

using EmbedIO;
using ServiceStack;
using ServiceStack.Text;

using SQLite;

namespace CumulusMX
{
	static class DiaryDataEditor
	{
		public static string GetDiaryData(string date)
		{

			StringBuilder json = new StringBuilder("{\"entry\":\"", 1024);

			var result = Program.cumulus.DiaryDB.Query<DiaryData>("select * from DiaryData where date(Timestamp) = ? order by Timestamp limit 1", date);

			if (result.Count > 0)
			{
				json.Append(result[0].entry + "\",");
				json.Append("\"snowFalling\":");
				json.Append(result[0].snowFalling + ",");
				json.Append("\"snowLying\":");
				json.Append(result[0].snowLying + ",");
				json.Append("\"snowDepth\":\"");
				json.Append(result[0].snowDepth);
				json.Append("\"}");
			}
			else
			{
				json.Append("\",\"snowFalling\":0,\"snowLying\":0,\"snowDepth\":\"\"}");
			}

			return json.ToString();
		}

		// Fetches all days  that have a diary entry
		public static string GetDiarySummary()
		{
			var json = new StringBuilder(512);
			var result = Program.cumulus.DiaryDB.Query<DiaryData>("select Timestamp from DiaryData order by Timestamp");

			if (result.Count > 0)
			{
				json.Append("{\"dates\":[");
				for (int i = 0; i < result.Count; i++)
				{
					json.Append('"');
					json.Append(result[i].Timestamp.ToString("yyyy-MM-dd"));
					json.Append("\",");
				}
				json.Length--;
				json.Append("]}");
			}
			else
			{
				json.Append("{\"dates\":[]}");
			}

			return json.ToString();
		}

		public static string EditDiary(IHttpContext context)
		{
			try
			{
				var request = context.Request;
				string text;

				using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
				{
					text = reader.ReadToEnd();
				}

				// Override the ServiceStack de-serialization function
				// Check which format provided, attempt to parse as DateTime or return minValue.
				JsConfig<DateTime>.DeSerializeFn = datetimeStr =>
				{
					if (string.IsNullOrWhiteSpace(datetimeStr))
					{
						return DateTime.MinValue;
					}

					DateTime.TryParseExact(datetimeStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime resultLocal);
					return resultLocal;
				};

				var newData = text.FromJson<DiaryData>();

				// write new/updated entry to the database
				var result = Program.cumulus.DiaryDB.InsertOrReplace(newData);

				return "{\"result\":\"" + ((result == 1) ? "Success" : "Failed") + "\"}";

			}
			catch (Exception ex)
			{
				Program.cumulus.LogExceptionMessage(ex, "Edit Diary: Error");
				return "{\"result\":\"Failed\"}";
			}
		}

		public static string DeleteDiary(IHttpContext context)
		{
			try
			{
				var request = context.Request;
				string text;

				using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
				{
					text = reader.ReadToEnd();
				}

				// Override the ServiceStack de-serialization function
				// Check which format provided, attempt to parse as DateTime or return minValue.
				JsConfig<DateTime>.DeSerializeFn = datetimeStr =>
				{
					if (string.IsNullOrWhiteSpace(datetimeStr))
					{
						return DateTime.MinValue;
					}

					DateTime.TryParseExact(datetimeStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime resultLocal);
					return resultLocal;
				};

				var record = text.FromJson<DiaryData>();

				// Delete the corresponding entry from the database
				var result = Program.cumulus.DiaryDB.Delete(record);

				return "{\"result\":\"" + ((result == 1) ? "Success" : "Failed") + "\"}";

			}
			catch (Exception ex)
			{
				Program.cumulus.LogExceptionMessage(ex, "Delete Diary: Error");
				return "{\"result\":\"Failed\"}";
			}
		}


		public class DiaryData
		{
			[PrimaryKey]
			public DateTime Timestamp { get; set; }
			public string entry { get; set; }
			public int snowFalling { get; set; }
			public int snowLying { get; set; }
			public double snowDepth { get; set; }
		}
	}
}
