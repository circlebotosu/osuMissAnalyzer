using System.Drawing;
using System.Reflection;
using CSharpDiscordWebhook;
using CSharpDiscordWebhook.Objects;
using Microsoft.AspNetCore.Diagnostics;

namespace circlebot.MissAnalyser.Handlers;

public class ExceptionLoggingHandler(ILogger<ExceptionLoggingHandler> logger, IConfiguration config) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogCritical("An unhandled exception occurred: {Exception}", exception);

        var webhookUrl = config["EXCEPTION_WEBHOOK_URL"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogWarning("EXCEPTION_WEBHOOK_URL is not configured; skipping Discord notification.");
            return true;
        }

        try
        {
            var webhook = new DiscordWebhook(new Uri(webhookUrl));
            var embedBuilder = new EmbedBuilder
            {
                Title = $"Exception in path {httpContext.Request.Path}",
                Color = Color.Red,
                Timestamp = DateTime.Now,
                Fields =
                [
                    new EmbedFieldBuilder
                    {
                        Name = "component",
                        Value = Assembly.GetEntryAssembly()!.GetName().Name!,
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "query string",
                        Value = httpContext.Request.QueryString.ToString(),
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "exception message",
                        Value = exception.Message,
                    }
                ]
            };

            var stream = new MemoryStream();
            var sw = new StreamWriter(stream, leaveOpen: true);

            await sw.WriteLineAsync(exception.Message);
            await sw.WriteLineAsync(exception.StackTrace);
            await sw.FlushAsync(cancellationToken);
            await sw.DisposeAsync();

            stream.Position = 0;

            var attachmentBuilder = new StreamAttachmentBuilder
            {
                Filename = "exception.txt",
                Stream = stream,
            };

            var message = new MessageBuilder
            {
                Content = $"<@&{config["DEVELOPER_ROLE_ID"]}>",
                Embeds = [embedBuilder],
                Attachments = [attachmentBuilder]
            };

            await webhook.SendMessageAsync(message);
            webhook.Dispose();
        }
        catch (Exception webhookEx)
        {
            logger.LogError(webhookEx, "Failed to send exception notification to Discord webhook.");
        }

        return true;
    }
}
