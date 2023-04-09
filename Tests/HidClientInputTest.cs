using FakeItEasy.Core;
using HidSharp;

namespace Tests;

public class HidClientInputTest {

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(4);

    private readonly HidDevice  _device     = A.Fake<HidDevice>();
    private readonly DeviceList _deviceList = A.Fake<DeviceList>();
    private readonly HidStream  _stream     = A.Fake<HidStream>();

    public HidClientInputTest() {
        A.CallTo(() => _device.VendorID).Returns(0x077d);
        A.CallTo(() => _device.ProductID).Returns(0x0410);
        A.CallTo(() => _device.GetMaxInputReportLength()).Returns(4);

        A.CallTo(() => _deviceList.GetDevices(A<DeviceTypes>._)).Returns(new[] { _device });

        A.CallTo(_device).Where(call => call.Method.Name == "OpenDeviceAndRestrictAccess").WithReturnType<DeviceStream>().Returns(_stream);
        A.CallTo(() => _stream.ReadAsync(A<byte[]>._, An<int>._, An<int>._, A<CancellationToken>._)).ReturnsLazily(FakeReadAsync(0, 1, 2, 3));
    }

    private static Func<IFakeObjectCall, Task<int>> FakeReadAsync(params byte[] fakeHidBytes) => call => {
        byte[] buffer        = (byte[]) call.Arguments[0]!;
        int    offset        = (int) call.Arguments[1]!;
        int    count         = (int) call.Arguments[2]!;
        int    occupiedCount = Math.Min(count, fakeHidBytes.Length);
        Array.Copy(fakeHidBytes, 0, buffer, offset, occupiedCount);
        return Task.FromResult(occupiedCount);
    };

    [Fact]
    public void Constructor() {
        new FakeHidClient().Dispose();
    }

    [Fact]
    public void Read() {
        FakeHidClient        client       = new(_deviceList);
        ManualResetEventSlim eventArrived = new();
        byte[]?              actualEvent  = null;
        client.HidRead += (_, @event) => {
            actualEvent = @event;
            eventArrived.Set();
        };
        eventArrived.Wait(TestTimeout);
        actualEvent.Should().NotBeNull();
        actualEvent.Should().BeEquivalentTo(new byte[] { 0, 1, 2, 3 });
    }

    [Fact]
    public void LateAttach() {
        A.CallTo(() => _deviceList.GetDevices(A<DeviceTypes>._)).ReturnsNextFromSequence(
            Enumerable.Empty<HidDevice>(),
            new[] { _device });

        bool?         connectedEventArg = null;
        FakeHidClient client            = new(_deviceList);
        client.IsConnected.Should().BeFalse();
        byte[]?              actualEvent        = null;
        ManualResetEventSlim inputReceived      = new();
        ManualResetEventSlim isConnectedChanged = new();
        client.HidRead += (_, @event) => {
            actualEvent = @event;
            inputReceived.Set();
        };
        client.IsConnectedChanged += (_, b) => {
            connectedEventArg = b;
            isConnectedChanged.Set();
        };

        _deviceList.RaiseChanged();

        inputReceived.Wait(TestTimeout);
        isConnectedChanged.Wait(TestTimeout);

        client.IsConnected.Should().BeTrue();
        connectedEventArg.HasValue.Should().BeTrue();
        connectedEventArg!.Value.Should().BeTrue();
        actualEvent.Should().NotBeNull();
        actualEvent.Should().BeEquivalentTo(new byte[] { 0, 1, 2, 3 });
    }

    [Fact]
    public void Reconnect() {
        A.CallTo(() => _stream.ReadAsync(A<byte[]>._, An<int>._, An<int>._, A<CancellationToken>._))
            .ThrowsAsync(new IOException("fake disconnected")).Once().Then
            .ReturnsLazily(FakeReadAsync(5, 6, 7, 8));

        ManualResetEventSlim eventArrived = new();
        FakeHidClient        client       = new(_deviceList);
        byte[]?              actualEvent  = null;
        client.HidRead += (_, @event) => {
            actualEvent = @event;
            eventArrived.Set();
        };

        _deviceList.RaiseChanged();

        eventArrived.Wait(TestTimeout);
        actualEvent.Should().NotBeNull();
        actualEvent.Should().BeEquivalentTo(new byte[] { 5, 6, 7, 8 });
    }

    [Fact]
    public void SynchronizationContext() {
        A.CallTo(() => _stream.ReadAsync(A<byte[]>._, An<int>._, An<int>._, A<CancellationToken>._))
            .ThrowsAsync(new IOException("fake disconnected"));

        ManualResetEventSlim   eventArrived           = new();
        SynchronizationContext synchronizationContext = A.Fake<SynchronizationContext>();
        A.CallTo(() => synchronizationContext.Post(A<SendOrPostCallback>._, An<object?>._)).Invokes(() => eventArrived.Set());

        using FakeHidClient client = new(_deviceList) { EventSynchronizationContext = synchronizationContext };

        A.CallTo(() => _stream.ReadAsync(A<byte[]>._, An<int>._, An<int>._, A<CancellationToken>._))
            .ThrowsAsync(new IOException("fake disconnected")).Once().Then
            .ReturnsLazily(FakeReadAsync(5, 6, 7));

        eventArrived.Wait(TestTimeout);

        A.CallTo(() => synchronizationContext.Post(A<SendOrPostCallback>._, An<object?>._)).MustHaveHappenedOnceOrMore();
    }

    [Fact]
    public void Dispose() {
        FakeHidClient client = new(_deviceList);
        client.Dispose();
    }

    [Fact]
    public void DisposeIdempotent() {
        FakeHidClient client = new(_deviceList);
        client.Dispose();
        client.Dispose();
    }

}