using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PboTools
{
    public enum ConfigPropertyType { Int, Float, String, Array }

    public class ConfigFile
    {
        ReadOnlyDictionary<string, int> enums;
        public ConfigFile(ConfigClass root, IDictionary<string, int> enums = null)
        {
            this.enums = new ReadOnlyDictionary<string, int>(enums);
            Root = root;
        }

        public ConfigClass Root { get; private set; }
        public IReadOnlyDictionary<string, int> Enums { get => this.enums; }

    }

    public class ConfigClass
    {
        public ConfigClass(string name,
                           string baseClass,
                           ICollection<ConfigProperty> properties,
                           ICollection<ConfigClass> subclasses = null,
                           ICollection<string> deletes = null
            )
        {
            Name = name;
            BaseClass = baseClass;
            this.properties = properties?.ToList() ?? new List<ConfigProperty>();
            this.subclasses = subclasses?.ToList() ?? new List<ConfigClass>();
            this.deletes = deletes?.ToList() ?? new List<string>();
        }
        public ConfigClass(string name) : this(name, null) { IsEmpty = true; }

        internal ConfigClass(string name, string baseClass) : this(name, baseClass, null) { }

        public bool IsEmpty { get; internal set; }
        public string Name { get; private set; }
        public string BaseClass { get; private set; }
        public IReadOnlyCollection<ConfigProperty> Properties { get => properties.AsReadOnly(); }
        public IReadOnlyCollection<ConfigClass> Subclasses { get => subclasses.AsReadOnly(); }
        public IReadOnlyCollection<string> Deletes { get => deletes.AsReadOnly(); }


        private List<ConfigProperty> properties;
        private List<ConfigClass> subclasses;
        private List<string> externalClasses;
        private List<string> deletes;

        internal void AddSubclass(ConfigClass c)
        {
            subclasses.Add(c);
        }
        internal void AddExternal(string name)
        {
            externalClasses.Add(name);
        }
        internal void AddDelete(string del)
        {
            deletes.Add(del);
        }
        internal void AddProperty(ConfigProperty prop)
        {
            properties.Add(prop);
        }
    }

    public class ConfigProperty
    {
        public ConfigProperty(string name, string value) : this(name, value, ConfigPropertyType.String) { }
        public ConfigProperty(string name, int value) : this(name, value, ConfigPropertyType.Int) { }
        public ConfigProperty(string name, float value) : this(name, value, ConfigPropertyType.Float) { }
        public ConfigProperty(string name, Array value) : this(name, value, ConfigPropertyType.Array) { }
        internal ConfigProperty(string name, object value, ConfigPropertyType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
        public string Name { get; private set; }
        public object Value { get; private set; }
        public ConfigPropertyType Type { get; private set; }

    }
}
