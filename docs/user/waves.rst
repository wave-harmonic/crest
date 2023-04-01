.. _wave-conditions-section:

Waves
=====

The *Animated Waves* simulation contains the animated surface shape.
This typically contains the waves from shape components, but can also contain waves from the ripple simulation.
All waves will eventually be combined into this simulation so the water shader only needs to sample once to animate vertices.


Environmental Waves
-------------------

The *ShapeFFT* component is used to generate waves in Crest.
To add waves, add the *ShapeFFT* component to a GameObject.

For advanced situations where a high level of control is required over the wave shape, the *ShapeGerstner* component can be used to add specific wave components.
It can be especially useful for Trochoidal waves and shoreline waves.
See the :ref:`shoreline-waves-section` section for more information on the latter.

The *Shape Gerstner Batched* component is deprecated.

.. tip::

   It is useful to see the animated ocean surface while tweaking the wave conditions.

   .. include:: includes/_animated-materials.rst

.. _wave-authoring-section:

Wave Conditions
^^^^^^^^^^^^^^^

The appearance and shape of the waves is determined by a *Wave Spectrum*.
A default wave spectrum will be created if none is specified.
To author wave conditions, click the *Create Asset* button next to the *Spectrum* field.
The resulting spectrum can then be edited by expanding this field.

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


The waves will be dampened/attenuated in shallow water if a *Sea Floor Depth* LOD data is used (see :ref:`sea-floor-depth-section`).
The amount that waves are attenuated is configurable using the *Attenuation In Shallows* setting.

Together these controls give the flexibility to express the great variation one can observe in real world seascapes.

.. _wave-placement-section:

Wave Placement
^^^^^^^^^^^^^^

Waves can be applied everywhere in the world, placed along or orthogonal to a spline, or injected via a custom shader.
See the :ref:`input-modes-section` section for information about these authoring modes.

Shape components' :ref:`renderer-mode` has custom shaders under *Crest/Inputs/Shape Waves*:

-  **Sample Spectrum** samples from the spectrum using a texture.
   The RG channels are the wave direction and together they make the magnitude.
   The values are 0-1 where 0.5 is zero magnitude (ie no waves).


.. _wave-manipulation-section:

Wave Manipulation
^^^^^^^^^^^^^^^^^

The Animated Waves simulation can also be manipulated directly using a *Register Animated Waves Input*.

For the *Register Animated Waves Input*'s :ref:`renderer-mode`, the following shaders are provided with *Crest* under the shader category *Crest/Inputs/Animated Waves*:

-  **Scale By Factor** scales the waves by a factor where zero is no waves and one leaves waves unchanged.
   Useful for reducing waves.

-  **Add From Texture** allows any kind of shape added to the surface from a texture.
   Can ether be a heightmap texture (1 channel) or a 3 channel XYZ displacement texture.

-  **Push Water Under Convex Hull** pushes the water underneath the geometry.
   Can be used to define a volume of space which should stay 'dry'.

-  **Wave Particle** is a 'bump' of water.
   Many bumps can be combined to make interesting effects such as wakes for boats or choppy water.
   Based loosely on http://www.cemyuksel.com/research/waveparticles/.

-  **Set Base Water Height Using Geometry** allows the sea level (average water height) to be offset some amount.
   The top surface of the geometry will provide the water height, and the waves will apply on top.

   .. admonition:: Deprecated

      This shader is deprecated in favour using a *Register Height Input*.

-  **Set Water Height Using Geometry** snaps the water surface to the top surface of the geometry.
   Will override any waves.

   .. admonition:: Deprecated

      This shader is deprecated in favour using a *Register Height Input*.


.. _animated_waves_settings:

Advanced Settings
^^^^^^^^^^^^^^^^^

The environmental waves are termed *Animated Waves* in the *Crest* system and can be configured by assigning an Animated Waves Sim Settings asset to the OceanRenderer script in your scene (:menuselection:`Create --> Crest --> Animated Wave Sim Settings`).

All of the settings below refer to the *Animated Waves Sim Settings* asset.

-  **Attenuation In Shallows** - How much waves are dampened in shallow water.
-  **Shallows Max Depth** - Any water deeper than this will receive full wave strength.
   The lower the value, the less effective the depth cache will be at attenuating very large waves.
   Set to the maximum value (1,000) to disable.
-  **Collision Source** - Where to obtain ocean shape on CPU for physics / gameplay.
-  **Max Query Count** - Maximum number of wave queries that can be performed when using ComputeShaderQueries.
-  **Ping Pong Combine Pass** - Whether to use a graphics shader for combining the wave cascades together.
   Disabling this uses a compute shader instead which doesn't need to copy back and forth between targets, but it may not work on some GPUs, in particular pre-DX11.3 hardware, which do not support typed UAV loads.
   The fail behaviour is a flat ocean.
