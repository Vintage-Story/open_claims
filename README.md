# Open Claims
GUI for claims and receive more blocks and areas to claim per hour.
Changes some aspects of the claiming such as forcing the full height claim, expires claim after some time blocks the claim if player try to claim too close to another player.

---

- Now you receive blocks to claims based on hours played on the server (just like the old school grief protection from minecraft)
- Now you receive areas to claim on hours played on the server
- Claim land using the map
- Title for the claimed region
- Allow and unallow using the Map
- List of claims in the map
- Forces the full height claim
- Claim expiration after a configurable time
- Blocks claims too close from other players

### About Open Claims
Open Claims is open source project and can easily be accessed on the github, all contents from this mod is completly free.

If you want to contribute into the project you can access the project github and make your pull request.

You are free to fork the project and make your own version of Open Claims, as long the name is changed.

### Building
- Install .NET in your system, open terminal type: ``dotnet new install VintageStory.Mod.Templates``
- Create a template with the name ``OpenClaims``: ``dotnet new vsmod --AddSolutionFile -o OpenClaims``
- [Clone the repository](https://github.com/LeansBoboDev/open_claims/archive/refs/heads/main.zip)
- Copy the ``CakeBuild`` and ``build.ps1`` or ``build.sh`` and paste inside the repository

Now you can build using the ``build.ps1`` or ``build.sh`` file

FTM License