// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Diagnostics;
#if EXPOSE_EXPERIMENTAL_FEATURES && NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter.Kafka;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Logs;

/// <summary>
/// Extension methods to simplify registering of the Kafka exporter.
/// </summary>
public static class KafkaLogsExporterExtensions
{
    /// <summary>
    /// Adds an Kafka Exporter to the OpenTelemetry <see cref="ILoggerProvider"/>.
    /// </summary>
    /// <remarks><inheritdoc cref="AddKafkaExporter(OpenTelemetryLoggerOptions, Action{KafkaExporterOptions})" path="/remarks"/></remarks>
    /// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/> options to use.</param>
    /// <returns>The instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
    public static OpenTelemetryLoggerOptions AddKafkaExporter(this OpenTelemetryLoggerOptions loggerOptions)
        => AddKafkaExporter(loggerOptions, name: null, configure: null);

    /// <summary>
    /// Adds an Kafka Exporter to the OpenTelemetry <see cref="ILoggerProvider"/>.
    /// </summary>
    /// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/> options to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="KafkaExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
    public static OpenTelemetryLoggerOptions AddKafkaExporter(
        this OpenTelemetryLoggerOptions loggerOptions,
        Action<KafkaExporterOptions> configure)
        => AddKafkaExporter(loggerOptions, name: null, configure);

    /// <summary>
    /// Adds an Kafka Exporter to the OpenTelemetry <see cref="ILoggerProvider"/>.
    /// </summary>
    /// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/> options to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="KafkaExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
    public static OpenTelemetryLoggerOptions AddKafkaExporter(
        this OpenTelemetryLoggerOptions loggerOptions,
        string? name,
        Action<KafkaExporterOptions>? configure)
    {
        Guard.ThrowIfNull(loggerOptions);

        var finalOptionsName = name ?? Options.DefaultName;

        return loggerOptions.AddProcessor(sp =>
        {
            var exporterOptions = GetOptions<KafkaExporterOptions>(sp, name, finalOptionsName, KafkaExporterOptions.CreateKafkaExporterOptions);

            var processorOptions = sp.GetRequiredService<IOptionsMonitor<LogRecordExportProcessorOptions>>().Get(finalOptionsName);

            configure?.Invoke(exporterOptions);

            return BuildKafkaLogsExporter(
                sp,
                exporterOptions,
                processorOptions,
                GetOptions(sp, Options.DefaultName, Options.DefaultName, (sp, c, n) => new SdkLimitOptions(c)),
                GetOptions(sp, name, finalOptionsName, (sp, c, n) => new ExperimentalOptions(c)));
        });
    }

    /// <summary>
    /// Adds an Kafka Exporter to the OpenTelemetry <see cref="ILoggerProvider"/>.
    /// </summary>
    /// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/> options to use.</param>
    /// <param name="configureExporterAndProcessor">Callback action for configuring <see cref="KafkaExporterOptions"/> and <see cref="LogRecordExportProcessorOptions"/>.</param>
    /// <returns>The instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
    public static OpenTelemetryLoggerOptions AddKafkaExporter(
        this OpenTelemetryLoggerOptions loggerOptions,
        Action<KafkaExporterOptions, LogRecordExportProcessorOptions> configureExporterAndProcessor)
        => AddKafkaExporter(loggerOptions, name: null, configureExporterAndProcessor);

