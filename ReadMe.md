# OptionsMonitor-OnChange-Lab

Lets say you have the following appsettings.json:

	{
		"Options1": {
			"Value1": "Value-1"
		},
		"Options2": {
			"Value1": "Value-1",
			"Value2": "Value-2"
		}
	}

and any of the following registrations:

	services.Configure<Options1>(configuration.GetSection(nameof(Options1)));

	services.Configure<Options1>("Test", configuration.GetSection(nameof(Options1)));

	services.Configure<Options2>(configuration.GetSection(nameof(Options2)));

	services.Configure<Options2>("Test", configuration.GetSection(nameof(Options2)));

	services.Configure<EmptyOptions>(configuration);

A change, or even an overwrite with the same file, will trigger IOptionsMonitor.OnChange for all the above registrations. So a change will be triggered even if the options-instance have not changed.

To be sure of this I have some tests:

- [Tests](/Tests/Integration-tests/Tests.cs)

## Links

- [OptionsMonitor OnChange is triggered even though its TOptions did not change](https://github.com/dotnet/extensions/issues/2671)
- [OptionsMonitor OnChange is triggered even though the last ConfigurationProvider has not changed](https://github.com/dotnet/extensions/issues/1862)