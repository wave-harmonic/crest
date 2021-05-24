The base reflection comes from a one of these sources:

-  Unity's specular cubemap.
   This is the default and is the same as what is applied to glossy objects in the scene.
   It will support reflection probes, as long as the probe extents cover the ocean tiles, which enables real-time update of the reflection environment (see Unity documentation for more details).

.. TODO: This feature was removed. Should it be removed in BIRP too? Or added here?
.. -  Override reflection cubemap.
..    If desired a cubemap can be provided to use for the reflections.
..    For best results supply a HDR cubemap.

-  Procedural skybox.
   Developed for stylized games, this is a simple approximation of sky colours that will give soft results.

This base reflection can then be overridden by dynamic planar reflections.
This can be used to augment the reflection with 3D objects such as boat or terrain.
This can be enabled by applying the *Ocean Planar Reflections* script to the active camera and configuring which layers get reflected (don't include the *Water* layer).
This renders every frame by default but can be configured to render less frequently.
This only renders one view but also only captures a limited field of view of reflections, and the reflection directions are scaled down to help keep them in this limited view, which can give a different appearance.
Furthermore 'planar' means the surface is approximated by a plane which is not the case for wavey ocean, so the effect can break down.
This method is good for capturing local objects like boats and etcetera.

A good strategy for debugging the use of Unity's specular cubemap is to put another reflective/glossy object in the scene near the surface, and verify that it is lit and reflects the scene properly.
Crest tries to use the same inputs for lighting/reflections, so if it works for a test object it should work for the water surface as well.
