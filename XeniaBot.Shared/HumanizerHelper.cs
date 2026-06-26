using Humanizer;

namespace XeniaBot.Shared;

public class HumanizerHelper
{
    public static void UpdateVocabulary()
    {
        Vocabularies.Default.AddPlural("role", "roles");
        Vocabularies.Default.AddPlural("guild", "guilds");
        Vocabularies.Default.AddPlural("record", "records");
    }
}