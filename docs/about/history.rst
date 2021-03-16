
Release Notes
=============

.. Disable section numbering for Latex. This is a bug in Sphinx.

.. raw:: latex

   \setcounter{secnumdepth}{0}

.. only:: html or readthedocs

   .. important::

      Release Notes only covers `URP` and `HDRP`.


|version|
---------

Changed
^^^^^^^
- Add :link:`online documentation <https://crest.readthedocs.io>`.
- Add inline editing for sim settings, wave spectrums and ocean material.
- Add Crest icons to sim settings and wave spectrums.
- Add button to fix issues on some validation help boxes.
- Add validation to inform whether the depth cache is outdated.
- Add validation for ocean depth cache with non uniform scale.
- Add scriptable custom time provider property which accepts interfaces.
- Validate simulation checkboxes and their respective material checkboxes and inputs.

Fixed
^^^^^

.. bullet_list::

   - Fix more cases of fine gaps.
   - Fix depth cache not reflecting updated properties when populating cache.
   - Fix RayTraceHelper not working.
   - Fix ShapeGerstner component breaking builds.
   - Fix PS4/PSSL errors.
   - Fix local waves flickering.

   .. only:: hdrp

      - Fix underwater normals incorrect orientation. `[HDRP]`

   .. only:: urp

      - Fix gray ocean by forcing depth and opaque texture when needed in the editor. `[URP]`
      - Only feather foam at shoreline if transparency is enabled. `[URP]`


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
.. bullet_list::

   -  Fix dynamic wave sim stablity by reducing *Courant number* default value
   -  Remove warning when camera not set which was displaying even when it shouldn't
   -  Change ocean depth cache populate event option to Start
   -  Fix for multiple gaps/cracks in ocean surface bugs
   -  Fix *Follow Horizontal Motion* for foam override
   -  Fix normals not being flipped for underwater with flow enabled

   .. only:: hdrp

      -  Fix meniscus shader not being enabled `[HDRP]`

   .. only:: urp

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

.. bullet_list::

   -  Add foam override shader and material to remove foam
   -  Add camera property to *OceanRenderer*. *ViewerHeightAboveWater* will use camera transform
   -  Add option to add downhill force to buoyancy for some floating objects

   .. only:: hdrp

      -  Disable underwater culling if underwater effect is not used `[HDRP]`
      -  Underwater effect uses stencil buffer instead of depth buffer again `[HDRP]`

Fixed
^^^^^
.. bullet_list::

   -  Improve platform support by improving texture compatibility checks
   -  Fix Unity 2020.2 / RP 10 support
   -  Fix shadows not following scene view camera
   -  Fix *Follow Horizontal Motion* not working
   -  Fix *Strength* on *Crest/Inputs/Foam/Add From Texture* being ignored
   -  Query system - fixed ring buffer exhausted error on some Linux and Android platforms

   .. only:: hdrp

      -  Fix shadow data breaking gizmos and GUI `[HDRP]`
      -  Fix underwater copy ocean material parameters option not working correctly when unchecked `[HDRP]`
      -  Fix underwater anti-aliasing artefacts around objects (HDRP 10+ required. See underwater documentation) `[HDRP]`

   .. only:: urp

      -  Fix mesh for underwater effects casting shadow in some projects `[URP]`
      -  Fix caustics moving, rotating or warping with camera for URP 7.4+ `[URP]`
      -  Fix caustics breaking for VR/XR SPI `[URP]`
      -  Fix underwater material from breaking on project load or recompile `[URP]`

Performance
^^^^^^^^^^^
.. bullet_list::

   -  Minor underwater performance improvement

   .. only:: hdrp

      -  Improve underwater XR multi-pass support (still not 100%) `[HDRP]`
      -  Improve underwater XR single pass instance performance `[HDRP]`
      -  Improve underwater performance when using dynamic scaling `[HDRP]`


4.5
---

