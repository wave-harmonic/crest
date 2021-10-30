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

Wave Splines (preview)
----------------------

.. youtube:: JRzPcUP5aaA

   Wave Splines

Wave Splines allow flexible and fast authoring of how waves manifest in the world.
A couple of use cases are demonstrated in the video above.

If the *Spline* component is attached to the same GameObject as a *ShapeFFT* component, the waves will be generated along the spline.
This allows for quick experimentation with placing and orienting waves in different areas of the environment.

The *Spline* component can also be combined with the *RegisterHeightInput* to make the water level follow the spline, and with the *RegisterFlowInput* to make water move along the spline.
