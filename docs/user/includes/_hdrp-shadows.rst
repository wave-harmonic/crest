To turn on this feature, enable the *Create Shadow Data* option on the *OceanRenderer* script.

Specular (direct) lighting on the ocean surface is not shadowed by this data.
It is shadowed by the pipeline.
But we still use the data to shadow anything not covered by the pipeline like caustic shadows.

To create this setup from scratch, the steps are the following.

#. On the `HDRP` asset (either the asset provided with Crest *Assets/Crest/CrestExampleHDRPAsset*, or the one used in your project), ensure that *Custom Pass* is enabled.

#. Shadow maps must be enabled in the frame settings for the camera.

#. Enable shadowing in Crest. Enable *Create Shadow Data* on the OceanRenderer script.

#. On the same script, assign a *Primary Light* for the shadows.
   This light needs to have shadows enabled, if not an error will be reported accordingly.

#. If desired the shadow sim can be configured by assigning a *Shadow Sim Settings* asset (*Create/Crest/Shadow Sim Settings*).
