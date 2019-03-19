
# Initial setup

## Importing Crest files into project

The steps to set up *Crest* in a new or existing project currently look as follows:

* Switch your project to Linear space rendering under *Edit > Project Settings > Player > Other Settings*. If your platform(s) require Gamma space, the material settings will need to be adjusted to compensate.
* Import *Crest* assets by either:
  * Picking a release from the [Releases page](https://github.com/huwb/crest-oceanrender/releases) and importing the desired packages
  * Getting latest by either cloning this repos or downloading it as a zip, and copying the *Crest* folder and the desired content from the *Crest-Examples* folders into your project. Be sure to always copy the .meta files.
  * Note that the *Crest* files are separated into the core files to import in any project, and example content. The core is intentionally kept small and general. If you are getting started for the first time you may want to import both and then remove what you don't need from the example content.

## Adding the ocean to a scene

The steps to set up the ocean:

* Create a new game object for the ocean
  * Assign the *OceanRenderer* component to it. On startup this component will generate the ocean geometry and do all required initialisation.
  * Assign the desired ocean material to the *OceanRenderer* script - this is a material using the *Crest/Ocean* shader.
  * Set the Y coordinate of the position to the desired sea level.
* Tag a primary camera as *MainCamera* if one is not tagged already, or provide the viewpoint transform to the *OceanRenderer* script.
* To add waves, create a new GameObject and add the *Shape Gerster Batched* component.
  * On startup this script creates a default ocean shape. To edit the shape, create an asset of type *Crest/Ocean Wave Spectrum* and provide it to this script.
  * Smooth blending of ocean shapes can be achieved by adding multiple *Shape Gerstner Batched* scripts and crossfading them using the *Weight* parameter.
* For geometry that should influence the ocean (attenuate waves, generate foam):
  * Static geometry should render ocean depth just once on startup into an *Ocean Depth Cache* - the island in the main scene in the example content demonstrates this.
  * Dynamic objects that need to render depth every frame should have a *Register Sea Floor Depth Input* component attached.
* Be sure to generate lighting from the Lighting window - the ocean lighting takes the ambient intensity from the baked spherical harmonics.

Have fun!


# Configuration

## Ocean look and behaviour

* Ocean material / shading: The default ocean materials contain many tweakable variables to control appearance. Turn off unnecessary features to maximize performance.
* Animated waves / ocean shape: Configured on the *ShapeGerstnerBatched* script by providing an *Ocean Wave Spectrum* asset. This asset has an equalizer-style interface for tweaking different scales of waves, and also has some parametric wave spectra from the literature for comparison.
* Ocean foam: Configured on the *OceanRenderer* script by providing a *Sim Settings Foam* asset.
* Dynamic wave simulation: Configured on the *OceanRenderer* script by providing a *Sim Settings Wave* asset.
* A big strength of *Crest* is that you can add whatever contributions you like into the system. You could add your own shape or deposit foam onto the surface where desired. Inputs are generally tagged with the *Register* scripts and examples can be found in the example content scenes.

All settings can be live authored. When tweaking ocean shape it can be useful to freeze time (set *Time.timeScale* to 0) to clearly see the effect of each octave of waves.

## Ocean construction parameters

There are just two parameters that control the construction of the ocean shape and geometry:

* **Lod Data Resolution** - the resolution of the various ocean LOD data including displacement textures, foam data, dynamic wave sims, etc. Sets the 'detail' present in the ocean - larger values give more detail at increased run-time expense.
* **Geometry Down Sample Factor** - geometry density - a value of 2 will generate one vert per 2x2 LOD data texels. A value of 1 means a vert is generated for every LOD data texel. Larger values give lower fidelity surface shape with higher performance.
* **Lod Count** - the number of levels of detail / scales of ocean geometry to generate. The horizontal range of the ocean surface doubles for each added LOD, while GPU processing time increases linearly. It can be useful to select the ocean in the scene view while running in editor to inspect where LODs are present.

## Global parameters

* **Wind direction angle** - this global wind direction affects the ocean shape
* **Max Scale** - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints. This sets the maximum the ocean will be scaled if set to a positive value.
* **Min Scale** - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level. Low values give lots of detail, but will limit the horizontal extents of the ocean detail.


