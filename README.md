### IOT Tcp

TCP Socket-based control and communication library for LXI-based instruments. 

* [Description](#Description)
* [Issues](#Issues)
* [Supported .Net Releases](#Supported-.Net-Releases)
* [Runtime Pre-Requisites](#Runtime-Pre-Requisites)
* [Known Issues](#Known-Issues)
* Project README files:
  * [cc.isr.Iot.Tcp.Server](/src/libs/server/readme.md) 
* [Attributions](Attributions.md)
* [Change Log](./CHANGELOG.md)
* [Cloning](Cloning.md)
* [Code of Conduct](code_of_conduct.md)
* [Contributing](contributing.md)
* [Legal Notices](#legal-notices)
* [License](LICENSE)
* [Open Source](Open-Source.md)
* [Repository Owner](#Repository-Owner)
* [Security](security.md)

#### Description

The ISR IOT TCP classes provide rudimentary methods for communicating with LXI instruments in mobile and desktop platforms.

Unlike VXI-11 or HiSlip, these classes do not implement the bus level method for issuing device clear, reading service requests or responding to instrument initiated event. While  control ports for these methods are available in some Keysight instruments, these ports are not part of the standard LXI framework.

#### Issues

##### read after write delay is required  for Async methods
A delay of 1 ms is required for implementing the asynchronous query method using the TCP Client write and read asynchronous methods. Neither the console nor unit tests are succeptible to this issue. 

#### Supported .NET Releases

* .NET Standard 2.0 - source code framework)
* .NET 6.0
* .NET 7.0
* .NET MAUI
* Windows Forms
* WPF

#### Repository Owner
* [ATE Coder]

<a name="Authors"></a>
#### Authors
* [ATE Coder]  
* [Josh Brown]

<a name="legal-notices"></a>
#### Legal Notices

Integrated Scientific Resources, Inc., and any contributors grant you a license to the documentation and other content in this repository under the [Creative Commons Attribution 4.0 International Public License], see the [LICENSE](./LICENSE) file, and grant you a license to any code in the repository under the [MIT License], see the [LICENSE-CODE](./LICENSE-CODE) file.

Integrated Scientific Resources, Inc., and/or other Integrated Scientific Resources, Inc., products and services referenced in the documentation may be either trademarks or registered trademarks of Integrated Scientific Resources, Inc., in the United States and/or other countries. The licenses for this project do not grant you rights to use any Integrated Scientific Resources, Inc., names, logos, or trademarks.

Integrated Scientific Resources, Inc., and any contributors reserve all other rights, whether under their respective copyrights, patents, or trademarks, whether by implication, estoppel or otherwise.

[Creative Commons Attribution 4.0 International Public License]:(https://creativecommons.org/licenses/by/4.0/legalcode)
[MIT License]:(https://opensource.org/licenses/MIT)
 
[ATE Coder]: https://www.IntegratedScientificResources.com
[dn.core]: https://www.bitbucket.org/davidhary/dn.core

[Josh Brown]: https://github.com/jbrown1234/
[ATE Coder]: https://www.IntegratedScientificResources.com
[Use sockets to send and receive data over TCP]: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services
[DMM7510 Digitizer Control Tool]: https://github.com/jbrown1234/DMM7510_Digitizer_Control_Tool/
[TCP Server]: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?redirectedfrom=MSDN&view=net-7.0
[Stopping a TCP Server]: https://stackoverflow.com/questions/1173774/stopping-a-tcplistener-after-calling-beginaccepttcpclient#:~:text=You%20should%20be%20able%20to%20check%20this%20by,EndAcceptTcpClient%20%28%29%20call.%20You%20should%20see%20the%20ObjectDisposedException.
