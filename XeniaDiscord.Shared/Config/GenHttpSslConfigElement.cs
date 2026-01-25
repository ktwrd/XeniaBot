using System;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Serialization;

namespace XeniaDiscord.Shared.Config;

public class GenHttpSslConfigElement : IXmlSerializable
{
    /// <summary>
    /// Should GenHTTP use SSL?
    /// When enabled, <see cref="ContentType"/> and <see cref="Content"/> is required.
    /// </summary>
    /// <remarks>
    /// This property is stored as an XML Element.
    /// </remarks>
    public bool Enabled { get; private set; } = false;

    /// <remarks>
    /// This property is stored as an XML Element with the name "ContentType"
    /// </remarks>
    public HealthSslContentType? ContentType { get; private set; }

    /// <summary>
    /// X.509 Certificate password (optional)
    /// </summary>
    /// <remarks>
    /// This property is stored as an XML Element with the name "Password"
    /// </remarks>
    public SecureString? Password { get; private set; }

    /// <summary>
    /// Content of the element.
    /// </summary>
    public string? Content { get; private set; }


    private X509Certificate2? _cachedCert = null;

    public byte[] ReadContent()
    {
        if (ContentType == null)
            throw new InvalidOperationException($"Property {nameof(ContentType)} cannot be null.");
        if (string.IsNullOrEmpty(Content))
            throw new InvalidOperationException($"Property {nameof(Content)} cannot be null.");

        return ContentType.Value switch
        {
            HealthSslContentType.Base64 => Convert.FromBase64String(Content),
            HealthSslContentType.FileLocation => File.ReadAllBytes(Content),
            _ => throw new NotImplementedException($"Unknown value for {nameof(ContentType)}: {ContentType}")
        };
    }

    private X509Certificate2 ParseCertificate()
    {
        var content = ReadContent();
        return new(content, Password);
    }
    public X509Certificate2 GetCertificate()
    {
        if (_cachedCert != null)
            return _cachedCert;
        return ParseCertificate();
    }

    #region IXmlSerializable
    /// <inheritdoc/>
    public System.Xml.Schema.XmlSchema? GetSchema()
    {
        return (null);
    }
    /// <inheritdoc/>
    public void ReadXml(XmlReader reader)
    {
        Enabled = false;
        ContentType = null;
        Password = null;
        Content = null;
        if (reader.HasAttributes)
        {
            var enabledValueString = reader.GetAttribute("Enabled");
            Enabled = bool.TryParse(enabledValueString, out var enabledValue) && enabledValue;

            var certPassValue = reader.GetAttribute("Password");
            var contentTypeValue = reader.GetAttribute("ContentType");

            if (string.IsNullOrEmpty(contentTypeValue))
            {
                throw new InvalidDataException("Attribute \"ContentType\" is required.");
            }

            if (!Enum.TryParse<HealthSslContentType>(contentTypeValue, out var contentType))
            {
                throw new InvalidOperationException($"Failed to parse value for attribute \"ContentType\" (value: {contentTypeValue})");
            }

            Password = certPassValue == null ? null
                    : new NetworkCredential("", certPassValue).SecurePassword;
        }

        reader.MoveToContent();

        var isEmptyElement = reader.IsEmptyElement;
        reader.ReadStartElement();
        if (!isEmptyElement)
        {
            var str = reader.ReadContentAsString();
            Content = str;
            reader.ReadEndElement();
        }

        if (Enabled)
        {
            _cachedCert = ParseCertificate();
        }
    }
    /// <inheritdoc/>
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("Enabled", Enabled.ToString());

        if (ContentType.HasValue && Enabled)
        {
            writer.WriteAttributeString("ContentType", ContentType.ToString());
        }
        if (Password != null && Enabled)
        {
            writer.WriteAttributeString("Password", new NetworkCredential("", Password).Password);
        }
        if (Content != null && Enabled)
        {
            writer.WriteString(Content);
        }
    }
    #endregion
}

public enum HealthSslContentType
{
    Base64,
    FileLocation
}
