using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Cinemachine;

namespace StarterAssets
{
    // This class needs to be a scriptable object to support dynamic determination of StarterAssets install path
    public partial class StarterAssetsDeployMenu : ScriptableObject
    {
        public const string MenuRoot = "Tools/Starter Assets";

        // prefab names
        private const string MainCameraPrefabName = "MainCamera";
        private const string PlayerCapsulePrefabName = "PlayerCapsule";

        // names in hierarchy
        private const string CinemachineVirtualCameraName = "PlayerFollowCamera";

        // tags
        private const string PlayerTag = "Player";
        private const string MainCameraTag = "MainCamera";
        private const string CinemachineTargetTag = "CinemachineTarget";

        private static GameObject _cinemachineVirtualCamera;

        private static void CheckCameras(Transform targetParent, string prefabFolder)
        {
            CheckMainCamera(prefabFolder);

            GameObject vcam = GameObject.Find(CinemachineVirtualCameraName);

            if (!vcam)
            {
                if (TryLocatePrefabByComponentNames(CinemachineVirtualCameraName, new string[] { prefabFolder }, new[] { "CinemachineVirtualCamera" }, out GameObject vcamPrefab, out string _))
                {
                    HandleInstantiatingPrefab(vcamPrefab, out vcam);
                    _cinemachineVirtualCamera = vcam;
                }
                else
                {
                    Debug.LogError("Couldn't find Cinemachine Virtual Camera prefab");
                }
            }
            else
            {
                _cinemachineVirtualCamera = vcam;
            }

            GameObject[] targets = GameObject.FindGameObjectsWithTag(CinemachineTargetTag);
            GameObject target = targets.FirstOrDefault(t => t.transform.IsChildOf(targetParent));
            if (target == null)
            {
                target = new GameObject("PlayerCameraRoot");
                target.transform.SetParent(targetParent);
                target.transform.localPosition = new Vector3(0f, 1.375f, 0f);
                target.tag = CinemachineTargetTag;
                Undo.RegisterCreatedObjectUndo(target, "Created new cinemachine target");
            }

            CheckVirtualCameraFollowReference(target, _cinemachineVirtualCamera);
        }

        private static void CheckMainCamera(string inFolder)
        {
            GameObject[] mainCameras = GameObject.FindGameObjectsWithTag(MainCameraTag);

            if (mainCameras.Length < 1)
            {
                // if there are no MainCameras, add one
                if (TryLocatePrefabByComponentNames(MainCameraPrefabName, new string[] { inFolder }, new[] { "CinemachineBrain", "Camera" }, out GameObject camera, out string _))
                {
                    HandleInstantiatingPrefab(camera, out _);
                }
                else
                {
                    Debug.LogError("Couldn't find Starter Assets Main Camera prefab");
                }
            }
            else
            {
                // make sure the found camera has a cinemachine brain (we only need 1)
                Type brainType = FindTypeInAssemblies("CinemachineBrain");
                if (brainType != null)
                {
                    var existing = mainCameras[0].GetComponent(brainType);
                    if (existing == null)
                        mainCameras[0].AddComponent(brainType);
                }
                else
                {
                    // Cinemachine not installed � skip adding, but don't error
                    Debug.Log("[StarterAssets] CinemachineBrain type not found; skipping add. Install Cinemachine package to enable runtime vcam support.");
                }
            }
        }

        private static void CheckVirtualCameraFollowReference(GameObject target,
            GameObject cinemachineVirtualCamera)
        {
            if (cinemachineVirtualCamera == null)
            {
                Debug.LogWarning("Cinemachine virtual camera instance is null; cannot set Follow reference.");
                return;
            }

            Type vcamType = FindTypeInAssemblies("CinemachineVirtualCamera");
            if (vcamType == null)
            {
                Debug.LogWarning("CinemachineVirtualCamera type not found in loaded assemblies; cannot set Follow.");
                return;
            }

            Component vcamComponent = cinemachineVirtualCamera.GetComponent(vcamType);
            if (vcamComponent == null)
            {
                Debug.LogError("CinemachineVirtualCamera component missing on the virtual camera GameObject.");
                return;
            }

            PropertyInfo followProp = vcamType.GetProperty("Follow");
            if (followProp == null || !followProp.CanWrite)
            {
                Debug.LogWarning("Cinemachine virtual camera has no writable 'Follow' property; cannot set Follow.");
                return;
            }

            Undo.RecordObject(vcamComponent, "Set Cinemachine Follow");
            followProp.SetValue(vcamComponent, target.transform);
            EditorUtility.SetDirty(vcamComponent);
        }

