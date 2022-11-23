using System.ComponentModel;
using System.Reflection;
using Elsa.Common.Extensions;
using Elsa.Common.Features;
using Elsa.Expressions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Features;
using Elsa.Workflows.Core.Features;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(SystemClockFeature))]
[DependsOn(typeof(WorkflowsFeature))]
public class WorkflowManagementFeature : FeatureBase
{
    public WorkflowManagementFeature(IModule module) : base(module)
    {
    }
    
    public HashSet<Type> ActivityTypes { get; } = new();
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();
    public Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>();

    public WorkflowManagementFeature AddActivity<T>() where T : IActivity
    {
        ActivityTypes.Add(typeof(T));
        return this;
    }
    
    public WorkflowManagementFeature AddActivitiesFrom<TMarker>()
    {
        var activityTypes = typeof(TMarker).Assembly.GetExportedTypes().Where(x =>
        {
            var browsableAttr = x.GetCustomAttribute<BrowsableAttribute>();
            var isBrowsable = browsableAttr == null || browsableAttr.Browsable; 
            return typeof(IActivity).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface && !x.IsGenericType && isBrowsable;
        }).ToList();
        return AddActivities(activityTypes);
    }
    
    public WorkflowManagementFeature AddActivities(IEnumerable<Type> activityTypes)
    {
        ActivityTypes.AddRange(activityTypes);
        return this;
    }

    public override void Apply()
    {
        Services
            .AddMemoryStore<WorkflowDefinition, MemoryWorkflowDefinitionStore>()
            .AddMemoryStore<WorkflowInstance, MemoryWorkflowInstanceStore>()
            .AddActivityProvider<TypedActivityProvider>()
            .AddSingleton(WorkflowInstanceStore)
            .AddSingleton(WorkflowDefinitionStore)
            .AddSingleton<IWorkflowDefinitionPublisher, WorkflowDefinitionPublisher>()
            .AddSingleton<IWorkflowDefinitionManager, WorkflowDefinitionManager>()
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddSingleton<IActivityRegistryPopulator, ActivityRegistryPopulator>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyOptionsResolver, PropertyOptionsResolver>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddSingleton<IExpressionSyntaxRegistry, ExpressionSyntaxRegistry>()
            .AddSingleton<IExpressionSyntaxProvider, DefaultExpressionSyntaxProvider>()
            .AddSingleton<IExpressionSyntaxRegistryPopulator, ExpressionSyntaxRegistryPopulator>()
            .AddSingleton<ISerializationOptionsConfigurator, SerializationOptionsConfigurator>()
            .AddSingleton<IWorkflowMaterializer, ClrWorkflowMaterializer>()
            .AddSingleton<IWorkflowMaterializer, JsonWorkflowMaterializer>()
            .AddSingleton<SerializerOptionsProvider>()
            ;

        Services.Configure<ApiOptions>(options =>
        {
            foreach (var activityType in ActivityTypes) 
                options.ActivityTypes.Add(activityType);
        });
    }
}