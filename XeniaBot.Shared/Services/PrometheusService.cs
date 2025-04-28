using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using System;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared.Services
{
    [XeniaController]
    public class PrometheusService : BaseService
    {
        private ConfigData _configData;
        private ProgramDetails _details;
        protected Prometheus.KestrelMetricServer? Server { get; private set; }
        public PrometheusService(IServiceProvider services)
            : base(services)
        {
            _configData = services.GetRequiredService<ConfigData>();
            _details = services.GetRequiredService<ProgramDetails>();
            Server = new Prometheus.KestrelMetricServer(
            hostname: _configData.Prometheus.Hostname,
                port: _configData.Prometheus.Port,
                 url: _configData.Prometheus.Url);
        }
        public event TaskDelegate? ServerStart;
        public event TaskDelegate? ReloadMetrics;

        public void OnReloadMetrics()
        {
            ReloadMetrics?.Invoke();
        }
        /// <summary>
        /// Invoked when we want to start the server.
        /// Ignored when <see cref="ProgramDetails.Platform"/> is <see cref="XeniaPlatform.WebPanel"/> or <see cref="PrometheusConfigItem.Enable"/> is `false`.
        /// </summary>
        private void OnServerStart()
        {
            if (_details.Platform == XeniaPlatform.WebPanel || !_configData.Prometheus.Enable)
                return;
            string address = _configData.Prometheus.Hostname;
            if (address == "+")
                address = "0.0.0.0";
            Log.Note($"Available at http://{address}:{_configData.Prometheus.Port}{_configData.Prometheus.Url}");
            ServerStart?.Invoke();
        }

        /// <summary>
        /// Initialize Prometheus.
        ///
        /// Ignored when <see cref="ProgramDetails.Platform"/> is <see cref="XeniaPlatform.WebPanel"/> or <see cref="PrometheusConfigItem.Enable"/> is `false`.
        /// </summary>
        public override Task InitializeAsync()
        {
            if (!_configData.Prometheus.Enable || _details.Platform == XeniaPlatform.WebPanel)
            {
                Log.Note("Prometheus Metrics is disabled");
                return Task.CompletedTask;
            }
            Log.Debug($"Starting server");
            Server?.Start();
            OnServerStart();
            return base.InitializeAsync();
        }


        #region MetricServer Wrapper Methods
        public Counter CreateCounter(string name, string help, string[]? labelNames = null, CounterConfiguration? config = null, bool publish = false)
        {
            var c = Metrics.CreateCounter(
                name,
                help,
                labelNames ?? Array.Empty<string>(),
                config);
            if (publish)
                c.Publish();
            return c;
        }
        public Gauge CreateGauge(string name, string help, string[]? labelNames = null, GaugeConfiguration? config = null, bool publish = false)
        {
            var g = Metrics.CreateGauge(
                name, 
                help,
                labelNames ?? Array.Empty<string>(), 
                config);
            if (publish)
                g.Publish();
            return g;
        }
        public Summary CreateSummary(string name, string help, string[]? labelNames = null, SummaryConfiguration? config = null, bool publish = false)
        {
            var s = Metrics.CreateSummary(
                name, 
                help,
                labelNames ?? Array.Empty<string>(), 
                config);
            if (publish)
                s.Publish();
            return s;
        }
        public Histogram CreateHistogram(string name, string help, string[]? labelNames = null, HistogramConfiguration? config = null, bool publish = false)
        {
            var h = Metrics.CreateHistogram(
                name,
                help,
                labelNames ?? Array.Empty<string>(),
                config);
            if (publish)
                h.Publish();
            return h;
        }
        #endregion
    }
}
