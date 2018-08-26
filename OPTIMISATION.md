
# Directions for Optimisation

The foundation of *Crest* is architected, but out of the box it is configured for quality and flexibility.

There are a number of directions for optimising the basic vanilla *Crest* that would make sense to explore in production scenarios to squeeze the maximum performance out of the system.


# Tweakable Variables

These are currently available for tweaking and should be explored on every project:

* See the two *Ocean Construction Parameters* on the main README - the LOD count and the base vert density - these directly control how much detail is in the ocean, and therefore the work required to render it.
* The ocean shader has accrued a number of features and has become a reasonably heavy shader. Where possible these are on toggles and can be disabled, which will help the rendering cost.

Consider tweaking these on a per scene/level basis.


# Optimisations Under Consideration

These may make it into *Crest* at some point.

* Pre-rendered wave displacements. Currently *Crest* by default has completely dynamic ocean surface shape which can be tweaked on the fly using the wave spectrum. This could however be offline rendered from an ocean sim, or it could be baked out dynamically at runtime. As described in the seminal *Simulating Ocean Water* paper from Tessendorf, waves can be made to loop in time by quantising the wave speed. In addition I believe it would be possible to bake out a fixed number of textures for each octave - perhaps 16 frames for each octave. Waves with very long period have very long wavelengths/low frequencies, so can probably be sampled with just 16 frames without issues. I'm considering working on a baking tool in Unity to generate the disp texs, potentially on the fly. This could have a big performance saving (wave computation would reduce to a few texture samples).
* Packing ocean shader params - many of the params on the ocean shader are single floats which take a large number of registers. Using a custom material inspector it should be possible to pack these into float4s which will reduce constant buffer size massively and may help perf (or not..).
* Create multiple ocean materials with different sets of features and switch between them at run-time. An obvious use case would be one for shallow water and one for deep water and perhaps switch between them on a per-tile basis. I believe Assassins Creed used a technique like this.
* Decouple displacements used for geometry from displacements used for shading. Right now geometry and displacement values are 1:1 - every texel in the displacement texture is sampled by a vert. Then normal maps are used to give higher frequency detail. @moosichu and I have discussed decoupling these to reduce the amount of geometry but hopefully retain detailed appearance. There are many very small triangles in the mid-to-background right now which are typically inefficient to render, and this might help.
* Limit range of LOD data. There is currently a min/max grid size option on the dynamic wave sim to limit what resolutions it runs at, this could be rolled out to other sim types.
* LOD data such as foam sim/wave sims/etc could be atlased into a single texture and run in one pass. There are quite a few draw calls to run the sims which could collapse significantly. This should help perf but I'm not sure by how much, or what impact this will have on the code/systems.


# Other Optimisations

* GPU-instance ocean material tiles. Discussed in Issue #27.
