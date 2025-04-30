# Changelog
Notable changes to this solution are documented in this file using the 
[Keep a Changelog] style. The dates specified are in coordinated universal time (UTC).

[1.0.9251]: https://github.com/ATECoder/dn.iot.tcp

## [1.0.9251] - 2025-03-29
- Serilog Settings
  - Set log level to warning.
- Tests
  - Remove Console.WriteLine( $"@{methodFullName}" );
  - Replace with $"{methodFullName} initializing" );
  - Reduce reporting of test class initialization.
  - Output the name of the assembly under test.
- Fix incorrect new line escape character.

## [1.0.8626] - 2023-08-14 Preview 202304
* Add Ieee488 client and unit tests. 

## [1.0.8551] - 2023-05-31 Preview 202304
* Use cc.isr.Json.AppSettings.ViewModels project for settings I/O.

## [0.1.8518] - 2023-04-28 Preview 202304
* Split README.MD to attribution, cloning, open-source and read me files.
* Add code of conduct, contribution and security documents.
* Increment version.

## [0.1.8360] - 2022-11-20
Fixes the asynchronous queries.

## [0.1.8359] - 2022-11-19
Add MAUI concept and console applications. Async query fails on the MAUI application.

## [0.1.8358] - 2022-11-18
* initial commit.

&copy;  2022 Integrated Scientific Resources, Inc. All rights reserved.

[Keep a Changelog]: https://keepachangelog.com/en/1.0.0/
