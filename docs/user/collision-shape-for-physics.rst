Collision Shape for Physics
===========================

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
   We gave a talk at GDC about this technique which may be useful to learn more: http://www.huwbowles.com/fpi-gdc-2016/.


Compute Shape Queries
---------------------

.. sponsor::

   Sponsoring us will help increase our development bandwidth which could work towards using Burst/Jobs for this feature.

   .. trello:: https://trello.com/c/qUvB1aSO

This is the default and recommended choice.
Query positions are uploaded to a compute shader which then samples the ocean data and returns the
desired results.
The result of the query accurately tracks the height of the surface, including all shape deformations and waves.

Using the GPU to perform the queries is efficient, but the results can take a couple of frames to return to the CPU.
This has a few non-trivial impacts on how it must be used.

Firstly, queries need to be registered with an ID so that the results can be tracked and retrieved from the GPU later.
This ID needs to be globally unique, and therefore should be acquired by calling *GetHashCode()* on an object/component which will be guaranteed to be unique.
A primary reason why *SampleHeightHelper* is useful is that it is an object in itself and there can pass its own ID, hiding this complexity from the user.

.. important::

   Queries should only be made once per frame from an owner - querying a second time using the same ID will stomp over the last query points.

Secondly, even if only a one-time query of the height is needed, the query function should be called every frame until it indicates that the results were successfully retrieved.
See *SampleHeightHelper* and its usages in the code - its *Sample()* function should be called until it returns true.
Posting the query and polling for its result are done through the same function.

Finally due to the above properties, the number of query points posted from a particular owner should be kept consistent across frames.
The helper classes always submit a fixed number of points this frame, so satisfy this criteria.

Gerstner Waves CPU
------------------

.. admonition:: Deprecated

   The Gerstner wave system in Crest is now deprecated. A CPU query path for the FFT waves is being worked on.

This collision option is serviced directly by the *GerstnerWavesBatched* component which implements the *ICollProvider* interface, check this interface to see functionality.
This sums over all waves to compute displacements, normals, velocities, etc.
In contrast to the displacement textures the horizontal range of this collision source is unlimited.

A drawback of this approach is the CPU performance cost of evaluating the waves.
It also does not include wave attenuation from water depth or any custom rendered shape.
A final limitation is the current system finds the first *GerstnerWavesBatched* component in the scene which may or may not be the correct one.
The system does not support cross blending of multiple scripts.
