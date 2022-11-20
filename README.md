# IOT Tcp

TCP Socket-based control and communication library for LXI-based instruments. 

- [Description](#Description)
- [Status](#Status)
- [Issues](#Issues)
- [Supported .Net Releases](#Supported-.Net-Releases)
- [Source Code](#Source-Code)
  - [Repositories](#Repositories)
  - [Global Configuration Files](#Global-Configuration-Files)
  - [Packages](#Packages)
- [Facilitated By](#FacilitatedBy)
- [Repository Owner](#Repository-Owner)
- [Authors](#Authors)
- [Acknowledgments](#Acknowledgments)
- [Open Source](#Open-Source)
- [Closed Software](#Closed-software)
- [Legal Notices](#Legal-Notices)

## Description

The ISR IOT TCP classes provide rudimentary methods for communicating with LXI instruments in mobile and desktop platforms.

Unlike VXI-11 or HiSlip, these classes do not implement the bus level method for issuing device clear, reading service requests or responding to instrument initiated event. While  control ports for these methods are available in some Keysight instruments, these ports are not part of the standard LXI framework.

## Status

isr.VI.Lite encountered delay issues. Code received from Keithley, which uses the same TCP client API encountered no such issues when using the synchronous API of the TCP client.

Taking advantage of the Keithley code, I aim at separating the instrument-specific (e.g., DMM) functions from then TCP client API. Thereafter, I will try to restore the Asynchronous calls aiming to identify the source of the delay issues.

To this end, I will use the user interface of the DMM7510 digitizer but with a single instrument.
 
## Issues

### Async query fails on the Maui concept application
the loop for checking for data availability does not terminate. If awaited with time out, the loop seems to continue attempting to read from a disposed socket.

## Supported .NET Releases

* .NET Standard 2.0 - source code framework)
* .NET 6.0
* .NET 7.0
* .NET MAUI - not tested yet.

<a name="Source-Code"></a>
## Source Code
Clone the repository along with its requisite repositories to their respective relative path.

### Repositories
The repositories listed in [external repositories] are required:
* [IDE Repository] - IDE support files.

```
git clone git@bitbucket.org:davidhary/vs.ide.git
git clone https://github.com/ATECoder/dn.iot.tcp.git
```

Clone the repositories into the following folders (parents of the .git folder):
```
%vslib%\core\ide
%dnlib%\vi\tcp
```
where %dnlib% and %vslib% are  the root folders of the .NET libraries, e.g., %my%\lib\vs 
and %my%\libraries\vs, respectively, and %my% is the root folder of the .NET solutions

### Global Configuration Files
ISR libraries use a global editor configuration file and a global test run settings file. 
These files can be found in the [IDE Repository].

Restoring Editor Configuration:
```
xcopy /Y %my%\.editorconfig %my%\.editorconfig.bak
xcopy /Y %vslib%\core\ide\code\.editorconfig %my%\.editorconfig
```

Restoring Run Settings:
```
xcopy /Y %userprofile%\.runsettings %userprofile%\.runsettings.bak
xcopy /Y %vslib%\core\ide\code\.runsettings %userprofile%\.runsettings
```
where %userprofile% is the root user folder.

### Packages
TBA

<a name="FacilitatedBy"></a>
## Facilitated By
* [Visual Studio]
* [Atomineer Code Documentation]
* [EW Software Spell Checker]
* [Funduc Search and Replace]
* [IVI Foundation] - IVI Foundation VISA

## Repository Owner
* [ATE Coder]

<a name="Authors"></a>
## Authors
* [ATE Coder]  
* [Josh Brown]

<a name="Acknowledgments"></a>
## Acknowledgments
* [Its all a remix] -- we are but a spec on the shoulders of giants  
* [John Simmons] - outlaw programmer  
* [Stack overflow] - Joel Spolsky  
* [.Net Foundation] - The .NET Foundation

<a name="Open-Source"></a>
## Open source
Open source used by this software is described and licensed at the following sites:  

<a name="Closed-software"></a>
## Closed software
Closed software used by this software are described and licensed on the following sites:  
[IVI Foundation]  

<a name="Resources"></a>
## Resources 

[Use sockets to send and receive data over TCP]
[Use sockets to send and receive data over TCP]  
[DMM7510 Digitizer Control Tool]  
[TCP Server]  
[Stopping a TCP Server]  

<a name="Legal-Notices"></a>
## Legal Notices

Integrated Scientific Resources, Inc., and any contributors grant you a license to the documentation and other content
in this repository under the [Creative Commons Attribution 4.0 International Public License](https://creativecommons.org/licenses/by/4.0/legalcode), see the [LICENSE](LICENSE) file, and grant you a license to any code in the repository under the [MIT License](https://opensource.org/licenses/MIT), see the [LICENSE-CODE](LICENSE-CODE) file.

Integrated Scientific Resources, Inc., and/or other Integrated Scientific Resources, Inc., products and services referenced in the documentation may be either trademarks or registered trademarks of Integrated Scientific Resources, Inc., in the United States and/or other countries. The licenses for this project do not grant you rights to use any Integrated Scientific Resources, Inc., names, logos, or trademarks.

Integrated Scientific Resources, Inc., and any contributors reserve all other rights, whether under their respective copyrights, patents, or trademarks, whether by implication, estoppel or otherwise.

[IVI Foundation]: https://www.ivifoundation.org

[Microsoft .NET Framework]: https://dotnet.microsoft.com/download

[external repositories]: ExternalReposCommits.csv

[IDE Repository]: https://www.bitbucket.org/davidhary/vs.ide

[ATE Coder]: https://www.IntegratedScientificResources.com

[Its all a remix]: https://www.everythingisaremix.info

[John Simmons]: https://www.codeproject.com/script/Membership/View.aspx?mid=7741

[Stack overflow]: https://www.stackoveflow.com

[Visual Studio]: https://www.visualstudio.com/

[Atomineer Code Documentation]: https://www.atomineerutils.com/

[EW Software Spell Checker]: https://github.com/EWSoftware/VSSpellChecker/wiki/

[Funduc Search and Replace]: http://www.funduc.com/search_replace.htm

[.Net Foundation]: https://source.dot.net

[Use sockets to send and receive data over TCP]: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services

[DMM7510 Digitizer Control Tool]: https://github.com/jbrown1234/DMM7510_Digitizer_Control_Tool/

[Josh Brown]: https://github.com/jbrown1234/

[TCP Server]: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?redirectedfrom=MSDN&view=net-7.0

[Stopping a TCP Server]: https://stackoverflow.com/questions/1173774/stopping-a-tcplistener-after-calling-beginaccepttcpclient#:~:text=You%20should%20be%20able%20to%20check%20this%20by,EndAcceptTcpClient%20%28%29%20call.%20You%20should%20see%20the%20ObjectDisposedException.
