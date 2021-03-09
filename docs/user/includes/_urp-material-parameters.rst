
.. note::

    This section was originally written for the LWRP ocean material.
    The material parameters may change for Crest URP.
    We are investigating building the ocean material on ShaderGraph and the material options and parameter names may change.
    When `URP` is released and these options are finalised this documentation will be updated.

Scattering

.. done

| **Diffuse** Base colour when looking straight down into water.
| **Diffuse Grazing** Base colour when looking into water at shallow/grazing angle.
| **Shadowing** Changes colour in shadow. Requires 'Create Shadow Data' enabled on OceanRenderer script.
| **Diffuse (Shadow)** Base colour in shadow.

Subsurface Scattering

.. done

| **Enable** Whether to to emulate light scattering through the water volume.
| **Colour** Colour tint for primary light contribution.
| **Base Mul** Amount of primary light contribution that always comes in.
| **Sun Mul** Primary light contribution in direction of light to emulate light passing through waves.
| **Sun Fall-Off** Fall-off for primary light scattering to affect directionality.

Shallow Scattering

.. done

| **Enable** Enable light scattering in shallow water.
| **Depth Max** Max depth that is considered 'shallow'.
| **Depth Power** Fall off of shallow scattering.
| **Shallow Colour** Colour in shallow water.
| **Shallow Colour (Shadow)** Shallow water colour in shadow (see comment on Shadowing param above).

Reflection Environment

| **Specular** Strength of specular lighting response.
| **Smoothness** Smoothness of surface.

| **Vary Smoothness Over Distance** Helps to spread out specular highlight in mid-to-background. From a theory point of view, models transfer of normal detail to microfacets in BRDF.
| **Smoothness Far** Material smoothness at far distance from camera.
| **Smoothness Far Distance** Definition of far distance.
| **Smoothness Power** How smoothness varies between near and far distance.


| **Softness** Acts as mip bias to smooth/blur reflection.
| **Light Intensity Multiplier** Main light intensity multiplier.
| **Fresnel Power** Controls harshness of Fresnel behaviour.
| **Refractive Index of Air** Index of refraction of air.
  Can be increased to almost 1.333 to increase visibility up through water surface.
| **Refractive Index of Water** Index of refraction of water. Typically left at 1.333.
| **Planar Reflections** Dynamically rendered 'reflection plane' style reflections.
  Requires OceanPlanarReflection script added to main camera.
| **Planar Reflections Distortion** How much the water normal affects the planar reflection.

Procedural Skybox

| **Enable** Enable a simple procedural skybox.
  Not suitable for realistic reflections, but can be useful to give control over reflection colour - especially in stylized/non realistic applications.
| **Base** Base sky colour.
| **Towards Sun** Colour in sun direction.
| **Directionality** Direction fall off.
| **Away From Sun** Colour away from sun direction.

Foam

| **Enable** Enable foam layer on ocean surface
| **Texture** Foam texture
| **Scale** Foam texture scale
| **Light Scale** Scale intensity of lighting
| **White Foam Color** Colour tint for whitecaps / foam on water surface
| **Bubble Foam Color** Colour tint bubble foam underneath water surface
| **Bubble Foam Parallax** Parallax for underwater bubbles to give
  feeling of volume
| **Shoreline Foam Min Depth** Proximity to sea floor where foam starts
  to get generated
| **Wave Foam Feather** Controls how gradual the transition is from full
  foam to no foam
| **Wave Foam Bubbles Coverage** How much underwater bubble foam is
  generated

Foam 3D Lighting

| **Enable** Generates normals for the foam based on foam values/texture
  and use it for foam lighting
| **Normals Strength** Strength of the generated normals
| **Specular Fall-Off** Acts like a gloss parameter for specular
  response
| **Specular Boost** Strength of specular response

Transparency

| **Enable** Whether light can pass through the water surface
| **Fog Density** Scattering coefficient within water volume, per
  channel
| **Refraction Strength** How strongly light is refracted when passing
  through water surface

Caustics

| **Enable** Approximate rays being focused/defocused on underwater
  surfaces
| **Caustics** Caustics texture
| **Scale** Caustics texture scale
| **Texture Average Value** The 'mid' value of the caustics texture,
  around which the caustic texture values are scaled
| **Strength** Scaling / intensity
| **Focal Depth** The depth at which the caustics are in focus
| **Depth Of Field** The range of depths over which the caustics are in
  focus
| **Distortion Strength** How much the caustics texture is distorted
| **Distortion Scale** The scale of the distortion pattern used to
  distort the caustics

Underwater

| **Enable** Whether the underwater effect is being used. This enables
  code that shades the surface correctly from underneath.
| **Cull Mode** Ordinarily set this to *Back* to cull back faces, but
  set to *Off* to make sure both sides of the surface draw if the
  underwater effect is being used.

Flow

| **Enable** Flow is horizontal motion in water as demonstrated in the
  'whirlpool' example scene. 'Create Flow Sim' must be enabled on the
  OceanRenderer to generate flow data.
