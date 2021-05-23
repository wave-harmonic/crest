from docutils import nodes
from sphinx.util.docutils import SphinxDirective

# These directives get around the issue where the "only" directive breaks blocks like lists or line blocks.
#
# Usage:
#
# .. line_block::
#
#    | Line 1
#
#    .. only:: tag
#
#       | Line 2
#       | Line 3
#
#    | Line 4

class Block(SphinxDirective):
    has_content = True
    def run(self):
        block_type = self.name
        container = nodes.container()
        self.state.nested_parse(self.content, self.content_offset, container)
        # Calling a method by string.
        block_node = getattr(nodes, block_type)()
        for node in container:
            node_copy = node
            if node_copy.asdom().tagName == "comment":
                continue
            if node_copy.asdom().tagName == "only":
                if not self.env.app.tags.eval_condition(node_copy.attributes["expr"]):
                    continue
                node_copy = []
                # We need to flatten the list by one level since indentation will make a sublist.
                # This sort of thing probably should be recursive.
                for inner_node in node:
                    if inner_node.asdom().tagName == "comment":
                        continue
                    if inner_node.asdom().tagName == block_type:
                        node_copy +=  inner_node
                        continue
                    # Assign block level nodes to whichever block type we found. This doesn't appear to be robust but I
                    # think it works as it should.
                    node_copy[-1] += inner_node
            # Copy elements over.
            block_node += node_copy[:]

        return [block_node]

def setup(app):
    app.add_directive("line_block", Block)
    app.add_directive("bullet_list", Block)
