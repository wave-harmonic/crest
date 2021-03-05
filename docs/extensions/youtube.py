from docutils import nodes
from docutils.parsers.rst import Directive
from sphinx import addnodes

class YouTube(Directive):

    required_arguments = 1

    def run(self):
        id = self.arguments[0]
        html = f"""
        <div class="video-container">
            <iframe width="100%" height="100%" src="https://www.youtube-nocookie.com/embed/{id}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen>
            </iframe>
        </div>"""

        # Add only HTML node.
        output = addnodes.only(expr="html")

        # Add YouTube HTML to only node.
        output += nodes.raw(text=html, format="html")

        # Add fallback for PDFs.
        fallback = addnodes.only(expr="latexpdf")
        # TODO: add optional fallback text
        fallback += nodes.paragraph(text=f"We published a video on this topic, available here: https://www.youtube.com/watch?v={id}")
        output += fallback

        return [output]



def setup(app):
    app.add_directive("youtube", YouTube)
    return {
        'version': '0.1',
        'parallel_read_safe': True,
        'parallel_write_safe': True,
    }
