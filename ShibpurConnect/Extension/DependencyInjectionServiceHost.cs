using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;

namespace ShibpurConnect.Extension
{
    public class DependencyInjectionServiceHost : ServiceHost
    {
        /// <summary>
        /// Initializes a new instance of the DependencyInjectionServiceHost class.
        /// </summary>
        /// <param name="dependencyInjector">The dependency injector.</param>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        public DependencyInjectionServiceHost(IUnityContainer dependencyInjector, Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            this.DependencyInjector = dependencyInjector;
            this.Description.Behaviors.Add(new DependencyInjectionServiceBehavior(this.DependencyInjector));
        }

        /// <summary>
        /// Gets the dependency injector.
        /// </summary>
        public IUnityContainer DependencyInjector { get; private set; }
    }
}