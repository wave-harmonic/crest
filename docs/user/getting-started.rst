Getting Started
===============

.. Set section numbering and ToC depth for PDFs because Sphinx has bugs and limitations.

.. raw:: latex

   \setcounter{secnumdepth}{2}
   \addtocontents{toc}{\protect\setcounter{tocdepth}{2}}


This section has steps for importing the `Crest` content into a project, and for adding a new ocean surface to a scene.

.. warning::

   When changing Unity versions, setting up a render pipeline or making changes to packages, the project can appear to break.
   This may manifest as spurious errors in the log, no ocean rendering, magenta materials, scripts unassigned in example scenes, etcetera.
   Often, restarting the Editor fixes it.
   Clearing out the *Library* folder can also help to reset the project and clear temporary errors.
   These issues are not specific to `Crest`, but we note them anyway as we find our users regularly encounter them.

To augment / complement this written documentation we published a video available here:

.. only:: birp

   .. tab:: `BIRP`

      .. youtube:: qsgeG4sSLFw

         Getting Start with Crest for `BIRP`

.. only:: hdrp

   .. tab:: `HDRP`

      .. youtube:: FE6l39Lt3js

         Getting Start with Crest for `HDRP`

.. only:: urp

   .. tab:: `URP`

      .. youtube:: TpJf13d_-3E

         Getting Start with Crest for `URP`


Requirements
------------

-  Unity Version: `UnityMinVersion`
-  Shader compilation target 4.5 or above
-  Crest does not support OpenGL or WebGL backends

.. only:: birp

   .. tab:: `BIRP`

      -  The `Crest` example content uses the post-processing package (for aesthetic reasons).
         If this is not present in your project, you will see an unassigned script warning which you can fix by removing the offending script.

.. only:: hdrp

   .. tab:: `HDRP`

      -  The minimum `HDRP` package version is `HDRPMinVersion`

.. only:: urp

   .. tab:: `URP`

      -  The minimum `URP` package version is `URPMinVersion`


Importing `Crest` files into project
------------------------------------

The steps to set up `Crest` in a new or existing project are as follows:


Pipeline Setup
^^^^^^^^^^^^^^

.. only:: birp

   .. tab:: `BIRP`

      .. include:: /includes/_birp-vars.rst
      .. include:: includes/_pipeline-setup.rst
      .. include:: includes/_color-space.rst

.. only:: hdrp

   .. tab:: `HDRP`

      .. include:: /includes/_hdrp-vars.rst
      .. include:: includes/_pipeline-setup.rst

.. only:: urp

   .. tab:: `URP`

      .. include:: /includes/_urp-vars.rst
      .. include:: includes/_pipeline-setup.rst
      .. include:: includes/_color-space.rst


.. _importing-crest-section:

Importing Crest
^^^^^^^^^^^^^^^

Import the `Crest` package into project using the *Asset Store* window in the Unity Editor.

.. note::
   The files under Crest-Examples are not required by our core functionality, but are provided for illustrative
   purposes. We recommend first time users import them as they may provide useful guidance.

.. only:: birp

   .. tab:: `BIRP`

      .. include:: includes/_importing-crest-birp.rst

.. only:: hdrp

   .. tab:: `HDRP`

      .. include:: includes/_importing-crest-hdrp.rst

.. only:: urp

   .. tab:: `URP`

      .. include:: includes/_importing-crest-urp.rst

.. tip::

   If you are starting from scratch we recommend :link:`creating a project using a template in the Unity Hub <{UnityDocLink}/ProjectTemplates.html>`.


Adding `Crest` to a Scene
-------------------------

.. Adding the Ocean
.. ^^^^^^^^^^^^^^^^

The steps to add an ocean to an existing scene are as follows:

