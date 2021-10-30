Configuration
=============

Some quick start pointers for changing the ocean look and behaviour:

-  Ocean surface appearance: The active ocean material is displayed below the *OceanRenderer* component.
   The material parameters are described in section :ref:`material_parameters`.
   Turn off unnecessary features to maximize performance.

-  Animated waves / ocean shape: Configured on the *ShapeFFT* script by providing an *Ocean Wave Spectrum* asset.
   This asset has an equalizer-style interface for tweaking different scales of waves, and also has some parametric wave spectra from the literature for comparison. See section :ref:`wave-conditions-section`.

-  Shallow water: Any ocean seabed geometry needs set up to register it with *Crest*.
   See section :ref:`shallows`.

-  Ocean foam: Configured on the *OceanRenderer* script by providing a *Sim Settings Foam* asset.

-  Underwater: If the camera needs to go underwater, the underwater effect must be configured.
   See section :ref:`underwater` for instructions.

-  Dynamic wave simulation: Simulates dynamic effects like object-water interaction.
   Configured on the *OceanRenderer* script by providing a *Sim Settings Wave* asset, described in section :ref:`dynamic_waves_settings`.

-  A big strength of *Crest* is that you can add whatever contributions you like into the system.
   You could add your own shape or deposit foam onto the surface where desired.
   Inputs are generally tagged with the *Register* scripts and examples can be found in the example content scenes.

All settings can be changed at run-time and live authored.
When tweaking ocean shape it can be useful to freeze time (from script, set *Time.timeScale* to 0) to clearly see the effect of each octave of waves.

.. tip::

   .. include:: includes/_animated-materials.rst


.. _material_parameters:

Material Parameters
-------------------

Normals
^^^^^^^

.. line_block::

   |  **Overall Normal Strength** Strength of the final surface normal (includes both wave normal and normal map)

   .. only:: birp or urp

      |  **Use Normal Map** Whether to add normal detail from a texture. Can be used to add visual detail to the water surface `[BIRP] [URP]`

   |  **Normal Map** Normal map and caustics distortion texture (should be set to Normals type in the properties)
   |  **Normal Map Scale** Scale of normal map texture
   |  **Normal Map Strength** Strength of normal map influence


Scattering
^^^^^^^^^^

.. line_block::

   |  **Scatter Colour Base** Base colour when looking straight down into water.

   .. only:: birp or urp

      |  **Scatter Colour Grazing** Base colour when looking into water at shallow/grazing angle. `[BIRP] [URP]`
      |  **Enable Shadowing** Changes colour in shadow. Requires 'Create Shadow Data' enabled on OceanRenderer script. `[BIRP] [URP]`

   |  **Scatter Colour Shadow** Base colour in shadow. Requires 'Create Shadow Data' enabled on OceanRenderer script.

Subsurface Scattering
^^^^^^^^^^^^^^^^^^^^^

.. line_block::

   .. only:: birp or urp

      |  **Enable** Whether to to emulate light scattering through the water volume. `[BIRP] [URP]`

   |  **SSS Tint** Colour tint for primary light contribution.
   |  **SSS Intensity Base** Amount of primary light contribution that always comes in.
   |  **SSS Intensity Sun** Primary light contribution in direction of light to emulate light passing through waves.
   |  **SSS Sun Falloff** Falloff for primary light scattering to affect directionality.


Shallow Scattering
^^^^^^^^^^^^^^^^^^

The water colour can be varied in shallow water (this requires a depth cache created so that the system knows which areas are shallow, see section :ref:`shallows`).

.. line_block::

   .. only:: birp or urp

      |  **Enable** Enable light scattering in shallow water. `[BIRP] [URP]`

   |  **Scatter Colour Shallow** Scatter colour used for shallow water.
   |  **Scatter Colour Depth Max** Maximum water depth that is considered 'shallow', in metres.
      Water that is deeper than this depth is not affected by shallow colour.
   |  **Scatter Colour Depth Falloff** Falloff of shallow scattering, which gives control over the appearance of the transition from shallow to deep.

   .. only:: birp or urp

      |  **Scatter Colour Shallow Shadow** Shallow water colour in shadow (see comment on Shadowing param above). `[BIRP] [URP]`


Reflection Environment
^^^^^^^^^^^^^^^^^^^^^^

