ReleaseNotesCompiler
====================

In order to improve the quality for our release notes we'll generate them based on the relevant github issues.

* The build server will compile the release notes either for each commit or daily
* Build will fail if release notes can't be generated
* All closed issues for a milestone will be included
* All issues must have one of the follwing tags Bug|Feature|Internal refactoring
* For now the text is taken from the name of the issue
* Milestones are named {major.minor.patch}
* Version is picked up from the build number (GFV) and that info is used to find the milestone
* No milestone found is considerd a exception
* The output format should follow http://www.semanticreleasenotes.org/
* We'll generate release notes as markdown for display on the website
* We'll generate release notes as X for inclusion in our nugets
* For each milestone a corresponding GitHub release will be created with the same name and set to tag master with the same tag when published


