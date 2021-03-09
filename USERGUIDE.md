# Render order

A typical render order for a frame is the following:

* Opaque geometry is rendered, writes to opaque depth buffer (queue <= 2500)
* Sky is rendered, probably at zfar with depth test enabled so it only renders outside the opaque surfaces
* Frame colours and depth are copied out for use later in postprocessing
* Ocean 'curtain' renders, draws underwater effect from bottom of screen up to water line (queue = 2510)
  * It is set to render before ocean in UnderwaterEffect.cs
  * Sky is at zfar and will be fully fogged/obscured by the water volume
* Ocean renders early in the transparent queue (queue = 2510)
  * It samples the postprocessing colours and depths, to do refraction
  * It reads and writes from the frame depth buffer, to ensure waves are sorted correctly
  * It stomps over the underwater curtain to make a correct final result
  * It stopms over sky - sky is at zfar and will be fully fogged/obscured by the water volume
* Particles and alpha render. If they have depth test enabled, they will clip against the surface
* Postprocessing runs with the postprocessing depth and colours

# Collision Shape for Physics

## Technical Notes

We use a technique called Fixed Point Iteration to calculate the water height.
We gave a talk at GDC about this technique which may be useful to learn more: [link](http://www.huwbowles.com/fpi-gdc-2016/).
