using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Google.Analytics;
using Google.GData.Analytics;
using Google.GData.Client;
using Google.GData.Extensions;

namespace RiseWork.Code
{
	public abstract class DataFeedLoader
	{

		private const String CLIENT_USERNAME = "fatalway@gmail.com";
		private const String CLIENT_PASS = "tmfdbtm124r3488uu";
		private static AnalyticsService _asv;

		public static Int64 GetVisitors(string tableId, DateTime gaStartDate, DateTime gaEndDate)
		{
			if (_asv == null)
			{
				_asv = new AnalyticsService("gaExportAPI_acctSample_v2.0");
				_asv.setUserCredentials(CLIENT_USERNAME, CLIENT_PASS);
			}
			String baseUrl = "https://www.google.com/analytics/feeds/data";
			DataQuery query = new DataQuery(baseUrl);
			query.Ids = tableId;
			query.Metrics = "ga:visits";
			query.GAStartDate = gaStartDate.ToString("yyyy'-'MM'-'dd");
			query.GAEndDate = gaEndDate.ToString("yyyy'-'MM'-'dd");
			Uri url = query.Uri;			
			DataFeed feed;
			try
			{
				feed = _asv.Query(query);
			}
			catch { return -1; }
			//var singleEntry = feed.Entries.Single() as DataEntry;
			//return Convert.ToInt64(singleEntry.Metrics.Single().Value);
			return Convert.ToInt64(feed.Aggregates.Metrics[0].Value);
		}
	}
}