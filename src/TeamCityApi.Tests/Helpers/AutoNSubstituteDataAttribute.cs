using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit;

namespace TeamCityApi.Tests.Helpers
{
    internal class AutoNSubstituteDataAttribute : AutoDataAttribute
    {
        internal AutoNSubstituteDataAttribute()
            : base(new Fixture().Customize(new AutoNSubstituteCustomization()))
        {
        }
    }
}