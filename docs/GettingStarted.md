# Getting Started

This section has steps for importing the _Crest_ content into a project, and for adding a new ocean surface to a scene.

> [!WARNING]
> Frequently when changing Unity versions the project can appear to break (no ocean rendering, materials appear pink, other issues). Usually restarting the Editor fixes it. In one case the scripts became unassigned in the example content scene, but closing Unity, removing the Library folder, and restarting resolved it.

## Getting Started Video

To augment / complement this written documentation, we published a getting started video which is available here:

<!-- select:start -->
<!-- select-menu-labels: Rendering Pipeline -->

### Built-in <!-- select-option -->

<iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/qsgeG4sSLFw" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen>
</iframe>

https://www.youtube.com/watch?v=qsgeG4sSLFw

### High Definition <!-- select-option -->

<iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/FE6l39Lt3js" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen>
</iframe>

https://www.youtube.com/watch?v=FE6l39Lt3js

### Universal <!-- select-option -->

<iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/TpJf13d_-3E" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen>
</iframe>

https://www.youtube.com/watch?v=TpJf13d_-3E

<!-- select:end -->

## Importing _Crest_ files into project

The steps to set up *Crest* in a new or existing project currently look as follows:

<!-- select:start -->
<!-- select-menu-labels: Rendering Pipeline -->

#### Built-in <!-- select-option -->

* Switch to Linear space rendering under *Edit/Project Settings/Player/Other Settings*. If your platform(s) require Gamma space, the material settings will need to be adjusted to compensate.
* Import *Crest* assets by either:
  * Picking a release from the [Releases page](https://github.com/huwb/crest-oceanrender/releases) and importing the desired packages
  * Getting latest by either cloning this repos or downloading it as a zip, and copying the *Crest/Assets/Crest/Crest* folder and the desired content from the nearby *Crest-Examples* folders into your project. Be sure to always copy the .meta files.


#### High Definition <!-- select-option -->

High Definition content

#### Universal <!-- select-option -->

##### Pipeline Setup

Ensure Universal Render Pipeline (URP) is setup and functioning, either by setting up a new project using the URP template or by installing the URP package into an existing project and configuring the Render Pipeline Asset.
Please see the [Unity documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@10.2/manual/InstallingAndConfiguringURP.html) for more information.

Switch to Linear space rendering under _Edit/Project Settings/Player/Other Settings_. If your platform(s) require Gamma space, the material settings will need to be adjusted to compensate.

##### Importing Crest

Import _Crest_ package into project using the _Asset Store_ window or the _Package Manager_ in the Unity Editor.

> [!TIP]
> The files under _Crest-Examples_ are not required by our core functionality, but are provided for illustrative purposes. We recommend first time users import them as they may provide useful guidance.

##### Transparency

To enable the water surface to be transparent, two options must be enabled in the URP configuration.
To find the configuration, open _Edit/Project Settings/Graphics_ and double click the _Scriptable Render Pipeline Settings_ field to open the render pipeline settings.
This field will be populated if URP was successfully installed.

![Graphics Settings](/_media/GraphicsSettings1.png)

After double clicking the graphics settings should appear in the Inspector. Transparency requires the following two options to be enabled, _Depth Texture_ and _Opaque Texture_:

![Pipeline Settings](/_media/UrpPipelineSettings1.png)

##### Shadowing

To enable shadowing of the water surface to darken the appearance in shadows, open the _Forward Renderer Data_ by clicking the gear icon in the render pipeline settings from the previous step:

![Pipeline Settings](/_media/UrpPipelineSettings2.png)

In the _Forward Renderer Data_ add the _SampleShadows_ render feature using the Add button:

![Pipeline Settings Renderer](/_media/UrpPipelineSettingsRenderer1.png)

<!-- select:end -->

## Adding the ocean to a scene

The steps to add an ocean to an existing scene are as follows:

* Preparation: generate lighting from the _Lighting_ window if necessary - the ocean lighting takes the ambient intensity from the baked spherical harmonics.
* Create a new _GameObject_ for the ocean, give it a descriptive name such as _Ocean_.
  * Assign the *OceanRenderer* component to it. On startup this component will generate the ocean geometry and do all required initialisation.
  * Assign the desired ocean material to the *OceanRenderer* script - this is a material using the *Crest/Ocean* shader.
  * Set the Y coordinate of the position to the desired sea level.
* Tag a primary camera as *MainCamera* if one is not tagged already, or provide the *Viewpoint* transform to the *OceanRenderer* script. If you need to switch between multiple cameras, update the *Viewpoint* field to ensure the ocean follows the correct view.
* To add waves, create a new GameObject and add the *Shape Gerstner Batched* component.
  * On startup this script creates a default ocean shape. To edit the shape, right click in the Project view and select *Create/Crest/Ocean Wave Spectrum* and provide it to this script.
  * Smooth blending of ocean shapes can be achieved by adding multiple *Shape Gerstner Batched* scripts and crossfading them using the *Weight* parameter.
* For geometry that should influence the ocean (attenuate waves, generate foam):
  * Static geometry should render ocean depth just once on startup into an *Ocean Depth Cache* - the island in the main scene in the example content demonstrates this.
  * Dynamic objects that need to render depth every frame should have a *Register Sea Floor Depth Input* component attached.
* Be sure to generate lighting from the Lighting window - the ocean lighting takes the ambient intensity from the baked spherical harmonics.
