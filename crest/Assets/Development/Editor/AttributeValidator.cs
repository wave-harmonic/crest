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
                    ValidateFilterEnumAttribute(type);

                    foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        ValidateDecoratorAttribute(type, field);
                    }
                }
            }
        }

        static void ValidateDecoratorAttribute(Type type, FieldInfo field)
        {
            if (field.GetCustomAttributes<DecoratorAttribute>().ToList().Count == 0) return;
            if (field.GetCustomAttributes<DecoratedPropertyAttribute>().ToList().Count > 0) return;
            Debug.LogError($"Crest: A decorator attribute on {type}.{field.Name} has no attribute which inherits from DecoratedPropertyAttribute. The decorator will be ignored.");
        }

        static void ValidateFilterEnumAttribute(Type type)
        {
            foreach (var attribute in type.GetCustomAttributes<FilterEnumAttribute>())
            {
                var field = type.GetField(attribute._property, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field == null)
                {
                    Debug.LogError($"Crest: A FilterEnumAttribute on {type} has no property named <i>{attribute._property}</i>. It will be ignored.");
                }
                else if (field.FieldType.BaseType != typeof(Enum))
                {
                    Debug.LogError($"Crest: A FilterEnumAttribute on {type} references <i>{attribute._property}</i> ({field.FieldType}) which is not an enum. It will be ignored.");
                }
                else if (field.GetCustomAttribute<FilteredAttribute>() == null)
                {
                    Debug.LogError($"Crest: A FilterEnumAttribute on {type} references <i>{attribute._property}</i> which has no FilteredAttribute. It will be ignored.");
                }
            }
        }
    }
}
