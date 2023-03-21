Crest Ocean System
==================

.. only:: html

   .. image:: https://readthedocs.org/projects/crest/badge/?version=latest
      :target: https://crest.readthedocs.io/en/latest/?badge=latest
      :alt: Documentation Status

.. NOTE:
.. Subsequent captions are broken in PDFs: https://github.com/sphinx-doc/sphinx/issues/4977.

.. NOTE:
.. :numbered: has bugs with PDFs: https://github.com/sphinx-doc/sphinx/issues/4318.

.. NOTE:
.. only directive does not work with tocree directive for HTML.

.. .. only:: latex
..
..    .. toctree::
..       :hidden:
..       :caption: User Guide
..
..       about/introduction

.. NOTE:
.. ":numbered: 1" means numbering is only one deep. Needed for the version history.


.. toctree::
   :numbered: 1
   :maxdepth: 2
   :caption: About

   about/introduction
   about/known-issues
   about/roadmap
   about/history

.. toctree::
   :numbered:
   :maxdepth: 3
   :caption: User Guide

   user/initial-setup
   user/quick-start-guide
   user/water-appearance
   user/water-inputs
   user/clipping
   user/waves
   user/water-bodies
   user/shallows-and-shorelines
   user/tides-and-currents
   user/underwater
   user/collision-shape-and-buoyancy-physics
   user/time-providers
   user/open-worlds
   user/watercraft
   user/performance-guide
   user/system-notes
   user/rendering-notes
   user/faq

.. NOTE:
.. Tried to have only the title show in the ToC, but it looks like Sphinx is ignoring toctree options.

.. .. only:: latex
..
..    .. toctree::
..
..       meta/history

.. TODO:
..   user/support

.. only:: html

   .. toctree::
      :maxdepth: 1
      :caption: Developer Guide

      dev/contributing
