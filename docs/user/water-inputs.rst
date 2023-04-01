Water Inputs
============

Inputs provides a means for developers to control the various simulations powering `Crest`.
The following video covers the basics:

.. _adding-inputs-video:

.. youtube:: sQIakAjSq4Y

   Basics of Adding Ocean Inputs


.. _input-modes-section:

Input Modes
-----------

A number of components provide multiple authoring modes depending on other attached components.


.. _wave-splines-section:

Spline Mode
^^^^^^^^^^^

If a Spline component is present, then this mode is activated.
It takes priority over all other modes.

This mode requires a *Spline* component to be present with at least two spline points added.
Help boxes in the Inspector serve to guide/automate this setup.

Once a *Spline* is created, this is used to drive the input.
A common use of splines is to set the water level to follow a riverbed using the *RegisterHeightInput* component.
A spline may also be used to add waves or flow velocity, if this gives the required level of fidelity.
Another typical use case of splines is to add waves aligned to shorelines.

Relevant data components will automatically be added to spline points.
For example if the spline is used with a *RegisterFlowInput* component, the *Spline Point Data Flow* component will be added to spline points which can then be used to configure the flow speed.

.. _renderer-mode:

Renderer Mode
^^^^^^^^^^^^^

This is the most advanced type of input and allows rendering any geometry/shader into the water system data.
One could draw foam directly into the foam data, or inject a flow map baked from an offline sim.

This mode will activate if a *Renderer* is present.
It will take priority except when a *Spline* is present.

The geometry can come from a :link:`MeshRenderer <{UnityDocScriptLink}/MeshRenderer.html>`, or it can come from any :link:`Renderer <{UnityDocScriptLink}/Renderer.html>` component such as a :link:`TrailRenderer <{UnityDocScriptLink}/TrailRenderer.html>`, :link:`LineRenderer <{UnityDocScriptLink}/LineRenderer.html>` or :link:`ParticleSystem <{UnityDocScriptLink}/ParticleSystem.html>`.
This geometry will be rendered from a orthographic top down perspective to "print" the data onto the water.
For simple cases it is recommended to use an upwards facing quad for the best performance.

The *Particle Renderer* example in the *Examples* scene shows a particle system being projected onto the water surface.

.. tip::

   Inputs only execute the first shader pass (pass zero).
   It is recommended to use unlit shader templates or unlit *Shader Graph* (`URP` only) if not using one of ours.

The following shaders can be used with any ocean input:

-  **Scale By Factor** scales the ocean data between zero and one inclusive.
   It is multiplicative, which can be inverted, so zero becomes no data and one leaves the data unchanged.
