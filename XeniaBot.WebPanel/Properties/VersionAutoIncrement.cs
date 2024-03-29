
using System;
using System.Reflection;

[assembly: AssemblyTitle("Xenia Bot Dashboard")]
[assembly: AssemblyVersion("0.8.818.107")]
[assembly: AssemblyFileVersion("0.8.818.107")]

namespace XeniaBot.WebPanel
{
	/// <summary>
	/// Information about this build
	/// </summary>
	public static class BuildInformation
	{
		/// <summary>
		/// Timestamp for when this was built at (Seconds since UTC Unix Epoch)
		/// </summary>
		public static long CreatedAtTimestamp => 1711676835;
		/// <summary>
		/// When this build was created.
		/// </summary>
		public static DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeSeconds(1711676835);
	}
}

