using Microsoft.Practices.Unity;
using SCDataAccess;
using SCDataAccess.Implementation;
using SCDataAccess.Interface;
using ShibpurConnect.Extension;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ShibpurConnect
{
    public static class UnityConfiguration
    {
        public static void ConfigureUnity()
        {
            UnityContainer unityContainer = new UnityContainer();

                unityContainer.RegisterType<ICache, LocalMemoryCache>("UsersCache", new ContainerControlledLifetimeManager());
                unityContainer.RegisterType<ICache, LocalMemoryCache>("Ticket", new ContainerControlledLifetimeManager());

                unityContainer.RegisterType<IContainer, SCCatalogEntities>(new PerThreadLifetimeManager());
                unityContainer.RegisterType<IUserAccess, UserAccess>(new PerThreadLifetimeManager());
            //unityContainer.RegisterType<IServiceBusSubscribeProvider, ServiceBusSubscribeProvider>(new PerThreadLifetimeManager());
            //unityContainer.RegisterType<ILearnerCourseActivityProvider, CachedLearnerCourseActivityProvider>(new PerThreadLifetimeManager());
            //unityContainer.RegisterType<ILearnerCourseActivityProvider, LearnerCourseActivityProvider>("UncachedLearnerCourseActivityProvider", new PerThreadLifetimeManager());
            //unityContainer.RegisterType<ILearnerCourseAssessmentProvider, LearnerCourseAssessmentProvider>("UncachedLearnerCourseAssessmentProvider", new PerThreadLifetimeManager());
 
            ////unity registration for UserAcademicStatus Logic Provider
            //unityContainer.RegisterType<IUserAcademicStatusLogicProvider, UserAcademicStatusLogicProvider>();

            DependencyInjectionServiceFactory.DependencyInjector = unityContainer;
            //IdentityExtensionsConfigurationManager.ProviderConfigs = unityContainer.Resolve<IAuthenticationIdentityConfigProvider>().GetAuthenticationIdentityProviderConfigs();
        }

        private static string GetConfigurationValue(string configurationKey)
        {
            return ConfigurationManager.AppSettings.Get(configurationKey);
        }
    }
}