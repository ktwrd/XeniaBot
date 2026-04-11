using System;
using System.Collections.Generic;
using System.Text;

namespace XeniaBot.Shared;

/// <summary>
/// Attribute used on a class or method to define that it should be included when sending commands to discordbotlist.com
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class RegisterDBLCommandAttribute : Attribute
{
}
