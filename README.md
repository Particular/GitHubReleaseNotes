ReleaseNotesCompiler
====================

In order to improve the quality for our release notes we'll generate them based on the relevant github issues.

* Each build will generate the release notes (master, develop, release-* , hotfix-* )
* Build will fail if release notes can't be generated
* All closes issues for a milestone will be included
* All issues must have one of the follwing tags Bug|Feature|Internal refactoring
* Milestones are named {major.minor.patch}
* Version is picked up from the build number (GFV) and that info is used to find the milestone
* No milestone found is considerd a exception
* The output format should follow http://www.semanticreleasenotes.org/
* We'll generate release notes as markdown for display on the website
* We'll generate release notes as X for inclusion in our nugets


