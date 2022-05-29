// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using System.Diagnostics;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Constructs a custom link to Crest's documentation for the help URL button.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
    public class CrestHelpURLAttribute : HelpURLAttribute
    {
        // 0 = Version, 1 = Path, 2 = Render Pipeline, 3 = Heading
        const string k_URL = "https://crest.readthedocs.io/en/{0}/{1}.html?rp={2}#{3}";

        public CrestHelpURLAttribute(string path = "", string hash = "") : base(GetPageLink(path, hash))
        {
            // Blank.
        }

        public static string GetPageLink(string path, string hash = "")
        {
            var rp = "birp";

            return string.Format(k_URL, Internal.Constants.k_Version, path, rp, hash);
        }
    }
}
