using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TraceContext.Net
{
    public class TraceContext
    {
        public string TraceId { get; private set; }
        public string SpanId { get; private set; }
        public string ParentSpanId { get; private set; }
        public TraceFlag TraceFlags { get; private set; }
        public IReadOnlyDictionary<string, string> TraceState => _traceState;

        public (string traceparent, string tracestate) GetOutgoingHeaderValues()
        {
            return (
                $"00-{TraceId}-{SpanId}-{(byte)TraceFlags:X2}",
                $"{string.Join(",", _traceState.Select(x => $"{x.Key}={x.Value}"))}");
        }
        
        public TraceContext()
        {
            SpanId = GetRandomHexString(8);
            TraceId = GetRandomHexString(16);
            ParentSpanId = null;
            TraceFlags = 0;
        }

        private SortedDictionary<string, string> _traceState { get; set; }
            = new SortedDictionary<string, string>();
        private readonly string _componentName;


        public static bool TryParse(string traceparent, string tracestate, string componentName,
            out TraceContext target)
        {
            target = new TraceContext();

            // only if traceparent is set we care about traceState
            return TryParseTraceParent(traceparent, target, componentName)
                && TryParseTraceState(tracestate, target, componentName);
        }

        private static readonly Regex parentRegex = new Regex(@"^00-([0-9a-fA-F]{32})-([0-9a-fA-F]{16})-([0-9a-fA-F]{2})$");
        private static bool TryParseTraceParent(string traceparent, TraceContext target, string componentName)
        {
            if (!string.IsNullOrWhiteSpace(traceparent))
            {
                var parentMatch = parentRegex.Match(traceparent);
                if (parentMatch.Groups.Count == 4)
                {
                    // if traceparent is valid, we should add it to tracestate
                    target._traceState.Add(componentName, traceparent);
                    
                    target.TraceId = parentMatch.Groups[1].Value;
                    target.ParentSpanId = parentMatch.Groups[2].Value;
                    target.TraceFlags = (TraceFlag)Convert.ToByte(parentMatch.Groups[3].Value);
                    return true;
                }
            }

            return false;
        }

        private static Regex stateRegex = new Regex(@"^([^=,]+=[^=,]+)(,([^=,]+=[^=,]+))*$");
        private static bool TryParseTraceState(string tracestate, TraceContext target, string componentName)
        {
            if (string.IsNullOrWhiteSpace(tracestate))
                return true;

            if (!stateRegex.IsMatch(tracestate))
                return false;

            foreach(var keyValueString in tracestate.Split(','))
            {
                var keyValue = keyValueString.Split('=').Select(x => x.Trim()).ToArray();
                if (keyValue.Length == 2 && !keyValue[0].Equals(componentName, StringComparison.OrdinalIgnoreCase))
                    target._traceState.Add(keyValue[0], keyValue[1]);
            }
            return true;
        }


        private static readonly Random _rd = new Random();
        private string GetRandomHexString(int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            _rd.NextBytes(bytes);
            return ByteConverter.ByteArrayToString(bytes);
        }
    }

    [Flags]
    public enum TraceFlag
    {
        None = 0,
        Recorded = 1
    }
}
