.. only:: html

   .. raw:: html

      <h4>Transparency</h4>

.. only:: latex

   Transparency
   """"""""""""

To enable the water surface to be transparent, two options must be enabled in the URP configuration.
To find the configuration, open *Edit/Project Settings/Graphics* and double click the *Scriptable Render Pipeline Settings* field to open the render pipeline settings.
This field will be populated if URP was successfully installed.

.. image:: /_media/GraphicsSettings.png

After double clicking the graphics settings should appear in the Inspector.
Transparency requires the following two options to be enabled, *Depth Texture* and *Opaque Texture*:

.. image:: /_media/UrpPipelineSettings.png

.. TODO:
.. We should ask Unity to improve documentation on locating the URP asset(s) so we can just link to it.
.. The best they have is /configuring-universalrp-for-use.html#adding-the-asset-to-your-graphics-settings.

Read :link:`Unity's documentation on the {URP} Asset <{URPDocLink}/universalrp-asset.html#general>` for more information on these options.


.. only:: html

   .. raw:: html

      <h4>Shadowing</h4>

.. only:: latex

   Shadowing
   """""""""

To enable shadowing of the water surface which will darken the appearance in shadows, add the *Sample Shadows* Render Feature by following :link:`How to add a Renderer Feature to a Renderer <{URPDocLink}/urp-renderer-feature-how-to-add.html>`.
