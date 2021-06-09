.. _wave-conditions-section:

Wave Conditions
===============

The following sections describe how to define the wave conditions.

.. tip::

    It is useful to see the animated ocean surface while tweaking the wave conditions.

    .. include:: includes/_animated-materials.rst


.. _wave-authoring-section:

Authoring
---------

To add waves, add the *ShapeGerstner* component to a GameObject.

The appearance and shape of the waves is determined by a *Wave Spectrum*.
A default wave spectrum will be created if none is specified.
To change the waves, right click in the Project view and select *Create/Crest/Ocean Wave Spectrum*, and assign the new asset to the *Spectrum* property of the *ShapeGerstner* script.

The spectrum has sliders for each wavelength to control contribution of different scales of waves.
To control the contribution of 2m wavelengths, use the slider labelled '2'.

The *Wave Direction Variance* controls the spread of wave directions.
This controls how aligned the waves are to the wind direction.

The *Chop* parameter scales the horizontal displacement.
Higher chop gives crisper wave crests but can result in self-intersections or 'inversions' if set too high, so it needs to be balanced.

To aid in tweaking the spectrum values we provide implementations of common wave spectra from the literature.
Select one of the spectra by toggling the button, and then tweak the spectra inputs, and the spectrum values will be set according to the selected model.
When done, toggle the button off to stop overriding the spectrum.

Together these controls give the flexibility to express the great variation one can observe in real world seascapes.


.. _local-waves-section:

Local Waves
-----------

By default the Gerstner waves will apply everywhere throughout the world, so 'globally'.
They can also be applied 'locally' - in a limited area of the world.

This is done by setting the *Mode* to *Geometry*.
In this case the system will look for a *MeshFilter/MeshRenderer* on the same GameObject and it will generate waves over the area of the geometry.
The geometry must be 'face up' - it must be visible from a top-down perspective in order to generate the waves.
It must also have a material using the *Crest/Inputs/Animated Waves/Gerstner Batch Geometry* shader applied.

For a concrete example, see the *GerstnerPatch* object in *boat.unity*.
It has a *MeshFilter* component with the *Quad* mesh applied, and is rotated so the quad is face up.
It has a *MeshRenderer* component with a material assigned with a Gerstner material.

The material has the *Feather at UV Extents* option enabled, which will fade down the waves where the UVs go to 0 or 1 (at the edges of the quad).
A more general solution is to scale the waves based on vertex colour so weights can be painted - this is provided through the *Weight from vertex colour (red channel)* option.
This allows different wave conditions in different areas of the world with smooth blending.


ShapeGerstnerBatched
--------------------

.. deprecated:: 4.9

    *ShapeGerstnerBatched* will be replaced by the much improved *ShapeGerstner*.


.. _shape-gerstner-section:

ShapeGerstner (preview)
-----------------------

A new Gerstner wave system has been added, intended to replace the current system.
It can be tested by adding a *ShapeGerstner* component to a GameObject.
The settings and behaviour are quite similar to the current system (described above).
The new system has the following advantages:

-  Much lower ocean update CPU cost per-frame (35% reduction in our tests for just one Gerstner component).
   Part of this efficiency comes from not recalculating the wave spectrum at run-time by default as toggled by the *Spectrum is static* option on the *ShapeGerstner* component.
   When this optimisation is enabled, *waves must be edited in edit mode (not in play mode)*.
-  Lower GPU cost (0.12ms saved for wave generation in our tests)
-  Wave foam generation works much better in background thanks to a new wave variance statistic
-  Support for wave splines (see below)

After more testing we will switch over to this new system and deprecate the *ShapeGerstnerBatched* component.


.. _shape-fft-section:

ShapeFFT (preview)
------------------

A wave simulation based on the `FFT` technique.

The usage is very similar to the *ShapeGerstner* component.
Add the *ShapeFFT* component to a GameObject, and follow the authoring instructions above to modify the wave conditions.

This simulation type adds more wave components together than the *ShapeGerstner* component and can produce more realistic water waves, at a similar performance cost.

.. _wave-splines-section:

Wave Splines (preview)
----------------------

.. youtube:: JRzPcUP5aaA

   Wave Splines

While it is possible to use the above steps to place localised waves in the world, we added a new system we call *Wave Splines* to make it easier and faster.

As part of this system, we added a generic *Spline* component which is in itself a useful spline tool which could be re-used for other purposes.

If the *Spline* component is attached to the same GameObject as a *ShapeGerstner* component, the waves will be generated along the spline.
This allows for quick experimentation with placing and orienting waves in different areas of the environment.
