Open Worlds
===========

`Crest` follows the camera so it is inheritently suitable for open worlds, but there will be engine issues to account for.


.. _floating-origin:

Floating Origin
---------------

*Crest* has support for 'floating origin' functionality, based on code from the *Unity Community Wiki*.
See the :link:`Floating Origin wiki page <https://wiki.unity3d.com/index.php/Floating_Origin>` for an overview and original code.

By default the *FloatingOrigin* script will call *FindObjectsOfType()* for a few different component types, which is a notoriously expensive operation.
It is possible to provide custom lists of components to the "override" fields, either by hand or programmatically, to avoid searching the entire scene(s) for the components.
Managing these lists at run-time is left to the user.

Stable World Shifts
^^^^^^^^^^^^^^^^^^^

.. tip::

    Not changing the threshold default value is the simplest approach to stable world shifts.

To avoid popping in the waves, the threshold needs to be set to *WaveResolution x LargestWavelength*.
The *WaveResolution* is on the Shape component (*Shape FFT* or *Shape Gerstner*) as just *Resolution*.
The *LargestWaveLength* can be found on the *Ocean Wave Spectrum* (the sliders with numbers from 0.0625
to 512) - find the largest number you are using.

For example, a *Shape FFT* with a resolution of 16 and a largest wavelength of 256 will require a threshold value at a minimum of 4,096 to have stable shifts.
You could halve this number once at the cost of some instability, but anything low will have very noticeable shifts.
