.. _collision-shape-buoyancy-physics:

Collision Shape and Buoyancy Physics
====================================


Collision Shape
---------------

The system has a few paths for computing information about the water surface such as height, displacement, flow and surface velocity.
These paths are covered in the following subsections, and are configured on the *Animated Waves Sim Settings*, assigned to the OceanRenderer script, using the Collision Source dropdown.

The system supports sampling collision at different resolutions.
The query functions have a parameter *Min Spatial Length* which is used to indicate how much detail is desired.
Wavelengths smaller than half of this min spatial length will be excluded from consideration.

To simplify the code required to get the ocean height or other data from C#, two helpers are provided, *SampleHeightHelper* and *SampleFlowHelper*.
Use of these is demonstrated in the example content.

.. TODO: Also add this under development or research?

.. admonition:: Research

   We use a technique called *Fixed Point Iteration* to calculate the water height.
   We gave a talk at GDC about this technique which may be useful to learn more: https://www.gdcvault.com/play/1023011/Fixed-Point-Iteration-A-Simple.

The *Visualise Collision Area* debug component is useful for visualising the collision shape for comparison against the render surface.
It draws debug line crosses in the Scene View around the position of the component.


Collision API Usage
^^^^^^^^^^^^^^^^^^^

The collision providers built intout our system perform queries asynchronously; queries are offloaded to the GPU or to spare CPU cores for processing.
This has a few non-trivial impacts on how the query API must be used.

Firstly, queries need to be registered with an ID so that the results can be tracked and retrieved later.
This ID needs to be globally unique, and therefore should be acquired by calling *GetHashCode()* on an object/component which will be guaranteed to be unique.
A primary reason why *SampleHeightHelper* is useful is that it is an object in itself and there can pass its own ID, hiding this complexity from the user.

.. important::

   Queries should only be made once per frame from an owner - querying a second time using the same ID will stomp over the last query points.

Secondly, even if only a one-time query of the height is needed, the query function should be called every frame until it indicates that the results were successfully retrieved.
See *SampleHeightHelper* and its usages in the code - its *Sample()* function should be called until it returns true.
Posting the query and polling for its result are done through the same function.

Finally due to the above properties, the number of query points posted from a particular owner should be kept consistent across frames.
The helper classes always submit a fixed number of points this frame, so satisfy this criteria.


Compute Shape Queries (GPU)
^^^^^^^^^^^^^^^^^^^^^^^^^^^

This is the default and recommended choice for when a GPU is present.
Query positions are uploaded to a compute shader which then samples the ocean data and returns the
desired results.
The result of the query accurately tracks the height of the surface, including all wave components and depth caches and other Crest features.


.. _collisions-fft-waves-cpu:

Baked FFT Data (CPU)
^^^^^^^^^^^^^^^^^^^^

.. admonition:: Preview

   This feature is in preview and may change in the future.

In scenarios where a GPU is not present such as for headless servers, a CPU option is available.

To use this feature, select a *Shape FFT* component that is generating the waves in a scene and enable the **Enable Baked Collision**.
Next configure the following options:

-  **Time Resolution** - Frames per second of baked data. Larger values may help the collision track the surface closely at the cost of more frames and increase baked data size.
-  **Smallest Wavelength Required** - Smallest wavelength required in collision. To preview the effect of this, disable power sliders in spectrum for smaller values than this number. Smaller values require more resolution and increase baked data size.
-  **Time Loop Length** - FFT waves will loop with a period of this many seconds. Smaller values decrease data size but can make waves visibly repetitive.

Next click **Bake to asset and assign to current settings** and select a path and filename for the result.
After the bake completes the current active *Animated Waves Sim Settings* will be configured to use this data.

.. important::

   There are currently a few key limitations of this approach:

   -  Only a single set of waves from one *Shape FFT* component is supported. This collision does not support multiple sets of waves.
   -  The *Depth Cache* components are not supported. In order to get a one to one match between the visuals and the collision data, depth caches should not be used.
   -  Varying water levels such as rivers flowing down a gradient or lakes at different altitudes is not supported. This feature assumes a fixed sea level for the whole scene.

.. sponsor::

   Sponsoring us will help increase our development bandwidth which could work towards solving the aforementioned limitations.

   .. trello:: https://trello.com/c/EJCQhvsL


.. _collisions-gerstner-waves-cpu:

Gerstner Waves CPU
^^^^^^^^^^^^^^^^^^

.. admonition:: Deprecated

   The *Shape Gerstner Batched* component is deprecated which supported this option.

This collision option is serviced directly by the *Shape Gerstner Batched* component which implements the *ICollProvider* interface, check this interface to see functionality.
This sums over all waves to compute displacements, normals, velocities, etc.
In contrast to the displacement textures the horizontal range of this collision source is unlimited.

A drawback of this approach is the CPU performance cost of evaluating the waves.
It also does not include wave attenuation from water depth or any custom rendered shape.
A final limitation is the current system finds the first *Shape Gerstner Batched* component in the scene which may or may not be the correct one.
The system does not support cross blending of multiple scripts.

.. _buoyancy:

Buoyancy
--------

.. note::

   Buoyancy physics for boats is not a core focus of `Crest`.
   For a professional physics solution we recommend the :link:`{DWP2} <https://assetstore.unity.com/packages/tools/physics/dynamic-water-physics-2-147990?aid=1011lic2K>` asset which is compatible with `Crest`.

   With that said, we do provide rudimentary physics scripts.

*SimpleFloatingObject* is a simple buoyancy script that attempts to match the object position and rotation with the surface height and normal.
This can work well enough for small water craft that don't need perfect floating behaviour, or floating objects such as buoys, barrels, etc.

*BoatProbes* is a more advanced implementation that computes buoyancy forces at a number of *ForcePoints* and uses these to apply force and torque to the object.
This gives more accurate results at the cost of more queries.

*BoatAlignNormal* is a rudimentary boat physics emulator that attaches an engine and rudder to *SimpleFloatingObject*.
It is not recommended for cases where high animation quality is required.
