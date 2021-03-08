To turn on this feature, enable the *Create Shadow Data* option on the *OceanRenderer* script, and ensure the *Shadowing* option is ticked on the ocean material.



We have provided an example configuration with shadows enabled;
*Assets/Crest/CrestExampleRPAsset*, which should be set to use the following Custom Renderer: *Assets/Crest/ForwardRendererCrestShadows*.
In the setup instructions in section `[initial_setup]`_, steps to use this asset and renderer were given, and no further action is required if this setup is used.

To create this setup from scratch, the steps are the following.

#. On the scriptable render pipeline asset (either the asset provided
   with Crest *Assets/Crest/CrestExampleRPAsset*, or the one used in
   your project), ensure that shadow cascades are enabled. Crest
   requires cascades to be enabled to obtain shadow information.

#. Create a new renderer which will have the sample shadows feature
   enabled. Right click a folder under Assets and select
   *Create/Rendering/Universal Render Pipeline/Forward Renderer*. Select
   the asset and click the '+' icon and select *Crest/SampleShadows*.

#. Enable the new renderer. Select your RP pipeline asset and set
   *General/Renderer Type* to *Custom* and assign the asset created in
   the previous step.

#. Enable shadowing in Crest. Enable *Create Shadow Data* on the
   OceanRenderer script.

#. On the same script, assign a *Primary Light* for the shadows. This
   light needs to have shadows enabled, if not an error will be reported
   accordingly.

#. If desired the shadow sim can be configured by assigning a *Shadow
   Sim Settings* asset (*Create/Crest/Shadow Sim Settings*).

#. Enable *Shadowing* on the ocean material to compile in the necessary
   shader code

.. _[initial_setup]: #initial_setup

The shadow sim can be configured by assigning a Shadow Sim Settings
asset to the OceanRenderer script in your scene (*Create/Crest/Shadow
Sim Settings*). In particular, the soft shadows are very soft by
default, and may not appear for small/thin shadow casters. This can be
configured using the *Jitter Diameter Soft* setting.

There will be times when the shadow jitter settings will cause shadows
or light to leak. An example of this is when trying to create a dark
room during daylight. At the edges of the room the jittering will cause
the ocean on the inside of the room (shadowed) to sample outside of the
room (not shadowed) resulting in light at the edges. Reducing the
*Jitter Diameter Soft* setting can solve this, but we have also provided
a *Register Shadow Input* component which can override the shadow data.
This component bypasses jittering and gives you full control.

Note: RP should allow sampling the shadow maps directly in the ocean shader which would be an alternative to using this shadow data, although it would not give the softer shadow component. This would likely work on 2018.