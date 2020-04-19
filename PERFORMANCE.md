
# Performance

The foundation of *Crest* is architected for performance from the ground up with an innovative LOD system. It is tweaked to achieve a good balance between quality and performance in the general case, but getting the most out of the system requires tweaking the parameters for the particular use case. These are documented below.


## Quality parameters

These are currently available for tweaking and should be explored on every project:

* See the *Ocean Construction Parameters* described in the USERGUIDE.md which directly control how much detail is in the ocean, and therefore the work required to update and render it.
* The ocean shader has accrued a number of features and has become a reasonably heavy shader. Where possible these are on toggles and can be disabled, which will help the rendering cost.
* The number of wave components will affect the update cost. This can be reduced by turning down sliders in the wave spectrum, and by reducing the *Components per Octave* setting on the *OceanGerstnerBatched* script.

Consider tweaking these on a per scene/level basis.


## Potential optimisations

The following are optimisation ideas.

* The ocean update runs as a command buffer, portions of which could potentially be ran asynchronously which may improve GPU utilisation. For example dynamic waves and gerstner waves could be computed in parallel before the combine pass merges them together.
* Create multiple ocean materials with different sets of features and switch between them at run-time. An obvious use case would be one for shallow water and one for deep water and perhaps switch between them on a per-tile basis. I believe Assassins Creed used a technique like this.
* Limit resolution range of LOD data. There is currently a min/max grid size option on the dynamic wave sim to limit what resolutions it runs at, this could be rolled out to other sim types.
* Set LOD data resolution per LOD type - e.g. the ocean depth and foam sim could potentially be stored at half resolution.
* LOD data such as foam sim/wave sims/etc could be atlased into a single texture and run in one pass. There are quite a few draw calls to run the sims which could collapse significantly. This should help perf but I'm not sure by how much, or what impact this will have on the code/systems.
* Texture readback takes around 0.5ms of main thread CPU time on a Dell XPS 15 laptop (for the default high quality settings in the crest example content). There may be ways to reduce this cost - see issue #60 .
* GPU-instance ocean material tiles. Discussed in Issue #27. Not currently planned for *Crest*.
* Pre-rendered wave displacements - sample waves from texture instead of computing them on the fly. I tried this out in the branch *feature/baked-waves* and found it challenging to get good shape without interpolation artifacts. Given that the baking step is also inconvenient, there are no plans to explore this further.
* Packing ocean shader params - many of the params on the ocean shader are single floats which take a large number of registers. Using a custom material inspector it should be possible to pack these into float4s which will reduce constant buffer size massively and may help perf (or not..).
