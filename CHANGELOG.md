# [9.0.0-beta.9](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.8...9.0.0-beta.9) (2022-01-25)


### Bug Fixes

* **mudblazor:** set stream buffer size to max if MaxFileSize = 0 ([9b5960e](https://github.com/sitkoru/Sitko.Core/commit/9b5960efcfec119537b62f574551f3eaa6b439d4))
* **remotestorage:** don't inject HttpClient, create via options of IHttpClientFactory to support dynamic options ([4071c4a](https://github.com/sitkoru/Sitko.Core/commit/4071c4aaaafab33f4f1db52c3d02aa76f406c038))
* **remotestorage:** support upload to empty path ([4b9c3d6](https://github.com/sitkoru/Sitko.Core/commit/4b9c3d6a61b44259a7c7ac68c3016bd6a3510c15))

# [9.0.0-beta.8](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.7...9.0.0-beta.8) (2022-01-21)


### Features

* **validation:** update Sitko.FluentValidation to 1.2.0 ([0a58b75](https://github.com/sitkoru/Sitko.Core/commit/0a58b75954070db08727a8a689ed87d6ea9ff165))

# [9.0.0-beta.7](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.6...9.0.0-beta.7) (2022-01-21)


### Bug Fixes

* **wasm:** pass real IJsRuntime from current application to logger sink ([96abbcc](https://github.com/sitkoru/Sitko.Core/commit/96abbcc7c60201f6d2cd6f7d0d403c66d5cdc827))

# [9.0.0-beta.6](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.5...9.0.0-beta.6) (2022-01-21)


### Bug Fixes

* **mudblazor:** await metadata result ([624f5e3](https://github.com/sitkoru/Sitko.Core/commit/624f5e34e3551944a40f267a7fcd43291481fa68))
* **s3storage:** fix saving metadata ([a2a174b](https://github.com/sitkoru/Sitko.Core/commit/a2a174b82ebf32057cfb041c9afb2c5806835bbd))
* **s3storage:** pass cancellation tokens ([f843719](https://github.com/sitkoru/Sitko.Core/commit/f843719bff3bd09a28f163745b4ff732e3551db1))
* **s3storage:** support presigned urls and settings bucket policy ([f3c7174](https://github.com/sitkoru/Sitko.Core/commit/f3c71743c1e321f5afaa5d449e8a54871d298f0b))


### Features

* **demo:** use remote storage ([d396b57](https://github.com/sitkoru/Sitko.Core/commit/d396b57ea0113536e331604b19a683bf37b41f25))
* **filestorage:** switch to UploadRequest ([3d6e65d](https://github.com/sitkoru/Sitko.Core/commit/3d6e65d55a5d4c32fa27258df1460dcafb468778))
* **remotestorage:** add to sln ([cbc55a1](https://github.com/sitkoru/Sitko.Core/commit/cbc55a10679ccd1858eac2dbee6563438c381464))
* **remotestorage:** empty storage ([85c8949](https://github.com/sitkoru/Sitko.Core/commit/85c89495e0775d5dac8cf60b935d39d595f393a8))
* **remotestorage:** fix urls, passing data and file downloading ([6438253](https://github.com/sitkoru/Sitko.Core/commit/6438253f7b40f1321624a4b32d4f5330065a28b8))
* **remotestorage:** implement base server controller ([858ab71](https://github.com/sitkoru/Sitko.Core/commit/858ab71749b5d06488bac7d987d06cb79e4d7587))
* **remotestorage:** implement RemoteStorageMetadataProvider (not finished) ([6b26f8e](https://github.com/sitkoru/Sitko.Core/commit/6b26f8e0249d6697953d2a14aeaa722e30b3ba49))
* **remotestorage:** implement storage list in controller ([8db06a0](https://github.com/sitkoru/Sitko.Core/commit/8db06a08a234d3ba598a9a12f5cd1e57a1b4db48))
* **remotestorage:** implement traverse with metadata ([090c1c8](https://github.com/sitkoru/Sitko.Core/commit/090c1c8c7016a853d9cc7b8294d58f6169c83d38))
* **remotestorage:** implement updating metadata ([998841f](https://github.com/sitkoru/Sitko.Core/commit/998841f960c40aaea4610fe57edf21607afa06f6))
* **remotestorage:** implement upload, download and existence check methods ([be8c184](https://github.com/sitkoru/Sitko.Core/commit/be8c1846543a9030a24afe5bbf7741053c8a2685))
* **remotestorage:** make access check methods virtual with default implementation ([004d893](https://github.com/sitkoru/Sitko.Core/commit/004d89365bc93004a5fec9bb46638894d6969d3d))
* **remotestorage:** return RemoteStorageItem with metadata from controller ([5daf6d8](https://github.com/sitkoru/Sitko.Core/commit/5daf6d83e0e3a14f369830a9b68b2d721f06efc1))
* **s3storage:** don't inject storage into metadata provider, pass from storage itself ([a5e9568](https://github.com/sitkoru/Sitko.Core/commit/a5e95682e2c8ec0c852cd5562b509a87ca498ac5))
* **s3storage:** switch to UploadRequest ([1547c1c](https://github.com/sitkoru/Sitko.Core/commit/1547c1ca58efa8befaa9817cb56af8f1423eec48))
* **storage:** don't inject storage into metadata provider, pass from storage itself ([ad08dd6](https://github.com/sitkoru/Sitko.Core/commit/ad08dd66cf5f314f16ed55d1827ad4e332c3e1c1))
* **storage:** implement upload via UploadRequest, final storage must return StorageItem ([3dba35f](https://github.com/sitkoru/Sitko.Core/commit/3dba35f6372a3eb4dcc7ef5677ec99007e44823a))
* **storage:** make GetAllItemsAsync public for now to use in controller ([a67c183](https://github.com/sitkoru/Sitko.Core/commit/a67c18361a67459c9624b4b221aa48189727ebd9))
* **storage:** move internal classes into Internal namespace and make them public ([cb2dbae](https://github.com/sitkoru/Sitko.Core/commit/cb2dbaeaafc0a3e928533f8754f022391c6691de))
* **storage:** move StorageItemDownloadInfo into Internal namespace and make it public. Make GetStream async ([1cf8e4f](https://github.com/sitkoru/Sitko.Core/commit/1cf8e4fef3288272a3d87593df58e3160337f434))
* **storage:** rework storage item creation ([4d969ea](https://github.com/sitkoru/Sitko.Core/commit/4d969ea4bf72a7216f865a8b4e17c8dde1527dad))
* **storage:** rework storage item creation ([5e6cbbc](https://github.com/sitkoru/Sitko.Core/commit/5e6cbbc4305eea68d482ce48ad093007efd69e41))
* **xunit:** add BeforeConfiguredAsync hook ([8e6d924](https://github.com/sitkoru/Sitko.Core/commit/8e6d92454301606d8b93e3903ccb9f023d507abf))

# [9.0.0-beta.5](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.4...9.0.0-beta.5) (2022-01-19)


### Bug Fixes

* **mudblazor:** use more appropriate preview icon in MudFileUpload ([98cb564](https://github.com/sitkoru/Sitko.Core/commit/98cb5647b7ee201d3b76f4c2e786443522db539f))


### Features

* **demo:** upd mudblazor demo ([e86a8b0](https://github.com/sitkoru/Sitko.Core/commit/e86a8b0730e59cbba96e42d0277a75677a9d620a))
* **mudblazor:** add Label, HelperText and errors display to MudFileUpload ([4912fa9](https://github.com/sitkoru/Sitko.Core/commit/4912fa9aa7f096698f04adbefcc6cf9269d28503))
* **mudblazor:** add MudValidationMessage component to display validation messages for Field ([9392350](https://github.com/sitkoru/Sitko.Core/commit/9392350a79b6f3836b0dedeb3a1d840aaedfbf14))

# [9.0.0-beta.4](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.3...9.0.0-beta.4) (2022-01-18)


### Features

* **fileupload:** merge FileUploadRequest and FileUploadInfo ([f02aa61](https://github.com/sitkoru/Sitko.Core/commit/f02aa6121d968cb285e45ccd2100adf2378fde67))

# [9.0.0-beta.3](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.2...9.0.0-beta.3) (2022-01-14)


### Features

* **blazor:** add BaseStateComponent with state persistence and compression ([b2923d4](https://github.com/sitkoru/Sitko.Core/commit/b2923d491774ae961cd7632ad4f6bb2a8ddc377a))
* **blazorserver:** support CompressedPersistentComponentState ([083315d](https://github.com/sitkoru/Sitko.Core/commit/083315d57a10a8b6d45adce45dc5a746fbe894f8))
* **blazorwasm:** configure wasm host builder via IWasmApplicationModule ([a44d04e](https://github.com/sitkoru/Sitko.Core/commit/a44d04ed23a834b37279cd98f191f5b57ce26cf2))
* **blazorwasm:** implement LogInternal ([30d2216](https://github.com/sitkoru/Sitko.Core/commit/30d221622c38b77d60ab2917df33d309e1d58391))
* **blazorwasm:** rework logging configuration ([cf34cb5](https://github.com/sitkoru/Sitko.Core/commit/cf34cb56d16f6434694c27062dca8dbd226bd41c))
* **blazorwasm:** support CompressedPersistentComponentState ([c5e65b5](https://github.com/sitkoru/Sitko.Core/commit/c5e65b5a77bd15e200ea1b6380d0e4f9efcd7b41))
* **blazorwasm:** support ScriptInjector ([13b9e84](https://github.com/sitkoru/Sitko.Core/commit/13b9e84bfec3a6e4c04019cc1906fd843246a9aa))
* **blazorwasm:** use single hostbuilder to create application ([45932fc](https://github.com/sitkoru/Sitko.Core/commit/45932fc8028c783e626226b0d1e4a185f21e5a7c))
* **logging:** unify serilog integration ([a102ef1](https://github.com/sitkoru/Sitko.Core/commit/a102ef1340070c14e1709f1027654de56c40062a))
* move internal logging from base application ([f3c0cb1](https://github.com/sitkoru/Sitko.Core/commit/f3c0cb118dca577edd214a185e12c648714bd907))

# [9.0.0-beta.2](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.1...9.0.0-beta.2) (2022-01-14)


### Features

* make WasmApplication abstract and force client app to configure WebAssemblyHostBuilder ([fadb4ed](https://github.com/sitkoru/Sitko.Core/commit/fadb4edfbe01f634fea76f1cd3a8c28323b0d3cb))

# [9.0.0-beta.1](https://github.com/sitkoru/Sitko.Core/compare/8.36.1...9.0.0-beta.1) (2022-01-14)


### Bug Fixes

* lazy init for app context options ([c94d703](https://github.com/sitkoru/Sitko.Core/commit/c94d703f7380170fa767650ff9e135d59c8063a5))


### Features

* **app:** base application should be independent of hosting model ([3ea04db](https://github.com/sitkoru/Sitko.Core/commit/3ea04db3ae059621bcdcfa2c8fcd0e8e5399d9a5))
* **blazor:** create new base application for BlazorWasm ([9dfba19](https://github.com/sitkoru/Sitko.Core/commit/9dfba194199fffa693a812348eb09cf005edd26a))
* **blazor:** Split blazor modules. Create separate packages for BlazorServer ([dab20f2](https://github.com/sitkoru/Sitko.Core/commit/dab20f2102c20a23f478d07cc706223d9af485a2))
* **blazor:** upd .sln ([34b7ea4](https://github.com/sitkoru/Sitko.Core/commit/34b7ea45fcbf8028e8ddb03589d40219d41df5af))
* **demo:** upd demo ([4cc93a2](https://github.com/sitkoru/Sitko.Core/commit/4cc93a2a0211bb284eaffd7259f69090a7881b79))
* introduce IApplicationContext to combine IConfiguration, IAppEnvironment and application options ([f2a7b5f](https://github.com/sitkoru/Sitko.Core/commit/f2a7b5f361a548e738a59cf811cd5aff589f690d))
* rebase WebApplication onto HostedApplication ([2f9ab6c](https://github.com/sitkoru/Sitko.Core/commit/2f9ab6c79d1d3055ee77146a25e01d89e2e3e2f6))
* switch modules to use IAppEnvironment instead of IHostEnvironment ([d713865](https://github.com/sitkoru/Sitko.Core/commit/d71386507abfbe7a663481b6dc8ad4020d2514c9))


### BREAKING CHANGES

* All modules must use IApplicationContext
* **blazor:** Sitko.Core.App.Blazor is split to Sitko.Core.Blazor and Sitko.Core.Blazor.Server
* All modules must be updated
* **app:** Application class is runtime-independent now. HostedApplication is new base class for web/console applications.

## [8.36.1](https://github.com/sitkoru/Sitko.Core/compare/8.36.0...8.36.1) (2022-01-12)


### Bug Fixes

* google auth module now correctly rejects unlisted users ([892e344](https://github.com/sitkoru/Sitko.Core/commit/892e344e591767b6e0507eea8a3698ec6779fd71))

# [8.36.0](https://github.com/sitkoru/Sitko.Core/compare/8.35.0...8.36.0) (2022-01-11)


### Features

* **grpc:** manually create grpc clients and channel to fix issue with service address update not applying to channel ([e25d951](https://github.com/sitkoru/Sitko.Core/commit/e25d95113a2f0a5d38d7557f3148b5562cde8431))

# [8.35.0](https://github.com/sitkoru/Sitko.Core/compare/8.34.0...8.35.0) (2022-01-10)


### Features

* **configuration:** run configuration checks before modules init ([7db3dd0](https://github.com/sitkoru/Sitko.Core/commit/7db3dd07a0b506acd2f2bc487b9a82c45718f92d))
* **graylog:** update Serilog.Sinks.Graylog from 2.2.2 to 2.3.0 ([2669deb](https://github.com/sitkoru/Sitko.Core/commit/2669deb433389481a6c7dd9759d269adbf9ae02a))
* **grpc:** grpc 2.43.0 and protobuf 3.19.2 ([4cb0146](https://github.com/sitkoru/Sitko.Core/commit/4cb0146f28a7d284a698f455886b8670135e1b70))
* **mudblazor:** update MudBlazor from 6.0.2 to 6.0.4 ([76ebc1d](https://github.com/sitkoru/Sitko.Core/commit/76ebc1d221956599f81b4fb6d928c3559172968c))
* **nats:** update NATS.Client from 0.14.3 to 0.14.4 ([f674438](https://github.com/sitkoru/Sitko.Core/commit/f674438e5353de8333bff50723977fa42304dc9a))
* **s3:** update AWSSDK.S3 from 3.7.7.5 to 3.7.7.10 ([2f4f3ba](https://github.com/sitkoru/Sitko.Core/commit/2f4f3ba7b335b124147880186ac4e4a75e057561))
* **vault:** add option to throw on empty secrets ([935fce4](https://github.com/sitkoru/Sitko.Core/commit/935fce4f2c5cdfede9b281b0cb323b7d7700f59d))
* **xunit:** update FluentAssertions from 6.2.0 to 6.3.0 ([e5753b2](https://github.com/sitkoru/Sitko.Core/commit/e5753b2b1ead6b607a215015dcf9a9fed8cf261d))

# [8.34.0](https://github.com/sitkoru/Sitko.Core/compare/8.33.1...8.34.0) (2021-12-29)


### Bug Fixes

* **repository:** make logger in repository more specific type ([0b8e913](https://github.com/sitkoru/Sitko.Core/commit/0b8e9130fdaedeb1445dd530c94ef371c7f2e01a))


### Features

* **antdesing:** update AntDesign from 0.10.2 to 0.10.3.1 ([67be530](https://github.com/sitkoru/Sitko.Core/commit/67be530adc2cbd89e90047bb7935c88b01bbc6fd))
* **consul:** update Consul from 1.6.10.3 to 1.6.10.4 ([f71fb8e](https://github.com/sitkoru/Sitko.Core/commit/f71fb8e71e657258547da73b25eefffd15432c8a))
* **fileupload:** update Tewr.Blazor.FileReader from 3.3.0.21348 to 3.3.1.21360 ([183edce](https://github.com/sitkoru/Sitko.Core/commit/183edceb53cc9b01c37d0dfbd7a2dd72cbba76bd))
* **hangfire:** update Hangfire.PostgreSql from 1.9.4 to 1.9.5 ([f3003ac](https://github.com/sitkoru/Sitko.Core/commit/f3003ac3abcffa2ab0a1fb6524d4d2d468bcfcc1))
* **postgres:** update Npgsql.EntityFrameworkCore.PostgreSQL from 6.0.1 to 6.0.2 ([0fd949a](https://github.com/sitkoru/Sitko.Core/commit/0fd949acd72aa0ca36b4bdc57dc555e0fea217e7))
* **puppeteer:** update PuppeteerSharp from 6.0.0 to 6.1.0 ([c739d65](https://github.com/sitkoru/Sitko.Core/commit/c739d65e48a5c21c153ac26a0f029a3e18a73297))
* **s3:** update AWSSDK.S3 from 3.7.7.3 to 3.7.7.5 ([1ef86db](https://github.com/sitkoru/Sitko.Core/commit/1ef86dbb9e34dfcf24f5465de8d0b4b4cb61ee6d))

## [8.33.1](https://github.com/sitkoru/Sitko.Core/compare/8.33.0...8.33.1) (2021-12-23)


### Bug Fixes

* **blazor:** pass loaded or new entity to LoadDataAsync method ([18c7ccf](https://github.com/sitkoru/Sitko.Core/commit/18c7ccf8531c9f02f484e7bf739d14b8d115a65a))

# [8.33.0](https://github.com/sitkoru/Sitko.Core/compare/8.32.0...8.33.0) (2021-12-23)


### Features

* **blazor:** add method to load data in same scope as entity ([e2348ae](https://github.com/sitkoru/Sitko.Core/commit/e2348ae78caf4b94fa491589675f073cec57c579))

# [8.32.0](https://github.com/sitkoru/Sitko.Core/compare/8.31.0...8.32.0) (2021-12-20)


### Features

* **grpc:** expose FillErrors method to gRPC services ([109e6c9](https://github.com/sitkoru/Sitko.Core/commit/109e6c9e77a9ea67abea955b6b829ee61476da37))

# [8.31.0](https://github.com/sitkoru/Sitko.Core/compare/8.30.0...8.31.0) (2021-12-20)


### Features

* **mudblazor:** remove new() constraint from MudRepositoryTable to allow abstract classes ([909c51e](https://github.com/sitkoru/Sitko.Core/commit/909c51ee91b08630694ac9c8095bcba366db2e4f))

# [8.30.0](https://github.com/sitkoru/Sitko.Core/compare/8.29.0...8.30.0) (2021-12-17)


### Features

* **core:** allow multiple module instances registration if supported by module ([b67ba30](https://github.com/sitkoru/Sitko.Core/commit/b67ba30060911756d183426916b16b37c0b2234f))
* **postgres:** allow to configure maximum migrations apply try count ([d947684](https://github.com/sitkoru/Sitko.Core/commit/d9476846a1433254dffbff15e46eedfe1be5add0))
* **repository:** rework repositories registration ([46fedd2](https://github.com/sitkoru/Sitko.Core/commit/46fedd24e1d5d00e62c67de6485e318ffb18c406))
* **xunit:** support multiple db contexts in test scope ([f835ffa](https://github.com/sitkoru/Sitko.Core/commit/f835ffa2ee50e7d9d1e5e19061311c66c7c408ed))

# [8.29.0](https://github.com/sitkoru/Sitko.Core/compare/8.28.0...8.29.0) (2021-12-17)


### Features

* **puppeteer:** move Puppeteer to separate module ([c9c8fbf](https://github.com/sitkoru/Sitko.Core/commit/c9c8fbf93786380fa3bed7592b06bd64a2e41bf8))
* **xunit:** rework tests to use IConfiguration ([9983061](https://github.com/sitkoru/Sitko.Core/commit/9983061675c97bead42f640eca2e0a4b330715b2))

# [8.28.0](https://github.com/sitkoru/Sitko.Core/compare/8.27.0...8.28.0) (2021-12-15)


### Features

* **repository:** add methods to bulk add/update/delete entities ([9bb789e](https://github.com/sitkoru/Sitko.Core/commit/9bb789e8e89d5f2db19dedf7660349ad2058e356))

# [8.27.0](https://github.com/sitkoru/Sitko.Core/compare/8.26.2...8.27.0) (2021-12-15)


### Features

* **.net:** .NET 5.0.13 ([f3cd021](https://github.com/sitkoru/Sitko.Core/commit/f3cd0218010264ed23ae2506073a24c473f1355e))
* **.net:** .NET 6.0.1 ([6a43368](https://github.com/sitkoru/Sitko.Core/commit/6a43368b44424615296dd6f6ec851bb1187103e8))
* **.net:** .NET Core 3.1.22 ([9fca006](https://github.com/sitkoru/Sitko.Core/commit/9fca006cb4cd8e2a2bed6dff2b2a42bbfeed3fb7))

## [8.26.2](https://github.com/sitkoru/Sitko.Core/compare/8.26.1...8.26.2) (2021-12-10)


### Bug Fixes

* **repository:** inject common dbcontext to all scope repositories ([98a15bd](https://github.com/sitkoru/Sitko.Core/commit/98a15bd623e3cfaab4c8de919886e69ee4fea00e))

## [8.26.1](https://github.com/sitkoru/Sitko.Core/compare/8.26.0...8.26.1) (2021-12-09)


### Bug Fixes

* **mudblazor:** remove CurrentPage parameter from table until https://github.com/MudBlazor/MudBlazor/issues/1403 is fixed ([1773278](https://github.com/sitkoru/Sitko.Core/commit/1773278e10653c7cb111a270ba54196ac5af4744))

# [8.26.0](https://github.com/sitkoru/Sitko.Core/compare/8.25.0...8.26.0) (2021-12-08)


### Bug Fixes

* **mudblazor:** improve image preview dialog in file upload component ([319c501](https://github.com/sitkoru/Sitko.Core/commit/319c5014b1972ca6dcaf977170a23140b2147e34))


### Features

* **blazor:** add description to layout ([352551b](https://github.com/sitkoru/Sitko.Core/commit/352551b8925bcc8f0459290a9aa9f2217756a59b))
* **mudblazor:** change surface background in dark theme to support nesting ([8d52d7a](https://github.com/sitkoru/Sitko.Core/commit/8d52d7aaa604bd300634122bd885fd12bd14dc1e))
* **mudblazor:** improve file upload markup ([a5b7515](https://github.com/sitkoru/Sitko.Core/commit/a5b7515238d5353273ac12a13334a6805892de13))
* **mudblazor:** restructure layout to support descriptions and mini-drawer ([88912ad](https://github.com/sitkoru/Sitko.Core/commit/88912adf92cbdc4fdc2dd4cf3f5c89e818f2cf4e))
* **mudblazor:** start adding MudBlazor library ([a687b7d](https://github.com/sitkoru/Sitko.Core/commit/a687b7dce5ac00218c3391721523089f02102170))
* **mudblazor:** switch to MudBlazor 6.0.2, target only .NET 6 ([5c6e25e](https://github.com/sitkoru/Sitko.Core/commit/5c6e25e9f5b5ed29a157e0a74f0c89ad3407c3bf))
* **mudblazor:** upd demo ([3ba9338](https://github.com/sitkoru/Sitko.Core/commit/3ba9338344d573ba0567c4c3ea8417db92dce85c))

# [8.25.0](https://github.com/sitkoru/Sitko.Core/compare/8.24.1...8.25.0) (2021-12-06)


### Features

* **validation:** move FluentGraphValidator to separate library Sitko.FluentValidation ([96eb71f](https://github.com/sitkoru/Sitko.Core/commit/96eb71f18c9616b9e0ad3d7cde22809b411346e8))

## [8.24.1](https://github.com/sitkoru/Sitko.Core/compare/8.24.0...8.24.1) (2021-11-29)


### Bug Fixes

* **blazor:** suppress dispose exceptions in components when it's caused by parent component dispose ([b69fa94](https://github.com/sitkoru/Sitko.Core/commit/b69fa94b991b3b008901e72626c7e23b1b6b76ae))

# [8.24.0](https://github.com/sitkoru/Sitko.Core/compare/8.23.1...8.24.0) (2021-11-26)


### Bug Fixes

* **validation:** don't validate system types at all ([3427bf3](https://github.com/sitkoru/Sitko.Core/commit/3427bf3947f921cdc6f508a40c425f1ddae06a3d))
* **validation:** implicitly exclude system types from validation ([a3614dd](https://github.com/sitkoru/Sitko.Core/commit/a3614dd89732b7f5c85970768d0d6c99f955e075))


### Features

* **validation:** add validator types cache ([19d262d](https://github.com/sitkoru/Sitko.Core/commit/19d262dfbff96a505f9a08ce2412d3073e13c6fc))

## [8.23.1](https://github.com/sitkoru/Sitko.Core/compare/8.23.0...8.23.1) (2021-11-17)


### Bug Fixes

* **repository:** override equality operators to simplify entities comparison ([b39ee59](https://github.com/sitkoru/Sitko.Core/commit/b39ee59e7b9d4424e584e5a6ccf2aadcfd5ca0aa))
* **validation:** use Equals to avoid reference comparison ([c25c948](https://github.com/sitkoru/Sitko.Core/commit/c25c948b0a5030717ab8510c3174ca68084a9f0a))

# [8.23.0](https://github.com/sitkoru/Sitko.Core/compare/8.22.0...8.23.0) (2021-11-17)


### Features

* **grpc:** add helpers to process incoming streams in grpc server ([ae66c5b](https://github.com/sitkoru/Sitko.Core/commit/ae66c5ba49b56ffadae04ad18ac8958fdabd5cdd))

# [8.22.0](https://github.com/sitkoru/Sitko.Core/compare/8.21.0...8.22.0) (2021-11-11)


### Bug Fixes

* **pdf:** download browser when BrowserWsEndpoint and PUPPETEER_EXECUTABLE_PATH is not provided ([38de058](https://github.com/sitkoru/Sitko.Core/commit/38de05879d0b4b0c87872f403ef78b5df831f357))


### Features

* **swagger:** remove swagger auth, add endpoint configuration ([87743d9](https://github.com/sitkoru/Sitko.Core/commit/87743d95528451c357d363f99f071181c9f8db7e))

# [8.21.0](https://github.com/sitkoru/Sitko.Core/compare/8.20.1...8.21.0) (2021-11-10)


### Features

* **email:** return IOperation result from email sender operations ([e95e0ec](https://github.com/sitkoru/Sitko.Core/commit/e95e0eccd531da29220d4af959fcd761dc0a584e))

## [8.20.1](https://github.com/sitkoru/Sitko.Core/compare/8.20.0...8.20.1) (2021-11-10)


### Bug Fixes

* **deps:** bump Npgsql.EntityFrameworkCore.PostgreSQL from 6.0.0-rc.2 to 6.0.0 ([b27301a](https://github.com/sitkoru/Sitko.Core/commit/b27301adee50dcfb924abdf6e7ed88730e6ec793))

# [8.20.0](https://github.com/sitkoru/Sitko.Core/compare/8.19.0...8.20.0) (2021-11-09)


### Features

* **.net:** .NET 5.0.12 ([ceb1af7](https://github.com/sitkoru/Sitko.Core/commit/ceb1af770f6605200eb7ef2c6d3f459e03cbf241))
* **.net:** .NET 6.0.0 ([c456461](https://github.com/sitkoru/Sitko.Core/commit/c456461eea6d7ae2ed49a497744042d8d89977d2))
* **.net:** .NET Core 3.1.21 ([f892395](https://github.com/sitkoru/Sitko.Core/commit/f892395247781ee868f5ccecf5bc7b51ba5f35dd))
* **.net:** use Microsoft.Extensions.* 6.0.0 ([fd56aaf](https://github.com/sitkoru/Sitko.Core/commit/fd56aaf31e3b5a79ec5468d8e422b3eddf0dc9b8))

# [8.19.0](https://github.com/sitkoru/Sitko.Core/compare/8.18.0...8.19.0) (2021-10-27)


### Features

* **db:** support for IDbContextFactory ([d03da84](https://github.com/sitkoru/Sitko.Core/commit/d03da84b1b092f62c4701dad22cca3babc180bb1))

# [8.18.0](https://github.com/sitkoru/Sitko.Core/compare/8.17.0...8.18.0) (2021-10-22)


### Features

* **grpc:** support operation result responses in gRPC services ([d4e9c6c](https://github.com/sitkoru/Sitko.Core/commit/d4e9c6c39a46d7f0e9ee310532133c9e36bc14a3))

# [8.17.0](https://github.com/sitkoru/Sitko.Core/compare/8.16.0...8.17.0) (2021-10-21)


### Features

* **results:** allow to specify custom error message for exception ([a8fa681](https://github.com/sitkoru/Sitko.Core/commit/a8fa681c23603dc7243c58639f432f2443323655))

# [8.16.0](https://github.com/sitkoru/Sitko.Core/compare/8.15.0...8.16.0) (2021-10-20)


### Features

* **consul:** use maintained Consul library ([5630e7b](https://github.com/sitkoru/Sitko.Core/commit/5630e7bfc1676d178db8dc55f1dedafed4dd40e6))

# [8.15.0](https://github.com/sitkoru/Sitko.Core/compare/8.14.0...8.15.0) (2021-10-19)


### Features

* **results:** add classes for operation results ([c6a9c2b](https://github.com/sitkoru/Sitko.Core/commit/c6a9c2b06b59e3f8a85c5bbf7f4a005ba9065657))

# [8.14.0](https://github.com/sitkoru/Sitko.Core/compare/8.13.0...8.14.0) (2021-10-19)


### Features

* **antdesign:** update AntDesign to 0.10.1 ([94a1a69](https://github.com/sitkoru/Sitko.Core/commit/94a1a69ae59209a0fdf5f717b803abdad5dd0886))
* **logging:** update Serilog.Exceptions to 7.1.0 ([c1d1f27](https://github.com/sitkoru/Sitko.Core/commit/c1d1f27145538f48f41ae7243994620df1d8f564))
* **vault:** update VaultSharp.Extensions.Configuration to 0.4.0 ([962e685](https://github.com/sitkoru/Sitko.Core/commit/962e685793e62ead1c8cece1474625a7d7c202b6))

# [8.13.0](https://github.com/sitkoru/Sitko.Core/compare/8.12.0...8.13.0) (2021-10-19)


### Features

* **antdesign:** update AntDesign to 0.10.1 ([544f99c](https://github.com/sitkoru/Sitko.Core/commit/544f99c567a534838b956ed52f2e53e79bfa9dd5))
* **logging:** update Serilog.Exceptions to 7.1.0 ([d383a85](https://github.com/sitkoru/Sitko.Core/commit/d383a8594ba922c76725390728c11094cc0338d1))

# [8.12.0](https://github.com/sitkoru/Sitko.Core/compare/8.11.1...8.12.0) (2021-10-19)


### Bug Fixes

* **app:** disallow adding modules after app host was built ([ee23cd6](https://github.com/sitkoru/Sitko.Core/commit/ee23cd638b68b17c81050069090fd97bcb00a3c1))
* **modules:** fix generating modules options for nested modules keys ([63f8cd8](https://github.com/sitkoru/Sitko.Core/commit/63f8cd8a058688ab9344830a6810f5c851871278))


### Features

* **consul:** ConsulWebModule no longer inherits ConsulModule ([dd495c8](https://github.com/sitkoru/Sitko.Core/commit/dd495c86a2cfd1604889d9eb23db82d60a983b75))

## [8.11.1](https://github.com/sitkoru/Sitko.Core/compare/8.11.0...8.11.1) (2021-10-18)


### Bug Fixes

* **consul:** use our own consul health check ([13e3683](https://github.com/sitkoru/Sitko.Core/commit/13e36834005e39d214115c7e7c8cdce9fec7041a))

# [8.11.0](https://github.com/sitkoru/Sitko.Core/compare/8.10.0...8.11.0) (2021-10-18)


### Features

* **consul:** use IConsulClientProvider to allow monitor consul uri change ([4ee774c](https://github.com/sitkoru/Sitko.Core/commit/4ee774cbfe7953aecd9f41f40dcf4888fb837aa6))

# [8.10.0](https://github.com/sitkoru/Sitko.Core/compare/8.9.0...8.10.0) (2021-10-18)


### Features

* **.net:** update .NET 5 to 5.0.11 ([670ad5d](https://github.com/sitkoru/Sitko.Core/commit/670ad5de7d90e3cc5465a985a57fbe3f0550230d))
* **.net:** update .NET 6 to 6.0.0-rc.2 ([30d56b7](https://github.com/sitkoru/Sitko.Core/commit/30d56b7c05cbe62b45d8a69d190437fe5aaaad35))
* **.net:** update .NET Core 3.1 to 3.1.20 ([be7ca34](https://github.com/sitkoru/Sitko.Core/commit/be7ca3475f86b0b6c920e606d214790284896c88))

# [8.9.0](https://github.com/sitkoru/Sitko.Core/compare/8.8.0...8.9.0) (2021-09-25)


### Bug Fixes

* **efcore:** revert efcore to preview7 until new npgsql is released ([be46411](https://github.com/sitkoru/Sitko.Core/commit/be46411948eb9f43723e6d72c8b0e68713032f01))
* **identity:** return reference to Microsoft.AspNetCore.Identity.EntityFrameworkCore for .NET 6 ([cb527c7](https://github.com/sitkoru/Sitko.Core/commit/cb527c77a8ed5db73cf6b546c3447150acfe4ec3))
* **identity:** return reference to Microsoft.AspNetCore.Identity.EntityFrameworkCore for .NET 6 ([5ca9f11](https://github.com/sitkoru/Sitko.Core/commit/5ca9f11182127cd084e484f36f9ca9c22647c6b7))
* **storage:** use utc so new npgsql can save it to database ([c191dfb](https://github.com/sitkoru/Sitko.Core/commit/c191dfb5880025864412a8bb8a1c3290f3ee7de3))


### Features

* **.net:** .NET 6.0.0 RC1 ([5ec5c1d](https://github.com/sitkoru/Sitko.Core/commit/5ec5c1d10b00fe5aa48a0a7b06f61d50778003a3))
* **.net:** .NET 6.0.0 RC1 ([25a5d7e](https://github.com/sitkoru/Sitko.Core/commit/25a5d7e6a472eca9a2085ac32a6cf179d19bf641))
* **.net:** .NET 6.0.0 RC1 ([894b6c6](https://github.com/sitkoru/Sitko.Core/commit/894b6c69b2e70814b73e16884b6e493621309fce))
* **db:** update ef core to 6.0.0-rc.1 ([c9a6551](https://github.com/sitkoru/Sitko.Core/commit/c9a6551864e6644215f63b980dfb21ab9d9f04ee))
* **storage:** use raw sql for storage metadata deletion ([0b18d2a](https://github.com/sitkoru/Sitko.Core/commit/0b18d2ad8c65501cfc1f9477f31d0b598e6ad491))

# [8.8.0](https://github.com/sitkoru/Sitko.Core/compare/8.7.0...8.8.0) (2021-09-22)


### Features

* **xunit:** include log context in xunit logs ([18d6602](https://github.com/sitkoru/Sitko.Core/commit/18d660282e83327041572ce791be8f1f26249578))

# [8.7.0](https://github.com/sitkoru/Sitko.Core/compare/8.6.1...8.7.0) (2021-09-21)


### Features

* **antblazor:** add debug logs to list operations ([b3c378d](https://github.com/sitkoru/Sitko.Core/commit/b3c378d2a6f8a8cc36faa6b6501e83f57f644f9f))
* **antdesign:** bump AntDesign to 0.10.0 ([d8970a9](https://github.com/sitkoru/Sitko.Core/commit/d8970a9afac5bdacd7d158d0ced3951bdaab3712))
* **storage:** update AWSSDK.S3 to 3.7.3.1 ([8c442d2](https://github.com/sitkoru/Sitko.Core/commit/8c442d23a016c55e9857ffc9c0751d981a75ddee))

## [8.6.1](https://github.com/sitkoru/Sitko.Core/compare/8.6.0...8.6.1) (2021-09-15)


### Bug Fixes

* **blazor:** rework form hooks to always call them last ([642b15a](https://github.com/sitkoru/Sitko.Core/commit/642b15ab0bb00e6f5add31dd3fa0b3306b72bcef))

# [8.6.0](https://github.com/sitkoru/Sitko.Core/compare/8.5.0...8.6.0) (2021-09-15)


### Features

* **imgproxy:** move imgproxy url generator to separate module ([7056422](https://github.com/sitkoru/Sitko.Core/commit/70564225308e75922a245ea63a2589fb4fa2699b))

# [8.5.0](https://github.com/sitkoru/Sitko.Core/compare/8.4.0...8.5.0) (2021-09-15)


### Features

* **blazor:** check if for was initialized already ([3875cf1](https://github.com/sitkoru/Sitko.Core/commit/3875cf157be8d8463f22950ec966a3a017a21cf3))
* **blazor:** expose IsInitialized from BaseComponent to child components ([d84945a](https://github.com/sitkoru/Sitko.Core/commit/d84945a23ba5f83a4727a8ddcaccf98925380f35))
* **postgres:** upd Npgsql.EntityFrameworkCore.PostgreSQL from 5.0.7 to 5.0.10 ([5500878](https://github.com/sitkoru/Sitko.Core/commit/55008782e9bb36d06a72c305d8807b8a2ce5b313))

# [8.4.0](https://github.com/sitkoru/Sitko.Core/compare/8.3.1...8.4.0) (2021-09-15)


### Bug Fixes

* **ci:** use specific .NET 6 version ([e6c0fd4](https://github.com/sitkoru/Sitko.Core/commit/e6c0fd41f6bf851b28bd56d3301e929cfa21d457))
* **identity:** return reference to Microsoft.AspNetCore.Identity.EntityFrameworkCore for .NET 6 ([82e3238](https://github.com/sitkoru/Sitko.Core/commit/82e3238cc658cdeecf8b3aa625feb505cfc1dcb4))


### Features

* **.net:** .NET 3.1.19 ([de133e3](https://github.com/sitkoru/Sitko.Core/commit/de133e38bc90f73529fc1024bfdc56264914f605))
* **.net:** .NET 5.0.10 ([5d70681](https://github.com/sitkoru/Sitko.Core/commit/5d70681da06c8466ff199079c6224a72cb994ba9))

## [8.3.1](https://github.com/sitkoru/Sitko.Core/compare/8.3.0...8.3.1) (2021-09-14)


### Bug Fixes

* **validation:** fix validating graph when some validators are missing ([996d43d](https://github.com/sitkoru/Sitko.Core/commit/996d43d4dcf81c846d825034b3a6ed98cce0a7a7))

# [8.3.0](https://github.com/sitkoru/Sitko.Core/compare/8.2.1...8.3.0) (2021-09-14)


### Features

* **blazor:** reimplement Blazor FluentValidator on top of FluentGraphValidator ([5f8d27d](https://github.com/sitkoru/Sitko.Core/commit/5f8d27dfa103fc9c1a95c33bd2f89b33e6c4d92b))
* **repository:** use FluentGraphValidator for validation ([ef262e1](https://github.com/sitkoru/Sitko.Core/commit/ef262e182e412f301e41d163767042d135c39688))
* **validation:** add FluentGraphValidator to validate model graph ([51c4147](https://github.com/sitkoru/Sitko.Core/commit/51c4147f8a3751964451c9998ae8cc7794cdcd21))

## [8.2.1](https://github.com/sitkoru/Sitko.Core/compare/8.2.0...8.2.1) (2021-09-14)


### Bug Fixes

* **nuget:** add missing SourceLink to packages ([03b8fdc](https://github.com/sitkoru/Sitko.Core/commit/03b8fdc2c60164ad7fcae043a18a236cca795111))

# [8.2.0](https://github.com/sitkoru/Sitko.Core/compare/8.1.0...8.2.0) (2021-09-06)


### Features

* **blazor:** add after initialization hooks to BaseComponent ([8ab75ff](https://github.com/sitkoru/Sitko.Core/commit/8ab75ff7385e4b1cf037df2abfb35f77b5b6e41e))

# [8.1.0](https://github.com/sitkoru/Sitko.Core/compare/8.0.1...8.1.0) (2021-09-06)


### Features

* **grpc:** add option to configure WebHost/Kestrel for gRPC server module ([f2a64e2](https://github.com/sitkoru/Sitko.Core/commit/f2a64e27ea2b9c70f4117d42fcc8096e992d48da))

## [8.0.1](https://github.com/sitkoru/Sitko.Core/compare/8.0.0...8.0.1) (2021-09-05)


### Bug Fixes

* **app:** require logging and configuration modules to implement interface so we run options validation only when needed ([4c3a0e7](https://github.com/sitkoru/Sitko.Core/commit/4c3a0e703494f7903e7b9339483fcdc4f0b802cf))

# [8.0.0](https://github.com/sitkoru/Sitko.Core/compare/7.22.0...8.0.0) (2021-09-03)


### Bug Fixes

* **app:** force validate module options on logging and services configuration steps only ([49907d2](https://github.com/sitkoru/Sitko.Core/commit/49907d296bc26f03a121bf64f716c00612ef15e0))
* **app:** return run command to supported list ([5bc1cc2](https://github.com/sitkoru/Sitko.Core/commit/5bc1cc212a4190dcfd88f43d46a4a513c2d9513a))
* **app:** rework commands implementation ([cd289bb](https://github.com/sitkoru/Sitko.Core/commit/cd289bb6648882322257b68dd7f34e5b0504664d))
* **app:** throw OptionsValidationException on module options validation failure ([7fb160a](https://github.com/sitkoru/Sitko.Core/commit/7fb160ac58f46d785f2117d2615ec4f3aad4dd60))
* **blazor:** don't add change for new entity ([888acab](https://github.com/sitkoru/Sitko.Core/commit/888acab7a3777f5df725bbc153bd3d92e4fb047a))
* **blazor:** notify state change on form validation change ([e64d22c](https://github.com/sitkoru/Sitko.Core/commit/e64d22c101d0689c54073e6852f6961594f9fb89))
* **blazor:** recalculate form state on reset ([60a0e4f](https://github.com/sitkoru/Sitko.Core/commit/60a0e4f4b54a6babe1fc42a384923000a970d7ec))
* **blazor:** remove old code ([191a969](https://github.com/sitkoru/Sitko.Core/commit/191a9692440886c2bb6a0ad09680f608c796bbdd))
* **blazor:** reset repository form on EntityId parameter change ([464fe9c](https://github.com/sitkoru/Sitko.Core/commit/464fe9c3fef799375e1d097af0f0a7d61aec3ab7))
* **blazor:** set currentEntityId after initialization ([6a10471](https://github.com/sitkoru/Sitko.Core/commit/6a104718ccd328e27dbe25295f669014bff0a39f))
* **blazor:** use JsonHelper for debug form view ([dde0d72](https://github.com/sitkoru/Sitko.Core/commit/dde0d72eafdc143e085e1cd4589c9dcd6c9c6744))
* **blazor:** use old parameter value to decide if form needs reset ([56f8d14](https://github.com/sitkoru/Sitko.Core/commit/56f8d141280652abfe132b7dab6ede0eed01c4a4))
* **ci:** fix blazor workflow paths ([99b0407](https://github.com/sitkoru/Sitko.Core/commit/99b040735cb9bfd57af20453235f9ee503aae59a))
* **ci:** one more time ([99d8e49](https://github.com/sitkoru/Sitko.Core/commit/99d8e49dbfb2fad431c819424b1f1384160f1d3d))
* **ci:** run semantic-release on public runner ([35056fc](https://github.com/sitkoru/Sitko.Core/commit/35056fc39880bc409b3d31bb36e8431d7f9dba7d))
* **demo:** raise log level ([d14f4ff](https://github.com/sitkoru/Sitko.Core/commit/d14f4ffef8f05909231b739dc8ea870af5d2adee))
* **demo:** rework edit page demo ([21544ba](https://github.com/sitkoru/Sitko.Core/commit/21544baa2d6693f3de0223840f6965a11c827eb3))
* **demo:** upd demo layout ([d65fa76](https://github.com/sitkoru/Sitko.Core/commit/d65fa761e25a05498a0978ec2119c66cb41fafaf))
* **fluentvalidation:** rework validator to support disposing ([0745f58](https://github.com/sitkoru/Sitko.Core/commit/0745f58b6e5345ebd307ca216568591c5db62cb1))
* **repository:** always copy current collection value for change entry ([80682f0](https://github.com/sitkoru/Sitko.Core/commit/80682f0165a02b5e667b72bd2dda6602dbdfddba))
* **repository:** detach all skip navigation subentities before load collection from db ([c99b82b](https://github.com/sitkoru/Sitko.Core/commit/c99b82b9a38b00fbfa004a879ad0c1118cd1e87e))
* **repository:** fix attaching external entities graph to dbContext ([5d04ad2](https://github.com/sitkoru/Sitko.Core/commit/5d04ad2e90d5b25318d33e1ff74a4d786e7d5f7d))
* **repository:** fix change state detection for whole entity ([ec6df27](https://github.com/sitkoru/Sitko.Core/commit/ec6df273606963522a626e82e06039f29eb04c14))
* **repository:** force load collection value from database instead of setting it to null ([bc612d1](https://github.com/sitkoru/Sitko.Core/commit/bc612d1166260dd999c95b81f140829ebff8676e))
* **repository:** if there is no changes - we must return Unchanged ([5559ae5](https://github.com/sitkoru/Sitko.Core/commit/5559ae52cf5e800f37f132fb3d61bd8aa74426e4))
* **repository:** process skip navigations only once ([33a6988](https://github.com/sitkoru/Sitko.Core/commit/33a69889fcb09da2adc8d4c86875dc42f5caf208))
* **repository:** skip fixups only if there is no changes inside ([f1d9b88](https://github.com/sitkoru/Sitko.Core/commit/f1d9b88f05273005251c48b59070bf795f65ca89))
* **vault:** fix token renew. add more logs. ([51b051e](https://github.com/sitkoru/Sitko.Core/commit/51b051e8ca3565274cecae1b2f779e43b73550be))


### Features

* v8 release ([79e39da](https://github.com/sitkoru/Sitko.Core/commit/79e39da312baf360d14f10cfe86be78db599e23d))
* **antblazor:** add AntFormItem component with hint support ([6c2e5a1](https://github.com/sitkoru/Sitko.Core/commit/6c2e5a1d69d73f9b2e8fb569fd03a524cf44fb4a))
* **antblazor:** add debug view for ant forms ([8356888](https://github.com/sitkoru/Sitko.Core/commit/83568886d9b30d1046f0e72c01a1b50569a076de))
* **app:** always try to validate module options on creation ([6965e4f](https://github.com/sitkoru/Sitko.Core/commit/6965e4f863c8e4da6f9c4040a98b1c1527f52c06))
* **auth:** add basic auth module options validation ([2e7dfcf](https://github.com/sitkoru/Sitko.Core/commit/2e7dfcfac5fcb94de5669e1ec569e027e57b6060))
* **auth:** unify auth modules and their options ([e271d90](https://github.com/sitkoru/Sitko.Core/commit/e271d904935e55b484a73995fa887110a17f4c3c))
* **blazor:** add additional information into list sort and filters about properties and values ([f7561fd](https://github.com/sitkoru/Sitko.Core/commit/f7561fd97e185896f4ae83ac2343693da58a4554))
* **blazor:** add EditorRequired attribute where possible ([80586a7](https://github.com/sitkoru/Sitko.Core/commit/80586a75069baf15567582e00192a2622900c9e4))
* **blazor:** move localization provider injection to BaseComponent ([dc7f5b5](https://github.com/sitkoru/Sitko.Core/commit/dc7f5b5197034ed8fb5669f5545390955c6c4e71))
* **blazor:** original entity value should come from different repository ([898fcfa](https://github.com/sitkoru/Sitko.Core/commit/898fcfab3c93e26d99787d6be3c8133a81c7c0d1))
* **blazor:** redo ant lists, create base list with markup and then inherit repository list from it ([7f847a9](https://github.com/sitkoru/Sitko.Core/commit/7f847a92e4142732b4603d0567c8fef2f72af31d))
* **demo:** demo AntFormItem with hint ([9582522](https://github.com/sitkoru/Sitko.Core/commit/9582522795e8978ad6526cfb02c5f1592fe07f88))
* **demo:** demo form debug and reset ([de73552](https://github.com/sitkoru/Sitko.Core/commit/de73552abf2398b5fb451fd41f359255590a6d22))
* **grpc:** add ErrorsString to ApiResponseError for simpler logging ([0051e80](https://github.com/sitkoru/Sitko.Core/commit/0051e80b3a65d236befd30f206784b9171a181ae))
* **grpc:** add IsSuccess shortcut to IGrpcResponse ([213c5d3](https://github.com/sitkoru/Sitko.Core/commit/213c5d38aa2d3ebf6cbe70aead60ff60402105e6))
* **grpc:** allow separate options per grpc client ([a059a2d](https://github.com/sitkoru/Sitko.Core/commit/a059a2de6352d4297921211b995a21e63947909e))
* **grpc:** don't need TRequest generic parameter, accept any IGrpcRequest ([159d4d6](https://github.com/sitkoru/Sitko.Core/commit/159d4d67fec0bd3bcc4a760bc27e87c9c46e62f9))
* **repository:** add more operators to repository conditions ([07bc24f](https://github.com/sitkoru/Sitko.Core/commit/07bc24fa69d5bb9109a2ae4a42ef08eb3a2688b1))
* **repository:** back to attaching entity to context on external update ([05e773d](https://github.com/sitkoru/Sitko.Core/commit/05e773d356b788e00ade34ff1a101d7ad32b62d4))
* **repository:** improve updating external entities ([5099e5e](https://github.com/sitkoru/Sitko.Core/commit/5099e5e78fca93170f2265489ef31437bf95cb68))
* **web:** remove razor runtime compilation from core. target application should handle it. ([7e244e7](https://github.com/sitkoru/Sitko.Core/commit/7e244e7243e3a4dc57852a6d1f87d8bd86e97468))


### Performance Improvements

* **repository:** use Any to improve performance ([ea47ab2](https://github.com/sitkoru/Sitko.Core/commit/ea47ab2357abbe474dc9c1917b076adf6c7be420))


### BREAKING CHANGES

* v8!
