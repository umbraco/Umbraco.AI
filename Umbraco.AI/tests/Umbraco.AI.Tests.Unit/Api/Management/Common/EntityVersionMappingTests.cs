using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Web.Api.Management.Common.Mapping;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Common;

public class EntityVersionMappingTests
{
    [Fact]
    public void Map_KeepsArchiverAndAuthorSeparate()
    {
        // Arrange
        var mapper = new UmbracoMapper(
            new MapDefinitionCollection(() => [new CommonMapDefinition()]),
            Mock.Of<ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);
        var version = new AIEntityVersion
        {
            Id = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            EntityType = "Profile",
            Version = 1,
            DateCreated = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = Guid.NewGuid(),
            DateAuthored = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            AuthoredByUserId = Guid.NewGuid(),
        };

        // Act
        var response = mapper.Map<EntityVersionResponseModel>(version)!;

        // Assert
        response.DateCreated.ShouldBe(version.DateCreated);
        response.CreatedByUserId.ShouldBe(version.CreatedByUserId);
        response.DateAuthored.ShouldBe(version.DateAuthored);
        response.AuthoredByUserId.ShouldBe(version.AuthoredByUserId);
    }
}
