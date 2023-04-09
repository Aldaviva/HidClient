using System.ComponentModel;

namespace HidClient;

/// <summary>
/// <para>Common library to receive updates from a USB HID and reconnect automatically when disconnected.</para>
/// <para>To get started developing a HID client, subclass <see cref="AbstractHidClient"/>.</para>
/// </summary>
public interface IHidClient: IDisposable, INotifyPropertyChanged {

    /// <summary>
    /// <para><see langword="true" /> if the client is currently connected to a HID, or <see langword="false" /> if it is disconnected, possibly because there is no such device
    /// plugged into the computer.</para>
    /// <para><see cref="AbstractHidClient"/> will automatically try to connect to a device when you construct a new instance, so you don't have to call any additional methods in order to make 
    /// it start connecting.</para>
    /// <para>If a device is plugged in, <see cref="IsConnected"/> will already be <see langword="true" /> by the time the <see cref="AbstractHidClient"/> constructor returns.</para>
    /// <para>To receive notifications when this property changes, you can subscribe to the <see cref="IsConnectedChanged"/> or <see cref="INotifyPropertyChanged.PropertyChanged"/> events.</para>
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// <para>Fired whenever the connection state of the device changes. Not fired when constructing or disposing the <see cref="AbstractHidClient"/> instance.</para>
    /// <para>The event argument contains the new value of <see cref="IsConnected"/>.</para>
    /// <para>This value can also be accessed at any time by reading the <see cref="IsConnected"/> property.</para>
    /// <para>If you want to use data binding which expects <see cref="INotifyPropertyChanged.PropertyChanged"/> events, <see cref="IHidClient"/> also implements
    /// <see cref="INotifyPropertyChanged"/>, so you can use that event instead.</para>
    /// </summary>
    event EventHandler<bool> IsConnectedChanged;

    /// <summary>
    /// <see cref="SynchronizationContext"/> on which to run event callbacks. Useful if your delegates need to update a user interface on the main thread. Callbacks run on the current thread by
    /// default.
    /// </summary>
    SynchronizationContext EventSynchronizationContext { get; set; }

}