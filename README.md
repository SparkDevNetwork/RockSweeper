# Rock Sweeper

This is a tool that is designed to help you quickly take a production
[RockRMS](https://www.rockrms.com) database and convert it into one
that can be used for sandbox testing or development purposes.

It should be noted that this may not catch every scenario you want it
to you. The first time you _sweep_ a database, you should spend extra
time looking over the results to make sure everything is in the proper
state you expect it to be. The good news is, once you have verified that
it is doing everything you thought it would, each time you run it with
the same options it will do _exactly_ the same thing. This removes the
need to worry that you forgot a step.

## Sandbox Usage

These steps assume your sandbox server is a full blown IIS/SQL server.

1. Backup your production `Rock` database.
2. Backup your production `RockWeb` folder.
3. Stop IIS (or at least the website that will host sandbox Rock).
4. Restore your `Rock` database and `RockWeb` folder to your sandbox server.
5. Run RockSweeper on the sandbox server's database.
6. Restart your IIS service if necessary.

Step 3 is important. You don't want to risk that somebody tries to log
into the sandbox server and causes Rock to startup before you have run
the sweeping tool on it. Otherwise you might start sending out e-mails
from your sandbox server.

## Development Usage

These steps are for if you are taking a production database and moving
it to a computer where you use Visual Studio and the
[Rockit](https://www.rockrms.com/Rock/Developer/Rockit) SDK.

1. Backup your production `Rock` database.
2. Backup your production `RockWeb` folder.
3. Ensure your Rockit SDK is running the same release version as the production server.
4. Quit Visual Studio (this ensures the IIS Express is fully stopped).
5. Restore your `Rock` database to your development box.
6. Restore your `RockWeb` folder to your development box _in a different location than the Rockit SDK_.
7. Run RockSweeper on the development box's database.
8. Fire up Visual Studio and run the Rockit project.

You can technically run a newer version of the Rockit SDK than your
production server, you just have to force it to run all the Rock migrations
to update the database after you restore.

## Actions

There are a number of options that can be performed on your database.
Many of them require you to select your RockWeb folder as well. This
doesn't make any changes to the files on disk, but it is needed to
properly scan the various plugins that might exist.

For a full list of actions and what each one does, head over to the [wiki](https://github.com/cabal95/RockSweeper/wiki/Actions).
