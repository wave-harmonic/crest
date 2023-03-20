.. _water-exclusion:

Water Exclusion
===============

Also referred to as clipping or masking, features detailed here can either exclude or include the surface and/or volume.

.. _clip-surface-section:

Clip Surface
------------

.. youtube:: jXphUy__J0o

   Water Bodies and Surface Clipping

This data drives clipping of the ocean surface (has no effect on the volume), as in carving out holes.
It is similar to Unity's Terrain Holes feature.
This can be useful for hollow vessels or low terrain that goes below sea level.
Data can come from primitives (signed-distance), geometry (convex hulls) or a texture.

To turn on this feature, enable the *Create Clip Surface Data* option on the *OceanRenderer* script, and ensure the *Enable* option is ticked in the *Clip Surface* group on the ocean material.

The data contains 0-1 values. Holes are carved into the surface when the value is greater than 0.5.

.. _clip_surface_settings:

Simulation Settings
^^^^^^^^^^^^^^^^^^^

All of the settings below refer to the *Clip Surface Sim Settings* asset.

-  **Render Texture Graphics Format** - The render texture format to use for the clip surface simulation.
   Consider using higher precision (like *R16_UNorm*) if you are using *Primitive* mode for even more accurate clipping.

.. _clip_surface_inputs:

User Inputs
^^^^^^^^^^^

The *Register Clip Surface Input* input only supports the modes listed in the *Mode* dropdown.

Primitive Mode
~~~~~~~~~~~~~~

Clip areas can be added using signed-distance primitives which produces accurate clipping and supports overlapping.
Add a *RegisterClipSurfaceInput* script to a *GameObject* and set *Mode* to *Primitive*.
The position, rotation and dimensions of the primitive is determined by the *Transform*.
See the *FloatingOpenContainer* object in the *boat.unity* scene for an example usage.

Geometry Mode
~~~~~~~~~~~~~

Clip areas can be added by adding geometry that covers the desired hole area to the scene and then assigning the *RegisterClipSurfaceInput* script and setting *Mode* to *Geometry*.
See the *RowBoat* object in the *main.unity* scene for an example usage.

To use other available shaders like *ClipSurfaceRemoveArea* or *ClipSurfaceRemoveAreaTexture*: create a material, assign to renderer and disable *Assign Clip Surface Material* option.
For the *ClipSurfaceRemoveArea* shaders, the geometry should be added from a top-down perspective and the faces pointing upwards.

The following input shaders are provided under *Crest/Inputs/Clip Surface*:

-  **Convex Hull** - Renders geometry into clip surface data taking all dimensions into account.
   An example use case is rendering the convex hull of a vessel to remove the ocean surface from within it.

   .. admonition:: Example

      See the *RowBoat* object in the *main.unity* scene for an example usage.

   .. note::

      Overlapping or adjacent meshes will not work correctly in most cases.
      There will be cases where one mesh will overwrite another resulting in the ocean surface appearing where it should not.
      The mesh is rendered from a top-down perspective.
      The back faces add clip surface data and the front faces remove from it which creates the convex hull.
      With an overlapping mesh, the front faces of the sides of one mesh will clear the clipping data creating by the other mesh.
      Overlapping boxes which are not rotated on the X or Z axes will work well whilst spheres will have issues.
      Consider using *Primitive* mode which supports overlapping.

-  **Include Area** - Removes clipping data so the ocean surface renders.

-  **Remove Area** - Adds clipping data to remove the ocean surface.

-  **Remove Area Texture** - Adds clipping data using a texture to remove the ocean surface.

Mask Underwater
---------------

The :ref:`portals-volumes` feature can remove both the water surface and the underwater volume.
Otherwise, enable/disable the *Underwater Renderer* where needed.
