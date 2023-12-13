using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace XeniaBot.Data.Models;

/// <summary>
/// To make use of the FlightCheck Validator, inherit this on a class that already inherits <see cref="XeniaBot.Shared.BaseController"/>
/// </summary>
public interface IFlightCheckValidator
{
    /// <summary>
    /// Run FlightCheck for this specific controller.
    /// </summary>
    /// <param name="guild">Guild to run the flight check on</param>
    /// <returns>Validation result. Look at the docs for <see cref="FlightCheckValidationResult"/></returns>
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
    /// Generate embed that is sent to the user.
    ///
    /// Field value will be formatted in a list, where each list item is an issue and <see cref="Description"/>
    /// will be prepended before the issue list.
    ///
    /// If an issue item has a line break, then every item after the 1st will be an indented list item like
    /// ```md
    /// Description
    /// - splitted[0]
    ///   * splitted[1]
    /// ```
    /// </summary>
    /// <returns>Will be `null` when <see cref="Success"/> is `true`.</returns>
    public EmbedFieldBuilder? GetEmbedField()
    {
        if (Success)
            return null;

        var val = new List<string>();
        foreach (var item in Issues)
        {
            var splitted = item.Split("\n");
            var inner = "";
            for (int i = 0; i < splitted.Length; i++)
            {
                if (i == 0)
                {
                    inner += $"- {splitted[i]}";
                }
                else
                {
                    inner += $"\n  * {splitted[i]}";
                }
            }
            val.Add(inner);
        }
        
        return new EmbedFieldBuilder()
            .WithName(Title)
            .WithValue(string.Join("\n", new string[]
            {
                Description,
                string.Join("\n", val)
            }));
    }
    
    /// <summary>
    /// Title of the FlightCheck that was done.
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Small description, should say what the user should do to resolve all issues.
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Issues that exist in this FlightCheck. When <see cref="Success"/> is `false`,
    /// this will always have at least `1` item.
    /// </summary>
    public List<string> Issues { get; set; }
    
    /// <param name="success">Did the validation run successfully</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="success"/> is `false` but <paramref name="field"/> is `null`.</exception>
    public FlightCheckValidationResult(bool success, string title, string description = "", IEnumerable<string>? issues = null)
    {
        var issueList = issues?.ToList() ?? new List<string>();
        if (!success && (issues == null || issueList.Count < 1))
        {
            throw new ArgumentException($"Argument \"issues\" must be set and have at least \"1\" item when argument \"success\" is \"false\"");
        }

        Success = success;
        Title = title;
        Description = description;
        Issues = issueList;
    }
}