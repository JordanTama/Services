using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Services
{
    internal static class Utility
    {
        internal static async Task<(T settings, AsyncOperationHandle<T> handle)> GetSettings<T>() where T : ServiceSettings
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

            bool exists = locationsHandle is
            {
                Status: AsyncOperationStatus.Succeeded,
                Result: {Count: > 0}
            };
            Addressables.Release(locationsHandle);

            // If it does, load it - otherwise create a new one
            if (exists)
            {
                var assetHandle = Addressables.LoadAssetAsync<T>(key);
                await assetHandle.Task;

                if (assetHandle.Status == AsyncOperationStatus.Succeeded)
                    return (assetHandle.Result, assetHandle);
                
                Addressables.Release(assetHandle);
            }

#if UNITY_EDITOR
            CreateDirectory(attribute.FilePath);
            string assetPath = attribute.FilePath.TrimEnd('/') + "/" + attribute.Key + ".asset";
            
            // Create the asset if missing
            var settings = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<T>();
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

            return (settings, default);
#else
            return (ScriptableObject.CreateInstance<T>(), default);
#endif
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