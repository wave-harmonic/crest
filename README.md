
<img src="https://raw.githubusercontent.com/wave-harmonic/crest/master/logo/crest-oceanrender-logomark128.png" width="107">

# Intro

*Crest* is a technically advanced ocean renderer implemented in Unity3D 2020.3.10 and later.
The version hosted here targets the **built-in render pipeline**, links to the scriptable render pipeline versions (URP/HDRP) on the Asset Store are below.

![Teaser](https://raw.githubusercontent.com/wave-harmonic/crest/master/img/teaser5.png)

**Supporting us:** Asset Store sales partially cover our dev costs, for the rest we are looking for sponsorship. Please see our [sponsorship page](https://github.com/sponsors/wave-harmonic) for more detals.

**Discord for news/updates/discussions:** https://discord.gg/g7GpjDC

**YouTube for tutorials and showcases:** [Crest Ocean System](https://www.youtube.com/channel/UC7_ZKKCXZmH64rRZqe-C0WA)

**Twitter:** [@crest_ocean](https://twitter.com/@crest_ocean)

|<a href="https://assetstore.unity.com/packages/tools/particles-effects/crest-ocean-system-urp-141674" target="_blank"><img src="https://assetstorev1-prd-cdn.unity3d.com/key-image/ae4a1e07-c6f1-4b2e-b1c3-9f2065d43515.jpg" alt="Crest Ocean System URP Asset" width="240" height="180"/></a>|<a href="https://assetstore.unity.com/packages/tools/particles-effects/crest-ocean-system-urp-141674" target="_blank"><img src="https://assetstorev1-prd-cdn.unity3d.com/key-image/68d1442e-488d-4ae9-8ec4-a7e3ff913788.jpg" alt="Crest Ocean System HDRP Asset" width="240" height="180"/></a>|
:-:|:-:|
[Crest Ocean System URP](https://assetstore.unity.com/packages/tools/particles-effects/crest-ocean-system-urp-141674?aid=1011lic2K)|[Crest Ocean System HDRP](https://assetstore.unity.com/packages/tools/particles-effects/crest-ocean-system-hdrp-164158?aid=1011lic2K)

# Showcase Gallery

<a href="https://www.youtube.com/watch?feature=player_embedded&v=aZScNG8-H2U" target="_blank"><img src="https://img.youtube.com/vi/aZScNG8-H2U/0.jpg" alt="Irval the Dragon in Crest Ocean and Lordenfel Ruins" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=_Rq5dfZfQ1k" target="_blank"><img src="https://img.youtube.com/vi/_Rq5dfZfQ1k/0.jpg" alt="Out of Reach: Treasure Royale - Trailer" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=70voKq6cdKQ" target="_blank"><img src="https://img.youtube.com/vi/70voKq6cdKQ/0.jpg" alt="Windbound - Brave the Storm Announce Trailer [Official]" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=ZmKto87To-0" target="_blank"><img src="https://img.youtube.com/vi/ZmKto87To-0/0.jpg" alt="An Adventure to the World of Artificial Intelligenc" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=nsQJ5IJVHVw" target="_blank"><img src="https://img.youtube.com/vi/nsQJ5IJVHVw/0.jpg" alt="Hope Adrift Gameplay & Release Trailer" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=Qfy5P4Zygvs" target="_blank"><img src="https://img.youtube.com/vi/Qfy5P4Zygvs/0.jpg" alt="Morild Navigator" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=LNIQ6RF5lrw" target="_blank"><img src="https://img.youtube.com/vi/LNIQ6RF5lrw/0.jpg" alt="Blue Water Dev Diary - CIWS Expo" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=3i6VpdKw2Q0" target="_blank"><img src="https://img.youtube.com/vi/3i6VpdKw2Q0/0.jpg" alt="Crest Ocean System - Pirate Cove Example Scene" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=m2ZojyD4PZc" target="_blank"><img src="https://img.youtube.com/vi/m2ZojyD4PZc/0.jpg" alt="Critter Cove & Crest Trailer" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=zCeK_Kdxqa0" target="_blank"><img src="https://img.youtube.com/vi/zCeK_Kdxqa0/0.jpg" alt="Of Ships & Scoundrels - Crest Demo" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=HVlJa2J0wSc" target="_blank"><img src="https://img.youtube.com/vi/HVlJa2J0wSc/0.jpg" alt="Rogue Waves" width="240" height="180" /></a>
<a href="https://www.youtube.com/watch?feature=player_embedded&v=SG4OTpVO9_E" target="_blank"><img src="https://img.youtube.com/vi/SG4OTpVO9_E/0.jpg" alt="Ship Simulator: Realistic" width="240" height="180" /></a>

# Documentation

[Full documentation is available online](https://crest.readthedocs.io/en/latest), including *initial setup steps*.

# Prerequisites

* Unity version:
  * The SRP assets on the Asset Store specify the minimum version required.
  * Releases on this GitHub target the built-in render pipeline, and each release specifies which version of Unity it was developed on. Currently Unity 2020.3.10 or later is the minimum version. See further down for 2018.4 support.
* *Crest* example content:
  * The post processing package is used (for aesthetic reasons), if this is not present in your project you will see an unassigned script warning which you can fix by removing the offending script.
* .NET 4.x runtime
* [Shader compilation target](https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html) 4.5 or above
  * *Crest* unfortunately does not support OpenGL or WebGL backends

# Installation

***You can download the latest version from [here](https://github.com/wave-harmonic/crest/archive/master.zip).***

Another way to obtain *Crest* is to fork/clone this repository.

The files that should be copied into an existing project are under *crest/Assets/Crest*.

Note that *crest/Assets/Crest/Crest-Examples* contains example content that is useful for first time users but not required for the core *Crest* functionality. Furthermore, the *crest/Assets/Crest/Development* folder is not needed as it is only for *Crest* development.

**Releases**: Currently we do not prepare release packages. However, we do tag each asset store version, so the zip corresponding to each version can be downloaded by clicking the desired version on the [Releases page](https://github.com/wave-harmonic/crest/releases). Once the zip is downloaded, the steps are as above.


# Notes and Issues

If you encounter an issue, please search the [Issues page](https://github.com/wave-harmonic/crest/issues) to see if there is already a resolution, and if you don't find one then please report it as a new issue.

There are a few issues worth calling out here:

* Sky solutions such as Azure[Sky] requires some code to be added to the ocean shader for the fogging/scattering to work. This is a requirement of these products which typically come with instructions for what needs to be added. See the [wiki](https://github.com/wave-harmonic/crest/wiki) for examples.
* *Crest* does not support OpenGL or WebGL backends
* Unity 2018 support - There is a [legacy branch] for 2018.4 support. A direct download to an archive is available [here](https://github.com/wave-harmonic/crest/archive/legacy/unity-2018.zip). It is not actively developed.

# Donations

With your support we aim to increase our development bandwidth significantly. Please see our sponsor page for sponsor tiers and rewards:

https://github.com/sponsors/wave-harmonic

## Sponsors

### Gold 🥇

[@ipthgil](https://github.com/ipthgil)

### Board Members 🚀

[@pachermann](https://github.com/pachermann) [@Bartolomeus755](https://github.com/Bartolomeus755) [@holdingjason](https://github.com/holdingjason) [@NeistH2o](https://github.com/NeistH2o)
