using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Sitko.Core.Xunit;

public class TestOutputSink : ILogEventSink
{
    private readonly IMessageSink? messageSink;
    private readonly ITestOutputHelper? testOutputHelper;
    private readonly ITextFormatter textFormatter;

    /// <summary>
    ///     Creates a new instance of <see cref="TestOutputSink" />
    /// </summary>
    /// <param name="messageSink">An <see cref="IMessageSink" /> implementation that can be used to provide test output</param>
    /// <param name="textFormatter">The <see cref="ITextFormatter" /> used when rendering the message</param>
    public TestOutputSink(IMessageSink messageSink, ITextFormatter textFormatter)
    {
        this.messageSink = messageSink ?? throw new ArgumentNullException(nameof(messageSink));
        this.textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
    }

    /// <summary>
    ///     Creates a new instance of <see cref="TestOutputSink" />
    /// </summary>
    /// <param name="testOutputHelper">
    ///     An <see cref="ITestOutputHelper" /> implementation that can be used to provide test
    ///     output
    /// </param>
    /// <param name="textFormatter">The <see cref="ITextFormatter" /> used when rendering the message</param>
    public TestOutputSink(ITestOutputHelper testOutputHelper, ITextFormatter textFormatter)
    {
        this.testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        this.textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
    }

    /// <summary>
    ///     Emits the provided log event from a sink
    /// </summary>
    /// <param name="logEvent">The event being logged</param>
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var renderSpace = new StringWriter();
        textFormatter.Format(logEvent, renderSpace);
        var message = renderSpace.ToString().Trim();
        messageSink?.OnMessage(new DiagnosticMessage { Message = message });
        testOutputHelper?.WriteLine(message);
    }
}
