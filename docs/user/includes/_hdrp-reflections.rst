`Crest` makes full use of the flexible lighting options in `HDRP` (it is lit the same as a shadergraph shader would be).

`HDRP` comes with a *Planar Reflection Probe* feature which enables dynamic reflection of the environment at run-time, with a corresponding cost.
See Unity's documentation on :link:`Planar Reflection Probes <{HDRPDocLink}/Planar-Reflection-Probe.html>`, but it is a little spares.

We could get it working by:

- Create new GameObject
- Set the height of the GameObject to the sea level
- Add the component from the Unity Editor menu using *Component/Rendering/Planar Reflection Probe*
- Set the extents of the probe to be large enough to cover everything that needs to be reflected
- Check the documentation linked above for details on individual parameters

.. tip::

  If reflections appear wrong, it can be useful to make a simple test shadergraph with our water normal map applied to it, to compare results.
  We provide a simple test shadergraph for debugging purposes - enable the *Apply Test Material* debug option on the *OceanRenderer* component to apply it.
  If you find you are getting good results with a test shadergraph but not with our ocean shader, please report this to us.
