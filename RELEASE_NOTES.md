### 3.0.0+3487749 (Released 2025-9-8)
* Additions:
    * [[#efc808c](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/efc808c858ecce76beb8eb8ea5fe531450fb6353)] upate ARCtrl dependency to 3.0
    * [[#096755e](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/096755e80afce5b01eb9b47a8ef1beb09fa76dca)] add first version of fragment access
    * [[#a3e5d3c](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/a3e5d3c71cc8438e6039a610d365141a5c0ed424)] some additional helper functions to retreive values from specific studies and assays
    * [[#6f1af0f](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/6f1af0f367710addd7351d39d7b6c4d0867c16a8)] start working extended DataContext inclusion
    * [[#d82e591](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/d82e59172de08152d1b4165b298d004376dbb6c0)] add some additional datacontext related helper functions
    * [[#cce6082](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/cce6082ab062045227fd16701d5cb701c0b67111)] add additional unit tests for CSV Fragment Selector parsing
    * [[#b968e6f](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/b968e6ff8ef070e3af08006fef67edeffc659b6c)] Add .zenodo.json for metadata and contributors
* Bugfixes:
    * [[#36baabd](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/36baabd7bc261afbd112f519da3697839e64cd54)] Fix JSON formatting in .zenodo.json

### 2.1.0+5fad60b (Released 2024-12-20)
* Deletions:
    * [[#240e99b](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/240e99bfc80dc5beaf280e5df277db59125c02e6)] remove deprecated ARCtrl.NET project and rename to ARCtrl.Querymodel
* Bugfixes:
    * [[#5fad60b](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/5fad60b3a13219e3503639c87a612257b12401eb)] small fix to build and test workflow
    * [[#5b3147e](https://github.com/nfdi4plants/ARCtrl.Querymodel/commit/5b3147e56ae9579094ad0ec210e556ad4ddc107a)] unsupported log file format fix....

### 2.0.2+0fa20cf (Released 2024-10-21)
* Additions:
    * [[#c0faecc](https://github.com/nfdi4plants/ARCtrl.NET/commit/c0faeccc18f20a366c65d3a8b4d67e23f509aa89)] Update README.md
    * [[#0fa20cf](https://github.com/nfdi4plants/ARCtrl.NET/commit/0fa20cf9f2228b6dfc43bc04010f35a7b89f3413)] update Querymodel ARCtrl reference

### 2.0.1+91e698a (Released 2024-8-29)
* Bugfixes:
    * [[#91e698a](https://github.com/nfdi4plants/ARCtrl.NET/commit/91e698aadc0f468284c2d48234c37595f94e2e44)] update ARCtrl dependency for 2.0.1 hotfix

### 2.0.0+c79c68d (Released 2024-8-29)
* Additions:
    * [[#c79c68d](https://github.com/nfdi4plants/ARCtrl.NET/commit/c79c68d4240f044fec4585a4dfb9cb464afc2bb3)] bump to 2.0.1
    * [[#bc26f3d](https://github.com/nfdi4plants/ARCtrl.NET/commit/bc26f3d3aa3e124f1fbf53871b036105f4e988ac)] Create manage-issues.yml
    * [[#075458b](https://github.com/nfdi4plants/ARCtrl.NET/commit/075458b84a50b745ce1dc572a1d35897e1b90d6b)] update dependencies of ARCtrl.NET
* Deletions:
    * [[#9f0e80c](https://github.com/nfdi4plants/ARCtrl.NET/commit/9f0e80c6b0969f6bba57cd492e8bdf1ccece628e)] remove querymodel from build and update ARCtrl

### 1.0.5+2195d8a (Released 2024-2-21)
* Additions:
    * [[#2c40682](https://github.com/nfdi4plants/ARCtrl.NET/commit/2c406826a109f14b033f12467c6248d963878820)] add querymodel helper function and minimal README
    * [[#26a3ed4](https://github.com/nfdi4plants/ARCtrl.NET/commit/26a3ed4cf0049c4c675c755256bdf8642aa72d69)] Create TestArc
    * [[#f0d8b49](https://github.com/nfdi4plants/ARCtrl.NET/commit/f0d8b49b2d7a13325efcf07b9cd94470b97d65ce)] add playground.fsx
    * [[#bce18c1](https://github.com/nfdi4plants/ARCtrl.NET/commit/bce18c198290b02b07213ecb7baebec2e65c4742)] Update playground.fsx
    * [[#eefaeea](https://github.com/nfdi4plants/ARCtrl.NET/commit/eefaeeab68e4a719bcc2f2d492227fe927b27c38)] Update playground.fsx
    * [[#ac9eb94](https://github.com/nfdi4plants/ARCtrl.NET/commit/ac9eb94922ec4350bac945e61c125c57c73b9afa)] update querymodel test objects
    * [[#67fe811](https://github.com/nfdi4plants/ARCtrl.NET/commit/67fe811e8e452a9cb0bf55928ed3197764c8f6af)] add minimal querymodel tests
    * [[#dff6154](https://github.com/nfdi4plants/ARCtrl.NET/commit/dff61542bb7176b774fec7b4ca04c0c440234121)] reduce complexity of querymodel testfiles
    * [[#d8d5c39](https://github.com/nfdi4plants/ARCtrl.NET/commit/d8d5c39aade197d70b0ea089f330cfe980cf7169)] improve quality of querymodel testarc
    * [[#468d3bb](https://github.com/nfdi4plants/ARCtrl.NET/commit/468d3bb67dc364a5b6251cbafb9883d6beb70bb6)] update test arc
* Bugfixes:
    * [[#e020abe](https://github.com/nfdi4plants/ARCtrl.NET/commit/e020abe86dbfd9c22cd94f7959cd3e8e80146b4e)] fix typos in sample arc, update playground.fsx
    * [[#f490ce6](https://github.com/nfdi4plants/ARCtrl.NET/commit/f490ce6a0211f03e28cc283182c967d076425e56)] fix querymodel getNodesBy returning duplicate nodes
    * [[#e28a464](https://github.com/nfdi4plants/ARCtrl.NET/commit/e28a4643495b151e839340ad17bc74c8ef6c6a5d)] test and fix querymodel value retrieval
    * [[#e4a7c14](https://github.com/nfdi4plants/ARCtrl.NET/commit/e4a7c14d1b4b1d51f17db811b4224ee145b567c9)] added querymodel test and fix for values of pooled output #18
    * [[#b40490c](https://github.com/nfdi4plants/ARCtrl.NET/commit/b40490cf94e600ed172accfa597d726438d8093b)] fix querymodel value getting

### 1.0.4+9e24cf4 (Released 2024-1-30)
* Additions:
    * Update dependencies

### 1.0.3+9f43871 (Released 2024-1-26)
* Additions:
    * [[#9f43871](https://github.com/nfdi4plants/ARCtrl.NET/commit/9f438717591ebf61bbbbc36052b4ffffc8a2b21b)] update references
    * [[#d201f25](https://github.com/nfdi4plants/ARCtrl.NET/commit/d201f2573a2b2ff8d05272dcf0dfca3b2815502e)] bump to 1.0.2

### 1.0.2+70b2b2c (Released 2024-1-24)
* Additions:
    * [[#70b2b2c](https://github.com/nfdi4plants/ARCtrl.NET/commit/70b2b2ceb3b09615ad74afbd9d3e8c9abfcf09e5)] update arctrl and fsspreadsheet refs
    * [[#2537c1f](https://github.com/nfdi4plants/ARCtrl.NET/commit/2537c1f431db261b6a327b0dc562a8e895951257)] bump to 1.0.1

### 1.0.1+0c285e8 (Released 2024-1-11)
* Additions:
    * [[#0c285e8](https://github.com/nfdi4plants/ARCtrl.NET/commit/0c285e8dd9825fc4f747ef031d210a5109cded1c)] update arctrl reference to 1.0.2

### 1.0.0+e8e4ca3 (Released 2023-12-21)
* Additions:
    * [[#e8e4ca3](https://github.com/nfdi4plants/ARCtrl.NET/commit/e8e4ca37ea5dc963d36c33d5f92286f139fdd04e)] Update README.md with links and minimal documentation
    * [[#d82fdf6](https://github.com/nfdi4plants/ARCtrl.NET/commit/d82fdf6f434902341e1da4dc26fc92da5c288fe6)] add querymodel test repo
    * [[#32b0b7d](https://github.com/nfdi4plants/ARCtrl.NET/commit/32b0b7d96b7d25882309efe266498be32523a710)] finish up querymodel transition
    * [[#49577a9](https://github.com/nfdi4plants/ARCtrl.NET/commit/49577a9cbba4bb171a092bdf731ad9477ffab47f)] continue transition of querymodel
    * [[#d596f8b](https://github.com/nfdi4plants/ARCtrl.NET/commit/d596f8b5256200d1663273509fb8fee4537c2c1c)] bump to 1.0.0-beta.2
    * [[#da0642b](https://github.com/nfdi4plants/ARCtrl.NET/commit/da0642b71f8ca4a9e4344fbee0879fb0a61f5831)] add querymodel project from previous ISADotNet state
    * [[#ac8fb85](https://github.com/nfdi4plants/ARCtrl.NET/commit/ac8fb8506310f8e2bf8d9b8a5d15b34251149b0e)] bump to 1.0.0-beta.1
    * [[#70032b4](https://github.com/nfdi4plants/ARCtrl.NET/commit/70032b47f82a1177895d89ec314e44c357f95d0a)] add simple arc read-in test
    * [[#132788e](https://github.com/nfdi4plants/ARCtrl.NET/commit/132788efe421ceb23526c40e7c2dceac5f1ded3f)] include simple test arc
    * [[#c805976](https://github.com/nfdi4plants/ARCtrl.NET/commit/c80597620132a916d2fe05c8214e3cd58ec8c9e0)] add contract tests
    * [[#41cdec5](https://github.com/nfdi4plants/ARCtrl.NET/commit/41cdec556c6f4d2d460bca25723a36fdd88706b9)] add arc write tests
    * [[#7d65d95](https://github.com/nfdi4plants/ARCtrl.NET/commit/7d65d9595eded9390e842ac52c7bd1e9aea27356)] update arctrl reference to 1.0.0-alpha9
    * [[#f3eda8e](https://github.com/nfdi4plants/ARCtrl.NET/commit/f3eda8e96a3a7791288c1b5975050742c1d803d9)] Merge pull request #9 from nfdi4plants/arctrl
    * [[#ba3d2fa](https://github.com/nfdi4plants/ARCtrl.NET/commit/ba3d2fabe007d9ca2c8e07b62d02ddc5264306d0)] update arctrl reference to 1.0.0-alpha10
    * [[#72d58da](https://github.com/nfdi4plants/ARCtrl.NET/commit/72d58daddfdbe03f707db03bfc4edc6801dbd51b)] bump to 1.0.0-alpha2
    * [[#d06e12c](https://github.com/nfdi4plants/ARCtrl.NET/commit/d06e12cb08726cdd349ab2d77db4fe1314271cca)] bump to 1.0.0-alpha1
    * [[#2118bdd](https://github.com/nfdi4plants/ARCtrl.NET/commit/2118bdd326281ba955c755d77d562fa070ba9f7b)] add write function
* Bugfixes:
    * [[#47011f8](https://github.com/nfdi4plants/ARCtrl.NET/commit/47011f8d30f3d72d927f4540a9515e55c0028f22)] add more contraints to getAllpaths fix #10

### 0.1.0+4145f8d (Released 2023-4-3)
* Additions:
    * [[#aca980b](https://github.com/nfdi4plants/arcIO.NET/commit/aca980bef59e7b6b17b8376e83b5177cf3f442d1)] include json in converter
    * [[#4145f8d](https://github.com/nfdi4plants/arcIO.NET/commit/4145f8dee6642bfef7384370750503a9d8a433a0)] setup test environmet pin dotnet version to 6.x.x update ISADotNet reference

### 0.0.6+da8c364 (Released 2023-1-26)
* Additions:
    * [[#6ed4bc8](https://github.com/nfdi4plants/arcIO.NET/commit/6ed4bc840dc09e2f8dd348a79241eb64c7de8d5c)] migrate githelper and logging from arcCommander
    * [[#929bab1](https://github.com/nfdi4plants/arcIO.NET/commit/929bab19595e511d96367a09945fab943638c13e)] Improve linux file reads for Study and Assay: autocorrect backslashes to frontslashes
    * [[#33e6b26](https://github.com/nfdi4plants/arcIO.NET/commit/33e6b267d3ec7d18274a47e2e8420b1a258af67a)] Improved investigation fromArcFolder and added autocorrect for reading file paths.
    * [[#23005bc](https://github.com/nfdi4plants/arcIO.NET/commit/23005bce740a7f0a4cd1f2d36c372d556282069f)] Assay create by name instead of create by filename
    * [[#bfa0335](https://github.com/nfdi4plants/arcIO.NET/commit/bfa0335cd7607d8fd471080f37e4eaffde7eb2bc)] Added arc init functionality (subfolders, investigation file, git)
    * [[#cf082e2](https://github.com/nfdi4plants/arcIO.NET/commit/cf082e2842f21b01503b453e3d17dde308bb8b16)] Added assay register to Investigation module
    * [[#da8c364](https://github.com/nfdi4plants/arcIO.NET/commit/da8c36448bec084fec3fc5fe7a08d6aa55a92521)] update isadotnet reference
* Bugfixes:
    * [[#b4748be](https://github.com/nfdi4plants/arcIO.NET/commit/b4748bef60b91b2dc148fe4d3018733f5e810790)] build fixes for linux

### 0.0.5+1d873f4 (Released 2023-1-8)
* Additions:
    * [[#b7c205e](https://github.com/nfdi4plants/arcIO.NET/commit/b7c205e6dd6a219d1c3f9b4dbe8ca7d1883785f2)] update ISADotNet reference
    * [[#8c89e83](https://github.com/nfdi4plants/arcIO.NET/commit/8c89e830409219fa8335a74704cf2ec89d35be3f)] setup build project
* Bugfixes:
    * [[#ccdec5d](https://github.com/nfdi4plants/arcIO.NET/commit/ccdec5d2610790f8bf73ae6bff769f63f0397643)] fix ci workflow

### 0.0.4+d08fcf47 (Released 2022-7-6)
* Additions:
    * latest commit #d08fcf47
    * 	* fix filestream issues when writing

### 0.0.1+ae26cc2 (Released 2022-7-6)
* Additions:
    * latest commit #ae26cc2
    * 	* initialize project with some core reading functionality

