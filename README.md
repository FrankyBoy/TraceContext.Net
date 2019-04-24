# TraceContext.Net

Aiming to provide a simple .net implementation of the [W3C TraceContext specification](https://www.w3.org/TR/trace-context/).

To create an instance, you can either ...
* simply create a `new TraceContext()`, or
* parse existing Headers by using `TraceContext.TryParse(...)`

To get the headers you need to set in outgoing requests, you can use the method
`(string traceparent, string tracestate) GetOutgoingHeaderValues()`. This returns 
a tuple of the two header values. 

