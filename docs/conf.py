# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Path setup --------------------------------------------------------------

# If extensions (or modules to document with autodoc) are in another directory,
# add these directories to sys.path here. If the directory is relative to the
# documentation root, use os.path.abspath to make it absolute, like shown here.

import os
import sys

sys.path.insert(0, os.path.abspath('./extensions'))

# -- Project information -----------------------------------------------------

project = 'Crest'
copyright = '2021, Wave Harmonic & Contributors'
author = 'Wave Harmonic & Contributors'

# -- Create API copy DefaultDocumentation  -----------------------------------

# try:
#     files = [file for file in os.listdir("../crest/Temp/bin/Debug") if file.endswith('.md')]
#     if not (os.path.exists('api/') and os.path.isdir('api/')):
#         os.mkdir('api/')
#     if len(files) == 0:
#         raise FileNotFoundError("No .md files were found in ../crest/Temp/bin/Debug/")

#     import shutil
#     for file_name in files:
#         shutil.copyfile(os.path.abspath('../crest/Temp/bin/Debug/' + file_name), os.path.abspath('api/' + file_name))
# except FileNotFoundError as e:
#     print(e)
#     print('Assuming .md files are already present in docs/api/')


# -- General configuration ---------------------------------------------------

# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions = [
    # Global packages
    "sphinx_inline_tabs",
    "sphinx_panels",
    "sphinx_issues",

    "furo",

    "recommonmark",
    "sphinx_markdown_tables",

    # Local packages
    "youtube",
    "variables",
    "tags",
    "links",
    "hacks",

    "hoverxref.extension",
    "sphinx_search.extension",
]

source_suffix = {
    '.rst': 'restructuredtext',
    '.md': 'markdown',
}

# Add any paths that contain templates here, relative to this directory.
templates_path = ['_templates']

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = ['_build', 'Thumbs.db', '.DS_Store', ".env", "extensions"]

# master_doc = "_user/overview"

# https://github.com/readthedocs/readthedocs.org/issues/4603
if os.environ.get('PLATFORM') == "READTHEDOCS":
    tags.add('readthedocs')
    tags.add("birp")
    tags.add("hdrp")
    tags.add("urp")

# -- Features ----------------------------------------------------------------

# Auto numbering of figures
numfig = True

# GitHub repo
issues_github_path = "wave-harmonic/crest"

# https://sphinx-hoverxref.readthedocs.io/en/latest/usage.html#tooltip-on-all-ref-roles
hoverxref_auto_ref = True

# -- Options for HTML output -------------------------------------------------

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
# https://github.com/pradyunsg/furo
# https://pradyunsg.me/furo/
html_theme = 'furo'
html_logo = '../logo/crest-oceanrender-logomark.png'
html_title = "CREST"

# html_logo = 'crest-oceanrender.png'
html_theme_options = {
    # "sidebar_hide_name": True,
    # "announcement": "<em>Important</em> announcement!",
}
# html_favicon = 'crest-oceanrender-logomark.png'

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
html_static_path = ['_static']

# These paths are either relative to html_static_path
# or fully qualified paths (eg. https://...)
html_css_files = [
    'custom.css',
]

html_js_files = [
    'https://cdnjs.cloudflare.com/ajax/libs/medium-zoom/1.0.6/medium-zoom.min.js',
    'custom.js',
]

# -- Options for PDF output --------------------------------------------------

# Customise PDF here. maketitle overrides the cover page.
#
# latex_elements = {
#     "maketitle": "\\input{your_cover.tex}"
# }

# -- Templating --------------------------------------------------------------

# The default role will be used for `` so we do not need to do :get:``.
default_role = "get"

# "replace" substitutions are static/global:
#   |name1| replace:: value
#   |name1|
# Cannot do this:
#   |name2| replace:: |name1|
# Inline content has no nested parsing.

# "set" only supports inline content. It will pass its contents to the parser so roles will be processed. Brace
# substitution is supported and is text only (it will lose any nodes). Use it when you need substitutions in role
# content.
#   .. set:: LongName Example
#   .. set:: ShortName :abbr:`{LongName}`
#   An example of using `ShortName`.

# For links where you want to use substitutions, use the link role:
#   .. set Something Example Page
#   .. set BaseURL https://example.com
#   :link:`Link Text for {Something} <{BaseURL}/example>`
# Pass the URL within the angle brackets. Brace substitution will work and will be text only for URLs and support nodes
# for the link text.
#
# For URLs, it is best to use braces even in "set" as they don't require being enclosed in escaped whitespace:
#   .. set:: Link `LinkBase`\ /something/\ `LinkPart`\ /example.html
# Versus:
#   .. set:: Link {LinkBase}/something/{LinkPart}/example.html

# The following will be included before every page:
rst_prolog = """
.. tags::

.. set:: AssetVersion 4.9
.. set:: RPMinVersion 7.3
.. set:: RPDocLinkBase \https://docs.unity3d.com/Packages/com.unity.render-pipelines.
.. set:: UnityMinVersionShort 2019.4
.. set:: UnityMinVersion {UnityMinVersionShort}.9
.. set:: UnityDocLink https://docs.unity3d.com/{UnityMinVersionShort}/Documentation/Manual
.. set:: AssetStoreLinkBase \https://assetstore.unity.com/packages/tools/particles-effects
.. set:: GitHubLink \https://github.com/wave-harmonic/crest
.. set:: WikiLink \{GitHubLink}/wiki

.. set:: [BIRP] :guilabel:`BIRP`
.. set:: BIRPNameLong Built-in
.. set:: BIRPNameShort BIRP
.. set:: BIRP :abbr:`{BIRPNameShort} ({BIRPNameLong} Render Pipeline)`
.. set:: BIRPMinVersion `RPMinVersion`
.. set:: BIRPDocLink {UnityDocLink}/

.. set:: [URP] :guilabel:`URP`
.. set:: URPNameLong Universal
.. set:: URPNameShort URP
.. set:: URP :abbr:`{URPNameShort} ({URPNameLong} Render Pipeline)`
.. set:: URPMinVersion `RPMinVersion`
.. set:: URPDocLink {RPDocLinkBase}universal@{URPMinVersion}/manual
.. set:: URPAssetLink {AssetStoreLinkBase}/crest-ocean-system-urp-141674

.. set:: [HDRP] :guilabel:`HDRP`
.. set:: HDRPNameLong High Definition
.. set:: HDRPNameShort HDRP
.. set:: HDRP :abbr:`{HDRPNameShort} ({HDRPNameLong} Render Pipeline)`
.. set:: HDRPMinVersion `RPMinVersion`
.. set:: HDRPDocLink {RPDocLinkBase}high-definition@{HDRPMinVersion}/manual
.. set:: HDRPAssetLink {AssetStoreLinkBase}/crest-ocean-system-hdrp-164158

.. set:: Crest *Crest*

.. set:: TAA :abbr:`TAA (Temporal Anti-Aliasing)`
.. set:: SMAA :abbr:`SMAA (Subpixel Morphological Anti-Aliasing)`
"""

# -- Debugging ---------------------------------------------------------------

# For debugging if you want to always have a tag on or off
# tags.add("tag")
# tags.remove("tag")
