
# crest

Wave sim development branch!


## Overview 

We introduce an elegant, unified data structure for an anti-aliased, level of detail water simulation and rendering system.

Level of detail is a central consideration in our system. We use the infinity norm with square isolines to drive the detail - this fits well to the square textures we use to simulate the water shape. We do not require spatial data structures such as quad trees to select detail which simplifies our implementation enormously.

The water shape is stored in multiple overlapping nested textures that are centered around the viewer. Each texture represents a different scale. The smallest scale gives high detail close to the viewer. Water simulation is done in the textures for each scale individually. The wave propagation speed is dependent on scale to support dispersion. No coupling between the simulations are required. Before rendering the results from the sims accumulated from largest sim to smallest, to add the large scale waves to the more detailed lods.

The ocean surface is rendered by submitting geometry tiles which are placed around the viewer on startup. The tiles are generated on the CPU on startup. The shape textures are sampled in the vertex shader to compute the final shape. The layout and resolution of the tiles match 1:1 with the shape texture resolution, so that data resolution and sampling rate are well matched. Building the mesh out of tiles afford standard frustum culling.

When the viewer moves, shape rendering snaps to texel positions and geometry is smoothly transitioned out towards the boundaries. The LODs also slide up and down scales when the viewer changes altitude. A height interpolation parameter deals with fading shape in/out at the lowest/highest levels of detail. This eliminates visible pops/discontinuities.

Although normal maps are not stored in shape textures or simulated, they are treated as first class shape, and are scaled with the LODs so that they always give the appearance of waves that are higher detailed than the most detailed shape LOD. The required scaling and blending calculations hang off the ocean geometry scales and use the same interpolation parameters.

The above gives a complete ocean rendering system. There are just a few core parameters which are intuitive to tweak, such as a single overall resolution slider and the number of LOD levels to generate.


## Overview of how these things run:

1. For each shape texture, from largest to smallest (camera Depth ensures this), any geometry marked as WaveData layer renders into the shape cameras (ShapeCam0, 1, ..):
  * Wave simulation shader runs first (via ShapeSimWaveEqn.shader on WaveSim quad). The render queue (2000) ensures it renders before subsequent shape stuff in the next step. After running the wave PDE this writes out: (x,y,z,w) = (current water height for this LOD, previous water height for this LOD, LOD foam value computed based on downwards acceleration of water surface, 0.).
  * Any interaction forces/shapes render afterwards (such as ShapeObstacle.shader on Obstacle1 quad). This can either add values to the surface, add foam, or both.
2. After the lod 0 shape camera ShapeCam0 has rendered, OnShapeCamerasFinishedRendering() is called. This does a combine pass where the results of the different simulations are accumulated down the LOD chain. E.g. lod 4 is copied into lod 3. The water height is added and written into the unused W channel. The foam value is simply added in place. The x,y channels are not touched, as these will be read in the simulate shader in the next frame (step 1).
3. When each ocean chunk will be rendered, OceanChunkRenderer::OnWillRenderObject() is called. This grabs the target texture off the appropriate shape camera and assigns it to the shader for sampling in the ocean vertex shader.


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
