Performance Guide
=================

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
   A potential idea would be to change materials on the fly from script, for example to switch to a deep water material when out at sea to avoid shallow water calculations

-  Our wave system uses an inefficient approach to generate the waves to avoid an incompatibility in older hardware.
   If you are shipping on a limited set of hardware which you can test the waves on, you may try disabling the *Ping pong combine* option in the *Animated Wave Settings* asset.


Mobile Performance
------------------

Mobile is not the primary target for `Crest`, but the following are some hints on getting better performance:

-  `Crest` can be draw call heavy which mobile platforms can be sensitive to.
   Together, reducing the LOD count and increasing the Min Scale can significantly reduce draw calls.

-  The *Underwater Renderer* can be very expensive as it will render the water mesh a second time to create a mask.
   Even though it has been deprecated, try using the *Underwater Curtain* instead.

-  Disabling *Transparency* on the ocean material will benefit platforms that use tile-based renderers.
