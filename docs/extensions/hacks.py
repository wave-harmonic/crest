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
                for inner_node in node_copy:
                    if inner_node.asdom().tagName == block_type:
                        node_copy = inner_node
                        break
            # Copy elements over.
            block_node += node_copy[:]

        return [block_node]

def setup(app):
    app.add_directive("line_block", Block)
    app.add_directive("bullet_list", Block)
