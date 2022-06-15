using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTests.Configuration;
using IntegrationTests.Configuration.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationTests
{
	// ReSharper disable All
	[TestClass]
	[SuppressMessage("Style", "IDE0063:Use simple 'using' statement")]
	public class Tests
	{
		#region Fields

		private const string _appSettingsFileName = "appsettings.json";
		private static readonly string _projectDirectoryPath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
		private static readonly string _resourcesDirectoryPath = Path.Combine(_projectDirectoryPath, "Resources");
		private static readonly string _testDirectoryPath = Path.Combine(_projectDirectoryPath, "Test-directory");

		#endregion

		#region Methods

		[TestMethod]
		public async Task AddOptions_Test()
		{
			var configuration = await this.CreateConfiguration(_resourcesDirectoryPath);
			var services = await this.CreateServices(configuration);
			services.AddOptions<Options1>();

			await using(var serviceProvider = services.BuildServiceProvider())
			{
				var options1 = serviceProvider.GetService<Options1>();
				Assert.IsNull(options1);

				var options1Options = serviceProvider.GetService<IOptions<Options1>>();
				Assert.IsNotNull(options1Options);

				var options1OptionsMonitor = serviceProvider.GetService<IOptionsMonitor<Options1>>();
				Assert.IsNotNull(options1OptionsMonitor);
			}

			configuration = await this.CreateConfiguration(_resourcesDirectoryPath);
			services = await this.CreateServices(configuration);
			services.AddOptions<Options1>().Bind(configuration.GetSection(nameof(Options1)));

			await using(var serviceProvider = services.BuildServiceProvider())
			{
				var options1 = serviceProvider.GetService<Options1>();
				Assert.IsNull(options1);

				var options1Options = serviceProvider.GetService<IOptions<Options1>>();
				Assert.IsNotNull(options1Options);

				var options1OptionsMonitor = serviceProvider.GetService<IOptionsMonitor<Options1>>();
				Assert.IsNotNull(options1OptionsMonitor);
			}
		}

		[TestMethod]
		public async Task Change_EmptyOptionsMonitorOnConfigurationRoot_Triggers()
		{
			EmptyOptions onChangeOptions = null;
			EmptyOptions onNamedChangeOptions = null;
			string onNamedChangeName = null;

			var temporaryTestDirectory = await this.CreateTemporaryTestDirectory(_appSettingsFileName);

			try
			{
				var configuration = await this.CreateConfiguration(temporaryTestDirectory);
				var services = await this.CreateServices(configuration);

				services.Configure<EmptyOptions>(configuration);

				await using(var serviceProvider = services.BuildServiceProvider())
				{
					var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EmptyOptions>>();

					var onChange = optionsMonitor.OnChange((options) => { onChangeOptions = options; });

					var onNamedChange = optionsMonitor.OnChange((options, name) =>
					{
						onNamedChangeOptions = options;
						onNamedChangeName = name;
					});

					Assert.IsNull(onChangeOptions);
					Assert.IsNull(onNamedChangeOptions);
					Assert.IsNull(onNamedChangeName);

					// Do the change.
					File.Copy(Path.Combine(_resourcesDirectoryPath, _appSettingsFileName), Path.Combine(temporaryTestDirectory, _appSettingsFileName), true);

					// Wait for the change to complete.
					Thread.Sleep(500);

					onChange.Dispose();
					onNamedChange.Dispose();

					Assert.IsNotNull(onChangeOptions);
					Assert.IsNotNull(onNamedChangeOptions);
					Assert.AreEqual(string.Empty, onNamedChangeName);
				}
			}
			finally
			{
				if(Directory.Exists(temporaryTestDirectory))
					Directory.Delete(temporaryTestDirectory, true);
			}
		}

		[TestMethod]
		public async Task Change_IfNoOptionsActuallyAreChanged_ShouldTriggerAll()
		{
			var expectedTriggeredChanges = new List<TriggeredChange>
			{
				this.CreateTriggeredChange(false, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, "Test", "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, string.Empty, "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, "Test", "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, string.Empty, "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue)
			};

			await this.ChangeTest(_appSettingsFileName, expectedTriggeredChanges);
		}

		[TestMethod]
		public async Task Change_IfOptions1AndOptions2AreChanged_ShouldTriggerAll()
		{
			var expectedTriggeredChanges = new List<TriggeredChange>
			{
				this.CreateTriggeredChange(true, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, "Test", "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, string.Empty, "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, "Test", "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, string.Empty, "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue)
			};

			await this.ChangeTest("appsettings.ChangedOptions1AndOptions2.json", expectedTriggeredChanges);
		}

		[TestMethod]
		public async Task Change_IfOptions1IsChanged_ShouldTriggerAll()
		{
			var expectedTriggeredChanges = new List<TriggeredChange>
			{
				this.CreateTriggeredChange(false, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, "Test", "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, string.Empty, "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, "Test", "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, string.Empty, "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1 (changed)\"", DateTimeOffset.MinValue)
			};

			await this.ChangeTest("appsettings.ChangedOptions1.json", expectedTriggeredChanges);
		}

		[TestMethod]
		public async Task Change_IfOptions2IsChanged_ShouldTriggerAll()
		{
			var expectedTriggeredChanges = new List<TriggeredChange>
			{
				this.CreateTriggeredChange(true, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, "Test", "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, null, "onChange2", "options2Monitor.OnChange((options2) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(true, string.Empty, "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", "Options2: Value1 = \"Value-1 (changed)\", Value2 = \"Value-2\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, "Test", "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, null, "onChange1", "options1Monitor.OnChange((options1) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue),
				this.CreateTriggeredChange(false, string.Empty, "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", "Options1: Value1 = \"Value-1\"", DateTimeOffset.MinValue)
			};

			await this.ChangeTest("appsettings.ChangedOptions2.json", expectedTriggeredChanges);
		}

		[TestMethod]
		public async Task Change_ObjectOptionsMonitorOnConfigurationRoot_DoNotTrigger()
		{
			object onChangeObject = null;
			object onNamedChangeObject = null;
			string onNamedChangeName = null;

			var temporaryTestDirectory = await this.CreateTemporaryTestDirectory(_appSettingsFileName);

			try
			{
				var configuration = await this.CreateConfiguration(temporaryTestDirectory);
				var services = await this.CreateServices(configuration);

				services.Configure<object>(configuration);

				await using(var serviceProvider = services.BuildServiceProvider())
				{
					var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EmptyOptions>>();

					var onChange = optionsMonitor.OnChange((options) => { onChangeObject = options; });

					var onNamedChange = optionsMonitor.OnChange((options, name) =>
					{
						onNamedChangeObject = options;
						onNamedChangeName = name;
					});

					Assert.IsNull(onChangeObject);
					Assert.IsNull(onNamedChangeObject);
					Assert.IsNull(onNamedChangeName);

					// Do the change.
					File.Copy(Path.Combine(_resourcesDirectoryPath, _appSettingsFileName), Path.Combine(temporaryTestDirectory, _appSettingsFileName), true);

					// Wait for the change to complete.
					Thread.Sleep(500);

					onChange.Dispose();
					onNamedChange.Dispose();

					Assert.IsNull(onChangeObject);
					Assert.IsNull(onNamedChangeObject);
					Assert.IsNull(onNamedChangeName);
				}
			}
			finally
			{
				if(Directory.Exists(temporaryTestDirectory))
					Directory.Delete(temporaryTestDirectory, true);
			}
		}

		protected internal async Task ChangeTest(string changedAppSettingsFileName, IList<TriggeredChange> expectedTriggeredChanges)
		{
			var temporaryTestDirectory = await this.CreateTemporaryTestDirectory(_appSettingsFileName);

			try
			{
				var configuration = await this.CreateConfiguration(temporaryTestDirectory);
				var services = await this.CreateServices(configuration);

				// Below will result in 8 triggers totally.
				services.Configure<Options1>(configuration.GetSection(nameof(Options1))); // Will trigger "onChange1" and "onNamedChange1".
				services.Configure<Options1>("Test", configuration.GetSection(nameof(Options1))); // Will trigger "onChange1" and "onNamedChange1".
				services.Configure<Options2>(configuration.GetSection(nameof(Options2))); // Will trigger "onChange2" and "onNamedChange2".
				services.Configure<Options2>("Test", configuration.GetSection(nameof(Options2))); // Will trigger "onChange2" and "onNamedChange2".

				var triggeredChanges = new List<TriggeredChange>();

				await using(var serviceProvider = services.BuildServiceProvider())
				{
					var initialOptions1 = serviceProvider.GetRequiredService<IOptions<Options1>>().Value;
					var options1Monitor = serviceProvider.GetRequiredService<IOptionsMonitor<Options1>>();
					var initialOptions2 = serviceProvider.GetRequiredService<IOptions<Options2>>().Value;
					var options2Monitor = serviceProvider.GetRequiredService<IOptionsMonitor<Options2>>();

					var onChange1 = options1Monitor.OnChange((options1) => { triggeredChanges.Add(this.CreateTriggeredChange(!initialOptions1.Equals(options1), null, "onChange1", "options1Monitor.OnChange((options1) =>", options1.ToString(), DateTimeOffset.Now)); });

					var onNamedChange1 = options1Monitor.OnChange((options1, name) => { triggeredChanges.Add(this.CreateTriggeredChange(!initialOptions1.Equals(options1), name, "onNamedChange1", "options1Monitor.OnChange((options1, name) =>", options1.ToString(), DateTimeOffset.Now)); });

					var onChange2 = options2Monitor.OnChange((options2) => { triggeredChanges.Add(this.CreateTriggeredChange(!initialOptions2.Equals(options2), null, "onChange2", "options2Monitor.OnChange((options2) =>", options2.ToString(), DateTimeOffset.Now)); });

					var onNamedChange2 = options2Monitor.OnChange((options2, name) => { triggeredChanges.Add(this.CreateTriggeredChange(!initialOptions2.Equals(options2), name, "onNamedChange2", "options2Monitor.OnChange((options2, name) =>", options2.ToString(), DateTimeOffset.Now)); });

					// Do the change.
					File.Copy(Path.Combine(_resourcesDirectoryPath, changedAppSettingsFileName), Path.Combine(temporaryTestDirectory, _appSettingsFileName), true);

					// Wait for the change to complete.
					Thread.Sleep(500);

					onChange1.Dispose();
					onNamedChange1.Dispose();
					onChange2.Dispose();
					onNamedChange2.Dispose();

					Assert.AreEqual(expectedTriggeredChanges.Count, triggeredChanges.Count);

					for(var i = 0; i < expectedTriggeredChanges.Count; i++)
					{
						var actualTriggeredChange = triggeredChanges[i];
						var expectedTriggeredChange = expectedTriggeredChanges[i];

						Assert.AreEqual(expectedTriggeredChange.Changed, actualTriggeredChange.Changed);
						Assert.AreEqual(expectedTriggeredChange.ConfigurationName, actualTriggeredChange.ConfigurationName);
						Assert.AreEqual(expectedTriggeredChange.Label, actualTriggeredChange.Label);
						Assert.AreEqual(expectedTriggeredChange.Listener, actualTriggeredChange.Listener);
						Assert.AreEqual(expectedTriggeredChange.OptionsAsString, actualTriggeredChange.OptionsAsString);
					}
				}
			}
			finally
			{
				if(Directory.Exists(temporaryTestDirectory))
					Directory.Delete(temporaryTestDirectory, true);
			}
		}

		protected internal virtual async Task<IConfiguration> CreateConfiguration(string directoryPath)
		{
			var configurationBuilder = new ConfigurationBuilder();
			configurationBuilder.SetBasePath(directoryPath);
			configurationBuilder.AddJsonFile(_appSettingsFileName, false, true);

			return await Task.FromResult(configurationBuilder.Build());
		}

		protected internal virtual async Task<IServiceCollection> CreateServices(IConfiguration configuration)
		{
			var services = new ServiceCollection();

			services.AddSingleton(configuration);

			return await Task.FromResult(services);
		}

		protected internal virtual async Task<string> CreateTemporaryTestDirectory(params string[] resourceFilesToCopy)
		{
			await Task.CompletedTask;

			var temporaryTestDirectoryPath = Path.Combine(_testDirectoryPath, $"{Guid.NewGuid()}");

			Directory.CreateDirectory(temporaryTestDirectoryPath);

			foreach(var resourceFile in resourceFilesToCopy ?? Array.Empty<string>())
			{
				File.Copy(Path.Combine(_resourcesDirectoryPath, resourceFile), Path.Combine(temporaryTestDirectoryPath, resourceFile));
			}

			return temporaryTestDirectoryPath;
		}

		protected internal virtual TriggeredChange CreateTriggeredChange(bool changed, string configurationName, string label, string listener, string optionsAsString, DateTimeOffset? timestamp)
		{
			return new TriggeredChange
			{
				Changed = changed,
				ConfigurationName = configurationName,
				Label = label,
				Listener = listener,
				OptionsAsString = optionsAsString,
				Timestamp = timestamp ?? DateTimeOffset.Now
			};
		}

		[TestMethod]
		public async Task ForeachConfiguredOptionsAnOnChangeTriggerWillBeTriggered()
		{
			await this.NumberOfTriggeredChangesTest(1, 1);
			await this.NumberOfTriggeredChangesTest(2, 2);
			await this.NumberOfTriggeredChangesTest(4, 4);
		}

		protected internal virtual async Task NumberOfTriggeredChangesTest(int numberOfTimesToConfigureOptions, int expectedNumberOfTriggeredChanges)
		{
			var numberOfTriggeredChanges = 0;
			var temporaryTestDirectory = await this.CreateTemporaryTestDirectory(_appSettingsFileName);

			try
			{
				var configuration = await this.CreateConfiguration(temporaryTestDirectory);
				var services = await this.CreateServices(configuration);

				for(var i = 0; i < numberOfTimesToConfigureOptions; i++)
				{
					services.Configure<EmptyOptions>(configuration);
				}

				await using(var serviceProvider = services.BuildServiceProvider())
				{
					var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EmptyOptions>>();

					var onChange = optionsMonitor.OnChange((options) => { numberOfTriggeredChanges++; });

					Assert.AreEqual(0, numberOfTriggeredChanges);

					// Do the change.
					File.Copy(Path.Combine(_resourcesDirectoryPath, _appSettingsFileName), Path.Combine(temporaryTestDirectory, _appSettingsFileName), true);

					// Wait for the change to complete.
					Thread.Sleep(500);

					onChange.Dispose();

					Assert.AreEqual(expectedNumberOfTriggeredChanges, numberOfTriggeredChanges);
				}
			}
			finally
			{
				if(Directory.Exists(temporaryTestDirectory))
					Directory.Delete(temporaryTestDirectory, true);
			}
		}

		#endregion
	}
	// ReSharper restore All
}