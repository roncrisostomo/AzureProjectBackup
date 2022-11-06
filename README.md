# AzureProjectBackup
Tool for creating backups of an existing project on Azure

## How to use

1. Build and run -> console window should appear with following text:
    - "Enter following items: [Target project] [Source project] [Azure organization] [Personal access token]"
1. Follow console instructions
    - E.g. "ProjectBackup20221107 OrigProject AzureOrg e2l7o4m3wuhaz95oayzdzqooxkr2xau943n77gixiwgddlih5wae
1. Voila! A new project should be added to your Azure organization

## Known issues

1. If source project has iteration called "Sprint 1", this iteration's start and finish dates will not be copied over to the new project
    - Reason: When a new project is created, it comes with a default iteration called "Sprint 1". Thus creation of iteration with same name fails.
    - Workaround: Manually update start and finish dates for Sprint 1
    - TBD code fix: Upon creation of target project, delete existing iterations before creating copies of iterations from source project.

## Flow

1. Overall flow
    1. Create target project if it does not exist yet
    1. Copy iterations from source to target
    1. Copy work items from source to target

## Assumptions

1. Project should use Azure Basic process: Epics, Issues, and Tasks
1. Tasks cannot be top-level work items, i.e. these need to be children of Epics or Issues
