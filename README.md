
<img src="https://raw.githubusercontent.com/huwb/crest-oceanrender/master/logo/crest-oceanrender-logotype1.png" width="214">

&nbsp;


# Intro

*Crest* is a technically advanced ocean renderer implemented in Unity3D (2018.1+).

![Teaser](https://raw.githubusercontent.com/huwb/crest-oceanrender/master/img/teaser4.png)


# Releases

Crest exercises [semantic versioning](https://semver.org/) and follows the branching strategy outlined [here](https://gist.github.com/stuartsaunders/448036/5ae4e961f02e441e98528927d071f51bf082662f), although there is no develop branch used yet - development occurs on feature branches that are merged directly into master. Unitypackages are uploaded with each release.


# The Tech

Some of the core ideas were described at SIGGRAPH 2017 in the *Advances in Real-Time Rendering* course (course page [link](http://advances.realtimerendering.com/s2017/index.html)). Since this initial publication we have been actively working to extend the feature set, which includes innovations in the following areas.


## Shape

It is well known that ocean shape can be well approximated by summing Gerstner waves. Dozens of these are required to obtain an interesting shape. In previous implementations this has been prohibitively expensive and shape is either generated using an online FFT, or precomputed and baked into textures.

We generate shape from Gerstner waves efficiently at runtime by rendering at multiple scales, and ensure that waves are never over-sampled (inefficient) or under-sampled (bad quality, aliasing). This is highly performant and gives detail close to the viewer, and shape to the horizon. This gives considerable flexibility in shape and opens possibilities such as attenuating waves based on ocean depth around shorelines.

We also introduce an intuitive and fun shape authoring interface - an *equalizer* style editor which makes it fast and easy to achieve surface shape. Art direction such as *small choppy waves with longer waves rolling in from a storm at the horizon* is simple to achieve in this framework. We also support empirical ocean spectra from the literature (Phillips, JONSWAP, etc) which can be used directly or as a comparison.

The branch *local_sim3* layers a dynamic wave simulation on top of the ocean waves to add local interactivity while maintaining the overall look and feel. The wave simulation also deposits foam enabling boat wakes to be generated.

The final shape is asynchronously read back to the CPU for gameplay/physics use. This gives access to the full, rich shape without requiring expensive CPU calculations or pipeline stalls.


## Mesh

We implement a 100% pop-free meshing solution, which follows the same unified multi-scale structure/layout as the shape data. The vertex densities and locations match the shape texels 1:1. This ensures that the shape is never over-sampled or under-sampled, giving the same guarantees as described above.

Our meshing approach requires only simple shader instructions in a vertex shader, and does not rely on tessellation or compute shaders or any other advanced shader model features. The geometry is composed of tiles which have strictly no overlap, and support frustum culling. These tiles are generated quickly on startup.

The multi-resolution representation (shape textures and geometry) is scaled horizontally when the camera changes altitude to ensure appropriate level of detail and good visual range for all viewpoints. To further extend the surface to the horizon we also add a strip of triangles at the mesh boundary.


## Shading / VFX

Normal maps are elegantly incorporated into our multi-scale framework. Normal map textures are treated as a slightly different form of shape that is too detailed to be efficiently sampled by geometry, and are sampled at a scale just below the shape textures. This combats typical normal map sampling issues such as lack of detail in the foreground, or a glassy flat appearance in the background.

The ocean colour comes from a subsurface scattering approximation based on view and normal vectors, and primary light direction.

Foam is shaded in two layers. Underwater bubbles have parallax and refraction offsets to give an impression of depth. White foam on top of the water is shaded with a simple 3D lighting model using procedurally generated normals.

Transparency and refraction are also supported, and Schlick's fresnel approximation selects between the refracted colour and a reflected colour. There is an option on the material to boost specular highlights by adding directional lighting if needed.

All shading features are on static switches and can be disabled if not required.

As an area of future work, the branch *fx_test* explores dynamically generating spray particle effects by randomly sampling points on the surface to detect wave peaks.


# Setup

The steps to set up Crest in a new or existing project currently look as follows. There is an example of all this running in *Crest-Examples/Scenes/main*.

* Switch your project to Linear space rendering under *Edit > Project Settings > Player > Other Settings*. If your platform(s) require Gamma space, the surface colours will need to be tweaked accordingly.
* Copy across the contents of the *Crest* folder - this has all the necessary components and assets. Be sure to include the .meta files.
  * Some of the infrastructure for versioned Releases has been set up but is still evolving. These steps will be updated the release process has matured.
* Drag *Crest/Prefabs/Ocean.prefab* into your scene(s), set y coordinate to desired sea level. On startup, this will generate the ocean geometry and initialise the ocean systems.
* To add waves, create a new GameObject and add the *Shape Gerster Batched* component.
  * This will create a default ocean shape. To edit the shape, create an asset of type *Crest/Ocean Wave Spectrum* and assign it to this script.
  * Smooth blending of ocean shapes can be achieved by adding multiple *Shape Gerstner Batched* scripts and crossfading them using the Weight parameter.
* For geometry that should influence the ocean (attenuate waves, generate foam):
  * Static geometry should render ocean depth just once on startup into an *Ocean Depth Cache*.
  * Dynamic objects that need to render depth every frame should have a *Render Ocean Depth* component attached.
* Be sure to generate lighting from the Lighting window - the ocean lighting takes the ambient intensity from the baked spherical harmonics.

Enjoy!


# Configuration

The components described above are driven by a small number of key parameters which are trivial to understand and tweak. The primary parameters configure the multi-scale representation. Unless otherwise specified thes parameters reside on the *OceanRenderer* component.

## Ocean Construction Parameters

There are just two parameters that control the construction of the ocean shape and geometry:

* **Base Vert density** - the base vert/shape texel density of an ocean patch. If you set the scale of a LOD to 1, this density would be the world space verts/m. More means more verts/shape, at the cost of more processing.
* **Lod Count** - the number of levels of detail / scales of ocean geometry to generate. More means more dynamic range of usable shape/mesh at the cost of more processing.

## Runtime Global Parameters

* **Wind direction angle** - this global wind direction affects the ocean shape
* **Max Scale** - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints. This sets the maximum the ocean will be scaled if set to a positive value.
* **Min Scale** - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level. Low values give lots of detail, but will limit the horizontal extents of the ocean detail.

## Ocean Shape

Ocean shape is currently authored on the *OceanWavesBatched* game object. The *WaveSpectrum* component provides an equalizer interface to tweak gain values for different frequency levels. We recommend combining use of the *Freeze waves* feature on the debug overlay, the toggle boxes in the equalizer, and undo/redo, to do fine tweaking of the ocean surface shape.

For reference a number of empirical spectra are also implemented and can be applied to the spectrum by clicking the appropriate toggle button. We find it interesting to observe how the surface shape evolves when a spectrum is enabled and the wind speed is tweaked.


# How it Works

On startup, the *OceanBuilder* script creates the ocean geometry as a LODs, each composed of geometry tiles and a shape camera to render the displacement texture for that LOD.

At run-time, the viewpoint is moved first, and then the *Ocean* object is placed at sea level under the viewer. A horizontal scale is computed for the ocean based on the viewer height, as well as a *_viewerAltitudeLevelAlpha* that captures where the camera is between the current scale and the next scale (x2), and allows a smooth transition between scales to be achieved using the two mechanisms described in the SIGGRAPH course.

Once the ocean has been placed, the ocean surface shape is generated by rendering Gerstner wave components into the shape LODs. These are visualised on screen if the *Show shape data* debug option is enabled. Each wave component is rendered into the shape LOD that is appropriate for the wavelength, to prevent over- or under- sampling and maximize efficiency. A final pass combines the results down the shape LODs (from largest to most-detailed), disable the *Shape combine pass* debug option to see the shape contents before this pass.

The ocean geometry is rendered with the Ocean shader. The vertex shader snaps the verts to grid positions to make them stable. It then computes a *lodAlpha* which starts at 0 for the inside of the LOD and becomes 1 at the outer edge. It is computed from taxicab distance as noted in the course. This value is used to drive the vertex layout transition, to enable a seemless match between the two. The vertex shader then samples the current LOD shape texture and the next shape texture and uses *lodAlpha* to interpolate them for a smooth transition across displacement textures. A foam value is also computed using the determinant of the Jacobian of the displacement texture. Finally, it passes the LOD geometry scale and *lodAlpha* to the pixel shader.

The ocean pixel shader samples normal maps at 2 different scales, both proportional to the current and next LOD scales, and then interpolates the result using *lodAlpha* for a smooth transition. Two layers of foam are added based on different thresholds of the foam value, with black point fading used to blend them.


# Bugs and Improvement Directions

* Using prebaked textures (i.e. from an offline ocean simulation) would be easy to implement in our framework by rendering the prebaked results into the shape textures, and would be the most efficient option (although completely dynamic shape now renders very efficiently).
* Persistent foam - generate from waves/dynamic sim, fade gradually over time
* Wetness simulation for shore
* Flow - texture to paint wind direction

# Contacts

Huw Bowles (@hdb1 , huw dot bowles at gmail dot com), Daniel Zimmermann (@DanyGZimmermann, infkdude at gmail dot com), Chino Noris (@chino_noris , chino dot noris at epost dot ch), Beibei Wang (bebei dot wang at gmail dot com)


# Links

Moved to [LINKS.md](https://github.com/huwb/crest-oceanrender/blob/master/LINKS.md).
