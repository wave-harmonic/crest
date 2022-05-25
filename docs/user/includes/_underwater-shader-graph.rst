Once the *Shader API* is enabled, any transparent object in the correct layer and using a modified shader (more on that later) will have its above water pixels rendered in the transparent pass and below water pixels rendered after the underwater pass.

In a perfect world, we would render the underwater pass before the transparent pass, and then apply the underwater effect to the final color of each transparent object using the *CrestNodeApplyUnderWaterFog* node.
But *Shader Graph* does not allow modification of the final color.

The workaround is in the example node *CrestNodeApplyUnderwaterFogExample*.
This node uses the *CrestNodeApplyUnderWaterFog* node and does a few things to get around this problem:

-  Apply the underwater effect only to the Emission input to bypass *Unity*'s lighting
-  Reduce the alpha and the color by distance from the camera

The end result is that the effect is inconsistent with the underwater pass.
Despite that we believe it is a decent enough approximation until *Unity* improves this area.

.. admonition:: Example

    |  We have an example *Surface Shader* which you can use as a reference:
    |  *Crest/Crest-Examples/Shared/Shaders/LitTransparentWithUnderwaterFog.shadergraph*

    Furthermore, you can view the shader in action in the *Transparent Object Underwater* example in the *Examples* scene.

Setting up a graph can be broken down to the following:

1. Add optional keywords (see example graph)
2. Add the *CrestNodeApplyUnderwaterFogExample* node
3. Connect *Fogged Color* (and alpha) and *Fogged Emission* outputs to the `Master_Stack`
4. Multiply *Factor* output with any properties except *Ambient Occlusion*
5. Enable *Alpha Clipping*
