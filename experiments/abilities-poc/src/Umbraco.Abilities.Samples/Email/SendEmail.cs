using System.ComponentModel;

namespace Umbraco.Abilities.Samples.Email;

public sealed record SendEmailInput(
    [property: Description("Recipient email address.")] string To,
    [property: Description("Subject line.")] string Subject,
    [property: Description("Optional body text.")] string? Body = null);

public sealed record SentEmail(string To, string Subject);

/// <summary>Records sent emails. While the server is down, sending fails, to show a failed step and a resumed apply.</summary>
public sealed class Outbox
{
    public List<SentEmail> Sent { get; } = [];

    public bool ServerDown { get; set; }
}

[Ability("email.send", "Send email", Kind = AbilityKind.Write,
    Description = "Sends an email.")]
public sealed class SendEmailAbility(Outbox outbox) : AbilityBase<SendEmailInput, SentEmail>
{
    protected override Task<AbilityResult> ExecuteAsync(SendEmailInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.To) || !input.To.Contains('@'))
        {
            throw new AbilityInputException("A valid recipient is required.", [new Problem("to", "Not an email address.")]);
        }

        if (outbox.ServerDown)
        {
            throw new InvalidOperationException("The mail server is not responding.");
        }

        var sent = new SentEmail(input.To, input.Subject);
        outbox.Sent.Add(sent);
        return Task.FromResult(Success(sent));
    }
}
