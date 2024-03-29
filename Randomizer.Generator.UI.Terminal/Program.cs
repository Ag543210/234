﻿using System;
using System.IO;
using Terminal.Gui;
using NStack;
using System.Collections.Generic;
using Randomizer.Generator.UI.Terminal.Utility;
using Randomizer.Generator.UI.Terminal.Models;
using Randomizer.Generator.UI.Terminal;
using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics;

namespace Randomizer.Generator.UI.Terminal
{
    class Program
    {
		private const String DEFAULT_DIRECTORY = @"%AppData%\Randomizer.Generator";
		private const String SETTINGS_DIRECTORY = @"%AppData%\Randomizer.Generator\settings.hjson";

		internal static String DefaultDirectory { get => Environment.ExpandEnvironmentVariables(DEFAULT_DIRECTORY); }
		internal static String SettingsDirectory { get => Environment.ExpandEnvironmentVariables(SETTINGS_DIRECTORY); }

		public static Action<String> CurrentDirectoryChanged;
		public static Action RefreshGeneratorList;

		private static StatusItem stsCurrentDirectory { get; set; }

		public static Toplevel TopLevelObject { get; private set; }
		public static MainWindow MainWindow { get; private set; }

		public static List<Tag> TagList { get; set; } = new();

		public static String CurrentDirectory
		{
			get => Directory.GetCurrentDirectory();
			set
			{
				value ??= Directory.GetCurrentDirectory();
				Directory.SetCurrentDirectory(value);
				stsCurrentDirectory.Title = CurrentDirectory;
				TagList = new();
				CurrentDirectoryChanged?.Invoke(value);
			}
		}

		static void Main(String settingsPath)
        {
			try
			{
				Application.Init();
				Directory.CreateDirectory(DefaultDirectory);
				Directory.SetCurrentDirectory(DefaultDirectory);

				Randomizer.Generator.DataAccess.DataAccess.Instance = new TUIDataAccess();

				TopLevelObject = Application.Top;
				MainWindow = new()
				{
					Title = AssemblyInfo.ProductName
				};

				if (!String.IsNullOrEmpty(settingsPath))
				{
					UserSettings.Instance.SettingPath = settingsPath;
				}
				UserSettings.Instance.Load();

				stsCurrentDirectory = new(Key.Null, ustring.Empty, null);
				CurrentDirectory = UserSettings.Instance.WorkingDirectory;
			
				TopLevelObject.Add(new StatusBar(new[] { stsCurrentDirectory }));
				TopLevelObject.Add(MainWindow);	
				Application.Run();
				if (UserSettings.Instance.RememberLastDirectory)
				{
					UserSettings.Instance.WorkingDirectory = CurrentDirectory;
					UserSettings.Instance.Save();
				}
			}
			catch (Exception ex)
			{
				var eventLog = new EventLog();
				eventLog.Source = "Application";
				eventLog.WriteEntry(ex.ToString(), EventLogEntryType.Error);
			}
		}

		public static void ChangeDirectory()
		{
			var dlg = new OpenDialog(String.Empty, "Choose a directory to view all of the generator definitions contained within.")
			{
				DirectoryPath = CurrentDirectory,
				CanChooseDirectories = true,
				CanChooseFiles = false
			};

			Application.Run(dlg);
			if (!dlg.Canceled)
				CurrentDirectory = dlg.FilePath.ToString();
		}
	}
}
