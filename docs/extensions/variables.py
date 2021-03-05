import re
from docutils import nodes
from docutils.parsers.rst import Directive
from sphinx import addnodes
from sphinx.util.docutils import SphinxDirective


dictionary = {}


class VariableSet(SphinxDirective):

    has_content = True

    def run(self):
        # First token is the key. We only support one line.
        [key, value] = self.content[0].split(" ", 1)
        node_list = []
        # Replace content with content without key.
        self.content[0] = brace_substitution_text(value)
        # Parse will produce nodes and add them to the node list.
        self.state.nested_parse(self.content, 0, node_list)
        # We only want the contents of the list to be store. Since this is inline, there is only one line.
        dictionary[key] = node_list[0]
        # No output to return.
        return []


def link_role(name, rawtext, text, lineno, inliner, options={}, content=[]):
    # Split the content so we have text and parameters.
    # :link:`Text Content <parameter>`
    index = text.index(" <")
    link_text = text[:index].strip()
    parameters = text[index:].strip()
    # Text substitution only for URLs using braces.
    parameters = brace_substitution_text(parameters)
    # Text substitution outputting as nodes for the text content. Also uses braces.
    node_list = brace_substitution_node(link_text)
    # Create the link node and add the content. Remove the angle brackets.
    node = nodes.reference(refuri=parameters[1:-1])
    node += node_list
    return [node], []


def get_role(name, rawtext, text, lineno, inliner, options={}, content=[]):
    return dictionary[text.split()[0]], []


def setup(app):
    app.add_directive("set", VariableSet)
    app.add_role('get', get_role)
    app.add_role('link', link_role)


##############################################################################
# Utility Functions
##############################################################################

def brace_substitution_text(text):
    for marker in re.findall(r"\{[^{}]+\}", text):
        # We remove the braces to get the key.
        text = text.replace(marker, dictionary[marker[1:-1]][0].astext())
    return text


def brace_substitution_node(text):
    # Text substitution outputting as nodes for the text content. Also uses braces.
    node_list = []
    for part in re.split(r"[{}]", text):
        if part in dictionary:
            node_list += dictionary[part]
        else:
            # This part will be just text so created a node.
            node_list += nodes.inline(text=part)
    return node_list