        private static bool TryLocatePrefab(string name, string[] inFolders, System.Type[] requiredComponentTypes, out GameObject prefab, out string path)
        {
            // Locate the player armature
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", inFolders);
            for (int i = 0; i < allPrefabs.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(allPrefabs[i]);

                if (assetPath.Contains("/com.unity.starter-assets/"))
                {
                    Object loadedObj = AssetDatabase.LoadMainAssetAtPath(assetPath);

                    if (PrefabUtility.GetPrefabAssetType(loadedObj) != PrefabAssetType.NotAPrefab &&
                        PrefabUtility.GetPrefabAssetType(loadedObj) != PrefabAssetType.MissingAsset)
                    {
                        GameObject loadedGo = loadedObj as GameObject;
                        bool hasRequiredComponents = true;
                        foreach (var componentType in requiredComponentTypes)
                        {
                            if (!loadedGo.TryGetComponent(componentType, out _))
                            {
                                hasRequiredComponents = false;
                                break;
                            }
                        }

                        if (hasRequiredComponents)
                        {
                            if (loadedGo.name == name)
                            {
                                prefab = loadedGo;
                                path = assetPath;
                                return true;
                            }
                        }
                    }
                }
            }

            prefab = null;
            path = null;
            return false;
        }

        // Variant of TryLocatePrefab that matches required components by type name (string) instead of
        // a compile-time Type[]. This avoids a hard reference to Cinemachine types, whose names changed
        // between Cinemachine 2.x and 3.x.
        private static bool TryLocatePrefabByComponentNames(string name, string[] inFolders, string[] requiredComponentTypeNames, out GameObject prefab, out string path)
        {
            string[] allPrefabs = inFolders != null && inFolders.Length > 0
                ? AssetDatabase.FindAssets("t:Prefab", inFolders)
                : AssetDatabase.FindAssets("t:Prefab");

            for (int i = 0; i < allPrefabs.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(allPrefabs[i]);
                Object loadedObj = AssetDatabase.LoadMainAssetAtPath(assetPath);

                if (PrefabUtility.GetPrefabAssetType(loadedObj) == PrefabAssetType.NotAPrefab ||
                    PrefabUtility.GetPrefabAssetType(loadedObj) == PrefabAssetType.MissingAsset)
                {
                    continue;
                }

                if (!(loadedObj is GameObject loadedGo) || loadedGo.name != name)
                {
                    continue;
                }

                bool hasRequiredComponents = true;
                foreach (var requiredName in requiredComponentTypeNames)
                {
                    if (!HasComponentByTypeName(loadedGo, requiredName))
                    {
                        hasRequiredComponents = false;
                        break;
                    }
                }

                if (hasRequiredComponents)
                {
                    prefab = loadedGo;
                    path = assetPath;
                    return true;
                }
            }

            prefab = null;
            path = null;
            return false;
        }

        // Returns true if the GameObject has a component whose type (or any base type) is named componentTypeName.
        private static bool HasComponentByTypeName(GameObject go, string componentTypeName)
        {
            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                for (Type t = component.GetType(); t != null; t = t.BaseType)
                {
                    if (t.Name == componentTypeName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Resolves a Type by its (non-namespaced) name across all loaded assemblies. Returns null if not found.
        private static Type FindTypeInAssemblies(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type != null && type.Name == typeName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static void HandleInstantiatingPrefab(GameObject prefab, out GameObject prefabInstance)
        {
            prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(prefabInstance, "Instantiate Starter Asset Prefab");

            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localEulerAngles = Vector3.zero;
            prefabInstance.transform.localScale = Vector3.one;
        }
    }
}