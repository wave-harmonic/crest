Q&A
===

Why does the ocean not update smoothly in edit mode?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The update rate is intentionally throttled by Unity to save power when
in edit mode. To enable real-time update, enable *Animated Materials* in
the Scene View toggles:

.. .. image:: AnimatedMaterialsOption
..    :alt: image

Is *Crest* well suited for medium-to-low powered mobile devices?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

*Crest* is built to be performant by design and has numerous
quality/performance levers. However it is also built to be very flexible
and powerful and as such can not compete with a minimal, mobile-centric
ocean renderer such as the one in the *BoatAttack* project. Therefore we
target *Crest* at PC/console platforms.

Which platforms does *Crest* support?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Testing occurs primarily on Windows. We have users targeting Windows,
Mac, Linux, PS4, XboxOne and Switch. Performance is a challenge on
Switch (and Quest) - see the previous question.

Is Crest well suited for localised bodies of water such as lakes?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Currently Crest is targeted towards large bodies of water. The
water could be pushed down where it's not wanted which would allow it to
achieve rivers and lakes to some extent.

Can *Crest* work with multiplayer?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Yes the animated waves are deterministic and easily synchronized. See
discussion in :issue:`75`. However,
the dynamic wave sim is not synchronized over the network and can not
currently be relied upon in networked situations. Additionally, *Crest*
does not currently support being run as a CPU-only headless instance. We
hope to improve this in the future.

Errors are present in the log that report *Kernel 'xxx.yyy' not found*
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Unity sometimes gets confused and needs assets reimported. This can be
done by clicking the *Crest* root folder in the Project window and
clicking *Reimport*. Alternatively the *Library* folder can be removed
from the project root which will force all assets to reimport.

Can I push the ocean below the terrain?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Yes, this is demonstrated in this video:
https://www.youtube.com/watch?v=sQIakAjSq4Y.

Does *Crest* support multiple viewpoints?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Currently only a single ocean instance can be created, and only one
viewpoint is supported at a time. We hope to support multiple
simultaneous views in the future.

Does Crest support orthographic projection?
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

It does. Please see section :ref:`orthographic_projection`.