    /// <summary>
    /// Adds an Kafka Exporter to the OpenTelemetry <see cref="ILoggerProvider"/>.
    /// </summary>
    /// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/> options to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configureExporterAndProcessor">Optional callback action for configuring <see cref="KafkaExporterOptions"/> and <see cref="LogRecordExportProcessorOptions"/>.</param>
    /// <returns>The instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
    public static OpenTelemetryLoggerOptions AddKafkaExporter(
        this OpenTelemetryLoggerOptions loggerOptions,
        string? name,
        Action<KafkaExporterOptions, LogRecordExportProcessorOptions>? configureExporterAndProcessor)
    {
        Guard.ThrowIfNull(loggerOptions);

        var finalOptionsName = name ?? Options.DefaultName;

        return loggerOptions.AddProcessor(sp =>
        {
            var exporterOptions = GetOptions<KafkaExporterOptions>(sp, name, finalOptionsName, KafkaExporterOptions.CreateKafkaExporterOptions);

            var processorOptions = sp.GetRequiredService<IOptionsMonitor<LogRecordExportProcessorOptions>>().Get(finalOptionsName);

            configureExporterAndProcessor?.Invoke(exporterOptions, processorOptions);

            return BuildKafkaLogsExporter(
                sp,
                exporterOptions,
                processorOptions,
                GetOptions(sp, Options.DefaultName, Options.DefaultName, (sp, c, n) => new SdkLimitOptions(c)),
                GetOptions(sp, name, finalOptionsName, (sp, c, n) => new ExperimentalOptions(c)));
        });
    }

#if EXPOSE_EXPERIMENTAL_FEATURES
    /// <summary>
    /// Adds an OTLP exporter to the LoggerProvider.
    /// </summary>
    /// <remarks><b>WARNING</b>: This is an experimental API which might change or be removed in the future. Use at your own risk.</remarks>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
#if NET8_0_OR_GREATER
    [Experimental(DiagnosticDefinitions.LoggerProviderExperimentalApi, UrlFormat = DiagnosticDefinitions.ExperimentalApiUrlFormat)]
#endif
    public
#else
    /// <summary>
    /// Adds an Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    internal
#endif
        static LoggerProviderBuilder AddKafkaExporter(this LoggerProviderBuilder builder)
        => AddKafkaExporter(builder, name: null, configureExporter: null);

#if EXPOSE_EXPERIMENTAL_FEATURES
    /// <summary>
    /// Adds an Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <remarks><b>WARNING</b>: This is an experimental API which might change or be removed in the future. Use at your own risk.</remarks>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <param name="configureExporter">Callback action for configuring <see cref="KafkaExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
#if NET8_0_OR_GREATER
    [Experimental(DiagnosticDefinitions.LoggerProviderExperimentalApi, UrlFormat = DiagnosticDefinitions.ExperimentalApiUrlFormat)]
#endif
    public
#else
    /// <summary>
    /// Adds an Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <param name="configureExporter">Callback action for configuring <see cref="KafkaExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    internal
#endif
        static LoggerProviderBuilder AddKafkaExporter(this LoggerProviderBuilder builder, Action<KafkaExporterOptions> configureExporter)
        => AddKafkaExporter(builder, name: null, configureExporter);

#if EXPOSE_EXPERIMENTAL_FEATURES
    /// <summary>
    /// Adds an Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <remarks><b>WARNING</b>: This is an experimental API which might change or be removed in the future. Use at your own risk.</remarks>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <param name="configureExporterAndProcessor">Callback action for
    /// configuring <see cref="KafkaExporterOptions"/> and <see
    /// cref="LogRecordExportProcessorOptions"/>.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
#if NET8_0_OR_GREATER
    [Experimental(DiagnosticDefinitions.LoggerProviderExperimentalApi, UrlFormat = DiagnosticDefinitions.ExperimentalApiUrlFormat)]
#endif
    public
#else
    internal
#endif
        static LoggerProviderBuilder AddKafkaExporter(this LoggerProviderBuilder builder, Action<KafkaExporterOptions, LogRecordExportProcessorOptions> configureExporterAndProcessor)
        => AddKafkaExporter(builder, name: null, configureExporterAndProcessor);

#if EXPOSE_EXPERIMENTAL_FEATURES
    /// <summary>
    /// Adds Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <remarks><b>WARNING</b>: This is an experimental API which might change or be removed in the future. Use at your own risk.</remarks>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configureExporter">Optional callback action for configuring <see cref="KafkaExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
#if NET8_0_OR_GREATER
    [Experimental(DiagnosticDefinitions.LoggerProviderExperimentalApi, UrlFormat = DiagnosticDefinitions.ExperimentalApiUrlFormat)]
#endif
    public
#else
    /// <summary>
    /// Adds Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configureExporter">Optional callback action for configuring <see cref="KafkaExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    internal
#endif
        static LoggerProviderBuilder AddKafkaExporter(
        this LoggerProviderBuilder builder,
        string? name,
        Action<KafkaExporterOptions>? configureExporter)
    {
        var finalOptionsName = name ?? Options.DefaultName;

        builder.ConfigureServices(services =>
        {
            if (name != null && configureExporter != null)
            {
                // If we are using named options we register the
                // configuration delegate into options pipeline.
                services.Configure(finalOptionsName, configureExporter);
            }

            RegisterOptions(services);
        });

        return builder.AddProcessor(sp =>
        {
            KafkaExporterOptions exporterOptions;

            if (name == null)
            {
                // If we are NOT using named options we create a new
                // instance always. The reason for this is
                // OtlpExporterOptions is shared by all signals. Without a
                // name, delegates for all signals will mix together. See:
                // https://github.com/open-telemetry/opentelemetry-dotnet/issues/4043
                exporterOptions = sp.GetRequiredService<IOptionsFactory<KafkaExporterOptions>>().Create(finalOptionsName);

                // Configuration delegate is executed inline on the fresh instance.
                configureExporter?.Invoke(exporterOptions);
            }
            else
            {
                // When using named options we can properly utilize Options
                // API to create or reuse an instance.
                exporterOptions = sp.GetRequiredService<IOptionsMonitor<KafkaExporterOptions>>().Get(finalOptionsName);
            }

            // Note: Not using finalOptionsName here for SdkLimitOptions.
            // There should only be one provider for a given service
            // collection so SdkLimitOptions is treated as a single default
            // instance.
            var sdkLimitOptions = sp.GetRequiredService<IOptionsMonitor<SdkLimitOptions>>().CurrentValue;

            return BuildKafkaLogsExporter(
                sp,
                exporterOptions,
                sp.GetRequiredService<IOptionsMonitor<LogRecordExportProcessorOptions>>().Get(finalOptionsName),
                sdkLimitOptions,
                sp.GetRequiredService<IOptionsMonitor<ExperimentalOptions>>().Get(finalOptionsName));
        });
    }

#if EXPOSE_EXPERIMENTAL_FEATURES
    /// <summary>
    /// Adds an Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <remarks><b>WARNING</b>: This is an experimental API which might change or be removed in the future. Use at your own risk.</remarks>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configureExporterAndProcessor">Optional callback action for
    /// configuring <see cref="KafkaExporterOptions"/> and <see
    /// cref="LogRecordExportProcessorOptions"/>.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
#if NET8_0_OR_GREATER
    [Experimental(DiagnosticDefinitions.LoggerProviderExperimentalApi, UrlFormat = DiagnosticDefinitions.ExperimentalApiUrlFormat)]
#endif
    public
#else
    /// <summary>
    /// Adds an Kafka exporter to the LoggerProvider.
    /// </summary>
    /// <param name="builder"><see cref="LoggerProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configureExporterAndProcessor">Optional callback action for
    /// configuring <see cref="KafkaExporterOptions"/> and <see
    /// cref="LogRecordExportProcessorOptions"/>.</param>
    /// <returns>The instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    internal
#endif
        static LoggerProviderBuilder AddKafkaExporter(
        this LoggerProviderBuilder builder,
        string? name,
        Action<KafkaExporterOptions, LogRecordExportProcessorOptions>? configureExporterAndProcessor)
    {
        var finalOptionsName = name ?? Options.DefaultName;

        builder.ConfigureServices(RegisterOptions);

        return builder.AddProcessor(sp =>
        {
            KafkaExporterOptions exporterOptions;

            if (name == null)
            {
                // If we are NOT using named options we create a new
                // instance always. The reason for this is
                // OtlpExporterOptions is shared by all signals. Without a
                // name, delegates for all signals will mix together. See:
                // https://github.com/open-telemetry/opentelemetry-dotnet/issues/4043
                exporterOptions = sp.GetRequiredService<IOptionsFactory<KafkaExporterOptions>>().Create(finalOptionsName);
            }
            else
            {
                // When using named options we can properly utilize Options
                // API to create or reuse an instance.
                exporterOptions = sp.GetRequiredService<IOptionsMonitor<KafkaExporterOptions>>().Get(finalOptionsName);
            }

            var processorOptions = sp.GetRequiredService<IOptionsMonitor<LogRecordExportProcessorOptions>>().Get(finalOptionsName);

            // Configuration delegate is executed inline.
            configureExporterAndProcessor?.Invoke(exporterOptions, processorOptions);

            // Note: Not using finalOptionsName here for SdkLimitOptions.
            // There should only be one provider for a given service
            // collection so SdkLimitOptions is treated as a single default
            // instance.
            var sdkLimitOptions = sp.GetRequiredService<IOptionsMonitor<SdkLimitOptions>>().CurrentValue;

            return BuildKafkaLogsExporter(
                sp,
                exporterOptions,
                processorOptions,
                sdkLimitOptions,
                sp.GetRequiredService<IOptionsMonitor<ExperimentalOptions>>().Get(finalOptionsName));
        });
    }

