Q & A
=====

.. Set section numbering and ToC depth for PDFs because Sphinx has bugs and limitations.

.. raw:: latex

   \setcounter{secnumdepth}{0}
   \addtocontents{toc}{\protect\setcounter{tocdepth}{0}}


.. NOTE:
.. It would be nice not to have section numbering for these. It could be done using styling, but not worth the disconnect.
.. Heading size has been reduced in styling.
.. The "topic" directive is a nice alternative, but they are not as readable in PDFs.


Why does the ocean not update smoothly in edit mode?
----------------------------------------------------
.. include:: /user/includes/_animated-materials.rst

Is *Crest* well suited for medium-to-low powered mobile devices?
----------------------------------------------------------------
*Crest* is built to be performant by design and has numerous quality/performance levers.
However it is also built to be very flexible and powerful and as such can not compete with a minimal, mobile-centric
ocean renderer such as the one in the *BoatAttack* project.
Therefore we target *Crest* at PC/console platforms.

Which platforms does `Crest` support?
-------------------------------------
Testing occurs primarily on Windows.

We have users targeting the following platforms:

-  Windows
-  Mac
-  Linux
-  PS4
-  XboxOne
-  Switch\*
-  iOS/Android\* `[[BIRP]]` `[[URP]]`
-  Quest\* `[[BIRP]]` `[[URP]]`

\* Performance is a challenge on these platforms. Please see the previous question.

`Crest` also supports VR/XR Multi-Pass, Single Pass and Single Pass Instanced rendering.

For additional platform notes, see :link:`Platform Support <{WikiLink}/Platform-Support>`.

Is `Crest` well suited for localised bodies of water such as lakes?
-------------------------------------------------------------------
Currently Crest is targeted towards large bodies of water.
This area is being actively developed.
Please see :ref:`water-bodies` for current progress.

Can *Crest* work with multiplayer?
----------------------------------
Yes, the animated waves are deterministic and easily synchronized.
See discussion in :issue:`75`.
However, the dynamic wave sim is not synchronized over the network and can not currently be relied upon in networked situations.
Additionally, *Crest* does not currently support being run as a CPU-only headless instance.
We hope to improve this in the future.

Errors are present in the log that report *Kernel 'xxx.yyy' not found*
----------------------------------------------------------------------
Unity sometimes gets confused and needs assets reimported.
This can be done by clicking the *Crest* root folder in the Project window and clicking *Reimport*.
Alternatively the *Library* folder can be removed from the project root which will force all assets to reimport.

Can I push the ocean below the terrain?
---------------------------------------
Yes, this is demonstrated in :numref:`adding-inputs-video`.

Does *Crest* support multiple viewpoints?
-----------------------------------------
Currently only a single ocean instance can be created, and only one viewpoint is supported at a time.
We hope to support multiple simultaneous views in the future.

Can I sample the water height at a position from C#?
----------------------------------------------------
Yes, see SampleHeightHelper class in SamplingHelpers.cs.
The OceanRenderer uses this helper to get the height of the viewer above the water, and makes this viewer height available via the ViewerHeightAboveWater property.

Can I trigger something when an object is above or under the ocean surface without any scripting knowledge?
-----------------------------------------------------------------------------------------------------------
The OceanSampleHeightEvents can be used for this purpose.
It will invoke a UnityEvent when the attached game object is above or below the ocean surface once per state change.

Does Crest support orthographic projection?
-------------------------------------------
Yes. Please see :ref:`orthographic_projection`.

Can the density of the fog in the water be reduced?
---------------------------------------------------
The density of the fog underwater can be controlled using the *Fog Density* parameter on the ocean material.
This applies to both above water and underwater.

.. only:: birp or urp

   Does Crest support third party sky assets? `[BIRP] [URP]`
   -----------------------------------------------------------
   We have heard of Crest users using TrueSky, AzureSky.
   These may require some code to be inserted into the ocean shader - there is a comment referring to this, search Ocean.shader for 'Azure'.

   Please see the Community Contributions section in our :link:`Wiki <{WikiLink}>` for some integration solutions.

Can I remove water from inside my boat?
---------------------------------------
Yes, this is referred to as 'clipping' and is covered in section :ref:`clip-surface-section`.

How to implement a swimming character?
--------------------------------------
As far as we know, existing character controller assets which support swimming do not support waves (they require a volume for the water or physics mesh for the water surface).
We have an efficient API to provide water heights, which the character controller could use instead of a physics volume.
Please request support for custom water height providers to your favourite character controller asset dev.

Can I render transparent shaders/materials underwater?
------------------------------------------------------
This is tricky because the underwater effect uses the opaque scene depths in order to render the water fog, which will not include transparents.

.. only:: birp

   .. tab:: `BIRP`

      .. include:: includes/_underwater-curtain-transparents.rst

.. only:: hdrp

   .. tab:: `HDRP`

      The Submarine example scene demonstrates an underwater transparent effect - the bubbles from the propellors when the submarine is in motion.
      This effect is from the *Bubbles Propellor* GameObject, which is assigned a specific layer *TransparentFX*.
      To drive the rendering, the *CustomPassForUnderwaterParticles* GameObject has a *Custom Pass Volume* component attached which is configured to render the *TransparentFX* layer in the *After Post Process* injection point, i.e. after the underwater postprocess has rendered.
      Transparents rendered after the underwater postprocess will not have the underwater water fog shading applied to them.
      The effect of the fog either needs to be faked by simply ramping the opacity down to 0 based on distance from the camera, or the water fog shader code needs included and called from teh transparent shader.
      The shader *UnderwaterPostProcessHDRP.shader* is a good reference for calculating the underwater effect.
      This will require various parameters on the shader like fog density and others.

.. only:: urp

   .. tab:: `URP`

      .. include:: includes/_underwater-curtain-transparents.rst