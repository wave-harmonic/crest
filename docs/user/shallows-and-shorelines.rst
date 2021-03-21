.. _shallows:

Shorelines and Shallows
=======================

.. versionchanged:: 4.9

    *Layer Names* (String[]) has changed to *Layers* (LayerMask).

*Crest* requires water depth information to attenuate large waves in shallow water, to generate foam near shorelines, and to provide shallow water shading.
The way this information is typically generated is through the *OceanDepthCache* component, which takes one or more layers, and renders everything in those layers (and within its bounds) from a top-down orthographic view to generate a heightfield for the seabed.
These layers could contain the render geometry/terrains, or it could be geometry that is placed in a non-rendered layer that serves only to populate the depth cache.
By default this generation is done at run-time during startup, but the component exposes other options such as generating offline and saving to an asset, or rendering on demand.

The seabed affects the wave simulation in a physical way - the rule of thumb is *waves will be affected by the seabed when the water depth is less than half of their wavelength*.
So for example when the water is 250m deep, this will start to dampen 500m wavelengths from the spectrum, so it is recommended that the seabed drop down to at least 500m away from islands so that there is a smooth transition between shallow and deep water without a 'step' in the sea floor which appears as a discontinuity in the surface waves and/or a line of foam.

Setup
-----

.. youtube:: jcmqUlboTUk

   Depth Cache usage and setup

One way to inform *Crest* of the seabed is to attach the *RegisterSeaFloorDepthInput* component.
*Crest* will record the height of these objects every frame, so they can be dynamic.

The *main.unity* example scene has an example of a cache set up around the island.
The cache GameObject is called *IslandDepthCache* and has a *OceanDepthCache* component attached.
The following are the key points of its configuration:

-  The transform position X and Z are centered over the island
-  The transform position y value is set to the sea level
-  The transform scale is set to 540 which sets the size of the cache.
   If gizmos are visible and the cache is selected, the area is demarcated with a white rectangle.
-  The *Camera Max Terrain Height* is the max height of any surfaces above the sea level that will render into the cache.
   If gizmos are visible and the cache is selected, this cutoff is visualised as a translucent gray rectangle.
-  The *Layers* field contains the layer that the island is assigned to (*Terrain* in our project).
   Only objects in these layer(s) will render into the cache.
-  Both the transform scale (white rectangle) and the *Layers* property determine what will be rendered into the cache.

By default the cache is populated in the *Start()* function.
It can instead be configured to populate from script by setting the *Refresh Mode* to *On Demand* and calling the *PopulateCache()* method on the component from script.

Once populated the cache contents can be saved to disk by clicking the *Save cache to file* button that will appear in the Inspector in play mode.
Once saved, the *Type* field can be set to *Baked* and the saved data can be assigned to the *Saved Cache* field.

Shoreline Waves
---------------

Modelling realistic shoreline waves efficiently is a challenging open problem.
We discuss further and make suggestions on how to set up shorelines with *Crest* in the following video.

.. youtube:: Y7ny8pKzWMk

   Tweaking Shorelines

Troubleshooting
---------------

*Crest* runs validation on the depth caches - look for warnings/errors in the Inspector, and in the log at run-time, where many issues will be highlighted.

To inspect the contents of the cache, look for a child GameObject parented below the cache with the name prefix *Draw\_*.
It will have a material with a *Texture* property.
By double clicking the icon to the right of this field, one can inspect the contents of the cache.
The cache will appear black for dry land and red for water that is at least 1m deep.
