`Crest` makes full use of the flexible lighting options in `HDRP` (it is lit the same as a shadergraph shader would be).

`HDRP` comes with a *Planar Reflection Probe* feature which enables dynamic reflection of the environment at run-time, with a corresponding cost.
See Unity's documentation on :link:`Planar Reflection Probes <{HDRPDocLink}/Planar-Reflection-Probe.html>`. At time of writing we used the following steps:

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

.. tip::

   If reflections appear wrong, it can be useful to make a simple test shadergraph with our water normal map applied to it, to compare results.
   We provide a simple test shadergraph for debugging purposes - enable the *Apply Test Material* debug option on the *OceanRenderer* component to apply it.
   If you find you are getting good results with a test shadergraph but not with our ocean shader, please report this to us.

.. TODO:
.. Find out why "Index of Refraction" material options are not in HDRP.
