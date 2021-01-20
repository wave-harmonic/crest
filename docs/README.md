<img src="https://raw.githubusercontent.com/huwb/crest-oceanrender/master/logo/crest-oceanrender-logotype1.png" width="214">

&nbsp;


# Overview

*Crest* is a technically-advanced ocean system for Unity. It is architected for performance and makes heavy use of Level Of Detail (LOD) strategies and GPU acceleration for fast update and rendering. It is also highly flexible and allows any custom input to the water shape/foam/dynamic waves/etc, and has an intuitive and easy to use shape authoring interface.


# Initial setup

A video walkthrough of the setup steps below is available on youtube: https://www.youtube.com/watch?v=qsgeG4sSLFw .

<iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/qsgeG4sSLFw" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

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
  * On startup this script creates a default ocean shape. To edit the shape, right click in the Project view and select *Create/Crest/Ocean Wave Spectrum* and provide it to this script.
  * Smooth blending of ocean shapes can be achieved by adding multiple *Shape Gerstner Batched* scripts and crossfading them using the *Weight* parameter.
* For geometry that should influence the ocean (attenuate waves, generate foam):
  * Static geometry should render ocean depth just once on startup into an *Ocean Depth Cache* - the island in the main scene in the example content demonstrates this.
  * Dynamic objects that need to render depth every frame should have a *Register Sea Floor Depth Input* component attached.
* Be sure to generate lighting from the Lighting window - the ocean lighting takes the ambient intensity from the baked spherical harmonics.
