using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using MLoop.Models;

namespace MLoop.Api.Infrastructure.OData;

public static class EdmModelBuilder
{
    public static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<MLScenario>("Scenarios");
        return builder.GetEdmModel();
    }
}