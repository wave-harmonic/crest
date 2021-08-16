To turn on this feature, enable the *Create Shadow Data* option on the *OceanRenderer* script, and ensure the *Shadowing* option is ticked on the ocean material.

To create this setup from scratch, the steps are the following.

#. In the :link:`shadow settings of the {URP} asset <{URPDocLink}/universalrp-asset.html#shadows>`, ensure that shadow cascades are enabled.
   `Crest` requires cascades to be enabled to obtain shadow information.

#. Enable shadowing in Crest.
   Enable *Create Shadow Data* on the OceanRenderer script.

#. On the same script, assign a *Primary Light* for the shadows.
   This light needs to have shadows enabled, if not an error will be reported accordingly.

#. If desired the shadow sim can be configured by assigning a *Shadow Sim Settings* asset (*Create/Crest/Shadow Sim Settings*).

#. Enable *Shadowing* on the ocean material to compile in the necessary shader code
