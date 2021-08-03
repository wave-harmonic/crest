// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers for using the Unity Package Manager.

namespace Crest.EditorHelpers
{
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using UnityEngine;

    public static class PackageManagerHelpers
    {
        static AddRequest Request;

        public static void AddMissingPackage(string packageName)
        {
            Request = Client.Add(packageName);
            EditorApplication.update += AddMissingPackageProgress;
        }

        static void AddMissingPackageProgress()
        {
            if (Request.IsCompleted)
            {
                if (Request.Status == StatusCode.Success)
                {
                    Debug.Log("Installed: " + Request.Result.packageId);
                }
                else if (Request.Status >= StatusCode.Failure)
                {
                    Debug.Log(Request.Error.message);
                }

                EditorApplication.update -= AddMissingPackageProgress;
            }
        }
    }
}
