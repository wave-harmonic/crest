
# Directions for optimisation

The foundation of *Crest* is architected for performance from the ground up with an innovative LOD system. However, the out-of-the-box example content is configured for quality and flexibility rather than maximum efficiency.

There are a number of directions for optimising the basic vanilla *Crest* that would make sense to explore in production scenarios to squeeze the maximum performance out of the system.


# Tweakable variables

These are currently available for tweaking and should be explored on every project:

* See the *Ocean Construction Parameters* described in the README.md which directly control how much detail is in the ocean, and therefore the work required to update and render it.
* The ocean shader has accrued a number of features and has become a reasonably heavy shader. Where possible these are on toggles and can be disabled, which will help the rendering cost.
* If the collision source is the GPU displacement textures, create an *Animated Waves Sim Settings* asset and set the *Min Object Width* and *Max Object Width* fields to the expect range of object sizes. Due to the dynamic nature of the LOD system underpinning *Crest* these settings can produce non-intuitive results. There is a validation helper function provided for assitance, see the collision section of [TECHNOLOGY.md](https://github.com/huwb/crest-oceanrender/blob/master/TECHNOLOGY.md).

Consider tweaking these on a per scene/level basis.


# Optimisations under consideration

These may make it into *Crest* at some point.

* Pre-rendered wave displacements. Currently *Crest* by default has completely dynamic ocean surface shape which can be tweaked on the fly using the wave spectrum. This could however be offline rendered from an ocean sim, or it could be baked out dynamically at runtime. As described in the seminal *Simulating Ocean Water* paper from Tessendorf, waves can be made to loop in time by quantising the wave speed. In addition I believe it would be possible to bake out a fixed number of textures for each octave - perhaps 16 frames for each octave. Waves with very long period have very long wavelengths/low frequencies, so can probably be sampled with just 16 frames without issues. I'm considering working on a baking tool in Unity to generate the disp texs, potentially on the fly. This could have a big performance saving (wave computation would reduce to a few texture samples).
* Packing ocean shader params - many of the params on the ocean shader are single floats which take a large number of registers. Using a custom material inspector it should be possible to pack these into float4s which will reduce constant buffer size massively and may help perf (or not..).
* Create multiple ocean materials with different sets of features and switch between them at run-time. An obvious use case would be one for shallow water and one for deep water and perhaps switch between them on a per-tile basis. I believe Assassins Creed used a technique like this.
* Limit resolution range of LOD data. There is currently a min/max grid size option on the dynamic wave sim to limit what resolutions it runs at, this could be rolled out to other sim types.
* LOD data such as foam sim/wave sims/etc could be atlased into a single texture and run in one pass. There are quite a few draw calls to run the sims which could collapse significantly. This should help perf but I'm not sure by how much, or what impact this will have on the code/systems.
* Texture readback takes around 0.5ms of main thread CPU time on a Dell XPS 15 laptop (for the default high quality settings in the crest example content). There may be ways to reduce this cost - see issue #60 . See also the object width settings mentioned above.
* The ocean update runs as a command buffer which could potentially be ran asynchronously which may improve utilisation.

# Other optimisations

* GPU-instance ocean material tiles. Discussed in Issue #27.