.. line_block::

   |  **Specular** Strength of specular lighting response.

   .. only:: hdrp

      |  **Occlusion** Strength of reflection. `[HDRP]`

   .. only:: hdrp or urp

      .. NOTE: Kind of like "Roughness" in BIRP.

      |  **Smoothness** Smoothness of surface. `[HDRP] [URP]`

   .. only:: urp

      .. NOTE: "Vary Falloff Over Distance" in BIRP.

      |  **Vary Smoothness Over Distance** Helps to spread out specular highlight in mid-to-background.
         From a theory point of view, models transfer of normal detail to microfacets in BRDF. `[URP]`

   .. only:: hdrp or urp

      |  **Smoothness Far** Material smoothness at far distance from camera. `[HDRP] [URP]`
      |  **Smoothness Far Distance** Definition of far distance. `[HDRP] [URP]`
      |  **Smoothness Falloff** How smoothness varies between near and far distance. `[HDRP] [URP]`

   .. only:: birp

      .. NOTE:
      .. Appears to be "Softness" in URP - but different. Roughness is the opposite of smoothness.
      .. "Softness" isn't really a thing from what I can see. I think this is both "Smoothness" and "Softness".

      |  **Roughness** Controls blurriness of reflection `[BIRP]`

   .. only:: urp

      |  **Softness** Acts as mip bias to smooth/blur reflection. `[URP]`

      .. NOTE: Directional Light "Boost" in BIRP.

      |  **Light Intensity Multiplier** Main light intensity multiplier. `[URP]`

   .. only:: birp or urp

      |  **Fresnel Power** Controls harshness of Fresnel behaviour. `[BIRP] [URP]`
      |  **Refractive Index of Air** Index of refraction of air.
         Can be increased to almost 1.333 to increase visibility up through water surface. `[BIRP] [URP]`

      .. admonition:: Deprecated

         The *Refractive Index of Air* property will be removed in a future version.

   |  **Refractive Index of Water** Index of refraction of water. Typically left at 1.333.

   .. only:: birp or urp

      |  **Planar Reflections** Dynamically rendered 'reflection plane' style reflections.
         Requires OceanPlanarReflection script added to main camera. `[BIRP] [URP]`
      |  **Planar Reflections Distortion** How much the water normal affects the planar reflection. `[BIRP] [URP]`

   .. only:: birp

      |  **Override Reflection Cubemap** Whether to use an overridden reflection cubemap (provided in the next property). `[BIRP]`
      |  **Reflection Cubemap Override** Custom environment map to reflect. `[BIRP]`


.. only:: birp

   Add Directional Light
   ^^^^^^^^^^^^^^^^^^^^^

   |  **Enable** Add specular highlights from the the primary light. `[BIRP]`
   |  **Boost** Specular highlight intensity. `[BIRP]`
   |  **Falloff** Falloff of the specular highlights from source to camera. `[BIRP]`
   |  **Vary Falloff Over Distance** Helps to spread out specular highlight in mid-to-background. `[BIRP]`
   |  **Far Distance** Definition of far distance. `[BIRP]`
   |  **Falloff At Far Distance** Same as "Falloff" except only up to "Far Distance". `[BIRP]`

.. only:: birp or urp

   Procedural Skybox
   ^^^^^^^^^^^^^^^^^

   |  **Enable** Enable a simple procedural skybox.
      Not suitable for realistic reflections, but can be useful to give control over reflection colour - especially in stylized/non realistic applications. `[BIRP] [URP]`
   |  **Base** Base sky colour. `[BIRP] [URP]`
   |  **Towards Sun** Colour in sun direction. `[BIRP] [URP]`
   |  **Directionality** Direction fall off. `[BIRP] [URP]`
   |  **Away From Sun** Colour away from sun direction. `[BIRP] [URP]`


Foam
^^^^

.. line_block::

   |  **Enable** Enable foam layer on ocean surface.
   |  **Foam** Foam texture.
   |  **Foam Scale** Foam texture scale.
   |  **Foam Feather** Controls how gradual the transition is from full foam to no foam.

   .. only:: birp or urp

      .. TODO: Consider removing "Shoreline Foam Min Depth" as it is just feathering the edges?

      |  **Foam Tint** Colour tint for whitecaps / foam on water surface. `[BIRP] [URP]`
      |  **Light Scale** Scale intensity of lighting. `[BIRP] [URP]`
      |  **Shoreline Foam Min Depth** Proximity to sea floor where foam starts to get generated. `[BIRP] [URP]`

      .. albedo intensity is foam colour except grayscale
      .. foam emissive intensity is light scale

   .. only:: hdrp

      |  **Foam Albedo Intensity** Scale intensity of diffuse lighting. `[HDRP]`
      |  **Foam Emissive Intensity** Scale intensity of emitted light. `[HDRP]`
      |  **Foam Smoothness** Smoothness of foam material. `[HDRP]`


.. NOTE: Adding the "only" directive only to heading will break the layout.


Foam 3D Lighting
^^^^^^^^^^^^^^^^

.. line_block::

   .. only:: birp or urp

      |  **Enable** Generates normals for the foam based on foam values/texture and use it for foam lighting. `[BIRP] [URP]`

   |  **Foam Normal Strength** Strength of the generated normals.

   .. only:: birp or urp

      |  **Specular Fall-Off** Acts like a gloss parameter for specular response. `[BIRP] [URP]`
      |  **Specular Boost** Strength of specular response. `[BIRP] [URP]`


