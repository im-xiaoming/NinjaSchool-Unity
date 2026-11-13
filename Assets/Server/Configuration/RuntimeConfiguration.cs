using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NsoServer.Data;

namespace NsoServer.Configuration
{
    /// <summary>
    /// Code-owned configuration. JSON overrides a captured legacy profile; runtime setters affect this session only.
    /// No Unity types, gameplay state, automatic file writes or changes to the existing balance/learning files.
    /// </summary>
    public static class RuntimeConfiguration
    {
        private static Dictionary<string, object> overrides = new Dictionary<string, object>();
        private static readonly Dictionary<string, Dictionary<string, object>> legacy = new Dictionary<string, Dictionary<string, object>>();
        private static readonly Dictionary<string, object> cache = new Dictionary<string, object>();
        public static string LastError { get; private set; } = "";

        public static void Load(string path)
        {
            overrides = new Dictionary<string, object>();
            legacy.Clear();
            cache.Clear();
            LastError = "";
            if (!File.Exists(path))
                return;
            try
            {
                var data = Json.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8)) as Dictionary<string, object>;
                if (data == null || !data.ContainsKey("version") || Json.Long(data["version"]) != 1)
                    throw new FormatException("Unsupported runtime configuration version");
                overrides = data;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        public static void Register(string section, string key, object value)
        {
            if (!legacy.TryGetValue(section, out var values))
                legacy[section] = values = new Dictionary<string, object>();
            values[key] = value;
            cache.Remove(section + "/" + key);
        }

        public static T Value<T>(string section, string key, T fallback)
        {
            string id = section + "/" + key;
            if (cache.TryGetValue(id, out var cached))
                return (T)cached;
            object baseline = fallback;
            if (legacy.TryGetValue(section, out var values) && values.TryGetValue(key, out var previous))
                baseline = previous;
            if (overrides.TryGetValue(section, out var group) && group is Dictionary<string, object> settings && settings.TryGetValue(key, out var replacement))
            {
                try
                {
                    baseline = ConvertValue(typeof(T), replacement, baseline);
                }
                catch (Exception ex)
                {
                    LastError = section + "." + key + ": " + ex.Message;
                }
            }

            cache[id] = baseline;
            return (T)baseline;
        }

        public static void Set<T>(string section, string key, T value)
        {
            Register(section, key, value);
            cache[section + "/" + key] = value;
        }

        private static object ConvertValue(Type type, object value, object baseline)
        {
            if (type == typeof(string))
                return value is string ? value : throw new FormatException("Expected string");
            if (type == typeof(bool))
                return value is bool ? value : throw new FormatException("Expected boolean");
            if (type.IsPrimitive || type == typeof(decimal))
            {
                if (!(value is long || value is double || value is int || value is float))
                    throw new FormatException("Expected number");
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            if (!(value is Dictionary<string, object> fields))
                throw new FormatException("Expected settings object");
            var result = Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object old = baseline == null ? field.GetValue(result) : field.GetValue(baseline);
                field.SetValue(result, fields.TryGetValue(field.Name, out var v) ? ConvertValue(field.FieldType, v, old) : old);
            }

            return result;
        }

        public static void ApplyStatic(string section, Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral || field.IsInitOnly || field.Name == "LlmApiKey")
                    continue;
                object value = field.GetValue(null);
                if (overrides.TryGetValue(section, out var group) && group is Dictionary<string, object> fields && fields.TryGetValue(field.Name, out var replacement))
                {
                    try
                    {
                        value = ConvertValue(field.FieldType, replacement, value);
                    }
                    catch (Exception ex)
                    {
                        LastError = section + "." + field.Name + ": " + ex.Message;
                    }
                }

                field.SetValue(null, value);
                if (field.Name.StartsWith("Brain", StringComparison.Ordinal) || overrides.TryGetValue(section, out var configured) && configured is Dictionary<string, object> explicitFields && explicitFields.ContainsKey(field.Name))
                    Register(section, field.Name, value);
            }
        }

        private static object JsonValue(object value)
        {
            if (value == null || value is string || value is bool || value.GetType().IsPrimitive || value is decimal)
                return value;
            if (value is IDictionary source)
            {
                var result = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in source)
                    result[Convert.ToString(entry.Key, CultureInfo.InvariantCulture)] = JsonValue(entry.Value);
                return result;
            }

            var fields = new Dictionary<string, object>();
            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                fields[field.Name] = JsonValue(field.GetValue(value));
            return fields;
        }

        public static void Export(string path)
        {
            if (File.Exists(path))
                throw new IOException("Will not overwrite an existing runtime configuration: " + path);
            var result = new Dictionary<string, object>
            {
                {
                    "version",
                    1
                }
            };
            foreach (var section in legacy)
            {
                var fields = new Dictionary<string, object>();
                foreach (var field in section.Value)
                    fields[field.Key] = JsonValue(cache.TryGetValue(section.Key + "/" + field.Key, out var v) ? v : field.Value);
                result[section.Key] = fields;
            }

            var writer = new JsonWriter();
            writer.Value(result);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            // CreateNew also protects against a file appearing after the existence check.
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var output = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                output.Write(writer.ToString());
        }
    }
}
