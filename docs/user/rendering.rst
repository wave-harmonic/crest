.. _rendering:

Rendering
=========

Transparency
------------

`Crest` is rendered in a standard way for water shaders - in the transparent pass and refracts the scene.
The refraction is implemented by sampling the camera's colour texture which has opaque surfaces only.
It writes to the depth buffer during rendering to ensure overlapping waves are sorted correctly to the camera.
The rendering of other transparent objects depends on the case, see headings below.
Knowledge of render pipeline features, rendering order and shaders is required to solving incompatibilities.

.. _transparent-object-before-ocean-surface:

Transparent Object In Front Of Ocean Surface
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Normal transparent shaders should blend correctly in front of the water surface.
However this will not work correctly for refractive objects.
`Crest` will not be available in the camera's colour texture when other refractive objects sample from it, as the camera colour texture will only contain opaque surfaces.
The end result is `Crest` not being visible behind the refractive object.

.. _transparent-object-after-ocean-surface:

Transparent Object Behind The Ocean Surface
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Alpha blend and refractive shaders will not render behind the water surface.
Other transparent objects will not be part of the camera's colour texture when `Crest` samples from it.
The end result is transparent objects not being visible behind `Crest`.

On the other hand, alpha test / alpha cutout shaders are effectively opaque from a rendering point of view and may be usable in some scenarios.

.. _transparent-object-underwater:

Transparent Object Underwater
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

This is tricky because the underwater effect uses the opaque scene depths in order to render the water fog, which will not include transparents.

.. only:: birp

   .. tab:: `BIRP`

      .. include:: includes/_underwater-curtain-transparents.rst

.. only:: hdrp

   .. tab:: `HDRP`

      The Submarine example scene demonstrates an underwater transparent effect - the bubbles from the propellors when the submarine is in motion.
      This effect is from the *Bubbles Propellor* GameObject, which is assigned a specific layer *TransparentFX*.
      To drive the rendering, the *CustomPassForUnderwaterParticles* GameObject has a *Custom Pass Volume* component attached which is configured to render the *TransparentFX* layer in the *After Post Process* injection point, i.e. after the underwater postprocess has rendered.
      Transparents rendered after the underwater postprocess will not have the underwater water fog shading applied to them.
      The effect of the fog either needs to be faked by simply ramping the opacity down to 0 based on distance from the camera, or the water fog shader code needs included and called from teh transparent shader.
      The shader *UnderwaterPostProcessHDRP.shader* is a good reference for calculating the underwater effect.
      This will require various parameters on the shader like fog density and others.

.. only:: urp

   .. tab:: `URP`

      .. include:: includes/_underwater-curtain-transparents.rst

.. only:: birp or urp

   Render Order `[BIRP] [URP]`
   ---------------------------

   A typical render order for a frame is the following:

   -  Opaque geometry is rendered, writes to opaque depth buffer.
   -  Sky is rendered, probably at zfar with depth test enabled so it only renders outside the opaque surfaces.
   -  Frame colours and depth are copied out for use later in postprocessing.
   -  Ocean 'curtain' renders, draws underwater effect from bottom of screen up to water line.

      -  Queue = Geometry+510 `[[BIRP]]`.
         Queue = Transparent-110 `[[URP]]`.
      -  It is set to render before ocean in UnderwaterEffect.cs.
      -  Sky is at zfar and will be fully fogged/obscured by the water volume.
   -  Ocean renders early in the transparent queue (queue = 2510).

      -  Queue = Geometry+510 `[[BIRP]]`.
         Queue = Transparent-100 `[[URP]]`.
      -  It samples the postprocessing colours and depths, to do refraction.
      -  It reads and writes from the frame depth buffer, to ensure waves are sorted correctly.
      -  It stomps over the underwater curtain to make a correct final result.
      -  It stomps over sky - sky is at zfar and will be fully fogged/obscured by the water volume.
   -  Particles and alpha render. If they have depth test enabled, they will clip against the surface.
   -  Postprocessing runs with the postprocessing depth and colours.
