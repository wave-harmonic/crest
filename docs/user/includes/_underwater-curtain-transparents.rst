The following requires shader knowledge.
One could create a new Unlit shader, create a material using this shader, and set the *Render Queue* property on the material to *Transparent*.
This will draw the material after the underwater effect has been drawn, which will make it visible, but it will not have the underwater effect applied.
The most obvious issue will be that the water fog is not applied.
The effect of the fog either needs to be faked by simply ramping the opacity down to 0 based on distance from the camera, or the water fog shader code needs to be included and called from the transparent shader.
The shader *UnderwaterCurtain.shader* is a good reference for calculating the underwater effect.
This will require various parameters on the shader like fog density and others.
