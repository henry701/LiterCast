using System;
using System.Collections.Generic;

namespace LiterCast
{
    internal sealed class RequestHeading
    {
        public string Protocol { get; private set; }
        public Version Version { get; private set; }
        public string Verb { get; private set; }
        public string Path { get; private set; }
        public IReadOnlyDictionary<string, string> Headers { get; private set; }

        public RequestHeading(IReadOnlyDictionary<string, string> headers, string protocol, string verb, string path, Version version)
        {
            Headers = headers;
            Protocol = protocol;
            Verb = verb;
            Path = path;
            Version = version;
        }
    }
}