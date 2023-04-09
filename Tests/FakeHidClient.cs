using HidClient;
using HidSharp;

namespace Tests;

public class FakeHidClient: AbstractHidClient {

    public FakeHidClient() { }
    public FakeHidClient(DeviceList deviceList): base(deviceList) { }

    public event EventHandler<byte[]>? HidRead;

    protected override int VendorId { get; } = 0x077d;
    protected override int ProductId { get; } = 0x0410;

    protected internal override void OnHidRead(byte[] readBuffer) {
        HidRead?.Invoke(this, readBuffer);
    }

}