import re
from docutils import nodes
from sphinx import addnodes
from sphinx.util.docutils import SphinxDirective, SphinxRole


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


class VariableRole(SphinxRole):
    def run(self):
        try:
            tags = self.env.app.tags
            keys = self.text.split()

            # NOTE: Only first key is used. The remainder are thrown away, but could also be used.
            if not keys[0].startswith("["):
                return dictionary[keys[0]], []

            # Bypass label stripping
            if keys[0].startswith("[[") and keys[0].endswith("]]"):
                return dictionary[keys[0][1:-1]], []

            # Implicit stripping of labels ([label]).
            node_list = []
            is_first = False
            for key in keys:
                if not is_first:
                    is_first = True
                else:
                    node_list += nodes.inline(text=" ")
                if tags.has("stripping") and tags.eval_condition(key[1:-1].lower()):
                    return [], []
                node_list += dictionary[key]

            return node_list, []
        except Exception as error:
            message = self.inliner.reporter.error(error, line=self.lineno)
            node = self.inliner.problematic(self.rawtext, self.rawtext, message)
            return [node], [message]


def setup(app):
    app.add_directive("set", VariableSet)
    app.add_role("get", VariableRole())
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
    # Make sure we only do substitutions when we are within a brace. We do NOT support nested braces.
    is_within_brace = False
    # Use () to keep the delimiters.
    for part in re.split(r"([{}])", text):
        if part == "{":
            is_within_brace = True
            continue
        elif part == "}":
            is_within_brace = False
            continue
        if is_within_brace and part in dictionary:
            node_list += dictionary[part]
        else:
            # This part will be just text so created a node.
            node_list += nodes.inline(part, part)
    return node_list
