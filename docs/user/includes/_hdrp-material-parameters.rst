Normal Mapping

.. done

| **Normals** Normal map texture (should be set to Normals type in the
  properties)
| **Normals Scale** Scale of normal map texture
| **Normals Strength** Strength of normal map influence

Scattering

.. done

| **Scatter Colour Base** Base colour when looking straight down into
  water
| **Scatter Colour Shadow** Base colour in shadow. Requires 'Create
  Shadow Data' enabled on OceanRenderer script.
| **Scatter Colour Shallow** Colour in shallow water
| **Scatter Colour Depth Max** Max depth that is considered 'shallow'
| **Scatter Colour Depth Falloff** Fall off of shallow scattering

Subsurface Scattering

.. done

| **SSS Intensity Base** Amount of primary light contribution that always comes in.
| **SSS Intensity Sun** Primary light contribution in direction of light to emulate light passing through waves.
| **SSS Tint** Colour tint for primary light contribution.
| **SSS Sun Falloff** Fall-off for primary light scattering to affect directionality.

Reflection Environment

.. done

| **Specular** Strength of specular lighting response
| **Occlusion** Strength of reflection
| **Smoothness** Smoothness of surface
| **Smoothness Far** Material smoothness at far distance from camera
| **Smoothness Far Distance** Definition of far distance
| **Smoothness Falloff** How smoothness varies between near and far distance

Foam

| **Enable** Enable foam layer on ocean surface.
| **Foam** Foam texture.
| **Foam Scale** Foam texture scale.
| **Foam Feather** Controls how gradual the transition is from full foam to no foam.
| **Foam Albedo Intensity** Scale intensity of diffuse lighting.
| **Foam Emissive Intensity** Scale intensity of emitted light.
| **Foam Smoothness** Smoothness of foam material.
| **Foam Normal Strength** Strength of the generated normals.
| **Foam Bubbles Color** Colour tint bubble foam underneath water surface.
| **Foam Bubbles Parallax** Parallax for underwater bubbles to give feeling of volume.
| **Foam Bubbles Coverage** How much underwater bubble foam is generated.

Transparency

| **Refraction Strength** How strongly light is refracted when passing through water surface.
| **Depth Fog Density** Scattering coefficient within water volume, per channel.

Caustics

| **Enable** Approximate rays being focused/defocused on underwater
  surfaces
| **Caustics** Caustics texture
| **Caustics Scale** Caustics texture scale
| **Caustics Texture Grey Point** The 'mid' value of the caustics
  texture, around which the caustic texture values are scaled
| **Caustics Strength** Scaling / intensity
| **Caustics Focal Depth** The depth at which the caustics are in focus
| **Caustics Depth Of Field** The range of depths over which the
  caustics are in focus
| **Caustics Distortion Strength** How much the caustics texture is
  distorted
| **Caustics Distortion Scale** The scale of the distortion pattern used
  to distort the caustics

Underwater

| **Cull Mode** Ordinarily set this to *Back* to cull back faces, but set to *Off* to make sure both sides of the surface draw if the underwater effect is being used.

Flow

| **Enable** Flow is horizontal motion in water as demonstrated in the 'whirlpool' example scene.
  'Create Flow Sim' must be enabled on the OceanRenderer to generate flow data.
