# Crest Documentation

## Style

Use three spaces for indentation. Three spaces is chosen becauses the `toctree` requires it apparently.
It also works well in other scenarios.

Try and align where possible, for example with lists:

```rst
-  Notice the two spaces after the dash.
   This is a new line.
```

Finally, please use one line per sentence. This helps with version control.

## Build HTML or PDF

1. Install [Python for Windows](https://www.python.org/downloads/) (should have it already for other OS)
    1. Check the *Add Python to Path* option
2. Install LaTex for [Windows](https://mg.readthedocs.io/latexmk.html) or [Other OS](https://www.latex-project.org/get/)
    1. If you already have LaTex, then make sure it is in the PATH and skip this step
3. Open a command prompt in the *docs* directory
4. `pip install -r requirements.txt`

5. `make html` for HTML and `make pdf` for PDFs
    1. For `make pdf`, you will need to install any missing packages that it reports (for MikTex)
    2. PDFs should be in *_build*
6. `make serve` to self host to preview HTML (`make live` for live reload)

When editing static files, generally you will need to do a `make clean html` to rebuild to see the updates.
`make clean` is your friend for both HTML and PDF when you are not seeing changes you think you should be seeing.

RTDs will rebuild automatically on git push.

## Versioned Documentation

[Read The Docs documentation](https://docs.readthedocs.io/en/stable/versions.html).

https://crest.readthedocs.io/en/latest/ points to HEAD on master.
https://crest.readthedocs.io/en/stable/ points to the latest git tag.
https://crest.readthedocs.io/ redirects to stable.
https://crest.readthedocs.io/en/4.9/ points to 4.9 git tag.

After a new version is published (new git tag and asset store upload), in the RTDs admin, the new tag must be made active for the version to be visible.

Then the version must be updated in the following two locations:
[docs/conf.py](https://github.com/wave-harmonic/crest/blob/master/docs/conf.py)
[crest/Assets/Crest/Crest/Scripts/Constants.cs](https://github.com/wave-harmonic/crest/blob/master/crest/Assets/Crest/Crest/Scripts/Constants.cs)

For example, after the git tag for 4.9 is published and the asset store versions are uploaded, we will open those two files to change 4.9 to 4.10.
Even though 4.10 doesn't exist yet, this approach removes the need to change the version before publishing, and then having to change it back to latest, which reduces the burden a little.

## Extensions

- [Furo Theme](https://pradyunsg.me/furo)
- [Sphinx Inline Tabs](https://sphinx-inline-tabs.readthedocs.io/en/latest/)
- [Sphinx Hoverxref](https://sphinx-hoverxref.readthedocs.io/en/latest/)
- [Sphinx Search](https://readthedocs-sphinx-search.readthedocs.io/en/latest/)
- [Sphinx Panels](https://sphinx-panels.readthedocs.io/en/latest/)
- [Sphinx Issues](https://github.com/sloria/sphinx-issues)

Some of these are custom forks. See requirements.txt to see which ones.

## Custom Features

### Get/Set Extension

This is a custom extension to support variables.
Sphinx's substitutions can only be defined once which is too restrictive for our use case.

Data can be set using the *set* directive, and fetched using the *get* role (which has been set to the default role).
The set usage is as follows:

```rst
.. set:: Key The remainder is the value.
```

The get usage is as follows
```rst
Get something with Key using the default role syntax: `Key`
Get something with Key using the explicit role syntax: :get:`Key`
```

A simple example is creating abbreviations:

```rst
.. set:: TAA :abbr:`TAA (Temporal Anti-Aliasing)`

`TAA` is a form of anti-aliasing.
```

The benefit is that these can be overwritten in the RST files.
A good example is in *_pipeline-setup.rst*.
It uses variable names which are meant to be overwritten.
For URP, the file that overwrites these variables is *_urp-vars.rst*.
And using them together:

```rst
.. only:: urp

    .. tab:: `URP`

        .. include:: /includes/_urp-vars.rst
        .. include:: includes/_pipeline-setup.rst
```

This makes reusing content easier.

#### Substitutions

The get/set extenstion also supports text substitutions using braces:

```rst
.. set:: TAALong :abbr:`Temporal Anti-Aliasing`
.. set:: TAA :abbr:`TAA ({TAALong})`

`TAA` is a form of anti-aliasing.
```

#### Links

To support the substitutions in this extension, a custom link role has been made for external links. The syntax is:

```rst
:link:`Text Content <url>`
```

An example:

```rst
This is a link: :link:`Upgrading to {HDRP} <{HDRPDocLink}/Upgrading-To-HDRP.html>`
```

It also supports brace expansion as seen above.


## Showing Content Per Render Pipeline

### `only` Directive

The following will only show content when the provided tag is set:

```rst
.. only:: birp or urp

   BIRP and URP content.

.. only:: hdrp

   HDRP content.
```

Output for HTML:
```
BIRP and URP content.

HDRP content.
```

Output for `make pdf-hdrp`:
```
HDRP content.
```

These tags are always set when rendering HTML or when rendering on the RTDs platform.
In other words, the *birp*, *urp* and *hdrp* tags are only relevant for creating the RP specific PDFs.

Very rarely will using an only directive alone will suffice.
Please use with other approaches like tabs or labels.
Alternatively, making it clear in the copy it is for a specific RP can work.

#### Using With Lists etc

The only directive will break up lists and line blocks. For example, the following will become three seperate lists:

```rst
-  All pipelines

.. only:: hdrp

   -  HDRP only

.. only:: urp

   -  URP only
```

A custom directive is provided which will ensure they all remain together:

```rst
.. bullet_list::

   -  All pipelines

   .. only:: hdrp

      -  HDRP only

   .. only:: urp

      -  URP only
```

See *_hacks.py* for what is available.
To add more, add another `app.add_directive("<class>", Block)` line and replace *\<class>* with a class name from [nodes.py](https://github.com/docutils-mirror/docutils/blob/master/docutils/nodes.py#L1580)

### Tabs

Tabs work great for videos.
Try to use sparingly with text content.

```rst
.. only:: birp

  .. tab:: `BIRP`

     BIRP content.

.. only:: hdrp

  .. tab:: `HDRP`

     HDRP content.

.. only:: urp

  .. tab:: `URP`

     URP content.
```

Be aware that headings do not work in tabs.
There is a workaround below, but they still will not have permalinks or be part of the ToC.

```rst
.. only:: html

    .. raw:: html

        <h4>Heading</h4>

.. only:: latex

    Heading
    """""""
```

### Labels

Labels can be another way to designate content for a specific RP.
The official way to create a label is:

```rst
Content :guilabel:`Label Text`
```

But for the RPs, there is a custom syntax which includes stripping on PDF generation.

```rst
BIRP and URP Content `[BIRP] [URP]`
HDRP Content `[HDRP]`
```

Take note of the first line how both tags are in the one set of backticks.
This is important so that they are both stripped correctly in the PDFs.
If they were separate, the URP PDF would have the BIRP label which would be confusing.

To create a label which will not be stripped in PDFs:

```rst
BIRP and URP Content `[[BIRP]]` `[[URP]]`
HDRP Content `[[HDRP]]`
```

Stripped works by taking the label's contents and matches it against tags.
When two labels are in the same set of backticks, it is treated as *OR*.

To define your own labels, use the set directive:
```rst
.. set:: [URP] :guilabel:`URP`
```
