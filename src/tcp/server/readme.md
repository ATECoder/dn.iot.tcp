### About

[cc.isr.Jason.AppSettings.ViewModels] is a .Net library for editing Jason-based configuration settings.

### How to Use

Examples for using this library are in the following projects:
* [cc.isr.Jason.AppSettings.ViewModels.MSTest]: tests storing and restoring of Json application settings using two settings classes. Includes tests classes for testing the model and the view model classes.
* [cc.isr.Json.AppSettings.WinForms]: includes Windows Forms user control and form for editing Json settings.
* [cc.isr.Json.AppSettings.WinForms.Demo]: demonstrates editing Json settings.

### Key Features

* Provides MVVM methods for editing Json-based configuration settings.
* Relay commands exceptions are set to propage to the [TaskScheduler.UnobservedTaskException]

### Main Types

The main types provided by this library are:

* _AppSettingsScribe_ read, writes and restores settings.
* _AssemblyFileInfo_ initializes the settings file and folder names.
* _ObjctExtensions_ implement deep cloning and binding of objects.
* _AppSettingsEditorViewModel_ a view model exposing the _AppSettingsScribe_ to user interfaces
* _IDialogService_ to receive confirmation fromthe user for writing over existing settings when restoring the settings from the application context. 

### Feedback

[cc.isr.Jason.AppSettings.ViewModels] is released as open source under the MIT license.
Bug reports and contributions are welcome at the [Json Repository].

[Json Repository]: https://bitbucket.org/davidhary/dn.json
[cc.isr.Jason.AppSettings.ViewModels]: https://bitbucket.org/davidhary/dn.json/main/src/app.settings/app.settings.view.models
[cc.isr.Jason.AppSettings.ViewModels.MSTest]: https://bitbucket.org/davidhary/dn.json/main/src/app.settings/app.settings.view.models.MSTest
[cc.isr.Json.AppSettings.WinForms]: https://bitbucket.org/davidhary/dn.json/main/src/app.settings.win.forms
[cc.isr.Json.AppSettings.WinForms.Demo]: https://bitbucket.org/davidhary/dn.json/main/src/app.settings.win.forms.demo
[TaskScheduler.UnobservedTaskException]: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler.unobservedtaskexception
