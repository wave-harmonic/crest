// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;

namespace Crest
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class OnChangeAttribute : Attribute
    {
        public string Method { get; private set; }

        public OnChangeAttribute(string method)
        {
            Method = method;
        }
    }
}
