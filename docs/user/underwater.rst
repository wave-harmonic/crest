.. _underwater:

Underwater
==========

*Crest* supports seamless transitions above/below water.
It can also has a meniscus which renders a subtle line at the intersection between the camera lens and the water to visually help the transition.
This is demonstrated in the *main.unity* scene in the example content.
The ocean in this scene uses the material *Ocean-Underwater.mat* which enables rendering the underside of the surface.

Out-scattering is provided as an example script which reduces environmental lighting with depth underwater.
See the *UnderwaterEnvironmentalLighting* component.

For performance reasons, the underwater effect is disabled if the viewpoint is not underwater.
Only the camera rendering the ocean surface will be used.

.. tip::

   Use opaque or alpha test materials for underwater surfaces.
   Transparent materials may not render correctly underwater.
   See :ref:`transparent-object-underwater` for possible workarounds.

.. admonition:: Bug

   Underwater effects do *not* support orthographic projection.


.. only:: birp or urp

   Underwater Curtain `[BIRP] [URP]`
   -----------------------------------

   In the *main.unity* scene, the *UnderWaterCurtainGeom* and *UnderWaterMeniscus* prefabs are parented to the camera which renders the underwater effects.

   Checklist for using underwater:

   -  Configure the ocean material for underwater rendering.
      In the *Underwater* section of the material params, ensure *Enabled* is turned on and *Cull Mode* is set to *Off* so that the underside of the ocean surface renders.
      See *Ocean-Underwater.mat* for an example.

   -  Place *UnderWaterCurtainGeom* and *UnderWaterMeniscus* prefabs under the camera (with cleared transform).


.. only:: hdrp

   Underwater Post-Process `[HDRP]`
   --------------------------------

   .. image:: /_media/UnderwaterPostProcess.png

   .. only:: html or readthedocs

      Unlike the Underwater Curtain, the custom post-process effect is pixel-perfect.


   .. _underwater_pp_setup:

   Setup steps
   ^^^^^^^^^^^

   Steps to set up underwater:

   #. Ensure Crest is properly set up and working before proceeding.

   #. Enable :link:`Custom Pass on the {HDRP} Asset <{HDRPDocLink}/HDRP-Asset.html#rendering>` and ensure that :link:`Custom pass on the camera's Frame Settings <{HDRPDocLink}/Frame-Settings.html#rendering>` is not disabled.

   #. Add the custom post-process (*Crest.UnderwaterPostProcessHDRP*) to the *Before Post Process* list.
      See the :link:`Custom Post Process documentation <{HDRPDocLink}/Custom-Post-Process.html#effect-ordering>`.

      .. note:: For Unity 2020.2+/`HDRP` 10+, use *Before TAA*. This will fix the outline on alpha clipped objects when undewater.

   #. Add the *Crest/Underwater* :link:`Volume Component <{HDRPDocLink}/Volume-Components.html>`.

      -  Please learn how to use the *Volume Framework* before proceeding as covering this is beyond the scope of our documentation:

         .. youtube:: vczkfjLoPf8

            Adding Volumes to `HDRP` (Tutorial)

   #. Configure the ocean material for underwater rendering - in the *Underwater* section of the material params, ensure *Cull Mode* is set to *Off* so that the underside of the ocean surface renders.
      See *Ocean-Underwater.mat* for an example.


.. _detecting_above_or_below_water:

Detecting Above or Below Water
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The *OceanRenderer* component has the *ViewerHeightAboveWater* property which can be accessed with ``OceanRenderer.Instance.ViewerHeightAboveWater``.
It will return the signed height from the ocean surface of the camera rendering the ocean.
Internally this uses the *SampleHeightHelper* class which can be found in *SamplingHelpers.cs*.

There is also the *OceanSampleHeightEvents* example component (requires example content to be imported) which uses :link:`UnityEvents <{UnityDocLink}/UnityEvents.html>` to provide a scriptless approach to triggering changes.
Simply attach it to a game object, and it will invoke a UnityEvent when the attached game object is above or below the ocean surface once per state change. A common use case is to use it to trigger different audio when above or below the surface.
