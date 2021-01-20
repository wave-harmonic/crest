# Q&A

**Can I sample the water height at a position from C#?**
Yes, see *SampleHeightHelper*. *OceanRenderer* uses this helper to get the height of the viewer above the water, and makes this viewer height available via the *ViewerHeightAboveWater* property.

**Can I trigger something when an object is above or under the ocean surface without any scripting knowledge?**
The *OceanSampleHeightEvents* can be used for this purpose. It will invoke a *UnityEvent* when the attached game object is above or below the ocean surface once per state change.

**Is Crest well suited for medium-to-low powered mobile devices?**
Crest is built to be performant by design and has numerous quality/performance levers.
However it is also built to be very flexible and powerful and as such can not compete with a minimal, mobile-centric ocean renderer such as the one in the *BoatAttack* project.
Therefore we target Crest at PC/console platforms.

**Which platforms does Crest support?**
Testing occurs primarily on Windows.
We have users targeting Windows, Mac, Linux, PS4, XboxOne, Switch and iOS/Android.
Performance is a challenge on Switch and mobile platforms - see the previous question. For additional platform notes, see [Platform Support](https://github.com/crest-ocean/crest/wiki/Platform-Support).

**Is Crest well suited for localised bodies of water such as lakes?**
Currently Crest is currrently targeted towards large bodies of water.
The water could be pushed down where it's not wanted which would allow it to achieve rivers and lakes to some extent.

**Does Crest support third party sky assets?**
We have heard of Crest users using TrueSky, AzureSky.
These may require some code to be inserted into the ocean shader - there is a comment referring to this, search *Ocean.shader* for 'Azure'.

**Can Crest work in Edit mode in the Unity Editor, or only in Play mode?**
Currently it only works in Play mode. Some work has been done to make it work in Edit mode but more work/fixes/testing is needed. https://github.com/huwb/crest-oceanrender/issues/208

**Can Crest work with multiplayer?**
Yes the animated waves are deterministic and easily synchronized.
See discussion in https://github.com/huwb/crest-oceanrender/issues/75.
However, the dynamic wave sim is not fully deterministic and can not currently be relied upon networked situations.

**Can the density of the fog in the water be reduced?**
The density of the fog underwater can be controlled using the *Fog Density* parameter on the ocean material. This applies to both above water and underwater.

**Does Crest support orthographic projection?**
It does. Please see the [Orthographic Projection](#orthographic-projection) section.
