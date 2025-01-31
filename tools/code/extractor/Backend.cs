﻿using common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace extractor;

internal static class Backend
{
    public static async ValueTask ExportAll(Boolean IsFilteringEnabled, ServiceDirectory serviceDirectory, ServiceUri serviceUri, ListRestResources listRestResources, GetRestResource getRestResource, ILogger logger, IEnumerable<string>? backendNamesToExport, CancellationToken cancellationToken)
    {
        await List(serviceUri, listRestResources, cancellationToken)
                // Filter out tags that should not be exported
                .Where(backendName => ShouldExport(IsFilteringEnabled,backendName, backendNamesToExport))
                .ForEachParallel(async backendName => await Export(serviceDirectory, serviceUri, backendName, getRestResource, logger, cancellationToken),
                                 cancellationToken);
    }

    private static IAsyncEnumerable<BackendName> List(ServiceUri serviceUri, ListRestResources listRestResources, CancellationToken cancellationToken)
    {
        var backendsUri = new BackendsUri(serviceUri);
        var backendJsonObjects = listRestResources(backendsUri.Uri, cancellationToken);
        return backendJsonObjects.Select(json => json.GetStringProperty("name"))
                                 .Select(name => new BackendName(name));
    }

    private static bool ShouldExport(Boolean IsFilteringEnabled, BackendName backendName, IEnumerable<string>? backendNamesToExport)
    {
        return Service.ShouldExport(IsFilteringEnabled,backendName.ToString(),backendNamesToExport);
    }

    private static async ValueTask Export(ServiceDirectory serviceDirectory, ServiceUri serviceUri, BackendName backendName, GetRestResource getRestResource, ILogger logger, CancellationToken cancellationToken)
    {
        var backendsDirectory = new BackendsDirectory(serviceDirectory);
        var backendDirectory = new BackendDirectory(backendName, backendsDirectory);

        var backendsUri = new BackendsUri(serviceUri);
        var backendUri = new BackendUri(backendName, backendsUri);

        await ExportInformationFile(backendDirectory, backendUri, backendName, getRestResource, logger, cancellationToken);
    }

    private static async ValueTask ExportInformationFile(BackendDirectory backendDirectory, BackendUri backendUri, BackendName backendName, GetRestResource getRestResource, ILogger logger, CancellationToken cancellationToken)
    {
        var backendInformationFile = new BackendInformationFile(backendDirectory);

        var responseJson = await getRestResource(backendUri.Uri, cancellationToken);
        var backendModel = BackendModel.Deserialize(backendName, responseJson);
        var contentJson = backendModel.Serialize();

        logger.LogInformation("Writing backend information file {filePath}...", backendInformationFile.Path);
        await backendInformationFile.OverwriteWithJson(contentJson, cancellationToken);
    }
}