# Minimal makefile for Sphinx documentation
#

# You can set these variables from the command line, and also
# from the environment for the first two.
SPHINXOPTS    ?=
SPHINXBUILD   ?= sphinx-build
SOURCEDIR     = .
BUILDDIR      = _build

# Put it first so that "make" without argument is like "make help".
help:
	@$(SPHINXBUILD) -M help "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) $(O)

.PHONY: help Makefile

serve:
	echo "http://localhost:8000"; python3 -m http.server -d _build/html

live:
	sphinx-autobuild "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) -a $(O)

pdf-hdrp:
	@$(SPHINXBUILD) -M latexpdf "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) -E -a -t hdrp -t no-tabs $(O)
	cp -f _build/latex/crest.pdf ../crest/Assets/Crest/userguide.pdf
	cp -f _build/latex/crest.pdf _build/crest-hdrp.pdf

pdf-urp:
	@$(SPHINXBUILD) -M latexpdf "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) -E -a -t urp -t no-tabs $(O)
	cp -f _build/latex/crest.pdf ../crest/Assets/Crest/userguide.pdf
	cp -f _build/latex/crest.pdf _build/crest-urp.pdf

pdf-birp:
	@$(SPHINXBUILD) -M latexpdf "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) -E -a -t birp -t no-tabs $(O)
	cp -f _build/latex/crest.pdf ../crest/Assets/Crest/userguide.pdf
	cp -f _build/latex/crest.pdf _build/crest-birp.pdf

pdf:
	make pdf-birp
	make pdf-urp
	make pdf-hdrp

# Catch-all target: route all unknown targets to Sphinx using the new
# "make mode" option.  $(O) is meant as a shortcut for $(SPHINXOPTS).
%: Makefile
	@$(SPHINXBUILD) -M $@ "$(SOURCEDIR)" "$(BUILDDIR)" $(SPHINXOPTS) $(O)
