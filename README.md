GitHubReleaseNotes
====================

Generates release notes for a milestone in a GitHub repo based on issues associated with the milestone.

### Conventions

* All closed issues for a milestone will be included.
* All issues must have exactly one `Type: xxx` label. E.g. `Type: Bug`, `Type: Feature`, `Type: Refactoring`, etc. Only issues labelled `Type: Bug` and `Type: Feature `will be included in the release notes. Issues with other `Type: xxx` labels will be included in the milestone but excluded from the release notes.
* Milestones are named `{major.minor.patch}`.
* The version is picked up from the build number (GFV) and that info is used to find the milestone.
* Release notes are generated as markdown.

### Plans

* The build server will compile the release notes either for each commit or daily
* Build will fail if release notes can't be generated
* No milestone found is considered a exception
* Want to be able to output in a manner compatible with http://www.semanticreleasenotes.org/
* We'll generate release notes as X for inclusion in our nugets
* For each milestone a corresponding GitHub release will be created with the same name and set to tag master with the same tag when published
