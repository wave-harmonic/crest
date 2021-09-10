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

.. note::

   If you are using the underwater effect, it is recommended to set *Opaque Downsampling* to *None*.
   *Opaque Downsampling* will make everything appear at a lower resolution when underwater.
   Be sure to test to see if recommendation is suitable for your project.

.. TODO:
.. We should ask Unity to improve documentation on locating the URP asset(s) so we can just link to it.
.. The best they have is /configuring-universalrp-for-use.html#adding-the-asset-to-your-graphics-settings.

Read :link:`Unity's documentation on the {URP} Asset <{URPDocLink}/universalrp-asset.html#general>` for more information on these options.
