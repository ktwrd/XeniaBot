using XeniaBot.Shared;
using XeniaDiscord.Common.Services.BanSync;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.UnitTest;

public class RequestBanSyncFeatureResultTests
{
    [Test]
    public void FormatMessage_PendingRequest()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.PendingRequest, mdl);
        Assert.That(data.Success, Is.False);
        
        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(RequestBanSyncFeatureResult.MsgPendingRequest));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(RequestBanSyncFeatureResult.MsgPendingRequest));
    }

    [Test]
    public void FormatMessage_TooYoung()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.TooYoung, mdl);
        Assert.That(data.Success, Is.False);

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(
            string.Format(
                RequestBanSyncFeatureResult.MsgTooYoung,
                "12 weeks old (about 3 months)")));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(
            string.Format(
                RequestBanSyncFeatureResult.MsgWebTooYoung,
                "12 weeks old (about 3 months)")));
    }

    [Test]
    public void FormatMessage_Blacklisted_WithSupport()
    {
        var cfg = new ConfigData()
        {
            SupportServerUrl = "https://discord.gg/example"
        };
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.Blacklisted, mdl);
        Assert.That(data.Success, Is.False);

        var expectedEmbed = RequestBanSyncFeatureResult.MsgBlacklisted + "\n"
            + string.Format(RequestBanSyncFeatureResult.MoreInfoSuffix, "https://discord.gg/example");

        var expectedDash = "Unable to request for BanSync feature since your server has been blacklisted.\n"
            + string.Format(RequestBanSyncFeatureResult.MoreInfoSuffix, "https://discord.gg/example");

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }
    [Test]
    public void FormatMessage_Blacklisted_WithNoSupport()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.Blacklisted, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = RequestBanSyncFeatureResult.MsgBlacklisted;
        const string expectedDash = "Unable to request for BanSync feature since your server has been blacklisted.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }

    [Test]
    public void FormatMessage_MissingBanMembersPermission()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.MissingBanMembersPermission, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Xenia is missing the \"Ban Members\" permission.\n" +
                                     "**This is required** for Xenia to see who's been banned in your server.\n" +
                                     "-# [Source](https://docs.discord.com/developers/resources/guild#get-guild-bans)";
        const string expectedDash = "Xenia is missing the \"Ban Members\" permission.\n" +
                                    "**This is required** for Xenia to see who's been banned in your server.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }


    [Test]
    public void FormatMessage_LogChannelMissing()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.LogChannelMissing, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Log channel has not been configured, please do so with the command: `/bansync setchannel`";
        const string expectedDash = "Unable to request for BanSync feature since you haven't configured a log channel.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }

    [Test]
    public void FormatMessage_LogChannelCannotAccess_WithChannelMention()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel()
        {
            LogChannelId = "123456"
        };
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.LogChannelCannotAccess, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Xenia is unable to access the log channel that you've configured: <#123456>\n" +
                                     "Please make sure it still exists, and that Xenia has the correct permissions. " +
                                     "([see guide](https://xenia.kate.pet/guide/required_permissions#content-bansync))";
        const string expectedDash =
            "Unable to request for BanSync feature since Xenia is unable to access the log channel that is currently configured.\n" +
            "Please make sure that it still exists, and that Xenia has the correct permissions. " +
            "([see guide](https://xenia.kate.pet/guide/required_permissions#content-bansync))\n" +
            "If it does not exist anymore, please select a new one from the settings below.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }

    [Test]
    public void FormatMessage_LogChannelCannotAccess_WithoutChannelMention()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.LogChannelCannotAccess, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Xenia is unable to access the log channel that you've configured.\n" +
                                     "Please make sure it still exists, and that Xenia has the correct permissions. " +
                                     "([see guide](https://xenia.kate.pet/guide/required_permissions#content-bansync))";
        const string expectedDash =
            "Unable to request for BanSync feature since Xenia is unable to access the log channel that is currently configured.\n" +
            "Please make sure that it still exists, and that Xenia has the correct permissions. " +
            "([see guide](https://xenia.kate.pet/guide/required_permissions#content-bansync))\n" +
            "If it does not exist anymore, please select a new one from the settings below.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }
    [Test]
    public void FormatMessage_LogChannelCannotSendMessages_WithChannelMention()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel()
        {
            LogChannelId = "123456"
        };
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.LogChannelCannotSendMessages, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Xenia is missing the `Send Messages` permission in the configured log channel: <#123456>";
        const string expectedDash = "Unable to request for BanSync feature, since Xenia is missing the `Send Messages` permission in the configured log channel.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }

    [Test]
    public void FormatMessage_LogChannelCannotSendMessages_WithoutChannelMention()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.LogChannelCannotSendMessages, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Xenia is missing the `Send Messages` permission in the configured log channel.";
        const string expectedDash = "Unable to request for BanSync feature, since " + expectedEmbed;

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }


    [Test]
    public void FormatMessage_LogChannelCannotEmbedLinks_WithChannelMention()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel()
        {
            LogChannelId = "123456"
        };
        Assert.That(mdl.LogChannelId.ParseULong(false), Is.EqualTo(123456));
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.LogChannelCannotSendEmbeds, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Xenia is missing the `Embed Links` permission in the configured log channel: <#123456>";
        const string expectedDash = "Unable to request for BanSync feature, since Xenia is missing the `Embed Links` permission in the configured log channel.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }

    [Test]
    public void FormatMessage_LogChannelCannotEmbedLinks_WithoutChannelMention()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.LogChannelCannotSendEmbeds, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Xenia is missing the `Embed Links` permission in the configured log channel.";
        const string expectedDash = "Unable to request for BanSync feature, since " + expectedEmbed;

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }

    [Test]
    public void FormatMessage_InternalError()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.InternalError, mdl);
        Assert.That(data.Success, Is.False);

        const string expectedEmbed = "Internal error.";
        const string expectedDash = "Failed to request for BanSync feature due to an internal error.";

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }

    [Test]
    public void FormatMessage_Valid()
    {
        var cfg = new ConfigData();
        var mdl = new BanSyncGuildModel();
        var data = new RequestBanSyncFeatureResult(cfg, BanSyncGuildKind.Valid, mdl);
        Assert.That(data.Success, Is.True);

        const string expectedEmbed = "Congratulations! Your server is eligible for the BanSync Feature!";
        const string expectedDash = expectedEmbed;

        var msgEmbed = data.FormatMessage(FormatMessageKind.MessageEmbed);
        Assert.That(msgEmbed, Is.EqualTo(expectedEmbed));

        var msgDash = data.FormatMessage(FormatMessageKind.Dashboard);
        Assert.That(msgDash, Is.EqualTo(expectedDash));
    }
}