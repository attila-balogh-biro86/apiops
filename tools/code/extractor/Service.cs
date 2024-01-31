using common;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace extractor;

internal static class Service
{
    public static async ValueTask Export(
        ServiceDirectory serviceDirectory,
        ServiceUri serviceUri,
        DefaultApiSpecification defaultSpecification,
        IEnumerable<string>? apiNamesToExport,
        IEnumerable<string>? loggerNamesToExport,
        IEnumerable<string>? diagnosticNamesToExport,
        IEnumerable<string>? namedValueNamesToExport,
        IEnumerable<string>? productNamesToExport,
        IEnumerable<string>? backendNamesToExport,
        IEnumerable<string>? tagNamesToExport,
        IEnumerable<string>? subscriptionNamesToExport,
        IEnumerable<string>? policyFragmentNamesToExport,
        ListRestResources listRestResources,
        GetRestResource getRestResource,
        DownloadResource downloadResource,
        ILogger logger,
        Boolean isProductGroupExportEnabled,
        Boolean IsFilteringEnabled,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Exporting named values...");
        await NamedValue.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, listRestResources, getRestResource, logger, namedValueNamesToExport, cancellationToken);

        logger.LogInformation("Exporting tags...");
        await Tag.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, listRestResources, getRestResource, logger, tagNamesToExport, cancellationToken);

        logger.LogInformation("Exporting version sets...");
        await ApiVersionSet.ExportAll(serviceDirectory, serviceUri, listRestResources, getRestResource, logger, cancellationToken);

        logger.LogInformation("Exporting loggers...");
        await Logger.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, listRestResources, getRestResource, logger, loggerNamesToExport, cancellationToken);

        logger.LogInformation("Exporting diagnostics...");
        await Diagnostic.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, listRestResources, getRestResource, logger, diagnosticNamesToExport, cancellationToken);

        logger.LogInformation("Exporting backends...");
        await Backend.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, listRestResources, getRestResource, logger, backendNamesToExport, cancellationToken);

        logger.LogInformation("Exporting products...");
        await Product.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, apiNamesToExport, listRestResources, getRestResource, 
        logger, isProductGroupExportEnabled, productNamesToExport,cancellationToken);

        logger.LogInformation("Exporting gateways...");
        await Gateway.ExportAll(serviceDirectory, serviceUri, apiNamesToExport, listRestResources, getRestResource, logger, cancellationToken);

        logger.LogInformation("Exporting policy fragments...");
        await PolicyFragment.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, policyFragmentNamesToExport, listRestResources, getRestResource, logger, cancellationToken);

        logger.LogInformation("Exporting service policies...");
        await ServicePolicy.ExportAll(serviceUri, serviceDirectory, listRestResources, getRestResource, logger, cancellationToken);

        logger.LogInformation("Exporting apis...");
        await Api.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, defaultSpecification, apiNamesToExport, listRestResources, getRestResource, downloadResource, logger, cancellationToken);

        logger.LogInformation("Exporting subscriptions...");
        await Subscription.ExportAll(IsFilteringEnabled,serviceDirectory, serviceUri, listRestResources, getRestResource, logger, subscriptionNamesToExport, cancellationToken);
    }
    public static Boolean ShouldExport(Boolean IsFilteringEnabled, string resourceName, IEnumerable<string>? resourceNamesToExport){

        if(!IsFilteringEnabled){
            return true;
        }
        else {
            return  resourceNamesToExport is not null 
        && resourceNamesToExport.Any(resourceNameToExport => resourceNameToExport.Equals(resourceName.ToString(), StringComparison.OrdinalIgnoreCase));     
    }
    }
}