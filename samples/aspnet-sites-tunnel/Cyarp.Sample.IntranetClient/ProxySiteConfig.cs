using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cyarp.Sample.IntranetClient;


public delegate void ProxySiteConfigEnableChangedEventHandler(ProxySiteConfig e);

public class ProxySiteConfig
{
    public event ProxySiteConfigEnableChangedEventHandler? OnEnabledChange;

    public bool Enable
    {
        get;
        set
        {
            field = value;
            OnEnabledChange?.Invoke(this);
        }
    }

    public string? Domain { get; set; }

    public string? TargetUri { get; set; }

    public CancellationTokenSource WorkerCancelTokenSource { get; set; }

    public ProxySiteConfig()
    {
        WorkerCancelTokenSource = new CancellationTokenSource();
    }


}

