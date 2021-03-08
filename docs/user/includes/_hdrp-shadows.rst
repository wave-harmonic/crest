To turn on this feature, enable the *Create Shadow Data* option on the *OceanRenderer* script.

Specular (direct) lighting on the ocean surface is not shadowed by this data.
It is shadowed by the pipeline.
But we still use the data to shadow anything not covered by the pipeline like caustic shadows.

To create this setup from scratch, the steps are the following.

#. On the scriptable render pipeline asset (either the asset provided
   with Crest *Assets/Crest/CrestExampleRPAsset*, or the one used in
   your project), ensure that *Custom Pass* is enabled.

#. Shadow maps must be enabled in the frame settings for the camera.

#. Enable shadowing in Crest. Enable *Create Shadow Data* on the
   OceanRenderer script.

#. On the same script, assign a *Primary Light* for the shadows. This
   light needs to have shadows enabled, if not an error will be reported
   accordingly.

#. If desired the shadow sim can be configured by assigning a *Shadow
   Sim Settings* asset (*Create/Crest/Shadow Sim Settings*).

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

Note: RP should allow sampling the shadow maps directly in the ocean
shader which would be an alternative to using this shadow data, although
it would not give the softer shadow component. This would likely work on
2018.