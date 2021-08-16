// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using System.Reflection;
    using UnityEditor.Callbacks;
    using System.Linq;
    using UnityEngine;
    using System;

    /// <summary>
    /// Validates decorator attributes. They need to also have a decorated attribute to work.
    /// </summary>
    static class AttributeValidator
    {
        [DidReloadScripts]
        static void OnDidReloadScripts()
        {
            // TODO: Try using TypeCache in Unity 2020.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith("Crest")).ToList();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var property in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        if (property.GetCustomAttributes<DecoratorAttribute>().ToList().Count == 0) continue;
                        if (property.GetCustomAttributes<DecoratedPropertyAttribute>().ToList().Count > 0) continue;
                        Debug.LogError($"Crest: A decorator attribute on {type}.{property.Name} has no attribute which inherits from DecoratedPropertyAttribute. The decorator will be ignored.");
                    }
                }
            }
        }
    }
}
