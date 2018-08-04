using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;

namespace LiterCast
{
    internal sealed class RequestHeadingParser
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        private static readonly Regex headersRegex = new Regex("(?<key>.*?) *: *(?<value>.*?)[\r\n]", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex firstLineRegex = new Regex("(?<verb>.*?) (?<path>.*?) (?<protocol>.*?)/(?<version>.*?)[\r\n]", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static RequestHeading ParseHttpRequestHeading(string requestStr)
        {
            int firstLineBreak = requestStr.IndexOfAny(new char[] { '\r', '\n' }) + 1;

            Match match = firstLineRegex.Match(requestStr);
            GroupCollection groups = match.Groups;
            string verb = groups["verb"].Value;
            string path = groups["path"].Value;
            string protocol = groups["protocol"].Value;
            Version version;
            try
            {
                version = new Version(groups["version"].Value);
            }
            catch
            {
                version = null;
            }
            

            MatchCollection headerMatches = headersRegex.Matches(requestStr, firstLineBreak);

            var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (Match headerMatch in headerMatches)
            {
                string key = headerMatch.Groups["key"].Value;
                string value = headerMatch.Groups["value"].Value;
                if (headers.ContainsKey(key))
                {
                    headers[key] = $"{headers[key]}, {value}";
                }
                else
                {
                    headers.Add(key, value);
                }
            }



            return new RequestHeading(verb: verb, headers: headers, protocol: protocol, path: path, version: version);
        }

        public static async Task<string> RetrieveHttpRequestHeadingFromStream(StreamReader streamReader, int timeout)
        {
            bool readSomething = false;
            string inputStr = "";
            while (inputStr.Length < 15000)
            {
                var task = streamReader.ReadLineAsync();
                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                {
                    string res;
                    try
                    {
                        res = task.Result;
                    }
                    catch (AggregateException)
                    {
                        return inputStr;
                    }
                    if (res == "") // Request is finished
                    {
                        if(readSomething)
                        {
                            break;
                        }
                    }
                    readSomething = true;
                    inputStr += res + "\r\n";
                }
                else
                {
                    LOGGER.Debug("Continued by input timeout - Possible SlowLoris Attack Attempt");
                    break;
                }
            }
            return inputStr;
        }
    }
}
