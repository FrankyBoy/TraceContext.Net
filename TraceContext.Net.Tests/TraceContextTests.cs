using System;
using Xunit;

namespace TraceContext.Net.Tests
{
    public class TraceContextTests
    {
        [Fact]
        public void Constructor()
        {
            // Arrange

            // Act
            var instance = new TraceContext();
            var headers = instance.GetOutgoingHeaderValues();

            // Assert
            Assert.Equal(TraceFlag.None, instance.TraceFlags);
            Assert.Matches(@"^[A-Z0-9]{16}$", instance.SpanId);
            Assert.Matches(@"^[A-Z0-9]{32}$", instance.TraceId);
            Assert.Null(instance.ParentSpanId);

            Assert.Empty(headers.tracestate);
            Assert.Contains($"00-{instance.TraceId}-{instance.SpanId}-00", headers.traceparent);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ParseValid_Simple(string tracestate)
        {
            // Arrange
            var parent = new TraceContext();
            var pHeaders = parent.GetOutgoingHeaderValues();

            // Act
            TraceContext result;
            Assert.True(TraceContext.TryParse(pHeaders.traceparent,
                tracestate, "test1", out result));
            var oHeaders = result.GetOutgoingHeaderValues();

            // Assert
            Assert.Equal(parent.TraceFlags, result.TraceFlags);
            Assert.Equal(parent.TraceId, result.TraceId);
            Assert.Equal(parent.SpanId, result.ParentSpanId);
            Assert.NotEqual(parent.SpanId, result.SpanId);
            Assert.Matches(@"^[A-Z0-9]{16}$", result.SpanId);
            Assert.Equal($"test1={pHeaders.traceparent}", oHeaders.tracestate);
        }

        // existing tracestate with that component name should be
        // replaced and moved to the front according to spec
        [Fact]
        public void TryParse_Valid_TraceStateHandling()
        {
            // Arrange
            var parent = new TraceContext();
            var pHeaders = parent.GetOutgoingHeaderValues();
            var pState = "test2=foo,test1=bar";

            // Act
            TraceContext result;
            Assert.True(TraceContext.TryParse(pHeaders.traceparent,
                pState, "test1", out result));
            var oHeaders = result.GetOutgoingHeaderValues();

            // Assert
            Assert.Equal($"test1={pHeaders.traceparent},test2=foo", oHeaders.tracestate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        // invalid version (always assumed 00)
        [InlineData("01-11112222333344445555666677778888-9999AAAABBBBCCCC-01")]
        // invalid characters in traceId
        [InlineData("00-xx112222333344445555666677778888-9999AAAABBBBCCCC-01")]
        // invalid characters in spanId
        [InlineData("00-11112222333344445555666677778888-xx99AAAABBBBCCCC-01")]
        // invalid trace flags (only 00 and 01 are valid)
        [InlineData("00-11112222333344445555666677778888-xx99AAAABBBBCCCC-02")]
        public void TryParse_InvalidTraceParent(string traceparent)
        {
            Assert.False(TraceContext.TryParse(traceparent, "", "test", out _));
        }

        [Theory]
        [InlineData("foo")] // no value
        [InlineData("foo=bar=baz")] // two equals in a row
        [InlineData("foo=bar,,baz=bla")] // two commas in a row
        [InlineData(",baz=bla")] // comma at beginning
        [InlineData("foo=bar,")] // comma at end
        public void TryParse_InvalidTraceState(string tracestate)
        {
            var traceparent = "01-11112222333344445555666677778888-9999AAAABBBBCCCC-01";
            Assert.False(TraceContext.TryParse(traceparent, tracestate, "test", out _));
        }
    }
}
