using System.ComponentModel;

namespace Umbraco.Abilities.Samples.Email;

public sealed record SendEmailInput(
    [property: Description("Recipient email address.")] string To,
    [property: Description("Subject line.")] string Subject,
    [property: Description("Optional body text.")] string? Body = null);

public sealed record SentEmail(string To, string Subject);

/// <summary>Records sent emails. A recipient at bounce.test fails, to show a failed operation rolling a set back.</summary>
public sealed class Outbox
{
    public List<SentEmail> Sent { get; } = [];
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

        if (input.To.EndsWith("@bounce.test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"The mail server rejected {input.To}.");
        }

        var sent = new SentEmail(input.To, input.Subject);
        outbox.Sent.Add(sent);
        return Task.FromResult(Success(sent));
    }
}
