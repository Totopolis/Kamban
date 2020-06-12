using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Kamban.Core;

namespace Kamban
{
    public interface IGa
    {
        void TrackPage(string page);
        void TrackEvent(string category, string action, string label = null, int? value = null);
    }

    // Sample: https://www.technical-recipes.com/2017/tracking-events-in-desktop-applications-using-google-analytics/

    public class Ga : IGa
    {
        private readonly string gaUrl = "https://www.google-analytics.com/collect";
        private readonly string tid = "{GOOGLE_TRACKING_CODE}";
        private readonly string cid;

        public Ga(IAppConfig cfg)
        {
            cid = cfg.AppGuid;
        }

        public void TrackPage(string page)
        {
            if (string.IsNullOrEmpty(tid) || tid.Contains("GOOGLE_TRACKING_CODE") || string.IsNullOrEmpty(cid))
                return;

            var request = (HttpWebRequest)WebRequest.Create(gaUrl);
            request.Method = "POST";

            // the request body we want to send
            var postData = new Dictionary<string, string>
                           {
                               { "v", "1" },
                               { "tid", tid },
                               { "cid", cid },
                               { "t", "pageview" },
                               { "dp", page }
                           };

            var postDataString = postData
                .Aggregate("", (data, next) => string.Format("{0}&{1}={2}", data, next.Key,
                                                             HttpUtility.UrlEncode(next.Value)))
                .TrimEnd('&');

            // set the Content-Length header to the correct value
            request.ContentLength = Encoding.UTF8.GetByteCount(postDataString);

            // write the request body to the request
            using (var writer = new StreamWriter(request.GetRequestStream()))
                writer.Write(postDataString);

            try
            {
                var webResponse = (HttpWebResponse)request.GetResponse();
                if (webResponse.StatusCode != HttpStatusCode.OK)
                {
                    int code = (int)webResponse.StatusCode;
                    throw new Exception($"{code}: Google Analytics tracking did not return OK 200");
                }
            }
            catch (Exception)
            {
                // do what you like here
            }
        }

        public void TrackEvent(string category, string action, string label, int? value = null)
        {

        }
    }
}
