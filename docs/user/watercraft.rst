.. _watercraft:

Watercraft
==========

.. note::

   Buoyancy physics for boats is not a core focus of `Crest`.
   For a professional physics solution we recommend the :link:`{DWP2} <https://assetstore.unity.com/packages/tools/physics/dynamic-water-physics-2-147990?aid=1011lic2K>` asset which is compatible with `Crest`.


Boats
-----

Adding Boats
^^^^^^^^^^^^

This section describes the simplest way to add a boat, including buoyancy and wakes, to your project.

Setting up a boat with physics can be a dark art.
The authors recommend duplicating and modifying one of the existing boat prefabs, and proceeding slowly and carefully as follows:

#. Pick an existing boat to replace. Only use *BoatAlignNormal* if good floating behaviour is not important, as mentioned above.
   The best choice is usually *BoatProbes*.

#. Duplicate the prefab of the one you want to replace, such as *crest/Assets/Crest/Crest-Examples/BoatDev/Data/BoatProbes.prefab*

#. Remove the render meshes from the prefab, and add the render mesh for your boat.
   We recommend lining up the meshes roughly.

#. Switch out the collision shape as desired.
   Some people report issues if the are multiple overlapping physics collision primitives (or multiple rigidbodies which should never be the case).
   We recommend keeping things as simple as possible and using only one collider if possible.

#. We recommend placing the render mesh so its approximate center of mass matches the center of the collider and is at the center of the boat transform.
   Put differently, we usually try to eliminate complex hierarchies or having nested non-zero'd transforms whenever possible within the boat hierarchy, at least on or above physical parts.

#. If you have followed these steps you will have a new boat visual mesh and collider, with the old rigidbody and boat script.
   You can then modify the physics settings to move the behaviour towards how you want it to be.

#. The mass and drag settings on the boat scripts and rigdibody help to give a feeling of weight.

#. Set the boat dimension:

   -  BoatProbes: Set the *Min Spatial Length* param to the width of the boat.
   -  BoatAlignNormal: Set the *Boat Width* and *Boat Length* params to the width and length of the boat.
   -  If, even after experimenting with the mass and drag, the boat is responding too much to small waves, increase these parameters (try doubling or quadrupling at first and then compensate).

#. There are power settings for engine turning which also help to give a feeling of weight.

#. The dynamic wave interaction is driven by the object in the boat hierarchy called *SphereWaterInteraction*.
   It can be scaled to match the dimensions of the boat.
   The *Weight* param controls the strength of the interaction.

The above steps should maintain a working boat throughout - we recommend testing after each step to catch issues early.


Adding Buoyancy
^^^^^^^^^^^^^^^

The simplest method to adding buoyancy is detailed above.
Further details about buoyancy components can be found in the :ref:`buoyancy` section.


Adding Wakes
^^^^^^^^^^^^

The *Sphere Water Interaction* component is used to add wakes.
See :ref:`adding-interaction-forces` section for more information on this component.


Removing Water From Inside Boat
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

There are various methods to removing water from Crest detailed on the :ref:`water-exclusion` page.
