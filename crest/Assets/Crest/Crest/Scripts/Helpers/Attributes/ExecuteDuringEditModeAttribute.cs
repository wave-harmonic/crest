using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ExecuteDuringEditModeAttribute : Attribute
{
    [Flags]
    public enum Include
    {
        None,
        PrefabStage,
        BuildPipeline,
        All = PrefabStage | BuildPipeline,
    }

    public Include _including;

    public ExecuteDuringEditModeAttribute(Include including = Include.PrefabStage)
    {
        _including = including;
    }
}
