﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using SkidBot.Core.Helpers;

namespace SkidBot.Core.Controllers.Wrappers.BigBrother;


public class BB_UserModel
    : BigBrotherBaseModel, IMentionable
{
    #region ISnowflakeEntity
    public DateTimeOffset CreatedAt { get; set; }
    #endregion
    
    #region IUser
    public string AvatarId { get; set; }
    public string Discriminator { get; set; }
    public ushort DiscriminatorValue { get; set; }
    
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public string Username { get; set; }
    
    public UserProperties? PublicFlags { get; set; }
    
    #region IMentionable
    public string Mention { get; set; }
    #endregion
    
    #region IPresence
    public UserStatus Status { get; set; }
    public ClientType[] ActiveClients { get; set; }
    public BB_Activity[] Activities { get; set; }
    #endregion
    #endregion

    public BB_UserModel()
    {
        ActiveClients = Array.Empty<ClientType>();
        Activities = Array.Empty<BB_Activity>();
    }

    public static BB_UserModel? FromUser(IUser? user)
    {
        if (user == null)
            return null;
        var instance = new BB_UserModel();
        instance.Snowflake = user.Id;
        instance.CreatedAt = user.CreatedAt;
        instance.AvatarId = user.AvatarId;
        instance.Discriminator = user.Discriminator;
        instance.DiscriminatorValue = user.DiscriminatorValue;
        instance.IsBot = user.IsBot;
        instance.IsWebhook = user.IsWebhook;
        instance.Username = user.Username;
        instance.PublicFlags = user.PublicFlags;
        instance.Mention = user.Mention;
        instance.Status = user.Status;
        instance.ActiveClients = user.ActiveClients.ToArray();
        instance.Activities = BigBrotherHelper.ForceTypeCast<IReadOnlyCollection<IActivity>, BB_Activity[]>(user.Activities) ?? Array.Empty<BB_Activity>();
        return instance;
    }
}

public class BB_Activity : IActivity
{
    public string Name { get; set; }
    public ActivityType Type { get; set; }
    public ActivityProperties Flags { get; set; }
    public string Details { get; set; }
}