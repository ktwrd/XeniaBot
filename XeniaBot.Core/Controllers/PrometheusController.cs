using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Controllers
{
    [BotController]
    public class PrometheusController : BaseController
    {
        private ConfigData _configData;
        protected Prometheus.KestrelMetricServer? Server { get; private set; }
        public PrometheusController(IServiceProvider services)
            : base(services)
        {
            _configData = services.GetRequiredService<ConfigData>();
            Server = new Prometheus.KestrelMetricServer(
            hostname: _configData.Prometheus_Hostname,
                port: _configData.Prometheus_Port,
                 url: _configData.Prometheus_Url);
        }
        public event TaskDelegate? ServerStart;
        public event TaskDelegate? ReloadMetrics;

        public void OnReloadMetrics()
        {
            ReloadMetrics?.Invoke();
        }
        private void OnServerStart()
        {
            string address = _configData.Prometheus_Hostname;
            if (address == "+")
                address = "0.0.0.0";
            Log.Note($"Available at http://{address}:{_configData.Prometheus_Port}{_configData.Prometheus_Url}");
            ServerStart?.Invoke();
        }

        public override Task InitializeAsync()
        {
            if (!_configData.Prometheus_Enable)
            {
                Log.Note("Prometheus Metrics is disabled");
                return Task.CompletedTask;
            }
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
