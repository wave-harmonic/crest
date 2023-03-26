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
