using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TraceContext.Net
{
    public class TraceContext
    {
        private readonly Random _rd = new Random();
        private SortedDictionary<string, string> _traceState { get; set; }
            = new SortedDictionary<string, string>();

        private readonly string _componentName;

        public byte Version { get; private set; }
        public byte[] TraceId { get; private set; }
        public byte[] SpanId { get; private set; }
        public byte[] ParentSpanId { get; private set; }
        public byte TraceFlags { get; private set; }
        public IReadOnlyDictionary<string, string> TraceState => _traceState;

        public TraceContext(string traceparent, string tracestate, string componentName)
        {
            // always need a new Span-ID, no matter what
            SpanId = GetRandomBytes(8);

            // only if traceparent is set we care about traceState
            if (TryParseTraceParent(traceparent))
                TryparseTraceState(tracestate);
            _componentName = componentName;
        }

        public (string traceparent, string tracestate) GetOutgoingHeaderValues()
        {
            return (
                $"{Version:X2}-{ByteArrayToString(TraceId)}-{ByteArrayToString(SpanId)}-{TraceFlags:X2}",
                $"{string.Join(",", _traceState.Select(x => $"{x.Key}={x.Value}"))}"
                );
        }

        public void OverrideState(string state)
        {
            _traceState[_componentName] = state;
        }

        private static readonly Regex parentRegex = new Regex(@"^([0-9a-fA-F]{2})-([0-9a-fA-F]{32})-([0-9a-fA-F]{16})-([0-9a-fA-F]{2})$");
        /// <summary>
        /// for valid traceparent, parse the values and return true
        /// otherwisse initialize with default values and return false
        /// </summary>
        private bool TryParseTraceParent(string traceparent)
        {
            if (!string.IsNullOrWhiteSpace(traceparent))
            {
                var parentMatch = parentRegex.Match(traceparent);
                if (parentMatch.Groups.Count == 5)
                {
                    // if traceparent is valid, we should add it to tracestate
                    _traceState.Add(_componentName, traceparent);

                    Version = Convert.ToByte(parentMatch.Groups[1].Value);
                    TraceId = StringToByteArray(parentMatch.Groups[2].Value);
                    ParentSpanId = StringToByteArray(parentMatch.Groups[3].Value);
                    TraceFlags = Convert.ToByte(parentMatch.Groups[4].Value);
                    return true;
                }
            }

            Version = 1;
            TraceId = GetRandomBytes(16);
            ParentSpanId = null;
            TraceFlags = 0;

            return false;
        }
        
        private void TryparseTraceState(string tracestate)
        {
            if (string.IsNullOrWhiteSpace(tracestate))
                return;

            foreach(var keyValueString in tracestate.Split(','))
            {
                var keyValue = keyValueString.Split('=').Select(x => x.Trim()).ToArray();
                if (keyValue.Length == 2 && !keyValue[0].Equals(_componentName, StringComparison.OrdinalIgnoreCase))
                    _traceState.Add(keyValue[0], keyValue[1]);
            }
        }

        private static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private byte[] GetRandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            _rd.NextBytes(bytes);
            return bytes;
        }

        private static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
    }
}