-  **Render Texture Graphics Format** - The render texture format to use for the wave simulation.
   Consider using higher precision (like R32G32B32A32_SFloat) if you see tearing or wierd normals.
   You may encounter this issue if you use any of the *Set Water Height* inputs.


.. _dynamic-waves-section:

Dynamic Waves
-------------

Overview
^^^^^^^^

Environmental/animated waves are 'static' in that they are not influenced by objects interacting with the water.
'Dynamic' waves are generated from a multi-resolution simulation that can take such interactions into account.

To turn on this feature, enable the *Create Dynamic Wave Sim* option on the *OceanRenderer* script, and to configure the sim, create or assign a *Dynamic Wave Sim Settings* asset on the *Sim Settings Dynamic Waves* option.

The dynamic wave simulation is added on top of the animated FFT waves to give the final shape.

The dynamic wave simulation is not suitable for use further than approximately 10km from the origin.
At this kind of distance the stability of the simulation can be compromised.
Use the *FloatingOrigin*  component to avoid travelling far distances from the world origin.

.. _adding-interaction-forces:

Adding Interaction Forces
^^^^^^^^^^^^^^^^^^^^^^^^^

Dynamic ripples from interacting objects can be generated by placing one or more spheres under the object to approximate the object's shape.
To do so, attach one or more *SphereWaterInteraction* components to children with the object and set the *Radius* parameter to roughly match the shape.

The following settings can be used to customise the interaction:

-  **Radius** - The radius of the sphere from which the interaction forces are calculated.

-  **Weight** - Strength of the effect. Can be set negative to invert.

-  **Weight Up Down Mul** - Multiplier for vertical motion, scales ripples generated from a sphere moving up or down.

-  **Inner Sphere Multiplier** - Internally the interaction is modelled by a pair of nested spheres.
   The forces from the two spheres combine to create the final effect.
   This parameter scales the effect of the inner sphere and can be tweaked to adjust the shape of the result.

-  **Inner Sphere Offset** - This parameter controls the size of the inner sphere and can be tweaked to give further control over the result.

-  **Velocity Offset** - Offsets the interaction position in the direction of motion.
   There is some latency between applying a force to the wave sim and the resulting waves appearing.
   Applying this offset can help to ensure the waves do not lag behind the sphere.

-  **Compensate For Wave Motion** - If set to 0, the input will always be applied at a fixed position before any horizontal displacement from waves.
   If waves are large then their displacement may cause the interactive waves to drift away from the object.
   This parameter can be increased to compensate for this displacement and combat this issue.
   However increasing too far can cause a feedback loop which causes strong 'ring' artifacts to appear in the dynamic waves.
   This parameter can be tweaked to balance this two effects.

Non-spherical objects can be approximated with multiple spheres, for an example see the *Spinner* object in the *boat.unity* example scene which is composed of multiple sphere interactions.
The intensity of the interaction can be scaled using the *Weight* setting.
For an example of usages in boats, search for GameObjects with "InteractionSphere" in their name in the *boat.unity* scene.

.. _dynamic_waves_settings:

Simulation Settings
^^^^^^^^^^^^^^^^^^^

All of the settings below refer to the *Dynamic Wave Sim Settings* asset.

The key settings that impact stability of the simulation are the **Damping** and **Courant Number** settings described below.

-  **Simulation Frequency** - Frequency to run the dynamic wave sim, in updates per second.
   Lower frequencies can be more efficient but may limit wave speed or lead to visible jitter.
   Default is 60 updates per second.

-  **Damping** - How much energy is dissipated each frame.
   Helps sim stability, but limits how far ripples will propagate.
   Set this as large as possible/acceptable.
   Default is 0.05.

-  **Courant Number** - Stability control.
   Lower values means more stable sim, but may slow down some dynamic waves.
   This value should be set as large as possible until sim instabilities/flickering begin to appear.
   Default is 0.7.

-  **Horiz Displace** - Induce horizontal displacements to sharpen simulated waves.

-  **Displace Clamp** - Clamp displacement to help prevent self-intersection in steep waves.
   Zero means unclamped.

-  **Gravity Multiplier** - Multiplier for gravity.
   More gravity means dynamic waves will travel faster.

-  **Attenuation in Shallows** - How much waves are dampened in shallow water.

The *OceanDebugGUI* script gives the debug overlay in the example content scenes and reports the number of sim steps taken each frame.


User Inputs
^^^^^^^^^^^

Dynamic Waves only supports the :ref:`renderer-mode`.

The recommended approach to injecting forces into the dynamic wave simulation is to use the *SphereWaterInteraction* component as described above.
This component will compute a robust interaction force between a sphere and the water, and multiple spheres can be composed to model non-spherical shapes.

However for when more control is required custom forces can be injected directly into the simulation using the *Renderer* input mode.
The following input shader is provided under *Crest/Inputs/Dynamic Waves*:

-  **Add Bump** adds a round force to pull the surface up (or push it down).
   This can be moved around to create interesting effects.
