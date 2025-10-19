using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

[XmlRoot("XeniaConfig")]
public class XeniaConfig
{
    public static XeniaConfig? Instance { get; set; }
    public static XeniaConfig Get()
    {
        if (Instance != null)
        {
            return Instance;
        }
        var location = FeatureFlags.XmlConfigLocation;
        if (!File.Exists(location))
        {
            throw new InvalidOperationException($"Cannot get config since {location} doesn't exist (via {nameof(FeatureFlags)}.{nameof(FeatureFlags.XmlConfigLocation)})");
        }
        Instance = new();
        Instance.ReadFromFile(location);
        return Instance;
    }
    public void WriteToFile(string location)
    {
        using var file = new FileStream(location, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        file.SetLength(0);
        file.Seek(0, SeekOrigin.Begin);
        Write(file);
    }

    public void ReadFromFile(string location)
    {
        if (!File.Exists(location))
        {
            throw new ArgumentException($"{location} does not exist", nameof(location));
        }

        var content = File.ReadAllText(location);
        var xmlSerializer = new XmlSerializer(GetType());
        var xmlTextReader = new XmlTextReader(new StringReader(content)) { XmlResolver = null };
        var data = (XeniaConfig?)xmlSerializer.Deserialize(xmlTextReader);
        if (data == null)
        {
            return;
        }

        foreach (var p in GetType().GetProperties())
        {
            p.SetValue(this, p.GetValue(data));
        }

        foreach (var f in GetType().GetFields())
        {
            f.SetValue(this, f.GetValue(data));
        }
    }

    public void Write(Stream stream)
    {
        var serializer = new XmlSerializer(GetType());
        var options = new XmlWriterSettings()
        {
            Indent = true
        };
        using var writer = XmlWriter.Create(stream, options);
        serializer.Serialize(writer, this);
    }

    [XmlElement("Database")]
    public PostgreSQLConfigElement Database { get; set; } = new();

    [XmlElement("MongoDb")]
    public MongoDbConfigElement MongoDb { get; set; } = new();

    [XmlElement("Discord")]
    public DiscordConfigElement Discord { get; set; } = new();

    [XmlElement("Cache")]
    public CacheConfigElement Cache { get; set; } = new();

    [XmlElement("Services.Reminder")]
    public ReminderServiceConfigElement ReminderService { get; set; } = new();

    [XmlElement("Services.ErrorReporting")]
    public ErrorReportConfigElement ErrorReporting { get; set; } = new();

    [XmlElement("Services.Dashboard")]
    public DashboardConfigElement Dashboard { get; set; } = new();

    [XmlElement("ApiKey")]
    public ApiKeysConfigElement ApiKey { get; set; } = new();

    [XmlElement("SupportServerUrl")]
    public string SupportServerUrl { get; set; }
}
