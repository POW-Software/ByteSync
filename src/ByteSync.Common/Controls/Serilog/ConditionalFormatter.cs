using System.Collections.Generic;
using System.IO;
using System.Linq;
using ByteSync.Common.Helpers;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace ByteSync.Common.Controls.Serilog;

// https://github.com/serilog/serilog-sinks-file/issues/137
public class ConditionalFormatter : ITextFormatter
{
    readonly MessageTemplateTextFormatter
        _properties = new MessageTemplateTextFormatter("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:j}{NewLine}{Properties:j}{NewLine}{Exception}"),
        _minimal = new MessageTemplateTextFormatter("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:j}{NewLine}{Exception}"),
        _memberName = new MessageTemplateTextFormatter("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {MemberName}: {Message:j}.{NewLine}{Exception}"),
        _source = new MessageTemplateTextFormatter("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:j}.{NewLine}{Exception}"),
        _sourceAndMember = new MessageTemplateTextFormatter("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}.{MemberName}: {Message:j}.{NewLine}{Exception}");

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var tokens = new HashSet<string>(logEvent.MessageTemplate.Tokens.OfType<PropertyToken>().Select(p => p.PropertyName));
        
    #if RELEASE
            logEvent.RemovePropertyIfPresent("SourceContext");
    #endif
        
        bool hideProperties = logEvent.Properties.All(p => tokens.Contains(p.Key) || p.Key.In("SourceContext", "MemberName"));

        MessageTemplateTextFormatter formatter;
        if (hideProperties)
        {
            if (logEvent.Properties.Any(p => p.Key.In("SourceContext") && logEvent.Properties.Any(p => p.Key.In("MemberName"))))
            {
                formatter = _sourceAndMember;
            }
            else if (logEvent.Properties.Any(p => p.Key.In("SourceContext")))
            {
                formatter = _source;
            }
            else if (logEvent.Properties.Any(p => p.Key.In("MemberName")))
            {
                formatter = _memberName;
            }
            else
            {
                formatter = _minimal;
            }
        }
        else
        {
            formatter = _properties;
        }

        formatter.Format(logEvent, output);
    }
}