
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

If you discover a bug, please :link:`open a bug report <{GitHubLink}/issues/new/choose>` or mention it on the :link:`bugs channel on our Discord <https://discord.com/channels/559866092546424832/1025347180468387860>`.


Unity Bugs
----------

There are some Unity issues that affect `Crest`.
Some of these may even be blocking new features from being developed.
If you could vote on these issues, that would be greatly appreciated:

-  :link:`Gizmos render over opaque objects with Post-Processing stack. <{UnityIssueLink}/1124862>` `[[BIRP]]`


Prefab Mode Not Supported
-------------------------

Crest does not support running in prefab mode which means dirty state in prefab mode will not be reflected in the scene view.
Save the prefab to see the changes.
