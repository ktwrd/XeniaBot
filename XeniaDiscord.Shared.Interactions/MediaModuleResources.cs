using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Shared.Interactions;

public static class MediaModuleResources
{
    public const string Namespace = "XeniaDiscord.Shared.Interactions.Resources.MediaModule.";
    public static Stream ImageSpeech => ResourceHelper.GetStream(Namespace + "speech.png");
    public static Stream ImageSpeechBubble => ResourceHelper.GetStream(Namespace + "speechbubble.png");
    public static Stream Image1984 => ResourceHelper.GetStream(Namespace + "1984.png");
    public static Stream Image1984Cover => ResourceHelper.GetStream(Namespace + "1984cover.png");
    public static Stream Image1984OriginalDate => ResourceHelper.GetStream(Namespace + "1984originaldate.png");
}
