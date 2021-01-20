# Other features

## Underwater

*Crest* supports seamless transitions above/below water. This is demonstrated in the *main.unity* scene in the example content. The ocean in this scene uses the material *Ocean-Underwater.mat* which enables rendering the underside of the surface, and has the prefab *UnderWaterCurtainGeom* parented to the camera which renders the underwater effect. It also has the prefab *UnderWaterMeniscus* parented which renders a subtle line at the intersection between the camera lens and the water to visually help the transition.

The density of the fog underwater can be controlled using the *Fog Density* parameter on the ocean material. This applies to both above water and underwater.

Out-scattering is provided as an example script which reduces environmental lighting with depth underwater. See *UnderwaterEnvironmentalLighting*.

Checklist for using underwater:

* Configure the ocean material for underwater rendering - in the **Underwater** section of the material params, ensure *Enabled* is turned on and *Cull Mode* is set to *Off* so that the underside of the ocean surface renders. See *Ocean-Underwater.mat* for an example.
* Place *UnderWaterCurtainGeom* and *UnderWaterMeniscus* prefabs under the camera (with cleared transform).
* Use opaque or alpha test materials for underwater surfaces. Transparents materials will not render correctly underwater.
* For performance reasons, the underwater effect is disabled if the viewpoint is not underwater. If there are multiple cameras, the *Viewpoint* property of the *OceanRenderer* component must be set to the current active camera.

## Floating origin

*Crest* has support for 'floating origin' functionality, based on code from the Unity community wiki. See the original wiki page for an overview and original code: [link](http://wiki.unity3d.com/index.php/Floating_Origin).

It is tricky to get pop free results for world space texturing. To make it work the following is required:

* Set the floating origin threshold to a power of 2 value such as 4096.
* Set the size/scale of any world space textures to be a smaller power of 2. This way the texture tiles an integral number of times across the threshold, and when the origin moves no change in appearance is noticeable. This includes the following textures:
  * Normals - set the Normal Mapping Scale on the ocean material
  * Foam texture - set the Foam Scale on the ocean material
  * Caustics - also should be a power of 2 scale, if caustics are visible when origin shifts happen

By default the *FloatingOrigin* script will call *FindObjectsOfType()* for a few different component types, which is a notoriously expensive operation. It is possible to provide custom lists of components to the 'override' fields, either by hand or programmatically, to avoid searching the entire scene(s) for the components. Managing these lists at run-time is left to the user.

## Buoyancy / Floating Physics

*SimpleFloatingObject* is a simple buoyancy script that attempts to match the object position and rotation with the surface height and normal. This can work well enough for small water craft that don't need perfect floating behaviour, or floating objects such as buoys, barrels, etc.

*BoatProbes* is a more advanced implementation that computes buoyancy forces at a number of *ForcePoints* and uses these to apply force and torque to the object. This gives more accurate results at the cost of more queries.

*BoatAlignNormal* is a rudimentary boat physics emulator that attaches an engine and rudder to *SimpleFloatingObject*. It's not recommended for cases where high animation quality is required.

### Adding boats

Setting up a boat with physics can be a dark art. The authors recommend duplicating and modifying one of the existing boat prefabs, and proceeding slowly and carefully as follows:

* Pick an existing boat to replace. Only use *BoatAlignNormal* if good floating behaviour is not important, as mentioned above. The best choice is usually boat probes.
* Duplicate the prefab of the one you want to replace, such as *crest\Assets\Crest\Crest-Examples\BoatDev\Data\BoatProbes.prefab*
* Remove the render meshes from the prefab, and add the render mesh for your boat. We recommend lining up the meshes roughly.
* Switch out the collision shape as desired. Some people report issues if the are multiple overlapping physics collision primitives (or multiple rigidbodies which should never be the case). We recommend keeping things as simple as possible and using only one collider if possible.
* We recommend placing the render mesh so its approximate center of mass matches the center of the collider and is at the center of the boat transform. Put differently, we usually try to eliminate complex hierarchies or having nested non-zero'd transforms whenever possible within the boat hierarchy, at least on or above physical parts.
* If you have followed these steps you will have a new boat visual mesh and collider, with the old rigidbody and boat script. You can then modify the physics settings to move the behaviour towards how you want it to be.
* The mass and drag settings on the boat scripts and rigdibody help to give a feeling of weight.
* Set the boat dimension:
  * BoatProbes: Set the *Min Spatial Length* param to the width of the boat.
  * BoatAlignNormal: Set the boat Boat Width and Boat Length to the width and length of the boat.
  * If, even after experimenting with the mass and drag, the boat is responding too much to small waves, increase these parameters (try doubling or quadrupling at first and then compensate).
* There are power settings for engine turning which also help to give a feeling of weight
* The dynamic wave interaction is driven by the object in the boat hierarchy called *WaterObjectInteractionSphere*. It can be scaled to match the dimensions of the boat. The *Weight* param controls the strength of the interaction.

The above steps should maintain a working boat throughout - we recommend testing after each step to catch issues early.
