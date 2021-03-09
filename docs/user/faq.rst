Q&A
===

Why does the ocean not update smoothly in edit mode?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
.. include:: /user/includes/_animated-materials.rst

Is *Crest* well suited for medium-to-low powered mobile devices?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
*Crest* is built to be performant by design and has numerous quality/performance levers.
However it is also built to be very flexible and powerful and as such can not compete with a minimal, mobile-centric
ocean renderer such as the one in the *BoatAttack* project.
Therefore we target *Crest* at PC/console platforms.

Which platforms does `Crest` support?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Testing occurs primarily on Windows.

We have users targeting the following platforms:

-  Windows
-  Mac
-  Linux
-  PS4
-  XboxOne
-  Switch
-  iOS/Android `[BIRP]` `[URP]`
-  Quest `[BIRP]` `[URP]`

`Crest` also supports VR/XR.

Performance is a challenge on Switch, mobile platforms and standalone headsets (like Quest). Please see the previous question.

For additional platform notes, see :link:`Platform Support <{WikiLink}/Platform-Support>`.

Is `Crest` well suited for localised bodies of water such as lakes?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Currently Crest is targeted towards large bodies of water.
This area is being actively developed.
Please see :ref:`water-bodies` for current progress.

Can *Crest* work with multiplayer?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Yes, the animated waves are deterministic and easily synchronized.
See discussion in :issue:`75`.
However, the dynamic wave sim is not synchronized over the network and can not currently be relied upon in networked situations.
Additionally, *Crest* does not currently support being run as a CPU-only headless instance.
We hope to improve this in the future.

Errors are present in the log that report *Kernel 'xxx.yyy' not found*
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Unity sometimes gets confused and needs assets reimported.
This can be done by clicking the *Crest* root folder in the Project window and clicking *Reimport*.
Alternatively the *Library* folder can be removed from the project root which will force all assets to reimport.

Can I push the ocean below the terrain?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Yes, this is demonstrated in :numref:`adding-inputs-video`.

Does *Crest* support multiple viewpoints?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Currently only a single ocean instance can be created, and only one viewpoint is supported at a time.
We hope to support multiple simultaneous views in the future.

Can I sample the water height at a position from C#?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Yes, see SampleHeightHelper class in SamplingHelpers.cs.
The OceanRenderer uses this helper to get the height of the viewer above the water, and makes this viewer height available via the ViewerHeightAboveWater property.

Can I trigger something when an object is above or under the ocean surface without any scripting knowledge?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
The OceanSampleHeightEvents can be used for this purpose.
It will invoke a UnityEvent when the attached game object is above or below the ocean surface once per state change.

Does Crest support orthographic projection?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Yes. Please see :ref:`orthographic_projection`.

Can the density of the fog in the water be reduced?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
The density of the fog underwater can be controlled using the *Fog Density* parameter on the ocean material.
This applies to both above water and underwater.

.. only:: birp or urp

    Does Crest support third party sky assets? `[BIRP]` `[URP]`
    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    We have heard of Crest users using TrueSky, AzureSky.
    These may require some code to be inserted into the ocean shader - there is a comment referring to this, search Ocean.shader for 'Azure'.

    Please see the Community Contributions section in our :link:`Wiki <{WikiLink}>` for some integration solutions.