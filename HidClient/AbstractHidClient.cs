using System.ComponentModel;
using System.Runtime.CompilerServices;
using HidSharp;

namespace HidClient;

/// <inheritdoc cref="IHidClient"/>
public abstract class AbstractHidClient: IHidClient {

    private readonly object _deviceStreamLock = new();

    private DeviceList?              _deviceList;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool                     _isConnected;
    private int                      _maxInputReportLength;

    /// <summary>
    /// <para><c>HidSharp</c> stream that can be used to read or write bytes from the device, or set features.</para>
    /// <para>This will be <see langword="null"/> when the device is disconnected.</para>
    /// </summary>
    protected HidStream? DeviceStream;

    /// <summary>
    /// <para>The USB Vendor ID or <c>VID</c> of the device you want to connect to.</para>
    /// <para>In Windows, this can be found in Device Manager as the hexadecimal <c>VID</c> value under Hardware Ids.</para>
    /// <para>In Linux, this can be found in the output of <c>lsusb</c> in the hexadecimal <c>ID</c> colon-delimited value.</para>
    /// </summary>
    protected abstract int VendorId { get; }

    /// <summary>
    /// <para>The USB Product ID or <c>PID</c> of the device you want to connect to.</para>
    /// <para>In Windows, this can be found in Device Manager as the hexadecimal <c>PID</c> value under Hardware Ids.</para>
    /// <para>In Linux, this can be found in the output of <c>lsusb</c> in the hexadecimal <c>ID</c> colon-delimited value.</para>
    /// </summary>
    protected abstract int ProductId { get; }

    /// <inheritdoc />
    public event EventHandler<bool>? IsConnectedChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public SynchronizationContext EventSynchronizationContext { get; set; } = SynchronizationContext.Current ?? new SynchronizationContext();

    /// <summary>
    /// <para>Construct a new instance using the local system device list. This is the default invocation which is recommended for general use.</para>
    /// <para>During construction, this new instance will attempt to connect to a device with the given <see cref="VendorId"/> and <see cref="ProductId"/>.</para>
    /// </summary>
    protected AbstractHidClient(): this(DeviceList.Local) { }

    /// <summary>
    /// <para>Construct a new instance using a custom device list. This is an advanced invocation that is useful for unit testing.</para>
    /// <para>During construction, this new instance will attempt to connect to a device with the given <see cref="VendorId"/> and <see cref="ProductId"/>.</para>
    /// </summary>
    protected AbstractHidClient(DeviceList deviceList) {
        _deviceList         =  deviceList;
        _deviceList.Changed += OnDeviceListChanged;
        AttachToDevice();
    }

    /// <inheritdoc />
    public bool IsConnected {
        get => _isConnected;
        private set {
            if (value != _isConnected) {
                _isConnected = value;
                EventSynchronizationContext.Post(_ => {
                    IsConnectedChanged?.Invoke(this, value);
                    OnPropertyChanged();
                }, null);
            }
        }
    }

    private void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e) {
        AttachToDevice();
    }

    private void AttachToDevice() {
        bool isNewStream = false;
        lock (_deviceStreamLock) {
            if (DeviceStream == null) {
                HidDevice? newDevice = _deviceList?.GetHidDeviceOrNull(VendorId, ProductId);
                if (newDevice != null) {
                    DeviceStream          = newDevice.Open();
                    _maxInputReportLength = newDevice.GetMaxInputReportLength();
                    isNewStream           = true;
                }
            }
        }

        if (DeviceStream != null && isNewStream) {
            DeviceStream.Closed      += ReattachToDevice;
            DeviceStream.ReadTimeout =  Timeout.Infinite;
            _cancellationTokenSource =  new CancellationTokenSource();
            IsConnected              =  true;
            OnConnect();

            try {
                Task.Factory.StartNew(HidReadLoop, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            } catch (TaskCanceledException) { }
        }
    }

    /// <summary>
    /// Called when connected to a device after it was previously disconnected. When invoked, <see cref="IsConnected"/> will be <see langword="true"/>.
    /// </summary>
    protected internal virtual void OnConnect() { }

    private async Task HidReadLoop() {
        CancellationToken cancellationToken = _cancellationTokenSource!.Token;

        try {
            byte[] readBuffer = new byte[_maxInputReportLength > 0 ? _maxInputReportLength : 128];
            while (!cancellationToken.IsCancellationRequested) {
                int readBytes = await DeviceStream!.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false);
                if (readBytes != 0) {
                    byte[] filledReadBuffer = readBuffer;
                    if (readBuffer.Length != readBytes) {
                        filledReadBuffer = new byte[readBytes];
                        Array.Copy(readBuffer, 0, filledReadBuffer, 0, readBytes);
                    }

                    OnHidRead(filledReadBuffer);
                }
            }
        } catch (IOException) {
            ReattachToDevice();
        }
    }

    /// <summary>
    /// Callback method that is invoked when HID bytes are read from the device.
    /// </summary>
    /// <param name="readBuffer">Bytes that were read from the device, matching the <c>HID Data</c> field in USBPcap.</param>
    protected internal abstract void OnHidRead(byte[] readBuffer);

    private void ReattachToDevice(object? sender = null, EventArgs? e = null) {
        bool disconnected = false;
        lock (_deviceStreamLock) {
            if (DeviceStream != null) {
                DeviceStream.Closed -= ReattachToDevice;
                DeviceStream.Close();
                DeviceStream.Dispose();
                DeviceStream = null;
                disconnected = true;
            }
        }

        if (disconnected) {
            IsConnected = false;
        }

        try {
            _cancellationTokenSource?.Cancel();
        } catch (AggregateException) { }

        AttachToDevice();
    }

    /// <summary>
    /// Raise the <see cref="PropertyChanged"/> event
    /// </summary>
    /// <param name="propertyName">the name of the property that changed, defaults to the caller member name</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// <para>Clean up managed and, optionally, unmanaged resources.</para>
    /// <para>When inheriting from <see cref="AbstractHidClient"/>, you should override this method, dispose of your managed resources if <paramref name="disposing"/> is <see langword="true" />, then
    /// free your unmanaged resources regardless of the value of <paramref name="disposing"/>, and finally call this base <see cref="Dispose(bool)"/> implementation.</para>
    /// <para>For more information, see <see url="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#implement-the-dispose-pattern-for-a-derived-class">Implement
    /// the dispose pattern for a derived class</see>.</para>
    /// </summary>
    /// <param name="disposing">Should be <see langword="false" /> when called from a finalizer, and <see langword="true" /> when called from the <see cref="Dispose()"/> method. In other words, it is
    /// <see langword="true" /> when deterministically called and <see langword="false" /> when non-deterministically called.</param>
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            try {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            } catch (AggregateException) { }

            lock (_deviceStreamLock) {
                if (DeviceStream != null) {
                    DeviceStream.Closed -= ReattachToDevice;
                    DeviceStream.Close();
                    DeviceStream.Dispose();
                    DeviceStream = null;
                }
            }

            if (_deviceList != null) {
                _deviceList.Changed -= OnDeviceListChanged;
                _deviceList         =  null;
            }
        }
    }

    /// <summary>
    /// <para>Disconnect from any connected device and clean up managed resources.</para>
    /// <para><see cref="IsConnectedChanged"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events will not be emitted if a device is disconnected during disposal.</para>
    /// <para>Subclasses of <see cref="AbstractHidClient"/> should override <see cref="Dispose(bool)"/>.</para>
    /// <para>For more information, see <see url="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/unmanaged">Cleaning Up Unmanaged Resources</see> and
    /// <see url="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose">Implementing a Dispose Method</see>.</para>
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}