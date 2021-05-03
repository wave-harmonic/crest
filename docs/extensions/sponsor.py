from docutils import nodes
from sphinx.util.docutils import SphinxDirective
from sphinx import addnodes

class Sponsor(SphinxDirective):

    has_content = True

    def run(self):
        sponsor_link = self.env.config.sponsor_link
        organization = self.env.config.organization

        admonition_node = nodes.admonition(classes=["admonition-sponsor"])
        admonition_node += nodes.title(text="Sponsor")

        # Parse admonition body contents.
        # Needs to use a container here instead of list otherwise exception.
        node_list = nodes.container()
        self.state.nested_parse(self.content, self.content_offset, node_list)
        admonition_node += node_list.children

        # Add sponsor button for HTML and fallback for PDF.
        html = f"<iframe src=\"{sponsor_link}/button\" title=\"Sponsor {organization}\" height=\"35\" width=\"116\" style=\"border: 0; display: block;\"></iframe>"
        html_node = nodes.raw(text=html, format="html")
        only_pdf = addnodes.only(expr="latex")
        only_pdf_paragraph = nodes.inline()
        self.state.nested_parse(nodes.inline(text=f":link:`Sponsor Us <{sponsor_link}?o=esb>`"), 0, only_pdf_paragraph)
        only_pdf += only_pdf_paragraph

        admonition_node += html_node
        admonition_node += only_pdf

        return [admonition_node]

def setup(app):
    app.add_config_value("organization", "", "env")
    app.add_config_value("sponsor_link", "", "env")
    app.add_directive("sponsor", Sponsor)
    return {
        'parallel_read_safe': True,
        'parallel_write_safe': True,
    }
