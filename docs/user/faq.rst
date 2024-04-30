Frequently Asked Questions
==========================

.. Set section numbering and ToC depth for PDFs because Sphinx has bugs and limitations.

.. raw:: latex

   \setcounter{secnumdepth}{0}
   \addtocontents{toc}{\protect\setcounter{tocdepth}{0}}

.. dropdown:: Why does the ocean not update smoothly in edit mode?

   .. include:: /user/includes/_animated-materials.rst

.. dropdown:: I am seeing "Crest does not support OpenGL/WebGL backends." in the editor

   It is likely Unity has defaulted to using OpenGL on your platform.
   You will need to switch to a supported graphics API like Vulkan.
   You will need to make Vulkan the default by :link:`overriding the graphics APIs <{UnityDocLink}/GraphicsAPIs.html>`.

.. dropdown:: Why aren't my prefab mode edits not reflected in the scene view?

   Crest does not support running in prefab mode which means dirty state in prefab mode will not be reflected in the scene view.
   Save the prefab to see the changes.

.. dropdown:: Is *Crest* well suited for medium-to-low powered mobile devices?

   *Crest* is built to be performant by design and has numerous quality/performance levers.
   However it is also built to be very flexible and powerful and as such can not compete with a minimal, mobile-centric
   water solution such as the one in the *Boat Attack* project.
   Therefore we target *Crest* at PC/console platforms.

   That being said, developers have had success with *Crest* on lower powered platforms like the *Nintendo Switch*.
   *Apple* devices are good targets as their processors are quite capable all round.
   *Meta Quest 2* is one device where it will be a struggle to take advantage of *Crest*.

.. dropdown:: Which platforms does `Crest` support?

   Testing occurs primarily on MacOS and Windows.
   iOS and Android are also periodically tested.

   Firstly, make sure your target platform adheres to the :ref:`requirements`.

   We have users targeting the following platforms:

   -  Windows
   -  MacOS
   -  Linux \*
   -  PlayStation \*
   -  Xbox \*
   -  Switch \* \*\*
   -  iOS \*\*
   -  Android/Quest \*\*

   | \* We do not have access to these platforms ourselves.
   | \*\* Performance is a challenge on these platforms. Please see the previous question.

   `Crest` also supports VR/XR Multi-Pass and Single Pass Instanced rendering.

   For additional platform notes, see :link:`Platform Support <{WikiLink}/Platform-Support>`.

.. dropdown:: Is `Crest` well suited for localised bodies of water such as lakes?

   Yes, see :ref:`water-bodies` for documentation.

.. dropdown:: Can *Crest* work with multiplayer?

   Yes, the animated waves are deterministic and can be synchronised across the network.
   For more information see :ref:`network-synchronisation`.

   Note however that the dynamic wave sim is not synchronized over the network and should not be relied upon in networked situations.

.. dropdown:: Errors are present in the log that report *Kernel 'xxx.yyy' not found*

   Unity sometimes gets confused and needs assets reimported.
   This can be done by clicking the *Crest* root folder in the Project window and clicking *Reimport*.
   Alternatively the *Library* folder can be removed from the project root which will force all assets to reimport.

.. dropdown:: Can I push the ocean below the terrain?

   Yes, this is demonstrated in :numref:`adding-inputs-video`.

.. dropdown:: Does *Crest* support multiple viewpoints?

   Currently only a single ocean instance can be created, and only one viewpoint is supported at a time.
   We hope to support multiple simultaneous views in the future.

.. dropdown:: Can I sample the water height at a position from C#?

   Yes, see usages of *SampleHeightHelper* class in *SamplingHelpers.cs*.
   The *OceanRenderer* uses this helper to get the height of the viewer above the water, and makes this viewer height available via the *ViewerHeightAboveWater* property.

.. dropdown:: Can I trigger something when an object is above or under the ocean surface without any scripting knowledge?

   Yes. Please see :ref:`detecting_above_or_below_water`.

.. dropdown:: Does Crest support orthographic projection?

   Yes. Please see :ref:`orthographic_projection`.

.. dropdown:: How do I disable underwater fog rendering in the scene view?

   You can enable/disable rendering in the scene view by toggling fog in the :link:`scene view control bar <{UnityDocLink}/ViewModes.html>`.

.. dropdown:: Can the density of the fog in the water be reduced?

   The density of the fog underwater can be controlled using the *Fog Density* parameter on the ocean material.
   This applies to both above water and underwater.
   The *Depth Fog Density Factor* on the *Underwater Renderer* can reduce the density of the fog for the underwater effect.

.. dropdown:: Does Crest support third party weather assets?

   Several weather assets provide integrations with Crest.
   Some will list support in their description, but often they only list support in their documentation.

   These may require some code to be inserted into the ocean shader.
   There is a comment referring to this, search Ocean.shader for 'Azure'.

   Please see the Community Contributions section in our :link:`Wiki <{WikiLink}>` for some integration solutions.

.. dropdown:: Can I remove water from inside my boat?

   Yes, this is referred to as 'clipping' and is covered in section :ref:`clip-surface-section`.

.. dropdown:: How to implement a swimming character?

   As far as we know, existing character controller assets which support swimming do not support waves (they require a volume for the water or physics mesh for the water surface).
   We have an efficient API to provide water heights, which the character controller could use instead of a physics volume.
   Please request support for custom water height providers to your favourite character controller asset dev.

.. dropdown:: Can I render transparent objects underwater?

   See :ref:`transparent-object-underwater`.

.. dropdown:: Can I render transparent objects in front of water?

   See :ref:`transparent-object-before-ocean-surface`.

.. dropdown:: Can I render transparent objects behind the ocean surface?

   See :ref:`transparent-object-after-ocean-surface`.

.. dropdown:: How can I fix the water rendering over the top of `HDRP`'s volumetric clouds when viewing from above?

   When viewing volumetric clouds from above, the water may render over them.

   For Unity 6 and above, set the *Refraction Model* on the water material to something other than *None* (like *Planar*).
   This has an overhead so it is recommended to set it to *None* if not required.

   For earlier Unity versions, the only way is to set *Surface Type* to *Opaque*.
   This can be done.
   The water will look difference in shallow areas so it is advise to set this only when needed.
