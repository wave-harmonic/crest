.. _wave-conditions-section:

Wave Conditions
===============

The following sections describe how to define the wave conditions.

.. tip::

   It is useful to see the animated ocean surface while tweaking the wave conditions.

   .. include:: includes/_animated-materials.rst


Wave Systems
------------

The *ShapeFFT* component is used to generate waves in Crest.

For advanced situations where a high level of control is required over the wave shape, the *ShapeGerstner* component can be used to add specific wave components.

.. _wave-authoring-section:

Authoring
---------

To add waves, add the *ShapeFFT* component to a GameObject (comparison of the two options above).

The appearance and shape of the waves is determined by a *Wave Spectrum*.
A default wave spectrum will be created if none is specified.
To author wave conditions, click the *Create Asset* button next to the *Spectrum* field. The resulting spectrum can then be edited by expanding this field.

The spectrum can be freely edited in Edit mode, and is locked by default in Play mode to save evaluating the spectrum every frame (this optimisation can be disabled using the *Spectrum Fixed At Runtime* toggle).
The spectrum has sliders for each wavelength to control contribution of different scales of waves.
To control the contribution of 2m wavelengths, use the slider labelled '2'.
Note that the wind speed may need to be increased on the *OceanRenderer* component in order for large wavelengths to be visible.

There is also control over how aligned waves are to the wind direction.
This is controlled via the *Wind Turbulence* control on the *ShapeFFT* component.

Another key control is the *Chop* parameter which scales the horizontal displacement.
Higher chop gives crisper wave crests but can result in self-intersections or 'inversions' if set too high, so it needs to be balanced.

To aid in tweaking the spectrum, we provide a standard empirical wave spectrum model from the literature, called the 'Pierson-Moskowitz' model.
To apply this model to a spectrum, select it in the *Empirical Spectra* section of the spectrum editor which will lock the spectrum to this model.
The model can be disabled afterwards which will unlock the spectrum power sliders for hand tweaking.

.. tip::

   Notice how the empirical spectrum places the power slider handles along a line.
   This is typical of real world wave conditions which will have linear power spectrums on average.
   However actual conditions can vary significantly based on wind conditions, land masses, etc, and we encourage experimentation to obtain visually interesting wave conditions, or conditions that work best for gameplay.


Together these controls give the flexibility to express the great variation one can observe in real world seascapes.


.. _wave-splines-section:

Wave Splines
------------

.. admonition:: Preview

   This feature is in preview and may change in the future.

.. youtube:: JRzPcUP5aaA

   Wave Splines

Wave Splines allow flexible and fast authoring of how waves manifest in the world.
A couple of use cases are demonstrated in the video above.

If the *Spline* component is attached to the same GameObject as a *ShapeFFT* component, the waves will be generated along the spline.
This allows for quick experimentation with placing and orienting waves in different areas of the environment.

The *Spline* component can also be combined with the *RegisterHeightInput* to make the water level follow the spline, and with the *RegisterFlowInput* to make water move along the spline.

Shoreline Wave Simulation
-------------------------

Crest provides a water simulation that can handle fluid dynamics in shallow water efficiently, known as a Shallow Water Simulation. This can be combined with normal water waves to simulated shoreline wave behaviour.

To add this simulation to the world, place a GameObject roughly at the center of the simulation area and add the *ShallowWaterSimulation* component.

The following parameters are used to setup the simulation:

- **Domain Width** - The width of the simulation area (m). Enable gizmos to see a wireframe outline of the domain.
- **Water Depth** - The depth of the water in the shallow water simulation (m). Any underwater surfaces deeper than this depth will not influence the sim. Large values can lead to instabilities / jitter in the result.
- **Texel Size** - Simulation resolution; width of simulation grid cell (m). Smaller values will increase resolution but take more computation time and memory, and may lead to instabilities for small values.
- **Max Resolution** - Maximum resolution of simulation grid. Safety limit to avoid simulation using large amount of video memory.
- **Drain Water At Boundaries** - Rate at which to remove water at the boundaries of the domain, useful for preventing buildup of water when simulating shoreline waves.
- **Friction** - Friction applied to water to prevent dampen velocities.
- **Maximum Velocity** - Maximum velocity that simulation is allowed to contain (m/s).

The following parameters provide control over the blend between the normal water waves and the shallow water simulation:

- **Blend Shallow Min Depth** - The minimum depth for blending (m). When the water depth is less than this value, animated waves will not contribute at all, water shape will come purely from this simulation. Negative depths are valid and occur when surfaces are above sea level.
- **Blend Shallow Max Depth** - The maximum depth for blending (m). When the water depth is greater than this value, this simulation will not contribute at all, water shape will come purely from the normal ocean waves. Negative depths are valid and occur when surfaces are above sea level.
- **Blend Push Up Strength** - The intensity at which ocean waves inject water into the simulation.

The following parameters control distance culling to shut down the simulation when the viewpoint is not nearby to save runtime performance cost:

- **Enable Distance Culling** - Disable simulation when viewpoint far from domain.
- **Cull Distance** - Disable simulation if viewpoint (main camera or Viewpoint transform set on OceanRenderer component) is more than this distance outside simulation domain.
