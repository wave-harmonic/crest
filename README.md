
# crest

![Teaser](https://raw.githubusercontent.com/huwb/crest-oceanrender/master/img/teaser3.png)  

Contacts: Huw Bowles (@hdb1 , huw dot bowles at gmail dot com), Daniel Zimmermann (@DanyGZimmermann, infkdude at gmail dot com), Chino Noris (@chino_noris , chino dot noris at epost dot ch), Beibei Wang (bebei dot wang at gmail dot com)


## Introduction

*Crest* is a technically advanced ocean renderer implemented in Unity3D. Some of the core ideas were described at SIGGRAPH 2017 in the *Advances in Real-Time Rendering* course (course page [link](http://advances.realtimerendering.com/s2017/index.html)). Since this initial publication we have been actively working to extend the feature set, which includes innovations in the following areas.


### Shape

It is well known that ocean shape can be well approximated by summing Gerstner waves. Dozens of these are required to obtain an interesting shape. In previous implementations this has been prohibitively expensive and shape is either generated using an online FFT, or precomputed and baked into textures.

We generate shape from Gerstner waves efficiently at runtime by rendering at multiple scales, and ensure that waves are never over-sampled (inefficient) or under-sampled (bad quality, aliasing). This is highly performant and gives detail close to the viewer, and shape to the horizon. This gives considerable flexibility in shape and opens possibilities such as attenuating waves based on ocean depth around shorelines.

We also introduce an intuitive and fun shape authoring interface - an *equalizer* style editor which makes it fast and easy to achieve surface shape. Art direction such as *small choppy waves with longer waves rolling in from a storm at the horizon* is simple to achieve in this framework. We also support empirical ocean spectra from the literature (Phillips, JONSWAP, etc) which can be used as a baseline.

We also explore simulating shape dynamically by solving the wave equation PDE efficiently in the same multi-scale framework. The branch *dynamic_simulation* generates the entire ocean shape using a dynamic sim that exhibits interesting effects such as dispersion, diffraction and reflection (this refers to physical wave behaviour, not light waves). This works well but is computed on a heightfield and loses some of the characteristic horizontal motion on the surface. The branch *local_sim* has a more practical scenario where a dynamic simulation handles local displacements and is added on top of existing Gerstner waves, to add local interactivity while maintaining the overall look and feel. The displacement of water from the boat's motion is approximated by a simple one-pass shader, and research is ongoing to make this more flexible and expressive.


### Mesh

We implement a 100% pop-free meshing solution, which follows the same unified multi-scale structure/layout as the shape data. The vertex densities and locations match the shape texels 1:1. This ensures that the shape is never over-sampled or under-sampled, giving the same guarantees as described above.

Our meshing approach requires only simple shader instructions in a vertex shader, and does not rely on tessellation or compute shaders or any other advanced shader model features. The geometry is composed of tiles which have strictly no overlap, and support frustum culling. These tiles are generated quickly on startup.

The multi-resolution representation (shape textures and geometry) is scaled horizontally when the camera changes altitude to ensure appropriate level of detail and good visual range for all viewpoints. To further extend the surface to the horizon we also add a strip of triangles at the mesh boundary.


### Shading / VFX

Normal maps are elegantly incorporated into our multi-scale framework. Normal map textures are treated as a slightly different form of shape that is too detailed to be efficiently sampled by geometry, and are sampled at a scale just below the shape textures. This combats typical normal map sampling issues such as lack of detail in the foreground, or a glassy flat appearance in the background.

Current shading effects include two foam layers are computed on the fly from the Jacobian of the displacement textures, and a subsurface scattering approximation based on view and normal vectors.

The branch *fx_test* explores dynamically generating spray particle effects by randomly sampling points on the surface to detect wave peaks.


## Configuration

The components described above are driven by a small number of key parameters which are trivial to understand and tweak. The primary parameters configure the multi-scale representation. Unless otherwise specified thes parameters reside on the *OceanRenderer* component.

### Ocean Construction Parameters

* **Base Vert density** - the base vert/shape texel density of an ocean patch. If you set the scale of a LOD to 1, this density would be the world space verts/m. More means more verts/shape, at the cost of more processing.
* **Lod Count** - the number of levels of detail / scales of ocean geometry to generate. More means more dynamic range of usable shape/mesh at the cost of more processing.
* **Max Wave Height** - this is just so that the ocean tiles bounding box height can be set, to ensure culling eliminates tiles correctly.

### Runtime Global Parameters

* **Wind direction angle** - this global wind direction affects the ocean shape
* **Chop** - controls how much horizontal displacement is present in the ocean shape
* **Max Scale** - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints. This sets the maximum the ocean will be scaled if set to a positive value.
* **Min Scale** - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level. This should be set to a low value gives lots of detail, but will limmit the horizontal extents of the ocean as the detail scales have a limited dynamic range (set by the previous Lod Count parameter).

### Ocean Shape

Ocean shape is currently authored on the *OceanWavesBatched* game object. The *WaveSpectrum* component provides an equalizer interface to tweak gain values for different frequency levels. We recommend combining use of the *Freeze waves* feature on the debug overlay, the toggle boxes in the equalizer, and undo/redo, to do fine tweaking of the ocean surface shape.

For reference a number of empirical spectra are also implemented and can be applied to the spectrum by clicking the appropriate toggle button. We find it interesting to observe how the surface shape evolves when a spectrum is enabled and the wind speed is tweaked.


## How it Works

On startup, the *OceanBuilder* script creates the ocean geometry as a LODs, each composed of geometry tiles and a shape camera to render the displacement texture for that LOD.

At run-time, the viewpoint is moved first, and then the *Ocean* object is placed at sea level under the viewer. A horizontal scale is computed for the ocean based on the viewer height, as well as a *_viewerAltitudeLevelAlpha* that captures where the camera is between the current scale and the next scale (x2), and allows a smooth transition between scales to be achieved using the two mechanisms described in the SIGGRAPH course.

Once the ocean has been placed, the ocean surface shape is generated by rendering Gerstner wave components into the shape LODs. These are visualised on screen if the *Show shape data* debug option is enabled. Each wave components is rendered into the shape LOD that is appropriate for the wavelength, to prevent over- or under- sampling and maximize efficiency. A final pass combines the results down the shape LODs (from largest to most-detailed), disable the *Shape combine pass* debug option to see the shape contents before this pass.

The ocean geometry is rendered with the Ocean shader. The vertex shader snaps the verts to grid positions to make them stable. It then computes a *lodAlpha* which starts at 0 for the inside of the LOD and becomes 1 at the outer edge. It is computed from taxicab distance as noted in the course. This value is used to drive the vertex layout transition, to enable a seemless match between the two. The vertex shader then samples the current LOD shape texture and the next shape texture and uses *lodAlpha* to interpolate them for a smooth transition across displacement textures. A foam value is also computed using the determinant of the Jacobian of the displacement texture. Finally, it passes the LOD geometry scale and *lodAlpha* to the pixel shader.

The ocean pixel shader samples normal maps at 2 different scales, both proportional to the current and next LOD scales, and then interpolates the result using *lodAlpha* for a smooth transition. Two layers of foam are added based on different thresholds of the foam value, with black point fading used to blend them.


## Bugs and Improvement Directions

* Add colour variation in shallow water
* The shape rendering currently happens for the entire shape lod texture, which covers areas outside of the view. One could render the shape only for the visible surface by generating geometry for the frustum, dilating and rendering this into the shape textures. There is a branch that begins to explore this: *render_frustum_into_shape*.
* Using prebaked textures (i.e. from an offline ocean simulation) would be easy to implement in our framework by rendering the prebaked results into the shape textures, and would be the most efficient option (although completely dynamic shape now renders very efficiently).
* Ocean surface tiles are updated and drawn as separate draw calls. This is convenient for research and supports frustum culling easily, but it might make sense to instance these in a production scenario.
* The refraction component of the ocean material looks odd in the Scene View. This is ugly but not high on the list of priorities currently.


## Links

### Core work

* A classic - Simulating Ocean Water - Tessendorf - has iWave approach for interactive apps: http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.131.5567&rep=rep1&type=pdf
* Also from tessendorf: https://people.cs.clemson.edu/~jtessen/papers_files/Interactive_Water_Surfaces.pdf
* Great thesis about implementing water sim into frostbite: http://www.dice.se/wp-content/uploads/2014/12/water-interaction-ottosson_bjorn.pdf
* Rigorous follow up work to Ottosson: https://gmrv.es/Publications/2016/CMTKPO16/main.pdf
* Water sim on (fixed) quad tree, talks about some of the issues with this: https://pdfs.semanticscholar.org/a3c5/5aeda63895d846c38ae23e921cec7320f584.pdf
* Strugar does multiple overlapping sims: article: http://vertexasylum.com/2010/10/30/gpu-based-water-simulator-thingie/ , video: https://www.youtube.com/watch?time_continue=20&v=jrhjxudnMNg
* GDC course notes from matthias mueller fischer: http://matthias-mueller-fischer.ch/talks/GDC2008.pdf
* Slightly old list of CG water references: http://vterrain.org/Water/
* Mueller - swe + splashes, ripples - nice results: https://pdfs.semanticscholar.org/e97f/38cb774c96aaf1c359d8331695efa3b2c26c.pdf , video: https://www.youtube.com/watch?v=bojdpqi2l_o
* Gomez 2000 - Interactive Simulation of Water Surfaces - Game Programming Gems
* Real-Time Open Water Environments with Interacting Objects - Cords and Staadt. Discusses/justifies multiple sims. Divides collision shapes into particles. - http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.162.2833&rep=rep1&type=pdf

### Wave Theory

* Useful notes on dispersive and non-dispersive waves: http://www-eaps.mit.edu/~rap/courses/12333_notes/dispersion.pdf
* More notes on waves: https://thayer.dartmouth.edu/~d30345d/books/EFM/chap4.pdf
* Dispersive wave equation: https://ccrma.stanford.edu/~jos/pasp/Dispersive_1D_Wave_Equation.html
* Dispersion does not apply to tsunamis: http://www.bu.edu/pasi-tsunami/files/2013/01/daytwo12.pdf
* Longer wavelengths travel faster. For a swell, longest wavelengths arrive first: 
..* https://physics.stackexchange.com/questions/121327/what-determines-the-speed-of-waves-in-water/121330#121330
..* https://en.wikipedia.org/wiki/Wind_wave
* Detailed SWE description from Thuerey: https://pdfs.semanticscholar.org/c902/c4f2c61734cbf4ec7ee8b792ccb01644943d.pdf
* Using SWE for ocean on large scales: http://kestrel.nmt.edu/~raymond/classes/ph332/notes/shallowgov/shallowgov.pdf
* Three stages of how wind generates waves, with refs: https://www.wikiwaves.org/Ocean-Wave_Spectra
* Miles - how energy is transferred from wind to wave: https://www.cambridge.org/core/journals/journal-of-fluid-mechanics/article/on-the-generation-of-surface-waves-by-shear-flows/40B503619B6D4571BEF3D31CB8925084
* Realistic simulation of waves using wave spectra: https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf
* Nice practical demo about testing different wave breakers: https://youtu.be/3yNoy4H2Z-o
* Useful notes/diagrams on waves: http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html, http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1

### Boundary conditions

* http://hplgit.github.io/wavebc/doc/pub/._wavebc_cyborg002.html
* https://pdfs.semanticscholar.org/c902/c4f2c61734cbf4ec7ee8b792ccb01644943d.pdf

### Water depth

* Wave speeds for different water depths (after eqn 4.9): https://tutcris.tut.fi/portal/files/4312220/kellomaki_1354.pdf . It also says the SWE are equivlanet to the WE although i didnt understand how/why. also discusses RB coupling.
* SWE with changing ocean depths: https://arxiv.org/pdf/1202.6542.pdf

### Breaking waves

* Real-time: http://matthias-mueller-fischer.ch/publications/breakingWaves.pdf

### Experiments

* 1D wave equation in shadertoy: https://www.shadertoy.com/view/MtlfzM
* Propagate gerstner waves with wave equation - click to simulate wind: https://www.shadertoy.com/view/XtlBDr

### Particle sim

* Mixes SPH and WE, uses SPH to get low frequency 3D flow: http://citeseerx.ist.psu.edu/viewdoc/download;jsessionid=8A10D0187910134E8C8330AF1C57B146?doi=10.1.1.127.1749&rep=rep1&type=pdf
* Mueller - deposits splash particles on surface, video: https://www.youtube.com/watch?v=bojdpqi2l_o

### Notes on generating ocean waves into simulation

* Sum of gerstner waves - each frame compute gerstner waves that are appropriate for each sim, apply a force to the ocean surface to pull towards gerstner wave
* Write dynamic state into sim - write dynamic state of an FFT or the sum of gerstner waves into the sim. This could be stamped onto the sim periodically, if the surface repeats with a given period. This is possible - each sim has a particular wave speed. If a strict scheme of only writing a particular wave length into each sim was employed, this would mean the waves would repeat with a particular period. However it's non-obvious how this could be strictly enforced in a practical game-like situation.
* The generation of waves by wind is well understood: https://www.wikiwaves.org/Ocean-Wave_Spectra . This could be modelled. There is a transfer of energy across wavelengths that allows waves that travel faster than wind to be generated, perhaps this can be modelled by transferring energy across sims. This feels like the approach that fits most accurately into the sim paradigm. It would require wind to be defined everywhere. Another problem is that this process occurs over large fetch areas (thousands of wavelengths in size), whereas the sim domains are very bounded, so the process would need to be accelerated (?).

