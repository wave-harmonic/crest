<img src="https://raw.githubusercontent.com/huwb/crest-oceanrender/master/logo/crest-oceanrender-logotype1.png" width="214">

&nbsp;


# Overview

*Crest* is a technically-advanced ocean system for Unity. It is architected for performance and makes heavy use of Level Of Detail (LOD) strategies and GPU acceleration for fast update and rendering. It is also highly flexible and allows any custom input to the water shape/foam/dynamic waves/etc, and has an intuitive and easy to use shape authoring interface.


# Initial setup

A video walkthrough of the setup steps below is available on youtube: https://www.youtube.com/watch?v=qsgeG4sSLFw .

Note: Frequently when changing Unity versions the project can appear to break (no ocean rendering, materials appear pink, other issues). Usually restarting the Editor fixes it. In one case the scripts became unassigned in the example content scene, but closing Unity, removing the Library folder, and restarting resolved it.

## Importing Crest files into project

The steps to set up *Crest* in a new or existing project currently look as follows:

* Switch to Linear space rendering under *Edit/Project Settings/Player/Other Settings*. If your platform(s) require Gamma space, the material settings will need to be adjusted to compensate.
* Import *Crest* assets by either:
  * Picking a release from the [Releases page](https://github.com/huwb/crest-oceanrender/releases) and importing the desired packages
  * Getting latest by either cloning this repos or downloading it as a zip, and copying the *Crest/Assets/Crest/Crest* folder and the desired content from the nearby *Crest-Examples* folders into your project. Be sure to always copy the .meta files.

## Adding the ocean to a scene

The steps to set up the ocean:

* Create a new game object for the ocean
  * Assign the *OceanRenderer* component to it. On startup this component will generate the ocean geometry and do all required initialisation.
  * Assign the desired ocean material to the *OceanRenderer* script - this is a material using the *Crest/Ocean* shader.
  * Set the Y coordinate of the position to the desired sea level.
* Tag a primary camera as *MainCamera* if one is not tagged already, or provide the *Viewpoint* transform to the *OceanRenderer* script. If you need to switch between multiple cameras, update the *Viewpoint* field to ensure the ocean follows the correct view.
* To add waves, create a new GameObject and add the *Shape Gerster Batched* component.
  * On startup this script creates a default ocean shape. To edit the shape, create an asset of type *Crest/Ocean Wave Spectrum* and provide it to this script.
  * Smooth blending of ocean shapes can be achieved by adding multiple *Shape Gerstner Batched* scripts and crossfading them using the *Weight* parameter.
* For geometry that should influence the ocean (attenuate waves, generate foam):
  * Static geometry should render ocean depth just once on startup into an *Ocean Depth Cache* - the island in the main scene in the example content demonstrates this.
  * Dynamic objects that need to render depth every frame should have a *Register Sea Floor Depth Input* component attached.
* Be sure to generate lighting from the Lighting window - the ocean lighting takes the ambient intensity from the baked spherical harmonics.


# Configuration

## Ocean look and behaviour

* Ocean material / shading: The default ocean materials contain many tweakable variables to control appearance. Turn off unnecessary features to maximize performance.
* Animated waves / ocean shape: Configured on the *ShapeGerstnerBatched* script by providing an *Ocean Wave Spectrum* asset. This asset has an equalizer-style interface for tweaking different scales of waves, and also has some parametric wave spectra from the literature for comparison.
* Ocean foam: Configured on the *OceanRenderer* script by providing a *Sim Settings Foam* asset.
* Dynamic wave simulation: Configured on the *OceanRenderer* script by providing a *Sim Settings Wave* asset.
* A big strength of *Crest* is that you can add whatever contributions you like into the system. You could add your own shape or deposit foam onto the surface where desired. Inputs are generally tagged with the *Register* scripts and examples can be found in the example content scenes.

All settings can be live authored. When tweaking ocean shape it can be useful to freeze time (set *Time.timeScale* to 0) to clearly see the effect of each octave of waves.

### Reflections

Reflections contribute hugely to the appearance of the ocean. The Index of Refraction settings control how much reflection contributes for different view angles. 

The base reflection comes from a one of these sources:

* Unity's specular cubemap. This is the default and is the same as what is applied to glossy objects in the scene. It will support reflection probes, as long as the probe extents cover the ocean tiles, which enables real-time update of the reflection environment (see Unity documentation for more details).
* Override reflection cubemap. If desired a cubemap can be provided to use for the reflections. For best results supply a HDR cubemap.
* Procedural skybox - developed for stylized games, this is a simple approximation of sky colours that will give soft results.

This base reflection can then be overridden by dynamic planar reflections. This can be used to augment the reflection with 3D objects such as boat or terrain. This can be enabled by applying the *Ocean Planar Reflections* script to the active camera and configuring which layers get reflected (don't include water). This renders every frame by default but can be configured to render less frequently. This only renders one view but also only captures a limited field of view of reflections, and the reflection directions are scaled down to help keep them in this limited view, which can give a different appearance. Furthermore 'planar' means the surface is approximated by a plane which is not the case for wavey ocean, so the effect can break down. This method is good for capturing local objects like boats etc.

## Ocean construction parameters

There are just two parameters that control the construction of the ocean shape and geometry:

* **Lod Data Resolution** - the resolution of the various ocean LOD data including displacement textures, foam data, dynamic wave sims, etc. Sets the 'detail' present in the ocean - larger values give more detail at increased run-time expense.
* **Geometry Down Sample Factor** - geometry density - a value of 2 will generate one vert per 2x2 LOD data texels. A value of 1 means a vert is generated for every LOD data texel. Larger values give lower fidelity surface shape with higher performance.
* **Lod Count** - the number of levels of detail / scales of ocean geometry to generate. The horizontal range of the ocean surface doubles for each added LOD, while GPU processing time increases linearly. It can be useful to select the ocean in the scene view while running in editor to inspect where LODs are present.

## Global parameters

* **Wind direction angle** - this global wind direction affects the ocean shape
* **Max Scale** - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints. This sets the maximum the ocean will be scaled if set to a positive value.
* **Min Scale** - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level. Low values give lots of detail, but will limit the horizontal extents of the ocean detail.


# High level system overview

On startup, the *OceanBuilder* script creates the ocean geometry at a set of scales/LOD levels, each composed of geometry tiles and a shape camera to render the displacement texture for that LOD.

At run-time, the ocean system updates its state in *LateUpdate*, after game state update and animation, etc. *OceanRenderer* updates before other scripts and first calculates a position and scale for the ocean. The ocean gameobject is placed at sea level under the viewer. A horizontal scale is computed for the ocean based on the viewer height, as well as a *_viewerAltitudeLevelAlpha* that captures where the camera is between the current scale and the next scale (x2), and allows a smooth transition between scales to be achieved.

Next any active LOD data are updated, such as animated waves, simulated foam, simulated waves, etc. The LOD data types are documented below. The ocean surface shape is generated by rendering Gerstner wave components into the shape LODs. These are visualised on screen if the *Show shape data* debug option is enabled. Each wave component is rendered into the shape LOD that is appropriate for the wavelength, to prevent over- or under- sampling and maximize efficiency. A final pass combines the results down the shape LODs (from largest to most-detailed), disable the *Shape combine pass* debug option to see the shape contents before this pass.

Finally *BuildCommandBuffer* constructs a command buffer to execute the ocean update on the GPU early in the frame before the graphics queue starts. See the *BuildCommandBuffer* code for the update scheduling and logic.

The ocean geometry is rendered by Unity as part of the graphics queue, and uses the *Crest/Ocean* shader. The vertex shader snaps the verts to grid positions to make them stable. It then computes a *lodAlpha* which starts at 0 for the inside of the LOD and becomes 1 at the outer edge. It is computed from taxicab distance as noted in the course. This value is used to drive the vertex layout transition, to enable a seemless match between the two. The vertex shader then samples the current LOD shape texture and the next shape texture and uses *lodAlpha* to interpolate them for a smooth transition across displacement textures. A foam value is also computed using the determinant of the Jacobian of the displacement texture. Finally, it passes the LOD geometry scale and *lodAlpha* to the pixel shader.

The ocean pixel shader samples normal maps at 2 different scales, both proportional to the current and next LOD scales, and then interpolates the result using *lodAlpha* for a smooth transition. Two layers of foam are added based on different thresholds of the foam value, with black point fading used to blend them.

Some of these components are described in more technical detail at SIGGRAPH 2017 in the *Advances in Real-Time Rendering* course (course page [link](http://advances.realtimerendering.com/s2017/index.html)).


# Ocean LOD data types

The backbone of *Crest* is an efficient Level Of Detail (LOD) representation for data that drives the rendering, such as surface shape/displacements, foam values, shadowing data, water depth, and others. This data is stored in a multi-resolution format, namely cascaded textures that are centered at the viewer. This data is generated and then sampled when the ocean surface geometry is rendered. This is all done on the GPU using a command buffer constructed each frame by *BuildCommandBuffer*.

Let's study one of the LOD data types in more detail. The surface shape is generated by the Animated Waves LOD Data, which maintains a set of *displacement textures* which describe the surface shape. A top down view of these textures laid out in the world looks as follows:

![CascadedShapeOverlapped](https://raw.githubusercontent.com/huwb/crest-oceanrender/master/img/doc/CascadedShapeOverlapped.png)

Each LOD is the same resolution (256x256 here), configured on the *OceanRenderer* script.
In this example the largest LOD covers a large area (4km squared), and the most detail LOD provides plenty of resolution close to the viewer.
These textures are visualised in the Debug GUI on the right hand side of the screen:

![DebugShapeVis](https://raw.githubusercontent.com/huwb/crest-oceanrender/master/img/doc/DebugShapeVis.png)

In the above screenshot the foam data is also visualised (red textures), and the scale of each LOD is clearly visible by looking at the data contained within. In the rendering each LOD is given a false colour which shows how the LODs are arranged around the viewer and how they are scaled. Notice also the smooth blend between LODs - LOD data is always interpolated using this blend factor so that there are never pops are hard edges between different resolutions.

In this example the LODs cover a large area in the world with a very modest amount of data. To put this in perspective, the entire LOD chain in this case could be packed into a small texel area:

![ShapePacked](https://raw.githubusercontent.com/huwb/crest-oceanrender/master/img/doc/ShapePacked.png)

A final feature of the LOD system is that the LODs change scale with the viewpoint. From an elevated perspective, horizontal range is more important than fine wave details, and the opposite is true when near the surface. The *OceanRenderer* has min and max scale settings to set limits on this dynamic range.

When rendering the ocean, the various LOD data are sample for each vert and the vert is displaced. This means that the data is carried with the waves away from its rest position. For some data like wave foam this is fine and desirable. For other data such as the depth to the ocean floor, this is not a quantity that should move around with the waves and this can currently cause issues, such as shallow water appearing to move with the waves as in issue 96.

The following sections describe the LOD data types in more detail.


## Animated Waves

The Gerstner waves are split by octave - each Gerstner wave component is only rendered once into the most suitable LOD (i.e. a long wavelength will render only into one of the large LODs), and then a combine pass is done to copy results from the resolution LODs down to the high resolution ones.

Crest supports rendering any shape into these textures. To add some shape, add some geometry into the world which when rendered from a top down perspective will draw the desired displacements. Then assign the *RegisterAnimWavesInput* script which will tag it for rendering into the shappe.

There is an example in the *boat.unity* scene, gameobject *wp0*, where a smoothstep bump is added to the water shape. This is an efficient way to generate dynamic shape. This renders with additive blend, but other blending modes are possible such as alpha blend, multiplicative blending, and min or max blending, which give powerful control over the shape.

The final shape textures are copied back to the CPU to provide collision information for physics etc, using the *ReadbackLodData* script.

The animated waves sim can be configured by assigning an Animated Waves Sim Settings asset to the OceanRenderer script in your scene (*Create/Crest/Animated Wave Sim Settings*). The waves will be dampened/attenuated in shallow water if a *Sea Floor Depth* LOD data is used (see below). The amount that waves are attenuated is configurable using the *Attenuation In Shallows* setting.


## Dynamic Waves

This LOD data is a multi-resolution dynamic wave simulation, which gives dynamic interaction with the water.

One use case for this is boat wakes. In the *boat.unity* scene, the geometry and shader on the *WaterObjectInteractionSphere0* will render forces into the sim. It has the *RegisterDynWavesInput* script that tags it as input.

After the simulation is advanced, the results are converted into displacements and copied into the displacement textures to affect the final ocean shape. The sim is added on top of the existing Gerstner waves.

Similar to animated waves, user provided contributions can be rendered into this LOD data to create dynamic wave effects. An example can be found in the boat prefab. Each LOD sim runs independently and it is desirable to add interaction forces into all appropriate sims. The *FeedVelocityToExtrude* script takes into account the boat size and counts how many sims are appropriate, and then weights the interaction forces based on this number, so the force is spread evenly to all sims. As noted above, the sim results will be copied into the dynamic waves LODs and then accumulated up the LOD chain to reconstruct a single simulation.

The dynamic waves sim can be configured by assigning a Dynamic Wave Sim Settings asset to the OceanRenderer script in your scene (*Create/Crest/Dynamic Wave Sim Settings*).


## Foam

The Foam LOD Data is simple type of simulation for foam on the surface. Foam is generated by choppy water (specifically when the surface is *pinched*). Each frame, the foam values are reduced to model gradual dissipation of foam over time.

User provided foam contributions can be added similar to the Animated Waves. In this case the *RegisterFoamInput* script should be applied to any inputs. There is no combine pass for foam so this does not have to be taken into consideration - one must simply render 0-1 values for foam as desired. See the *DepositFoamTex* object in the *whirlpool.unity* scene for an example.

The foam sim can be configured by assigning a Foam Sim Settings asset to the OceanRenderer script in your scene (*Create/Crest/Foam Sim Settings*). There are also parameters on the material which control the appearance of the foam.


## Sea Floor Depth

This LOD data provides a sense of water depth. This is useful information for the system; it is used to attenuate large waves in shallow water, to generate foam near shorelines, and to provide shallow water shading. It is calculated by rendering the render geometry in the scene for each LOD from a top down perspective and recording the Y value of the surface.

The following will contribute to ocean depth:

* Objects that have the *RegisterSeaFloorDepthInput* component attached. These objects will render every frame. This is useful for any dynamically moving surfaces that need to generate shoreline foam, etc.
* It is also possible to place world space depth caches. The scene objects will be rendered into this cache once, and the results saved. Once the cache is populated it is then copied into the Sea Floor Depth LOD Data. The cache has a gizmo that represents the extents of the cache (white outline) and the near plane of the camera that renders the depth (translucent rectangle). The cache should be placed at sea level and rotated/scaled to encapsulate the terrain.

When the water is e.g. 250m deep, this will start to dampen 500m wavelengths, so it is recommended that the sea floor drop down to around this depth away from islands so that there is a smooth transition between shallow and deep water without a visible boundary.


## Shadow

To enable shadowing of the ocean surface, data is captured from the shadow maps Unity renders. These shadow maps are always rendered in front of the viewer. The Shadow LOD Data then reads these shadow maps and copies shadow information into its LOD textures.

It stores two channels - one channel is normal shadowing, and the other jitters the lookup and accumulates across many frames to blur and soften the shadow data. The latter channel is used to affect scattered light within the water volume.

The shadow sim can be configured by assigning a Shadow Sim Settings asset to the OceanRenderer script in your scene (*Create/Crest/Shadow Sim Settings*).

Currently in the built-in render pipeline, shadows only work when the primary camera is set to Forward rendering.


# Collision Shape for Physics

There are two options to access the ocean shape on the CPU (from script) in order to compute buoyancy physics or perform camera collision, etc.
These options are configured on the *Animated Waves Sim Settings*, assigned to the OceanRenderer script, using the Collision Source dropdown.
These options are described in the following sections.

The system supports sampling collision at different resolutions.
The query functions have a parameter *Min Spatial Length* which is used to indicate how much detail is desired.
Wavelengths smaller than half of this min spatial length will be excluded from consideration.

Sampling the height of a displacement texture is in general non-trivial.
A displacement can define a concave surface with overhanging elements such as a wave that has begun to break.
At such locations the surface has multiple heights, so we need some mechanism to search for a height.
Luckily there is a powerful tool to do this search known as Fixed Point Iteration (FPI).
For an introduction to FPI and a discussion of this scenario see this GDC talk: [link](http://www.huwbowles.com/fpi-gdc-2016/).
Computing this height is relatively expensive as each search step samples the displacement.
To help reduce cost a height cache can be enabled in the *Animated Waves Sim Settings* which will cache the water height at a 2D position so that any subsequent samples in the same frame will quickly return the height.

## Ocean Displacement Textures GPU

This collision source copies the displacement textures from the GPU to the CPU. It does so asynchronously and the data typically takes 2-3 frames to arrive.
 This is the default collision source and gives the final ocean shape, including any bespoke shape rendering, attenuation from water depth, and any other effects.

It uses memory bandwidth to transfer this data and CPU time to take a copy of it once it arrives, so it is best to limit the number of textures copied.
If you know in advance the limits of the minimum spatial lengths you will be requesting, set these on the *Animated Waves Sim Settings* using the *Min Object Width* and *Max Object Width* fields.

As described above the displacements are arranged as cascaded textures which shift based on the elevation of the viewpoint.
This complicates matters significantly as the requested resolutions may or may not exist at different times.
Call *ICollProvider.CheckAvailability()* at run-time to check for issues and perform validation.

## Gerstner Waves CPU

This collision option is serviced directly by the *GerstnerWavesBatched* component which implements the *ICollProvider* interface, check this interface to see functionality.
This sums over all waves to compute displacements, normals, velocities, etc. In contrast to the displacement textures the horizontal range of this collision source is unlimited.

This avoids some of the complexity of using the displacement textures described above, but comes at a CPU cost.
It also does not include wave attenuation from water depth or any custom rendered shape.
A final limitation is the current system finds the first GerstnerWavesBatched component in the scene which may or may not be the correct one.
The system does not support cross blending of multiple scripts.


# Other features

## Underwater

*Crest* supports seamless transitions above/below water. This is demonstrated in the *main.unity* scene in the example content. The ocean in this scene uses the material *Ocean-Underwater.mat* which enables rendering the underside of the surface, and has the prefab *UnderWaterCurtainGeom* parented to the camera which renders the underwater effect. It also has the prefab *UnderWaterMeniscus* parented which renders a subtle line at the intersection between the camera lens and the water to visually help the transition.

## Masking out surface

There are times when it is useful to mask out the ocean surface which prevents it drawing on some part of the screen.
The scene *main.unity* in the example content has a rowboat which, without masking, would appear to be full of water.
To prevent water appearing inside the boat, the *WaterMask* gameobject writes depth into the GPU's depth buffer which can occlude any water behind it, and therefore prevent drawing water inside the boat.
The *RegisterMaskInput* component is required to ensure this depth draws early before the ocean surface.

## Floating origin

*Crest* has support for 'floating origin' functionality, based on code from the Unity community wiki. See the original wiki page for an overview and original code: [link](http://wiki.unity3d.com/index.php/Floating_Origin).

It is tricky to get pop free results for world space texturing. To make it work the following is required:

* Set the floating origin threshold to a power of 2 value such as 4096.
* Set the size/scale of any world space textures to be a smaller power of 2. This way the texture tiles an integral number of times across the threshold, and when the origin moves no change in appearance is noticeable. This includes the following textures:
  * Normals - set the Normal Mapping Scale on the ocean material
  * Foam texture - set the Foam Scale on the ocean material
  * Caustics - also should be a power of 2 scale, if caustics are visible when origin shifts happen 

By default the *FloatingOrigin* script will call *FindObjectsOfType()* for a few different component types, which is a notoriously expensive operation. It is possible to provide custom lists of components to the 'override' fields, either by hand or programmatically, to avoid searching the entire scene(s) for the components. Managing these lists at run-time is left to the user.

## Buoyancy / Floating Physics

*BoatAlignNormal* is a simple script that attempts to match the object position and rotation with the surface height and normal. This can work well enough for small water craft that don't need perfect floating behaviour, or floating objects such as buoys, barrels, etc.

*BoatProbes* is a more advanced implementation that computes buoyancy forces at a number of *ForcePoints* and uses these to apply force and torque to the object. This gives more accurate results at the cost of more queries.

We've found issues caused by having multiple overlapping physics collision primitives (or multiple rigidbodies) within the floating object, or when the object pivot is not at the center of mass.

# Q&A

**Is Crest well suited for medium-to-low powered mobile devices?**
Crest is built to be performant by design and has numerous quality/performance levers.
However it is also built to be very flexible and powerful and as such can not compete with a minimal, mobile-centric ocean renderer such as the one in the *BoatAttack* project.
Therefore we target Crest at PC/console platforms.

**Which platforms does Crest support?**
Testing occurs primarily on Windows.
We have users targeting Windows, Mac, Linux, PS4, XboxOne, Switch and iOS/Android.
Performance is a challenge on Switch and mobile platforms - see the previous question.

**Is Crest well suited for localised bodies of water such as lakes?**
Currently Crest is currrently targeted towards large bodies of water.
The water could be pushed down where it's not wanted which would allow it to achieve rivers and lakes to some extent.

**Does Crest support third party sky assets?**
We have heard of Crest users using TrueSky, AzureSky.
These may require some code to be inserted into the ocean shader - there is a comment referring to this, search *Ocean.shader* for 'Azure'.

**Can Crest work in Edit mode in the Unity Editor, or only in Play mode?**
Currently it only works in Play mode. Some work has been done to make it work in Edit mode but more work/fixes/testing is needed. https://github.com/huwb/crest-oceanrender/issues/208

**Can Crest work with multiplayer?**
Yes the animated waves are deterministic and easily synchronized.
See discussion in https://github.com/huwb/crest-oceanrender/issues/75.
However, the dynamic wave sim is not fully deterministic and can not currently be relied upon networked situations.
