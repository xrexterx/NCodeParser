﻿using System;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace NCodeParser.IO
{
	public static class UpdateHelper
	{
		private static readonly string LatestReleaseURL = "https://api.github.com/repos/imnotcode/NCodeParser/releases/latest";

		public static async Task<bool> CheckUpdate()
		{
			using (var client = new CookieAwareWebClient())
			{
				client.Headers.Add("User-Agent: Other");
				client.UseDefaultCredentials = true;

				string URL = LatestReleaseURL;

				var bytes = await client.DownloadDataTaskAsync(new Uri(URL)).ConfigureAwait(false);
				var downloadedText = Encoding.UTF8.GetString(bytes);

				var jObject = JObject.Parse(downloadedText);

				string tagVersion = jObject["tag_name"].ToString();
				tagVersion = tagVersion.Replace("v", "");

				// TODO

				return false;
			}
		}
	}
}
