using XeniaBot.Data.Services;
using XeniaBot.Evidence.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Evidence.StorageProxy;

public class Program
{
    public static void Main(string[] args)
    {
        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Core = new CoreContext(Details);
        Core.StartTimestamp = StartTimestamp;
        Core.AlternativeMain = async (a) =>
        {
            await Main_AspNet(a);
            await Task.Delay(-1);
        };
        Core.MainAsync(args, (s) =>
        {
            AttributeHelper.InjectControllerAttributes(typeof(XeniaHelper).Assembly, s);
            AttributeHelper.InjectControllerAttributes(typeof(BanSyncService).Assembly, s);
            AttributeHelper.InjectControllerAttributes(typeof(EvidenceFileRepository).Assembly, s);
            AttributeHelper.InjectControllerAttributes("XeniaBot.Evidence.StorageProxy", s);
            return Task.CompletedTask;
        }).Wait();
    }
    /// <summary>
    /// Unix Timestamp (UTC, Seconds)
    /// </summary>
    public static long StartTimestamp { get; set; }
    public static ProgramDetails Details => new ProgramDetails()
    {
        VersionRaw = typeof(Program).Assembly.GetName().Version,
        Platform = XeniaPlatform.StorageProxy,
        Debug = 
#if DEBUG
            true
#else
false
#endif
    };
    /// <summary>
    /// Please refer to <see cref="CoreContext.Instance"/> if you want to get the current running instance.
    /// </summary>
    public static CoreContext Core { get; private set; }
    public static async Task Main_AspNet(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseAuthorization();


        app.MapControllers();

        await app.RunAsync();
    }
}