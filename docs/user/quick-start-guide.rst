Quick Start Guide
=================

This section provides a summary of common steps for setting up water with links for further reading:

-  **Add water**: Add *Crest* water to your scene as described in section :ref:`add-crest-to-scene-section`.

-  **Ocean surface appearance**: The active ocean material is displayed below the *OceanRenderer* component.
   The material parameters are described in section :ref:`material_parameters`.
   Turn off unnecessary features to maximize performance.

-  **Add Waves**: Add *Shape FFT* component to a GameObject and assign a *Ocean Wave Spectrum* asset.
   Waves can be generated everywhere, or in specific areas by placing them on a *Spline*.
   See section :ref:`wave-conditions-section`.

-  **Shallow Water**: To reduce waves in shallow water capture the underlying terrain shape into a *Ocean Depth Cache*.
   See section :ref:`shallows`.

-  **Oceans, Rivers and Lakes**: *Crest* supports setting up networks of connected water bodies, by setting the sea level via *Spline* inputs.
   See section :ref:`water-bodies`.

-  **Underwater**: If the camera needs to go underwater, the underwater effect must be enabled.
   See section :ref:`underwater`.

-  **Dynamic wave simulation**: Simulates dynamic effects like object-water interaction.
   See section :ref:`dynamic-waves-section`.

-  **Boats**: Several components combined can create a convincing boat.
   See page :ref:`watercraft`.

-  **Networking**: *Crest* is built with networking in mind and can synchronise waves across the network.
   It also has limited support for headless servers.
   See section :ref:`network-synchronisation`.

-  **Open Worlds**: *Crest* comes with 'floating origin' support to enable large open worlds.
   See section :ref:`floating-origin`.

.. tip::

   .. include:: includes/_animated-materials.rst
