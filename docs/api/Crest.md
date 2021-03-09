<a name='assembly'></a>
# Crest

## Contents

- [BoatProbes](#T-Crest-BoatProbes 'Crest.BoatProbes')
- [BuildCommandBuffer](#T-Crest-BuildCommandBuffer 'Crest.BuildCommandBuffer')
  - [BuildAndExecute()](#M-Crest-BuildCommandBuffer-BuildAndExecute 'Crest.BuildCommandBuffer.BuildAndExecute')
- [BuildCommandBufferBase](#T-Crest-BuildCommandBufferBase 'Crest.BuildCommandBufferBase')
  - [_lastUpdateFrame](#F-Crest-BuildCommandBufferBase-_lastUpdateFrame 'Crest.BuildCommandBufferBase._lastUpdateFrame')
- [CollProviderNull](#T-Crest-CollProviderNull 'Crest.CollProviderNull')
- [CrestSortedList\`2](#T-Crest-CrestSortedList`2 'Crest.CrestSortedList`2')
- [DuplicateKeyComparer\`1](#T-Crest-DuplicateKeyComparer`1 'Crest.DuplicateKeyComparer`1')
- [EditorHelpers](#T-Crest-EditorHelpers-EditorHelpers 'Crest.EditorHelpers.EditorHelpers')
  - [GetActiveSceneViewCamera()](#M-Crest-EditorHelpers-EditorHelpers-GetActiveSceneViewCamera 'Crest.EditorHelpers.EditorHelpers.GetActiveSceneViewCamera')
- [EmbeddedAssetEditor](#T-Crest-EditorHelpers-EmbeddedAssetEditor 'Crest.EditorHelpers.EmbeddedAssetEditor')
  - [#ctor()](#M-Crest-EditorHelpers-EmbeddedAssetEditor-#ctor 'Crest.EditorHelpers.EmbeddedAssetEditor.#ctor')
  - [OnChanged](#F-Crest-EditorHelpers-EmbeddedAssetEditor-OnChanged 'Crest.EditorHelpers.EmbeddedAssetEditor.OnChanged')
  - [OnCreateEditor](#F-Crest-EditorHelpers-EmbeddedAssetEditor-OnCreateEditor 'Crest.EditorHelpers.EmbeddedAssetEditor.OnCreateEditor')
  - [m_CreateButtonGUIContent](#F-Crest-EditorHelpers-EmbeddedAssetEditor-m_CreateButtonGUIContent 'Crest.EditorHelpers.EmbeddedAssetEditor.m_CreateButtonGUIContent')
  - [DrawEditorCombo()](#M-Crest-EditorHelpers-EmbeddedAssetEditor-DrawEditorCombo-System-String,System-String,System-String,System-String,System-Boolean,UnityEditor-SerializedProperty- 'Crest.EditorHelpers.EmbeddedAssetEditor.DrawEditorCombo(System.String,System.String,System.String,System.String,System.Boolean,UnityEditor.SerializedProperty)')
  - [OnDisable()](#M-Crest-EditorHelpers-EmbeddedAssetEditor-OnDisable 'Crest.EditorHelpers.EmbeddedAssetEditor.OnDisable')
- [FloatingObjectBase](#T-Crest-FloatingObjectBase 'Crest.FloatingObjectBase')
- [FloatingOrigin](#T-Crest-FloatingOrigin 'Crest.FloatingOrigin')
  - [MoveOriginDisablePhysics()](#M-Crest-FloatingOrigin-MoveOriginDisablePhysics 'Crest.FloatingOrigin.MoveOriginDisablePhysics')
  - [MoveOriginOcean()](#M-Crest-FloatingOrigin-MoveOriginOcean-UnityEngine-Vector3- 'Crest.FloatingOrigin.MoveOriginOcean(UnityEngine.Vector3)')
  - [MoveOriginParticles()](#M-Crest-FloatingOrigin-MoveOriginParticles-UnityEngine-Vector3- 'Crest.FloatingOrigin.MoveOriginParticles(UnityEngine.Vector3)')
  - [MoveOriginTransforms()](#M-Crest-FloatingOrigin-MoveOriginTransforms-UnityEngine-Vector3- 'Crest.FloatingOrigin.MoveOriginTransforms(UnityEngine.Vector3)')
- [FlowProviderNull](#T-Crest-FlowProviderNull 'Crest.FlowProviderNull')
- [ICollProvider](#T-Crest-ICollProvider 'Crest.ICollProvider')
  - [CleanUp()](#M-Crest-ICollProvider-CleanUp 'Crest.ICollProvider.CleanUp')
  - [Query(i_ownerHash,i_minSpatialLength,i_queryPoints,o_resultHeights,o_resultNorms,o_resultVels)](#M-Crest-ICollProvider-Query-System-Int32,System-Single,UnityEngine-Vector3[],System-Single[],UnityEngine-Vector3[],UnityEngine-Vector3[]- 'Crest.ICollProvider.Query(System.Int32,System.Single,UnityEngine.Vector3[],System.Single[],UnityEngine.Vector3[],UnityEngine.Vector3[])')
  - [Query(i_ownerHash,i_minSpatialLength,i_queryPoints,o_resultDisps,o_resultNorms,o_resultVels)](#M-Crest-ICollProvider-Query-System-Int32,System-Single,UnityEngine-Vector3[],UnityEngine-Vector3[],UnityEngine-Vector3[],UnityEngine-Vector3[]- 'Crest.ICollProvider.Query(System.Int32,System.Single,UnityEngine.Vector3[],UnityEngine.Vector3[],UnityEngine.Vector3[],UnityEngine.Vector3[])')
  - [RetrieveSucceeded()](#M-Crest-ICollProvider-RetrieveSucceeded-System-Int32- 'Crest.ICollProvider.RetrieveSucceeded(System.Int32)')
  - [UpdateQueries()](#M-Crest-ICollProvider-UpdateQueries 'Crest.ICollProvider.UpdateQueries')
- [IFloatingOrigin](#T-Crest-IFloatingOrigin 'Crest.IFloatingOrigin')
  - [SetOrigin()](#M-Crest-IFloatingOrigin-SetOrigin-UnityEngine-Vector3- 'Crest.IFloatingOrigin.SetOrigin(UnityEngine.Vector3)')
- [IFlowProvider](#T-Crest-IFlowProvider 'Crest.IFlowProvider')
  - [CleanUp()](#M-Crest-IFlowProvider-CleanUp 'Crest.IFlowProvider.CleanUp')
  - [Query(i_ownerHash,i_minSpatialLength,i_queryPoints,o_resultVels)](#M-Crest-IFlowProvider-Query-System-Int32,System-Single,UnityEngine-Vector3[],UnityEngine-Vector3[]- 'Crest.IFlowProvider.Query(System.Int32,System.Single,UnityEngine.Vector3[],UnityEngine.Vector3[])')
  - [RetrieveSucceeded()](#M-Crest-IFlowProvider-RetrieveSucceeded-System-Int32- 'Crest.IFlowProvider.RetrieveSucceeded(System.Int32)')
  - [UpdateQueries()](#M-Crest-IFlowProvider-UpdateQueries 'Crest.IFlowProvider.UpdateQueries')
- [IPropertyWrapper](#T-Crest-IPropertyWrapper 'Crest.IPropertyWrapper')
- [ITimeProvider](#T-Crest-ITimeProvider 'Crest.ITimeProvider')
- [LodDataMgr](#T-Crest-LodDataMgr 'Crest.LodDataMgr')
- [LodDataMgrAnimWaves](#T-Crest-LodDataMgrAnimWaves 'Crest.LodDataMgrAnimWaves')
  - [_shapeCombinePass](#F-Crest-LodDataMgrAnimWaves-_shapeCombinePass 'Crest.LodDataMgrAnimWaves._shapeCombinePass')
  - [SuggestDataLOD()](#M-Crest-LodDataMgrAnimWaves-SuggestDataLOD-UnityEngine-Rect- 'Crest.LodDataMgrAnimWaves.SuggestDataLOD(UnityEngine.Rect)')
- [LodDataMgrClipSurface](#T-Crest-LodDataMgrClipSurface 'Crest.LodDataMgrClipSurface')
- [LodDataMgrDynWaves](#T-Crest-LodDataMgrDynWaves 'Crest.LodDataMgrDynWaves')
- [LodDataMgrFlow](#T-Crest-LodDataMgrFlow 'Crest.LodDataMgrFlow')
- [LodDataMgrFoam](#T-Crest-LodDataMgrFoam 'Crest.LodDataMgrFoam')
- [LodDataMgrPersistent](#T-Crest-LodDataMgrPersistent 'Crest.LodDataMgrPersistent')
  - [SetAdditionalSimParams()](#M-Crest-LodDataMgrPersistent-SetAdditionalSimParams-Crest-IPropertyWrapper- 'Crest.LodDataMgrPersistent.SetAdditionalSimParams(Crest.IPropertyWrapper)')
- [LodDataMgrSeaFloorDepth](#T-Crest-LodDataMgrSeaFloorDepth 'Crest.LodDataMgrSeaFloorDepth')
- [LodDataMgrShadow](#T-Crest-LodDataMgrShadow 'Crest.LodDataMgrShadow')
- [LodTransform](#T-Crest-LodTransform 'Crest.LodTransform')
- [MultiPropertyAttribute](#T-Crest-MultiPropertyAttribute 'Crest.MultiPropertyAttribute')
  - [BuildLabel()](#M-Crest-MultiPropertyAttribute-BuildLabel-UnityEngine-GUIContent- 'Crest.MultiPropertyAttribute.BuildLabel(UnityEngine.GUIContent)')
  - [GetPropertyHeight()](#M-Crest-MultiPropertyAttribute-GetPropertyHeight-UnityEditor-SerializedProperty,UnityEngine-GUIContent- 'Crest.MultiPropertyAttribute.GetPropertyHeight(UnityEditor.SerializedProperty,UnityEngine.GUIContent)')
  - [OnGUI()](#M-Crest-MultiPropertyAttribute-OnGUI-UnityEngine-Rect,UnityEditor-SerializedProperty,UnityEngine-GUIContent,Crest-EditorHelpers-MultiPropertyDrawer- 'Crest.MultiPropertyAttribute.OnGUI(UnityEngine.Rect,UnityEditor.SerializedProperty,UnityEngine.GUIContent,Crest.EditorHelpers.MultiPropertyDrawer)')
- [ObjectWaterInteraction](#T-Crest-ObjectWaterInteraction 'Crest.ObjectWaterInteraction')
- [OceanBuilder](#T-Crest-OceanBuilder 'Crest.OceanBuilder')
- [OceanChunkRenderer](#T-Crest-OceanChunkRenderer 'Crest.OceanChunkRenderer')
- [OceanDepthCache](#T-Crest-OceanDepthCache 'Crest.OceanDepthCache')
- [OceanPlanarReflection](#T-Crest-OceanPlanarReflection 'Crest.OceanPlanarReflection')
  - [RefreshPerFrames](#F-Crest-OceanPlanarReflection-RefreshPerFrames 'Crest.OceanPlanarReflection.RefreshPerFrames')
  - [_frameRefreshOffset](#F-Crest-OceanPlanarReflection-_frameRefreshOffset 'Crest.OceanPlanarReflection._frameRefreshOffset')
  - [ForceDistanceCulling(farClipPlane)](#M-Crest-OceanPlanarReflection-ForceDistanceCulling-System-Single- 'Crest.OceanPlanarReflection.ForceDistanceCulling(System.Single)')
- [OceanRenderer](#T-Crest-OceanRenderer 'Crest.OceanRenderer')
  - [CollisionProvider](#P-Crest-OceanRenderer-CollisionProvider 'Crest.OceanRenderer.CollisionProvider')
  - [CurrentLodCount](#P-Crest-OceanRenderer-CurrentLodCount 'Crest.OceanRenderer.CurrentLodCount')
  - [MaxHorizDisplacement](#P-Crest-OceanRenderer-MaxHorizDisplacement 'Crest.OceanRenderer.MaxHorizDisplacement')
  - [MaxVertDisplacement](#P-Crest-OceanRenderer-MaxVertDisplacement 'Crest.OceanRenderer.MaxVertDisplacement')
  - [Scale](#P-Crest-OceanRenderer-Scale 'Crest.OceanRenderer.Scale')
  - [ScaleCouldDecrease](#P-Crest-OceanRenderer-ScaleCouldDecrease 'Crest.OceanRenderer.ScaleCouldDecrease')
  - [ScaleCouldIncrease](#P-Crest-OceanRenderer-ScaleCouldIncrease 'Crest.OceanRenderer.ScaleCouldIncrease')
  - [SeaLevel](#P-Crest-OceanRenderer-SeaLevel 'Crest.OceanRenderer.SeaLevel')
  - [ViewerAltitudeLevelAlpha](#P-Crest-OceanRenderer-ViewerAltitudeLevelAlpha 'Crest.OceanRenderer.ViewerAltitudeLevelAlpha')
  - [ViewerHeightAboveWater](#P-Crest-OceanRenderer-ViewerHeightAboveWater 'Crest.OceanRenderer.ViewerHeightAboveWater')
  - [ReportMaxDisplacementFromShape()](#M-Crest-OceanRenderer-ReportMaxDisplacementFromShape-System-Single,System-Single,System-Single- 'Crest.OceanRenderer.ReportMaxDisplacementFromShape(System.Single,System.Single,System.Single)')
- [OceanWaveSpectrum](#T-Crest-OceanWaveSpectrum 'Crest.OceanWaveSpectrum')
  - [GenerateWaveData()](#M-Crest-OceanWaveSpectrum-GenerateWaveData-System-Int32,System-Single[]@,System-Single[]@- 'Crest.OceanWaveSpectrum.GenerateWaveData(System.Int32,System.Single[]@,System.Single[]@)')
- [PatchType](#T-Crest-OceanBuilder-PatchType 'Crest.OceanBuilder.PatchType')
  - [Count](#F-Crest-OceanBuilder-PatchType-Count 'Crest.OceanBuilder.PatchType.Count')
  - [Fat](#F-Crest-OceanBuilder-PatchType-Fat 'Crest.OceanBuilder.PatchType.Fat')
  - [FatX](#F-Crest-OceanBuilder-PatchType-FatX 'Crest.OceanBuilder.PatchType.FatX')
  - [FatXOuter](#F-Crest-OceanBuilder-PatchType-FatXOuter 'Crest.OceanBuilder.PatchType.FatXOuter')
  - [FatXSlimZ](#F-Crest-OceanBuilder-PatchType-FatXSlimZ 'Crest.OceanBuilder.PatchType.FatXSlimZ')
  - [FatXZ](#F-Crest-OceanBuilder-PatchType-FatXZ 'Crest.OceanBuilder.PatchType.FatXZ')
  - [FatXZOuter](#F-Crest-OceanBuilder-PatchType-FatXZOuter 'Crest.OceanBuilder.PatchType.FatXZOuter')
  - [Interior](#F-Crest-OceanBuilder-PatchType-Interior 'Crest.OceanBuilder.PatchType.Interior')
  - [SlimX](#F-Crest-OceanBuilder-PatchType-SlimX 'Crest.OceanBuilder.PatchType.SlimX')
  - [SlimXFatZ](#F-Crest-OceanBuilder-PatchType-SlimXFatZ 'Crest.OceanBuilder.PatchType.SlimXFatZ')
  - [SlimXZ](#F-Crest-OceanBuilder-PatchType-SlimXZ 'Crest.OceanBuilder.PatchType.SlimXZ')
- [PredicatedFieldAttribute](#T-Crest-PredicatedFieldAttribute 'Crest.PredicatedFieldAttribute')
  - [#ctor(propertyName,inverted,disableIfValueIs)](#M-Crest-PredicatedFieldAttribute-#ctor-System-String,System-Boolean,System-Int32- 'Crest.PredicatedFieldAttribute.#ctor(System.String,System.Boolean,System.Int32)')
- [QueryBase](#T-Crest-QueryBase 'Crest.QueryBase')
  - [CalculateVelocities()](#M-Crest-QueryBase-CalculateVelocities-System-Int32,UnityEngine-Vector3[]- 'Crest.QueryBase.CalculateVelocities(System.Int32,UnityEngine.Vector3[])')
  - [CompactQueryStorage()](#M-Crest-QueryBase-CompactQueryStorage 'Crest.QueryBase.CompactQueryStorage')
  - [DataArrived()](#M-Crest-QueryBase-DataArrived-UnityEngine-Rendering-AsyncGPUReadbackRequest- 'Crest.QueryBase.DataArrived(UnityEngine.Rendering.AsyncGPUReadbackRequest)')
  - [RemoveQueryPoints()](#M-Crest-QueryBase-RemoveQueryPoints-System-Int32- 'Crest.QueryBase.RemoveQueryPoints(System.Int32)')
  - [RetrieveResults()](#M-Crest-QueryBase-RetrieveResults-System-Int32,UnityEngine-Vector3[],System-Single[],UnityEngine-Vector3[]- 'Crest.QueryBase.RetrieveResults(System.Int32,UnityEngine.Vector3[],System.Single[],UnityEngine.Vector3[])')
  - [UpdateQueryPoints()](#M-Crest-QueryBase-UpdateQueryPoints-System-Int32,System-Single,UnityEngine-Vector3[],UnityEngine-Vector3[]- 'Crest.QueryBase.UpdateQueryPoints(System.Int32,System.Single,UnityEngine.Vector3[],UnityEngine.Vector3[])')
- [QueryDisplacements](#T-Crest-QueryDisplacements 'Crest.QueryDisplacements')
- [QueryFlow](#T-Crest-QueryFlow 'Crest.QueryFlow')
- [RayTraceHelper](#T-Crest-RayTraceHelper 'Crest.RayTraceHelper')
  - [#ctor()](#M-Crest-RayTraceHelper-#ctor-System-Single,System-Single- 'Crest.RayTraceHelper.#ctor(System.Single,System.Single)')
  - [Init(i_rayOrigin,i_rayDirection)](#M-Crest-RayTraceHelper-Init-UnityEngine-Vector3,UnityEngine-Vector3- 'Crest.RayTraceHelper.Init(UnityEngine.Vector3,UnityEngine.Vector3)')
  - [Trace(o_distance)](#M-Crest-RayTraceHelper-Trace-System-Single@- 'Crest.RayTraceHelper.Trace(System.Single@)')
- [RegisterAnimWavesInput](#T-Crest-RegisterAnimWavesInput 'Crest.RegisterAnimWavesInput')
- [RegisterClipSurfaceInput](#T-Crest-RegisterClipSurfaceInput 'Crest.RegisterClipSurfaceInput')
- [RegisterDynWavesInput](#T-Crest-RegisterDynWavesInput 'Crest.RegisterDynWavesInput')
- [RegisterFlowInput](#T-Crest-RegisterFlowInput 'Crest.RegisterFlowInput')
- [RegisterFoamInput](#T-Crest-RegisterFoamInput 'Crest.RegisterFoamInput')
- [RegisterLodDataInputBase](#T-Crest-RegisterLodDataInputBase 'Crest.RegisterLodDataInputBase')
- [RegisterLodDataInput\`1](#T-Crest-RegisterLodDataInput`1 'Crest.RegisterLodDataInput`1')
- [RegisterSeaFloorDepthInput](#T-Crest-RegisterSeaFloorDepthInput 'Crest.RegisterSeaFloorDepthInput')
- [RegisterShadowInput](#T-Crest-RegisterShadowInput 'Crest.RegisterShadowInput')
- [RenderAlphaOnSurface](#T-Crest-RenderAlphaOnSurface 'Crest.RenderAlphaOnSurface')
- [RenderWireFrame](#T-RenderWireFrame 'RenderWireFrame')
- [SampleFlowHelper](#T-Crest-SampleFlowHelper 'Crest.SampleFlowHelper')
  - [Init(i_queryPos,i_minLength)](#M-Crest-SampleFlowHelper-Init-UnityEngine-Vector3,System-Single- 'Crest.SampleFlowHelper.Init(UnityEngine.Vector3,System.Single)')
  - [Sample()](#M-Crest-SampleFlowHelper-Sample-UnityEngine-Vector2@- 'Crest.SampleFlowHelper.Sample(UnityEngine.Vector2@)')
- [SampleHeightHelper](#T-Crest-SampleHeightHelper 'Crest.SampleHeightHelper')
  - [Init(i_queryPos,i_minLength,allowMultipleCallsPerFrame)](#M-Crest-SampleHeightHelper-Init-UnityEngine-Vector3,System-Single,System-Boolean,UnityEngine-Object- 'Crest.SampleHeightHelper.Init(UnityEngine.Vector3,System.Single,System.Boolean,UnityEngine.Object)')
  - [Sample()](#M-Crest-SampleHeightHelper-Sample-System-Single@- 'Crest.SampleHeightHelper.Sample(System.Single@)')
- [ScriptableObjectUtility](#T-Crest-EditorHelpers-ScriptableObjectUtility 'Crest.EditorHelpers.ScriptableObjectUtility')
  - [CreateAt()](#M-Crest-EditorHelpers-ScriptableObjectUtility-CreateAt-System-Type,System-String- 'Crest.EditorHelpers.ScriptableObjectUtility.CreateAt(System.Type,System.String)')
  - [CreateAt\`\`1()](#M-Crest-EditorHelpers-ScriptableObjectUtility-CreateAt``1-System-String- 'Crest.EditorHelpers.ScriptableObjectUtility.CreateAt``1(System.String)')
- [SegmentRegistrar](#T-Crest-QueryBase-SegmentRegistrar 'Crest.QueryBase.SegmentRegistrar')
- [SegmentRegistrarRingBuffer](#T-Crest-QueryBase-SegmentRegistrarRingBuffer 'Crest.QueryBase.SegmentRegistrarRingBuffer')
- [ShapeGerstner](#T-Crest-ShapeGerstner 'Crest.ShapeGerstner')
  - [MinWavelength()](#M-Crest-ShapeGerstner-MinWavelength-System-Int32- 'Crest.ShapeGerstner.MinWavelength(System.Int32)')
- [ShapeGerstnerBatched](#T-Crest-ShapeGerstnerBatched 'Crest.ShapeGerstnerBatched')
  - [UpdateBatch()](#M-Crest-ShapeGerstnerBatched-UpdateBatch-System-Int32,System-Int32,System-Int32,Crest-ShapeGerstnerBatched-GerstnerBatch- 'Crest.ShapeGerstnerBatched.UpdateBatch(System.Int32,System.Int32,System.Int32,Crest.ShapeGerstnerBatched.GerstnerBatch)')
- [ShapeGerstnerSplineHandling](#T-Crest-ShapeGerstnerSplineHandling 'Crest.ShapeGerstnerSplineHandling')
- [SimSettingsAnimatedWaves](#T-Crest-SimSettingsAnimatedWaves 'Crest.SimSettingsAnimatedWaves')
  - [CreateCollisionProvider()](#M-Crest-SimSettingsAnimatedWaves-CreateCollisionProvider 'Crest.SimSettingsAnimatedWaves.CreateCollisionProvider')
- [SimSettingsBase](#T-Crest-SimSettingsBase 'Crest.SimSettingsBase')
- [SimpleFloatingObject](#T-Crest-SimpleFloatingObject 'Crest.SimpleFloatingObject')
  - [FixedUpdateOrientation()](#M-Crest-SimpleFloatingObject-FixedUpdateOrientation-UnityEngine-Vector3- 'Crest.SimpleFloatingObject.FixedUpdateOrientation(UnityEngine.Vector3)')
- [SphereWaterInteraction](#T-Crest-SphereWaterInteraction 'Crest.SphereWaterInteraction')
- [Spline](#T-Crest-Spline-Spline 'Crest.Spline.Spline')
- [SplineInterpolation](#T-Crest-Spline-SplineInterpolation 'Crest.Spline.SplineInterpolation')
  - [GenerateCubicSplineHull(splinePoints,splinePointsAndTangents)](#M-Crest-Spline-SplineInterpolation-GenerateCubicSplineHull-Crest-Spline-SplinePoint[],UnityEngine-Vector3[],System-Boolean- 'Crest.Spline.SplineInterpolation.GenerateCubicSplineHull(Crest.Spline.SplinePoint[],UnityEngine.Vector3[],System.Boolean)')
  - [InterpolateCubicPosition(splinePointCount,splinePointsAndTangents,t,position)](#M-Crest-Spline-SplineInterpolation-InterpolateCubicPosition-System-Single,UnityEngine-Vector3[],System-Single,UnityEngine-Vector3@- 'Crest.Spline.SplineInterpolation.InterpolateCubicPosition(System.Single,UnityEngine.Vector3[],System.Single,UnityEngine.Vector3@)')
  - [InterpolateLinearPosition(points,t,position)](#M-Crest-Spline-SplineInterpolation-InterpolateLinearPosition-UnityEngine-Vector3[],System-Single,UnityEngine-Vector3@- 'Crest.Spline.SplineInterpolation.InterpolateLinearPosition(UnityEngine.Vector3[],System.Single,UnityEngine.Vector3@)')
- [SplinePoint](#T-Crest-Spline-SplinePoint 'Crest.Spline.SplinePoint')
- [TimeProviderCustom](#T-Crest-TimeProviderCustom 'Crest.TimeProviderCustom')
- [TimeProviderDefault](#T-Crest-TimeProviderDefault 'Crest.TimeProviderDefault')
- [UnderwaterEffect](#T-Crest-UnderwaterEffect 'Crest.UnderwaterEffect')
- [VisualiseCollisionArea](#T-Crest-VisualiseCollisionArea 'Crest.VisualiseCollisionArea')
- [VisualiseRayTrace](#T-Crest-VisualiseRayTrace 'Crest.VisualiseRayTrace')
- [WaterBody](#T-Crest-WaterBody 'Crest.WaterBody')

<a name='T-Crest-BoatProbes'></a>
## BoatProbes `type`

##### Namespace

Crest

##### Summary

Boat physics by sampling at multiple probe points.

<a name='T-Crest-BuildCommandBuffer'></a>
## BuildCommandBuffer `type`

##### Namespace

Crest

##### Summary

The default builder for the ocean update command buffer which takes care of updating all ocean-related data, for
example rendering animated waves and advancing sims. This runs in LateUpdate after the Default bucket, after the ocean
system been moved to an up to date position and frame processing is done.

<a name='M-Crest-BuildCommandBuffer-BuildAndExecute'></a>
### BuildAndExecute() `method`

##### Summary

Construct the command buffer and attach it to the camera so that it will be executed in the render.

##### Parameters

This method has no parameters.

<a name='T-Crest-BuildCommandBufferBase'></a>
## BuildCommandBufferBase `type`

##### Namespace

Crest

##### Summary

Base class for the command buffer builder, which takes care of updating all ocean-related data. If you wish to provide your
own update logic, you can create a new component that inherits from this class and attach it to the same GameObject as the
OceanRenderer script. The new component should be set to update after the Default bucket, similar to BuildCommandBuffer.

<a name='F-Crest-BuildCommandBufferBase-_lastUpdateFrame'></a>
### _lastUpdateFrame `constants`

##### Summary

Used to validate update order

<a name='T-Crest-CollProviderNull'></a>
## CollProviderNull `type`

##### Namespace

Crest

##### Summary

Gives a flat, still ocean.

<a name='T-Crest-CrestSortedList`2'></a>
## CrestSortedList\`2 `type`

##### Namespace

Crest

##### Summary

This is a list this is meant to be similar in behaviour to the C#
 SortedList, but without allocations when used directly in a foreach loop.

 It works by using a regular list as as backing and ensuring that it is
 sorted when the enumerator is accessed and used. This is a simple approach
 that means we avoid sorting each time an element is added, and helps us
 avoid having to develop our own more complex data structure.

<a name='T-Crest-DuplicateKeyComparer`1'></a>
## DuplicateKeyComparer\`1 `type`

##### Namespace

Crest

##### Summary

Comparer that always returns less or greater, never equal, to get work around unique key constraint

<a name='T-Crest-EditorHelpers-EditorHelpers'></a>
## EditorHelpers `type`

##### Namespace

Crest.EditorHelpers

##### Summary

Provides general helper functions for the editor.

<a name='M-Crest-EditorHelpers-EditorHelpers-GetActiveSceneViewCamera'></a>
### GetActiveSceneViewCamera() `method`

##### Summary

Returns the scene view camera if the scene view is focused.

##### Parameters

This method has no parameters.

<a name='T-Crest-EditorHelpers-EmbeddedAssetEditor'></a>
## EmbeddedAssetEditor `type`

##### Namespace

Crest.EditorHelpers

##### Summary

Helper for drawing embedded asset editors

<a name='M-Crest-EditorHelpers-EmbeddedAssetEditor-#ctor'></a>
### #ctor() `constructor`

##### Summary

Create in OnEnable()

##### Parameters

This constructor has no parameters.

<a name='F-Crest-EditorHelpers-EmbeddedAssetEditor-OnChanged'></a>
### OnChanged `constants`

##### Summary

Called when the asset being edited was changed by the user.

<a name='F-Crest-EditorHelpers-EmbeddedAssetEditor-OnCreateEditor'></a>
### OnCreateEditor `constants`

##### Summary

Called after the asset editor is created, in case it needs
to be customized

<a name='F-Crest-EditorHelpers-EmbeddedAssetEditor-m_CreateButtonGUIContent'></a>
### m_CreateButtonGUIContent `constants`

##### Summary

Customize this after creation if you want

<a name='M-Crest-EditorHelpers-EmbeddedAssetEditor-DrawEditorCombo-System-String,System-String,System-String,System-String,System-Boolean,UnityEditor-SerializedProperty-'></a>
### DrawEditorCombo() `method`

##### Summary

Call this from OnInspectorGUI.  Will draw the asset reference field, and
the embedded editor, or a Create Asset button, if no asset is set.

##### Parameters

This method has no parameters.

<a name='M-Crest-EditorHelpers-EmbeddedAssetEditor-OnDisable'></a>
### OnDisable() `method`

##### Summary

Free the resources in OnDisable()

##### Parameters

This method has no parameters.

<a name='T-Crest-FloatingObjectBase'></a>
## FloatingObjectBase `type`

##### Namespace

Crest

##### Summary

Base class for objects that float on water.

<a name='T-Crest-FloatingOrigin'></a>
## FloatingOrigin `type`

##### Namespace

Crest

##### Summary

This script translates all objects in the world to keep the camera near the origin in order to prevent spatial jittering due to limited
floating-point precision. The script detects when the camera is further than 'threshold' units from the origin in one or more axes, at which
point it moves everything so that the camera is back at the origin. There is also an option to disable physics beyond a certain point. This
script should normally be attached to the viewpoint, typically the main camera.

<a name='M-Crest-FloatingOrigin-MoveOriginDisablePhysics'></a>
### MoveOriginDisablePhysics() `method`

##### Summary

Disable physics outside radius

##### Parameters

This method has no parameters.

<a name='M-Crest-FloatingOrigin-MoveOriginOcean-UnityEngine-Vector3-'></a>
### MoveOriginOcean() `method`

##### Summary

Notify ocean of origin shift

##### Parameters

This method has no parameters.

<a name='M-Crest-FloatingOrigin-MoveOriginParticles-UnityEngine-Vector3-'></a>
### MoveOriginParticles() `method`

##### Summary

Move all particles that are simulated in world space

##### Parameters

This method has no parameters.

<a name='M-Crest-FloatingOrigin-MoveOriginTransforms-UnityEngine-Vector3-'></a>
### MoveOriginTransforms() `method`

##### Summary

Move transforms to recenter around new origin

##### Parameters

This method has no parameters.

<a name='T-Crest-FlowProviderNull'></a>
## FlowProviderNull `type`

##### Namespace

Crest

##### Summary

Gives a stationary ocean (no horizontal flow).

<a name='T-Crest-ICollProvider'></a>
## ICollProvider `type`

##### Namespace

Crest

##### Summary

Interface for an object that returns ocean surface displacement and height.

<a name='M-Crest-ICollProvider-CleanUp'></a>
### CleanUp() `method`

##### Summary

On destroy, to cleanup resources

##### Parameters

This method has no parameters.

<a name='M-Crest-ICollProvider-Query-System-Int32,System-Single,UnityEngine-Vector3[],System-Single[],UnityEngine-Vector3[],UnityEngine-Vector3[]-'></a>
### Query(i_ownerHash,i_minSpatialLength,i_queryPoints,o_resultHeights,o_resultNorms,o_resultVels) `method`

##### Summary

Query water physical data at a set of points. Pass in null to any out parameters that are not required.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| i_ownerHash | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | Unique ID for calling code. Typically acquired by calling GetHashCode(). |
| i_minSpatialLength | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The min spatial length of the object, such as the width of a boat. Useful for filtering out detail when not needed. Set to 0 to get full available detail. |
| i_queryPoints | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | The world space points that will be queried. |
| o_resultHeights | [System.Single[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single[] 'System.Single[]') | Float array of water heights at the query positions. Pass null if this information is not required. |
| o_resultNorms | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | Water normals at the query positions. Pass null if this information is not required. |
| o_resultVels | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | Water surface velocities at the query positions. Pass null if this information is not required. |

<a name='M-Crest-ICollProvider-Query-System-Int32,System-Single,UnityEngine-Vector3[],UnityEngine-Vector3[],UnityEngine-Vector3[],UnityEngine-Vector3[]-'></a>
### Query(i_ownerHash,i_minSpatialLength,i_queryPoints,o_resultDisps,o_resultNorms,o_resultVels) `method`

##### Summary

Query water physical data at a set of points. Pass in null to any out parameters that are not required.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| i_ownerHash | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | Unique ID for calling code. Typically acquired by calling GetHashCode(). |
| i_minSpatialLength | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The min spatial length of the object, such as the width of a boat. Useful for filtering out detail when not needed. Set to 0 to get full available detail. |
| i_queryPoints | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | The world space points that will be queried. |
| o_resultDisps | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | Displacement vectors for water surface points that will displace to the XZ coordinates of the query points. Water heights are given by sea level plus the y component of the displacement. |
| o_resultNorms | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | Water normals at the query positions. Pass null if this information is not required. |
| o_resultVels | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | Water surface velocities at the query positions. Pass null if this information is not required. |

<a name='M-Crest-ICollProvider-RetrieveSucceeded-System-Int32-'></a>
### RetrieveSucceeded() `method`

##### Summary

Check if query results could be retrieved successfully using return code from Query() function

##### Parameters

This method has no parameters.

<a name='M-Crest-ICollProvider-UpdateQueries'></a>
### UpdateQueries() `method`

##### Summary

Per frame update callback

##### Parameters

This method has no parameters.

<a name='T-Crest-IFloatingOrigin'></a>
## IFloatingOrigin `type`

##### Namespace

Crest

<a name='M-Crest-IFloatingOrigin-SetOrigin-UnityEngine-Vector3-'></a>
### SetOrigin() `method`

##### Summary

Set a new origin. This is equivalent to subtracting the new origin position from any world position state.

##### Parameters

This method has no parameters.

<a name='T-Crest-IFlowProvider'></a>
## IFlowProvider `type`

##### Namespace

Crest

##### Summary

Interface for an object that returns ocean surface displacement and height.

<a name='M-Crest-IFlowProvider-CleanUp'></a>
### CleanUp() `method`

##### Summary

On destroy, to cleanup resources

##### Parameters

This method has no parameters.

<a name='M-Crest-IFlowProvider-Query-System-Int32,System-Single,UnityEngine-Vector3[],UnityEngine-Vector3[]-'></a>
### Query(i_ownerHash,i_minSpatialLength,i_queryPoints,o_resultVels) `method`

##### Summary

Query water flow data (horizontal motion) at a set of points.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| i_ownerHash | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | Unique ID for calling code. Typically acquired by calling GetHashCode(). |
| i_minSpatialLength | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The min spatial length of the object, such as the width of a boat. Useful for filtering out detail when not needed. Set to 0 to get full available detail. |
| i_queryPoints | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | The world space points that will be queried. |
| o_resultVels | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | Water surface flow velocities at the query positions. |

<a name='M-Crest-IFlowProvider-RetrieveSucceeded-System-Int32-'></a>
### RetrieveSucceeded() `method`

##### Summary

Check if query results could be retrieved successfully using return code from Query() function

##### Parameters

This method has no parameters.

<a name='M-Crest-IFlowProvider-UpdateQueries'></a>
### UpdateQueries() `method`

##### Summary

Per frame update callback

##### Parameters

This method has no parameters.

<a name='T-Crest-IPropertyWrapper'></a>
## IPropertyWrapper `type`

##### Namespace

Crest

##### Summary

Unified interface for setting properties on both materials and material property blocks

<a name='T-Crest-ITimeProvider'></a>
## ITimeProvider `type`

##### Namespace

Crest

##### Summary

Base class for scripts that provide the time to the ocean system. See derived classes for examples.

<a name='T-Crest-LodDataMgr'></a>
## LodDataMgr `type`

##### Namespace

Crest

##### Summary

Base class for data/behaviours created on each LOD.

<a name='T-Crest-LodDataMgrAnimWaves'></a>
## LodDataMgrAnimWaves `type`

##### Namespace

Crest

##### Summary

Captures waves/shape that is drawn kinematically - there is no frame-to-frame state. The Gerstner
 waves are drawn in this way. There are two special features of this particular LodData.

  * A combine pass is done which combines downwards from low detail LODs down into the high detail LODs (see OceanScheduler).
  * The textures from this LodData are passed to the ocean material when the surface is drawn (by OceanChunkRenderer).
  * LodDataDynamicWaves adds its results into this LodData. The dynamic waves piggy back off the combine
    pass and subsequent assignment to the ocean material (see OceanScheduler).

 The RGB channels are the XYZ displacement from a rest plane at sea level to the corresponding displaced position on the
 surface. The A channel holds the variance/energy in all the smaller wavelengths that are too small to go into the cascade
 slice. This is used as a statistical measure for the missing waves and is used to ensure foam is generated everywhere.

<a name='F-Crest-LodDataMgrAnimWaves-_shapeCombinePass'></a>
### _shapeCombinePass `constants`

##### Summary

Turn shape combine pass on/off. Debug only - ifdef'd out in standalone

<a name='M-Crest-LodDataMgrAnimWaves-SuggestDataLOD-UnityEngine-Rect-'></a>
### SuggestDataLOD() `method`

##### Summary

Returns index of lod that completely covers the sample area, and contains wavelengths that repeat no more than twice across the smaller
spatial length. If no such lod available, returns -1. This means high frequency wavelengths are filtered out, and the lod index can
be used for each sample in the sample area.

##### Parameters

This method has no parameters.

<a name='T-Crest-LodDataMgrClipSurface'></a>
## LodDataMgrClipSurface `type`

##### Namespace

Crest

##### Summary

Drives ocean surface clipping (carving holes). 0-1 values, surface clipped when > 0.5.

<a name='T-Crest-LodDataMgrDynWaves'></a>
## LodDataMgrDynWaves `type`

##### Namespace

Crest

##### Summary

A dynamic shape simulation that moves around with a displacement LOD.

<a name='T-Crest-LodDataMgrFlow'></a>
## LodDataMgrFlow `type`

##### Namespace

Crest

##### Summary

A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.

<a name='T-Crest-LodDataMgrFoam'></a>
## LodDataMgrFoam `type`

##### Namespace

Crest

##### Summary

A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.

<a name='T-Crest-LodDataMgrPersistent'></a>
## LodDataMgrPersistent `type`

##### Namespace

Crest

##### Summary

A persistent simulation that moves around with a displacement LOD.

<a name='M-Crest-LodDataMgrPersistent-SetAdditionalSimParams-Crest-IPropertyWrapper-'></a>
### SetAdditionalSimParams() `method`

##### Summary

Set any sim-specific shader params.

##### Parameters

This method has no parameters.

<a name='T-Crest-LodDataMgrSeaFloorDepth'></a>
## LodDataMgrSeaFloorDepth `type`

##### Namespace

Crest

##### Summary

Renders depth of the ocean (height of sea level above ocean floor), by rendering the relative height of tagged objects from top down.

<a name='T-Crest-LodDataMgrShadow'></a>
## LodDataMgrShadow `type`

##### Namespace

Crest

##### Summary

Stores shadowing data to use during ocean shading. Shadowing is persistent and supports sampling across
many frames and jittered sampling for (very) soft shadows.

<a name='T-Crest-LodTransform'></a>
## LodTransform `type`

##### Namespace

Crest

##### Summary

This script is attached to the parent GameObject of each LOD. It provides helper functionality related to each LOD.

<a name='T-Crest-MultiPropertyAttribute'></a>
## MultiPropertyAttribute `type`

##### Namespace

Crest

<a name='M-Crest-MultiPropertyAttribute-BuildLabel-UnityEngine-GUIContent-'></a>
### BuildLabel() `method`

##### Summary

Override this method to customise the label.

##### Parameters

This method has no parameters.

<a name='M-Crest-MultiPropertyAttribute-GetPropertyHeight-UnityEditor-SerializedProperty,UnityEngine-GUIContent-'></a>
### GetPropertyHeight() `method`

##### Summary

Override this method to specify how tall the GUI for this field is in pixels.

##### Parameters

This method has no parameters.

<a name='M-Crest-MultiPropertyAttribute-OnGUI-UnityEngine-Rect,UnityEditor-SerializedProperty,UnityEngine-GUIContent,Crest-EditorHelpers-MultiPropertyDrawer-'></a>
### OnGUI() `method`

##### Summary

Override this method to make your own IMGUI based GUI for the property.

##### Parameters

This method has no parameters.

<a name='T-Crest-ObjectWaterInteraction'></a>
## ObjectWaterInteraction `type`

##### Namespace

Crest

##### Summary

Drives object/water interaction - sets parameters each frame on material that renders into the dynamic wave sim.

<a name='T-Crest-OceanBuilder'></a>
## OceanBuilder `type`

##### Namespace

Crest

##### Summary

Instantiates all the ocean geometry, as a set of tiles.

<a name='T-Crest-OceanChunkRenderer'></a>
## OceanChunkRenderer `type`

##### Namespace

Crest

##### Summary

Sets shader parameters for each geometry tile/chunk.

<a name='T-Crest-OceanDepthCache'></a>
## OceanDepthCache `type`

##### Namespace

Crest

##### Summary

Renders terrain height / ocean depth once into a render target to cache this off and avoid rendering it every frame.
This should be used for static geometry, dynamic objects should be tagged with the Render Ocean Depth component.

<a name='T-Crest-OceanPlanarReflection'></a>
## OceanPlanarReflection `type`

##### Namespace

Crest

##### Summary

Attach to a camera to generate a reflection texture which can be sampled in the ocean shader.

<a name='F-Crest-OceanPlanarReflection-RefreshPerFrames'></a>
### RefreshPerFrames `constants`

##### Summary

Refresh reflection every x frames(1-every frame)

<a name='F-Crest-OceanPlanarReflection-_frameRefreshOffset'></a>
### _frameRefreshOffset `constants`

##### Summary

To relax OceanPlanarReflection refresh to different frames need to set different values for each script

<a name='M-Crest-OceanPlanarReflection-ForceDistanceCulling-System-Single-'></a>
### ForceDistanceCulling(farClipPlane) `method`

##### Summary

Limit render distance for reflection camera for first 32 layers

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| farClipPlane | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | reflection far clip distance |

<a name='T-Crest-OceanRenderer'></a>
## OceanRenderer `type`

##### Namespace

Crest

##### Summary

The main script for the ocean system. Attach this to a GameObject to create an ocean. This script initializes the various data types and systems
and moves/scales the ocean based on the viewpoint. It also hosts a number of global settings that can be tweaked here.

<a name='P-Crest-OceanRenderer-CollisionProvider'></a>
### CollisionProvider `property`

##### Summary

Provides ocean shape to CPU.

<a name='P-Crest-OceanRenderer-CurrentLodCount'></a>
### CurrentLodCount `property`

##### Summary

The number of LODs/scales that the ocean is currently using.

<a name='P-Crest-OceanRenderer-MaxHorizDisplacement'></a>
### MaxHorizDisplacement `property`

##### Summary

The maximum horizontal distance that the shape scripts are displacing the shape.

<a name='P-Crest-OceanRenderer-MaxVertDisplacement'></a>
### MaxVertDisplacement `property`

##### Summary

The maximum height that the shape scripts are displacing the shape.

<a name='P-Crest-OceanRenderer-Scale'></a>
### Scale `property`

##### Summary

Current ocean scale (changes with viewer altitude).

<a name='P-Crest-OceanRenderer-ScaleCouldDecrease'></a>
### ScaleCouldDecrease `property`

##### Summary

Could the ocean horizontal scale decrease (for e.g. if the viewpoint drops in altitude). Will be false if ocean already at minimum scale.

<a name='P-Crest-OceanRenderer-ScaleCouldIncrease'></a>
### ScaleCouldIncrease `property`

##### Summary

Could the ocean horizontal scale increase (for e.g. if the viewpoint gains altitude). Will be false if ocean already at maximum scale.

<a name='P-Crest-OceanRenderer-SeaLevel'></a>
### SeaLevel `property`

##### Summary

Sea level is given by y coordinate of GameObject with OceanRenderer script.

<a name='P-Crest-OceanRenderer-ViewerAltitudeLevelAlpha'></a>
### ViewerAltitudeLevelAlpha `property`

##### Summary

The ocean changes scale when viewer changes altitude, this gives the interpolation param between scales.

<a name='P-Crest-OceanRenderer-ViewerHeightAboveWater'></a>
### ViewerHeightAboveWater `property`

##### Summary

Vertical offset of camera vs water surface.

<a name='M-Crest-OceanRenderer-ReportMaxDisplacementFromShape-System-Single,System-Single,System-Single-'></a>
### ReportMaxDisplacementFromShape() `method`

##### Summary

User shape inputs can report in how far they might displace the shape horizontally and vertically. The max value is
saved here. Later the bounding boxes for the ocean tiles will be expanded to account for this potential displacement.

##### Parameters

This method has no parameters.

<a name='T-Crest-OceanWaveSpectrum'></a>
## OceanWaveSpectrum `type`

##### Namespace

Crest

##### Summary

Ocean shape representation - power values for each octave of wave components.

<a name='M-Crest-OceanWaveSpectrum-GenerateWaveData-System-Int32,System-Single[]@,System-Single[]@-'></a>
### GenerateWaveData() `method`

##### Summary

Samples spectrum to generate wave data. Wavelengths will be in ascending order.

##### Parameters

This method has no parameters.

<a name='T-Crest-OceanBuilder-PatchType'></a>
## PatchType `type`

##### Namespace

Crest.OceanBuilder

<a name='F-Crest-OceanBuilder-PatchType-Count'></a>
### Count `constants`

##### Summary

Number of patch types

<a name='F-Crest-OceanBuilder-PatchType-Fat'></a>
### Fat `constants`

##### Summary

Adds a full skirt all of the way around a patch

      -------------
      |  |  |  |  |
    1 -------------
      |  |  |  |  |
  z   -------------
      |  |  |  |  |
    0 -------------
      |  |  |  |  |
      -------------
         0     1
x

<a name='F-Crest-OceanBuilder-PatchType-FatX'></a>
### FatX `constants`

##### Summary

Adds a skirt on the right hand side of the patch

    1 ----------
      |  |  |  |
  z   ----------
      |  |  |  |
    0 ----------
      0     1
         x

<a name='F-Crest-OceanBuilder-PatchType-FatXOuter'></a>
### FatXOuter `constants`

##### Summary

Outer most side - this adds an extra skirt on the left hand side of the patch,
 which will point outwards and be extended to Zfar

    1 --------------------------------------------------------------------------------------
      |  |  |      |
  z   --------------------------------------------------------------------------------------
      |  |  |      |
    0 --------------------------------------------------------------------------------------
      0     1
         x

<a name='F-Crest-OceanBuilder-PatchType-FatXSlimZ'></a>
### FatXSlimZ `constants`

##### Summary

Adds a skirt on the right hand side of the patch, removes skirt from top

<a name='F-Crest-OceanBuilder-PatchType-FatXZ'></a>
### FatXZ `constants`

##### Summary

Adds skirts at the top and right sides of the patch

<a name='F-Crest-OceanBuilder-PatchType-FatXZOuter'></a>
### FatXZOuter `constants`

##### Summary

Adds skirts at the top and right sides of the patch and pushes them to horizon

<a name='F-Crest-OceanBuilder-PatchType-Interior'></a>
### Interior `constants`

##### Summary

Adds no skirt. Used in interior of highest detail LOD (0)

    1 -------
      |  |  |
  z   -------
      |  |  |
    0 -------
      0     1
         x

<a name='F-Crest-OceanBuilder-PatchType-SlimX'></a>
### SlimX `constants`

##### Summary

One less set of verts in x direction

<a name='F-Crest-OceanBuilder-PatchType-SlimXFatZ'></a>
### SlimXFatZ `constants`

##### Summary

One less set of verts in x direction, extra verts at start of z direction

      ----
      |  |
    1 ----
      |  |
  z   ----
      |  |
    0 ----
      0     1
         x

<a name='F-Crest-OceanBuilder-PatchType-SlimXZ'></a>
### SlimXZ `constants`

##### Summary

One less set of verts in both x and z directions

<a name='T-Crest-PredicatedFieldAttribute'></a>
## PredicatedFieldAttribute `type`

##### Namespace

Crest

<a name='M-Crest-PredicatedFieldAttribute-#ctor-System-String,System-Boolean,System-Int32-'></a>
### #ctor(propertyName,inverted,disableIfValueIs) `constructor`

##### Summary

The field with this attribute will be drawn enabled/disabled based on another field. For example can be used
to disable a field if a toggle is false. Limitation - conflicts with other property drawers such as Range().

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| propertyName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of the other property whose value dictates whether this field is enabled or not. |
| inverted | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Flip behaviour - for example disable if a bool field is set to true (instead of false). |
| disableIfValueIs | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | If the field has this value, disable the GUI (or enable if inverted is true). |

<a name='T-Crest-QueryBase'></a>
## QueryBase `type`

##### Namespace

Crest

##### Summary

Provides heights and other physical data about the water surface. Works by uploading query positions to GPU and computing
the data and then transferring back the results asynchronously. An exception to this is water surface velocities - these can
not be computed on the GPU and are instead computed on the CPU by retaining last frames' query results and computing finite diffs.

<a name='M-Crest-QueryBase-CalculateVelocities-System-Int32,UnityEngine-Vector3[]-'></a>
### CalculateVelocities() `method`

##### Summary

Compute time derivative of the displacements by calculating difference from last query. More complicated than it would seem - results
may not be available in one or both of the results, or the query locations in the array may change.

##### Parameters

This method has no parameters.

<a name='M-Crest-QueryBase-CompactQueryStorage'></a>
### CompactQueryStorage() `method`

##### Summary

Remove air bubbles from the query array. Currently this lazily just nukes all the registered
query IDs so they'll be recreated next time (generating garbage).

##### Parameters

This method has no parameters.

<a name='M-Crest-QueryBase-DataArrived-UnityEngine-Rendering-AsyncGPUReadbackRequest-'></a>
### DataArrived() `method`

##### Summary

Called when a compute buffer has been read back from the GPU to the CPU.

##### Parameters

This method has no parameters.

<a name='M-Crest-QueryBase-RemoveQueryPoints-System-Int32-'></a>
### RemoveQueryPoints() `method`

##### Summary

Signal that we're no longer servicing queries. Note this leaves an air bubble in the query buffer.

##### Parameters

This method has no parameters.

<a name='M-Crest-QueryBase-RetrieveResults-System-Int32,UnityEngine-Vector3[],System-Single[],UnityEngine-Vector3[]-'></a>
### RetrieveResults() `method`

##### Summary

Copy out displacements, heights, normals. Pass null if info is not required.

##### Parameters

This method has no parameters.

<a name='M-Crest-QueryBase-UpdateQueryPoints-System-Int32,System-Single,UnityEngine-Vector3[],UnityEngine-Vector3[]-'></a>
### UpdateQueryPoints() `method`

##### Summary

Takes a unique request ID and some world space XZ positions, and computes the displacement vector that lands at this position,
to a good approximation. The world space height of the water at that position is then SeaLevel + displacement.y.

##### Parameters

This method has no parameters.

<a name='T-Crest-QueryDisplacements'></a>
## QueryDisplacements `type`

##### Namespace

Crest

##### Summary

Samples water surface shape - displacement, height, normal, velocity.

<a name='T-Crest-QueryFlow'></a>
## QueryFlow `type`

##### Namespace

Crest

##### Summary

Samples horizontal motion of water volume

<a name='T-Crest-RayTraceHelper'></a>
## RayTraceHelper `type`

##### Namespace

Crest

##### Summary

Helper to trace a ray against the ocean surface, by sampling at a set of points along the ray and interpolating the
intersection location.

<a name='M-Crest-RayTraceHelper-#ctor-System-Single,System-Single-'></a>
### #ctor() `constructor`

##### Summary

Constructor. The length of the ray and the step size must be given here. The smaller the step size, the greater the accuracy.

##### Parameters

This constructor has no parameters.

<a name='M-Crest-RayTraceHelper-Init-UnityEngine-Vector3,UnityEngine-Vector3-'></a>
### Init(i_rayOrigin,i_rayDirection) `method`

##### Summary

Call this each frame to initialize the trace.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| i_rayOrigin | [UnityEngine.Vector3](#T-UnityEngine-Vector3 'UnityEngine.Vector3') | World space position of ray origin |
| i_rayDirection | [UnityEngine.Vector3](#T-UnityEngine-Vector3 'UnityEngine.Vector3') | World space ray direction |

<a name='M-Crest-RayTraceHelper-Trace-System-Single@-'></a>
### Trace(o_distance) `method`

##### Summary

Call this once each frame to do the query, after calling Init().

##### Returns

True if the results have come back from the GPU, and if the ray intersects the water surface.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| o_distance | [System.Single@](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single@ 'System.Single@') | The distance along the ray to the first intersection with the water surface. |

<a name='T-Crest-RegisterAnimWavesInput'></a>
## RegisterAnimWavesInput `type`

##### Namespace

Crest

##### Summary

Registers a custom input to the wave shape. Attach this GameObjects that you want to render into the displacmeent textures to affect ocean shape.

<a name='T-Crest-RegisterClipSurfaceInput'></a>
## RegisterClipSurfaceInput `type`

##### Namespace

Crest

##### Summary

Registers a custom input to the clip surface simulation. Attach this to GameObjects that you want to use to
clip the surface of the ocean.

<a name='T-Crest-RegisterDynWavesInput'></a>
## RegisterDynWavesInput `type`

##### Namespace

Crest

##### Summary

Registers a custom input to the dynamic wave simulation. Attach this GameObjects that you want to influence the sim to add ripples etc.

<a name='T-Crest-RegisterFlowInput'></a>
## RegisterFlowInput `type`

##### Namespace

Crest

##### Summary

Registers a custom input to the flow data. Attach this GameObjects that you want to influence the horizontal flow of the water volume.

<a name='T-Crest-RegisterFoamInput'></a>
## RegisterFoamInput `type`

##### Namespace

Crest

##### Summary

Registers a custom input to the foam simulation. Attach this GameObjects that you want to influence the foam simulation, such as depositing foam on the surface.

<a name='T-Crest-RegisterLodDataInputBase'></a>
## RegisterLodDataInputBase `type`

##### Namespace

Crest

##### Summary

Base class for scripts that register input to the various LOD data types.

<a name='T-Crest-RegisterLodDataInput`1'></a>
## RegisterLodDataInput\`1 `type`

##### Namespace

Crest

##### Summary

Registers input to a particular LOD data.

<a name='T-Crest-RegisterSeaFloorDepthInput'></a>
## RegisterSeaFloorDepthInput `type`

##### Namespace

Crest

##### Summary

Tags this object as an ocean depth provider. Renders depth every frame and should only be used for dynamic objects.
For static objects, use an Ocean Depth Cache.

<a name='T-Crest-RegisterShadowInput'></a>
## RegisterShadowInput `type`

##### Namespace

Crest

##### Summary

Registers a custom input for shadow data. Attach this to GameObjects that you want use to override shadows.

<a name='T-Crest-RenderAlphaOnSurface'></a>
## RenderAlphaOnSurface `type`

##### Namespace

Crest

##### Summary

Helper script for alpha geometry rendering on top of ocean surface. This is required to select the best
LOD and assign the shape texture to the material.

<a name='T-RenderWireFrame'></a>
## RenderWireFrame `type`

##### Namespace



##### Summary

Triggers the scene render to happen in wireframe. Unfortunately this currently affects the GUI elements as well.

<a name='T-Crest-SampleFlowHelper'></a>
## SampleFlowHelper `type`

##### Namespace

Crest

##### Summary

Helper to obtain the flow data (horizontal water motion) at a single location. This is not particularly efficient to sample a single height,
but is a fairly common case.

<a name='M-Crest-SampleFlowHelper-Init-UnityEngine-Vector3,System-Single-'></a>
### Init(i_queryPos,i_minLength) `method`

##### Summary

Call this to prime the sampling

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| i_queryPos | [UnityEngine.Vector3](#T-UnityEngine-Vector3 'UnityEngine.Vector3') | World space position to sample |
| i_minLength | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The smallest length scale you are interested in. If you are sampling data for boat physics,
pass in the boats width. Larger objects will filter out detailed flow information. |

<a name='M-Crest-SampleFlowHelper-Sample-UnityEngine-Vector2@-'></a>
### Sample() `method`

##### Summary

Call this to do the query. Can be called only once after Init().

##### Parameters

This method has no parameters.

<a name='T-Crest-SampleHeightHelper'></a>
## SampleHeightHelper `type`

##### Namespace

Crest

##### Summary

Helper to obtain the ocean surface height at a single location per frame. This is not particularly efficient to sample a single height,
but is a fairly common case.

<a name='M-Crest-SampleHeightHelper-Init-UnityEngine-Vector3,System-Single,System-Boolean,UnityEngine-Object-'></a>
### Init(i_queryPos,i_minLength,allowMultipleCallsPerFrame) `method`

##### Summary

Call this to prime the sampling. The SampleHeightHelper is good for one query per frame - if it is called multiple times in one frame
it will throw a warning. Calls from FixedUpdate are an exception to this - pass true as the last argument to disable the warning.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| i_queryPos | [UnityEngine.Vector3](#T-UnityEngine-Vector3 'UnityEngine.Vector3') | World space position to sample |
| i_minLength | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The smallest length scale you are interested in. If you are sampling data for boat physics,
pass in the boats width. Larger objects will ignore small wavelengths. |
| allowMultipleCallsPerFrame | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Pass true if calling from FixedUpdate(). This will omit a warning when there on multipled-FixedUpdate frames. |

<a name='M-Crest-SampleHeightHelper-Sample-System-Single@-'></a>
### Sample() `method`

##### Summary

Call this to do the query. Can be called only once after Init().

##### Parameters

This method has no parameters.

<a name='T-Crest-EditorHelpers-ScriptableObjectUtility'></a>
## ScriptableObjectUtility `type`

##### Namespace

Crest.EditorHelpers

<a name='M-Crest-EditorHelpers-ScriptableObjectUtility-CreateAt-System-Type,System-String-'></a>
### CreateAt() `method`

##### Summary

Create a scriptable object asset

##### Parameters

This method has no parameters.

<a name='M-Crest-EditorHelpers-ScriptableObjectUtility-CreateAt``1-System-String-'></a>
### CreateAt\`\`1() `method`

##### Summary

Create a scriptable object asset

##### Parameters

This method has no parameters.

<a name='T-Crest-QueryBase-SegmentRegistrar'></a>
## SegmentRegistrar `type`

##### Namespace

Crest.QueryBase

##### Summary

Holds information about all query points. Maps from unique hash code to position in point array.

<a name='T-Crest-QueryBase-SegmentRegistrarRingBuffer'></a>
## SegmentRegistrarRingBuffer `type`

##### Namespace

Crest.QueryBase

##### Summary

Since query results return asynchronously and may not return at all (in theory), we keep a ringbuffer
of the registrars of the last frames so that when data does come back it can be interpreted correctly.

<a name='T-Crest-ShapeGerstner'></a>
## ShapeGerstner `type`

##### Namespace

Crest

##### Summary

Gerstner ocean waves.

<a name='M-Crest-ShapeGerstner-MinWavelength-System-Int32-'></a>
### MinWavelength() `method`

##### Summary

Min wavelength for a cascade in the wave buffer. Does not depend on viewpoint.

##### Parameters

This method has no parameters.

<a name='T-Crest-ShapeGerstnerBatched'></a>
## ShapeGerstnerBatched `type`

##### Namespace

Crest

##### Summary

Support script for Gerstner wave ocean shapes.
Generates a number of batches of Gerstner waves.

<a name='M-Crest-ShapeGerstnerBatched-UpdateBatch-System-Int32,System-Int32,System-Int32,Crest-ShapeGerstnerBatched-GerstnerBatch-'></a>
### UpdateBatch() `method`

##### Summary

Computes Gerstner params for a set of waves, for the given lod idx. Writes shader data to the given property.
Returns number of wave components rendered in this batch.

##### Parameters

This method has no parameters.

<a name='T-Crest-ShapeGerstnerSplineHandling'></a>
## ShapeGerstnerSplineHandling `type`

##### Namespace

Crest

##### Summary

Generates mesh suitable for rendering gerstner waves from a spline

<a name='T-Crest-SimSettingsAnimatedWaves'></a>
## SimSettingsAnimatedWaves `type`

##### Namespace

Crest

<a name='M-Crest-SimSettingsAnimatedWaves-CreateCollisionProvider'></a>
### CreateCollisionProvider() `method`

##### Summary

Provides ocean shape to CPU.

##### Parameters

This method has no parameters.

<a name='T-Crest-SimSettingsBase'></a>
## SimSettingsBase `type`

##### Namespace

Crest

##### Summary

Base class for simulation settings.

<a name='T-Crest-SimpleFloatingObject'></a>
## SimpleFloatingObject `type`

##### Namespace

Crest

##### Summary

Applies simple approximation of buoyancy force - force based on submerged depth and torque based on alignment
to water normal.

<a name='M-Crest-SimpleFloatingObject-FixedUpdateOrientation-UnityEngine-Vector3-'></a>
### FixedUpdateOrientation() `method`

##### Summary

Align to water normal. One normal by default, but can use a separate normal based on boat length vs width. This gives
varying rotations based on boat dimensions.

##### Parameters

This method has no parameters.

<a name='T-Crest-SphereWaterInteraction'></a>
## SphereWaterInteraction `type`

##### Namespace

Crest

##### Summary

This script and associated shader approximate the interaction between a sphere and the water. Multiple
spheres can be used to model the interaction of a non-spherical shape.

<a name='T-Crest-Spline-Spline'></a>
## Spline `type`

##### Namespace

Crest.Spline

##### Summary

Simple spline object. Spline points are child gameobjects.

<a name='T-Crest-Spline-SplineInterpolation'></a>
## SplineInterpolation `type`

##### Namespace

Crest.Spline

##### Summary

Support functions for interpolating a spline

<a name='M-Crest-Spline-SplineInterpolation-GenerateCubicSplineHull-Crest-Spline-SplinePoint[],UnityEngine-Vector3[],System-Boolean-'></a>
### GenerateCubicSplineHull(splinePoints,splinePointsAndTangents) `method`

##### Summary

Takes user-placed spline points and generates an array of points and generates an array of positions and tangents
suitable for plugging into cubic interpolation

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| splinePoints | [Crest.Spline.SplinePoint[]](#T-Crest-Spline-SplinePoint[] 'Crest.Spline.SplinePoint[]') | Input user-placed spline positions |
| splinePointsAndTangents | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | Generated spline points and tangents |

<a name='M-Crest-Spline-SplineInterpolation-InterpolateCubicPosition-System-Single,UnityEngine-Vector3[],System-Single,UnityEngine-Vector3@-'></a>
### InterpolateCubicPosition(splinePointCount,splinePointsAndTangents,t,position) `method`

##### Summary

Cubic interpolation of spline points

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| splinePointCount | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | Number of user placed spline points (not including tangent points) |
| splinePointsAndTangents | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | The spline handle points and tangent points |
| t | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | 0-1 parameter along entire spline |
| position | [UnityEngine.Vector3@](#T-UnityEngine-Vector3@ 'UnityEngine.Vector3@') | Result position |

<a name='M-Crest-Spline-SplineInterpolation-InterpolateLinearPosition-UnityEngine-Vector3[],System-Single,UnityEngine-Vector3@-'></a>
### InterpolateLinearPosition(points,t,position) `method`

##### Summary

Linearly interpolate between spline points

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| points | [UnityEngine.Vector3[]](#T-UnityEngine-Vector3[] 'UnityEngine.Vector3[]') | The spline points |
| t | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | 0-1 parameter along entire spline |
| position | [UnityEngine.Vector3@](#T-UnityEngine-Vector3@ 'UnityEngine.Vector3@') | Result position |

<a name='T-Crest-Spline-SplinePoint'></a>
## SplinePoint `type`

##### Namespace

Crest.Spline

##### Summary

Spline point, intended to be child of Spline object

<a name='T-Crest-TimeProviderCustom'></a>
## TimeProviderCustom `type`

##### Namespace

Crest

##### Summary

This time provider fixes the ocean time at a custom value which is usable for testing/debugging.

<a name='T-Crest-TimeProviderDefault'></a>
## TimeProviderDefault `type`

##### Namespace

Crest

##### Summary

Default time provider - sets the ocean time to Unity's game time.

<a name='T-Crest-UnderwaterEffect'></a>
## UnderwaterEffect `type`

##### Namespace

Crest

##### Summary

Handles effects that need to track the water surface. Feeds in wave data and disables rendering when
not close to water.

<a name='T-Crest-VisualiseCollisionArea'></a>
## VisualiseCollisionArea `type`

##### Namespace

Crest

##### Summary

Debug draw crosses in an area around the GameObject on the water surface.

<a name='T-Crest-VisualiseRayTrace'></a>
## VisualiseRayTrace `type`

##### Namespace

Crest

##### Summary

Debug draw a line trace from this gameobjects position, in this gameobjects forward direction.

<a name='T-Crest-WaterBody'></a>
## WaterBody `type`

##### Namespace

Crest

##### Summary

Demarcates an AABB area where water is present in the world. If present, ocean tiles will be
culled if they don't overlap any WaterBody.
