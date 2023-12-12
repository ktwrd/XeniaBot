using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace XeniaBot.Core.Models;

public interface IFlightCheckValidator
{
    /// <summary>
    /// Run FlightCheck for this specific controller.
    /// </summary>
    /// <param name="guild">Guild to run the flight check on</param>
    /// <returns></returns>
    public Task<FlightCheckValidationResult> FlightCheckGuild(SocketGuild guild);
}

/// <summary>
/// Result for <see cref="IFlightCheckValidator.FlightCheckGuild"/>.
///
/// Used to tell <see cref="XeniaBot.Core.Controllers.BotAdditions.FlightCheckController"/> if <see cref="IFlightCheckValidator.FlightCheckGuild"/> ran successfully.
/// </summary>
public class FlightCheckValidationResult
{
    /// <summary>
    /// Was this FlightCheck Validation successful?
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// When <see cref="Success"/> is `false`, this must not be null.
    /// </summary>
    public EmbedFieldBuilder? EmbedField { get; set; }
    /// <summary>
    /// Value of <see cref="EmbedField.Value"/> or <see cref="string.Empty"/>.
    /// </summary>
    public string Message => EmbedField?.Value.ToString() ?? "";
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="success">Did the validation run successfully</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="success"/> is `false` but <paramref name="field"/> is `null`.</exception>
    public FlightCheckValidationResult(bool success, EmbedFieldBuilder? field = null)
    {
        if (!success && field == null)
        {
            throw new ArgumentException($"Argument \"embed\" must not be null when argument \"success\" is \"true\"");
        }

        Success = success;
        EmbedField = field;
    }
}