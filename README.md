# NEAAnalyzer
NEAAnalyzer is two WPF applications used for validation of XML data from archival information packages (AIPs) based on SIARDDK (regulations 1007 and 128).

This code consists of two applications: NEAAnalyzer and HardHorn. They are both based on the same library (nealib), and have most functionalities in common.
NEAAnalyzer has a more simple GUI and is easier to get started with.

# Development
Restore nuget packages with ``nuget restore``.

The GUIs are build around a MVVM model, and all GUI elements are defined using XAML.

## Submodule nealib
The project is based on a special branch of nealib, which is currently stale and only used by the NEAAnalyzer applications.
This reason for this is that most specific functionality was removed from nealib to create a simpler library.
The classes and methods in nealib->used-by-neaanalyzer used by NEAAnalyzer sould be moved to the NEAAnalyzer source code.

# Deployment
The applications are deployed using Clickonce deployment to a local or shared folder, from with the application can be installed.

