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

The following only applies to the *Underwater Renderer*.

.. only:: birp

   .. tab:: `BIRP`

      Transparents will need to be rendered after the underwater effect.
      The underwater effect is rendered at the :link:`CameraEvent.AfterForwardAlpha <{UnityDocScriptLink}/Rendering.CameraEvent.AfterForwardAlpha.html>` event.
      They can be rendered after the underwater effect using :link:`Command Buffers <{BIRPDocLink}/GraphicsCommandBuffers.html>`.
      Transparents rendered after the underwater effect will not have the underwater water fog shading applied to them.
      The effect of the fog either needs to be faked by simply ramping the opacity down to 0 based on distance from the camera, or the water fog shader code needs to be included and called from the transparent shader.

.. only:: hdrp

   .. tab:: `HDRP`

      The Submarine example scene demonstrates an underwater transparent effect - the bubbles from the propellors when the submarine is in motion.
      This effect is from the *Bubbles Propellor* GameObject, which is assigned a specific layer *TransparentFX*.
      The particles need to be rendered between the underwater and post-processing passes which is achieved using a *Custom Pass Volume* component attached to the *CustomPassForUnderwaterParticles* GameObject.
      It is configured to render the *TransparentFX* layer in the *Before Post Process* injection point with a priority of "-1" (which orders it to render after the underwater pass).
      Transparents rendered after the underwater effect will not have the underwater water fog shading applied to them.
      The effect of the fog either needs to be faked by simply ramping the opacity down to 0 based on distance from the camera, or the water fog shader code needs to be included and called from the transparent shader.
      The shader *UnderwaterEffectPassHDRP.shader* is a good reference for calculating the underwater effect.
      This will require various parameters on the shader like fog density and others.

.. only:: urp

   .. tab:: `URP`

      Transparents will need to be rendered after the underwater effect.
      The underwater effect is rendered at the *BeforeRenderingPostProcessing* event.
      They can be rendered after the underwater effect using the :link:`Render Objects Render Feature <{URPDocLink}/urp-renderer-feature-how-to-add.html>` set to *BeforeRenderingPostProcessing*.
      Transparents rendered after the underwater effect will not have the underwater water fog shading applied to them.
      The effect of the fog either needs to be faked by simply ramping the opacity down to 0 based on distance from the camera, or the water fog shader code needs to be included and called from the transparent shader.

.. only:: birp or urp

   Render Order `[BIRP] [URP]`
   ---------------------------

   A typical render order for a frame is the following:

   -  Opaque geometry is rendered, writes to opaque depth buffer.
   -  Sky is rendered, probably at zfar with depth test enabled so it only renders outside the opaque surfaces.
   -  Frame colours and depth are copied out for use later in postprocessing.
   -  Ocean renders early in the transparent queue (queue = 2510).

      -  Queue = Geometry+510 `[[BIRP]]`.
         Queue = Transparent-100 `[[URP]]`.
      -  It samples the postprocessing colours and depths, to do refraction.
      -  It reads and writes from the frame depth buffer, to ensure waves are sorted correctly.
      -  It stomps over sky - sky is at zfar and will be fully fogged/obscured by the water volume.
   -  Particles and alpha render. If they have depth test enabled, they will clip against the surface.
   -  Postprocessing runs with the postprocessing depth and colours.

      -  If enabled, underwater postprocess constructs a screenspace mask for the ocean and uses it to draw the underwater effect over the screen.
