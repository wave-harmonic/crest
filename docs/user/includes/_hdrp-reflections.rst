`Crest` makes full use of the flexible lighting options in `HDRP` (it is lit the same as a shadergraph shader would be).


.. only:: html

   .. raw:: html

      <h4>Planar Reflection Probes</h4>

.. only:: latex

   Planar Reflection Probes
   """"""""""""""""""""""""

`HDRP` comes with a *Planar Reflection Probe* feature which enables dynamic reflection of the environment at run-time, with a corresponding cost.
See Unity's documentation on :link:`Planar Reflection Probes <{HDRPDocLink}/Planar-Reflection-Probe.html>`.
At time of writing we used the following steps:

-  Create new GameObject
-  Set the height of the GameObject to the sea level.
-  Add the component from the Unity Editor menu using *Component/Rendering/Planar Reflection Probe*
-  Set the extents of the probe to be large enough to cover everything that needs to be reflected. We recommend starting large (1000m or more as a starting point).
-  Ensure water is not included in the reflection by deselecting *Water* on the *Culling Mask* field
-  Check the documentation linked above for details on individual parameters

`HDRP`'s planar reflection probe is very sensitive to surface normals and often 'leaks' reflections, for example showing the reflection of a boat on the water above the boat.
If you see these issues we recommend reducing the *Overall Normal Strength* parameter on the ocean material.

The planar reflection probe assumes the reflecting surface is a flat plane.
This is not the case for for a wavey water surface and this can also produce 'leaky' reflections.
In such cases it can help to lower the reflection probe below sea level slightly.


.. only:: html

   .. raw:: html

      <h4>Screen-Space Reflections</h4>

.. only:: latex

   Screen-Space Reflections
   """"""""""""""""""""""""

`HDRP` has a separate setting for transparents to receive `SSR` and it is not enabled by default.
It is important that you understand the basics of `HDRP` before proceeding.

#. Enable *Screen Space Refection* and the *Transparent* sub-option in the :link:`Frame Settings <{HDRPDocLink}/Frame-Settings.html>`.
#. Add and configure the :link:`{SSR} Volume Override <{HDRPDocLink}/Override-Screen-Space-Reflection.html>`

   -  Please learn how to use the *Volume Framework* before proceeding as covering this is beyond the scope of our documentation:

      .. youtube:: vczkfjLoPf8

         Adding Volumes to `HDRP` (Tutorial)

#. Enable *Receives Screen-Space Reflections* on the ocean material.
