Water Appearance
================

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

.. admonition:: Deprecated

   *Shallow Scattering* will be removed in a future version.
   A properly tweaked *Depth Fog Density* achieves better results at lower cost.
   Consider copying over the value from our materials.

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

.. admonition:: Example

    Flow is demonstrated in the *whirlpool* example scene.

|  **Enable** Flow is horizontal motion in water.
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



.. _foam-section:

Foam
----

Overview
^^^^^^^^

Crest simulates foam getting generated by choppy water (*pinched*) wave crests) and in shallow water to approximate foam from splashes at shoreline.
Each update (default is 30 updates per second), the foam values are reduced to model gradual dissipation of foam over time.

To turn on this feature, enable the *Create Foam Sim* option on the *OceanRenderer* script, and ensure the *Enable* option is ticked in the Foam group on the ocean material.

To configure the foam sim, create a *Foam Sim Settings* asset by right clicking the a folder in the *Project* window and selecting *Create/Crest/Foam Sim Settings*, and assigning it to the OceanRenderer component in your scene.


User Inputs
^^^^^^^^^^^

Foam supports :ref:`wave-splines-section` and :ref:`renderer-mode`.

Crest supports inputing any foam into the system, which can be helpful for fine tuning where foam is placed.
To place foam, add some geometry into the world at the area where foam should be added.
Then assign the *RegisterFoamInput* script which will tag it for rendering into the shape, and apply a material with a shader of type *Crest/Inputs/Foam/...*.
See the *DepositFoamTex* object in the *whirlpool.unity* scene for an example.

The process for adding inputs is demonstrated in this :numref:`adding-inputs-video`.

The following input shaders are provided under *Crest/Inputs/Foam*:

-  **Add From Texture** adds foam values read from a user provided texture.
   Can be useful for placing 'blobs' of foam as desired, or can be moved around at runtime to paint foam into the sim.

-  **Add From Vert Colours** can be applied to geometry and uses the red channel of vertex colours to add foam to the sim.
   Similar in purpose to *Add From Texture*, but can be authored in a modelling workflow instead of requiring at texture.

-  **Override Foam** sets the foam to the provided value.
   Useful for removing foam from unwanted areas.


.. _foam-settings:

Simulation Settings
^^^^^^^^^^^^^^^^^^^

General Settings
~~~~~~~~~~~~~~~~

-  **Foam Fade Rate** - How quickly foam dissipates.
   Low values mean foam remains on surface for longer.
   This setting should be balanced with the generation *strength* parameters below.


Wave foam / whitecaps
~~~~~~~~~~~~~~~~~~~~~

Crest detects where waves are 'pinched' and deposits foam to approximate whitecaps.

-  **Wave Foam Strength** - Scales intensity of foam generated from waves.
   This setting should be balanced with the *Foam Fade Rate* setting.

-  **Wave Foam Coverage** - How much of the waves generate foam.
   Higher values will lower the threshold for foam generation, giving a larger area.

.. _shoreline-foam-section:

Shoreline foam
~~~~~~~~~~~~~~

If water depth input is provided to the system (see **Sea Floor Depth** section below), the foam sim can automatically generate foam when water is very shallow, which can approximate accumulation of foam at shorelines.

-  **Shoreline Foam Max Depth** - Foam will be generated in water shallower than this depth.
   Controls how wide the band of foam at the shoreline will be.
   Note that this is not a distance to shoreline, but a threshold on water depth, so the width of the foam band can vary
   based on terrain slope.
   To address this limitation we allow foam to be manually added from geometry or from a texture, see the next
   section.

-  **Shoreline Foam Strength** - Scales intensity of foam generated in shallow water.
   This setting should be balanced with the *Foam Fade Rate* setting.


Developer Settings
~~~~~~~~~~~~~~~~~~

These settings should generally be left unchanged unless one is experiencing issues.

-  **Simulation Frequency** - Frequency to run the foam sim, in updates per second.
   Lower frequencies can be more efficient but may lead to visible jitter.
   Default is 30 updates per second.



.. _shadows-section:

Shadows
-------

