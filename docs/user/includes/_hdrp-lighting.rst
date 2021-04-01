As other shaders would, the ocean will get its lighting from the primary directional light (AKA sun).
Like other mesh renderers, this can be masked by setting the *Rendering Layer Mask* property on the *Ocean
Renderer*.
Please see the :link:`{HDRP} documentation on light layers <{HDRPDocLink}/Light-Layers.html>` for more information on setup and usage.

But some lighting will come from the light set as the *Primary Light* on the *Ocean Renderer*.
This includes the sub-surface scattering colour.

Lighting can also be overriden with the *Indirect Lighting Controller*.
Please see the :link:`{HDRP} documentation on volume overrides <{HDRPDocLink}/Light-Layers.html>` for more information on setup and usage.

For the ocean to have lighting completely separate from everything else, you would need to do all of the above.
