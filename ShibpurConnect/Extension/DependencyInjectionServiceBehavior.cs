using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Web;

namespace ShibpurConnect.Extension
{
    public class DependencyInjectionServiceBehavior : IServiceBehavior
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyInjectionServiceBehavior"/> class.
        /// </summary>
        /// <param name="dependencyInjector">The dependency injector.</param>
        public DependencyInjectionServiceBehavior(IUnityContainer dependencyInjector)
        {
            this.DependencyInjector = dependencyInjector;
        }

        /// <summary>
        /// Gets the dependency injector.
        /// </summary>
        public IUnityContainer DependencyInjector { get; private set; }

        /// <summary>
        /// Gets or sets the type to resolve (if not set, it will use the type of the service).
        /// </summary>
        public Type TypeToResolve { get; set; }

        /// <summary>
        /// Provides the ability to change run-time property values or insert custom extension objects such as error handlers, message or parameter interceptors, security extensions, and other custom extension objects.
        /// </summary>
        /// <param name="serviceDescription">The service description.</param>
        /// <param name="serviceHostBase">The host that is currently being built.</param>
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var channelDispatcher in serviceHostBase.ChannelDispatchers.OfType<ChannelDispatcher>())
            {
                foreach (var endpointDispatcher in channelDispatcher.Endpoints)
                {
                    var typeToResolve = this.TypeToResolve ?? serviceDescription.ServiceType;

                    // Interface interception relies on the intercepted type being an interface.
                    // By convention, our service types (e.g. Service) implement an interface with
                    // the same name prefixed with an I (e.g. IService).  If the type to resolve
                    // is a concrete class, try to resolve the associated interface instead.
                    if (!typeToResolve.IsInterface)
                    {
                        var interfaces = typeToResolve.GetInterfaces();
                        var typeName = typeToResolve.Name;
                        var candidate = interfaces.SingleOrDefault(i => i.Name == "I" + typeName);
                        if (candidate != null)
                        {
                            typeToResolve = candidate;
                        }
                    }

                    var instanceProvider = new DependencyInjectionInstanceProvider(this.DependencyInjector, typeToResolve);

                    endpointDispatcher.DispatchRuntime.InstanceProvider = instanceProvider;
                }
            }
        }

        /// <summary>
        /// Adds the binding parameters.
        /// </summary>
        /// <param name="serviceDescription">The service description.</param>
        /// <param name="serviceHostBase">The service host base.</param>
        /// <param name="endpoints">The endpoints.</param>
        /// <param name="bindingParameters">The binding parameters.</param>
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        /// <summary>
        /// Provides the ability to inspect the service host and the service description to confirm that the service can run successfully.
        /// </summary>
        /// <param name="serviceDescription">The service description.</param>
        /// <param name="serviceHostBase">The service host that is currently being constructed.</param>
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
    }
}