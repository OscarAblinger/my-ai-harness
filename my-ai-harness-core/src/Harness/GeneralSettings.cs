using System;
using System.IO;
using Ablinger.MyAiHarness.Core.Plugins.Interfaces;

namespace Ablinger.MyAiHarness.Core.Harness;

public class GeneralSettings : ISettings.SimpleAttributeBasedSettings
{
    [Entry]
    public string GlobalPath { get; } = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)!;
}