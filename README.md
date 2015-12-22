## Conduit.Net ##

A .NET Pipeline Library.  

Most programs can be boiled-down to the following:

	[Input] -> Transform -> [Output] 

A pipeline consists of one or data more processing units.  Each unit may execute concurrently within the confines of the restrictions 

## Features ##

- Designed for async operations
- Enforces SRP
- 

### Pipeline ###

A pipeline is a collection of [Pipeline Components](#PipelineComponents) 

* Work-In-Progress


### Pipeline Components ###

A

## Example ##

Here is an example Pipeline.  It is invoked with a stream 

```csharp

        protected override void Describe(IPipelineBuilder<OperationContext> thisPipeline)
        {
            // Components
            var reader = Component;		// Reads a file
            var parser = Component;		// Parse each line
            var emitter = Component;	// Converts a line to a POCO
            var writer = Component;		// Writes a POCO to the database

            // File enumerator
            thisPipeline
                .Constructs(_streamLineReader)
                .Into(ref reader);
                
            // We will need a parser to parse each line
            thisPipeline
                .Constructs(_csvParser)
                .Into(ref parser);

            // And a component to compose the data objects
            thisPipeline
                .Constructs(_pocoEmitter)
                .Into(ref emitter);

            // Something which writes to the database
            thisPipeline
                .Constructs(_writer)
                .Into(ref writer);

            // When invoked
            thisPipeline
                .IsInvokedAsyncBy(ref _entryPoint)
                .WhichTransmitsTo(reader);

```

The pipeline will auto-wire itself based on the description found inside each component, but sometimes different behavior may desired.  For instance, because