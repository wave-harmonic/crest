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

## Setting Up Development

## Creating PDFs

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
