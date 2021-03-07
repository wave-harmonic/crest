from docutils import nodes
from docutils.parsers.rst import Directive
from sphinx import addnodes

class YouTube(Directive):

    required_arguments = 1

    has_content = True

    def run(self):
        id = self.arguments[0]

        [only_html, only_pdf] = youtube_embed(id)

        # TODO: should we skip figure if no caption?
        figure_node = nodes.figure("")

        figure_node += only_html
        figure_node += only_pdf

        has_caption = len(self.content) > 0

        if has_caption:
            caption = self.content[0]
            inodes, messages = self.state.inline_text(caption, self.lineno)
            caption_node = nodes.caption(caption, '', *inodes)
            figure_node += caption_node

        return [figure_node]
        # return [only_html, only_pdf]

def youtube_embed(id):
    html = f"""
    <div class="video-container">
        <iframe width="100%" height="100%" src="https://www.youtube-nocookie.com/embed/{id}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen>
        </iframe>
    </div>"""

    # Add only HTML node.
    only_html = addnodes.only(expr="html")

    text = "https://www.youtube.com/watch?v={id}"

    # Add YouTube HTML to only node.
    only_html += nodes.raw(text=html, format="html")

    # Add fallback for PDFs.
    only_pdf = addnodes.only(expr="latex")
    # TODO: add optional fallback text
    only_pdf += nodes.paragraph(text=f"https://www.youtube.com/watch?v={id}")

    return [only_html, only_pdf]


def setup(app):
    app.add_directive("youtube", YouTube)
    return {
        'version': '0.1',
        'parallel_read_safe': True,
        'parallel_write_safe': True,
    }
