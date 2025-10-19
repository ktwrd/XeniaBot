using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaDiscord.Common;

public static class InteractionIdentifier
{
    public const string ConfessionModalCreate = "ns:event:discord:modalinteraction:confession:create";
    public const string ConfessionModal = "ns:event:discord:modalinteraction:confession:modal";
    public const string ConfessionModalContent = ConfessionModal + ":content";
}
