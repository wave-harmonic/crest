// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers for using the Unity Package Manager.

#if UNITY_EDITOR

namespace Crest.EditorHelpers
{
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using UnityEngine;

    public static class PackageManagerHelpers
    {
        static AddRequest s_Request;
        public static bool IsBusy => s_Request?.IsCompleted == false;

        public static void AddMissingPackage(string packageName)
        {
            s_Request = Client.Add(packageName);
            EditorApplication.update += AddMissingPackageProgress;
        }

        static void AddMissingPackageProgress()
        {
            if (s_Request.IsCompleted)
            {
                if (s_Request.Status == StatusCode.Success)
                {
                    Debug.Log("Installed: " + s_Request.Result.packageId);
                }
                else if (s_Request.Status >= StatusCode.Failure)
                {
                    Debug.Log(s_Request.Error.message);
                }

                EditorApplication.update -= AddMissingPackageProgress;
            }
        }
    }
}

#endif // UNITY_EDITOR
