# Wave conditions

## Authoring

To add waves, add the *ShapeGerstnerBatched* component to a GameObject.

The appearance and shape of the waves is determined by a *wave spectrum*.
A default wave spectrum will be created if none is specified.
To change the waves, right click in the Project view and select *Create/Crest/Ocean Wave Spectrum*, and assign the new asset to the *Spectrum* property of the *ShapeGerstnerBatched* script.

The spectrum has sliders for each wavelength to control contribution of different scales of waves.
To control the contribution of ~2m wavelengths, use the slider labelled '2'.

The *Wave Direction Variance* controls the spread of wave directions.
This controls how aligned the waves are to the wind direction.

The *Chop* parameter scales the horizontal displacement.
Higher chop gives crisper wave crests but can result in self-intersections or 'inversions' if set too high, so it needs to be balanced.

To aid in tweaking the spectrum values we provide implementations of common wave spectra from the literature.
Select one of the spectra by toggling the button, and then tweak the spectra inputs, and the spectrum values will be set according to the selected model.
When done, toggle the button off to stop overriding the spectrum.

All of the above can be tweaked in play mode.
Together these controls give the flexibility to express the great variation one can observe in real world seascapes.

## Local waves

By default the Gerstner waves will apply everywhere throughout the world, so 'globally'.
They can also be applied 'locally' - in a limited area of the world.

This is done by setting the *Mode* to *Geometry*.
In this case the system will look for a *MeshFilter*/*MeshRenderer* on the same GameObject and it will generate waves over the area of the geometry.
The geometry must be 'face up' - it must be visible from a top-down perspective in order to generate the waves.
It must also have a material using the *Crest/Inputs/Animated Waves/Gerstner Batch Geometry* shader applied.

For a concrete example, see the *GerstnerPatch* object in *boat.unity*.
It has a *MeshFilter* component with the *Quad* mesh applied, and is rotated so the quad is face up.
It has a *MeshRenderer* component with a material assigned with a Gerstner material.

The material has the *Feather at UV Extents* option enabled, which will fade down the waves where the UVs go to 0 or 1 (at the edges of the quad).
A more general solution is to scale the waves based on vertex colour so weights can be painted - this is provided through the *Weight from vertex colour (red channel)* option.
This allows different wave conditions in different areas of the world with smooth blending.


# Shorelines and shallow water

For this information in video format, see here: https://www.youtube.com/watch?v=jcmqUlboTUk

*Crest* requires water depth information to attenuate large waves in shallow water, to generate foam near shorelines, and to provide shallow water shading. It is calculated by rendering the render geometry in the scene for each LOD from a top down perspective and recording the Y value of the surface.

When the ocean is e.g. 250m deep, this will start to dampen 500m wavelengths, so it is recommended that the sea floor drop down to around this depth away from islands so that there is a smooth transition between shallow and deep water without a 'step' in the sea floor which appears as a discontinuity in the surface waves and/or a line of foam.

One way to inform *Crest* of the seabed is to attach the *RegisterSeaFloorDepthInput* component. *Crest* will record the height of these objects every frame, so they can be dynamic.

This dynamic update comes at a cost. For parts for of the seabed which are static, *Crest* has a mechanism for recording their heights just once, instead of updating every frame, using an ocean depth cache. The *main.unity* example scene has an example of a cache set up around the island. The cache GameObject is called *IslandDepthCache* and has a *OceanDepthCache* component attached. The following are the key points of its configuration:

* The transform position X and Z are centered over the island
* The transform position Y value is set to the sea level
* The transform scale is set to 540 which sets the size of the cache. If gizmos are visible and the cache is selected, the area is demarcated with a white rectangle.
* The *Camera Max Terrain Height* is the max height of any surfaces above the sea level that will render into the cache. If gizmos are visible and the cache is selected, this cut-off height is visualised as a translucent gray rectangle.
* The *Layer Names* field contains the layer that the island is assigned to: *Terrain*. Only objects in these layer(s) will render into the cache.

On startup, validation is done on the cache (and on various other components of the *Crest* setup). Be sure to check the log for warnings and errors.

At runtime, a child object underneath the cache will be created with the prefix *Draw_* it will have a material with a *Texture* property. By double clicking the icon to the right of this field, one can inspect the contents of the cache.

By default the cache is populated in the `Start()` function. It can instead be configured to populate from script by setting the *Refresh Mode* to *On Demand* and calling the `PopulateCache()` method on the component from script.

Once populated the cache contents can be saved to disk by clicking the *Save cache to file* button that will appear in the Inspector in play mode. Once saved, the *Type* can be set to *Baked* and the saved data can be assigned to the *Saved Cache* field.