*  Create a new *GameObject* for the ocean, give it a descriptive name such as *Ocean*.

   *  Assign the *OceanRenderer* component to it.
      This component will generate the ocean geometry and do all required initialisation.
   *  Assign the desired ocean material to the *OceanRenderer* script - this is a material using the *Crest/Ocean* shader.
   *  Set the Y coordinate of the position to the desired sea level.

*  Tag a primary camera as *MainCamera* if one is not tagged already, or provide the *Camera* to the *View Camera* property on the *OceanRenderer* script.
   If you need to switch between multiple cameras, update the *ViewCamera* field to ensure the ocean follows the correct view.
*  Be sure to generate lighting if necessary. The ocean lighting takes the ambient intensity from the baked spherical harmonics.
   It can be found at the following:

   :menuselection:`Window --> Rendering --> Lighting Settings --> Debug Settings --> Generate Lighting`

   .. tip:: You can check *Auto Generate* to ensure lighting is always generated.

*  To add waves, create a new GameObject and add the *Shape FFT* component.
   See :ref:`wave-conditions-section` section for customisation.
*  Any ocean seabed geometry needs set up to register it with `Crest`. See section :ref:`shallows`.
*  If the camera needs to go underwater, the underwater effect must be configured.
   See section :ref:`underwater` for instructions.

.. TODO: Is separate headings better for quickstart?

.. Adding Waves
.. ^^^^^^^^^^^^

.. To add waves:

.. * Create a new GameObject and add the *Shape FFT* component.
.. * On startup this script creates a default ocean shape. To edit the shape, right click in the Project view and select *Create/Crest/Ocean Wave Spectrum* and provide it to this script.
.. * Smooth blending of ocean shapes can be achieved by adding multiple *Shape FFT* scripts and crossfading them using the *Weight* parameter.

.. See :ref:`_wave-authoring-section` for in depth documentation.


.. Adding Ocean Depth
.. ^^^^^^^^^^^^^^^^^^

.. For geometry that should influence the ocean (attenuate waves, generate foam):

.. * Static geometry should render ocean depth just once on startup into an *Ocean Depth Cache* - the island in the main scene in the example content demonstrates this.
.. * Dynamic objects that need to render depth every frame should have a *Register Sea Floor Depth Input* component attached.

.. See :ref:`shallows` for in depth documentation.


.. Underwater
.. ^^^^^^^^^^

.. * If the camera needs to go underwater, the underwater effect must be configured.

.. See section :ref:`underwater` for instructions.


Frequent Setup Issues
---------------------

The following are kinks or bugs with the install process which come up frequently.

Errors present, or visual issues
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Try restarting Unity as a first step.


.. only:: hdrp or urp

   Compile errors in the log, not possible to enter play mode, visual issues in the scene
   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

   Verify that render pipeline is installed and enabled in the settings. See the follow for documentation:

   .. only:: hdrp

      :link:`Upgrading to {HDRP} <{HDRPDocLink}/Upgrading-To-HDRP.html>`

   .. only:: urp

      :link:`Installing {URP} into a project <{URPDocLink}/InstallURPIntoAProject.html>`


Possible to enter play mode, but errors appear in the log at runtime that mention missing 'kernels'
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Recent versions of Unity have a bug that makes shader import unreliable.
Please try reimporting the *Crest/Shaders* folder using the right click menu in the project view.
Or simply close Unity, delete the Library folder and restart which will trigger everything to reimport.


Ocean framerate low in edit mode
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. include:: includes/_animated-materials.rst

.. only:: hdrp

   Ocean surface appears blurred under motion `[HDRP]`
   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

   .. include:: includes/_hdrp-taa.rst

   Ocean reflections/lighting/fog looks wrong `[HDRP]`
   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

   If reflections appear wrong, it can be useful to make a simple test shadergraph with our water normal map applied to it, to compare results.
   We provide a simple test shadergraph for debugging purposes - enable the *Apply test material* debug option on the *OceanRenderer* component to apply it.
   If you find you are getting good results with a test shadergraph but not with our ocean shader, please report this to us.
