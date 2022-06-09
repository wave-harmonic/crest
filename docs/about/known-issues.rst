
.. _known-issues:

Known Issues
============

We keep track of issues on GitHub for all pipelines.
Please see the following links:

.. bullet_list::

    -  :link:`Issues on GitHub <{GitHubLink}/issues>`.

    .. only:: birp

        -  :link:`{BIRP} specific issues on GitHub <{GitHubLink}/issues?q=is%3Aopen+label%3Abug+label%3ABIRP>`.

    .. only:: hdrp

        -  :link:`{HDRP} specific issues on GitHub <{GitHubLink}/issues?q=is%3Aopen+label%3Abug+label%3AHDRP>`.

    .. only:: urp

        -  :link:`{URP} specific issues on GitHub <{GitHubLink}/issues?q=is%3Aopen+label%3Abug+label%3AURP>`.

If you discover a bug, please :link:`open a bug report <{GitHubLink}/issues/new?template=bug_report.md>` or mention it on the :link:`bugs channel on our Discord <https://discord.com/channels/559866092546424832/559872374682681345>`.


Unity Bugs
----------

There are some Unity issues that affect `Crest`.
Some of these may even be blocking new features from being developed.
If you could vote on these issues, that would be greatly appreciated:

-  :link:`Gizmos render over opaque objects with Post-Processing stack. <{UnityIssueLink}/1124862>` `[[BIRP]]`


Unity Features
--------------

There are upcoming features being developed by Unity which will greatly help `Crest`.
If you could vote on these features, that would be greatly appreciated:

-  :link:`Post Processing Custom Effects <https://portal.productboard.com/8ufdwj59ehtmsvxenjumxo82/c/37-post-processing-custom-effects>` `[[URP]]`


Prefab Mode Not Supported
-------------------------

Crest does not support running in prefab mode which means dirty state in prefab mode will not be reflected in the scene view.
Save the prefab to see the changes.
