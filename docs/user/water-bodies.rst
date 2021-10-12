.. _water-bodies:

Oceans, Rivers and Lakes
========================

.. note::

   The features described in this section are in preview and may evolve in future versions.

Oceans
------

By default Crest generates an infinite body of water at a fixed sea level, suitable for oceans and very large lakes.

Lakes
-----

Crest can be configured to efficiently generate smaller bodies of water, using the following mechanisms.

-  The waves can be generated in a limited area - see the :ref:`wave-splines-section` section.
-  The *WaterBody* component, if present, marks areas of the scene where water should be present.
   It can be created by attaching this component to a GameObject and setting the X/Z scale to set the size of the water body.
   If gizmos are enabled an outline showing the size will be drawn in the Scene View.
-  The *WaterBody* component turns off tiles that do not overlap the desired area.
   The *Clip Surface* feature can be used to precisely remove any remaining water outside the intended area.
   Additionally, the clipping system can be configured to clip everything by default, and then areas can be defined where water should be included. See the :ref:`clip-surface-section` section.
-  If the lake altitude differs from the global sea level, create a spline that covers the area of the lake and attach the *RegisterHeightInput* component which will set the water level to match the spline (or click the *Set Height* button in the *Spline* inspector).
   It is recommended to cover a larger area than the lake itself, to give a protective margin against LOD effects in the distance.

Another advantage of the *WaterBody* component is it allows an optional override material to be provided, to change the appearance of the water.
This currently only changes the appearance of the water surface, it does not currently affect the underwater effect.

Rivers
------

Splines can also be used to create rivers, by creating a spline at the water surface of the river, and attaching the following components:

-  *RegisterHeightInput* can be used to set the water level to match the spline.
-  *RegisterFlowInput* can be used to make the water move along the spline.
-  *ShapeFFT* can be used to generate waves that propagate along the river.

The *Add Feature* section of the *Spline* inspector has helper buttons to quickly add these components.