    internal static BaseProcessor<LogRecord> BuildKafkaLogsExporter(
        IServiceProvider sp,
        KafkaExporterOptions exporterOptions,
        LogRecordExportProcessorOptions processorOptions,
        SdkLimitOptions sdkLimitOptions,
        ExperimentalOptions experimentalOptions,
        Func<BaseExporter<LogRecord>, BaseExporter<LogRecord>>? configureExporterInstance = null)
    {
        // Note: sp is not currently used by this method but it should be used
        // at some point for IHttpClientFactory integration.
        Debug.Assert(sp != null, "sp was null");
        Debug.Assert(exporterOptions != null, "exporterOptions was null");
        Debug.Assert(processorOptions != null, "processorOptions was null");
        Debug.Assert(sdkLimitOptions != null, "sdkLimitOptions was null");
        Debug.Assert(experimentalOptions != null, "experimentalOptions was null");

        /*
         * Note:
         *
         * We don't currently enable IHttpClientFactory for OtlpLogExporter.
         *
         * The DefaultHttpClientFactory requires the ILoggerFactory in its ctor:
         * https://github.com/dotnet/runtime/blob/fa40ecf7d36bf4e31d7ae968807c1c529bac66d6/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L64
         *
         * This creates a circular reference: ILoggerFactory ->
         * OpenTelemetryLoggerProvider -> OtlpLogExporter -> IHttpClientFactory
         * -> ILoggerFactory
         *
         * exporterOptions.TryEnableIHttpClientFactoryIntegration(sp,
         * "OtlpLogExporter");
         */

        BaseExporter<LogRecord> kafkaExporter = new KafkaLogsExporter(
            exporterOptions!,
            sdkLimitOptions!,
            experimentalOptions!);

        if (configureExporterInstance != null)
        {
            kafkaExporter = configureExporterInstance(kafkaExporter);
        }

        if (processorOptions!.ExportProcessorType == ExportProcessorType.Simple)
        {
            return new SimpleLogRecordExportProcessor(kafkaExporter);
        }
        else
        {
            var batchOptions = processorOptions.BatchExportProcessorOptions;

            return new BatchLogRecordExportProcessor(
                kafkaExporter,
                batchOptions.MaxQueueSize,
                batchOptions.ScheduledDelayMilliseconds,
                batchOptions.ExporterTimeoutMilliseconds,
                batchOptions.MaxExportBatchSize);
        }
    }