Changed
^^^^^^^
.. bullet_list::

   -  Add option to ocean input to allow it to move with ocean surface horizontally (was always on in last version)
   -  Allow save depth cache to file in edit mode
   -  Remove ocean depth cache updating every frame in edit mode
   -  Improve feedback in builds when spectrum is invalid
   -  Improve spectrum inspector
   -  Validate OceanRenderer transform component
   -  Validate enter play mode settings

   .. only:: hdrp

      -  Add soft/volume shadows support `[HDRP]`
      -  Add light/shadow layer support `[HDRP]`
      -  Remove caustics strength scaling by sun light and sea depth `[HDRP]`

   .. only:: urp

      -  Add option to clip ocean surface under terrain `[URP]`
      -  Use local shader keywords `[URP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix undo/redo for spectrum inspector
   -  Fix dynamic waves crashing when flow or depth sim not enabled
   -  Fix culling issues with turbulent waves
   -  Fix precision issues causing gaps in ocean surface
   -  Fix shadow sampling not following camera after changing viewpoint
   -  Fix shadow sampling not following scene camera
   -  Fix caustics and shadows not being correctly aligned
   -  Fix material being allocated every frame in edit mode

   .. only:: hdrp

      -  Fix underwater effect for MSAA `[HDRP]`
      -  Fix many cases where gaps would appear with underwater effect `[HDRP]`
      -  Fix underwater effect rendering at top of viewport in certain cases `[HDRP]`
      -  Fix shader errors for HDRP 8.2 `[HDRP]`

   .. only:: urp

      -  Fix underwater effects for URP 7.4+ `[URP]`


4.4
---

Changed
^^^^^^^
.. bullet_list::

   -  Gerstner waves from geometry shader - allow wave scaling using vertex colour
   -  Usability: disable inactive fields on ocean components in Inspector
   -  Validation: improve lighting settings validation

   .. only:: hdrp

      -  XR: add single pass instanced support to underwater effects `[HDRP]`

   .. only:: urp

      -  XR: add Single Pass Instanced support `[URP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix for buffer overrun in height query system which caused crashes on Metal
   -  Fix for height query system breaking down at high frame rates when queries made from FixedUpdate
   -  Fix height queries when Scene Reload is disabled
   -  Fix various null reference exceptions in edit mode
   -  Fix for small wavelengths that could never be disabled
   -  Fix popping caused by shallow subsurface scattering colour
   -  Fix some null exceptions if OceanRenderer is not enabled in scene
   -  Fix mode (Global/Geometry) not applying in edit mode for ShapeGerstnerBatched component
   -  Clean up validation logging to console when a component is added in edit mode

   .. only:: hdrp

      -  Fix global keywords not being local in underwater shader `[HDRP]`
      -  Fix ocean material keywords not applying to underwater `[HDRP]`
      -  Fix underwater breaking when dynamic scaling is used `[HDRP]`
      -  Fix caustics occasionally appearing on underside of surface `[HDRP]`
      -  Fix caustics briefly being too intense when switching cameras with adaptive exposure `[HDRP]`
      -  Fix indirect lighting controller multipliers not being applied `[HDRP]`
      -  Fix primary light intensity not reducing when primary light goes below the horizon `[HDRP]`
      -  Fix null exceptions when primary light is unset `[HDRP]`

   .. only:: urp

      -  Fix underwater shader/material breaking on project load `[URP]`
      -  Fix shadow sampling running on cameras which isn't the main camera `[URP]`

Performance
^^^^^^^^^^^
-  Fix for ocean depth cache populating every frame erroneously


4.3
---

.. only:: urp

   .. important::

      **Crest LWRP deprecated**. We are no longer able to support LWRP, and have removed the LWRP version of Crest in this release.
      Do not install this version if you need to remain on LWRP.

Changed
^^^^^^^
.. bullet_list::

   -  Ocean now runs in edit mode
   -  Realtime validation in the form of inspector help boxes

   .. only:: hdrp

      -  Add Submarine example scene created by the Digital Wizards team (Aldana Zanetta and Fernando Zanetta). `[HDRP]`

   .. only:: urp

      -  Make compatible with dynamic batching `[URP]`
      -  Add option to disable occlusion culling in planar reflections to fix flickering (disabled by default) `[URP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix *Segment registrar scratch exhausted* error that could appear in editor

   .. only:: hdrp

      -  Fix underwater effect rendering when using baked occlusion culling `[HDRP]`
      -  Fix gaps appearing in underwater effect for very turbulent water `[HDRP]`
      -  Fix underwater raising exception when switching cameras `[HDRP]`
      -  Fix caustics rendering short of ocean surface when underwater `[HDRP]`


4.2
---

Changed
^^^^^^^
.. bullet_list::

   -  Scale caustics intensity by lighting, depth fog density and depth.
   -  Show proxy plane in edit mode to visualise sea level.
   -  Validate ocean input shader, warn if wrong input type used.
   -  Warn if SampleHeightHelper reused multiple times in a frame.

   .. only:: hdrp

      -  Clamp reflection ray to horizon to avoid picking up below-horizon colours. `[HDRP]`
      -  Use sampler settings for normal map textures to allow changing filtering settings.
         Turned on anisotropic sampling to reduce blurring. `[HDRP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix leaked height query GUIDs which could generate 'too many GUIDs' error after some time.
   -  Fix for cracks that could appear between ocean tiles.
   -  Fix for null ref exception in SRP version verification.
   -  Metal - fix shader error messages in some circumstances.
   -  Fix for erroneous water motion if Flow option enabled on material but no Flow simulation present.
   -  Fix sea floor depth being in incorrect state when disabled.

   .. only:: hdrp

      -  Fix for a few cases where a crack or line is visible at the horizon. `[HDRP]`
      -  Fix for caustics showing above surface. `[HDRP]`
      -  Fix foam normals which were not working. `[HDRP]`

   .. only:: urp

      -  Fix caustics stereo rendering for single-pass VR `[URP]`


4.1
---

Changed
^^^^^^^
.. bullet_list::

   -  Clip surface shader - add convex hull support
   -  Add support for local patch of Gerstner waves, demonstrated by GameObject *GerstnerPatch* in *boat.unity*
   -  Darkening of the environment lighting underwater due to out-scattering is now done with scripting.
      See the *UnderwaterEnvironmentalLighting* component on the camera in *main.unity*.
   -  Remove object-water interaction weight parameter on script. Use strength on material instead.

   .. only:: hdrp

      -  Automatically pick the *sun* light if no *Primary Light* is specified. `[HDRP]`

   .. only:: urp

      -  Bump version to 4.1 to match versioning with *Crest HDRP*. `[URP]`

Fixed
^^^^^
.. bullet_list::

   -  Fix garbage allocations.
   -  Fix PS4 compile errors.
   -  Multiple fixes to height query code that could produce 'flat water' issues or use incorrect wave data.
   -  Better retention of foam on water surface under camera motion.

   .. only:: hdrp

      -  Fix flow not affecting displaced waves. `[HDRP]`
      -  Fix flow not working in *Whirlpool* example scene in standalone builds. `[HDRP]`
      -  Fixed caustics effect when underwater and added distortion. `[HDRP]`


.. only:: hdrp

   4.0 `[HDRP]`
   ------------

   -  First release!


.. only:: urp

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
   - Ocean depth mask - replaced by clip surface shader

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

   Fixed
   ^^^^^
   -  Fixes for wave shape and underwater curtain on Vulkan
   -  Fix for user input to animated wave shape, add to shape now works correctly
   -  Fix for underwater appearing off-colour in standalone builds
   -  Fix garbage generated by planar reflections script
   -  Fix for invalid sampling data error for height queries
   -  Fix for underwater effect not working in secondary cameras

   .. TODO: Determine why URP has this here but SRP has it in 2.2
   .. -  Fix waves not working on some GPUs and Quest VR - :issue:`279`
   .. -  Fix planar reflections not lining up with visuals for different aspect ratios
   .. -  Documentation - strategy for configuring dynamic wave simulation
   .. -  Documentation - dedicated, fleshed out section for shallow water and shoreline foam
   .. -  Documentation - technical information about render/draw order


   3.1 `[URP]`
   -----------

   Changed
   ^^^^^^^
   -  Preview 1 of Crest URP - package uploaded for Unity 2019.3

   Fixed
   ^^^^^
   -  Made more robust against VR screen depth bug, resolves odd shapes appearing on surface
   - :issue:`279`


.. Maybe the following were deleted?

   3.0 `[URP]`
   -----------

   Changed
   ^^^^^^^
   -  *SampleHeightHelper* and *SampleFlowHelper* helpers added to make it easier and simpler to sample ocean data on the CPU
   -  Vary smoothness over distance - helps widen specular response on surface
   -  Support for coloured caustics texture

   Performance
   ^^^^^^^^^^^
   -  Compute Shader Queries - simpler and faster system to service ocean height queries


   2.2 `[URP]`
   -----------

   Changed
   ^^^^^^^
   -  Documentation - strategy for configuring dynamic wave simulation
   -  Documentation - dedicated, fleshed out section for shallow water and shoreline foam
   -  Documentation - technical information about render/draw order

   Fixed
   ^^^^^
   -  Fix waves not working on some GPUs and Quest VR - :issue:`279`
   -  Fix planar reflections not lining up with visuals for different aspect ratios


   2.1 `[URP]`
   -----------

   .. important::

      Version 2 of *Crest* introduced significant changes.
      We recommend backing up your project before upgrading from 1.x to 2.x.

   Changed
   ^^^^^^^
   -  Better validation and errors around legacy wave spectra data to prevent runtime errors

   Fixed
   ^^^^^
   -  Clear dynamic wave, foam and shadow data to prevent unitialised data entering system
   -  Fix potential out of bounds array access in Crest shaders
   -  Remove geometry shader code path for rendering inputs to fix bugs and simplify codebase
   - :issue:`279`

   .. note::

      There are earlier versions where release note history have yet to be added.