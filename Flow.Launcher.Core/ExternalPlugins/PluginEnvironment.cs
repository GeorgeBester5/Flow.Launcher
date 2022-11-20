﻿using Droplex;
using Flow.Launcher.Core.Plugin;
using Flow.Launcher.Infrastructure.Logger;
using Flow.Launcher.Infrastructure.UserSettings;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Flow.Launcher.Core.ExternalPlugins
{
    public class PluginEnvironment
    {
        private const string environments = "Environments";

        private const string python = "Python";

        private static string pythonEnvDirPath = Path.Combine(DataLocation.DataDirectory(), environments, python);

        private static string pythonDirPath = Path.Combine(pythonEnvDirPath, "PythonEmbeddable-v3.8.9");

        private string pythonFilePath = Path.Combine(pythonDirPath, "pythonw.exe");

        private const string nodejs = "Node.js";

        private static string nodeEnvDirPath = Path.Combine(DataLocation.DataDirectory(), environments, nodejs);

        private static string nodeDirPath = Path.Combine(nodeEnvDirPath, "Node-v16.18.0");

        private string nodeFilePath = Path.Combine(nodeDirPath, $"node-v16.18.0-win-x64\\node.exe");

        private List<PluginMetadata> pluginMetadataList;

        private PluginsSettings pluginSettings;

        internal PluginEnvironment(List<PluginMetadata> pluginMetadataList, PluginsSettings pluginSettings)
        {
            this.pluginMetadataList = pluginMetadataList;
            this.pluginSettings = pluginSettings;
        }
        //TODO: CHECK IF NEED TO RESET PATH AFTER FLOW UPDATE
        internal IEnumerable<PluginPair> PythonSetup()
        {
            return Setup(AllowedLanguage.Python, python);
        }

        internal IEnumerable<PluginPair> TypeScriptSetup()
        {
            return Setup(AllowedLanguage.TypeScript, nodejs);
        }

        internal IEnumerable<PluginPair> JavaScriptSetup()
        {
            return Setup(AllowedLanguage.JavaScript, nodejs);
        }

        private IEnumerable<PluginPair> Setup(string languageType, string environment)
        {
            if (!pluginMetadataList.Any(o => o.Language.Equals(languageType, StringComparison.OrdinalIgnoreCase)))
                return new List<PluginPair>();

            var envFilePath = string.Empty;

            switch (languageType)
            {
                case AllowedLanguage.Python:
                    if (!string.IsNullOrEmpty(pluginSettings.PythonFilePath) && FilesFolders.FileExists(pluginSettings.PythonFilePath))
                    {
                        EnsureLatestInstalled(
                            pythonFilePath,
                            pluginSettings.PythonFilePath,
                            pythonEnvDirPath,
                            languageType);

                        return SetPathForPluginPairs(pluginSettings.PythonFilePath, languageType);
                    }

                    break;

                case AllowedLanguage.TypeScript:
                case AllowedLanguage.JavaScript:
                    if (!string.IsNullOrEmpty(pluginSettings.NodeFilePath) && FilesFolders.FileExists(pluginSettings.NodeFilePath))
                    {
                        EnsureLatestInstalled(
                            nodeFilePath,
                            pluginSettings.NodeFilePath,
                            nodeEnvDirPath,
                            languageType);

                        return SetPathForPluginPairs(pluginSettings.NodeFilePath, languageType);
                    }

                    break;

                default:
                    break;
            }

            if (MessageBox.Show($"Flow detected you have installed {languageType} plugins, which " +
                                $"will require {environment} to run. Would you like to download {environment}? " +
                                Environment.NewLine + Environment.NewLine +
                                "Click no if it's already installed, " +
                                $"and you will be prompted to select the folder that contains the {environment} executable",
                    string.Empty, MessageBoxButtons.YesNo) == DialogResult.No)
            {
                var msg = $"Please select the {environment} executable";
                var selectedFile = string.Empty;

                switch (languageType)
                {
                    case AllowedLanguage.Python:
                        selectedFile = GetFileFromDialog(msg, "Python|pythonw.exe");

                        if (!string.IsNullOrEmpty(selectedFile))
                            pluginSettings.PythonFilePath = selectedFile;
                        break;

                    case AllowedLanguage.TypeScript:
                    case AllowedLanguage.JavaScript:
                        selectedFile = GetFileFromDialog(msg);

                        if (!string.IsNullOrEmpty(selectedFile))
                            pluginSettings.NodeFilePath = selectedFile;
                        break;

                    default:
                        break;
                }

                // Nothing selected because user pressed cancel from the file dialog window
                if (string.IsNullOrEmpty(selectedFile))
                    InstallEnvironment(languageType);
            }
            else
            {
                InstallEnvironment(languageType);
            }

            switch (languageType)
            {
                case AllowedLanguage.Python when FilesFolders.FileExists(pluginSettings.PythonFilePath):
                    return SetPathForPluginPairs(pluginSettings.PythonFilePath, languageType);

                case AllowedLanguage.TypeScript when FilesFolders.FileExists(pluginSettings.NodeFilePath):
                case AllowedLanguage.JavaScript when FilesFolders.FileExists(pluginSettings.NodeFilePath):
                    return SetPathForPluginPairs(pluginSettings.NodeFilePath, languageType);

                default:
                    MessageBox.Show(
                    "Unable to set Python executable path, please try from Flow's settings (scroll down to the bottom).");
                    Log.Error("PluginsLoader",
                        $"Not able to successfully set Python path, setting's PythonFilePath variable is still an empty string.",
                        "PluginEnvironment");

                    return new List<PluginPair>();
            }
        }

        private void InstallEnvironment(string languageType)
        {
            switch (languageType)
            {
                case AllowedLanguage.Python:
                    FilesFolders.RemoveFolderIfExists(pythonDirPath);

                    // Python 3.8.9 is used for Windows 7 compatibility
                    DroplexPackage.Drop(App.python_3_8_9_embeddable, pythonDirPath).Wait();

                    pluginSettings.PythonFilePath = pythonFilePath;
                    break;

                case AllowedLanguage.TypeScript:
                case AllowedLanguage.JavaScript:
                    FilesFolders.RemoveFolderIfExists(nodeDirPath);

                    DroplexPackage.Drop(App.nodejs_16_18_0, nodeDirPath).Wait();

                    pluginSettings.NodeFilePath = nodeFilePath;
                    break;

                default:
                    break;
            }
        }

        private void EnsureLatestInstalled(string expectedPath, string currentPath, string installedDirPath, string languagType)
        {
            if (expectedPath == currentPath)
                return;

            FilesFolders.RemoveFolderIfExists(installedDirPath);

            InstallEnvironment(languagType);

        }

        private IEnumerable<PluginPair> SetPathForPluginPairs(string filePath, string languageToSet)
        {
            var pluginPairs = new List<PluginPair>();

            foreach (var metadata in pluginMetadataList) {

                switch (languageToSet)
                {
                    case AllowedLanguage.Python when metadata.Language.Equals(languageToSet, StringComparison.OrdinalIgnoreCase):
                        pluginPairs.Add(new PluginPair
                        {
                            Plugin = new PythonPlugin(filePath),
                            Metadata = metadata
                        });
                        break;
                    
                    case AllowedLanguage.TypeScript when metadata.Language.Equals(languageToSet, StringComparison.OrdinalIgnoreCase):
                        pluginPairs.Add(new PluginPair
                        {
                            Plugin = new NodePlugin(filePath),
                            Metadata = metadata
                        });
                        break;

                    case AllowedLanguage.JavaScript when metadata.Language.Equals(languageToSet, StringComparison.OrdinalIgnoreCase):
                        pluginPairs.Add(new PluginPair
                        {
                            Plugin = new NodePlugin(filePath),
                            Metadata = metadata
                        });
                        break;

                    default:
                        break;
                }
            }

            return pluginPairs;
        }

        public static string GetFileFromDialog(string title, string filter="")
        {
            var dlg = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                Title = title,
                Filter = filter
            };

            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                return dlg.FileName;
            }
            else
            {
                return string.Empty;
            }
        }

    }
}
