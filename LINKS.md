# Links

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
* Hydrax - open source ocean plug-in for OGRE - https://github.com/imperative/CommunityHydrax
* Survey ocean simulation techniques - 2011 - https://arxiv.org/pdf/1109.6494.pdf
* Weta - Synthesizing waves from animated heightfields - 2012 - deals with optimizing a physical ocean surface to match an artist authored shape, numerical issues with tanh(), eliminating overlaps, computing a 3D velocity field: http://cs.au.dk/~bang/publications/NielsenSoderstromBridsonTOG2012.pdf

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

### Wave Simulation

* Mode splitting - surface + volume sim combined: http://www.hilkocords.de/publications/mode_splitting.pdf
* Boat interaction: https://www.youtube.com/watch?v=YK_Za2MY2a0 , paper: http://www.hilkocords.de/publications/open_water.pdf
* Setting up boat interactioin in maya: https://www.youtube.com/watch?v=O-8ow82gQw8 . Touches on issues related to combining heightfield with displacement texture, and the wake lagging behind the object.
* Water Surface Wavelets - Jeschke et al. SIGGRAPH 2018 - http://visualcomputing.ist.ac.at/publications/2018/WSW/ - Interesting rederivation of water motion into a more computationally friendly form. The LOD system in Crest is very competitive with this technique.

### 3D Simulation

* Bubble sim. Hill spherical vortex - irrotational flow around sphere. http://matthias-mueller-fischer.ch/publications/bubbles.pdf

### Wave particles

* Original wave particles work: http://www.cemyuksel.com/research/waveparticles/
* Water Surface Wavelets: http://visualcomputing.ist.ac.at/publications/2018/WSW/

### Boundary conditions

* http://hplgit.github.io/wavebc/doc/pub/._wavebc_cyborg002.html
* https://pdfs.semanticscholar.org/c902/c4f2c61734cbf4ec7ee8b792ccb01644943d.pdf

### Water depth

* Wave speeds for different water depths (after eqn 4.9): https://tutcris.tut.fi/portal/files/4312220/kellomaki_1354.pdf . It also says the SWE are equivlanet to the WE although i didnt understand how/why. also discusses RB coupling.
* SWE with changing ocean depths: https://arxiv.org/pdf/1202.6542.pdf

### Breaking waves

* Real-time for shallow water simulations: http://matthias-mueller-fischer.ch/publications/breakingWaves.pdf , https://www.youtube.com/watch?v=Gk0AeRufsws

### Experiments

* 1D wave equation in shadertoy: https://www.shadertoy.com/view/MtlfzM
* Propagate gerstner waves with wave equation - click to simulate wind: https://www.shadertoy.com/view/XtlBDr

### Particle sim

* Mixes SPH and WE, uses SPH to get low frequency 3D flow: http://citeseerx.ist.psu.edu/viewdoc/download;jsessionid=8A10D0187910134E8C8330AF1C57B146?doi=10.1.1.127.1749&rep=rep1&type=pdf
* Mueller - deposits splash particles on surface, video: https://www.youtube.com/watch?v=bojdpqi2l_o

### Meshing

* Real-time Optimally-Adapting Meshes - http://www.cognigraph.com/ROAM_homepage/

### Ref - wave videos

* Big waves (Top Fives) - https://www.youtube.com/watch?v=lwuKvmNQrRM . The wave at 8:40 is a monster! Nice foam/bubble ref from 9:05.
* Boat wakes
..* https://www.youtube.com/watch?v=BvB-iYHjqw4

### Ref - photos

* Shallow water / ocean colouring: https://static1.squarespace.com/static/52cc03b7e4b0f8365c6e11c7/55e807e8e4b018b9867d9e0b/55e80e41e4b0f4565e4d9236/1441271361373/MZA0146-%C2%AETPESCHAK.jpg?format=1000w
* Shallow water: https://imgcs.artprintimages.com/img/print/print/louise-murray-aerial-photography-of-coral-reef-formations-of-the-great-barrier-reef_a-l-13832306-4990827.jpg?w=550&h=550
* Crazy shallow water colour: https://www.researchgate.net/profile/Ruy_Kikuchi/publication/306096339/figure/fig2/AS:401490431758337@1472734186304/Aerial-photograph-of-Porto-de-Galinhas-coral-reef-on-the-coast-of-the-State-of.png

### Ref - underwater photos

* Underwater surfers: https://images.fineartamerica.com/images/artworkimages/mediumlarge/1/reef-surfers-sean-davey.jpg
* Underwater waves: https://static1.squarespace.com/static/52cc03b7e4b0f8365c6e11c7/t/56964bbed8af10829fdf06b7/1452690366672/Thomas+Peschak+13.jpg?format=2500w
* Water transition: https://static1.squarespace.com/static/52cc03b7e4b0f8365c6e11c7/55eeb228e4b0ca03718d0110/55eeb284e4b05b152fccb7ac/1441706629593/_DSC1344-%C2%AEThomas+P.+Peschak.jpg?format=1000w
* Water transition: https://static1.squarespace.com/static/52cc03b7e4b0f8365c6e11c7/55ed83e5e4b0b8a6d109b00a/55ed8562e4b067c4314e3c10/1441629539099/010Arabian+Seas-%C2%AEThomas+P.+Peschak.jpg?format=1000w
* Great barrier reef, water transition: http://katiepurlingphotography.com/galleries/under-the-sea-on-the-great-barrier-reef/

### Other

* Ocean transparency measurements: http://www.dtic.mil/dtic/tr/fulltext/u2/718333.pdf
