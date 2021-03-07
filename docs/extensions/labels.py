import re
from docutils import nodes
from docutils.parsers.rst import Directive
from sphinx import addnodes
from sphinx.util.docutils import SphinxDirective

def label_role(name, rawtext, text, lineno, inliner, options={}, content=[]):

    node_list = []

    if inliner.env.app.tags.has("html"):
        node = nodes.inline

    return node_list


def setup(app):
    app.add_role('label', label_role)
