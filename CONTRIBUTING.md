Thank you for your interest in contributing to Crest! :)

To propose changes/additions, please fork this repository and then open a pull request.

Time and effort has been put into maximising quality of the Crest codebase. Below a number of stylistic conventions are outlined that aid in readability and understanding. The code has gone through many iterations and many refactoring passes to smooth out kinks and simplify complexity wherever possible. Unfortunately this volume of change may cause some pain for forks. Now that the existing code is maturing we will establish releases to provide less volatile branches which will hopefully help give stability.

There are a few stylistic conventions followed in the code:

* Member variables follow lower camel case with a single underscore prefix: \_exampleVariable
* Single blank line between functions and between logically separate code
* Public member variables typically grouped together above private ones
* Comments placed on their own line above the relevant code
* No commented code in the master branch, unless it is instructive to include it and not distracting for readers
* No code that is not used in the master branch; experimental or unfinished code belongs in development branches
* [Documentation comments](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments) preferred when useful for documentation
* Simplicity is king, with modularity favoured when it does not hurt readability or workflows

There are currently some bad practices in the code that are slowly being cleaned up:

* Lack of documentation comments
* Public variables used in some cases where a private variable and public accessor is more appropriate
* Commit messages have historically missed the 50 char summary line (see [git-style-guide](https://github.com/agis/git-style-guide) for details on this)

Finally, the [git-style-guide](https://github.com/agis/git-style-guide) provides many examples of good practices that Crest aligns with.
