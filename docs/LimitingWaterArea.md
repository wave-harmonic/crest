# Limiting water area

By default the system generates a water surface that expands our to the horizon in every direction. There are mechanisms to limit the area.

* The waves can be generated in a limited area - see **Local Waves** section above
* The *WaterBody* component, if present, marks areas of the scene where water should be present. It can be created by attaching this component to a GameObject and setting the X/Z scale to set the size of the water body. If gizmos are enabled an outline showing the size will be drawn in the Scene View.
* The *WaterBody* component turns off tiles that do not overlap the desired area. The *Clip Surface* feature can be used to precisely remove any remaining water outside the intended area. Additionally, the clipping system can be configured to clip everything by default, and then areas can be defined where water should be included. See the **Clip Surface** section above.

We recently added a 'wizard' to help create this setup. It is available on the *master* branch in this repository, but not yet in Crest URP/HDRP. It can be used as follows:

* Open the wizard window by selecting *Window/Crest/Create Water Body*
* Click *Create Water Body*. A white plane should appear in the Scene View visualising the location and size
* Set the position using the translation gizmo in the Scene View, or using the *Center position* input
* Set the size using the *Size X* and *Size Z* inputs
* Each of the above components are available via the *Create ...* toggles
* Click *Create* and a water body should be created in the scene
* Click *Done* to close the wizard


# Collision Shape for Physics

The system has a few paths for computing information about the water surface such as height, displacement, flow and surface velocity.
These paths are covered in the following subsections, and are configured on the *Animated Waves Sim Settings*, assigned to the OceanRenderer script, using the Collision Source dropdown.

The system supports sampling collision at different resolutions.
The query functions have a parameter *Min Spatial Length* which is used to indicate how much detail is desired.
Wavelengths smaller than half of this min spatial length will be excluded from consideration.

## Compute Shader Queries

This is the default and recommended choice.
Query positions are uploaded to a compute shader which then samples the ocean data and returns the desired results.
The result of the query accurately tracks the height of the surface, including all shape deformations and waves.

Using the GPU to perform the queries is efficient, but the results can take a couple of frames to return to the CPU. This has a few non-trivial impacts on how it must be used.

Firstly, queries need to be registered with an ID so that the results can be tracked and retrieved from the GPU later.
This ID needs to be globally unique, and therefore should be acquired by calling *GetHashCode()* on an object/component which will be guaranteed to be unique.
**Queries should only be made once per frame from an owner - querying a second time using the same ID will stomp over the last query points**.
A primary reason why *SampleHeightHelper* is useful is that it is an object in itself and there can pass its own ID, hiding this complexity from the user.

Secondly, even if only a one-time query of the height is needed, the query function should be called every frame until it indicates that the results were successfully retrieved.
See *SampleHeightHelper* and its usages in the code - its *Sample()* function should be called until it returns true.
Posting the query and polling for its result are done through the same function.

Finally due to the above properties, the number of query points posted from a particular owner should be kept consistent across frames.
The helper classes always submit a fixed number of points this frame, so satisfy this criteria.

## Gerstner Waves CPU

This collision option is serviced directly by the *GerstnerWavesBatched* component which implements the *ICollProvider* interface, check this interface to see functionality.
This sums over all waves to compute displacements, normals, velocities, etc. In contrast to the displacement textures the horizontal range of this collision source is unlimited.

A drawback of this approach is the CPU performance cost of evaluating the waves.
It also does not include wave attenuation from water depth or any custom rendered shape.
A final limitation is the current system finds the first GerstnerWavesBatched component in the scene which may or may not be the correct one.
The system does not support cross blending of multiple scripts.

## Technical Notes

Sampling the height of a displacement texture is in general non-trivial.
A displacement can define a concave surface with overhanging elements such as a wave that has begun to break.
At such locations the surface has multiple heights, so we need some mechanism to search for a height.
Luckily there is a powerful tool to do this search known as Fixed Point Iteration (FPI).
For an introduction to FPI and a discussion of this scenario see this GDC talk: [link](http://www.huwbowles.com/fpi-gdc-2016/).
Computing this height is relatively expensive as each search step samples the displacement.
To help reduce cost a height cache can be enabled in the *Animated Waves Sim Settings* which will cache the water height at a 2D position so that any subsequent samples in the same frame will quickly return the height.
