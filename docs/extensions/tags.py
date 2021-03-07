from sphinx.util.docutils import SphinxDirective

class Tags(SphinxDirective):

    def run(self):
        # If HTML or ReadTheDocs, then we don't need stripping based on pipeline.
        # We need to do this here because builder tags is populated after conf.py is loaded.
        tags = self.env.app.tags
        if tags.has("html") or tags.has("readthedocs"):
            tags.add("birp")
            tags.add("hdrp")
            tags.add("urp")

        return []

def setup(app):
    app.add_directive("tags", Tags)
