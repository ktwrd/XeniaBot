
using System;
using System.Reflection;

[assembly: AssemblyTitle("Xenia Bot")]
[assembly: AssemblyVersion("1.10.818.107")]
[assembly: AssemblyFileVersion("1.10.818.107")]

namespace XeniaBot.Core
{
	/// <summary>
	/// Information about this build
	/// </summary>
	public static class BuildInformation
	{
		/// <summary>
		/// Timestamp for when this was built at (Seconds since UTC Unix Epoch)
		/// </summary>
		public static long CreatedAtTimestamp => 1711676840;
		/// <summary>
		/// When this build was created.
		/// </summary>
		public static DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeSeconds(1711676840);
	}
}

