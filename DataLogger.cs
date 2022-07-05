using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace CumulusMX
{
	internal class DataLogger : IDisposable
	{
		private readonly BlockingCollection<string> blockingCollection = new BlockingCollection<string>();
		private readonly StreamWriter log = null;
		public bool run = true;
		public bool disposed = false;
		readonly Task task = null;

		public DataLogger(string logFilePath)
		{
			log = new StreamWriter(logFilePath, true);
			task = Task.Factory.StartNew(() =>
			{
				try
				{
					while (run)
					{
						log.WriteLine(blockingCollection.Take());
						log.Flush();
					}
				}
				catch { }
			});
		}

		public void WriteLine(string value)
		{
			blockingCollection.Add(value);
		}

		public void Dispose()
		{
			run = false;
			WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "log close requested");
			task.Wait();
			// Dispose managed resources.
			task.Dispose();
			log.Close();
			log.Dispose();

			disposed = true;

			GC.SuppressFinalize(this);
		}
	}
}
