Performance
===========

The foundation of *Crest* is architected for performance from the ground up with an innovative LOD system.
It is tweaked to achieve a good balance between quality and performance in the general case, but getting the most out of the system requires tweaking the parameters for the particular use case.
These are documented below.


Quality parameters
------------------

These are available for tweaking out of the box and should be explored on every project:

-  See :ref:`ocean_construction_parameters` for parameters that directly control how much detail is in the ocean, and therefore the work required to update and render it.
   These are the primary quality settings from a performance point of view.

-  The ocean shader has accrued a number of features and has become a reasonably heavy shader.
   Where possible these are on toggles and can be disabled, which will help the rendering cost (see :ref:`material_parameters`).

-  Our Gerstner system uses an inefficient approach to generate the waves to avoid an incompatibility in older hardware.
   If you are shipping on a limited set of hardware which you can test the waves on, you may try disabling the *Ping pong combine* option in the *Animated Wave Settings* asset.


Potential Optimisations
-----------------------

The following are optimisation ideas that have not been explored further in *Crest* yet, but are listed as ideas in case the reader has the resources to try them out.

-  The ocean update runs as a command buffer, portions of which could potentially be ran asynchronously which may improve GPU utilisation.
   For example dynamic waves and Gerstner waves could be computed in parallel before the combine pass merges them together.
   We attempted this but found platform support is currently too limited to allow us to implement this in core *Crest*.

-  Create multiple ocean materials with different sets of features and switch between them at run-time.
   An obvious use case would be one for shallow water and one for deep water and perhaps switch between them
   on a per-tile basis.

-  Limit resolution range of LOD data.
   There is currently a min/max grid size option on the dynamic wave sim to limit what resolutions it runs at, this could be rolled out to other sim types.

-  Set LOD data resolution per LOD type - e.g. the ocean depth and foam sim could potentially be stored at half resolution or lower.

-  Pre-rendered wave displacements - sample waves from texture instead of computing them on the fly.
   We did some initial experimentation in the branch :branch:`feature/baked-waves` and found it challenging to get good shape without interpolation artifacts.
   Given that the baking step is also inconvenient, there are no plans to explore this further.
