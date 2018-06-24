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
* Boat interaction: https://www.youtube.com/watch?v=YK_Za2MY2a0

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

### Notes on generating ocean waves into simulation

* Sum of gerstner waves - each frame compute gerstner waves that are appropriate for each sim, apply a force to the ocean surface to pull towards gerstner wave
* Write dynamic state into sim - write dynamic state of an FFT or the sum of gerstner waves into the sim. This could be stamped onto the sim periodically, if the surface repeats with a given period. This is possible - each sim has a particular wave speed. If a strict scheme of only writing a particular wave length into each sim was employed, this would mean the waves would repeat with a particular period. However it's non-obvious how this could be strictly enforced in a practical game-like situation.
* The generation of waves by wind is well understood: https://www.wikiwaves.org/Ocean-Wave_Spectra . This could be modelled. There is a transfer of energy across wavelengths that allows waves that travel faster than wind to be generated, perhaps this can be modelled by transferring energy across sims. This feels like the approach that fits most accurately into the sim paradigm. It would require wind to be defined everywhere. Another problem is that this process occurs over large fetch areas (thousands of wavelengths in size), whereas the sim domains are very bounded, so the process would need to be accelerated (?).