The shadow data consists of two channels.
One is for normal shadows (hard shadow term) as would be used to block specular reflection of the light.
The other is a much softer shadowing value (soft shadow term) that can approximately variation in light scattering in the water volume.

This data is captured from the shadow maps Unity renders before the transparent pass.
These shadow maps are always rendered in front of the viewer.
The Shadow LOD Data then reads these shadow maps and copies shadow information into its LOD textures.


.. only:: birp

   .. tab:: `BIRP`

      .. include:: includes/_birp-shadows.rst

.. only:: hdrp

   .. tab:: `HDRP`

      .. include:: includes/_hdrp-shadows.rst

.. only:: urp

   .. tab:: `URP`

      .. include:: includes/_urp-shadows.rst

The shadow sim can be configured by assigning a Shadow Sim Settings asset to the OceanRenderer script in your scene (*Create/Crest/Shadow Sim Settings*).
In particular, the soft shadows are very soft by default, and may not appear for small/thin shadow casters.
This can be configured using the *Jitter Diameter Soft* setting.

There will be times when the shadow jitter settings will cause shadows or light to leak.
An example of this is when trying to create a dark room during daylight.
At the edges of the room the jittering will cause the ocean on the inside of the room (shadowed) to sample outside of the room (not shadowed) resulting in light at the edges.
Reducing the *Jitter Diameter Soft* setting can solve this, but we have also provided a *Register Shadow Input* component which can override the shadow data.
This component bypasses jittering and gives you full control.

Shadows only supports the :ref:`renderer-mode`.

.. Note: RP should allow sampling the shadow maps directly in the ocean shader which would be an alternative to using this shadow data, although it would not give the softer shadow component. This would likely work on 2018.

.. _albedo-section:

Custom Albedo
-------------

Overview
^^^^^^^^

The Albedo feature allows a colour layer to be composited on top of the water surface.
This is useful for projecting colour onto the surface.

This is somewhat similar to decals, except the colour only affects the water.

.. note::

   HDRP has a :link:`Decal Projector <{HDRPDocLink}/Decal-Projector.html>` feature that works with the water, and the effect is more configurable and may be preferred over this feature. When using this feature be sure to enable :link:`Affects Transparent <{HDRPDocLink}/Decal-Projector.html#properties>`.

   URP 2022 has a decal system but it does not support transparent surfaces like water.

   There is a *Render Alpha On Surface* component which is an alternative.
   It behaves similar to a decal projector, but has several issues like z-order issues.


User Inputs
^^^^^^^^^^^

.. note::

   Inputs only execute the first shader pass (pass zero).
   It is recommended to use unlit shader templates or unlit *Shader Graph* (`URP` only) if not using one of ours.
   Shaders provided by *Unity* generally will not work as their primary pass is not zero - even for unlit shaders.

Albedo only supports the :ref:`renderer-mode`.

Any geometry or particle system can add colour to the water. It will be projected from a top down perspective onto the water surface.

To tag GameObjects to render onto the water, attach the *RegisterAlbedoInput* component.


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

-  **Lod Count** - the number of levels of detail / scales of ocean geometry to generate.
   The horizontal range of the ocean surface doubles for each added LOD, while GPU processing time increases linearly.
   It can be useful to select the ocean in the scene view while running in editor to inspect where LODs are present.

-  **Max Scale** - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints.
   This sets the maximum the ocean will be scaled if set to a positive value.

-  **Min Scale** - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level.
   Low values give lots of detail, but will limit the horizontal extents of the ocean detail.
   Increasing this value can be a great performance saving for mobile as it will reduce draw calls.


.. _advanced_ocean_renderer_options:

Advanced Ocean Parameters
-------------------------

These parameters are found on the *Ocean Renderer* under the *Advanced* heading.

-  **Surface Self-Intersection Mode** - How Crest should handle self-intersections of the ocean surface caused by choppy waves which can cause a flipped underwater effect.
   When not using the portals/volumes, this fix is only applied when within 2 metres of the ocean surface.
   *Automatic* will disable the fix if portals/volumes are used and is the recommended setting.

-  **Underwater Cull Limit** - Proportion of visibility below which ocean will be culled underwater.
   The larger the number, the closer to the camera the ocean tiles will be culled.
