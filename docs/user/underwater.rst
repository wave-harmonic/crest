.. _underwater:

Underwater
==========

.. TODO: I have placed common documentation before pipeline specific documentation. Need to fix.
.. TODO: Mention meniscus

*Crest* supports seamless transitions above/below water.
It can also render a meniscus (seam).
This is demonstrated in the *main.unity* scene in the example content.
The ocean in this scene uses the material *Ocean-Underwater.mat* which enables rendering the underside of the surface.

Out-scattering is provided as an example script which reduces environmental lighting with depth underwater.
See *UnderwaterEnvironmentalLighting*.

For performance reasons, the underwater effect is disabled if the viewpoint is not underwater.
Only the camera rendering the ocean surface will be used.

.. TODO: refer to a common camera section

.. tip::

   Use opaque or alpha test materials for underwater surfaces.
   Transparent materials may not render correctly underwater.

.. admonition:: Bug

   Underwater effects do *not* support orthographic projection.

.. only:: birp or urp

   Underwater Curtain `[BIRP]` `[URP]`
   -----------------------------------

   In the *main.unity* scene, the *UnderWaterCurtainGeom* prefab is parented to the camera which renders the underwater effect.
   It also has the prefab *UnderWaterMeniscus* parented which renders a subtle line at the intersection between the camera lens and the water to visually help the transition.

   Checklist for using underwater:

   -  Configure the ocean material for underwater rendering.
      In the *Underwater* section of the material params, ensure *Enabled* is turned on and *Cull Mode* is set to *Off* so that the underside of the ocean surface renders.
      See *Ocean-Underwater.mat* for an example.

   -  Place *UnderWaterCurtainGeom* and *UnderWaterMeniscus* prefabs under the camera (with cleared transform).


.. only:: hdrp

   Underwater Post-Process `[HDRP]`
   --------------------------------

   .. image:: /_media/UnderwaterPostProcess.png


   Unlike the Underwater Curtain, the custom post-process effect is pixel-perfect.


   Setup steps
   ^^^^^^^^^^^

   Steps to set up underwater:

   #. Ensure Crest is properly set up and working before proceeding.

   #. Add the custom post-process (*Crest.UnderwaterPostProcessHDRP*) to the *Before Post Process* list.
      See the :link:`Custom Post Process documentation <{HDRPDocLink}/Custom-Post-Process.html#effect-ordering}>`

      .. note:: For Unity 2020.2+/`HDRP` 10+, use *Before TAA*. This will fix the outline on alpha clipped objects when undewater.

   #. Configure the ocean material for underwater rendering - in the *Underwater* section of the material params, ensure *Cull Mode* is set to *Off* so that the underside of the ocean surface renders.
      See *Ocean-Underwater.mat* for an example.
