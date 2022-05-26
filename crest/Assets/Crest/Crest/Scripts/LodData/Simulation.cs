namespace Crest
{
    using UnityEngine;

    // Covariant
    public interface ISimulation<out DataType, out SettingsType>
    {
        SettingsType Settings { get; }
        DataType Data { get; }
        bool Enabled { get; }
        string Name { get; }

        void Update(OceanRenderer ocean);
        void CleanUpData();
    }

    public interface ISimulationWithMaterialKeyword
    {
        public bool Enabled { get; }
        string MaterialKeywordProperty { get; }
        string MaterialKeyword { get; }
        string ErrorMaterialKeywordMissing { get; }
        string ErrorMaterialKeywordMissingFix { get; }
        string ErrorMaterialKeywordOnFeatureOff { get; }
        string ErrorMaterialKeywordOnFeatureOffFix { get; }

        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var oceanMaterial = ocean.OceanMaterial;
            var isValid = true;

            if (oceanMaterial.HasProperty(MaterialKeywordProperty) && Enabled != oceanMaterial.IsKeywordEnabled(MaterialKeyword))
            {
                if (Enabled)
                {
                    showMessage(ErrorMaterialKeywordMissing, ErrorMaterialKeywordMissingFix,
                        ValidatedHelper.MessageType.Error, oceanMaterial,
                        (material) => ValidatedHelper.FixSetMaterialOptionEnabled(material, MaterialKeyword, MaterialKeywordProperty, true));
                    isValid = false;
                }
                else
                {
                    showMessage(ErrorMaterialKeywordOnFeatureOff, ErrorMaterialKeywordOnFeatureOffFix,
                        ValidatedHelper.MessageType.Info, ocean);
                }
            }

            return isValid;
        }
    }

    [System.Serializable]
    public abstract class Simulation<DataType, SettingsType> : ISimulation<DataType, SettingsType>
        where DataType : LodDataMgr
        where SettingsType : SimSettingsBase
    {
        [Tooltip("Whether the simulation is enabled.")]
        [SerializeField]
        protected bool _enabled;
        public bool Enabled => _enabled && _data != null;

        public abstract string Name { get; }

        // The LodDataMgr.
        [System.NonSerialized]
        protected DataType _data;
        public DataType Data
        {
            get
            {
                if (!_enabled)
                {
                    return null;
                }

                return _data;
            }
        }

        [SerializeField, Embedded]
        internal SettingsType _settings;
        SettingsType _defaultSettings;
        public SettingsType Settings
        {
            get
            {
                if (_settings != null)
                {
                    return _settings;
                }

                if (_defaultSettings == null)
                {
                    _defaultSettings = ScriptableObject.CreateInstance<SettingsType>();
                    _defaultSettings.name = Name + " Auto-generated Settings";
                }

                return _defaultSettings;
            }
        }

        // AKA batch mode.
        internal virtual bool RunsInHeadless => false;

        // Instantiates the LodDataMgr* class. Cannot use generics.
        protected abstract void AddData(OceanRenderer ocean);

        internal virtual void SetUpData(OceanRenderer ocean)
        {
            if (!_enabled)
            {
                return;
            }

            if (OceanRenderer.RunningWithoutGPU)
            {
                return;
            }

            if (OceanRenderer.RunningHeadless && !RunsInHeadless)
            {
                return;
            }

            Debug.Assert(_data == null, $"Crest: {Name} simulation data should be null when calling Enable.");

            AddData(ocean);
            _data.OnEnable();
        }

        public virtual void CleanUpData()
        {
            Debug.Assert(_data != null, $"Crest: {Name} simulation data should not be null when calling Disable.");
            _data.OnDisable();
            _data = null;
        }

        internal bool _failed;

        // Update is here to manage enabled/disabled state with data.
        public void Update(OceanRenderer ocean)
        {
            if (_failed)
            {
                return;
            }

            if (!_enabled && _data != null)
            {
                CleanUpData();
                return;
            }

            if (_enabled && _data == null)
            {
                SetUpData(ocean);
            }

            _data?.UpdateLodData();
        }
    }
}
