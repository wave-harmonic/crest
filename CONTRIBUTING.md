# Contributing

Thank you for your interest in contributing to Crest! :)

To propose changes/additions, please fork this repository and then open a pull request.

There are a few stylistic conventions followed in the code:

* The Format Document command in vanilla VS2017 is used to apply code formatting after every code edit (shortcut Ctrl+K, Ctrl+D)
* Simplicity is king, with modularity favoured when it does not hurt readability or workflows
* Member variables follow lower camel case with a single underscore prefix: \_exampleVariable
* Single blank line between functions and between logically separate bits of code
* Tweakable/serialised variables are private and tagged with *SerializeField*
* [Documentation comments](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments) preferred when useful for documentation, except when member variable is exposed to the Inspector GUI, in which case the comment is placed in *Tooltip* string so that it can aid users.
* Comments placed on their own line above the relevant code
* Public member variables typically ordered together before private ones in class definitions
* No dead or commented code in the master branch, unless it is instructive to include it and not distracting for readers. Experimental or unfinished code belongs in development branches.
* Commit messages contain a 50 char summary line (see [git-style-guide](https://github.com/agis/git-style-guide) for details on this)

The [git-style-guide](https://github.com/agis/git-style-guide) provides additional examples of good practices that Crest aligns with.
