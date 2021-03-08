.. only:: html

    .. raw:: html

        <h4>Transparency</h4>

.. only:: latex

    Transparency
    """"""""""""

To enable the water surface to be transparent, two options must be enabled in the URP configuration.
To find the configuration, open *Edit/Project Settings/Graphics* and double click the *Scriptable Render Pipeline Settings* field to open the render pipeline settings.
This field will be populated if URP was successfully installed.

.. https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.5/manual/configuring-universalrp-for-use.html#adding-the-asset-to-your-graphics-settings

.. image:: /_media/GraphicsSettings1.png

After double clicking the graphics settings should appear in the Inspector. Transparency requires the following two options to be enabled, *Depth Texture* and *Opaque Texture*:

.. image:: /_media/UrpPipelineSettings1.png

.. TODO:
.. We should ask Unity to improve documentation on locating the URP asset(s) so we can just link to it. The best they
.. have is /configuring-universalrp-for-use.html#adding-the-asset-to-your-graphics-settings.

Read :link:`Unity's documentation on the {URP} Asset <{URPDocLink}/universalrp-asset.html#general>` for more information on these options.


.. only:: html

    .. raw:: html

        <h4>Shadowing</h4>

.. only:: latex

    Shadowing
    """""""""

To enable shadowing of the water surface which will darken the appearance in shadows, add the *Sample Shadows* Render Feature by following :link:`How to add a Renderer Feature to a Renderer <{URPDocLink}/urp-renderer-feature-how-to-add.html>`.

.. To enable shadowing of the water surface to darken the appearance in shadows, open the *Forward Renderer Data* by clicking the gear icon in the render pipeline settings from the previous step:

.. .. figure:: /_media/UrpPipelineSettings2.png

..     Gear/More icon

.. In the *Forward Renderer Data* add the *SampleShadows* render feature using the Add button:

.. .. image:: /_media/UrpPipelineSettingsRenderer1.png