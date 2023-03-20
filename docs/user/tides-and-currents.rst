Tides and Currents
==================

.. _flow-section:

Flow
----

Overview
^^^^^^^^

Flow is the horizontal motion of the water volumes.
It does not affect wave directions, but transports the waves horizontally.
This horizontal motion also affects physics.

This can be used to simulate water currents and other waterflow.

.. admonition:: Example

   See the *whirlpool.unity* example scene where flow is used to rotate the waves and foam around the vortex.


User Inputs
^^^^^^^^^^^

Foam supports :ref:`wave-splines-section` and :ref:`renderer-mode`.

Crest supports adding any flow velocities to the system.
To add flow, add some geometry into the world which when rendered from a top down perspective will draw the desired displacements.
Then assign the *RegisterFlowInput* script which will tag it for rendering into the flow, and apply a material using one of the following shaders.

The following input shaders are provided under *Crest/Inputs/Flow*:

The *Crest/Inputs/Flow/Add Flow Map* shader writes a flow texture into the system.
It assumes the x component of the flow velocity is packed into 0-1 range in the red channel, and the z component of the velocity is packed into 0-1 range in the green channel.
The shader reads the values, subtracts 0.5, and multiplies them by the provided scale value on the shader.
The process of adding ocean inputs is demonstrated in :numref:`adding-inputs-video`.


Tides
-----

It is possible to move the entire water surface on the Y axis to simulate tides.
