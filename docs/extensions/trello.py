from docutils import nodes
from sphinx.util.docutils import SphinxDirective
from sphinx import addnodes

import inspect

class Trello(SphinxDirective):

    required_arguments = 1
    has_content = True

    def run(self):
        link = self.arguments[0]
        embed_type = "Card" if "/c/" in link else "Board"

        container = nodes.container()

        html = f"""
        <blockquote class="trello-{embed_type.lower()}-compact">
            <a href="{link}">Trello {embed_type}</a>
        </blockquote>"""

        html_node = nodes.raw(text=html, format="html")
        only_pdf = addnodes.only(expr="latex")
        # We need to provide a node for nested parsing. And another node for populating the parsed node.
        only_pdf_paragraph = nodes.inline()
        self.state.nested_parse(nodes.inline(text=f"`Trello {embed_type} <{link}>`_"), 0, only_pdf_paragraph)
        only_pdf += only_pdf_paragraph
        container += html_node
        container += only_pdf
        return container.children

def setup(app):
    app.add_directive("trello", Trello)
    return {
        'parallel_read_safe': True,
        'parallel_write_safe': True,
    }
