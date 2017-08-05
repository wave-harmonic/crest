
# crest

![Teaser](https://raw.githubusercontent.com/huwb/crest-oceanrender/master/img/teaser.png)  

Contacts: **Huw Bowles** (@hdb1 , huw dot bowles at gmail dot com), **Daniel Zimmermann** (@DanyGZimmermann, infkdude at gmail dot com), **Chino Noris** (@chino_noris , chino dot noris at epost dot ch), **Beibei Wang** (bebei dot wang at gmail dot com)


## Introduction

*Crest* is a Unity3D implementation of a number of novel ocean rendering techniques published at SIGGRAPH 2017 in the *Advances in Real-Time Rendering* course (course page [link](http://advances.realtimerendering.com/s2017/index.html)).

It demonstrates a number of techniques described in this course:

* CDClipmaps - a new meshing approach that combines the simplicity of Clipmaps with the Continuous Detail of CDLOD.
* GPU-based shape system - each LOD has an associated displacement texture which is rendered by the *WaveDataCam* game object by running generic Shape Shaders.
* Normal map scaling - a technique to improve the range of view distances for which a set of normal maps will work.
* Foam - two foam layers that are computed on the fly from the displacement textures.

## How it Works

On startup, the *OceanBuilder* script creates the ocean geometry as a LODs, each composed of geometry tiles and a shape camera to render the displacement texture for that LOD. It has the following parameters that are passed to it on startup from the OceanRenderer script:

* Base Vert density - the base vert/shape texel density of an ocean patch. If you set the scale of a LOD to 1, this density would be the world space verts/m. More means more verts/shape, at the cost of more processing.
* Lod Count - the number of levels of detail / scales of ocean geometry to generate. More means more dynamic range of usable shape/mesh at the cost of more processing.
* Max Wave Height - this is just so that the ocean tiles bounding box height can be set, to ensure culling eliminates tiles correctly.
* Max Scale - the ocean is scaled horizontally with viewer height, to keep the meshing suitable for elevated viewpoints. This sets the maximum the ocean will be scaled if set to a positive value.
* Min Scale - this clamps the scale from below, to prevent the ocean scaling down to 0 when the camera approaches the sea level. This should be set to a low value gives lots of detail, but will limit the horizontal extents of the ocean as the detail scales have a limited dynamic range (set by the previous Lod Count parameter).

At run-time, the ocean is placed in front of the viewer by the *SphereOffset* script, using the heuristic described in the course. The *ShapeCameras* will render any shape geometry that is assigned to the *WaveData* layer, to generate the displacement textures. Such geometry is grouped under the ShapeRender game object. A horizontal scale is compute for the ocean based on the viewer height, as well as a *_viewerAltitudeLevelAlpha* that captures where the camera is between the current scale and the next scale (x2), and allows a smooth transition between scales to be achieved using the two mechanisms described in the course.

The ocean geometry itself as the Ocean shader attached. The vertex shader snaps the verts to grid positions to make them stable. It then computes a *lodAlpha* which starts at 0 for the inside of the LOD and becomes 1 at the outer edge. It is computed from taxicab distance as noted in the course. This value is used to drive the vertex layout transition, to enable a seemless match between the two. The vertex shader then samples the current LOD shape texture and the next shape texture and uses *lodAlpha* to interpolate them for a smooth transition across displacement textures. A foam value is also computed using the determinant of the Jacobian of the displacement texture. Finally, it passes the LOD geometry scale and *lodAlpha* to the pixel shader.

The ocean pixel shader samples normal maps at 2 different scales, both proportional to the current and next LOD scales, and then interpolates the result using *lodAlpha* for a smooth transition. Two layers of foam are added based on different thresholds of the foam value, with black point fading used to blend them.

## Bugs and Improvement Directions

* Each Gerstner wave is computed and blended into the displacement texture individually. This makes them very easy to work and convenient, but baking them down to a single pass would be an interesting optimisation direction.
* While ocean geometry tiles are frustum culled, currently displacements are rendered for the entire world. Rendering a top down projection of the camera frustum geometry to seed the shape computation might be an interesting direction.
* Ocean tiles are updated and drawn as separate draw calls. This is convenient for research and supports frustum culling easily, but it might make sense to instance these in a production scenario.
