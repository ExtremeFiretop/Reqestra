## [4.61.19](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.18...v4.61.19) (2026-10-09)


### Bug Fixes

* **updater:** harden Windows update extraction ([dabbe88](https://github.com/ExtremeFiretop/Reqestra/commit/dabbe88a4ce05cee2414411282b89f9e2be1eaa4))



## [4.61.18](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.17...v4.61.18) (2026-10-09)


### Bug Fixes

* **plex:** harden episode sync against orphaned cache rows ([6b747dd](https://github.com/ExtremeFiretop/Reqestra/commit/6b747dd577d43761e41c1ef2bd6257c26c4ba5d6))



## [4.61.17](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.16...v4.61.17) (2026-10-09)



## [4.61.16](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.15...v4.61.16) (2026-10-09)


### Bug Fixes

* **newsletter:** skip orphaned media episodes ([949b28f](https://github.com/ExtremeFiretop/Reqestra/commit/949b28fbe8e2b242f6f32389dd1151a26bec0322))



## [4.61.15](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.14...v4.61.15) (2026-10-08)



## [4.61.14](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.13...v4.61.14) (2026-10-08)


### Bug Fixes

* **sonarr:** normalize monitoring for newly-added series ([310469a](https://github.com/ExtremeFiretop/Reqestra/commit/310469abdf82324c81c2c6372f7ead2b38ae74bd))



## [4.61.13](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.12...v4.61.13) (2026-10-08)


### Bug Fixes

* **sonarr:** allow safe trailing provider episode gaps ([d36a349](https://github.com/ExtremeFiretop/Reqestra/commit/d36a3499ebdbcba920afd8adabebc51df9a30a25))



## [4.61.12](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.11...v4.61.12) (2026-10-08)



## [4.61.11](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.10...v4.61.11) (2026-10-08)


### Bug Fixes

* **sonarr:** allow placeholder metadata for exact seasons ([177d226](https://github.com/ExtremeFiretop/Reqestra/commit/177d2265cc5e2f28c6386056f6adbce9b260958d))



## [4.61.10](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.9...v4.61.10) (2026-10-07)


### Bug Fixes

* **plex:** serialize content sync and database refresh ([339c106](https://github.com/ExtremeFiretop/Reqestra/commit/339c106d7026e023a16e8f326df4f22da5a77e81))



## [4.61.9](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.8...v4.61.9) (2026-10-06)


### Bug Fixes

* **ci:** reconcile release tag ancestry before versioning ([91a2310](https://github.com/ExtremeFiretop/Reqestra/commit/91a2310d4ee5676378dda23b3cadc69aaa9a07ab))
* **plex:** handle rate-limited API failures safely ([a19e043](https://github.com/ExtremeFiretop/Reqestra/commit/a19e043f1c8bc5d3b8c727455e4a8197542b0c9b))
* **tests:** import Plex friends models in API tests ([a787933](https://github.com/ExtremeFiretop/Reqestra/commit/a7879331163373502374ec6798d265b2431bbe2a))



## [4.61.8](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.7...v4.61.8) (2026-10-03)


### Bug Fixes

* **ci:** isolate wizard from full E2E suite ([0fb9839](https://github.com/ExtremeFiretop/Reqestra/commit/0fb983961a9be135435f08379fd5b8d9c1c3f716))
* **security:** harden user-controlled logging ([c65a46c](https://github.com/ExtremeFiretop/Reqestra/commit/c65a46c8bb2653bf91a07ea0f83ee01d98a6cc89))



## [4.61.7](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.6...v4.61.7) (2026-10-03)


### Bug Fixes

* **ci:** make release versioning tag-driven ([6cc5800](https://github.com/ExtremeFiretop/Reqestra/commit/6cc580080d84d3f440db028ba69e392ec40f82f8))
* **notifications:** bound queue and retry transient failures ([35236e3](https://github.com/ExtremeFiretop/Reqestra/commit/35236e36d9997ae315bdeb50b4cb3e634858239b))
* **notifications:** replace fire-and-forget cleanup emails ([244574d](https://github.com/ExtremeFiretop/Reqestra/commit/244574d0e49b1f15d4f83a4061fc49e49439d5f8))



## [4.61.6](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.5...v4.61.6) (2026-10-03)


### Bug Fixes

* **updater:** align Microsoft.Extensions dependencies with .NET 8 ([3f99665](https://github.com/ExtremeFiretop/Reqestra/commit/3f9966597ccb4066f3af348dd8c8a07fe5fe5bb8))



## [4.61.5](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.4...v4.61.5) (2026-10-03)


### Bug Fixes

* **updater:** harden process and Windows service handoff ([5672d79](https://github.com/ExtremeFiretop/Reqestra/commit/5672d79834f342faf429e7ef251062fef61e4bd9))
* **updater:** resolve Octokit FileMode ambiguity ([18e2fd7](https://github.com/ExtremeFiretop/Reqestra/commit/18e2fd757136bd0002d416b33d38f8c60eace115))
* **updater:** restore built-in release installation ([a66a615](https://github.com/ExtremeFiretop/Reqestra/commit/a66a615a3a691376685e70de430a9b3d9619dde4))
* **updater:** tolerate transient GitHub update-check failures ([30cfe00](https://github.com/ExtremeFiretop/Reqestra/commit/30cfe00e709f7e19b58c44129f735418c4cf17aa))



## [4.61.4](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.3...v4.61.4) (2026-10-02)


### Bug Fixes

* **ci:** harden multi-arch frontend Docker build ([3824f35](https://github.com/ExtremeFiretop/Reqestra/commit/3824f35d0c2a5bf5dbf61b85560bc3a0752783d4))



## [4.61.3](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.2...v4.61.3) (2026-10-02)


### Bug Fixes

* **ci:** provide version metadata to Docker build ([513f557](https://github.com/ExtremeFiretop/Reqestra/commit/513f55781d8abafbd9650cc3f196e3e7d4af56a5))
* **metadata:** repair legacy invalid TMDB identifiers ([3340005](https://github.com/ExtremeFiretop/Reqestra/commit/33400053090c017959c6f3d489eee49d44503faa))


### Reverts

* Revert "test(e2e): stabilize discover card state tests" ([d235b4e](https://github.com/ExtremeFiretop/Reqestra/commit/d235b4e048ac7757d81d13a44818554c09bdd35e))



## [4.61.2](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.1...v4.61.2) (2026-10-02)


### Bug Fixes

* **media-cleanup:** harden legacy deletion-plan migration ([83c08db](https://github.com/ExtremeFiretop/Reqestra/commit/83c08db82504fdf719f01d5450f8bdc6ef15509d))
* **metadata:** repair legacy invalid TVDB identifiers ([ea099f5](https://github.com/ExtremeFiretop/Reqestra/commit/ea099f59b55f560c57d5c07c05587d805cdf988c))



## [4.61.1](https://github.com/ExtremeFiretop/Reqestra/compare/v4.61.0...v4.61.1) (2026-10-02)


### Bug Fixes

* **api:** skip Plex Community error-body deserialization ([8ef59cc](https://github.com/ExtremeFiretop/Reqestra/commit/8ef59cc7a803bf06d4859c8969894707e516759a))
* **media-cleanup:** backfill legacy scheduled deletion plans ([411edcf](https://github.com/ExtremeFiretop/Reqestra/commit/411edcffba33e01d3a913fbaa241337f22e61dc6))



# [4.61.0](https://github.com/ExtremeFiretop/Reqestra/compare/c57f33151bc07819bc950675f5a0a7978ba26348...v4.61.0) (2026-10-01)


### Bug Fixes

* :adhesive_bandage: See if this fixes the proxy issue ([74d1aca](https://github.com/ExtremeFiretop/Reqestra/commit/74d1acae499707a7e21401f53eb2bb90c5bb9cfa))
* :bookmark: Set version to correct number ([1a7b9b5](https://github.com/ExtremeFiretop/Reqestra/commit/1a7b9b50578675532fdb0f656cbaf51306166b84))
* :bug: Add UI for Emby recently added cronjob settings ([#4469](https://github.com/ExtremeFiretop/Reqestra/issues/4469)) ([7d47bbe](https://github.com/ExtremeFiretop/Reqestra/commit/7d47bbe92204855bf75d70b8fa548f9c3f3612bc))
* :bug: bump ver ([5207b21](https://github.com/ExtremeFiretop/Reqestra/commit/5207b21f2f98cc63f16a72bd79ecac6abf838139))
* :bug: Fixed missing externals ([#4712](https://github.com/ExtremeFiretop/Reqestra/issues/4712)) ([fcc1eaa](https://github.com/ExtremeFiretop/Reqestra/commit/fcc1eaaa377683dcdc81d62a2a688fb0c4490c7b))
* :bug: Fixed Ombi not writing the baseUrl correctly ([e9cc8b6](https://github.com/ExtremeFiretop/Reqestra/commit/e9cc8b6fe71d3e10c1a901e70227989b3362afe3))
* :bug: Fixed the issue parsing TheMovieDB dates. They have broken something... ([6e397e0](https://github.com/ExtremeFiretop/Reqestra/commit/6e397e02e95f894a92e8bf02428efdcac1275b31))
* :bug: Fixed the issue where the user management login dates were not local time ([97be373](https://github.com/ExtremeFiretop/Reqestra/commit/97be3737700ed7b1ee915dbcd9f44103665d472c)), closes [#2925](https://github.com/ExtremeFiretop/Reqestra/issues/2925)
* :bug: Fixed the issue where we were not generating the newsletter plex mediaserver link correctly ([b6064e9](https://github.com/ExtremeFiretop/Reqestra/commit/b6064e9308ee1218517f54d331c9bd7953ca631e))
* :bug: Fixed the MySQL issue after .net 6 upgrade [#4393](https://github.com/ExtremeFiretop/Reqestra/issues/4393) ([fea7ff0](https://github.com/ExtremeFiretop/Reqestra/commit/fea7ff05139e9ff50c8097fa5389b4ef9ad21a15))
* :bug: Fixed the Request On Behalf autocomplete not filtering correctly ([a8ba2f3](https://github.com/ExtremeFiretop/Reqestra/commit/a8ba2f3544a1c01c57f217c4036a277ab0e67a09)), closes [#4539](https://github.com/ExtremeFiretop/Reqestra/issues/4539)
* :bug: Fixed the Request on Behalf of having blanks ([#4667](https://github.com/ExtremeFiretop/Reqestra/issues/4667)) ([7dd9b1c](https://github.com/ExtremeFiretop/Reqestra/commit/7dd9b1cac07f571dd35b362544e4fe0226c4b817))
* :bug: Fixed where the Play On {MediaSever} was not appearing for TV Shows ([06b60ee](https://github.com/ExtremeFiretop/Reqestra/commit/06b60ee9d2bab4bef24ba3d9c3a6830a12bc3c0f)), closes [#4303](https://github.com/ExtremeFiretop/Reqestra/issues/4303)
* :bug: Hides no results message during search. ([#4375](https://github.com/ExtremeFiretop/Reqestra/issues/4375)) ([b819b0e](https://github.com/ExtremeFiretop/Reqestra/commit/b819b0e007e578bf3d8425f19591f87029c64d06))
* :bug: Pretending to fix a bug ([5351c14](https://github.com/ExtremeFiretop/Reqestra/commit/5351c14cb087f9ecbb37b784724bb35107d17cb8))
* :bug: Really really fix it this time? ([543d36e](https://github.com/ExtremeFiretop/Reqestra/commit/543d36e5615341bc8378cac377b843a3dbc1ef99))
* :bug: Set the real tag version now ([9287d83](https://github.com/ExtremeFiretop/Reqestra/commit/9287d83c134e62c9f0a5d271cfd88eefc9dcae39))
* :bug: Small change to the URL we generate for the Play on Plex link ([e8810e8](https://github.com/ExtremeFiretop/Reqestra/commit/e8810e82b1a8ecc28869186f63708f25c423b3d6))
* :bug: Stop Devops from building and releasing, GH Actions will now call the APT Build ([1fda2a1](https://github.com/ExtremeFiretop/Reqestra/commit/1fda2a1d37c1182177fdca55e38b98a85dc1fe05))
* :bug: updated contributors PAT ([a1d22db](https://github.com/ExtremeFiretop/Reqestra/commit/a1d22db4d14c63c39e79d47d99cc7ca2efe393df))
* :bug: Upload the correct artifacts ([a249805](https://github.com/ExtremeFiretop/Reqestra/commit/a2498051cfd679dd19206571883a08d77e159e2b))
* :fire: Really fix the base url issue this time ([9f36923](https://github.com/ExtremeFiretop/Reqestra/commit/9f36923c51bfabf9cb026f2da14f9947050af0d9))
* :globe_with_meridians: Added Czech and Chinese Simplified to the language list ([68ef366](https://github.com/ExtremeFiretop/Reqestra/commit/68ef366e8525e2c349b9e81704ad8bcca6c347a0))
* [#5223](https://github.com/ExtremeFiretop/Reqestra/issues/5223) ([cf0c161](https://github.com/ExtremeFiretop/Reqestra/commit/cf0c1614a496b4f7cf19d78e885c3e37dae5cf0f))
* **#4344:** :bug: Fixed an issue where we errored on Plex Episode Scan ([cd5532f](https://github.com/ExtremeFiretop/Reqestra/commit/cd5532fa8f7ebbfaf942841398672bafb9a405d4))
* **#4345:** :bug: Fixed the issue where denied requests we not appearing correctly ([5a2f652](https://github.com/ExtremeFiretop/Reqestra/commit/5a2f652a28f5699dd667afef8dde129817e53392))
* **#4847:** Invalid Discord request fixed, also fixed an issue where App Only users would not show as logged in on the user management page ([#4848](https://github.com/ExtremeFiretop/Reqestra/issues/4848)) ([f229d88](https://github.com/ExtremeFiretop/Reqestra/commit/f229d88bd744bc5253b5d3db69ae5ef22d014230))
* **#4906:** :bug: Fixed an issue with power users and permissions ([80884bc](https://github.com/ExtremeFiretop/Reqestra/commit/80884bcd725c329867c278ad235cd4096cd4fe7a))
* **4153:** :bug: fixed gravitar issues ([ec963d8](https://github.com/ExtremeFiretop/Reqestra/commit/ec963d869a2bf57edfcfef418e2bf8a1d679d8ca))
* **4616:** :bug: fixed mandatory fields ([d8f2260](https://github.com/ExtremeFiretop/Reqestra/commit/d8f2260c7ae3ed48386743b7adbd06e284487034))
* **4k:** Hide 'Has 4K Request' column list if 4k feature is disabled ([#4521](https://github.com/ExtremeFiretop/Reqestra/issues/4521)) ([a9a6067](https://github.com/ExtremeFiretop/Reqestra/commit/a9a60678e74d22fa7ba34051a2645db86b600b4a))
* Add missing using System to EmbyHelper ([bb6300f](https://github.com/ExtremeFiretop/Reqestra/commit/bb6300fe984dd8b7d5d3f10e64885301e80693d4))
* added media type tag to media type text ([#4638](https://github.com/ExtremeFiretop/Reqestra/issues/4638)) ([fe501d3](https://github.com/ExtremeFiretop/Reqestra/commit/fe501d34a0c36ac9f000b107eca49dbc6694d006))
* **API:** Allow RequestOnBehalf rights if requested from the API ([#4919](https://github.com/ExtremeFiretop/Reqestra/issues/4919)) ([bb6dedd](https://github.com/ExtremeFiretop/Reqestra/commit/bb6deddfaecb3d6c7c3c6970414444b619bb9106))
* **API:** Fix pagination in some edge cases ([#4649](https://github.com/ExtremeFiretop/Reqestra/issues/4649)) ([a70bf8f](https://github.com/ExtremeFiretop/Reqestra/commit/a70bf8f46c76d74c9dfdf908c53bd9955ca0a35d))
* **API:** Fixed an issue where the API key couldn't delete a request [#4489](https://github.com/ExtremeFiretop/Reqestra/issues/4489) ([8e42dbf](https://github.com/ExtremeFiretop/Reqestra/commit/8e42dbf8f78caa51ca891bf3d702c6b0ac401f9c))
* **auth:** Fixed an issue where refreshing the page as a power user would stop the application from loading [#5242](https://github.com/ExtremeFiretop/Reqestra/issues/5242) ([cee4014](https://github.com/ExtremeFiretop/Reqestra/commit/cee40146ee02f7fb79e2019d6fe2f9d5c5dbdfc8))
* **auto-delete:** :bug: We now also auto delete music requests, this was previously missing ([9fe1f8e](https://github.com/ExtremeFiretop/Reqestra/commit/9fe1f8e988aa31d36e7a685ae19f72d9c8414dc0))
* auto-import and link Plex Admin during Plex OAuth login ([#5446](https://github.com/ExtremeFiretop/Reqestra/issues/5446)) ([8e1d264](https://github.com/ExtremeFiretop/Reqestra/commit/8e1d2647d013e03df6cfea52e704ae535a7fb1b5))
* **availability-rules:** :bug: Fixed a small issue where some shows would not appear as Available even know they had no future unaired episodes listed ([914b096](https://github.com/ExtremeFiretop/Reqestra/commit/914b096781c9b73292a533a010a5dd05ecfd0aac))
* **availability-rules:** :bug: Show the 'Requested' button when a show has all of the episodes marked as requested ([cb7ecf6](https://github.com/ExtremeFiretop/Reqestra/commit/cb7ecf684ac3ab204f329a28baecfd4f6cd408f7))
* **availability:** :bug: Fixed an issue where with 4k content, we could repeat notifications ([f9ebc1c](https://github.com/ExtremeFiretop/Reqestra/commit/f9ebc1cc2e13c7cd335121cd86295b10eda529ba))
* **availability:** 🐛 Fixed a issue with the availability checker after the previous update. Added full test coverage around that area ([28e2480](https://github.com/ExtremeFiretop/Reqestra/commit/28e248046ad56390595f84172bbd5f5961325b4d))
* **availability:** Ensure that when Radarr/Sonarr has priority, stick to it [#5286](https://github.com/ExtremeFiretop/Reqestra/issues/5286) ([8f3f87a](https://github.com/ExtremeFiretop/Reqestra/commit/8f3f87a1896b5bc2b251fc9ed819933620a3bcfa))
* **availability:** Fixed an issue where we wouldn't mark a available 4k movie as available (when 4K request feature is disabled) ([b492699](https://github.com/ExtremeFiretop/Reqestra/commit/b49269961d4830a530e3054976a47f519524948b))
* **availability:** Make sure we check radarr/sonarr in the availability rules for it's prioritization [#5286](https://github.com/ExtremeFiretop/Reqestra/issues/5286) ([8af1d67](https://github.com/ExtremeFiretop/Reqestra/commit/8af1d678ac5e91b927ddabfad0f2194d8b763ebb))
* **availability:** mark movies available when Radarr only has a 4K file ([264ae07](https://github.com/ExtremeFiretop/Reqestra/commit/264ae07cbfcec5166f53d9e2ba2e1746243b18d8))
* base url issue [#5343](https://github.com/ExtremeFiretop/Reqestra/issues/5343) ([fb39386](https://github.com/ExtremeFiretop/Reqestra/commit/fb39386e1615cd2eabb882484b64e6a76485e4cb))
* **build:** silence remaining esbuild/sass warnings and remove unused imports ([1915b09](https://github.com/ExtremeFiretop/Reqestra/commit/1915b09e557128d43d70cdecb87f38277497bacb))
* Consistently reset loading flag when requesting movies on discover page. ([#4777](https://github.com/ExtremeFiretop/Reqestra/issues/4777)) ([a40ab5c](https://github.com/ExtremeFiretop/Reqestra/commit/a40ab5cddf769d4147696eca50c1610b466ab99b))
* Correct 4K movie request existence check ([ba6e708](https://github.com/ExtremeFiretop/Reqestra/commit/ba6e708e189f52f2ff4ebc073fa38a4f53f1061c)), closes [#4798](https://github.com/ExtremeFiretop/Reqestra/issues/4798)
* Correct EmbyHelper app.emby.media path detection ([9adb2ed](https://github.com/ExtremeFiretop/Reqestra/commit/9adb2ed4ef6deae79de1f1f2b5c8d249f9400e24))
* Correct EmbyHelper app.emby.media path detection ([88b5215](https://github.com/ExtremeFiretop/Reqestra/commit/88b52159e7582022b7261b79c24359edc8e70da2)), closes [#689](https://github.com/ExtremeFiretop/Reqestra/issues/689)
* Correct EmbyHelper app.emby.media path detection ([840d740](https://github.com/ExtremeFiretop/Reqestra/commit/840d740cd0860b46b185a82ef4855334b3f48301)), closes [#689](https://github.com/ExtremeFiretop/Reqestra/issues/689)
* Correct EmbyHelper app.emby.media path detection ([d16858d](https://github.com/ExtremeFiretop/Reqestra/commit/d16858dbbc5e0b1adb2c96fc0e5c6650a7431556))
* Cron Validation ([#4842](https://github.com/ExtremeFiretop/Reqestra/issues/4842)) ([97cc42f](https://github.com/ExtremeFiretop/Reqestra/commit/97cc42ffa8672e7d0d0996b5fbda7f7fe699da2d))
* **database:** Just some tweaks, shouldn't notice any difference, maybe a less error in the log ([67fb992](https://github.com/ExtremeFiretop/Reqestra/commit/67fb9921c0c025025286eb6c0a9d09fd01b18465))
* **details:** :alien: Removed the RT API calls from the frontend for now ([99c7289](https://github.com/ExtremeFiretop/Reqestra/commit/99c728918e74805a0b2ecdb72db41d45b57b619e)), closes [#4305](https://github.com/ExtremeFiretop/Reqestra/issues/4305)
* **details:** :bug: Fixed the missing Play on Media server button for 4k content [#4529](https://github.com/ExtremeFiretop/Reqestra/issues/4529) ([68600f3](https://github.com/ExtremeFiretop/Reqestra/commit/68600f3b45376e12dd2ef263d81ca4040c84cbca))
* disable font inlining in Angular production build ([713c2fd](https://github.com/ExtremeFiretop/Reqestra/commit/713c2fd5e9456efbf6da016db67e4b3364c82fb0))
* **discord:** 🐛 Fixed an issue where the Icon in the discord notifications wouldn't apply ([32da949](https://github.com/ExtremeFiretop/Reqestra/commit/32da949a9547f68c57eb4338f749228b7de167c2))
* **discover:** :bug: Created new Image component to handle 429's from TMDB ([#4698](https://github.com/ExtremeFiretop/Reqestra/issues/4698)) and fixed [#4635](https://github.com/ExtremeFiretop/Reqestra/issues/4635) ([#4699](https://github.com/ExtremeFiretop/Reqestra/issues/4699)) ([f22d3da](https://github.com/ExtremeFiretop/Reqestra/commit/f22d3da765799365455b919027f7563e52b347c3))
* **discover:** :bug: Display TV + movies on actor page in user language ([#4395](https://github.com/ExtremeFiretop/Reqestra/issues/4395)) ([fe635c7](https://github.com/ExtremeFiretop/Reqestra/commit/fe635c7106bc487ff879bdc8a73bab16cb389b97))
* **discover:** :bug: Fixed an issue where monitored movies in radarr were not correctly represented on the search results ([75b15bc](https://github.com/ExtremeFiretop/Reqestra/commit/75b15bc7cba21f0a14a18c8e64fd52482f5c6325))
* **discover:** :bug: Fixed the default poster not taking into account the base url in some scenarios [#4845](https://github.com/ExtremeFiretop/Reqestra/issues/4845) ([8eda250](https://github.com/ExtremeFiretop/Reqestra/commit/8eda250367953183daec03ccb5cdf9fe94275b27))
* **discover:** :bug: Fixed the issue where there was an option on the discover to request 4k shows (that's not supported currently) ([dcfd688](https://github.com/ExtremeFiretop/Reqestra/commit/dcfd688c8d2337e55fa9c6c33b7c3e80fc560cda))
* **discover:** :bug: Fixed up the discover lists, ([fc69158](https://github.com/ExtremeFiretop/Reqestra/commit/fc691585976893f69b5c4806147afbd68bdaeb61))
* **discover:** :bug: Made the poster images on discover the same size no matter how many results there are ([78b9eef](https://github.com/ExtremeFiretop/Reqestra/commit/78b9eef48322d44e6c393a8075d486580dfe4027))
* **discover:** :children_crossing: Improved the new Genre buttons, it now includes TV results ([b087d60](https://github.com/ExtremeFiretop/Reqestra/commit/b087d606ff36565208e564f8856903f2a4098db5))
* **discover:** 🌐 Localize episodes names in TV details ([#4467](https://github.com/ExtremeFiretop/Reqestra/issues/4467)) [skip ci] ([35806ea](https://github.com/ExtremeFiretop/Reqestra/commit/35806ea2d2c866d628cf08577026a02ab04e49d9))
* **discover:** Carousel touch not working when scrolling page and recommendations and similar movie navigation ([#4633](https://github.com/ExtremeFiretop/Reqestra/issues/4633)) ([d5ef1d5](https://github.com/ExtremeFiretop/Reqestra/commit/d5ef1d53e5f77d19dba8b8059c80b538a3e14f2a))
* **discover:** Fix cache mix up ([03d9422](https://github.com/ExtremeFiretop/Reqestra/commit/03d94220c7eaafb50c6c80a6ed1150794b873ac3))
* **discover:** Fix denied requests displayed as approved ([#4901](https://github.com/ExtremeFiretop/Reqestra/issues/4901)) ([1e87f20](https://github.com/ExtremeFiretop/Reqestra/commit/1e87f2010491b0f3fdda70d2b19d9afd94438df7))
* **discover:** Fix new trending feature detection ([6794b88](https://github.com/ExtremeFiretop/Reqestra/commit/6794b887f6544fb41528bdd9728b7824b65e47ee))
* **discover:** Improved rendering on the discover page ([ea00d6c](https://github.com/ExtremeFiretop/Reqestra/commit/ea00d6c12f4441da243287d0fbc706d66c0afd82))
* **discover:** TV shows now display on the Actor Pages ([#4388](https://github.com/ExtremeFiretop/Reqestra/issues/4388)) ([6b716e7](https://github.com/ExtremeFiretop/Reqestra/commit/6b716e722076e3d1e6bf2097c5263645d5ea9edf))
* **dvr:** reset auto-increment in a database-provider-aware way ([#5436](https://github.com/ExtremeFiretop/Reqestra/issues/5436)) ([968aeb3](https://github.com/ExtremeFiretop/Reqestra/commit/968aeb3ed851a07929c3859258a01a434d6bbd56)), closes [#5435](https://github.com/ExtremeFiretop/Reqestra/issues/5435) [#5435](https://github.com/ExtremeFiretop/Reqestra/issues/5435)
* **email-notifications:** :bug: Fixed the issue where legacy requests were showing broken poster images [#4452](https://github.com/ExtremeFiretop/Reqestra/issues/4452) ([0ece2fd](https://github.com/ExtremeFiretop/Reqestra/commit/0ece2fd6e0eb01e0b7d4d2a01e1a276c7a9c5a51))
* **emby:** :bug: Fixed an issue where we slightly broke the full sync ([332d934](https://github.com/ExtremeFiretop/Reqestra/commit/332d9344d002a5ffd5aeac516c7441dcdec52248))
* **emby:** :bug: Fixed an issue where we were not properly syncing episodes ([75529dd](https://github.com/ExtremeFiretop/Reqestra/commit/75529dd972c5102f3c5234a2acf6fe664a1bcfad))
* **emby:** :bug: Fixed the emby content sync [#4513](https://github.com/ExtremeFiretop/Reqestra/issues/4513) ([2927504](https://github.com/ExtremeFiretop/Reqestra/commit/2927504f0e0b4e7251e69b44e0e30c7ec9519980))
* **emby:** :bug: Fixed the emby content sync [#4513](https://github.com/ExtremeFiretop/Reqestra/issues/4513) ([bd441cb](https://github.com/ExtremeFiretop/Reqestra/commit/bd441cb54fd77d6befb03fae321dc36c29f0de2e))
* **emby:** :bug: Fixed where we didn't scan episodes for mixed content libraries ([b815853](https://github.com/ExtremeFiretop/Reqestra/commit/b81585363ff5fd8eb06a814f36f20692077cdb27))
* **emby:** :bug: Skip very large multipart episodes ([fb70aa1](https://github.com/ExtremeFiretop/Reqestra/commit/fb70aa16dea5e682a27878c0845d2ef2cf3e7b07))
* **emby/jellyfin:** :bug: A more reliable Emby and Jellyfin sync [skip ci] ([ad677fa](https://github.com/ExtremeFiretop/Reqestra/commit/ad677fa02eb75633014e9c9791c21ed2d6a23229))
* **emby/jellyfin:** :bug: Fixed another issue where we were not correctly displaying the correct status' for movies ([5c0556e](https://github.com/ExtremeFiretop/Reqestra/commit/5c0556e6f44b8997a611f3a4d8e9e4e05d08bd13))
* **emby/jellyfin:** fix [#5338](https://github.com/ExtremeFiretop/Reqestra/issues/5338) ([6217461](https://github.com/ExtremeFiretop/Reqestra/commit/62174617bbef0839527c179909cdbe50e90f5955))
* **emby:** actually fix the dupes ([4167942](https://github.com/ExtremeFiretop/Reqestra/commit/41679427f65cfa6756ab8bb1d89eb37cf8a40b42))
* **emby:** Add more logging on the PlaySync to check for Tv Shows without a valid TMDB ([08eb13b](https://github.com/ExtremeFiretop/Reqestra/commit/08eb13b788582d576a0e1befdb8e84ef7ff0d2f3))
* **emby:** Fix Emby played sync running a full sync during recently added sync ([#4932](https://github.com/ExtremeFiretop/Reqestra/issues/4932)) ([9424586](https://github.com/ExtremeFiretop/Reqestra/commit/9424586e9c1b622b6475aeb8ee3cf4a8f346da6e))
* **emby:** fixed duplicate episodes ([a1d3755](https://github.com/ExtremeFiretop/Reqestra/commit/a1d37554bdb9167dc82df121caa194e7641142df))
* **emby:** make episode sync pagination deterministic and harden multi-episode fill ([#5438](https://github.com/ExtremeFiretop/Reqestra/issues/5438)) ([20aecdd](https://github.com/ExtremeFiretop/Reqestra/commit/20aecdd1b7d9a00c65706ce240f9691774f5da6f))
* **emby:** remove stale records, harden played sync, keep provider-id-less series ([#5439](https://github.com/ExtremeFiretop/Reqestra/issues/5439)) ([fd44619](https://github.com/ExtremeFiretop/Reqestra/commit/fd44619d84dc4b88235590672673bc9b48531e38))
* extend request cache TTL and add null guards for TV results ([195c989](https://github.com/ExtremeFiretop/Reqestra/commit/195c9898ebcae816fed73ff12859b55b67f3def1))
* filter out excluded notification agents from user preferences ([c9ab4f4](https://github.com/ExtremeFiretop/Reqestra/commit/c9ab4f4f9faa66dbf263da693db1eefcf68beeec)), closes [#5196](https://github.com/ExtremeFiretop/Reqestra/issues/5196)
* Fix conflicting property name for Swagger ([#4733](https://github.com/ExtremeFiretop/Reqestra/issues/4733)) ([d661f32](https://github.com/ExtremeFiretop/Reqestra/commit/d661f32e8a9e105faab6380b4b7b642896b98163))
* Fix denied movie shown as 'processing request' in details view ([#4900](https://github.com/ExtremeFiretop/Reqestra/issues/4900)) ([0069bfd](https://github.com/ExtremeFiretop/Reqestra/commit/0069bfdf54e0785bad45c832ca052f19fd4b940b))
* fix swagger api key auth ([#4483](https://github.com/ExtremeFiretop/Reqestra/issues/4483)) [no ci] ([32ee4e8](https://github.com/ExtremeFiretop/Reqestra/commit/32ee4e88ec05bf610150933974408fbe95ccd7bd))
* Fix various styling issues ([#4935](https://github.com/ExtremeFiretop/Reqestra/issues/4935)) ([90b934a](https://github.com/ExtremeFiretop/Reqestra/commit/90b934a36996c0f489096f3641350a1c0d3b7c89))
* fixed an issue where I broke images for some users ([81ddc85](https://github.com/ExtremeFiretop/Reqestra/commit/81ddc8553b9094c3f6843b036daebb2eb9262e00))
* fixed build ([f877921](https://github.com/ExtremeFiretop/Reqestra/commit/f8779219146051ea74f8b6408658ff7975afb88b))
* fixed emails not being able to load the template ([6b49d9b](https://github.com/ExtremeFiretop/Reqestra/commit/6b49d9bc7108a0b663ca05de19dbf4841c9c43c1))
* fixed stats controller ([#4742](https://github.com/ExtremeFiretop/Reqestra/issues/4742)) ([47ea64b](https://github.com/ExtremeFiretop/Reqestra/commit/47ea64b5a401770f1943b575ca40f84d515e96b3))
* Fixed the issue where the login page is still present after logging in with oauth ([aca4ee3](https://github.com/ExtremeFiretop/Reqestra/commit/aca4ee37915a28200e5233be3dc711ccc4a5aee9))
* **Fixed the UI not applying the correct timezone settings:** :bug: ([029ea79](https://github.com/ExtremeFiretop/Reqestra/commit/029ea7919220fbc506898733caeb4370053051a7))
* fixed trakt image not loading when base url present ([#4711](https://github.com/ExtremeFiretop/Reqestra/issues/4711)) ([f102dcf](https://github.com/ExtremeFiretop/Reqestra/commit/f102dcf751c2eb62ebfe30f9f8e4b2ad863c3b0d))
* Fixes default image for recently requested items. ([#4767](https://github.com/ExtremeFiretop/Reqestra/issues/4767)) ([2e6f35f](https://github.com/ExtremeFiretop/Reqestra/commit/2e6f35f89abb3dd3685ec8289f8620c7ef7072cd))
* **frontend:** add link on logo text ([6d7274a](https://github.com/ExtremeFiretop/Reqestra/commit/6d7274a94ea24343f32fc028984bd3fd5d5f2ee3))
* guard Docker image push to master/develop branches only ([86427e5](https://github.com/ExtremeFiretop/Reqestra/commit/86427e57df1cf91dc68a911870939db9a208fae2))
* **healthchecks:** Removed redundant ping check ([1751305](https://github.com/ExtremeFiretop/Reqestra/commit/1751305064176d2c0135f867773ccc46b03915ec))
* **Hide music from navbar and request list when not enabled:** :bug: ([5123a76](https://github.com/ExtremeFiretop/Reqestra/commit/5123a76954e9f81d58c05e31afc7a29aec19cb7a))
* **http:** enable automatic decompression on OmbiClient ([#5410](https://github.com/ExtremeFiretop/Reqestra/issues/5410)) ([a002528](https://github.com/ExtremeFiretop/Reqestra/commit/a0025284926ebe41ca9ed730216969396f4978bc))
* **i18n:** keep paginator labels from rendering as translation keys ([0c17762](https://github.com/ExtremeFiretop/Reqestra/commit/0c17762d412e1132948339b5e523b06ee296ed00))
* **images:** Retry images with a backoff when we get a Too Many requests from TheMovieDb [#4685](https://github.com/ExtremeFiretop/Reqestra/issues/4685) ([3f1f35d](https://github.com/ExtremeFiretop/Reqestra/commit/3f1f35df3164db6739691cdda8f925c296239791))
* **importer:** 🐛 Allow you to only import Plex Admins without the Plex Users ([8c9ad9b](https://github.com/ExtremeFiretop/Reqestra/commit/8c9ad9b414fdc6c88bdb911d6057ae5d38783b98))
* Improve Swagger documentation ([#4652](https://github.com/ExtremeFiretop/Reqestra/issues/4652)) ([181892b](https://github.com/ExtremeFiretop/Reqestra/commit/181892bcfe88e6d76febf49ef57745d04552d08e))
* issue deletion failing due to foreign key constraint on IssueComments ([#5393](https://github.com/ExtremeFiretop/Reqestra/issues/5393)) ([bfd4166](https://github.com/ExtremeFiretop/Reqestra/commit/bfd4166578df3589a31b624644f35a74750095ce))
* **issues:** :bug: Added the issue category to the issue 'cards' [#4403](https://github.com/ExtremeFiretop/Reqestra/issues/4403) ([a3739f3](https://github.com/ExtremeFiretop/Reqestra/commit/a3739f375c49f48e34da12f0a74e4e068f12ab40))
* **issues:** :bug: Added the issues back to the details page for TV Shows ([0225000](https://github.com/ExtremeFiretop/Reqestra/commit/02250000c08a253e57d8a0a855c2d30b8a1e5baa))
* **issues:** :bug: Fixed an issue where you couldn't navigate to the details page from TV issues ([1a2825b](https://github.com/ExtremeFiretop/Reqestra/commit/1a2825bf3839b891b16e1dde4030afe53efe090e))
* **issues:** :bug: Fixed where we did not show the poster when an issue is raised for media we do not have a request for [#4402](https://github.com/ExtremeFiretop/Reqestra/issues/4402) ([15e37b5](https://github.com/ExtremeFiretop/Reqestra/commit/15e37b532a83097dbdf1a9fea3eead7d0e211898))
* **issues:** Fix label ID in chatbox page ([#4520](https://github.com/ExtremeFiretop/Reqestra/issues/4520)) ([76882ad](https://github.com/ExtremeFiretop/Reqestra/commit/76882adf231f92e1cdd396239933c13467c112b3))
* **jellyfin:** :bug: Fixed an issue where Jellyfin content was showing the Play on Emby button ([18b167d](https://github.com/ExtremeFiretop/Reqestra/commit/18b167d16a3d682b5060ee36dedbbb069bef09de)), closes [#4542](https://github.com/ExtremeFiretop/Reqestra/issues/4542)
* **jellyfin:** Fixed an issue where the sync could stop working. Removed unused properties so the deseralization no longer fails ([0e5e0ad](https://github.com/ExtremeFiretop/Reqestra/commit/0e5e0adf862701d0f672beff14ec0aa75e4b5220))
* Landing and Login page improvements ([#4690](https://github.com/ExtremeFiretop/Reqestra/issues/4690)) ([6d423b5](https://github.com/ExtremeFiretop/Reqestra/commit/6d423b5447c52c5e59d8d2bd92a23b47468eb736))
* **lidarr:** Change monitor to Existing to properly add artist [#3597](https://github.com/ExtremeFiretop/Reqestra/issues/3597) ([506f607](https://github.com/ExtremeFiretop/Reqestra/commit/506f60773bf1031d0be51ccd34289b855a04ea40)), closes [/github.com/Lidarr/Lidarr/issues/3597#issuecomment-1530804055](https://github.com//github.com/Lidarr/Lidarr/issues/3597/issues/issuecomment-1530804055)
* **localisation:** Localize request types in notifications ([#4516](https://github.com/ExtremeFiretop/Reqestra/issues/4516)) ([e09435d](https://github.com/ExtremeFiretop/Reqestra/commit/e09435da455b12fc429f129372de31e0654da797))
* Localize recently requested on discover page ([#4729](https://github.com/ExtremeFiretop/Reqestra/issues/4729)) ([bf65c76](https://github.com/ExtremeFiretop/Reqestra/commit/bf65c76ff9ce38f65a9e5feb872734e8d8e35eb6))
* Log Microsoft warnings to log file ([#4723](https://github.com/ExtremeFiretop/Reqestra/issues/4723)) ([26ac75f](https://github.com/ExtremeFiretop/Reqestra/commit/26ac75f0c223c2a91e3471797ae46ede3fde89cc))
* make discover page processing sequential to fix test failures ([aa087c7](https://github.com/ExtremeFiretop/Reqestra/commit/aa087c7b9b860b4c39283b54163c95da83d5b08a))
* **media-sync:** Add sanity checks upon media server sync ([#4493](https://github.com/ExtremeFiretop/Reqestra/issues/4493)) ([9915234](https://github.com/ExtremeFiretop/Reqestra/commit/9915234d38d4701c527081ccc21b566980375331)), closes [#4472](https://github.com/ExtremeFiretop/Reqestra/issues/4472)
* **mediaserver:** fixed an issue where we were not detecting available content correctly [#4542](https://github.com/ExtremeFiretop/Reqestra/issues/4542) ([9cdd6f4](https://github.com/ExtremeFiretop/Reqestra/commit/9cdd6f41cdab8825a984905c089611409c53c753))
* **mediaserver:** fixed some more issues in the media server sync and availability checks ([f3ea979](https://github.com/ExtremeFiretop/Reqestra/commit/f3ea979b8bd77842780ce8e6928b16237dd779cf))
* **metadata:** improved the metadata job to also lookup the media in Plex to see if it has any more uptodate metadata ([83d1a15](https://github.com/ExtremeFiretop/Reqestra/commit/83d1a15cc9d0ee91be73bd91c4672cf1bcf2728a))
* Missing Poster broken link fix ([#4637](https://github.com/ExtremeFiretop/Reqestra/issues/4637)) ([4070f0d](https://github.com/ExtremeFiretop/Reqestra/commit/4070f0d093b1c92487a1c80cabad8283a9650f51))
* More automation tests mainly around the Plex Settings page ([#4821](https://github.com/ExtremeFiretop/Reqestra/issues/4821)) ([21bfc5a](https://github.com/ExtremeFiretop/Reqestra/commit/21bfc5a45adf6da6a80854e19494a8ffdc9c0761))
* move API throttle inside cache factory to avoid contention on cache hits ([4e41ce6](https://github.com/ExtremeFiretop/Reqestra/commit/4e41ce6dcf9fc900a193338ac729be56d8ab28ce))
* **newsletter:** :bug: Fixed a few small bugs in the newsletter ([21dba4c](https://github.com/ExtremeFiretop/Reqestra/commit/21dba4c524b98b9f2b883d97e7e13329425a8762))
* **newsletter:** Fix newsletter not publishing double episodes ([#4495](https://github.com/ExtremeFiretop/Reqestra/issues/4495)) ([ddf63fb](https://github.com/ExtremeFiretop/Reqestra/commit/ddf63fbed0b9cbe458aec37318758c76b99b2de9))
* **notificaitons:** Add the RequestedByAlias field to the Notification Message ([7e9c8be](https://github.com/ExtremeFiretop/Reqestra/commit/7e9c8bec6b02bb4e11f8db50394e493d4dd07723))
* **notifications:** :bug: Fixed the DenyReason sometimes not appearing in the notification message [#4409](https://github.com/ExtremeFiretop/Reqestra/issues/4409) ([209e311](https://github.com/ExtremeFiretop/Reqestra/commit/209e31175c95f6ee8909d878d45bf8269a9842d9))
* **notifications:** :bug: This is a fix for some of the duplicate notification issues [#3825](https://github.com/ExtremeFiretop/Reqestra/issues/3825) ([22bb422](https://github.com/ExtremeFiretop/Reqestra/commit/22bb4226ead2d62e8c2c2c05be47d7da621402e2))
* **notifications:** 🐛 Do not notify user upon auto approval of a TV show ([#4432](https://github.com/ExtremeFiretop/Reqestra/issues/4432)) ([3ad3bdd](https://github.com/ExtremeFiretop/Reqestra/commit/3ad3bddd8313d607ee2a39a51a92e61a3673082c)), closes [#4431](https://github.com/ExtremeFiretop/Reqestra/issues/4431)
* **notifications:** fixed an error that could happen when Ombi sends out a issue notification ([7442dcf](https://github.com/ExtremeFiretop/Reqestra/commit/7442dcf59da5d2190cc3087b10402e85bcfcf83b))
* **notifications:** Fixed the error when sending multiple test notifications. Added more logging when Discord complains the message is invalid ([fc14780](https://github.com/ExtremeFiretop/Reqestra/commit/fc14780bd354483119ddcbb55a8c382e1890a783))
* **notifications:** Fixed the Partially TV notifications going to the admin [#4797](https://github.com/ExtremeFiretop/Reqestra/issues/4797) ([#4799](https://github.com/ExtremeFiretop/Reqestra/issues/4799)) ([bcb3e7f](https://github.com/ExtremeFiretop/Reqestra/commit/bcb3e7f00380a4c4278f59dc55febf43e6d05d47))
* **notifications:** harden legacy mobile recipient handling ([85caf3c](https://github.com/ExtremeFiretop/Reqestra/commit/85caf3cc3f7d54c6b0624a5d459133d011d3ab0d))
* **notifications:** Remove generic admin email in favour of admins' email ([#4519](https://github.com/ExtremeFiretop/Reqestra/issues/4519)) ([b90fc5f](https://github.com/ExtremeFiretop/Reqestra/commit/b90fc5fea771a83e6cf576c71a307066efd59ea4))
* **oauth:** :bug: Fixed an issue where on occasions the Plex OAuth wouldn't work ([ce1ed01](https://github.com/ExtremeFiretop/Reqestra/commit/ce1ed01a5f1a6e99d37a3cf32e53cf5de4f5943b))
* **oauth:** :lock: Fixed the issue where some users running their browsers in a different language could not open the Plex OAuth window ([d5404ea](https://github.com/ExtremeFiretop/Reqestra/commit/d5404eaad7837010d6e4563cd8f7a1009231d362)), closes [#4408](https://github.com/ExtremeFiretop/Reqestra/issues/4408)
* Only log error messages from Microsoft ([#4787](https://github.com/ExtremeFiretop/Reqestra/issues/4787)) ([c614e0c](https://github.com/ExtremeFiretop/Reqestra/commit/c614e0ca5fe5023cbe7ced326145273cd75be85d))
* Override Sonarr V3 Profiles endpoint ([#4678](https://github.com/ExtremeFiretop/Reqestra/issues/4678)) ([875da95](https://github.com/ExtremeFiretop/Reqestra/commit/875da959f353119b05138d68ee6d32a49e14b91e))
* parallelize discover page API calls and processing for faster load times ([c417eea](https://github.com/ExtremeFiretop/Reqestra/commit/c417eeaa68f313a37369726fa4917d4550a7fa71))
* Partially Available prevents further TV requests ([#4768](https://github.com/ExtremeFiretop/Reqestra/issues/4768)) ([#4779](https://github.com/ExtremeFiretop/Reqestra/issues/4779)) ([031e2b9](https://github.com/ExtremeFiretop/Reqestra/commit/031e2b9283b239827cabaca4e35f69f2f93a4d7b))
* **permissions:** :bug: Improved the security around the role "Manage Own Requests" ([#4397](https://github.com/ExtremeFiretop/Reqestra/issues/4397)) ([334a32b](https://github.com/ExtremeFiretop/Reqestra/commit/334a32bca42f90198d9b720d2bdb710a583be47f)), closes [#4391](https://github.com/ExtremeFiretop/Reqestra/issues/4391)
* **pipes:** update HumanizePipe signature and add empty value handling ([4b4dfe2](https://github.com/ExtremeFiretop/Reqestra/commit/4b4dfe2997cc089690a205f621da94514f4ed2af))
* **plex-api:** Switch over to the new API to avoid deprecation & save… ([#4986](https://github.com/ExtremeFiretop/Reqestra/issues/4986)) ([2f2d35e](https://github.com/ExtremeFiretop/Reqestra/commit/2f2d35ec867a8e5488e368db294bd37bcf92d843))
* **plex-api:** update Plex Watchlist URL ([11fd7a5](https://github.com/ExtremeFiretop/Reqestra/commit/11fd7a5fc853da75974a16bf4fdecd72a836f54b))
* **plex-oauth:** 🐛 Fixed an issue where using OAuth you could log in as a Ombi Local user [#4835](https://github.com/ExtremeFiretop/Reqestra/issues/4835) ([4098da3](https://github.com/ExtremeFiretop/Reqestra/commit/4098da305aaea9dae9a552644268a4fed7204cfe))
* **plex-watchlist:** :bug: Fixed the issue where the watchlist didn't work for users logging in via OAuth ([6398f6a](https://github.com/ExtremeFiretop/Reqestra/commit/6398f6a4f7755281ebeac537e3ff623df5cfa0f3))
* **plex-watchlist:** Index out of bounds error ([8cd556e](https://github.com/ExtremeFiretop/Reqestra/commit/8cd556e268931596b9c1d1ae0ce533bfaaf330f4))
* **plex-watchlist:** Lookup the ID from different sources when Plex doesn't contain the metadata ([#4843](https://github.com/ExtremeFiretop/Reqestra/issues/4843)) ([a2cc23b](https://github.com/ExtremeFiretop/Reqestra/commit/a2cc23b351c4a568c44e6c855f94db9f71ad084a))
* **plex-watchlist:** Only request the latest season when importing from the watchlist ([77a47ff](https://github.com/ExtremeFiretop/Reqestra/commit/77a47ff157c6c5feafe3f2a29a3fcba8df4fdfef))
* **plex:** :bug: Fixed not being able to enable watchlist requests in the Plex settings ([3e5158e](https://github.com/ExtremeFiretop/Reqestra/commit/3e5158ef9cda58ea2dd3be143f07aa5433691d79))
* **plex:** :bug: Fixed the issue where you couldn't add a new server on a fresh setup after the settings page rework ([187b18d](https://github.com/ExtremeFiretop/Reqestra/commit/187b18d5c01f6a13831e4a410b5d7c349e27d847))
* **plex:** 🐛 Fixed an issue with the Plex Sync ([ab1a11a](https://github.com/ExtremeFiretop/Reqestra/commit/ab1a11af78efbe9d37bd55aa80a640796c138a98))
* **plex:** Added the watchlist request whole show back into the settings ([10701c4](https://github.com/ExtremeFiretop/Reqestra/commit/10701c4a0b6190eebb75c5d8b18224f3d0bc8502))
* **plex:** content sync duplicate key errors ([cefe989](https://github.com/ExtremeFiretop/Reqestra/commit/cefe989d4efc9b2a686b50f47479d283992f9267))
* **plex:** Fixed an issue where in a rare case we couldn't sync the data [#4502](https://github.com/ExtremeFiretop/Reqestra/issues/4502) ([191318d](https://github.com/ExtremeFiretop/Reqestra/commit/191318ddad5a8148422955bf928f1c49b890e3eb))
* **plex:** Fixed an issue where sometimes the availability checker would throw an exception when checking episodes ([17ba202](https://github.com/ExtremeFiretop/Reqestra/commit/17ba2020ee0950c2c0e0e03fdb7835b579da75a9))
* **plex:** Fixed some errors around the scanner that was causing the scan to fail ([d9787dc](https://github.com/ExtremeFiretop/Reqestra/commit/d9787dc32aace808d196f6f87456ef45de3d7bbf))
* **plex:** migrate legacy numeric provider ids during watchlist import ([#5400](https://github.com/ExtremeFiretop/Reqestra/issues/5400)) ([fa6d304](https://github.com/ExtremeFiretop/Reqestra/commit/fa6d304a0329d7e3493aee9078046a6a87737241))
* **plex:** skip watchlist target when username collision is detected ([#5403](https://github.com/ExtremeFiretop/Reqestra/issues/5403)) ([90b386a](https://github.com/ExtremeFiretop/Reqestra/commit/90b386a38360b16b97da59fc1f50bf613d866b93))
* **plex:** stop the plex sync from deleting episodes when we can't find the plex key ([66b05e5](https://github.com/ExtremeFiretop/Reqestra/commit/66b05e5a85dbfe1fec5f9366e80987f2cfa1f4fe))
* **radarr-4k:** :bug: Fixed an issue where the overrides wouldn't work for 4k Requests ([0fb29a0](https://github.com/ExtremeFiretop/Reqestra/commit/0fb29a0b16b1fc87f71df1a589f6141324cf2f1b))
* **radarr-4k:** 🐛 Fixed an issue when using Radarr 4k with user set quality profiles. There are now user quality profiles for 4k profile [#5025](https://github.com/ExtremeFiretop/Reqestra/issues/5025) ([62b9a1f](https://github.com/ExtremeFiretop/Reqestra/commit/62b9a1f65fc4ee5f1eaf9dfabf37742a9007433e))
* **radarr-settings:** 🐛 Fixed a typo ([4a50a00](https://github.com/ExtremeFiretop/Reqestra/commit/4a50a00d4729d99f4359874b9af4dbc58a0c220b))
* **radarr-settings:** this.normalForm is undefined ([#5207](https://github.com/ExtremeFiretop/Reqestra/issues/5207)) ([dc2b958](https://github.com/ExtremeFiretop/Reqestra/commit/dc2b958915bf6cb77e093ada843ef6d9f62a3755)), closes [#4994](https://github.com/ExtremeFiretop/Reqestra/issues/4994)
* **radarr:** :bug: Enable validation on the radarr settings page ([0af3511](https://github.com/ExtremeFiretop/Reqestra/commit/0af3511e819d24e0f4edf6f33931e61bba743224))
* **radarr/sonarr:** :bug: Sanitize usernames when adding them as tags to Radarr/Sonarr [#5307](https://github.com/ExtremeFiretop/Reqestra/issues/5307) ([d3d1d38](https://github.com/ExtremeFiretop/Reqestra/commit/d3d1d380d5695fcd6d55239966bbf18a0082d961))
* **radarr:** ensure RequestedUser is loaded when creating tags ([f8658fe](https://github.com/ExtremeFiretop/Reqestra/commit/f8658fe6d56488aa5caa68093245cbf021a31810)), closes [#5045](https://github.com/ExtremeFiretop/Reqestra/issues/5045)
* **radarr:** Fixed an issue where the radarr sync would break ([de4baad](https://github.com/ExtremeFiretop/Reqestra/commit/de4baade9f87248d77106ff1a313a498870f4fb3))
* **radarr:** Fixed an issue where we couldn't sync radarr content [#4577](https://github.com/ExtremeFiretop/Reqestra/issues/4577) ([a5355a3](https://github.com/ExtremeFiretop/Reqestra/commit/a5355a3023e6900c4dd1b0da4722d7596c03907f))
* reduce cognitive complexity ([acc5bb8](https://github.com/ExtremeFiretop/Reqestra/commit/acc5bb8a06950bb9426e99a851824c8dd768de1d))
* Remove Angular TSLint ([#4973](https://github.com/ExtremeFiretop/Reqestra/issues/4973)) ([93969b5](https://github.com/ExtremeFiretop/Reqestra/commit/93969b5a2d82f442299bee418fae43cb590d7743))
* remove hard-coded item width for recently requested carousel ([5d79c80](https://github.com/ExtremeFiretop/Reqestra/commit/5d79c80462692a6d4d68f1469ec46e0d5f0bb994))
* remove negated condition ([6902895](https://github.com/ExtremeFiretop/Reqestra/commit/69028954021e09aaf95797746d24d4a8af754e8c))
* Remove old trending source ([#4987](https://github.com/ExtremeFiretop/Reqestra/issues/4987)) ([aacaa3e](https://github.com/ExtremeFiretop/Reqestra/commit/aacaa3e140b43f5d196da612f785cc4451717752))
* remove SemaphoreSlim throttle - unnecessary given TMDB rate limits ([feee9f8](https://github.com/ExtremeFiretop/Reqestra/commit/feee9f8514b07ddd877615eb42beedd470daeef5))
* remove sort header ([969bc7b](https://github.com/ExtremeFiretop/Reqestra/commit/969bc7bb25ea900ab9199509b079b36843e5bd6f))
* remove unsupported linux/arm/v7 platform from Docker build ([90d502f](https://github.com/ExtremeFiretop/Reqestra/commit/90d502f8c3359d0d09c229f501a6478abe81c230))
* **request-limits:** :bug: Fixed the issue where we were calculating Tv Request limit reset date incorrectly ([ceaec3f](https://github.com/ExtremeFiretop/Reqestra/commit/ceaec3feb0c9fbdab48595d7e425930a39d87ad5))
* **request-list:** :bug: Fixed an issue where the bulk delete would not work for movie requests ([4b540fb](https://github.com/ExtremeFiretop/Reqestra/commit/4b540fb45bcc389664f0953159802288d005db9f))
* **request-list:** :bug: Fixed an issue where the request options were not appearing for Music requests ([c0406a2](https://github.com/ExtremeFiretop/Reqestra/commit/c0406a2ddebafb03d98ed25cdf7d89dc9a600c7d))
* **requests:** :bug: Fixed an issue where you couldn't approve movies from the request list ([1611ef9](https://github.com/ExtremeFiretop/Reqestra/commit/1611ef9198befbb7a4db50a4f0953e50f29a788f))
* **requests:** :bug: Fixed the approve 4k option on the requests list not working as expected ([c0189da](https://github.com/ExtremeFiretop/Reqestra/commit/c0189dad478ea375beda61ba3bee3f029a39b8e5))
* **requests:** :bug: Fixed the issue where Approving a 4K Request wouldn't send it to the correct 4K radarr instance ([87cb990](https://github.com/ExtremeFiretop/Reqestra/commit/87cb9903db30e1dead25ee8c5ea34305eb084a03)), closes [#4509](https://github.com/ExtremeFiretop/Reqestra/issues/4509)
* **requests:** :bug: Fixed the issue where we could no longer approve TV Requests from the requests list ([19fe4e3](https://github.com/ExtremeFiretop/Reqestra/commit/19fe4e342efe5578c26ab8ba7ee2f2e64bbc9418))
* **requests:** :bug: Power users can now set profiles and root folders when requesting ([138df1e](https://github.com/ExtremeFiretop/Reqestra/commit/138df1eb25c709c1939d01d4c9f9ece63f8e0fde))
* **requests:** match loading placeholder to compact table view ([#5428](https://github.com/ExtremeFiretop/Reqestra/issues/5428)) ([4cc0302](https://github.com/ExtremeFiretop/Reqestra/commit/4cc03025190e03ec53db5af5f0f5a9c8ad42a767))
* reset sqlite_sequence after cache table deletes to prevent Int32 overflow ([#5398](https://github.com/ExtremeFiretop/Reqestra/issues/5398)) ([02e7c8e](https://github.com/ExtremeFiretop/Reqestra/commit/02e7c8e3c013b9c4620c381e143ecbded25f694d)), closes [#5224](https://github.com/ExtremeFiretop/Reqestra/issues/5224)
* resolve discover card details and action visibility on mobile touch devices ([#5442](https://github.com/ExtremeFiretop/Reqestra/issues/5442)) ([550af0c](https://github.com/ExtremeFiretop/Reqestra/commit/550af0c3d208d6e4d6d04c34831f72b0c02f67cb))
* resolve inconsistencies in view at 768px screen width ([7cd4225](https://github.com/ExtremeFiretop/Reqestra/commit/7cd42257a333ec6e62ffd603fcd3dac77c9fe4b8))
* resolve user data leakage in BCC mass email templates ([#5444](https://github.com/ExtremeFiretop/Reqestra/issues/5444)) [skip ci] ([a5cfb02](https://github.com/ExtremeFiretop/Reqestra/commit/a5cfb02d4ab973dd2183b39c3436a3d393818e2f))
* respect Sonarr availability priority for TV episodes ([#5445](https://github.com/ExtremeFiretop/Reqestra/issues/5445)) [skip ci] ([ddba9e3](https://github.com/ExtremeFiretop/Reqestra/commit/ddba9e3224201fd8a3e195e03e2e3df641973989))
* restore missing SearchViewModel using in Emby and Jellyfin rules ([afad1c9](https://github.com/ExtremeFiretop/Reqestra/commit/afad1c9890c30cb5483a9b6b8e61e2b96a345eaa))
* restore TransformMovieResultsToResponse to match original code ([5cd43e2](https://github.com/ExtremeFiretop/Reqestra/commit/5cd43e282e609be953c5b9527e901111bb9df24b))
* restore TV details hero Request button behavior ([#5396](https://github.com/ExtremeFiretop/Reqestra/issues/5396)) ([0804e2b](https://github.com/ExtremeFiretop/Reqestra/commit/0804e2b8556e190f70b7d8f48dcdf9c9a49827c0))
* Reworked the version check ([#4719](https://github.com/ExtremeFiretop/Reqestra/issues/4719)) ([#4781](https://github.com/ExtremeFiretop/Reqestra/issues/4781)) ([55855c5](https://github.com/ExtremeFiretop/Reqestra/commit/55855c5adda3cd1c51b7fbd0c19b469fc813f98e))
* run search rules sequentially to avoid DbContext thread-safety issues ([7172269](https://github.com/ExtremeFiretop/Reqestra/commit/7172269d1026a43a8cfcb87a29f1ccfd45bab8fd))
* **search:** :bug: Fixed an issue where changing the filter wouldn't refresh the search results ([10f5e05](https://github.com/ExtremeFiretop/Reqestra/commit/10f5e056c8257f8b8fe954bfbc70d3c3daa7a8e3))
* **search:** Fixed some cases where search wouldn't work correctly ([#4398](https://github.com/ExtremeFiretop/Reqestra/issues/4398)) ([4410790](https://github.com/ExtremeFiretop/Reqestra/commit/4410790bc096826bc11554098f846e3acb59589a))
* set MarkedAsApproved on TV requests ([57d3880](https://github.com/ExtremeFiretop/Reqestra/commit/57d3880115f8e65e7d7d522aaa725b01878b45fe))
* set PartlyAvailable when fully available with unaired episodes ([783620b](https://github.com/ExtremeFiretop/Reqestra/commit/783620bc7ea6b99e75480757ab8cdf6b1622fe62))
* Set the default job for the watchlist import to hourly instead of daily ([75906af](https://github.com/ExtremeFiretop/Reqestra/commit/75906af0adee3e3c68d825c3aaa8f7b918461b1f))
* **settings:** :bug: Fixed an issue where we were not displaying the excluded keyworks correctly in the TheMovieDbSettings page ([d3b3316](https://github.com/ExtremeFiretop/Reqestra/commit/d3b3316cbac18356b2f6b0912a3deb2c183e6534))
* **settings:** :bug: Fixed the issue where you couldn't save the Plex settings ([89a856a](https://github.com/ExtremeFiretop/Reqestra/commit/89a856a5d22a5f517702642876652d451f6e9b3e))
* **settings:** Allow toggling features when there are more than one ([a373359](https://github.com/ExtremeFiretop/Reqestra/commit/a373359ae8e6bad42b558a6e01a8ff2840d3bbaa))
* **settings:** resolve mobile dropdown positioning and horizontal layout overflow ([#5443](https://github.com/ExtremeFiretop/Reqestra/issues/5443)) ([1ba4825](https://github.com/ExtremeFiretop/Reqestra/commit/1ba48252b6e1b2adbb22ae1d44e81b637cf2877e))
* Show the ApiAlias in the requests-list ([9ff624c](https://github.com/ExtremeFiretop/Reqestra/commit/9ff624ce4646815b239fbb8327117947f0a90e4b))
* **sickrage:** Fixed issue with incorrect handling of SiCKRAGE episode results returned during episode status changes, now expects array of objects from data path if present ([#4648](https://github.com/ExtremeFiretop/Reqestra/issues/4648)) ([6d16442](https://github.com/ExtremeFiretop/Reqestra/commit/6d16442d4d714920367df065a3ced42b729f4233))
* small fix to search by decade ([7da1721](https://github.com/ExtremeFiretop/Reqestra/commit/7da1721a557e7aa2e92e760a7672b2dd2c88b060))
* Some minor tweaks to the movie info panel ([#4883](https://github.com/ExtremeFiretop/Reqestra/issues/4883)) ([1244487](https://github.com/ExtremeFiretop/Reqestra/commit/12444871df2f7602200f73971fce962f06b4a80b))
* **sonarr:** :bug: Added some more error handling and information around testing sonarr ([bd2c2d3](https://github.com/ExtremeFiretop/Reqestra/commit/bd2c2d3901e239393010fd582b207f1571fb4b7e)), closes [#4877](https://github.com/ExtremeFiretop/Reqestra/issues/4877)
* **sonarr:** :bug: Cleaned up and removed Sonarr v3 option, sonarr v3 is now the default. This allows us to get ready for the upcoming Sonarr v4 ([#4764](https://github.com/ExtremeFiretop/Reqestra/issues/4764)) ([2cddec7](https://github.com/ExtremeFiretop/Reqestra/commit/2cddec759004b6490f686ff74cb092238e3dc946))
* **sonarr:** :bug: Ensure we are monitoring shows that already exist in Sonarr [#5257](https://github.com/ExtremeFiretop/Reqestra/issues/5257) ([bf83c95](https://github.com/ExtremeFiretop/Reqestra/commit/bf83c95da05feefa956ea73a3959304af94483dd))
* **Sonarr:** :bug: Fixed an issue where some seasons were not being monitored correctly in sonarr ([60cfd41](https://github.com/ExtremeFiretop/Reqestra/commit/60cfd41f68e9006555c1a419dcff1aaa24b3e09f)), closes [#4506](https://github.com/ExtremeFiretop/Reqestra/issues/4506)
* **sonarr:** :bug: Fixed an issue where the language list didn't correctly load for power users in the advanced options [#4782](https://github.com/ExtremeFiretop/Reqestra/issues/4782) ([2173670](https://github.com/ExtremeFiretop/Reqestra/commit/217367047d1568070dd507e54ad3fd2c68f05b88))
* **sonarr:** :bug: Fixed an issue where we could attempt to add a series to sonarr before sonarr has got all the metadata [#4459](https://github.com/ExtremeFiretop/Reqestra/issues/4459) ([5c691dc](https://github.com/ExtremeFiretop/Reqestra/commit/5c691dc98437a4cd24560ff625414fe05dd22f89))
* **sonarr:** :bug: Fixed an issue where we were sometimes incorrectly setting the state of episodes that are already monitored in sonarr ([fd1acb8](https://github.com/ExtremeFiretop/Reqestra/commit/fd1acb88cbc5e73f91b7f81e6e28ee06d66b277e))
* **sonarr:** :bug: Improved the error handling in the sonarr settings page in the UI ([fcd78fe](https://github.com/ExtremeFiretop/Reqestra/commit/fcd78fee619d10ec7d78e8c8ec6c3ac4b0a361a1)), closes [#4877](https://github.com/ExtremeFiretop/Reqestra/issues/4877)
* **sonarr:** :bug: Sonarr V4 should work now ([#4810](https://github.com/ExtremeFiretop/Reqestra/issues/4810)) ([37655af](https://github.com/ExtremeFiretop/Reqestra/commit/37655aff9d3d133b42f5664bc9445d6571e966d6))
* **sonarr:** :bug: Stop the sonarr version endpoint from breaking when Sonarr is down [#4895](https://github.com/ExtremeFiretop/Reqestra/issues/4895) ([7bb8bec](https://github.com/ExtremeFiretop/Reqestra/commit/7bb8becfb140ef6012356752a71d53b5b404e482))
* **sonarr:** 🐛 Correctly monitor episodes ([57e7830](https://github.com/ExtremeFiretop/Reqestra/commit/57e7830f8e54b65ce3c1f0b122cbcc517d1af926))
* **sonarr:** fall back to IMDb when resolving TMDB IDs during sync ([57767a3](https://github.com/ExtremeFiretop/Reqestra/commit/57767a389fde029185a3d53862c2b6fe640bae12))
* **sonarr:** Fixed where requesting all seasons would only mark the latest as monitored [#4496](https://github.com/ExtremeFiretop/Reqestra/issues/4496) ([cfb85c2](https://github.com/ExtremeFiretop/Reqestra/commit/cfb85c23d77626b9ec1d99a6cf76497c438d0338))
* SonarrSync now deletes episodes per-series to prevent incomplete cache ([89b8c52](https://github.com/ExtremeFiretop/Reqestra/commit/89b8c5255b5f251d1eafc36df32b05fa6488657a)), closes [#5306](https://github.com/ExtremeFiretop/Reqestra/issues/5306)
* **sonarr:** V4 actually works this time around ([f62e70f](https://github.com/ExtremeFiretop/Reqestra/commit/f62e70fc493c7971da5e4508ce10522f5df0bbf7))
* src/Ombi.Notifications/Ombi.Notifications.csproj to reduce vulnerabilities ([#4969](https://github.com/ExtremeFiretop/Reqestra/issues/4969)) [skip ci] ([8584ad4](https://github.com/ExtremeFiretop/Reqestra/commit/8584ad46053c51f5da40b24f3efd1b9e5a031ddd))
* src/Ombi.Notifications/Ombi.Notifications.csproj to reduce vulnerabilities ([#5167](https://github.com/ExtremeFiretop/Reqestra/issues/5167)) ([e1f2a84](https://github.com/ExtremeFiretop/Reqestra/commit/e1f2a848065d79c8bba9eafff4f1f5db4a994b53))
* src/Ombi.Store/Ombi.Store.csproj to reduce vulnerabilities ([#5160](https://github.com/ExtremeFiretop/Reqestra/issues/5160)) ([9c21074](https://github.com/ExtremeFiretop/Reqestra/commit/9c2107418939ee92e50c59765481f30efac12eff))
* src/Ombi/ClientApp/package.json & src/Ombi/ClientApp/yarn.lock to reduce vulnerabilities ([e55de4e](https://github.com/ExtremeFiretop/Reqestra/commit/e55de4e4e5496bd61d3f80d9f2719b8df04b1d4b))
* src/Ombi/ClientApp/package.json & src/Ombi/ClientApp/yarn.lock to reduce vulnerabilities ([80c96a7](https://github.com/ExtremeFiretop/Reqestra/commit/80c96a77c9667fc56b8f8c2d79ce0cc0f75e53f6))
* src/Ombi/ClientApp/package.json & src/Ombi/ClientApp/yarn.lock to reduce vulnerabilities ([294b075](https://github.com/ExtremeFiretop/Reqestra/commit/294b075b4e0c0eec5693cc5a9af62e6f7cd65004))
* src/Ombi/ClientApp/package.json & src/Ombi/ClientApp/yarn.lock to reduce vulnerabilities ([2b27b0d](https://github.com/ExtremeFiretop/Reqestra/commit/2b27b0d65a21a29a5640551815079d6ccada6753))
* src/Ombi/ClientApp/package.json & src/Ombi/ClientApp/yarn.lock to reduce vulnerabilities ([#5010](https://github.com/ExtremeFiretop/Reqestra/issues/5010)) [skip ci] ([9c2e1b4](https://github.com/ExtremeFiretop/Reqestra/commit/9c2e1b435305d51cc9ab7f5d6932ccd3fa723e6c))
* src/Ombi/ClientApp/package.json & src/Ombi/ClientApp/yarn.lock to reduce vulnerabilities ([#5040](https://github.com/ExtremeFiretop/Reqestra/issues/5040))  [skip ci] ([955a742](https://github.com/ExtremeFiretop/Reqestra/commit/955a742fae1d0a3983c59cf77eb1a2d222f18b48))
* src/Ombi/ClientApp/package.json & src/Ombi/ClientApp/yarn.lock to reduce vulnerabilities ([#5072](https://github.com/ExtremeFiretop/Reqestra/issues/5072)) [skip ci] ([af6a986](https://github.com/ExtremeFiretop/Reqestra/commit/af6a9867719deb7b651a6a78352a8ce0df7a0cf0))
* src/Ombi/Ombi.csproj to reduce vulnerabilities ([#5066](https://github.com/ExtremeFiretop/Reqestra/issues/5066)) [skip ci] ([71df058](https://github.com/ExtremeFiretop/Reqestra/commit/71df05886512b8589f193a5cda0166c694438fc0))
* src/Ombi/package.json to reduce vulnerabilities ([2fe175b](https://github.com/ExtremeFiretop/Reqestra/commit/2fe175b165447fae1e09108bd82301660927b775))
* stop the TV discover endpoints duplicating episode data ([#5472](https://github.com/ExtremeFiretop/Reqestra/issues/5472)) ([#5473](https://github.com/ExtremeFiretop/Reqestra/issues/5473)) ([44e5749](https://github.com/ExtremeFiretop/Reqestra/commit/44e5749b2026bf73a6dffb605feadbfd94eac75f))
* Support duplicates in Emby/JF collections ([#4902](https://github.com/ExtremeFiretop/Reqestra/issues/4902)) ([141f96d](https://github.com/ExtremeFiretop/Reqestra/commit/141f96da5e45d5b3fa5f496806b102e473da6607))
* switch back to the old plex friends API [#4989](https://github.com/ExtremeFiretop/Reqestra/issues/4989) ([c8ad12e](https://github.com/ExtremeFiretop/Reqestra/commit/c8ad12eb5f53889609d1793ae907afd33ba6ef38))
* **sync:** Emby+Jellyfin - sync multi-episode files of 3+ episodes ([bd8fd89](https://github.com/ExtremeFiretop/Reqestra/commit/bd8fd890554c9d85d6da4d2cee813e82ce698e52))
* the post path on the requests page for TV ([089eef5](https://github.com/ExtremeFiretop/Reqestra/commit/089eef5e30e0e2dc2cdc13d709678c58ad0c0b92))
* **tmdb:** Fix rendering of the movie db page ([a0b84f7](https://github.com/ExtremeFiretop/Reqestra/commit/a0b84f74620f7e0b8c33a03fa3cc5f7d6d42ab93))
* **translations:** 🌐 Fix incorrect text translation reference RequestedByOn ([#4420](https://github.com/ExtremeFiretop/Reqestra/issues/4420)) ([202d155](https://github.com/ExtremeFiretop/Reqestra/commit/202d155493c29a6ddd4c5507186bf376a28f4c1d))
* **translations:** 🌐 Localization - Ensuring all of the app including backend are localized [#4366](https://github.com/ExtremeFiretop/Reqestra/issues/4366) ([5e140ab](https://github.com/ExtremeFiretop/Reqestra/commit/5e140ab6183b887a7665f5e870eb0bd05d487ace))
* **translations:** 🌐 New translations %two_letters_code% from Crowdin [skip ci] ([8fbd267](https://github.com/ExtremeFiretop/Reqestra/commit/8fbd267b516ddaa80fd16c091bae532b860fbf45))
* **translations:** 🌐 New translations en.json from Crowdin [skip ci] ([52eda6a](https://github.com/ExtremeFiretop/Reqestra/commit/52eda6ab917a73842bc02b0d8e0c442e564ca8f0))
* **translations:** 🌐 New translations en.json from Crowdin [skip ci] ([1095d52](https://github.com/ExtremeFiretop/Reqestra/commit/1095d524962648a1e427f0bcd8105fa734dd5b60))
* **translations:** 🌐 New translations from Crowdin ([5cfb76c](https://github.com/ExtremeFiretop/Reqestra/commit/5cfb76cad7a25eed8b452bf9c01cef8c32804369))
* **translations:** 🌐 New translations from Crowdin  ([8943a97](https://github.com/ExtremeFiretop/Reqestra/commit/8943a978bf459eaeb496d50c61c4d1506c727366))
* **translations:** 🌐 New translations from Crowdin  ([473c172](https://github.com/ExtremeFiretop/Reqestra/commit/473c1724922515fe376e0b2058ac391807c923f2))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([cb6e7ce](https://github.com/ExtremeFiretop/Reqestra/commit/cb6e7ce96cbe25a9ad02a69d710440d0a032b5b1))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([cbeb8a3](https://github.com/ExtremeFiretop/Reqestra/commit/cbeb8a325e56634695348b2737cd4796138e8342))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([116775e](https://github.com/ExtremeFiretop/Reqestra/commit/116775ecbccafdcb70948f4f464444d370ff2bfb))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([5a561c9](https://github.com/ExtremeFiretop/Reqestra/commit/5a561c974c8000cbb361ee35aa0091e1dea91ae2))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([8474267](https://github.com/ExtremeFiretop/Reqestra/commit/8474267fbd59e841597f62593db4401792c9f495))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([ff56bce](https://github.com/ExtremeFiretop/Reqestra/commit/ff56bce713cc1f3f021cb3de0fd61f4035ec0138))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([3cb52a2](https://github.com/ExtremeFiretop/Reqestra/commit/3cb52a27aa39c3972f855ee22307957cfd53609b))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([9446001](https://github.com/ExtremeFiretop/Reqestra/commit/94460017111d493e9678a11684d01c239fc97cf9))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([12c6c8a](https://github.com/ExtremeFiretop/Reqestra/commit/12c6c8a37d57375dc5520a66bf3cedb9ef5b57c5))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([b16dcef](https://github.com/ExtremeFiretop/Reqestra/commit/b16dcef1925ca087cc677873c7abae450f2f1d7f))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([f04cf8d](https://github.com/ExtremeFiretop/Reqestra/commit/f04cf8d8ce2d8899fefb2e55c5e5647d1da3ced4))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([f14ea54](https://github.com/ExtremeFiretop/Reqestra/commit/f14ea540912bc738d2cacef96a851424867cdb90))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([6ed8ebc](https://github.com/ExtremeFiretop/Reqestra/commit/6ed8ebc64bc4418d771f5dde734e9a8048d812a0))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([6aeeafb](https://github.com/ExtremeFiretop/Reqestra/commit/6aeeafb02fe8eb9f51de4f69bbda036889f4c226))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([cbf7421](https://github.com/ExtremeFiretop/Reqestra/commit/cbf742197286cef05d344112683f0d86c1620f08))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([9c785fb](https://github.com/ExtremeFiretop/Reqestra/commit/9c785fb7dcbcb43b26af5aabc8746cccd2c69744))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([32ecec1](https://github.com/ExtremeFiretop/Reqestra/commit/32ecec1cdf7b4858c0a37e9588f88cc9e0379bb3))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([7497cf2](https://github.com/ExtremeFiretop/Reqestra/commit/7497cf2b6d4c5ca1d8f140d3198b66e9b098030d))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([6d92d48](https://github.com/ExtremeFiretop/Reqestra/commit/6d92d4839889417e3fd3b2b95c82133ae4d17f7a))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([3236324](https://github.com/ExtremeFiretop/Reqestra/commit/3236324efd0c40c173738fe5fbd68f9f18e638cd))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([2e2aacd](https://github.com/ExtremeFiretop/Reqestra/commit/2e2aacd1767f6c6b7571f6d40e6454caa939f4a0))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([c282583](https://github.com/ExtremeFiretop/Reqestra/commit/c2825833c17b64b70ba5166ca2d5e34c2a35b783))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([102c84e](https://github.com/ExtremeFiretop/Reqestra/commit/102c84edb0df41f2f9ca27fd9d76f1e9cb22e755))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([dbbfdd9](https://github.com/ExtremeFiretop/Reqestra/commit/dbbfdd926f0808f6d16f0b2cd8b5406e6b610c82))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([53a6a09](https://github.com/ExtremeFiretop/Reqestra/commit/53a6a092b14b8b8bdbff95d066926d3dbe6951f4))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([0e8a64b](https://github.com/ExtremeFiretop/Reqestra/commit/0e8a64b8ca00d210fbe843ac2c3f6af218d80cbc))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([7b0ad61](https://github.com/ExtremeFiretop/Reqestra/commit/7b0ad61bfcff3986b33180dc64022cba7ea8eefb))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([4fc2c1f](https://github.com/ExtremeFiretop/Reqestra/commit/4fc2c1f24534085a783a3d5791f5533b68272153))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([76ab733](https://github.com/ExtremeFiretop/Reqestra/commit/76ab733b91791e4d93d184f3c7d0779c6a388695))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([06e4cef](https://github.com/ExtremeFiretop/Reqestra/commit/06e4cefa7b4e55b860da9a64f461f6ec8fa17367))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([c12d89d](https://github.com/ExtremeFiretop/Reqestra/commit/c12d89d6781a337520977ad285f8d08c93f434dd))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([bc0c2f6](https://github.com/ExtremeFiretop/Reqestra/commit/bc0c2f622e34fb5a2711039d9ed7aad34f982b15))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([e4b00e6](https://github.com/ExtremeFiretop/Reqestra/commit/e4b00e6b3468bd9389eeb02fc6ad7daf27abc3b3))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([d1998d3](https://github.com/ExtremeFiretop/Reqestra/commit/d1998d326f999a38586d0a351a20c5448df95842))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([bee4ccb](https://github.com/ExtremeFiretop/Reqestra/commit/bee4ccb804594e7385b1fbdc9fe2ef5c42e0d21f))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([80233ed](https://github.com/ExtremeFiretop/Reqestra/commit/80233ed560cc976e83570d0655c3472f20171fb3))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([8a78adc](https://github.com/ExtremeFiretop/Reqestra/commit/8a78adc9bb62f277f2b213dcb3847ed6d0089fcb))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([d04c60a](https://github.com/ExtremeFiretop/Reqestra/commit/d04c60aa5909b47ba6bffa6f66b03079cbd43521))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([92a785e](https://github.com/ExtremeFiretop/Reqestra/commit/92a785e736fa4b72a45270da2d0f4661df433078))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([634982d](https://github.com/ExtremeFiretop/Reqestra/commit/634982df2661cefab5ea9f5163fe04a005cc0171))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([b404baa](https://github.com/ExtremeFiretop/Reqestra/commit/b404baad6d0aeaa1561701e0db8db4e78613a364))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([d14f11e](https://github.com/ExtremeFiretop/Reqestra/commit/d14f11e0eb20ab0a68e765ee77968b3b3e54e995))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([7cf64f9](https://github.com/ExtremeFiretop/Reqestra/commit/7cf64f909d78908edaabeffb8a39a7d02e73fe7e))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([0c9e1ec](https://github.com/ExtremeFiretop/Reqestra/commit/0c9e1ec090827080cc8f7393e5e91456ff37d691))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([3b0b730](https://github.com/ExtremeFiretop/Reqestra/commit/3b0b730cb02efe24f6d4026e5fdb20d37e495119))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([6ed1a03](https://github.com/ExtremeFiretop/Reqestra/commit/6ed1a03b7ff4077f09ea9e13394b18b0d138f4c3))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([2941acd](https://github.com/ExtremeFiretop/Reqestra/commit/2941acd3b2ec74a5e6aeea275ab5a39d2653f37f))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([c075a1a](https://github.com/ExtremeFiretop/Reqestra/commit/c075a1a66784d975eaf60f2dfbbcbe048f2f63d7))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([76bd81c](https://github.com/ExtremeFiretop/Reqestra/commit/76bd81c3ca55a98c6ec944a838dc01294a6193a6))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([0d38275](https://github.com/ExtremeFiretop/Reqestra/commit/0d3827507e002bcf58f673e97ffcc3bd25dcf337))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([5c99601](https://github.com/ExtremeFiretop/Reqestra/commit/5c99601b07aec1a65d0186a4c4327440811e64c6))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([01546a0](https://github.com/ExtremeFiretop/Reqestra/commit/01546a0f7f86379528b486463246ef9bdfb9033e))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([d7fea78](https://github.com/ExtremeFiretop/Reqestra/commit/d7fea7843aaaab7ddff8dc31ca6d2a9117471dcc))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([1a6b95d](https://github.com/ExtremeFiretop/Reqestra/commit/1a6b95d45c220310213b8d811272a63f0f6ff42b))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([fa10174](https://github.com/ExtremeFiretop/Reqestra/commit/fa1017422c4efd4b0897871bd3c671151774d7c3))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([0c31e62](https://github.com/ExtremeFiretop/Reqestra/commit/0c31e628df376aac6d56ae67c7c705a9a4a7c080))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([6399643](https://github.com/ExtremeFiretop/Reqestra/commit/63996437a02fe10ffae6822ffa15369bec0a6b36))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([5826e2d](https://github.com/ExtremeFiretop/Reqestra/commit/5826e2d9a1c3f1210a87fa270dc0c81bac32944a))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([d434514](https://github.com/ExtremeFiretop/Reqestra/commit/d43451405be489254d7cdc7755d5f516a1e495a5))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([0b9596d](https://github.com/ExtremeFiretop/Reqestra/commit/0b9596d807178f5e071113ec0347868ec7f0960b))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([8c4c0b2](https://github.com/ExtremeFiretop/Reqestra/commit/8c4c0b262978c1303767af360d802c4b4c2b4d24))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([289ab77](https://github.com/ExtremeFiretop/Reqestra/commit/289ab77b0e04aae235b6f6cebc86e0a8d1f0cf2b))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([30e3417](https://github.com/ExtremeFiretop/Reqestra/commit/30e3417285a4eed18d429d7776f0e74096e834c0))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([6c0a5da](https://github.com/ExtremeFiretop/Reqestra/commit/6c0a5dadd4b8f37760252eb0fe7f88908f55506d))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([d5bf969](https://github.com/ExtremeFiretop/Reqestra/commit/d5bf9692ce1fc0ccfe7beca6dd200c78be177bdc))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([8a9e7ea](https://github.com/ExtremeFiretop/Reqestra/commit/8a9e7ea588aefbcd73ed82625887e3614e1703ea))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([01047a3](https://github.com/ExtremeFiretop/Reqestra/commit/01047a3fd67153f3ff16f860d2c7b50213e8d9b2))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([698a23f](https://github.com/ExtremeFiretop/Reqestra/commit/698a23fb83f323cdd1dd57cb49803079d44214a7))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([24eb842](https://github.com/ExtremeFiretop/Reqestra/commit/24eb842fc4424f7bcc3ec2949d7f5472492e96f6))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([ac8b16a](https://github.com/ExtremeFiretop/Reqestra/commit/ac8b16a3051ad71dbd54a8973c7dd847b564a515))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([f428ce6](https://github.com/ExtremeFiretop/Reqestra/commit/f428ce6a700c081437703839bc84d2f2b1138bcc))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([94b16df](https://github.com/ExtremeFiretop/Reqestra/commit/94b16dfe09bf1d2cd6286777d74eb5d4496abbbb))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([4881775](https://github.com/ExtremeFiretop/Reqestra/commit/4881775eda69a8f136ce0d8fbbf970e3d0406dc9))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([8297db9](https://github.com/ExtremeFiretop/Reqestra/commit/8297db91e85da308bde6fb09ad78347dee063630))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([d1152ab](https://github.com/ExtremeFiretop/Reqestra/commit/d1152ab7674243daa528c524c0cdc87d81ad49c9))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([eb2788b](https://github.com/ExtremeFiretop/Reqestra/commit/eb2788b761b55c487a59a049427ca08f6c10e836))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([21a794c](https://github.com/ExtremeFiretop/Reqestra/commit/21a794cbc0a5fa735ca0347c8f7f1ac04a487fbc))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([356c742](https://github.com/ExtremeFiretop/Reqestra/commit/356c7424e0ce8c1c5063b04bc6ed9b809f214d65))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([6fcaecf](https://github.com/ExtremeFiretop/Reqestra/commit/6fcaecf80b766f2d43ac7082d74364238e1638b7))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([132f4d4](https://github.com/ExtremeFiretop/Reqestra/commit/132f4d4e609b7fb7e37f38ee2f395926e2911abe))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([f292006](https://github.com/ExtremeFiretop/Reqestra/commit/f292006a08894a8d0ba899c8c6e9fe863e558dda))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([c6acb45](https://github.com/ExtremeFiretop/Reqestra/commit/c6acb45f8d3f371c0b4024c4272849d0d0cc867f))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([18c220a](https://github.com/ExtremeFiretop/Reqestra/commit/18c220a0cd0d19e45a07d0c319da2b9512778a8a))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([b0f3abb](https://github.com/ExtremeFiretop/Reqestra/commit/b0f3abb9ceebdbe5d6c20af98b7355df2999eb58))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([77d017b](https://github.com/ExtremeFiretop/Reqestra/commit/77d017b3d8ffd1714a2f6efecc8c900d56d062e4))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([f6e9784](https://github.com/ExtremeFiretop/Reqestra/commit/f6e9784367d3678d899ed79bef6caa52005b6661))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([601a877](https://github.com/ExtremeFiretop/Reqestra/commit/601a87762a2ad393ee5fa2fe52052ceeeefb1bef))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([a4a80ba](https://github.com/ExtremeFiretop/Reqestra/commit/a4a80ba4da49733a65e691003646c0f349bd4c5f))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([2961319](https://github.com/ExtremeFiretop/Reqestra/commit/2961319f61e95b2871480152b86ddca3375576a1))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([fc8d108](https://github.com/ExtremeFiretop/Reqestra/commit/fc8d108b660d53f499538328bfc271b05ac47d2b))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([1e03651](https://github.com/ExtremeFiretop/Reqestra/commit/1e03651c3b0eb77e45f9f6c55d31ee672eacd51e))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([c0dd327](https://github.com/ExtremeFiretop/Reqestra/commit/c0dd327426514e305a88750d7c3deb21c194108f))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([2156129](https://github.com/ExtremeFiretop/Reqestra/commit/2156129f175335746f204bb123035c070f518e96))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([aef0368](https://github.com/ExtremeFiretop/Reqestra/commit/aef0368de3aec306245bd1b16bc0de596a20d451))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([a38090b](https://github.com/ExtremeFiretop/Reqestra/commit/a38090b8dde17d1d150af0bca2830ea45d013a0e))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([c5f1d33](https://github.com/ExtremeFiretop/Reqestra/commit/c5f1d3355758a5c3648479d44e50397c7f6c1a9d))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([3846d56](https://github.com/ExtremeFiretop/Reqestra/commit/3846d56a6e561a1b1dc65c385151d90fdd6217ee))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([dafe9c1](https://github.com/ExtremeFiretop/Reqestra/commit/dafe9c1a19d84f00c13f0a51ba90927c24282926))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([edb418a](https://github.com/ExtremeFiretop/Reqestra/commit/edb418a6f05887c68a0c24c48decc691996f97e4))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([dadabf9](https://github.com/ExtremeFiretop/Reqestra/commit/dadabf93e1582a0c39321fd9bf3de3fb11e3f406))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([#4526](https://github.com/ExtremeFiretop/Reqestra/issues/4526)) ([7e9f54f](https://github.com/ExtremeFiretop/Reqestra/commit/7e9f54fc80a09c938184e6be40ce5f49ce9673ef))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([#4713](https://github.com/ExtremeFiretop/Reqestra/issues/4713)) ([ff142b0](https://github.com/ExtremeFiretop/Reqestra/commit/ff142b09abbb2f9540387284222552e6e12639fe))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([#4801](https://github.com/ExtremeFiretop/Reqestra/issues/4801)) ([4692003](https://github.com/ExtremeFiretop/Reqestra/commit/46920032baed04675b2ffbe1700afdc0740a4ac4))
* **translations:** 🌐 New translations from Crowdin [skip ci] ([#4926](https://github.com/ExtremeFiretop/Reqestra/issues/4926)) ([151efe1](https://github.com/ExtremeFiretop/Reqestra/commit/151efe19d0012b85f317c175da5ab4802ea14e20))
* try and fix the artifact upload ([fb9ff4c](https://github.com/ExtremeFiretop/Reqestra/commit/fb9ff4ccde9121dce6da379198de225686123457))
* **tv:** avoid unsafe TMDB episode dedupe in Sonarr cache ([a42c55e](https://github.com/ExtremeFiretop/Reqestra/commit/a42c55eefb1e6b4140037091af77de68df3087ef))
* **tv:** derive episode availability from Sonarr file state ([f43467c](https://github.com/ExtremeFiretop/Reqestra/commit/f43467cbaf136be7cc3eb78d31f633f7081161c7))
* **tv:** Display TV show as requested if all episodes are requested ([#4518](https://github.com/ExtremeFiretop/Reqestra/issues/4518)) ([2ed8c48](https://github.com/ExtremeFiretop/Reqestra/commit/2ed8c48d128a69f0d144c5d332286dbf3b0bdf28))
* **tv:** don't mark unaired shows as available ([#5434](https://github.com/ExtremeFiretop/Reqestra/issues/5434)) ([782896a](https://github.com/ExtremeFiretop/Reqestra/commit/782896af65b511b612da28b5142e3051c84d53fe)), closes [#5433](https://github.com/ExtremeFiretop/Reqestra/issues/5433)
* **tv:** harden request identity and availability handling ([3b4126a](https://github.com/ExtremeFiretop/Reqestra/commit/3b4126a76854da0679bcd9e05915954282bfaf4f))
* **tv:** limit episode metadata fetches to requested seasons ([a800758](https://github.com/ExtremeFiretop/Reqestra/commit/a8007585fef081e3035b34deeb7c690d56b42452))
* **tv:** make request dedupe and parent attachment anthology-aware ([531b58a](https://github.com/ExtremeFiretop/Reqestra/commit/531b58a65491d25703420cb3da087c8efcb7ac0c))
* **tv:** materialize TV request search before filtering to avoid EF Core 500 ([#5421](https://github.com/ExtremeFiretop/Reqestra/issues/5421)) ([2f5cdd1](https://github.com/ExtremeFiretop/Reqestra/commit/2f5cdd160e02f399ba8a63db20e4ac2988291bd5)), closes [#5420](https://github.com/ExtremeFiretop/Reqestra/issues/5420)
* **tv:** prevent duplicate watchlist requests across provider ID changes ([4f2b833](https://github.com/ExtremeFiretop/Reqestra/commit/4f2b833c983cac8bba9853e3737f3436e37e6fa6))
* **tv:** prevent zero-ID Sonarr matches and remap Plex anthology seasons ([0cb757e](https://github.com/ExtremeFiretop/Reqestra/commit/0cb757e2f88032527c49866f1302f07a4d3d74a7))
* **tv:** repair missing TVDB IDs using existing Sonarr series ([2555364](https://github.com/ExtremeFiretop/Reqestra/commit/255536490b6f4681329b31f97cd00d0ecb28ccf7))
* **tv:** resolve existing requests across provider ID changes ([c8a6ab5](https://github.com/ExtremeFiretop/Reqestra/commit/c8a6ab5c40e349732cb21b28cb45111fde17b0de))
* **tv:** resolve missing provider IDs before availability checks ([963ad72](https://github.com/ExtremeFiretop/Reqestra/commit/963ad72731e4b76e14c6b7f9087c88b4d3708005))
* **tv:** scope media-server episode availability to matched series ([fcb085f](https://github.com/ExtremeFiretop/Reqestra/commit/fcb085f3cdd20f95110fff6bd033009f99404e75))
* **ui:** correct timezone handling in OmbiDatePipe ([f88c5ad](https://github.com/ExtremeFiretop/Reqestra/commit/f88c5ad818fadea7064e7dfbe46f07eae855109a)), closes [#5102](https://github.com/ExtremeFiretop/Reqestra/issues/5102)
* Unable to Delete Jellyfin Server ([#4705](https://github.com/ExtremeFiretop/Reqestra/issues/4705)) ([#4780](https://github.com/ExtremeFiretop/Reqestra/issues/4780)) ([76a0d0d](https://github.com/ExtremeFiretop/Reqestra/commit/76a0d0d26893bd480fea4735f77522ac6261a425))
* update actions/checkout to v4 in docker job ([d8c2042](https://github.com/ExtremeFiretop/Reqestra/commit/d8c2042e037cbaf73c4064eb57be13cbebbc5066))
* update Dockerfile COPY commands to match current project structure ([3d67329](https://github.com/ExtremeFiretop/Reqestra/commit/3d6732919d8208d7552df25f6d4f4e700a825cd1))
* upgrade @fortawesome/fontawesome-free from 6.1.2 to 6.4.0 ([#4965](https://github.com/ExtremeFiretop/Reqestra/issues/4965)) [skip ci] ([84454e5](https://github.com/ExtremeFiretop/Reqestra/commit/84454e53c00c808e8a393c7750bdc418a7593e91))
* upgrade @fortawesome/fontawesome-free from 6.4.0 to 6.4.2 ([#5005](https://github.com/ExtremeFiretop/Reqestra/issues/5005)) [skip ci] ([f703ff2](https://github.com/ExtremeFiretop/Reqestra/commit/f703ff255cf389a60bdece824214d38f57f03f90))
* upgrade @fortawesome/fontawesome-free from 6.4.2 to 6.5.0 ([#5053](https://github.com/ExtremeFiretop/Reqestra/issues/5053))  [skip ci] ([5017e38](https://github.com/ExtremeFiretop/Reqestra/commit/5017e38f87e32821adb744935fffcb2d76927e2c))
* upgrade @microsoft/signalr from 6.0.11 to 6.0.16 ([#4964](https://github.com/ExtremeFiretop/Reqestra/issues/4964)) [skip ci] ([a0201e3](https://github.com/ExtremeFiretop/Reqestra/commit/a0201e3f585dc52f717e33c46ede35a4eccac736))
* upgrade @microsoft/signalr from 6.0.18 to 6.0.20 ([#4999](https://github.com/ExtremeFiretop/Reqestra/issues/4999)) [skip ci] ([563a044](https://github.com/ExtremeFiretop/Reqestra/commit/563a0443ea09ff71c5aa740173b4f3c5627cb543))
* upgrade @microsoft/signalr from 6.0.21 to 6.0.22 ([#5020](https://github.com/ExtremeFiretop/Reqestra/issues/5020)) ([1261a44](https://github.com/ExtremeFiretop/Reqestra/commit/1261a446e7ffa4d7540623f76ecc9d7e643ca4ce))
* upgrade @microsoft/signalr from 6.0.22 to 6.0.23 ([#5032](https://github.com/ExtremeFiretop/Reqestra/issues/5032)) [skip ci] ([11edac9](https://github.com/ExtremeFiretop/Reqestra/commit/11edac961b0b828658be83a9c4e67079345d3b0b))
* upgrade @types/jquery from 3.5.16 to 3.5.17 ([#5011](https://github.com/ExtremeFiretop/Reqestra/issues/5011)) [skip ci] ([40ee175](https://github.com/ExtremeFiretop/Reqestra/commit/40ee175ccd6e53b1254833163954b6a1be0d5251))
* upgrade @types/jquery from 3.5.18 to 3.5.19 ([#5022](https://github.com/ExtremeFiretop/Reqestra/issues/5022)) ([291425e](https://github.com/ExtremeFiretop/Reqestra/commit/291425e6091d90ac07010ff65cc3a53309965546))
* upgrade @types/jquery from 3.5.22 to 3.5.23 ([#5034](https://github.com/ExtremeFiretop/Reqestra/issues/5034)) [skip ci] ([9e28879](https://github.com/ExtremeFiretop/Reqestra/commit/9e28879fc1a479e1bd77ca003e3e748fec27f081))
* upgrade @types/jquery from 3.5.27 to 3.5.28 ([#5049](https://github.com/ExtremeFiretop/Reqestra/issues/5049))  [skip ci] ([2c8fe80](https://github.com/ExtremeFiretop/Reqestra/commit/2c8fe8087aea227e7425e82392ad9ccb3f8261b4))
* upgrade cypress-real-events from 1.10.0 to 1.10.1 ([#5014](https://github.com/ExtremeFiretop/Reqestra/issues/5014)) ([ed06c22](https://github.com/ExtremeFiretop/Reqestra/commit/ed06c22fb26fb605b857f3f8e1b26b2dbab15710))
* upgrade cypress-real-events from 1.7.4 to 1.8.1 ([#4968](https://github.com/ExtremeFiretop/Reqestra/issues/4968)) [skip ci] ([8a24b56](https://github.com/ExtremeFiretop/Reqestra/commit/8a24b56299c3bc98bf0d719ba448972aaa7f7461))
* upgrade cypress-real-events from 1.8.1 to 1.9.1 ([#5000](https://github.com/ExtremeFiretop/Reqestra/issues/5000)) [skip ci] ([19e0a88](https://github.com/ExtremeFiretop/Reqestra/commit/19e0a886ced344bfe6284f3916fba12826b7de08))
* upgrade jquery from 3.6.1 to 3.7.0 ([#4974](https://github.com/ExtremeFiretop/Reqestra/issues/4974)) ([f2552ef](https://github.com/ExtremeFiretop/Reqestra/commit/f2552ef6ede011080a8d5499e11930c4d41d04c2))
* upgrade jquery from 3.7.0 to 3.7.1 ([#5015](https://github.com/ExtremeFiretop/Reqestra/issues/5015)) ([7bc915c](https://github.com/ExtremeFiretop/Reqestra/commit/7bc915cc14ed27ad0f35142279f9362e5b7b27b8))
* upgrade moment from 2.29.4 to 2.30.1 ([#5075](https://github.com/ExtremeFiretop/Reqestra/issues/5075)) [skip ci] ([460fa39](https://github.com/ExtremeFiretop/Reqestra/commit/460fa39bb95c73bafcd65fcc394fecca04d3ac49))
* upgrade multiple dependencies with Snyk ([#4961](https://github.com/ExtremeFiretop/Reqestra/issues/4961)) ([3c3edf6](https://github.com/ExtremeFiretop/Reqestra/commit/3c3edf6273fa98c420989ebcebfee52b2545e402))
* upgrade multiple dependencies with Snyk ([#4963](https://github.com/ExtremeFiretop/Reqestra/issues/4963)) [skip ci] ([6025c5e](https://github.com/ExtremeFiretop/Reqestra/commit/6025c5ed757438d3a5d79bd36fd789ef0297ce70))
* upgrade multiple dependencies with Snyk ([#5030](https://github.com/ExtremeFiretop/Reqestra/issues/5030)) ([7e1e254](https://github.com/ExtremeFiretop/Reqestra/commit/7e1e254cfe6c84c1f143388f436d63efd4686e55))
* upgrade multiple dependencies with Snyk ([#5073](https://github.com/ExtremeFiretop/Reqestra/issues/5073)) [skip ci] ([a27b459](https://github.com/ExtremeFiretop/Reqestra/commit/a27b4592471c58fca9ad5193b979171fa7c5e66d))
* upgrade multiple dependencies with Snyk ([#5104](https://github.com/ExtremeFiretop/Reqestra/issues/5104)) [skip ci] ([a1083f6](https://github.com/ExtremeFiretop/Reqestra/commit/a1083f67c73c968b9ff0a0feebe5a9aac6a7c7c9))
* upgrade primeng from 15.0.0-rc.1 to 15.4.1 ([#4962](https://github.com/ExtremeFiretop/Reqestra/issues/4962)) [skip ci] ([23a4fed](https://github.com/ExtremeFiretop/Reqestra/commit/23a4fede69898a25b342aed78a8cda553c1fd18d))
* upgrade zone.js from 0.11.8 to 0.13.0 ([#4975](https://github.com/ExtremeFiretop/Reqestra/issues/4975)) ([37f6564](https://github.com/ExtremeFiretop/Reqestra/commit/37f65648a2f8742020b0954acec4168aee048942))
* upgrade zone.js from 0.13.1 to 0.13.2 ([#5019](https://github.com/ExtremeFiretop/Reqestra/issues/5019)) ([c5c8dda](https://github.com/ExtremeFiretop/Reqestra/commit/c5c8dda7e4f744fa47841efba9f0e8fee9ef67c6))
* use DateTime.Now.Date consistently in AvailabilityRuleHelper ([59dca57](https://github.com/ExtremeFiretop/Reqestra/commit/59dca5799ba1701ae06d5762a23799c7a60495de))
* Use new server discovery url [#5260](https://github.com/ExtremeFiretop/Reqestra/issues/5260) ([56a91d6](https://github.com/ExtremeFiretop/Reqestra/commit/56a91d6240f53a4306fcd0648575976f9d459048))
* **user-import:** Do not import users that do not have access to the server [#5064](https://github.com/ExtremeFiretop/Reqestra/issues/5064) ([a801cfd](https://github.com/ExtremeFiretop/Reqestra/commit/a801cfdb0946cbee3c35b7e917a240f69020f221))
* **user-importer:** Do not delete the Plex Admin as part of the user Importer cleanup [#4870](https://github.com/ExtremeFiretop/Reqestra/issues/4870) ([#4981](https://github.com/ExtremeFiretop/Reqestra/issues/4981)) ([4e80e7b](https://github.com/ExtremeFiretop/Reqestra/commit/4e80e7b7c3239a46a645ab6d1054993734ad4dd6))
* **user-importer:** don't delete admins in the cleanup ([895b9bf](https://github.com/ExtremeFiretop/Reqestra/commit/895b9bf6a060a678d4b0cca8083aa96c38e47b95))
* **user-importer:** Fixed not importing all correct users [#4989](https://github.com/ExtremeFiretop/Reqestra/issues/4989) ([34c32f8](https://github.com/ExtremeFiretop/Reqestra/commit/34c32f8338705ea3f790d95b91c9ada21a41b9f2))
* **user-management:** :bug: Fixed an issue where the Copy users App Link did not generate the correct app link for that user ([8cafcdc](https://github.com/ExtremeFiretop/Reqestra/commit/8cafcdcc3baa85c55d75e43835b2289bbea69b0e))
* **user-management:** :bug: Fixed an issue where you couldn't 'unset' a users custom quality and root folders ([bae96af](https://github.com/ExtremeFiretop/Reqestra/commit/bae96af17f50a80ae3ade235a5ef68d5d2dc12ba))
* **user-management:** Put back the user filter ([cb63060](https://github.com/ExtremeFiretop/Reqestra/commit/cb63060c778011927dc280f47f8b1836b6739150))
* **watchlist:** actually fixed it this time... ([d962a32](https://github.com/ExtremeFiretop/Reqestra/commit/d962a3211eca29520662ddce962676e3aea17ec5))
* **watchlist:** debounce Plex watchlist history pruning to stop re-requests ([#5427](https://github.com/ExtremeFiretop/Reqestra/issues/5427)) ([#5440](https://github.com/ExtremeFiretop/Reqestra/issues/5440)) ([05bf8f8](https://github.com/ExtremeFiretop/Reqestra/commit/05bf8f8693e811377cdbea0f1c84da6eb40da912))
* **watchlist:** only prune Plex watchlist history on a complete sync ([#5429](https://github.com/ExtremeFiretop/Reqestra/issues/5429)) ([186978c](https://github.com/ExtremeFiretop/Reqestra/commit/186978c2bfff8b2f481639ca49a3cbd19a23ce38))
* **webhook:** Remove added trailing slash from webhook URL [#4710](https://github.com/ExtremeFiretop/Reqestra/issues/4710) ([369eb33](https://github.com/ExtremeFiretop/Reqestra/commit/369eb339171671101be219486e2aab27a20f3d74))
* window size check ([4fd47c1](https://github.com/ExtremeFiretop/Reqestra/commit/4fd47c18b57793303dc4c75c7fd6acb15173e064))
* **wizard:** :bug: Fixed the issue where the Application Url wasn't validated in the wizard ([33b8d11](https://github.com/ExtremeFiretop/Reqestra/commit/33b8d1111a1c6663d8c0bbd912be4660da7d013f)), closes [#4417](https://github.com/ExtremeFiretop/Reqestra/issues/4417)
* **wizard:** :bug: Stop access to the wizard when you have already setup ombi ([#4866](https://github.com/ExtremeFiretop/Reqestra/issues/4866)) ([353de98](https://github.com/ExtremeFiretop/Reqestra/commit/353de981a462e1753288d225ec4644a44a62d2bc))
* **wizard:** Fixed an issue when using Plex OAuth it could fail setting up ([b743cf4](https://github.com/ExtremeFiretop/Reqestra/commit/b743cf4fafa7341ad1b163276f006d7ab0e9dcff))


### Features

* :sparkles: Added the ability to provide your own custom plex url ([6c6b7bb](https://github.com/ExtremeFiretop/Reqestra/commit/6c6b7bb498f142002e5b407a30896f4aa91a7c6c))
* :sparkles: Added the ability to specify which branch you are on ([61f3e94](https://github.com/ExtremeFiretop/Reqestra/commit/61f3e94308fb3d239140b73d34c12f1496459989))
* :sparkles: Added the ability to specify which branch you are on ([777ed2f](https://github.com/ExtremeFiretop/Reqestra/commit/777ed2f3224b91a0acb47c30efa96adaaba46fd2))
* :sparkles: Upgrade Ombi to .NET 6 ([#4390](https://github.com/ExtremeFiretop/Reqestra/issues/4390)) ([719eb7d](https://github.com/ExtremeFiretop/Reqestra/commit/719eb7dbe37b3a72d264e2f670067518eef70694)), closes [#4392](https://github.com/ExtremeFiretop/Reqestra/issues/4392)
* ✨ Recently Requested on Discover Page ([#4387](https://github.com/ExtremeFiretop/Reqestra/issues/4387)) ([44d38fb](https://github.com/ExtremeFiretop/Reqestra/commit/44d38fbaae521dbb467b61c7471b2384015ac52e))
* Add Auto Approve 4K role ([#4982](https://github.com/ExtremeFiretop/Reqestra/issues/4982)) ([#4983](https://github.com/ExtremeFiretop/Reqestra/issues/4983)) ([ac05495](https://github.com/ExtremeFiretop/Reqestra/commit/ac054954254b9d77a42e057f1065570c7fdc1093)), closes [#4957](https://github.com/ExtremeFiretop/Reqestra/issues/4957)
* add auto-update settings UI and improve updater configuration ([#1460](https://github.com/ExtremeFiretop/Reqestra/issues/1460)) ([#5394](https://github.com/ExtremeFiretop/Reqestra/issues/5394)) ([ba9f972](https://github.com/ExtremeFiretop/Reqestra/commit/ba9f9727e527e6e7631b6d87b90f248e2de4ffe4))
* add crew on movie page ([#4722](https://github.com/ExtremeFiretop/Reqestra/issues/4722)) ([1d53261](https://github.com/ExtremeFiretop/Reqestra/commit/1d532613823804b25984bd1d223d081a54ad143d))
* add Docker image publishing to CI pipeline ([2268f7b](https://github.com/ExtremeFiretop/Reqestra/commit/2268f7b8f4b72001c1821e892d1f43e09af5e4fe))
* Add Ntfy ([dfcea55](https://github.com/ExtremeFiretop/Reqestra/commit/dfcea555887efdff4311d2a5734561ab7bbd9380))
* Add the option for header authentication to create users ([#4841](https://github.com/ExtremeFiretop/Reqestra/issues/4841)) ([e6c9ce5](https://github.com/ExtremeFiretop/Reqestra/commit/e6c9ce5ad0056608ecda8273fb8124ed292e2942))
* Added the ability for the Watchlist to automatically refresh the users token. This will reduce the need for the user to log in ([067c029](https://github.com/ExtremeFiretop/Reqestra/commit/067c029f42e9fd853d060fdb2093013b15ac14c0))
* added the watchlist notification ([0dfd453](https://github.com/ExtremeFiretop/Reqestra/commit/0dfd4533dba01cb04ea2217c020de8833ddf39c6))
* Adding postgres support to ombi (beta) ([#5050](https://github.com/ExtremeFiretop/Reqestra/issues/5050)) ([f8c6102](https://github.com/ExtremeFiretop/Reqestra/commit/f8c61027bf53d657d7955a98b69d7ab90b66a75a))
* allow admins to force Plex watchlist revalidation ([4fa17a8](https://github.com/ExtremeFiretop/Reqestra/commit/4fa17a8f8155a6eeb40840444eca01a96f0134e0))
* Angular 15 and Dependency upgrades ([#4818](https://github.com/ExtremeFiretop/Reqestra/issues/4818)) ([4816acf](https://github.com/ExtremeFiretop/Reqestra/commit/4816acf6f94443d23ebef6091d4cfcbca580f9ca))
* **availability:** :sparkles: Add the option for the *arr to take media availability priority ([d43a2e4](https://github.com/ExtremeFiretop/Reqestra/commit/d43a2e4efcbf9ea55e25305a52657dc107acaee3)), closes [#5286](https://github.com/ExtremeFiretop/Reqestra/issues/5286)
* centre requested carousel items ([01c4fa9](https://github.com/ExtremeFiretop/Reqestra/commit/01c4fa916f5e6cc57cea459ad4300443ba457caa))
* **customization:** :sparkles: Added possibility for custom favicons ([40af659](https://github.com/ExtremeFiretop/Reqestra/commit/40af6593b668d4712327c18f92f5b7b5a0a65e26))
* **details:** :sparkles: Added the notify button back into the details pages for requests ([8b33cdc](https://github.com/ExtremeFiretop/Reqestra/commit/8b33cdccef83db8794414b247438214b00860fac))
* **discover:** :lipstick: Moved the advanced search to the nav bar for better discovery ([f83abaf](https://github.com/ExtremeFiretop/Reqestra/commit/f83abafdd9fc416c8ca4d99a52b5fc94e0b781fd))
* **discover:** ✨ Added infinite scroll on advanced search results ([898bc89](https://github.com/ExtremeFiretop/Reqestra/commit/898bc89fa78245c1f3de9481f6c724f087a16e39))
* **discover:** Add deny option to recently requested ([#4907](https://github.com/ExtremeFiretop/Reqestra/issues/4907)) ([78f340e](https://github.com/ExtremeFiretop/Reqestra/commit/78f340ee5f309c55690497170897533801957668))
* **discover:** Add new trending source experimental feature ([1a0823c](https://github.com/ExtremeFiretop/Reqestra/commit/1a0823ca80559417c67323aaeaa1ef5243e98031))
* **discover:** Add original language filter ([ef7ec86](https://github.com/ExtremeFiretop/Reqestra/commit/ef7ec861d8aede2a4817752c990617f583805391))
* **discover:** Default trending source to new logic ([4f12939](https://github.com/ExtremeFiretop/Reqestra/commit/4f12939e22020a67a5ee75e2907923faea136e8d))
* **discover:** Show more relevant shows in upcoming TV ([8357819](https://github.com/ExtremeFiretop/Reqestra/commit/8357819b53b8c675c0b246d7006b5a778bdba33f))
* **email-notifications:** Add a link to Ombi details page in email notifications ([#4517](https://github.com/ExtremeFiretop/Reqestra/issues/4517)) ([a3e97b3](https://github.com/ExtremeFiretop/Reqestra/commit/a3e97b31e2298d95e7deebd71268095b8ed5e9dc))
* **emby:** :sparkles: Added a emby recently added sync! ([a0e1406](https://github.com/ExtremeFiretop/Reqestra/commit/a0e14068f4bc457f8a4a565de71707a8f16c803c))
* **emby:** Show end-user external IP address to Emby when logging in as an Emby user ([#4949](https://github.com/ExtremeFiretop/Reqestra/issues/4949)) ([79cef7e](https://github.com/ExtremeFiretop/Reqestra/commit/79cef7e0f8643e36536a9ea84dd1a07c232403a9)), closes [#4947](https://github.com/ExtremeFiretop/Reqestra/issues/4947)
* **emby:** Show watched status for Movie requests ([9cfb10b](https://github.com/ExtremeFiretop/Reqestra/commit/9cfb10bb1ee69067a6f47bd2c8a72d4e6834350e))
* **emby:** Show watched status for TV requests ([1f37de0](https://github.com/ExtremeFiretop/Reqestra/commit/1f37de08888812b6d130d92bb664a89e89149105))
* Hide watched status when request is not available ([#4934](https://github.com/ExtremeFiretop/Reqestra/issues/4934)) ([82c7f1c](https://github.com/ExtremeFiretop/Reqestra/commit/82c7f1c44fd7c87d57cc2b0c34a10fcda7628f4e))
* improve contributor guidance ([d045f32](https://github.com/ExtremeFiretop/Reqestra/commit/d045f32b85fd4c019239e13a6a0eefb842ed1b31))
* **mass-email:** :sparkles: Added the ability to configure the Mass Email, we can now send BCC and we are less likely to be rate limited when not using bcc [#4377](https://github.com/ExtremeFiretop/Reqestra/issues/4377) ([ca655ae](https://github.com/ExtremeFiretop/Reqestra/commit/ca655ae57042dec44106a2f2ef5ba2e6f1019ee4))
* **media-cleanup:** add structured destructive-operation audit logging ([e8c00bb](https://github.com/ExtremeFiretop/Reqestra/commit/e8c00bb2a9d89b5fb076c1ad651f785e7732d8d4))
* **media-details:** Add Trakt to social icons ([#4522](https://github.com/ExtremeFiretop/Reqestra/issues/4522)) ([d6ae79c](https://github.com/ExtremeFiretop/Reqestra/commit/d6ae79ce9eddbd5b7b888ab1b9f7e342d9d9ff9e))
* **newsletter:** Started to localize the newsletter ([#4485](https://github.com/ExtremeFiretop/Reqestra/issues/4485)) ([b5ec556](https://github.com/ExtremeFiretop/Reqestra/commit/b5ec5562435021ea4b8af07c9b64a3f7249b570a))
* **notifications:** :sparkles: Added Partially Available Notifications ([1ef45dc](https://github.com/ExtremeFiretop/Reqestra/commit/1ef45dc44c93d566a0f04c011bfcefe2009a24b5))
* **notifications:** :sparkles: Send new request email notifications to power users ([#4462](https://github.com/ExtremeFiretop/Reqestra/issues/4462)) ([10cc0c0](https://github.com/ExtremeFiretop/Reqestra/commit/10cc0c0951f13221179516f8ff5c44dbecc9a0fd))
* **notifications:** Add more curly variables for partially available notification ([66aa101](https://github.com/ExtremeFiretop/Reqestra/commit/66aa101019c4c4b34e186db9d303049d02b9c781))
* pipes ([1ac20e8](https://github.com/ExtremeFiretop/Reqestra/commit/1ac20e84dbf9b90e6797cb49cf3fd4e863f7352d))
* **plex:** :zap: Use the new Plex Pass includeGuids feature for syncing plex movies ([7c5646d](https://github.com/ExtremeFiretop/Reqestra/commit/7c5646d416372983ac8996b1a6539e671db18c86))
* **plex:** ✨ Added the ability to configure the watchlist to request the whole TV show rather than latest season ([#4774](https://github.com/ExtremeFiretop/Reqestra/issues/4774)) ([fa65712](https://github.com/ExtremeFiretop/Reqestra/commit/fa65712bd570fe8d5d21b8ca0abe182b84960017))
* **plex:** Rework the Plex Settings page ([#4805](https://github.com/ExtremeFiretop/Reqestra/issues/4805)) ([1b8c47f](https://github.com/ExtremeFiretop/Reqestra/commit/1b8c47f3163f618851d4904732cb07015e1e93ff))
* Provide a flag for missing users on Plex Server ([#4688](https://github.com/ExtremeFiretop/Reqestra/issues/4688)) ([#4778](https://github.com/ExtremeFiretop/Reqestra/issues/4778)) ([b4a14c2](https://github.com/ExtremeFiretop/Reqestra/commit/b4a14c2d28218409390e517b226130e3e84efee1))
* Radarr tags ([#4815](https://github.com/ExtremeFiretop/Reqestra/issues/4815)) ([6fa5064](https://github.com/ExtremeFiretop/Reqestra/commit/6fa506491fe867cdeef9df79991ae49319d71c3d))
* **radarr:** 4K Requests and Radarr 4K support  ([ba88848](https://github.com/ExtremeFiretop/Reqestra/commit/ba88848866b0a9dedb1e79b55c4d81a0fd453843))
* Recently requested improvements ([#4755](https://github.com/ExtremeFiretop/Reqestra/issues/4755)) ([ff04d87](https://github.com/ExtremeFiretop/Reqestra/commit/ff04d875343604c77c391bf55d0968977e480281))
* **request-limits:** :card_file_box: Added new user field migrations to mysql and sqlite ([f73bccb](https://github.com/ExtremeFiretop/Reqestra/commit/f73bccbea759fb4aeadc32f94b1ef6c9aecc5e94))
* **request-limits:** :sparkles: Added in the main logic for the new request limits ([70d5bf5](https://github.com/ExtremeFiretop/Reqestra/commit/70d5bf52bff2e321fb1f3d00fd1cd1121a2717b7))
* **request-limits:** :sparkles: Added the new request limit options into the user importer ([01d4f4d](https://github.com/ExtremeFiretop/Reqestra/commit/01d4f4d718fe85ac181dae52565fb1b427965b4f))
* **request-limits:** :sparkles: Added the new request limit options to the bulk edit ([03bc23a](https://github.com/ExtremeFiretop/Reqestra/commit/03bc23a74e4308aa6b4c6b25636edcdeb65c1f0e))
* **request-limits:** :sparkles: Added the UI portion to set the new limits ([978d4ea](https://github.com/ExtremeFiretop/Reqestra/commit/978d4ea33b32d5a8333c75e29d4cd702e434c5f0))
* **request-limits:** :sparkles: Request limits are no longer a rolling date. But reset at the start of the week or month depending on the preference ([364b9f1](https://github.com/ExtremeFiretop/Reqestra/commit/364b9f11afcd470cc2b112cf81cd840316ddc80e))
* **request-limits:** :sparkles: Updated the RequestLimit Rules to use the new refactored service ([e31ee8d](https://github.com/ExtremeFiretop/Reqestra/commit/e31ee8d89213a8fc179db56cc51d3f02648b51ec))
* **request-limits:** :tada: Started on the request limits, applied to the user model ([f5310b7](https://github.com/ExtremeFiretop/Reqestra/commit/f5310b786b43b3c00d392da977c2b3367a5e4e11))
* **requests:** add compact view, sorting and user filtering to requests list ([#5424](https://github.com/ExtremeFiretop/Reqestra/issues/5424)) ([4e82fb1](https://github.com/ExtremeFiretop/Reqestra/commit/4e82fb16e37f15bc684d2da5fa3e43866a7222a6)), closes [#5422](https://github.com/ExtremeFiretop/Reqestra/issues/5422)
* Search by genre ([1837419](https://github.com/ExtremeFiretop/Reqestra/commit/18374198f9f2462ba85c5781b0fcc05892728b21))
* **sonarr:** :sparkles: Add the username to a Sonarr tag when sent to Sonarr ([#4802](https://github.com/ExtremeFiretop/Reqestra/issues/4802)) ([1d5fabd](https://github.com/ExtremeFiretop/Reqestra/commit/1d5fabd317e3ce8f6dd31f06d15dc81277f39dbd))
* **sonarr:** Added the ability to add default tags when sending to Sonarr ([#4803](https://github.com/ExtremeFiretop/Reqestra/issues/4803)) ([ecfbb8e](https://github.com/ExtremeFiretop/Reqestra/commit/ecfbb8eda91e1a90239dcf8be847afcc2394a78e))
* **sync:** Detect reidentified movies in Emby and Jellyfin ([5938077](https://github.com/ExtremeFiretop/Reqestra/commit/5938077d82a5357f79c07b218b3986557a5816e8))
* **sync:** Detect reidentified series in Emby and Jellyfin ([9096e91](https://github.com/ExtremeFiretop/Reqestra/commit/9096e91d55d268819bce22831f8a8b27f2a1776b))
* **TvSender:** add logging for missing seasons in Sonarr during monitoring updates ([0d219e4](https://github.com/ExtremeFiretop/Reqestra/commit/0d219e4612ce047d7a507c2447ed6d76d616768e))
* upgrade @ngx-translate/core from 14.0.0 to 15.0.0 ([#5158](https://github.com/ExtremeFiretop/Reqestra/issues/5158)) ([48d3dec](https://github.com/ExtremeFiretop/Reqestra/commit/48d3dec26d36002a9d613432fb7f9232d8801cba))
* upgrade @ngx-translate/http-loader from 7.0.0 to 8.0.0 ([#5159](https://github.com/ExtremeFiretop/Reqestra/issues/5159)) ([3bd98c1](https://github.com/ExtremeFiretop/Reqestra/commit/3bd98c1d711786bff66f1528dcdddcafe256abd2))
* Upgrade to Angular14 ([#4668](https://github.com/ExtremeFiretop/Reqestra/issues/4668)) ([b9d55a4](https://github.com/ExtremeFiretop/Reqestra/commit/b9d55a469b412558cbf67c1e25db7fdda5964cd8))
* use dynamic scroll sizing for recently requested carousel ([5b0f325](https://github.com/ExtremeFiretop/Reqestra/commit/5b0f3252f68d33f93c26b54cdf83780697ed6172))
* Watchlist history errors([#4741](https://github.com/ExtremeFiretop/Reqestra/issues/4741)) ([c222f1a](https://github.com/ExtremeFiretop/Reqestra/commit/c222f1a945e944ef34e68cad2b61f40e57cab823))
* **wizard:** :sparkles: Added the ability to start with a different database ([#5208](https://github.com/ExtremeFiretop/Reqestra/issues/5208)) ([cc98fc6](https://github.com/ExtremeFiretop/Reqestra/commit/cc98fc6aca27111a8afc3b7b5b8e53207b73fe15))


### Performance Improvements

* :zap: Use ngxs store for the whole customization section of the app ([97b493d](https://github.com/ExtremeFiretop/Reqestra/commit/97b493d869feee59d360b484a6c59388a2aead1f))
* ⚡ Improve render performance on the discover, movie and tv pages ([#5084](https://github.com/ExtremeFiretop/Reqestra/issues/5084)) ([71c86a8](https://github.com/ExtremeFiretop/Reqestra/commit/71c86a8db9e63bf0ab779f9a8b5d62a42c246392))
* **discover:** :zap: Improve the loading performance on the discover page ([97d5167](https://github.com/ExtremeFiretop/Reqestra/commit/97d5167db6c9f915021f32b96b281d7db3741d7f))
* Improvements to api calls ([6907604](https://github.com/ExtremeFiretop/Reqestra/commit/69076047f5ace434b175fa424acd78d87eeeb1de))
* stop populating obsolete subscribe fields ([#4625](https://github.com/ExtremeFiretop/Reqestra/issues/4625)) ([9a73463](https://github.com/ExtremeFiretop/Reqestra/commit/9a734637665f671b17c2bb440d93b35a891c142b))


### Reverts

* Revert "Restore Reqestra changelog after merge conflict" ([1f9e686](https://github.com/ExtremeFiretop/Reqestra/commit/1f9e68665f2e680d4b70001bb29232eb88de5b9b))
* Revert "deterministic refresh (rather than 3 second wait)" ([a8bd017](https://github.com/ExtremeFiretop/Reqestra/commit/a8bd01793f67d86f6da345d7a3fe3adbaee920ca))
* Revert "address sonarqube feedback" ([10992c7](https://github.com/ExtremeFiretop/Reqestra/commit/10992c78b6c9c33fa5b31407b02fc8eee9be59f2))
* Revert "Update src/Ombi/Controllers/V1/TokenController.cs" ([0294dba](https://github.com/ExtremeFiretop/Reqestra/commit/0294dba4cc8d27ace0503fd2518b4419c3f0f08f))
* Revert "Revert request.service.ts to version on upstream/develop" ([a23c103](https://github.com/ExtremeFiretop/Reqestra/commit/a23c1030f487145684f0eab1a57101f422a69f6c))
* Revert "Fixed Plex OAuth, should no longer show Insecure warning" ([cbf331c](https://github.com/ExtremeFiretop/Reqestra/commit/cbf331cd09409b58d51506c18e0874aaac747c46))
* Revert "Sorting out the current state of migrations" ([3256a74](https://github.com/ExtremeFiretop/Reqestra/commit/3256a743d968418885c4a8bafa7dc4a472fafdf8))
* Revert "Small changes" ([c57f331](https://github.com/ExtremeFiretop/Reqestra/commit/c57f33151bc07819bc950675f5a0a7978ba26348))



