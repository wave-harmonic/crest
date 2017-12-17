
# crest

![Teaser](https://raw.githubusercontent.com/huwb/crest-oceanrender/master/img/teaser.png)  

Contacts: Huw Bowles (@hdb1 , huw dot bowles at gmail dot com), Daniel Zimmermann (@DanyGZimmermann, infkdude at gmail dot com), Chino Noris (@chino_noris , chino dot noris at epost dot ch), Beibei Wang (bebei dot wang at gmail dot com)


## Introduction

*Crest* is a Unity3D implementation of a number of novel ocean rendering techniques published at SIGGRAPH 2017 in the *Advances in Real-Time Rendering* course (course page [link](http://advances.realtimerendering.com/s2017/index.html)).

It demonstrates a number of techniques described in this course:

* CDClipmaps - a new meshing approach that combines the simplicity of Clipmaps with the Continuous Detail of CDLOD.
* GPU-based shape system - each LOD has an associated displacement texture which is rendered by the *WaveDataCam* game object.
* Normal map scaling - a technique to improve the range of view distances for which a set of normal maps will work.
* Foam - two foam layers that are computed on the fly from the displacement textures.

Additionally, since the course, we have converted the kinematic shape system to a fully dynamic multi-scale simulation. The waves are generated from wind as in real life, using a wave spectrum modelled of real wave measurements. The waves interact with terrain in a realistic way. The sims fit naturally into our shape structure and LOD framework and are pop-free in all situations.


## Summary of contributions

We introduce an elegant, unified data structure for an anti-aliased, level of detail water simulation and rendering system.

Level of detail is a central consideration in our system. We use the infinity norm with square isolines to drive the detail - this fits well to the square textures we use to simulate the water shape. We do not require spatial data structures such as quad trees to select detail which simplifies our implementation enormously.

The water shape is stored in multiple overlapping nested textures that are centered around the viewer. Each texture represents a different scale. The smallest scale gives high detail close to the viewer. Water simulation is done in the textures for each scale individually. The wave propagation speed is dependent on scale to support dispersion. No coupling between the simulations are required. Before rendering the results from the sims accumulated from largest sim to smallest, to add the large scale waves to the more detailed lods.

The ocean surface is rendered by submitting geometry tiles which are placed around the viewer on startup. The tiles are generated on the CPU on startup. The shape textures are sampled in the vertex shader to compute the final shape. The layout and resolution of the tiles match 1:1 with the shape texture resolution, so that data resolution and sampling rate are well matched. Building the mesh out of tiles afford standard frustum culling.

When the viewer moves, shape rendering snaps to texel positions and geometry is smoothly transitioned out towards the boundaries. The LODs also slide up and down scales when the viewer changes altitude. A height interpolation parameter deals with fading shape in/out at the lowest/highest levels of detail. This eliminates visible pops/discontinuities.

Although normal maps are not stored in shape textures or simulated, they are treated as first class shape, and are scaled with the LODs so that they always give the appearance of waves that are higher detailed than the most detailed shape LOD. The required scaling and blending calculations hang off the ocean geometry scales and use the same interpolation parameters.

The above gives a complete ocean rendering system. There are just a few core parameters which are intuitive to tweak, such as a single overall resolution slider and the number of LOD levels to generate.


## How it Works

On startup, the *OceanBuilder* script creates the ocean geometry as a LODs, each composed of geometry tiles and a shape camera to render the displacement texture for that LOD. It has the following parameters that are passed to it on startup from the OceanRenderer script:

* Base Vert density - the base vert/shape texel density of an ocean patch. If you set the scale of a LOD to 1, this density would be the world space verts/m. More means more verts/shape, at the cost of more processing.
* Lod Count - the number of levels of detail / scales of ocean geometry to generate. More means more dynamic range of usable shape/mesh at the cost of more processing.
* Max Wave Height - this is just so that the ocean tiles bounding box height can be set, to ensure culling eliminates tiles correctly.
* Max Scale - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints. This sets the maximum the ocean will be scaled if set to a positive value.
* Min Scale - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level. This should be set to a low value gives lots of detail, but will limmit the horizontal extents of the ocean as the detail scales have a limited dynamic range (set by the previous Lod Count parameter).

At run-time, the viewpoint is moved first, and then the *Ocean* object is placed at sea level under the viewer. A horizontal scale is compute for the ocean based on the viewer height, as well as a *_viewerAltitudeLevelAlpha* that captures where the camera is between the current scale and the next scale (x2), and allows a smooth transition between scales to be achieved using the two mechanisms described in the course.

Once the ocean has been placed, the ocean surface shape is simulated, as follows:

1. For each shape texture, from largest to smallest (camera Depth ensures this), any geometry marked as WaveData layer renders into the shape cameras (ShapeCam0, 1, ..):
    1. Wave simulation shader runs first (via ShapeSimWaveEqn.shader on WaveSim quad). The render queue (2000) ensures it renders before subsequent shape stuff in the next step. After running the wave PDE this writes out: (x,y,z,w) = (current water height for this LOD, previous water height for this LOD, LOD foam value computed based on downwards acceleration of water surface, 0.).
    2. Any interaction forces/shapes render afterwards (such as ShapeObstacle.shader on Obstacle1 quad). This can either add values to the surface, add foam, or both.
2. After the lod 0 shape camera ShapeCam0 has rendered, OnShapeCamerasFinishedRendering() is called. This does a combine pass where the results of the different simulations are accumulated down the LOD chain. E.g. lod 4 is copied into lod 3. The water height is added and written into the unused W channel. The foam value is simply added in place. The x,y channels are not touched, as these will be read in the simulate shader in the next frame (step 1).
3. When each ocean chunk will be rendered, OceanChunkRenderer::OnWillRenderObject() is called. This grabs the target texture off the appropriate shape camera and assigns it to the shader for sampling in the ocean vertex shader.

The ocean geometry itself as the Ocean shader attached. The vertex shader snaps the verts to grid positions to make them stable. It then computes a *lodAlpha* which starts at 0 for the inside of the LOD and becomes 1 at the outer edge. It is computed from taxicab distance as noted in the course. This value is used to drive the vertex layout transition, to enable a seemless match between the two. The vertex shader then samples the current LOD shape texture and the next shape texture and uses *lodAlpha* to interpolate them for a smooth transition across displacement textures. A foam value is also computed using the determinant of the Jacobian of the displacement texture. Finally, it passes the LOD geometry scale and *lodAlpha* to the pixel shader.

The ocean pixel shader samples normal maps at 2 different scales, both proportional to the current and next LOD scales, and then interpolates the result using *lodAlpha* for a smooth transition. Two layers of foam are added based on different thresholds of the foam value, with black point fading used to blend them.


## Bugs and Improvement Directions

* Each Gerstner wave is computed and blended into the displacement texture individually. This makes them very easy to work and convenient, but baking them down to a single pass would be an interesting optimisation direction. Using prebaked textures (i.e. from an offline ocean simulation) would also be an option.
* Ocean tiles are updated and drawn as separate draw calls. This is convenient for research and supports frustum culling easily, but it might make sense to instance these in a production scenario.


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
* Mueller - deposits splash particles on surface, looks good, video: https://www.youtube.com/watch?v=bojdpqi2l_o

### Generating ocean waves into simulation

* Sum of gerstner waves - each frame compute gerstner waves that are appropriate for each sim, apply a force to the ocean surface to pull towards gerstner wave
* Write dynamic state into sim - write dynamic state of an FFT or the sum of gerstner waves into the sim. This could be stamped onto the sim periodically, if the surface repeats with a given period. This is possible - each sim has a particular wave speed. If a strict scheme of only writing a particular wave length into each sim was employed, this would mean the waves would repeat with a particular period. However it's non-obvious how this could be strictly enforced in a practical game-like situation.
* The generation of waves by wind is well understood: https://www.wikiwaves.org/Ocean-Wave_Spectra . This could be modelled. There is a transfer of energy across wavelengths that allows waves that travel faster than wind to be generated, perhaps this can be modelled by transferring energy across sims. This feels like the approach that fits most accurately into the sim paradigm. It would require wind to be defined everywhere. Another problem is that this process occurs over large fetch areas (thousands of wavelengths in size), whereas the sim domains are very bounded, so the process would need to be accelerated (?).

