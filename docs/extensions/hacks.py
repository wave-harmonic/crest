from docutils import nodes
from sphinx.util.docutils import SphinxDirective

class LineBlock(SphinxDirective):
    has_content = True
    def run(self):
        container = nodes.container()
        self.state.nested_parse(self.content, self.content_offset, container)
        line_block_node = nodes.line_block()
        for node in container:
            node_copy = node
            if node_copy.asdom().tagName == "comment":
                continue
            if node_copy.asdom().tagName == "only":
                if not self.env.app.tags.eval_condition(node_copy.attributes["expr"]):
                    continue
                for inner_node in node_copy:
                    if inner_node.asdom().tagName == "line_block":
                        node_copy = inner_node
                        break
            line_block_node += node_copy[:]

        return [line_block_node]

def setup(app):
    app.add_directive("line-block", LineBlock)
