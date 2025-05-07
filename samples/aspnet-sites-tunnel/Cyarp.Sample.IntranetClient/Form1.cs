using CYarp.Client;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Threading;

namespace Cyarp.Sample.IntranetClient
{
    public partial class Form1 : Form
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<ILogger> logger;
        List<ProxySiteConfig> list;
        public Form1(IServiceProvider serviceProvider, ILogger<ILogger> logger)
        {
            list = new List<ProxySiteConfig>
            {
                new ProxySiteConfig{ Domain="qq.demo.com", TargetUri="https://www.qq.com"   },
                new ProxySiteConfig{ Domain="baidu.demo.com", TargetUri="https://www.baidu.com" },
            };
            InitializeComponent();
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.bindingSource1.DataSource = list;
            this.dataGridView1.DataSource = this.bindingSource1;

            foreach (var item in list)
            {
                item.OnEnabledChange += Item_OnEnabledChange;
            }
        }

        private void Item_OnEnabledChange(ProxySiteConfig config)
        {
            if (config.Enable)
            {
                config.WorkerCancelTokenSource = new CancellationTokenSource();
                var cancelToken = config.WorkerCancelTokenSource.Token;
                var serverUri = new Uri(this.txtServerUri.Text);

                var worker = async () =>
                {
                    while (!cancelToken.IsCancellationRequested)
                    {
                        logger.LogInformation($"{Thread.CurrentThread.ManagedThreadId},{config.Domain}");

                        var options = new CYarpClientOptions
                        {
                            ServerUri = serverUri,
                            TargetUri = new Uri(config.TargetUri!),
                            ConnectHeaders = new Dictionary<string, string>
                            {
                                {"HOST",config.Domain!}
                            }

                        };
                        using var client = new CYarp.Client.CYarpClient(options, this.logger);
                        await client.TransportAsync(cancelToken);

                        await Task.Delay(TimeSpan.FromSeconds(5d), cancelToken);
                    }
                    logger.LogInformation($"{Thread.CurrentThread.ManagedThreadId},{config.Domain} cancelled");
                };

                Task.Run(worker, config.WorkerCancelTokenSource.Token);
            }
            else
            {
                config.WorkerCancelTokenSource.Cancel();
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1 && e.ColumnIndex == 0)
            {
                this.list[e.RowIndex].Enable = !this.list[e.RowIndex].Enable;
            }
        }
    }
}
