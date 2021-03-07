from docutils import nodes

def setup(app):
    app.add_role('pr', github('https://github.com/wave-harmonic/crest/pull/%s'))
    app.add_role('issue', github('https://github.com/wave-harmonic/crest/issues/%s'))
    app.add_role('github', autolink('https://github.com/%s'))
    app.add_role('module', autolink('https://github.com/bemusic/bemuse/tree/master/src/%s'))
    app.add_role('tree', autolink('https://github.com/bemusic/bemuse/tree/master/%s'))

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
