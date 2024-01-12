// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Diagnostics;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient;
using OpenTelemetry.Internal;
using OtlpCollector = OpenTelemetry.Proto.Collector.Trace.V1;
using OtlpResource = OpenTelemetry.Proto.Resource.V1;

namespace OpenTelemetry.Exporter.Kafka;

/// <summary>
/// Exporter consuming <see cref="Activity"/> and exporting the data to Kafka.
/// </summary>
public class KafkaTraceExporter : BaseExporter<Activity>
{
    private readonly SdkLimitOptions? sdkLimitOptions;

    private readonly ExperimentalOptions? experimentalOptions;

    private readonly IExportClient<OtlpCollector.ExportTraceServiceRequest> exportClient;

    private OtlpResource.Resource? processResource;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaTraceExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    public KafkaTraceExporter(KafkaExporterOptions options)
        : this(options, sdkLimitOptions: new(), experimentalOptions: new(), exportClient: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaTraceExporter"/> class.
    /// </summary>
    /// <param name="exporterOptions"><see cref="KafkaExporterOptions"/>.</param>
    /// <param name="sdkLimitOptions"><see cref="SdkLimitOptions"/>.</param>
    /// <param name="experimentalOptions"><see cref="ExperimentalOptions"/>.</param>
    /// <param name="exportClient">Client used for sending export request.</param>
    internal KafkaTraceExporter(
        KafkaExporterOptions exporterOptions,
        SdkLimitOptions? sdkLimitOptions,
        ExperimentalOptions? experimentalOptions,
        IExportClient<OtlpCollector.ExportTraceServiceRequest>? exportClient = null)
    {
        Debug.Assert(exporterOptions != null, "exporterOptions was null");
        Debug.Assert(sdkLimitOptions != null, "sdkLimitOptions was null");

        // Debug.Assert(experimentalOptions != null, "experimentalOptions was null");
        this.sdkLimitOptions = sdkLimitOptions;
        this.experimentalOptions = experimentalOptions;
        OtlpKeyValueTransformer.LogUnsupportedAttributeType = (string tagValueType, string tagKey) =>
        {
            OpenTelemetryProtocolExporterEventSource.Log.UnsupportedAttributeType(tagValueType, tagKey);
        };
        ConfigurationExtensions.LogInvalidEnvironmentVariable = (string key, string value) =>
        {
            OpenTelemetryProtocolExporterEventSource.Log.InvalidEnvironmentVariable(key, value);
        };
        if (exportClient != null)
        {
            this.exportClient = exportClient;
        }
        else
        {
            this.exportClient = exporterOptions!.GetTraceExportClient();
        }
    }

    internal OtlpResource.Resource ProcessResource
         => this.processResource ??= this.ParentProvider.GetResource().ToOtlpResource();

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> activityBatch)
    {
        // Prevents the exporter's gRPC and HTTP operations from being instrumented.
        using var scope = SuppressInstrumentationScope.Begin();
        var request = new OtlpCollector.ExportTraceServiceRequest();
        try
        {
            request.AddBatch(this.sdkLimitOptions, this.ProcessResource, activityBatch);
            if (!this.exportClient.SendExportRequest(request))
            {
                return ExportResult.Failure;
            }
        }
        catch (Exception ex)
        {
            OpenTelemetryProtocolExporterEventSource.Log.ExportMethodException(ex);
            return ExportResult.Failure;
        }
        finally
        {
            request.Return();
        }

        return ExportResult.Success;
    }

    /// <inheritdoc />
    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        return this.exportClient?.Shutdown(timeoutMilliseconds) ?? true;
    }
}