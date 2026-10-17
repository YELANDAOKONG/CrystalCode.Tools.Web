using CrystalCode.Web.Searching;

namespace CrystalCode.Web.Tests.Searching;

public sealed class SearchProvidersTests
{
    [Fact]
    public void EveryProviderHasAParseableDefinition()
    {
        foreach (var provider in Enum.GetValues<SearchProvider>())
        {
            var definition = SearchProviders.Get(provider);

            Assert.Equal(provider, definition.Provider);
            Assert.False(string.IsNullOrWhiteSpace(definition.DefaultEndpoint));
            Assert.False(string.IsNullOrWhiteSpace(definition.KeyVariable));
            Assert.True(SearchProviders.TryParse(definition.Name, out var parsed));
            Assert.Equal(provider, parsed);
        }
    }
}