    private static void RegisterOptions(IServiceCollection services)
    {
        KafkaExporterOptions.RegisterKafkaExporterOptionsFactory(services);
        services.RegisterOptionsFactory(configuration => new SdkLimitOptions(configuration));
        services.RegisterOptionsFactory(configuration => new ExperimentalOptions(configuration));
    }

    private static T GetOptions<T>(
        IServiceProvider sp,
        string? name,
        string finalName,
        Func<IServiceProvider, IConfiguration, string, T> createOptionsFunc)
        where T : class, new()
    {
        // Note: If OtlpExporter has been registered for tracing and/or metrics
        // then IOptionsFactory will be set by a call to
        // OtlpExporterOptions.RegisterOtlpExporterOptionsFactory. However, if we
        // are only using logging, we don't have an opportunity to do that
        // registration so we manually create a factory.

        var optionsFactory = sp.GetRequiredService<IOptionsFactory<T>>();
        if (optionsFactory is not DelegatingOptionsFactory<T>)
        {
            optionsFactory = new DelegatingOptionsFactory<T>(
                (c, n) => createOptionsFunc(sp, c, n),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetServices<IConfigureOptions<T>>(),
                sp.GetServices<IPostConfigureOptions<T>>(),
                sp.GetServices<IValidateOptions<T>>());

            return optionsFactory.Create(finalName);
        }

        if (name == null)
        {
            // If we are NOT using named options we create a new
            // instance always. The reason for this is
            // OtlpExporterOptions is shared by all signals. Without a
            // name, delegates for all signals will mix together.
            return optionsFactory.Create(finalName);
        }

        // If we have a valid factory AND we are using named options, we can
        // safely use the Options API fully.
        return sp.GetRequiredService<IOptionsMonitor<T>>().Get(finalName);
    }
}