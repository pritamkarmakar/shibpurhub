using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Web;

namespace ShibpurConnect.Extension
{
    public class DependencyInjectionInstanceProvider : IInstanceProvider
    {
        /// <summary>
        /// Service type.
        /// </summary>
        private readonly Type serviceType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyInjectionInstanceProvider"/> class.
        /// </summary>
        /// <param name="dependencyInjector">The dependency injector.</param>
        /// <param name="serviceType">Type of the service.</param>
        public DependencyInjectionInstanceProvider(IUnityContainer dependencyInjector, Type serviceType)
        {
            this.DependencyInjector = dependencyInjector;
            this.serviceType = serviceType;
        }

        /// <summary>
        /// Gets the dependency injector.
        /// </summary>
        public IUnityContainer DependencyInjector { get; private set; }

        /// <summary>
        /// Returns a service object given the specified <see cref="T:System.ServiceModel.InstanceContext"/> object.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="T:System.ServiceModel.InstanceContext"/> object.</param>
        /// <returns>A user-defined service object.</returns>
        public object GetInstance(InstanceContext instanceContext)
        {
            return this.GetInstance(instanceContext, null);
        }

        /// <summary>
        /// Returns a service object given the specified <see cref="T:System.ServiceModel.InstanceContext"/> object.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="T:System.ServiceModel.InstanceContext"/> object.</param>
        /// <param name="message">The message that triggered the creation of a service object.</param>
        /// <returns>The service object.</returns>
        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            return this.DependencyInjector.Resolve(this.serviceType);
        }

        /// <summary>
        /// Called when an <see cref="T:System.ServiceModel.InstanceContext"/> object recycles a service object.
        /// </summary>
        /// <param name="instanceContext">The service's instance context.</param>
        /// <param name="instance">The service object to be recycled.</param>
        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
        }
    }
}