﻿using common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace extractor;

internal static class NamedValue
{
    public static async ValueTask ExportAll(Boolean IsFilteringEnabled, ServiceDirectory serviceDirectory, ServiceUri serviceUri, ListRestResources listRestResources, GetRestResource getRestResource, ILogger logger, IEnumerable<string>? namedValueNamesToExport, CancellationToken cancellationToken)
    {
        await List(serviceUri, listRestResources, cancellationToken)
                // Filter out namedValues that should not be exported
                .Where(namedValueName => ShouldExport(IsFilteringEnabled,namedValueName, namedValueNamesToExport))
                .ForEachParallel(async namedValueName => await Export(serviceDirectory, serviceUri, namedValueName, getRestResource, logger, cancellationToken),
                                 cancellationToken);
    }

    private static IAsyncEnumerable<NamedValueName> List(ServiceUri serviceUri, ListRestResources listRestResources, CancellationToken cancellationToken)
    {
        var namedValuesUri = new NamedValuesUri(serviceUri);
        var namedValueJsonObjects = listRestResources(namedValuesUri.Uri, cancellationToken);
        return namedValueJsonObjects.Select(json => json.GetStringProperty("name"))
                                    .Select(name => new NamedValueName(name));
    }

    private static bool ShouldExport(Boolean IsFilteringEnabled, NamedValueName namedValueName, IEnumerable<string>? namedValueNamesToExport)
    {
       return Service.ShouldExport(IsFilteringEnabled, namedValueName.ToString(), namedValueNamesToExport);
    }

    private static async ValueTask Export(ServiceDirectory serviceDirectory, ServiceUri serviceUri, NamedValueName namedValueName, GetRestResource getRestResource, ILogger logger, CancellationToken cancellationToken)
    {
        var namedValuesDirectory = new NamedValuesDirectory(serviceDirectory);
        var namedValueDirectory = new NamedValueDirectory(namedValueName, namedValuesDirectory);

        var namedValuesUri = new NamedValuesUri(serviceUri);
        var namedValueUri = new NamedValueUri(namedValueName, namedValuesUri);

        await ExportInformationFile(namedValueDirectory, namedValueUri, namedValueName, getRestResource, logger, cancellationToken);
    }

    private static async ValueTask ExportInformationFile(NamedValueDirectory namedValueDirectory, NamedValueUri namedValueUri, NamedValueName namedValueName, GetRestResource getRestResource, ILogger logger, CancellationToken cancellationToken)
    {
        var namedValueInformationFile = new NamedValueInformationFile(namedValueDirectory);

        var responseJson = await getRestResource(namedValueUri.Uri, cancellationToken);
        var namedValueModel = NamedValueModel.Deserialize(namedValueName, responseJson);
        var contentJson = namedValueModel.Serialize();

        logger.LogInformation("Writing named value information file {filePath}...", namedValueInformationFile.Path);
        await namedValueInformationFile.OverwriteWithJson(contentJson, cancellationToken);
    }
}