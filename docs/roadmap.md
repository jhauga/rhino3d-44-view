# Roadmap

## Goal

Publish 44 View on the Rhino package server and on Food4Rhino once the alpha
has been checked on real models.

## Current state: 1.0.0-alpha.0.0.0

The command does what it sets out to do. `44View` restores the four default
viewports, including a closed or maximized one, fits Top, Front and Right at
one shared scale centered on the model, fits Perspective, and saves and shows
the four named views. The options work from the prompt, from macros and from
scripts. Automated suites check the fit inside Rhino 8 by measuring the
viewports: the shared scale, the centering, the camera targets, the
construction planes and the grid alignment across views.

Treat every build as an alpha until the checks below are done.

## Before the first release

- Check large models, inch and meter models, and unusual window proportions by
  hand.
- Run the suites once in a non-English Rhino.
- Push a package to the yak test server and install it from there.
- Confirm that the package server accepts the package name `44-view`.

## After the first release

- First-angle layouts, with Top below Front.
- A three-view variant to pair with Rhino's `3View`.
- Padding per view, including Perspective.
- A toolbar button.
- Rhino for Mac.

## Release plan

1. Alpha builds while the checks above are open, each with a GitHub
   prerelease made by the Build workflow.
2. A test-server push of the release candidate.
3. Version `1.0.0` on the package server, then the Food4Rhino page created
   from it.
