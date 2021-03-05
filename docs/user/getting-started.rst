Getting Started
===============

This section has steps for importing the `Crest` content into a project, and for adding a new ocean surface to a scene.

.. warning::

    When changing Unity versions, setting up a render pipeline or making changes to packages, the project can appear to break.
    This may manifest as spurious errors in the log, no ocean rendering, magenta materials, scripts unassigned in example scenes, etcetera.
    Often, restarting the Editor fixes it.
    Clearing out the \textit{/Library} folder can also help to reset the project and clear temporary errors.
    These issues are not specific to `Crest`, but we note them anyway as we find our users regularly encounter them.

.. Getting Started Video
.. ---------------------

To augment / complement this written documentation we published a video available here:

.. only:: html or birp

    .. tab:: `BIRP`

        .. youtube:: qsgeG4sSLFw

.. only:: html or hdrp

    .. tab:: `HDRP`

        .. youtube:: FE6l39Lt3js

.. only:: html or urp

    .. tab:: `URP`

        .. youtube:: TpJf13d_-3E


Requirements
------------

- Unity Version: `UnityMinVersion`
- .NET 4.x runtime
- Shader compilation target 4.5 or above
- Crest unfortunately does not support OpenGL or WebGL backends

.. only:: html or birp

    .. tab:: `BIRP`

        - The `Crest` example content uses the post-processing package (for aesthetic reasons).
          If this is not present in your project, you will see an unassigned script warning which you can fix by removing the offending script.

.. only:: html or hdrp

    .. tab:: `HDRP`

        - The minimum `HDRP` package version is `HDRPMinVersion`

.. only:: html or urp

    .. tab:: `URP`

        - The minimum `URP` package version is `URPMinVersion`


Importing `Crest` files into project
------------------------------------

The steps to set up `Crest` in a new or existing project are as follows:


Pipeline Setup
^^^^^^^^^^^^^^

.. only:: html or birp

    .. tab:: `BIRP`

        .. include:: includes/_birp-vars.rst
        .. include:: includes/_pipeline-setup.rst

.. only:: html or hdrp

    .. tab:: `HDRP`

        .. include:: includes/_hdrp-vars.rst
        .. include:: includes/_pipeline-setup.rst

        `HDRP` defaults to using `TAA`, which does not work well with the water material and makes it look blurry under motion.
        We recommend switching to a different anti-aliasing method such as `SMAA` using the *Anti-aliasing* option on the camera component.

.. only:: html or urp

    .. tab:: `URP`

        .. include:: includes/_urp-vars.rst
        .. include:: includes/_pipeline-setup.rst

Switch to Linear space rendering under :menuselection:`Edit --> Project Settings --> Player --> Other Settings`.
If your platform(s) require Gamma space (and providing your pipeline supports it), the material settings will need to be adjusted to compensate.
Please see the :link:`Unity documentation <{UnityDocLinkBase}/LinearRendering-LinearOrGammaWorkflow.html>` for more information.


Importing Crest
^^^^^^^^^^^^^^^

Import the `Crest` package into project using the *Asset Store* window in the Unity Editor.

.. note::
    The files under Crest-Examples are not required by our core functionality, but are provided for illustrative
    purposes. We recommend first time users import them as they may provide useful guidance.

.. only:: html or birp

    .. tab:: `BIRP`

        TODO

.. only:: html or hdrp

    .. tab:: `HDRP`

        TODO

.. only:: html or urp

    .. tab:: `URP`

        .. include:: includes/_importing-crest-urp.rst

.. TODO
.. If you imported the example content, open an example scene such as *Crest/Crest-Examples/Main/Scenes/main.unity* and press Play and the ocean will get generated.
.. Otherwise proceed to the next section to add the ocean to an existing scene.

Adding `Crest` to a Scene
-------------------------

Adding the Ocean
^^^^^^^^^^^^^^^^

.. TODO: Update camera instructions to reflect ViewCamera

The steps to add an ocean to an existing scene are as follows:

* Create a new *GameObject* for the ocean, give it a descriptive name such as *Ocean*.

  * Assign the *OceanRenderer* component to it. On startup this component will generate the ocean geometry and do all required initialisation.
  * Assign the desired ocean material to the *OceanRenderer* script - this is a material using the *Crest/Ocean* shader.
  * Set the Y coordinate of the position to the desired sea level.

* Tag a primary camera as *MainCamera* if one is not tagged already, or provide the *Viewpoint* transform to the *OceanRenderer* script. If you need to switch between multiple cameras, update the *Viewpoint* field to ensure the ocean follows the correct view.

* Be sure to generate lighting if necessary. The ocean lighting takes the ambient intensity from the baked spherical
  harmonics. It can be found at the following:

  :menuselection:`Window --> Rendering --> Lighting Settings --> Debug Settings --> Generate Lighting`

  .. tip:: You can check *Auto Generate* to ensure lighting is always generated.


Adding Waves
^^^^^^^^^^^^

To add waves:

* Create a new GameObject and add the *Shape Gerstner Batched* component.
* On startup this script creates a default ocean shape. To edit the shape, right click in the Project view and select *Create/Crest/Ocean Wave Spectrum* and provide it to this script.
* Smooth blending of ocean shapes can be achieved by adding multiple *Shape Gerstner Batched* scripts and crossfading them using the *Weight* parameter.


Adding Ocean Depth
^^^^^^^^^^^^^^^^^^

For geometry that should influence the ocean (attenuate waves, generate foam):

* Static geometry should render ocean depth just once on startup into an *Ocean Depth Cache* - the island in the main scene in the example content demonstrates this.
* Dynamic objects that need to render depth every frame should have a *Register Sea Floor Depth Input* component attached.
