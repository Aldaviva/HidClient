HidClient
===

[![Nuget](https://img.shields.io/nuget/v/HidClient?logo=nuget)](https://www.nuget.org/packages/HidClient/) [![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/Aldaviva/HidClient/dotnetpackage.yml?branch=master&logo=github)](https://github.com/Aldaviva/HidClient/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:HidClient/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B)](https://aldaviva.testspace.com/spaces/210789) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/HidClient?logo=coveralls)](https://coveralls.io/github/Aldaviva/HidClient?branch=master)

*Common library class to receive updates from a USB HID and reconnect automatically when disconnected*

![USB-A plug](https://raw.githubusercontent.com/Aldaviva/HidClient/master/.github/images/readme-header.jpg)

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3" bullets="1.,-,-,-" -->

1. [Introduction](#introduction)
1. [Prerequisites](#prerequisites)
1. [Installation](#installation)
1. [Usage](#usage)
1. [Testing](#testing)

<!-- /MarkdownTOC -->

## Introduction

This library provides `AbstractHidClient`, a class that layers on top of [HidSharp](https://www.nuget.org/packages/HidSharp/) and provides the following useful abstractions.

- Automatically connect to a device with the given Vendor ID and Product ID
- If the device is not physically connected yet, wait for it to be available and use it automatically as soon as it's ready
- If the device disconnects, wait for it and automatically reconnect when it's available again
- Properties and events that let you observe the connection state
- Automatically run a message pump thread to receive data from the device

This common logic was extracted from [Aldaviva/PowerMate](https://github.com/Aldaviva/PowerMate) so it could be reused in [Aldaviva/WebScale.Net](https://github.com/Aldaviva/WebScale.Net). It is intended to help developers write device-specific HID libraries without duplicating boilerplate connection management code in each project.

## Prerequisites

- Any .NET runtime that supports [.NET Standard 2.0 or later](https://docs.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0#net-standard-versions):
    - [.NET 5.0 or later](https://dotnet.microsoft.com/en-us/download/dotnet)
    - [.NET Core 2.0 or later](https://dotnet.microsoft.com/en-us/download/dotnet)
    - [.NET Framework 4.6.1 or later](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- Operating systems:
    - Windows
    - MacOS
    - Linux (although HidSharp seems to be unable to detect most devices on Linux)

## Installation

```ps1
dotnet add package HidClient
```

## Usage

1. Create a subclass of `AbstractHidClient` and stub out the mandatory overrides.
    ```cs
    public class WebScale: AbstractHidClient {

        public WebScale() { }
        public WebScale(DeviceList deviceList): base(deviceList) { }

        protected override int VendorId { get; }
        protected override int ProductId { get; }

        protected override void OnHidRead(byte[] readBuffer) { }

    }
    ```
1. Override the `VendorId` and `ProductId` properties to return the VID and PID of your device.
    - In Windows, these can be found in Device Manager as the hexadecimal `VID` and `PID` values under Hardware Ids.
    - In Linux, these can be found in the output of `lsusb` as the hexadecimal `ID` colon-delimited value.
    ```cs
    protected override int VendorId { get; } = 0x2474;
    protected override int ProductId { get; } = 0x0550;
    ```
1. If you need to run initialization logic each time the device connects, for example resetting LED brightness that the device doesn't persist on its own, you may optionally override the `OnConnect()` method.
    ```cs
    protected override void OnConnect() {
        DeviceStream?.SetFeature(new byte[]{ 0x00, 0x41, 0x01, 0x01, 0x00, 0x50, 0x00, 0x00, 0x00 });
    }
    ```
1. Override the `OnHidRead(byte[])` method to handle the bytes read from the device.
    ```cs
    protected override void OnHidRead(byte[] readBuffer) {
        double ounces = BitConverter.ToInt16(readBuffer, 4) / 10.0;
        Weight = Force.FromOunceForce(ounces);
    }
    ```
1. To send commands to the device, call `DeviceStream.Write(byte[])` or `DeviceStream.WriteAsync(byte[], int, int)`.
    ```cs
    public void Tare() {
        DeviceStream?.Write(new byte[]{ 0x04, 0x01 });
    }
    ```

## Testing

See [Aldaviva/PowerMate](https://github.com/Aldaviva/PowerMate/tree/master/Tests) and [Aldaviva/WebScale.Net](https://github.com/Aldaviva/WebScale.Net) for examples of unit testing HidClient. These test suites mock HidSharp using [FakeItEasy](https://fakeiteasy.github.io) so they don't need real devices connected to the build machines.