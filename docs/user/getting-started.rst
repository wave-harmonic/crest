Getting Started
===============

This section has steps for importing the *Crest* content into a project, and for adding a new ocean surface to a scene.

.. warning:: Frequently when changing Unity versions the project can appear to break (no ocean rendering, materials
    appear pink, other issues). Usually restarting the Editor fixes it. In one case the scripts became unassigned in the
    example content scene, but closing Unity, removing the Library folder, and restarting resolved it.

.. Getting Started Video
.. ---------------------

To augment / complement this written documentation we published a video available here:

.. only:: html

    .. tabs::

        .. group-tab:: |brp_long|

            .. raw:: html

                <div class="video-container">
                    <iframe width="100%" height="100%" src="https://www.youtube-nocookie.com/embed/qsgeG4sSLFw" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen>
                        Fallback
                    </iframe>
                </div>

        .. group-tab:: |hdrp_long|

            .. raw:: html

                <div class="video-container">
                    <iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/FE6l39Lt3js" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
                </div>

        .. group-tab:: |urp_long|

            .. raw:: html

                <div class="video-container">
                    <iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/TpJf13d_-3E" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
                </div>


.. only:: latex

    .. only:: readthedocs or brp

        .. only:: readthedocs

            |brp_long|

        https://www.youtube.com/watch?v=qsgeG4sSLFw

    .. only:: readthedocs or urp

        .. only:: readthedocs

            |urp_long|

        https://www.youtube.com/watch?v=TpJf13d_-3E

    .. only:: readthedocs or hdrp

        .. only:: readthedocs

            |hdrp_long|

        https://www.youtube.com/watch?v=FE6l39Lt3js


Importing *Crest* files into project
------------------------------------

Pipeline Setup
^^^^^^^^^^^^^^

.. tabs::

    .. group-tab:: |brp_long|

        Ensure |brp_long| Render Pipeline (BRP) is setup and functioning, either by setting up a new project using the
        URP template or by installing the URP package into an existing project and configuring the Render Pipeline
        Asset. Please see the Unity documentation for more information.

        .. note::

            Switch to Linear space rendering under Edit/Project Settings/Player/Other Settings. If your platform(s) require
            Gamma space, the material settings will need to be adjusted to compensate.

    .. group-tab:: |hdrp_long|

        Ensure |hdrp_long| Render Pipeline (URP) is setup and functioning, either by setting up a new project using the
        URP template or by installing the URP package into an existing project and configuring the Render Pipeline
        Asset. Please see the Unity documentation for more information.

    .. group-tab:: |urp_long|

        Ensure the |urp_long| Render Pipeline (URP) is setup and functioning, either by setting up a new project using the
        URP template or by installing the URP package into an existing project and configuring the Render Pipeline
        Asset. Please see the Unity documentation for more information.

        .. note::

            Switch to Linear space rendering under Edit/Project Settings/Player/Other Settings. If your platform(s) require
            Gamma space, the material settings will need to be adjusted to compensate.

Importing Crest
^^^^^^^^^^^^^^^

Import the *Crest* package into project using the *Asset Store* window in the Unity Editor.

.. note::
    The files under Crest-Examples are not required by our core functionality, but are provided for illustrative
    purposes. We recommend first time users import them as they may provide useful guidance.

.. only:: html

    .. tabs::

        .. group-tab:: |brp_long|

            TODO

        .. group-tab:: |hdrp_long|

            TODO

        .. group-tab:: |urp_long|

            .. include:: includes/_importing-crest-urp.rst

.. only:: latex

    .. only:: urp

        .. include:: includes/_importing-crest-urp.rst

Adding *Crest* to a Scene
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
