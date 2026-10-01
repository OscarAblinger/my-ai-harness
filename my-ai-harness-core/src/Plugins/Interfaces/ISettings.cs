using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ablinger.MyAiHarness.Core.Plugins.Interfaces;

public interface ISettings
{
    List<SettingsEntry> GetEntries();

    public record struct SettingsEntry(
        bool Active,
        string Id,
        SettingsType Type,
        string Name,
        string? Description,
        Func<object?> GetValue,
        Action<object?> SetValue);

    public enum SettingsType
    {
        Bool,
        String,

        /// <summary>
        /// WARNING: The secret is still stored in plain text in the settings .json file.
        /// It will, however, block it from being saved in project settings (unless the file is manually edited) so that
        /// it's not accidentally checked into a repository.
        /// </summary>
        Secret,
    }

    public abstract class SimpleAttributeBasedSettings : ISettings
    {
        protected SimpleAttributeBasedSettings()
        {
            entries = (from member in GetType().GetFields().Concat<MemberInfo>(GetType().GetProperties())
                let entry = member.GetCustomAttribute<Entry>()
                where entry != null
                select ToSettingsEntry(member, entry)).ToList();
        }

        protected SettingsEntry ToSettingsEntry(MemberInfo member, Entry entry)
        {
            return new SettingsEntry(
                entry.Active,
                GetId(member, entry),
                entry.Type,
                entry.Name ?? FieldNameToHumanName(member.Name),
                entry.Description,
                () => member switch
                {
                    FieldInfo fi => fi.GetValue(this),
                    PropertyInfo pi => pi.GetValue(this),
                    _ => throw new ArgumentException(
                        "Argument is of unsupported Type. " +
                        $"Can only happen if you extend {typeof(SimpleAttributeBasedSettings)} with entries beyond " +
                        "fields & properties, but don't overwrite ToSettingsEntry.")
                },
                (newValue) =>
                {
                    switch (member)
                    {
                        case FieldInfo fi:
                            fi.SetValue(this, newValue);
                            break;
                        case PropertyInfo pi:
                            pi.SetValue(this, newValue);
                            break;
                        default:
                            throw new ArgumentException(
                                "Argument is of unsupported Type. " +
                                $"Can only happen if you extend {typeof(SimpleAttributeBasedSettings)} with entries beyond " +
                                "fields & properties, but don't overwrite ToSettingsEntry.");
                    }
                });
        }

        private static string GetId(MemberInfo memberInfo, Entry entry)
        {
            return entry.Id ?? memberInfo.Name;
        }

        protected string FieldNameToHumanName(string fieldName)
        {
            var result = new StringBuilder();
            for (int i = 0; i < fieldName.Length; i++)
            {
                if (i > 0 && char.IsUpper(fieldName[i]) &&
                    (char.IsLower(fieldName[i - 1]) ||
                     (i + 1 < fieldName.Length && char.IsLower(fieldName[i + 1]))))
                {
                    result.Append(' ');
                }

                result.Append(fieldName[i]);
            }

            return result.ToString();
        }

        private readonly List<SettingsEntry> entries;

        public List<SettingsEntry> GetEntries()
        {
            return entries;
        }

        public sealed class Entry(
            bool active = true,
            string? id = null,
            SettingsType type = SettingsType.String,
            string? name = null,
            string? description = null) : Attribute
        {
            public readonly bool Active = active;
            public readonly string? Id = id;
            public readonly SettingsType Type = type;
            public readonly string? Name = name;
            public readonly string? Description = description;
        }
    }
}