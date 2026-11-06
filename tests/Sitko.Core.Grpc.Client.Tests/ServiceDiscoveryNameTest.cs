using Sitko.Core.ServiceDiscovery;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class ServiceDiscoveryNameTest
    {
        [Theory]
        [InlineData("MYCG.Auth.Grpc.UsersService", "grpc-production-users-2dddfe5cbf3f")]
        [InlineData("MYCG.AI.Grpc.AiAssistantGrpcService", "grpc-production-ai-assistant-grpc-cbf9382ed9cf")]
        public void KnownContractHasReadableEnvironmentFirstName(string contract, string expected) =>
            Assert.Equal(expected, ServiceDiscoveryName.Create("production", contract));

        [Theory]
        [InlineData("example.ServiceDirectoryService", "grpc-local-service-directory-bc0b138c8a1c")]
        [InlineData("example.Users", "grpc-local-users-d01e3bfb0809")]
        [InlineData("example.Usersservice", "grpc-local-usersservice-a39d42645a04")]
        public void OnlyTrailingCaseSensitiveServiceIsRemoved(string contract, string expected) =>
            Assert.Equal(expected, ServiceDiscoveryName.Create("local", contract));

        [Fact]
        public void EnvironmentAndAcronymContractUseDnsAsciiCharacters()
        {
            var name = ServiceDiscoveryName.Create("Staging_EU!", "example.HTTPStatus2Service");

            Assert.Equal("grpc-staging-eu-http-status2-20ad2953d457", name);
            Assert.Matches("^[a-z0-9][a-z0-9-]*[a-z0-9]$", name);
        }

        [Theory]
        [InlineData("local", "example.Service", "grpc-local-09e2a0367d5e")]
        [InlineData("", "", "grpc-ba768b331fd8")]
        [InlineData("!!!", "例", "grpc-8d88cb95a1c5")]
        public void EmptyReadablePartsKeepValidDnsName(string environment, string contract, string expected) =>
            Assert.Equal(expected, ServiceDiscoveryName.Create(environment, contract));

        [Fact]
        public void LongKnownContractKeepsHashAtTheEndWithinDnsLimit()
        {
            var name = ServiceDiscoveryName.Create("production",
                "MYCG.Yandex.Direct.Grpc.YandexDirectNegativeKeywordSharedSetsService");

            Assert.Equal("grpc-production-yandex-direct-negative-keyword-sha-8207a87a089f", name);
            Assert.Equal(63, name.Length);
        }

        [Theory]
        [InlineData(38, 62)]
        [InlineData(39, 63)]
        [InlineData(40, 63)]
        public void DnsLengthBoundaryKeepsStableHashSuffix(int contractLength, int expectedLength)
        {
            var contract = "example." + new string('a', contractLength);
            var name = ServiceDiscoveryName.Create("local", contract);

            Assert.Equal(expectedLength, name.Length);
            Assert.StartsWith("grpc-local-", name);
            Assert.Matches("-[a-f0-9]{12}$", name);
            Assert.Equal(name, ServiceDiscoveryName.Create("local", contract));
        }

        [Theory]
        [InlineData("local", "one.UsersService", "local", "two.UsersService")]
        [InlineData("local", "example.UsersService", "local", "example.usersService")]
        [InlineData("Local_A", "example.UsersService", "Local-A", "example.UsersService")]
        public void FullIdentityHashSeparatesEqualReadableNames(string environmentA, string contractA,
            string environmentB, string contractB)
        {
            var nameA = ServiceDiscoveryName.Create(environmentA, contractA);
            var nameB = ServiceDiscoveryName.Create(environmentB, contractB);

            Assert.Equal(nameA[..^13], nameB[..^13]);
            Assert.NotEqual(nameA[^12..], nameB[^12..]);
            Assert.Equal(nameA, ServiceDiscoveryName.Create(environmentA, contractA));
            Assert.Equal(nameB, ServiceDiscoveryName.Create(environmentB, contractB));
        }

        [Fact]
        public void TruncatedNamesKeepDistinctStableFullIdentityHashesAtTheEnd()
        {
            var commonContract = "example." + new string('a', 80);
            var nameA = ServiceDiscoveryName.Create("production", commonContract + "OneService");
            var nameB = ServiceDiscoveryName.Create("production", commonContract + "TwoService");
            var longEnvironment = ServiceDiscoveryName.Create(new string('b', 100), commonContract);

            Assert.Equal(63, nameA.Length);
            Assert.Equal(63, nameB.Length);
            Assert.Equal(63, longEnvironment.Length);
            Assert.Equal(nameA[..^13], nameB[..^13]);
            Assert.StartsWith("grpc-production-", nameA);
            Assert.Matches("^[a-z0-9-]+-[a-f0-9]{12}$", nameA);
            Assert.Matches("^[a-z0-9-]+-[a-f0-9]{12}$", longEnvironment);
            Assert.NotEqual(nameA[^12..], nameB[^12..]);
            Assert.Equal(nameA, ServiceDiscoveryName.Create("production", commonContract + "OneService"));
            Assert.Equal(nameB, ServiceDiscoveryName.Create("production", commonContract + "TwoService"));
        }
    }
}
