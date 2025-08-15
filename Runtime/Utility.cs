using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Services
{
    internal static class Utility
    {
        internal static async Task<T> GetSettings<T>() where T : ServiceSettings
        {
            var settingsType = typeof(T);
            var attribute = settingsType.GetCustomAttribute<ServiceSettingsAttribute>();
            if (attribute == null)
            {
                throw new Exception(
                    $"{settingsType.Name} is missing attribute of type {nameof(ServiceSettingsAttribute)}");
            }
            
            // Check that an asset with 'key' already exists
            string key = attribute.Key;
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            await locationsHandle.Task;

            T settings = null;

            // If it does, load it - otherwise create a new one
            if (locationsHandle is
                {
                    Status: UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded,
                    Result: {Count: > 0}
                })
            {
                var assetHandle = Addressables.LoadAssetAsync<T>(key);
                await assetHandle.Task;

                if (assetHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    settings = assetHandle.Result;
                
                Addressables.Release(assetHandle);
            }
            else
            {
                settings = ScriptableObject.CreateInstance<T>();

#if UNITY_EDITOR
                CreateDirectory(attribute.FilePath);
                string assetPath = attribute.FilePath.TrimEnd('/') + "/" + attribute.Key + ".asset";
                
                // Create the asset if missing
                if (!System.IO.File.Exists(assetPath))
                {
                    AssetDatabase.CreateAsset(settings, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                
                // Settings
                var addressablesSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);

                // Ensure GeneratedAssets group exists
                const string targetGroupName = "GeneratedAssets";
                var group = addressablesSettings.FindGroup(targetGroupName);
                if (group == null)
                {
                    group = addressablesSettings.CreateGroup(
                        targetGroupName,
                        setAsDefaultGroup: false,
                        readOnly: false,
                        postEvent: false,
                        null,
                        typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                        typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema)
                    );

                    // Mark as local-only
                    var bundledSchema =
                        group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                    
                    if (bundledSchema != null)
                    {
                        bundledSchema.BuildPath.SetVariableByName(addressablesSettings, "LocalBuildPath");
                        bundledSchema.LoadPath.SetVariableByName(addressablesSettings, "LocalLoadPath");
                    }
                }

                // Get asset GUID
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(guid))
                {
                    var entry = addressablesSettings.FindAssetEntry(guid);
                    if (entry == null)
                    {
                        entry = addressablesSettings.CreateOrMoveEntry(guid, group);
                        entry.SetAddress(attribute.Key);
                        EditorUtility.SetDirty(addressablesSettings);
                        AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        if (entry.parentGroup != group)
                            addressablesSettings.MoveEntry(entry, group);

                        if (entry.address != attribute.Key)
                        {
                            entry.SetAddress(attribute.Key);
                            EditorUtility.SetDirty(addressablesSettings);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
#endif
            }

            Addressables.Release(locationsHandle);
            return settings;
        }

#if UNITY_EDITOR
        private static void CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            // Normalize separators and trim whitespace/slashes
            string normalized = path.Replace('\\', '/').Trim();

            // If the path starts with 'Assets', remove it
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal))
                normalized = normalized["Assets/".Length..];
            else if (string.Equals(normalized, "Assets", StringComparison.Ordinal))
            {
                AssetDatabase.Refresh();
                return;
            }

            // Split path into individual folders
            string[] parts = normalized.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // Iterate, creating each folder if it doesn't exist
            string current = "Assets";
            foreach (string part in parts)
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);

                current = next;
            }

            // Refresh the AssetDatabase
            AssetDatabase.Refresh();
        }
#endif
    }
}