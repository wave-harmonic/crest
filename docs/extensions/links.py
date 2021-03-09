from docutils import nodes

def setup(app):
    app.add_role('branch', autolink('https://github.com/wave-harmonic/crest/tree/%s'))
    app.add_role('wiki', autolink('https://github.com/wave-harmonic/crest/wiki/%s'))

def github(pattern):
    def role(name, rawtext, text, lineno, inliner, options={}, content=[]):
        url = pattern % (text,)
        node = nodes.reference(rawtext, f"#{text}", refuri=url, **options)
        return [node], []
    return role

def autolink(pattern):
    def role(name, rawtext, text, lineno, inliner, options={}, content=[]):
        url = pattern % (text,)
        node = nodes.reference(rawtext, text, refuri=url, **options)
        return [node], []
    return role
