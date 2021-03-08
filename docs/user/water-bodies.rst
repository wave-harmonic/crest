.. _water-bodies:

Water Bodies
============

.. youtube:: jXphUy__J0o

   Water Bodies and Surface Clipping

.. note::

   *Water Bodies* as a complete feature is a work-in-progress.

By default the system generates a water surface that expands our to the horizon in every direction.
There are mechanisms to limit the area:

-  The waves can be generated in a limited area - see the :ref:`local-waves-section` section.
-  The *WaterBody* component, if present, marks areas of the scene where water should be present.
   It can be created by attaching this component to a GameObject and setting the X/Z scale to set the size of the water body.
   If gizmos are enabled an outline showing the size will be drawn in the Scene View.
-  The *WaterBody* component turns off tiles that do not overlap the desired area.
   The *Clip Surface* feature can be used to precisely remove any remaining water outside the intended area.
   Additionally, the clipping system can be configured to clip everything by default, and then areas can be defined where water should be included. See the :ref:`clip-surface-section` section.


Wizard (preview)
----------------

.. TODO: Isn't this available for SRP now, too?

We recently added a 'wizard' to help create this setup. It is available on the *master* branch in this repository, but not yet in Crest URP/HDRP.
It can be used as follows:

-  Open the wizard window by selecting *Window/Crest/Create Water Body*
-  Click *Create Water Body*. A white plane should appear in the Scene View visualising the location and size
-  Set the position using the translation gizmo in the Scene View, or using the *Center position* input
-  Set the size using the *Size X* and *Size Z* inputs
-  Each of the above components are available via the *Create ...* toggles
-  Click *Create* and a water body should be created in the scene
-  Click *Done* to close the wizard

