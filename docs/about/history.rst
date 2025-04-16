
Release Notes
=============

.. Set section numbering and ToC depth for PDFs because Sphinx has bugs and limitations.

.. raw:: latex

   \setcounter{secnumdepth}{0}
   \addtocontents{toc}{\protect\setcounter{tocdepth}{0}}

4.22.1
------

Changed
^^^^^^^
.. bullet_list::

   -  Add game/scene to planar reflection camera name to better distinguish them

Fixed
^^^^^
.. bullet_list::

   -  Fix script compilation error when Gaia is installed on Unity 6+
   -  Fix CS0414 script warning on build
   -  Fix planar reflection "release target texture" exception
   -  Fix planar reflection capturing inverted geometry in edit mode
   -  Fix both scene and game view planar reflection camera executing, when only one should
   -  Fix several cases of "Screen position out of view frustum" when using planar reflections

   .. only:: hdrp

      -  Fix underwater not working in play mode when domain reload is enabled when entering play mode `[HDRP]`

4.22
----

Changed
^^^^^^^
.. bullet_list::

   -  Add *Gaia Pro VS* integration
   -  Add Wind Zone support (directional only)
   -  Add *Global Wind Direction* to WaterRenderer
   -  Add *Global Wind Turbulence* to WaterRenderer
   -  Add *Swell* preset to ShapeGerstner
   -  Add *Enable Render Queue Sorting* option to allow changing the render queue within the transparent pass.
      This is useful for some third-party integrations
   -  Add *Far Clip Plane* property to OceanDepthCache
   -  Add UnderwaterRenderer.AfterCopyMaterial event to allow easy underwater material changes via script
   -  No longer disable components silently if they fail validation
   -  Improve shifting origin handling for tiled textures (like foam)
   -  Add vertical axis support to shifting origin
   -  Make some more properties public
   -  Make Ocean-Underwater material the default

   .. only:: hdrp

      -  Validate that refraction is enabled if using transparency `[HDRP]`
      -  Validate that custom passes are enabled if using underwater `[HDRP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix "Screen position out of view frustum" with 2D scene view when planar reflections is active
   -  Fix PSSL shader compilation error for FFT waves
   -  Fix OceanDepthCache not working after disabling then enabling in builds
   -  Fix *Hide Depth Cache Cam* not applying immediately
   -  Fix OceanDepthCache's very short capture range (from 1,000 to 10,000 and now configurable)
   -  Fix effects being incorrect based on water depth when sea level is not zero
   -  Fix missing caustics when using shifting origin on mobile devices
   -  Fix tiled texture quality being dependent on *LOD Resolution*.
      They are now scaled by LOD scale.
      This may require changing your scale values for these textures to look as they did before
   -  Fix Spline.UpdateSpline not working in builds
   -  Fix wave spline initialization causing inconsistency between editor and builds

   .. only:: urp

      -  Fix Unity fog when using orthographic projection `[URP]`
      -  Fix Unity 6.1 "USE_FORWARD_PLUS" shader compiler warning  `[URP]`

   .. only:: hdrp or urp

      -  Fix errors when inspecting Shader Graph nodes `[HDRP] [URP]`


.. Trim PDF history
.. raw:: latex

   \iffalse


4.21.4
------

Fixed
^^^^^
.. bullet_list::

   -  Fix GUI freezing and other potential issues when rendering Crest to a render texture
   -  Fix NullReferenceException when Sea Floor Depth is disabled

   .. only:: hdrp

      -  Fix NullReferenceException from HDRP volume stack `[HDRP]`

   .. only:: urp

      -  Remove unused *Verify Opaque And Depth Textures Enabled* property `[URP]`

4.21.3
------

Fixed
^^^^^
.. bullet_list::

   -  Fix underwater rendering for Unity 6 `[HDRP]`

4.21.2
------

.. only:: urp

   Breaking
   ^^^^^^^^
   .. bullet_list::

      -  Remove foveated rendering support from 2022.3.
         Please use Unity 6 if you need this feature `[URP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix buoyancy behaving erratically in Unity 6

   .. only:: urp

      -  Fix shader compilation errors due to foveated rendering in 2022.3 `[URP]`

4.21.1
------

Fixed
^^^^^
.. bullet_list::

   -  Fix underwater rendering issues when using MSAA `[URP]`
   -  Fix "Graphics.CopyTexture called with mismatching texture sizes" error with underwater and `STPP` `[URP]`
   -  Fix underwater rendering issues with `STPP` when Dynamic Resolution is disabled on the camera (likely Unity bug) `[URP]`

4.21
----

Changed
^^^^^^^
.. bullet_list::

   -  Take mesh back-face into account when using the underwater Volume feature
   -  Scale water mesh extents with LOD count so it more likely reaches the horizon
   -  Remove delay in scale changes when not needed (no varied water level)
   -  Add LOD Bias and Maximum LOD Level overrides to Ocean Depth Cache
   -  Add gravity override to Ocean Renderer
   -  Expose float field for wave spectrum power values
   -  Update Dynamic Waves gravity multiplier tooltip to note that it can be a source of instability
   -  Guard against corrupting global settings
   -  Unify wave gravity implementation

   .. only:: hdrp

      -  Greatly improve water lighting response quality to Physically Based Sky `[HDRP]`

   .. only:: urp

      -  Warn about opaque down sampling outline `[URP]`
      -  Support non-uniform foveated rendering (untested) `[URP]`
      -  Support additional lights for Forward+ `[URP]`
      -  Support rendering layers for additional lights `[URP]`
      -  Update warning about SSAO/Depth Priming bug to include new fixed version 2021.3.45f1 `[URP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix artifacts (bright spots) at the horizon when using MSAA
   -  Fix "SampleHeightHelper.Init() called multiple times in one frame" warning
   -  Fix orphaned Ocean Depth Cache component on undo
   -  Fix Sphere Water Interaction spike in OnEnable
   -  Fix Sphere Water Interaction not working correctly with frame rates outside of the set simulation frequency
   -  Fix "There are too many instances of SphereWaterInteraction. A maximum of 1023 instances is supported" warning
   -  Fix wave spectrum parameters missing when editing from a ShapeGerstnerBatched
   -  Fix rendering issues (black) and exceptions when Physics.gravity is zero
   -  Fix exception in builds if no wave spectrum is set
   -  Fix Ocean Depth Cache issues with Android by using more compatible texture format
   -  Fix Ocean Depth Cache baked texture issues with Android (2021.3+ only)
   -  Fix potentially missing example scene lighting for Unity 6

   .. only:: birp

      -  Fix underwater lighting being different when using color temperature or linear lighting `[BIRP]`

   .. only:: hdrp

      -  Fix "Object.FindFirstObjectByType" missing script compilation errors `[HDRP]`

   .. only:: urp

      -  Fix obsolete warnings for Unity 6 `[URP]`
      -  Fix "spherical culling" errors in Unity 6 when using OceanPlanarReflections `[URP]`
      -  Fix errors on certain Unity versions when using OceanDepthCache `[URP]`

.. only:: urp

   Performance
   ^^^^^^^^^^^
   .. bullet_list::

      -  No longer multi-sample depth for 2022.3+, as Unity now handles this correctly `[URP]`

4.20.1
------

Changed
^^^^^^^
-  Relax *Lod Resolution* being divisible by 128 requirement to divisible by 16.

Fixed
^^^^^
-  Fix "'FindObjectsByType' does not exist in the current context" etc errors by reverting "Use optimized FindObject(s) methods" due to no way to conditionally compile them.
   Unity 6+ will keep these changes.
-  Fix spurious format changes by reverting "Improve texture format compatibility".
   Add CREST_VERIFYRANDOMWRITESUPPORT symbol to keep these changes but not recommended.
-  Fix *Ocean Depth Cache* not working on Windows when switched to Android build target.
-  Fix *Surface Type > Opaque* missing scattering colors. `[HDRP]`
-  Fix water rendering over volumetric clouds when viewed from above (requires Unity 6+).
   This required adding a refraction model which has an overhead.
   Set *Refraction Model* to *Planar/Box* otherwise it is recommended to keep it as *None*. `[HDRP]`
-  Fix "Warn about *SSAO* and *Depth Priming* bug" warning on fixed Unity versions. `[URP]`

4.20
----

Changed
^^^^^^^
-  Add Unity 6 beta support.
-  Improve documentation.
-  Remove "Preview" from everything.
-  Improve *Lakes and Rivers* scene.
-  Make several *Underwater Renderer* fields public.
-  Add *Filter By Wavelength* and *Render After Dynamic Waves* options to *Register Anim Waves Input*.
   When both are disabled it will render to all LODs and before *Dynamic Waves*.
   Useful for scaling waves without affecting ripples and wakes.
-  Add feathering to spline inputs.
-  Add basic Unity Splines integration to generate our spline from theirs.
-  Add *Spline* API (*Spline.UpdateSpline* and getters/setters) for updating splines in editor and standalone.
-  Validate that *Lod Data Resolution* is divisible by 128 which is the smallest interval that is robust enough.
-  Clarify *Lod Data Resolution* tooltip about 128 intervals.
-  Improve texture format compatibility.
-  Remove random write access requirement for Shadow and Clip Surface Lod render texture.
-  Rename *Floating Origin* to *Shifting Origin*.
   If you see "Overriding existing asset with new GUID from package" warning, this is expected.
-  *Ocean Planar Reflections* now work in edit mode. `[BIRP] [URP]`
-  Make *Skybox* the default *Clear Flags* for *Ocean Planar Reflections* as this is more compatible with custom shaders. `[BIRP] [URP]`
-  Warn if Clear Flags on *Ocean Planar Reflections* is not Skybox and inform that it is more compatible with custom shaders. `[BIRP] [URP]`
-  Alert users if they have the wrong `Crest` package downloaded for their Unity version. `[HDRP]`
-  Add support for Render Graph in Unity 6 beta. `[URP]`

Fixed
^^^^^
-  Fix *Underwater Renderer* overwriting alpha channel which prevented XR passthrough from working.
-  Fix underwater objects being refracted when they are in front of the surface.
-  Fix larger wavelengths ignoring attenuation when using *Wave Resolution Multiplier*.
-  Fix texture "random writes" format error for *Shape FFT* on some devices.
   Requires Unity 2021.3+.
-  Fix possible texture "random writes" format error for LOD textures on some devices.
   Requires Unity 2021.3+.
-  Fix water chunks being incorrectly culled (ie missing) at screen edges.
-  Fix many cases where splines would not update in the editor.
-  Validate that height data is enabled when using height inputs.
-  Fix *Sphere Water Interaction* debug line orientation.
-  Fix banding in *Examples* scene.
-  Fix seams and double flow in *Lakes and Rivers* scene.
-  Fix "Failed to create Physics Mesh from source mesh" in 2023.3.
-  Silence several warnings.
-  Fix underwater multi-view rendering (primarily *Meta Quest*). `[BIRP]`
-  Fix *Depth Fog Density Factor* not supporting *Shader API*. `[BIRP]`
-  Fix *Depth Fog Density Factor* not supporting *Portals & Volumes* feature. `[BIRP]`
-  Fix *Shader API* not apply shadow scattering to underside of surface. `[BIRP]`
-  Fix underwater surface and volume having visibly different colors when using gamma color space and *Underwater Renderer > Shader API*. `[BIRP]`
-  Fix water being affected by `SAO` and other effects by setting Render Type to Transparent.
-  Fix reflected waves example rendering issues in *Examples* scene.
-  Fix *Underwater Effect* not working. `[BIRP] [URP]`
-  Fix refraction artifacts causing an outline/ghosting like effect for underwater objects. `[BIRP] [URP]`
-  Fix underwater colors being incorrect when using gamma color space. `[BIRP] [URP]`
-  Only warn about post-processing bug when using the bugged version.
   Recommend upgrading to fixed version. `[BIRP] [URP]`
-  Fix shader errors when enabling raytracing features. `[HDRP]`
-  Fix *Rendering Layer Mask* changes not applying immediately. `[HDRP]`
-  Throw an error if the wrong package version was downloaded for the Unity version. `[HDRP]`
-  Fix errors when searching the scene. `[HDRP] [URP]`
-  Fix additional lights subsurface scattering not rendering correctly. `[URP]`
-  Fix broken rendering with *Depth Texture Mode > Depth After Transparent*. `[URP]`
-  Fix broken rendering when *Depth Texture* is disabled. `[URP]`
-  Fix broken rendering when *Opaque Texture* is disabled. `[URP]`
-  Warn about *SSAO* and *Depth Priming* bug. `[URP]`

Removed
^^^^^^^
-  Remove row boat sample.

Performance
^^^^^^^^^^^
-  Improve *Spline* editor performance.
-  Improve per frame script performance by time slicing chunk bounds updates.


4.19.1
------

Fixed
^^^^^
-  Fix shader compilation error for 2022.3. `[HDRP]`


4.19
----

Changed
^^^^^^^
-  Add Unity 2023.1 support.
-  Add *Wave Resolution Multiplier* to *Animated Waves* settings which improves wave quality at high resolutions (eg 768 *LOD Data Resolution* with *Wave Resolution Multiplier* of 2).
-  Add *Filter Waves* parameter to foam settings to filter lower wavelengths from foam contribution which can help with too much foam underneath camera.
   Used in the *Main* example scene.
-  Remove validation warnings for multiple *Rigidbody* components found in children of floating objects (eg *Boat Probes*).
-  Remove unused files. `[HDRP] [URP]`

Fixed
^^^^^
-  Silence several obsolete warnings for Unity 2023.1.
-  Fix non ping-pong combine pass (compute) for iOS and possibly other platforms.
-  Fix *Shape Gerstner* for iOS and possibly other platforms.
-  Fix clip surface affecting extents causing popping.
-  Fix clip surface inputs getting stuck near the edge of the last LOD.
-  Fix shader compiler errors for consoles.
-  Fix *Dynamic Waves* reflected waves added at LOD boundaries on camera movement.
   Reverts *"Better utilise Dynamic Waves data textures by removing soft border"*.
-  Fix exceptions/crash in headless/server environment.
-  Fix "#ifdef UNITY_EDITOR … serialized properties" warning in standalone.
-  Fix broken rendering and errors spamming console caused by Unity bug on macOS in sample scenes. `[URP]`

Performance
^^^^^^^^^^^
-  Use optimized FindObject(s) methods.


4.18
----

Breaking
^^^^^^^^
-  Add *Crest.Examples* namespace to all example scripts.

Changed
^^^^^^^
-  Better utilise *Dynamic Waves* data textures by removing soft border.
-  Add *Terrain Pixel Error Override* to *Ocean Depth Cache*.
-  Add *ReflectionCamera* script property to *OceanPlanarReflections* for those who need a reference. `[BIRP] [URP]`
-  Disable water mesh for *Ocean Planar Reflections* preventing possible user error of including water in reflections leading to intersections. `[BIRP] [URP]`

Fixed
^^^^^
-  Fix *ShapeFFT* memory leak when using the default spectrum (no spectrum set to property).
-  Fix script compilation error when Unity's input system is enabled but the package is not installed.
-  Fix *Dynamic Waves* streaking on camera movement.
-  Fix terrains having low precision in *Ocean Depth Cache* in 2022.2+.
-  Fix *Underwater Renderer* not working if enabled after the *Ocean Renderer* or the *Ocean Renderer* was disabled/enabled. `[HDRP]`
-  Fix underwater surface appearance not having correct reflection coefficient. `[HDRP]`
-  Fix reflections for Forward+. `[URP]`
-  Fix shader warnings on build when using Forward+ by disabling additional lights (unsupported). `[URP]`
-  Fix some properties on *Ocean Planar Reflections* not updating after being changed. `[BIRP] [URP]`
-  Fix lens flare appearing in scene view in sample scenes. `[HDRP] [URP]`

Performance
^^^^^^^^^^^
-  Minor CPU performance improvements.
-  Disable water mesh for planar reflections camera skipping culling entirely. `[BIRP] [URP]`
-  Skip sampling normal maps if *Normals Strength* is zero. `[HDRP]`


4.17.3
------

Fixed
^^^^^
-  Fix *OnEnable* being triggered twice for *Crest* components in play mode in the editor when *Scene Reload* is enabled.
   This potentially caused problems with some components.
-  Fix certain components like *Underwater Renderer* and *Ocean Depth Cache* not working in play mode (2021.2+ only). `[HDRP]`
-  Fix *Underwater Renderer* not working after *Ocean Renderer* is disabled and then enabled (2021.2+ only). `[HDRP]`
-  Fix *Shadow Simulation* not working after *Ocean Renderer* is disabled and then enabled (2021.2+ only). `[HDRP]`


4.17.2
------

Fixed
^^^^^
-  Fix *Underwater Renderer* breaking in editor randomly.


4.17.1
------

Fixed
^^^^^
-  Fix "Unknown Error" shader compilation error. `[HDRP]`
-  Fix black edge where water intersects surface in Examples scene. `[HDRP]`


4.17
----

Changed
^^^^^^^
-  Reorganise documentation to make things easier to find.
-  Reduce *Water Body* material override feature leaking outside of water bodies.
-  No longer execute when editor is inactive (ie out of focus) to prevent edge cases where memory leaks can occur and to save energy.
-  Improve *Water Body* gizmo by adding a wireframe.
-  Use *Register Height Input* in *Boat* scene instead of *Register Animated Waves Input*.
-  Rate limit shadow simulation to *Ocean Renderer > Editor Mode FPS*.
-  Move *Ocean Renderer* debug options into foldout.
-  Release *Ocean Renderer* resources in *OnDestroy* instead of *OnDisable* to prevent performance penality of rebuilding the system.
   The option *Debug > Destroy Resources In On Disable* will revert this behaviour if needed.
-  Make *Ocean Depth Cache* depth relative.
   This benefits baked depth caches by allowing them to be moved after baking providing the contents are moved with it.
-  Add *Update Saved Cache File* button to *Ocean Depth Cache*.
-  Automatically set *Ocean Depth Cache* to *Baked* and set texture after baking.
-  Show `Crest` version on *Ocean Renderer*.
-  Add helpbox to *Shifting Origin* directing users to documentation for solving potential popping issues.
-  Improve spacing for spectrum power slider labels.
-  Ramp planar reflection distortion with distance using the new *Planar Reflections Distortion Distance Factor* material property. `[BIRP] [URP]`

Fixed
^^^^^
-  Reduce `GC` allocations when using *ShapeFFT* or *ShapeGerstner*.
   To not have per frame `GC` allocations, ensure *Spectrum Fixed At Runtime* is enabled.
-  Remove or reduce several runtime `GC` allocations.
-  Remove several editor `GC` allocations.
-  Fix culling and performance issues in edit mode when using RegisterHeightInput, RegisterAnimWavesInput or Whirlpool.
-  Fix gizmos not drawing for inputs when using an attached renderer.
-  Fix potential cases where water tiles were being culled incorrectly.
-  Fix *Sphere Water Interaction* not working in builds.
-  Fix larger waves not blending out when using wave blending.
-  Fix "shader_feature keyword '\\' is not started with a letter or underscore, ignoring the whole line." shader compilation warning. `[BIRP]`
-  Actually fix "shadow simulation executing for all cameras". `[BIRP]`
-  Fix scene camera "CopyTexture" errors and warnings when using PPv2 with *Underwater Renderer*. `[BIRP]`
-  Fix *Scatter Colour Shadow* only having a minimal effect and/or causing an outline in shadowed areas. `[HDRP]`
-  Fix motion vectors popping when camera height changes. `[HDRP]`
-  Fix motion vectors popping on first frame. `[HDRP]`
-  Fix *Ocean* *Shader Graph* features (eg shadows) from jittering on camera move for Unity 2021.2+. `[HDRP]`
-  Fix *Underwater Renderer* compatibility with depth prepass. `[URP]`
-  Fix *Underwater Renderer* not working with multiple cameras in certain cases. `[URP]`
-  Fix rendering artifacts when *Windows Graphics API* is set to *Direct3D11* and the *Android Graphics API* is set to *Vulkan*. `[URP]`
-  Fix *Ocean Planar Reflections* capturing reflections from only one viewpoint when used with multiple cameras in builds. `[URP]`
-  Fix shadow simulation breaking cameras that use *StereoTargetEyeMask* when XR `SPI` is enabled. `[URP]`
-  Check correct `URP` asset when doing validation to prevent possible exceptions or erroneous validation. `[URP]`
-  Fix shader compilation errors from `BIRP` shaders being previously included in package. `[HDRP] [URP]`
-  Fix Ocean material texture properties not binding on some platforms (PS5). `[BIRP] [URP]`

Performance
^^^^^^^^^^^
-  Improve water tile culling significantly.
   The bounds for each tile are normally expanded to accommodate mesh displacement (to prevent culling), but they were much larger than required in many cases leading to reduced culling hits which is no longer the case.
-  Reduce the amount of displacement queries LOD inputs make significantly making performance more scalable.
-  Optimise LOD inputs cost per frame when used with a *Renderer*.
-  Minor performance optimisations.


4.16
----

Breaking
^^^^^^^^
-  Set minimum Unity version to 2020.3.40.
-  Set minimum render pipeline package version to 10.10. `[HDRP] [URP]`

Changed
^^^^^^^
-  Add support for multiple cameras to the *Underwater Renderer*.
   One limitation is that underwater culling will be disabled when using multiple *Underwater Renderer*\ s.
-  ShapeFFT/Gerstner can now take a mesh renderer as an input.
-  Add *Crest/Inputs/Shape Waves/Sample Spectrum* shader which samples the spectrum using a texture.
-  Ocean inputs provided via the *Register* components now sort on sibling index in addition to queue, so multiple inputs with the same queue can be organised in the hierarchy to control sort order.
-  Add ability to alpha blend waves (effectively an override) instead of only having additive blend waves.
   Set *Blend Mode* to *Alpha Blend* on the *ShapeFFT* or *ShapeGerstner* to use.
   It's useful for preventing rivers and lakes from receiving ocean waves.
-  Add *Water Tile Prefab* field to *Ocean Renderer* to provide more control over water tile mesh renderers like reflection probes settings.
-  Warn users that edits in prefab mode will not be reflected in scene view until prefab is saved.
-  Validate that no scale can be applied to the *OceanRenderer*.
-  Viewpoint validation has been removed as it was unnecessary and spammed the logs.
-  Whirlpool now executes in edit mode.
-  *Visualise Ray Trace* now executes in edit mode.
-  *Render Alpha On Surface* now executes in edit mode.
-  Only report no Shape component validation as help boxes (ie no more console logs).
-  Remove outdated lighting validation.
-  Validate layers to warn users of potential build failures if `Crest` related renderers are not on the same layer as the *OceanRenderer.Layer*.
-  No longer log info level validation to the console.
-  Add info validation for tips on using reflection probes when found in a scene.
-  Set *Ocean Renderer* *Wind Speed* default value to the maxmimum to reduce UX friction for new users.
-  Also search *Addressables* and *Resources* for ocean materials when stripping keywords from underwater shader.
-  Add *Ocean Renderer > Extents Size Multiplier* to adjust the extents so they can be increased in size to meet the horizon in cases where they do not.
-  Greatly improve performance when many SphereWaterInteraction components are used by utilising GPU Instancing.
-  Improve example scenes.
-  Improve *Ocean Depth Cache* capture performance by excluding all render features. `[URP]`

Fixed
^^^^^
-  Fix FFTs incorrectly adding extra foam.
-  Limit minimum phase period of flow technique applied to waves to fix objectionable phasing issues in flowing water like rivers.
-  Fix some components breaking in edit mode after entering/exiting prefab mode.
-  Fix *Build Processor* deprecated/obsolete warnings.
-  Fix spurious "headless/batch mode" error during builds.
-  Greatly improve spline performance in the editor.
-  Fix PSSL compiler errors.
-  Fix incompatibility with EasySave3 and similar assets where water tiles would be orphaned when exiting play mode.
-  Fix ocean tiles being pickable in the editor.
-  Fix several memory leaks.
-  Fix *Sea Floor Depth Data* disabled state as it was still attenuating waves when disabled.
-  No longer execute when building which caused several issues.
-  Fix self-intersecting polygon (and warning) on Ferry model.
-  Fix *Examples* scene UI not scaling and thus looking incorrect for non 4K resolution.
-  Fix build failure for *main* scene if reflection probe is added that excluded the *Water* layer.
-  Prevent bad values (NaN etc) from propagating in the *Dynamic Waves* simulation.
   This manifested as the water surface disappearing from a singlar point.
-  Fix shader include path error when moving `Crest` folder from the standard location.
-  No longer disable the *Underwater Renderer* if it fails validation.
-  Fix *Underwater Curtain* lighting not matching the water surface causing a visible seam at the far plane. `[BIRP] [URP]`
-  Fix "mismatching output texture dimension" error when using XR `SPI`. `[BIRP] [URP]`
-  Fix caustics not rendering in XR `SPI` when shadow simulation is disabled. `[BIRP]`
-  Fix XR spectator camera breaking if shadow simulation enabled. `[BIRP]`
-  Fix shadow simulation executing for all cameras which could cause incorrect shadows. `[BIRP]`
-  Fix underwater effect not rendering properly if spectator camera is used with XR `SPI`. `[BIRP]`
-  Fix ocean moving in edit mode when *Always Refresh* is disabled. `[HDRP]`
-  Fix ocean not rendering if no active *Underwater Renderer* is present. `[HDRP]`
-  Fix *Clip Surface* adding negative alpha values when *Alpha Clipping* is disabled on the ocean material. `[HDRP]`
-  Fix *Sort Priority* on the ocean material not having an effect. `[HDRP]`
-  Improve performance by removing duplicated pass when using shadow simulation. `[HDRP]`
-  Improve XR `MP` performance by removing shadow copy pass from the right eye. `[HDRP]`
-  Fix Unity 2022.2 shader compilation errors. `[HDRP]`
-  Fix Unity 2023.1 script compilation errors. `[HDRP]`
-  Fix *Underwater Renderer* incompatibility with `SSAO`. `[URP]`
-  Fix Unity 2022.2 obsolete warnings. `[URP]`


4.15.2
------

Changed
^^^^^^^
-  Default FFT resolution increased to match quality standards.
-  FFT samples-per-wave now scales proportionally to FFT resolution, meaning overall quality scales gracefully with the resolution setting.
-  Re-enable height queries in edit-mode which allows several height based components to work in edit-mode.
   They can still be disabled with the new *Height Queries* toggle on the *Ocean Renderer*.

Fixed
^^^^^
-  Provide feedback on how to solve errors from *Sphere-Water Interaction* moving file locations.
-  Fix *Underwater Renderer* stereo rendering not working in builds for Unity 2021.2.
-  Fix *Underwater Renderer* stereo rendering issue where both eyes are same for color and/or depth with certain features enabled.
-  Fix stereo rendering for *Examples* scene.
-  Fix many memory/reference leaks.
-  Fix excessively long build times when no *Underwater Renderer* is present in scene.
-  Fix *Underwater Renderer* not working with varying water level.
-  Fix jagged shoreline foam when using baked *Sea Floor Depth* cache.
-  Fix color being incorrect for *Underwater Shader API*. `[BIRP]`
-  Fix ocean not rendering in builds for Unity 2021.2 if no *Underwater Renderer* is present. `[HDRP]`
-  Disable `SSAO` for *Examples* scene and warn users of incompatibility with *Portals and Volumes* feature. `[URP]`


4.15.1
------

Fixed
^^^^^
-  Fix shader compiler error.


4.15
----

Breaking
^^^^^^^^
-  Ocean inputs will now only execute the first shader pass (pass zero).
   Before all passes were executed in sequence which caused incompatibilities with `URP` unlit *Shader Graph*.
   This is only a concern to those who are using custom shaders with multiple passes which we believe is very few.

Preview
^^^^^^^
-  Add new CPU-based collision provider - *Baked FFT Data*.
-  Add portals and volumes to *Underwater Renderer* (affects both underwater and ocean surface).
   See :ref:`portals-volumes` for more information.
-  Add *Shader API* to *Underwater Renderer* to facilate adding underwater fog to transparent objects.
   See :ref:`underwater-shader-api` for more information.
-  Add *Albedo Data* feature which allows layering colour onto the water surface similar to decals.

Changed
^^^^^^^
-  Add new example scene named *Examples* which contains many mini examples of different features of `Crest`.
-  Add new example scene named *LakesAndRivers* for adding lakes and rivers using splines.
-  Add support for rendering in edit mode (camera preview and scene view) to *Underwater Renderer*.
   It can be enabled/disabled with the fog scene view toggle.
-  Add *CREST_OCEAN* scripting defines symbol.
-  Add *Depth Fog Density Factor* to *Underwater Renderer* which can be used to decrease underwater fog intensity when underwater.
   Greatly improves shadows at shorelines.
-  Add UV feathering option to Flow shaders.
-  Add *Attenuation in Shallows* to *Dynamic Waves Sims Settings*.
-  Add *Shallows Max Depth* to *Sim Settings Animated Waves* as an alternative to having to extend terrain to 500m below sea level to avoid discontinuity issues.
-  Add *Allow No Shadows* to *Sim Settings Shadows* to allow shadows to be enabled/disabled dynamically.
-  Add *Ocean Renderer >  Water Body Culling* option so the ocean can ignore culling.
   Useful if using *Water Body > Override Material* and still want an ocean.
-  Improve multiple *Water Body* overlapping case when *Water Body > Override Material* option is used.
-  Water Body adds an inclusion to clipping (ie unclips) if *Default Clipping State* is *Everything Clipped*.
-  Add *Underwater Renderer* support for *Water Body > Override Material*.
-  Add scroll bar to *Ocean Debug GUI* when using *Draw LOD Datas Actual Size*.
-  Add support for *TrailRenderer*, *LineRenderer* and *ParticleSystem* to be used as ocean inputs in addition to *MeshRenderer*.
-  Un-deprecate *ShapeGerstner* as it is useful in some situations for adding a small number of distinct waves with high degree of control.
-  Add *Reverse Wave Weight* setting to *ShapeGerstner* for fine control over generated wave pairs.
-  Double sample count for *ShapeGerstner* waves to improve quality.
-  Tidy up wave spectrum inspector by only showing *ShapeGerstner*-specific controls when editing within a *ShapeGerstner* component.
-  Add option (enabled by default) to prewarm foam simulation on load and camera teleports.
-  *Underwater Renderer* validates *Ocean Renderer* material.
-  Add *Debug > Draw Queries* to *Boat Probes* to draw gizmos for queries.
-  *SphereWaterInteraction* component upgraded to produce crisp foam-generating waves without creating large displacements. :pr:`979`
-  Add new example scene *BoatWakes* to showcase improvements to *SphereWaterInteraction* component.
-  Allow scaling FFT waves on spline (not supported previously). *SplinePointDataGerstner* has been renamed to *SplinePointDataWaves* which works for both *ShapeFFT* and *ShapeGerstner*.
-  Add *Surface Self-Intersection Fix Mode* (advanced option) to control how self-intersections of the ocean surface caused by intense/choppy waves are handled.
-  Add *Maximum Buoyancy Force* for preventing objects from having too much force being applied when fully submerged.
-  Updated all example scenes.
-  Unity 2021.2 users can now use the Shader Graph version of the ocean shader.
   The generated shader is deprecated and should not be used as it does not work correctly for 2021.2. `[HDRP]`
-  Add support for *Ray-Traced Reflections* for Unity 2021.2. `[HDRP]`
-  Revert to using Unity's material inspector which gives more control and is more reliable. `[HDRP]`
-  Improve ocean material inspector for Unity 2021.2. `[HDRP]`
-  Caustics and foam textures now use the sampler defined on the texure asset.
   If using our caustics texture, it will now use trilinear sampling instead of linear. `[HDRP]`
-  Add support for secondary lights like point or spot to ocean shader.
   Only supports pixel lights and not vertex lights. `[URP]`

Fixed
^^^^^
-  Fix incorrect baked depth cache data that were baked since `Crest` 4.14.
-  Fix XR `SPI` underwater rendering for Unity 2021.2 standalone.
-  Fix *Underwater Renderer* not rendering on *Intel iGPUs*.
-  Fix clip surface inputs losing accuracy with large waves.
-  Fix waves at shorelines being incorrectly shadowed. :pr:`945`
-  Fix shadow bleeding at shorelines by using the *Sea Floor Depth* data to reject invalid shadows. :pr:`947`
-  Fix exceptions thrown for server/headless builds.
-  Fix exceptions thrown if foam, dynamic waves and shadows all were disabled.
-  Fix *Shifting Origin* for *Shape Gerstner* and *Shape FFT*.
-  Fix ocean textures popping (normals, caustics etc) when *Shifting Origin* teleports.
-  Fix collision queries (eg buoyancy) popping when *Shifting Origin* teleports.
-  Fix ocean scale smoothing on first frame and teleports.
   This issue appears as the ocean detail being low and slowly becoming high detailed.
-  Fix shadow data not always clearing.
-  Fix shadow simulation not recovering after error being resolved in edit mode.
-  Fix *Allow Null Light* option on *Sim Settings Shadows* not working.
-  Fix ocean tiles not reverting to *Ocean Renderer > Material* if *Water Body > Override Material* was used and *Water Body* was disabled or removed.
-  Add *Time Scale* control for FFT (*Gravity* setting was broken).
-  Fix underwater rendering when the camera's culling mask excludes the *Ocean Renderer > Layer*.
-  Fix visible "rings" in dynamic wave sim resulting from fast moving objects that have the *Sphere Water Interaction* component attached.
   Simulation frequency can be increased to improve result further, at the cost of more simulation steps per frame.
-  Fix *Sphere Water Interaction* component not working in standalone builds.
-  Fix pop/discontinuity issue with dynamic waves.
-  Fix underwater culling when *Ocean Renderer > Viewpoint* is set and different from the camera.
-  Fix several minor exceptions in cases where components were not set up correctly.
-  Fix possible cases of underwater effect being inverted on self-intersecting waves when further than 2m from ocean surface.
-  Fix a per frame GC allocation.
-  Fix ocean input validation incorrectly reporting that there is no spline attached when game object is disabled.
-  Fix *Shape FFT* with zero weight causing visible changes or pops to the ocean surface.
-  Fix *Shape FFT* waves animating too quickly when two or more are in the scene with different resolutions.
-  Fix *Shape Gerstner* weight not updating correctly if less than one on game load.
-  Fix *Shape Gerstner* weight being applied twice instead of once.
   You may need to adjust your weight if between zero and one.
-  Fix Unity 2021.2 script upgrade requirement.
-  Fix compilation error if both `HDRP` and `URP` packages are installed.
-  Fix shadow simulation null exceptions if primary light becomes null. `[BIRP]`
-  Fix shadows flickering when *Sea Floor Depth* data is populated by preventing shadow passes from executing for *Ocean Depth Cache* camera. `[BIRP]`
-  Fix *Underwater Renderer* using a non directional light when a transparent object is in range of light and in view of camera. `[BIRP]`
-  Fix caustics not rendering if shadow data is disabled. `[BIRP]`
-  Fix *Underwater Renderer* looking washed out due to using incorrect colour space for Unity 2021.2. `[BIRP]`
-  Fix *Underwater Renderer* high memory usage by reverting change of using temporary render textures. `[BIRP] [URP]`
-  Fix *Underwater Renderer* not using *Filter Ocean Data* for caustics. `[BIRP] [URP]`
-  Fix ocean input incompatibilities with unlit *Shader Graph*. `[URP]`
-  Fix possible "Extensions" class naming collision compilation error. `[HDRP] [URP]`
-  Fix motion vectors not working by exposing motion vector toggle on ocean material. `[HDRP]`
-  Fix foam bubbles parallax effect using the incorrect normal space. `[HDRP]`
-  Fix foam bubbles texture scaling. `[HDRP]`

Performance
^^^^^^^^^^^
-  Reduce cost of populating the ocean depth cache. `[HDRP]`


4.14
----

Changed
^^^^^^^
-  Add *Dynamic Waves* reflections from *Ocean Depth Cache* geometry.
-  Add inverted option to *Clip Surface* signed-distance primitives and convex hulls which removes clipping.
-  Add *Override Material* field to the *Water Body* component to enable varying water material across water bodies.
-  *Sphere Water Interaction* component simplified - no mesh renderer/shader setup required, and no 'register' component required.
-  *Sphere Water Interaction* produces more consistent results at different radii/scales.
-  Improve `FFT` wave quality by doubling the sampling from two to four.
-  *RegisterHeightInput* can be used in conjunction with our *Spline* component to offset the water level.
   This can be used to create water bodies at different altitudes, and to create rivers that flow between them.
-  All water features updated to support varying water level.
-  Add buttons to *Spline* inspector to quickly enable water features.
-  Exposed control over *Spline* ribbon alignment - spline points now define the center of the ribbon by default.
-  Caustics no longer render in shadows casted from objects underwater.
-  Added motion vectors (for TAA, DLSS and many screen-space effects). `[HDRP]`
-  Added shadow distance fade to shadow data. `[URP]`
-  Improve `URP` shadow settings validation. `[URP]`

Fixed
^^^^^
-  Fix lines in foam data producing noticeable repeating patterns when using `FFT` waves.
-  Fix caustics jittering when far from zero and underwater in XR.
-  Fix disabled simulations' data being at maximum when "Texture Quality" is not "Full Res".
   In one case this manifested as the entire ocean being shadowed in builds.
-  Fix high CPU memory usage from underwater effect shader in builds.
-  Fix FFT spectrum not being editable when time is paused.
-  Fix *ShapeFFT* component producing inverted looking waves when enabled in editor play mode.
-  Fix SSS colour missing or popping in the distance.
-  Fix underwater artefacts (bright specks).
-  Fix shadows for MacOS. `[BIRP]`
-  Fix shadows for *Shadow Projection > Close Fit*. `[BIRP]`
-  Fix shadows for deferred rendering path. `[BIRP]`
-  Fix *Crest/Framework* shader compiler errors for 2021.2. `[URP]`
-  Fix "xrRendering" build error. `[URP]`
-  Fix *Default Clipping State > Everything Clipped* not clipping extents. `[HDRP]`
-  Fix Ocean shader compilation errors for `HDRP` 10.7. `[HDRP]`

Removed
^^^^^^^
-  Remove *Texels Per Wave* parameter from Ocean Renderer and hard-code to Nyquist limit as it is required for `FFT`\ s to work well.
-  Removed *Create Water Body* wizard window.
   The water body setup has been simplified and works without this additional tooling.
-  *Smoothing* feature removed from *Spline*, underlying code made more robust.
-  Remove *Assign Layer* component.

Performance
^^^^^^^^^^^
-  Only calculate inverse view projection matrix when required.
-  Reduce shader variants by removing GPU instancing (not supported currently).
-  Reduce shadow simulation GPU performance cost by almost 50%. `[BIRP] [HDRP]`
-  Improve *Underwater Renderer* GPU memory usage. `[BIRP] [URP]`
-  Reduce ocean shader GPU performance cost for shadows. `[HDRP]`

Deprecated
^^^^^^^^^^
-  Made *ObjectWaterInteraction* component obsolete, this is replaced by the more simple and robust *SphereWaterInteraction*. Removed usages of this component from the example scenes.
-  Made *ShapeGerstner* and *ShapeGerstnerBatched* components obsolete as they are replaced by the *ShapeFFT* component. Example scenes moved over to *ShapeFFT*.


4.13
----

Changed
^^^^^^^
-  Add signed-distance primitives for more accurate clipping and overlapping.
   See :ref:`clip-surface-section` for more information.
-  Add *Render Texture Graphics Format* option to *Clip Surface Sim Settings* to support even more accurate clipping for signed-distance primitives.
-  Add *Render Texture Graphics Format* option to *Animated Waves Sim Settings* to solve precision issues when using height inputs.
-  Always report displacement in *Register Height Input* to solve culling issues.
-  Add default textures to ocean shader.
-  Update ocean shader default values.
-  Improve foam detail at medium to long distance.
-  Add *Scale By Factor* shader for all inputs which is particularly useful when used with *Animated Waves* for reducing waves.
-  Add a simpler custom material inspector. `[HDRP]`
-  Add XR `SPI` support to *Underwater Renderer*. `[URP]`

Fixed
^^^^^
-  Fix ocean not rendering on Xbox One and Xbox Series X.
-  Fix height input (and others) from not working 100m above sea level and 500m below sea level.
-  Fix FFT shader build errors for Game Core platforms.
-  Fix FFT material allocations every frame.
-  Fix flow simulation sometimes not clearing after disabling last input.
-  Fix outline around objects when MSAA is enabled by making it less noticeable.
-  Fix pixelated looking foam bubbles at medium to long distance.
-  Fix underwater effect undershooting or overshooting ocean surface when XR camera is nearly aligned with horizon.
-  Fix underwater effect being flipped at certain camera orientations.
-  Fix meniscus thickness consistency (in some cases disappearing) with different camera orientations.
-  Fix inputs (eg keyboard) working when game view is not focused.
-  Fix *Ocean Depth Cache* disabling itself in edit mode when no ocean is present.
-  Fix ocean disappearing when viewed from an area clipped by a clip surface input. `[HDRP]`
-  Fix shadows breaking builds when XR package is present. `[HDRP]`
-  Fix shadows not working with XR `SPI`. `[HDRP]`
-  Fix 2021.2.0b9 shader compile errors. `[HDRP]`
-  Fix ocean material properties missing for 2021.2 material inspector. `[HDRP]`
-  Fix outline around refracted objects by making it less noticeable. `[HDRP]`
-  Fix *Underwater Renderer* caustics jittering for some XR devices. `[BIRP] [URP]`
-  Fix shadow artefacts when no shadow casters are within view. `[URP]`
-  Remove sample shadow scriptable render feature error. `[URP]`


4.12
----

Breaking
^^^^^^^^
-  Set minimum Unity version to 2020.3.10.
-  Set minimum render pipeline package version to 10.5. `[HDRP] [URP]`
-  *Underwater Post-Processing* is disabled by default which means it will be inactive if the *Underwater Volume Override* is not present in the scene. `[HDRP]`
-  Remove *Sample Shadows* Render Feature as it is now scripted.
   Unity will raise a missing Render Feature reference error.
   Remove the missing Render Feature to resolve. `[URP]`

Changed
^^^^^^^
-  Add new *Underwater Renderer* component which executes a fullscreen pass between transparent and post-processing pass.
   Please see :ref:`underwater` for more information.
-  FFT generator count added to debug GUI.
-  *ShapeFFT* component allows smooth changing of wind direction everywhere in world.
-  Default *Wind Speed* setting on *OceanRenderer* component to 10m/s.
-  *CustomTimeProvider* override time/delta time functions are now defaulted to opt-in instead of opt-out.
-  Improve meniscus rendering by also rendering below ocean surface line. `[HDRP]`

Fixed
^^^^^
-  Fix case where normal could be NaN, which could make screen flash black in `HDRP`.
-  Fix *ShapeFFT* *Spectrum Fixed At Runtime* option not working.
-  Fix shader compile errors on Windows 7.
-  Fix ocean depth cache shader compile error.
-  Fix ocean not rendering on *Unity Cloud Build* (unconfirmed).
-  Fix ShapeGerstner and ShapeFFT having no default spectrum in builds.
-  Fix "missing custom editor" error for *Whirlpool* component.
-  Fix ocean breaking after leaving a prefab scene.
-  Fix underwater breaking for XR `SPI`. `[HDRP]`
-  Fix underwater artefacts for XR `MP`. `[HDRP]`
-  Fix meniscus rendering incorrectly when camera is rotated. `[HDRP]`

Performance
^^^^^^^^^^^
-  FFT wave generation factored out so that multiple *ShapeFFT* components sharing the same settings will only run one FFT.
-  Underwater ocean mask now deactivates when the underwater effect is not active. `[HDRP]`

Deprecated
^^^^^^^^^^
-  The *Underwater Effect* component (including *UnderWaterCurtainGeom.prefab* and *UnderWaterMeniscus.prefab*) has been superseded by the *Underwater Renderer*.
   Please see :ref:`underwater` for more information. `[BIRP] [URP]`
-  The *Underwater Post-Process* effect has been superseded by the *Underwater Renderer*.
   Please see :ref:`underwater` for more information. `[HDRP]`


4.11
----

.. important::

   This will be the last version which supports Unity 2019 LTS.

   Spectrum data will be upgraded in this version.
   Due to a unity bug, in some rare cases upgrading the spectrum may fail and waves will be too large.
   Restart Unity to restore the spectrum.

Preview
^^^^^^^
-  `FFT` wave simulation added via new ShapeFFT component.

Changed
^^^^^^^
-  Sponsorship page launched!
   Asset Store sales only cover fixes and basic support.
   To support new feature development and give us financial stability please consider sponsoring us, no amount is too small! https://github.com/sponsors/wave-harmonic
-  Wind speed added to OceanRenderer component so that wave conditions change naturally for different wind conditions.
-  Empirical spectra retweaked and use the aforementioned wind speed.
-  Add Overall Normals Scale parameter to material that scales final surface normal (includes both normal map and wave simulation normal).
-  Headless support - add support for running without display, with new toggle on OceanRenderer to emulate it in Editor.
-  No GPU support - add support for running without GPU, with new toggle on OceanRenderer to emulate it in Editor.
-  OceanRenderer usability - system automatically rebuilds when changing settings on the component, 'Rebuild' button removed.
-  Ocean material can now be set with scripting.
-  Custom Time Provider has pause toggle, for easy pausing functionality.
-  Network Time Provider added to easily sync water simulation to server time.
-  Cutscene Time Provider added to drive water simulation time from Timelines.
-  Made many fields scriptable (public) on *BoatProbes*, *BoatAlignNormal* and *SimpleFloatingObject*.
-  Tweaked colours and some of properties for *Ocean-Underwater* material. `[BIRP] [URP]`
-  *Copy Ocean Material Params Each Frame* is now enabled by default for *Underwater Post Process*. `[HDRP]`
-  Add *Refractive Index of Water* property to ocean material. `[HDRP]`

Fixed
^^^^^
-  Fix build errors for platforms that do not support XR/VR.
-  Fix "black square" bug on Oculus Quest.
-  Fix for bugs where a large boat may stop moving when camera is close.
-  Fix bad data being sampled from simulations when they're not enabled like the entire ocean being shadowed when shadow data was disabled.
-  Fix null exception for attach renderer help box fix button.
-  Fix "remove renderer" help box not showing when it should.
-  Fix bug where wind direction could not be set per ShapeGerstner component.
-  Fix compilation errors when only Unity's new *Input System* backend is available.
-  Fix null exceptions in validation when *OceanRenderer* is not present.
-  Fix incorrect validation showing in prefab mode.
-  Fix shadow data for XR/VR `SPI` from working and breaking builds. `[HDRP]`
-  Fix underwater effect from breaking after all cameras being disabled. `[HDRP]`
-  Fix ocean tiles disappearing when far from zero. `[URP]`

Removed
^^^^^^^
-  Remove Phillips and JONSWAP spectrum model options.

Deprecated
^^^^^^^^^^
-  *Layer Name* on the *Ocean Renderer* has been deprecated. Use *Layer* instead.
-  The *Refractive Index of Air* on the ocean material will be removed in a future version. `[BIRP] [URP]`

Documentation
^^^^^^^^^^^^^
-  Document issues with transparency in new :ref:`rendering` page.
-  Improve :ref:`lighting` section.


4.10
----

Changed
^^^^^^^
-  Set minimum Unity version to 2019.4.24.
-  Spline can now be used with any ocean input type, so can be used to set water level, add flow, and more.
-  System for tweaking data on spline points such as flow speed.
-  *RegisterHeightInput* component added for a clearer way to change water height (can be used instead of *RegisterAnimWavesInput*).
-  More validation help boxes added to catch a wider range of setup issues.
-  Fix buttons in help boxes now describe action that will be taken.
-  Rename *Add Water Height From Geometry* to *Set Base Water Height Using Geometry*.
-  Rename *Set Water Height To Geometry* to *Set Water Height Using Geometry*.
-  Improved spline gizmo line drawing to highlight selected spline point.
-  Add version and render pipeline to help button documentation links.
-  Validate scene view effects toggle options.
-  Add various fix buttons for depth cache issues.
-  Set minimum render pipeline package version to 7.6 which is correct for 2019.4. `[HDRP] [URP]`
-  Rearrange some material properties. `[HDRP]`

Fixed
^^^^^
-  Fix water body creation not being part of undo/redo history.
-  Fix spline point delete not being part of undo/redo history.
-  Fix validation fix buttons that attach components not being part of undo/redo history.
-  Fix ShapeGerstnerBatched not having default spectrum when using "Reset" and correct undo/redo history.
-  Fix properties with embedded asset editors appearing broken for Unity 2020 and 2021.
-  Fix shader compilation errors for `HDRP` 10.4. `[HDRP]`
-  Remove duplicate foam bubble properties. `[HDRP]`
-  New horizon line bug fix which is enabled by default (with option to switch back to old safety margin). `[HDRP]`

Documentation
^^^^^^^^^^^^^
-  Add :ref:`detecting_above_or_below_water` and have Q&A question refer to it.
-  Add :ref:`known-issues` page.
-  Document *Caustics Distortion Texture*. `[HDRP]`
-  Fixed Underwater :ref:`underwater_pp_setup` not being complete. `[HDRP]`
-  Fix broken Unity documentation links by correctly setting minimum render pipeline version. `[HDRP] [URP]`


4.9
---

Breaking
^^^^^^^^
-  Dynamic Waves and Foam simulations now run at configurable fixed timesteps for consistency across different frame rates.
   Tweaking of settings may be required.
   See :pr:`778` for more details.
-  Change *Layer Names* (string array) to *Layers* (LayerMask) on *Ocean Depth Cache*.

Preview
^^^^^^^
-  Add wizard for creating local water bodies. See :ref:`water-bodies`.

Changed
^^^^^^^
-  Add :link:`online documentation <https://crest.readthedocs.io>`.
-  Set up help button linking to new documentation for multiple components, and added material help button.
-  Add inline editing for sim settings, wave spectrums and ocean material.
-  Add `Crest` icons to sim settings and wave spectrums.
-  Add button to fix issues on some validation help boxes.
-  Add validation to inform whether the depth cache is outdated.
-  Add validation for ocean depth cache with non uniform scale.
-  Add scriptable custom time provider property which accepts interfaces.
-  Validate simulation checkboxes and their respective material checkboxes and inputs.
-  Add "`Crest`" prefix to component menu items.
-  Organise "`Crest`" component menu items into subfolders.

Fixed
^^^^^
-  Fix more cases of fine gaps.
-  Fix depth cache not reflecting updated properties when populating cache.
-  Fix RayTraceHelper not working.
-  Fix ShapeGerstner component breaking builds.
-  Fix PS4/PSSL shader errors.
-  Fix local waves flickering in some cases.
-  Fix VFACE breaking shaders on consoles.
-  Fix underwater normals incorrect orientation. `[HDRP]`
-  Fix shader errors for latest consoles. `[HDRP]`
-  Fix gray ocean by forcing depth and opaque texture when needed in the editor. `[URP]`
-  Only feather foam at shoreline if transparency is enabled. `[URP]`

Deprecated
^^^^^^^^^^
-  *Assign Layer* component is no longer used in examples and will be removed.


4.8
---

Preview
^^^^^^^
-  Add new Gerstner component *ShapeGerstner* with better performance, improved foam at a distance, correct wave direction and spline support (preview).
   See notes in the *Wave conditions* section of the user guide.
-  Add new spline tool component *Spline* which can be wave splines for new gerstner system (preview).
   See notes in the *Wave conditions* section of the user guide.

Changed
^^^^^^^
-  Change minimum Unity version to 2019.4.9
-  Add orthographic projection support to ocean surface
-  Add weight control for *Underwater Environmental Lighting* component
-  Calculate sub-surface light scattering from surface pinch, to enable other fixes/improvements.
   May require retweaking of the scattering settings on the ocean material.
-  Improve error reporting when compute shaders fail
-  Change shader level target for combine shader to 3.5 which might fix some issues on Quest

Fixed
^^^^^
-  Fix dynamic wave sim stablity by reducing *Courant number* default value
-  Remove warning when camera not set which was displaying even when it shouldn't
-  Change ocean depth cache populate event option to Start
-  Fix for multiple gaps/cracks in ocean surface bugs
-  Fix *Follow Horizontal Motion* for foam override
-  Fix normals not being flipped for underwater with flow enabled
-  Fix meniscus shader not being enabled `[HDRP]`
-  Fix ocean depth cache triggered by other cameras or probes `[URP]`
-  Fix underwater effect flickering when other cameras are in the scene `[URP]`

Performance
^^^^^^^^^^^
-  Add option on *AnimWaveSimSetting* to disable ping pong for combine pass.
   See notes in performance section of user guide.


4.7
---

Changed
^^^^^^^
-  Add foam override shader and material to remove foam
-  Add camera property to *OceanRenderer*. *ViewerHeightAboveWater* will use camera transform
-  Add option to add downhill force to buoyancy for some floating objects
-  Disable underwater culling if underwater effect is not used `[HDRP]`
-  Underwater effect uses stencil buffer instead of depth buffer again `[HDRP]`

Fixed
^^^^^
-  Improve platform support by improving texture compatibility checks
-  Fix Unity 2020.2 / RP 10 support
-  Fix shadows not following scene view camera
-  Fix *Follow Horizontal Motion* not working
-  Fix *Strength* on *Crest/Inputs/Foam/Add From Texture* being ignored
-  Query system - fixed ring buffer exhausted error on some Linux and Android platforms
-  Fix shadow data breaking gizmos and GUI `[HDRP]`
-  Fix underwater copy ocean material parameters option not working correctly when unchecked `[HDRP]`
-  Fix underwater anti-aliasing artefacts around objects (HDRP 10+ required. See underwater documentation) `[HDRP]`

Performance
^^^^^^^^^^^
-  Minor underwater performance improvement
-  Improve underwater XR multi-pass support (still not 100%) `[HDRP]`
-  Improve underwater XR single pass instance performance `[HDRP]`
-  Improve underwater performance when using dynamic scaling `[HDRP]`


4.6
---

Changed
^^^^^^^
-  Change minimum Unity version to 2019.4.8
-  Improve foam texture
-  Add height component that uses *UnityEvents* (under examples)
-  Add shadow LOD data inputs
-  Add support for disable scene reloading
-  Add more dynamic waves debug reporting options
-  Disable horizontal motion correction on animated waves inputs by default
-  Make some shader parameters globally available
-  Add reflections to ocean surface underside from water volume `[HDRP]`

Fixed
^^^^^
-  Fix precision artefacts in waves for mobile devices when far away from world centre
-  Fix spectrum editor not working in play mode with time freeze
-  Fix build error
-  Fix *UnderwaterEnvironmentalLighting* component restoring un-initialised values
-  Fix precision issues causing very fine gaps in ocean surface
-  Fix some memory leaks in edit mode
-  Fix mesh for underwater effects casting shadow in some projects `[URP]`
-  Fix caustics moving, rotating or warping with camera for `URP` 7.4+ `[URP]`
-  Fix caustics breaking for VR/XR `SPI` `[URP]`
-  Fix underwater material from breaking on project load or recompile `[URP]`
-  Fix underwater surface colour being added to transparent parts of ocean surface when underwater `[HDRP]`
-  Fix sample height warning for XR multi-pass `[HDRP]`
-  Fix underwater caustics not working in build due to stripping `[HDRP]`
-  Fix shadows breaking VR/XR single pass instanced `[HDRP]`
-  Fix deprecated XR API call warning `[HDRP]`
-  Fix underwater breaking camera when ocean is disabled during run-time `[HDRP]`
-  Fix ocean falloff parameters allowing bad values `[HDRP]`

Performance
^^^^^^^^^^^
-  Improve performance by reducing work done on scripted shader parameters every frame


4.5
---

Changed
^^^^^^^
-  Add option to ocean input to allow it to move with ocean surface horizontally (was always on in last version)
-  Allow save depth cache to file in edit mode
-  Remove ocean depth cache updating every frame in edit mode
-  Improve feedback in builds when spectrum is invalid
-  Improve spectrum inspector
-  Validate OceanRenderer transform component
-  Validate enter play mode settings
-  Add soft/volume shadows support `[HDRP]`
-  Add light/shadow layer support `[HDRP]`
-  Remove caustics strength scaling by sun light and sea depth `[HDRP]`
-  Add option to clip ocean surface under terrain `[URP]`
-  Use local shader keywords `[URP]`

Fixed
^^^^^
-  Fix undo/redo for spectrum inspector
-  Fix dynamic waves crashing when flow or depth sim not enabled
-  Fix culling issues with turbulent waves
-  Fix precision issues causing gaps in ocean surface
-  Fix shadow sampling not following camera after changing viewpoint
-  Fix shadow sampling not following scene camera
-  Fix caustics and shadows not being correctly aligned
-  Fix material being allocated every frame in edit mode
-  Fix underwater effect for MSAA `[HDRP]`
-  Fix many cases where gaps would appear with underwater effect `[HDRP]`
-  Fix underwater effect rendering at top of viewport in certain cases `[HDRP]`
-  Fix shader errors for HDRP 8.2 `[HDRP]`
-  Fix underwater effects for URP 7.4+ `[URP]`


4.4
---

Changed
^^^^^^^
-  Gerstner waves from geometry shader - allow wave scaling using vertex colour
-  Usability: disable inactive fields on ocean components in Inspector
-  Validation: improve lighting settings validation
-  XR: add single pass instanced support to underwater effects `[HDRP]`
-  XR: add Single Pass Instanced support `[URP]`

Fixed
^^^^^
-  Fix for buffer overrun in height query system which caused crashes on Metal
-  Fix for height query system breaking down at high frame rates when queries made from FixedUpdate
-  Fix height queries when Scene Reload is disabled
-  Fix various null reference exceptions in edit mode
-  Fix for small wavelengths that could never be disabled
-  Fix popping caused by shallow subsurface scattering colour
-  Fix some null exceptions if OceanRenderer is not enabled in scene
-  Fix mode (Global/Geometry) not applying in edit mode for ShapeGerstnerBatched component
-  Clean up validation logging to console when a component is added in edit mode
-  Fix global keywords not being local in underwater shader `[HDRP]`
-  Fix ocean material keywords not applying to underwater `[HDRP]`
-  Fix underwater breaking when dynamic scaling is used `[HDRP]`
-  Fix caustics occasionally appearing on underside of surface `[HDRP]`
-  Fix caustics briefly being too intense when switching cameras with adaptive exposure `[HDRP]`
-  Fix indirect lighting controller multipliers not being applied `[HDRP]`
-  Fix primary light intensity not reducing when primary light goes below the horizon `[HDRP]`
-  Fix null exceptions when primary light is unset `[HDRP]`
-  Fix underwater shader/material breaking on project load `[URP]`
-  Fix shadow sampling running on cameras which isn't the main camera `[URP]`

Performance
^^^^^^^^^^^
-  Fix for ocean depth cache populating every frame erroneously


4.3
---

.. important::

   **Crest LWRP deprecated**. We are no longer able to support LWRP, and have removed the LWRP version of Crest in this release.
   Do not install this version if you need to remain on LWRP.

Changed
^^^^^^^
-  Ocean now runs in edit mode
-  Realtime validation in the form of inspector help boxes
-  Add Submarine example scene created by the Digital Wizards team (Aldana Zanetta and Fernando Zanetta). `[HDRP]`
-  Make compatible with dynamic batching `[URP]`
-  Add option to disable occlusion culling in planar reflections to fix flickering (disabled by default) `[URP]`

Fixed
^^^^^
-  Fix *Segment registrar scratch exhausted* error that could appear in editor
-  Fix underwater effect rendering when using baked occlusion culling `[HDRP]`
-  Fix gaps appearing in underwater effect for very turbulent water `[HDRP]`
-  Fix underwater raising exception when switching cameras `[HDRP]`
-  Fix caustics rendering short of ocean surface when underwater `[HDRP]`


4.2
---

Changed
^^^^^^^
-  Scale caustics intensity by lighting, depth fog density and depth.
-  Show proxy plane in edit mode to visualise sea level.
-  Validate ocean input shader, warn if wrong input type used.
-  Warn if SampleHeightHelper reused multiple times in a frame.
-  Clamp reflection ray to horizon to avoid picking up below-horizon colours. `[HDRP]`
-  Use sampler settings for normal map textures to allow changing filtering settings.
   Turned on anisotropic sampling to reduce blurring. `[HDRP]`

Fixed
^^^^^
-  Fix leaked height query GUIDs which could generate 'too many GUIDs' error after some time.
-  Fix for cracks that could appear between ocean tiles.
-  Fix for null ref exception in SRP version verification.
-  Metal - fix shader error messages in some circumstances.
-  Fix for erroneous water motion if Flow option enabled on material but no Flow simulation present.
-  Fix sea floor depth being in incorrect state when disabled.
-  Fix for a few cases where a crack or line is visible at the horizon. `[HDRP]`
-  Fix for caustics showing above surface. `[HDRP]`
-  Fix foam normals which were not working. `[HDRP]`
-  Fix caustics stereo rendering for single-pass VR `[URP]`


4.1
---

Changed
^^^^^^^
-  Clip surface shader - add convex hull support
-  Add support for local patch of Gerstner waves, demonstrated by GameObject *GerstnerPatch* in *boat.unity*
-  Darkening of the environment lighting underwater due to out-scattering is now done with scripting.
   See the *UnderwaterEnvironmentalLighting* component on the camera in *main.unity*.
-  Remove object-water interaction weight parameter on script. Use strength on material instead.
-  Automatically pick the *sun* light if no *Primary Light* is specified. `[HDRP]`
-  Bump version to 4.1 to match versioning with *Crest HDRP*. `[URP]`

Fixed
^^^^^
-  Fix garbage allocations.
-  Fix PS4 compile errors.
-  Multiple fixes to height query code that could produce 'flat water' issues or use incorrect wave data.
-  Better retention of foam on water surface under camera motion.
-  Fix flow not affecting displaced waves. `[HDRP]`
-  Fix flow not working in *Whirlpool* example scene in standalone builds. `[HDRP]`
-  Fixed caustics effect when underwater and added distortion. `[HDRP]`


4.0
---
-  First release! `[HDRP]`


3.8 `[URP]`
-----------

Changed
^^^^^^^
-  Refactor: Move example content into prefabs to allow sharing between multiple variants of Crest

Fixed
^^^^^
-  Fix for missing shadergraph subgraph used in test/development shaders.
   This does not affect main functionality but fixes import errors.


3.7 `[URP]`
-----------

Changed
^^^^^^^
-  Clip surface shader - replaces the ocean depth mask which is now deprecated
-  Exposed maximum height query count in *Animated Wave Settings*
-  Support disabling *Domain Reload* in 2019.3 for fast iteration

Deprecated
^^^^^^^^^^
-  Ocean depth mask - replaced by clip surface shader

Removed
^^^^^^^
-  Removed the deprecated GPU readback system for getting wave heights on CPU


3.6 `[URP]`
-----------

Changed
^^^^^^^
-  Third party notices added to meet license requirements.
   See *thirdpartynotices.md* in the package root.


3.5 `[URP]`
-----------

Changed
^^^^^^^
-  Gizmos - color coded wireframe rendering of geometry for ocean inputs
-  Object-water interaction: 'adaptor' component so that interaction can be used without a 'boat'.
   See *AnimatedObject* object in *boat.unity*.
-  Object-water interaction: new script to generate dynamic waves from spheres, which can be composed together.
   See *Spinner* object in *boat.unity*.
-  Input shader for flowmap textures
-  Better validation of depth caches to catch issues
-  Documentation - link to new tutorial video about creating ocean inputs

Fixed
^^^^^
-  VR refraction fix - ocean transparency now works in VR using *Single Pass* mode.
-  Fix visual pop bug at background/horizon when viewer gains altitude
-  Fix for compile errors for some ocean input shaders


3.4 `[URP]`
-----------

Changed
^^^^^^^
-  Ocean depth cache supports saving cache to texture on disk
-  Ray trace helper for ray queries against water
-  Input shader for flowmaps
-  Shader code misc refactors and cleanup

Fixed
^^^^^
-  Fix for dynamic wave sim compute shader not compiling on iOS


3.3 `[URP]`
-----------

Fixed
^^^^^
-  Fix for compute-based height queries which would return wrong results under some circumstances (visible when using Visualise Collision Area script)
-  VR: Fix case where sea floor depth cache was not populated
-  VR: Fix case where ocean planar reflections broken


3.2 `[URP]`
-----------

Changed
^^^^^^^
-  Add links to recently published videos to documentation
-  Asmdef files added to make Crest compilation self-contained
-  Documentation - strategy for configuring dynamic wave simulation
-  Documentation - dedicated, fleshed out section for shallow water and shoreline foam
-  Documentation - technical information about render/draw order

Fixed
^^^^^
-  Fixes for wave shape and underwater curtain on Vulkan
-  Fix for user input to animated wave shape, add to shape now works correctly
-  Fix for underwater appearing off-colour in standalone builds
-  Fix garbage generated by planar reflections script
-  Fix for invalid sampling data error for height queries
-  Fix for underwater effect not working in secondary cameras
-  Fix waves not working on some GPUs and Quest VR - :issue:`279`
-  Fix planar reflections not lining up with visuals for different aspect ratios


3.1 `[URP]`
-----------

Changed
^^^^^^^
-  Preview 1 of Crest URP - package uploaded for Unity 2019.3

Fixed
^^^^^
-  Made more robust against VR screen depth bug, resolves odd shapes appearing on surface
-  :issue:`279`


.. Trim PDF history
.. raw:: latex

   \fi

.. only:: latex

   | Full version history has been omitted for brevity.
     It can be found at :link:`Release Notes <{DocLinkBase}/about/history.html>`.
