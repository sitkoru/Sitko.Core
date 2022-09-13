## [9.10.1](https://github.com/sitkoru/Sitko.Core/compare/9.10.0...9.10.1) (2022-09-13)


### Bug Fixes

* **postgres:** use extension to pass schema to db context ([718668e](https://github.com/sitkoru/Sitko.Core/commit/718668edae8606682e6c74509b00bb64c3b9aec0))

# [9.10.0](https://github.com/sitkoru/Sitko.Core/compare/9.9.0...9.10.0) (2022-09-12)


### Features

* **postgres:** support different schemas ([21b37e3](https://github.com/sitkoru/Sitko.Core/commit/21b37e329c2e55b20a733ee820f881456541d2a6))

# [9.9.0](https://github.com/sitkoru/Sitko.Core/compare/9.8.2...9.9.0) (2022-09-01)


### Features

* **hangfire:** now we can use IHangfireModule to describe dependency from Hangfire module ([067bb79](https://github.com/sitkoru/Sitko.Core/commit/067bb79b8b2ee3fc0c865c203a1daf0e44651b59))

## [9.8.2](https://github.com/sitkoru/Sitko.Core/compare/9.8.1...9.8.2) (2022-08-15)


### Bug Fixes

* **repository:** underlying collection should be list to allow adding new conditions ([1a1441c](https://github.com/sitkoru/Sitko.Core/commit/1a1441cdc6cb0b7e6bd5360652256419d3472d3e))

## [9.8.1](https://github.com/sitkoru/Sitko.Core/compare/9.8.0...9.8.1) (2022-08-12)


### Bug Fixes

* added condition for displaying header container ([dd4f00f](https://github.com/sitkoru/Sitko.Core/commit/dd4f00feb5c4a2f10ea180a49f12b85a2486dd87))

# [9.8.0](https://github.com/sitkoru/Sitko.Core/compare/9.7.1...9.8.0) (2022-08-11)


### Bug Fixes

* **repository:** pass correct type for value parsing ([c14a01f](https://github.com/sitkoru/Sitko.Core/commit/c14a01f8a1cfdcb9cf9a1f29985993cb893b8cd5))


### Features

* **consul:** upgrade Consul from 1.6.10.6 to 1.6.10.7 ([18adc44](https://github.com/sitkoru/Sitko.Core/commit/18adc44599f513b86765003b2a4e16bd754b37e2))
* **demo:** upd deps ([f0fd37d](https://github.com/sitkoru/Sitko.Core/commit/f0fd37d0b7fad45ba16a9766a7238c80d06cc368))
* **dotnet:** .NET 6.0.8 ([f285fd8](https://github.com/sitkoru/Sitko.Core/commit/f285fd8fc95293e5f3fb05ab027d771bbf1cd83c))
* **dotnet:** .NET Core 3.1.28 ([abb62b2](https://github.com/sitkoru/Sitko.Core/commit/abb62b2faae2269bf6303976e92a57c78a949bfd))
* **mudblazor:** upgrade MudBlazor from 6.0.12 to 6.0.14 ([e0b2377](https://github.com/sitkoru/Sitko.Core/commit/e0b23775e28c65a97d2a2026063f8b680d694abd))
* **protobuf:** upgrade Google.Protobuf from 3.21.3 to 3.21.5 ([5aeb079](https://github.com/sitkoru/Sitko.Core/commit/5aeb0797feb681775339ce6d4e3c6414408a7e69))
* **repository:** helpers for grpc filters ([be7a1c3](https://github.com/sitkoru/Sitko.Core/commit/be7a1c3e4ce73fa903c38d3ac53378b0bf9b09bc))
* **s3storage:** upgrade AWSSDK.S3 from 3.7.9.30 to 3.7.9.37 ([1c8f54e](https://github.com/sitkoru/Sitko.Core/commit/1c8f54e3f55958fd6f3c3c322d25f07483370824))
* **vault:** support only .NET 6 ([45dd0ef](https://github.com/sitkoru/Sitko.Core/commit/45dd0efd3ef75740440f1fe7921ead11c230fa08))
* **vault:** upgrade VaultSharp.Extensions.Configuration from 0.4.3 to 0.5.1 ([4fa4e42](https://github.com/sitkoru/Sitko.Core/commit/4fa4e4282324a64f336ea98ae691480ef961fec4))
* **vault:** use different vault packages for different runtimes ([e13d3ca](https://github.com/sitkoru/Sitko.Core/commit/e13d3ca7500f0c69bca0df9c786e4912ec4f32ea))
* **xunit:** upgrade Microsoft.NET.Test.Sdk from 17.2.0 to 17.3.0 ([f35eb6e](https://github.com/sitkoru/Sitko.Core/commit/f35eb6ec0559e02612a9bdf1573920c799e5fbdf))
* **xunit:** upgrade xunit from 2.4.1 to 2.4.2 ([a6198ac](https://github.com/sitkoru/Sitko.Core/commit/a6198ac52c7970d32011d920121001700c6d8bbd))

## [9.7.1](https://github.com/sitkoru/Sitko.Core/compare/9.7.0...9.7.1) (2022-08-04)


### Bug Fixes

* **identity:** don't crash on empty web host environment ([d75e8cb](https://github.com/sitkoru/Sitko.Core/commit/d75e8cbff39f5c71a744a4af51053cb4e61ec3e8))

# [9.7.0](https://github.com/sitkoru/Sitko.Core/compare/9.6.1...9.7.0) (2022-08-01)


### Features

* **swagger:** expose swagger gen options ([9c41326](https://github.com/sitkoru/Sitko.Core/commit/9c413268cee6b77900d8b3acf0e199c25898bf78))
* **swagger:** support Swashbuckle.AspNetCore.Annotations ([09801b9](https://github.com/sitkoru/Sitko.Core/commit/09801b9b00a7bf619e8fcd668cfc2fe8d4f3c0c5))

## [9.6.1](https://github.com/sitkoru/Sitko.Core/compare/9.6.0...9.6.1) (2022-08-01)


### Bug Fixes

* **s3metadata:** fix s3 storage metadata config key ([08b58ba](https://github.com/sitkoru/Sitko.Core/commit/08b58ba7fc9db4bbbfe6eab6a10f62447a0df83b))
* **swagger:** correct bearer authentication ([3b8744b](https://github.com/sitkoru/Sitko.Core/commit/3b8744b46472b2cc2fcdf1a94576f6b36f3be6b2))

# [9.6.0](https://github.com/sitkoru/Sitko.Core/compare/9.5.1...9.6.0) (2022-07-27)


### Features

* **postgres:** allow to configure all connection options ([e094472](https://github.com/sitkoru/Sitko.Core/commit/e09447269efafbcb4d1bec18f29e1551c52f743d))

## [9.5.1](https://github.com/sitkoru/Sitko.Core/compare/9.5.0...9.5.1) (2022-07-26)


### Bug Fixes

* **efrepository:** don't run change detection on whole graph ffs ([f996b47](https://github.com/sitkoru/Sitko.Core/commit/f996b47c33d04fa4fa6326d218bce5163dd78a20))

# [9.5.0](https://github.com/sitkoru/Sitko.Core/compare/9.4.0...9.5.0) (2022-07-26)


### Bug Fixes

* **elasticstack:** support ElasticSearch 8 ([99ac7ee](https://github.com/sitkoru/Sitko.Core/commit/99ac7eee23a6c659573d554bfed16f8cc3715be0))


### Features

* **.net:** .NET 6.0.7 ([87451d1](https://github.com/sitkoru/Sitko.Core/commit/87451d1dc1d8fc1b374982256d298678091fab98))
* **.net:** .NET Core 3.1.27 ([7fe4e57](https://github.com/sitkoru/Sitko.Core/commit/7fe4e57f85d5acdc047d35e8d0393cd0d07cd14e))
* **app:** update Serilog.Exceptions from 8.2.0 to 8.3.0 ([388581b](https://github.com/sitkoru/Sitko.Core/commit/388581b7ff631dd0baad472b0647d96963204d88))
* **demo:** upd deps ([ac986eb](https://github.com/sitkoru/Sitko.Core/commit/ac986ebfb97a9d46b6e3283c5076102b50c1fef6))
* **efrepository:** update System.Linq.Dynamic.Core from 1.2.18 to 1.2.19 ([3669686](https://github.com/sitkoru/Sitko.Core/commit/3669686e1956d7e28aee9fce3cfabd79eb281824))
* **elasticsearch:** update NEST from 7.17.2 to 7.17.4 ([f73d8b6](https://github.com/sitkoru/Sitko.Core/commit/f73d8b66fa097641b0f3304068cd4e9bddf19769))
* **grpc:** update gRPC to 2.47.0, Protobuf to 3.21.3 ([faf7050](https://github.com/sitkoru/Sitko.Core/commit/faf705029276adcc1795567a081669a388bef6ec))
* **mudblazor:** update MudBlazor from 6.0.10 to 6.0.12 ([51b09b2](https://github.com/sitkoru/Sitko.Core/commit/51b09b29cd921dfaf8a32432b3efcf55bd85e510))
* **queuenats:** update NATS.Client from 0.14.7 to 0.14.8 ([6ba35c6](https://github.com/sitkoru/Sitko.Core/commit/6ba35c66533577884b7dabc80f40c2bfca610cb0))
* **s3storage:** update AWSSDK.S3 from 3.7.9.18 to 3.7.9.30 ([daf2e10](https://github.com/sitkoru/Sitko.Core/commit/daf2e105158c4df98de197ec13718822e23938c9))
* **swagger:** update AspNetCore from 6.3.1 to 6.4.0 ([0230b23](https://github.com/sitkoru/Sitko.Core/commit/0230b2304666613a3919bffafedcc382bbd7bc47))

# [9.4.0](https://github.com/sitkoru/Sitko.Core/compare/9.3.0...9.4.0) (2022-07-04)


### Bug Fixes

* **repository:** get items count from repository response when !needCount ([d403435](https://github.com/sitkoru/Sitko.Core/commit/d40343599f38703fab9d1768f76380103422c2c1))


### Features

* create wasm demo app ([68bd4b3](https://github.com/sitkoru/Sitko.Core/commit/68bd4b3a946ab9b6b39fb37207135787347ff54d))

# [9.3.0](https://github.com/sitkoru/Sitko.Core/compare/9.2.0...9.3.0) (2022-06-23)


### Features

* **.NET:** .NET 5.0.17 ([ac8b61a](https://github.com/sitkoru/Sitko.Core/commit/ac8b61adb5d718968f2928e3bacac7bae53ed5ca))
* **.NET:** .NET 6.0.6 ([c4b2558](https://github.com/sitkoru/Sitko.Core/commit/c4b2558e1900b79434d4e78991ae8ee55714773c))
* **.NET:** .NET Core 3.1.26 ([c6835cc](https://github.com/sitkoru/Sitko.Core/commit/c6835ccf31e6ebc9d20f90c5a82804996598bdd1))
* **antdesign:** update AntDesign from 0.10.5 to 0.11.0 ([3fa5ec3](https://github.com/sitkoru/Sitko.Core/commit/3fa5ec31c6cbdc9d288983502c1b54371f03c098))
* **app:** update Scrutor from 4.1.0 to 4.2.0 ([d2f819e](https://github.com/sitkoru/Sitko.Core/commit/d2f819ef9d89ef8c72e4be149a8c3d2f5e3d668d))
* **app:** update Serilog.Exceptions from 8.1.0 to 8.2.0 ([51fdf9e](https://github.com/sitkoru/Sitko.Core/commit/51fdf9e094a31a948b3cf1fec0cc17e43867a98d))
* **consul:** update Consul from 1.6.10.5 to 1.6.10.6 ([46d6ec3](https://github.com/sitkoru/Sitko.Core/commit/46d6ec3e4813fb564109a982893b2e2cb62fb2e0))
* **demo:** upd deps ([1add3cc](https://github.com/sitkoru/Sitko.Core/commit/1add3cc4bb986365ea189bd6ee7c8a346112367b))
* **elasticstack:** update Elastic.Apm.NetCoreAll from 1.14.1 to 1.16.1 ([72bb40e](https://github.com/sitkoru/Sitko.Core/commit/72bb40ec0fdb4259eb1528d28f1d3e5b96a1189e))
* **elastic:** update NEST from 7.17.1 to 7.17.2 ([69c4bbf](https://github.com/sitkoru/Sitko.Core/commit/69c4bbfbde9e3af16f74745278550d08f3fbc89b))
* **email:** update HtmlAgilityPack from 1.11.42 to 1.11.43 ([2a2d094](https://github.com/sitkoru/Sitko.Core/commit/2a2d094db3e369811d0d7caed1853276f1566957))
* **grpc:** update gRPC to 2.46.0, Protobuf to 3.21.1 ([643b13f](https://github.com/sitkoru/Sitko.Core/commit/643b13f89e4c3d07c4c996dbf24fd0da6e54245c))
* **hangfire:** update Hangfire.AspNetCore from 1.7.29 to 1.7.30 ([09e5c3c](https://github.com/sitkoru/Sitko.Core/commit/09e5c3cc4c20e6d629585f4ac6897862ef7724e6))
* **nats:** update NATS.Client from 0.14.6 to 0.14.7 ([10e67cc](https://github.com/sitkoru/Sitko.Core/commit/10e67ccd8aae1b3995dc05a446ef0f187629978c))
* **postgres:** update Npgsql.EntityFrameworkCore.PostgreSQL from 6.0.4 to 6.0.5 ([c3f0db8](https://github.com/sitkoru/Sitko.Core/commit/c3f0db838a907295e778341cdd868d19558f2664))
* **smtp:** update MailKit from 3.2.0 to 3.3.0 ([0bfa264](https://github.com/sitkoru/Sitko.Core/commit/0bfa2644e05c5442a7e7a8320516d09675492cfc))
* **storages3:** update AWSSDK.S3 from 3.7.8.28 to 3.7.9.18 ([8f6a078](https://github.com/sitkoru/Sitko.Core/commit/8f6a0782f8a2c286adb8b9605df602613f4e6906))
* **vault:** update VaultSharp.Extensions.Configuration from 0.4.2 to 0.4.3 ([3ba0c9c](https://github.com/sitkoru/Sitko.Core/commit/3ba0c9c66fc2e08cab8a9fc72646f96d6eb9ae78))
* **web:** update Microsoft.Extensions.Caching.StackExchangeRedis from 6.0.4 to 6.0.6 ([949d2e6](https://github.com/sitkoru/Sitko.Core/commit/949d2e6accf2958a20e05739dc2b1d752455b9ac))
* **xunit:** update FluentAssertions from 6.6.0 to 6.7.0 ([bd85cd7](https://github.com/sitkoru/Sitko.Core/commit/bd85cd7d71c837f891acc70ffbbb49f1c1846c0b))
* **xunit:** update Microsoft.NET.Test.Sdk from 17.1.0 to 17.2.0 ([0dfe4f1](https://github.com/sitkoru/Sitko.Core/commit/0dfe4f11664ebe2308f0accf29b8c3e4681349f8))

# [9.2.0](https://github.com/sitkoru/Sitko.Core/compare/9.1.0...9.2.0) (2022-05-26)


### Features

* don't cache s3client ([8ba1e04](https://github.com/sitkoru/Sitko.Core/commit/8ba1e044d69695b737b432a671e9a85e002c6706))

# [9.1.0](https://github.com/sitkoru/Sitko.Core/compare/9.0.0...9.1.0) (2022-05-25)


### Features

* Hangfire 1.7.29 ([f2505de](https://github.com/sitkoru/Sitko.Core/commit/f2505de35013f894b2dbcf76492a96cedcc4f66a))
* **hangfire:** multiple hangfire servers per instance ([393318e](https://github.com/sitkoru/Sitko.Core/commit/393318e059ecdd1d65202ddee990a3a7c025639c))

# [9.0.0](https://github.com/sitkoru/Sitko.Core/compare/8.36.1...9.0.0) (2022-05-06)


### Bug Fixes

* add missing test repositories ([e0b06f0](https://github.com/sitkoru/Sitko.Core/commit/e0b06f0f5af8f3e10593fca626cd586c4bc0917f))
* add param to httpclient ([3ee4c3a](https://github.com/sitkoru/Sitko.Core/commit/3ee4c3ada1a1b85740228d3980f7a54602d3a8a7))
* add RemoteRepositoryQuery parameterless ctor ([24c09c2](https://github.com/sitkoru/Sitko.Core/commit/24c09c2d9ab75521d3f44054dee775c7e760a486))
* add scoped repositoryTransport and remove module options ([37703d4](https://github.com/sitkoru/Sitko.Core/commit/37703d408300a9029cdbdc499f1226c61d0cb0f8))
* add url param to SendHttpRequestAsync() ([e51fe7b](https://github.com/sitkoru/Sitko.Core/commit/e51fe7bf6c33ed14594c648e2d1e1b7dc064ac94))
* added support for CancellationToken ([35faf17](https://github.com/sitkoru/Sitko.Core/commit/35faf1752311006065c703597e68bba0826b68aa))
* **app:** fix IsProduction override in HostedApplication ([d73a3b7](https://github.com/sitkoru/Sitko.Core/commit/d73a3b750d6d65b9bcc38ef78e0073cf009ecb1c))
* **app:** fix module options validation ([ea1a14e](https://github.com/sitkoru/Sitko.Core/commit/ea1a14eaf936c1199b16087f7c5ee316751809df))
* **app:** get assembly name and version from current application instance ([f7a88dd](https://github.com/sitkoru/Sitko.Core/commit/f7a88dd4fc661e894e94cfe98c82089e4e9dea48))
* **app:** reading env name should be case-insensitive ([a75f11b](https://github.com/sitkoru/Sitko.Core/commit/a75f11b4e2d29b0cc3800c156278510012803468))
* **app:** use existing list of enabled modules ([8a12a3a](https://github.com/sitkoru/Sitko.Core/commit/8a12a3abb29982f8a19c169220d2214bf9af3f8f))
* change GetAll return type ([39d0880](https://github.com/sitkoru/Sitko.Core/commit/39d0880f21e641b29c91cbe8d00f53450c730f0c))
* change methods param type to SerializedQuery ([5557334](https://github.com/sitkoru/Sitko.Core/commit/55573344c9eec24e29858b5acdcf13dd7fb241b2))
* change options module inheritance ([9b00b89](https://github.com/sitkoru/Sitko.Core/commit/9b00b89d5d1700e21c85e3b68b0773132a66a859))
* change route params to Uri type and drop ctor ([21c8186](https://github.com/sitkoru/Sitko.Core/commit/21c81865a496a6d9e9251a45ac5c51ef1f9c2dac))
* change target framework for remote repo server project ([6144a10](https://github.com/sitkoru/Sitko.Core/commit/6144a10db9762aa6305184e3f9f254a3052c2cb7))
* change transport module connection ([954585c](https://github.com/sitkoru/Sitko.Core/commit/954585c6649c1e7ede91efb93d258669da4c9bb8))
* do not throw invalid operation on not success result ([a411e45](https://github.com/sitkoru/Sitko.Core/commit/a411e457c79de69d8d09f84f3b81a4c9dc0b467f))
* drop unused methods, change methods request Type, fix methods param ([5fb8f87](https://github.com/sitkoru/Sitko.Core/commit/5fb8f872e53d89771d9823a584941f721c07bfac))
* **efrepository:** await refresh result ([ccd407e](https://github.com/sitkoru/Sitko.Core/commit/ccd407ea62338aea197d301095df4d787050283b))
* fix repository module initializing ([e815cc8](https://github.com/sitkoru/Sitko.Core/commit/e815cc86400fff14bb689ea286eda8ae474ff3e1))
* fixed getting name of desired entity type for controller route ([8eb01c3](https://github.com/sitkoru/Sitko.Core/commit/8eb01c39ba7937bf804c37ca905e9e91cbf1c52b))
* fixed include query building ([cebf568](https://github.com/sitkoru/Sitko.Core/commit/cebf568b6cc52e3d17c05794e4f0807220a5e00f))
* fixed transport application extension ([af6819d](https://github.com/sitkoru/Sitko.Core/commit/af6819d9b8d73ad0b137663b1cafc973bd88e5f5))
* **grpc:** don't need separate module, just add external and configure handler wrapper ([97527d1](https://github.com/sitkoru/Sitko.Core/commit/97527d187eecaafc492ded457c9506cdebc3d769))
* implement missing select members from interface ([86a97f2](https://github.com/sitkoru/Sitko.Core/commit/86a97f27f98bb1a4b15939098a5302d6141b331e))
* initialize collection ([3ac46ec](https://github.com/sitkoru/Sitko.Core/commit/3ac46ec0dfb04fc999112b84cf5ed95d7a681ed2))
* initialize stateCompressor from constructor ([eb63b7e](https://github.com/sitkoru/Sitko.Core/commit/eb63b7e8ec785ce397379a0db9607288850cd615))
* invoke GetParamsFromUrl ([9baffc6](https://github.com/sitkoru/Sitko.Core/commit/9baffc6db620cc78c70a35f46af768be0905817f))
* lazy init for app context options ([c94d703](https://github.com/sitkoru/Sitko.Core/commit/c94d703f7380170fa767650ff9e135d59c8063a5))
* make BaseRemoteRepo non abstract ([e73f521](https://github.com/sitkoru/Sitko.Core/commit/e73f52194b59ad44d43f6579cb539986dda802e8))
* make DateParseHandling equals DateTimeOffset to avoid skipping specified timezone ([fd1840e](https://github.com/sitkoru/Sitko.Core/commit/fd1840e921a1237fb624da80bd49947a4ac648a7))
* make DoSaveSync return CompletedTask ([dc6c468](https://github.com/sitkoru/Sitko.Core/commit/dc6c468d242e1cc9abafdec411bb2eb290de72be))
* make transport validator non abstract ([3a91ba6](https://github.com/sitkoru/Sitko.Core/commit/3a91ba607ed7da202152a8d0d5524599d4076236))
* move repository transport injection into repository context ([8497ea1](https://github.com/sitkoru/Sitko.Core/commit/8497ea14f8f0c2dba370e32535e2d09730b63b5c))
* move scope to data folder, add repositories in app ([0265742](https://github.com/sitkoru/Sitko.Core/commit/0265742827d91162e620510484e479f49d8c17fb))
* **mudblazor:** await metadata result ([624f5e3](https://github.com/sitkoru/Sitko.Core/commit/624f5e34e3551944a40f267a7fcd43291481fa68))
* **mudBlazor:** change RowsPerPage in init ServerReloadAsync ([a33c274](https://github.com/sitkoru/Sitko.Core/commit/a33c27455bb86449b3acfec2d29285b2abd9acf0))
* **mudBlazor:** rename EnableAddFiltersToUrl to EnableUrlNavigation ([d9b2d58](https://github.com/sitkoru/Sitko.Core/commit/d9b2d5884014f17467f82aa363e6ea91b18d58e9))
* **mudblazor:** set stream buffer size to max if MaxFileSize = 0 ([9b5960e](https://github.com/sitkoru/Sitko.Core/commit/9b5960efcfec119537b62f574551f3eaa6b439d4))
* **mudblazor:** use more appropriate preview icon in MudFileUpload ([98cb564](https://github.com/sitkoru/Sitko.Core/commit/98cb5647b7ee201d3b76f4c2e786443522db539f))
* **oidc:** allow configure auto token refresh ([39da17f](https://github.com/sitkoru/Sitko.Core/commit/39da17f366d78174fc177cde3f1ae7f51ccd2d9f))
* register repositories from all assemblies ([440b0c7](https://github.com/sitkoru/Sitko.Core/commit/440b0c73a4fec25733459631b7f726004c33bd77))
* **remoterepository:** avoid snapshot duplications issue ([4176747](https://github.com/sitkoru/Sitko.Core/commit/41767473363cde0a548e32decc196e5fad2179ba))
* **remoterepository:** cleanup transactions code ([6d4609a](https://github.com/sitkoru/Sitko.Core/commit/6d4609adb1bc46675f8e96a782a61c70ae149ad1))
* **remoterepository:** don't need those select methods, reformat code ([625c1b0](https://github.com/sitkoru/Sitko.Core/commit/625c1b0131f78a4bcd572363b407680346aff56a))
* **remoterepository:** fill change with real objects, not strings ([85018ae](https://github.com/sitkoru/Sitko.Core/commit/85018ae455f628390cff20adf1585855239d0b80))
* **remoterepository:** fix sequential includes ([12abda7](https://github.com/sitkoru/Sitko.Core/commit/12abda78ab7f26192a0c5d23e22dae59c9a65f5b))
* **remoterepository:** init properties ([f09301f](https://github.com/sitkoru/Sitko.Core/commit/f09301f1383a53c9b758548e1c1186a6066b8978))
* **remoterepository:** pass sum type as parameter ([d0057e5](https://github.com/sitkoru/Sitko.Core/commit/d0057e56c8f1eda59f2a29c02cc7b434f637cb08))
* **remoterepository:** remove old snapshots code ([59c5093](https://github.com/sitkoru/Sitko.Core/commit/59c5093b795163ccc5a348e20c47195b7f179676))
* **remoterepository:** use correct property ([b093a38](https://github.com/sitkoru/Sitko.Core/commit/b093a3839a3dfed4b32614ece324723f45753020))
* **remoterepository:** use ListResult cause tuple serialization is hard ([363f8f2](https://github.com/sitkoru/Sitko.Core/commit/363f8f2760508fbd57115c29b940cc48842914de))
* **remotestorage:** don't inject HttpClient, create via options of IHttpClientFactory to support dynamic options ([4071c4a](https://github.com/sitkoru/Sitko.Core/commit/4071c4aaaafab33f4f1db52c3d02aa76f406c038))
* **remotestorage:** support upload to empty path ([4b9c3d6](https://github.com/sitkoru/Sitko.Core/commit/4b9c3d6a61b44259a7c7ac68c3016bd6a3510c15))
* remove repository options and unnecessary props ([0410263](https://github.com/sitkoru/Sitko.Core/commit/0410263558427c1d32fcd3414a899069c3b9a6c9))
* remove transport injecting from test controllers ([62d6659](https://github.com/sitkoru/Sitko.Core/commit/62d665917bd0c9878c11d746bb2246901888264f))
* remove unnecessary generic param when adding httpclient ([bc2bbd2](https://github.com/sitkoru/Sitko.Core/commit/bc2bbd2c4d0d5c18d0876c525cde9e1303bc7698))
* remove unneeded files from remote tests ([c2dc8db](https://github.com/sitkoru/Sitko.Core/commit/c2dc8db7f38393334a87729df03e3e8f05546538))
* rename ASPNET_ENVIRONMENT to ASPNETCORE_ENVIRONMENT ([8b9145d](https://github.com/sitkoru/Sitko.Core/commit/8b9145d224d68f97285587ef3355ea3d03b21df9))
* **repository:** don't finish batch if it wasn't started inside this operation ([d0a8216](https://github.com/sitkoru/Sitko.Core/commit/d0a82162ce2261dbfc24b082ddf9a044c386e0c1))
* **repository:** remove property duplication ([eeec044](https://github.com/sitkoru/Sitko.Core/commit/eeec044fb5417dac24b6b01c79b7dc574907480b))
* **repository:** use common extensions methods for ThenInclude ([3992547](https://github.com/sitkoru/Sitko.Core/commit/3992547c96c46703770200f45f77cba09221794b))
* **s3storage:** fix saving metadata ([a2a174b](https://github.com/sitkoru/Sitko.Core/commit/a2a174b82ebf32057cfb041c9afb2c5806835bbd))
* **s3storage:** pass cancellation tokens ([f843719](https://github.com/sitkoru/Sitko.Core/commit/f843719bff3bd09a28f163745b4ff732e3551db1))
* **s3storage:** support presigned urls and settings bucket policy ([f3c7174](https://github.com/sitkoru/Sitko.Core/commit/f3c71743c1e321f5afaa5d449e8a54871d298f0b))
* **sln:** fix sln after rebase ([bbf1462](https://github.com/sitkoru/Sitko.Core/commit/bbf1462654c5fbf1dec016c2353dd8bebfe603f0))
* **test:** fix controller routes ([7efd191](https://github.com/sitkoru/Sitko.Core/commit/7efd1917c0e3faf95fdc7377b36be8845f4e8da0))
* **tests:** fix repositories injected repository context ([f533f97](https://github.com/sitkoru/Sitko.Core/commit/f533f97c8f5a02112a9eb336785cbd77c5afbc25))
* **tests:** make repository ctor public ([07d63e1](https://github.com/sitkoru/Sitko.Core/commit/07d63e1d0f8b19e5000acab079ad490ab47d4ba5))
* **tests:** replace add test to getall test ([9d161a5](https://github.com/sitkoru/Sitko.Core/commit/9d161a51da765c0ce2f0343fb73c153a1372fdd8))
* throw only if not 404 ([d0773a5](https://github.com/sitkoru/Sitko.Core/commit/d0773a53784e3dcea17c2fcd8938c577bd011f87))
* unify methods for sending request, change methods params ([ffc048a](https://github.com/sitkoru/Sitko.Core/commit/ffc048aabc9bede68dc953e9830fa728eeeea1e8))
* use different options ([42b562d](https://github.com/sitkoru/Sitko.Core/commit/42b562d267a6856f8fbb5fd797d8ec28c1c3cf7a))
* **wasm:** pass real IJsRuntime from current application to logger sink ([96abbcc](https://github.com/sitkoru/Sitko.Core/commit/96abbcc7c60201f6d2cd6f7d0d403c66d5cdc827))
* **xunit:** pass scope name to BeforeConfiguredAsync hook ([fb78aa3](https://github.com/sitkoru/Sitko.Core/commit/fb78aa38712641b64238f0efe46c41791a492ed6))
* **xunit:** prevent dispose cycle ([db0b40c](https://github.com/sitkoru/Sitko.Core/commit/db0b40c9291c14b538a3b2a713203541e6dae052))
* **xunit:** use scope id for database name generation ([7225355](https://github.com/sitkoru/Sitko.Core/commit/7225355e7cc98afcaa871392e6bf532775122571))


### Features

* **.net:** .NET 5.0.14 ([8dcd63f](https://github.com/sitkoru/Sitko.Core/commit/8dcd63f6ff8b62e3bfbee41203bb35f4947159ce))
* **.net:** .NET 6.0.2 ([ee32cc5](https://github.com/sitkoru/Sitko.Core/commit/ee32cc545b492abde6081aa0f38551095f75ee24))
* **.net:** 3.1.24 ([2284a54](https://github.com/sitkoru/Sitko.Core/commit/2284a5433e165952af47b3c008a5f3858353b598))
* **.net:** 5.0.16 ([192fb4b](https://github.com/sitkoru/Sitko.Core/commit/192fb4b67a7c56594bd36f8627684d4472526664))
* **.net:** 6.0.4 ([573a171](https://github.com/sitkoru/Sitko.Core/commit/573a1718221211b38aaa5a1687d0674badad2f8d))
* add base remote repository class ([bd7b3c0](https://github.com/sitkoru/Sitko.Core/commit/bd7b3c0809055979b4078f8692d599122a8161f8))
* add blank for init db, add validation models from assembly ([ee6eea5](https://github.com/sitkoru/Sitko.Core/commit/ee6eea5cd608d694839bdb053b94b3b423140084))
* add blank Select method ([bfaf68a](https://github.com/sitkoru/Sitko.Core/commit/bfaf68a078e6f896906fd4976b7e5c3ac07a0d86))
* add extensions method to remote repo ([a483da7](https://github.com/sitkoru/Sitko.Core/commit/a483da79cab45e1bf9097eb276fad6c5dfece75d))
* add Get test for null ([df13655](https://github.com/sitkoru/Sitko.Core/commit/df136553e0f7e27f1370e4abad19bd437e1f7b68))
* add http repository transport ([1cd9b45](https://github.com/sitkoru/Sitko.Core/commit/1cd9b451d56c6aada6594049d2f0b9f96f8b376f))
* add http repository transport application extension ([2a1a04f](https://github.com/sitkoru/Sitko.Core/commit/2a1a04f101091d01b21b139f155f75d730857186))
* add Include method to IRepository ([0c3739f](https://github.com/sitkoru/Sitko.Core/commit/0c3739f0b0f51404b9786bd34b274ca1df6a2dd1))
* add init webapp method ([979f35c](https://github.com/sitkoru/Sitko.Core/commit/979f35c6301beb83e2cbbbb96c8a8fef40be533c))
* add IRemoteRepo interface ([9598bfc](https://github.com/sitkoru/Sitko.Core/commit/9598bfcec31bcc8cdca3ec41bf5fbf2110c57e98))
* add methods, fix routes ([634188b](https://github.com/sitkoru/Sitko.Core/commit/634188b2fe739e85f750b05afa7857ed036bb5b6))
* add modules to remote repo ([370b31d](https://github.com/sitkoru/Sitko.Core/commit/370b31dc2922d3c9961d6a3c48dedb1b44678f6d))
* add queries for remote repo ([28c8c70](https://github.com/sitkoru/Sitko.Core/commit/28c8c70dfb8875c2c520ba5fb020d62166618c07))
* add remote repo add test blank ([65171b9](https://github.com/sitkoru/Sitko.Core/commit/65171b9575dcbf2fba087ef86891ac8b2ec0ae1f))
* add remote repo options ([a35fa6a](https://github.com/sitkoru/Sitko.Core/commit/a35fa6ad8e3820d76e631c85e52b8610d4716560))
* add remote repo test scope ([e320242](https://github.com/sitkoru/Sitko.Core/commit/e320242d4e38309705421d0e1a28ebf3b7a0c617))
* add remote repository projects ([7907606](https://github.com/sitkoru/Sitko.Core/commit/7907606228400d77f7e7c62f4f579de877758bac))
* add remote repository scope ([2709bdc](https://github.com/sitkoru/Sitko.Core/commit/2709bdc248421f99ed0aa8d2f81c7fc7fe4487db))
* add remote tests ([5b89ee5](https://github.com/sitkoru/Sitko.Core/commit/5b89ee5ec7bccf3854c25a275fe13920abb5289f))
* add repository options ([1d394bf](https://github.com/sitkoru/Sitko.Core/commit/1d394bfcee254f2644ff00aacbee780bf789599d))
* add repository transport interface ([282c360](https://github.com/sitkoru/Sitko.Core/commit/282c36045a8c9cdf541428356e0165fe44c75e47))
* add select methods to IRepositoryQuery ([fb3d2fd](https://github.com/sitkoru/Sitko.Core/commit/fb3d2fd39b89587b9372506e87f4180c98fd290c))
* add server controller for remote repo ([2a970b5](https://github.com/sitkoru/Sitko.Core/commit/2a970b5d5baa689514a4943d7c20eca9a82c07e5))
* add test data ([2ecf9f9](https://github.com/sitkoru/Sitko.Core/commit/2ecf9f94e666acec0c0777ea58ef3c9480533acc))
* add test ef repo ([da13af5](https://github.com/sitkoru/Sitko.Core/commit/da13af5e20faa066cb5e09c893c5d76f25dc0cc6))
* add test repositories ([a3afa98](https://github.com/sitkoru/Sitko.Core/commit/a3afa98a37abe435aea2723104e6dfb3a22d0b13))
* allow all forwarded headers by default ([1827082](https://github.com/sitkoru/Sitko.Core/commit/1827082ac04e9ff1fc92005e4afef4ae55f80844))
* **antdesign:** update AntDesign from 0.10.3.1 to 0.10.4 ([79c5200](https://github.com/sitkoru/Sitko.Core/commit/79c52000d7675afc9e38b870cb3df618e2dd25f8))
* **antdesign:** update AntDesign from 0.10.4 to 0.10.5 ([14aaa33](https://github.com/sitkoru/Sitko.Core/commit/14aaa337bc6c7627fbe81efedcb840e2e1d6e81d))
* **app:** base application should be independent of hosting model ([3ea04db](https://github.com/sitkoru/Sitko.Core/commit/3ea04db3ae059621bcdcfa2c8fcd0e8e5399d9a5))
* **app:** change services registration order - modules, app actions, startup ([865ea98](https://github.com/sitkoru/Sitko.Core/commit/865ea9875261002680ab554a0b5b97fe463cb185))
* **app:** drop tmp host builder, create configuration and env manually ([d298c34](https://github.com/sitkoru/Sitko.Core/commit/d298c34389fa77dcc18245a7cdbf0387a29de19f))
* **app:** proceed only specific modules to reduce options building ([3f17438](https://github.com/sitkoru/Sitko.Core/commit/3f17438b9d1b61db51970741a41b6532c7aac985))
* **apps:** add table filters to demo mudblazor app ([91a0cf7](https://github.com/sitkoru/Sitko.Core/commit/91a0cf7557e96e2a801823819497866901d902f8))
* **app:** update CompareNETObjects from 4.75.0 to 4.76.0 ([19aecf8](https://github.com/sitkoru/Sitko.Core/commit/19aecf84e547c216683629cbbf4b0adc694867d1))
* **app:** update FluentValidation.DependencyInjectionExtensions from 10.3.6 to 10.4.0 ([41e9024](https://github.com/sitkoru/Sitko.Core/commit/41e9024c6b14e37be20fe57609c3f1ab9586ba5b))
* **app:** update Scrutor from 3.3.0 to 4.0.0 ([5cbbd6a](https://github.com/sitkoru/Sitko.Core/commit/5cbbd6ae192887db8df92bd866fc14ad1651861c))
* **app:** update Scrutor from 4.0.0 to 4.1.0 ([f109903](https://github.com/sitkoru/Sitko.Core/commit/f109903a4c34e79dd853ddc395a4b386cc695771))
* **app:** update Serilog.Exceptions from 8.0.0 to 8.1.0 ([cd74978](https://github.com/sitkoru/Sitko.Core/commit/cd7497893946aaeec8a4ec09e301c67e73ff737c))
* **auth:** update idunno.Authentication.Basic from 2.2.2 to 2.2.3 ([6a8bf9a](https://github.com/sitkoru/Sitko.Core/commit/6a8bf9a950a84562dd38d8818e55e2a2ff22659e))
* **blazor:** add BaseStateComponent with state persistence and compression ([b2923d4](https://github.com/sitkoru/Sitko.Core/commit/b2923d491774ae961cd7632ad4f6bb2a8ddc377a))
* **blazor:** create new base application for BlazorWasm ([9dfba19](https://github.com/sitkoru/Sitko.Core/commit/9dfba194199fffa693a812348eb09cf005edd26a))
* **blazorserver:** support CompressedPersistentComponentState ([083315d](https://github.com/sitkoru/Sitko.Core/commit/083315d57a10a8b6d45adce45dc5a746fbe894f8))
* **blazor:** Split blazor modules. Create separate packages for BlazorServer ([dab20f2](https://github.com/sitkoru/Sitko.Core/commit/dab20f2102c20a23f478d07cc706223d9af485a2))
* **blazor:** upd .sln ([34b7ea4](https://github.com/sitkoru/Sitko.Core/commit/34b7ea45fcbf8028e8ddb03589d40219d41df5af))
* **blazor:** update CompareNETObjects from 4.74.0 to 4.75.0 ([d1d0c64](https://github.com/sitkoru/Sitko.Core/commit/d1d0c64e73f4d66945de831f95f717dd5aca3aaa))
* **blazor:** update CompareNETObjects from 4.76.0 to 4.77.0 ([a6f40e1](https://github.com/sitkoru/Sitko.Core/commit/a6f40e1030b4412f67c103e5a35a9266b16b3b9b))
* **blazorwasm:** configure wasm host builder via IWasmApplicationModule ([a44d04e](https://github.com/sitkoru/Sitko.Core/commit/a44d04ed23a834b37279cd98f191f5b57ce26cf2))
* **blazorwasm:** implement LogInternal ([30d2216](https://github.com/sitkoru/Sitko.Core/commit/30d221622c38b77d60ab2917df33d309e1d58391))
* **blazorwasm:** rework logging configuration ([cf34cb5](https://github.com/sitkoru/Sitko.Core/commit/cf34cb56d16f6434694c27062dca8dbd226bd41c))
* **blazorwasm:** support CompressedPersistentComponentState ([c5e65b5](https://github.com/sitkoru/Sitko.Core/commit/c5e65b5a77bd15e200ea1b6380d0e4f9efcd7b41))
* **blazorwasm:** support ScriptInjector ([13b9e84](https://github.com/sitkoru/Sitko.Core/commit/13b9e84bfec3a6e4c04019cc1906fd843246a9aa))
* **blazorwasm:** use single hostbuilder to create application ([45932fc](https://github.com/sitkoru/Sitko.Core/commit/45932fc8028c783e626226b0d1e4a185f21e5a7c))
* change Refresh implementation ([5e57a69](https://github.com/sitkoru/Sitko.Core/commit/5e57a696f4dfa570d57e933d2f9cd16778197271))
* change return type for Refresh method ([3c69181](https://github.com/sitkoru/Sitko.Core/commit/3c691814544f05088a26e2838111ddd0cb3d32b5))
* **ci:** update tests steps ([326dcfa](https://github.com/sitkoru/Sitko.Core/commit/326dcfa5bfd797f5a92bc74bbd4b6750b436257a))
* **consul:** update Consul from 1.6.10.4 to 1.6.10.5 ([b82fefa](https://github.com/sitkoru/Sitko.Core/commit/b82fefa0b814786c8ffa7518ee5025fa0444b56a))
* **core:** .NET 5.0.15 ([3dd4ab3](https://github.com/sitkoru/Sitko.Core/commit/3dd4ab3527e83f030bdd308e11e4e52399ce9ab8))
* **core:** .NET 6.0.3 ([aba8ed6](https://github.com/sitkoru/Sitko.Core/commit/aba8ed6d870d94782f62204de052394b6ea32a13))
* **core:** .NET Core 3.1.23 ([da57226](https://github.com/sitkoru/Sitko.Core/commit/da57226248e3bb16d8ddc9b1cb522437616d8175))
* create addPersistentState with default services ([4910dd2](https://github.com/sitkoru/Sitko.Core/commit/4910dd252f57ebe1012a94d050a0ee6556120775))
* create PersistentStateModule ([beec222](https://github.com/sitkoru/Sitko.Core/commit/beec2225523c0c28dd007f2c116d66e48189b0cd))
* create standalone http transport module ([624773b](https://github.com/sitkoru/Sitko.Core/commit/624773b0d6377143ba388cb35ca0f94841109867))
* create standalone httptransport ([53b07ac](https://github.com/sitkoru/Sitko.Core/commit/53b07acc11e632d39f930294945c92d7f0a9df3a))
* **demo:** upd demo ([4cc93a2](https://github.com/sitkoru/Sitko.Core/commit/4cc93a2a0211bb284eaffd7259f69090a7881b79))
* **demo:** upd deps ([38503a6](https://github.com/sitkoru/Sitko.Core/commit/38503a60ede447a8c092282810ebe9bc39135e19))
* **demo:** upd mudblazor demo ([e86a8b0](https://github.com/sitkoru/Sitko.Core/commit/e86a8b0730e59cbba96e42d0277a75677a9d620a))
* **demo:** update demo deps ([f844f54](https://github.com/sitkoru/Sitko.Core/commit/f844f545b922e8469db4425db02639f5795af200))
* **demo:** use remote storage ([d396b57](https://github.com/sitkoru/Sitko.Core/commit/d396b57ea0113536e331604b19a683bf37b41f25))
* **efrepository:** update System.Linq.Dynamic.Core from 1.2.15 to 1.2.17 ([833a3b3](https://github.com/sitkoru/Sitko.Core/commit/833a3b3f35958516e549c167b79f42ffd76521ad))
* **efrepository:** update System.Linq.Dynamic.Core from 1.2.17 to 1.2.18 ([a26b6bd](https://github.com/sitkoru/Sitko.Core/commit/a26b6bd3bf3d431b9678297307ae3f2662af93db))
* **elastickstack:** update Elastic.Apm.NetCoreAll from 1.14.0 to 1.14.1 ([36587d2](https://github.com/sitkoru/Sitko.Core/commit/36587d2adc78fa1a0c7c9be569af11ca6a565ba8))
* **elasticsearch:** update NEST from 7.16.0 to 7.17.0 ([78cd828](https://github.com/sitkoru/Sitko.Core/commit/78cd8286c85bca4e219630b4f869c6e4709f81bf))
* **elasticsearch:** update NEST from 7.17.0 to 7.17.1 ([b2b8d6c](https://github.com/sitkoru/Sitko.Core/commit/b2b8d6c4c55d2f3af0a33330548081fff7cea9aa))
* **elasticstack:** expose elastic sink failure handling options ([55115a9](https://github.com/sitkoru/Sitko.Core/commit/55115a9eb0466fa5a38359e7a2b608b98aa11095))
* **elasticstack:** update Elastic.Apm.NetCoreAll from 1.12.1 to 1.14.0 ([930e6dc](https://github.com/sitkoru/Sitko.Core/commit/930e6dc446a46630150c836ce2152ca078142998))
* **emailmailgun:** update FluentEmail.Mailgun from 3.0.0 to 3.0.2 ([a1b94ec](https://github.com/sitkoru/Sitko.Core/commit/a1b94ec4991d1a05dfb766af055ef90b1361ec9a))
* **emailsmtp:** update MailKit from 3.1.1 to 3.2.0 ([007a9e7](https://github.com/sitkoru/Sitko.Core/commit/007a9e7c5cb7006d1e6094a05b82291c4737b42e))
* **email:** update FluentEmail.Core from 3.0.0 to 3.0.2 ([cdcf746](https://github.com/sitkoru/Sitko.Core/commit/cdcf746ede17d741e7b7fc4f33267250768fb10d))
* **email:** update HtmlAgilityPack from 1.11.39 to 1.11.42 ([774c8ad](https://github.com/sitkoru/Sitko.Core/commit/774c8ad8b1ae50734d89595f0638001c94ea5a45))
* **email:** update MailKit from 3.0.0 to 3.1.1 ([742be4c](https://github.com/sitkoru/Sitko.Core/commit/742be4cbc0ab40e22989aef292e3ba6e335550d2))
* expand methods fore remote repository ([bf55040](https://github.com/sitkoru/Sitko.Core/commit/bf55040a44e782729a1e2765383e782af47babaf))
* expand remote transport interface ([f909fcc](https://github.com/sitkoru/Sitko.Core/commit/f909fcc3cadff80118d749f7187fc538cc3fa5d7))
* **filestorage:** switch to UploadRequest ([3d6e65d](https://github.com/sitkoru/Sitko.Core/commit/3d6e65d55a5d4c32fa27258df1460dcafb468778))
* **fileupload:** merge FileUploadRequest and FileUploadInfo ([f02aa61](https://github.com/sitkoru/Sitko.Core/commit/f02aa6121d968cb285e45ccd2100adf2378fde67))
* **grpc:** add grpc web wrapper module ([91e35da](https://github.com/sitkoru/Sitko.Core/commit/91e35dab2cfc9133d2f89d78ac33bcfb1773ef57))
* **grpc:** grpc 2.45.0, protobuf 3.20.0 ([c9e6790](https://github.com/sitkoru/Sitko.Core/commit/c9e6790f2a99c18af1e0170b6e6aaeafc0547e4c))
* **grpc:** update Grpc.Net from 2.41.0 to 2.42.0 ([8ddceee](https://github.com/sitkoru/Sitko.Core/commit/8ddceeed51b10e71db40541e69844e916e24d9e1))
* **grpc:** update Grpc.Net to 2.43.0 ([4bd9b87](https://github.com/sitkoru/Sitko.Core/commit/4bd9b8726165c13328ed62e4a707732a15e74cd1))
* **grpc:** update Grpc.Tools to 2.44.0 ([23fe847](https://github.com/sitkoru/Sitko.Core/commit/23fe847b1234e8113dc563d8e936da1a70a41c6a))
* **hangfire:** allow to configure all hangfire dashboard options ([acdaeb4](https://github.com/sitkoru/Sitko.Core/commit/acdaeb46c1dacdf0f8085725d20bc43db3527647))
* **hangfire:** provide some hangfire dashboard auth filters ([39f3dc8](https://github.com/sitkoru/Sitko.Core/commit/39f3dc81af59d99ea2bdb10f707dabab1ae11201))
* **hangfire:** switch to use AddHangfireServer ([c504ae3](https://github.com/sitkoru/Sitko.Core/commit/c504ae3255b9cc3f5294a14431d56b24270398ac))
* **hangfire:** update Hangfire.PostgreSql from 1.9.5 to 1.9.6 ([4386a4a](https://github.com/sitkoru/Sitko.Core/commit/4386a4a1d2b7ec407b5c60d8dd53de388e91e751))
* **hangfire:** update Hangfire.PostgreSql from 1.9.6 to 1.9.7 ([0c321ca](https://github.com/sitkoru/Sitko.Core/commit/0c321ca2dbbf99176710a40e7c0f89d1d3cc2a9c))
* implement GetChangesAsync() ([ed72ac1](https://github.com/sitkoru/Sitko.Core/commit/ed72ac12c7d9141a8b161aea520a9bf211049184))
* implement include for RemoteRepositoryQuery ([99a7dd6](https://github.com/sitkoru/Sitko.Core/commit/99a7dd6f740af8b4d74e995ed8a60d83801f6864))
* implement includes ([49ee44d](https://github.com/sitkoru/Sitko.Core/commit/49ee44dafc182962a34283c307b6c4a676942e0b))
* implement missed methods ([f3fb327](https://github.com/sitkoru/Sitko.Core/commit/f3fb32773333b46819c7ac5d8c778f79736c31c9))
* implement missing remote transport interface members ([ae06e75](https://github.com/sitkoru/Sitko.Core/commit/ae06e75668f9a3712b49879d617f665c10906cb7))
* implement some methods ([ddd662f](https://github.com/sitkoru/Sitko.Core/commit/ddd662f8ac0ca8ff779fb46fbc17ca0785bb1589))
* implement sum methods, bit of refactoring ([e75e91d](https://github.com/sitkoru/Sitko.Core/commit/e75e91de97b4ee8589962c0bc7dae6da72663417))
* implement transactions methods and refresh method ([2bd3cc7](https://github.com/sitkoru/Sitko.Core/commit/2bd3cc71c450d98ca16654f0fed08b200fef7d7e))
* introduce IApplicationContext to combine IConfiguration, IAppEnvironment and application options ([f2a7b5f](https://github.com/sitkoru/Sitko.Core/commit/f2a7b5f361a548e738a59cf811cd5aff589f690d))
* **logging:** unify serilog integration ([a102ef1](https://github.com/sitkoru/Sitko.Core/commit/a102ef1340070c14e1709f1027654de56c40062a))
* make WasmApplication abstract and force client app to configure WebAssemblyHostBuilder ([fadb4ed](https://github.com/sitkoru/Sitko.Core/commit/fadb4edfbe01f634fea76f1cd3a8c28323b0d3cb))
* move internal logging from base application ([f3c0cb1](https://github.com/sitkoru/Sitko.Core/commit/f3c0cb118dca577edd214a185e12c648714bd907))
* **mudblazor:** add Label, HelperText and errors display to MudFileUpload ([4912fa9](https://github.com/sitkoru/Sitko.Core/commit/4912fa9aa7f096698f04adbefcc6cf9269d28503))
* **mudblazor:** add MudValidationMessage component to display validation messages for Field ([9392350](https://github.com/sitkoru/Sitko.Core/commit/9392350a79b6f3836b0dedeb3a1d840aaedfbf14))
* **mudBlazor:** bind RowsPerPage value ([9a95bc6](https://github.com/sitkoru/Sitko.Core/commit/9a95bc6fa1fc14dde9f94ec828e52ae98f818fba))
* **mudBlazor:** check result TryGetQueryString on null in mudTable component ([ac87bcb](https://github.com/sitkoru/Sitko.Core/commit/ac87bcbae899013ebacebae523571bfa70335903))
* **mudBlazor:** save table pagination, sort, filters to url ([a0773de](https://github.com/sitkoru/Sitko.Core/commit/a0773de55732f70129acee401b5ab6cb6b75ed02))
* **mudblazor:** update MudBlazor from 6.0.4 to 6.0.6 ([f71bd0d](https://github.com/sitkoru/Sitko.Core/commit/f71bd0dc618f352e76b036e018034d78ac2646a5))
* **mudblazor:** update MudBlazor from 6.0.6 to 6.0.7 ([f1b9b55](https://github.com/sitkoru/Sitko.Core/commit/f1b9b55220f2d4fd79337055b3ddfe7061e560d1))
* **mudblazor:** update MudBlazor from 6.0.7 to 6.0.9 ([5594362](https://github.com/sitkoru/Sitko.Core/commit/5594362f8f74a0be34906a6ca4ed83eec5a0b72b))
* **mudblazor:** update MudBlazor from 6.0.9 to 6.0.10 ([fb728c5](https://github.com/sitkoru/Sitko.Core/commit/fb728c5d85edc1f0e4a44734327198fdc9d18455))
* **mudBlazor:** use TryGetQueryString for get params from url ([43a086c](https://github.com/sitkoru/Sitko.Core/commit/43a086c6773d04613cd84072995226f39eeb365e))
* multiply sum methods for structs ([8ad71c2](https://github.com/sitkoru/Sitko.Core/commit/8ad71c2b60df16f43ce1976e471d25aaa1fd66aa))
* **nats:** update NATS.Client from 0.14.4 to 0.14.5 ([45f788b](https://github.com/sitkoru/Sitko.Core/commit/45f788bf92a895fe21861a8767a3696de6783361))
* **nats:** update NATS.Client from 0.14.5 to 0.14.6 ([5ced38e](https://github.com/sitkoru/Sitko.Core/commit/5ced38e8e9adb865b8fa33cd902986039ccd8560))
* new include method realization for EFRepositoryQuery ([238f214](https://github.com/sitkoru/Sitko.Core/commit/238f21435d98ff5f45f740f461685966c40edc85))
* **oidc:** add tokens auto-update ([7c30b93](https://github.com/sitkoru/Sitko.Core/commit/7c30b93a96362edc784bea272a65ea9c22eac5b5))
* parse query string with nullable types ([f7b96cd](https://github.com/sitkoru/Sitko.Core/commit/f7b96cd083016febca78ef91fb0647442887d33d))
* **postgresdb:** add option to configure dbcontext factory lifetime ([747f8d3](https://github.com/sitkoru/Sitko.Core/commit/747f8d3cc2cc9e987be51a7ca6aebf3eacd5a84d))
* **protobuf:** update Google.Protobuf from 3.19.2 to 3.19.4 ([ffc2226](https://github.com/sitkoru/Sitko.Core/commit/ffc2226550efda491b019938bd0db41894c0573a))
* **protobuf:** update Google.Protobuf from 3.20.0 to 3.20.1 ([f5814aa](https://github.com/sitkoru/Sitko.Core/commit/f5814aaaf4ece43efddc20ec9dcb6e7173dcaf47))
* **puppeteer:** update PuppeteerSharp from 6.1.0 to 6.2.0 ([7c7097b](https://github.com/sitkoru/Sitko.Core/commit/7c7097b0ee9231da7293c5fe39ac0683b96a2b9e))
* rearrange tests for repositories ([d416156](https://github.com/sitkoru/Sitko.Core/commit/d416156a14626cf0400e0080e652211d99103a5c))
* rebase WebApplication onto HostedApplication ([2f9ab6c](https://github.com/sitkoru/Sitko.Core/commit/2f9ab6c79d1d3055ee77146a25e01d89e2e3e2f6))
* remote controller now have EfRepo DI ([7cf8453](https://github.com/sitkoru/Sitko.Core/commit/7cf8453922ff1b623764a76d91343fb1028b53aa))
* **remoterepository:** add full configuration method for http transport ([b7d2f64](https://github.com/sitkoru/Sitko.Core/commit/b7d2f644bd8f2b4efc871e1e7f229138363d14e0))
* **remoterepository:** add ThenInclude extensions and test ([5288b09](https://github.com/sitkoru/Sitko.Core/commit/5288b090f51e62236a773befd3b850b726a0e227))
* **remoterepository:** create named http client, pass http client factory to factory ([4d98110](https://github.com/sitkoru/Sitko.Core/commit/4d98110a925ade91db12c38d4f7758932709c9a5))
* **remoterepository:** improve error handling in controller and transport ([67724b5](https://github.com/sitkoru/Sitko.Core/commit/67724b51c34be5277ecb832b1a5b7f851686465f))
* **remoterepository:** new WasmHttpRepositoryTransportModule with browser cookies in http requests ([df6ee18](https://github.com/sitkoru/Sitko.Core/commit/df6ee18c3751cb8806c693ba31dc8c4ce9938b2b))
* **remoterepository:** rework server controller ([f03ffd7](https://github.com/sitkoru/Sitko.Core/commit/f03ffd703d76871dec1d4c0c6f81fdb26e5bffdf))
* **remoterepository:** split serialized query and it's data to simplify syntax and serialization ([4851f49](https://github.com/sitkoru/Sitko.Core/commit/4851f49da0d2ea2e178a589c2b5bb98c864de130))
* **remoterepository:** support abstract select expression instead of type int/long/etc. Support whereByString. Grab all data from source query. ([9728f2b](https://github.com/sitkoru/Sitko.Core/commit/9728f2be6cda02a3ad4f03a8b71725bc8fca4b6c))
* **remoterepository:** support more where/sort methods ([fe6bc07](https://github.com/sitkoru/Sitko.Core/commit/fe6bc072b64fe224ae2eb346b7a21e572f673bda))
* **remoterepository:** update CompareNETObjects from 4.74.0 to 4.75.0 ([328ee43](https://github.com/sitkoru/Sitko.Core/commit/328ee434d7e5c5b42330a3299d8e2a756faaf3c9))
* **remoterepository:** update CompareNETObjects from 4.76.0 to 4.77.0 ([dfebca0](https://github.com/sitkoru/Sitko.Core/commit/dfebca080ad722eace04f4814e53d98524e20618))
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
* remove base value for api route and add validator for it ([1308c39](https://github.com/sitkoru/Sitko.Core/commit/1308c391e4be52bc5081e157a1350017db611701))
* replace PersistentState module to extension ([2df5d42](https://github.com/sitkoru/Sitko.Core/commit/2df5d428469939cfbbbd28219e60d37e20296334))
* **repository:** convert AddOrUpdateOperationResult to record for better serializability ([9b525f3](https://github.com/sitkoru/Sitko.Core/commit/9b525f3216c90175795e66c6ea779817331c11cf))
* **s3storage:** don't inject storage into metadata provider, pass from storage itself ([a5e9568](https://github.com/sitkoru/Sitko.Core/commit/a5e95682e2c8ec0c852cd5562b509a87ca498ac5))
* **s3storage:** switch to UploadRequest ([1547c1c](https://github.com/sitkoru/Sitko.Core/commit/1547c1ca58efa8befaa9817cb56af8f1423eec48))
* **s3storage:** update AWSSDK.S3 from 3.7.7.21 to 3.7.8.1 ([9d3136e](https://github.com/sitkoru/Sitko.Core/commit/9d3136eff1301519c45a6fb38006ef0106589c09))
* **s3storage:** update AWSSDK.S3 from 3.7.8.1 to 3.7.8.5 ([701d80c](https://github.com/sitkoru/Sitko.Core/commit/701d80c51e4430cc02fa2c092ba153120927c174))
* **s3storage:** update AWSSDK.S3 from 3.7.8.22 to 3.7.8.27 ([3a3b691](https://github.com/sitkoru/Sitko.Core/commit/3a3b6919cf987e56001f3b6c1df6b3d0c45babc4))
* **s3storage:** update AWSSDK.S3 from 3.7.8.27 to 3.7.8.28 ([5034ada](https://github.com/sitkoru/Sitko.Core/commit/5034adab514b65d2b9d13ce04a12ccd98180b074))
* **s3storage:** update AWSSDK.S3 from 3.7.8.5 to 3.7.8.22 ([4581157](https://github.com/sitkoru/Sitko.Core/commit/45811579c000d3c2beb342af89fc31fa97387737))
* **s3:** update AWSSDK.S3 from 3.7.7.10 to 3.7.7.21 ([cc98e8a](https://github.com/sitkoru/Sitko.Core/commit/cc98e8a64e669b7a29ee25b6f3912836ad954ff9))
* split test repositories to efrepos and remote repos ([9763aa6](https://github.com/sitkoru/Sitko.Core/commit/9763aa6b9fd992503c9db6d82c2d792163ffe0f8))
* **storage:** don't inject storage into metadata provider, pass from storage itself ([ad08dd6](https://github.com/sitkoru/Sitko.Core/commit/ad08dd66cf5f314f16ed55d1827ad4e332c3e1c1))
* **storage:** implement upload via UploadRequest, final storage must return StorageItem ([3dba35f](https://github.com/sitkoru/Sitko.Core/commit/3dba35f6372a3eb4dcc7ef5677ec99007e44823a))
* **storage:** make GetAllItemsAsync public for now to use in controller ([a67c183](https://github.com/sitkoru/Sitko.Core/commit/a67c18361a67459c9624b4b221aa48189727ebd9))
* **storage:** move internal classes into Internal namespace and make them public ([cb2dbae](https://github.com/sitkoru/Sitko.Core/commit/cb2dbaeaafc0a3e928533f8754f022391c6691de))
* **storage:** move StorageItemDownloadInfo into Internal namespace and make it public. Make GetStream async ([1cf8e4f](https://github.com/sitkoru/Sitko.Core/commit/1cf8e4fef3288272a3d87593df58e3160337f434))
* **storage:** rework storage item creation ([4d969ea](https://github.com/sitkoru/Sitko.Core/commit/4d969ea4bf72a7216f865a8b4e17c8dde1527dad))
* **storage:** rework storage item creation ([5e6cbbc](https://github.com/sitkoru/Sitko.Core/commit/5e6cbbc4305eea68d482ce48ad093007efd69e41))
* **swagger:** update Swashbuckle.AspNetCore from 6.2.3 to 6.3.0 ([9e14bba](https://github.com/sitkoru/Sitko.Core/commit/9e14bba2f9038f445289cd0f331cd8cf8a578e11))
* **swagger:** update Swashbuckle.AspNetCore from 6.3.0 to 6.3.1 ([5b24751](https://github.com/sitkoru/Sitko.Core/commit/5b247516298389c6cf49ea22987488704bab2dbc))
* switch modules to use IAppEnvironment instead of IHostEnvironment ([d713865](https://github.com/sitkoru/Sitko.Core/commit/d71386507abfbe7a663481b6dc8ad4020d2514c9))
* **tests:** add remote repository tests project ([d1c1540](https://github.com/sitkoru/Sitko.Core/commit/d1c15408284984ecad40bfa6866808982e8d72a9))
* **tests:** add server side controller ([cbeab71](https://github.com/sitkoru/Sitko.Core/commit/cbeab71e9bdf1e9f667b58dc25d88c6be49eccf9))
* **tests:** make EfTest abstract basic repository tests ([fda6e2f](https://github.com/sitkoru/Sitko.Core/commit/fda6e2f94daf305098ed24e95bc7f6b567acc737))
* use interfaces in state component ([75e8070](https://github.com/sitkoru/Sitko.Core/commit/75e8070c940e9ad5f1a322849e6a230dc83141a5))
* **validation:** update Sitko.FluentValidation to 1.2.0 ([0a58b75](https://github.com/sitkoru/Sitko.Core/commit/0a58b75954070db08727a8a689ed87d6ea9ff165))
* **xunit:** add BeforeConfiguredAsync hook ([8e6d924](https://github.com/sitkoru/Sitko.Core/commit/8e6d92454301606d8b93e3903ccb9f023d507abf))
* **xunit:** rework test scope dispose ([b327591](https://github.com/sitkoru/Sitko.Core/commit/b32759173d351c59d673f801073f950a5505d046))
* **xunit:** update FluentAssertions from 6.3.0 to 6.5.1 ([bea9635](https://github.com/sitkoru/Sitko.Core/commit/bea96351b47a1d86197a1150ba1a2cbf18ed9cc5))
* **xunit:** update FluentAssertions from 6.5.1 to 6.6.0 ([26d40d1](https://github.com/sitkoru/Sitko.Core/commit/26d40d18c0d09eef6deefe32ed7c8e75c8ab571a))
* **xunit:** update Microsoft.NET.Test.Sdk from 17.0.0 to 17.1.0 ([b32396e](https://github.com/sitkoru/Sitko.Core/commit/b32396ecd21f9fa4b40d3cf07be81f16f4d100c4))
* **xunit:** update xunit.runner.visualstudio from 2.4.3 to 2.4.5 ([b9d3c8d](https://github.com/sitkoru/Sitko.Core/commit/b9d3c8db67ba2bf0b45bf3bb0002782aa9c3658f))
* **xunitweb:** implement base scope with test server for WebApplication ([5300efe](https://github.com/sitkoru/Sitko.Core/commit/5300efe1c43890261db15856817693b9f1d81284))


### Performance Improvements

* **app:** reuse application context, cache module options ([e1a1029](https://github.com/sitkoru/Sitko.Core/commit/e1a102901e1b2b2141c619ceb9736c5f8775255b))


### BREAKING CHANGES

* All modules must use IApplicationContext
* **blazor:** Sitko.Core.App.Blazor is split to Sitko.Core.Blazor and Sitko.Core.Blazor.Server
* All modules must be updated
* **app:** Application class is runtime-independent now. HostedApplication is new base class for web/console applications.

# [9.0.0-beta.41](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.40...9.0.0-beta.41) (2022-05-06)


### Features

* **s3storage:** update AWSSDK.S3 from 3.7.8.27 to 3.7.8.28 ([5034ada](https://github.com/sitkoru/Sitko.Core/commit/5034adab514b65d2b9d13ce04a12ccd98180b074))

# [9.0.0-beta.40](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.39...9.0.0-beta.40) (2022-05-05)


### Features

* **blazor:** update CompareNETObjects from 4.76.0 to 4.77.0 ([a6f40e1](https://github.com/sitkoru/Sitko.Core/commit/a6f40e1030b4412f67c103e5a35a9266b16b3b9b))
* **consul:** update Consul from 1.6.10.4 to 1.6.10.5 ([b82fefa](https://github.com/sitkoru/Sitko.Core/commit/b82fefa0b814786c8ffa7518ee5025fa0444b56a))
* **mudblazor:** update MudBlazor from 6.0.9 to 6.0.10 ([fb728c5](https://github.com/sitkoru/Sitko.Core/commit/fb728c5d85edc1f0e4a44734327198fdc9d18455))
* **nats:** update NATS.Client from 0.14.5 to 0.14.6 ([5ced38e](https://github.com/sitkoru/Sitko.Core/commit/5ced38e8e9adb865b8fa33cd902986039ccd8560))
* **protobuf:** update Google.Protobuf from 3.20.0 to 3.20.1 ([f5814aa](https://github.com/sitkoru/Sitko.Core/commit/f5814aaaf4ece43efddc20ec9dcb6e7173dcaf47))
* **remoterepository:** support more where/sort methods ([fe6bc07](https://github.com/sitkoru/Sitko.Core/commit/fe6bc072b64fe224ae2eb346b7a21e572f673bda))
* **remoterepository:** update CompareNETObjects from 4.76.0 to 4.77.0 ([dfebca0](https://github.com/sitkoru/Sitko.Core/commit/dfebca080ad722eace04f4814e53d98524e20618))
* **s3storage:** update AWSSDK.S3 from 3.7.8.22 to 3.7.8.27 ([3a3b691](https://github.com/sitkoru/Sitko.Core/commit/3a3b6919cf987e56001f3b6c1df6b3d0c45babc4))
* **swagger:** update Swashbuckle.AspNetCore from 6.3.0 to 6.3.1 ([5b24751](https://github.com/sitkoru/Sitko.Core/commit/5b247516298389c6cf49ea22987488704bab2dbc))
* **xunit:** update xunit.runner.visualstudio from 2.4.3 to 2.4.5 ([b9d3c8d](https://github.com/sitkoru/Sitko.Core/commit/b9d3c8db67ba2bf0b45bf3bb0002782aa9c3658f))

# [9.0.0-beta.39](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.38...9.0.0-beta.39) (2022-04-28)


### Bug Fixes

* **oidc:** allow configure auto token refresh ([39da17f](https://github.com/sitkoru/Sitko.Core/commit/39da17f366d78174fc177cde3f1ae7f51ccd2d9f))

# [9.0.0-beta.38](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.37...9.0.0-beta.38) (2022-04-28)


### Features

* **oidc:** add tokens auto-update ([7c30b93](https://github.com/sitkoru/Sitko.Core/commit/7c30b93a96362edc784bea272a65ea9c22eac5b5))

# [9.0.0-beta.37](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.36...9.0.0-beta.37) (2022-04-20)


### Bug Fixes

* **mudBlazor:** change RowsPerPage in init ServerReloadAsync ([a33c274](https://github.com/sitkoru/Sitko.Core/commit/a33c27455bb86449b3acfec2d29285b2abd9acf0))

# [9.0.0-beta.36](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.35...9.0.0-beta.36) (2022-04-20)


### Bug Fixes

* invoke GetParamsFromUrl ([9baffc6](https://github.com/sitkoru/Sitko.Core/commit/9baffc6db620cc78c70a35f46af768be0905817f))

# [9.0.0-beta.35](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.34...9.0.0-beta.35) (2022-04-20)


### Features

* **mudBlazor:** bind RowsPerPage value ([9a95bc6](https://github.com/sitkoru/Sitko.Core/commit/9a95bc6fa1fc14dde9f94ec828e52ae98f818fba))

# [9.0.0-beta.34](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.33...9.0.0-beta.34) (2022-04-20)


### Features

* **.net:** 3.1.24 ([2284a54](https://github.com/sitkoru/Sitko.Core/commit/2284a5433e165952af47b3c008a5f3858353b598))
* **.net:** 5.0.16 ([192fb4b](https://github.com/sitkoru/Sitko.Core/commit/192fb4b67a7c56594bd36f8627684d4472526664))
* **.net:** 6.0.4 ([573a171](https://github.com/sitkoru/Sitko.Core/commit/573a1718221211b38aaa5a1687d0674badad2f8d))
* **antdesign:** update AntDesign from 0.10.4 to 0.10.5 ([14aaa33](https://github.com/sitkoru/Sitko.Core/commit/14aaa337bc6c7627fbe81efedcb840e2e1d6e81d))
* **app:** update FluentValidation.DependencyInjectionExtensions from 10.3.6 to 10.4.0 ([41e9024](https://github.com/sitkoru/Sitko.Core/commit/41e9024c6b14e37be20fe57609c3f1ab9586ba5b))
* **demo:** upd deps ([38503a6](https://github.com/sitkoru/Sitko.Core/commit/38503a60ede447a8c092282810ebe9bc39135e19))
* **elastickstack:** update Elastic.Apm.NetCoreAll from 1.14.0 to 1.14.1 ([36587d2](https://github.com/sitkoru/Sitko.Core/commit/36587d2adc78fa1a0c7c9be569af11ca6a565ba8))
* **elasticsearch:** update NEST from 7.17.0 to 7.17.1 ([b2b8d6c](https://github.com/sitkoru/Sitko.Core/commit/b2b8d6c4c55d2f3af0a33330548081fff7cea9aa))
* **emailmailgun:** update FluentEmail.Mailgun from 3.0.0 to 3.0.2 ([a1b94ec](https://github.com/sitkoru/Sitko.Core/commit/a1b94ec4991d1a05dfb766af055ef90b1361ec9a))
* **emailsmtp:** update MailKit from 3.1.1 to 3.2.0 ([007a9e7](https://github.com/sitkoru/Sitko.Core/commit/007a9e7c5cb7006d1e6094a05b82291c4737b42e))
* **email:** update FluentEmail.Core from 3.0.0 to 3.0.2 ([cdcf746](https://github.com/sitkoru/Sitko.Core/commit/cdcf746ede17d741e7b7fc4f33267250768fb10d))
* **grpc:** grpc 2.45.0, protobuf 3.20.0 ([c9e6790](https://github.com/sitkoru/Sitko.Core/commit/c9e6790f2a99c18af1e0170b6e6aaeafc0547e4c))
* **hangfire:** update Hangfire.PostgreSql from 1.9.6 to 1.9.7 ([0c321ca](https://github.com/sitkoru/Sitko.Core/commit/0c321ca2dbbf99176710a40e7c0f89d1d3cc2a9c))
* **mudblazor:** update MudBlazor from 6.0.7 to 6.0.9 ([5594362](https://github.com/sitkoru/Sitko.Core/commit/5594362f8f74a0be34906a6ca4ed83eec5a0b72b))
* **s3storage:** update AWSSDK.S3 from 3.7.8.5 to 3.7.8.22 ([4581157](https://github.com/sitkoru/Sitko.Core/commit/45811579c000d3c2beb342af89fc31fa97387737))
* **xunit:** update FluentAssertions from 6.5.1 to 6.6.0 ([26d40d1](https://github.com/sitkoru/Sitko.Core/commit/26d40d18c0d09eef6deefe32ed7c8e75c8ab571a))

# [9.0.0-beta.33](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.32...9.0.0-beta.33) (2022-04-19)


### Features

* **mudBlazor:** check result TryGetQueryString on null in mudTable component ([ac87bcb](https://github.com/sitkoru/Sitko.Core/commit/ac87bcbae899013ebacebae523571bfa70335903))
* parse query string with nullable types ([f7b96cd](https://github.com/sitkoru/Sitko.Core/commit/f7b96cd083016febca78ef91fb0647442887d33d))

# [9.0.0-beta.32](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.31...9.0.0-beta.32) (2022-04-19)


### Bug Fixes

* **repository:** use common extensions methods for ThenInclude ([3992547](https://github.com/sitkoru/Sitko.Core/commit/3992547c96c46703770200f45f77cba09221794b))

# [9.0.0-beta.31](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.30...9.0.0-beta.31) (2022-04-19)


### Bug Fixes

* **remoterepository:** fix sequential includes ([12abda7](https://github.com/sitkoru/Sitko.Core/commit/12abda78ab7f26192a0c5d23e22dae59c9a65f5b))

# [9.0.0-beta.30](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.29...9.0.0-beta.30) (2022-04-15)


### Bug Fixes

* **mudBlazor:** rename EnableAddFiltersToUrl to EnableUrlNavigation ([d9b2d58](https://github.com/sitkoru/Sitko.Core/commit/d9b2d5884014f17467f82aa363e6ea91b18d58e9))


### Features

* **apps:** add table filters to demo mudblazor app ([91a0cf7](https://github.com/sitkoru/Sitko.Core/commit/91a0cf7557e96e2a801823819497866901d902f8))
* **mudBlazor:** save table pagination, sort, filters to url ([a0773de](https://github.com/sitkoru/Sitko.Core/commit/a0773de55732f70129acee401b5ab6cb6b75ed02))
* **mudBlazor:** use TryGetQueryString for get params from url ([43a086c](https://github.com/sitkoru/Sitko.Core/commit/43a086c6773d04613cd84072995226f39eeb365e))

# [9.0.0-beta.29](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.28...9.0.0-beta.29) (2022-04-14)


### Bug Fixes

* initialize stateCompressor from constructor ([eb63b7e](https://github.com/sitkoru/Sitko.Core/commit/eb63b7e8ec785ce397379a0db9607288850cd615))


### Features

* create addPersistentState with default services ([4910dd2](https://github.com/sitkoru/Sitko.Core/commit/4910dd252f57ebe1012a94d050a0ee6556120775))
* create PersistentStateModule ([beec222](https://github.com/sitkoru/Sitko.Core/commit/beec2225523c0c28dd007f2c116d66e48189b0cd))
* replace PersistentState module to extension ([2df5d42](https://github.com/sitkoru/Sitko.Core/commit/2df5d428469939cfbbbd28219e60d37e20296334))
* use interfaces in state component ([75e8070](https://github.com/sitkoru/Sitko.Core/commit/75e8070c940e9ad5f1a322849e6a230dc83141a5))

# [9.0.0-beta.28](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.27...9.0.0-beta.28) (2022-04-13)


### Bug Fixes

* rename ASPNET_ENVIRONMENT to ASPNETCORE_ENVIRONMENT ([8b9145d](https://github.com/sitkoru/Sitko.Core/commit/8b9145d224d68f97285587ef3355ea3d03b21df9))

# [9.0.0-beta.27](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.26...9.0.0-beta.27) (2022-04-12)


### Bug Fixes

* **app:** reading env name should be case-insensitive ([a75f11b](https://github.com/sitkoru/Sitko.Core/commit/a75f11b4e2d29b0cc3800c156278510012803468))


### Features

* allow all forwarded headers by default ([1827082](https://github.com/sitkoru/Sitko.Core/commit/1827082ac04e9ff1fc92005e4afef4ae55f80844))

# [9.0.0-beta.26](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.25...9.0.0-beta.26) (2022-04-08)


### Bug Fixes

* do not throw invalid operation on not success result ([a411e45](https://github.com/sitkoru/Sitko.Core/commit/a411e457c79de69d8d09f84f3b81a4c9dc0b467f))
* throw only if not 404 ([d0773a5](https://github.com/sitkoru/Sitko.Core/commit/d0773a53784e3dcea17c2fcd8938c577bd011f87))


### Features

* add Get test for null ([df13655](https://github.com/sitkoru/Sitko.Core/commit/df136553e0f7e27f1370e4abad19bd437e1f7b68))

# [9.0.0-beta.25](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.24...9.0.0-beta.25) (2022-03-21)


### Bug Fixes

* make DateParseHandling equals DateTimeOffset to avoid skipping specified timezone ([fd1840e](https://github.com/sitkoru/Sitko.Core/commit/fd1840e921a1237fb624da80bd49947a4ac648a7))

# [9.0.0-beta.24](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.23...9.0.0-beta.24) (2022-03-14)


### Bug Fixes

* added support for CancellationToken ([35faf17](https://github.com/sitkoru/Sitko.Core/commit/35faf1752311006065c703597e68bba0826b68aa))

# [9.0.0-beta.23](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.22...9.0.0-beta.23) (2022-03-09)


### Features

* **antdesign:** update AntDesign from 0.10.3.1 to 0.10.4 ([79c5200](https://github.com/sitkoru/Sitko.Core/commit/79c52000d7675afc9e38b870cb3df618e2dd25f8))
* **app:** update CompareNETObjects from 4.75.0 to 4.76.0 ([19aecf8](https://github.com/sitkoru/Sitko.Core/commit/19aecf84e547c216683629cbbf4b0adc694867d1))
* **app:** update Scrutor from 4.0.0 to 4.1.0 ([f109903](https://github.com/sitkoru/Sitko.Core/commit/f109903a4c34e79dd853ddc395a4b386cc695771))
* **core:** .NET 5.0.15 ([3dd4ab3](https://github.com/sitkoru/Sitko.Core/commit/3dd4ab3527e83f030bdd308e11e4e52399ce9ab8))
* **core:** .NET 6.0.3 ([aba8ed6](https://github.com/sitkoru/Sitko.Core/commit/aba8ed6d870d94782f62204de052394b6ea32a13))
* **core:** .NET Core 3.1.23 ([da57226](https://github.com/sitkoru/Sitko.Core/commit/da57226248e3bb16d8ddc9b1cb522437616d8175))
* **demo:** update demo deps ([f844f54](https://github.com/sitkoru/Sitko.Core/commit/f844f545b922e8469db4425db02639f5795af200))
* **efrepository:** update System.Linq.Dynamic.Core from 1.2.17 to 1.2.18 ([a26b6bd](https://github.com/sitkoru/Sitko.Core/commit/a26b6bd3bf3d431b9678297307ae3f2662af93db))
* **s3storage:** update AWSSDK.S3 from 3.7.8.1 to 3.7.8.5 ([701d80c](https://github.com/sitkoru/Sitko.Core/commit/701d80c51e4430cc02fa2c092ba153120927c174))
* **swagger:** update Swashbuckle.AspNetCore from 6.2.3 to 6.3.0 ([9e14bba](https://github.com/sitkoru/Sitko.Core/commit/9e14bba2f9038f445289cd0f331cd8cf8a578e11))

# [9.0.0-beta.22](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.21...9.0.0-beta.22) (2022-03-05)


### Features

* **postgresdb:** add option to configure dbcontext factory lifetime ([747f8d3](https://github.com/sitkoru/Sitko.Core/commit/747f8d3cc2cc9e987be51a7ca6aebf3eacd5a84d))

# [9.0.0-beta.21](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.20...9.0.0-beta.21) (2022-03-05)


### Features

* **app:** change services registration order - modules, app actions, startup ([865ea98](https://github.com/sitkoru/Sitko.Core/commit/865ea9875261002680ab554a0b5b97fe463cb185))

# [9.0.0-beta.20](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.19...9.0.0-beta.20) (2022-02-26)


### Features

* **remoterepository:** add full configuration method for http transport ([b7d2f64](https://github.com/sitkoru/Sitko.Core/commit/b7d2f644bd8f2b4efc871e1e7f229138363d14e0))
* **remoterepository:** create named http client, pass http client factory to factory ([4d98110](https://github.com/sitkoru/Sitko.Core/commit/4d98110a925ade91db12c38d4f7758932709c9a5))
* **remoterepository:** new WasmHttpRepositoryTransportModule with browser cookies in http requests ([df6ee18](https://github.com/sitkoru/Sitko.Core/commit/df6ee18c3751cb8806c693ba31dc8c4ce9938b2b))

# [9.0.0-beta.19](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.18...9.0.0-beta.19) (2022-02-26)


### Bug Fixes

* **repository:** don't finish batch if it wasn't started inside this operation ([d0a8216](https://github.com/sitkoru/Sitko.Core/commit/d0a82162ce2261dbfc24b082ddf9a044c386e0c1))


### Features

* **app:** update Scrutor from 3.3.0 to 4.0.0 ([5cbbd6a](https://github.com/sitkoru/Sitko.Core/commit/5cbbd6ae192887db8df92bd866fc14ad1651861c))
* **app:** update Serilog.Exceptions from 8.0.0 to 8.1.0 ([cd74978](https://github.com/sitkoru/Sitko.Core/commit/cd7497893946aaeec8a4ec09e301c67e73ff737c))
* **efrepository:** update System.Linq.Dynamic.Core from 1.2.15 to 1.2.17 ([833a3b3](https://github.com/sitkoru/Sitko.Core/commit/833a3b3f35958516e549c167b79f42ffd76521ad))
* **grpc:** update Grpc.Net to 2.43.0 ([4bd9b87](https://github.com/sitkoru/Sitko.Core/commit/4bd9b8726165c13328ed62e4a707732a15e74cd1))
* **grpc:** update Grpc.Tools to 2.44.0 ([23fe847](https://github.com/sitkoru/Sitko.Core/commit/23fe847b1234e8113dc563d8e936da1a70a41c6a))
* **mudblazor:** update MudBlazor from 6.0.6 to 6.0.7 ([f1b9b55](https://github.com/sitkoru/Sitko.Core/commit/f1b9b55220f2d4fd79337055b3ddfe7061e560d1))
* **puppeteer:** update PuppeteerSharp from 6.1.0 to 6.2.0 ([7c7097b](https://github.com/sitkoru/Sitko.Core/commit/7c7097b0ee9231da7293c5fe39ac0683b96a2b9e))
* **remoterepository:** update CompareNETObjects from 4.74.0 to 4.75.0 ([328ee43](https://github.com/sitkoru/Sitko.Core/commit/328ee434d7e5c5b42330a3299d8e2a756faaf3c9))
* **s3storage:** update AWSSDK.S3 from 3.7.7.21 to 3.7.8.1 ([9d3136e](https://github.com/sitkoru/Sitko.Core/commit/9d3136eff1301519c45a6fb38006ef0106589c09))
* **xunit:** update Microsoft.NET.Test.Sdk from 17.0.0 to 17.1.0 ([b32396e](https://github.com/sitkoru/Sitko.Core/commit/b32396ecd21f9fa4b40d3cf07be81f16f4d100c4))

# [9.0.0-beta.18](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.17...9.0.0-beta.18) (2022-02-22)


### Features

* **elasticstack:** expose elastic sink failure handling options ([55115a9](https://github.com/sitkoru/Sitko.Core/commit/55115a9eb0466fa5a38359e7a2b608b98aa11095))

# [9.0.0-beta.17](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.16...9.0.0-beta.17) (2022-02-17)


### Bug Fixes

* add missing test repositories ([e0b06f0](https://github.com/sitkoru/Sitko.Core/commit/e0b06f0f5af8f3e10593fca626cd586c4bc0917f))
* add param to httpclient ([3ee4c3a](https://github.com/sitkoru/Sitko.Core/commit/3ee4c3ada1a1b85740228d3980f7a54602d3a8a7))
* add RemoteRepositoryQuery parameterless ctor ([24c09c2](https://github.com/sitkoru/Sitko.Core/commit/24c09c2d9ab75521d3f44054dee775c7e760a486))
* add scoped repositoryTransport and remove module options ([37703d4](https://github.com/sitkoru/Sitko.Core/commit/37703d408300a9029cdbdc499f1226c61d0cb0f8))
* add url param to SendHttpRequestAsync() ([e51fe7b](https://github.com/sitkoru/Sitko.Core/commit/e51fe7bf6c33ed14594c648e2d1e1b7dc064ac94))
* change GetAll return type ([39d0880](https://github.com/sitkoru/Sitko.Core/commit/39d0880f21e641b29c91cbe8d00f53450c730f0c))
* change methods param type to SerializedQuery ([5557334](https://github.com/sitkoru/Sitko.Core/commit/55573344c9eec24e29858b5acdcf13dd7fb241b2))
* change options module inheritance ([9b00b89](https://github.com/sitkoru/Sitko.Core/commit/9b00b89d5d1700e21c85e3b68b0773132a66a859))
* change route params to Uri type and drop ctor ([21c8186](https://github.com/sitkoru/Sitko.Core/commit/21c81865a496a6d9e9251a45ac5c51ef1f9c2dac))
* change target framework for remote repo server project ([6144a10](https://github.com/sitkoru/Sitko.Core/commit/6144a10db9762aa6305184e3f9f254a3052c2cb7))
* change transport module connection ([954585c](https://github.com/sitkoru/Sitko.Core/commit/954585c6649c1e7ede91efb93d258669da4c9bb8))
* drop unused methods, change methods request Type, fix methods param ([5fb8f87](https://github.com/sitkoru/Sitko.Core/commit/5fb8f872e53d89771d9823a584941f721c07bfac))
* **efrepository:** await refresh result ([ccd407e](https://github.com/sitkoru/Sitko.Core/commit/ccd407ea62338aea197d301095df4d787050283b))
* fix repository module initializing ([e815cc8](https://github.com/sitkoru/Sitko.Core/commit/e815cc86400fff14bb689ea286eda8ae474ff3e1))
* fixed getting name of desired entity type for controller route ([8eb01c3](https://github.com/sitkoru/Sitko.Core/commit/8eb01c39ba7937bf804c37ca905e9e91cbf1c52b))
* fixed include query building ([cebf568](https://github.com/sitkoru/Sitko.Core/commit/cebf568b6cc52e3d17c05794e4f0807220a5e00f))
* fixed transport application extension ([af6819d](https://github.com/sitkoru/Sitko.Core/commit/af6819d9b8d73ad0b137663b1cafc973bd88e5f5))
* implement missing select members from interface ([86a97f2](https://github.com/sitkoru/Sitko.Core/commit/86a97f27f98bb1a4b15939098a5302d6141b331e))
* initialize collection ([3ac46ec](https://github.com/sitkoru/Sitko.Core/commit/3ac46ec0dfb04fc999112b84cf5ed95d7a681ed2))
* make BaseRemoteRepo non abstract ([e73f521](https://github.com/sitkoru/Sitko.Core/commit/e73f52194b59ad44d43f6579cb539986dda802e8))
* make DoSaveSync return CompletedTask ([dc6c468](https://github.com/sitkoru/Sitko.Core/commit/dc6c468d242e1cc9abafdec411bb2eb290de72be))
* make transport validator non abstract ([3a91ba6](https://github.com/sitkoru/Sitko.Core/commit/3a91ba607ed7da202152a8d0d5524599d4076236))
* move repository transport injection into repository context ([8497ea1](https://github.com/sitkoru/Sitko.Core/commit/8497ea14f8f0c2dba370e32535e2d09730b63b5c))
* move scope to data folder, add repositories in app ([0265742](https://github.com/sitkoru/Sitko.Core/commit/0265742827d91162e620510484e479f49d8c17fb))
* register repositories from all assemblies ([440b0c7](https://github.com/sitkoru/Sitko.Core/commit/440b0c73a4fec25733459631b7f726004c33bd77))
* **remoterepository:** avoid snapshot duplications issue ([4176747](https://github.com/sitkoru/Sitko.Core/commit/41767473363cde0a548e32decc196e5fad2179ba))
* **remoterepository:** cleanup transactions code ([6d4609a](https://github.com/sitkoru/Sitko.Core/commit/6d4609adb1bc46675f8e96a782a61c70ae149ad1))
* **remoterepository:** don't need those select methods, reformat code ([625c1b0](https://github.com/sitkoru/Sitko.Core/commit/625c1b0131f78a4bcd572363b407680346aff56a))
* **remoterepository:** fill change with real objects, not strings ([85018ae](https://github.com/sitkoru/Sitko.Core/commit/85018ae455f628390cff20adf1585855239d0b80))
* **remoterepository:** init properties ([f09301f](https://github.com/sitkoru/Sitko.Core/commit/f09301f1383a53c9b758548e1c1186a6066b8978))
* **remoterepository:** pass sum type as parameter ([d0057e5](https://github.com/sitkoru/Sitko.Core/commit/d0057e56c8f1eda59f2a29c02cc7b434f637cb08))
* **remoterepository:** remove old snapshots code ([59c5093](https://github.com/sitkoru/Sitko.Core/commit/59c5093b795163ccc5a348e20c47195b7f179676))
* **remoterepository:** use correct property ([b093a38](https://github.com/sitkoru/Sitko.Core/commit/b093a3839a3dfed4b32614ece324723f45753020))
* **remoterepository:** use ListResult cause tuple serialization is hard ([363f8f2](https://github.com/sitkoru/Sitko.Core/commit/363f8f2760508fbd57115c29b940cc48842914de))
* remove repository options and unnecessary props ([0410263](https://github.com/sitkoru/Sitko.Core/commit/0410263558427c1d32fcd3414a899069c3b9a6c9))
* remove transport injecting from test controllers ([62d6659](https://github.com/sitkoru/Sitko.Core/commit/62d665917bd0c9878c11d746bb2246901888264f))
* remove unnecessary generic param when adding httpclient ([bc2bbd2](https://github.com/sitkoru/Sitko.Core/commit/bc2bbd2c4d0d5c18d0876c525cde9e1303bc7698))
* remove unneeded files from remote tests ([c2dc8db](https://github.com/sitkoru/Sitko.Core/commit/c2dc8db7f38393334a87729df03e3e8f05546538))
* **repository:** remove property duplication ([eeec044](https://github.com/sitkoru/Sitko.Core/commit/eeec044fb5417dac24b6b01c79b7dc574907480b))
* **sln:** fix sln after rebase ([bbf1462](https://github.com/sitkoru/Sitko.Core/commit/bbf1462654c5fbf1dec016c2353dd8bebfe603f0))
* **test:** fix controller routes ([7efd191](https://github.com/sitkoru/Sitko.Core/commit/7efd1917c0e3faf95fdc7377b36be8845f4e8da0))
* **tests:** fix repositories injected repository context ([f533f97](https://github.com/sitkoru/Sitko.Core/commit/f533f97c8f5a02112a9eb336785cbd77c5afbc25))
* **tests:** make repository ctor public ([07d63e1](https://github.com/sitkoru/Sitko.Core/commit/07d63e1d0f8b19e5000acab079ad490ab47d4ba5))
* **tests:** replace add test to getall test ([9d161a5](https://github.com/sitkoru/Sitko.Core/commit/9d161a51da765c0ce2f0343fb73c153a1372fdd8))
* unify methods for sending request, change methods params ([ffc048a](https://github.com/sitkoru/Sitko.Core/commit/ffc048aabc9bede68dc953e9830fa728eeeea1e8))
* use different options ([42b562d](https://github.com/sitkoru/Sitko.Core/commit/42b562d267a6856f8fbb5fd797d8ec28c1c3cf7a))


### Features

* add base remote repository class ([bd7b3c0](https://github.com/sitkoru/Sitko.Core/commit/bd7b3c0809055979b4078f8692d599122a8161f8))
* add blank for init db, add validation models from assembly ([ee6eea5](https://github.com/sitkoru/Sitko.Core/commit/ee6eea5cd608d694839bdb053b94b3b423140084))
* add blank Select method ([bfaf68a](https://github.com/sitkoru/Sitko.Core/commit/bfaf68a078e6f896906fd4976b7e5c3ac07a0d86))
* add extensions method to remote repo ([a483da7](https://github.com/sitkoru/Sitko.Core/commit/a483da79cab45e1bf9097eb276fad6c5dfece75d))
* add http repository transport ([1cd9b45](https://github.com/sitkoru/Sitko.Core/commit/1cd9b451d56c6aada6594049d2f0b9f96f8b376f))
* add http repository transport application extension ([2a1a04f](https://github.com/sitkoru/Sitko.Core/commit/2a1a04f101091d01b21b139f155f75d730857186))
* add Include method to IRepository ([0c3739f](https://github.com/sitkoru/Sitko.Core/commit/0c3739f0b0f51404b9786bd34b274ca1df6a2dd1))
* add init webapp method ([979f35c](https://github.com/sitkoru/Sitko.Core/commit/979f35c6301beb83e2cbbbb96c8a8fef40be533c))
* add IRemoteRepo interface ([9598bfc](https://github.com/sitkoru/Sitko.Core/commit/9598bfcec31bcc8cdca3ec41bf5fbf2110c57e98))
* add methods, fix routes ([634188b](https://github.com/sitkoru/Sitko.Core/commit/634188b2fe739e85f750b05afa7857ed036bb5b6))
* add modules to remote repo ([370b31d](https://github.com/sitkoru/Sitko.Core/commit/370b31dc2922d3c9961d6a3c48dedb1b44678f6d))
* add queries for remote repo ([28c8c70](https://github.com/sitkoru/Sitko.Core/commit/28c8c70dfb8875c2c520ba5fb020d62166618c07))
* add remote repo add test blank ([65171b9](https://github.com/sitkoru/Sitko.Core/commit/65171b9575dcbf2fba087ef86891ac8b2ec0ae1f))
* add remote repo options ([a35fa6a](https://github.com/sitkoru/Sitko.Core/commit/a35fa6ad8e3820d76e631c85e52b8610d4716560))
* add remote repo test scope ([e320242](https://github.com/sitkoru/Sitko.Core/commit/e320242d4e38309705421d0e1a28ebf3b7a0c617))
* add remote repository projects ([7907606](https://github.com/sitkoru/Sitko.Core/commit/7907606228400d77f7e7c62f4f579de877758bac))
* add remote repository scope ([2709bdc](https://github.com/sitkoru/Sitko.Core/commit/2709bdc248421f99ed0aa8d2f81c7fc7fe4487db))
* add remote tests ([5b89ee5](https://github.com/sitkoru/Sitko.Core/commit/5b89ee5ec7bccf3854c25a275fe13920abb5289f))
* add repository options ([1d394bf](https://github.com/sitkoru/Sitko.Core/commit/1d394bfcee254f2644ff00aacbee780bf789599d))
* add repository transport interface ([282c360](https://github.com/sitkoru/Sitko.Core/commit/282c36045a8c9cdf541428356e0165fe44c75e47))
* add select methods to IRepositoryQuery ([fb3d2fd](https://github.com/sitkoru/Sitko.Core/commit/fb3d2fd39b89587b9372506e87f4180c98fd290c))
* add server controller for remote repo ([2a970b5](https://github.com/sitkoru/Sitko.Core/commit/2a970b5d5baa689514a4943d7c20eca9a82c07e5))
* add test data ([2ecf9f9](https://github.com/sitkoru/Sitko.Core/commit/2ecf9f94e666acec0c0777ea58ef3c9480533acc))
* add test ef repo ([da13af5](https://github.com/sitkoru/Sitko.Core/commit/da13af5e20faa066cb5e09c893c5d76f25dc0cc6))
* add test repositories ([a3afa98](https://github.com/sitkoru/Sitko.Core/commit/a3afa98a37abe435aea2723104e6dfb3a22d0b13))
* change Refresh implementation ([5e57a69](https://github.com/sitkoru/Sitko.Core/commit/5e57a696f4dfa570d57e933d2f9cd16778197271))
* change return type for Refresh method ([3c69181](https://github.com/sitkoru/Sitko.Core/commit/3c691814544f05088a26e2838111ddd0cb3d32b5))
* **ci:** update tests steps ([326dcfa](https://github.com/sitkoru/Sitko.Core/commit/326dcfa5bfd797f5a92bc74bbd4b6750b436257a))
* create standalone http transport module ([624773b](https://github.com/sitkoru/Sitko.Core/commit/624773b0d6377143ba388cb35ca0f94841109867))
* create standalone httptransport ([53b07ac](https://github.com/sitkoru/Sitko.Core/commit/53b07acc11e632d39f930294945c92d7f0a9df3a))
* expand methods fore remote repository ([bf55040](https://github.com/sitkoru/Sitko.Core/commit/bf55040a44e782729a1e2765383e782af47babaf))
* expand remote transport interface ([f909fcc](https://github.com/sitkoru/Sitko.Core/commit/f909fcc3cadff80118d749f7187fc538cc3fa5d7))
* implement GetChangesAsync() ([ed72ac1](https://github.com/sitkoru/Sitko.Core/commit/ed72ac12c7d9141a8b161aea520a9bf211049184))
* implement include for RemoteRepositoryQuery ([99a7dd6](https://github.com/sitkoru/Sitko.Core/commit/99a7dd6f740af8b4d74e995ed8a60d83801f6864))
* implement includes ([49ee44d](https://github.com/sitkoru/Sitko.Core/commit/49ee44dafc182962a34283c307b6c4a676942e0b))
* implement missed methods ([f3fb327](https://github.com/sitkoru/Sitko.Core/commit/f3fb32773333b46819c7ac5d8c778f79736c31c9))
* implement missing remote transport interface members ([ae06e75](https://github.com/sitkoru/Sitko.Core/commit/ae06e75668f9a3712b49879d617f665c10906cb7))
* implement some methods ([ddd662f](https://github.com/sitkoru/Sitko.Core/commit/ddd662f8ac0ca8ff779fb46fbc17ca0785bb1589))
* implement sum methods, bit of refactoring ([e75e91d](https://github.com/sitkoru/Sitko.Core/commit/e75e91de97b4ee8589962c0bc7dae6da72663417))
* implement transactions methods and refresh method ([2bd3cc7](https://github.com/sitkoru/Sitko.Core/commit/2bd3cc71c450d98ca16654f0fed08b200fef7d7e))
* multiply sum methods for structs ([8ad71c2](https://github.com/sitkoru/Sitko.Core/commit/8ad71c2b60df16f43ce1976e471d25aaa1fd66aa))
* new include method realization for EFRepositoryQuery ([238f214](https://github.com/sitkoru/Sitko.Core/commit/238f21435d98ff5f45f740f461685966c40edc85))
* rearrange tests for repositories ([d416156](https://github.com/sitkoru/Sitko.Core/commit/d416156a14626cf0400e0080e652211d99103a5c))
* remote controller now have EfRepo DI ([7cf8453](https://github.com/sitkoru/Sitko.Core/commit/7cf8453922ff1b623764a76d91343fb1028b53aa))
* **remoterepository:** add ThenInclude extensions and test ([5288b09](https://github.com/sitkoru/Sitko.Core/commit/5288b090f51e62236a773befd3b850b726a0e227))
* **remoterepository:** improve error handling in controller and transport ([67724b5](https://github.com/sitkoru/Sitko.Core/commit/67724b51c34be5277ecb832b1a5b7f851686465f))
* **remoterepository:** rework server controller ([f03ffd7](https://github.com/sitkoru/Sitko.Core/commit/f03ffd703d76871dec1d4c0c6f81fdb26e5bffdf))
* **remoterepository:** split serialized query and it's data to simplify syntax and serialization ([4851f49](https://github.com/sitkoru/Sitko.Core/commit/4851f49da0d2ea2e178a589c2b5bb98c864de130))
* **remoterepository:** support abstract select expression instead of type int/long/etc. Support whereByString. Grab all data from source query. ([9728f2b](https://github.com/sitkoru/Sitko.Core/commit/9728f2be6cda02a3ad4f03a8b71725bc8fca4b6c))
* remove base value for api route and add validator for it ([1308c39](https://github.com/sitkoru/Sitko.Core/commit/1308c391e4be52bc5081e157a1350017db611701))
* **repository:** convert AddOrUpdateOperationResult to record for better serializability ([9b525f3](https://github.com/sitkoru/Sitko.Core/commit/9b525f3216c90175795e66c6ea779817331c11cf))
* split test repositories to efrepos and remote repos ([9763aa6](https://github.com/sitkoru/Sitko.Core/commit/9763aa6b9fd992503c9db6d82c2d792163ffe0f8))
* **tests:** add remote repository tests project ([d1c1540](https://github.com/sitkoru/Sitko.Core/commit/d1c15408284984ecad40bfa6866808982e8d72a9))
* **tests:** add server side controller ([cbeab71](https://github.com/sitkoru/Sitko.Core/commit/cbeab71e9bdf1e9f667b58dc25d88c6be49eccf9))
* **tests:** make EfTest abstract basic repository tests ([fda6e2f](https://github.com/sitkoru/Sitko.Core/commit/fda6e2f94daf305098ed24e95bc7f6b567acc737))

# [9.0.0-beta.16](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.15...9.0.0-beta.16) (2022-02-14)


### Bug Fixes

* **grpc:** don't need separate module, just add external and configure handler wrapper ([97527d1](https://github.com/sitkoru/Sitko.Core/commit/97527d187eecaafc492ded457c9506cdebc3d769))


### Features

* **.net:** .NET 5.0.14 ([8dcd63f](https://github.com/sitkoru/Sitko.Core/commit/8dcd63f6ff8b62e3bfbee41203bb35f4947159ce))
* **.net:** .NET 6.0.2 ([ee32cc5](https://github.com/sitkoru/Sitko.Core/commit/ee32cc545b492abde6081aa0f38551095f75ee24))
* **auth:** update idunno.Authentication.Basic from 2.2.2 to 2.2.3 ([6a8bf9a](https://github.com/sitkoru/Sitko.Core/commit/6a8bf9a950a84562dd38d8818e55e2a2ff22659e))
* **blazor:** update CompareNETObjects from 4.74.0 to 4.75.0 ([d1d0c64](https://github.com/sitkoru/Sitko.Core/commit/d1d0c64e73f4d66945de831f95f717dd5aca3aaa))
* **elasticsearch:** update NEST from 7.16.0 to 7.17.0 ([78cd828](https://github.com/sitkoru/Sitko.Core/commit/78cd8286c85bca4e219630b4f869c6e4709f81bf))
* **elasticstack:** update Elastic.Apm.NetCoreAll from 1.12.1 to 1.14.0 ([930e6dc](https://github.com/sitkoru/Sitko.Core/commit/930e6dc446a46630150c836ce2152ca078142998))
* **email:** update HtmlAgilityPack from 1.11.39 to 1.11.42 ([774c8ad](https://github.com/sitkoru/Sitko.Core/commit/774c8ad8b1ae50734d89595f0638001c94ea5a45))
* **email:** update MailKit from 3.0.0 to 3.1.1 ([742be4c](https://github.com/sitkoru/Sitko.Core/commit/742be4cbc0ab40e22989aef292e3ba6e335550d2))
* **grpc:** update Grpc.Net from 2.41.0 to 2.42.0 ([8ddceee](https://github.com/sitkoru/Sitko.Core/commit/8ddceeed51b10e71db40541e69844e916e24d9e1))
* **hangfire:** update Hangfire.PostgreSql from 1.9.5 to 1.9.6 ([4386a4a](https://github.com/sitkoru/Sitko.Core/commit/4386a4a1d2b7ec407b5c60d8dd53de388e91e751))
* **mudblazor:** update MudBlazor from 6.0.4 to 6.0.6 ([f71bd0d](https://github.com/sitkoru/Sitko.Core/commit/f71bd0dc618f352e76b036e018034d78ac2646a5))
* **nats:** update NATS.Client from 0.14.4 to 0.14.5 ([45f788b](https://github.com/sitkoru/Sitko.Core/commit/45f788bf92a895fe21861a8767a3696de6783361))
* **protobuf:** update Google.Protobuf from 3.19.2 to 3.19.4 ([ffc2226](https://github.com/sitkoru/Sitko.Core/commit/ffc2226550efda491b019938bd0db41894c0573a))
* **s3:** update AWSSDK.S3 from 3.7.7.10 to 3.7.7.21 ([cc98e8a](https://github.com/sitkoru/Sitko.Core/commit/cc98e8a64e669b7a29ee25b6f3912836ad954ff9))
* **xunit:** update FluentAssertions from 6.3.0 to 6.5.1 ([bea9635](https://github.com/sitkoru/Sitko.Core/commit/bea96351b47a1d86197a1150ba1a2cbf18ed9cc5))

# [9.0.0-beta.15](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.14...9.0.0-beta.15) (2022-02-14)


### Bug Fixes

* **xunit:** use scope id for database name generation ([7225355](https://github.com/sitkoru/Sitko.Core/commit/7225355e7cc98afcaa871392e6bf532775122571))


### Features

* **grpc:** add grpc web wrapper module ([91e35da](https://github.com/sitkoru/Sitko.Core/commit/91e35dab2cfc9133d2f89d78ac33bcfb1773ef57))

# [9.0.0-beta.14](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.13...9.0.0-beta.14) (2022-02-03)


### Bug Fixes

* **app:** fix IsProduction override in HostedApplication ([d73a3b7](https://github.com/sitkoru/Sitko.Core/commit/d73a3b750d6d65b9bcc38ef78e0073cf009ecb1c))

# [9.0.0-beta.13](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.12...9.0.0-beta.13) (2022-01-26)


### Bug Fixes

* **xunit:** pass scope name to BeforeConfiguredAsync hook ([fb78aa3](https://github.com/sitkoru/Sitko.Core/commit/fb78aa38712641b64238f0efe46c41791a492ed6))


### Features

* **xunitweb:** implement base scope with test server for WebApplication ([5300efe](https://github.com/sitkoru/Sitko.Core/commit/5300efe1c43890261db15856817693b9f1d81284))

# [9.0.0-beta.12](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.11...9.0.0-beta.12) (2022-01-26)


### Bug Fixes

* **app:** fix module options validation ([ea1a14e](https://github.com/sitkoru/Sitko.Core/commit/ea1a14eaf936c1199b16087f7c5ee316751809df))
* **app:** use existing list of enabled modules ([8a12a3a](https://github.com/sitkoru/Sitko.Core/commit/8a12a3abb29982f8a19c169220d2214bf9af3f8f))
* **xunit:** prevent dispose cycle ([db0b40c](https://github.com/sitkoru/Sitko.Core/commit/db0b40c9291c14b538a3b2a713203541e6dae052))


### Features

* **app:** drop tmp host builder, create configuration and env manually ([d298c34](https://github.com/sitkoru/Sitko.Core/commit/d298c34389fa77dcc18245a7cdbf0387a29de19f))
* **app:** proceed only specific modules to reduce options building ([3f17438](https://github.com/sitkoru/Sitko.Core/commit/3f17438b9d1b61db51970741a41b6532c7aac985))
* **xunit:** rework test scope dispose ([b327591](https://github.com/sitkoru/Sitko.Core/commit/b32759173d351c59d673f801073f950a5505d046))


### Performance Improvements

* **app:** reuse application context, cache module options ([e1a1029](https://github.com/sitkoru/Sitko.Core/commit/e1a102901e1b2b2141c619ceb9736c5f8775255b))

# [9.0.0-beta.11](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.10...9.0.0-beta.11) (2022-01-25)


### Features

* **hangfire:** allow to configure all hangfire dashboard options ([acdaeb4](https://github.com/sitkoru/Sitko.Core/commit/acdaeb46c1dacdf0f8085725d20bc43db3527647))
* **hangfire:** provide some hangfire dashboard auth filters ([39f3dc8](https://github.com/sitkoru/Sitko.Core/commit/39f3dc81af59d99ea2bdb10f707dabab1ae11201))
* **hangfire:** switch to use AddHangfireServer ([c504ae3](https://github.com/sitkoru/Sitko.Core/commit/c504ae3255b9cc3f5294a14431d56b24270398ac))

# [9.0.0-beta.10](https://github.com/sitkoru/Sitko.Core/compare/9.0.0-beta.9...9.0.0-beta.10) (2022-01-25)


### Bug Fixes

* **app:** get assembly name and version from current application instance ([f7a88dd](https://github.com/sitkoru/Sitko.Core/commit/f7a88dd4fc661e894e94cfe98c82089e4e9dea48))

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