Foam Bubbles
^^^^^^^^^^^^

|  **Foam Bubbles Color** Colour tint bubble foam underneath water surface.
|  **Foam Bubbles Parallax** Parallax for underwater bubbles to give feeling of volume.
|  **Foam Bubbles Coverage** How much underwater bubble foam is generated.


Transparency
^^^^^^^^^^^^

.. line_block::

   .. only:: birp or urp

      |  **Enable** Whether light can pass through the water surface. `[BIRP] [URP]`

   |  **Refraction Strength** How strongly light is refracted when passing through water surface.
   |  **Depth Fog Density** Scattering coefficient within water volume, per channel.


Caustics
^^^^^^^^
.. line_block::

   |  **Enable** Approximate rays being focused/defocused on underwater surfaces.
   |  **Caustics** Caustics texture.
   |  **Caustics Scale** Caustics texture scale.
   |  **Caustics Texture Grey Point** The 'mid' value of the caustics texture, around which the caustic texture values are scaled.
   |  **Caustics Strength** Scaling / intensity.
   |  **Caustics Focal Depth** The depth at which the caustics are in focus.
   |  **Caustics Depth Of Field** The range of depths over which the caustics are in focus.

   .. only:: hdrp

      .. TODO: Why does SG have a distortion texture and SL uses the normal map?

      |  **Caustics Distortion Texture** Texture to distort caustics. `[HDRP]`

   |  **Caustics Distortion Strength** How much the caustics texture is distorted.
   |  **Caustics Distortion Scale** The scale of the distortion pattern used to distort the caustics.

Underwater
^^^^^^^^^^

.. line_block::

   .. only:: birp or urp

      .. NOTE: Will be removed once we migrate to the underwater post-process effect.

      |  **Enable** Whether the underwater effect is being used. This enables code that shades the surface correctly from underneath. `[BIRP] [URP]`

   |  **Cull Mode** Ordinarily set this to *Back* to cull back faces, but set to *Off* to make sure both sides of the surface draw if the underwater effect is being used.

Flow
^^^^

|  **Enable** Flow is horizontal motion in water as demonstrated in the 'whirlpool' example scene.
   'Create Flow Sim' must be enabled on the OceanRenderer to generate flow data.

.. _lighting:

Lighting
--------

General
^^^^^^^

.. only:: birp

   .. tab:: `BIRP`

      .. include:: includes/_birp-lighting.rst

.. only:: hdrp

   .. tab:: `HDRP`

      .. include:: includes/_hdrp-lighting.rst

.. only:: urp

   .. tab:: `URP`

      .. include:: includes/_urp-lighting.rst


Reflections
^^^^^^^^^^^

Reflections contribute hugely to the appearance of the ocean.
The look of the ocean will dramatically changed based on the reflection environment.

The Index of Refraction setting controls how much reflection contributes for different view angles.

.. only:: birp

   .. tab:: `BIRP`

      .. include:: includes/_birp-reflections.rst

.. only:: hdrp

   .. tab:: `HDRP`

      .. include:: includes/_hdrp-reflections.rst

.. only:: urp

   .. tab:: `URP`

      .. include:: includes/_urp-reflections.rst


Refractions
^^^^^^^^^^^

Refractions sample from the camera's colour texture.
Anything rendered in the transparent pass or higher will not be included in refractions.

See :ref:`transparent-object-before-ocean-surface` for issues with Crest and other refractive materials.


.. _orthographic_projection:

Orthographic Projection
-----------------------

Crest supports orthographic projection out-of-the-box, but it might require some configuration to get a desired appearance.

Crest uses the camera's position for the LOD system which can be awkward for orthographic which uses the size property on the camera.
Use the *Viewpoint* property on the *Ocean Renderer* to override the camera's
position.

Underwater effects do *not* currently support orthographic projection.


.. _ocean_construction_parameters:

Ocean Construction Parameters
-----------------------------

There are a small number of parameters that control the construction of the ocean shape and geometry:

-  **Lod Data Resolution** - the resolution of the various ocean LOD data including displacement textures, foam data, dynamic wave sims, etc.
   Sets the 'detail' present in the ocean - larger values give more detail at increased run-time expense.

-  **Geometry Down Sample Factor** - geometry density - a value of 2 will generate one vert per 2x2 LOD data texels.
   A value of 1 means a vert is generated for every LOD data texel.
   Larger values give lower fidelity surface shape with higher performance.

-  **Lod Count** - the number of levels of detail / scales of ocean geometry to generate. The horizontal range of the ocean surface doubles for each added LOD, while GPU processing time increases linearly.
   It can be useful to select the ocean in the scene view while running in editor to inspect where LODs are present.

-  **Max Scale** - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints.
   This sets the maximum the ocean will be scaled if set to a positive value.

-  **Min Scale** - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level.
   Low values give lots of detail, but will limit the horizontal extents of the ocean detail.
