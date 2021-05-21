To turn on this feature, enable the *Create Shadow Data* option on the *OceanRenderer* script, and ensure the *Shadowing* option is ticked on the ocean material.

We have provided an example configuration with shadows enabled;
*Assets/Crest/CrestExampleURPAsset*, which should be set to use the following Custom Renderer: *Assets/Crest/ForwardRendererCrestShadows*.
In the setup instructions in section :ref:`importing-crest-section`, steps to use this asset and renderer were given, and no further action is required if this setup is used.

To create this setup from scratch, the steps are the following.

#. On the `URP` asset (either the asset provided with Crest *Assets/Crest/CrestExampleURPAsset*, or the one used in your project), ensure that shadow cascades are enabled.
   `Crest` requires cascades to be enabled to obtain shadow information.

#. Create a new renderer which will have the sample shadows feature enabled.
   Right click a folder under Assets and select *Create/Rendering/Universal Render Pipeline/Forward Renderer*.
   Then see :link:`How to add a Renderer Feature to a Renderer <{URPDocLink}/urp-renderer-feature-how-to-add.html>`.

   .. NOTE: If Unity had instructions on creating a Forward Renderer, we could refer to that.

#. Add the new renderer to *General/Renderer List* of the `URP` asset created in the previous step.

#. Enable shadowing in Crest.
   Enable *Create Shadow Data* on the OceanRenderer script.

#. On the same script, assign a *Primary Light* for the shadows.
   This light needs to have shadows enabled, if not an error will be reported accordingly.

#. If desired the shadow sim can be configured by assigning a *Shadow Sim Settings* asset (*Create/Crest/Shadow Sim Settings*).

#. Enable *Shadowing* on the ocean material to compile in the necessary shader code
