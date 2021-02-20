.. only:: html

    .. raw:: html

        <h4>Transparency</h4>

.. only:: latex

    Transparency
    """"""""""""

To enable the water surface to be transparent, two options must be enabled in the URP configuration.
To find the configuration, open *Edit/Project Settings/Graphics* and double click the *Scriptable Render Pipeline Settings* field to open the render pipeline settings.
This field will be populated if URP was successfully installed.

.. image:: /_media/GraphicsSettings1.png

After double clicking the graphics settings should appear in the Inspector. Transparency requires the following two options to be enabled, *Depth Texture* and *Opaque Texture*:

.. image:: /_media/UrpPipelineSettings1.png

.. only:: html

    .. raw:: html

        <h4>Shadowing</h4>

.. only:: latex

    Shadowing
    """""""""

To enable shadowing of the water surface to darken the appearance in shadows, open the *Forward Renderer Data* by clicking the gear icon in the render pipeline settings from the previous step:

.. figure:: /_media/UrpPipelineSettings2.png

    Gear/More icon

In the *Forward Renderer Data* add the *SampleShadows* render feature using the Add button:

.. image:: /_media/UrpPipelineSettingsRenderer1.png