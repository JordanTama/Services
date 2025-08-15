using System;

namespace Services
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceSettingsAttribute : Attribute
    {
        public readonly string Key;
        public readonly string FilePath;

        public ServiceSettingsAttribute(string key, string filePath)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException($"'{key}' is an invalid key.");
            
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException($"'{filePath}' is an invalid path.");
            
            Key = key;
            FilePath = filePath;
        }
    }
}